using System;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using log4net;
using Polar.Communication.Packets.Outgoing.Handshake;
using Polar.Core;
using Polar.HabboHotel.Rooms;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Achievements;
using Polar.HabboHotel.Users.Badges;
using Polar.HabboHotel.Users.Inventory;
using Polar.HabboHotel.Users.Messenger;
using Polar.HabboHotel.Users.Relationships;
using Polar.HabboHotel.Users.UserDataManagement;
using Polar.HabboHotel.Items.Crafting;

using Polar.HabboHotel.Users.Process;
using Polar.Communication.Packets.Outgoing.Inventory.Purse;
using Polar.Communication.Packets.Outgoing;

using Polar.HabboHotel.Users.Navigator.SavedSearches;
using Polar.HabboHotel.Users.Effects;
using Polar.HabboHotel.Users.Messenger.FriendBar;
using Polar.HabboHotel.Users.Clothing;
using Polar.Communication.Packets.Outgoing.Navigator;
using Polar.Communication.Packets.Outgoing.Rooms.Session;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Rooms.Chat.Commands;
using Polar.HabboHotel.Users.Permissions;
using Polar.HabboHotel.Subscriptions;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.Camera;
using Polar.HabboRoleplay.RoleplayUsers;

namespace Polar.HabboHotel.Users
{
    public class Habbo
    {
        //Roleplay Variables
        public bool BoostingCheck;
        public bool DebugStacking = false;
        public double StackHeight = 0;

        //Generic player values.
        private static readonly ILog log = LogManager.GetLogger("Polar.HabboHotel.Users");
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

        //Abilitys triggered by generic events.
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
        //Player saving.
        public bool _disconnected;
        private bool _habboSaved;
        private bool _changingName;

        //Polls
        internal HashSet<int> AnsweredPolls;
        public bool AnsweredMatchingPoll = false;

        //Crafting
        internal HashSet<CraftingRecipe> UnlockedRecipes;

        //Counters
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


        //Room related
        private int _tentId;
        private int _hopperId;
        private bool _isHopping;
        private int _teleportId;
        private bool _isTeleporting;
        private int _teleportingRoomId;
        private bool _roomAuthOk;
        private int _currentRoomId;
        private bool _letInAppartment;

        //Advertising reporting system.
        private bool _hasSpoken;
        private bool _advertisingReported;
        private double _lastAdvertiseReport;
        private bool _advertisingReportBlocked;
        private int _advertisingStrikes;

        //Values generated within the game.
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


        //Fastfood
        private int _fastfoodScore;

        //Just random fun stuff.
        private int _petId;
        private string _colour;

        //Anti-script placeholders.
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

            // Polls — ya viene precargado
            if (!IsBot && pollRow != null)
            {
                int pollId = Convert.ToInt32(pollRow["poll_id"]);
                AnsweredPolls.Add(pollId);
            }

            // Recipes — ya viene precargado
            if (!IsBot && recipesTable != null)
            {
                foreach (DataRow recipeRow in recipesTable.Rows)
                {
                    string recipeName = recipeRow["recipe"].ToString();
                    var recipe = CraftingManager.getRecipe(recipeName);
                    if (recipe != null)
                        UnlockedRecipes.Add(recipe);
                }
            }

