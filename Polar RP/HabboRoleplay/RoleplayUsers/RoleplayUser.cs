using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboRoleplay.Timers;
using Polar.HabboRoleplay.Cooldowns;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Events;
using Polar.Database.Interfaces;
using Polar.HabboRoleplay.Weapons;
using Polar.HabboRoleplay.RoleplayUsers.Offers;
using Polar.HabboHotel.Catalog.Clothing;
using Polar.HabboRoleplay.Turfs;
using Polar.HabboHotel.Users.Messenger;
using Polar.HabboRoleplay.Bots.Manager;
using Polar.HabboRoleplay.Bots;
using Polar.HabboRoleplay.ProductOwned;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.BattlePass;
using Polar.Communication.Packets.Outgoing.Inventory.Weapons;
using Polar.HabboHotel.Polls;
using Polar.HabboRoleplay.Farming;
using Polar.HabboRoleplay.Web.Outgoing.Statistics;
using Polar.HabboRoleplay.Web.Util.ChatRoom;
using Newtonsoft.Json;
using Polar.HabboRoleplay.Timers.Types;
using System.Drawing;
using Polar.HabboRoleplay.PhoneOwned;
using Polar.HabboRoleplay.PhoneAppOwned;
using Polar.HabboRoleplay.PhonesApps;
using Polar.HabboRoleplay.Houses;
using Polar.HabboRoleplay.Wizards;
using ConnectionManager;

namespace Polar.HabboRoleplay.RoleplayUsers
{

    public enum WalkDirections
    {
        Down,
        Up,
        Left,
        Right,
        None
    }
    public class RoleplayUser
    {
        #region Saved Variables
        // Client Info
        GameClient Client;

        // Basic Info
        private bool roomEntryHandled;
        private uint mId;
        private int mLevel;
        private int mLevelEXP;
        private string mClass;
        private bool mPermanentClass;

        // Jobs
        private int mJobId;
        private int mJobRank;
        private int mJobRequest;

        // Taxi WS
        public bool LoadRoomNodes = false;
        public int LastRoomNodeLoaded = 0;
        public bool ViewTaxiList = false;
        public bool CallingTaxi = false;
        public int TaxiNodeGo = -1;
        public int TaxiLastIndex = 1;

        // Human Needs
        private int mMaxHealth;
        private int mCurHealth;
        private int mMaxEnergy;
        private int mMaxAlcohol;
        private int mCurAlcohol;
        private int mCurEnergy;
		private int mHunger;
        private int mArmor;
        private int mSida;
        private int mAnimo;
        private int mHygiene;
        private int mPoop;
        public int EquippedArmorIndex { get; set; } = -1; // -1 = ninguno equipado
        #region Mecánico
        private int mMecLvl;
        private int mMecXP;
        #endregion
        #region Camionero
        private int mCamLvl;
        private int mCamXP;
        #endregion

        #region Armero
        private int mArmLvl;
        private int mArmXP;
        #endregion

        #region Basurero
        private int mBasuLvl;
        private int mBasuXP;
        #endregion

        private int mHuntPoints;
        private string mHuntSkins;
        // God
        /*public bool FirstTickBool = false;
        public int GodModeTicks = 0;
        public bool GodMode = false;
        public bool IsGodMode = false;*/
        public int FastCarNew = 0;

        // Levelable Stats
        private int mIntelligence;
        private int mStrength;
        private int mStamina;
        public WeedTimer WeedTimer;
        #region Inmunity
        public bool RoomJoinedInmunity = false;
		public bool WalkNoDiagonal = false;
        #endregion Inmunity
        public Dictionary<string, int> MultiCoolDown = new Dictionary<string, int>();
        //private bool mCheckingMultiCooldown;

        // Jailed/Dead - Wanted/Probation - Sendhome - Cuffed
        /*private bool mIsDying;
        private int mDyingTimeLeft;*/
        private bool mIsStun;
        public bool Paralized = false;
        private bool mIsDead;
        private int mDeadTimeLeft;
        private bool mIsJailed;
        private int mJailedTimeLeft;
        private bool mIsWanted;
        private int mWantedLevel;
        private int mWantedTimeLeft;
        private bool mOnProbation;
        private int mProbationTimeLeft;
        private int mSendHomeTimeLeft;
        private bool mCuffed;
        private int mCuffedTimeLeft;
        private int mTutorialStep;
        // Tutorial
        public bool InTutorial = false;
        // CAMIONERO
        public int CamCargId = 0;

        // Ladron
        public bool IsForcingHouse = false;
        public House HouseToForce = null;
        public string Object = "";
        public int ObjectPrice = 0;

        // Timers
        public bool IsCamLoading = false;
        public bool IsCamUnLoading = false;
        // Timers
        public int LoadingTimeLeft = 0;
        public bool ViewCamCargas = false;
        // END CAMIONERO
        // Robbery/Learning
        public bool Robbery = false;
        public bool RobartiendaRobbery = false;
        public bool ATMRobbery = false;
        public int ATMRobTimeLeft = 180;
        public bool Learning = false;
        public bool ProcessCocaine = false;
        public bool ProcessWeed = false;
        public bool ProcessHeroine = false;
        public Room HRidProcess;
        public Point HRidCoordinate;
        public Item HRidItem;

        // Statistics
        private int mPunches;
        private int mKills;
        private int mHitKills;
        private int mGunKills;
        private int mDeaths;
        private int mCopDeaths;
        private int mTimeWorked;
        private int mArrests;
        private int mArrested;
        private int mEvasions;

        // Fuel
        public bool IsFuelCharging = false;
        public int FuelChargingCant = 0;

        // GeneralTimer
        public bool BreakGeneralTimer = false;
        private int mBidon;
        // Basurero
        public bool IsBasuChofer = false;
        public bool IsBasuPasaj = false;
        public int BasuTeamId = 0;
        public string BasuTeamName = "";
        public int BasuTrashCount = 0;

        public WalkDirections WalkDirection = WalkDirections.None;
        // Banking
        private int mBankAccount;
        private int mBankTarget;
        private int mBankChequings;
        private int mBankSavings;
        private int mWeedBaul;

        // Target
        public bool TargetLock = false;
        public string Target = "";
        public string LastMsgText = "";

        // Special Data
        private int mLastKilled;
        private int mMarriedTo;
        private int mHijo;
        private int mChangeNameCount;
        // Gangs
        public int HechizoDamage = 0;
        public int HechizoRange = 0;
        public int HechizoShield = 0;
        public int HechizoHealth = 0;
        public bool GroupRoom = false;
        public bool ViewHouse = false;
        public bool ViewBaul = false;
        public bool ViewCarList = false;
        public bool ViewWizardsList = false;
        public bool ViewWeaponsList = false;
        public bool ViewProducts = false;
        public bool ViewChangeName = false;
        public bool UsingPhone = false;
        public bool SendMsg = false;
        public string InApp = "";
        public bool SendToName = false;
        public bool ViewShopPhones = false;
        public bool ViewApartments = false;
        public bool ViewArmeroPieces = false;
        public bool ViewArmeroWeapons = false;
        public bool ViewHospBotiq = false;
        private int mGangId;
        private int mGangRank;
        private int mGangRequest;
        // Vehicles
        private int mCarLimit;
        private int mCarType;
        private int mCarFuel;
        private int mCarMaxFuel;

        // ARMERO
        public int ArmPiecesTo = 0;
        public int ArmUserTo = 0;
        // END ARMERO
        // Inventory
        private int mWeed;
        private int mWeedmateria;
        private int mCocaine;
        private int mHeroina;
        private int mCaramelos;
        //private int mBasura;
        private int mMedicina;
        private int mCigarettes;
        private int mPildoras;
        private int mDynamite;
        private int mBoveda;
        private int mArmMat;
        private int mArmPieces;

