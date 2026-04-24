using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.Users;
using System.Collections.Concurrent;

using Polar.Database.Interfaces;
using Polar.Utilities;
using log4net;
using Polar.Communication.Packets.Outgoing.Groups;
using Polar.HabboHotel.Cache;

namespace Polar.HabboHotel.Groups
{
    public class GroupManager
    {
        private static readonly ILog log = LogManager.GetLogger("Polar.HabboHotel.Groups.GroupManager");

        public Dictionary<int, GroupBackGroundColours> BackGroundColours;
        public List<GroupBaseColours> BaseColours;
        public List<GroupBases> Bases;

        public Dictionary<int, GroupSymbolColours> SymbolColours;
        public List<GroupSymbols> Symbols;

        public static ConcurrentDictionary<int, Group> Jobs = new ConcurrentDictionary<int, Group>();
        public static ConcurrentDictionary<int, Group> Gangs = new ConcurrentDictionary<int, Group>();
        public static ConcurrentDictionary<int, GroupRank> GenericGangRanks = new ConcurrentDictionary<int, GroupRank>();

        public void Initialize()
        {
            GetGenericData();
            GetJobData();
            GetGangData();

            //log.Info("Cargados " + Jobs.Count + " Empresas y " + Gangs.Count + " Pandillas.");
            Out.WriteLine("Cargados " + Jobs.Count + " Empresas y " + Gangs.Count + " Pandillas.", "Polar.HabboHotel", ConsoleColor.DarkGray);
        }

        public void GetGenericData()
        {
            Bases = new List<GroupBases>();
            Symbols = new List<GroupSymbols>();
            BaseColours = new List<GroupBaseColours>();
            SymbolColours = new Dictionary<int, GroupSymbolColours>();
            BackGroundColours = new Dictionary<int, GroupBackGroundColours>();

            ClearGenericData();
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT * FROM `groups_items` WHERE `enabled` = '1'");
                DataTable dItems = dbClient.getTable();

                foreach (DataRow dRow in dItems.Rows)
                {
                    switch (dRow[0].ToString())
                    {
                        case "base":
                            Bases.Add(new GroupBases(Convert.ToInt32(dRow[1]), dRow[2].ToString(), dRow[3].ToString()));
                            break;

                        case "symbol":
                            Symbols.Add(new GroupSymbols(Convert.ToInt32(dRow[1]), dRow[2].ToString(), dRow[3].ToString()));
                            break;

                        case "color":
                            BaseColours.Add(new GroupBaseColours(Convert.ToInt32(dRow[1]), dRow[2].ToString()));
                            break;

                        case "color2":
                            SymbolColours.Add(Convert.ToInt32(dRow[1]), new GroupSymbolColours(Convert.ToInt32(dRow[1]), dRow[2].ToString()));
                            break;

                        case "color3":
                            BackGroundColours.Add(Convert.ToInt32(dRow[1]), new GroupBackGroundColours(Convert.ToInt32(dRow[1]), dRow[2].ToString()));
                            break;
                    }
                }
            }
        }

