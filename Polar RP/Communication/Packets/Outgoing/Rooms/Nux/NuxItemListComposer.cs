using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using Polar.Database.Interfaces;

namespace Polar.Communication.Packets.Outgoing.Rooms.Nux
{
    internal class NuxItemListComposer : ServerPacket
    {
        public NuxItemListComposer() : base(ServerPacketHeader.NuxItemListComposer)
        {
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(1); // Número de páginas.

            packet.WriteInteger(1); // ELEMENTO 1
            packet.WriteInteger(3); // ELEMENTO 2
            packet.WriteInteger(3); // Número total de premios:

            using (IQueryAdapter dbQuery = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbQuery.SetQuery("SELECT * FROM `nux_gifts` LIMIT 3");
                DataTable gUsersTable = dbQuery.getTable();

                foreach (DataRow Row in gUsersTable.Rows)
                {
                    packet.WriteString(Convert.ToString(Row["image"])); // image.library.url + string
                    packet.WriteInteger(1); // items:
                    packet.WriteString(Convert.ToString(Row["title"])); // item_name (product_x_name)
                    packet.WriteString(""); // can be null
                }
            }

            #region Eybuenas xd
            //packet.WriteInteger(1); // Número de páginas.

            //packet.WriteInteger(1); // ELEMENTO 1
            //packet.WriteInteger(3); // ELEMENTO 2
            //packet.WriteInteger(2); // Número total de premios:

            //packet.WriteString("nux/hc_promotion.png"); // image.library.url + string
            //packet.WriteInteger(1); // items:
            //packet.WriteString("a0 throne"); // item_name (product_x_name)
            //packet.WriteString(""); // can be null

            //packet.WriteString("nux/xxxx.png");
            //packet.WriteInteger(1);
            //packet.WriteString("a0 spyro");
            //packet.WriteString("");


            /*  packet.WriteInteger(0);
              packet.WriteInteger(0);
              packet.WriteInteger(3);
              packet.WriteString("nux/hc_promotion.png"); // Imagen en SWFs
              packet.WriteInteger(1);
              packet.WriteString("hc_promotion"); // Nombre Premio
              packet.WriteString("");
              packet.WriteString("nux/xxxx.png");
              packet.WriteInteger(1);
              packet.WriteString("nux_gift_diamonds");
              packet.WriteString("");
              packet.WriteString("nux/xxxx.png");
              packet.WriteInteger(1);
              packet.WriteString("nux_gift_vip_1_day");
              packet.WriteString(""); */

            //packet.WriteInteger(1); // Número de páginas.

            //packet.WriteInteger(1);
            //packet.WriteInteger(1);
            //packet.WriteInteger(1);
            //packet.WriteString("nux/hc_promotion.png"); // Imagen en SWFs
            //packet.WriteInteger(1);
            //packet.WriteString("hc_promotion"); // Nombre Premio
            //packet.WriteString("");
            //packet.WriteString("nux/xxxx.png");
            //packet.WriteInteger(2);
            //packet.WriteString("nux_gift_diamonds");
            //packet.WriteString("");
            //packet.WriteString("nux/xxxx.png");
            //packet.WriteInteger(3);
            //packet.WriteString("nux_gift_vip_1_day");
            //packet.WriteString("");
            #endregion
        }
    }
}