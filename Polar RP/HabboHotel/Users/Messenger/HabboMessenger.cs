using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Quests;

using Polar.HabboHotel.Groups;
using Polar.Communication.Packets.Outgoing.Messenger;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using Polar.Communication.Packets.Outgoing;
using Polar.Utilities;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Cache;
using Polar.HabboRoleplay.Bots;
using Polar.HabboRoleplay.Bots.Manager;
using Polar.HabboHotel.Rooms;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.Users.Effects;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;

namespace Polar.HabboHotel.Users.Messenger
{
    public class HabboMessenger
    {
        public bool AppearOffline;
        private readonly int _userId;

        public Dictionary<int, MessengerBuddy> _friends;
        private Dictionary<int, MessengerRequest> _requests;

        public HabboMessenger(int UserId)
        {
            this._userId = UserId;

            this._requests = new Dictionary<int, MessengerRequest>();
            this._friends = new Dictionary<int, MessengerBuddy>();
        }


        public void Init(Dictionary<int, MessengerBuddy> friends, Dictionary<int, MessengerRequest> requests)
        {
            this._requests = new Dictionary<int, MessengerRequest>(requests);
            this._friends = new Dictionary<int, MessengerBuddy>(friends);
        }

        public bool TryGetRequest(int senderID, out MessengerRequest Request)
        {
            return this._requests.TryGetValue(senderID, out Request);
        }

        public bool TryGetFriend(int UserId, out MessengerBuddy Buddy)
        {
            return this._friends.TryGetValue(UserId, out Buddy);
        }

        public void ProcessOfflineMessages()
        {
            DataTable GetMessages = null;
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT * FROM `messenger_offline_messages` WHERE `to_id` = @id;");
                dbClient.AddParameter("id", this._userId);
                GetMessages = dbClient.getTable();

                if (GetMessages != null)
                {
                    GameClient Client = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(this._userId);
                    if (Client == null)
                        return;

                    foreach (DataRow Row in GetMessages.Rows)
                    {
                        Client.SendMessage(new NewConsoleMessageComposer(Convert.ToInt32(Row["from_id"]), Convert.ToString(Row["message"]), Math.Abs(Convert.ToInt32(PolarEnvironment.GetUnixTimestamp()) - Convert.ToInt32(Row["timestamp"]))));
                    }

                    dbClient.SetQuery("DELETE FROM `messenger_offline_messages` WHERE `to_id` = @id");
                    dbClient.AddParameter("id", this._userId);
                    dbClient.RunQuery();
                }
            }
        }

        public void Destroy()
        {
            IEnumerable<GameClient> onlineUsers = PolarEnvironment.GetGame().GetClientManager().GetClientsById(_friends.Keys);

            foreach (GameClient client in onlineUsers)
            {
                if (client.GetHabbo() == null || client.GetHabbo().GetMessenger() == null)
                    continue;

                client.GetHabbo().GetMessenger().UpdateFriend(_userId, null, true);
            }
        }

        public void OnStatusChanged(bool notification)
        {
            if (GetClient() == null || GetClient().GetHabbo() == null || GetClient().GetHabbo().GetMessenger() == null)
                return;

            if (_friends == null)
                return;

            IEnumerable<GameClient> onlineUsers = PolarEnvironment.GetGame().GetClientManager().GetClientsById(_friends.Keys);
            if (onlineUsers.Count() == 0)
                return;

            foreach (GameClient client in onlineUsers.ToList())
            {
                try
                {
                    if (client == null || client.GetHabbo() == null || client.GetHabbo().GetMessenger() == null)
                        continue;

                    client.GetHabbo().GetMessenger().UpdateFriend(_userId, client, true);

                    if (this == null || client == null || client.GetHabbo() == null)
                        continue;

                    UpdateFriend(client.GetHabbo().Id, client, notification);
                }
                catch
                {
                    continue;
                }
            }
        }

