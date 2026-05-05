using Polar.Communication.Interfaces;
using Polar.Communication.Packets.Outgoing;
using Polar.Communication.Packets.Outgoing.QuickPolls;
using Polar.Communication.Packets.Outgoing.Rooms.Avatar;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.Communication.Packets.Outgoing.Rooms.Furni.Wired;
using Polar.Communication.Packets.Outgoing.Rooms.Session;
using Polar.Core;
using Polar.Database.Interfaces;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Items.Data.Moodlight;
using Polar.HabboHotel.Items.Data.RentableSpace;
using Polar.HabboHotel.Items.Data.Toner;
using Polar.HabboHotel.Rooms.AI;
using Polar.HabboHotel.Rooms.AI.Speech;
using Polar.HabboHotel.Rooms.Games;
using Polar.HabboHotel.Rooms.Games.Banzai;
using Polar.HabboHotel.Rooms.Games.Football;
using Polar.HabboHotel.Rooms.Games.Freeze;
using Polar.HabboHotel.Rooms.Games.Teams;
using Polar.HabboHotel.Rooms.Instance;
using Polar.HabboHotel.Rooms.TraxMachine;
using Polar.HabboHotel.Users;
using Polar.HabboRoleplay.Bots.Manager;
using Polar.HabboRoleplay.Houses;
using Polar.HabboRoleplay.Misc;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Data;
using System.Net.Sockets;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Polar.HabboHotel.Rooms
{
    public class Room : RoomData, IDisposable
    {
        // ✅ FIX #1: Eliminado _cancellationTokenSource duplicado.
        //           Solo existe _mainProcessSource para controlar el loop de proceso.
        private CancellationTokenSource _mainProcessSource;

        public bool isCrashed;
        public bool mDisposed;
        public bool RoomMuted;
        public DateTime lastTimerReset;
        public DateTime lastRegeneration;
        public delegate void FurnisLoaded();

        public Task ProcessTask;
        public List<Trade> ActiveTrades { get; set; }
        private RoomTraxManager _traxManager;
        public TonerData TonerData;
        public MoodlightData MoodlightData;
        public int wiredInspectMask = WIRED_ACCESS_DEFAULT_INSPECT_MASK;
        public int wiredModifyMask = WIRED_ACCESS_DEFAULT_MODIFY_MASK;
        public static int WIRED_ACCESS_EVERYONE = 1;
        public static int WIRED_ACCESS_USERS_WITH_RIGHTS = 2;
        public static int WIRED_ACCESS_GROUP_MEMBERS = 4;
        public static int WIRED_ACCESS_GROUP_ADMINS = 8;
        public static int WIRED_ACCESS_ALLOWED_INSPECT_MASK = WIRED_ACCESS_EVERYONE | WIRED_ACCESS_USERS_WITH_RIGHTS | WIRED_ACCESS_GROUP_MEMBERS | WIRED_ACCESS_GROUP_ADMINS;
        public static int WIRED_ACCESS_ALLOWED_MODIFY_MASK = WIRED_ACCESS_USERS_WITH_RIGHTS | WIRED_ACCESS_GROUP_MEMBERS | WIRED_ACCESS_GROUP_ADMINS;
        public static int WIRED_ACCESS_DEFAULT_INSPECT_MASK = 0;
        public static int WIRED_ACCESS_DEFAULT_MODIFY_MASK = 0;
        public Dictionary<int, double> Bans;
        public Dictionary<int, double> MutedUsers;

        private readonly TimeSpan _saveFurnitureTimer = TimeSpan.FromMinutes(2);
        private DateTime _saveFurnitureTimerLast = DateTime.Now;

        private Dictionary<int, List<RoomUser>> Tents;
        private Task _processTask;
        public List<int> UsersWithRights;
        private GameManager _gameManager;
        private Freeze _freeze;
        private Soccer _soccer;
        private BattleBanzai _banzai;
        private object wiredSettingsLock = new object();
        private Gamemap _gamemap;
        private GameItemHandler _gameItemHandler;
        private RoomData _roomData;
        public TeamManager teambanzai;
        public TeamManager teamfreeze;

        private RoomUserManager _roomUserManager;
        private RoomItemHandling _roomItemHandling;
        private volatile bool wiredSettingsLoaded;
        private List<string> _wordFilterList;
        private FilterComponent _filterComponent;
        private WiredComponent _wiredComponent;

        public bool mCycleEnded { get; set; }
        internal string poolQuestion;
        internal List<int> yesPoolAnswers;
        internal List<int> noPoolAnswers;
        public int IsLagging { get; set; }
        public int IdleTime { get; set; }
        public ConcurrentDictionary<string, string> WiredVariables;
        private bool _hideWired;
        private bool _gamblingRoom;
        public bool DiscoMode;
        
        public Room(RoomData Data)
        {
            _mainProcessSource = new CancellationTokenSource();
            IsLagging = 0;
            this.IdleTime = 0;
            this._roomData = Data;
            RoomMuted = false;
            mDisposed = false;
            this.mCycleEnded = false;
            this.Id = Data.Id;
            this.Name = Data.Name;
            this.Description = Data.Description;
            this.OwnerName = Data.OwnerName;
            this.OwnerId = Data.OwnerId;
            this.WiredScoreBordDay = Data.WiredScoreBordDay;
            this.WiredScoreBordWeek = Data.WiredScoreBordWeek;
            this.WiredScoreBordMonth = Data.WiredScoreBordMonth;
            this.WiredScoreFirstBordInformation = Data.WiredScoreFirstBordInformation;
            this.Category = Data.Category;
            this.Type = Data.Type;
            this.State = Data.State;
            this.UsersNow = 0;
            this.City = Data.City;
            this.UsersMax = Data.UsersMax;
            this.ModelName = Data.ModelName;
            this.Score = Data.Score;
            this.Tags = new List<string>();
            foreach (string tag in Data.Tags) Tags.Add(tag);
            this.AllowPets = Data.AllowPets;
            this.AllowPetsEating = Data.AllowPetsEating;
            this.RoomBlockingEnabled = Data.RoomBlockingEnabled;
            this.Hidewall = Data.Hidewall;
            this.Group = Data.Group;
            this.GroupId = Data.GroupId;
            this.Password = Data.Password;
            this.Wallpaper = Data.Wallpaper;
            this.Floor = Data.Floor;
            this.Landscape = Data.Landscape;
            this._hideWired = Data.HideWired;
            this.WallThickness = Data.WallThickness;
            this.FloorThickness = Data.FloorThickness;
            this.chatMode = Data.chatMode;
            this.chatSize = Data.chatSize;
            this.chatSpeed = Data.chatSpeed;
            this.chatDistance = Data.chatDistance;
            this.extraFlood = Data.extraFlood;
            this.TradeSettings = Data.TradeSettings;
            this.WhoCanBan = Data.WhoCanBan;
            this.WhoCanKick = Data.WhoCanKick;
            this.PushEnabled = Data.PushEnabled;
            this.PullEnabled = Data.PullEnabled;
            this.SPullEnabled = Data.SPullEnabled;
            this.SPushEnabled = Data.SPushEnabled;
            this.EnablesEnabled = Data.EnablesEnabled;
            this.RespectNotificationsEnabled = Data.RespectNotificationsEnabled;
            this.PetMorphsAllowed = Data.PetMorphsAllowed;
            this.poolQuestion = string.Empty;
            this.yesPoolAnswers = new List<int>();
            this.noPoolAnswers = new List<int>();
            this.WiredVariables = new ConcurrentDictionary<string, string>();
            this.WardrobeEnabled = Data.WardrobeEnabled;
            this.PhoneStoreEnabled = Data.PhoneStoreEnabled;
            this.MallEnabled = Data.MallEnabled;
            this.SupermarketEnabled = Data.SupermarketEnabled;
            this.HuntZoneEnabled = Data.HuntZoneEnabled;
            this.BuyCarEnabled = Data.BuyCarEnabled;
            this.BankEnabled = Data.BankEnabled;
            this.ShootEnabled = Data.ShootEnabled;
            this.HitEnabled = Data.HitEnabled;
            this.SafeZoneEnabled = Data.SafeZoneEnabled;
            this.LearningEnabled = Data.LearningEnabled;
            this.SexCommandsEnabled = Data.SexCommandsEnabled;
            this.TurfEnabled = Data.TurfEnabled;
            this.RobEnabled = Data.RobEnabled;
            this.GymEnabled = Data.GymEnabled;
            this.DeliveryEnabled = Data.DeliveryEnabled;
            this.TutorialEnabled = Data.TutorialEnabled;
            this.DriveEnabled = Data.DriveEnabled;
            this.TaxiFromEnabled = Data.TaxiFromEnabled;
            this.TaxiToEnabled = Data.TaxiToEnabled;
            this.BusToEnabled = Data.BusToEnabled;
            this.EnterRoomMessage = Data.EnterRoomMessage;
            this.IsHospital = Data.IsHospital;
            this.IsPrison = Data.IsPrison;
            this.IsPrison2 = Data.IsPrison2;
            this.IsPolStation = Data.IsPolStation;
            this.IsCamionero = Data.IsCamionero;
            this.IsMecanico = Data.IsMecanico;
            this.IsBasurero = Data.IsBasurero;
            this.ActiveTrades = new List<Trade>();
            this.Bans = new Dictionary<int, double>();
            this.MutedUsers = new Dictionary<int, double>();
            this.Tents = new Dictionary<int, List<RoomUser>>();

            _gamemap = new Gamemap(this);
            if (_roomItemHandling == null)
                _roomItemHandling = new RoomItemHandling(this);
            _roomUserManager = new RoomUserManager(this);
            _filterComponent = new FilterComponent(this);
            _wiredComponent = new WiredComponent(this);
            this._traxManager = new RoomTraxManager(this);

            GetRoomItemHandler().LoadFurniture();
            GetGameMap().GenerateMaps();
            this.LoadPromotions();
            this.LoadRights();
            this.LoadBans();
            this.LoadFilter();
            this.InitBots();

            if (RoleplayBotManager.isInit == false)
                RoleplayBotManager.Initialize(false);

            this.InitPets();

            if (this.GetRoomUserManager() != null && this.GetRoomUserManager().GetRoomUsers() != null)
                Data.UsersNow = this.GetRoomUserManager().GetRoomUsers().Where(x => !x.IsBot).Count();
            else
                Data.UsersNow = 0;

            StartRoomProcessing();
        }

        internal void StartRoomProcessing()
        {
            if (_mainProcessSource == null || _mainProcessSource.IsCancellationRequested)
                _mainProcessSource = new CancellationTokenSource();

            _processTask = Task.Run(async () =>
            {
                while (!_mainProcessSource.IsCancellationRequested)
                {
                    try
                    {
                        if (mDisposed || _roomUserManager == null || _roomItemHandling == null)
                        {
                            await Task.Delay(480, _mainProcessSource.Token);
                            continue;
                        }

                        // ✅ FIX WALK-1: Usar Stopwatch en lugar de GetIUnixTimestamp().
                        //   GetIUnixTimestamp() tiene resolución de segundos enteros — al restar
                        //   siempre da 0 o 1, lo que hace que el wait sea siempre exactamente
                        //   targetCycleMs sin compensar el tiempo real de ProcessRoom.
                        //   Stopwatch usa QueryPerformanceCounter y tiene resolución de ~100ns.
                        var sw = System.Diagnostics.Stopwatch.StartNew();

                        await ProcessRoom();

                        sw.Stop();

                        int userCount = 0;
                        try { userCount = _roomUserManager?.GetUserList()?.Count ?? 0; }
                        catch { userCount = 0; }

                        // 500ms = 2 ticks/seg. Es más estable que 460ms porque deja más margen
                        // para que Task.Delay (resolución ~15ms en Windows) no acumule deriva.
                        int targetCycleMs = userCount == 0 ? 2000 : 480;
                        int wait = Math.Max(0, targetCycleMs - (int)sw.ElapsedMilliseconds);

                        await Task.Delay(wait, _mainProcessSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception e)
                    {
                        Logging.HandleException(e, "RoomProcessing");
                        await Task.Delay(1000);
                    }
                }
            }, _mainProcessSource.Token);
        }

        public List<string> WordFilterList
        {
            get { return this._wordFilterList; }
            set { this._wordFilterList = value; }
        }

        public bool GamblingRoom
        {
            get { return this._gamblingRoom; }
            set { this._gamblingRoom = value; }
        }

        #region Room Bans

        public bool UserIsBanned(int pId) => Bans.ContainsKey(pId);

        public void RemoveBan(int pId) => Bans.Remove(pId);

        public void AddBan(int pId, long Time)
        {
            if (!Bans.ContainsKey(pId))
                Bans.Add(pId, PolarEnvironment.GetUnixTimestamp() + Time);

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                // ✅ FIX #3: Parámetros en lugar de concatenación directa (SQL injection)
                dbClient.SetQuery("REPLACE INTO `room_bans` VALUES (@userId, @roomId, @expire)");
                dbClient.AddParameter("userId", pId);
                dbClient.AddParameter("roomId", Id);
                dbClient.AddParameter("expire", PolarEnvironment.GetUnixTimestamp() + Time);
                dbClient.RunQuery();
            }
        }

        public List<int> BannedUsers()
        {
            var result = new List<int>();
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT user_id FROM room_bans WHERE expire > UNIX_TIMESTAMP() AND room_id = @roomId");
                dbClient.AddParameter("roomId", Id);
                DataTable table = dbClient.getTable();

                foreach (DataRow row in table.Rows)
                    result.Add(Convert.ToInt32(row[0]));
            }
            return result;
        }

        public bool HasBanExpired(int pId)
        {
            if (!UserIsBanned(pId)) return true;
            return Bans[pId] < PolarEnvironment.GetUnixTimestamp();
        }

        public void Unban(int userId)
        {
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("DELETE FROM `room_bans` WHERE `user_id` = @userId AND `room_id` = @roomId LIMIT 1");
                dbClient.AddParameter("userId", userId);
                dbClient.AddParameter("roomId", Id);
                dbClient.RunQuery();
            }

            Bans.Remove(userId);
        }

        #endregion

        #region Trading

        public bool HasActiveTrade(RoomUser User)
        {
            if (User.IsBot) return false;
            return this.HasActiveTrade(User.GetClient().GetHabbo().Id);
        }

        public bool HasActiveTrade(int UserId)
        {
            foreach (Trade trade in this.ActiveTrades)
                if (trade.ContainsUser(UserId)) return true;
            return false;
        }

        public Trade GetUserTrade(int UserId)
        {
            foreach (Trade trade in this.ActiveTrades)
                if (trade.ContainsUser(UserId)) return trade;
            return null;
        }

        public void TryStartTrade(RoomUser UserOne, RoomUser UserTwo)
        {
            if (UserOne == null || UserTwo == null) return;
            if (UserOne.IsBot || UserTwo.IsBot || UserOne.IsTrading || UserTwo.IsTrading ||
                this.HasActiveTrade(UserOne) || this.HasActiveTrade(UserTwo))
                return;

            this.ActiveTrades.Add(new Trade(
                UserOne.GetClient().GetHabbo().Id,
                UserTwo.GetClient().GetHabbo().Id,
                this.Id));
        }

        public void TryStopTrade(int UserId)
        {
            Trade userTrade = this.GetUserTrade(UserId);
            if (userTrade == null) return;
            userTrade.CloseTrade(UserId);
            this.ActiveTrades.Remove(userTrade);
        }

        #endregion

        public Task RunTask(Func<Task> callBack)
        {
            // ✅ FIX #4: Usar el token del manager en lugar del CTS eliminado
            return Task.Run(async () =>
            {
                if (this.mDisposed) return;
                await callBack();
            }, _mainProcessSource?.Token ?? CancellationToken.None);
        }

        public List<ServerPacket> HideWiredMessages(bool hideWired)
        {
            List<ServerPacket> list = new List<ServerPacket>();
            Item[] items = this.GetRoomItemHandler().GetFloor.ToArray();

            // ✅ FIX #5: .Count() (LINQ O(n)) → .Length (O(1)) en array
            for (int i = 0; i < items.Length; i++)
            {
                Item item = items[i];
                if (!item.IsWired) continue;

                list.Add(hideWired
                    ? new ObjectRemoveComposer(item, 0)
                    : new ObjectAddComposer(item, this));
            }
            return list;
        }

        public bool HideWired
        {
            get { return this._hideWired; }
            set { this._hideWired = value; }
        }

        public RoomTraxManager GetTraxManager() => this._traxManager;

        public int UserCount => _roomUserManager.GetRoomUsers().Count;

        public int RoomId => Id;

        public bool CanTradeInRoom => true;

        public RoomData RoomData => _roomData;

        public Gamemap GetGameMap() => _gamemap;

        public RoomItemHandling GetRoomItemHandler()
        {
            if (_roomItemHandling == null)
                _roomItemHandling = new RoomItemHandling(this);
            return _roomItemHandling;
        }

        public RoomUserManager GetRoomUserManager() => _roomUserManager;

        public Soccer GetSoccer()
        {
            if (_soccer == null) _soccer = new Soccer(this);
            return _soccer;
        }

        public TeamManager GetTeamManagerForBanzai()
        {
            if (teambanzai == null) teambanzai = TeamManager.createTeamforGame("banzai");
            return teambanzai;
        }

        public TeamManager GetTeamManagerForFreeze()
        {
            if (teamfreeze == null) teamfreeze = TeamManager.createTeamforGame("freeze");
            return teamfreeze;
        }

        public BattleBanzai GetBanzai()
        {
            if (_banzai == null) _banzai = new BattleBanzai(this);
            return _banzai;
        }

        public Freeze GetFreeze()
        {
            if (_freeze == null) _freeze = new Freeze(this);
            return _freeze;
        }

        public GameManager GetGameManager()
        {
            if (_gameManager == null) _gameManager = new GameManager(this);
            return _gameManager;
        }

        public GameItemHandler GetGameItemHandler()
        {
            if (_gameItemHandler == null) _gameItemHandler = new GameItemHandler(this);
            return _gameItemHandler;
        }

        public bool GotSoccer() => _soccer != null;
        public bool GotBanzai() => _banzai != null;
        public bool GotFreeze() => _freeze != null;

        public void ClearTags() => Tags.Clear();

        public void setPoolQuestion(string pool) => this.poolQuestion = pool;

        public void clearPoolAnswers()
        {
            this.yesPoolAnswers.Clear();
            this.noPoolAnswers.Clear();
        }

        public void startQuestion(string question, int Time)
        {
            setPoolQuestion(question);
            clearPoolAnswers();
            SendMessage(new QuickPollMessageComposer(question, Time));
        }

        public void endQuestion()
        {
            setPoolQuestion(string.Empty);
            SendMessage(new QuickPollResultsMessageComposer(yesPoolAnswers.Count, noPoolAnswers.Count));
            clearPoolAnswers();
        }

        public void AddTagRange(List<string> tags) => Tags.AddRange(tags);

        public void InitBots()
        {
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT `id`,`room_id`,`name`,`motto`,`look`,`x`,`y`,`z`,`rotation`,`gender`,`user_id`,`ai_type`,`walk_mode`,`automatic_chat`,`speaking_interval`,`mix_sentences`,`chat_bubble` FROM `bots` WHERE `room_id` = @roomId AND `ai_type` != 'pet'");
                dbClient.AddParameter("roomId", RoomId);
                DataTable Data = dbClient.getTable();
                if (Data == null) return;

                foreach (DataRow Bot in Data.Rows)
                {
                    dbClient.SetQuery("SELECT `text` FROM `bots_speech` WHERE `bot_id` = @botId");
                    dbClient.AddParameter("botId", Convert.ToInt32(Bot["id"]));
                    DataTable BotSpeech = dbClient.getTable();

                    List<RandomSpeech> Speeches = new List<RandomSpeech>();
                    foreach (DataRow Speech in BotSpeech.Rows)
                        Speeches.Add(new RandomSpeech(Convert.ToString(Speech["text"]), Convert.ToInt32(Bot["id"])));

                    _roomUserManager.DeployBot(new RoomBot(
                        Convert.ToInt32(Bot["id"]), Convert.ToInt32(Bot["room_id"]),
                        Convert.ToString(Bot["ai_type"]), Convert.ToString(Bot["walk_mode"]),
                        Convert.ToString(Bot["name"]), Convert.ToString(Bot["motto"]),
                        Convert.ToString(Bot["look"]),
                        int.Parse(Bot["x"].ToString()), int.Parse(Bot["y"].ToString()),
                        int.Parse(Bot["z"].ToString()), int.Parse(Bot["rotation"].ToString()),
                        0, 0, 0, 0, ref Speeches, "M", 0,
                        Convert.ToInt32(Bot["user_id"].ToString()),
                        Convert.ToBoolean(Bot["automatic_chat"]),
                        Convert.ToInt32(Bot["speaking_interval"]),
                        PolarEnvironment.EnumToBool(Bot["mix_sentences"].ToString()),
                        Convert.ToInt32(Bot["chat_bubble"])), null);
                }
            }
        }

        public void InitPets()
        {
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery(@"
                    SELECT b.`id`, b.`user_id`, b.`room_id`, b.`name`, b.`x`, b.`y`, b.`z`,
                           p.`type`, p.`race`, p.`color`, p.`experience`, p.`energy`, p.`nutrition`, 
                           p.`respect`, p.`createstamp`, p.`have_saddle`, p.`anyone_ride`, 
                           p.`hairdye`, p.`pethair`, p.`gnome_clothing`
                    FROM `bots` b
                    INNER JOIN `bots_petdata` p ON p.`id` = b.`id`
                    WHERE b.`room_id` = @roomId AND b.`ai_type` = 'pet'");
                dbClient.AddParameter("roomId", RoomId);
                DataTable Data = dbClient.getTable();
                if (Data == null) return;

                foreach (DataRow Row in Data.Rows)
                {
                    Pet Pet = new Pet(
                        Convert.ToInt32(Row["id"]), Convert.ToInt32(Row["user_id"]),
                        Convert.ToInt32(Row["room_id"]), Convert.ToString(Row["name"]),
                        Convert.ToInt32(Row["type"]), Convert.ToString(Row["race"]),
                        Convert.ToString(Row["color"]), Convert.ToInt32(Row["experience"]),
                        Convert.ToInt32(Row["energy"]), Convert.ToInt32(Row["nutrition"]),
                        Convert.ToInt32(Row["respect"]), Convert.ToDouble(Row["createstamp"]),
                        Convert.ToInt32(Row["x"]), Convert.ToInt32(Row["y"]),
                        Convert.ToDouble(Row["z"]), Convert.ToInt32(Row["have_saddle"]),
                        Convert.ToInt32(Row["anyone_ride"]), Convert.ToInt32(Row["hairdye"]),
                        Convert.ToInt32(Row["pethair"]), Convert.ToString(Row["gnome_clothing"]));

                    var RndSpeechList = new List<RandomSpeech>();
                    _roomUserManager.DeployBot(new RoomBot(
                        Pet.PetId, RoomId, "pet", "freeroam", Pet.Name, "", Pet.Look,
                        Pet.X, Pet.Y, Convert.ToInt32(Pet.Z), 0, 0, 0, 0, 0,
                        ref RndSpeechList, "", 0, Pet.OwnerId, false, 0, false, 0), Pet);
                }
            }
        }

        public FilterComponent GetFilter() => _filterComponent;

        public WiredComponent GetWired()
        {
            if (_wiredComponent != null) return _wiredComponent;
            _wiredComponent = new WiredComponent(this);
            return _wiredComponent;
        }

        public void LoadPromotions()
        {
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT * FROM `room_promotions` WHERE `room_id` = @roomId LIMIT 1");
                dbClient.AddParameter("roomId", Id);
                DataRow row = dbClient.getRow();

                if (row != null && Convert.ToDouble(row["timestamp_expire"]) > PolarEnvironment.GetUnixTimestamp())
                    RoomData.Promotion = new RoomPromotion(
                        Convert.ToString(row["title"]), Convert.ToString(row["description"]),
                        Convert.ToDouble(row["timestamp_start"]), Convert.ToDouble(row["timestamp_expire"]),
                        Convert.ToInt32(row["category_id"]));
            }
        }

        public void LoadRights()
        {
            UsersWithRights = new List<int>();
            if (Group != null) return;

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT room_rights.user_id FROM room_rights WHERE room_id = @roomid");
                dbClient.AddParameter("roomid", Id);
                DataTable data = dbClient.getTable();

                if (data != null)
                    foreach (DataRow row in data.Rows)
                        UsersWithRights.Add(Convert.ToInt32(row["user_id"]));
            }
            PushWiredSettingsToCurrentHabbos();
        }

        public List<Item> GetItemsByInteraction(InteractionType ItemInteraction)
        {
            if (this.GetRoomItemHandler()?.GetFloor == null)
                return new List<Item>();

            return this.GetRoomItemHandler().GetFloor
                .Where(i => i?.GetBaseItem()?.InteractionType == ItemInteraction)
                .ToList();
        }

        public List<Item> GetItemsByName(string ItemName)
        {
            if (this.GetRoomItemHandler()?.GetFloor == null)
                return new List<Item>();

            return this.GetRoomItemHandler().GetFloor
                .Where(i => i?.GetBaseItem()?.ItemName?.ToLower() == ItemName?.ToLower())
                .ToList();
        }

        private void LoadFilter()
        {
            this._wordFilterList = new List<string>();
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT * FROM `room_filter` WHERE `room_id` = @roomid");
                dbClient.AddParameter("roomid", Id);
                DataTable data = dbClient.getTable();
                if (data == null) return;
                foreach (DataRow row in data.Rows)
                    this._wordFilterList.Add(Convert.ToString(row["word"]));
            }
        }

        public void LoadBans()
        {
            this.Bans = new Dictionary<int, double>();
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT user_id, expire FROM room_bans WHERE room_id = @roomId");
                dbClient.AddParameter("roomId", Id);
                DataTable bans = dbClient.getTable();
                if (bans == null) return;
                foreach (DataRow ban in bans.Rows)
                    this.Bans.Add(Convert.ToInt32(ban[0]), Convert.ToDouble(ban[1]));
            }
        }

        public bool TilesTouching(int X1, int Y1, int X2, int Y2) =>
            (Math.Abs(X1 - X2) <= 1 && Math.Abs(Y1 - Y2) <= 1) || (X1 == X2 && Y1 == Y2);

        public bool CheckRights(GameClient Session) => CheckRights(Session, false);

        public bool CheckRights(GameClient Session, bool RequireOwnership, bool CheckForGroups = false)
        {
            try
            {
                if (Session?.GetHabbo() == null) return false;
                if (Session.GetHabbo().Username == OwnerName && Type == "private") return true;
                if (Session.GetHabbo().GetPermissions().HasRight("room_any_owner")) return true;

                if (!RequireOwnership && Type == "private")
                {
                    if (this.TryGetHouse(out House house) && house != null &&
                        house.OwnerId == Session.GetHabbo().Id && !house.ForSale)
                        return true;

                    if (Session.GetHabbo().GetPermissions().HasRight("room_any_rights"))
                        return true;
                }

                return UsersWithRights.Contains(Session.GetHabbo().Id);
            }
            catch (Exception e)
            {
                Logging.HandleException(e, "Room.CheckRights");
                return false;
            }
        }

        public bool CheckTerrain(GameClient Session, string[] Data)
        {
            if (Data.Length < 4) return false;
            if (!int.TryParse(Data[1], out int X) || !int.TryParse(Data[2], out int Y))
                return false;

            return CheckTerrainInternal(Session, X, Y);
        }

        public bool CheckTerrain(GameClient Session, int furnix, int furniy)
        {
            if (Session == null) return false;
            return CheckTerrainInternal(Session, furnix, furniy);
        }

        // ✅ FIX #6: Lógica duplicada de CheckTerrain extraída a método privado
        private bool CheckTerrainInternal(GameClient Session, int X, int Y)
        {
            List<House> houses = PolarEnvironment.GetGame().GetHouseManager().GetTerrainsBySignRoomId(this.Id);
            if (houses.Count <= 0) return false;

            foreach (House house in houses)
            {
                if (house.OwnerId != Session.GetHabbo().Id) continue;
                try
                {
                    int.TryParse(house.Space[0].Split(',')[0], out int lix);
                    int.TryParse(house.Space[1].Split(',')[0], out int lsx);
                    int.TryParse(house.Space[2].Split(',')[0], out int liy);
                    int.TryParse(house.Space[3].Split(',')[0], out int lsy);

                    if (X >= lix && X <= lsx && Y >= liy && Y <= lsy)
                        return true;
                }
                catch { }
            }
            return false;
        }

        public void OnUserShoot(RoomUser User, Item Ball)
        {
            string Key = null;
            foreach (Item item in this.GetRoomItemHandler().GetFurniObjects(Ball.GetX, Ball.GetY).ToList())
            {
                if (item.GetBaseItem().ItemName.StartsWith("fball_goal_"))
                {
                    Key = item.GetBaseItem().ItemName.Split('_')[2];
                    User.UnIdle();
                    User.DanceId = 0;
                    PolarEnvironment.GetGame().GetAchievementManager()
                        .ProgressAchievement(User.GetClient(), "ACH_FootballGoalScored", 1);
                    SendMessage(new ActionComposer(User.VirtualId, 1));
                }
            }

            if (Key != null)
            {
                string scoreItemName = "fball_score_" + Key;
                foreach (Item item2 in this.GetRoomItemHandler().GetFloor
                    .Where(p => p.GetBaseItem().ItemName == scoreItemName).ToList())
                {
                    item2.ExtraData = string.IsNullOrEmpty(item2.ExtraData)
                        ? "1"
                        : (Convert.ToInt32(item2.ExtraData) + 1).ToString();
                    item2.UpdateState();
                }
            }
        }

        public async Task ProcessRoom()
        {
            if (mDisposed) return;

            try
            {
                var timeStarted = DateTime.Now;

                if (this.GetRoomUserManager().GetRoomUsers().Count == 0 &&
                    this.GetRoomUserManager().GetRoleplayBots().Count == 0)
                    this.IdleTime++;
                else if (this.IdleTime > 0)
                    this.IdleTime = 0;

                if (HasActivePromotion && Promotion.HasExpired) EndPromotion();

                if (IdleTime >= 60 && !HasActivePromotion)
                {
                    await PolarEnvironment.GetGame().GetRoomManager().UnloadRoom(this);
                    return;
                }

                try { GetRoomItemHandler().OnCycle(); }
                catch (Exception e) { Logging.LogException(e.ToString()); }

                try { GetRoomUserManager().OnCycle(); }
                catch (Exception e) { Logging.LogException(e.ToString()); }

                try { GetRoomUserManager().SerializeStatusUpdates(); }
                catch (Exception e) { Logging.LogException(e.ToString()); }

                try { if (_gameItemHandler != null) _gameItemHandler.OnCycle(); }
                catch (Exception e) { Logging.LogException(e.ToString()); }

                try { GetWired()?.OnCycle(); }
                catch (Exception e) { Logging.LogException(e.ToString()); }

                try { this._traxManager.OnCycle(); }
                catch (Exception e) { Logging.LogException(e.ToString()); }

                if (timeStarted > this._saveFurnitureTimerLast + this._saveFurnitureTimer)
                {
                    this._saveFurnitureTimerLast = timeStarted;
                    this._roomItemHandling.SaveFurniture();
                }
            }
            catch (Exception e)
            {
                Logging.WriteLine($"Room ID [{RoomId}] se crashed.");
                Logging.LogException($"Room ID [{RoomId}] se crashed. {e}");
                OnRoomCrash(e);
            }
        }

        private void OnRoomCrash(Exception e)
        {
            Logging.LogThreadException(e.ToString(), $"Tarea de ciclo de habitación para habitación {RoomId}");

            try
            {
                foreach (RoomUser user in _roomUserManager.GetRoomUsers().ToList())
                {
                    if (user?.GetClient() == null) continue;
                    user.GetClient().SendNotification("Lo siento, parece que la habitación se ha estrellado.");
                    try { GetRoomUserManager().RemoveUserFromRoom(user.GetClient(), true, false); }
                    catch (Exception e2) { Logging.LogException(e2.ToString()); }
                }
            }
            catch (Exception e3) { Logging.LogException(e3.ToString()); }

            isCrashed = true;
            PolarEnvironment.GetGame().GetRoomManager().UnloadRoom(this, true);
        }

        public bool CheckMute(GameClient Session)
        {
            if (MutedUsers.ContainsKey(Session.GetHabbo().Id))
            {
                if (MutedUsers[Session.GetHabbo().Id] < PolarEnvironment.GetUnixTimestamp())
                    MutedUsers.Remove(Session.GetHabbo().Id);
                else
                    return true;
            }

            return Session.GetHabbo().TimeMuted > 0 ||
                   (RoomMuted && Session.GetHabbo().Username != OwnerName);
        }

        public void AddChatlog(int Id, string Message)
        {
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("INSERT INTO `chatlogs` (user_id, room_id, message, timestamp) VALUES (@user, @room, @message, @time)");
                dbClient.AddParameter("user", Id);
                dbClient.AddParameter("room", RoomId);
                dbClient.AddParameter("message", Message);
                dbClient.AddParameter("time", PolarEnvironment.GetUnixTimestamp());
                dbClient.RunQuery();
            }
        }

        public bool TryGetHouse(out House House) =>
            PolarEnvironment.GetGame().GetHouseManager().HouseList.TryGetValue(this.RoomId, out House);

        public void SendObjects(GameClient Session)
        {
            Room Room = Session.GetHabbo().CurrentRoom;

            // Java orden:
            // 1° RoomRelativeMapComposer → HeightMapComposer  (width + totalTiles + shorts)
            // 2° RoomHeightMapComposer   → FloorHeightMapComposer (bool + wallHeight + string)
            Session.SendMessage(new HeightMapComposer(Room));
            Session.SendMessage(new FloorHeightMapComposer(Room));

            var userList = _roomUserManager.GetUserList().ToList();

            if (userList.Count > 0)
                Session.SendMessage(new UsersComposer(userList));

            foreach (RoomUser roomUser in userList)
            {
                if (roomUser == null) continue;

                if (roomUser.IsBot && roomUser.BotData.DanceId > 0)
                    Session.SendMessage(new DanceComposer(roomUser, roomUser.BotData.DanceId));
                else if (!roomUser.IsBot && !roomUser.IsPet && roomUser.IsDancing)
                    Session.SendMessage(new DanceComposer(roomUser, roomUser.DanceId));

                if (roomUser.IsAsleep)
                    Session.SendMessage(new SleepComposer(roomUser, true));

                if (roomUser.CarryItemID > 0 && roomUser.CarryTimer > 0)
                    Session.SendMessage(new CarryObjectComposer(roomUser.VirtualId, roomUser.CarryItemID));

                if (!roomUser.IsBot && !roomUser.IsPet && roomUser.CurrentEffect > 0)
                    Session.SendMessage(new AvatarEffectComposer(roomUser.VirtualId, roomUser.CurrentEffect));
            }

            Session.SendMessage(new UserUpdateComposer(userList));
            Session.SendMessage(new ObjectsComposer(Room.GetRoomItemHandler().GetFloor.ToArray(), Room));
            Session.SendMessage(new ItemsComposer(Room.GetRoomItemHandler().GetWall.ToArray(), Room));
        }

        #region Tents

        public void AddTent(int TentId)
        {
            Tents[TentId] = new List<RoomUser>();
        }

        public void RemoveTent(int TentId, Item Item)
        {
            if (!Tents.TryGetValue(TentId, out List<RoomUser> users)) return;

            foreach (RoomUser user in users.ToList())
            {
                if (user?.GetClient()?.GetHabbo() == null) continue;
                user.GetClient().GetHabbo().TentId = 0;
            }
            Tents.Remove(TentId);
        }

        public void AddUserToTent(int TentId, RoomUser User, Item Item)
        {
            if (User?.GetClient()?.GetHabbo() == null) return;
            if (!Tents.ContainsKey(TentId)) Tents[TentId] = new List<RoomUser>();
            if (!Tents[TentId].Contains(User)) Tents[TentId].Add(User);
            User.GetClient().GetHabbo().TentId = TentId;
        }

        public void RemoveUserFromTent(int TentId, RoomUser User, Item Item)
        {
            if (User?.GetClient()?.GetHabbo() == null) return;
            if (!Tents.ContainsKey(TentId)) Tents[TentId] = new List<RoomUser>();
            Tents[TentId].Remove(User);
            User.GetClient().GetHabbo().TentId = 0;
        }

        public void SendToTent(int Id, int TentId, IServerPacket Packet)
        {
            if (!Tents.TryGetValue(TentId, out List<RoomUser> users)) return;

            foreach (RoomUser user in users.ToList())
            {
                if (user?.GetClient()?.GetHabbo() == null) continue;
                if (user.GetClient().GetHabbo().MutedUsers.Contains(Id)) continue;
                if (user.GetClient().GetHabbo().TentId != TentId) continue;
                user.GetClient().SendMessage(Packet);
            }
        }

        #endregion

        #region Communication (Packets)

        public void SendMessage(IServerPacket Message, bool UsersWithRightsOnly = false)
        {
            if (Message == null) return;

            try
            {
                // ✅ FIX #7: "if (this == null)" eliminado — nunca puede ser verdadero en C#
                if (_roomUserManager == null) return;

                var userList = _roomUserManager.GetUserList();
                foreach (RoomUser User in userList)
                {
                    if (User == null || User.IsBot) continue;
                    if (User.GetClient() == null) continue;
                    if (UsersWithRightsOnly && !this.CheckRights(User.GetClient())) continue;
                    User.GetClient().SendMessage(Message);
                }
            }
            catch (Exception e)
            {
                Logging.HandleException(e, "Room.SendMessage");
            }
        }

        public void SendMessage(List<ServerPacket> Messages)
        {
            if (Messages == null || Messages.Count == 0) return;

            try
            {
                int totalLength = 0;
                var packetBytes = new List<byte[]>(Messages.Count);

                foreach (var packet in Messages)
                {
                    var bytes = packet.GetBytes();
                    packetBytes.Add(bytes);
                    totalLength += bytes.Length;
                }

                byte[] totalBytes = ArrayPool<byte>.Shared.Rent(totalLength);

                int offset = 0;
                foreach (var bytes in packetBytes)
                {
                    Buffer.BlockCopy(bytes, 0, totalBytes, offset, bytes.Length);
                    offset += bytes.Length;
                }

                this.BroadcastPacket(totalBytes, totalLength);
                ArrayPool<byte>.Shared.Return(totalBytes, clearArray: false);
            }
            catch (Exception e)
            {
                Logging.HandleException(e, "Room.SendMessage List<ServerPacket>");
            }
        }

        public void BroadcastPacket(byte[] Packet, int length)
        {
            if (Packet == null || length <= 0) return;

            var users = _roomUserManager?.GetUserList();
            if (users == null) return;

            foreach (RoomUser User in users)
            {
                if (User == null || User.IsBot) continue;
                var conn = User.GetClient()?.GetConnection();
                if (conn == null) continue;
                try { conn.SendData(Packet, 0, length); }
                catch { }
            }
        }

        public void BroadcastPacket(byte[] Packet)
        {
            if (Packet != null) BroadcastPacket(Packet, Packet.Length);
        }

        #endregion

        private void SaveAI()
        {
            foreach (RoomUser User in GetRoomUserManager().GetRoomUsers().ToList())
            {
                // ✅ FIX #8: Doble if (User.IsBot) eliminado — era redundante
                if (User == null || !User.IsBot) continue;

                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("UPDATE bots SET x=@x, y=@y, z=@z, name=@name, look=@look, rotation=@rotation WHERE id=@id LIMIT 1");
                    dbClient.AddParameter("name", User.BotData.Name);
                    dbClient.AddParameter("look", User.BotData.Look);
                    dbClient.AddParameter("rotation", User.BotData.Rot);
                    dbClient.AddParameter("x", User.X);
                    dbClient.AddParameter("y", User.Y);
                    dbClient.AddParameter("z", User.Z);
                    dbClient.AddParameter("id", User.BotData.BotId);
                    dbClient.RunQuery();
                }
            }
        }

        public void Dispose()
        {
            DisposeAsync().GetAwaiter().GetResult();
        }
        public static bool hasWiredAccess(int mask, int permissionMask)
        {
            return (mask & permissionMask) != 0;
        }

        public static int sanitizeWiredInspectMask(int mask)
        {
            int sanitizedMask = mask & WIRED_ACCESS_ALLOWED_INSPECT_MASK;

            if (hasWiredAccess(sanitizedMask, WIRED_ACCESS_GROUP_MEMBERS))
            {
                sanitizedMask |= WIRED_ACCESS_GROUP_ADMINS;
            }

            return sanitizedMask;
        }
        private static int sanitizeWiredModifyMask(int mask)
        {
            int sanitizedMask = mask & WIRED_ACCESS_ALLOWED_MODIFY_MASK;

            if (hasWiredAccess(sanitizedMask, WIRED_ACCESS_GROUP_MEMBERS))
            {
                sanitizedMask |= WIRED_ACCESS_GROUP_ADMINS;
            }

            return sanitizedMask;
        }
        private void ensureWiredSettingsLoaded()
        {
            if (wiredSettingsLoaded) return;

            lock (wiredSettingsLock)
            {
                if (wiredSettingsLoaded) return;

                wiredInspectMask = WIRED_ACCESS_DEFAULT_INSPECT_MASK;
                wiredModifyMask = WIRED_ACCESS_DEFAULT_MODIFY_MASK;

                try
                {
                    using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                    {
                        dbClient.SetQuery(
                            "SELECT `inspect_mask`, `modify_mask` FROM `room_wired_settings` " +
                            "WHERE `room_id` = @roomId LIMIT 1");
                        dbClient.AddParameter("roomId", this.Id);

                        System.Data.DataRow row = dbClient.getRow();
                        if (row != null)
                        {
                            wiredInspectMask = sanitizeWiredInspectMask(Convert.ToInt32(row["inspect_mask"]));
                            wiredModifyMask = sanitizeWiredModifyMask(Convert.ToInt32(row["modify_mask"]));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logging.LogException($"[EnsureWiredSettingsLoaded] SQL error: {ex}");
                }

                wiredSettingsLoaded = true;
            }
        }

        public bool canManageWiredSettings(Habbo habbo)
        {
            return habbo != null;
        }
        public bool canModifyWired(Habbo habbo)
        {
            if (habbo == null)
            {
                return false;
            }

            if (this.canManageWiredSettings(habbo))
            {
                return true;
            }
            this.ensureWiredSettingsLoaded();
            return false;
        }

        public bool canInspectWired(Habbo habbo)
        {
            if (habbo == null)
            {
                return false;
            }

            if (this.canManageWiredSettings(habbo))
            {
                return true;
            }

            return false;
        }

        public bool SaveWiredSettings(int inspectMask, int modifyMask)
        {
            int sanitizedInspectMask = sanitizeWiredInspectMask(inspectMask);
            int sanitizedModifyMask = sanitizeWiredModifyMask(modifyMask);

            // Java: sanitizedInspectMask |= sanitizedModifyMask
            sanitizedInspectMask |= sanitizedModifyMask;

            lock (wiredSettingsLock)
            {
                int previousInspectMask = this.wiredInspectMask;
                int previousModifyMask = this.wiredModifyMask;

                this.wiredInspectMask = sanitizedInspectMask;
                this.wiredModifyMask = sanitizedModifyMask;
                this.wiredSettingsLoaded = true;

                try
                {
                    using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                    {
                        dbClient.SetQuery(
                            "INSERT INTO `room_wired_settings` (`room_id`, `inspect_mask`, `modify_mask`) " +
                            "VALUES (@roomId, @inspectMask, @modifyMask) " +
                            "ON DUPLICATE KEY UPDATE `inspect_mask` = VALUES(`inspect_mask`), `modify_mask` = VALUES(`modify_mask`)");
                        dbClient.AddParameter("roomId", this.Id);
                        dbClient.AddParameter("inspectMask", sanitizedInspectMask);
                        dbClient.AddParameter("modifyMask", sanitizedModifyMask);
                        dbClient.RunQuery();
                    }

                    PushWiredSettingsToCurrentHabbos();
                    return true;
                }
                catch (Exception ex)
                {
                    // Revertir en caso de error, igual que el Java
                    this.wiredInspectMask = previousInspectMask;
                    this.wiredModifyMask = previousModifyMask;
                    Logging.LogException($"[SaveWiredSettings] SQL error: {ex}");
                    return false;
                }
            }
        }

        public void PushWiredSettingsToCurrentHabbos()
        {
            foreach (RoomUser roomUser in GetRoomUserManager().GetUserList())
            {
                if (roomUser == null || roomUser.IsBot || roomUser.GetClient() == null) continue;

                Habbo habbo = roomUser.GetClient().GetHabbo();
                if (habbo == null) continue;

                roomUser.GetClient().SendMessage(new WiredRoomSettingsDataComposer(this, habbo));
            }
        }
        public async ValueTask DisposeAsync()
        {
            if (mDisposed) return;

            SendMessage(new CloseConnectionComposer());
            isCrashed = false;
            mDisposed = true;
            mCycleEnded = true;

            _mainProcessSource?.Cancel();

            // ✅ FIX #9: Timeout en la espera del processTask para evitar bloqueo
            //           indefinido si el task no responde a la cancelación.
            if (_processTask != null)
            {
                try
                {
                    await _processTask.WaitAsync(TimeSpan.FromSeconds(5));
                }
                catch (TimeoutException)
                {
                    //Logging.WriteLine($"[Room {RoomId}] ProcessTask no terminó en 5s, continuando Dispose.");
                }
                catch (OperationCanceledException) { /* esperado */ }
            }

            this.GetRoomItemHandler().SaveFurniture();

            foreach (Pet pet in _roomUserManager.GetPets())
            {
                if (pet != null && pet.DbState != PetDatabaseUpdateState.Updated)
                    pet.Save();
            }

            this.SaveAI();

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("UPDATE `rooms` SET `users_now` = '0' WHERE `id` = @roomId LIMIT 1");
                dbClient.AddParameter("roomId", Id);
                dbClient.RunQuery();
            }

            UsersNow = 0;
            RoomData.UsersNow = 0;
            UsersWithRights?.Clear();
            Bans?.Clear();
            MutedUsers?.Clear();
            Tents?.Clear();
            TonerData = null;
            MoodlightData = null;

            _gameItemHandler?.Dispose();
            _gameManager?.Dispose();
            _freeze?.Dispose();
            _banzai?.Dispose();
            _soccer?.Dispose();
            _gamemap?.Dispose();
            _roomUserManager?.Dispose();
            _roomItemHandling?.Dispose();
            _filterComponent?.Cleanup();
            _wiredComponent?.Cleanup();
            ActiveTrades?.Clear();

            // ✅ FIX #10: "new Task(...).Start()" era un antipatrón peligroso para
            //            limpiar el CTS. Ahora se usa Task.Delay directamente.
            _ = Task.Run(async () =>
            {
                await Task.Delay(2500);
                _mainProcessSource?.Dispose();
                _mainProcessSource = null;
            });
        }
    }
}