using System;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.Core;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.Rooms;
using Polar.HabboRoleplay.VehicleOwned;
using System.Collections.Generic;
using Polar.HabboHotel.Items;
using System.Linq;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.HabboHotel.Users.Effects;
using Polar.HabboHotel.Groups;

namespace Polar.HabboRoleplay.Timers.Types
{
    /// <summary>
    /// Makes the citizen get hungry over time
    /// </summary>
    public class FuelTimer : RoleplayTimer
    {
        public FuelTimer(string Type, GameClient Client, int Time, bool Forever, object[] Params) 
            : base(Type, Client, Time, Forever, Params)
        {
            // Convert to milliseconds

            TimeLeft = base.Client.GetRoleplay().LoadingTimeLeft * 1000;
        }
 
        /// <summary>
        /// Increases the users hunger
        /// </summary>
        public override void Execute()
        {
            try
            {
                if (base.Client == null || base.Client.GetHabbo() == null || base.Client.GetRoleplay() == null || base.Client.GetRoleplay().IsDead || base.Client.GetRoleplay().IsJailed || base.Client.GetRoomUser() == null || base.Client.GetRoomUser().IsWalking || base.Client.GetRoleplay().BreakGeneralTimer)
                {
                    base.EndTimer();

                    base.Client.GetRoleplay().BreakGeneralTimer = false;
                    //base.Client.GetRoleplay().LoadingTimeLeft = 0;

                    #region Retornamos Variables de Timers

                    #region Combustible
                    if (base.Client.GetRoleplay().IsFuelCharging)
                    {
                        base.Client.GetRoleplay().IsFuelCharging = false;
                        base.Client.GetRoleplay().FuelChargingCant = 0;
                        base.Client.SendWhisper("No debes moverte ni apagar tu vehículo hasta que se termine de llenar el tanque.", 1);
                    }
                    #endregion
                    #endregion
                    return;
                }


                if (base.Client.GetRoomUser() == null)
                    return;

                if (base.Client.GetRoomUser().IsAsleep)
                    return;

                #region Specific Special Conditions
                if ((base.Client.GetRoleplay().IsFuelCharging && !base.Client.GetRoleplay().DrivingCar))
                {
                    base.Client.GetRoleplay().BreakGeneralTimer = true;
                    return;
                }
                #endregion

                TimeCount++;
                
                TimeLeft -= 1000;

                base.Client.GetRoleplay().LoadingTimeLeft--;

                if (TimeLeft > 0)
                {
                    if (TimeCount == 2)
                    {
                        if (!base.Client.GetRoleplay().TurfCapturing)
                            base.Client.SendWhisper("Debes esperar " + base.Client.GetRoleplay().LoadingTimeLeft + " segundo(s)...", 1);
                        TimeCount = 0;
                    }
                    return;
                }

                #region Cumple el Timer

                #region Fuel Charging
                if (base.Client.GetRoleplay().IsFuelCharging)
                {
                    int Cant = base.Client.GetRoleplay().FuelChargingCant;
                    int Price = Cant * RoleplayManager.FuelPrice;
                    base.Client.SendWhisper("¡" + Cant + " L. de Combustible Cargado(s)! Gracias por su compra. (-$" + Price + ")", 1);

                    base.Client.GetHabbo().Credits -= Price;
                    base.Client.GetHabbo().UpdateCreditsBalance();

                    base.Client.GetRoleplay().IsFuelCharging = false;
                    base.Client.GetRoleplay().FuelChargingCant = 0;

                    List<VehiclesOwned> VO = PolarEnvironment.GetGame().GetVehiclesOwnedManager().getVehiclesOwnedList(base.Client.GetRoleplay().DrivingCarId);
                    if (VO != null && VO.Count > 0)
                    {
                        VO[0].Fuel += Cant;
                        RoleplayManager.UpdateVehicleStat(VO[0].Id, "fuel", VO[0].Fuel);// Actualizamos en DB
                        base.Client.GetRoleplay().CarFuel = VO[0].Fuel;
                    }

                    PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(base.Client, "event_vehicle", "open");
                    base.Client.GetRoleplay().LoadingTimeLeft = 0;
                }
                #endregion

                base.Client.GetRoleplay().LoadingTimeLeft = 0;
                base.EndTimer();
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