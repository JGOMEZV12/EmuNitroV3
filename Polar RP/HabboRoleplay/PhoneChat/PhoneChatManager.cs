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

namespace Polar.HabboRoleplay.PhoneChat
{
    public class PhoneChatManager
    {
        /// <summary>
        /// log4net
        /// </summary>
        private readonly ILog log = LogManager.GetLogger("Polar.HabboRoleplay.PhoneChat.PhoneChatManaer");
        

        /// <summary>
        /// Thread-safe dictionary containing all houses
        /// </summary>
        public ConcurrentDictionary<int, PhoneChat> ChatList = new ConcurrentDictionary<int, PhoneChat>();

        /// <summary>
        /// Initializes the house list dictionary
        /// </summary>
        public void Init()
        {
            ChatList.Clear();
            /*
            DataTable Houses;

            using (IQueryAdapter DB = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                DB.SetQuery("SELECT * from `play_houses`");
                Houses = DB.getTable();

                if (Houses != null)
                {
                    foreach (DataRow Row in Houses.Rows)
                    {
                        int ItemId = Convert.ToInt32(Row["sign_id"]);
                        int RoomId = Convert.ToInt32(Row["room_id"]);
                        int OwnerId = Convert.ToInt32(Row["owner_id"]);
                        int Cost = Convert.ToInt32(Row["cost"]);
                        bool ForSale = PolarEnvironment.EnumToBool(Row["for_sale"].ToString());
                        int Level = Convert.ToInt32(Row["level"]);
                        string[] Upgrades = Row["upgrades"].ToString().Split(',');
                        bool IsLocked = PolarEnvironment.EnumToBool(Row["is_locked"].ToString());
                        int InsideRoomId = Convert.ToInt32(Row["inside_room_id"]);
                        int DoorX = Convert.ToInt32(Row["door_x"]);
                        int DoorY = Convert.ToInt32(Row["door_y"]);
                        int DoorZ = Convert.ToInt32(Row["door_z"]);
                        
                        House newHouse = new House(ItemId, RoomId, OwnerId, Cost, ForSale, Level, Upgrades, IsLocked, InsideRoomId, DoorX, DoorY, DoorZ);
                        HouseList.TryAdd(ItemId, newHouse);
                    }
                }
            }

            */

            log.Info("PhoneChat Initialized 100%");
        }

        public void NewPhoneChat(int ID, int Type, int EmisorId, string EmisorName, int ReceptorId, string ReceptorName, string Msg, DateTime TimeStamp)
        {
            PhoneChat newChat = new PhoneChat(ID, Type, EmisorId, EmisorName, ReceptorId, ReceptorName, Msg, TimeStamp);
            ChatList.TryAdd(ID, newChat);
        }

        public List<PhoneChat> GetPhoneChatsByMyID(int MyId)
        {
            if (MyId == 0)
                return null;

            if (ChatList.Values.Where(x => (x.EmisorId == MyId || x.ReceptorId == MyId) && x.Type == 1).ToList().Count > 0)
                return ChatList.Values.Where(x => (x.EmisorId == MyId || x.ReceptorId == MyId) && x.Type == 1).ToList();
            else
                return null;
        }
        public List<PhoneChat> GetPhoneWhatsChatsByMyID(int MyId)
        {
            if (MyId == 0)
                return null;

            if (ChatList.Values.Where(x => (x.EmisorId == MyId || x.ReceptorId == MyId) && x.Type == 2).ToList().Count > 0)
                return ChatList.Values.Where(x => (x.EmisorId == MyId || x.ReceptorId == MyId) && x.Type == 2).ToList();
            else
                return null;
        }

        public List<PhoneChat> GetPhoneWhatsChatsByChatting(int MyId, int ToId)
        {
            if (MyId == 0)
                return null;

            if (ChatList.Values.Where(x => ((x.EmisorId == MyId && x.ReceptorId == ToId) || (x.EmisorId == ToId && x.ReceptorId == MyId)) && x.Type == 2).ToList().Count > 0)
                return ChatList.Values.Where(x => ((x.EmisorId == MyId && x.ReceptorId == ToId) || (x.EmisorId == ToId && x.ReceptorId == MyId)) && x.Type == 2).ToList();
            else
                return null;
        }

        public int GetIDbyContact(GameClient Session, string Target)
        {
            int ID = 0;
            Session.GetRoleplay().SendToName = true;// Envió a nombre de contacto

            if (!int.TryParse(PolarEnvironment.GetUserInfoBy("id", "username", Target), out ID))
            {
                // Limpiamos y dejamos solo numeros
                Target = PolarEnvironment.GetGame().GetClientManager().ClearNumbers(Target);

                if (Target.Length != 10)
                {
                    ID = 0;
                }
                else
                {
                    // Damos Formato al Número (xxx)-xxx-xxxx
                    Target = PolarEnvironment.GetGame().GetClientManager().NumberFormatRP(Target);
                    if (!int.TryParse(PolarEnvironment.GetUserIdByPhoneNumber(Target), out ID))
                    {
                        ID = 0;
                    }
                    Session.GetRoleplay().SendToName = false;// No Envió a nombre de contacto
                }
            }

            return ID;
        }
    }
}
