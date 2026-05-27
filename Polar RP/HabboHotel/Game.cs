using JNogueira.Discord.Webhook.Client;
using log4net;
using Polar.Communication.Packets;
using Polar.Core;
using Polar.Core.FigureData;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Achievements;
using Polar.HabboHotel.Animations;
using Polar.HabboHotel.Badges;
using Polar.HabboHotel.BattlePass;
using Polar.HabboHotel.Bots;
using Polar.HabboHotel.Cache;
using Polar.HabboHotel.Catalog;
using Polar.HabboHotel.Catalog.FurniMatic;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Games;
using Polar.HabboHotel.Global;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.Guides;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Items.Crafting;
using Polar.HabboHotel.Items.Televisions;
using Polar.HabboHotel.LandingView;
using Polar.HabboHotel.Moderation;
using Polar.HabboHotel.Navigator;
using Polar.HabboHotel.Permissions;
using Polar.HabboHotel.Polls;
using Polar.HabboHotel.Quests;
using Polar.HabboHotel.Rewards;
using Polar.HabboHotel.Roleplay.Web;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Chat;
using Polar.HabboHotel.Rooms.TraxMachine;
using Polar.HabboHotel.Subscriptions;
using Polar.HabboHotel.Support;
using Polar.HabboHotel.Talents;
using Polar.HabboRoleplay.Apartments;
using Polar.HabboRoleplay.ApartmentsOwned;
using Polar.HabboRoleplay.Combat;
using Polar.HabboRoleplay.Comodin;
using Polar.HabboRoleplay.Events;
using Polar.HabboRoleplay.Farming;
using Polar.HabboRoleplay.Food;
using Polar.HabboRoleplay.Gambling;
using Polar.HabboRoleplay.Houses;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.PhoneChat;
using Polar.HabboRoleplay.PhoneOwned;
using Polar.HabboRoleplay.Phones;
using Polar.HabboRoleplay.PhonesApps;
using Polar.HabboRoleplay.PlayInternet;
using Polar.HabboRoleplay.Products;
using Polar.HabboRoleplay.RPRoom;
using Polar.HabboRoleplay.Skins;
using Polar.HabboRoleplay.Turfs;
using Polar.HabboRoleplay.VehicleOwned;
using Polar.HabboRoleplay.Vehicles;
using Polar.HabboRoleplay.VehiclesJobs;
using Polar.HabboRoleplay.Weapons;
using Polar.HabboRoleplay.Web.Util.ChatRoom;
using Polar.HabboRoleplay.Wizards;
using Polar.Messages.Net.MusCommunication;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Polar.HabboHotel
{
    public class Game
    {
        private static readonly ILog log = LogManager.GetLogger("Polar.HabboHotel.Game");

        // Ping packet: 4 bytes length (big-endian = 2) + 2 bytes header 21 (0x00 0x15)
        private static readonly byte[] PingPacket = { 0x00, 0x00, 0x00, 0x02, 0x00, 0x15 };
        private static readonly TimeSpan PingThreshold       = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan DisconnectThreshold = TimeSpan.FromMinutes(10);
        private const int GameLoopDelayMs = 480;
        private const int HighLatencyWarnMs = 480;

        // Core
        private PacketManager         _packetManager;
        private MusPacketManager      _muspacketManager;
        private GameClientManager     _clientManager;
        private ModerationManager     _modManager;
        private ModerationTool        _moderationTool;
        private ItemDataManager       _itemDataManager;
        private CatalogManager        _catalogManager;
        private NavigatorManager      _navigatorManager;
        private RoomManager           _roomManager;
        private ChatManager           _chatManager;
        private GroupManager          _groupManager;
        private QuestManager          _questManager;
        private AnimationManager      _animationManager;
        private AchievementManager    _achievementManager;
        private TalentManager         _talentManager;
        private TalentTrackManager    _talentTrackManager;
        private LandingViewManager    _landingViewManager;
        private GameDataManager       _gameDataManager;
        private LanguageLocale        _languageLocale;
        private BotManager            _botManager;
        private CacheManager          _cacheManager;
        private RewardManager         _rewardManager;
        private BadgeManager          _badgeManager;
        private PermissionManager     _permissionManager;
        private SubscriptionManager   _subscriptionManager;
        private GuideManager          _guideManager;
        private PollManager           _pollManager;
        private HouseManager          _houseManager;
        private ApartmentOwnedManager _apartmentownedManager;
        private WebEventManager       _webEventManager;
        private BattlePassManager _battlePassManager;
        private CrackableManager      _crackableManager;
        private TargetedOffersManager _targetedoffersManager;
        private FurniMaticRewardsManager _furniMaticRewardsManager;
        private PhoneChatManager      _phonechatManager;
        private VehiclesOwnedManager  _vehiclesownedManager;
        private PhonesOwnedManager    _phonesownedManager;
        private TurfManager           _gangturfsManager;
        private HallOfFame            _hallOfFame;
        private TelevisionManager     _televisionManager;
        public  RPRoomManager         _rproomManager;
        public FigureDataManager _figureManager;
        // Game loop
        private Task _gameLoop;
        public  static bool gameLoopEnabled = true;
        public  bool gameLoopActive;
        public  bool gameLoopEnded;
        private bool _cycleEnded;
        private readonly Stopwatch _moduleWatch = new();
        public  bool ClientManagerCycleEnded;

        public Game()
        {
            AbstractBar bar = new AnimatedBar();
            const int wait = 15, end = 5;

            LoadCore(bar, wait, end);
            LoadHotel(bar, wait, end);
            LoadRoleplay(bar, wait, end);
        }

        // ─── Initialization ────────────────────────────────────────────────────────

        private void LoadCore(AbstractBar bar, int wait, int end)
        {
            Progress(bar, wait, end, "Cargando Packets...");
            _packetManager    = new PacketManager();
            _muspacketManager = new MusPacketManager();

            Progress(bar, wait, end, "Cargando GameClientManager...");
            _clientManager  = new GameClientManager();
            _modManager     = new ModerationManager();
            _moderationTool = new ModerationTool();

            Progress(bar, wait, end, "Cargando configuraciones extras...");
            ExtraSettings.RunExtraSettings();
            CatalogSettings.RunCatalogSettings();

            Progress(bar, wait, end, "Cargando Items...");
            _itemDataManager = new ItemDataManager();

            Progress(bar, wait, end, "Cargando Catálogo...");
            _catalogManager = new CatalogManager();

            Progress(bar, wait, end, "Cargando Televisiones...");
            _televisionManager = new TelevisionManager();

            Progress(bar, wait, end, "Cargando FigureData...");
            _figureManager = new FigureDataManager();
            _figureManager.Init();
        }

        private void LoadHotel(AbstractBar bar, int wait, int end)
        {
            Progress(bar, wait, end, "Cargando Navegador de salas...");
            _navigatorManager = new NavigatorManager();

            Progress(bar, wait, end, "Cargando Salas...");
            _roomManager = new RoomManager();
            _roomManager.LoadModels();

            Progress(bar, wait, end, "Cargando Administrador de chat...");
            _chatManager = new ChatManager();

            Progress(bar, wait, end, "Cargando Misiones...");
            _questManager = new QuestManager();

            Progress(bar, wait, end, "Cargando Logros y talentos...");
            _achievementManager  = new AchievementManager();
            _talentManager       = new TalentManager();
            _talentManager.Initialize();
            _talentTrackManager  = new TalentTrackManager();

            Progress(bar, wait, end, "Cargando Vista del hotel...");
            _landingViewManager = new LandingViewManager();

            Progress(bar, wait, end, "Cargando Datos del juego...");
            _gameDataManager = new GameDataManager();

            Progress(bar, wait, end, "Cargando Actualizaciones del sistema...");
            ServerStatusUpdater.Init();

            Progress(bar, wait, end, "Cargando Lenguajes...");
            _languageLocale = new LanguageLocale();

            Progress(bar, wait, end, "Cargando Bots...");
            _botManager = new BotManager();

            Progress(bar, wait, end, "Cargando Administrador de caché...");
            _cacheManager = new CacheManager();

            Progress(bar, wait, end, "Cargando Recompensas...");
            _rewardManager = new RewardManager();

            Progress(bar, wait, end, "Cargando Jukebox...");
            TraxSoundManager.Init();

            Progress(bar, wait, end, "Cargando Administrador de placas...");
            _badgeManager = new BadgeManager();
            _badgeManager.Init();

            Progress(bar, wait, end, "Cargando Permisos...");
            _permissionManager = new PermissionManager();
            _permissionManager.Init();

            Progress(bar, wait, end, "Cargando Suscripciones...");
            _subscriptionManager = new SubscriptionManager();
            _subscriptionManager.Init();

            _guideManager = new GuideManager();

            Progress(bar, wait, end, "Cargando Salón de la fama...");
            _hallOfFame = new HallOfFame();

            Progress(bar, wait, end, "Cargando Administrador de encuestas...");
            _pollManager = new PollManager();
            _pollManager.Init(out _);

            Progress(bar, wait, end, "Cargando Crafting...");
            _crackableManager = new CrackableManager();
            _crackableManager.Initialize(PolarEnvironment.GetDatabaseManager().GetQueryReactor());

            Progress(bar, wait, end, "Cargando Mega Ofertas...");
            _targetedoffersManager = new TargetedOffersManager();
            _targetedoffersManager.Initialize(PolarEnvironment.GetDatabaseManager().GetQueryReactor());

            Progress(bar, wait, end, "Cargando Furni-Matic...");
            this._furniMaticRewardsManager = new FurniMaticRewardsManager();
            this._furniMaticRewardsManager.Initialize(PolarEnvironment.GetDatabaseManager().GetQueryReactor());
        }

        private void LoadRoleplay(AbstractBar bar, int wait, int end)
        {
            Progress(bar, wait, end, "Cargando Sistema Roleplay...");

            RoleplayData.Initialize();
            EventManager.Initialize();
            CombatManager.Initialize();

            _groupManager = new GroupManager();
            _groupManager.Initialize();

            TexasHoldEmManager.Initialize();

            _gangturfsManager = new TurfManager();
            _gangturfsManager.Initialize();

            WeaponManager.Initialize();
            WSkinManager.Initialize();
            HechizosManager.Initialize();
            FoodManager.Initialize();
            FarmingManager.Initialize();
            CraftingManager.Initialize();
            LotteryManager.Initialize();
            ToDoManager.Initialize();
            BlackListManager.Initialize();
            BountyManager.Initialize();
            WebSocketChatManager.Initialiaze();
            ProductsManager.Initialize();
            RoleplayManager.AssingInventoryProducts();

            _houseManager = new HouseManager();
            _houseManager.Init();

            _rproomManager = new RPRoomManager();
            _rproomManager.Init();

            VehicleManager.Initialize();
            VehicleJobsManager.Initialize();
            PhoneManager.Initialize();
            PhoneAppManager.Initialize();
            PlayInternetManager.Init();

            _phonechatManager = new PhoneChatManager();
            _phonechatManager.Init();

            _vehiclesownedManager = new VehiclesOwnedManager();
            _vehiclesownedManager.Init();

            _phonesownedManager = new PhonesOwnedManager();
            _phonesownedManager.Init();

            ComodinManager.Initialize();
            ApartmentManager.Init();

            _webEventManager = new WebEventManager();
            _webEventManager.Init();

            _animationManager = new AnimationManager();
            _animationManager.Init();

            _apartmentownedManager = new ApartmentOwnedManager();
            _apartmentownedManager.Init();

            Progress(bar, wait, end, "Cargando BattlePass...");
            _battlePassManager = new BattlePassManager();
            _battlePassManager.Initialize(PolarEnvironment.GetDatabaseManager().GetQueryReactor());

        }

        public async Task InitializeAsync()
        {
            await _itemDataManager.InitAsync();
            await _catalogManager.InitAsync(_itemDataManager);
        }

        // ─── Game Loop ─────────────────────────────────────────────────────────────

        public void StartGameLoop()
        {
            gameLoopActive = true;
            _gameLoop = MainGameLoop();
        }

        private async Task MainGameLoop()
        {
            while (gameLoopActive)
            {
                _cycleEnded = false;
                try
                {
                    if (gameLoopEnabled)
                    {
                        RunAndWarn("ServerStatusUpdater", ServerStatusUpdater.Process);
                        RunAndWarn("GameClientManager", () =>
                        {
                            _clientManager.OnCycle();
                            CheckClientConnections();
                        });
                        HuntManager.Initialize();
                    }
                }
                catch (OperationCanceledException e)
                {
                    Console.WriteLine("Canceled operation {0}", e);
                }

                _cycleEnded = true;
                await Task.Delay(GameLoopDelayMs);
            }

            Console.WriteLine("MainGameLoop end");
            gameLoopEnded = true;
        }

        private void RunAndWarn(string label, Action action)
        {
            _moduleWatch.Restart();
            action();
            if (_moduleWatch.ElapsedMilliseconds > HighLatencyWarnMs)
                Console.WriteLine("High latency in {0} ({1} ms)", label, _moduleWatch.ElapsedMilliseconds);
        }

        // ─── Client Ping / Disconnect ───────────────────────────────────────────────

        private void CheckClientConnections()
        {
            var clients = _clientManager?.GetClients;
            if (clients == null) return;

            var now = DateTime.UtcNow;

            foreach (var client in clients.ToList())
            {
                var conn = client?.GetConnection();
                if (conn == null) continue;

                var idle = now - conn.LastReceiveUtc;

                if (idle > DisconnectThreshold)
                {
                    try { client.Disconnect(true); } catch { }
                    continue;
                }

                if (idle > PingThreshold)
                {
                    try { conn.SendData(PingPacket, 0, PingPacket.Length); } catch { }
                }
            }
        }


        public FigureDataManager GetFigureManager() => _figureManager;
        // ─── Discord ────────────────────────────────────────────────────────────────

        public async Task SendMsPhoto(GameClient author, string image)
        {
            var cfg = PolarEnvironment.GetConfig().data;
            var client = new DiscordWebhookClient(cfg["Webhook_URL"]);

            var message = new DiscordMessage(
                "Una nueva foto! " + DiscordEmoji.Grinning,
                username:  cfg["Webhook_Username"],
                avatarUrl: cfg["Webhook_Image"],
                tts: false,
                embeds: new[]
                {
                    new DiscordMessageEmbed(
                        "Notificación de login" + DiscordEmoji.Thumbsup,
                        color:       4833120,
                        author:      new DiscordMessageEmbedAuthor(author.GetHabbo().Username),
                        description: "Ha ingresado al cliente del hotel",
                        image:       new DiscordMessageEmbedImage(image),
                        footer:      new DiscordMessageEmbedFooter("Creado por " + cfg["Webhook_Username"], cfg["Webhook_Image"])
                    )
                }
            );

            await client.SendToDiscord(message);
            Console.WriteLine("Foto enviada a Discord.", ConsoleColor.DarkCyan);
        }

        // ─── Helpers ────────────────────────────────────────────────────────────────

        public static void Progress(AbstractBar bar, int wait, int end, string message)
        {
            bar.PrintMessage(message);
            for (var i = 0; i < end; i++)
                bar.Step();
        }

        public static void DatabaseCleanup()
        {
            using IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
            dbClient.RunQuery("UPDATE users     SET online = '0'       WHERE online != '0'");
            dbClient.RunQuery("UPDATE users     SET auth_ticket = ''   WHERE auth_ticket != ''");
            dbClient.RunQuery("UPDATE rooms     SET users_now = '0'    WHERE users_now > '0'");
            dbClient.RunQuery("UPDATE server_status SET users_online = '0', loaded_rooms = '0'");
        }

        public async Task DestroyAsync()
        {
            DatabaseCleanup();
            Console.WriteLine("Disposing RoomManager...");
            await _roomManager.DisposeAsync();
            Console.WriteLine("Apagando servidor...");
        }

        // ─── Accessors ──────────────────────────────────────────────────────────────

        public PacketManager          GetPacketManager()          => _packetManager;
        public MusPacketManager       GetMusPacketManager()       => _muspacketManager;
        public GameClientManager      GetClientManager()          => _clientManager;
        public CatalogManager         GetCatalog()                => _catalogManager;
        public NavigatorManager       GetNavigator()              => _navigatorManager;
        public ItemDataManager        GetItemManager()            => _itemDataManager;
        public RoomManager            GetRoomManager()            => _roomManager;
        public AnimationManager       GetAnimationManager()       => _animationManager;
        public AchievementManager     GetAchievementManager()     => _achievementManager;
        public ChatManager            GetChatManager()            => _chatManager;
        public GroupManager           GetGroupManager()           => _groupManager;
        public QuestManager           GetQuestManager()           => _questManager;
        public TalentManager          GetTalentManager()          => _talentManager;
        public TalentTrackManager     GetTalentTrackManager()     => _talentTrackManager;
        public LandingViewManager     GetLandingManager()         => _landingViewManager;
        public TelevisionManager      GetTelevisionManager()      => _televisionManager;
        public GameDataManager        GetGameDataManager()        => _gameDataManager;
        public LanguageLocale         GetLanguageLocale()         => _languageLocale;
        public BotManager             GetBotManager()             => _botManager;
        public CacheManager           GetCacheManager()           => _cacheManager;
        public RewardManager          GetRewardManager()          => _rewardManager;
        public BadgeManager           GetBadgeManager()           => _badgeManager;
        public PermissionManager      GetPermissionManager()      => _permissionManager;
        public SubscriptionManager    GetSubscriptionManager()    => _subscriptionManager;
        public HouseManager           GetHouseManager()           => _houseManager;
        public ApartmentOwnedManager  GetApartmentOwnedManager()  => _apartmentownedManager;
        public WebEventManager        GetWebEventManager()        => _webEventManager;
        public PhoneChatManager       GetPhoneChatManager()       => _phonechatManager;
        public PhonesOwnedManager     GetPhonesOwnedManager()     => _phonesownedManager;
        public VehiclesOwnedManager   GetVehiclesOwnedManager()   => _vehiclesownedManager;
        public TurfManager            GetGangTurfsManager()       => _gangturfsManager;
        public HallOfFame             GetHallOfFame()             => _hallOfFame;
        public RPRoomManager          GetRPRoomManager()          => _rproomManager;
        public BattlePassManager GetBattlePassManager() => _battlePassManager;
        public TargetedOffersManager  GetTargetedOffersManager()  => _targetedoffersManager;
        public CrackableManager       GetPinataManager()          => _crackableManager;
        public FurniMaticRewardsManager GetFurniMaticRewardsMnager() => _furniMaticRewardsManager;
        public ModerationTool         GetModerationTool()         => _moderationTool;
        public ModerationManager      GetModerationManager()      => _modManager;

        internal GuideManager         GetGuideManager()           => _guideManager;
        internal PollManager          GetPollManager()            => _pollManager;
    }
}