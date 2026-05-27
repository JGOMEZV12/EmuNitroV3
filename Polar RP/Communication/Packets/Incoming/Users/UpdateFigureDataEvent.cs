using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Global;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Quests;

using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.Database.Interfaces;
using Polar.Communication.Packets.Outgoing.Moderation;

using Polar.HabboRoleplay.Misc;

namespace Polar.Communication.Packets.Incoming.Users
{
    internal class UpdateFigureDataEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            // Validación inicial reforzada
            if (Session?.GetHabbo() == null || Packet == null)
                return;

            // Obtener RoomUser de forma segura
            var roomUser = Session.GetRoomUser();
            if (roomUser == null)
                return;

            // Generar Room con validación
            if (!RoleplayManager.GenerateRoom(roomUser.RoomId, out Room Room))
                return;

            string Gender;
            string Look;
            try
            {
                Gender = Packet.PopString()?.ToUpper() ?? "M";
                Look = Packet.PopString() ?? Session.GetHabbo().Look;
            }
            catch
            {
                return;
            }


            Look = PolarEnvironment.GetGame().GetFigureManager().ProcessFigure(Look, Gender, true);
            // Validar ClothingRoom
            string clothingRoomData = RoleplayData.GetData("clothing", "roomid");
            if (!int.TryParse(clothingRoomData, out int ClothingRoom))
                ClothingRoom = 18; // Valor por defecto seguro

            // Validar estado en sala
            if (!Session.GetHabbo().InRoom || Session.GetHabbo().CurrentRoom == null)
                return;

            #region Validaciones de Contexto
            if (Session.GetHabbo().Rank < 3)
            {
                bool wardrobeAccess = Room.WardrobeEnabled
                    || roomUser.RoomId == 18
                    || roomUser.RoomId == ClothingRoom
                    || Session.GetRoleplay()?.NearItem("uni_wardrobe", 1) == true;

                if (!wardrobeAccess)
                {
                    Session.SendNotification("¡Ve a una tienda de ropa o probador para cambiar!");
                    return;
                }
            }

            if (Session.GetRoleplay()?.IsWorking == true)
            {
                Session.SendNotification("¡No puedes cambiarte trabajando! Usa :notrabajar");
                return;
            }
            #endregion

            // Validar Look y créditos
            if (Look == Session.GetHabbo().Look || Session.GetHabbo().Credits < 250)
            {
                Session.SendWhisper("¡Ya tienes este look o créditos insuficientes!", 1);
                return;
            }

            // Bloqueo por spam
            if ((DateTime.Now - Session.GetHabbo().LastClothingUpdateTime).TotalSeconds <= 2.0)
            {
                Session.GetHabbo().ClothingUpdateWarnings++;
                if (Session.GetHabbo().ClothingUpdateWarnings >= 25)
                    Session.GetHabbo().SessionClothingBlocked = true;
                return;
            }

            if (Session.GetHabbo().SessionClothingBlocked)
                return;

            // Actualizar tiempo
            Session.GetHabbo().LastClothingUpdateTime = DateTime.Now;

            // Validar género
            string[] AllowedGenders = { "M", "F" };
            if (!AllowedGenders.Contains(Gender))
            {
                Session.SendMessage(new BroadcastMessageAlertComposer("Género no válido."));
                return;
            }

            // Actualizar base de datos con seguridad
            var dbManager = PolarEnvironment.GetDatabaseManager();
            if (dbManager != null)
            {
                using (IQueryAdapter dbClient = dbManager.GetQueryReactor())
                {
                    if (dbClient != null)
                    {
                        dbClient.SetQuery("UPDATE users SET look = @look, gender = @gender WHERE id = @id");
                        dbClient.AddParameter("look", PolarEnvironment.FilterFigure(Look));
                        dbClient.AddParameter("gender", Gender);
                        dbClient.AddParameter("id", Session.GetHabbo().Id);
                        dbClient.RunQuery();
                    }
                }
            }

            // Actualizar apariencia
            Session.GetRoleplay().OriginalOutfit = Look;
            Session.GetHabbo().Look = Look;
            Session.GetHabbo().Gender = Gender.ToLower();
            Session.SendMessage(new AvatarAspectUpdateComposer(Look, Gender));

            // Notificar sala
            if (Session.GetHabbo().CurrentRoom != null)
            {
                var currentRoomUser = Session.GetHabbo().CurrentRoom.GetRoomUserManager()?.GetRoomUserByHabbo(Session.GetHabbo().Id);
                if (currentRoomUser != null)
                {
                    Session.SendMessage(new UserChangeComposer(currentRoomUser, true));
                    Session.GetHabbo().CurrentRoom.SendMessage(new UserChangeComposer(currentRoomUser, false));
                }
            }

            // Manejar costos
            var houseManager = PolarEnvironment.GetGame()?.GetHouseManager();
            var apartmentManager = PolarEnvironment.GetGame()?.GetApartmentOwnedManager();

            bool isFreeChange = houseManager?.GetHouseByInsideRoom(Room.Id) != null
                              || apartmentManager?.GetApartmentByInsideRoom(Room.Id) != null;

            if (!isFreeChange)
            {
                Session.GetHabbo().Credits -= 250;
                Session.GetHabbo().UpdateCreditsBalance();
                RoleplayManager.Shout(Session, "*¡Ha comprado nuevo atuendo!*", 5);
                Session.GetRoleplay().ClearWebSocketDialogue();
                Session.GetRoleplay().RefreshStatDialogue();
            }

            // Actualizar tutorial
            if (Session.GetRoleplay()?.TutorialStep == 13 && Room.WardrobeEnabled)
            {
                Session.GetRoleplay().TutorialStep = 17;
                PolarEnvironment.GetGame()?.GetWebEventManager()?.SendDataDirect(Session, "compose_tutorial|16");
            }
        }
    }
}