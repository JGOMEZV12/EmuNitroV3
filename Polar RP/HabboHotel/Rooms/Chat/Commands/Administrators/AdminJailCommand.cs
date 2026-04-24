using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Collections.Generic;
using System.Threading.Tasks;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using Polar.HabboRoleplay.RPRoom;
using Polar.HabboRoleplay.Timers.Types;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Administrators
{
    class AdminJailCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_admin_jail"; }
        }

        public string Parameters
        {
            get { return "%user% %stars%"; }
        }

        public string Description
        {
            get { return "Encarcela a un ciudadano por un número selecto de estrellas."; }
        }

        public async Task Execute(GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length < 3)
            {
                Session.SendWhisper("Vaya, se le olvidó ingresar un nombre de usuario o # de estrellas de 1-6!", 1);
                return;
            }

            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);
            if (TargetClient == null)
            {
                Session.SendWhisper("Se ha producido un error al intentar encontrar a ese usuario, tal vez están fuera de línea.", 1);
                return;
            }

            var RoomUser = Session.GetRoomUser();
            var TargetRoomUser = TargetClient.GetRoomUser();

            if (RoomUser == null || TargetRoomUser == null)
                return;

            if (TargetClient.GetRoleplay().IsJailed)
            {
                Session.SendWhisper("Esta persona ya está encarcelada!", 1);
                return;
            }

            int Stars;
            if (!int.TryParse(Params[2], out Stars))
            {
                Session.SendWhisper("Por favor, introduzca un número válido de estrellas de 1-6", 1);
                return;
            }
            else
            {
                if (Stars < 1 || Stars > 6)
                {
                    Session.SendWhisper("Por favor, introduzca un número válido de estrellas de 1-6", 1);
                    return;
                }
            }

            int WantedTime = Stars * 5;
            TargetClient.GetRoleplay().WantedLevel = Stars;

            int ReduceTime = 0;
            string ExtraMsg = "";

            if (TargetClient.GetHabbo().VIPRank == 1)
            {
                ExtraMsg = " con una reducción del 10% de tiempo por ser VIP.";
                ReduceTime = WantedTime / 10;
            }
            if (TargetClient.GetHabbo().VIPRank == 2)
            {
                ExtraMsg = " con una reducción del 25% de tiempo por ser VIP2.";
                ReduceTime = WantedTime / 4;
            }

            TargetClient.GetRoleplay().WantedLevel = Stars;
            if (TargetClient.GetRoleplay().IsDead)
            {
                TargetClient.GetRoleplay().IsDead = false;
                TargetClient.GetRoleplay().ReplenishStats(true);
                TargetClient.GetHabbo().Poof();
                // Refrescamos WS
                TargetClient.GetRoleplay().UpdateInteractingUserDialogues();
                TargetClient.GetRoleplay().RefreshStatDialogue();
            }

            if (TargetClient.GetRoleplay().IsWorking)
            {
                WorkManager.RemoveWorkerFromList(TargetClient);
                TargetClient.GetRoleplay().IsWorking = false;
                TargetClient.GetHabbo().Poof();
            }

            if (TargetClient.GetRoleplay().Cuffed)
                TargetClient.GetRoleplay().Cuffed = false;

            if (TargetClient.GetRoleplay().OnProbation)
                TargetClient.GetRoleplay().OnProbation = false;

            if (TargetRoomUser.Frozen)
                TargetRoomUser.Frozen = false;

            #region Desequipar al Concito
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
                TargetClient.GetRoleplay().Cocaine = 0;
                TargetClient.GetRoleplay().Weed = 0;
            }
            else
            {
                TargetClient.GetRoleplay().Cocaine = 0;
                TargetClient.GetRoleplay().Weed = 0;
            }
            #endregion

            RoleplayManager.CheckOnCar(TargetClient);

            if (!TargetClient.GetRoleplay().IsJailed)
            {
                TargetClient.GetRoleplay().IsJailed = true;
                TargetClient.GetRoleplay().JailedTimeLeft = WantedTime;
                TargetClient.GetRoleplay().TimerManager.CreateTimer("jail", 1000, false);
            }

            //int JailRID = Convert.ToInt32(RoleplayData.GetData("jail", "insideroomid"));

            string MyCity = Room.City;
            RPRoom Data;
            int JailRID = PolarEnvironment.GetGame().GetRPRoomManager().TryToGetJail(MyCity, out Data);//prision de la cd.

            if (TargetClient.GetHabbo().CurrentRoomId == JailRID)
            {
                RoleplayManager.GetLookAndMotto(TargetClient);
                RoleplayManager.SpawnBeds(TargetClient, "bed_silo_one");
                TargetClient.SendMessage(new RoomBubbleNotificationComposer("room_jail_prison", "Usted ha sido arrestado por " + Session.GetHabbo().Username + " por " + WantedTime + " minutos!", ""));
            }
            else
            {
                TargetClient.SendMessage(new RoomBubbleNotificationComposer("room_jail_prison", "Usted ha sido arrestado por  " + Session.GetHabbo().Username + " por " + WantedTime + " minutos!", ""));
                RoleplayManager.SendUserOld2(TargetClient, JailRID);
            }

            if (RoleplayManager.WantedList.ContainsKey(TargetClient.GetHabbo().Id))
            {
                Wanted Junk;
                RoleplayManager.WantedList.TryRemove(TargetClient.GetHabbo().Id, out Junk);
                PolarEnvironment.GetGame().GetClientManager().JailAlert("[RADIO] " + TargetClient.GetHabbo().Username + " Acaba de ser arrestado por " + Session.GetHabbo().Username + "! Buen trabajo a todos.");
            }

/*
            try
            {
                PolarEnvironment.SendMs("**__¡LiveFeed!__** `|` **" + TargetClient.GetHabbo().Username + "** Ha sido arrestad@ por **" + Session.GetHabbo().Username + "**");
            }
            catch (Exception ex)
            {
                // Solo loggear, no interrumpir el flujo
                Console.WriteLine($"[AdminJail] Error enviando a Discord: {ex.Message}");
            }*/

            Session.GetRoleplay().Arrests++;
            TargetClient.GetRoleplay().Arrested++;

            Session.Shout("*Utiliza sus poderes divinos y administra a las cárceles " + TargetClient.GetHabbo().Username + " por " + WantedTime + " minutos*", 23);
            return;
        }
    }
}