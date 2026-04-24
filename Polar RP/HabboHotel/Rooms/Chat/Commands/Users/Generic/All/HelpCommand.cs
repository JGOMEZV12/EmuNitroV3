
using System;
using System.Linq;
using System.Text;
using System.Data;
using System.Collections.Generic;

using Polar.Communication.Packets.Outgoing.Users;
using Polar.Communication.Packets.Outgoing.Notifications;

using Polar.HabboHotel.GameClients;

namespace Polar.HabboHotel.Rooms.Chat.Commands.User
{
    class HelpsCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_help"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Recibe ayuda específica sobre los comandos."; }
        }

        public async Task Execute(GameClient Session, Room Room, string[] Params)
        {
            if (Params.Length == 2)
            {
                switch (Params[1].ToLower())
                {
                    #region Trabajos
                    case "trabajos":
                        PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_commands", "jobs");
                        break;
                    #endregion

                    #region Vehículos
                    case "vehiculos":
                        PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_commands", "vehicles");
                        break;
                    #endregion

                    #region Empresas
                    case "empresas":
                        PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_commands", "bussines");
                        break;
                    #endregion

                    #region Bandas
                    case "bandas":
                        PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_commands", "gangs");
                        break;
                    #endregion

                    #region Default
                    default:
                        Session.SendWhisper("((Comando inválido. Usa :ayuda para recibir más información))", 1);
                    break;
                    #endregion
                }
            }
            else
            {
                // Enviamos WS de ventana de comandos.
                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_commands", "open");
            }
        }
    }
}
