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
using Polar.Database.Interfaces;
using System.Drawing;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.Communication.Packets.Outgoing.Inventory.Furni;
using System.Data;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using Polar.HabboRoleplay.VehiclesJobs;
using Polar.Communication.Packets.Outgoing.Notifications;
using Polar.HabboRoleplay.VehicleOwned;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Interactions.Self
{
    class LocalizarCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_localizar"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Te muestra donde se encuentran tu vehículo."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            List<VehiclesOwned> VO = PolarEnvironment.GetGame().GetVehiclesOwnedManager().getMyVehiclesOwned(Session.GetHabbo().Id);

            #region Conditions
            if (VO == null || VO.Count <= 0)
            {
                Session.SendWhisper("No tienes ningún vehículo a tu nombre a localizar. ¡Compra uno en el Concesionario de la ciudad!", 1);
                return;
            }
            if (Session.GetRoleplay().TryGetCooldown("localizar"))
                return;
            #endregion

            #region Execute
            string str = "";
            str += "\n============================================\n                  Localizador de tus Vehículos \n============================================\n";

            foreach (VehiclesOwned _vo in VO)
            {
                string localizado = "Corralón";
                if (RoleplayManager.GenerateRoom(_vo.Location, out Room loc))
                    localizado = loc.Name;

                str += "\nModelo: " + _vo.Model + "\n";
                str += "Última Localización: " + localizado + "\n";
            }

            str += "\n\nRecuerda que puedes ir a la Municipalidad de la Ciudad a pedir servicio de Grúa para tu vehículo extraviado.";
            Session.SendNotifWithScroll(str);
            Session.GetRoleplay().CooldownManager.CreateCooldown("localizar", 1000, 3);
            #endregion
        }

    }
}