        // Weapons
        public int Bullets = 0;
        public int WLife = 0;

        // Phone
        public int mPhone = 0;
        public int mPhoneModelId = 0;
        public string mPhoneNumber = "";

        // Phone App Owend
        private ConcurrentDictionary<int, PhonesAppsOwned> mOwnedPhonesApps;
        private ConcurrentDictionary<string, Hechizos> mOwnedHechizos;
        private ConcurrentDictionary<string, Weapon> mOwnedWeapons;
        // Products Owned
        private ConcurrentDictionary<int, ProductsOwned> mOwnedProducts;
        public int EffectSeconds = 0;

        // Farming
        private FarmingStats mFarmingStats;

        // Extra Variables for Levelable Stats
        private int mStrengthEXP;
        private int mStaminaEXP;
        private int mIntelligenceEXP;

        // Misc
        private string[] mRPQuests;
        private int mBrawlWins;
        private int mCwWins;
        private int mMwWins;
        private int mSoloQueueWins;
        private int mVIPBanned;
        private string mLastCoordinates;

        // Passive Mode
        private bool mPassiveMode;
        public bool TogglingPSV = false;

        // Noob
        private bool mIsNoob;
        private int mNoobTimeLeft;

        // WebSocket
        private bool mBannedFromChatting;
        private bool mBannedFromMakingChat;

        public int CarJobId = 0;
        public int CarJobLastItemId = 0;

        // Check My Corp
        public bool ViewMyCorp = false;
        public int ViewCorpId = 0;
        public bool BuyingCorp = false;

        // Cars
        public bool isParking = false;// Para que deje colocar furni (users sin permisos/sin terreno)
        public int DrivingCarId = 0;
        public int DrivingCarItem = 0;
        public bool DrivingCar = false;
        public bool DrivingInCar = false;// ID en diccionario play_vehicles_owned
        public int CarEnableId = 0;
        public int CarEffectId = 0;
        public int CarTimer = 0;
        public int CarLife = 0;
        public string CarWSBaul = "";
        public int VehicleTimer = 0;
        #endregion

        #region UnSaved Variables
        // Toggles
        public bool DisableVIPA = false;
        public bool DisableRadio = false;

        public int HouseSignId = 0;
        public string HouseOwner = "";

        // Houses
        public bool ExitingHouse = false;
        public int HouseX = 0;
        public int HouseY = 0;
        public double HouseZ = 0;

        // Work
        public bool IsWorking = false;

        // Noob
        public bool NoobWarned = false;
        public bool NoobWarned2 = false;

        // Outfits
        public string OriginalOutfit = null;
        public string OriginalMotto = null;
        public ClothingItem Clothing = null;
        public bool PurchasingClothing = false;

        // Combat
        public bool AmbassadorOnDuty = false;
        public bool StaffOnDuty = false;
        public bool InCombat = false;
        public bool CombatMode = false;
        public Weapon EquippedWeapon = null;
        public string LastCommand = "";
        public int GunShots = 0;

        // Police Related
        public bool PoliceTrial = false;
        public bool Jailbroken = false;
        public string WantedFor = "";
        public bool Trialled = false;

        // Farming
        public bool WateringCan = false;
        public FarmingItem FarmingItem = null;

        //Sistema de Pasajeros Final Jeihden
        public bool Pasajero = false;
        public bool Chofer = false;
        public int ChoferID = 0;
        //Para el chofer
        public int PasajerosCount = 0;
        public string Pasajeros = "";
        public string ChoferName = "";
        public GameClient ChoferClient = null;
        private int mMecParts;

        // Misc
        public int TextTimer = 0;
        public int RapeTimer = 0;
        public int KissTimer = 0;
        public int HugTimer = 0;
        public int SexTimer = 0;
        public int ChalecoPor = 0;
        public int RobBankActive = 0;
        public int Embarazo = 0;
        public int TexasHoldEmPlayer = 0;
        public bool CraftingCheck = true;
        public bool InsideTaxi = false;
        public bool InsideBus = false;
        public bool AntiArrowCheck = false;
        public bool BeingHealed = false;
        public int ItemId = 1014;
        public bool IsWorkingOut = false;
        public bool InShower = false;
        public bool InCagar = false;
        public Item RobItem = null;
        public bool FreeNameChange = false;
        public bool HighOffHeroina = false;
        public bool HighOffCocaine = false;
        public bool HighOffMedicina = false;
        public bool HighOffWeed = false;
        public ConcurrentDictionary<int, List<PollQuestion>> AnsweredPollQuestions;

        // ATM Poll
        public string ATMAccount = "";
        public string ATMAction = "";
        public bool ATMFailed = false;

        // Manages the timers for the user
        public TimerManager TimerManager;

        // Manages the cooldowns for the user
        public CooldownManager CooldownManager;

        // Saved Cooldowns
        public ConcurrentDictionary<string, int> SpecialCooldowns = new ConcurrentDictionary<string, int>();

        // BattlePass Data
        public BattlePassUserData BattlePassData;

        // Manages the offers for the user
        public OfferManager OfferManager;

        // Handles the users data
        public UserDataHandler UserDataHandler;

        // Minigames
        public bool GameSpawned = false;
        public bool NeedsRespawn = false;

        // Police
        public GameClient GuideOtherUser = null;
        public bool SentRealCall = false;
        public bool Sent911Call = false;
        public string CallMessage = "";
        public bool HandlingCalls = false;
        public bool HandlingJailbreaks = false;
        public bool HandlingHeists = false;

        // Phone Chats
        public int LastChatID = 0;
        public string LastChat = null;
        public bool UpdateChats = false;
        public string LastWhatsChat = null;
        public bool UpdateWhatsChats = false;

        // Mecanic
        public int MecPartsTo = 0;
        public int MecPriceTo = 0;
        public bool IsMecLoading = false;
        public int MecUserToRepair = 0;
        public int MecCarToRepair = 0;
        public int MecNewState = 0;
        public int MecRotPosition = 0;
        public Point MecCordinates;
        public bool PediMec = false;
        public bool PediArm = false;

        // Turfs
        public Turf CapturingTurf = null;

        // Gangs
        public bool BankCapturing = false;
        public bool TurfCapturing = false;
        public int TurfFlagId = 0;

        // HOSPITAL
        // Hosp Herido
        public string HeridaName = null;
        public int RevisPaci = 0;
        public bool Revisado = false;
        public int HeridaPaci = 0;
        //:usarbotiquin
        public int BotiquinDoc = 0;
        public string BotiquinName = null;
        // Ambulancia
        public bool PediMedico = false;
        public bool TargetReanim = false;
        public int HospReanim = 0;
        // END HOSPITAL

        public bool IsDisconnecting = false;

        // WebSocket
        public int UserViewing = 0;


        // Internet
        public List<string> InternetHisto = null;
        public string InternetCurPage = "";
        public ConnectionInformation WebSocketConnection
        {
            get
            {
                if (PolarEnvironment.GetGame().GetWebEventManager() != null)
                    return PolarEnvironment.GetGame().GetWebEventManager().GetUsersConnection(Client);
                else
                    return null;
            }
        }
        //public WalkDirections WalkDirection = WalkDirections.None;
        public bool ArrowEnabled = false;
        public bool CaptchaSent = false;
        public int CaptchaTime = 0;
        public List<int> ATMAmount = new List<int>();
        public bool InState = false;
        public bool UsingAtm = false;
        public bool UsingCola = false;
        public bool UsingAgua = false;
        public bool UsingCaramelo = false;
        public bool UsingCajero = false;
        public bool UsingComida = false;
        public ConcurrentDictionary<string, WebSocketChatRoom> ChatRooms = new ConcurrentDictionary<string, WebSocketChatRoom>();
        public int socketChatSpamCount = 0;
        public int socketChatSpamTicks = 0;
        public double socketChatFloodTime = 0;
        public bool DownloadingApplication = false;

