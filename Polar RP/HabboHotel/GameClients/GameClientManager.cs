using System;
using System.Collections.Generic;
using System.Text;
using ConnectionManager;

using Polar.Core;
using Polar.HabboHotel.Users.Messenger;

using Polar.HabboRoleplay.Bots.Manager;
using System.Linq;
using System.Collections.Concurrent;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Items;
using log4net;
using System.Data;
using Polar.Communication.Packets.Outgoing.Notifications;
using Polar.Database.Interfaces;
using System.Collections;
using Polar.Communication.Packets.Outgoing.Handshake;
using System.Diagnostics;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net.NetworkInformation;

namespace Polar.HabboHotel.GameClients
{
    public class GameClientManager
    {
        private static readonly ILog log = LogManager.GetLogger("Polar.HabboHotel.GameClients.GameClientManager");

        private ConcurrentDictionary<int, GameClient> _clients;
        private ConcurrentDictionary<int, GameClient> _userIDRegister;
        private ConcurrentDictionary<string, GameClient> _usernameRegister;
        private ConcurrentDictionary<string, GameClient> _usernameRegisterPhone;
        public Dictionary<string, int> RobberyUsers = new Dictionary<string, int>();

        private readonly System.Collections.Concurrent.ConcurrentQueue<GameClient> timedOutConnections;

        private readonly Stopwatch clientPingStopwatch;

        public GameClientManager()
        {
            this._clients = new ConcurrentDictionary<int, GameClient>();
            this._userIDRegister = new ConcurrentDictionary<int, GameClient>();
            this._usernameRegister = new ConcurrentDictionary<string, GameClient>();
            this._usernameRegisterPhone = new ConcurrentDictionary<string, GameClient>();

            timedOutConnections = new System.Collections.Concurrent.ConcurrentQueue<GameClient>();

            clientPingStopwatch = new Stopwatch();
            clientPingStopwatch.Start();
        }

        public void OnCycle()
        {
            TestClientConnections();
            HandleTimeouts();
            //PolarEnvironment.GetGame().ClientManagerCycleEnded = true;
        }

        public GameClient GetClientByUserID(int userID)
        {
            // FIX: TryGetValue — una sola búsqueda en lugar de ContainsKey + indexer
            _userIDRegister.TryGetValue(userID, out GameClient client);
            return client;
        }

        public GameClient GetClientByUsername(string username)
        {
            _usernameRegister.TryGetValue(username.ToLower(), out GameClient client);
            return client;
        }

        public GameClient GetClientByPhoneNumber(string number)
        {
            _usernameRegisterPhone.TryGetValue(number.ToLower(), out GameClient client);
            return client;
        }

        public bool TryGetClient(int ClientId, out GameClient Client)
        {
            return this._clients.TryGetValue(ClientId, out Client);
        }

        public bool UpdateClientUsername(GameClient Client, string OldUsername, string NewUsername)
        {
            if (Client == null || !_usernameRegister.ContainsKey(OldUsername.ToLower()))
                return false;

            _usernameRegister.TryRemove(OldUsername.ToLower(), out Client);
            _usernameRegister.TryAdd(NewUsername.ToLower(), Client);
            return true;
        }

        public int GetIdByName(string username)
        {
            GameClient client = GetClientByUsername(username);

            if (client != null && client.GetHabbo() != null)
                return client.GetHabbo().Id;

            int id = 0;
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT id FROM users WHERE username = @username LIMIT 1");
                dbClient.AddParameter("username", username);
                id = dbClient.getInteger();
            }

            return id; // FIX: era "id > 0 ? id : 0" — si id <= 0 ya vale 0, el ternario era redundante
        }
        public string GetNameById(int Id)
        {
            GameClient client = GetClientByUserID(Id);

            if (client != null)
                return client.GetHabbo().Username;

            string username;
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT `username` FROM `users` WHERE `id` = @id LIMIT 1");
                dbClient.AddParameter("id", Id);
                username = dbClient.getString();
            }

