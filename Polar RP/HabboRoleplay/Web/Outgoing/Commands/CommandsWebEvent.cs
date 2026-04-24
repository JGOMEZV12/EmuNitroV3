using ConnectionManager;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Incoming.Groups;
using Polar.Communication.Packets.Outgoing;
using Polar.Communication.Packets.Outgoing.Catalog;
using Polar.Communication.Packets.Outgoing.Groups;
using Polar.Communication.Packets.Outgoing.Messenger;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using Polar.Communication.Packets.Outgoing.Rooms.Permissions;
using Polar.Communication.Packets.Outgoing.Rooms.Session;
using Polar.Database.Interfaces;
using Polar.Net;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Chat.Commands;
using Polar.HabboHotel.Users;
using Polar.HabboRoleplay.Houses;
using Polar.HabboRoleplay.Misc;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Group = Polar.HabboHotel.Groups.Group;

namespace Polar.HabboHotel.Roleplay.Web.Outgoing.Misc
{
    /// <summary>
    /// CommandsWebEvent class.
    /// </summary>
    class CommandsWebEvent : IWebEvent
    {
        /// <summary>
        /// Executes socket data.
        /// </summary>
        /// <param name="Client"></param>
        /// <param name="Data"></param>
        /// <param name="Socket"></param>
        public List<string> _aliases = new List<string>();
        public void Execute(GameClient Client, string Data, ConnectionInformation Socket)
        {
            if (!PolarEnvironment.GetGame().GetWebEventManager().SocketReady(Client, true) || !PolarEnvironment.GetGame().GetWebEventManager().SocketReady(Socket))
                return;

            string Action = (Data.Contains(',') ? Data.Split(',')[0] : Data);

            switch (Action)
            {
                #region Police CMDS
                case "show_police_cmds":
                    {
                        Socket.SendWS( "compose_commands|show_police_cmds|");
                    }
                    break;
                case "hide_police_cmds":
                    {
                        Socket.SendWS( "compose_commands|hide_police_cmds|");
                    }
                    break;
                #endregion

                #region Map
                case "map":
                    {
                        Socket.SendWS( "compose_commands|map|");
                    }
                    break;
                #endregion

                #region Open
                case "open":
                    {
                        StringBuilder html = new StringBuilder();

                        // Pestañas superiores
                        html.AppendLine("<div class=\"tabs-38iVv_0\">");
                        html.AppendLine("    <div class=\"tab-2ddeR_0 CMDS_Normales selected-3s9hj_0\">Normales</div>");
                        html.AppendLine("    <div class=\"tab-2ddeR_0 CMDS_VIP\">VIP</div>");
                        html.AppendLine("    <div class=\"tab-2ddeR_0 CMDS_Roleplay\">Roleplay</div>");
                        html.AppendLine("    <div class=\"tab-2ddeR_0 CMDS_Vehiculos\">Veh&iacute;culos</div>");
                        html.AppendLine("    <div class=\"tab-2ddeR_0 CMDS_Bandas\">Bandas</div>");
                        html.AppendLine("    <div class=\"tab-2ddeR_0 CMDS_Trabajos\">Trabajos</div>");
                        if (Client.GetHabbo().Rank > 3)
                        {
                            html.AppendLine("    <div class=\"tab-2ddeR_0 CMDS_Staff\">Staff</div>");
                        }
                        html.AppendLine("</div>");

                        // Cuerpo donde van las listas de comandos
                        html.AppendLine("<div class=\"body-1skWP_0\" style=\"max-height: 312px;\">");

                        // Función auxiliar para generar el HTML de una lista de comandos
                        Func<string, IEnumerable<KeyValuePair<string, IChatCommand>>, string> generarBloque = (id, coleccion) =>
                        {
                            if (coleccion == null) return string.Empty;

                            StringBuilder bloque = new StringBuilder();
                            bool esPrimero = id == "CMDS_Normales"; // Solo el primero visible
                            string displayStyle = esPrimero ? "" : " style=\"display: none;\"";

                            bloque.AppendLine($"    <div id=\"{id}\" class=\"flex items-center\"{displayStyle}>");
                            bloque.AppendLine("        <div class=\"flex flex-col items-center flex-1\">");
                            bloque.AppendLine("            <div class=\"flex justify-center mt-2 w-full\">");
                            bloque.AppendLine("                    <div class=\"overflow-hidden\">");

                            foreach (var cmd in coleccion)
                            {
                                // Filtrar alias y permisos
                                if (_aliases.Contains(cmd.Key.ToLower()))
                                    continue;

                                if (!string.IsNullOrEmpty(cmd.Value.PermissionRequired) &&
                                    !Client.GetHabbo().GetPermissions().HasCommand(cmd.Value.PermissionRequired))
                                    continue;

                                string commandText = ":" + cmd.Key;
                                if (!string.IsNullOrEmpty(cmd.Value.Parameters))
                                {
                                    string parameters = cmd.Value.Parameters;
                                    // Reemplaza %cualquiercosa% por [cualquiercosa]
                                    parameters = Regex.Replace(parameters, @"%([^%]+)%", "[$1]");
                                    commandText += " " + parameters;
                                }

                                string encodedDescription = System.Web.HttpUtility.HtmlEncode(cmd.Value.Description);
                                bloque.AppendLine($"                        <label><b>{commandText}</b></label> <small>- {cmd.Value.Description}</small><br>");
                            }

                            bloque.AppendLine("                </div>");
                            bloque.AppendLine("            </div>");
                            bloque.AppendLine("        </div>");
                            bloque.AppendLine("    </div>");

                            return bloque.ToString();
                        };

                        // Obtener colecciones (ajusta los nombres según tu ChatManager)
                        var chatManager = PolarEnvironment.GetGame().GetChatManager().GetCommands();

                        // Todos los comandos registrados
                        var todos = chatManager._commands;
                        var comandosNormales = todos.Where(x =>
    !chatManager._vipcommands.ContainsKey(x.Key) &&
    !chatManager._gangcommands.ContainsKey(x.Key) &&
     !chatManager._staffcommands.ContainsKey(x.Key) &&
      !chatManager._eventcommands.ContainsKey(x.Key) &&
      !chatManager._rpCommands.ContainsKey(x.Key) &&
      !chatManager._vehiclescommands.ContainsKey(x.Key) &&
      !chatManager._ambassadorcommands.ContainsKey(x.Key) &&
    !chatManager._jobcommands.ContainsKey(x.Key)
// Añade más condiciones si tienes otras categorías (roleplay, vehiculos, etc.)
).ToDictionary(x => x.Key, x => x.Value);

                        var vip = chatManager._vipcommands;
                        var bandas = chatManager._gangcommands;          // Ajusta
                        var trabajos = chatManager._jobcommands;      // Ajusta

                        // Generar cada bloque
                        html.Append(generarBloque("CMDS_Normales", comandosNormales));
                        html.Append(generarBloque("CMDS_VIP", chatManager._vipcommands));
                        html.Append(generarBloque("CMDS_Roleplay", chatManager._rpCommands));
                        html.Append(generarBloque("CMDS_Vehiculos", chatManager._vehiclescommands));
                        html.Append(generarBloque("CMDS_Bandas", chatManager._gangcommands));
                        html.Append(generarBloque("CMDS_Trabajos", chatManager._jobcommands));
                        if (Client.GetHabbo().Rank > 3)
                        {
                            html.Append(generarBloque("CMDS_Staff", chatManager._staffcommands));
                        }

                        // Cerrar el cuerpo
                        html.AppendLine("</div>");

                        // Enviar al cliente
                        Socket.SendWS( "compose_commands|open|" + html.ToString());
                    }
                    break;
                #endregion

                #region Jobs
                case "jobs":
                    {
                        Socket.SendWS( "compose_commands|jobs|");
                        #region Tutorial Step Check
                        if (Client.GetRoleplay().TutorialStep == 32)
                        {
                            Socket.SendWS( "compose_tutorial|32");
                        }
                        #endregion
                    }
                    break;
                #endregion

                #region Houses
                case "houses":
                    {
                        Socket.SendWS( "compose_commands|houses|");
                    }
                    break;
                #endregion

                #region Vehicles
                case "vehicles":
                    {
                        Socket.SendWS( "compose_commands|vehicles|");
                    }
                    break;
                #endregion

                #region Empresas
                case "bussines":
                    {
                        Socket.SendWS( "compose_commands|bussines|");
                    }
                    break;
                #endregion

                #region Terrenos
                case "terrains":
                    {
                        Socket.SendWS( "compose_commands|terrains|");
                    }
                    break;
                #endregion

                #region Marihauana
                case "marijane":
                    {
                        Socket.SendWS( "compose_commands|marijane|");
                    }
                    break;
                #endregion

                #region Bandas
                case "gangs":
                    {
                        Socket.SendWS( "compose_commands|gangs|");
                    }
                    break;
                #endregion

                default:
                    break;
            }
        }

        // ── Helper: envía texto como frame WebSocket usando ConnectionInformation
        private static void SendWS(ConnectionInformation socket, string message)
        {
            if (socket == null || string.IsNullOrEmpty(message)) return;
            socket.SendData(System.Text.Encoding.UTF8.GetBytes(message));
        }

    }
}