        public void UpdateFriend(int userid, GameClient client, bool notification)
        {
            if (_friends.ContainsKey(userid))
            {
                _friends[userid].UpdateUser(client);

                if (notification)
                {
                    GameClient Userclient = GetClient();
                    if (Userclient != null)
                        Userclient.SendMessage(SerializeUpdate(_friends[userid]));
                }
            }
        }

        public void HandleAllRequests()
        {
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.RunQuery("DELETE FROM messenger_requests WHERE from_id = " + _userId + " OR to_id = " + _userId);
            }

            ClearRequests();
        }

        public void HandleRequest(int sender)
        {
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.RunQuery("DELETE FROM messenger_requests WHERE (from_id = " + _userId + " AND to_id = " + sender + ") OR (to_id = " + _userId + " AND from_id = " + sender + ")");
            }

            _requests.Remove(sender);
        }

        public void CreateFriendship(int friendID)
        {
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.RunQuery("REPLACE INTO messenger_friendships (user_one_id,user_two_id) VALUES (" + _userId + "," + friendID + ")");
            }

            OnNewFriendship(friendID);

            GameClient User = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(friendID);

            if (User != null && User.GetHabbo().GetMessenger() != null)
            {
                User.GetHabbo().GetMessenger().OnNewFriendship(_userId);
            }
        }

        public void DestroyFriendship(int friendID)
        {
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.RunQuery("DELETE FROM messenger_friendships WHERE (user_one_id = " + _userId + " AND user_two_id = " + friendID + ") OR (user_two_id = " + _userId + " AND user_one_id = " + friendID + ")");

            }

            OnDestroyFriendship(friendID);

            GameClient User = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(friendID);

            if (User != null && User.GetHabbo().GetMessenger() != null)
                User.GetHabbo().GetMessenger().OnDestroyFriendship(_userId);
        }

        public void OnNewFriendship(int friendID)
        {
            GameClient friend = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(friendID);

            MessengerBuddy newFriend;
            if (friend == null || friend.GetHabbo() == null)
            {
                DataRow dRow;
                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("SELECT id,username,motto,look,last_online,hide_inroom,hide_online FROM users WHERE `id` = @friendid LIMIT 1");
                    dbClient.AddParameter("friendid", friendID);
                    dRow = dbClient.getRow();
                }

                newFriend = new MessengerBuddy(friendID, Convert.ToString(dRow["username"]), Convert.ToString(dRow["look"]), Convert.ToString(dRow["motto"]), Convert.ToInt32(dRow["last_online"]),
                    PolarEnvironment.EnumToBool(dRow["hide_online"].ToString()), PolarEnvironment.EnumToBool(dRow["hide_inroom"].ToString()), false);
            }
            else
            {
                Habbo user = friend.GetHabbo();


                newFriend = new MessengerBuddy(friendID, user.Username, user.Look, user.Motto, 0, user.AppearOffline, user.AllowPublicRoomStatus, false);
                newFriend.UpdateUser(friend);
            }

            if (!_friends.ContainsKey(friendID))
                _friends.Add(friendID, newFriend);

            GetClient().SendMessage(SerializeUpdate(newFriend));
        }

        public bool RequestExists(int requestID)
        {
            if (_requests.ContainsKey(requestID))
                return true;

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT user_one_id FROM messenger_friendships WHERE user_one_id = @myID AND user_two_id = @friendID");
                dbClient.AddParameter("myID", Convert.ToInt32(_userId));
                dbClient.AddParameter("friendID", Convert.ToInt32(requestID));
                return dbClient.findsResult();
            }
        }

        public bool FriendshipExists(int friendID)
        {
            return _friends.ContainsKey(friendID);
        }

        public void OnDestroyFriendship(int Friend)
        {
            if (_friends.ContainsKey(Friend))
                _friends.Remove(Friend);

            #region Bot Friendship Remover
            if (GetClient() != null && GetClient().GetRoleplay() != null && GetClient().GetRoleplay().BotFriendShips != null)
            {
                int BotId = Friend - RoleplayBotManager.BotFriendMultiplyer;

                if (GetClient().GetRoleplay().FriendsWithBot(BotId))
                    GetClient().GetRoleplay().RemoveBotAsFriend(BotId);
            }
            #endregion

            GetClient().SendMessage(new FriendListUpdateComposer(Friend));
        }
        private string StripHtml(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Eliminar cualquier etiqueta HTML como <...>
            return System.Text.RegularExpressions.Regex.Replace(input, "<.*?>", string.Empty);
        }
        public bool RequestBuddy(string UserQuery)
        {
            UserQuery = StripHtml(UserQuery);

            //Console.WriteLine("[DEBUG 1] Inicio de RequestBuddy para usuario: " + UserQuery);

            int userID;
            bool hasFQDisabled;

            #region Bot Friendship
            //Console.WriteLine("[DEBUG 2] Verificando si UserQuery es un bot...");
            if (RoleplayBotManager.GetDeployedBotByName(UserQuery) != null)
            {
                //Console.WriteLine("[DEBUG 3] Es un bot. Obteniendo RoomUser...");
                RoomUser BotUser = RoleplayBotManager.GetDeployedBotByName(UserQuery);

                if (BotUser.GetBotRoleplay() == null)
                {
                    //Console.WriteLine("[DEBUG 4] BotRoleplay es null. Retornando false.");
                    return false;
                }

                if (GetClient().GetRoleplay().BotFriendShips.ContainsKey(BotUser.GetBotRoleplay().Id))
                {
                    //Console.WriteLine("[DEBUG 5] Ya eres amigo de este bot. Enviando whisper y retornando false.");
                    GetClient().SendWhisper("¡Ya eres amigo de este PNJ!", 1);
                    return false;
                }

                if (!BotUser.GetBotRoleplay().AddableBot)
                {
                    //Console.WriteLine("[DEBUG 6] Bot no es agregable. Enviando whisper y retornando false.");
                    GetClient().SendWhisper("¡No puedes agregar este bot!", 1);
                    return false;
                }

                //Console.WriteLine("[DEBUG 7] Agregando bot como amigo...");
                BotUser.Chat("Okay " + GetClient().GetHabbo().Username + ", Te he agregado a la lista de contactos de mi teléfono", true);
                GetClient().GetRoleplay().AddBotAsFriend(BotUser.GetBotRoleplay().Id);
                //Console.WriteLine("[DEBUG 8] Bot agregado exitosamente (sin paquete NewBuddyRequestComposer). Retornando true.");
                return true;
            }
            #endregion

            //Console.WriteLine("[DEBUG 9] No es un bot. Continuando con solicitud normal.");

            /*if (GetClient().GetRoleplay().PhoneType <= 0)
            {
                GetClient().SendWhisper("Usted no tiene un teléfono para hacer esto! ¡Compre uno en la tienda telefónica!", 1);
                return false;
            }*/

            GameClient client = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(UserQuery);
            //Console.WriteLine("[DEBUG 10] client (target online?) = " + (client != null ? "Online" : "Offline"));

            if (client == null)
            {
                //Console.WriteLine("[DEBUG 11] Target offline. Consultando DB...");
                DataRow Row = null;
                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("SELECT `id`,`block_newfriends` FROM `users` WHERE `username` = @query LIMIT 1");
                    dbClient.AddParameter("query", UserQuery.ToLower());
                    Row = dbClient.getRow();
                }

                if (Row == null)
                {
                    //Console.WriteLine("[DEBUG 12] No se encontró usuario en DB. Retornando false.");
                    return false;
                }

                userID = Convert.ToInt32(Row["id"]);
                hasFQDisabled = PolarEnvironment.EnumToBool(Row["block_newfriends"].ToString());
                //Console.WriteLine("[DEBUG 13] userID=" + userID + ", hasFQDisabled=" + hasFQDisabled);
            }
            else
            {
                userID = client.GetHabbo().Id;
                hasFQDisabled = client.GetHabbo().AllowFriendRequests;
                //Console.WriteLine("[DEBUG 14] Target online. userID=" + userID + ", AllowFriendRequests=" + hasFQDisabled);
            }

            if (hasFQDisabled)
            {
                //Console.WriteLine("[DEBUG 15] El target tiene bloqueadas las solicitudes. Enviando MessengerErrorComposer y retornando false.");
                GetClient().SendMessage(new MessengerErrorComposer(39, 3));
                return false;
            }

            int ToId = userID;
            if (RequestExists(ToId))
            {
                //Console.WriteLine("[DEBUG 16] Ya existe una solicitud pendiente para ToId=" + ToId + ". Retornando true.");
                return true;
            }

            //Console.WriteLine("[DEBUG 17] Insertando solicitud en messenger_requests...");
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.RunQuery("REPLACE INTO `messenger_requests` (`from_id`,`to_id`) VALUES ('" + _userId + "','" + ToId + "')");
            }
            //Console.WriteLine("[DEBUG 18] Solicitud insertada en DB.");

            GameClient ToUser = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(ToId);
            if (ToUser == null || ToUser.GetHabbo() == null)
            {
                //Console.WriteLine("[DEBUG 19] ToUser no está conectado o su Habbo es null. Solicitud guardada pero no se enviará paquete. Retornando true.");
                return true;
            }

            //Console.WriteLine("[DEBUG 20] ToUser está online. Creando MessengerRequest...");
            MessengerRequest Request = new MessengerRequest(ToId, _userId, PolarEnvironment.GetGame().GetClientManager().GetNameById(_userId));

            //Console.WriteLine("[DEBUG 21] Llamando a OnNewRequest en el messenger del target...");
            ToUser.GetHabbo().GetMessenger().OnNewRequest(_userId);

            //Console.WriteLine("[DEBUG 22] Generando UserCache para enviar NewBuddyRequestComposer...");
            using (UserCache ThisUser = PolarEnvironment.GetGame().GetCacheManager().GenerateUser(_userId))
            {
                if (ThisUser != null)
                {
                    //Console.WriteLine("[DEBUG 23] Enviando paquete NewBuddyRequestComposer al target " + ToUser.GetHabbo().Username);
                    ToUser.SendMessage(new NewBuddyRequestComposer(ThisUser));
                    //Console.WriteLine("[DEBUG 24] Paquete enviado correctamente.");
                }
                else
                {
                    //Console.WriteLine("[DEBUG 25] UserCache es null. No se pudo enviar el paquete.");
                }
            }

            _requests.Add(ToId, Request);
            //Console.WriteLine("[DEBUG 26] Solicitud añadida a la lista local. Retornando true.");
            return true;
        }

        public void OnNewRequest(int friendID)
        {
            if (!_requests.ContainsKey(friendID))
                _requests.Add(friendID, new MessengerRequest(_userId, friendID, PolarEnvironment.GetGame().GetClientManager().GetNameById(friendID)));
        }

        public void SendInstantMessageTwo(int ToId, string Message)
        {
            if (ToId == 0)
                return;

            if (GetClient() == null)
                return;

            if (GetClient().GetHabbo() == null)
                return;


            #region Custom Chats
            var Group = PolarEnvironment.GetGame().GetGroupManager().GetGroupsForUser(GetClient().GetHabbo().Id).Where(c => c.HasChat).ToList();
            foreach (var gp in Group)
            {
                if (ToId == int.MinValue + gp.Id) // int.MaxValue
                {
                    //PolarEnvironment.GetGame().GetClientManager().SendMessaget(new FuckingConsoleMessageComposer(ToId, Message, GetClient().GetHabbo().Username + "/" + GetClient().GetHabbo().Look + "/" + GetClient().GetHabbo().Id), GetClient().GetHabbo().Id);
                    PolarEnvironment.GetGame().GetClientManager().GroupChatAlert(new FuckingConsoleMessageComposer(int.MinValue + gp.Id, Message, GetClient().GetHabbo().Username + "/" + GetClient().GetHabbo().Look + "/" + GetClient().GetHabbo().Id), gp, GetClient().GetHabbo().Id);
                    return;
                }

            }
            if (GetClient().GetHabbo().GetPermissions().HasRight("staff_chat") && ToId == int.MinValue) // int.MaxValue
            {
                PolarEnvironment.GetGame().GetClientManager().StaffAlert(new FuckingConsoleMessageComposer(ToId, Message, GetClient().GetHabbo().Username + "/" + GetClient().GetHabbo().Look + "/" + GetClient().GetHabbo().Id), GetClient().GetHabbo().Id);
                return;
            }
            else if (GetClient().GetHabbo().GetPermissions().HasRight("guias_chat") && ToId == (int.MinValue + 1))
            {
                PolarEnvironment.GetGame().GetClientManager().StaffAlert(new FuckingConsoleMessageComposer(ToId, Message, GetClient().GetHabbo().Username + "/" + GetClient().GetHabbo().Look + "/" + GetClient().GetHabbo().Id), GetClient().GetHabbo().Id);
                return;
            }
            #endregion


            if (GetClient().GetHabbo().MessengerSpamCount >= 12)
            {
                GetClient().GetHabbo().MessengerSpamTime = PolarEnvironment.GetUnixTimestamp() + 60;
                GetClient().GetHabbo().MessengerSpamCount = 0;
                GetClient().SendNotification("No puedes enviar un mensaje, has inundado la consola.\n\nPuede enviar un mensaje en 60 segundos.");
                return;
            }
            else if (GetClient().GetHabbo().MessengerSpamTime > PolarEnvironment.GetUnixTimestamp())
            {
                double Time = GetClient().GetHabbo().MessengerSpamTime - PolarEnvironment.GetUnixTimestamp();
                GetClient().SendNotification("No puedes enviar un mensaje, has inundado la consola.\n\nPuede enviar un mensaje en " + Convert.ToInt32(Time) + " segundos.");
                return;
            }

            GetClient().GetHabbo().MessengerSpamCount++;

            GameClient Client = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(ToId);
            if (Client == null || Client.GetHabbo() == null || Client.GetHabbo().GetMessenger() == null)
            {
                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("INSERT INTO `messenger_offline_messages` (`to_id`, `from_id`, `message`, `timestamp`) VALUES (@tid, @fid, @msg, UNIX_TIMESTAMP())");
                    dbClient.AddParameter("tid", ToId);
                    dbClient.AddParameter("fid", GetClient().GetHabbo().Id);
                    dbClient.AddParameter("msg", Message);
                    dbClient.RunQuery();
                }
                return;
              
            }

            if (Client != null && Client.LoggingOut)
            {
                GetClient().SendMessage(new NewConsoleMessageComposer(ToId, "¡No puedo recibir tu mensaje ahora que mi teléfono ha muerto!"));
                return;
            }

            if (Client == null)
            {
                GetClient().SendMessage(new InstantMessageErrorComposer(MessengerMessageErrors.FRIEND_BUSY, ToId));
                return;
            }

            if (Client.GetHabbo() == null)
            {
                GetClient().SendMessage(new InstantMessageErrorComposer(MessengerMessageErrors.FRIEND_BUSY, ToId));
                return;
            }

            if (!Client.GetHabbo().AllowConsoleMessages || Client.GetHabbo().MutedUsers.Contains(GetClient().GetHabbo().Id))
            {
                GetClient().SendMessage(new InstantMessageErrorComposer(MessengerMessageErrors.FRIEND_BUSY, ToId));
                return;
            }

            if (GetClient().GetHabbo().TimeMuted > 0)
            {
                GetClient().SendMessage(new InstantMessageErrorComposer(MessengerMessageErrors.YOUR_MUTED, ToId));
                return;
            }

            if (Client.GetHabbo().TimeMuted > 0)
            {
                GetClient().SendMessage(new InstantMessageErrorComposer(MessengerMessageErrors.FRIEND_MUTED, ToId));
            }

            if (String.IsNullOrEmpty(Message))
                return;

            Client.SendMessage(new NewConsoleMessageComposer(_userId, Message));
        }

        public void SendInstantMessage(int ToId, string Message)
        {
            if (ToId == 0)
                return;

            if (GetClient() == null)
                return;

            if (GetClient().GetHabbo() == null)
                return;

            /*#region Bot Friendship Messager
            if (ToId > 1000000)
            {
                if (GetClient().GetRoleplay() != null)
                {
                    if (GetClient().GetRoleplay().FriendsWithBot(ToId))
                    {
                        if (CanAfford)
                            GetClient().GetRoleplay().MessageBot(ToId, Message);
                        else
                            GetClient().SendMessage(new NewConsoleMessageComposer(ToId, "No puedo recibir su mensaje ahora mismo, usted no tiene saldo de teléfono suficiente para enviarme un texto!"));
                    }
                }
                return;
            }
            #endregion*/

            #region Custom Chats
            var Group = PolarEnvironment.GetGame().GetGroupManager().GetGroupsForUser(GetClient().GetHabbo().Id).Where(c => c.HasChat).ToList();
            foreach (var gp in Group)
            {
                if (ToId == int.MinValue + gp.Id) // int.MaxValue
                {
                    //PolarEnvironment.GetGame().GetClientManager().SendMessaget(new FuckingConsoleMessageComposer(ToId, Message, GetClient().GetHabbo().Username + "/" + GetClient().GetHabbo().Look + "/" + GetClient().GetHabbo().Id), GetClient().GetHabbo().Id);
                    PolarEnvironment.GetGame().GetClientManager().GroupChatAlert(new FuckingConsoleMessageComposer(int.MinValue + gp.Id, Message, GetClient().GetHabbo().Username + "/" + GetClient().GetHabbo().Look + "/" + GetClient().GetHabbo().Id), gp, GetClient().GetHabbo().Id);
                    return;
                }

            }
            if (GetClient().GetHabbo().GetPermissions().HasRight("staff_chat") && ToId == int.MinValue) // int.MaxValue
            {
                PolarEnvironment.GetGame().GetClientManager().StaffAlert(new FuckingConsoleMessageComposer(ToId, Message, GetClient().GetHabbo().Username + "/" + GetClient().GetHabbo().Look + "/" + GetClient().GetHabbo().Id), GetClient().GetHabbo().Id);
                return;
            }
            else if (GetClient().GetHabbo().GetPermissions().HasRight("guias_chat") && ToId == (int.MinValue + 1))
            {
                PolarEnvironment.GetGame().GetClientManager().StaffAlert(new FuckingConsoleMessageComposer(ToId, Message, GetClient().GetHabbo().Username + "/" + GetClient().GetHabbo().Look + "/" + GetClient().GetHabbo().Id), GetClient().GetHabbo().Id);
                return;
            }
            #endregion


            if (!FriendshipExists(ToId))
            {
                GetClient().SendMessage(new InstantMessageErrorComposer(MessengerMessageErrors.YOUR_NOT_FRIENDS, ToId));
                return;
            }

            if (GetClient().GetHabbo().MessengerSpamCount >= 12)
            {
                GetClient().GetHabbo().MessengerSpamTime = PolarEnvironment.GetUnixTimestamp() + 60;
                GetClient().GetHabbo().MessengerSpamCount = 0;
                GetClient().SendNotification("No puedes enviar un mensaje, has inundado la consola.\n\nPuede enviar un mensaje en 60 segundos.");
                return;
            }
            else if (GetClient().GetHabbo().MessengerSpamTime > PolarEnvironment.GetUnixTimestamp())
            {
                double Time = GetClient().GetHabbo().MessengerSpamTime - PolarEnvironment.GetUnixTimestamp();
                GetClient().SendNotification("No puedes enviar un mensaje, has inundado la consola.\n\nPuede enviar un mensaje en " + Convert.ToInt32(Time) + " segundos.");
                return;
            }

            GetClient().GetHabbo().MessengerSpamCount++;

            GameClient Client = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(ToId);
            if (Client == null || Client.GetHabbo() == null || Client.GetHabbo().GetMessenger() == null)
            {
                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("INSERT INTO `messenger_offline_messages` (`to_id`, `from_id`, `message`, `timestamp`) VALUES (@tid, @fid, @msg, UNIX_TIMESTAMP())");
                    dbClient.AddParameter("tid", ToId);
                    dbClient.AddParameter("fid", GetClient().GetHabbo().Id);
                    dbClient.AddParameter("msg", Message);
                    dbClient.RunQuery();
                }
                return;
                /*if (CanAfford)
                {
                    string UserName = "Username";
                    using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                    {
                        dbClient.SetQuery("SELECT `username` FROM `users` WHERE `id` = @id LIMIT 1");
                        dbClient.AddParameter("id", ToId);
                        UserName = dbClient.getString();
                    }

                    GetClient().SendMessage(new RoomNotificationComposer("text_message", "message", "Ha enviado un mensaje de texto sin conexión a " + UserName + "!"));
                    if (GetClient().GetHabbo().Translating)
                    {
                        string LG1 = GetClient().GetHabbo().FromLanguage.ToLower();
                        string LG2 = GetClient().GetHabbo().ToLanguage.ToLower();

                        NotifyStaffMembers(GetClient().GetHabbo(), UserName, PolarEnvironment.TranslateText(Message, LG1 + "|" + LG2) + " [" + LG1.ToUpper() + " -> " + LG2.ToUpper() + "]", false);
                    }
                    else
                        NotifyStaffMembers(GetClient().GetHabbo(), UserName, Message, false);

                    using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                    {
                        dbClient.SetQuery("INSERT INTO `messenger_offline_messages` (`to_id`, `from_id`, `message`, `timestamp`) VALUES (@tid, @fid, @msg, UNIX_TIMESTAMP())");
                        dbClient.AddParameter("tid", ToId);
                        dbClient.AddParameter("fid", GetClient().GetHabbo().Id);
                        dbClient.AddParameter("msg", Message);
                        dbClient.RunQuery();
                    }

                    LogPM(_userId, ToId, Message);

                    return;
                }
                else
                    GetClient().SendMessage(new NewConsoleMessageComposer(ToId, "No puedo recibir su mensaje ahora mismo, usted no tiene crédito de teléfono suficiente para enviarme un texto"));
            */
            }

            if (Client != null && Client.LoggingOut)
            {
                GetClient().SendMessage(new NewConsoleMessageComposer(ToId, "¡No puedo recibir tu mensaje ahora que mi teléfono ha muerto!"));
                return;
            }

            if (Client == null)
            {
                GetClient().SendMessage(new InstantMessageErrorComposer(MessengerMessageErrors.FRIEND_BUSY, ToId));
                return;
            }

            if (Client.GetHabbo() == null)
            {
                GetClient().SendMessage(new InstantMessageErrorComposer(MessengerMessageErrors.FRIEND_BUSY, ToId));
                return;
            }

            if (!Client.GetHabbo().AllowConsoleMessages || Client.GetHabbo().MutedUsers.Contains(GetClient().GetHabbo().Id))
            {
                GetClient().SendMessage(new InstantMessageErrorComposer(MessengerMessageErrors.FRIEND_BUSY, ToId));
                return;
            }

            if (GetClient().GetHabbo().TimeMuted > 0)
            {
                GetClient().SendMessage(new InstantMessageErrorComposer(MessengerMessageErrors.YOUR_MUTED, ToId));
                return;
            }

            if (Client.GetHabbo().TimeMuted > 0)
            {
                GetClient().SendMessage(new InstantMessageErrorComposer(MessengerMessageErrors.FRIEND_MUTED, ToId));
            }

            if (String.IsNullOrEmpty(Message))
                return;

            Client.SendMessage(new NewConsoleMessageComposer(_userId, Message));
        }

        public void NotifyStaffMembers(Habbo Sender, string Username, string Message, bool CheckReceiver = true)
        {
            if (Sender.CurrentRoom != null)
            {
                List<RoomUser> ToNotify = Sender.CurrentRoom.GetRoomUserManager().GetRoomUserByRank(7);
                if (ToNotify.Count > 0)
                {
                    foreach (RoomUser user in ToNotify)
                    {
                        if (user != null && user.GetUsername() != Username && user.HabboId != Sender.Id)
                        {
                            if (user.GetClient() != null && user.GetClient().GetHabbo() != null && !user.GetClient().GetHabbo().IgnorePublicWhispers)
                            {
                                if (Sender != null && Sender.GetClient() != null && Sender.GetClient().GetRoomUser() != null)
                                    user.GetClient().SendMessage(new WhisperComposer(Sender.GetClient().GetRoomUser().VirtualId, "[Mensaje a " + Username + "] " + Message, 0, Sender.GetClient().GetRoomUser().LastBubble));
                            }
                        }
                    }
                }
            }

            if (CheckReceiver)
            {
                var Client = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Username);

                if (Client == null || Client.GetRoomUser() == null || Client.GetHabbo() == null)
                    return;

                if (Client.GetHabbo().CurrentRoom == null)
                    return;

                if (Client.GetHabbo().CurrentRoomId != Sender.CurrentRoomId)
                {
                    List<RoomUser> ToNotify2 = Client.GetHabbo().CurrentRoom.GetRoomUserManager().GetRoomUserByRank(5);
                    if (ToNotify2.Count > 0)
                    {
                        foreach (RoomUser user in ToNotify2)
                        {
                            if (user != null && user.GetUsername() != Username && user.HabboId != Sender.Id)
                            {
                                if (user.GetClient() != null && user.GetClient().GetHabbo() != null && !user.GetClient().GetHabbo().IgnorePublicWhispers)
                                {
                                    if (Client != null && Client.GetRoomUser() != null)
                                        user.GetClient().SendMessage(new WhisperComposer(Client.GetRoomUser().VirtualId, "[Mensaje de " + Sender.Username + "] " + Message, 0, Client.GetRoomUser().LastBubble));
                                }
                            }
                        }
                    }
                }
            }
        }

        public void LogPM(int From_Id, int ToId, string Message)
        {
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("INSERT INTO chatlogs_console VALUES (NULL, " + From_Id + ", " + ToId + ", @message, @timestamp)");
                dbClient.AddParameter("message", Message);
                dbClient.AddParameter("timestamp", PolarEnvironment.GetUnixTimestamp());
                dbClient.RunQuery();
            }
        }

        public ServerPacket SerializeUpdate(MessengerBuddy friend)
        {
            return new FriendListUpdateComposer(GetClient(), friend);

        }

        public void BroadcastAchievement(int UserId, MessengerEventTypes Type, string Data)
        {
            IEnumerable<GameClient> MyFriends = PolarEnvironment.GetGame().GetClientManager().GetClientsById(this._friends.Keys);

            foreach (GameClient Client in MyFriends.ToList())
            {
                if (Client.GetHabbo() != null && Client.GetHabbo().GetMessenger() != null)
                {
                    Client.SendMessage(new FriendNotificationComposer(UserId, Type, Data));
                    Client.GetHabbo().GetMessenger().OnStatusChanged(true);
                }
            }
        }

        public void ClearRequests()
        {
            this._requests.Clear();
        }

        private GameClient GetClient()
        {
            return PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(this._userId);
        }

        public ICollection<MessengerRequest> GetRequests()
        {
            return this._requests.Values;
        }

        public ICollection<MessengerBuddy> GetFriends()
        {
            return this._friends.Values;
        }
    }
}