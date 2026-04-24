using System;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.Core;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;

namespace Polar.HabboRoleplay.Timers.Types
{
    /// <summary>
    /// Makes the citizen get hungry over time
    /// </summary>
    public class HungerTimer : RoleplayTimer
    {
        public HungerTimer(string Type, GameClient Client, int Time, bool Forever, object[] Params)
            : base(Type, Client, Time, Forever, Params)
        {

        }

        /// <summary>
        /// Increases the users hunger
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

                if (base.Client.GetRoomUser() == null)
                    return;

                if (base.Client.GetRoomUser().IsAsleep || base.Client.GetRoleplay().IsDead || base.Client.GetRoleplay().Cuffed || base.Client.GetRoleplay().IsJailed)
                    return;

                if (base.Client.GetRoleplay().StaffOnDuty)
                    return;

                if (base.Client.GetRoleplay().AmbassadorOnDuty)
                    return;

                if (base.Client.GetRoleplay().TexasHoldEmPlayer > 0)
                    return;

                if (base.Client.GetHabbo().CurrentRoom != null)
                {
                    if (base.Client.GetHabbo().CurrentRoom.TutorialEnabled)
                        return;
                }

                TimeCount++;

                if (TimeCount < 150)
                    return;

                int AmountOfHunger = Random.Next(1, 5);
                base.Client.GetRoleplay().Hunger += AmountOfHunger;
                TimeCount = 0;

                if (base.Client.GetRoleplay().Hunger < 100)
                    return;

                base.Client.GetRoleplay().Hunger = 100;

                if (base.Client.GetRoleplay().CurHealth - 5 <= 0 && base.Client.GetRoleplay().IsJailed)
                    return;

                if (base.Client.GetRoleplay().CurHealth - 5 <= 0)
                    base.Client.GetRoleplay().CurHealth = 0;
                else
                {
                    //base.Client.SendWhisper("You have lost 5 HP as you have not ate in days! Replenish your hunger to avoid losing more health!", 1);
                    base.Client.SendMessage(new RoomBubbleNotificationComposer("hunger_high_warning", "¡Estás hambriento! Visite el restaurante para obtener algo de comida y reducir su hambre.", ""));
                    base.Client.GetRoleplay().CurHealth -= 5;
                }
            }
            catch (Exception e)
            {
                Logging.LogRPTimersError("Error in Execute() void: " + e);
                base.EndTimer();
            }
        }
    }
}