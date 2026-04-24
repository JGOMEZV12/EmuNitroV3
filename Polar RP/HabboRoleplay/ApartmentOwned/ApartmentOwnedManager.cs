using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Pathfinding;
using log4net;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;
using System.Diagnostics;
using Polar.HabboRoleplay.Apartments;

namespace Polar.HabboRoleplay.ApartmentsOwned
{
    public class ApartmentOwnedManager
    {
        /// <summary>
        /// log4net
        /// </summary>
        private readonly ILog log = LogManager.GetLogger("Polar.HabboRoleplay.Houses.ApartmentOwnedManager");

        /// <summary>
        /// Thread-safe dictionary containing all apartments list
        /// </summary>
        public ConcurrentDictionary<int, ApartmentOwned> ApartmentsOwn = new ConcurrentDictionary<int, ApartmentOwned>();

        /// <summary>
        /// Initializes the apartment list dictionary
        /// </summary>
        public void Init()
        {
            ApartmentsOwn.Clear();
            DataTable AP;

            using (IQueryAdapter DB = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                DB.SetQuery("SELECT * from `rp_apartments_owned`");
                AP = DB.getTable();

                if (AP != null)
                {
                    foreach (DataRow Row in AP.Rows)
                    {
                        int ID = Convert.ToInt32(Row["id"]);
                        int ApartId = Convert.ToInt32(Row["apart_id"]);
                        int RoomId = Convert.ToInt32(Row["room_id"]);
                        int LobbyId = Convert.ToInt32(Row["lobby_id"]);
                        int Owner = Convert.ToInt32(Row["owner"]);
                        bool ForSale = PolarEnvironment.EnumToBool(Convert.ToString(Row["for_sale"]));
                        int Price = Convert.ToInt32(Row["price"]);
                        string PaymentType = Convert.ToString(Row["payment_type"]);
                        bool FloorEditor = PolarEnvironment.EnumToBool(Convert.ToString(Row["floor_editor"]));

                        ApartmentOwned newApart = new ApartmentOwned(ID, ApartId, RoomId, LobbyId, Owner, ForSale, Price, PaymentType, FloorEditor);
                        ApartmentsOwn.TryAdd(RoomId, newApart);
                    }
                }
            }

            log.Info("Loaded " + ApartmentsOwn.Count + " apartments owned.");
        }

        public List<ApartmentOwned> GetApartmentsOwned()
        {
            List<ApartmentOwned> VO = new List<ApartmentOwned>();

            foreach (var item in ApartmentsOwn)
            {
                VO.Add(item.Value);
            }

            return VO;
        }

        public List<ApartmentOwned> GetApartmentsOwned(int userid)
        {
            List<ApartmentOwned> VO = new List<ApartmentOwned>();

            foreach (var item in ApartmentsOwn)
            {
                if(item.Value.Owner == userid)
                    VO.Add(item.Value);
            }

            return VO;
        }

        public List<ApartmentOwned> GetOfferApartmentsByLobby(int lobbyid)
        {
            List<ApartmentOwned> VO = new List<ApartmentOwned>();

            foreach (var item in ApartmentsOwn)
            {
                if (item.Value.LobbyId == lobbyid && item.Value.ForSale)
                    VO.Add(item.Value);
            }

            return VO;
        }

        public ApartmentOwned GetApartmentOwnedById(int ID)
        {
            if (ApartmentsOwn.ContainsKey(ID))
                return ApartmentsOwn[ID];
            else
                return null;
        }

        public ApartmentOwned GetApartmentByInsideRoom(int insideroom)
        {
            if (ApartmentsOwn.Values.Where(x => x.RoomId == insideroom).ToList().Count > 0)
                return ApartmentsOwn.Values.FirstOrDefault(x => x.RoomId == insideroom);
            else
                return null;
        }