        #endregion

        #region Getters & Setters
        public int Level
        {
            get { return mLevel; }
            set { mLevel = value; }
        }
        public int LevelEXP
        {
            get { return mLevelEXP; }
            set { mLevelEXP = value; }
        }
        public string Class
        {
            get { return mClass; }
            set { mClass = value; }
        }
        public bool PermanentClass
        {
            get { return mPermanentClass; }
            set { mPermanentClass = value; }
        }
        public int JobId
        {
            get { return mJobId; }
            set { mJobId = value; }
        }
        public int JobRank
        {
            get { return mJobRank; }
            set { mJobRank = value; }
        }
        public int JobRequest
        {
            get { return mJobRequest; }
            set { mJobRequest = value; }
        }
        public int SendHomeTimeLeft
        {
            get { return mSendHomeTimeLeft; }
            set { mSendHomeTimeLeft = value; }
        }
        public int MaxHealth
        {
            get { return mMaxHealth; }
            set { mMaxHealth = value; EventManager.TriggerEvent("OnHealthChange", Client); }
        }
        public int CurHealth
        {
            get { return mCurHealth; }
            set { mCurHealth = value; EventManager.TriggerEvent("OnHealthChange", Client); }
        }
		public int Armor
        {
            get { return mArmor; }
            set { mArmor = value; EventManager.TriggerEvent("OnHealthChange", Client); }
        }
        public int MaxEnergy
        {
            get { return mMaxEnergy; }
            set { mMaxEnergy = value; EventManager.TriggerEvent("OnHealthChange", Client); }
        }
        public int CurEnergy
        {
            get { return mCurEnergy; }
            set
            {
                mCurEnergy = value;
                RefreshStatDialogue();
                UpdateInteractingUserDialogues();
            }
        }
        public int MaxAlcohol
        {
            get { return mMaxAlcohol; }
            set { mMaxAlcohol = value; }
        }
        public int CurAlcohol
        {
            get { return mCurAlcohol; }
            set
            {
                mCurAlcohol = value;
                RefreshStatDialogue();
                UpdateInteractingUserDialogues();
            }
        }

        public bool RoomEntryHandled { get; set; } = false;
        
        public int Hunger
        {
            get { return mHunger; }
            set { mHunger = value; }
        }
        public int Sida
        {
            get { return mSida; }
            set { mSida = value; }
        }
        public int Animo
        {
            get { return mAnimo; }
            set { mAnimo = value; }
        }
        public int Hygiene
        {
            get { return mHygiene; }
            set { mHygiene = value; }
        }
        public int Poop
        {
            get { return mPoop; }
            set { mPoop = value; }
        }
        public int Intelligence
        {
            get { return mIntelligence; }
            set { mIntelligence = value; }
        }
        public int Strength
        {
            get { return mStrength; }
            set { mStrength = value; }
        }
        public int Stamina
        {
            get { return mStamina; }
            set { mStamina = value; }
        }
        public bool IsDead
        {
            get { return mIsDead; }
            set { mIsDead = value; }
        }

        public bool IsStun
        {
            get { return mIsStun; }
            set { mIsStun = value; }
        }
        public int DeadTimeLeft
        {
            get { return mDeadTimeLeft; }
            set { mDeadTimeLeft = value; }
        }
        public bool IsJailed
        {
            get { return mIsJailed; }
            set { mIsJailed = value; }
        }
        public int JailedTimeLeft
        {
            get { return mJailedTimeLeft; }
            set { mJailedTimeLeft = value; }
        }
        public bool IsWanted
        {
            get { return mIsWanted; }
            set { mIsWanted = value; }
        }
        public int WantedLevel
        {
            get { return mWantedLevel; }
            set { mWantedLevel = value; }
        }
        public int WantedTimeLeft
        {
            get { return mWantedTimeLeft; }
            set { mWantedTimeLeft = value; }
        }
        public bool OnProbation
        {
            get { return mOnProbation; }
            set { mOnProbation = value; }
        }
        public int ProbationTimeLeft
        {
            get { return mProbationTimeLeft; }
            set { mProbationTimeLeft = value; }
        }
        public bool Cuffed
        {
            get { return mCuffed; }
            set { mCuffed = value; }
        }
        public int CuffedTimeLeft
        {
            get { return mCuffedTimeLeft; }
            set { mCuffedTimeLeft = value; }
        }
        public int Punches
        {
            get { return mPunches; }
            set { mPunches = value; }
        }
        public int Kills
        {
            get { return mKills; }
            set { mKills = value; }
        }
        public int HitKills
        {
            get { return mHitKills; }
            set { mHitKills = value; }
        }
        public int GunKills
        {
            get { return mGunKills; }
            set { mGunKills = value; }
        }
        public int Deaths
        {
            get { return mDeaths; }
            set { mDeaths = value; }
        }
        public int CopDeaths
        {
            get { return mCopDeaths; }
            set { mCopDeaths = value; }
        }
        public int TimeWorked
        {
            get { return mTimeWorked; }
            set { mTimeWorked = value; }
        }
        public int Arrests
        {
            get { return mArrests; }
            set { mArrests = value; }
        }

        public int TutorialStep
        {
            get { return mTutorialStep; }
            set { mTutorialStep = value; }
        }
        public int ChangeNameCount
        {
            get { return mChangeNameCount; }
            set { mChangeNameCount = value; }
        }
        public int Arrested
        {
            get { return mArrested; }
            set { mArrested = value; }
        }
        public int Evasions
        {
            get { return mEvasions; }
            set { mEvasions = value; }
        }
        public int BankAccount
        {
            get { return mBankAccount; }
            set { mBankAccount = value; }
        }
        public int BankTarget
        {
            get { return mBankTarget; }
            set { mBankTarget = value; }
        }
        public int BankChequings
        {
            get { return mBankChequings; }
            set { mBankChequings = value; }
        }
        public int WeedBaul
        {
            get { return mWeedBaul; }
            set { mWeedBaul = value; }
        }
        public int BankSavings
        {
            get { return mBankSavings; }
            set { mBankSavings = value; }
        }
        public int CarLimit
        {
            get { return mCarLimit; }
            set { mCarLimit = value; }
        }
        public int CarType
        {
            get { return mCarType; }
            set { mCarType = value; }
        }
        public int CarFuel
        {
            get { return mCarFuel; }
            set { mCarFuel = value; }
        }
        public int CarMaxFuel
        {
            get { return mCarMaxFuel; }
            set { mCarMaxFuel = value; }
        }
        public int Weed
        {
            get { return mWeed; }
            set { mWeed = value; Client.SendMessage(new WeaponsComposer(Client)); }
        }
        public int Cocaine
        {
            get { return mCocaine; }
            set { mCocaine = value; Client.SendMessage(new WeaponsComposer(Client));}
        }

        public int Heroina
        {
            get { return mHeroina; }
            set { mHeroina = value; Client.SendMessage(new WeaponsComposer(Client)); }
        }
        public int Weedmateria
        {
            get { return mWeedmateria; }
            set { mWeedmateria = value; }
        }
        public int Caramelos
        {
            get { return mCaramelos; }
            set { mCaramelos = value; Client.SendMessage(new WeaponsComposer(Client)); }
        }

