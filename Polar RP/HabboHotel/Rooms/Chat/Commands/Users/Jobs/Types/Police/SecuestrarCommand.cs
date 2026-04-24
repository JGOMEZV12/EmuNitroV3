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

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Jobs.Types.Police
{
    class SecuestrarCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_secuestrar"; }
        }

        public string Parameters
        {
            get { return "%user%"; }
        }

        public string Description
        {
            get { return "Secuestre al usuario objetivo."; }
        }

        public async Task Execute(GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            if (Params.Length == 1)
            {
                Session.SendWhisper("Vaya, se le olvidó ingresar un nombre de usuario", 1);
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
                Session.SendWhisper("Se ha producido un error al encontrar a ese usuario, tal vez no estén en línea o en esta sala.", 1);
                return;
            }

            if (TargetUser == null)
            {
                Session.SendWhisper("Se ha producido un error al encontrar a ese usuario, tal vez no estén en línea o en esta sala.", 1);
                return;
            }

            if (TargetClient.GetRoleplay().IsDead)
            {
                Session.SendWhisper("¡No puedes secuestrar a alguien que está muerto!", 1);
                return;
            }

            if (TargetClient.GetRoleplay().IsJailed && !TargetClient.GetRoleplay().Jailbroken)
            {
                Session.SendWhisper("¡No puedes secuestrar a alguien que ya está en la cárcel!", 1);
                return;
            }
            if (Session.GetRoleplay().PassiveMode)
            {
                Session.SendWhisper("No puedes realizar acciones ilegales en modo pasivo.", 1);
                return;
            }

            if (TargetClient.GetRoleplay().PassiveMode)
            {
                Session.SendWhisper("¡Este usuario tiene el modo pasivo activo!.", 1);
                return;
            }
            if (!TargetClient.GetRoleplay().Cuffed)
            {
                Session.SendWhisper("¡No puedes arrestar a alguien que no está esposado!", 1);
                return;
            }

            if (TargetUser.IsAsleep)
            {
                Session.SendWhisper("¡No puedes arrestar a alguien que no está jugando el juego ahora mismo!", 1);
                return;
            }

            if (!TargetClient.GetRoleplay().Jailbroken)
            {
                if (!RoleplayManager.WantedList.ContainsKey(TargetClient.GetHabbo().Id))
                {
                    Session.SendWhisper("Este usuario ni siquiera se quiere", 1);
                    return;
                }
            }
            #endregion

            #region Execute
            Point ClientPos = new Point(RoomUser.X, RoomUser.Y);
            Point TargetClientPos = new Point(TargetUser.X, TargetUser.Y);
            double Distance = RoleplayManager.GetDistanceBetweenPoints2D(ClientPos, TargetClientPos);
            Wanted Wanted = RoleplayManager.WantedList.ContainsKey(TargetClient.GetHabbo().Id) ? RoleplayManager.WantedList[TargetClient.GetHabbo().Id] : null;
            int WantedTime = Wanted == null ? 6 : Wanted.WantedLevel * 5;

            if (Distance <= 1)
            {
                if (TargetClient.GetRoleplay().IsWorking)
                {
                    WorkManager.RemoveWorkerFromList(TargetClient);
                    TargetClient.GetRoleplay().IsWorking = false;
                    TargetClient.GetHabbo().Poof();
                }

                Session.Shout("*Agarra a " + TargetClient.GetHabbo().Username + " le pone un trapo en la boca y nariz para secuestrarlo*", 37);
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
                    TargetClient.GetRoleplay().TimerManager.CreateTimer("secuestro", 1000, false);
                }

                if (TargetClient.GetRoleplay().Jailbroken && !JailbreakManager.FenceBroken)
                    TargetClient.GetRoleplay().Jailbroken = false;

                int JailRID = Convert.ToInt32(RoleplayData.GetData("secuestro", "insideroomid"));

                if (TargetClient.GetHabbo().CurrentRoomId == JailRID)
                {
                    RoleplayManager.GetLookAndMotto(TargetClient);
                    RoleplayManager.SpawnBeds(TargetClient, "grunge_mattress");
                    TargetClient.SendMessage(new RoomBubbleNotificationComposer("room_jail_prison", "Has sido secuestrado por " + Session.GetHabbo().Username + " por " + WantedTime + " minutos!", ""));
                }
                else
                {
                    TargetClient.SendMessage(new RoomBubbleNotificationComposer("room_jail_prison", "Has sido secuestrado por " + Session.GetHabbo().Username + " por " + WantedTime + " minutos!", ""));
                    RoleplayManager.SendUserOld2(TargetClient, JailRID);
                }

                PolarEnvironment.GetGame().GetClientManager().JailAlert("[RADIO Alert] " + TargetClient.GetHabbo().Username + " Acaba de ser secuestrado por " + Session.GetHabbo().Username + "! ¡Rescaten al secuestrado!");
                PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(Session, "compose_discord|" + Session.GetHabbo().Username + "|Secuestro a|" + TargetClient.GetHabbo().Username);
                #region Live Feed
                foreach (GameClient client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                {
                    if (client == null)
                        continue;

                    PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(client, "event_feedcomposer", "alert|" + Session.GetHabbo().Username + "|" + TargetClient.GetHabbo().Username + "|" + "secuestro a");
                }
                #endregion

                PolarEnvironment.GetGame().GetAchievementManager().ProgressAchievement(Session, "ACH_Arrests", 1);
                Session.GetRoleplay().Arrests++;
                PolarEnvironment.GetGame().GetAchievementManager().ProgressAchievement(TargetClient, "ACH_Arrested", 1);
                TargetClient.GetRoleplay().Arrested++;
                return;
            }
            else
            {
                Session.SendWhisper("¡Deben acercarse a este ciudadano para detenerlos!", 1);
                return;
            }
            #endregion
        }

        public void ExecuteBot(GameClient Session, RoomUser Bot, Room Room)
        {
            if (!Bot.GetBotRoleplay().Jailed)
            {
                Session.SendWhisper("Perdón pero " + Bot.GetBotRoleplay().Name + " No está encarcelado", 1);
                return;
            }

            double Distance = RoleplayManager.GetDistanceBetweenPoints2D(Session.GetRoomUser().Coordinate, Bot.Coordinate);
            Wanted Wanted = RoleplayManager.WantedList.ContainsKey(Bot.GetBotRoleplay().Id) ? RoleplayManager.WantedList[Bot.GetBotRoleplay().Id] : null;
            int WantedTime = Wanted == null ? 5 : Wanted.WantedLevel * 5;

            if (Distance <= 1)
            {
                // cba rn
            }
            else
            {
                Session.SendWhisper("Debes acercarte a " + Bot.GetBotRoleplay().Name + " para poder arrestarlo", 1);
                return;
            }
        }
    }
}