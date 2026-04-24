using System;
using System.Data;
using Polar.Database.Interfaces;
using Polar.HabboRoleplay.Farming;

namespace Polar.HabboHotel.Items.Data.RentableSpace
{
    public class RentableSpaceData
    {
        public int ItemId;
        public int RoomId;
        public int OwnerId;
        public int Cost;
        public bool ForSale;
        public int Level;
        public string[] Upgrades;
        public bool IsLocked;
        public int InsideRoomId;
        public int DoorX;
        public int DoorY;
        public double DoorZ;
        public int Type;
        public long Last_Forcing;
        public FarmingSpace FarmingSpace;

        public RentableSpaceData(int Item, int RoomId, int X, int Y, double Z)
        {
            this.ItemId = Item;
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT * FROM `rp_houses` WHERE `sign_id` = '" + ItemId + "' LIMIT 1");
                DataRow Row = dbClient.getRow();

                if (Row == null)
                {
                    dbClient.RunQuery("INSERT INTO `rp_houses` VALUES ('" + ItemId + "','" + RoomId + "','0','20000', '1', '1', 'none', '0', '" + RoomId + "', '" + X + "', '" + Y + "', '" + Z + "', '1', '0', '1,1;10,1;1,10;10,10')");
                    dbClient.SetQuery("SELECT * FROM `rp_houses` WHERE `sign_id` = '" + ItemId + "' LIMIT 1");
                    Row = dbClient.getRow();
                }
                else
                {
                    this.ItemId = Convert.ToInt32(Row["sign_id"]);
                    this.RoomId = Convert.ToInt32(Row["room_id"]);
                    this.OwnerId = Convert.ToInt32(Row["owner_id"]);
                    this.Cost = Convert.ToInt32(Row["cost"]);
                    this.ForSale = PolarEnvironment.EnumToBool(Row["for_sale"].ToString());
                    this.Level = Convert.ToInt32(Row["level"]);
                    //this.Upgrades[0] = Row["upgrades"].ToString();
                    this.IsLocked = PolarEnvironment.EnumToBool(Row["is_locked"].ToString());
                    this.InsideRoomId = Convert.ToInt32(Row["inside_room_id"]);
                    this.DoorX = Convert.ToInt32(Row["door_x"]);
                    this.DoorY = Convert.ToInt32(Row["door_y"]);
                    this.DoorZ = Convert.ToInt32(Row["door_z"]);
                    this.Type = Convert.ToInt32(Row["type"]);
                    this.Last_Forcing = Convert.ToInt32(Row["last_forcing"]);
                    this.FarmingSpace = null;
                }
            }
        }

        public RentableSpaceData(FarmingSpace FarmingSpace, int ItemId)
        {
            this.OwnerId = FarmingSpace.OwnerId;
            //this.Enabled = FarmingSpace.OwnerId == 0 ? false : true;
            this.ItemId = ItemId;
            //this.House = null;
            this.FarmingSpace = FarmingSpace;
            //this.TimeLeft = FarmingSpace.Expiration;
        }

        public RentableSpaceData(RentableSpaceData House, int ItemId)
        {
            this.ItemId = ItemId;
            this.RoomId = House.RoomId;
            this.OwnerId = House.OwnerId;
            this.Cost = House.Cost;
            this.ForSale = House.ForSale;
            this.Level = House.Level;
            this.Upgrades = House.Upgrades;
            this.IsLocked = House.IsLocked;
            this.InsideRoomId = House.InsideRoomId;
            this.DoorX = House.DoorX;
            this.DoorY = House.DoorY;
            this.DoorZ = House.DoorZ;
            this.FarmingSpace = null;
        }
        public void UpdateData()
        {
            using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("UPDATE `rp_houses` SET `owner_id` = @ownerid, `for_sale` = @forsale, `is_locked` = @islocked WHERE `sign_id` = @itemid");
                dbClient.AddParameter("itemid", this.ItemId);
                dbClient.AddParameter("ownerid", this.OwnerId);
                dbClient.AddParameter("forsale", PolarEnvironment.BoolToEnum(this.ForSale));
                dbClient.AddParameter("islocked", PolarEnvironment.BoolToEnum(this.IsLocked));
                dbClient.RunQuery();
            }
        }
    }
}