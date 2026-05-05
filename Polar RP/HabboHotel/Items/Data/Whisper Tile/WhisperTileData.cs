using System;
using System.Data;
using Polar.Database.Interfaces;

namespace Polar.HabboHotel.Items.Data.WhisperTile
{
    public class WhisperTileData
    {
        public int ItemId;
        public string Message;

        public WhisperTileData(int Item, DataRow row = null)
        {
            ItemId = Item;
            DataRow Row = row;

            if (Row == null || !Row.Table.Columns.Contains("whisper_message"))
            {
                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("SELECT item_id, message AS whisper_message FROM `room_items_whisper_tile` WHERE `item_id` = @id LIMIT 1");
                    dbClient.AddParameter("id", ItemId);
                    Row = dbClient.getRow();
                }
            }

            if (Row == null || Row["whisper_message"] == DBNull.Value)
            {
                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.RunQuery("INSERT INTO `room_items_whisper_tile` VALUES ('" + ItemId + "','')");
                    dbClient.SetQuery("SELECT item_id, message AS whisper_message FROM `room_items_whisper_tile` WHERE `item_id` = '" + ItemId + "' LIMIT 1");
                    Row = dbClient.getRow();
                }
            }

            Message = Row["whisper_message"].ToString();
        }
    }
}