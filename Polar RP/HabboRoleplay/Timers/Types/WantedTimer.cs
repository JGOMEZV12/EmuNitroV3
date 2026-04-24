using System;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.Core;
using Polar.HabboHotel.Rooms;
using Polar.HabboRoleplay.Houses;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;

namespace Polar.HabboRoleplay.Timers.Types
{
    /// <summary>
    /// Countdown to evade police
    /// </summary>
    public class WantedTimer : RoleplayTimer
    {
        public WantedTimer(string Type, GameClient Client, int Time, bool Forever, object[] Params)
            : base(Type, Client, Time, Forever, Params)
        {
            // 10 minutes converted to miliseconds
            TimeLeft = base.Client.GetRoleplay().WantedTimeLeft * 60000;
        }

        /// <summary>
        /// Removes the user from the wanted list
        /// </summary>
        public override void Execute()
        {
            try
            {
                if (base.Client == null || base.Client.GetHabbo() == null || base.Client.GetRoleplay() == null)
                {
                    base.EndTimer();
                    return;
                }

                if (base.Client.GetRoleplay().Jailbroken || !base.Client.GetRoleplay().IsWanted || base.Client.GetRoleplay().IsJailed)
                {
                    base.Client.GetRoleplay().IsWanted = false;
                    base.Client.GetRoleplay().WantedLevel = 0;
                    base.Client.GetRoleplay().WantedTimeLeft = 0;
                    base.EndTimer();
                    return;
                }


                RoomData roomData = PolarEnvironment.GetGame().GetRoomManager().GenerateRoomData(base.Client.GetHabbo().CurrentRoomId);

                if (base.Client.GetRoomUser() == null)
                    return;

                if (base.Client.GetRoomUser().IsAsleep)
                    return;

                House House;
                if (roomData.TurfEnabled || base.Client.GetHabbo().CurrentRoom.TryGetHouse(out House))
                    return;

                if (base.Client.GetRoleplay().TexasHoldEmPlayer > 0)
                    return;

                TimeCount++;
                TimeLeft -= 1000;

                if (TimeCount == 60)
                    base.Client.GetRoleplay().WantedTimeLeft--;

                if (TimeLeft > 0)
                {
                    if (TimeCount == 60)
                    {
                        base.Client.SendWhisper("Tienes " + (TimeLeft / 60000) + " minuto(s) de evadir a la policía", 1);
                        base.Client.SendMessage(new RoomBubbleNotificationComposer("wanted_level", "Eres búscado por " + (TimeLeft / 60000) + " minuto(s) escondete bien y evade a la policía", ""));
                        TimeCount = 0;
                    }
                    return;
                }

                base.Client.GetRoleplay().IsWanted = false;
                base.Client.GetRoleplay().WantedLevel = 0;
                base.Client.GetRoleplay().WantedTimeLeft = 0;
                base.Client.GetRoleplay().Evasions++;
                PolarEnvironment.GetGame().GetAchievementManager().ProgressAchievement(base.Client, "ACH_Evasions", 1);

                Wanted Junk;
                RoleplayManager.WantedList.TryRemove(base.Client.GetHabbo().Id, out Junk);
                PolarEnvironment.GetGame().GetClientManager().JailAlert("[RADIO Alert] " + base.Client.GetHabbo().Username + " ¿Ha evadido a las autoridades legales! Mejor suerte la próxima vez.");
                RoleplayManager.Shout(base.Client, "*Finalmente evade a la policía después de huir durante 10 minutos*", 4);
                base.Client.SendMessage(new RoomBubbleNotificationComposer("evasion_success_notice", "¡Has evadido! Usted ha evadido con éxito a las autoridades legales.", ""));
                base.EndTimer();
            }
            catch (Exception e)
            {
                Logging.LogRPTimersError("Error in Execute() void: " + e);
                base.EndTimer();
            }
        }
    }
}