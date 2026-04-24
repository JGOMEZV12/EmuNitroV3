using System;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.Core;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboRoleplay.Timers.Types
{
    /// <summary>
    /// Makes the citizens cleanliness decrease over time
    /// </summary>
    public class HygieneTimer : RoleplayTimer
    {
        public HygieneTimer(string Type, GameClient Client, int Time, bool Forever, object[] Params)
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

                if (base.Client.GetHabbo().CurrentRoom != null)
                {
                    if (base.Client.GetHabbo().CurrentRoom.TutorialEnabled)
                        return;
                }

                if (base.Client.GetRoleplay().InShower)
                    return;

                TimeCount++;

                if (TimeCount < ((base.Client.GetHabbo().VIPRank == 1) ? (RoleplayManager.HygieneTime * 2) : RoleplayManager.HygieneTime))// 500 segundos para disminuir la higiene
                    return;

                TimeCount = 0;

                // Verificar si la higiene ya está completa, si es así quitar el efecto
                if (base.Client.GetRoleplay().Hygiene >= 100)
                {
                    if (base.Client.GetRoomUser().CurrentEffect == 10)
                    {
                        base.Client.GetRoomUser().ApplyEffect(0);
                    }
                    return;
                }

                if (base.Client.GetRoleplay().Hygiene == 0)
                {
                    if (base.Client.GetRoomUser() != null && base.Client.GetRoomUser().CurrentEffect != 10)
                        base.Client.GetRoomUser().ApplyEffect(10);

                    int AmountOfEnergy = Random.Next(1, 5);

                    if (base.Client.GetRoleplay().CurEnergy - AmountOfEnergy <= 0)
                        base.Client.GetRoleplay().CurEnergy = 0;
                    else
                        base.Client.GetRoleplay().CurEnergy -= AmountOfEnergy;

                    //base.Client.SendWhisper("You really do smell! You better hurry up and take a shower!", 1);
                    base.Client.SendMessage(new RoomBubbleNotificationComposer("bathroom_shower1_icon", "¡Eres apestoso! Visite el gimnasio para obtener una ducha y rellenar su higiene.", ""));
                    return;
                }

                int AmountOfHygiene = Random.Next(1, 5);

                if (base.Client.GetRoleplay().Hygiene - AmountOfHygiene <= 0)
                    base.Client.GetRoleplay().Hygiene = 0;
                else
                    base.Client.GetRoleplay().Hygiene -= AmountOfHygiene;

                if (base.Client.GetRoleplay().Hygiene > 0)
                    return;

                if (base.Client.GetRoomUser() != null && base.Client.GetRoomUser().CurrentEffect != 10)
                    base.Client.GetRoomUser().ApplyEffect(10);
                //base.Client.SendWhisper("You start to really stink! You've got to do something about this smell!", 1);
                base.Client.SendMessage(new RoomBubbleNotificationComposer("bathroom_shower1_icon", "¡Te estás volviendo loco! Visite el gimnasio o vaya a su casa, para obtener una ducha y rellenar su higiene.", ""));
            }
            catch (Exception e)
            {
                Logging.LogRPTimersError("Error in Execute() void: " + e);
                base.EndTimer();
            }
        }
    }
}