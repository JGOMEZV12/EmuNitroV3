using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Data;
using System.Linq;
using Polar.Database.Interfaces;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Turfs;
using Polar.Communication.Packets.Outgoing.Groups;
using Polar.Communication.Packets.Outgoing.Users;
using Polar.HabboHotel.Cache;
using Polar.Communication.Packets.Outgoing.Messenger;
using Polar.HabboRoleplay.RoleplayUsers;

namespace Polar.HabboHotel.Groups
{
    public class Group
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int AdminOnlyDeco { get; set; }
        public string Badge { get; set; }
        public int CreateTime { get; set; }
        public int CreatorId { get; set; }
        public string Description { get; set; }
        public int RoomId { get; set; }
        public string Colour1 { get; set; }
        public string Colour2 { get; set; }

        public bool ForumEnabled;
        public string ForumName;
        public string ForumDescription;
        public int ForumMessagesCount;
        public double ForumScore;
        public int ForumLastPosterId;
        public string ForumLastPosterName;
        public int ForumLastPosterTimestamp;

        public int WhoCanMod;
        public int WhoCanPost;
        public int WhoCanRead;
        public int WhoCanThread;

        public int GangKills { get; set; }
        public int GangCopKills { get; set; }
        public int GangDeaths { get; set; }
        public int GangScore { get; set; }
        public int GangTurfsTaken { get; set; }
        public int GangTurfsDefended { get; set; }
        public int MediPacks { get; set; }
        public bool HasChat { get; set; }
        public int Balance { get; set; }
        public bool IsGang { get; set; }
        public GroupType GroupType;
        public int GType { get; set; }
        public int Stock { get; set; }
        public bool BankRuptcy { get; set; }


        public ConcurrentDictionary<int, GroupRank> Ranks;
        public ConcurrentDictionary<int, GroupMember> Members;
        public ConcurrentDictionary<int, GroupLogs> Logs;
        public List<int> Requests;
        
        public Group(int Id, string Name, string Description, string Badge, int RoomId, int Owner, int Time, int Type, string Colour1, string Colour2, int AdminOnlyDeco,
            bool forumEnabled, string forumName, string forumDescription, int forumMessagesCount, double forumScore, 
            int forumLastPosterId, string forumLastPosterName, int forumLastPosterTimestamp, 
            int WhoCanMod, int WhoCanPost, int WhoCanRead, int WhoCanThread, 
            ConcurrentDictionary<int, GroupRank> Ranks, ConcurrentDictionary<int, GroupMember> Members, List<int> Requests,
            int GangKills, int GangCopKills, int GangDeaths, int GangScore, int gangTurfsTaken, int gangTurfsDefended, int MediPacks, bool hChat, int Bbalance, int Stock, bool isGang, bool BankRuptcy, ConcurrentDictionary<int, GroupLogs> Logs)
        {
            this.Id = Id;
            this.Name = Name;
            this.Description = Description;
            this.RoomId = RoomId;
            this.Badge = Badge;
            this.CreateTime = Time;
            this.CreatorId = Owner;
            this.Colour1 = Colour1;
            this.Colour2 = Colour2;
            /*this.Colour1 = (Colour1 == 0) ? 1 : Colour1;
            this.Colour2 = (Colour2 == 0) ? 1 : Colour2;*/
            this.AdminOnlyDeco = AdminOnlyDeco;

            this.ForumEnabled = forumEnabled;
            this.ForumName = forumName;
            this.ForumDescription = forumDescription;
            this.ForumMessagesCount = forumMessagesCount;
            this.ForumScore = forumScore;
            this.ForumLastPosterId = forumLastPosterId;
            this.ForumLastPosterName = forumLastPosterName;
            this.ForumLastPosterTimestamp = forumLastPosterTimestamp;

            this.WhoCanMod = WhoCanMod;
            this.WhoCanPost = WhoCanPost;
            this.WhoCanRead = WhoCanRead;
            this.WhoCanThread = WhoCanThread;

            this.GangKills = GangKills;

            this.GangCopKills = GangCopKills;
            this.GangDeaths = GangDeaths;
            this.GangScore = GangScore;
            this.GangTurfsTaken = gangTurfsTaken;
            this.GangTurfsDefended = gangTurfsDefended;
            this.MediPacks = MediPacks;
            this.HasChat = hChat;
            this.Balance = Bbalance;
            this.IsGang = isGang;
            this.Stock = Stock;

            this.BankRuptcy = BankRuptcy;

            if (Id >= 1000) 
                this.GType = 3;
            else 
                this.GType = 2;

            switch (Type)
            {
                case 0:
                    this.GroupType = GroupType.OPEN;
                    break;
                case 1:
                    this.GroupType = GroupType.LOCKED;
                    break;
                case 2:
                    this.GroupType = GroupType.PRIVATE;
                    break;
            }

            this.Ranks = Ranks;
            this.Members = Members;
            this.Requests = Requests;
            this.Logs = Logs;

            InitLogs();
        }