        public bool TryGetGroup(int Id, out Group Group)
        {
            Group = null;
            if (Id > 1000)
            {
                if (Gangs.ContainsKey(Id))
                    return Gangs.TryGetValue(Id, out Group);

                DataRow Row = null;
                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("SELECT * FROM `rp_gangs` WHERE `id` = @id LIMIT 1");
                    dbClient.AddParameter("id", Id);
                    Row = dbClient.getRow();
                    ConcurrentDictionary<int, GroupRank> Ranks = GenerateGangRanks();
                    List<int> Requests;
                    ConcurrentDictionary<int, GroupMember> Members = GenerateGangMembers(Id, out Requests);
                    if (Row != null)
                    {
                        int Idx = Convert.ToInt32(Row["id"]);
                        string Name = Row["name"].ToString();
                        string Description = Row["desc"].ToString();
                        string Badge = Row["badge"].ToString();
                        int OwnerId = Convert.ToInt32(Row["owner_id"]);
                        int Created = Convert.ToInt32(Row["created"]);
                        int RoomId = Convert.ToInt32(Row["room_id"]);
                        int State = Convert.ToInt32(Row["state"]);
                        string Colour1 = Convert.ToString(Row["colour1"]);
                        string Colour2 = Convert.ToString(Row["colour2"]);
                        int AdminOnlyDeco = Convert.ToInt32(Row["admindeco"]);

                        bool ForumEnabled = PolarEnvironment.EnumToBool(Row["forum_enabled"].ToString());
                        int ForumMessagesCount = Convert.ToInt32(Row["forum_messages_count"]);
                        double ForumScore = Convert.ToDouble(Row["forum_score"]);
                        int LastPosterId = Convert.ToInt32(Row["forum_lastposter_id"]);
                        string LastPosterName = PolarEnvironment.GetHabboById(LastPosterId) == null ? "HoloRP" : PolarEnvironment.GetHabboById(LastPosterId).Username;
                        int LastPosterTimeStamp = Convert.ToInt32(Row["forum_lastposter_timestamp"]);

                        int WhoCanRead = Convert.ToInt32(Row["who_can_read"]);
                        int WhoCanPost = Convert.ToInt32(Row["who_can_post"]);
                        int WhoCanThread = Convert.ToInt32(Row["who_can_thread"]);
                        int WhoCanMod = Convert.ToInt32(Row["who_can_mod"]);

                        int Kills = Convert.ToInt32(Row["gang_kills"]);
                        int CopKills = Convert.ToInt32(Row["gang_cop_kills"]);
                        int Deaths = Convert.ToInt32(Row["gang_deaths"]);
                        int Score = Convert.ToInt32(Row["gang_score"]);
                        int MediPacks = Convert.ToInt32(Row["medipacks"]);
                        int Stock = Convert.ToInt32(Row["stock"]);
                        bool hChat = PolarEnvironment.EnumToBool(Row["has_chat"].ToString());
                        int Balance = Convert.ToInt32(Row["bank_balance"]);
                        bool isGang = PolarEnvironment.EnumToBool(Row["isGang"].ToString());
                        bool Rupcy = PolarEnvironment.EnumToBool(Row["bankruptcy"].ToString());
                        int GangTurfTaken = Convert.ToInt32(Row["gang_turfs_taken"]);
                        int GangTurfDefend = Convert.ToInt32(Row["gang_turfs_defend"]);

                        ConcurrentDictionary<int, GroupLogs> Logs = GenerateLogs(Id);

                        Group = new Group(Id, Name, Description, Badge, RoomId, OwnerId, Created, State, Colour1, Colour2, AdminOnlyDeco,
                                                  ForumEnabled, Name, Description, ForumMessagesCount, ForumScore, LastPosterId, LastPosterName, LastPosterTimeStamp,
                                                  WhoCanMod, WhoCanPost, WhoCanRead, WhoCanThread, GenericGangRanks, Members, Requests, Kills, CopKills, Deaths, Score, GangTurfTaken, GangTurfDefend, MediPacks, hChat, Balance, Stock, isGang, Rupcy, Logs);
                        Gangs.TryAdd(Group.Id, Group);
                        return true;
                    }
                }
            }
            else if (Id < 1000)
            {
                if (Jobs.ContainsKey(Id))
                    return Jobs.TryGetValue(Id, out Group);

                DataRow Row = null;
                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("SELECT * FROM `rp_jobs` WHERE `id` = @id LIMIT 1");
                    dbClient.AddParameter("id", Id);
                    Row = dbClient.getRow();

                    int Idx = Convert.ToInt32(Row["id"]);
                    string Name = Row["name"].ToString();
                    string Description = Row["desc"].ToString();
                    string Badge = Row["badge"].ToString();
                    int OwnerId = Convert.ToInt32(Row["owner_id"]);
                    int Created = Convert.ToInt32(Row["created"]);
                    int RoomId = Convert.ToInt32(Row["room_id"]);
                    int State = Convert.ToInt32(Row["state"]);
                    string Colour1 = Convert.ToString(Row["colour1"]);
                    string Colour2 = Convert.ToString(Row["colour2"]);
                    int AdminOnlyDeco = Convert.ToInt32(Row["admindeco"]);

                    bool ForumEnabled = PolarEnvironment.EnumToBool(Row["forum_enabled"].ToString());
                    int ForumMessagesCount = Convert.ToInt32(Row["forum_messages_count"]);
                    double ForumScore = Convert.ToDouble(Row["forum_score"]);
                    int LastPosterId = Convert.ToInt32(Row["forum_lastposter_id"]);
                    string LastPosterName = PolarEnvironment.GetHabboById(LastPosterId) == null ? PolarEnvironment.GetConfig().data["hotel.name"] : PolarEnvironment.GetHabboById(LastPosterId).Username;
                    int LastPosterTimeStamp = Convert.ToInt32(Row["forum_lastposter_timestamp"]);

                    int WhoCanRead = Convert.ToInt32(Row["who_can_read"]);
                    int WhoCanPost = Convert.ToInt32(Row["who_can_post"]);
                    int WhoCanThread = Convert.ToInt32(Row["who_can_thread"]);
                    int WhoCanMod = Convert.ToInt32(Row["who_can_mod"]);
                    bool hChat = PolarEnvironment.EnumToBool(Row["has_chat"].ToString());
                    int Balance = Convert.ToInt32(Row["bank_balance"]);
                    int Stock = Convert.ToInt32(Row["stock"]);
                    bool isGang = PolarEnvironment.EnumToBool(Row["isGang"].ToString());


                    ConcurrentDictionary<int, GroupRank> Ranks = GenerateJobRanks(Id);

                    List<int> Requests;
                    ConcurrentDictionary<int, GroupMember> Members = GenerateJobMembers(Id, out Requests);
                    ConcurrentDictionary<int, GroupLogs> Logs = GenerateLogs(Id);

                    Group = new Group(Id, Name, Description, Badge, RoomId, OwnerId, Created, State, Colour1, Colour2, AdminOnlyDeco,
                                          ForumEnabled, Name, Description, ForumMessagesCount, ForumScore, LastPosterId, LastPosterName, LastPosterTimeStamp,
                                          WhoCanMod, WhoCanPost, WhoCanRead, WhoCanThread, Ranks, Members, Requests, 0, 0, 0, 0, 0, 0, 0, hChat, Balance, Stock, isGang, false, Logs);
                    Jobs.TryAdd(Group.Id, Group);
                    return true;
                }
            }
            return false;
        }
        public void ClearGenericData()
        {
            Bases.Clear();
            Symbols.Clear();
            BaseColours.Clear();
            SymbolColours.Clear();
            BackGroundColours.Clear();
        }

        public void GetJobData()
        {
            Jobs.Clear();

            using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT * FROM `rp_jobs`");
                DataTable Table = dbClient.getTable();

                if (Table != null)
                {
                    foreach (DataRow Row in Table.Rows)
                    {
                        int Id = Convert.ToInt32(Row["id"]);
                        string Name = Row["name"].ToString();
                        string Description = Row["desc"].ToString();
                        string Badge = Row["badge"].ToString();
                        int OwnerId = Convert.ToInt32(Row["owner_id"]);
                        int Created = Convert.ToInt32(Row["created"]);
                        int RoomId = Convert.ToInt32(Row["room_id"]);
                        int State = Convert.ToInt32(Row["state"]);
                        string Colour1 = Convert.ToString(Row["colour1"]);
                        string Colour2 = Convert.ToString(Row["colour2"]);
                        int AdminOnlyDeco = Convert.ToInt32(Row["admindeco"]);

                        bool ForumEnabled = PolarEnvironment.EnumToBool(Row["forum_enabled"].ToString());
                        int ForumMessagesCount = Convert.ToInt32(Row["forum_messages_count"]);
                        double ForumScore = Convert.ToDouble(Row["forum_score"]);
                        int LastPosterId = Convert.ToInt32(Row["forum_lastposter_id"]);
                        string LastPosterName = PolarEnvironment.GetHabboById(LastPosterId) == null ? PolarEnvironment.GetConfig().data["hotel.name"] : PolarEnvironment.GetHabboById(LastPosterId).Username;
                        int LastPosterTimeStamp = Convert.ToInt32(Row["forum_lastposter_timestamp"]);

                        int WhoCanRead = Convert.ToInt32(Row["who_can_read"]);
                        int WhoCanPost = Convert.ToInt32(Row["who_can_post"]);
                        int WhoCanThread = Convert.ToInt32(Row["who_can_thread"]);
                        int WhoCanMod = Convert.ToInt32(Row["who_can_mod"]);
                        bool hChat = PolarEnvironment.EnumToBool(Row["has_chat"].ToString());
                        int Balance = Convert.ToInt32(Row["bank_balance"]);
                        int Stock = Convert.ToInt32(Row["stock"]);
                        bool isGang = PolarEnvironment.EnumToBool(Row["isGang"].ToString());


                        ConcurrentDictionary<int, GroupRank> Ranks = GenerateJobRanks(Id);

                        List<int> Requests;

                        ConcurrentDictionary<int, GroupMember> Members = GenerateJobMembers(Id, out Requests);
                        ConcurrentDictionary<int, GroupLogs> Logs = GenerateLogs(Id);

                        Group Job = new Group(Id, Name, Description, Badge, RoomId, OwnerId, Created, State, Colour1, Colour2, AdminOnlyDeco,
                                              ForumEnabled, Name, Description, ForumMessagesCount, ForumScore, LastPosterId, LastPosterName, LastPosterTimeStamp,
                                              WhoCanMod, WhoCanPost, WhoCanRead, WhoCanThread, Ranks, Members, Requests, 0, 0, 0, 0, 0, 0, 0, hChat, Balance, Stock, isGang, false, Logs);

                        if (!Jobs.ContainsKey(Id))
                            Jobs.TryAdd(Id, Job);
                    }
                }
            }
        }

        public ICollection<Group> GangsG
        {
            get { return Gangs.Values; }
        }
        public List<Group> GetJobsForUser(int UserId)// Obtiene Grupos con Type 1 o 2
        {
            List<Group> Groups = new List<Group>();

            lock (Jobs)
            {
                if (Jobs.Values.Where(x => ((x.Members.ContainsKey(UserId) || x.CreatorId == UserId) && (x.GType < 3))).ToList().Count > 0)
                    Groups.Add(Jobs.Values.FirstOrDefault(x => ((x.Members.ContainsKey(UserId) || x.CreatorId == UserId) && (x.GType < 3))));
            }
            return Groups;
        }
        /*public List<Group> GetGangsForUser(int UserId) // Obtiene todos los grupos en general
        {
            List<Group> Groups = new List<Group>();

            lock (Gangs)
            {
                if (Gangs.Values.Where(x => ((x.Members.ContainsKey(UserId) || x.CreatorId == UserId) && (x.GType > 2))).ToList().Count > 0)
                    Groups.Add(Gangs.Values.FirstOrDefault(x => ((x.Members.ContainsKey(UserId) || x.CreatorId == UserId) && (x.GType > 2))));
            }
            return Groups;
        }*/

        public List<Group> GetGangsForUser(int UserId)
        {
            List<Group> Groups = new List<Group>();
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT g.id FROM `rp_stats` AS m RIGHT JOIN `rp_gangs` AS g ON m.gang_id = g.id WHERE m.id = @user");
                dbClient.AddParameter("user", UserId);
                DataTable GetGroups = dbClient.getTable();

                if (GetGroups != null)
                {
                    foreach (DataRow Row in GetGroups.Rows)
                    {
                        Group Group = null;
                        if (this.TryGetGroup(Convert.ToInt32(Row["id"]), out Group))
                            Groups.Add(Group);
                    }
                }
            }
            return Groups;
        }

        public List<Group> GetJobsForUserDict(int UserId)// Obtiene Grupos con Type 1 o 2
        {
            List<Group> Groups = new List<Group>();

            lock (Jobs)
            {
                if (Jobs.Values.Where(x => ((x.Members.ContainsKey(UserId) || x.CreatorId == UserId) && (x.GType < 3))).ToList().Count > 0)
                    Groups.Add(Jobs.Values.FirstOrDefault(x => ((x.Members.ContainsKey(UserId) || x.CreatorId == UserId) && (x.GType < 3))));
            }
            return Groups;
        }
        public ConcurrentDictionary<int, GroupRank> GenerateJobRanks(int Id)
        {
            ConcurrentDictionary<int, GroupRank> Ranks = new ConcurrentDictionary<int, GroupRank>();

            using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT * FROM `rp_jobs_ranks` WHERE `job` = '" + Id + "'");
                DataTable Table = dbClient.getTable();

                if (Table != null)
                {
                    foreach (DataRow Row in Table.Rows)
                    {
                        int JobRank = Convert.ToInt32(Row["rank"]);
                        string Name = Row["name"].ToString();
                        string MaleFigure = Row["male_figure"].ToString();
                        string FemaleFigure = Row["female_figure"].ToString();
                        int Pay = Convert.ToInt32(Row["pay"]);
                        string[] Commands = Row["commands"].ToString().Split(',');
                        string[] WorkRooms = Row["workrooms"].ToString().Split(',');
                        int Limit = Convert.ToInt32(Row["limit"]);

                        GroupRank Rank = new GroupRank(Id, JobRank, Name, MaleFigure, FemaleFigure, Pay, Commands, WorkRooms, Limit);

                        if (!Ranks.ContainsKey(JobRank))
                            Ranks.TryAdd(JobRank, Rank);
                    }
                }
            }
            return Ranks;
        }

        public ConcurrentDictionary<int, GroupMember> GenerateJobMembers(int Id, out List<int> Requests)
        {
            ConcurrentDictionary<int, GroupMember> Members = new ConcurrentDictionary<int, GroupMember>();
            Requests = new List<int>();

            using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT id,job_rank FROM `rp_stats` WHERE `job_id` = '" + Id + "'");
                DataTable Table = dbClient.getTable();

                if (Table != null)
                {
                    foreach (DataRow Row in Table.Rows)
                    {
                        int UserId = Convert.ToInt32(Row["id"]);
                        int Rank = Convert.ToInt32(Row["job_rank"]);
                        bool IsAdmin = Rank == 6;

                        GroupMember Member = new GroupMember(Id, UserId, Rank, IsAdmin);

                        if (!Members.ContainsKey(UserId))
                            Members.TryAdd(UserId, Member);
                    }
                }

                dbClient.SetQuery("SELECT `id` FROM `rp_stats` WHERE `job_request` = '" + Id + "'");
                DataTable RequestTable = dbClient.getTable();

                if (RequestTable != null)
                {
                    foreach (DataRow Row in RequestTable.Rows)
                    {
                        int UserId = Convert.ToInt32(Row["id"]);

                        if (!Requests.Contains(UserId))
                            Requests.Add(UserId);
                    }
                }
            }

            return Members;
        }

        public void GetGangData()
        {
            Gangs.Clear();
            GenericGangRanks.Clear();
            GenericGangRanks = GenerateGangRanks();

            using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT * FROM `rp_gangs`");
                DataTable Table = dbClient.getTable();

                if (Table != null)
                {
                    foreach (DataRow Row in Table.Rows)
                    {
                        int Id = Convert.ToInt32(Row["id"]);
                        string Name = Row["name"].ToString();
                        string Description = Row["desc"].ToString();
                        string Badge = Row["badge"].ToString();
                        int OwnerId = Convert.ToInt32(Row["owner_id"]);
                        int Created = Convert.ToInt32(Row["created"]);
                        int RoomId = Convert.ToInt32(Row["room_id"]);
                        int State = Convert.ToInt32(Row["state"]);
                        string Colour1 = Convert.ToString(Row["colour1"]);
                        string Colour2 = Convert.ToString(Row["colour2"]);
                        int AdminOnlyDeco = Convert.ToInt32(Row["admindeco"]);

                        bool ForumEnabled = PolarEnvironment.EnumToBool(Row["forum_enabled"].ToString());
                        int ForumMessagesCount = Convert.ToInt32(Row["forum_messages_count"]);
                        double ForumScore = Convert.ToDouble(Row["forum_score"]);
                        int LastPosterId = Convert.ToInt32(Row["forum_lastposter_id"]);
                        string LastPosterName = PolarEnvironment.GetHabboById(LastPosterId) == null ? "HoloRP" : PolarEnvironment.GetHabboById(LastPosterId).Username;
                        int LastPosterTimeStamp = Convert.ToInt32(Row["forum_lastposter_timestamp"]);

                        int WhoCanRead = Convert.ToInt32(Row["who_can_read"]);
                        int WhoCanPost = Convert.ToInt32(Row["who_can_post"]);
                        int WhoCanThread = Convert.ToInt32(Row["who_can_thread"]);
                        int WhoCanMod = Convert.ToInt32(Row["who_can_mod"]);

                        int Kills = Convert.ToInt32(Row["gang_kills"]);
                        int CopKills = Convert.ToInt32(Row["gang_cop_kills"]);
                        int Deaths = Convert.ToInt32(Row["gang_deaths"]);
                        int Score = Convert.ToInt32(Row["gang_score"]);
                        int MediPacks = Convert.ToInt32(Row["medipacks"]);
                        bool hChat = PolarEnvironment.EnumToBool(Row["has_chat"].ToString());
                        int Balance = Convert.ToInt32(Row["bank_balance"]);
                        int Stock = Convert.ToInt32(Row["stock"]);
                        bool isGang = PolarEnvironment.EnumToBool(Row["isGang"].ToString());
                        bool Rupcy = PolarEnvironment.EnumToBool(Row["bankruptcy"].ToString());
                        int GangTurfTaken = Convert.ToInt32(Row["gang_turfs_taken"]);
                        int GangTurfDefend = Convert.ToInt32(Row["gang_turfs_defend"]);

                        List<int> Requests;
                        ConcurrentDictionary<int, GroupMember> Members = GenerateGangMembers(Id, out Requests);
                        ConcurrentDictionary<int, GroupLogs> Logs = GenerateLogs(Id);

                        Group Gang = new Group(Id, Name, Description, Badge, RoomId, OwnerId, Created, State, Colour1, Colour2, AdminOnlyDeco,
                                              ForumEnabled, Name, Description, ForumMessagesCount, ForumScore, LastPosterId, LastPosterName, LastPosterTimeStamp,
                                              WhoCanMod, WhoCanPost, WhoCanRead, WhoCanThread, GenericGangRanks, Members, Requests, Kills, CopKills, Deaths, Score, GangTurfTaken, GangTurfDefend, MediPacks, hChat, Balance, Stock, isGang, Rupcy, Logs);

                        if (!Gangs.ContainsKey(Id))
                            Gangs.TryAdd(Id, Gang);
                    }
                }
            }
        }

        public ConcurrentDictionary<int, GroupRank> GenerateGangRanks()
        {
            ConcurrentDictionary<int, GroupRank> Ranks = new ConcurrentDictionary<int, GroupRank>();

            using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT * FROM `rp_gangs_ranks` WHERE `gang` = '1000'");
                DataTable Table = dbClient.getTable();

                if (Table != null)
                {
                    foreach (DataRow Row in Table.Rows)
                    {
                        int GangRank = Convert.ToInt32(Row["rank"]);
                        string Name = Row["name"].ToString();
                        string MaleFigure = "";
                        string FemaleFigure = "";
                        int Pay = 0;
                        string[] Commands = Row["commands"].ToString().Split(',');
                        string[] WorkRooms = "".Split(',');
                        int Limit = Convert.ToInt32(Row["limit"]);

                        GroupRank Rank = new GroupRank(1000, GangRank, Name, MaleFigure, FemaleFigure, Pay, Commands, WorkRooms, Limit);

                        if (!Ranks.ContainsKey(GangRank))
                            Ranks.TryAdd(GangRank, Rank);
                    }
                }
            }
            return Ranks;
        }

        public ConcurrentDictionary<int, GroupMember> GenerateGangMembers(int Id, out List<int> Requests)
        {
            ConcurrentDictionary<int, GroupMember> Members = new ConcurrentDictionary<int, GroupMember>();
            Requests = new List<int>();

            using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT id,gang_rank FROM `rp_stats` WHERE `gang_id` = '" + Id + "'");
                DataTable Table = dbClient.getTable();

                if (Table != null)
                {
                    foreach (DataRow Row in Table.Rows)
                    {
                        int UserId = Convert.ToInt32(Row["id"]);
                        int Rank = Convert.ToInt32(Row["gang_rank"]);
                        bool IsAdmin = Rank >= 5;

                        GroupMember Member = new GroupMember(Id, UserId, Rank, IsAdmin);

                        if (!Members.ContainsKey(UserId))
                            Members.TryAdd(UserId, Member);
                    }
                }

                dbClient.SetQuery("SELECT `id` FROM `rp_stats` WHERE `gang_request` = '" + Id + "'");
                DataTable RequestTable = dbClient.getTable();

                if (RequestTable != null)
                {
                    foreach (DataRow Row in RequestTable.Rows)
                    {
                        int UserId = Convert.ToInt32(Row["id"]);

                        if (!Requests.Contains(UserId))
                            Requests.Add(UserId);
                    }
                }
            }

            return Members;
        }

        public ConcurrentDictionary<int, GroupLogs> GenerateLogs(int Id)
        {
            ConcurrentDictionary<int, GroupLogs> Logs = new ConcurrentDictionary<int, GroupLogs>();

            using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT * FROM `groups_logs` WHERE `group_id` = '" + Id + "' ORDER BY timestamp DESC LIMIT 30");
                DataTable Table = dbClient.getTable();
                int c = 0;
                if (Table != null)
                {
                    foreach (DataRow Row in Table.Rows)
                    {
                        GroupLogs mLogs = new GroupLogs(Id, Convert.ToInt32(Row["user_id"]), Convert.ToString(Row["action"]), Convert.ToInt32(Row["cant"]), PolarEnvironment.UnixTimeStampToDateTime(Convert.ToDouble(Row["timestamp"])));

                        if (!Logs.ContainsKey(c))
                            Logs.TryAdd(c, mLogs);
                        c++;
                    }
                }
            }

            return Logs;
        }
        public static bool JobExists(int JobId, int RankId)
        {
            Group Job = GetJob(JobId);

            if (Job != null && Job.Ranks.ContainsKey(RankId))
                return true;

            return false;
        }

        public static Group GetJob(int Id)
        {
            if (Jobs.ContainsKey(Id))
                return Jobs[Id];

            return null;
        }

        public static bool validJustJob(int JobId)
        {
            return Jobs.ContainsKey(JobId) ? true : false;
        }

        public static Group GetJobByName(string Name)
        {
            if (Jobs.Values.Where(x => x.Name.ToLower() == Name.ToLower()).ToList().Count > 0)
                return Jobs.Values.FirstOrDefault(x => x.Name.ToLower() == Name.ToLower());

            return null;
        }

        public static Group GetGang(int Id)
        {
            if (Gangs.ContainsKey(Id))
                return Gangs[Id];

            return null;
        }

        public static bool validJustGang(int GangId)
        {
            return Gangs.ContainsKey(GangId) ? true : false;
        }

        public static bool GangExists(int tGangId, int RankId)
        {
            Group Gang = GetGang(tGangId);

            if (Gang != null && Gang.Ranks.ContainsKey(RankId))
                return true;

            return false;
        }

        public static List<GroupMember> GetGangMembersByRank(int GangId, int RankId)
        {
            Group Gang = GetGang(GangId);

            if (Gang != null && Gang.Ranks.ContainsKey(RankId))
                return Gang.Members.Values.Where(x => x.UserRank == RankId).ToList();

            return null;
        }

        public static GroupRank GetJobRank(int JobId, int RankId)
        {
            Group Group = GetJob(JobId);

            if (Group != null && Group.Ranks.ContainsKey(RankId))
                return Group.Ranks[RankId];

            return null;
        }

        public static GroupRank GetGangRank(int GangId, int RankId)
        {
            Group Group = GetGang(GangId);

            if (Group != null && Group.Ranks.ContainsKey(RankId))
                return Group.Ranks[RankId];

            return null;
        }

        public static int GetMessageCountForThread(int id)
        {
            using (var queryReactor = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                queryReactor.SetQuery(string.Format("SELECT COUNT(*) FROM groups_forums_posts WHERE parent_id='{0}'", id));
                return int.Parse(queryReactor.getString());
            }
        }

        public static bool HasJobCommand(GameClient Session, string command)
        {
            if (Session == null || Session.GetRoleplay() == null)
                return false;

            int JobId = Session.GetRoleplay().JobId;
            int JobRank = Session.GetRoleplay().JobRank;

            if (JobId == 1)
                return false;

            Group Job = GetJob(JobId);
            GroupRank Rank = GetJobRank(JobId, JobRank);

            if (Job == null || Rank == null)
                return false;

            if (!Rank.HasCommand(command))
                return false;

            return true;
        }

        public static bool HasGangCommand(GameClient Session, string command)
        {
            if (Session == null || Session.GetRoleplay() == null)
                return false;

            int GangId = Session.GetRoleplay().GangId;
            int GangRank = Session.GetRoleplay().GangRank;

            if (GangId == 1000)
                return false;

            Group Gang = GetGang(GangId);
            GroupRank Rank = GetGangRank(GangId, GangRank);

            if (Gang == null || Rank == null)
                return false;

            if (!Rank.HasCommand(command))
                return false;

            return true;
        }

        public bool TryCreateGroup(Habbo Player, string Name, string Description, int RoomId, string Badge, string Colour1, string Colour2, out Group Group)
        {
            Group = new Group(0, Name, Description, Badge, RoomId, Player.Id, (int)PolarEnvironment.GetUnixTimestamp(), 1, Colour1, Colour2, 0,
                false, Name, Description, 0, 0, 0, "", 0, 1, 2, 2, 3, GenericGangRanks, new ConcurrentDictionary<int, GroupMember>(), new List<int>(), 0, 0, 0, 0, 0, 0, 0, false, 0, 0, true, false, new ConcurrentDictionary<int, GroupLogs>());

            try
            {
                if (Gangs.Values.Where(x => x.CreatorId == Player.Id).ToList().Count > 0)
                    return false;

                if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Badge))
                    return false;

                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("INSERT INTO `rp_gangs` (`name`, `desc`, `badge`, `owner_id`, `created`, `room_id`, `state`, `colour1`, `colour2`, `admindeco`, `bank_balance`) VALUES (@name, @desc, @badge, @owner, UNIX_TIMESTAMP(), @room, '0', @colour1, @colour2, '0', @bank)");
                    dbClient.AddParameter("name", Group.Name);
                    dbClient.AddParameter("desc", Group.Description);
                    dbClient.AddParameter("owner", Group.CreatorId);
                    dbClient.AddParameter("badge", Group.Badge);
                    dbClient.AddParameter("room", Group.RoomId);
                    dbClient.AddParameter("colour1", Group.Colour1);
                    dbClient.AddParameter("colour2", Group.Colour2);
                    dbClient.AddParameter("bank", (RoleplayManager.GangsPrice / 4));
                    Group.Id = Convert.ToInt32(dbClient.InsertQuery());
                    Group.Balance = (RoleplayManager.GangsPrice / 4);

                    if (Gangs.ContainsKey(Group.Id))
                        return false;

                    Gangs.TryAdd(Group.Id, Group);

                    Player.GetClient().GetRoleplay().GangId = Group.Id;
                    Player.GetClient().GetRoleplay().GangRank = 6;
                    Player.GetClient().GetRoleplay().GangRequest = 0;

                    Group.AddNewMember(Player.Id, 6, true);
                    Group.MakeAdmin(Player.Id);

                    UserCache Junk;
                    PolarEnvironment.GetGame().GetCacheManager().TryRemoveUser(Player.Id, out Junk);
                    PolarEnvironment.GetGame().GetCacheManager().GenerateUser(Player.Id);
                }
            }
            catch (Exception e)
            {
                log.Info(e.Message);
                return false;
            }

            return true;
        }

        public string CheckActiveSymbol(string Symbol)
        {
            if (Symbol == "s000" || Symbol == "s00000")
            {
                return "";
            }
            return Symbol;
        }

        public string GetGroupColour(int Index, bool Colour1)
        {
            if (Colour1)
            {
                if (SymbolColours.ContainsKey(Index))
                {
                    return SymbolColours[Index].Colour;
                }
            }
            else
            {
                if (BackGroundColours.ContainsKey(Index))
                {
                    return BackGroundColours[Index].Colour;
                }
            }

            return "4f8a00";
        }

        public Group GetJobByID(int CorpId)
        {
            if (Jobs.Values.Where(x => x.Id == CorpId).ToList().Count > 0)
                return Jobs.Values.FirstOrDefault(x => x.Id == CorpId);

            return null;
        }

        public static GroupRank GetGroupRank(int JobId, int RankId)
        {
            Group Group = null;
            PolarEnvironment.GetGame().GetGroupManager().TryGetGroup(JobId, out Group);

            if (Group != null && Group.Ranks.ContainsKey(RankId))
                return Group.Ranks[RankId];

            return null;
        }
        public void DeleteGroupRank(int JobId, int RankId)
        {
            Group Group = GetJob(JobId);

            if (Group != null && Group.Ranks.Where(x => x.Value.RankId == RankId).Count() > 0)
            {
                GroupRank Junk;
                //Group.Ranks.TryRemove(RankId, out Junk);
                var itemtoremove = Group.Ranks.Where(item => item.Value.RankId == RankId).First();
                Group.Ranks.TryRemove(itemtoremove.Key, out Junk);

                // Refresh the dictionary for the new keys
                Group.Ranks = GenerateJobRanks(JobId);
            }
        }
        // ─── Recarga desde base de datos ──────────────────────────────────────

        /// <summary>
        /// Recarga un grupo/pandilla concreto desde la DB y actualiza el diccionario en memoria.
        /// Devuelve el Group actualizado, o null si no existe en DB.
        /// </summary>
        public Group ReloadGroup(int groupId)
        {
            try
            {
                // Determinar si es job o gang
                bool isGang = groupId >= 1000;
                string table = isGang ? "rp_gangs" : "rp_jobs";

                DataRow Row = null;
                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("SELECT * FROM `" + table + "` WHERE `id` = @id LIMIT 1");
                    dbClient.AddParameter("id", groupId);
                    Row = dbClient.getRow();
                }

                if (Row == null)
                {
                    // No existe en DB → eliminar del cache si estaba
                    if (isGang)
                    {
                        Group old; Gangs.TryRemove(groupId, out old);
                        old?.Dispose();
                    }
                    else
                    {
                        Group old; Jobs.TryRemove(groupId, out old);
                        old?.Dispose();
                    }
                    return null;
                }

                // Si ya está cacheado, hacer refresh in-place (más rápido, mantiene referencias)
                Group existing = isGang
                    ? (Gangs.ContainsKey(groupId) ? Gangs[groupId] : null)
                    : (Jobs.ContainsKey(groupId)  ? Jobs[groupId]  : null);

                if (existing != null)
                {
                    existing.RefreshFromDatabase();
                    Out.WriteLine("Grupo #" + groupId + " '" + existing.Name + "' recargado (in-place).", "GroupManager", ConsoleColor.Cyan);
                    return existing;
                }

                // No estaba en cache → construirlo completo y añadirlo
                Group rebuilt = BuildGroupFromRow(Row, groupId, isGang);
                if (rebuilt != null)
                {
                    if (isGang) Gangs.TryAdd(groupId, rebuilt);
                    else        Jobs.TryAdd(groupId, rebuilt);
                    Out.WriteLine("Grupo #" + groupId + " '" + rebuilt.Name + "' cargado al cache.", "GroupManager", ConsoleColor.Cyan);
                }
                return rebuilt;
            }
            catch (Exception ex)
            {
                log.Error("[GroupManager.ReloadGroup] Id=" + groupId + " -> " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Recarga todos los Jobs desde la DB (equivalente a GetJobData pero sin limpiar Gangs).
        /// </summary>
        public void ReloadAllJobs()
        {
            try
            {
                Out.WriteLine("Recargando todos los Jobs...", "GroupManager", ConsoleColor.Cyan);
                GetJobData();
                Out.WriteLine("Jobs recargados: " + Jobs.Count, "GroupManager", ConsoleColor.Green);
            }
            catch (Exception ex)
            {
                log.Error("[GroupManager.ReloadAllJobs] " + ex.Message);
            }
        }

        /// <summary>
        /// Recarga todas las Pandillas desde la DB (equivalente a GetGangData).
        /// </summary>
        public void ReloadAllGangs()
        {
            try
            {
                Out.WriteLine("Recargando todas las Pandillas...", "GroupManager", ConsoleColor.Cyan);
                GetGangData();
                Out.WriteLine("Pandillas recargadas: " + Gangs.Count, "GroupManager", ConsoleColor.Green);
            }
            catch (Exception ex)
            {
                log.Error("[GroupManager.ReloadAllGangs] " + ex.Message);
            }
        }

        /// <summary>
        /// Recarga absolutamente todo (GenericData + Jobs + Gangs).
        /// Equivalente a llamar Initialize() de nuevo en caliente.
        /// </summary>
        public void ReloadAll()
        {
            try
            {
                Out.WriteLine("Recargando todos los grupos y pandillas...", "GroupManager", ConsoleColor.Yellow);
                Initialize();
                Out.WriteLine("Recarga completa: " + Jobs.Count + " Jobs, " + Gangs.Count + " Pandillas.", "GroupManager", ConsoleColor.Green);
            }
            catch (Exception ex)
            {
                log.Error("[GroupManager.ReloadAll] " + ex.Message);
            }
        }

        /// <summary>
        /// Helper interno: construye un Group completo desde un DataRow de rp_jobs o rp_gangs.
        /// </summary>
        private Group BuildGroupFromRow(DataRow Row, int Id, bool isGang)
        {
            string Name        = Row["name"].ToString();
            string Description = Row["desc"].ToString();
            string Badge       = Row["badge"].ToString();
            int OwnerId        = Convert.ToInt32(Row["owner_id"]);
            int Created        = Convert.ToInt32(Row["created"]);
            int RoomId         = Convert.ToInt32(Row["room_id"]);
            int State          = Convert.ToInt32(Row["state"]);
            string Colour1     = Convert.ToString(Row["colour1"]);
            string Colour2     = Convert.ToString(Row["colour2"]);
            int AdminOnlyDeco  = Convert.ToInt32(Row["admindeco"]);

            bool ForumEnabled      = PolarEnvironment.EnumToBool(Row["forum_enabled"].ToString());
            int ForumMessagesCount = Convert.ToInt32(Row["forum_messages_count"]);
            double ForumScore      = Convert.ToDouble(Row["forum_score"]);
            int LastPosterId       = Convert.ToInt32(Row["forum_lastposter_id"]);
            string LastPosterName  = PolarEnvironment.GetHabboById(LastPosterId) == null
                                        ? "HoloRP"
                                        : PolarEnvironment.GetHabboById(LastPosterId).Username;
            int LastPosterTimeStamp = Convert.ToInt32(Row["forum_lastposter_timestamp"]);

            int WhoCanRead   = Convert.ToInt32(Row["who_can_read"]);
            int WhoCanPost   = Convert.ToInt32(Row["who_can_post"]);
            int WhoCanThread = Convert.ToInt32(Row["who_can_thread"]);
            int WhoCanMod    = Convert.ToInt32(Row["who_can_mod"]);

            bool hChat   = PolarEnvironment.EnumToBool(Row["has_chat"].ToString());
            int Balance  = Convert.ToInt32(Row["bank_balance"]);
            int Stock    = Convert.ToInt32(Row["stock"]);
            bool isGangB = PolarEnvironment.EnumToBool(Row["isGang"].ToString());

            List<int> Requests;
            ConcurrentDictionary<int, GroupMember> Members;
            ConcurrentDictionary<int, GroupRank>   Ranks;
            ConcurrentDictionary<int, GroupLogs>   Logs = GenerateLogs(Id);

            int Kills = 0, CopKills = 0, Deaths = 0, Score = 0, MediPacks = 0, Stock2 = Stock;
            int GangTurfTaken = 0, GangTurfDefend = 0;
            bool Rupcy = false;

            if (isGang)
            {
                Kills         = Convert.ToInt32(Row["gang_kills"]);
                CopKills      = Convert.ToInt32(Row["gang_cop_kills"]);
                Deaths        = Convert.ToInt32(Row["gang_deaths"]);
                Score         = Convert.ToInt32(Row["gang_score"]);
                MediPacks     = Convert.ToInt32(Row["medipacks"]);
                Rupcy         = PolarEnvironment.EnumToBool(Row["bankruptcy"].ToString());
                GangTurfTaken = Convert.ToInt32(Row["gang_turfs_taken"]);
                GangTurfDefend= Convert.ToInt32(Row["gang_turfs_defend"]);

                Members = GenerateGangMembers(Id, out Requests);
                Ranks   = GenericGangRanks;
            }
            else
            {
                Members = GenerateJobMembers(Id, out Requests);
                Ranks   = GenerateJobRanks(Id);
            }

            return new Group(Id, Name, Description, Badge, RoomId, OwnerId, Created, State, Colour1, Colour2, AdminOnlyDeco,
                             ForumEnabled, Name, Description, ForumMessagesCount, ForumScore, LastPosterId, LastPosterName, LastPosterTimeStamp,
                             WhoCanMod, WhoCanPost, WhoCanRead, WhoCanThread, Ranks, Members, Requests,
                             Kills, CopKills, Deaths, Score, GangTurfTaken, GangTurfDefend, MediPacks,
                             hChat, Balance, Stock, isGangB, Rupcy, Logs);
        }

        // ─────────────────────────────────────────────────────────────────────

        public void DeleteGroup(int Id)
        {
            Group Group = null;
            if (Gangs.ContainsKey(Id))
                Gangs.TryRemove(Id, out Group);

            if (Group != null)
                Group.Dispose();
        }

        public List<Group> GetGroupsForUser(int UserId)
        {
            List<Group> Groups = new List<Group>();

            lock (Jobs)
            {
                if (Jobs.Values.Where(x => x.Members.ContainsKey(UserId) || x.CreatorId == UserId).ToList().Count > 0)
                    Groups.Add(Jobs.Values.FirstOrDefault(x => x.Members.ContainsKey(UserId) || x.CreatorId == UserId));
            }

            lock (Gangs)
            {
                if (Gangs.Values.Where(x => x.Members.ContainsKey(UserId) || x.CreatorId == UserId).ToList().Count > 0)
                    Groups.Add(Gangs.Values.FirstOrDefault(x => x.Members.ContainsKey(UserId) || x.CreatorId == UserId));
            }

            return Groups;
        }
    }
}