        public int ArmLvl
        {
            get { return mArmLvl; }
            set { mArmLvl = value; }
        }
        public int ArmXP
        {
            get { return mArmXP; }
            set { mArmXP = value; }
        }
        public int ArmMat
        {
            get { return mArmMat; }
            set { mArmMat = value; }
        }
        public int ArmPieces
        {
            get { return mArmPieces; }
            set { mArmPieces = value; }
        }
        public int MecLvl
        {
            get { return mMecLvl; }
            set { mMecLvl = value; }
        }
        public int MecXP
        {
            get { return mMecXP; }
            set { mMecXP = value; }
        }
        public int MecParts
        {
            get { return mMecParts; }
            set { mMecParts = value; }
        }
        /*public int Basura
        {
            get { return mBasura; }
            set { mBasura = value; }
        }*/
        public int Medicina
        {
            get { return mMedicina; }
            set { mMedicina = value; }
        }
        public int Cigarettes
        {
            get { return mCigarettes; }
            set { mCigarettes = value; }
        }
        public int Pildoras
        {
            get { return mPildoras; }
            set { mPildoras = value; }
        }
        /* Old
        public int Bullets
        {
            get { return mBullets; }
            set { mBullets = value; }
        }*/
        public int Dynamite
        {
            get { return mDynamite; }
            set { mDynamite = value; }
        }
        public int Boveda
        {
            get { return mBoveda; }
            set { mBoveda = value; }
        }
        public int LastKilled
        {
            get { return mLastKilled; }
            set { mLastKilled = value; }
        }
        public int MarriedTo
        {
            get { return mMarriedTo; }
            set { mMarriedTo = value; }
        }
        public int Hijo
        {
            get { return mHijo; }
            set { mHijo = value; }
        }
        public int GangId
        {
            get { return mGangId; }
            set { mGangId = value; }
        }
        public int GangRank
        {
            get { return mGangRank; }
            set { mGangRank = value; }
        }
        public int GangRequest
        {
            get { return mGangRequest; }
            set { mGangRequest = value; }
        }
        public FarmingStats FarmingStats
        {
            get { return mFarmingStats; }
            set { mFarmingStats = value; }
        }

        public int Phone
        {
            get { return mPhone; }
            set { mPhone = value; }
        }
        public int PhoneModelId
        {
            get { return mPhoneModelId; }
            set { mPhoneModelId = value; }
        }
        public string PhoneNumber
        {
            get { return mPhoneNumber; }
            set { mPhoneNumber = value; }
        }

        public ConcurrentDictionary<string, Hechizos> OwnedHechizos
        {
            get { return mOwnedHechizos; }
            set { mOwnedHechizos = value; }
        }
        public ConcurrentDictionary<string, Weapon> OwnedWeapons
        {
            get { return mOwnedWeapons; }
            set { mOwnedWeapons = value; }
        }

        public ConcurrentDictionary<int, ProductsOwned> OwnedProducts
        {
            get { return mOwnedProducts; }
            set { mOwnedProducts = value; }
        }

        public ConcurrentDictionary<int, PhonesAppsOwned> OwnedPhonesApps
        {
            get { return mOwnedPhonesApps; }
            set { mOwnedPhonesApps = value; }
        }
        public int StrengthEXP
        {
            get { return mStrengthEXP; }
            set { mStrengthEXP = value; }
        }
        public int StaminaEXP
        {
            get { return mStaminaEXP; }
            set { mStaminaEXP = value; }
        }
        public int IntelligenceEXP
        {
            get { return mIntelligenceEXP; }
            set { mIntelligenceEXP = value; }
        }
        public string[] RPQuests
        {
            get { return mRPQuests; }
            set { mRPQuests = value; }
        }
        public string LastCoordinates
        {
            get { return mLastCoordinates; }
            set { mLastCoordinates = value; }
        }
        public int BrawlWins
        {
            get { return mBrawlWins; }
            set { mBrawlWins = value; }
        }
        public int CwWins
        {
            get { return mCwWins; }
            set { mCwWins = value; }
        }
        public int MwWins
        {
            get { return mMwWins; }
            set { mMwWins = value; }
        }
        public int SoloQueueWins
        {
            get { return mSoloQueueWins; }
            set { mSoloQueueWins = value; }
        }
        public bool IsNoob
        {
            get { return mIsNoob; }
            set { mIsNoob = value; }
        }

        public bool PassiveMode
        {
            get { return mPassiveMode; }
            set { mPassiveMode = value; }
        }

        public int NoobTimeLeft
        {
            get { return mNoobTimeLeft; }
            set { mNoobTimeLeft = value; }
        }

        public bool BannedFromChatting
        {
            get { return mBannedFromChatting; }
            set { mBannedFromChatting = value; }
        }

        public bool BannedFromMakingChat
        {
            get { return mBannedFromMakingChat; }
            set { mBannedFromMakingChat = value; }
        }

        public int VIPBanned
        {
            get { return mVIPBanned; }
            set { mVIPBanned = value; }
        }