        public void InitLogs()
        {
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                DataTable GetLogs = null;
                dbClient.SetQuery("SELECT * FROM `groups_logs` WHERE `group_id` = @id ORDER BY timestamp DESC LIMIT 30");
                dbClient.AddParameter("id", this.Id);
                GetLogs = dbClient.getTable();
                int c = 0;
                if (GetLogs != null)
                {
                    foreach (DataRow Row in GetLogs.Rows)
                    {
                        GroupLogs Logs = new GroupLogs(this.Id, Convert.ToInt32(Row["user_id"]), Convert.ToString(Row["action"]), Convert.ToInt32(Row["cant"]), PolarEnvironment.UnixTimeStampToDateTime(Convert.ToDouble(Row["timestamp"])));

                        this.Logs.TryAdd(c, Logs);
                        c++;
                    }
                }
            }
        }
        public void SetBussines(int bank, int stock)
        {
            string isGroup = "";
            this.Balance = bank;
            this.Stock = stock;

            this.BankRuptcy = (this.Balance <= ((RoleplayManager.GangsPrice / 4) * -1));

            if (Id >= 1000)
                isGroup = "rp_gangs";
            else
                isGroup = "rp_jobs";

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("UPDATE "+ isGroup+" SET `bank_balance` = @bank, `stock` = @stock, `bankruptcy` = @br WHERE id = @gid");
                dbClient.AddParameter("gid", this.Id);
                dbClient.AddParameter("bank", (bank < 0 ? 0 : bank));
                dbClient.AddParameter("stock", stock);
                dbClient.AddParameter("br", PolarEnvironment.BoolToEnum(this.BankRuptcy));
                dbClient.RunQuery();
            }
        }


        public void ClaimTurf(int RoomId, int TurfFlagId)
        {
            List<Turf> TF = PolarEnvironment.GetGame().GetGangTurfsManager().getTurfbyRoomList(RoomId);
            if (TF != null && TF.Count > 0)
            {
                TF[0].GangId = this.Id;
            }

            // Actualizamos el group de la sala y colores de la bandera en DB
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.RunQuery("UPDATE `rooms` SET `group_id` = '" + this.Id + "' WHERE `id` = '" + RoomId + "' LIMIT 1");
                dbClient.RunQuery("UPDATE `items_groups` SET `group_id` = '" + this.Id + "' WHERE `id` = '" + TurfFlagId + "' LIMIT 1");
            }
        }


        public void UpdateStat(int GId, string stat, string value)
        {
            string isGroup = "";

            if (GId > 1000)
            {

                isGroup = "rp_gangs";
            }
            else if (GId < 1000)
            {
                isGroup = "rp_jobs";
            }

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("UPDATE "+ isGroup+" SET `" + stat + "` = '" + value + "' WHERE id = @gid");
                dbClient.AddParameter("gid", GId);
                dbClient.RunQuery();
            }
        }

        public void UpdateStat(int GId, string stat, int value)
        {
            string isGroup = "";

            if (GId > 1000)
            {

                isGroup = "rp_gangs";
            }
            else if (GId < 1000)
            {
                isGroup = "rp_jobs";
            }

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("UPDATE "+ isGroup+" SET `" + stat + "` = " + value + " WHERE id = @gid");
                dbClient.AddParameter("gid", GId);
                dbClient.RunQuery();
            }
        }

        public bool GetGroupbyUserId(int userid)
        {
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                DataTable GetData = null;
                dbClient.SetQuery("SELECT `job_id`,`gang_id` FROM `rp_stats` WHERE `id` = @id LIMIT 1");
                dbClient.AddParameter("id", userid);
                GetData = dbClient.getTable();

                if (GetData != null)
                {
                    foreach (DataRow Row in GetData.Rows)
                    {
                        if (Convert.ToInt32(Row["job_id"]) == this.Id)
                            return true;
                        else if (Convert.ToInt32(Row["gang_id"]) == this.Id)
                            return true;
                    }
                }
            }

            return false;
        }
        public int ForumLastPostTime
        {
            get { return (Convert.ToInt32(PolarEnvironment.GetUnixTimestamp()) - ForumLastPosterTimestamp); }
        }

        public void UpdateForum()
        {
            if (!ForumEnabled)
                return;

            using (var adapter = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                adapter.SetQuery("UPDATE rp_jobs SET forum_messages_count = @msgcount , forum_score = @score , forum_lastposter_id = @lastposterid , forum_lastposter_name = @lastpostername , forum_lastposter_timestamp = @lasttimestamp WHERE id = @id");
                adapter.AddParameter("id", this.Id);
                adapter.AddParameter("msgcount", this.ForumMessagesCount);
                adapter.AddParameter("score", this.ForumScore.ToString());
                adapter.AddParameter("lastposterid", this.ForumLastPosterId);
                adapter.AddParameter("lastpostername", this.ForumLastPosterName);
                adapter.AddParameter("lasttimestamp", this.ForumLastPosterTimestamp);
                adapter.RunQuery();
            }
        }

        public void ClearRequests()
        {
            using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                if (this.Id < 1000)
                    dbClient.SetQuery("UPDATE `rp_stats` SET `job_request` = '0' WHERE `job_request` = '" + this.Id + "'");
                else
                    dbClient.SetQuery("UPDATE `rp_stats` SET `gang_request` = '0' WHERE `gang_request` = '" + this.Id + "'");
                dbClient.RunQuery();
            }

            this.Requests.Clear();
        }

        public ConcurrentDictionary<int, GroupLogs> GetLogs
        {
            get
            {
                return this.Logs;
            }
        }

        public List<GroupLogs> getAllLogs()
        {
            List<GroupLogs> VH = new List<GroupLogs>();

            foreach (var item in this.Logs)
            {
                VH.Add(item.Value);
            }
            return VH;
        }
        public List<int> GetRequests
        {
            get { return this.Requests.ToList(); }
        }

        public bool IsMember(int UserId)
        {
            return this.Members.ContainsKey(UserId);
        }

        public bool IsAdmin(int UserId)
        {
            if (!this.Members.ContainsKey(UserId))
                return false;

            return this.Members[UserId].IsAdmin;
        }

        public bool HasRequest(int UserId)
        {
            return this.Requests.Contains(UserId);
        }

        public void HandleRequest(int UserId, bool Accepted)
        {
            if (!HasRequest(UserId))
                return;

            if (Accepted)
                AddNewMember(UserId);
            else
            {
                this.Requests.Remove(UserId);

                GameClient Client = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(UserId);

                if (Client != null && Client.GetRoleplay() != null)
                {
                    if (this.Id < 1000)
                        Client.GetRoleplay().JobRequest = 0;
                    else
                        Client.GetRoleplay().GangRequest = 0;
                }
                else
                {
                    using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                    {
                        if (this.Id < 1000)
                            dbClient.SetQuery("UPDATE `rp_stats` SET `job_request` = '0' WHERE `id` = '" + UserId + "' LIMIT 1");
                        else
                            dbClient.SetQuery("UPDATE `rp_stats` SET `gang_request` = '0' WHERE `id` = '" + UserId + "' LIMIT 1");
                        dbClient.RunQuery();
                    }
                }
            }
        }

        public void MakeOwner(int Id)
        {
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("UPDATE rp_gangs SET `owner_id` = @uid WHERE `id` = @gid LIMIT 1");
                dbClient.AddParameter("gid", this.Id);
                dbClient.AddParameter("uid", Id);
                dbClient.RunQuery();
            }
        }

        public void AddNewMember(int UserId, int RankId = 1, bool UpdateDatabase = false)
        {
            UserCache Habbo = PolarEnvironment.GetGame().GetCacheManager().GenerateUser(UserId);

            if (Habbo == null)
                return;

            RemoveAllOldGroups(UserId);
            RemoveAllOldRequests(UserId);

            GroupMember Member = new GroupMember(this.Id, UserId, RankId, RankId >= 6);

            this.Members.TryAdd(UserId, Member);

            if (Habbo != null || UpdateDatabase)
            {
                using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    if (this.Id < 1000)
                        dbClient.SetQuery("UPDATE `rp_stats` SET `job_id` = '" + this.Id + "', `job_rank` = '" + RankId + "', `job_request` = '0', `time_worked` = '0' WHERE `id` = '" + UserId + "' LIMIT 1");
                    else
                        dbClient.SetQuery("UPDATE `rp_stats` SET `gang_id` = '" + this.Id + "', `gang_rank` = '" + RankId + "', `gang_request` = '0' WHERE `id` = '" + UserId + "' LIMIT 1");
                    dbClient.RunQuery();
                }
            }
        }

        /*public void AddNewMember(int UserId, int RankId = 1, bool UpdateDatabase = false)
        {
            //RemoveAllOldGroups(UserId);
            //RemoveAllOldRequests(UserId);

            if (this.IsMember(Id) || this.GroupType == GroupType.LOCKED && this.Requests.Contains(Id))
                return;

            UserCache Habbo = PolarEnvironment.GetGame().GetCacheManager().GenerateUser(UserId);

            if (Habbo == null)
                return;

            GroupMember Member = new GroupMember(this.Id, UserId, RankId, RankId >= 6);

            //this.Members.TryAdd(UserId, Member);

            if (Habbo != null || UpdateDatabase)
            {
                using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    if (this.GroupType == GroupType.LOCKED/* && this.GetAdministrator.Count != 0)
                    {
                        if (Habbo == null)
                        {
                            if (this.Id < 1000)
                                dbClient.SetQuery("INSERT INTO `rp_jobs_requests` (user_id, job_id, timestamp) VALUES ('" + UserId + "', '" + this.Id + "', " + PolarEnvironment.GetUnixTimestamp() + ")");
                            else
                                dbClient.SetQuery("INSERT INTO `rp_gangs_requests` (user_id, gang_id) VALUES ('" + UserId + "', '" + this.Id + "')");
 
                            this.Requests.Add(Id);
                        }
                        if (Habbo != null && Habbo.GetRoleplay() != null)
                        {
                            if (this.Id < 1000)
                                dbClient.SetQuery("UPDATE `rp_stats` SET `job_id` = '" + this.Id + "', `job_rank` = '" + RankId + "', `job_request` = '0', `time_worked` = '0' WHERE `id` = '" + UserId + "' LIMIT 1");
                            else
                                dbClient.SetQuery("UPDATE `rp_stats` SET `gang_id` = '" + this.Id + "', `gang_rank` = '" + RankId + "', `gang_request` = '0' WHERE `id` = '" + UserId + "' LIMIT 1");

                            this.Members.TryAdd(Id, Member);
                            if (this.IsMember(Id))
                            {
                                if (!this.IsAdmin(Id))
                                {
                                    this.MakeAdmin(Id);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (this.Id < 1000)
                            dbClient.SetQuery("UPDATE `rp_stats` SET `job_id` = '" + this.Id + "', `job_rank` = '" + RankId + "', `job_request` = '0', `time_worked` = '0' WHERE `id` = '" + UserId + "' LIMIT 1");
                        else
                            dbClient.SetQuery("UPDATE `rp_stats` SET `gang_id` = '" + this.Id + "', `gang_rank` = '" + RankId + "', `gang_request` = '0' WHERE `id` = '" + UserId + "' LIMIT 1");
                        

                        this.Members.TryAdd(Id, Member);
                    }
                    dbClient.RunQuery();
                }
            }
            
            

        }*/

        public void RemoveAllOldGroups(int UserId)
        {
            List<Group> OldGroups;

            if (this.Id < 1000)
                OldGroups = GroupManager.Jobs.Values.Where(x => x.Members.ContainsKey(UserId)).ToList();
            else
                OldGroups = GroupManager.Gangs.Values.Where(x => x.Members.ContainsKey(UserId)).ToList();

            if (OldGroups.Count > 0)
            {
                foreach(Group Group in OldGroups)
                {
                    GroupMember Junk;
                    Group.Members.TryRemove(UserId, out Junk);
                }
            }
        }

        public void RemoveAllOldRequests(int UserId)
        {
            List<Group> OldGroups;

            if (this.Id < 1000)
                OldGroups = GroupManager.Jobs.Values.Where(x => x.Requests.Contains(UserId)).ToList();
            else
                OldGroups = GroupManager.Gangs.Values.Where(x => x.Requests.Contains(UserId)).ToList();

            if (OldGroups.Count > 0)
            {
                foreach (Group Group in OldGroups)
                {
                    Group.Requests.Remove(UserId);
                }
            }
        }

        public void MakeAdmin(int UserId)
        {
            if (!this.Ranks.ContainsKey(6) || this.Id >= 1000)
                return;

            if (!IsMember(UserId))
                return;

            RemoveAllOldRequests(UserId);

            using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("UPDATE `rp_stats` SET `job_id` = '" + this.Id + "', `job_rank` = '6', `job_request` = '0' WHERE `id` = '" + UserId + "' LIMIT 1");
                dbClient.RunQuery();
            }

            this.Members[UserId].UserRank = 6;
            this.Members[UserId].IsAdmin = true;

            GameClient Client = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(UserId);
            if (Client != null)
            {
                if (Client.GetRoleplay() != null && Client.GetRoleplay().IsWorking)
                {
                    WorkManager.RemoveWorkerFromList(Client);
                    Client.GetRoleplay().IsWorking = false;
                    Client.GetHabbo().Poof();
                }

                Client.GetRoleplay().JobRank = 6;
                Client.GetRoleplay().JobRequest = 0;
                Client.SendNotification("You have just been promoted to the Manager of the " + this.Name + " corporation!");
            }
        }

        public void TakeAdmin(int UserId)
        {
            if (!IsMember(UserId) || this.Id >= 1000)
                return;

            RemoveAllOldRequests(UserId);

            using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("UPDATE `rp_stats` SET `job_id` = '" + this.Id + "', `job_rank` = '1', `job_request` = '0' WHERE `id` = '" + UserId + "' LIMIT 1");
                dbClient.RunQuery();
            }

            this.Members[UserId].UserRank = 1;
            this.Members[UserId].IsAdmin = false;

            GameClient Client = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(UserId);
            if (Client != null)
            {
                if (Client.GetRoleplay() != null && Client.GetRoleplay().IsWorking)
                {
                    WorkManager.RemoveWorkerFromList(Client);
                    Client.GetRoleplay().IsWorking = false;
                    Client.GetHabbo().Poof();
                }

                Client.GetRoleplay().JobRank = 1;
                Client.GetRoleplay().JobRequest = 0;
                Client.SendNotification("You have just been removed as the Manager of the " + this.Name + " corporation!");
            }
        }

        public void UpdateJobMember(int UserId)
        {
            GameClient Client = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(UserId);

            if (Client != null && Client.GetRoleplay() != null)
            {

                if (!this.Members.ContainsKey(UserId))
                    return;

                this.Members[UserId].UserRank = Client.GetRoleplay().JobRank;
                this.Members[UserId].IsAdmin = Client.GetRoleplay().JobRank == 6;
            }
        }

        public void UpdateGangMember(int UserId)
        {
            GameClient Client = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(UserId);

            if (Client != null && Client.GetRoleplay() != null)
            {
                this.Members[UserId].UserRank = Client.GetRoleplay().GangRank;
                this.Members[UserId].IsAdmin = Client.GetRoleplay().GangRank == 5;
            }
        }

        public void UpdateGangMemberDB(int UserId)
        {
            using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("UPDATE `rp_stats` SET `gang_id` = '0', `gang_rank` = '1', `gang_request` = '0' WHERE `id` = '" + UserId + "' LIMIT 1");
                dbClient.RunQuery();
            }
        }

        public void DeleteMember(int Id)
        {
            if (IsMemberDict(Id))
            {
                GroupMember Junk;
                this.Members.TryRemove(Id, out Junk);
            }

            if (this.HasChat)
            {
                GameClient Client;
                Client = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Id);
                if (Client != null)
                {
                    Client.SendMessage(new FriendListUpdateComposer(int.MinValue + this.Id));

                }


            }

            // NEW
            if (this.GType < 3)
            {
                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("UPDATE rp_stats SET `job_id` = '0' WHERE id = @uid");
                    dbClient.AddParameter("uid", Id);
                    dbClient.RunQuery();
                }
            }
            else
            {
                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("UPDATE rp_stats SET `gang_id` = '0' WHERE id = @uid");
                    dbClient.AddParameter("uid", Id);
                    dbClient.RunQuery();
                }
            }
        }

        public void TransferGangOwnership(GameClient Session, GameClient Client)
        {
            if (Session == null || Session.GetRoleplay() == null || Session.GetHabbo() == null)
                return;

            if (Client == null || Client.GetRoleplay() == null || Client.GetHabbo() == null)
                return;

            this.CreatorId = Client.GetHabbo().Id;
            using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("UPDATE `rp_gangs` SET `owner_id` = '" + Client.GetHabbo().Id + "' WHERE `id` = '" + this.Id + "' LIMIT 1");
                dbClient.RunQuery();
            }

            Session.GetRoleplay().GangRank = 1;
            Session.GetRoleplay().GangRequest = 0;
            UpdateGangMember(Session.GetHabbo().Id);

            Client.GetRoleplay().GangRank = 6;
            Client.GetRoleplay().GangRequest = 0;
            UpdateGangMember(Client.GetHabbo().Id);

            UserCache Junk = null;
            PolarEnvironment.GetGame().GetCacheManager().TryRemoveUser(Session.GetHabbo().Id, out Junk);
            PolarEnvironment.GetGame().GetCacheManager().GenerateUser(Session.GetHabbo().Id);

            UserCache Junk2 = null;
            PolarEnvironment.GetGame().GetCacheManager().TryRemoveUser(Client.GetHabbo().Id, out Junk2);
            PolarEnvironment.GetGame().GetCacheManager().GenerateUser(Client.GetHabbo().Id);

            SendPackets(Session);
            SendPackets(Client);
        }

        public void SendPackets(GameClient Client)
        {
            if (Client == null || Client.GetHabbo() == null)
                return;

            if (Client.GetRoomUser() != null && Client.GetRoomUser().GetRoom() != null)
            {
                if (this.Id < 1000)
                {
                    Client.GetRoomUser().GetRoom().SendMessage(new UpdateFavouriteGroupComposer(this, Client.GetRoomUser().VirtualId));
                    Client.GetRoomUser().GetRoom().SendMessage(new RefreshFavouriteGroupComposer(Client.GetHabbo().Id));
                }
                //Client.SendMessage(new GroupInfoComposer(this, Client));
                //Client.GetRoomUser().GetRoom().SendMessage(new GroupMemberUpdatedComposer(this.Id, Client.GetHabbo(), 4));
                Client.GetRoomUser().GetRoom().SendMessage(new HabboGroupBadgesComposer(this));
            }
            else
            {
                if (this.Id < 1000)
                    Client.SendMessage(new RefreshFavouriteGroupComposer(Client.GetHabbo().Id));

                //Client.SendMessage(new GroupInfoComposer(this, Client));
                //Client.SendMessage(new GroupMemberUpdatedComposer(this.Id, Client.GetHabbo(), 4));
                Client.SendMessage(new HabboGroupBadgesComposer(this));
            }
        }

        public void SendMembersPackets(GameClient Client)
        {
            if (Client == null || Client.GetHabbo() == null || Client.GetRoleplay() == null)
                return;

            List<int> Members = new List<int>();
            List<GroupMember> Administrators = this.Members.Values.Where(x => x.IsAdmin).OrderBy(x => x.UserId).ToList();
            List<GroupMember> NonAdministrators = this.Members.Values.Where(x => !x.IsAdmin).OrderBy(x => x.UserId).ToList();

            List<GroupMember> MembersToCheck = new List<GroupMember>();
            MembersToCheck.AddRange(Administrators);
            MembersToCheck.AddRange(NonAdministrators);

            MembersToCheck = MembersToCheck.Take(500).ToList();

            if (this.Id <= 1000 && !Members.Contains(0))
                Members.Add(0);

            foreach (GroupMember Member in MembersToCheck)
            {
                if (!Members.Contains(Member.UserId))
                    Members.Add(Member.UserId);
            }

            int FinishIndex = 14 < this.Members.Count ? 14 : this.Members.Count;
            int MembersCount = Members.Count;

            Client.SendMessage(new GroupMembersComposer(this, Members, MembersCount, 0, (this.CreatorId == Client.GetHabbo().Id || this.IsAdmin(Client.GetHabbo().Id) || Client.GetHabbo().GetPermissions().HasRight("roleplay_corp_manager")), 0, ""));
            Client.SendMessage(new GroupInfoComposer(this, Client));
        }

        public bool IsMemberDict(int UserId)
        {
            return this.Members.ContainsKey(UserId);
        }

        public void UpdateGroupName(string NewName)
        {
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("UPDATE rp_gangs SET `name` = @name WHERE id = @gid");
                dbClient.AddParameter("gid", this.Id);
                dbClient.AddParameter("name", NewName);
                dbClient.RunQuery();
            }
            this.Name = NewName;
        }

        public void UpdateGangAccessType(int AccessType)
        {

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("UPDATE rp_gangs SET `state` = @atype WHERE id = @gid");
                dbClient.AddParameter("gid", this.Id);
                dbClient.AddParameter("atype", AccessType.ToString());
                dbClient.RunQuery();
            }

            if (AccessType == 1)
                this.GroupType = GroupType.LOCKED;
            else
                this.GroupType = GroupType.OPEN;
        }
        public void SetBussines(int bank)
        {
            this.Balance = bank;

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("UPDATE rp_gangs SET `bank_balance` = @bank WHERE id = @gid");
                dbClient.AddParameter("gid", this.Id);
                dbClient.AddParameter("bank", (bank < 0 ? 0 : bank));
                dbClient.RunQuery();
            }
        }

        public void AddLog(int Id, string Action, int Cant)
        {
            if (!this.IsMember(Id) && !this.IsMemberDict(Id) && !this.IsAdmin(Id))
                return;

            GroupLogs Log = new GroupLogs(this.Id, Id, Action, Cant, DateTime.Now);

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("INSERT INTO `groups_logs` (user_id, group_id, action, cant, timestamp) VALUES (@uid, @gid, @act, @cant, @tim)");
                dbClient.AddParameter("gid", this.Id);
                dbClient.AddParameter("uid", Id);
                dbClient.AddParameter("act", Action);
                dbClient.AddParameter("cant", Cant);
                dbClient.AddParameter("tim", PolarEnvironment.GetUnixTimestamp());
                dbClient.RunQuery();

                this.Logs.TryAdd(this.Logs.Count, Log);
            }
        }
        public void UpdateJobBadge(string URL)
        {
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("UPDATE rp_jobs SET `badge` = @url WHERE id = @gid");
                dbClient.AddParameter("gid", this.Id);
                dbClient.AddParameter("url", URL);
                dbClient.RunQuery();
            }
            this.Badge = URL;
        }

        public void UpdateJobSettings(int RankId, string NewName, int NewPay, int NewTimer)
        {
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                if (!string.IsNullOrEmpty(NewName))
                {
                    dbClient.SetQuery("UPDATE rp_jobs_ranks SET `name` = @name, `pay` = @pay WHERE job = @gid AND rank = @rank");
                    dbClient.AddParameter("gid", this.Id);
                    dbClient.AddParameter("rank", RankId);
                    dbClient.AddParameter("name", NewName);
                    dbClient.AddParameter("pay", NewPay);
                }
                else
                {
                    dbClient.SetQuery("UPDATE rp_jobs_ranks SET `pay` = @pay WHERE job = @gid AND rank = @rank");
                    dbClient.AddParameter("gid", this.Id);
                    dbClient.AddParameter("rank", RankId);
                    dbClient.AddParameter("pay", NewPay);
                }

                dbClient.RunQuery();
            }

            if (!string.IsNullOrEmpty(NewName))
                this.Ranks.ToList().Where(x => x.Value.RankId == RankId).ToList().ForEach(x => x.Value.Name = NewName);

            this.Ranks.ToList().Where(x => x.Value.RankId == RankId).ToList().ForEach(x => x.Value.Pay = NewPay);
        }

        public void UpdateJobLooks(int RankId, string NewFigure, string Gender)
        {
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                string table = "male_figure";

                if (Gender == "F")
                    table = "female_figure";

                dbClient.SetQuery("UPDATE rp_jobs_ranks SET `" + table + "` = @figure WHERE job = @gid AND rank = @rank");
                dbClient.AddParameter("gid", this.Id);
                dbClient.AddParameter("rank", RankId);
                dbClient.AddParameter("figure", NewFigure);
                dbClient.RunQuery();
            }

            if (Gender == "M")
                this.Ranks.ToList().Where(x => x.Value.RankId == RankId).ToList().ForEach(x => x.Value.MaleFigure = NewFigure);
            else
                this.Ranks.ToList().Where(x => x.Value.RankId == RankId).ToList().ForEach(x => x.Value.FemaleFigure = NewFigure);
        }

        public void UpdateJobCommads(int RankId, string NewCommand)
        {
            GroupRank Rank = GroupManager.GetJobRank(this.Id, RankId);

            if (Rank == null)
                return;

            string RnwCommands = "";

            for (int i = 0; i < Rank.Commands.Length; i++)
            {
                //Console.WriteLine(i + ": " + Rank.Commands[i]);
                if (Rank.Commands[i].ToString().Length > 0)
                    RnwCommands += Rank.Commands[i].ToString() + ",";
            }

            if (this.Ranks.ToList().Where(x => x.Value.RankId == RankId && x.Value.HasCommand(NewCommand)).Count() > 0)
            {
                // Tiene el permiso. Quitamos del Arrelgo.
                RnwCommands = RnwCommands.Replace(NewCommand + ",", "");
            }
            else
            {
                // No tiene el permiso. Agregarlo al arreglo.
                RnwCommands += NewCommand + ",";
            }

            string[] ArrayCommands = RnwCommands.Split(',');
            this.Ranks.ToList().Where(x => x.Value.RankId == RankId).ToList().ForEach(x => x.Value.Commands = ArrayCommands);

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("UPDATE rp_jobs_ranks SET `commands` = @cmds WHERE job = @gid AND rank = @rank");
                dbClient.AddParameter("gid", this.Id);
                dbClient.AddParameter("rank", RankId);
                dbClient.AddParameter("cmds", RnwCommands);
                dbClient.RunQuery();
            }
        }

        public void AddRank(int GroupId, int RankId, string Name, string M_Figure, string F_Figure, int Pay, string[] Commands, string[] WorkRooms, int Limit = 0)
        {
            if (GroupManager.JobExists(GroupId, RankId))
                return;

            GroupRank Rank = new GroupRank(GroupId, RankId, Name, M_Figure, F_Figure, Pay, Commands, WorkRooms, Limit);

            string sCommands = "";
            string sWorkRooms = "";

            for (int i = 0; i < Commands.Length; i++)
            {
                if (Commands[i].Length > 0)
                    sCommands += Commands[i].ToString() + ",";
            }

            for (int i = 0; i < WorkRooms.Length; i++)
            {
                if (WorkRooms[i].Length > 0)
                    sWorkRooms += WorkRooms[i].ToString() + ",";
            }

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("INSERT INTO `rp_gangs_ranks` (`gang`, `rank`, `name`, `male_figure`, `female_figure`, `pay`, `commands`, `workrooms`, `limit`, `timer`) VALUES (@job, @rank, @name, @male_figure, @female_figure, @pay, @commands, @workrooms, @limit, @timer)");
                this.Ranks.TryAdd(RankId, Rank);

                dbClient.AddParameter("job", GroupId);
                dbClient.AddParameter("rank", RankId);
                dbClient.AddParameter("name", Name);
                dbClient.AddParameter("male_figure", M_Figure);
                dbClient.AddParameter("female_figure", F_Figure);
                dbClient.AddParameter("pay", Pay);
                dbClient.AddParameter("commands", sCommands);
                dbClient.AddParameter("workrooms", sWorkRooms);
                dbClient.AddParameter("limit", Limit);
                dbClient.RunQuery();
            }
        }

        public string GetBadge()
        {
            DataRow Data = null;
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT `badge` FROM `rp_gangs` WHERE `id` = '" + this.Id + "' LIMIT 1");
                Data = dbClient.getRow();

                if (Data != null)
                    return Data["badge"].ToString();
            }
            return string.Empty;
        }
        public ConcurrentDictionary<int, GroupMember> GetAllMembersDict
        {
            get
            {
                return this.Members;
            }
        }
        /// <summary>
        /// Recarga todos los campos escalares del grupo/pandilla desde la base de datos
        /// y regenera Members, Requests y Logs en memoria.
        /// No toca Ranks de Jobs (se gestionan con GenerateJobRanks en GroupManager).
        /// </summary>
        public bool RefreshFromDatabase()
        {
            string table = (this.Id >= 1000) ? "rp_gangs" : "rp_jobs";

            try
            {
                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("SELECT * FROM `" + table + "` WHERE `id` = @id LIMIT 1");
                    dbClient.AddParameter("id", this.Id);
                    DataRow Row = dbClient.getRow();

                    if (Row == null) return false;

                    // ── Campos comunes ─────────────────────────────────────────
                    this.Name        = Row["name"].ToString();
                    this.Description = Row["desc"].ToString();
                    this.Badge       = Row["badge"].ToString();
                    this.CreatorId   = Convert.ToInt32(Row["owner_id"]);
                    this.RoomId      = Convert.ToInt32(Row["room_id"]);
                    this.Colour1     = Convert.ToString(Row["colour1"]);
                    this.Colour2     = Convert.ToString(Row["colour2"]);
                    this.AdminOnlyDeco = Convert.ToInt32(Row["admindeco"]);
                    this.HasChat     = PolarEnvironment.EnumToBool(Row["has_chat"].ToString());
                    this.Balance     = Convert.ToInt32(Row["bank_balance"]);
                    this.Stock       = Convert.ToInt32(Row["stock"]);
                    this.IsGang      = PolarEnvironment.EnumToBool(Row["isGang"].ToString());

                    int state = Convert.ToInt32(Row["state"]);
                    switch (state)
                    {
                        case 0: this.GroupType = GroupType.OPEN;    break;
                        case 1: this.GroupType = GroupType.LOCKED;  break;
                        case 2: this.GroupType = GroupType.PRIVATE; break;
                    }

                    // ── Foro ───────────────────────────────────────────────────
                    this.ForumEnabled           = PolarEnvironment.EnumToBool(Row["forum_enabled"].ToString());
                    this.ForumMessagesCount     = Convert.ToInt32(Row["forum_messages_count"]);
                    this.ForumScore             = Convert.ToDouble(Row["forum_score"]);
                    this.ForumLastPosterId      = Convert.ToInt32(Row["forum_lastposter_id"]);
                    this.ForumLastPosterName    = PolarEnvironment.GetHabboById(this.ForumLastPosterId) == null
                                                    ? "HoloRP"
                                                    : PolarEnvironment.GetHabboById(this.ForumLastPosterId).Username;
                    this.ForumLastPosterTimestamp = Convert.ToInt32(Row["forum_lastposter_timestamp"]);
                    this.WhoCanMod    = Convert.ToInt32(Row["who_can_mod"]);
                    this.WhoCanPost   = Convert.ToInt32(Row["who_can_post"]);
                    this.WhoCanRead   = Convert.ToInt32(Row["who_can_read"]);
                    this.WhoCanThread = Convert.ToInt32(Row["who_can_thread"]);

                    // ── Campos exclusivos de pandillas ─────────────────────────
                    if (this.Id >= 1000)
                    {
                        this.GangKills        = Convert.ToInt32(Row["gang_kills"]);
                        this.GangCopKills     = Convert.ToInt32(Row["gang_cop_kills"]);
                        this.GangDeaths       = Convert.ToInt32(Row["gang_deaths"]);
                        this.GangScore        = Convert.ToInt32(Row["gang_score"]);
                        this.GangTurfsTaken   = Convert.ToInt32(Row["gang_turfs_taken"]);
                        this.GangTurfsDefended= Convert.ToInt32(Row["gang_turfs_defend"]);
                        this.MediPacks        = Convert.ToInt32(Row["medipacks"]);
                        this.BankRuptcy       = PolarEnvironment.EnumToBool(Row["bankruptcy"].ToString());
                    }
                }

                // ── Regenerar miembros, peticiones y logs ──────────────────────
                var gm = PolarEnvironment.GetGame().GetGroupManager();

                this.Members.Clear();
                this.Requests.Clear();
                this.Logs.Clear();

                List<int> newRequests;
                ConcurrentDictionary<int, GroupMember> newMembers;

                if (this.Id >= 1000)
                    newMembers = gm.GenerateGangMembers(this.Id, out newRequests);
                else
                    newMembers = gm.GenerateJobMembers(this.Id, out newRequests);

                foreach (var kv in newMembers)
                    this.Members.TryAdd(kv.Key, kv.Value);

                foreach (int uid in newRequests)
                    this.Requests.Add(uid);

                ConcurrentDictionary<int, GroupLogs> newLogs = gm.GenerateLogs(this.Id);
                foreach (var kv in newLogs)
                    this.Logs.TryAdd(kv.Key, kv.Value);

                // ── Regenerar ranks de trabajos (pandillas usan GenericGangRanks) ─
                if (this.Id < 1000)
                {
                    this.Ranks.Clear();
                    ConcurrentDictionary<int, GroupRank> newRanks = gm.GenerateJobRanks(this.Id);
                    foreach (var kv in newRanks)
                        this.Ranks.TryAdd(kv.Key, kv.Value);
                }

                return true;
            }
            catch (Exception ex)
            {
                Polar.Core.Logging.LogException("[Group.RefreshFromDatabase] Id=" + this.Id + " -> " + ex);
                return false;
            }
        }

        public void Dispose()
        {
            if (this.Id < 1000)
                this.Ranks.Clear();
            this.Members.Clear();
            this.Requests.Clear();
        }
    }
}