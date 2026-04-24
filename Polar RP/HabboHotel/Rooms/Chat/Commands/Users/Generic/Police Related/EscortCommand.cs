using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Drawing;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using Polar.Communication.Packets.Outgoing.Notifications;
using Polar.HabboRoleplay.RPRoom;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Jobs.Types.Police
{
    class EscortCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_police_related_escort"; }
        }

        public string Parameters
        {
            get { return "%user%"; }
        }

        public string Description
        {
            get { return "Acompaña al usuario con el brazalete en función de su nivel deseado."; }
        }

        public async Task Execute(GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            if (Params.Length == 1)
            {
                Session.SendWhisper("Vaya, te olvidaste de introducir un nombre de usuario!", 1);
                return;
            }

            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);
            if (TargetClient == null)
            {
                Session.SendWhisper("Se ha producido un error al intentar encontrar a ese usuario, tal vez estén sin conexión.", 1);
                return;
            }

            RoomUser RoomUser = Session.GetRoomUser();
            RoomUser TargetUser = Room.GetRoomUserManager().GetRoomUserByHabbo(TargetClient.GetHabbo().Username);

            if (TargetUser == null)
            {
                Session.SendWhisper("Se ha producido un error al encontrar a ese usuario, tal vez no están en línea o en esta habitación.", 1);
                return;
            }

            if (TargetClient.GetRoleplay().IsDead)
            {
                Session.SendWhisper("¡No puedes escoltar a alguien que está muerto!", 1);
                return;
            }

            if (TargetClient.GetRoleplay().IsJailed && !TargetClient.GetRoleplay().Jailbroken)
            {
                Session.SendWhisper("¡No puedes escoltar a alguien que ya está en la cárcel!", 1);
                return;
            }
            if (TargetClient.GetRoleplay().PassiveMode)
            {
                Session.SendWhisper("¡No puedes hacerle eso a una persona que está en modo pasivo!", 1);
                return;
            }

            if (!TargetClient.GetRoleplay().Cuffed)
            {
                Session.SendWhisper("¡No puedes escoltar a alguien que no está esposado!", 1);
                return;
            }

            if (TargetUser.IsAsleep)
            {
                Session.SendWhisper("¡No puedes escoltar a alguien que no esté jugando el juego ahora mismo!", 1);
                return;
            }

            if (TargetClient == Session)
            {
                Session.SendWhisper("¡No puedes acompañarte!", 1);
                return;
            }

            if (!TargetClient.GetRoleplay().Jailbroken)
            {
                if (!RoleplayManager.WantedList.ContainsKey(TargetClient.GetHabbo().Id))
                {
                    if (TargetClient.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("probation"))
                        TargetClient.GetRoleplay().TimerManager.ActiveTimers["probation"].EndTimer();

                    TargetClient.GetRoleplay().IsWanted = true;
                    TargetClient.GetRoleplay().WantedLevel = 1;
                    TargetClient.GetRoleplay().WantedTimeLeft = 2;

                    TargetClient.GetRoleplay().TimerManager.CreateTimer("wanted", 1000, false);
                    RoleplayManager.WantedList.TryUpdate(TargetClient.GetHabbo().Id, new Wanted(Convert.ToUInt32(TargetClient.GetHabbo().Id), Room.Id.ToString(), 1), RoleplayManager.WantedList[TargetClient.GetHabbo().Id]);
                }
            }

            #endregion

            #region Execute
            Point ClientPos = new Point(RoomUser.X, RoomUser.Y);
            Point TargetClientPos = new Point(TargetUser.X, TargetUser.Y);
            double Distance = RoleplayManager.GetDistanceBetweenPoints2D(ClientPos, TargetClientPos);
            Wanted Wanted = RoleplayManager.WantedList.ContainsKey(TargetClient.GetHabbo().Id) ? RoleplayManager.WantedList[TargetClient.GetHabbo().Id] : null;
            int WantedTime/* = Wanted == null ? 6 : Wanted.WantedLevel * 5*/;

            if (Wanted == null)
                WantedTime = 4;
            else if (Wanted.WantedLevel == 2)
                WantedTime = 4;
            else if (Wanted.WantedLevel == 3)
                WantedTime = 6;
            else if (Wanted.WantedLevel == 4)
                WantedTime = 8;
            else if (Wanted.WantedLevel == 5)
                WantedTime = 10;
            else
                WantedTime = 2;
            string MyCity = Room.City;
            RPRoom Data;
            RPRoom Data2;
            int JailRID = PolarEnvironment.GetGame().GetRPRoomManager().TryToGetJail(MyCity, out Data);//prision de la cd.
            int PolStationID = PolarEnvironment.GetGame().GetRPRoomManager().TryToGetPolStation(MyCity, out Data2);//prision
            if (Distance <= 1)
            {
                if (TargetClient.GetRoleplay().IsWorking)
                {
                    WorkManager.RemoveWorkerFromList(TargetClient);
                    TargetClient.GetRoleplay().IsWorking = false;
                    TargetClient.GetHabbo().Poof();
                }

                Session.Shout("*Desbloquea las esposas de " + TargetClient.GetHabbo().Username + "'y las acompaña a la cárcel por " + WantedTime + " minutos*", (GroupManager.HasJobCommand(Session, "guide") && Session.GetRoleplay().IsWorking ? 37 : 4));
                TargetClient.GetRoleplay().Cuffed = false;
                TargetClient.GetRoomUser().ApplyEffect(0);

                if (TargetClient.GetHabbo().Look.Contains("lg-78322"))
                {
                    if (!TargetClient.GetRoleplay().WantedFor.Contains("indecent exposure"))
                        TargetClient.GetRoleplay().WantedFor = TargetClient.GetRoleplay().WantedFor + "indecent exposure, ";
                }

                if (TargetUser.Frozen)
                    TargetUser.Frozen = false;

                if (!TargetClient.GetRoleplay().IsJailed)
                {
                    TargetClient.GetRoleplay().IsJailed = true;
                    TargetClient.GetRoleplay().JailedTimeLeft = WantedTime;
                    TargetClient.GetRoleplay().TimerManager.CreateTimer("jail", 1000, false);
                }

                if (TargetClient.GetRoleplay().Jailbroken && !JailbreakManager.FenceBroken)
                    TargetClient.GetRoleplay().Jailbroken = false;

                if (TargetClient.GetHabbo().CurrentRoomId == JailRID)
                {
                    RoleplayManager.GetLookAndMotto(TargetClient);
                    RoleplayManager.SpawnBeds(TargetClient, "bed_silo_one");
                    TargetClient.SendMessage(new RoomBubbleNotificationComposer("room_jail_prison", "Usted ha sido escoltado por " + Session.GetHabbo().Username + " por " + WantedTime + " minutos!", ""));
                }
                else
                {
                    TargetClient.SendMessage(new RoomBubbleNotificationComposer("room_jail_prison", "Usted ha sido escoltado por  " + Session.GetHabbo().Username + " por " + WantedTime + " minutos!", ""));
                    RoleplayManager.SendUserOld2(TargetClient, JailRID);
                }

                #region Live Feed
                foreach (GameClient client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                {
                    if (client == null)
                        continue;

                    PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(client, "event_feedcomposer", "alert|" + Session.GetHabbo().Username + "|" + TargetClient.GetHabbo().Username + "|" + "Escolta a");
                }
                #endregion

                PolarEnvironment.GetGame().GetClientManager().JailAlert("[RADIO Alert] " + TargetClient.GetHabbo().Username + " acaba de ser escoltado a la cárcel por " + Session.GetHabbo().Username + "!");
                PolarEnvironment.GetGame().GetAchievementManager().ProgressAchievement(Session, "ACH_Arrests", 1);
                Session.GetRoleplay().Arrests++;
                PolarEnvironment.GetGame().GetAchievementManager().ProgressAchievement(TargetClient, "ACH_Arrested", 1);
                TargetClient.GetRoleplay().Arrested++;
                return;
            }
            else
            {
                Session.SendWhisper("¡Debes acercarte a este ciudadano para poder escoltarlo!", 1);
                return;
            }
            #endregion
        }
    }
}