            // Stats — ya viene precargado
            if (!IsBot)
            {
                // ── Polls ──────────────────────────────────────────────────────────────
                // Antes: SELECT `poll_id` FROM `user_polls` WHERE `user_id` = ... LIMIT 1
                // Ahora: DataRow precargado por UserDataFactory
                if (pollRow != null)
                {
                    int pollId = Convert.ToInt32(pollRow["poll_id"]);
                    if (!AnsweredPolls.Contains(pollId))
                        AnsweredPolls.Add(pollId);
                }

                // ── Recipes ────────────────────────────────────────────────────────────
                // Antes: SELECT `recipe` FROM `user_recipes` WHERE `user_id` = ...
                // Ahora: DataTable precargado por UserDataFactory
                if (recipesTable != null)
                {
                    foreach (DataRow recipeRow in recipesTable.Rows)
                    {
                        string recipeName = recipeRow["recipe"].ToString();
                        var recipe = CraftingManager.getRecipe(recipeName);
                        if (recipe == null) continue;
                        if (!UnlockedRecipes.Contains(recipe))
                            UnlockedRecipes.Add(recipe);
                    }
                }

                // ── clientVolume ────────────────────────────────────────────────────────
                // (sin cambios — no era una query, solo parseo de string)
                foreach (string str in clientVolume.Split(','))
                {
                    if (int.TryParse(str, out int val))
                        this._clientVolume.Add(val);
                    else
                        this._clientVolume.Add(100);
                }

                // ── Stats ───────────────────────────────────────────────────────────────
                // Antes: SELECT ... FROM `user_stats` WHERE `id` = @user_id LIMIT 1
                //        + INSERT si no existía + UPDATE si cambió el día
                // Ahora: DataRow precargado por UserDataFactory
                if (statsRow != null)
                {
                    try
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

                        // Si el día cambió, resetear respects
                        if (Convert.ToString(statsRow["respectsTimestamp"]) != DateTime.Today.ToString("MM/dd"))
                        {
                            this._habboStats.RespectsTimestamp = DateTime.Today.ToString("MM/dd");

                            SubscriptionData subData = null;
                            int dailyRespects = 3;

                            if (this._permissions.HasRight("mod_tool"))
                                dailyRespects = 3;
                            else if (PolarEnvironment.GetGame().GetSubscriptionManager()
                                         .TryGetSubscriptionData(VIPRank, out subData))
                                dailyRespects = subData.Respects;

                            this._habboStats.DailyRespectPoints = dailyRespects;
                            this._habboStats.DailyPetRespectPoints = dailyRespects;

                            // Única query que queda aquí: depende de dailyRespects calculado en runtime
                            using var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
                            dbClient.RunQuery(
                                $"UPDATE `user_stats` SET " +
                                $"`dailyRespectPoints` = '{dailyRespects}', " +
                                $"`dailyPetRespectPoints` = '{dailyRespects}', " +
                                $"`respectsTimestamp` = '{DateTime.Today:MM/dd}' " +
                                $"WHERE `id` = '{Id}' LIMIT 1");
                        }
                    }
                    catch (Exception e)
                    {
                        Logging.LogException(e.ToString());
                    }
                }
                else
                {
                    // Fallback: stats vacíos si por alguna razón no llegó el DataRow
                    this._habboStats = new HabboStats(0, 0, 0, 0, 0, 0, 3, 3, 0, 0, 0,
                        DateTime.Today.ToString("MM/dd"), 0);
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
            this._allowTradingRequests = true;//TODO
            this._allowUserFollowing = true;//TODO
            this._allowFriendRequests = HasFriendRequestsDisabled;//TODO
            this._allowMessengerInvites = IgnoreInvites;
            this._allowPetSpeech = PetsMuted;
            this._allowBotSpeech = BotsMuted;
            this._allowPublicRoomStatus = HideInRoom;
            this._allowConsoleMessages = true;
            this._allowGifts = AllowGifts;
            this._allowMimic = AllowMimic;
            //this._lastPhotoPreview = lastPhotoPreview;
            this._lastPhotoRandom = lastPhotoRandom;
            this._receiveWhispers = true;
            this._ignorePublicWhispers = false;
            this._playingFastFood = false;
            this._friendbarState = FriendBarStateUtility.GetEnum(FriendBarState);
            this._christmasDay = ChristmasDay;
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

            if (!IsBot)
            {
                if (this._tradingLockExpiry > 0 && PolarEnvironment.GetUnixTimestamp() > this.TradingLockExpiry)
                {
                    this._tradingLockExpiry = 0;
                    using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                    {
                        dbClient.RunQuery("UPDATE `user_info` SET `trading_locked` = '0' WHERE `user_id` = '" + Id + "' LIMIT 1");
                    }
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

            this.InitPermissions();

            if (!IsBot)
            {
                //TODO: Nope.

                #region Stats
                DataRow StatRow = null;
                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
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

                        if (Convert.ToString(StatRow["respectsTimestamp"]) != DateTime.Today.ToString("MM/dd"))
                        {
                            this._habboStats.RespectsTimestamp = DateTime.Today.ToString("MM/dd");
                            SubscriptionData SubData = null;

                            int DailyRespects = 3;

                            if (this._permissions.HasRight("mod_tool"))
                                DailyRespects = 3;
                            else if (PolarEnvironment.GetGame().GetSubscriptionManager().TryGetSubscriptionData(VIPRank, out SubData))
                                DailyRespects = SubData.Respects;

                            this._habboStats.DailyRespectPoints = DailyRespects;
                            this._habboStats.DailyPetRespectPoints = DailyRespects;

                            dbClient.RunQuery("UPDATE `user_stats` SET `dailyRespectPoints` = '" + DailyRespects + "', `dailyPetRespectPoints` = '" + DailyRespects + "', `respectsTimestamp` = '" + DateTime.Today.ToString("MM/dd") + "' WHERE `id` = '" + Id + "' LIMIT 1");
                        }
                    }
                    catch (Exception e)
                    {
                        Logging.LogException(e.ToString());
                    }
                }
                #endregion

            }
        }

        public int Id
        {
            get { return this._id; }
            set { this._id = value; }
        }

        public int TokenId
        {
            get { return this._uniqueId; }
            set { this._uniqueId = value; }
        }

        public string Username
        {
            get { return this._username; }
            set { this._username = value; }
        }

        public int Rank
        {
            get { return this._rank; }
            set { this._rank = value; }
        }

        public string Motto
        {
            get { return this._motto; }
            set { this._motto = value; }
        }
        public string Look
        {
            get { return this._look; }
            set { this._look = value; }
        }

        public int BackgroundId
        {
            get { return this._backgroundId; }
            set { this._backgroundId = value; }
        }

        public int StandId
        {
            get { return this._standId; }
            set { this._standId = value; }
        }

        public int OverlayId
        {
            get { return this._overlayId; }
            set { this._overlayId = value; }
        }

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

        public string Gender
        {
            get { return this._gender; }
            set { this._gender = value; }
        }

        public string FootballLook
        {
            get { return this._footballLook; }
            set { this._footballLook = value; }
        }

        public string FootballGender
        {
            get { return this._footballGender; }
            set { this._footballGender = value; }
        }

        public int Credits
        {
            get { return this._credits; }
            set { this._credits = value; }
        }

        public int Duckets
        {
            get { return this._duckets; }
            set { this._duckets = value; }
        }

        public int Diamonds
        {
            get { return this._diamonds; }
            set { this._diamonds = value; }
        }

        public string PinClient
        {
            get { return this._pinClient; }
            set { this._pinClient = value; }
        }

        public int EventPoints
        {
            get { return this._eventPoints; }
            set { this._eventPoints = value; }
        }

        public int HomeRoom
        {
            get { return this._homeRoom; }
            set { this._homeRoom = value; }
        }

        public double LastOnline
        {
            get { return this._lastOnline; }
            set { this._lastOnline = value; }
        }

        public int Online
        {
            get { return this._Online; }
            set { this._Online = value; }
        }

        public double AccountCreated
        {
            get { return this._accountCreated; }
            set { this._accountCreated = value; }
        }

        public List<int> ClientVolume
        {
            get { return this._clientVolume; }
            set { this._clientVolume = value; }
        }

