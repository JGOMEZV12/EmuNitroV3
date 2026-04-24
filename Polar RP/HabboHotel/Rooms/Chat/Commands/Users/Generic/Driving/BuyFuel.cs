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
    class BuyFuelCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_buy_fuel"; }
        }

        public string Parameters
        {
            get { return "%litros%"; }
        }

        public string Description
        {
            get { return "Comprar combustible para tu vehículo por cantidad."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            if (Params.Length != 2)
            {
                Session.SendWhisper("Comando Inválido. Usa ':combustible [cantidad (L)]' o :llenartanque.", 1);
                return;
            }
            if (!Session.GetRoleplay().DrivingCar)
            {
                Session.SendWhisper("Debes estar conduciendo el vehículo. Si tu vehículo no enciende, comprar Bidones.", 1);
                return;
            }
            /*if (!Room.GasEnabled)
            {
                Session.SendWhisper("Debes estar en una Gasolinera para comprar combustible.", 1);
                return;
            }*/
            int Amount;
            if (!int.TryParse(Params[1], out Amount))
            {
                Session.SendWhisper("Cantidad de Combustible inválida.", 1);
                return;
            }
            if (Amount <= 0)
            {
                Session.SendWhisper("La cantidad de combustible debe ser mayor a 0 Litros.", 1);
                return;
            }
            int Price = Amount * RoleplayManager.FuelPrice;
            if (Session.GetHabbo().Credits < Price)
            {
                Session.SendWhisper("No cuentas con $" + Price + " para comprar " + Amount + " L. de Combustible.", 1);
                return;
            }
            List<VehiclesOwned> VO = PolarEnvironment.GetGame().GetVehiclesOwnedManager().getVehiclesOwnedList(Session.GetRoleplay().DrivingCarId);
            if (VO == null || VO.Count <= 0)
            {
                Session.SendWhisper("((No se pudo obtener la información del vehículo que conduces))", 1);
                return;
            }
            if ((VO[0].Fuel + Amount) > Session.GetRoleplay().CarMaxFuel)
            {
                Session.SendWhisper("A tu tanque solo le caben " + Session.GetRoleplay().CarMaxFuel + " L. de Combustible.", 1);
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
            Session.GetRoleplay().LoadingTimeLeft = 10;
            Session.GetRoleplay().TimerManager.CreateTimer("general", 1000, true);
            Session.GetRoleplay().CooldownManager.CreateCooldown("buyfuel", 1000, 3);
            return;

            #endregion
        }

    }
}