            return username;
        }

        public IEnumerable<GameClient> GetClientsById(Dictionary<int, MessengerBuddy>.KeyCollection users)
        {
            foreach (int id in users)
            {
                GameClient client = GetClientByUserID(id);
                if (client != null)
                    yield return client;
            }
        }

        public void StaffWhisperAlert(string Message, GameClient Session)
        {
            // FIX: ConcurrentDictionary.Values es snapshot seguro — ToList() crea copia innecesaria
            foreach (GameClient client in this._clients.Values)
            {
                if (client == null || client.GetHabbo() == null)
                    continue;

                if (!client.GetHabbo().GetPermissions().HasRight("mod_tool"))
                    continue;

                client.SendWhisper("[STAFF Alert] [" + Session.GetHabbo().Username + "] " + Message, 23);
            }
        }

        public void AmbassadorWhisperAlert(string Message, GameClient Session)
        {
            foreach (GameClient client in this.GetClients.ToList())
            {
                if (client == null || client.GetHabbo() == null)
                    continue;

                if (!client.GetHabbo().GetPermissions().HasRight("ambassador"))
                    continue;

                client.SendWhisper("[AMBASSADOR Alert] [" + Session.GetHabbo().Username + "] " + Message, 37);
            }
        }

        public void VIPWhisperAlert(string Message, GameClient Session)
        {
            if (!Session.GetHabbo().GetPermissions().HasRight("advertisement_filter_override"))
            {
                string Phrase = "";
                if (PolarEnvironment.GetGame().GetChatManager().GetFilter().IsUnnaceptableWord(Message, out Phrase))
                {
                    Session.GetHabbo().AdvertisingStrikes++;

                    if (Session.GetHabbo().AdvertisingStrikes < 2)
                    {
                        Session.SendMessage(new RoomNotificationComposer("ADVERTENCIA", "Abstengase de anunciar otros sitios web que no estén respaldados, afiliados o ofrecidos por "+ PolarEnvironment.GetConfig().data["hotel.name"]+". ¡Te silenciarás si lo haces de nuevo!<br><br>Blacklisted palabra prohibida: '" + Phrase + "'", "frank10", "OK", "event:"));
                        return;
                    }

                    if (Session.GetHabbo().AdvertisingStrikes >= 2)
                    {
                        Session.GetHabbo().TimeMuted = 3600;

                        using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                        {
                            // FIX: parámetro en lugar de concatenación
                            dbClient.SetQuery("UPDATE `users` SET `time_muted` = '3600' WHERE `id` = @uid LIMIT 1");
                            dbClient.AddParameter("uid", Session.GetHabbo().Id);
                            dbClient.RunQuery();
                        }

                        Session.SendMessage(new RoomNotificationComposer("¡Has sido silenciado!", "Lamentablemente por no prestar atención a la advertencia anterior, usted ha sido MUTEADO por hacer publicidad de otra comunidad. '" + Phrase + "'.<br><br>The moderation team has been notified and action will be taken within your account!", "frank10", "ok", "event:"));

                        List<string> Messages = new List<string>();
                        Messages.Add(Message);
                        PolarEnvironment.GetGame().GetModerationTool().SendNewTicket(Session, 9, Session.GetHabbo().Id, "[Server] Ciudadano ha sido atrapado en haciendo publicidad " + Phrase + ".", Messages);
                        return;
                    }

                    return;
                }
            }

            foreach (GameClient client in this.GetClients.ToList())
            {
                if (client == null || client.GetHabbo() == null)
                    continue;

                if (client.GetHabbo().VIPRank == 0)
                    continue;

                if (client.GetRoleplay().DisableVIPA == true)
                    continue;

                client.SendWhisper("[VIP Alert] [" + Session.GetHabbo().Username + "] " + Message, 11);
            }
        }

        public void RadioAlert(string Message, GameClient Session)
        {
            if (!Session.GetHabbo().GetPermissions().HasRight("advertisement_filter_override"))
            {
                string Phrase = "";
                if (PolarEnvironment.GetGame().GetChatManager().GetFilter().IsUnnaceptableWord(Message, out Phrase))
                {
                    Session.GetHabbo().AdvertisingStrikes++;

                    if (Session.GetHabbo().AdvertisingStrikes < 2)
                    {
                        Session.SendMessage(new RoomNotificationComposer("Warning!", "Abstenerse de anunciar otros sitios web que no estén respaldados, afiliados o ofrecidos por " + PolarEnvironment.GetConfig().data["hotel.name"] + ". ¡Se silenciará si lo haces de nuevo! <br> <br> Blacklisted frase: '" + Phrase + "'", "frank10", "ok", "event:"));
                        return;
                    }

                    if (Session.GetHabbo().AdvertisingStrikes >= 2)
                    {
                        Session.GetHabbo().TimeMuted = 3600;

                        using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                        {
                            // FIX: parámetro en lugar de concatenación
                            dbClient.SetQuery("UPDATE `users` SET `time_muted` = '3600' WHERE `id` = @uid LIMIT 1");
                            dbClient.AddParameter("uid", Session.GetHabbo().Id);
                            dbClient.RunQuery();
                        }

                        Session.SendMessage(new RoomNotificationComposer("You've been muted!", "Lo sentimos, pero se ha silenciado automáticamente por anunciar el retro '" + Phrase + "'.<br><br>The moderation team has been notified and action will be taken within your account!", "frank10", "ok", "event:"));

                        List<string> Messages = new List<string>();
                        Messages.Add(Message);
                        PolarEnvironment.GetGame().GetModerationTool().SendNewTicket(Session, 9, Session.GetHabbo().Id, "[Server] Civil ha sido atrapado en publicidad " + Phrase + ".", Messages);
                        return;
                    }

                    return;
                }
            }


            foreach (GameClient client in this.GetClients.ToList())
            {
                if (client == null || client.GetHabbo() == null)
                    continue;

                if (!Groups.GroupManager.HasJobCommand(client, "radio") && !client.GetHabbo().GetPermissions().HasRight("corporation_rights"))
                    continue;

                if (client.GetRoleplay().DisableRadio == true)
                    continue;

                client.SendWhisper("[RADIO Alert] [" + Session.GetHabbo().Username + "] " + Message, 30);
            }
        }

        public void sendWorkAlert(string Message, string work, bool uniform = false, int RID = 0)
        {
            foreach (GameClient client in this.GetClients.ToList())
            {
                if (client == null || client.GetHabbo() == null)
                    continue;

                if (uniform)
                {
                    if (!client.GetRoleplay().IsWorking)
                        continue;
                }

                if (!GroupManager.HasJobCommand(client, work))
                    continue;

                if (client.GetRoleplay().DisableRadio == true)
                    continue;

                client.SendWhisper("[SERVICIO] " + Message, 1);
            }
        }
        public void JailAlert(string Message)
        {
            foreach (GameClient client in this.GetClients.ToList())
            {
                if (client == null || client.GetHabbo() == null)
                    continue;

                if (!Groups.GroupManager.HasJobCommand(client, "radio") && !client.GetHabbo().GetPermissions().HasRight("corporation_rights"))
                    continue;

                if (client.GetRoleplay().DisableRadio)
                    continue;

                client.SendWhisper(Message, 30);
                //client.SendMessage(new RoomNotificationComposer("police_announcement", "message", Message.Replace("[RADIO Alert] ", "")));
            }
        }

        public void sendGangMsg(int Gang, string Msg)
        {
            foreach (GameClient Client in this.GetClients.ToList())
            {
                if (Client == null || Client.GetConnection() == null || Client.GetHabbo() == null)
                    continue;

                if (Client.GetRoleplay().GangId != Gang)
                    continue;

                Client.SendWhisper("[BANDA] " + Msg);
            }
        }
        public void EmergenciaAlert(string Message)
        {
            foreach (GameClient client in this.GetClients.ToList())
            {
                if (client == null || client.GetHabbo() == null)
                    continue;

                if (!Groups.GroupManager.HasJobCommand(client, "emergencia") && !client.GetHabbo().GetPermissions().HasRight("corporation_rights"))
                    continue;

                if (client.GetRoleplay().DisableRadio)
                    continue;

                client.SendWhisper(Message, 34);
            }
        }

        public void StaffAlert(ServerPacket Message, int Exclude = 0)
        {
            foreach (GameClient client in this.GetClients.ToList())
            {
                if (client == null || client.GetHabbo() == null)
                    continue;

                if (client.GetHabbo().Rank < 2 || client.GetHabbo().Id == Exclude)
                    continue;

                client.SendMessage(Message);
            }
        }

        public void StaffAlertMsg(string Message, int Exclude = 0)
        {
            foreach (GameClient client in this.GetClients.ToList())
            {
                if (client == null || client.GetHabbo() == null)
                    continue;

                if (client.GetHabbo().Rank < 3 || client.GetHabbo().Id == Exclude)
                    continue;

                client.SendWhisper(Message, 1);
            }
        }

        public void QuizzAlert(ServerPacket Message, Item Item, Room room, int Exclude = 0)
        {
            foreach (RoomUser RoomUser in room.GetRoomUserManager().GetRoomUsers())
            {
                if (RoomUser == null || RoomUser.GetClient().GetHabbo() == null)
                    continue;

                RoomUser Human = room.GetRoomUserManager().GetRoomUserByHabbo(RoomUser.GetClient().GetHabbo().Id);

                // FIX: || en lugar de && — antes incluía usuarios en la misma fila O columna que el item
                if ((Human.X != Item.GetX || Human.Y != Item.GetY) || RoomUser.GetClient().GetHabbo().Id == Exclude)
                    continue;

                RoomUser.GetClient().SendMessage(Message);
            }
        }

        public int GetLevelById(int Id)
        {
            GameClient client = GetClientByUserID(Id);

            if (client != null)
                return client.GetRoleplay().Level;

            int level;
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT level FROM rp_stats WHERE id = @id LIMIT 1");
                dbClient.AddParameter("id", Id);
                level = dbClient.getInteger();
            }

            return level;
        }

        public string GetLookById(int Id)
        {
            GameClient client = GetClientByUserID(Id);

            if (client != null && client.GetHabbo() != null)
                return client.GetHabbo().Look;

            string look;
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT look FROM users WHERE id = @id LIMIT 1");
                dbClient.AddParameter("id", Id);
                look = dbClient.getString();
            }

            return look;
        }

        public string GetNumberById(int Id)
        {
            GameClient client = GetClientByUserID(Id);

            if (client != null)
                return client.GetRoleplay().PhoneNumber;

            string number;
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT phone_number FROM rp_phones_owned WHERE user_id = @id LIMIT 1");
                dbClient.AddParameter("id", Id);
                number = dbClient.getString();
            }

            return number;
        }

        public int GetVipById(int Id)
        {
            GameClient client = GetClientByUserID(Id);

            if (client != null)
                return client.GetHabbo().VIPRank;

            int viptype;
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT rank_vip FROM users WHERE id = @id LIMIT 1");
                dbClient.AddParameter("id", Id);
                viptype = dbClient.getInteger();
            }

            return viptype;
        }

        public void ManagerAlert(ServerPacket Message, int Exclude = 0)
        {
            foreach (GameClient client in this.GetClients.ToList())
            {
                if (client == null || client.GetHabbo() == null)
                    continue;

                if (client.GetHabbo().Rank < 9 || client.GetHabbo().Id == Exclude)
                    continue;

                client.SendMessage(Message);
            }
        }

        public void GroupChatAlert(ServerPacket Message, HabboHotel.Groups.Group Group, int Exclude = 0)
        {
            foreach (GameClient client in this.GetClients.ToList())
            {
                if (client == null || client.GetHabbo() == null)
                    continue;

                if (!Group.IsMember(client.GetHabbo().Id) || client.GetHabbo().Id == Exclude)
                    continue;

                client.SendMessage(Message);
            }
        }

        public void LogsNotif(string Message, string Key)
        {
            PolarEnvironment.GetGame().GetClientManager().StaffAlert(new RoomNotificationComposer(Message, Key), 0);
        }

        public void SendBubble(string Message, string Key)
        {
            PolarEnvironment.GetGame().GetClientManager().SendMessage(new RoomNotificationComposer(Message, Key));
        }

        public void ModAlert(string Message)
        {
            foreach (GameClient client in this.GetClients.ToList())
            {
                if (client == null || client.GetHabbo() == null)
                    continue;

                if (client.GetHabbo().GetPermissions().HasRight("mod_tool") && !client.GetHabbo().GetPermissions().HasRight("staff_ignore_mod_alert"))
                {
                    try { client.SendWhisper(Message, 23); }
                    catch { }
                }
            }
        }

        public void DoAdvertisingReport(GameClient Reporter, GameClient Target)
        {
            if (Reporter == null || Target == null || Reporter.GetHabbo() == null || Target.GetHabbo() == null)
                return;

            StringBuilder Builder = new StringBuilder();
            Builder.Append("New report submitted!\r\r");
            Builder.Append("Reporter: " + Reporter.GetHabbo().Username + "\r");
            Builder.Append("Reported User: " + Target.GetHabbo().Username + "\r\r");
            Builder.Append(Target.GetHabbo().Username + "s last 10 messages:\r\r");

            DataTable GetLogs = null;
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                // FIX: parámetro en lugar de concatenación directa
                dbClient.SetQuery("SELECT `message` FROM `chatlogs` WHERE `user_id` = @uid ORDER BY `id` DESC LIMIT 10");
                dbClient.AddParameter("uid", Target.GetHabbo().Id);
                GetLogs = dbClient.getTable();

                if (GetLogs != null)
                {
                    int Number = 11;
                    foreach (DataRow Log in GetLogs.Rows)
                    {
                        Number -= 1;
                        Builder.Append(Number + ": " + Convert.ToString(Log["message"]) + "\r");
                    }
                }
            }

            foreach (GameClient Client in this.GetClients.ToList())
            {
                if (Client == null || Client.GetHabbo() == null)
                    continue;

                if (Client.GetHabbo().GetPermissions().HasRight("mod_tool") && !Client.GetHabbo().GetPermissions().HasRight("staff_ignore_advertisement_reports"))
                    Client.SendMessage(new MOTDNotificationComposer(Builder.ToString()));
            }
        }

        public void SendMessage(ServerPacket Packet, string fuse = "")
        {
            foreach (GameClient Client in this._clients.Values.ToList())
            {
                if (Client == null || Client.GetHabbo() == null)
                    continue;

                if (!string.IsNullOrEmpty(fuse))
                {
                    if (!Client.GetHabbo().GetPermissions().HasRight(fuse))
                        continue;
                }

                Client.SendMessage(Packet);
            }
        }

        public void CreateAndStartClient(int clientID, ConnectionInformation connection)
        {
            GameClient Client = new GameClient(clientID, connection);
            if (this._clients.TryAdd(Client.ConnectionID, Client))
                Client.StartConnection();
            else
                connection.Dispose();
        }

        public void DisposeConnection(int clientID)
        {
            GameClient Client = null;
            if (!TryGetClient(clientID, out Client))
                return;

            if (Client != null)
            {
                //Client.Disconnect(false);
                Client.Dispose(clientID);
            }

            this._clients.TryRemove(clientID, out Client);
        }

        public void removeConnection(int clientID)
        {
            GameClient Client = null;
            this._clients.TryRemove(clientID, out Client);
        }
        public void LogClonesOut(int UserID)
        {
            GetClientByUserID(UserID)?.Disconnect(true);
        }

        public void RegisterClient(GameClient client, int userID, string username)
        {
            // FIX: AddOrUpdate es atómico — elimina race condition de ContainsKey+TryAdd
            _usernameRegister.AddOrUpdate(username.ToLower(), client, (_, __) => client);
            _userIDRegister.AddOrUpdate(userID, client, (_, __) => client);
        }

        public void RegisterClientPhone(GameClient client, int userID, string number)
        {
            // FIX: AddOrUpdate atómico
            _usernameRegisterPhone.AddOrUpdate(number.ToLower(), client, (_, __) => client);
            _userIDRegister.AddOrUpdate(userID, client, (_, __) => client);
        }


        public void UnregisterClient(int userid, string username)
        {
            GameClient Client = null;
            _userIDRegister.TryRemove(userid, out Client);
            _usernameRegister.TryRemove(username.ToLower(), out Client);
        }

        public void UnregisterClientPhone(int userid, string number)
        {
            GameClient Client = null;
            _userIDRegister.TryRemove(userid, out Client);
            _usernameRegisterPhone.TryRemove(number.ToLower(), out Client);
        }

        public void CloseAll()
        {
            #region Old Code (Un-necessary as GameClient.cs Disconnect() does it as well)

            foreach (GameClient client in this.GetClients.ToList())
            {
                if (client == null)
                    continue;

            }

            log.Info("Done saving users inventory!");
            log.Info("Closing server connections...");

            #endregion

            try
            {
                foreach (GameClient client in this.GetClients.ToList())
                {
                    if (client == null || client.GetConnection() == null)
                        continue;

                    try
                    {
                        client.Disconnect(true);
                        client.GetConnection().Dispose();
                    }
                    catch { }

                }
            }
            catch (Exception e)
            {
                Logging.LogCriticalException(e.ToString());
            }

            // FIX: Console.Clear y mensaje DESPUÉS del loop, no dentro
            Console.Clear();
            Out.WriteLine("<< -SERVER SHUTDOWN->> CLOSING CONNECTIONS", "Polar.GameClientManager", ConsoleColor.Red);

            if (this._clients.Count > 0)
                this._clients.Clear();

            //log.Info("Connections closed!");
            Out.WriteLine("Connections closed!", "Polar.GameClientManager", ConsoleColor.Red);
        }

        private void TestClientConnections()
        {
            if (clientPingStopwatch.ElapsedMilliseconds >= 30000)
            {
                clientPingStopwatch.Restart();
                try
                {
                    var toPing = new List<GameClient>();
                    foreach (GameClient client in this._clients.Values.ToList())
                    {
                        if (client.PingCount < 6)
                        {
                            client.PingCount++;
                            toPing.Add(client);
                        }
                        else
                        {
                            // FIX: ConcurrentQueue — sin lock necesario
                            timedOutConnections.Enqueue(client);
                        }
                    }
                    foreach (var client in toPing)
                    {
                        try
                        {
                            client.SendMessage(new PongComposer());
                        }
                        catch
                        {
                            timedOutConnections.Enqueue(client);
                        }
                    }
                }
                catch (Exception)
                {
                    //ignored
                }
            }
        }

        private void HandleTimeouts()
        {
            // FIX: ConcurrentQueue.TryDequeue — sin lock, sin double-check de Count
            while (timedOutConnections.TryDequeue(out GameClient client))
                client.Disconnect(false);
        }

        public string ClearNumbers(string str)
        {
            return Regex.Replace(str, @"[^0-9]", "", RegexOptions.None);
        }
        public string NumberFormatRP(string number)
        {
            // (xxx)-xxx-xxxx
            return "(" + number.Substring(0, 3) + ")-" + number.Substring(3, 3) + "-" + number.Substring(6, 4);
        }
        public int Count
        {
            get { return this._clients.Count; }
        }

        public ICollection<GameClient> GetClients
        {
            get
            {
                return this._clients.Values;
            }
        }
    }
}