        public bool BuyNewApartment(GameClient Session, string Model, string Name, int ApartId, int LobbyId, string City, bool FloorEditor)
        {
            RoomData NewRoom = PolarEnvironment.GetGame().GetRoomManager().CreateRoom(Session, Name, "", Model, 9, City, 50, 0);
            if (NewRoom != null)
            {
                int APID = 0;
                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("INSERT INTO `rp_apartments_owned` (`apart_id`, `room_id`, `lobby_id`, `owner`, `floor_editor`) VALUES ('" + ApartId + "', '" + NewRoom.Id + "', '" + LobbyId + "', '" + Session.GetHabbo().Id + "', '" + PolarEnvironment.BoolToEnum(FloorEditor) + "')");

                    APID = Convert.ToInt32(dbClient.InsertQuery());

                    ApartmentOwned newApart = new ApartmentOwned(APID, ApartId, NewRoom.Id, LobbyId, Session.GetHabbo().Id, false, 0, "Dinero", FloorEditor);
                    ApartmentsOwn.TryAdd(NewRoom.Id, newApart);
                    return true;
                }
            }
            return false;
        }

        public bool ToggleOfferApartment(ApartmentOwned AP, int Price, string Moneda)
        {
            if (AP != null)
            {
                AP.Price = Price;
                AP.PaymentType = Moneda;
                AP.ForSale = !AP.ForSale;

                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.RunQuery("UPDATE `rp_apartments_owned` SET `for_sale` = '" + PolarEnvironment.BoolToEnum(AP.ForSale) + "', `price` = '" + Price + "', `payment_type` = '" + Moneda + "' WHERE `room_id` = '" + AP.RoomId + "' LIMIT 1");
                }
            }
            return AP.ForSale;
        }

        public bool BuyOfferApartment(GameClient Session, ApartmentOwned AP)
        {
            if (!AP.ForSale)
                return false;

            if (AP.Owner == Session.GetHabbo().Id)
                return false;

            GameClient Owner = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(AP.Owner);
            double calc = AP.Price - (AP.Price * 0.15); // Disccount 15%
            int pay = (int) calc;

            // Si el dueño está Online
            if (Owner != null && Owner.GetHabbo() != null)
            {
                if (AP.PaymentType == "Dinero")
                {
                    Owner.GetHabbo().Credits += pay;
                    //Owner.GetPlay().MoneyEarned += pay;
                    Owner.GetHabbo().UpdateCreditsBalance();
                    Owner.SendNotification("Tu apartamento ha sido comprado por " + Session.GetHabbo().Username + " y recibes $" + String.Format("{0:N0}", pay));

                }
                else
                {
                    Owner.GetHabbo().Diamonds += pay;
                    //Owner.GetPlay().PLEarned += pay;
                    Owner.GetHabbo().UpdateDiamondsBalance();
                    Owner.SendNotification("Tu apartamento ha sido comprado por " + Session.GetHabbo().Username + " y recibes " + pay + " PL");
                }
            }
            // Si el dueño está Offline
            else
            {
                using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    if (AP.PaymentType == "Dinero")
                    {
                        dbClient.SetQuery("UPDATE `users` SET `credits` = (credits + @prize) WHERE `id` = @winner LIMIT 1");
                        dbClient.AddParameter("prize", pay);
                        dbClient.AddParameter("winner", AP.Owner);
                        dbClient.RunQuery();
                    }
                    else
                    {
                        dbClient.SetQuery("UPDATE `users` SET `vip_points` = (vip_points + @prize) WHERE `id` = @winner LIMIT 1");
                        dbClient.AddParameter("prize", pay);
                        dbClient.AddParameter("winner", AP.Owner);
                        dbClient.RunQuery();
                    }
                }
            }

            AP.Owner = Session.GetHabbo().Id;
            AP.ForSale = false;

            using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("UPDATE `rp_apartments_owned` SET `owner` = @owner, `for_sale` = @forsale WHERE `room_id` = @roomid");
                dbClient.AddParameter("owner", AP.Owner);
                dbClient.AddParameter("forsale", PolarEnvironment.BoolToEnum(AP.ForSale));
                dbClient.AddParameter("roomid", AP.RoomId);// Just for WHERE in SQL
                dbClient.RunQuery();
            }

            if(AP.RoomId > 0)
                RoleplayManager.AssingRights(Session, AP.RoomId, true);

            return true;
        }

        public bool SellApartmentGouv(GameClient Session, ApartmentOwned AP)
        {
            if (AP != null)
            {
                ApartmentOwned trash;
                ApartmentsOwn.TryRemove(AP.ID, out trash);
                return RoleplayManager.DeleteRoomRP(Session, AP.RoomId);
            }

            return false;
        }
    }
}
