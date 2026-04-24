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

namespace Polar.HabboRoleplay.Army
{
    class ArmyManager
    {
        /// <summary>
        /// log4net
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger("Polar.HabboRoleplay.Army.ArmyManager");

        /// <summary>
        /// Thread-safe dictionary containing all roleplay Armys
        /// </summary>
        public static ConcurrentDictionary<string, Army> ArmyList = new ConcurrentDictionary<string, Army>();

        /// <summary>
        /// Initializes the Army list dictionary
        /// </summary>
        public static void Initialize()
        {
            ArmyList.Clear();
            DataTable Armys;

            using (IQueryAdapter DB = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                DB.SetQuery("SELECT * from `rp_army`");
                Armys = DB.getTable();

                if (Armys != null)
                {
                    foreach (DataRow Army in Armys.Rows)
                    {
                        string Name = Army["name"].ToString();
                        string Type = Convert.ToString(Army["type"]);
                        int ItemId = Convert.ToInt32(Army["item_id"]);
                        string ExtraData = Army["extra_data"].ToString();
                        int Cost = Convert.ToInt32(Army["cost"]);
                        int Health = Convert.ToInt32(Army["health"]);
                        int Energy = Convert.ToInt32(Army["energy"]);
                        int Alcohol = Convert.ToInt32(Army["alcohol"]);
                        int Hunger = Convert.ToInt32(Army["hunger"]);
                        string ServeText = Army["serve_text"].ToString();
                        string EatText = Army["eat_text"].ToString();
                        bool Servable = PolarEnvironment.EnumToBool(Army["servable"].ToString());

                        Army newArmy = new Army(Name, Type, ItemId, ExtraData, Cost, Health, Energy, Alcohol, Hunger, ServeText, EatText, Servable);
                        ArmyList.TryAdd(Name, newArmy);
                    }
                }
            }

            // log.Info("Cargada " + ArmyList.Count + " comidas.");
            Out.WriteLine("Carga: " + ArmyList.Count + " objetos " + PolarEnvironment.GetConfig().data["hotel.name"] + " Fire", "Polar.HabboRoleplay.Army.ArmyManager", ConsoleColor.DarkGray);
        }

        /// <summary>
        /// Gets the Army based on itemid
        /// </summary>
        public static Army GetArmy(int itemid)
        {
            try
            {
                Army theArmy = null;

                foreach (Army Army in ArmyList.Values)
                {
                    if (Army.ItemId == itemid)
                    {
                        return Army;
                    }
                }

                return theArmy;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the Army/drink based on Army/drink name
        /// </summary>
        public static Army GetArmyAndDrink(string name)
        {
            try
            {
                Army theArmyanddrink = null;

                foreach (Army Army in ArmyList.Values)
                {
                    if (Army.Name == name.ToLower())
                    {
                        return Army;
                    }
                }

                return theArmyanddrink;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the Army based on Army name
        /// </summary>
        public static Army GetArmy(string name)
        {
            try
            {
                Army theArmy = null;

                foreach (Army Army in ArmyList.Values)
                {
                    if (Army.Type != "army")
                        continue;

                    if (Army.Name == name.ToLower())
                    {
                        return Army;
                    }
                }

                return theArmy;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the drink based on drink name
        /// </summary>
        public static Army GetDrink(string name)
        {
            try
            {
                Army thedrink = null;

                foreach (Army Army in ArmyList.Values)
                {
                    if (Army.Type != "drink")
                        continue;

                    if (Army.Name == name.ToLower())
                    {
                        return Army;
                    }
                }

                return thedrink;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Checks if the square infront of the user can be served on
        /// </summary>
        public static bool CanServe(RoomUser User)
        {
            try
            {

                // hardys a retard

                bool CanPlace = false;

                Room Room;
                if (!PolarEnvironment.GetGame().GetRoomManager().TryGetRoom(User.RoomId, out Room))
                    return false;

                foreach (Item Item in Room.GetRoomItemHandler().GetFloor)
                {
                    foreach (var point in Item.GetAffectedTiles)
                    {
                        if (User.SquareInFront.X == point.X && User.SquareInFront.Y == point.Y)
                        {
                            if (Item.Data.ItemName.ToLower().Contains("table") || Item.Data.PublicName.ToLower().Contains("table"))
                            {
                                CanPlace = true;
                                break;
                            }
                        }
                    }

                    if (CanPlace)
                        break;
                }

                foreach (Item Item in Room.GetRoomItemHandler().GetFloor)
                {
                    if (Item.GetX == User.SquareInFront.X && Item.GetY == User.SquareInFront.Y)
                    {
                        if (GetArmy(Item.BaseItem) != null)
                        {
                            CanPlace = false;
                            break;
                        }
                    }
                }
                return CanPlace;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Provides a string containing all the Army or drinks the session can serve
        /// </summary>
        public static string GetServableItems(HabboHotel.GameClients.GameClient Session)
        {
            if (!HabboHotel.Groups.GroupManager.HasJobCommand(Session, "serve"))
                return "";

            StringBuilder Items = new StringBuilder();
            var List = new List<Army>();

            foreach (var item in ArmyList.Values)
            {
                if (!HabboHotel.Groups.GroupManager.HasJobCommand(Session, item.Type.ToLower()))
                    continue;

                if (!List.Contains(item))
                    List.Add(item);
            }

            int count = 0;
            foreach (var item in List)
            {
                count++;

                Items.Append(item);

                if (count < List.Count)
                    Items.Append(",");
            }

            List = null;
            return Items.ToString();
        }

        /// <summary>
        /// Provides a list containing all the Army or drinks the bot can serve
        /// </summary>
        public static List<Army> GetServableBotItems(string type)
        {
            List<Army> ServableItems = new List<Army>();

            foreach (var Army in ArmyList.Values)
            {
                if (Army.Type.ToLower() != type.ToLower())
                    continue;

                if (!ServableItems.Contains(Army))
                    ServableItems.Add(Army);
            }

            return ServableItems;
        }
    }
}
