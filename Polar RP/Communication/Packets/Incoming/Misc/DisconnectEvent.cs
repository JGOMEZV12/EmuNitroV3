using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.HabboHotel.Rooms;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.GameClients;

namespace Polar.Communication.Packets.Incoming.Misc
{
    internal class DisconnectEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            Session.Disconnect(false);
        }

        #region Chofer Check
        public void ChoferCheck(HabboHotel.GameClients.GameClient Client)
        {
            if (!Client.GetRoleplay().Chofer)
                return;

            if (Client.GetRoleplay().IsBasuChofer)
                Client.GetRoleplay().IsBasuChofer = false;

            //Si lleva pasajeros
            #region Pasajeros
            //Vars
            string Pasajeros = Client.GetRoleplay().Pasajeros;
            string[] stringSeparators = new string[] { ";" };
            string[] result;
            result = Pasajeros.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);

            foreach (string psjs in result)
            {
                HabboHotel.GameClients.GameClient PJ = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(psjs);
                if (PJ != null)
                {
                    if (PJ.GetRoleplay().ChoferName == Client.GetHabbo().Username)
                    {
                        HabboRoleplay.Misc.RoleplayManager.Shout(PJ, "*Baja del vehículo de " + Client.GetHabbo().Username + "*", 5);
                    }
                    // PASAJERO
                    PJ.GetRoleplay().Pasajero = false;
                    PJ.GetRoleplay().ChoferName = "";
                    PJ.GetRoleplay().ChoferID = 0;
                    PJ.GetRoomUser().CanWalk = true;
                    PJ.GetRoomUser().FastWalking = false;
                    PJ.GetRoomUser().TeleportEnabled = false;
                    PJ.GetRoomUser().AllowOverride = false;

                    // Descontamos Pasajero
                    Client.GetRoleplay().PasajerosCount--;
                    Client.GetRoleplay().Pasajeros.Replace(PJ.GetHabbo().Username + ";", "");

                    // CHOFER 
                    Client.GetRoleplay().Chofer = (Client.GetRoleplay().PasajerosCount <= 0) ? false : true;
                    Client.GetRoomUser().AllowOverride = (Client.GetRoleplay().PasajerosCount <= 0) ? false : true;

                    // SI EL PASAJERO ES COMPAÑERO DE BASURERO
                    if (PJ.GetRoleplay().IsBasuPasaj)
                        PJ.GetRoleplay().IsBasuPasaj = false;
                }
            }
            #endregion
        }
        #endregion

        // Pasajero CHECK
        // Descontarle el pasajero al Chofer
        #region Pasajero Check
        public void PasajeroCheck(HabboHotel.GameClients.GameClient Client)
        {
            if (!Client.GetRoleplay().Pasajero)
                return;

            GameClient Chofer = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Client.GetRoleplay().ChoferName);
            if (Chofer != null)
            {
                // Descontamos Pasajero
                Chofer.GetRoleplay().PasajerosCount--;
                Chofer.GetRoleplay().Pasajeros.Replace(Client.GetHabbo().Username + ";", "");

                // CHOFER 
                Chofer.GetRoleplay().Chofer = (Client.GetRoleplay().PasajerosCount <= 0) ? false : true;
                Chofer.GetRoomUser().AllowOverride = (Client.GetRoleplay().PasajerosCount <= 0) ? false : true;
            }
        }
        #endregion

    }
}
