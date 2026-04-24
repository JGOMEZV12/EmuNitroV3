using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using Polar.Utilities;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using Polar.HabboRoleplay.RPRoom;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Jobs.Types.Police
{
    class StunCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_police_stun"; }
        }

        public string Parameters
        {
            get { return "%user%"; }
        }

        public string Description
        {
            get { return "Paraliza a una persona para detenerla."; }
        }

        public async Task Execute(GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            if (Params.Length == 1)
            {
                Session.SendWhisper("Debes ingresar el nombre de la persona.", 1);
                return;
            }

            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);
            if (TargetClient == null)
            {
                Session.SendWhisper("Ha ocurrido un error al buscar a la persona, probablemente esté desconectada.", 1);
                return;
            }

            RoomUser RoomUser = Session.GetRoomUser();
            RoomUser TargetUser = Room.GetRoomUserManager().GetRoomUserByHabbo(TargetClient.GetHabbo().Username);
            if (TargetUser == null)
            {
                Session.SendWhisper("Ha ocurrido un error al buscar a la persona, probablemente esté desconectada o no está en esta zona.", 1);
                return;
            }
            if (!Session.GetHabbo().GetPermissions().HasRight("corporation_rights"))
            {
                if (TargetClient.GetHabbo().Id == Session.GetHabbo().Id)
                {
                    Session.SendWhisper("No puedes hacerte eso a ti mism@.", 1);
                    return;
                }
                if (GroupManager.HasJobCommand(TargetClient, "law"))
                {
                    Session.SendWhisper("¡No puedes hacer eso entre compañeros de trabajo!", 1);
                    return;
                }
                if (!TargetClient.GetRoleplay().IsWanted)
                {
                    Session.SendWhisper("No puedes hacer eso a alguien que no está en la lista de buscados.", 1);
                    return;
                }
            }
            /*if (Session != TargetClient && TargetClient.GetHabbo().Rank > 3 && TargetClient.GetHabbo().Rank >= Session.GetHabbo().Rank)
            {
                Session.SendWhisper("((No puedes hacerle eso a un miembro de la administración))", 1);
                return;
            }*/
            if (TargetClient.GetRoleplay().Cuffed)
            {
                Session.SendWhisper("Esta persona se encuentra esposada. No hace falta paralizarla.", 1);
                return;
            }
            if (!GroupManager.HasJobCommand(Session, "stun") && !Session.GetRoleplay().PoliceTrial && !Session.GetHabbo().GetPermissions().HasRight("corporation_rights"))
            {
                Session.SendWhisper("¡Solo un oficial de policía puede hacer eso!", 1);
                return;
            }

            if (!Session.GetRoleplay().IsWorking && !Session.GetRoleplay().PoliceTrial && !Session.GetHabbo().GetPermissions().HasRight("corporation_rights"))
            {
                Session.SendWhisper("Debes estar trabajando para hacer eso.", 1);
                return;
            }

            /*if (Session.GetRoleplay().DrivingCar || Session.GetRoleplay().DrivingInCar)
            {
                Session.SendWhisper("¡No puedes hacer eso mientras vas dentro de un vehículo!", 1);
                return;
            }*/

            if (TargetClient.GetRoleplay().IsDead)
            {
                Session.SendWhisper("¡No puedes hacerle eso a una persona muert@!", 1);
                return;
            }

            if (TargetClient.GetRoleplay().IsJailed)
            {
                Session.SendWhisper("¡No puedes hacerle eso a una persona encarcelad@!", 1);
                return;
            }

            /*if (TargetClient.GetRoleplay().DrivingCar)
            {
                Session.SendWhisper("¡No puedes hacerle eso a una persona mientras va conduciendo!", 1);
                return;
            }
            if (TargetClient.GetRoleplay().Pasajero)
            {
                Session.SendWhisper("¡No puedes hacerle eso a una persona mientras va de pasajer@!", 1);
                return;
            }*/

            if (TargetClient.GetRoomUser().Frozen)
            {
                Session.SendWhisper("¡No puedes aturdir a alguien que ya está aturdido, debes esposarlo!", 1);
                return;
            }

            if (TargetUser.IsAsleep)
            {
                Session.SendWhisper("¡No puedes aturdir a un usuario ausente!", 1);
                return;
            }
            if (Session.GetRoleplay().EquippedWeapon == null)
            {
                Session.SendWhisper("¡Debes equiparte el arma!", 1);
                return;  
            }

            if (Session.GetRoleplay().EquippedWeapon.Name != "electrica")
            {
                Session.SendWhisper("¡Debes equiparte la pistola electrica!", 1);
                return;
            }
            /* if (Session.GetRoleplay().TryGetCooldown("stun"))
                 return;*/
            #endregion

            #region Execute
            Point ClientPos = new Point(RoomUser.Coordinate.X, RoomUser.Coordinate.Y);
            Point TargetClientPos = new Point(TargetUser.Coordinate.X, TargetUser.Coordinate.Y);
            double Distance = RoleplayManager.GetDistanceBetweenPoints2D(ClientPos, TargetClientPos);
            string MyCity = Room.City;
            RPRoom Data;
            int JailRID = PolarEnvironment.GetGame().GetRPRoomManager().TryToGetJail(MyCity, out Data);
            int WantedTime = 5;
            CryptoRandom Random = new CryptoRandom();
            int Chance = Random.Next(1, 101);

            Session.GetRoomUser().ApplyEffect(536);

            RoleplayManager.Shout(Session, "*Dispara su pistola electrica hacia " + TargetClient.GetHabbo().Username + " inmovilizándolo inmediatamente*", 37);

            // Iniciar timer de stun
            TargetClient.GetRoleplay().TimerManager.CreateTimer("stun", 1000, false);

            // Configurar estado de stun
            TargetClient.GetRoleplay().IsStun = true;
            TargetClient.GetRoomUser().Frozen = true;
            TargetClient.GetRoomUser().CanWalk = false;
            TargetClient.GetRoomUser().ClearMovement(true);

            #region Desequipar al Convicto
            if (TargetClient.GetRoleplay().EquippedWeapon != null)
            {
                string UnEquipMessage = TargetClient.GetRoleplay().EquippedWeapon.UnEquipText;
                UnEquipMessage = UnEquipMessage.Replace("[NAME]", TargetClient.GetRoleplay().EquippedWeapon.PublicName);

                RoleplayManager.Shout(TargetClient, UnEquipMessage, 5);

                if (TargetClient.GetRoomUser().CurrentEffect == TargetClient.GetRoleplay().EquippedWeapon.EffectID)
                    TargetClient.GetRoomUser().ApplyEffect(0);

                if (TargetClient.GetRoomUser().CarryItemID == TargetClient.GetRoleplay().EquippedWeapon.HandItem)
                    TargetClient.GetRoomUser().CarryItem(0);

                TargetClient.GetRoleplay().CooldownManager.CreateCooldown("unequip", 1000, 3);
                TargetClient.GetRoleplay().EquippedWeapon = null;

                TargetClient.GetRoleplay().WLife = 0;
                TargetClient.GetRoleplay().Bullets = 0;
            }
            #endregion

            // Establecer cooldown de stun (CORREGIDO)
            TargetClient.GetRoleplay().SpecialCooldowns["stun"] = 1800;
           /* // O usando AddOrUpdate:
            // TargetClient.GetRoleplay().SpecialCooldowns.AddOrUpdate("stun", 1800, (key, oldValue) => 1800);

            if (!TargetClient.GetRoleplay().IsJailed)
            {
                TargetClient.GetRoleplay().IsJailed = true;
                TargetClient.GetRoleplay().JailedTimeLeft = WantedTime;
                TargetClient.GetRoleplay().TimerManager.CreateTimer("jail", 1000, false);
            }

            if (TargetClient.GetHabbo().CurrentRoomId == JailRID)
            {
                RoleplayManager.GetLookAndMotto(TargetClient);
                RoleplayManager.SpawnBeds(TargetClient, "bed_silo_one");
                TargetClient.SendMessage(new RoomBubbleNotificationComposer("room_jail_prison", "Usted ha sido escoltado por " + Session.GetHabbo().Username + " por " + WantedTime + " minutos!", ""));
            }
            else
            {
                TargetClient.SendMessage(new RoomBubbleNotificationComposer("room_jail_prison", "Usted ha sido escoltado por " + Session.GetHabbo().Username + " por " + WantedTime + " minutos!", ""));
                RoleplayManager.SendUserOld2(TargetClient, JailRID);
            }
            */
            #region Sound System
            foreach (RoomUser RoomUsers in Session.GetRoomUser().GetRoom().GetRoomUserManager().GetRoomUsers())
            {
                if (RoomUsers == null || RoomUsers.GetClient() == null)
                    continue;

                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(RoomUsers.GetClient(), "event_feedcomposer", "sound|paralizer");
            }
            #endregion
            #endregion
        }
    }
}