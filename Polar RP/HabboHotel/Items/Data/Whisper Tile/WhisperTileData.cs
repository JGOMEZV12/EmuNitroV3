using System;
using System.Data;
using Polar.Database.Interfaces;

namespace Polar.HabboHotel.Items.Data.WhisperTile
{
    public class WhisperTileData
    {
        public int ItemId;
        public string Message;

        public WhisperTileData(int Item)
        {
            ItemId = Item;
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT * FROM `room_items_whisper_tile` WHERE `item_id` = '" + ItemId + "' LIMIT 1");
                DataRow Row = dbClient.getRow();

                if (Row == null)
                {
                    dbClient.RunQuery("INSERT INTO `room_items_whisper_tile` VALUES ('" + ItemId + "','')");
                    dbClient.SetQuery("SELECT * FROM `room_items_whisper_tile` WHERE `item_id` = '" + ItemId + "' LIMIT 1");
                    Row = dbClient.getRow();
                }

                Message = Row["message"].ToString();
            }
        }
    }
}