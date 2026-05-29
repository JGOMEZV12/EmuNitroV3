using log4net;
using Polar.Communication.Packets.Outgoing;
using Polar.Communication.Packets.Outgoing.Handshake;
using Polar.Communication.Packets.Outgoing.Inventory.Purse;
using Polar.Communication.Packets.Outgoing.Navigator;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.Communication.Packets.Outgoing.Rooms.Session;
using System.Collections.Generic;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Achievements;
using Polar.HabboHotel.Camera;
using Polar.HabboHotel.Catalog;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Items.Crafting;
using Polar.HabboHotel.Items.Wired;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Chat.Commands;
using Polar.HabboHotel.Subscriptions;
using Polar.HabboHotel.Users.Badges;
using Polar.HabboHotel.Users.Clothing;
using Polar.HabboHotel.Users.Effects;
using Polar.HabboHotel.Users.Inventory;
using Polar.HabboHotel.Users.Messenger;
using Polar.HabboHotel.Users.Messenger.FriendBar;
using Polar.HabboHotel.Users.Navigator.SavedSearches;
using Polar.HabboHotel.Users.Permissions;
using Polar.HabboHotel.Users.Process;
using Polar.HabboHotel.Users.Relationships;
using Polar.HabboHotel.Users.UserDataManagement;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.RoleplayUsers;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Polar.HabboHotel.Users
{
    public class Habbo
    {
        private static readonly ILog log = LogManager.GetLogger("Polar.HabboHotel.Users");
        private Dictionary<int, CatalogItem> recentPurchases = new Dictionary<int, CatalogItem>();
        public bool BoostingCheck;
        public bool DebugStacking = false;
        public double StackHeight = 0;
        public int RobberyU;

        private int _id;
        private string _username;
        private int _rank;
        private string _motto;
        private string _look;
        private string _gender;
        private string _footballLook;
        private string _footballGender;
        private int _credits;
        private int _duckets;
        private int _diamonds;
        private int _eventPoints;
        private int _homeRoom;
        private double _lastOnline;
        private int _Online;
        private double _accountCreated;
        private List<int> _clientVolume;
        private double _lastNameChange;
        private string _machineId;
        private bool _chatPreference;
        private bool _focusPreference;
        private bool _isExpert;
        private int _CurrentTalentLevel;
        internal int Sussurrando;
        private int _vipRank;
        private string _pinClient;
        public bool StaffOk = false;
        public string ssoTicket;
        public string _lastPhotoPreview;
        public JSONCamera lastPhotoPreview;
        public string _lastPhotoRandom;
        public string lastPhotoRandom;

        private bool _appearOffline;
        private bool _allowTradingRequests;
        private bool _allowUserFollowing;
        private bool _allowFriendRequests;
        private bool _allowMessengerInvites;
        private bool _allowPetSpeech;
        private bool _allowBotSpeech;
        private bool _allowPublicRoomStatus;
        private bool _allowConsoleMessages;
        private bool _allowGifts;
        private bool _allowMimic;
        private bool _receiveWhispers;
        private bool _ignorePublicWhispers;
        private bool _playingFastFood;
        private FriendBarState _friendbarState;
        private int _christmasDay;
        private int _wantsToRideHorse;
        private int _timeAFK;
        private bool _disableForcedEffects;
        private ClubManager ClubManager;
        internal List<int> HabboQuizQuestions;

        public bool _disconnected;
        private bool _habboSaved;
        private bool _changingName;

        internal HashSet<int> AnsweredPolls;
        public bool AnsweredMatchingPoll = false;
        internal HashSet<CraftingRecipe> UnlockedRecipes;

        private double _floodTime;
        private int _friendCount;
        private double _timeMuted;
        private double _tradingLockExpiry;
        private int _bannedPhraseCount;
        private double _sessionStart;
        private int _messengerSpamCount;
        private double _messengerSpamTime;
        private int _creditsTickUpdate;
        public bool _NUX;
        public bool PassedNuxNavigator = false, PassedNuxDuckets = false, PassedNuxItems = false, PassedNuxChat = false, PassedNuxCatalog = false, PassedNuxMMenu = false, PassedNuxCredits = false;
        public byte _TargetedBuy;

        private int _tentId;
        private int _hopperId;
        private bool _isHopping;
        private int _teleportId;
        private bool _isTeleporting;
        private int _teleportingRoomId;
        private bool _roomAuthOk;
        private int _currentRoomId;
        private bool _letInAppartment;

        private bool _hasSpoken;
        private bool _advertisingReported;
        private double _lastAdvertiseReport;
        private bool _advertisingReportBlocked;
        private int _advertisingStrikes;

        private bool _wiredInteraction;
        private int _questLastCompleted;
        private bool _inventoryAlert;
        private bool _ignoreBobbaFilter;
        private bool _wiredTeleporting;
        private int _customBubbleId;
        private int _tempInt;
        private int _uniqueId;
        private int _backgroundId;
        private int _standId;
        private int _overlayId;
        private string _namePrefix;

        private int _fastfoodScore;
        private int _petId;
        private string _colour;

        private DateTime _lastGiftPurchaseTime;
        private DateTime _lastMottoUpdateTime;
        private DateTime _lastClothingUpdateTime;
        private DateTime _lastForumMessageUpdateTime;

        private int _giftPurchasingWarnings;
        private int _mottoUpdateWarnings;
        private int _clothingUpdateWarnings;

        private bool _sessionGiftBlocked;
        private bool _sessionMottoBlocked;
        private bool _sessionClothingBlocked;

        public List<int> RatedRooms;
        public List<int> MutedUsers;
        public List<RoomData> UsersRooms;
        internal string lastLayout;
        private GameClient _client;
        private HabboStats _habboStats;
        private HabboMessenger Messenger;
        private ProcessComponent _process;
        public ArrayList FavoriteRooms;
        public Dictionary<int, int> quests;
        private BadgeComponent BadgeComponent;
        private InventoryComponent InventoryComponent;
        public Dictionary<int, Relationship> Relationships;
        public ConcurrentDictionary<string, UserAchievement> Achievements;

        private DateTime _timeCached;
        private SearchesComponent _navigatorSearches;
        private EffectsComponent _fx;
        private ClothingComponent _clothing;
        private PermissionComponent _permissions;
        private Polar.HabboHotel.Users.Inventory.PrefixesComponent _prefixes;

        private IChatCommand _iChatCommand;

        public bool Translating = false;
        public string FromLanguage = "";
        public string ToLanguage = "";
        private Dictionary<int, UserTalent> _Talents;
        public List<string> Tags;
        public bool IsCitizen => CurrentTalentLevel > 4;
        internal int CitizenshipLevel;
        public string TalentStatus;
        public string PetFigure;

        public bool IsLogin = true;
        public bool LoginGodProtect = true;

        public Habbo(int Id, string Username, int Rank, string Motto, string Look, string Gender, int Credits, int ActivityPoints, int HomeRoom,
            bool HasFriendRequestsDisabled, int LastOnline, bool AppearOffline, bool HideInRoom, double CreateDate, int Diamonds,
            string machineID, string clientVolume, bool ChatPreference, bool FocusPreference, bool PetsMuted, bool BotsMuted, bool AdvertisingReportBlocked, double LastNameChange,
            int EventPoints, bool IgnoreInvites, double TimeMuted, double TradingLock, bool AllowGifts, int FriendBarState, bool DisableForcedEffects, bool AllowMimic, int VIPRank, bool IsBot, string Colour, string namePrefix, string citizenShip, bool nux, byte TargetedBuy, int citizenshipLevel, int onLine, string PinCliente, int UniqueId, int backgroundId, int standId, int overlayId, DataRow pollRow, DataTable recipesTable, DataRow statsRow)
        {
            this._id = Id;
            this._uniqueId = UniqueId;
            this._username = Username;
            this._rank = Rank;
            this._motto = Motto;
            this._look = Look;
            this._gender = Gender.ToLower();
            this._footballLook = PolarEnvironment.FilterFigure(Look.ToLower());
            this._footballGender = Gender.ToLower();
            this._credits = Credits;
            this._duckets = ActivityPoints;
            this._diamonds = Diamonds;
            this._eventPoints = EventPoints;
            this._homeRoom = HomeRoom;
            this._lastOnline = LastOnline;
            this._Online = onLine;
            this._accountCreated = CreateDate;
            this._clientVolume = new List<int>();
            this.AnsweredPolls = new HashSet<int>();
            this.UnlockedRecipes = new HashSet<CraftingRecipe>();
            this.CitizenshipLevel = citizenshipLevel;
            this._TargetedBuy = TargetedBuy;
            this._backgroundId = backgroundId;
            this._standId = standId;
            this._overlayId = overlayId;
            this.recentPurchases = new Dictionary<int, CatalogItem>(0);
            this.InitPermissions();

            if (!IsBot)
            {
                if (pollRow != null)
                {
                    int pollId = Convert.ToInt32(pollRow["poll_id"]);
                    AnsweredPolls.Add(pollId);
                }

                if (recipesTable != null)
                {
                    foreach (DataRow recipeRow in recipesTable.Rows)
                    {
                        string recipeName = recipeRow["recipe"].ToString();
                        var recipe = CraftingManager.getRecipe(recipeName);
                        if (recipe != null) UnlockedRecipes.Add(recipe);
                    }
                }

                foreach (string str in clientVolume.Split(','))
                {
                    if (int.TryParse(str, out int val)) this._clientVolume.Add(val);
                    else this._clientVolume.Add(100);
                }

                if (statsRow != null)
                {
                    this._habboStats = new HabboStats(
                        Convert.ToInt32(statsRow["roomvisits"]),
                        Convert.ToDouble(statsRow["onlineTime"]),
                        Convert.ToInt32(statsRow["respect"]),
                        Convert.ToInt32(statsRow["respectGiven"]),
                        Convert.ToInt32(statsRow["giftsGiven"]),
                        Convert.ToInt32(statsRow["giftsReceived"]),
                        Convert.ToInt32(statsRow["dailyRespectPoints"]),
                        Convert.ToInt32(statsRow["dailyPetRespectPoints"]),
                        Convert.ToInt32(statsRow["AchievementScore"]),
                        Convert.ToInt32(statsRow["quest_id"]),
                        Convert.ToInt32(statsRow["quest_progress"]),
                        Convert.ToString(statsRow["respectsTimestamp"]),
                        Convert.ToInt32(statsRow["forum_posts"]));

                    if (Convert.ToString(statsRow["respectsTimestamp"]) != DateTime.Today.ToString("MM/dd"))
                    {
                        this._habboStats.RespectsTimestamp = DateTime.Today.ToString("MM/dd");
                        int dailyRespects = 3;
                        SubscriptionData subData = null;

                        if (this._permissions.HasRight("mod_tool")) dailyRespects = 3;
                        else if (PolarEnvironment.GetGame().GetSubscriptionManager().TryGetSubscriptionData(VIPRank, out subData))
                            dailyRespects = subData.Respects;

                        this._habboStats.DailyRespectPoints = dailyRespects;
                        this._habboStats.DailyPetRespectPoints = dailyRespects;

                        using var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
                        dbClient.RunQuery($"UPDATE `user_stats` SET `dailyRespectPoints` = '{dailyRespects}', `dailyPetRespectPoints` = '{dailyRespects}', `respectsTimestamp` = '{DateTime.Today:MM/dd}' WHERE `id` = '{Id}' LIMIT 1");
                    }
                }
                else
                {
                    this._habboStats = new HabboStats(0, 0, 0, 0, 0, 0, 3, 3, 0, 0, 0, DateTime.Today.ToString("MM/dd"), 0);
                }
            }

            this._pinClient = PinCliente;
            this._lastNameChange = LastNameChange;
            this._machineId = machineID;
            this._NUX = nux;
            this._chatPreference = ChatPreference;
            this._focusPreference = FocusPreference;
            this._isExpert = IsExpert == true;
            this._Talents = new Dictionary<int, UserTalent>();
            this.TalentStatus = citizenShip;
            this._CurrentTalentLevel = GetCurrentTalentLevel();
            this._appearOffline = AppearOffline;
            this._allowTradingRequests = true;
            this._allowUserFollowing = true;
            this._allowFriendRequests = HasFriendRequestsDisabled;
            this._allowMessengerInvites = IgnoreInvites;
            this._allowPetSpeech = PetsMuted;
            this._allowBotSpeech = BotsMuted;
            this._allowPublicRoomStatus = HideInRoom;
            this._allowConsoleMessages = true;
            this._allowGifts = AllowGifts;
            this._allowMimic = AllowMimic;
            this._lastPhotoRandom = lastPhotoRandom;
            this._receiveWhispers = true;
            this._ignorePublicWhispers = false;
            this._playingFastFood = false;
            this._friendbarState = FriendBarStateUtility.GetEnum(FriendBarState);
            this._christmasDay = 0; // Fix if needed
            this._wantsToRideHorse = 0;
            this._timeAFK = 0;
            this._disableForcedEffects = DisableForcedEffects;
            this._vipRank = VIPRank;

            this._disconnected = false;
            this._habboSaved = false;
            this._changingName = false;

            this._floodTime = 0;
            this._friendCount = 0;
            this._timeMuted = TimeMuted;
            this._timeCached = DateTime.Now;
            this.Tags = new List<string>();
            this._tradingLockExpiry = TradingLock;

            if (!IsBot && this._tradingLockExpiry > 0 && PolarEnvironment.GetUnixTimestamp() > this.TradingLockExpiry)
            {
                this._tradingLockExpiry = 0;
                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.RunQuery("UPDATE `user_info` SET `trading_locked` = '0' WHERE `user_id` = '" + Id + "' LIMIT 1");
                }
            }

            this._bannedPhraseCount = 0;
            this._sessionStart = PolarEnvironment.GetUnixTimestamp();
            this._messengerSpamCount = 0;
            this._messengerSpamTime = 0;
            this._creditsTickUpdate = PolarStaticGameSettings.UserCreditsUpdateTimer;

            this._tentId = 0;
            this._hopperId = 0;
            this._isHopping = false;
            this._teleportId = 0;
            this._isTeleporting = false;
            this._teleportingRoomId = 0;
            this._roomAuthOk = false;
            this._letInAppartment = false;
            this._currentRoomId = 0;

            this._hasSpoken = false;
            this._lastAdvertiseReport = 0;
            this._advertisingReported = false;
            this._advertisingReportBlocked = AdvertisingReportBlocked;
            this._advertisingStrikes = 0;

            this._wiredInteraction = false;
            this._questLastCompleted = 0;
            this._inventoryAlert = false;
            this._ignoreBobbaFilter = false;
            this._wiredTeleporting = false;
            this._customBubbleId = 0;
            this._fastfoodScore = 0;
            this._petId = 0;
            this._tempInt = 0;

            this._lastGiftPurchaseTime = DateTime.Now;
            this._lastMottoUpdateTime = DateTime.Now;
            this._lastClothingUpdateTime = DateTime.Now;
            this._lastForumMessageUpdateTime = DateTime.Now;

            this._giftPurchasingWarnings = 0;
            this._mottoUpdateWarnings = 0;
            this._clothingUpdateWarnings = 0;

            this._sessionGiftBlocked = false;
            this._sessionMottoBlocked = false;
            this._sessionClothingBlocked = false;

            this.FavoriteRooms = new ArrayList();
            this.MutedUsers = new List<int>();
            this.Achievements = new ConcurrentDictionary<string, UserAchievement>();
            this.Relationships = new Dictionary<int, Relationship>();
            this.RatedRooms = new List<int>();
            this.UsersRooms = new List<RoomData>();
            this.HabboQuizQuestions = new List<int>(5);

            this._colour = Colour;
            this._namePrefix = namePrefix;
            this.PetFigure = null;
        }

        public int Id { get => _id; set => _id = value; }
        public int TokenId { get => _uniqueId; set => _uniqueId = value; }
        public string Username { get => _username; set => _username = value; }
        public int Rank { get => _rank; set => _rank = value; }
        public string Motto { get => _motto; set => _motto = value; }
        public string Look { get => _look; set => _look = value; }
        public int BackgroundId { get => _backgroundId; set => _backgroundId = value; }
        public int StandId { get => _standId; set => _standId = value; }
        public int OverlayId { get => _overlayId; set => _overlayId = value; }

        public string GetDisplayName()
        {
            string name = this.Username;
            if (!string.IsNullOrEmpty(this.NameColor))
            {
                name = (this.NameColor.ToLower() == "rainbow")
                    ? CommandManager.GenerateRainbowText(this.Username)
                    : $"<font color='#{this.NameColor}'>{this.Username}</font>";
            }

            if (!string.IsNullOrEmpty(this.NamePrefix))
            {
                name = $"{this.NamePrefix} {name}";
            }

            return name;
        }

        public string Gender { get => _gender; set => _gender = value; }
        public string FootballLook { get => _footballLook; set => _footballLook = value; }
        public string FootballGender { get => _footballGender; set => _footballGender = value; }
        public int Credits { get => _credits; set => _credits = value; }
        public int Duckets { get => _duckets; set => _duckets = value; }
        public int Diamonds { get => _diamonds; set => _diamonds = value; }
        public string PinClient { get => _pinClient; set => _pinClient = value; }
        public int EventPoints { get => _eventPoints; set => _eventPoints = value; }
        public int HomeRoom { get => _homeRoom; set => _homeRoom = value; }
        public double LastOnline { get => _lastOnline; set => _lastOnline = value; }
        public int Online { get => _Online; set => _Online = value; }
        public double AccountCreated { get => _accountCreated; set => _accountCreated = value; }
        public List<int> ClientVolume { get => _clientVolume; set => _clientVolume = value; }
        public double LastNameChange { get => _lastNameChange; set => _lastNameChange = value; }
        public string MachineId { get => _machineId; set => _machineId = value; }
        public bool ChatPreference { get => _chatPreference; set => _chatPreference = value; }
        public bool FocusPreference { get => _focusPreference; set => _focusPreference = value; }
        public bool IsExpert { get => _isExpert; set => _isExpert = value; }
        public bool AppearOffline { get => _appearOffline; set => _appearOffline = value; }
        public int VIPRank { get => _vipRank; set => _vipRank = value; }
        public int TempInt { get => _tempInt; set => _tempInt = value; }
        public bool AllowTradingRequests { get => _allowTradingRequests; set => _allowTradingRequests = value; }
        public bool AllowUserFollowing { get => _allowUserFollowing; set => _allowUserFollowing = value; }
        public bool AllowFriendRequests { get => _allowFriendRequests; set => _allowFriendRequests = value; }
        public bool AllowMessengerInvites { get => _allowMessengerInvites; set => _allowMessengerInvites = value; }
        public bool AllowPetSpeech { get => _allowPetSpeech; set => _allowPetSpeech = value; }
        public bool AllowBotSpeech { get => _allowBotSpeech; set => _allowBotSpeech = value; }
        public bool AllowPublicRoomStatus { get => _allowPublicRoomStatus; set => _allowPublicRoomStatus = value; }
        internal ClubManager GetClubManager() => ClubManager;
        public bool AllowConsoleMessages { get => _allowConsoleMessages; set => _allowConsoleMessages = value; }
        public bool AllowGifts { get => _allowGifts; set => _allowGifts = value; }
        public bool AllowMimic { get => _allowMimic; set => _allowMimic = value; }
        public bool ReceiveWhispers { get => _receiveWhispers; set => _receiveWhispers = value; }
        public bool IgnorePublicWhispers { get => _ignorePublicWhispers; set => _ignorePublicWhispers = value; }
        public bool PlayingFastFood { get => _playingFastFood; set => _playingFastFood = value; }
        public FriendBarState FriendbarState { get => _friendbarState; set => _friendbarState = value; }
        public int ChristmasDay { get => _christmasDay; set => _christmasDay = value; }
        public int WantsToRideHorse { get => _wantsToRideHorse; set => _wantsToRideHorse = value; }
        public int TimeAFK { get => _timeAFK; set => _timeAFK = value; }
        public bool DisableForcedEffects { get => _disableForcedEffects; set => _disableForcedEffects = value; }
        public bool ChangingName { get => _changingName; set => _changingName = value; }
        public int FriendCount { get => _friendCount; set => _friendCount = value; }
        public double FloodTime { get => _floodTime; set => _floodTime = value; }
        public int BannedPhraseCount { get => _bannedPhraseCount; set => _bannedPhraseCount = value; }
        public bool RoomAuthOk { get => _roomAuthOk; set => _roomAuthOk = value; }
        public bool LetInAppartment { get => _letInAppartment; set => _letInAppartment = value; }
        public int CurrentRoomId { get => _currentRoomId; set => _currentRoomId = value; }
        public int QuestLastCompleted { get => _questLastCompleted; set => _questLastCompleted = value; }
        public int MessengerSpamCount { get => _messengerSpamCount; set => _messengerSpamCount = value; }
        public double MessengerSpamTime { get => _messengerSpamTime; set => _messengerSpamTime = value; }
        public double TimeMuted { get => _timeMuted; set => _timeMuted = value; }
        public double TradingLockExpiry { get => _tradingLockExpiry; set => _tradingLockExpiry = value; }
        public double SessionStart { get => _sessionStart; set => _sessionStart = value; }
        public int TentId { get => _tentId; set => _tentId = value; }
        public int HopperId { get => _hopperId; set => _hopperId = value; }
        public bool IsHopping { get => _isHopping; set => _isHopping = value; }
        public int TeleporterId { get => _teleportId; set => _teleportId = value; }
        public bool IsTeleporting { get => _isTeleporting; set => _isTeleporting = value; }
        public int TeleportingRoomID { get => _teleportingRoomId; set => _teleportingRoomId = value; }
        public bool HasSpoken { get => _hasSpoken; set => _hasSpoken = value; }
        public double LastAdvertiseReport { get => _lastAdvertiseReport; set => _lastAdvertiseReport = value; }
        public bool AdvertisingReported { get => _advertisingReported; set => _advertisingReported = value; }
        public bool AdvertisingReportedBlocked { get => _advertisingReportBlocked; set => _advertisingReportBlocked = value; }
        public int AdvertisingStrikes { get => _advertisingStrikes; set => _advertisingStrikes = value; }
        public bool WiredInteraction { get => _wiredInteraction; set => _wiredInteraction = value; }
        public bool InventoryAlert { get => _inventoryAlert; set => _inventoryAlert = value; }
        public bool IgnoreBobbaFilter { get => _ignoreBobbaFilter; set => _ignoreBobbaFilter = value; }
        public bool WiredTeleporting { get => _wiredTeleporting; set => _wiredTeleporting = value; }
        public int CustomBubbleId { get => _customBubbleId; set => _customBubbleId = value; }
        public int FastfoodScore { get => _fastfoodScore; set => _fastfoodScore = value; }
        public int PetId { get => _petId; set { if (value != _petId) PetFigure = null; this._petId = value; } }
        public int CreditsUpdateTick { get => _creditsTickUpdate; set => _creditsTickUpdate = value; }
        public IChatCommand IChatCommand { get => _iChatCommand; set => _iChatCommand = value; }
        public DateTime LastGiftPurchaseTime { get => _lastGiftPurchaseTime; set => _lastGiftPurchaseTime = value; }
        public DateTime LastMottoUpdateTime { get => _lastMottoUpdateTime; set => _lastMottoUpdateTime = value; }
        public DateTime LastClothingUpdateTime { get => _lastClothingUpdateTime; set => _lastClothingUpdateTime = value; }
        public DateTime LastForumMessageUpdateTime { get => _lastForumMessageUpdateTime; set => _lastForumMessageUpdateTime = value; }
        public int GiftPurchasingWarnings { get => _giftPurchasingWarnings; set => _giftPurchasingWarnings = value; }
        public int MottoUpdateWarnings { get => _mottoUpdateWarnings; set => _mottoUpdateWarnings = value; }
        public int ClothingUpdateWarnings { get => _clothingUpdateWarnings; set => _clothingUpdateWarnings = value; }
        public bool SessionGiftBlocked { get => _sessionGiftBlocked; set => _sessionGiftBlocked = value; }
        public Dictionary<int, UserTalent> Talents { get => _Talents; set => _Talents = value; }
        public int CurrentTalentLevel { get => _CurrentTalentLevel; set => _CurrentTalentLevel = value; }
        public bool SessionMottoBlocked { get => _sessionMottoBlocked; set => _sessionMottoBlocked = value; }
        public bool SessionClothingBlocked { get => _sessionClothingBlocked; set => _sessionClothingBlocked = value; }
        public string NameColor { get => _colour; set => _colour = value; }
        public string NamePrefix { get => _namePrefix; set => _namePrefix = value; }

        internal bool GotPollData(int pollId) => AnsweredPolls.Contains(pollId);
        public HabboStats GetStats() => _habboStats;
        public bool InRoom => CurrentRoomId >= 1 && CurrentRoom != null;
        public Room CurrentRoom { get { if (CurrentRoomId <= 0) return null; if (PolarEnvironment.GetGame().GetRoomManager().TryGetRoom(CurrentRoomId, out Room room)) return room; return null; } }
        public bool CacheExpired() { TimeSpan span = DateTime.Now - _timeCached; return span.TotalMinutes >= 30; }

        public string Frase(string Key)
        {
            using var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
            dbClient.SetQuery("SELECT `macro_val` FROM `user_macros` WHERE `user_id` = @userId AND `macro_tecla` = @key LIMIT 1");
            dbClient.AddParameter("userId", this.Id);
            dbClient.AddParameter("key", Key);
            return dbClient.getString();
        }

        public void macro(string Key, string Val, string Tecla)
        {
            using var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
            dbClient.SetQuery("INSERT INTO user_macros (user_id, macro_key, macro_val, macro_tecla) VALUES (@user_id, @key, @value, @tecla);");
            dbClient.AddParameter("user_id", this.Id);
            dbClient.AddParameter("key", Key);
            dbClient.AddParameter("value", Val);
            dbClient.AddParameter("tecla", Tecla);
            dbClient.RunQuery();
        }

        public string Tecla(string Key)
        {
            using var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
            dbClient.SetQuery("SELECT `macro_tecla` FROM `user_macros` WHERE `user_id` = @userId AND `macro_tecla` = @key LIMIT 1");
            dbClient.AddParameter("userId", this.Id);
            dbClient.AddParameter("key", Key);
            return dbClient.getString();
        }

        public bool InitProcess() { this._process = new ProcessComponent(); return this._process.Init(this); }
        public bool InitSearches() { this._navigatorSearches = new SearchesComponent(); return this._navigatorSearches.Init(this); }
        public bool InitFX() { this._fx = new EffectsComponent(); return this._fx.Init(this); }
        public bool InitClothing() { this._clothing = new ClothingComponent(); return this._clothing.Init(this); }
        public bool InitPermissions() { bool hasSpecial = (this.Id == 1 && this.VIPRank > 0); this._permissions = new PermissionComponent(hasSpecial); return this._permissions.Init(this); }

        public void LoadTalents(Dictionary<int, UserTalent> talents) => this._Talents = talents;
        public UserTalent GetTalentData(int t) { this._Talents.TryGetValue(t, out var result); return result; }
        public int GetCurrentTalentLevel() => this._Talents.Values.Select(c => PolarEnvironment.GetGame().GetTalentManager().GetTalent(c.TalentId).Level).Concat(new[] { 1 }).Max();

        public void InitInformation(UserData data)
        {
            BadgeComponent = new BadgeComponent(this, data);
            Relationships = data.Relations;
            this._prefixes = new Polar.HabboHotel.Users.Inventory.PrefixesComponent(this);
            this._prefixes.UpdateDisplayName();
        }

        public void Init(GameClient client, UserData data)
        {
            this.Achievements = data.achievements;
            this.FavoriteRooms = new ArrayList();
            foreach (int id in data.favouritedRooms) FavoriteRooms.Add(id);
            this.MutedUsers = data.ignores;
            this._client = client;
            BadgeComponent = new BadgeComponent(this, data);
            InventoryComponent = new InventoryComponent(Id, client);
            quests = data.quests;
            Messenger = new HabboMessenger(Id);
            Messenger.Init(data.friends, data.requests);
            this._friendCount = Convert.ToInt32(data.friends.Count);
            this._disconnected = false;
            UsersRooms = data.rooms;
            Relationships = data.Relations;
            this._prefixes = new Polar.HabboHotel.Users.Inventory.PrefixesComponent(this);
            this._prefixes.UpdateDisplayName();
            this.InitSearches();
            this.InitFX();
            this.InitClothing();
            this.ClubManager = new ClubManager(this.Id, data);
        }

        public void LoadTags(List<string> tags) => Tags = tags;
        public Polar.HabboHotel.Users.Inventory.PrefixesComponent GetPrefixesComponent() => this._prefixes;
        public PermissionComponent GetPermissions() => this._permissions;

        public void UpdateCreditsBalance()
        {
            if (_client == null) return;
            _client.SendMessage(new CreditBalanceComposer(_client.GetHabbo().Credits <= 0 ? 0 : _client.GetHabbo().Credits));
            using var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
            dbClient.SetQuery("UPDATE users SET credits = @credits WHERE id = @id LIMIT 1");
            dbClient.AddParameter("credits", _client.GetHabbo().Credits <= 0 ? 0 : _client.GetHabbo().Credits);
            dbClient.AddParameter("id", _client.GetHabbo().Id);
            dbClient.RunQuery();
            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(_client, "event_purse", "credits");
        }

        public void UpdateBankBalance()
        {
            if (_client == null) return;
            using var DB = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
            DB.SetQuery("UPDATE `rp_stats` SET `bank_chequings` = @moneyC, `bank_savings` = @moneyB WHERE `rp_stats`.`id` = @id");
            DB.AddParameter("id", _client.GetHabbo().Id);
            DB.AddParameter("moneyC", (_client.GetRoleplay().BankChequings <= 0 ? 0 : _client.GetRoleplay().BankSavings));
            DB.AddParameter("moneyB", (_client.GetRoleplay().BankSavings <= 0 ? 0 : _client.GetRoleplay().BankSavings));
            DB.RunQuery();
        }

        public void UpdateDucketsBalance(int duckets = 0)
        {
            if (_client == null) return;
            _client.SendMessage(new HabboActivityPointNotificationComposer(_client.GetHabbo().Duckets <= 0 ? 0 : _client.GetHabbo().Duckets, duckets));
            using var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
            dbClient.SetQuery("UPDATE users SET activity_points = @duckets WHERE id = @id LIMIT 1");
            dbClient.AddParameter("duckets", _client.GetHabbo().Duckets <= 0 ? 0 : _client.GetHabbo().Duckets);
            dbClient.AddParameter("id", _client.GetHabbo().Id);
            dbClient.RunQuery();
            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(_client, "event_purse", "duckets");
        }

        public void UpdateDiamondsBalance(int diamonds = 0)
        {
            if (_client == null) return;
            _client.SendMessage(new HabboActivityPointNotificationComposer(_client.GetHabbo().Diamonds <= 0 ? 0 : _client.GetHabbo().Diamonds, diamonds, 5));
            using var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
            dbClient.SetQuery("UPDATE users SET vip_points = @diamonds WHERE id = @id LIMIT 1");
            dbClient.AddParameter("diamonds", _client.GetHabbo().Diamonds <= 0 ? 0 : _client.GetHabbo().Diamonds);
            dbClient.AddParameter("id", _client.GetHabbo().Id);
            dbClient.RunQuery();
            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(_client, "event_purse", "diamonds");
        }

        public void UpdateEventPointsBalance()
        {
            if (_client == null) return;
            _client.SendMessage(new HabboActivityPointNotificationComposer(_client.GetHabbo().EventPoints, _client.GetHabbo().EventPoints, 103));
        }

        public void SendComposerToCorrectUsers(ServerPacket Packet)
        {
            var Client = this.GetClient();
            if (Client == null) return;
            if (this.CurrentRoom == null) { Client.SendMessage(Packet); return; }
            if (Client.GetRoleplay() == null) return;
            bool invisible = Client.GetRoleplay().Invisible;
            lock (this.CurrentRoom.GetRoomUserManager().GetRoomUsers())
            {
                foreach (var user in this.CurrentRoom.GetRoomUserManager().GetRoomUsers())
                {
                    if (user == null || user.IsBot || user.GetClient()?.GetRoleplay() == null) continue;
                    if (invisible) { if (user.GetClient().GetRoleplay().Invisible) user.GetClient().SendMessage(Packet); }
                    else user.GetClient().SendMessage(Packet);
                }
            }
        }

        public void OnDisconnect()
        {
            if (this._disconnected) { UserDataFactory.ClearUserData(Id); return; }
            try { if (this._process != null) this._process.Dispose(); } catch { }
            this._disconnected = true;
            if (this.ClubManager != null) { this.ClubManager.Clear(); this.ClubManager = null; }
            PolarEnvironment.GetGame().GetClientManager().UnregisterClient(Id, Username);
            PolarEnvironment.GetGame().GetClientManager().UnregisterClientPhone(Id, GetClient().GetRoleplay().PhoneNumber);
            if (!this._habboSaved)
            {
                this._habboSaved = true;
                using var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
                dbClient.SetQuery("UPDATE `users` SET `online` = '0', `last_online` = @lastOnline, `activity_points` = @duckets, `credits` = @credits, `vip_points` = @diamonds, `home_room` = @homeRoom, `event_points` = @eventPoints, `time_muted` = @timeMuted, `friend_bar_state` = @friendBarState WHERE id = @id LIMIT 1");
                dbClient.AddParameter("lastOnline", PolarEnvironment.GetUnixTimestamp());
                dbClient.AddParameter("duckets", this.Duckets);
                dbClient.AddParameter("credits", this.Credits);
                dbClient.AddParameter("diamonds", this.Diamonds);
                dbClient.AddParameter("homeRoom", this.HomeRoom);
                dbClient.AddParameter("eventPoints", this.EventPoints);
                dbClient.AddParameter("timeMuted", this.TimeMuted);
                int friendBarState = FriendBarStateUtility.GetInt(this._friendbarState);
                if (friendBarState < 0 || friendBarState > 2) friendBarState = 0;
                dbClient.AddParameter("friendBarState", friendBarState);
                dbClient.AddParameter("id", this.Id);
                dbClient.RunQuery();
                dbClient.SetQuery("UPDATE `user_stats` SET `roomvisits` = @roomVisits, `onlineTime` = @onlineTime, `respect` = @respect, `respectGiven` = @respectGiven, `giftsGiven` = @giftsGiven, `giftsReceived` = @giftsReceived, `dailyRespectPoints` = @dailyRespectPoints, `dailyPetRespectPoints` = @dailyPetRespectPoints, `AchievementScore` = @achievementScore, `quest_id` = @questId, `quest_progress` = @questProgress, `forum_posts` = @forumPosts WHERE `id` = @id LIMIT 1");
                dbClient.AddParameter("roomVisits", this._habboStats.RoomVisits);
                dbClient.AddParameter("onlineTime", (PolarEnvironment.GetUnixTimestamp() - this.SessionStart + this._habboStats.OnlineTime));
                dbClient.AddParameter("respect", this._habboStats.Respect);
                dbClient.AddParameter("respectGiven", this._habboStats.RespectGiven);
                dbClient.AddParameter("giftsGiven", this._habboStats.GiftsGiven);
                dbClient.AddParameter("giftsReceived", this._habboStats.GiftsReceived);
                dbClient.AddParameter("dailyRespectPoints", this._habboStats.DailyRespectPoints);
                dbClient.AddParameter("dailyPetRespectPoints", this._habboStats.DailyPetRespectPoints);
                dbClient.AddParameter("achievementScore", this._habboStats.AchievementPoints);
                dbClient.AddParameter("questId", this._habboStats.QuestID);
                dbClient.AddParameter("questProgress", this._habboStats.QuestProgress);
                dbClient.AddParameter("forumPosts", this._habboStats.ForumPosts);
                dbClient.AddParameter("id", this.Id);
                dbClient.RunQuery();
                if (GetPermissions().HasRight("mod_tickets"))
                    dbClient.RunQuery("UPDATE `moderation_tickets` SET `status` = 'open', `moderator_id` = '0' WHERE `status` ='picked' AND `moderator_id` = '" + Id + "'");
            }
            UserDataFactory.ClearUserData(Id);
            this.Dispose();
            this._client = null;
        }

        public void Dispose()
        {
            this.recentPurchases.Clear();
            if (this.InventoryComponent != null) this.InventoryComponent.SetIdleState();
            if (this.UsersRooms != null) UsersRooms.Clear();
            if (this.InRoom && this.CurrentRoom != null) this.CurrentRoom.GetRoomUserManager().RemoveUserFromRoom(this._client, false, false);
            if (Messenger != null) { this.Messenger.AppearOffline = true; this.Messenger.Destroy(); }
            if (this._fx != null) this._fx.Dispose();
            if (this._clothing != null) this._clothing.Dispose();
            if (this._permissions != null) this._permissions.Dispose();
        }

        public GameClient GetClient() => this._client ?? PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Id);
        public HabboMessenger GetMessenger() => Messenger;
        public BadgeComponent GetBadgeComponent() => BadgeComponent;
        public InventoryComponent GetInventoryComponent() => InventoryComponent;
        public SearchesComponent GetNavigatorSearches() => this._navigatorSearches;
        public EffectsComponent Effects() => this._fx;
        public ClothingComponent GetClothing() => this._clothing;
        public int GetQuestProgress(int p) { quests.TryGetValue(p, out int progress); return progress; }
        public UserAchievement GetAchievementData(string p) { Achievements.TryGetValue(p, out var achievement); return achievement; }

        public void ChangeName(string Username)
        {
            this.LastNameChange = PolarEnvironment.GetUnixTimestamp();
            this.Username = Username;
            this.SaveKey("username", Username);
            this.SaveKey("last_change", this.LastNameChange.ToString());
        }

        public void SaveKey(string Key, string Value)
        {
            using var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
            dbClient.SetQuery("UPDATE `users` SET " + Key + " = @value WHERE `id` = '" + this.Id + "' LIMIT 1;");
            dbClient.AddParameter("value", Value);
            dbClient.RunQuery();
        }

        public bool PrepareApartment(int Id, string Password)
        {
            if (this.GetClient()?.GetHabbo() == null) return false;
            if (this.GetClient().GetHabbo().IsTeleporting && this.GetClient().GetHabbo().TeleportingRoomID != Id) { this.GetClient().SendMessage(new CloseConnectionComposer()); return false; }
            if (!PolarEnvironment.GetGame().GetRoomManager().LoadRoom(Id, out Room room) || room == null) { this.GetClient().SendMessage(new CloseConnectionComposer()); return false; }
            if (room.isCrashed) { this.GetClient().SendNotification("Esta sala está corrompida :("); this.GetClient().SendMessage(new CloseConnectionComposer()); return false; }
            if (room.GetRoomUserManager().userCount >= room.UsersMax && !this.GetClient().GetHabbo().GetPermissions().HasRight("room_enter_full") && this.GetClient().GetHabbo().Id != room.OwnerId) { this.GetClient().SendMessage(new CantConnectComposer(1)); this.GetClient().SendMessage(new CloseConnectionComposer()); return false; }
            if (!this.GetClient().GetHabbo().GetPermissions().HasRight("room_ban_override") && room.UserIsBanned(this.GetClient().GetHabbo().Id))
            {
                if (room.HasBanExpired(this.GetClient().GetHabbo().Id)) room.RemoveBan(this.GetClient().GetHabbo().Id);
                else { this.GetClient().GetHabbo().RoomAuthOk = false; this.GetClient().SendMessage(new CantConnectComposer(4)); this.GetClient().SendMessage(new CloseConnectionComposer()); return false; }
            }
            this.GetClient().SendMessage(new OpenConnectionComposer());
            if (!room.CheckRights(this.GetClient(), true, true) && !this.GetClient().GetHabbo().IsTeleporting && !this.GetClient().GetHabbo().IsHopping)
            {
                if (room.State == 1 && !this.GetClient().GetHabbo().GetPermissions().HasRight("room_enter_locked"))
                {
                    if (room.UserCount > 0) { RoleplayManager.Shout(this._client, "*Toca el timbre de un apartamento y espera*", 5); room.SendMessage(new DoorbellComposer(this.GetClient().GetHabbo().Username), true); return true; }
                    else { PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(this._client, "event_apart", "msg_ele_error," + "No puedes entrar a un apartamento con timbre y nadie dentro. ¡Nadie abrirá!|"); return false; }
                }
                else if (room.State == 2 && !this.GetClient().GetHabbo().GetPermissions().HasRight("room_enter_locked"))
                {
                    if (Password.ToLower() != room.Password.ToLower() || String.IsNullOrWhiteSpace(Password)) { PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(this._client, "event_apart", "open_apart_lock," + "<b>" + room.Name + "</b><br>Clave incorrecta. Intenta nuevamente o desiste de entrar.<input type=\"hidden\" id=\"AP_Elevator_Pass_Roomid\" value=\"" + room.Id + "\">"); return false; }
                }
            }
            RoleplayManager.Shout(this.GetClient(), "*Entra a un apartamento*", 5);
            if (this.GetClient().GetHabbo().InRoom) { if (!PolarEnvironment.GetGame().GetRoomManager().TryGetRoom(this.GetClient().GetHabbo().CurrentRoomId, out Room oldRoom)) return false; oldRoom.GetRoomUserManager()?.RemoveUserFromRoom(this.GetClient(), false, false); }
            this.GetClient().GetHabbo().CurrentRoomId = room.RoomId;
            if (!EnterRoom(room)) this.GetClient().SendMessage(new CloseConnectionComposer());
            return true;
        }

        public void PrepareRoom(int Id, string Password)
        {
            if (this.GetClient()?.GetHabbo() == null) return;
            if (this.GetClient().GetHabbo().InRoom) { if (PolarEnvironment.GetGame().GetRoomManager().TryGetRoom(this.GetClient().GetHabbo().CurrentRoomId, out Room oldRoom)) oldRoom.GetRoomUserManager()?.RemoveUserFromRoom(this.GetClient(), false, false); }
            if (this.GetClient().GetRoleplay().InsideTaxi) this.GetClient().GetRoleplay().AntiArrowCheck = true;
            if (this.GetClient().GetRoleplay().InsideBus) this.GetClient().GetRoleplay().AntiArrowCheck = true;
            if (this.GetClient().GetHabbo().IsTeleporting && this.GetClient().GetHabbo().TeleportingRoomID != Id && !this.GetClient().GetRoleplay().AntiArrowCheck) { this.GetClient().SendMessage(new CloseConnectionComposer()); return; }
            if (!PolarEnvironment.GetGame().GetRoomManager().LoadRoom(Id, out Room room) || room == null) { this.GetClient().SendMessage(new CloseConnectionComposer()); return; }
            if (room.isCrashed) { this.GetClient().SendNotification("Esta habitación se ha estrellado: ("); this.GetClient().SendMessage(new CloseConnectionComposer()); return; }
            if (this.GetClient() == null) { this.GetClient().SendMessage(new CloseConnectionComposer()); return; }
            this.GetClient().GetHabbo().CurrentRoomId = room.RoomId;
            if (room.GetRoomUserManager().userCount >= room.UsersMax && !this.GetClient().GetHabbo().GetPermissions().HasRight("room_enter_full") && this.GetClient().GetHabbo().Id != room.OwnerId) { this.GetClient().SendMessage(new CantConnectComposer(1)); this.GetClient().SendMessage(new CloseConnectionComposer()); return; }
            if (!this.GetClient().GetHabbo().GetPermissions().HasRight("room_ban_override") && room.UserIsBanned(this.GetClient().GetHabbo().Id))
            {
                if (room.HasBanExpired(this.GetClient().GetHabbo().Id)) room.RemoveBan(this.GetClient().GetHabbo().Id);
                else { this.GetClient().GetHabbo().RoomAuthOk = false; this.GetClient().SendMessage(new CantConnectComposer(4)); this.GetClient().SendMessage(new CloseConnectionComposer()); return; }
            }
            this.GetClient().SendMessage(new OpenConnectionComposer());
            if (!room.CheckRights(this.GetClient(), true, true) && !this.GetClient().GetHabbo().IsTeleporting && !this.GetClient().GetHabbo().IsHopping && !this.LetInAppartment)
            {
                if (room.State == 1 && !this.GetClient().GetHabbo().GetPermissions().HasRight("room_enter_locked"))
                {
                    if (room.UserCount > 0) { this.GetClient().SendMessage(new DoorbellComposer("")); room.SendMessage(new DoorbellComposer(this.GetClient().GetHabbo().Username), true); return; }
                    else { this.GetClient().SendMessage(new FlatAccessDeniedComposer("")); this.GetClient().SendMessage(new CloseConnectionComposer()); return; }
                }
                else if (room.State == 2 && !this.GetClient().GetHabbo().GetPermissions().HasRight("room_enter_locked"))
                {
                    if (Password.ToLower() != room.Password.ToLower() || String.IsNullOrWhiteSpace(Password)) { this.GetClient().SendMessage(new GenericErrorComposer(-100002)); this.GetClient().SendMessage(new CloseConnectionComposer()); return; }
                }
            }
            if (!EnterRoom(room)) this.GetClient().SendMessage(new CloseConnectionComposer());
            this.LetInAppartment = false;
        }

        public bool EnterRoom(Room Room)
        {
            if (Room == null) this.GetClient().SendMessage(new CloseConnectionComposer());
            this.GetClient().SendMessage(new RoomReadyComposer(Room.RoomId, Room.ModelName));
            if (Room.Wallpaper != "0.0") this.GetClient().SendMessage(new RoomPropertyComposer("wallpaper", Room.Wallpaper));
            if (Room.Floor != "0.0") this.GetClient().SendMessage(new RoomPropertyComposer("floor", Room.Floor));
            this.GetClient().SendMessage(new RoomPropertyComposer("landscape", Room.Landscape));
            this.GetClient().SendMessage(new RoomRatingComposer(Room.Score, !(this.GetClient().GetHabbo().RatedRooms.Contains(Room.RoomId) || Room.OwnerId == this.GetClient().GetHabbo().Id)));
            if (Room.OwnerId != this.Id) this.GetClient().GetHabbo().GetStats().RoomVisits += 1;
            return true;
        }
        public Dictionary<int, CatalogItem> GetRecentPurchases()
        {
            return this.recentPurchases;
        }

        public void DisposeRecentPurchases()
        {
            this.recentPurchases.Clear();
        }
        internal void Poof(bool RoleplayCheck = true)
        {
            if (RoleplayCheck) HabboRoleplay.Misc.RoleplayManager.GetLookAndMotto(this.GetClient(), "poof");
            else
            {
                if (this.GetClient()?.GetHabbo()?.CurrentRoom?.GetRoomUserManager() != null)
                {
                    this.GetClient().SendMessage(new AvatarAspectUpdateComposer(this.GetClient().GetHabbo().Look, this.GetClient().GetHabbo().Gender));
                    RoomUser roomUser = this.GetClient().GetHabbo().CurrentRoom.GetRoomUserManager().GetRoomUserByHabbo(this.GetClient().GetHabbo().Id);
                    if (roomUser != null) { this.GetClient().SendMessage(new UserChangeComposer(roomUser, true)); this.GetClient().GetHabbo().CurrentRoom.SendMessage(new UserChangeComposer(roomUser, false)); }
                }
            }
        }
    }
}
