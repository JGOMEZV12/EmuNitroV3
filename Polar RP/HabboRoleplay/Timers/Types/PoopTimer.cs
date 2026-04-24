using System;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.Core;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;

namespace Polar.HabboRoleplay.Timers.Types
{
    /// <summary>
    /// Makes the citizens cleanliness decrease over time
    /// </summary>
    public class PoopTimer : RoleplayTimer
    {
        public PoopTimer(string Type, GameClient Client, int Time, bool Forever, object[] Params)
            : base(Type, Client, Time, Forever, Params)
        {

        }

        /// <summary>
        /// Decreases the users hygiene
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

                if (base.Client.GetRoomUser().IsAsleep)
                    return;

                if (base.Client.GetRoleplay().StaffOnDuty)
                    return;

                if (base.Client.GetRoleplay().AmbassadorOnDuty)
                    return;

                if (base.Client.GetRoleplay().InCagar)
                    return;

                TimeCount++;

                if (TimeCount < 200)
                    return;

                TimeCount = 0;

                if (base.Client.GetRoleplay().Poop == 0)
                {
                    if (base.Client.GetRoomUser() != null)
                        base.Client.GetRoomUser().ApplyEffect(10);

                    int AmountOfEnergy = Random.Next(1, 2);

                    if (base.Client.GetRoleplay().CurEnergy - AmountOfEnergy <= 0)
                        base.Client.GetRoleplay().CurEnergy = 0;
                    else
                        base.Client.GetRoleplay().CurEnergy -= AmountOfEnergy;

                    //base.Client.SendWhisper("You really do smell! You better hurry up and take a Cagar!", 1);
                    base.Client.SendMessage(new RoomBubbleNotificationComposer("hygiene_low_warning", "¡Hueles a mierda! encuentra un baño y usa un toilet de inmediato.", ""));
                    return;
                }

                int AmountOfPoop = Random.Next(1, 2);

                if (base.Client.GetRoleplay().Poop - AmountOfPoop <= 0)
                    base.Client.GetRoleplay().Poop = 0;
                else
                    base.Client.GetRoleplay().Poop -= AmountOfPoop;

                if (base.Client.GetRoleplay().Poop > 0)
                    return;

                if (base.Client.GetRoomUser() != null)
                    base.Client.GetRoomUser().ApplyEffect(10);
                //base.Client.SendWhisper("You start to really stink! You've got to do something about this smell!", 1);
                base.Client.SendMessage(new RoomBubbleNotificationComposer("bathroom_toilet3_icon", "¡Te cagaste los pantalones y hueles a mierda! usa un toilet de inmediato.", ""));
            }
            catch (Exception e)
            {
                Logging.LogRPTimersError("Error in Execute() void: " + e);
                base.EndTimer();
            }
        }
    }
}