        public double LastNameChange
        {
            get { return this._lastNameChange; }
            set { this._lastNameChange = value; }
        }

        public string MachineId
        {
            get { return this._machineId; }
            set { this._machineId = value; }
        }

        public bool ChatPreference
        {
            get { return this._chatPreference; }
            set { this._chatPreference = value; }
        }
        public bool FocusPreference
        {
            get { return this._focusPreference; }
            set { this._focusPreference = value; }
        }

        public bool IsExpert
        {
            get { return this._isExpert; }
            set { this._isExpert = value; }
        }

        public bool AppearOffline
        {
            get { return this._appearOffline; }
            set { this._appearOffline = value; }
        }

        public int VIPRank
        {
            get { return this._vipRank; }
            set { this._vipRank = value; }
        }

        public int TempInt
        {
            get { return this._tempInt; }
            set { this._tempInt = value; }
        }

        public bool AllowTradingRequests
        {
            get { return this._allowTradingRequests; }
            set { this._allowTradingRequests = value; }
        }

        public bool AllowUserFollowing
        {
            get { return this._allowUserFollowing; }
            set { this._allowUserFollowing = value; }
        }

        public bool AllowFriendRequests
        {
            get { return this._allowFriendRequests; }
            set { this._allowFriendRequests = value; }
        }

        public bool AllowMessengerInvites
        {
            get { return this._allowMessengerInvites; }
            set { this._allowMessengerInvites = value; }
        }

        public bool AllowPetSpeech
        {
            get { return this._allowPetSpeech; }
            set { this._allowPetSpeech = value; }
        }

        public bool AllowBotSpeech
        {
            get { return this._allowBotSpeech; }
            set { this._allowBotSpeech = value; }
        }

        public bool AllowPublicRoomStatus
        {
            get { return this._allowPublicRoomStatus; }
            set { this._allowPublicRoomStatus = value; }
        }

        internal ClubManager GetClubManager()
        {
            return this.ClubManager;
        }

        public bool AllowConsoleMessages
        {
            get { return this._allowConsoleMessages; }
            set { this._allowConsoleMessages = value; }
        }

        public bool AllowGifts
        {
            get { return this._allowGifts; }
            set { this._allowGifts = value; }
        }

        public bool AllowMimic
        {
            get { return this._allowMimic; }
            set { this._allowMimic = value; }
        }

        public bool ReceiveWhispers
        {
            get { return this._receiveWhispers; }
            set { this._receiveWhispers = value; }
        }

        public bool IgnorePublicWhispers
        {
            get { return this._ignorePublicWhispers; }
            set { this._ignorePublicWhispers = value; }
        }

        public bool PlayingFastFood
        {
            get { return this._playingFastFood; }
            set { this._playingFastFood = value; }
        }

        public FriendBarState FriendbarState
        {
            get { return this._friendbarState; }
            set { this._friendbarState = value; }
        }

        public int ChristmasDay
        {
            get { return this._christmasDay; }
            set { this._christmasDay = value; }
        }

        public int WantsToRideHorse
        {
            get { return this._wantsToRideHorse; }
            set { this._wantsToRideHorse = value; }
        }

        public int TimeAFK
        {
            get { return this._timeAFK; }
            set { this._timeAFK = value; }
        }

        public bool DisableForcedEffects
        {
            get { return this._disableForcedEffects; }
            set { this._disableForcedEffects = value; }
        }

        public bool ChangingName
        {
            get { return this._changingName; }
            set { this._changingName = value; }
        }

        public int FriendCount
        {
            get { return this._friendCount; }
            set { this._friendCount = value; }
        }

        public double FloodTime
        {
            get { return this._floodTime; }
            set { this._floodTime = value; }
        }

        public int BannedPhraseCount
        {
            get { return this._bannedPhraseCount; }
            set { this._bannedPhraseCount = value; }
        }

        public bool RoomAuthOk
        {
            get { return this._roomAuthOk; }
            set { this._roomAuthOk = value; }
        }

        public bool LetInAppartment
        {
            get { return this._letInAppartment; }
            set { this._letInAppartment = value; }
        }


        public int CurrentRoomId
        {
            get { return this._currentRoomId; }
            set { this._currentRoomId = value; }
        }

        public int QuestLastCompleted
        {
            get { return this._questLastCompleted; }
            set { this._questLastCompleted = value; }
        }

        public int MessengerSpamCount
        {
            get { return this._messengerSpamCount; }
            set { this._messengerSpamCount = value; }
        }

        public double MessengerSpamTime
        {
            get { return this._messengerSpamTime; }
            set { this._messengerSpamTime = value; }
        }

        public double TimeMuted
        {
            get { return this._timeMuted; }
            set { this._timeMuted = value; }
        }

        public double TradingLockExpiry
        {
            get { return this._tradingLockExpiry; }
            set { this._tradingLockExpiry = value; }
        }

        public double SessionStart
        {
            get { return this._sessionStart; }
            set { this._sessionStart = value; }
        }

        public int TentId
        {
            get { return this._tentId; }
            set { this._tentId = value; }
        }

        public int HopperId
        {
            get { return this._hopperId; }
            set { this._hopperId = value; }
        }

        public bool IsHopping
        {
            get { return this._isHopping; }
            set { this._isHopping = value; }
        }

        public int TeleporterId
        {
            get { return this._teleportId; }
            set { this._teleportId = value; }
        }

        public bool IsTeleporting
        {
            get { return this._isTeleporting; }
            set { this._isTeleporting = value; }
        }

        public int TeleportingRoomID
        {
            get { return this._teleportingRoomId; }
            set { this._teleportingRoomId = value; }
        }

