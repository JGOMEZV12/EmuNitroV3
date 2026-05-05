using System;
using System.Data;
using Polar.Database.Interfaces;
using Polar.Communication.Packets.Incoming;


namespace Polar.HabboHotel.Items.Data.Toner
{
    public class TonerData
    {
        public int ItemId;
        public int Hue;
        public int Saturation;
        public int Lightness;
        public int Enabled;

        public TonerData(int Item, DataRow row = null)
        {
            ItemId = Item;

            DataRow Row = row;

            if (Row == null || !Row.Table.Columns.Contains("toner_enabled"))
            {
                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("SELECT enabled AS toner_enabled,data1,data2,data3 FROM room_items_toner WHERE id=@id LIMIT 1");
                    dbClient.AddParameter("id", ItemId);
                    Row = dbClient.getRow();
                }
            }

            if (Row == null || Row["toner_enabled"] == DBNull.Value)
            {
                //throw new NullReferenceException("No toner data found in the database for " + ItemId);
                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.RunQuery("INSERT INTO `room_items_toner` VALUES (" + ItemId + ",'0',0,0,0)");
                    dbClient.SetQuery("SELECT enabled AS toner_enabled,data1,data2,data3 FROM room_items_toner WHERE id=" + ItemId + " LIMIT 1");
                    Row = dbClient.getRow();
                }
            }

            Enabled = Convert.ToInt32(Row["toner_enabled"]);
            Hue = Convert.ToInt32(Row["data1"]);
            Saturation = Convert.ToInt32(Row["data2"]);
            Lightness = Convert.ToInt32(Row["data3"]);
        }
    }
}