        public int Bidon
        {
            get { return mBidon; }
            set { mBidon = value; }
        }
        /*public bool IsDying
        {
            get { return mIsDying; }
            set { mIsDying = value; }
        }
        public int DyingTimeLeft
        {
            get { return mDyingTimeLeft; }
            set { mDyingTimeLeft = value; }
        }*/
        public ConcurrentDictionary<int, RoleplayBot> BotFriendShips
        {
            get;
            private set;
        }
        public bool Invisible
        {
            get;
            set;
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Constructs the class
        /// </summary>
        public RoleplayUser(GameClient Client, DataRow user, DataRow cooldown, DataRow farming)
        {
            // Client Info
            this.Client = Client;

            // Basic Info
            this.mId = Convert.ToUInt32(user["id"]);
            this.mLevel = Convert.ToInt32(user["level"]);
            this.mLevelEXP = Convert.ToInt32(user["level_exp"]);
            this.mClass = Convert.ToString(user["class"]);
            this.mPermanentClass = PolarEnvironment.EnumToBool(user["permanent_class"].ToString());

            // Work Related
            this.mJobId = Convert.ToInt32(user["job_id"]);
            this.mJobRank = Convert.ToInt32(user["job_rank"]);
            this.mJobRequest = Convert.ToInt32(user["job_request"]);
            this.mSendHomeTimeLeft = Convert.ToInt32(user["sendhome_time_left"]);

            // Human Needs
            this.mMaxHealth = Convert.ToInt32(user["maxhealth"]);
            this.mCurHealth = Convert.ToInt32(user["curhealth"]);
            this.mMaxEnergy = Convert.ToInt32(user["maxenergy"]);
            this.mCurEnergy = Convert.ToInt32(user["curenergy"]);
            this.mCurAlcohol = Convert.ToInt32(user["curalcohol"]);
            this.mMaxAlcohol = Convert.ToInt32(user["maxalcohol"]);
			this.mArmor = Convert.ToInt32(user["kevlar"]);
            this.mHunger = Convert.ToInt32(user["hunger"]);
            this.mSida = Convert.ToInt32(user["sida"]);
            this.mHygiene = Convert.ToInt32(user["hygiene"]);
            this.mAnimo = Convert.ToInt32(user["animo"]);
            this.mPoop = Convert.ToInt32(user["poop"]);

            // Levelable Statistics
            this.mIntelligence = Convert.ToInt32(user["intelligence"]);
            this.mStrength = Convert.ToInt32(user["strength"]);
            this.mStamina = Convert.ToInt32(user["stamina"]);

            // Extra Variables for Levelable Stats
            this.mIntelligenceEXP = Convert.ToInt32(user["intelligence_exp"]);
            this.mStrengthEXP = Convert.ToInt32(user["strength_exp"]);
            this.mStaminaEXP = Convert.ToInt32(user["stamina_exp"]);
            // Passive Mode
            this.mPassiveMode = PolarEnvironment.EnumToBool(user["passive_mode"].ToString());

            // Jailed - Dead - Wanted - Probation
            /*this.mIsDying = PolarEnvironment.EnumToBool(user["is_dying"].ToString());
            this.mDyingTimeLeft = Convert.ToInt32(user["dying_time_left"]);*/
            this.mIsStun = PolarEnvironment.EnumToBool(user["is_stun"].ToString());
            this.mIsDead = PolarEnvironment.EnumToBool(user["is_dead"].ToString());
            this.mDeadTimeLeft = Convert.ToInt32(user["dead_time_left"]);
            this.mIsJailed = PolarEnvironment.EnumToBool(user["is_jailed"].ToString());
            this.mJailedTimeLeft = Convert.ToInt32(user["jailed_time_left"]);
            this.mIsWanted = PolarEnvironment.EnumToBool(user["is_wanted"].ToString());
            this.mWantedLevel = Convert.ToInt32(user["wanted_level"]);
            this.mWantedTimeLeft = Convert.ToInt32(user["wanted_time_left"]);
            this.mOnProbation = PolarEnvironment.EnumToBool(user["on_probation"].ToString());
            this.mProbationTimeLeft = Convert.ToInt32(user["probation_time_left"]);
            this.mCuffed = PolarEnvironment.EnumToBool(user["is_cuffed"].ToString());
            this.mCuffedTimeLeft = Convert.ToInt32(user["cuffed_time_left"]);

            // Statistics
            this.mPunches = Convert.ToInt32(user["punches"]);
            this.mKills = Convert.ToInt32(user["kills"]);
            this.mHitKills = Convert.ToInt32(user["hit_kills"]);
            this.mGunKills = Convert.ToInt32(user["gun_kills"]);
            this.mDeaths = Convert.ToInt32(user["deaths"]);
            this.mCopDeaths = Convert.ToInt32(user["cop_deaths"]);
            this.mTimeWorked = Convert.ToInt32(user["time_worked"]);
            this.mArrests = Convert.ToInt32(user["arrests"]);
            this.mArrested = Convert.ToInt32(user["arrested"]);
            this.mEvasions = Convert.ToInt32(user["evasions"]);

            // Banking
            this.mBankAccount = Convert.ToInt32(user["bank_account"]);
            this.mBankTarget = Convert.ToInt32(user["bank_target"]);
            this.mBankChequings = Convert.ToInt32(user["bank_chequings"]);
            this.mBankSavings = Convert.ToInt32(user["bank_savings"]);
            this.mWeedBaul = Convert.ToInt32(user["weedbaul"]);

            // Basurero
            this.mBasuLvl = Convert.ToInt32(user["BasuLvl"]);
            this.mBasuXP = Convert.ToInt32(user["BasuXP"]);

            // Caza
            this.mHuntPoints = Convert.ToInt32(user["hunt_points"]);
            this.mHuntSkins = Convert.ToString(user["hunt_skins"]);

            // Armero
            this.mArmLvl = Convert.ToInt32(user["ArmLvl"]);
            this.mArmXP = Convert.ToInt32(user["ArmXP"]);

            // Mecánico
            this.mMecLvl = Convert.ToInt32(user["MecLvl"]);
            this.mMecXP = Convert.ToInt32(user["MecXP"]);

            this.mMecParts = 0;

            this.mTutorialStep = 36;

            //Camionero
            this.mCamLvl = Convert.ToInt32(user["CamLvl"]);
            this.mCamXP = Convert.ToInt32(user["CamXP"]);

            // Affiliations
            this.mLastKilled = Convert.ToInt32(user["last_killed"]);
            this.mMarriedTo = Convert.ToInt32(user["married_to"]);
            this.mHijo = Convert.ToInt32(user["hijo"]);

            // Gangs
            this.mGangId = Convert.ToInt32(user["gang_id"]);
            this.mGangRank = Convert.ToInt32(user["gang_rank"]);
            this.mGangRequest = Convert.ToInt32(user["gang_request"]);

            // Change Name
            this.mChangeNameCount = Convert.ToInt32(user["changename_count"]);

            // Inventory
            this.mCarType = Convert.ToInt32(user["car"]);
            this.mCarFuel = Convert.ToInt32(user["car_fuel"]);
            this.mWeed = Convert.ToInt32(user["weed"]);
            this.mCocaine = Convert.ToInt32(user["cocaine"]);
            this.mHeroina = Convert.ToInt32(user["heroina"]);
            this.mCaramelos = Convert.ToInt32(user["caramelos"]);
            //this.mBasura = Convert.ToInt32(user["basura"]);
            this.mMedicina = Convert.ToInt32(user["medicina"]);
            this.mCigarettes = Convert.ToInt32(user["cigarette"]);
            this.mPildoras = Convert.ToInt32(user["pildora"]);
            // Old this.mBullets = Convert.ToInt32(user["bullets"]);
            this.mDynamite = Convert.ToInt32(user["dynamite"]);
            this.mWeedmateria = Convert.ToInt32(user["weedmateria"]);
            // Products
            this.mOwnedProducts = LoadAndReturnProducts();
            this.mOwnedWeapons = LoadAndReturnWeapons();
            this.mOwnedHechizos = LoadAndReturnHechizos();

            // Phone
            this.mPhone = 0;
            this.mPhoneModelId = 0;
            this.mPhoneNumber = "";

            List<PhonesOwned> PO = PolarEnvironment.GetGame().GetPhonesOwnedManager().getMyPhonesOwned(Convert.ToInt32(user["id"]));
            if (PO != null && PO.Count > 0)
            {
                this.mPhone = PO[0].Id;
                this.mPhoneModelId = Convert.ToInt32(PO[0].PhoneId);
                this.mPhoneNumber = PO[0].PhoneNumber;
            }

            // Phones Apps Owned
            this.mOwnedPhonesApps = LoadAndReturnPhonesApps();


            // Farming
            this.FarmingStats = new FarmingStats(farming);

            // Misc
            this.mRPQuests = user["unlocked_quests"].ToString().Split(',');
            this.mBrawlWins = Convert.ToInt32(user["brawl_wins"]);
            this.mCwWins = Convert.ToInt32(user["cw_wins"]);
            this.mMwWins = Convert.ToInt32(user["mw_wins"]);
            this.mSoloQueueWins = Convert.ToInt32(user["soloqueue_wins"]);
            this.mIsNoob = PolarEnvironment.EnumToBool(user["is_noob"].ToString());
            this.mNoobTimeLeft = Convert.ToInt32(user["noob_time_left"]);
            this.AnsweredPollQuestions = new ConcurrentDictionary<int, List<PollQuestion>>();
            this.mVIPBanned = Convert.ToInt32(user["vip_banned"]);
            this.mLastCoordinates = user["last_coordinates"].ToString();

            // Manages the timers for the user
            this.TimerManager = new TimerManager(Client);

            // Manages the cooldowns for the user
            this.CooldownManager = new CooldownManager(Client);
            this.SpecialCooldowns = this.LoadAndReturnCooldowns(cooldown);

            // Manages the offers for the user
            this.OfferManager = new OfferManager(Client);

            // Handles the users data
            this.UserDataHandler = new UserDataHandler(Client, this);

            // Handles bot friendships
            this.BotFriendShips = new ConcurrentDictionary<int, RoleplayBot>();

            // Fun stuff
            this.Invisible = false;
            this.EffectSeconds = 0;
            this.mBidon = 0;
            this.mArmMat = 0;
            this.mArmPieces = 0;

            // WebSocket
            if (user.Table.Columns.Contains("wchat_banned"))
                this.BannedFromChatting = PolarEnvironment.EnumToBool(Convert.ToString(user["wchat_banned"]));

            if (user.Table.Columns.Contains("wchat_making_banned"))
                this.BannedFromMakingChat = PolarEnvironment.EnumToBool(Convert.ToString(user["wchat_making_banned"]));

            // Load BattlePass Data
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                this.BattlePassData = PolarEnvironment.GetGame().GetBattlePassManager().GetUserData((int)this.mId, dbClient);
            }

        }
        #endregion