        public bool HasSpoken
        {
            get { return this._hasSpoken; }
            set { this._hasSpoken = value; }
        }

        public double LastAdvertiseReport
        {
            get { return this._lastAdvertiseReport; }
            set { this._lastAdvertiseReport = value; }
        }

        public bool AdvertisingReported
        {
            get { return this._advertisingReported; }
            set { this._advertisingReported = value; }
        }

        public bool AdvertisingReportedBlocked
        {
            get { return this._advertisingReportBlocked; }
            set { this._advertisingReportBlocked = value; }
        }

        public int AdvertisingStrikes
        {
            get { return this._advertisingStrikes; }
            set { this._advertisingStrikes = value; }
        }

        public bool WiredInteraction
        {
            get { return this._wiredInteraction; }
            set { this._wiredInteraction = value; }
        }

        public bool InventoryAlert
        {
            get { return this._inventoryAlert; }
            set { this._inventoryAlert = value; }
        }

        public bool IgnoreBobbaFilter
        {
            get { return this._ignoreBobbaFilter; }
            set { this._ignoreBobbaFilter = value; }
        }

        public bool WiredTeleporting
        {
            get { return this._wiredTeleporting; }
            set { this._wiredTeleporting = value; }
        }

        public int CustomBubbleId
        {
            get { return this._customBubbleId; }
            set { this._customBubbleId = value; }
        }

        public int FastfoodScore
        {
            get { return this._fastfoodScore; }
            set { this._fastfoodScore = value; }
        }

        public int PetId
        {
            get { return this._petId; }
            set
            {

                if (value != _petId)
                {
                    PetFigure = null;
                }

                this._petId = value;
            }
        }

        public int CreditsUpdateTick
        {
            get { return this._creditsTickUpdate; }
            set { this._creditsTickUpdate = value; }
        }

        public IChatCommand IChatCommand
        {
            get { return this._iChatCommand; }
            set { this._iChatCommand = value; }
        }

        public DateTime LastGiftPurchaseTime
        {
            get { return this._lastGiftPurchaseTime; }
            set { this._lastGiftPurchaseTime = value; }
        }

        public DateTime LastMottoUpdateTime
        {
            get { return this._lastMottoUpdateTime; }
            set { this._lastMottoUpdateTime = value; }
        }

        public DateTime LastClothingUpdateTime
        {
            get { return this._lastClothingUpdateTime; }
            set { this._lastClothingUpdateTime = value; }
        }

        public DateTime LastForumMessageUpdateTime
        {
            get { return this._lastForumMessageUpdateTime; }
            set { this._lastForumMessageUpdateTime = value; }
        }

        public int GiftPurchasingWarnings
        {
            get { return this._giftPurchasingWarnings; }
            set { this._giftPurchasingWarnings = value; }
        }

        public int MottoUpdateWarnings
        {
            get { return this._mottoUpdateWarnings; }
            set { this._mottoUpdateWarnings = value; }
        }

        public int ClothingUpdateWarnings
        {
            get { return this._clothingUpdateWarnings; }
            set { this._clothingUpdateWarnings = value; }
        }

        public bool SessionGiftBlocked
        {
            get { return this._sessionGiftBlocked; }
            set { this._sessionGiftBlocked = value; }
        }

        public Dictionary<int, UserTalent> Talents
        {
            get { return this._Talents; }
            set { this._Talents = value; }
        }

        public int CurrentTalentLevel
        {
            get { return this._CurrentTalentLevel; }
            set { this._CurrentTalentLevel = value; }
        }

        public bool SessionMottoBlocked
        {
            get { return this._sessionMottoBlocked; }
            set { this._sessionMottoBlocked = value; }
        }

        public bool SessionClothingBlocked
        {
            get { return this._sessionClothingBlocked; }
            set { this._sessionClothingBlocked = value; }
        }

        public string NameColor
        {
            get { return this._colour; }
            set { this._colour = value; }
        }

        public string NamePrefix
        {
            get { return this._namePrefix; }
            set { this._namePrefix = value; }
        }

        internal bool GotPollData(int pollId)
        {
            if (AnsweredPolls.Contains(pollId))
                return true;
            else
                return false;
        }

        public HabboStats GetStats()
        {
            return this._habboStats;
        }

        public bool InRoom
        {
            get
            {
                return CurrentRoomId >= 1 && CurrentRoom != null;
            }
        }

        public Room CurrentRoom
        {
            get
            {
                if (CurrentRoomId <= 0)
                    return null;

                Room _room = null;
                if (PolarEnvironment.GetGame().GetRoomManager().TryGetRoom(CurrentRoomId, out _room))
                    return _room;

                return null;
            }
        }

        public bool CacheExpired()
        {
            TimeSpan Span = DateTime.Now - _timeCached;
            return (Span.TotalMinutes >= 30);
        }

        public string Frase(string Key)
        {
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT `macro_val` FROM `user_macros` WHERE `user_id` = @userId AND `macro_tecla` = @key LIMIT 1");
                dbClient.AddParameter("userId", this.Id);
                dbClient.AddParameter("key", Key);
                string Val = dbClient.getString();


                return Val;

            }
        }

