using System;
using System.Data;
using Polar.Database.Interfaces;
using Polar.HabboRoleplay.Houses;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Rooms;
using Polar.HabboRoleplay.Misc;
using System.Linq;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Items.Data.RentableSpace;

namespace Polar.HabboRoleplay.Farming
{
    public class FarmingSpace
    {
        public int Id;
        public int ItemId;
        public int RoomId;
        public int Cost;
        public int X;
        public int Y;
        public double Z;
        public int OwnerId;
        public int Expiration;
        public bool Spawned;
        public Item Item;

        public FarmingSpace(int Id, int ItemId, int RoomId, int Cost, int X, int Y, double Z, int OwnerId, int Expiration)
        {
            this.Id = Id;
            this.ItemId = ItemId;
            this.RoomId = RoomId;
            this.Cost = Cost;
            this.X = X;
            this.Y = Y;
            this.Z = Z;
            this.OwnerId = OwnerId;
            this.Expiration = Expiration;
            this.Spawned = false;
            this.Item = null;
        }

        public void SpawnSign()
        {
            if (RoleplayManager.GenerateRoom(this.RoomId, out Room Room, false) && this.Item != null)
            {
                if (Room.GetRoomItemHandler().GetFloor.Where(x => x.Id == this.Item.Id).ToList().Count > 0)
                {
                    foreach (Item Item in Room.GetRoomItemHandler().GetFloor.Where(x => x.Id == this.Item.Id).ToList())
                    {
                        Room.GetRoomItemHandler().RemoveFurniture(null, Item.Id);
                    }
                }
            }

            if (Room != null)
            {
                this.Item = RoleplayManager.PlaceItemToRoom(null, this.ItemId, 0, this.X, this.Y, this.Z, 0, false, this.RoomId, false, "0", false, "", null, this);
                this.Spawned = true;
            }
        }

        public void BuySpace(GameClient Session, Item Item)
        {
            if (Session == null || Item == null)
                return;

            if (this.OwnerId > 0)
                return;

            this.OwnerId = Session.GetHabbo().Id;
            this.Expiration = 3600;

            //Item.RentableSpaceData.Enabled = true;
            Item.RentableSpaceData.OwnerId = Session.GetHabbo().Id;
            //Item.RentableSpaceData.TimeLeft = 3600;

            using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("UPDATE `rp_farming_spaces` SET `expiration` = @expiration, `owner_id` = @owner WHERE `id` = @id");
                dbClient.AddParameter("owner", this.OwnerId);
                dbClient.AddParameter("expiration", this.Expiration);
                dbClient.AddParameter("id", this.Id);
                dbClient.RunQuery();
            }
        }
        
    }
}