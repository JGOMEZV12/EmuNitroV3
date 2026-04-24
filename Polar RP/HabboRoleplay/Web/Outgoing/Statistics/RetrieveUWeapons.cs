using ConnectionManager;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polar.HabboRoleplay.Weapons;
using Polar.Net;
using Polar.HabboHotel.GameClients;
using System.IO;
using Polar.HabboHotel.Roleplay.Web;
using Polar.Database.Interfaces;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboRoleplay.Web.Outgoing.Statistics
{
    /// <summary>
    /// RetrieveUStatsWebEvent class.
    /// </summary>
    class RetrieveUWeapons : IWebEvent
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

            Client.GetRoleplay().OwnedWeapons = null;
            Client.GetRoleplay().OwnedWeapons = Client.GetRoleplay().LoadAndReturnWeapons();

            string Message = "";
            foreach (Weapon Weapon in Client.GetRoleplay().OwnedWeapons.Values)
            {
                Message += "name:" + Weapon.Name + ",bullets:" + Weapon.TotalBullets + ";";
            }
            if (String.IsNullOrEmpty(Message))
                return;

            Socket.SendWS( "compose_weapons_new|" + Message);

            string Message2 = "";
            foreach (Weapon Weapon in Client.GetRoleplay().OwnedWeapons.Values.Where(x => x.BaulCar == 0))
            {
                string weaponName = Weapon.Name;

                // Si el EffectId es mayor que 0, obtenemos el nombre del skin desde la base de datos
                if (Weapon.EffectID > 0)
                {
                    using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                    {
                        dbClient.SetQuery("SELECT name FROM rp_weapons_skins WHERE effectid = @effectid");
                        dbClient.AddParameter("effectid", Weapon.EffectID);

                        var result = dbClient.getRow();
                        if (result != null)
                        {
                            weaponName = result["name"].ToString();
                        }
                    }
                }
                Message2 += "<div class=\"estilo-inventario\" id=\"UpdateW\">";
                Message2 += "<div class=\"pop-number-bullets " + Weapon.Name + "\" idwe=\"" + Weapon.Name + "\" idhyb=\"" + Weapon.TotalBullets + "\">";
                Message2 += "<span id=\"bullets2\">" + Weapon.TotalBullets + "</span></div>";
                Message2 += "<div class=\"item-inv\" id=\""+Weapon.Name+"\" idhb=\""+Weapon.Name+"\">";
                Message2 += "<div style=\"margin: 9px;width: 41px;left: 0px;height: 41px;image-rendering: optimizequality;background: url("+RoleplayManager.CdnURL2+"/armas/" + weaponName + ".png) no-repeat;background-size: 100%;\">\r\n                                            </div>";
                Message2 += "</div></div>";
            }
            if (String.IsNullOrEmpty(Message2))
                return;

            Socket.SendWS( "compose_weapons|" + Message2);

        }

        // ── Helper: envía texto como frame WebSocket usando ConnectionInformation
        private static void SendWS(ConnectionInformation socket, string message)
        {
            if (socket == null || string.IsNullOrEmpty(message)) return;
            socket.SendData(System.Text.Encoding.UTF8.GetBytes(message));
        }

    }
}