        #region Methods

        /// <summary>
        /// Loads and returns the owned weapons
        /// </summary>
        /// <returns></returns>
        internal ConcurrentDictionary<string, Weapon> LoadAndReturnWeapons()
        {
            DataTable Weps = null;
            ConcurrentDictionary<string, Weapon> Weapons = new ConcurrentDictionary<string, Weapon>();

            Weapons.Clear();

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT * FROM `rp_weapons_owned` WHERE `user_id` = '" + this.mId + "' AND `baul_car_id` = '0'");
                Weps = dbClient.getTable();
                uint id = 0;

                if (Weps != null)
                {
                    foreach (DataRow Row in Weps.Rows)
                    {
                        id++;

                        string basename = Convert.ToString(Row["base_weapon"]);
                        string name = Convert.ToString(Row["name"]);
                        int mindam = Convert.ToInt32(Row["min_damage"]);
                        int maxdam = Convert.ToInt32(Row["max_damage"]);
                        int range = Convert.ToInt32(Row["range"]);
                        int totalbullets = Convert.ToInt32(Row["bullets"]);
                        bool canuse = PolarEnvironment.EnumToBool(Row["can_use"].ToString());
                        int wlife = Convert.ToInt32(Row["life"]);
                        int baulcar = Convert.ToInt32(Row["baul_car_id"]);
                        int effectid = Convert.ToInt32(Row["effectid"]);
                        if (!Weapons.ContainsKey(basename))
                        {
                            Weapon BaseWeapon = WeaponManager.getWeapon(basename);

                            if (BaseWeapon != null)
                            {
                                Weapon Weapon = new Weapon(id, basename, name, BaseWeapon.FiringText, BaseWeapon.EquipText, BaseWeapon.UnEquipText, BaseWeapon.ReloadText, BaseWeapon.Energy, effectid <= 0 ? BaseWeapon.EffectID : effectid, BaseWeapon.HandItem, range, mindam, maxdam, BaseWeapon.ClipSize, BaseWeapon.ReloadTime, BaseWeapon.Cost, BaseWeapon.CostFine, BaseWeapon.Stock, BaseWeapon.LevelRequirement, canuse, totalbullets, wlife, BaseWeapon.isVip, baulcar, BaseWeapon.Category);

                                if (Weapon != null)
                                    Weapons.TryAdd(basename, Weapon);
                            }
                        }
                    }
                }
            }

            return Weapons;
        }

        internal ConcurrentDictionary<string, Hechizos> LoadAndReturnHechizos()
        {
            DataTable Weps = null;
            ConcurrentDictionary<string, Hechizos> Wizards = new ConcurrentDictionary<string, Hechizos>();

            Wizards.Clear();

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT * FROM `rp_hechizos_owned` WHERE `user_id` = '" + this.mId + "'");
                Weps = dbClient.getTable();
                //uint id = 0;

                if (Weps != null)
                {
                    foreach (DataRow Row in Weps.Rows)
                    {
                        //id++;
                        uint Id = Convert.ToUInt32(Row["id"]);
                        string basename = Convert.ToString(Row["base_wizard"]);
                        string name = Convert.ToString(Row["name"]);
                        int power = Convert.ToInt32(Row["power"]);
                        int frange = Convert.ToInt32(Row["firingrange"]);
                        int shields = Convert.ToInt32(Row["shields"]);
                        int fdamage = Convert.ToInt32(Row["firingdamage"]);
                        int health = Convert.ToInt32(Row["health"]);
                        if (!Wizards.ContainsKey(basename))
                        {
                            Hechizos BaseWeapon = HechizosManager.getWizard(basename);

                            if (BaseWeapon != null)
                            {
                                Hechizos Hechizos = new Hechizos(Id, basename, name, BaseWeapon.Message, power, frange, shields, fdamage, health, BaseWeapon.Cost, BaseWeapon.CostFine, BaseWeapon.Stock);

                                if (Wizards != null)
                                    Wizards.TryAdd(basename, Hechizos);
                            }
                        }
                    }
                }
            }

