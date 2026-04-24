using System;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.Core;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.Rooms;
using Polar.HabboRoleplay.VehicleOwned;
using System.Collections.Generic;

namespace Polar.HabboRoleplay.Timers.Types
{
    /// <summary>
    /// Makes the citizen get hungry over time
    /// </summary>
    public class VehicleJobTimer : RoleplayTimer
    {
        public VehicleJobTimer(string Type, GameClient Client, int Time, bool Forever, object[] Params)
            : base(Type, Client, Time, Forever, Params)
        {
            // Convert to milliseconds
            TimeLeft = base.Client.GetRoleplay().VehicleTimer * 1000; // 5 mins, polis 10 mins
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

                if (base.Client.GetRoleplay().IsDead || base.Client.GetRoleplay().IsJailed)
                {
                    RoleplayManager.CheckCorpCarp(base.Client);
                    base.Client.GetRoleplay().CamCargId = 0;
                    base.EndTimer();
                    base.Client.SendWhisper("¡Tu Vehículo de trabajo ha sido regresado a su sitio por haberlo abandonado mucho tiempo!", 1);
                    return;
                }

                if (base.Client.GetRoomUser() == null)
                    return;

                if (base.Client.GetRoleplay().DrivingCar && base.Client.GetRoleplay().CarJobLastItemId > 0)
                    return;

                TimeCount++;

                TimeLeft -= 1000;

                if (TimeLeft > 0)
                {
                    if (TimeLeft % 60000 == 0)
                    {
                        base.Client.SendWhisper("Te queda(n) " + (TimeLeft / 60000) + " minuto(s) para volver a tu vehículo de trabajo. ¡Si no vuelves será decomisado!", 1);
                    }
                    return;
                }

                #region Cumple el Timer
                RoleplayManager.CheckCorpCarp(base.Client);
                base.Client.GetRoleplay().CamCargId = 0;
                base.EndTimer();
                base.Client.SendWhisper("¡Tu Vehículo de trabajo ha sido regresado a su sitio por haberlo abandonado mucho tiempo!", 1);
                #endregion                
            }
            catch (Exception e)
            {
                Logging.LogRPTimersError("Error in Execute() void: " + e);
                base.EndTimer();
            }
        }
    }
}