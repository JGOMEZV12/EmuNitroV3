using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Items;

using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboRoleplay.Houses
{
    public class House
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
        public int Type; // 1 = Normal | 2 = Robo | 3 = Terreno
        public long Last_Forcing;
        public string[] Space;

        public House(int ItemId, int RoomId, int OwnerId, int Cost, bool ForSale, int Level, string[] Upgrades, bool IsLocked, int InsideRoomId, int DoorX, int DoorY, double DoorZ, int Type, long Last_Forcing, string[] Space)
        {
            this.ItemId = ItemId;
            this.RoomId = RoomId;
            this.OwnerId = OwnerId;
            this.Cost = Cost;
            this.ForSale = ForSale;
            this.Level = Level;
            this.Upgrades = Upgrades;
            this.IsLocked = IsLocked;
            this.InsideRoomId = InsideRoomId;
            this.DoorX = DoorX;
            this.DoorY = DoorY;
            this.DoorZ = DoorZ;
            this.Type = Type;
            this.Last_Forcing = Last_Forcing;
            this.Space = Space;
        }
        
        // OFF
        public void SpawnSign()
        {
            /*
            if (RoleplayManager.GenerateRoom(this.Sign.RoomId, out Room Room, false) && Sign.Item != null)
            {
                if (Room.GetRoomItemHandler().GetFloor.Where(x => x.Id == this.Sign.Item.Id).ToList().Count > 0)
                {
                    foreach (Item Item in Room.GetRoomItemHandler().GetFloor.Where(x => x.Id == this.Sign.Item.Id).ToList())
                    {
                        Room.GetRoomItemHandler().RemoveFurniture(null, Item.Id);
                    }
                }
            }

            if (Room != null)
            {
                this.Sign.Item = RoleplayManager.PlaceItemToRoomRP(null, 5224, 0, this.Sign.X, this.Sign.Y, this.Sign.Z, 0, false, this.Sign.RoomId, false, "0", false, "", this);
                this.Sign.Spawned = true;
            }
            */
        }

        public void UpdateCost(int Cost, bool indb = true)
        {
            this.Cost = Cost;

            if (indb)
            {
                using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("UPDATE `rp_houses` SET `cost` = @cost WHERE `owner_id` = @owner");
                    dbClient.AddParameter("owner", this.OwnerId);
                    dbClient.AddParameter("cost", this.Cost);
                    dbClient.RunQuery();
                }
            }
        }

        public void BuyHouse(GameClient Session, bool indb = true)
        {
            if (!this.ForSale)
                return;

            bool pl = false;

            if (this.Cost <= 100)
                pl = true;

            // Si tiene dueño que no sea 0
            if (this.OwnerId != 0 && Session.GetHabbo().Id != this.OwnerId)
            {
                GameClient Owner = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(this.OwnerId);
                // Si el dueño está Online
                if (Owner != null && Owner.GetHabbo() != null)
                {
                    if (!pl)
                    {
                        Owner.GetHabbo().Credits += this.Cost;
                       // Owner.GetPlay().MoneyEarned += this.Cost;
                        Owner.GetHabbo().UpdateCreditsBalance();
                        Owner.SendNotification("Tu propiedad ha sido comprada por " + Session.GetHabbo().Username + " y recibes $" + this.Cost);

                    }
                    else
                    {
                        Owner.GetHabbo().Diamonds += this.Cost;
                        //Owner.GetPlay().PLEarned += this.Cost;
                        Owner.GetHabbo().UpdateDiamondsBalance();
                        Owner.SendNotification("Tu propiedad ha sido comprada por " + Session.GetHabbo().Username + " y recibes " + this.Cost + " PL");
                    }
                }
                // Si el dueño está Offline
                else
                {   
                    using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                    {
                        if (!pl)
                        {
                            dbClient.SetQuery("UPDATE `users` SET `credits` = (credits + @prize) WHERE `id` = @winner LIMIT 1");
                            dbClient.AddParameter("prize", this.Cost);
                            dbClient.AddParameter("winner", this.OwnerId);
                            dbClient.RunQuery();
                        }
                        else
                        {
                            dbClient.SetQuery("UPDATE `users` SET `vip_points` = (vip_points + @prize) WHERE `id` = @winner LIMIT 1");
                            dbClient.AddParameter("prize", this.Cost);
                            dbClient.AddParameter("winner", this.OwnerId);
                            dbClient.RunQuery();
                        }
                    }
                }
            }

            this.OwnerId = Session.GetHabbo().Id;
            this.ForSale = false;

            if (indb)
            {
                using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("UPDATE `rp_houses` SET `room_id` = @roomid, `owner_id` = @owner, `for_sale` = @forsale WHERE `sign_id` = @signid");
                    dbClient.AddParameter("owner", this.OwnerId);
                    dbClient.AddParameter("roomid", Session.GetHabbo().CurrentRoomId);
                    dbClient.AddParameter("forsale", PolarEnvironment.BoolToEnum(this.ForSale));
                    dbClient.AddParameter("signid", this.ItemId);// Just for WHERE in SQL
                    dbClient.RunQuery();
                }
            }

            if (this.InsideRoomId > 0)
                RoleplayManager.AssingRights(Session, this.InsideRoomId);
        }

        public void SellHouse(GameClient Session, bool indb = true)
        {
            if (this.ForSale)
                return;

            if (this.OwnerId != Session.GetHabbo().Id)
                return;

            this.ForSale = true;

            if (indb)
            {
                using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("UPDATE `rp_houses` SET `for_sale` = @forsale WHERE `owner_id` = @owner AND `sign_id` = @signid");
                    dbClient.AddParameter("forsale", PolarEnvironment.BoolToEnum(this.ForSale));
                    dbClient.AddParameter("owner", this.OwnerId);// Just for WHERE in sql
                    dbClient.AddParameter("signid", this.ItemId);// Just for WHERE in sql
                    dbClient.RunQuery();
                }
            }
        }

        public void NoSellHouse(GameClient Session, bool indb = true)
        {
            if (!this.ForSale)
                return;

            if (this.OwnerId != Session.GetHabbo().Id)
                return;

            this.ForSale = false;

            if (indb)
            {
                using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("UPDATE `rp_houses` SET `for_sale` = @forsale WHERE `owner_id` = @owner AND `sign_id` = @signid");
                    dbClient.AddParameter("forsale", PolarEnvironment.BoolToEnum(this.ForSale));
                    dbClient.AddParameter("owner", this.OwnerId);// Just for WHERE in sql
                    dbClient.AddParameter("signid", this.ItemId);// Just for WHERE in sql
                    dbClient.RunQuery();
                }
            }
        }

        public void OpenHouse(GameClient Session, bool indb = true)
        {
            if (!this.IsLocked)
                return;

            if (this.OwnerId != Session.GetHabbo().Id)
                return;

            this.IsLocked = false;

            if (indb)
            {
                using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("UPDATE `rp_houses` SET `is_locked` = @doorhouse WHERE `owner_id` = @owner AND `sign_id` = @signid");
                    dbClient.AddParameter("doorhouse", PolarEnvironment.BoolToEnum(this.IsLocked));
                    dbClient.AddParameter("owner", this.OwnerId);// Just for WHERE in sql
                    dbClient.AddParameter("signid", this.ItemId);// Just for WHERE in sql
                    dbClient.RunQuery();
                }
            }
        }

        public void CloseHouse(GameClient Session, bool indb = true)
        {
            if (this.IsLocked)
                return;

            if (this.OwnerId != Session.GetHabbo().Id)
                return;

            this.IsLocked = true;

            if (indb)
            {
                using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("UPDATE `rp_houses` SET `is_locked` = @doorhouse WHERE `owner_id` = @owner AND `sign_id` = @signid");
                    dbClient.AddParameter("doorhouse", PolarEnvironment.BoolToEnum(this.IsLocked));
                    dbClient.AddParameter("owner", this.OwnerId);// Just for WHERE in sql
                    dbClient.AddParameter("signid", this.ItemId);// Just for WHERE in sql
                    dbClient.RunQuery();
                }
            }
        }

        public void ForceHouse(long Now, bool indb = true)
        {
            this.Last_Forcing = Now;

            if (indb)
            {
                using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("UPDATE `rp_houses` SET `last_forcing` = @timenow WHERE `sign_id` = @signid");
                    dbClient.AddParameter("timenow", Now);
                    dbClient.AddParameter("signid", this.ItemId);// Just for WHERE in sql
                    dbClient.RunQuery();
                }
            }
        }
        
    }
}
