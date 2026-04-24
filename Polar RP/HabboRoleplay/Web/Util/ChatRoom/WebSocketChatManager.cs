using log4net;
using Polar.Core;
using Polar.Database.Interfaces;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Polar.HabboRoleplay.Web.Util.ChatRoom
{
    public class WebSocketChatManager
    {
        /// <summary>
        /// log4net
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger("Polar.HabboRoleplay.Food.FoodManager");

        /// <summary>
        /// Thread safe web socket chatrooms dictionary
        /// </summary>
        public static ConcurrentDictionary<string, WebSocketChatRoom> RunningChatRooms = new ConcurrentDictionary<string, WebSocketChatRoom>();

        /// <summary>
        /// Main timer for WebSocketChatManager processing additional tasks
        /// </summary>
        public static WebSocketChatManagerMainTimer WebSocketChatManagerMainTimer = null;

        /// <summary>
        /// Initialize WebSocketChatManager and insert chats from database
        /// </summary>
        public static void Initialiaze()
        {

            #region Stop running chats
            if (RunningChatRooms.Count > 0)
            {
                StopAllChats();
            }

            #endregion

            #region Insert data
            RunningChatRooms.Clear();
            DataTable ChatRooms;

            using (IQueryAdapter DB = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                DB.SetQuery("SELECT * from `rp_chat_rooms`");
                ChatRooms = DB.getTable();

                if (ChatRooms != null)
                {
                    foreach (DataRow ChatRoom in ChatRooms.Rows)
                    {
                        string ChatName = ChatRoom["name"].ToString();
                        int OwnerID = Convert.ToInt32(ChatRoom["owner_id"]);
                        string ChatPassword = ChatRoom["password"].ToString();
                        int GangID = Convert.ToInt32(ChatRoom["gang_id"]);
                        string Locked = ChatRoom["locked"].ToString();
                        List<int> ChatAdmins = (!String.IsNullOrEmpty(Convert.ToString(ChatRoom["admins"])) && Convert.ToString(ChatRoom["admins"]).Contains(":")) ? (Convert.ToString(ChatRoom["admins"]).StartsWith(":")) ? Convert.ToString(ChatRoom["admins"]).Remove(0, 1).Split(':').Select(int.Parse).ToList() : Convert.ToString(ChatRoom["admins"]).Split(':').Select(int.Parse).ToList() : new List<int>();

                        WebSocketChatRoom newChatRoom = new WebSocketChatRoom(ChatName, OwnerID, new Dictionary<object, object>() { { "password", ChatPassword }, { "gang", GangID }, { "locked", PolarEnvironment.EnumToBool(Locked) } }, ChatAdmins, true);
                        newChatRoom.RefreshChatRoomData(); // Get chat bans & mutes

                        RunningChatRooms.TryAdd(newChatRoom.ChatName, newChatRoom);
                    }
                }
            }
            #endregion

            if (WebSocketChatManagerMainTimer == null)
            {
                object[] Params = null;
                WebSocketChatManagerMainTimer = new WebSocketChatManagerMainTimer("websocketchatmanager", 1000, true, Params);
            }

            //log.Info("Loaded " + RunningChatRooms.Count + " Chats.");
            Out.WriteLine("Cargado(s): " + RunningChatRooms.Count + " Chats.", "Polar.HabboRoleplay", ConsoleColor.DarkGray);
        }

        /// <summary>
        /// Stops all running chatrooms
        /// </summary>
        public static void StopAllChats()
        {
            try
            {
                int Removed = 0;
                int RemovedUsers = 0;
                lock (RunningChatRooms)
                {
                    foreach (WebSocketChatRoom Chat in RunningChatRooms.Values)
                    {
                        if (Chat == null)
                            continue;

                        if (Chat.FromDB)
                            Chat.SaveChatRoomData();

                        new Thread(() =>
                        {

                            Thread.Sleep(50);

                            Chat.Stop("This chat was stopped! Please join a new one!");
                            WebSocketChatRoom OoUT;
                            RunningChatRooms.TryRemove(Chat.ChatName, out OoUT);
                            Removed++;

                        }).Start();
                    }

                    SaveNewChats();
                }

                //log.Info("Removed " + Removed + " roleplay chat rooms, along with " + RemovedUsers + " Users to refresh.");
                Out.WriteLine("Se eliminaron " + Removed + " salas de chat de roleplay, junto con " + RemovedUsers + " usuarios para actualizar.", "Polar.HabboRoleplay", ConsoleColor.DarkGray);
            }
            catch (Exception e)
            {
                Logging.LogException(e.ToString());

            }
        }

        /// <summary>
        /// Save any chats that were created to the database
        /// </summary>
        public static void SaveNewChats()
        {
            foreach (WebSocketChatRoom ChatRoom in RunningChatRooms.Values)
            {
                if (ChatRoom == null)
                    continue;

                if (ChatRoom.FromDB == true)
                    continue;

                DataRow CheckRow = null;

                using (IQueryAdapter DB = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    DB.SetQuery("SELECT NULL FROM rp_chat_rooms WHERE name = '" + ChatRoom.ChatName + "'");
                    CheckRow = DB.getRow();
                    if (CheckRow == null)
                    {
                        DB.RunQuery("INSERT INTO rp_chat_rooms(owner_id, name, password, locked, gang_id, admins) VALUES('" + ChatRoom.ChatOwner + "', '" + ChatRoom.ChatName + "', '" + ChatRoom.ChatValues["password"] + "', '" + PolarEnvironment.BoolToEnum(Convert.ToBoolean(ChatRoom.ChatValues["locked"])) + "', '" + ChatRoom.ChatValues["gang"] + "', '" + String.Join(":", ChatRoom.ChatAdmins) + "')");
                        ChatRoom.FromDB = false;
                    }
                }
            }
        }

        /// <summary>
        /// Checks if the user is authenticated {insert into the chatuser concurrentdictionary}
        /// </summary>
        /// <param name="User">Target user</param>
        /// <param name="ChatName">Target chat</param>
        /// <returns>true or false</returns>
        public static bool AuthenticatedInChatRoom(GameClient User, string ChatName)
        {
            if (User == null)
                return false;

            if (User.GetRoleplay() == null)
                return false;

            if (User.GetRoleplay().WebSocketConnection == null)
            {
                User.SendWhisper("¡Debes estar conectado al websocket para poder unirte a los chats! ¡Comuníquese con un miembro del personal si este problema persiste!", 1);
                return false;
            }

            if (!RunningChatRooms.ContainsKey(ChatName.ToLower()))
            {
                User.SendWhisper("¡Este chat no existe!", 1);
                return false;
            }

            WebSocketChatRoom ChatRoom = RunningChatRooms[ChatName.ToLower()];

            if (!ChatRoom.ChatUsers.ContainsKey(User))
            {
                User.SendWhisper("No estás en este chat, acceso denegado.", 1);
                return false;
            }


            return true;

        }

        /// <summary>
        /// Retrieves a Chatroom's Instance that IS running by its name
        /// </summary>
        /// <param name="ChatName">Target chat's name</param>
        /// <returns>Returns the chats instance</returns>
        public static WebSocketChatRoom GetChatByName(string ChatName)
        {
            return ((RunningChatRooms.ContainsKey(ChatName.ToLower())) ? (RunningChatRooms[ChatName.ToLower()]) : (null));
        }

        /// <summary>
        /// Completely remove a user from a target chat name
        /// </summary>
        /// <param name="User">Target user to remove from </param>
        /// <param name="ChatName">Target chat's name</param>
        /// <param name="AlertMSG">Alert sent to user while user is removed</param>
        /// <param name="MSG">Whether to send an alert while removing</param>
        public static void Disconnect(GameClient User, string ChatName, bool AlertMSG = false, string? MSG = null)
        {
            if (User == null)
                return;

            if (RunningChatRooms == null)
                return;

            if (!RunningChatRooms.ContainsKey(ChatName.ToLower()))
                return;

            WebSocketChatRoom Chat = GetChatByName(ChatName);

            if (Chat == null)
                return;

            if (AlertMSG && MSG != null)
            {
                if (User.GetRoleplay() != null)
                    User.GetRoleplay().SendTopAlert(MSG);
            }

            if (!Chat.ChatUsers.ContainsKey(User))
                return;

            RunningChatRooms[ChatName.ToLower()].DecomposeChatDIV(User);

            if (Chat.ChatUsers.ContainsKey(User))
                Chat.OnUserLeft(User);

            WebSocketChatRoom outChat;

            if (!User.LoggingOut)
            {
                if (User.GetRoleplay() == null)
                    return;
                if (User.GetRoleplay().ChatRooms == null)
                    return;
                User.GetRoleplay().ChatRooms.TryRemove(ChatName.ToLower(), out outChat);
            }

            RunningChatRooms[ChatName.ToLower()].CheckDelete();
        }

        /// <summary>
        /// Alert a user with a alert(); {redundant}
        /// </summary>
        /// <param name="User">Target user</param>
        /// <param name="MSG">Message to send the user</param>
        public static void AlertUser(GameClient User, string MSG)
        {
            if (User == null)
                return;
            if (User.LoggingOut)
                return;
            if (User.GetRoleplay().WebSocketConnection == null)
                return;
            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(User, "event_sendjsalert", MSG);
        }

        public static void DeleteChat(WebSocketChatRoom ChatRoom)
        {

            ChatRoom.Stop("This chat was deleted by a staff member!");

            using (IQueryAdapter DB = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                DB.RunQuery("DELETE FROM rp_chat_rooms WHERE name = '" + ChatRoom.ChatName + "'");
                DB.RunQuery("DELETE FROM rp_chat_rooms_data WHERE chat_name = '" + ChatRoom.ChatName + "'");
                DB.RunQuery("DELETE FROM rp_chat_rooms_logs WHERE chat_name = '" + ChatRoom.ChatName + "'");
            }
        }
    }
}