        public void macro(string Key, string Val, string Tecla)
        {
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("INSERT INTO user_macros (user_id, macro_key, macro_val, macro_tecla) VALUES (@user_id, @key, @value, @tecla);");
                dbClient.AddParameter("user_id", this.Id);
                dbClient.AddParameter("key", Key);
                dbClient.AddParameter("value", Val);
                dbClient.AddParameter("tecla", Tecla);
                dbClient.RunQuery();
            }
        }
        public string Tecla(string Key)
        {
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT `macro_tecla` FROM `user_macros` WHERE `user_id` = @userId AND `macro_tecla` = @key LIMIT 1");
                dbClient.AddParameter("userId", this.Id);
                dbClient.AddParameter("key", Key);
                string Val = dbClient.getString();


                return Val;

            }
        }
        public bool InitProcess()
        {
            this._process = new ProcessComponent();
            if (this._process.Init(this))
                return true;
            return false;
        }

        public bool InitSearches()
        {
            this._navigatorSearches = new SearchesComponent();
            if (this._navigatorSearches.Init(this))
                return true;
            return false;
        }

        public bool InitFX()
        {
            this._fx = new EffectsComponent();
            if (this._fx.Init(this))
                return true;
            return false;
        }

        public bool InitClothing()
        {
            this._clothing = new ClothingComponent();
            if (this._clothing.Init(this))
                return true;
            return false;
        }

        public bool InitPermissions()
        {
            //  bool HasSpecialRights = (this.Id == 1 && this.VIPRank == 2) ? true : false;
            bool HasSpecialRights = (this.Id == 1 && this.VIPRank > 0 ) ? true : false;
            this._permissions = new PermissionComponent(HasSpecialRights);
            if (this._permissions.Init(this))
                return true;
            return false;
        }


        public void LoadTalents(Dictionary<int, UserTalent> talents)
        {
            this._Talents = talents;
        }

        public UserTalent GetTalentData(int t)
        {
            UserTalent result;
            this._Talents.TryGetValue(t, out result);

            return result;
        }

        public int GetCurrentTalentLevel()
        {
            int level = this._Talents.Values.Select(current => PolarEnvironment.GetGame().GetTalentManager().GetTalent(current.TalentId).Level).Concat(new[] { 1 }).Max();
            return level;
        }


        public void InitInformation(UserData data)
        {
            BadgeComponent = new BadgeComponent(this, data);
            Relationships = data.Relations;
        }

        public void Init(GameClient client, UserData data)
        {
            this.Achievements = data.achievements;

            this.FavoriteRooms = new ArrayList();
            foreach (int id in data.favouritedRooms)
            {
                FavoriteRooms.Add(id);
            }

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

            this.InitSearches();
            this.InitFX();
            //this.LoadTalents(data.Talents);
            this.InitClothing();
            this.ClubManager = new ClubManager(this.Id, data);
            //this.LoadTags(data.Tags);
        }

        public void LoadTags(List<string> tags)
        {
            Tags = tags;
        }

        public PermissionComponent GetPermissions()
        {
            return this._permissions;
        }


        public void UpdateCreditsBalance()
        {
            if (_client == null)
                return;

            _client.SendMessage(new CreditBalanceComposer(_client.GetHabbo().Credits <= 0 ? 0 : _client.GetHabbo().Credits));
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("UPDATE users SET credits = @credits WHERE id = @id LIMIT 1");
                dbClient.AddParameter("credits", _client.GetHabbo().Credits <= 0 ? 0 : _client.GetHabbo().Credits);
                dbClient.AddParameter("id", _client.GetHabbo().Id);
                dbClient.RunQuery();
            }
            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(_client, "event_purse", "credits");
        }
        public void UpdateBankBalance()
        {
            if (_client == null)
                return;

            using (var DB = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                DB.SetQuery("UPDATE `rp_stats` SET `bank_chequings` = @moneyC, `bank_savings` = @moneyB WHERE `rp_stats`.`id` = @id");
                DB.AddParameter("id", _client.GetHabbo().Id);
                DB.AddParameter("moneyC", (_client.GetRoleplay().BankChequings <= 0 ? 0 : _client.GetRoleplay().BankSavings));
                DB.AddParameter("moneyB", (_client.GetRoleplay().BankSavings <= 0 ? 0 : _client.GetRoleplay().BankSavings));
                DB.RunQuery();
                // Agregar UpdateBalance WS
            }
        }

        public void UpdateDucketsBalance(int duckets = 0)
        {
            if (_client == null)
                return;

            if (duckets == 0)
            {
                _client.SendMessage(new HabboActivityPointNotificationComposer(_client.GetHabbo().Duckets <= 0 ? 0 : _client.GetHabbo().Duckets, 0));
            }
            else
            {
                _client.SendMessage(new HabboActivityPointNotificationComposer(_client.GetHabbo().Duckets <= 0 ? 0 : _client.GetHabbo().Duckets, duckets));
            }
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("UPDATE users SET activity_points = @duckets WHERE id = @id LIMIT 1");
                dbClient.AddParameter("duckets", _client.GetHabbo().Duckets <= 0 ? 0 : _client.GetHabbo().Duckets);
                dbClient.AddParameter("id", _client.GetHabbo().Id);
                dbClient.RunQuery();
            }
            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(_client, "event_purse", "duckets");
        }

        public void UpdateDiamondsBalance(int diamonds = 0)
        {
            if (_client == null)
                return;

            if (diamonds == 0)
            {
                _client.SendMessage(new HabboActivityPointNotificationComposer(_client.GetHabbo().Diamonds <= 0 ? 0 : _client.GetHabbo().Diamonds, 0, 5));
            }
            else
            {
                _client.SendMessage(new HabboActivityPointNotificationComposer(_client.GetHabbo().Diamonds <= 0 ? 0 : _client.GetHabbo().Diamonds, diamonds, 5));
            }
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("UPDATE users SET vip_points = @diamonds WHERE id = @id LIMIT 1");
                dbClient.AddParameter("diamonds", _client.GetHabbo().Diamonds <= 0 ? 0 : _client.GetHabbo().Diamonds);
                dbClient.AddParameter("id", _client.GetHabbo().Id);
                dbClient.RunQuery();
            }
            //_client.SendMessage(new HabboActivityPointNotificationComposer(_client.GetHabbo().Diamonds, 0, 5));
            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(_client, "event_purse", "diamonds");
        }


        public void UpdateEventPointsBalance()
        {
            if (_client == null)
                return;

            _client.SendMessage(new HabboActivityPointNotificationComposer(_client.GetHabbo().EventPoints, _client.GetHabbo().EventPoints, 103));
        }

        public void SendComposerToCorrectUsers(ServerPacket Packet)
        {
            var Client = this.GetClient();

            if (Client == null)
                return;

            if (this.CurrentRoom == null)
            {
                Client.SendMessage(Packet);
                return;
            }

            if (Client.GetRoleplay() == null)
                return;

            bool Invisible = Client.GetRoleplay().Invisible;

            lock (this.CurrentRoom.GetRoomUserManager().GetRoomUsers())
            {
                foreach (var user in this.CurrentRoom.GetRoomUserManager().GetRoomUsers())
                {
                    if (user == null)
                        continue;

                    if (user.IsBot)
                        continue;

                    if (user.GetClient() == null)
                        continue;

                    if (user.GetClient().GetRoleplay() == null)
                        continue;

                    if (Invisible)
                    {
                        if (user.GetClient().GetRoleplay().Invisible)
                            user.GetClient().SendMessage(Packet);
                    }
                    else
                    {
                        user.GetClient().SendMessage(Packet);
                    }
                }
            }
        }

        public void OnDisconnect()
        {
            if (this._disconnected)
            {
                // Aunque ya esté disconnected, SIEMPRE limpiar el caché
                UserDataFactory.ClearUserData(Id);
                return;
            }

            try
            {
                if (this._process != null)
                    this._process.Dispose();
            }
            catch { }

            this._disconnected = true;

            if (this.ClubManager != null)
            {
                this.ClubManager.Clear();
                this.ClubManager = null;
            }

            PolarEnvironment.GetGame().GetClientManager().UnregisterClient(Id, Username);
            PolarEnvironment.GetGame().GetClientManager().UnregisterClientPhone(Id, GetClient().GetRoleplay().PhoneNumber);

            if (!this._habboSaved)
            {
                this._habboSaved = true;
                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("UPDATE `users` SET `online` = '0', `last_online` = @lastOnline, `activity_points` = @duckets, `credits` = @credits, `vip_points` = @diamonds, `home_room` = @homeRoom, `event_points` = @eventPoints, `time_muted` = @timeMuted, `friend_bar_state` = @friendBarState WHERE id = @id LIMIT 1");
                    dbClient.AddParameter("lastOnline", PolarEnvironment.GetUnixTimestamp());
                    dbClient.AddParameter("duckets", this.Duckets);
                    dbClient.AddParameter("credits", this.Credits);
                    dbClient.AddParameter("diamonds", this.Diamonds);
                    dbClient.AddParameter("homeRoom", this.HomeRoom);
                    dbClient.AddParameter("eventPoints", this.EventPoints);
                    dbClient.AddParameter("timeMuted", this.TimeMuted);

                    int friendBarState = FriendBarStateUtility.GetInt(this._friendbarState);
                    if (friendBarState < 0 || friendBarState > 2)
                        friendBarState = 0;
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
            }

            UserDataFactory.ClearUserData(Id); // ← siempre se ejecuta
            this.Dispose();
            this._client = null;
        }

        public void Dispose()
        {
            if (this.InventoryComponent != null)
                this.InventoryComponent.SetIdleState();

            if (this.UsersRooms != null)
                UsersRooms.Clear();

            if (this.InRoom && this.CurrentRoom != null)
                this.CurrentRoom.GetRoomUserManager().RemoveUserFromRoom(this._client, false, false);

            if (Messenger != null)
            {
                this.Messenger.AppearOffline = true;
                this.Messenger.Destroy();
            }

            if (this._fx != null)
                this._fx.Dispose();

            if (this._clothing != null)
                this._clothing.Dispose();

            if (this._permissions != null)
                this._permissions.Dispose();
        }

        public GameClient GetClient()
        {
            if (this._client != null)
                return this._client;

            return PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Id);
        }

        public HabboMessenger GetMessenger()
        {
            return Messenger;
        }

        public BadgeComponent GetBadgeComponent()
        {
            return BadgeComponent;
        }

        public InventoryComponent GetInventoryComponent()
        {
            return InventoryComponent;
        }

        public SearchesComponent GetNavigatorSearches()
        {
            return this._navigatorSearches;
        }

        public EffectsComponent Effects()
        {
            return this._fx;
        }

        public ClothingComponent GetClothing()
        {
            return this._clothing;
        }

        public int GetQuestProgress(int p)
        {
            int progress = 0;
            quests.TryGetValue(p, out progress);
            return progress;
        }

        public UserAchievement GetAchievementData(string p)
        {
            UserAchievement achievement = null;
            Achievements.TryGetValue(p, out achievement);
            return achievement;
        }

        public void ChangeName(string Username)
        {
            this.LastNameChange = PolarEnvironment.GetUnixTimestamp();
            this.Username = Username;

            this.SaveKey("username", Username);
            this.SaveKey("last_change", this.LastNameChange.ToString());
        }

        public void SaveKey(string Key, string Value)
        {
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("UPDATE `users` SET " + Key + " = @value WHERE `id` = '" + this.Id + "' LIMIT 1;");
                dbClient.AddParameter("value", Value);
                dbClient.RunQuery();
            }
        }

        public bool PrepareApartment(int Id, string Password)
        {
            if (this.GetClient() == null || this.GetClient().GetHabbo() == null)
                return false;

            if (this.GetClient().GetHabbo().IsTeleporting && this.GetClient().GetHabbo().TeleportingRoomID != Id)
            {
                this.GetClient().SendMessage(new CloseConnectionComposer());
                return false;
            }

            Room Room;
            if (!PolarEnvironment.GetGame().GetRoomManager().LoadRoom(Id, out Room) || Room == null)
            {
                this.GetClient().SendMessage(new CloseConnectionComposer());
                return false;
            }

            if (Room.isCrashed)
            {
                this.GetClient().SendNotification("Esta sala está corrompida :(");
                this.GetClient().SendMessage(new CloseConnectionComposer());
                return false;
            }

            if (Room.GetRoomUserManager().userCount >= Room.UsersMax && !this.GetClient().GetHabbo().GetPermissions().HasRight("room_enter_full") && this.GetClient().GetHabbo().Id != Room.OwnerId)
            {
                this.GetClient().SendMessage(new CantConnectComposer(1));
                this.GetClient().SendMessage(new CloseConnectionComposer());
                return false;
            }

            if (!this.GetClient().GetHabbo().GetPermissions().HasRight("room_ban_override") && Room.UserIsBanned(this.GetClient().GetHabbo().Id))
            {
                if (Room.HasBanExpired(this.GetClient().GetHabbo().Id))
                    Room.RemoveBan(this.GetClient().GetHabbo().Id);
                else
                {
                    this.GetClient().GetHabbo().RoomAuthOk = false;
                    this.GetClient().SendMessage(new CantConnectComposer(4));
                    this.GetClient().SendMessage(new CloseConnectionComposer());
                    return false;
                }
            }

            this.GetClient().SendMessage(new OpenConnectionComposer());

            if (!Room.CheckRights(this.GetClient(), true, true) && !this.GetClient().GetHabbo().IsTeleporting && !this.GetClient().GetHabbo().IsHopping)
            {
                if (Room.State == 1 && !this.GetClient().GetHabbo().GetPermissions().HasRight("room_enter_locked"))
                {
                    if (Room.UserCount > 0)
                    {
                        RoleplayManager.Shout(this._client, "*Toca el timbre de un apartamento y espera*", 5);
                        //this.GetClient().SendMessage(new DoorbellComposer(""));
                        Room.SendMessage(new DoorbellComposer(this.GetClient().GetHabbo().Username), true);
                        return true;
                    }
                    else
                    {
                        PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(this._client, "event_apart", "msg_ele_error," + "No puedes entrar a un apartamento con timbre y nadie dentro. ¡Nadie abrirá!|");
                        //this.GetClient().SendMessage(new FlatAccessDeniedComposer(""));
                        //this.GetClient().SendMessage(new CloseConnectionComposer());
                        return false;
                    }
                }
                else if (Room.State == 2 && !this.GetClient().GetHabbo().GetPermissions().HasRight("room_enter_locked"))
                {
                    if (Password.ToLower() != Room.Password.ToLower() || String.IsNullOrWhiteSpace(Password))
                    {
                        PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(this._client, "event_apart", "open_apart_lock," + "<b>" + Room.Name + "</b><br>Clave incorrecta. Intenta nuevamente o desiste de entrar.<input type=\"hidden\" id=\"AP_Elevator_Pass_Roomid\" value=\"" + Room.Id + "\">");
                        //this.GetClient().SendMessage(new GenericErrorComposer(-100002));
                        //this.GetClient().SendMessage(new CloseConnectionComposer());
                        return false;
                    }
                }
            }

            RoleplayManager.Shout(this.GetClient(), "*Entra a un apartamento*", 5);

            if (this.GetClient().GetHabbo().InRoom)
            {
                Room OldRoom = null;
                if (!PolarEnvironment.GetGame().GetRoomManager().TryGetRoom(this.GetClient().GetHabbo().CurrentRoomId, out OldRoom))
                    return false;

                if (OldRoom.GetRoomUserManager() != null)
                    OldRoom.GetRoomUserManager().RemoveUserFromRoom(this.GetClient(), false, false);
            }

            this.GetClient().GetHabbo().CurrentRoomId = Room.RoomId;

            if (!EnterRoom(Room))
                this.GetClient().SendMessage(new CloseConnectionComposer());

            return true;
        }

        public void PrepareRoom(int Id, string Password)
        {
            if (this.GetClient() == null || this.GetClient().GetHabbo() == null)
                return;

            if (this.GetClient().GetHabbo().InRoom)
            {
                Room OldRoom = null;
                if (!PolarEnvironment.GetGame().GetRoomManager().TryGetRoom(this.GetClient().GetHabbo().CurrentRoomId, out OldRoom))
                    return;

                if (OldRoom.GetRoomUserManager() != null)
                    OldRoom.GetRoomUserManager().RemoveUserFromRoom(this.GetClient(), false, false);
            }

            if (this.GetClient().GetRoleplay().InsideTaxi)
                this.GetClient().GetRoleplay().AntiArrowCheck = true;

            if (this.GetClient().GetRoleplay().InsideBus)
                this.GetClient().GetRoleplay().AntiArrowCheck = true;

            if (this.GetClient().GetHabbo().IsTeleporting && this.GetClient().GetHabbo().TeleportingRoomID != Id && !this.GetClient().GetRoleplay().AntiArrowCheck)
            {
                this.GetClient().SendMessage(new CloseConnectionComposer());
                return;
            }

            Room Room;
            if (!PolarEnvironment.GetGame().GetRoomManager().LoadRoom(Id, out Room) || Room == null)
            {
                this.GetClient().SendMessage(new CloseConnectionComposer());
                return;
            }

            if (Room.isCrashed)
            {
                this.GetClient().SendNotification("Esta habitación se ha estrellado: (");
                this.GetClient().SendMessage(new CloseConnectionComposer());
                return;
            }

            if (this.GetClient() == null)
            {
                this.GetClient().SendMessage(new CloseConnectionComposer());
                return;
            }

            if (this.GetClient().GetHabbo() == null)
            {
                this.GetClient().SendMessage(new CloseConnectionComposer());
                return;
            }

            this.GetClient().GetHabbo().CurrentRoomId = Room.RoomId;

            #region Non-RP Features

            if (Room.GetRoomUserManager().userCount >= Room.UsersMax && !this.GetClient().GetHabbo().GetPermissions().HasRight("room_enter_full") && this.GetClient().GetHabbo().Id != Room.OwnerId)
            {
                this.GetClient().SendMessage(new CantConnectComposer(1));
                this.GetClient().SendMessage(new CloseConnectionComposer());
                return;
            }

            if (!this.GetClient().GetHabbo().GetPermissions().HasRight("room_ban_override") && Room.UserIsBanned(this.GetClient().GetHabbo().Id))
            {
                if (Room.HasBanExpired(this.GetClient().GetHabbo().Id))
                    Room.RemoveBan(this.GetClient().GetHabbo().Id);
                else
                {
                    this.GetClient().GetHabbo().RoomAuthOk = false;
                    this.GetClient().SendMessage(new CantConnectComposer(4));
                    this.GetClient().SendMessage(new CloseConnectionComposer());
                    return;
                }
            }

            this.GetClient().SendMessage(new OpenConnectionComposer());
            if (!Room.CheckRights(this.GetClient(), true, true) && !this.GetClient().GetHabbo().IsTeleporting && !this.GetClient().GetHabbo().IsHopping && !this.LetInAppartment)
            {
                if (Room.State == 1 && !this.GetClient().GetHabbo().GetPermissions().HasRight("room_enter_locked"))
                {
                    if (Room.UserCount > 0)
                    {
                        //RoleplayManager.Shout(this._client, "*Toca el timbre de un apartamento y espera*", 5);
                        this.GetClient().SendMessage(new DoorbellComposer(""));
                        Room.SendMessage(new DoorbellComposer(this.GetClient().GetHabbo().Username), true);
                        return;
                    }
                    else
                    {
                        this.GetClient().SendMessage(new FlatAccessDeniedComposer(""));
                        this.GetClient().SendMessage(new CloseConnectionComposer());
                        return;
                    }
                }
                else if (Room.State == 2 && !this.GetClient().GetHabbo().GetPermissions().HasRight("room_enter_locked"))
                {
                    if (Password.ToLower() != Room.Password.ToLower() || String.IsNullOrWhiteSpace(Password))
                    {
                        this.GetClient().SendMessage(new GenericErrorComposer(-100002));
                        this.GetClient().SendMessage(new CloseConnectionComposer());
                        return;
                    }
                }
            }
            #endregion

            if (!EnterRoom(Room))
                this.GetClient().SendMessage(new CloseConnectionComposer());

            this.LetInAppartment = false;
        }

        public bool EnterRoom(Room Room)
        {
            if (Room == null)
                this.GetClient().SendMessage(new CloseConnectionComposer());

            this.GetClient().SendMessage(new RoomReadyComposer(Room.RoomId, Room.ModelName));
            if (Room.Wallpaper != "0.0")
                this.GetClient().SendMessage(new RoomPropertyComposer("wallpaper", Room.Wallpaper));
            if (Room.Floor != "0.0")
                this.GetClient().SendMessage(new RoomPropertyComposer("floor", Room.Floor));

            this.GetClient().SendMessage(new RoomPropertyComposer("landscape", Room.Landscape));
            this.GetClient().SendMessage(new RoomRatingComposer(Room.Score, !(this.GetClient().GetHabbo().RatedRooms.Contains(Room.RoomId) || Room.OwnerId == this.GetClient().GetHabbo().Id)));

            /* Evitamos esta consulta para ahorrar memoria.
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.RunQuery("INSERT INTO user_roomvisits (user_id,room_id,entry_timestamp,exit_timestamp,hour,minute) VALUES ('" + this.GetClient().GetHabbo().Id + "','" + this.GetClient().GetHabbo().CurrentRoomId + "','" + PolarEnvironment.GetUnixTimestamp() + "','0','" + DateTime.Now.Hour + "','" + DateTime.Now.Minute + "');");// +
            }*/


            if (Room.OwnerId != this.Id)
            {
                this.GetClient().GetHabbo().GetStats().RoomVisits += 1;
                //PolarEnvironment.GetGame().GetAchievementManager().ProgressAchievement(this.GetClient(), "ACH_RoomEntry", 1);
                //PolarEnvironment.GetGame().GetAchievementManager().ProgressAchievement(this.GetClient(), "ACH_RoomEntry", 1);
            }

            return true;
        }

        internal void Poof(bool RoleplayCheck = true)
        {
            if (RoleplayCheck)
                HabboRoleplay.Misc.RoleplayManager.GetLookAndMotto(this.GetClient(), "poof");
            else
            {
                if (this.GetClient() != null && this.GetClient().GetHabbo() != null && this.GetClient().GetHabbo().CurrentRoom != null && this.GetClient().GetHabbo().CurrentRoom.GetRoomUserManager() != null)
                {
                    this.GetClient().SendMessage(new AvatarAspectUpdateComposer(this.GetClient().GetHabbo().Look, this.GetClient().GetHabbo().Gender));

                    RoomUser RoomUser = this.GetClient().GetHabbo().CurrentRoom.GetRoomUserManager().GetRoomUserByHabbo(this.GetClient().GetHabbo().Id);
                    if (RoomUser != null)
                    {
                        this.GetClient().SendMessage(new UserChangeComposer(RoomUser, true));
                        this.GetClient().GetHabbo().CurrentRoom.SendMessage(new UserChangeComposer(RoomUser, false));
                    }
                }
            }
        }
    }

}