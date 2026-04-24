using ConnectionManager;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polar.HabboHotel.Rooms;
using Polar.Database.Interfaces;
using Polar.Net;
using System.Data;
using Polar.HabboHotel.GameClients;
using System.IO;
using Polar.HabboHotel.Roleplay.Web;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboRoleplay.Web.Outgoing.Statistics
{
    /// <summary>
    /// RetrieveUStatsWebEvent class.
    /// </summary>
    class MapaWebEvent : IWebEvent
    {
        /// <summary>
        /// Executes socket data.
        /// </summary>
        /// <param name="Client"></param>
        /// <param name="Data"></param>
        /// <param name="Socket"></param>
        public void Execute(GameClient Client, string Data, ConnectionInformation Socket)
        {
            if (!PolarEnvironment.GetGame().GetWebEventManager().SocketReady(Client, true) || !PolarEnvironment.GetGame().GetWebEventManager().SocketReady(Socket))
                return;

            if (Client.GetRoleplay().IsJailed || Client.GetRoleplay().IsDead)
            {
                Client.SendNotification("¡No puedes utilizar el mapa mientras estes muerto o encarcelado!");
                return;
            }
               

            int UserId = 0;
            DataTable GetRooms = null;
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                    dbClient.SetQuery("SELECT * FROM `rooms` ORDER BY `users_now` DESC");
                    GetRooms = dbClient.getTable();
            }
            string Message2 = "";
            List<RoomData> Results = new List<RoomData>();
            if (GetRooms != null)
            {
                foreach (DataRow Row in GetRooms.Rows)
                {
                    using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                    {
                        dbClient.SetQuery("SELECT * FROM `rp_rooms` WHERE `id` = " + Convert.ToInt32(Row["id"]) + " LIMIT 1");
                        DataRow RPRow = dbClient.getRow();

                        RoomData RoomData = PolarEnvironment.GetGame().GetRoomManager().FetchRoomData(Convert.ToInt32(Row["id"]), Row, RPRow);
                        if (RoomData != null && !Results.Contains(RoomData))
                        {
                            Message2 += "<div class=\"corpo-quartos\">";
                            Message2 += "<div class=\"corpo-nome-id\"><div class=\"gif-users\"><img src=\"" + RoleplayManager.CdnURL2 + "/images/vacio.gif\"></div>";
                            Message2 += "<div class=\"corpo-nome\" data-name=\"" + Row["caption"] + "\">" + Row["caption"] + "</div>";
                            Message2 += "<ul class=\"infostax-ul\"><li>";
                            Message2 += "<div class=\"dados-ap\">";
                            Message2 += "<img src=\"//2.bp.blogspot.com/-GlPGXSHTLaE/V1kVsm8K1zI/AAAAAAAAqAU/tYfNj0FiyZAiQoWo2M9jK8rJ7PvjIHUqwCKgB/s1600/iconhabbosmall.png\">";
                            Message2 += "<span class=\"ap-id ml-5px\">" + Row["id"] + "</span></div>";
                            Message2 += "<div class=\"dados-ap ml-5px\">";
                            Message2 += "<img src=\"//1.bp.blogspot.com/-O8jDEgowsAA/XK0oYizaInI/AAAAAAABOsg/5hQ9ojZE2SMVvjvh7lwFcq0D-C3qx0PagCKgBGAs/s1600/new_20.gif\">";
                            Message2 += "<span class=\"ap-id ml-5px\">" + Row["users_now"] + "</span></div>";
                            Message2 += "<div class=\"dados-ap ml-5px\" id=\"botao-taxi\" valortaxi=\"0\" idtaxi=\"" + Row["id"] + "\">";
                            Message2 += "<img src=\"//4.bp.blogspot.com/-zi3f8VG-3IE/XK0oYklHK3I/AAAAAAABOsg/KBNWdeeFLMwDonwzZVwovc4zGZBGdipiACKgBGAs/s1600/new_09.gif\">";
                            Message2 += "</div></li></ul></div></div>";
                        }
                    }
                }
            }
           
            
           /* foreach (Room Room in PolarEnvironment.GetGame().GetRoomManager().GetRooms().ToList().OrderBy(key => key.Id))
            {
                Message2 += "<div class=\"corpo-quartos\">";
                Message2 += "<div class=\"gif-users\"><img src=\"/v2/images/vacio.gif\"></div>";
                Message2 += "<div class=\"corpo-nome-id\">";
                Message2 += "<div class=\"corpo-nome\" data-name=\"" + Room.Name +"\">" + Room.Name +"</div>";
                Message2 += "<ul class=\"infostax-ul\"><li>";
                Message2 += "<div class=\"dados-ap\">";
                Message2 += "<img src=\"//2.bp.blogspot.com/-GlPGXSHTLaE/V1kVsm8K1zI/AAAAAAAAqAU/tYfNj0FiyZAiQoWo2M9jK8rJ7PvjIHUqwCKgB/s1600/iconhabbosmall.png\">";
                Message2 += "<span class=\"ap-id ml-5px\">"+ Room.Id +"</span></div>";
                Message2 += "<div class=\"dados-ap ml-5px\">";
                Message2 += "<img src=\"//1.bp.blogspot.com/-O8jDEgowsAA/XK0oYizaInI/AAAAAAABOsg/5hQ9ojZE2SMVvjvh7lwFcq0D-C3qx0PagCKgBGAs/s1600/new_20.gif\">";
                Message2 += "<span class=\"ap-id ml-5px\">" + Room.UserCount + "</span></div>";
                Message2 += "<div class=\"dados-ap ml-5px\" id=\"botao-taxi\" valortaxi=\"0\" idtaxi=\""+ Room.Id +"\">";
                Message2 += "<img src=\"//4.bp.blogspot.com/-zi3f8VG-3IE/XK0oYklHK3I/AAAAAAABOsg/KBNWdeeFLMwDonwzZVwovc4zGZBGdipiACKgBGAs/s1600/new_09.gif\">";
                Message2 += "</div></li></ul></div></div>";
            }*/
            if (String.IsNullOrEmpty(Message2))
                return;

            Socket.SendWS( "compose_mapa_new|" + Message2);

        }

        // ── Helper: envía texto como frame WebSocket usando ConnectionInformation
        private static void SendWS(ConnectionInformation socket, string message)
        {
            if (socket == null || string.IsNullOrEmpty(message)) return;
            socket.SendData(System.Text.Encoding.UTF8.GetBytes(message));
        }

    }
}