            return Wizards;
        }


        internal ConcurrentDictionary<int, ProductsOwned> LoadAndReturnProducts()
        {
            DataTable Prods = null;
            ConcurrentDictionary<int, ProductsOwned> Products = new ConcurrentDictionary<int, ProductsOwned>();

            Products.Clear();

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT * FROM `rp_products_owned` WHERE `user_id` = '" + this.mId + "'");
                Prods = dbClient.getTable();
                int id = 0;

                if (Prods != null)
                {
                    foreach (DataRow Row in Prods.Rows)
                    {
                        id++;
                        int ProductId = Convert.ToInt32(Row["product_id"]);
                        int UserId = Convert.ToInt32(Row["user_id"]);
                        string Extradata = Convert.ToString(Row["extradata"]);

                        if (!Products.ContainsKey(ProductId))
                        {
                            ProductsOwned newPhOwn = new ProductsOwned(id, ProductId, UserId, Extradata);
                            Products.TryAdd(ProductId, newPhOwn);
                        }

                        if (ProductId == RoleplayManager.BidonID)
                            this.Bidon = Convert.ToInt32(Extradata);
                        if (ProductId == RoleplayManager.MecPartsID)
                            this.MecParts = Convert.ToInt32(Extradata);
                        if (ProductId == RoleplayManager.ArmMatID)
                            this.ArmMat = Convert.ToInt32(Extradata);
                        if (ProductId == RoleplayManager.ArmPiecesID)
                            this.ArmPieces = Convert.ToInt32(Extradata);
                    }
                }
            }

            return Products;
        }

        internal ConcurrentDictionary<int, PhonesAppsOwned> LoadAndReturnPhonesApps()
        {
            DataTable Apps = null;
            ConcurrentDictionary<int, PhonesAppsOwned> PhonesApps = new ConcurrentDictionary<int, PhonesAppsOwned>();

            PhonesApps.Clear();

            if (this.mPhone > 0)
            {
                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("SELECT * FROM `rp_phones_apps_owned` WHERE `phone_id` = '" + this.mPhone + "'");
                    Apps = dbClient.getTable();
                    int id = 0;

                    if (Apps != null)
                    {
                        foreach (DataRow Row in Apps.Rows)
                        {
                            id++;
                            int PhoneId = Convert.ToInt32(Row["phone_id"]);
                            int AppId = Convert.ToInt32(Row["app_id"]);
                            int ScreenId = Convert.ToInt32(Row["screen_id"]);
                            int SlotId = Convert.ToInt32(Row["slot_id"]);
                            string Extradata = Convert.ToString(Row["extradata"]);

                            if (!PhonesApps.ContainsKey(AppId))
                            {
                                PhonesAppsOwned newPhOwn = new PhonesAppsOwned(id, PhoneId, AppId, ScreenId, SlotId, Extradata);
                                PhonesApps.TryAdd(AppId, newPhOwn);
                            }
                        }
                    }
                }
            }

            return PhonesApps;
        }
        /// <summary>
        /// Returns a true/false bool if friends with bot
        /// </summary>
        /// <param name="BotId"></param>
        /// <returns></returns>
        /// 

        public void InitStatDialogue()
        {
            if (WebSocketConnection == null)
                return;

            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_characterbar", "");
            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_charweapons", "");
        }
        public void InitWSDialogues()
        {
            if (WebSocketConnection == null)
                return;

            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_initwsdialogues", "");
        }
        internal bool FriendsWithBot(int BotId)
        {
            return (BotFriendShips.ContainsKey(BotId));
        }

        /// <summary>
        /// Sends a message to a bot
        /// </summary>
        /// <param name="BotId"></param>
        /// <param name="Message"></param>
        internal void MessageBot(int BotId, string Message)
        {
            /*ServerPacket serverMessage = new NewConsoleMessageComposer(Client.GetHabbo().Id, Message);
            serverMessage.WriteInteger(BotId); //userid
            serverMessage.WriteString(Client.GetHabbo().Username);
            serverMessage.WriteInteger(0);*/

            RoomUser Bot = RoleplayBotManager.GetDeployedBotById(BotId - RoleplayBotManager.BotFriendMultiplyer);
            Bot.GetBotRoleplayAI().OnMessaged(Client, Message);

        }

        /// <summary>
        /// Adds a bot friendship
        /// </summary>
        /// <param name="BotId"></param>
        internal void AddBotAsFriend(int BotId)
        {
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.RunQuery("INSERT INTO `rp_bots_friendships` (`bot_id`, `user_id`) VALUES ('" + BotId + "', '" + Client.GetHabbo().Id + "')");
            }

            LoadBotFriendships();
        }

        /// <summary>
        /// Removes a bot friendship
        /// </summary>
        /// <param name="BotId"></param>
        internal void RemoveBotAsFriend(int BotId)
        {
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.RunQuery("DELETE FROM `rp_bots_friendships` WHERE `bot_id` = '" + BotId + "' AND `user_id` = '" + Client.GetHabbo().Id + "'");
            }

            Client.SendWhisper("Tu removiste correctamente a " + RoleplayBotManager.GetCachedBotById(BotId).Name + " De su lista de contactos de teléfonos", 1);
        }

        /// <summary>
        /// Loads bot friendships
        /// </summary>
        internal void LoadBotFriendships()
        {
            BotFriendShips = new ConcurrentDictionary<int, RoleplayBot>();

            DataTable BotFriends = null;

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {

                dbClient.SetQuery("SELECT `bot_id` FROM `rp_bots_friendships` WHERE `user_id` = '" + Client.GetHabbo().Id + "'");
                BotFriends = dbClient.getTable();

                if (BotFriends == null)
                    return;

                foreach (DataRow BotUser in BotFriends.Rows)
                {
                    int BotId = Convert.ToInt32(BotUser["bot_id"]);

                    RoleplayBot Botfriend = RoleplayBotManager.GetCachedBotById(BotId);

                    if (Botfriend == null)
                        return;

                    MessengerBuddy newfriend = new MessengerBuddy(Botfriend.Id + RoleplayBotManager.BotFriendMultiplyer,
                      Botfriend.Name,
                      Botfriend.Figure,
                      Botfriend.Motto, 0, false, true, true);

                    int addid = Botfriend.Id + RoleplayBotManager.BotFriendMultiplyer;

                    if (Client.GetHabbo() == null)
                        return;

                    if (Client.GetHabbo().GetMessenger() == null)
                        return;

                    if (Client.GetHabbo().GetMessenger()._friends == null)
                        return;

                    if (!Client.GetHabbo().GetMessenger()._friends.ContainsKey(addid))
                        Client.GetHabbo().GetMessenger()._friends.Add(addid, newfriend);

                    if (!BotFriendShips.ContainsKey(Botfriend.Id))
                        BotFriendShips.TryAdd(Botfriend.Id, Botfriend);

                    Client.SendMessage(Client.GetHabbo().GetMessenger().SerializeUpdate(newfriend));
                }
            }
        }

        /// <summary>
        /// Loads and returns special cooldowns
        /// </summary>
        /// <param name="Row"></param>
        /// <returns></returns>
        internal ConcurrentDictionary<string, int> LoadAndReturnCooldowns(DataRow Row)
        {
            ConcurrentDictionary<string, int> specialCooldowns = new ConcurrentDictionary<string, int>();

            int RobberyCooldown = Convert.ToInt32(Row["robbery"]);
            int TextCooldown = Convert.ToInt32(Row["text_cooldown"]);
            int RobberyCooldown2 = Convert.ToInt32(Row["robbery_bank"]);
            int MediPacks = Convert.ToInt32(Row["medipacks"]);
            int PassiveMode = Convert.ToInt32(Row["psvmode"]);
            int StunCool = Convert.ToInt32(Row["stun_cooldown"]);
            specialCooldowns.TryAdd("robbery", RobberyCooldown);
            specialCooldowns.TryAdd("text_cooldown", TextCooldown);
            specialCooldowns.TryAdd("robbery_bank", RobberyCooldown2);
            specialCooldowns.TryAdd("medipacks", MediPacks);
            specialCooldowns.TryAdd("psvmode", PassiveMode);
            specialCooldowns.TryAdd("stun_cooldown", StunCool);

            foreach (var cooldown in specialCooldowns)
            {
                if (cooldown.Value > 0)
                {
                    this.CooldownManager.CreateCooldown(cooldown.Key, 1000, cooldown.Value);
                }
            }

            return specialCooldowns;
        }

        /// <summary>
        /// Ends all timers/cooldowns and removes all offers
        /// </summary>
        public void EndCycle()
        {
            if (TimerManager != null)
            {
                TimerManager.EndAllTimers();
                TimerManager = null;
            }
            if (OfferManager != null)
            {
                OfferManager.EndAllOffers();
                OfferManager = null;
            }
            if (CooldownManager != null)
            {
                CooldownManager.EndAllCooldowns();
                CooldownManager = null;
            }

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.RunQuery("UPDATE `users` SET vip_points = '0' WHERE vip_points < 1");
             }
        }

        /// <summary>
        /// Sets stats to max
        /// </summary>
        public void ReplenishStats(bool Bool = false)
        {

            if (CurHealth < MaxHealth)
                CurHealth = MaxHealth;

            if (Hunger > 0)
                Hunger = 0;

            if (Sida > 0)
                Sida = 0;

            if (Animo < 100)
                Animo = 0;

            if (Hygiene < 100)
                Hygiene = 100;

            if (Poop < 100)
                Poop = 100;

            if (!Bool)
            {
                if (CurEnergy < MaxEnergy)
                    CurEnergy = MaxEnergy;
            }

            if (!Bool)
            {
                if (CurAlcohol < MaxAlcohol)
                    CurAlcohol = MaxAlcohol;
            }
        }

        /// <summary>
        /// Opens statistic dialogue for target user
        /// </summary>
        /// <param name="Target">User targetting</param>
        public void OpenUsersDialogue(GameClient Target)
        {
            if (Target == null)
                return;

            if (Target != Client)
            {
                if (UserViewing != Target.GetHabbo().Id)
                    UserViewing = Target.GetHabbo().Id;
            }

            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_characterbar", "" + Target.GetHabbo().Username);
            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_charweapons", "" + Target.GetHabbo().Username);
        }

        /// <summary>
        /// Closes the statistic dialogue of anybody viewing the users one
        /// </summary>
        public void CloseInteractingUserDialogues()
        {
            foreach (GameClient iClient in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
            {
                if (iClient == null)
                    continue;

                if (iClient.GetRoleplay() == null)
                    continue;

                if (iClient.GetRoleplay().UserViewing != Client.GetHabbo().Id)
                    continue;

                if (iClient.LoggingOut)
                    continue;

                if (iClient.GetRoleplay().WebSocketConnection == null)
                    continue;

                iClient.GetRoleplay().ClearWebSocketDialogue();
            }
        }

        /// <summary>
        /// Refreshes statistic dialogue of anybody viewing users one
        /// </summary>
        public void UpdateInteractingUserDialogues()
        {
            foreach (GameClient iClient in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
            {
                if (iClient == null)
                    continue;

                if (iClient.GetRoleplay() == null)
                    continue;

                if (iClient.GetRoleplay().UserViewing != Client.GetHabbo().Id)
                    continue;

                if (iClient.LoggingOut)
                    continue;

                if (iClient.GetRoleplay().WebSocketConnection == null)
                    continue;

                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(iClient, "event_characterbar", "" + Client.GetHabbo().Username);
                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(iClient, "event_charweapons", "");
            }
        }

        /// <summary>
        /// Refreshes users statistic dialogue
        /// </summary>
        public void RefreshStatDialogue()
        {
            if (WebSocketConnection == null)
                return;

            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_retrieveconnectingstatistics", "");
            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_charweapons", "");
        }

        /// <summary>
        /// Clear websocket dialogues
        /// </summary>
        public void ClearWebSocketDialogue(bool Force = false)
        {
            UserViewing = 0;

            // New Target System
            Client.GetRoleplay().Target = "";
            Client.GetRoleplay().TargetLock = false;

            GetUserComponent.ClearStatisticsDialogue(Client);
        }

        public void UpdateTimerDialogue(string timer, string action, int val1, int val2)
        {
            if (WebSocketConnection != null)
                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_timerdialogue", "action:" + action + ",timer:" + timer + ",value:" + val1 + "/" + val2 + ",bypass:true");
        }

        /// <summary>
        /// Opens the CAPTCHA box with a generated random string to the user
        /// </summary>
        /// <param name="Title"></param>
        public void CreateCaptcha(string Title)
        {
            if (this.WebSocketConnection == null)
                return;

            string Action = "create";
            string CaptchaTitle = Title;

            Random Random = new Random();
            const string AvailableCharacters = "ABCDEFGHJKMNOPQRSTUVWXYZ0123456789";
            string GeneratedString = new string(Enumerable.Repeat(AvailableCharacters, 6).Select(s => s[Random.Next(s.Length)]).ToArray());

            string Data = Action + "," + CaptchaTitle + "," + GeneratedString;

            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_captcha", Data);

            if (this.Client != null)
                this.Client.SendWhisper(Title, 1);
        }

        /// <summary>
        /// Returns the cooldown time if cooldown exists
        /// </summary>
        public bool TryGetCooldown(string cooldown, bool sendwhisper = true, bool minutes = false)
        {
            if (this.CooldownManager != null && this.CooldownManager.ActiveCooldowns != null)
            {
                if (this.CooldownManager.ActiveCooldowns.ContainsKey(cooldown.ToLower()))
                {
                    var CoolDown = this.CooldownManager.ActiveCooldowns[cooldown.ToLower()];

                    if (CoolDown == null)
                        return false;

                    if (this.Client.GetHabbo().VIPRank == 0 && CoolDown.Type.ToLower() != "fist" && CoolDown.Type.ToLower() != "reload")
                        return false;

                    if (sendwhisper && minutes == false) {
                        this.Client.SendWhisper("Espera [" + (CoolDown.TimeLeft / 1000) + "/" + CoolDown.Amount + "]!", 1);

                    }
                    else if (sendwhisper && minutes == true)
                    {
                        this.Client.SendWhisper("Debes esperar " + (CoolDown.TimeLeft / 60000) + " minutos!", 1);

                    }
                    return true;
                }
            }
            return false;
        }

        public bool CheckingCoolDown = false;
        public bool NearItem(string Item, int MaxDistance)
        {
            GameClient Session = Client;
            RoomUser User = Session.GetRoomUser();

            Item Inter = null;
            lock (Session.GetHabbo().CurrentRoom.GetRoomItemHandler().GetFloor)
            {
                foreach (Item item in Session.GetHabbo().CurrentRoom.GetRoomItemHandler().GetFloor)
                {

                    if (item == null)
                        continue;

                    if (item.GetBaseItem() == null)
                        continue;

                    HabboHotel.Pathfinding.Vector2D Pos1 = new HabboHotel.Pathfinding.Vector2D(item.GetX, item.GetY);
                    HabboHotel.Pathfinding.Vector2D Pos2 = new HabboHotel.Pathfinding.Vector2D(User.X, User.Y);

                    if (RoleplayManager.Distance(Pos1, Pos2) <= MaxDistance)
                    {
                        if (!item.GetBaseItem().ItemName.Contains(Item))
                            continue;

                        Inter = item;

                    }
                }

                if (Inter != null)
                {
                    return true;
                }
            }
            return false;
        }

        public void UpdateEventWins(string Event, int IncrementalValue)
        {
            switch (Event)
            {
                case "brawl":
                    BrawlWins += IncrementalValue;
                    break;

                case "cw":
                case "colorwars":
                    CwWins += IncrementalValue;
                    break;

                case "soloqueue":
                case "sq":
                    SoloQueueWins += IncrementalValue;
                    break;

                case "mw":
                case "mafia":
                case "mafiawars":
                    MwWins += IncrementalValue;
                    break;
            }
        }

        public void SendTopAlert(string Message)
        {
            if (this.WebSocketConnection == null)
            {
                return;
            }

            PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(this.Client, JsonConvert.SerializeObject(new Dictionary<object, object>()
             {
                { "event", "chatManager" },
                { "chatname", "getchats" },
                { "action", "newnotifyuser" },
                { "chatmessage", Message }
             }));
        }

        public int HuntPoints
        {
            get { return mHuntPoints; }
            set { mHuntPoints = value; }
        }
        public string HuntSkins
        {
            get { return mHuntSkins; }
            set { mHuntSkins = value; }
        }

        public void AddHuntSkin(int type, int amount)
        {
            string typeName = type switch
            {
                4 => "Bear",
                6 => "Lion",
                7 => "Rhino",
                12 => "Dragon",
                14 => "Monkey",
                15 => "Horse",
                17 => "Bunny",
                21 => "Pigeon",
                22 => "Pigeon",
                23 => "DemonMonkey",
                24 => "BabyBear",
                26 => "Gnome",
                28 => "Kitten",
                30 => "Piglet",
                31 => "Haloompa",
                33 => "Pterosaur",
                34 => "Velociraptor",
                35 => "Cow",
                36 => "Penguin",
                37 => "Elephant",
                _ => "Wild"
            } + "Pieles";

            Dictionary<string, int> skins = new Dictionary<string, int>();

            if (!string.IsNullOrEmpty(mHuntSkins))
            {
                string[] parts = mHuntSkins.Split('|');
                foreach (string part in parts)
                {
                    if (string.IsNullOrEmpty(part)) continue;
                    string[] kv = part.Split(':');
                    if (kv.Length == 2 && int.TryParse(kv[1], out int count))
                    {
                        skins[kv[0]] = count;
                    }
                }
            }

            if (skins.ContainsKey(typeName))
                skins[typeName] += amount;
            else
                skins[typeName] = amount;

            mHuntSkins = string.Join("|", skins.Select(x => x.Key + ":" + x.Value));
        }
        public int BasuLvl
        {
            get { return mBasuLvl; }
            set { mBasuLvl = value; }
        }
        public int BasuXP
        {
            get { return mBasuXP; }
            set { mBasuXP = value; }
        }
        public int CamLvl
        {
            get { return mCamLvl; }
            set { mCamLvl = value; }
        }
        public int CamXP
        {
            get { return mCamXP; }
            set { mCamXP = value; }
        }
        #endregion
    }
}