using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.HabboHotel.Items;
using Polar.HabboRoleplay.Vehicles;
using Polar.HabboHotel.Rooms;
using Polar.Communication.Packets.Outgoing.Rooms.Avatar;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;

using Polar.HabboRoleplay.VehicleOwned;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Interactions.Self
{
    class BuyFuelFillCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_buy_fuel_fill"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Llena el Tanque de combustible de tu vehículo."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            if (!Session.GetRoleplay().DrivingCar)
            {
                Session.SendWhisper("Debes estar conduciendo el vehículo. Si tu vehículo no enciende, compra Bidones.", 1);
                return;
            }
            /*if (!Room.GasEnabled)
            {
                Session.SendWhisper("Debes estar en una Gasolinera para comprar combustible.", 1);
                return;
            }*/
            List<VehiclesOwned> VO = PolarEnvironment.GetGame().GetVehiclesOwnedManager().getVehiclesOwnedList(Session.GetRoleplay().DrivingCarId);
            if (VO == null || VO.Count <= 0)
            {
                Session.SendWhisper("((No se pudo obtener la información del vehículo que conduces))", 1);
                return;
            }
            int Amount = Session.GetRoleplay().CarMaxFuel - VO[0].Fuel;            
            if(Amount <= 0)
            {
                Session.SendWhisper("Tu tanque ya se encuentra lleno.", 1);
                return;
            }
            int Price = Amount * RoleplayManager.FuelPrice;
            if (Session.GetHabbo().Credits < Price)
            {
                Session.SendWhisper("No cuentas con $"+ Price + " para comprar " + Amount + " L. de Combustible.", 1);
                return;
            }
            if (Session.GetRoleplay().TryGetCooldown("buyfuel"))
                return;
            #endregion

            #region Comodin Conditions
            Item BTile = null;
            BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "comodin_carro" && x.Coordinate == Session.GetRoomUser().Coordinate);
            if (BTile == null)
            {
                Session.SendWhisper("Debes estar cerca de la despachadora de Combustible.", 1);
                return;
            }
            #endregion

            #region Execute
            Session.SendWhisper("Estamos empezando a llenar tu tanque, por favor, espera.", 1);
            Session.GetRoleplay().IsFuelCharging = true;
            Session.GetRoleplay().FuelChargingCant = Amount;
            Session.GetRoleplay().LoadingTimeLeft = Amount;
            Session.GetRoleplay().TimerManager.CreateTimer("general", 1000, true);
            Session.GetRoleplay().CooldownManager.CreateCooldown("buyfuel", 1000, 3);

            return;

            #endregion
        }

    }
}
