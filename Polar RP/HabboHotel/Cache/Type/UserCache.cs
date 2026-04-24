using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Users;
using Polar.HabboHotel.Users.Messenger;
using Polar.HabboHotel.Users.Permissions;
using Polar.HabboHotel.Users.Relationships;
using Polar.Core;
using System.Data;
using Polar.HabboRoleplay.Timers.Types;

namespace Polar.HabboHotel.Cache
{
    public class UserCache : IDisposable
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Motto { get; set; }
        public string Look { get; set; }
        public int MarriedId { get; set; }
        public int HijoId { get; set; }
        public int Level { get; set; }
        public int Hygiene { get; set; }
        public int Hunger { get; set; }
        public int Arrests { get; set; }
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int HitKills { get; set; }
        public int Intelligence { get; set; }
        public int Strength { get; set; }
        public string SocketStatistics { get; set; }
        public double AccountCreated { get; set; }
        public double LastOnline { get; set; }
        public int Online { get; set; }
        public DateTime AddedTime { get; set; }
        public RoleplayUser _roleplay;
        public HabboStats _habboStats;
        private HabboMessenger Messenger;
        public DataRow StatRow = null;
        public DataTable dFriends = null;
        public DataTable dRelations = null;
        public DataTable dRequests = null;
        public UserCache(int Id, string Username, string Motto, string Look, int MarriedId, int HijoId, int Level, string SocketStatistics, double accCreated, double lastOn, int isOnline, int hygiene, int hunger)
        {
            this.Id = Id;
            this.Username = Username;
            this.Motto = Motto;
            this.Look = Look;
            this.MarriedId = MarriedId;
            this.HijoId = HijoId;
            this.Level = Level;
            this.AddedTime = DateTime.Now;
            this.SocketStatistics = SocketStatistics;
            this.LastOnline = lastOn;
            this.AccountCreated = accCreated;
            this.Online = isOnline;
            this.Hygiene = hygiene;
            this.Hunger = hunger;

          /*  #region Roleplay Data
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery(
                    "SELECT users.id,users.username,users.motto,users.look,users.last_online,users.hide_inroom,users.hide_online " +
                    "FROM users " +
                    "JOIN messenger_friendships " +
                    "ON users.id = messenger_friendships.user_one_id " +
                    "WHERE messenger_friendships.user_two_id = " + Id + " " +
                    "UNION ALL " +
                    "SELECT users.id,users.username,users.motto,users.look,users.last_online,users.hide_inroom,users.hide_online " +
                    "FROM users " +
                    "JOIN messenger_friendships " +
                    "ON users.id = messenger_friendships.user_two_id " +
                    "WHERE messenger_friendships.user_one_id = " + Id);
                dFriends = dbClient.getTable();

                dbClient.SetQuery("SELECT messenger_requests.from_id,messenger_requests.to_id,users.username FROM users JOIN messenger_requests ON users.id = messenger_requests.from_id WHERE messenger_requests.to_id = " + Id);
                dRequests = dbClient.getTable();

                dbClient.SetQuery("SELECT `id`,`user_id`,`target`,`type` FROM `user_relationships` WHERE `user_id` = '" + Id + "'");
                dRelations = dbClient.getTable();

                dbClient.SetQuery("SELECT * FROM `rp_stats` WHERE `id` = '" + Id + "' LIMIT 1");
                DataRow UserRPRow = dbClient.getRow();

                dbClient.SetQuery("SELECT * FROM `rp_stats_cooldowns` WHERE `id` = '" + Id + "' LIMIT 1");
                DataRow UserRPCooldowns = dbClient.getRow();

                if (UserRPCooldowns == null)
                {
                    dbClient.RunQuery("INSERT INTO `rp_stats_cooldowns` (`id`) VALUES ('" + Id + "')");
                    dbClient.SetQuery("SELECT * FROM `rp_stats_cooldowns` WHERE `id` = '" + Id + "' LIMIT 1");
                    UserRPCooldowns = dbClient.getRow();
                }

                dbClient.SetQuery("SELECT * FROM `rp_stats_farming` WHERE `id` = '" + Id + "' LIMIT 1");
                DataRow UserRPFarming = dbClient.getRow();

                if (UserRPFarming == null)
                {
                    dbClient.RunQuery("INSERT INTO `rp_stats_farming` (`id`) VALUES ('" + Id + "')");
                    dbClient.SetQuery("SELECT * FROM `rp_stats_farming` WHERE `id` = '" + Id + "' LIMIT 1");
                    UserRPFarming = dbClient.getRow();
                }

                dbClient.SetQuery("SELECT `id`,`roomvisits`,`onlinetime`,`respect`,`respectgiven`,`giftsgiven`,`giftsreceived`,`dailyrespectpoints`,`dailypetrespectpoints`,`achievementscore`,`quest_id`,`quest_progress`,`groupid`,`tickets_answered`,`respectstimestamp`,`forum_posts` FROM `user_stats` WHERE `id` = @user_id LIMIT 1");
                dbClient.AddParameter("user_id", Id);
                StatRow = dbClient.getRow();

                if (StatRow == null)//No row, add it yo
                {
                    dbClient.RunQuery("INSERT INTO `user_stats` (`id`) VALUES ('" + Id + "')");
                    dbClient.SetQuery("SELECT `id`,`roomvisits`,`onlinetime`,`respect`,`respectgiven`,`giftsgiven`,`giftsreceived`,`dailyrespectpoints`,`dailypetrespectpoints`,`achievementscore`,`quest_id`,`quest_progress`,`groupid`,`tickets_answered`,`respectstimestamp`,`forum_posts` FROM `user_stats` WHERE `id` = @user_id LIMIT 1");
                    dbClient.AddParameter("user_id", Id);
                    StatRow = dbClient.getRow();
                }

                try
                {
                    this._habboStats = new HabboStats(Convert.ToInt32(StatRow["roomvisits"]), Convert.ToDouble(StatRow["onlineTime"]), Convert.ToInt32(StatRow["respect"]), Convert.ToInt32(StatRow["respectGiven"]), Convert.ToInt32(StatRow["giftsGiven"]),
                        Convert.ToInt32(StatRow["giftsReceived"]), Convert.ToInt32(StatRow["dailyRespectPoints"]), Convert.ToInt32(StatRow["dailyPetRespectPoints"]), Convert.ToInt32(StatRow["AchievementScore"]),
                        Convert.ToInt32(StatRow["quest_id"]), Convert.ToInt32(StatRow["quest_progress"]), Convert.ToString(StatRow["respectsTimestamp"]), Convert.ToInt32(StatRow["forum_posts"])); 
                }
                catch (Exception e)
                {
                    Logging.LogException(e.ToString());
                }

                Dictionary<int, MessengerRequest> requests = new Dictionary<int, MessengerRequest>();
                foreach (DataRow dRow in dRequests.Rows)
                {
                    int receiverID = Convert.ToInt32(dRow["from_id"]);
                    int senderID = Convert.ToInt32(dRow["to_id"]);

                    string requestUsername = Convert.ToString(dRow["username"]);

                    if (receiverID != Id)
                    {
                        if (!requests.ContainsKey(receiverID))
                            requests.Add(receiverID, new MessengerRequest(Id, receiverID, requestUsername));
                    }
                    else
                    {
                        if (!requests.ContainsKey(senderID))
                            requests.Add(senderID, new MessengerRequest(Id, senderID, requestUsername));
                    }
                }

                Dictionary<int, MessengerBuddy> friends = new Dictionary<int, MessengerBuddy>();
                foreach (DataRow dRow in dFriends.Rows)
                {
                    int friendID = Convert.ToInt32(dRow["id"]);
                    string friendName = Convert.ToString(dRow["username"]);
                    string friendLook = Convert.ToString(dRow["look"]);
                    string friendMotto = Convert.ToString(dRow["motto"]);
                    int friendLastOnline = Convert.ToInt32(dRow["last_online"]);
                    bool friendHideOnline = PolarEnvironment.EnumToBool(dRow["hide_online"].ToString());
                    bool friendHideRoom = PolarEnvironment.EnumToBool(dRow["hide_inroom"].ToString());

                    if (friendID == Id)
                        continue;

                    if (!friends.ContainsKey(friendID))
                        friends.Add(friendID, new MessengerBuddy(friendID, friendName, friendLook, friendMotto, friendLastOnline, friendHideOnline, friendHideRoom, false));
                }

                Dictionary<int, Relationship> Relationships = new Dictionary<int, Relationship>();
                foreach (DataRow Row in dRelations.Rows)
                {
                    if (friends.ContainsKey(Convert.ToInt32(Row[2])))
                        Relationships.Add(Convert.ToInt32(Row[2]), new Relationship(Convert.ToInt32(Row[0]), Convert.ToInt32(Row[2]), Convert.ToInt32(Row[3].ToString())));
                }

                Messenger = new HabboMessenger(Id);
                Messenger.Init(friends, requests);
                _roleplay = new RoleplayUser(null, UserRPRow, UserRPCooldowns, UserRPFarming);
            }
            #endregion*/
        }
        public bool isExpired() => (DateTime.Now - this.AddedTime).TotalMinutes >= 30.0;

        public HabboStats GetStats()
        {
            return this._habboStats;
        }


        public HabboMessenger GetMessenger()
        {
            return Messenger;
        }

        public RoleplayUser GetRoleplay()
        {
            return _roleplay;
        }

        public void Dispose()
        {
            UserCache OutCache = null;
            PolarEnvironment.GetGame().GetCacheManager()._usersCached.TryRemove(this.Id, out OutCache);

            new Thread(() => {
                Thread.Sleep(500);
                this.Id = 0;
                this.Username = null;
                this.Motto = null;
                this.Look = null;
                this.MarriedId = 0;
                this.HijoId = 0;
                this.Level = 0;
                this.AccountCreated = 0;
                this.LastOnline = 0;
                this.SocketStatistics = null;
                StatRow = null;
                dFriends = null;
                dRelations = null;
                dRequests = null;
                this._roleplay = null;
            }).Start();
        }

    }
}
