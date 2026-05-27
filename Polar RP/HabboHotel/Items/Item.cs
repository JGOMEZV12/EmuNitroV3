using Polar.Communication.Packets.Outgoing.Handshake;
using Polar.Communication.Packets.Outgoing.Navigator;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using Polar.Communication.Packets.Outgoing.Rooms.Session;
using Polar.Core;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.Items.Data.RentableSpace;
using Polar.HabboHotel.Items.Data.WhisperTile;
using Polar.HabboHotel.Items.Interactor;
using Polar.HabboHotel.Items.Wired;
using Polar.HabboHotel.Pathfinding;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Games.Freeze;
using Polar.HabboHotel.Rooms.Games.Teams;
using Polar.HabboHotel.Rooms.Instance;
using Polar.HabboHotel.Rooms.Map.Movement;
using Polar.HabboHotel.Users;
using Polar.HabboHotel.Users.Effects;
using Polar.HabboRoleplay.Comodin;
using Polar.HabboRoleplay.Farming;
using Polar.HabboRoleplay.Gambling;
using Polar.HabboRoleplay.Houses;
using Polar.HabboRoleplay.Vehicles;
using Polar.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading;

namespace Polar.HabboHotel.Items
{
    public class Item
    {
        // ─────────────────────────────────────
        //  Campos públicos (mantenidos por compatibilidad)
        // ─────────────────────────────────────
        public int Id;
        public int _iBallValue;
        public int BaseItem;
        public bool ballstop = false;
        public int ballsteps = 0;
        public string ExtraData;
        public bool ReboteBall = false;
        public bool ejex;
        public bool ejey;
        public int ChuteBall = 1;
        public int BallDireccion;
        public string Figure;
        public string Gender;
        public int GroupId;
        public int InteractingUser;
        public int InteractingUser2;
        public int LimitedNo;
        public int LimitedTot;
        public bool MagicRemove = false;
        public int RoomId;
        public int Rotation;
        public int UpdateCounter;
        public int UserID;
        public string Username;
        public int interactingBallUser;
        public byte interactionCount;
        public byte interactionCountHelper;
        public string DeliveryType = "";

        public TEAM team;
        public bool pendingReset = false;
        public FreezePowerUp freezePowerUp;
        public MovementState movement;
        public MovementDirection MovementDir;

        public int value;
        public string wallCoord;

        public RentableSpaceData RentableSpaceData;
        public WhisperTileData WhisperTileData;
        public FarmingData FarmingData;
        public TexasHoldEmItem TexasHoldEmData;
        public WiredComponent WiredComponent;
        public string WiredData;

        public Dictionary<int, ThreeDCoord> GetAffectedTiles2 { get; private set; }

        // ─────────────────────────────────────
        //  Campos privados
        // ─────────────────────────────────────
        private ItemData _data;
        private int _coordX;
        private int _coordY;
        private double _coordZ;
        private bool _updateNeeded;
        private Room _room;
        private List<Point> _affectedPoints;

        private readonly bool _isRoller;
        private readonly bool _isWallItem;
        private readonly bool _isFloorItem;

        // FIX: Random no es thread-safe — [ThreadStatic] da una instancia por hilo
        [ThreadStatic]
        private static Random _random;
        private static Random SafeRandom => _random ?? (_random = new Random());

        // ─────────────────────────────────────
        //  Constructor
        // ─────────────────────────────────────
        public Item(int id, int roomId, int baseItem, string extraData, int x, int y, double z, int rot,
            int userid, int group, int limitedNumber, int limitedStack, string wallCoord,
            Room room = null, RentableSpaceData house = null, FarmingSpace farmingSpace = null,
            TexasHoldEmItem texasHoldEmData = null, string wiredData = "", DataRow dataRow = null)
        {
            ItemData data = null;
            if (!PolarEnvironment.GetGame().GetItemManager().GetItem(baseItem, out data))
                return;

            Id = id;
            RoomId = roomId;
            _room = room;
            _data = data;
            BaseItem = baseItem;
            ExtraData = extraData;
            GroupId = group;

            _coordX = x;
            _coordY = y;
            if (!double.IsInfinity(z))
                _coordZ = z;

            Rotation = rot;
            UpdateNeeded = false;
            UpdateCounter = 0;
            InteractingUser = 0;
            InteractingUser2 = 0;
            interactingBallUser = 0;
            interactionCount = 0;
            value = 0;
            UserID = userid;
            Username = PolarEnvironment.GetUserInfoBy("username", "id", UserID.ToString());
            LimitedNo = limitedNumber;
            LimitedTot = limitedStack;
            TexasHoldEmData = texasHoldEmData;
            WiredData = wiredData;

            // FIX: cacheamos GetBaseItem() una sola vez en el constructor
            var baseItemData = GetBaseItem();

            FarmingData = baseItemData.InteractionType == InteractionType.FARMING
                ? new FarmingData(id) : null;

            if (baseItemData.InteractionType == InteractionType.HOUSE_SIGN)
                RentableSpaceData = house != null
                    ? new RentableSpaceData(house, id)
                    : new RentableSpaceData(id, roomId, x, y, z, dataRow);
            else
                RentableSpaceData = null;

            WhisperTileData = baseItemData.InteractionType == InteractionType.WHISPER_TILE
                ? new WhisperTileData(id, dataRow) : null;

            if (baseItemData.InteractionType == InteractionType.MOODLIGHT)
            {
                if (_room != null && _room.MoodlightData == null)
                    _room.MoodlightData = new Data.Moodlight.MoodlightData(id, dataRow);
            }
            else if (baseItemData.InteractionType == InteractionType.TONER)
            {
                if (_room != null && _room.TonerData == null)
                    _room.TonerData = new Data.Toner.TonerData(id, dataRow);
            }
            switch (baseItemData.InteractionType)
            {
                case InteractionType.TELEPORT:
                case InteractionType.HOPPER:
                    RequestUpdate(0, true);
                    break;

                case InteractionType.ROLLER:
                    _isRoller = true;
                    if (roomId > 0)
                    {
                        var rh = GetRoom();
                        if (rh != null)
                            rh.GetRoomItemHandler().GotRollers = true;
                    }
                    break;

                case InteractionType.banzaiscoreblue:
                case InteractionType.footballcounterblue:
                case InteractionType.banzaigateblue:
                case InteractionType.FREEZE_BLUE_GATE:
                case InteractionType.freezebluecounter:
                    team = TEAM.BLUE;
                    break;

                case InteractionType.banzaiscoregreen:
                case InteractionType.footballcountergreen:
                case InteractionType.banzaigategreen:
                case InteractionType.freezegreencounter:
                case InteractionType.FREEZE_GREEN_GATE:
                    team = TEAM.GREEN;
                    break;

                case InteractionType.banzaiscorered:
                case InteractionType.footballcounterred:
                case InteractionType.banzaigatered:
                case InteractionType.freezeredcounter:
                case InteractionType.FREEZE_RED_GATE:
                    team = TEAM.RED;
                    break;

                case InteractionType.banzaiscoreyellow:
                case InteractionType.footballcounteryellow:
                case InteractionType.banzaigateyellow:
                case InteractionType.freezeyellowcounter:
                case InteractionType.FREEZE_YELLOW_GATE:
                    team = TEAM.YELLOW;
                    break;

                case InteractionType.banzaitele:
                    ExtraData = "";
                    break;
            }

            string typeStr = baseItemData.Type.ToString().ToLower();
            _isWallItem = typeStr == "i";
            _isFloorItem = typeStr == "s";

            GetAffectedTiles2 = Gamemap.GetAffectedTiles(baseItemData.Length, baseItemData.Width, x, y, rot);

            if (_isFloorItem)
                _affectedPoints = GetAffectedTiles2.Values.Select(t => new Point(t.X, t.Y)).ToList();
            else if (_isWallItem)
            {
                this.wallCoord = wallCoord;
                _affectedPoints = new List<Point>();
            }
        }

        // ─────────────────────────────────────
        //  Propiedades
        // ─────────────────────────────────────
        public ItemData Data
        {
            get => _data;
            set => _data = value;
        }

        public List<Point> GetAffectedTiles => _affectedPoints;

        public int GetX { get => _coordX; set => _coordX = value; }
        public int GetY { get => _coordY; set => _coordY = value; }
        public double GetZ { get => _coordZ; set => _coordZ = value; }

        public bool UpdateNeeded
        {
            get => _updateNeeded;
            set
            {
                // Solo encolar si pasa de false → true
                if (value && !_updateNeeded && GetRoom() != null)
                    GetRoom().GetRoomItemHandler().QueueRoomItemUpdate(this);
                _updateNeeded = value;
            }
        }

        public bool IsRoller => _isRoller;
        public bool IsWallItem => _isWallItem;
        public bool IsFloorItem => _isFloorItem;

        public Point Coordinate => new Point(GetX, GetY);

        public List<Point> GetCoords
        {
            get
            {
                var list = new List<Point> { Coordinate };
                foreach (var t in GetAffectedTiles2.Values)
                    list.Add(new Point(t.X, t.Y));
                return list;
            }
        }

        public List<Point> GetSides() => new List<Point>
        {
            SquareBehind, SquareInFront, SquareLeft, SquareRight, Coordinate
        };

        // FIX: ExtradataInt — reemplazado patrón verbose de 7 líneas por expresión de 1
        public int ExtradataInt => int.TryParse(ExtraData, out int n) ? n : 0;

        public double TotalHeight
        {
            get
            {
                var baseItemData = GetBaseItem();
                double h = 0.0;
                if (baseItemData.AdjustableHeights.Count > 1 &&
                    int.TryParse(ExtraData, out int num) &&
                    baseItemData.AdjustableHeights.Count - 1 >= num)
                {
                    h = GetZ + baseItemData.AdjustableHeights[ExtraData];
                }
                return h <= 0.0 ? GetZ + baseItemData.Height : h;
            }
        }

        // FIX: IsWired — cacheamos GetBaseItem() en variable local
        public bool IsWired
        {
            get
            {
                var t = GetBaseItem()?.InteractionType;
                return t == InteractionType.WIRED_EFFECT
                    || t == InteractionType.WIRED_TRIGGER
                    || t == InteractionType.WIRED_CONDITION
                    || t == InteractionType.WIRED_ADDON;

            }
        }

        // ─────────────────────────────────────
        //  Squares direccionales
        // ─────────────────────────────────────
        public Point SquareInFront
        {
            get
            {
                var sq = new Point(GetX, GetY);
                if (Rotation == 0) sq.Y--;
                else if (Rotation == 2) sq.X++;
                else if (Rotation == 4) sq.Y++;
                else if (Rotation == 6) sq.X--;
                return sq;
            }
        }

        public Point SquareBehind
        {
            get
            {
                var sq = new Point(GetX, GetY);
                if (Rotation == 0) sq.Y++;
                else if (Rotation == 2) sq.X--;
                else if (Rotation == 4) sq.Y--;
                else if (Rotation == 6) sq.X++;
                return sq;
            }
        }

        public Point SquareLeft
        {
            get
            {
                var sq = new Point(GetX, GetY);
                if (Rotation == 0) sq.X++;
                else if (Rotation == 2) sq.Y--;
                else if (Rotation == 4) sq.X--;
                else if (Rotation == 6) sq.Y++;
                return sq;
            }
        }

        public Point SquareRight
        {
            get
            {
                var sq = new Point(GetX, GetY);
                if (Rotation == 0) sq.X--;
                else if (Rotation == 2) sq.Y++;
                else if (Rotation == 4) sq.X++;
                else if (Rotation == 6) sq.Y--;
                return sq;
            }
        }

        // ─────────────────────────────────────
        //  Interactor
        // ─────────────────────────────────────
        public IFurniInteractor Interactor
        {
            get
            {
                if (IsWired)
                {
                    var boxType = WiredBoxTypeUtility.FromInteractionType(GetBaseItem().InteractionType.ToString());

                    if (WiredBoxTypeUtility.IsTriggerType(boxType)) return new InteractorWiredTrigger();
                    if (WiredBoxTypeUtility.IsEffectType(boxType)) return new InteractorWiredEffect();
                    if (WiredBoxTypeUtility.IsConditionType(boxType)) return new InteractorWiredCondition();
                    if (WiredBoxTypeUtility.IsAddonType(boxType)) return new InteractorWiredAddon();

                    return new InteractorWired(); // fallback
                }


                switch (GetBaseItem().InteractionType)
                {
                    case InteractionType.MULTI_HEIGHT: return new InteractorMultiHeight();
                    case InteractionType.FOOTBALL: return new InteractorBall();
                    case InteractionType.TRAINER_GYM: return new InteractorTrainerGym();
                    case InteractionType.JUKEBOX: return new InteractorJukebox();
                    case InteractionType.MUSIC_DISC: return new InteractorMusicDisc();
                    case InteractionType.CRAFTING: return new InteractorCrafting();
                    case InteractionType.FARMING: return new InteractorFarming();
                    case InteractionType.ATM_MACHINE: return new InteractorATM();
                    case InteractionType.RP_NUKE: return new InteractorNuking();
                    case InteractionType.TRASH_CAN: return new InteractorTrashCan();
                    case InteractionType.HOUSE_SIGN: return new InteractorHouseSing();
                    case InteractionType.BASURERO: return new InteractorRecolector();
                    case InteractionType.TRAGAMONEDAS: return new InteractorTragamonedas();
                    case InteractionType.WEEDMATERIA: return new InteractorWeedMateria();
                    case InteractionType.Cocaina: return new InteractorCocaina();
                    case InteractionType.HEROINA: return new InteractorHeroina();
                    case InteractionType.WEEDPORRO: return new InteractorPorro();
                    case InteractionType.BASURAENTREGA: return new InteractorBasura();
                    case InteractionType.MINERIA: return new InteractorMineria();
                    case InteractionType.PEPSIMACHINE: return new InteractorCola();
                    case InteractionType.AGUAENERGY: return new InteractorAgua();
                    case InteractionType.CARAMELOMACHINE: return new InteractorCaramelo();
                    case InteractionType.CAJERORUBY: return new InteractorCajero();
                    case InteractionType.COMIDAMACHINE: return new InteractorComida();
                    case InteractionType.DELIVERY_BOX: return new InteractorDeliveryBox();
                    case InteractionType.BOTTLE: return new InteractorSpinningBottle();
                    case InteractionType.HABBO_WHEEL: return new InteractorHabboWheel();
                    case InteractionType.LOVE_SHUFFLER: return new InteractorLoveShuffler();
                    case InteractionType.ALERT: return new InteractorAlert();
                    case InteractionType.VENDING_MACHINE: return new InteractorVendor();
                    case InteractionType.SCOREBOARD: return new InteractorScoreboard();
                    case InteractionType.GUILD_ITEM: return new InteractorGroupFlag();
                    case InteractionType.PUZZLE_BOX: return new InteractorPuzzleBox();
                    case InteractionType.MANNEQUIN: return new InteractorMannequin();
                    case InteractionType.banzaicounter: return new InteractorBanzaiTimer();
                    case InteractionType.freezetimer: return new InteractorFreezeTimer();
                    case InteractionType.FREEZE_TILE_BLOCK:
                    case InteractionType.FREEZE_TILE: return new InteractorFreezeTile();
                    case InteractionType.footballcounterblue:
                    case InteractionType.footballcountergreen:
                    case InteractionType.footballcounterred:
                    case InteractionType.footballcounteryellow: return new InteractorScoreCounter();
                    case InteractionType.banzaiscoreblue:
                    case InteractionType.banzaiscoregreen:
                    case InteractionType.banzaiscorered:
                    case InteractionType.banzaiscoreyellow: return new InteractorBanzaiScoreCounter();
                    case InteractionType.LOVELOCK: return new InteractorLoveLock();
                    case InteractionType.PINATATRIGGERED: return new InteractorPinata();
                    case InteractionType.MAGICEGG: return new InteractorMagicEgg();
                    case InteractionType.MAGICCHEST: return new InteractorMagicChest();
                    case InteractionType.CANNON: return new InteractorCannon();
                    case InteractionType.COUNTER: return new InteractorCounter();
                    case InteractionType.CAMERA_PICTURE: return new InteractorCameraPicture();
                    case InteractionType.WF_FLOOR_SWITCH_1:
                    case InteractionType.WF_FLOOR_SWITCH_2:
                    case InteractionType.SWITCH:
                    case InteractionType.SWITCH_REMOTE:
                    case InteractionType.EFFECT_TOGGLE:
                    case InteractionType.RANDOM_STATE:
                        return new InteractorSwitch();
                    case InteractionType.DICE:
                    case InteractionType.COLOR_WHEEL:
                        return new InteractorDice();
                    case InteractionType.CRACKABLE:
                    case InteractionType.CRACKABLE_MONSTER:
                        return new InteractorGenericSwitch(); // Fallback for now
                    case InteractionType.GATE:
                    case InteractionType.ONE_WAY_GATE:
                    case InteractionType.GATE_VIP:
                    case InteractionType.GUILD_GATE:
                    case InteractionType.CLUB_GATE:
                    case InteractionType.EFFECT_GATE:
                        return new InteractorGate();
                    case InteractionType.TELEPORT:
                    case InteractionType.HOPPER:
                    case InteractionType.COSTUME_HOPPER:
                    case InteractionType.CLUB_HOPPER:
                        return new InteractorTeleport();
                    case InteractionType.NONE:
                    default: return new InteractorGenericSwitch();
                }
            }
        }

        // ─────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────

        // FIX: helper extraído — Shower y CAGAR tenían este bloque copy-paste
        private bool HasUsersOnTile()
        {
            var baseItemData = GetBaseItem();
            foreach (var sq in Gamemap.GetAffectedTiles(baseItemData.Length, baseItemData.Width, GetX, GetY, Rotation).Values)
            {
                if (GetRoom().GetRoomUserManager().GetUserForSquare(sq.X, sq.Y) != null)
                    return true;
            }
            return false;
        }

        // FIX: Fisher-Yates O(n) en lugar del O(n log n) con LINQ + KeyValuePair
        public static string[] RandomizeStrings(string[] arr)
        {
            var result = (string[])arr.Clone();
            var rng = SafeRandom;
            for (int i = result.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                string tmp = result[i];
                result[i] = result[j];
                result[j] = tmp;
            }
            return result;
        }

        // ─────────────────────────────────────
        //  SetState / Destroy
        // ─────────────────────────────────────
        public void SetState(int pX, int pY, double pZ, Dictionary<int, ThreeDCoord> tiles)
        {
            GetX = pX;
            GetY = pY;
            if (!double.IsInfinity(pZ))
                _coordZ = pZ;
            _affectedPoints = tiles.Values.Select(t => new Point(t.X, t.Y)).ToList();
            GetAffectedTiles2 = tiles;
        }

        public void Destroy()
        {
            _affectedPoints?.Clear();
            _affectedPoints = null; // FIX: antes solo hacía Clear(), el objeto seguía en memoria
            _room = null;
            _data = null;
        }

        // ─────────────────────────────────────
        //  ProcessUpdates
        // ─────────────────────────────────────
        public void ProcessUpdates()
        {
            try
            {
                UpdateCounter--;
                if (UpdateCounter > 0)
                    return;

                UpdateNeeded = false;
                UpdateCounter = 0;

                // FIX: GetBaseItem() cacheado una sola vez por ciclo en lugar de llamarlo en cada case
                var baseItemData = GetBaseItem();
                if (baseItemData == null) return;

                RoomUser user = null;
                RoomUser user2 = null;

                // Treadmill (Roleplay)
                string itemName = baseItemData.ItemName.ToLower();
                if (itemName == "olympics_c16_treadmill" || itemName == "olympics_c16_crosstrainer")
                {
                    var users = GetRoom().GetGameMap().GetRoomUsers(Coordinate);
                    if (users == null || users.Count <= 0)
                    {
                        ExtraData = "0";
                        UpdateState(false, true);
                        InteractingUser = 0;
                    }
                    else
                        RequestUpdate(1, false);
                    return;
                }

                switch (baseItemData.InteractionType)
                {
                    case InteractionType.SHOWER:
                    case InteractionType.CAGAR:
                        // FIX: duplicación eliminada — ambos usaban el mismo bloque
                        if (ExtraData == "1")
                        {
                            if (!HasUsersOnTile())
                            {
                                ExtraData = "0";
                                UpdateState(false, true);
                                InteractingUser = 0;
                            }
                            else
                                RequestUpdate(1, false);
                        }
                        break;

                    case InteractionType.TRASH_CAN:
                    case InteractionType.BASURERO:
                    case InteractionType.TRAGAMONEDAS:
                    case InteractionType.Cocaina:
                    case InteractionType.WEEDMATERIA:
                    case InteractionType.WEEDPORRO:
                    case InteractionType.BASURAENTREGA:
                    case InteractionType.MINERIA:
                    case InteractionType.PEPSIMACHINE:
                        // FIX: 9 casos idénticos colapsados en uno
                        if (ExtraData == "1")
                        {
                            ExtraData = "0";
                            UpdateState(false, true);
                        }
                        break;

                    case InteractionType.AGUAENERGY:
                        if (ExtraData == "0")
                        {
                            ExtraData = "1";
                            UpdateState(false, true);
                        }
                        break;

                    case InteractionType.CARAMELOMACHINE:
                    case InteractionType.COMIDAMACHINE:
                        // FIX: dos casos idénticos colapsados en uno
                        if (ExtraData == "1")
                        {
                            user = GetRoom().GetRoomUserManager().GetRoomUserByHabbo(InteractingUser);
                            if (user == null) break;
                            user.UnlockWalking();
                            if (baseItemData.VendingIds.Count > 0)
                                user.CarryItem(baseItemData.VendingIds[RandomNumber.GenerateRandom(0, baseItemData.VendingIds.Count - 1)]);
                            InteractingUser = 0;
                            ExtraData = "0";
                            UpdateState(false, true);
                        }
                        break;

                    case InteractionType.SLIDING_DOORS:
                    case InteractionType.GUILD_GATE:
                        if (ExtraData == "1")
                        {
                            if (!HasUsersOnTile())
                            {
                                ExtraData = "0";
                                UpdateState(false, true);
                            }
                            else
                                RequestUpdate(2, false);
                        }
                        break;

                    case InteractionType.EFFECT:
                    case InteractionType.BEDEFFECT:
                        if (ExtraData == "1")
                        {
                            if (GetRoom().GetRoomUserManager().GetUserForSquare(GetX, GetY) == null)
                            {
                                ExtraData = "0";
                                UpdateState(false, true);
                            }
                            else
                                RequestUpdate(2, false);
                        }
                        break;

                    case InteractionType.ONE_WAY_GATE:
                        user = InteractingUser > 0
                            ? GetRoom().GetRoomUserManager().GetRoomUserByHabbo(InteractingUser)
                            : null;

                        if (user != null && user.X == GetX && user.Y == GetY)
                        {
                            ExtraData = "1";
                            user.MoveTo(SquareBehind);
                            user.InteractingGate = false;
                            user.GateId = 0;
                            RequestUpdate(1, false);
                            UpdateState(false, true);
                        }
                        else if (user != null && user.Coordinate == SquareBehind)
                        {
                            user.UnlockWalking();
                            ExtraData = "0";
                            InteractingUser = 0;
                            user.InteractingGate = false;
                            user.GateId = 0;
                            UpdateState(false, true);
                        }
                        else if (ExtraData == "1")
                        {
                            ExtraData = "0";
                            UpdateState(false, true);
                        }

                        if (user == null) InteractingUser = 0;
                        break;

                    case InteractionType.GATE_VIP:
                        user = InteractingUser > 0
                            ? GetRoom().GetRoomUserManager().GetRoomUserByHabbo(InteractingUser)
                            : null;

                        int newX = 0, newY = 0;
                        if (user != null && user.X == GetX && user.Y == GetY)
                        {
                            if (user.RotBody == 4) newY = 1;
                            else if (user.RotBody == 0) newY = -1;
                            else if (user.RotBody == 6) newX = -1;
                            else if (user.RotBody == 2) newX = 1;
                            user.MoveTo(user.X + newX, user.Y + newY);
                            RequestUpdate(1, false);
                        }
                        else if (user != null && (user.Coordinate == SquareBehind || user.Coordinate == SquareInFront))
                        {
                            user.UnlockWalking();
                            ExtraData = "0";
                            InteractingUser = 0;
                            UpdateState(false, true);
                        }
                        else if (ExtraData == "1")
                        {
                            ExtraData = "0";
                            UpdateState(false, true);
                        }
                        if (user == null) InteractingUser = 0;
                        break;

                    case InteractionType.HOPPER:
                        {
                            user = null;
                            user2 = null;
                            bool showHopperEffect = false;
                            bool keepDoorOpen = false;
                            int pause = 0;

                            if (InteractingUser > 0)
                            {
                                user = GetRoom().GetRoomUserManager().GetRoomUserByHabbo(InteractingUser);
                                if (user != null)
                                {
                                    if (user.Coordinate == Coordinate)
                                    {
                                        user.AllowOverride = false;
                                        if (user.TeleDelay == 0)
                                        {
                                            int roomHopId = ItemHopperFinder.GetAHopper(user.RoomId);
                                            int nextHopId = ItemHopperFinder.GetHopperId(roomHopId);
                                            // FIX: null check ANTES de usar user (antes era al revés)
                                            if (!user.IsBot && user.GetClient() != null && user.GetClient().GetHabbo() != null)
                                            {
                                                user.GetClient().GetHabbo().IsHopping = true;
                                                user.GetClient().GetHabbo().HopperId = nextHopId;
                                                user.GetClient().GetHabbo().PrepareRoom(roomHopId, "");
                                                InteractingUser = 0;
                                            }
                                        }
                                        else
                                        {
                                            user.TeleDelay--;
                                            showHopperEffect = true;
                                        }
                                    }
                                    else if (user.Coordinate == SquareInFront)
                                    {
                                        user.AllowOverride = true;
                                        keepDoorOpen = true;
                                        if (user.IsWalking && (user.GoalX != GetX || user.GoalY != GetY))
                                            user.ClearMovement(true);
                                        user.CanWalk = false;
                                        user.AllowOverride = true;
                                        user.MoveTo(Coordinate.X, Coordinate.Y, true);
                                    }
                                    else
                                        InteractingUser = 0;
                                }
                                else
                                    InteractingUser = 0;
                            }

                            if (InteractingUser2 > 0)
                            {
                                user2 = GetRoom().GetRoomUserManager().GetRoomUserByHabbo(InteractingUser2);
                                if (user2 != null)
                                {
                                    keepDoorOpen = true;
                                    user2.UnlockWalking();
                                    user2.MoveTo(SquareInFront);
                                }
                                InteractingUser2 = 0;
                            }

                            if (keepDoorOpen)
                            {
                                if (ExtraData != "1") { ExtraData = "1"; UpdateState(false, true); }
                            }
                            else if (showHopperEffect)
                            {
                                if (ExtraData != "2") { ExtraData = "2"; UpdateState(false, true); }
                            }
                            else
                            {
                                if (ExtraData != "0" && pause == 0)
                                {
                                    ExtraData = "0";
                                    UpdateState(false, true);
                                    pause = 2;
                                }
                                else if (pause > 0)
                                    pause--;
                            }

                            RequestUpdate(1, false);
                            break;
                        }

                    case InteractionType.TELEPORT:
                        {
                            user = null;
                            user2 = null;
                            bool keepDoorOpen = false;
                            bool showTeleEffect = false;

                            if (InteractingUser > 0)
                            {
                                user = GetRoom().GetRoomUserManager().GetRoomUserByHabbo(InteractingUser);
                                if (user != null)
                                {
                                    if (user.Coordinate == Coordinate)
                                    {
                                        user.AllowOverride = false;
                                        if (ItemTeleporterFinder.IsTeleLinked(Id, GetRoom()))
                                        {
                                            showTeleEffect = true;
                                            // FIX: eliminado el "if (true)" que era condición muerta
                                            int teleId = ItemTeleporterFinder.GetLinkedTele(Id, GetRoom());
                                            int teleRoomId = ItemTeleporterFinder.GetTeleRoomId(teleId, GetRoom());

                                            if (teleRoomId == RoomId)
                                            {
                                                Item linkedItem = GetRoom().GetRoomItemHandler().GetItem(teleId);
                                                if (linkedItem == null)
                                                    user.UnlockWalking();
                                                else
                                                {
                                                    user.SetPos(linkedItem.GetX, linkedItem.GetY, linkedItem.GetZ);
                                                    user.SetRot(linkedItem.Rotation, false);
                                                    linkedItem.ExtraData = "2";
                                                    linkedItem.UpdateState(false, true);
                                                    linkedItem.InteractingUser2 = InteractingUser;
                                                    GetRoom().GetGameMap().RemoveUserFromMap(user, new Point(GetX, GetY));
                                                    InteractingUser = 0;
                                                }
                                            }
                                            else
                                            {
                                                if (user.TeleDelay == 0)
                                                {
                                                    if (!user.IsBot && user.GetClient() != null && user.GetClient().GetHabbo() != null)
                                                    {
                                                        user.GetClient().GetHabbo().IsTeleporting = true;
                                                        user.GetClient().GetHabbo().TeleportingRoomID = teleRoomId;
                                                        user.GetClient().GetHabbo().TeleporterId = teleId;
                                                        user.GetClient().GetHabbo().PrepareRoom(teleRoomId, "");
                                                        InteractingUser = 0;
                                                    }
                                                }
                                                else
                                                {
                                                    user.TeleDelay--;
                                                    showTeleEffect = true;
                                                }
                                            }
                                            GetRoom().GetGameMap().GenerateMaps();
                                        }
                                        else
                                        {
                                            user.UnlockWalking();
                                            InteractingUser = 0;
                                        }
                                    }
                                    else if (user.Coordinate == SquareInFront)
                                    {
                                        user.AllowOverride = true;
                                        keepDoorOpen = true;
                                        if (user.IsWalking && (user.GoalX != GetX || user.GoalY != GetY))
                                            user.ClearMovement(true);
                                        user.CanWalk = false;
                                        user.AllowOverride = true;
                                        user.MoveTo(Coordinate.X, Coordinate.Y, true);
                                    }
                                    else
                                        InteractingUser = 0;
                                }
                                else
                                    InteractingUser = 0;
                            }

                            if (InteractingUser2 > 0)
                            {
                                user2 = GetRoom().GetRoomUserManager().GetRoomUserByHabbo(InteractingUser2);
                                if (user2 != null)
                                {
                                    keepDoorOpen = true;
                                    user2.UnlockWalking();
                                    user2.MoveTo(SquareInFront);
                                }
                                InteractingUser2 = 0;
                            }

                            if (showTeleEffect)
                            {
                                if (ExtraData != "2") { ExtraData = "2"; UpdateState(false, true); }
                            }
                            else if (keepDoorOpen)
                            {
                                if (ExtraData != "1") { ExtraData = "1"; UpdateState(false, true); }
                            }
                            else
                            {
                                if (ExtraData != "0") { ExtraData = "0"; UpdateState(false, true); }
                            }

                            RequestUpdate(1, false);
                            break;
                        }

                    case InteractionType.BOTTLE:
                        ExtraData = RandomNumber.GenerateNewRandom(0, 7).ToString();
                        UpdateState();
                        break;

                    case InteractionType.DICE:
                        {
                            if (ExtraData == "-1")
                                ExtraData = RandomizeStrings(new[] { "1", "2", "3", "4", "5", "6" })[0];

                            if (TexasHoldEmData != null)
                            {
                                TexasHoldEmData.Rolled = true;
                                TexasHoldEmData.Value = Convert.ToInt32(ExtraData);

                                TexasHoldEm game;
                                int playerId = TexasHoldEmManager.GetPlayerByDice(this, out game);

                                if (game != null && playerId == 0)
                                {
                                    UpdateState(false, true);
                                    if (game.GameSequence < 3)
                                    {
                                        game.PlayersTurn = 0;
                                        game.ChangeTurn();
                                    }
                                    else
                                    {
                                        new Thread(() =>
                                        {
                                            lock (game)
                                            {
                                                foreach (var it in game.Player1.Values) it.Furni?.UpdateState(false, true);
                                                foreach (var it in game.Player2.Values) it.Furni?.UpdateState(false, true);
                                                foreach (var it in game.Player3.Values) it.Furni?.UpdateState(false, true);
                                                game.SendStartMessage("The winner will be chosen in 5 seconds!");
                                                Thread.Sleep(5000);
                                                game.ChooseWinner();
                                            }
                                        }).Start();
                                    }
                                }

                                if (game != null && playerId > 0)
                                {
                                    if (game.GameSequence != 0) break;

                                    GameClient client = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(playerId);
                                    if (client != null)
                                    {
                                        if (IsFloorItem)
                                            client.SendMessage(new ObjectUpdateComposer(this, 0));
                                        else
                                            client.SendMessage(new ItemUpdateComposer(this, 0));
                                    }

                                    if (game.PlayerList != null)
                                    {
                                        var entry = game.PlayerList.FirstOrDefault(x => x.Value?.UserId == playerId);
                                        if (entry.Value != null)
                                        {
                                            var playerData = entry.Key == 1 ? game.Player1
                                                           : entry.Key == 2 ? game.Player2
                                                           : game.Player3;
                                            if (playerData.Values.Count(x => x?.Rolled == true) >= 2)
                                                game.ChangeTurn();
                                        }
                                    }
                                }
                            }
                            else
                                UpdateState();

                            break;
                        }

                    case InteractionType.HABBO_WHEEL:
                        ExtraData = RandomNumber.GenerateRandom(1, 10).ToString();
                        UpdateState();
                        break;

                    case InteractionType.LOVE_SHUFFLER:
                        if (ExtraData == "0")
                        {
                            ExtraData = RandomNumber.GenerateNewRandom(1, 4).ToString();
                            RequestUpdate(20, false);
                        }
                        else if (ExtraData != "-1")
                            ExtraData = "-1";
                        UpdateState(false, true);
                        break;

                    case InteractionType.ALERT:
                        if (ExtraData == "1") { ExtraData = "0"; UpdateState(false, true); }
                        break;

                    case InteractionType.VENDING_MACHINE:
                        if (ExtraData != "1") break;
                        var vendUser = GetRoom().GetRoomUserManager().GetRoomUserByHabboId(InteractingUser);
                        if (vendUser != null)
                        {
                            int drink = baseItemData.VendingIds[PolarEnvironment.GetRandomNumber(0, baseItemData.VendingIds.Count - 1)];
                            vendUser.CarryItem(drink);
                        }
                        InteractingUser = 0;
                        ExtraData = "0";
                        UpdateState(false, true);
                        break;

                    case InteractionType.SCOREBOARD:
                    case InteractionType.COUNTER:
                    case InteractionType.banzaicounter:
                    case InteractionType.freezetimer:
                        {
                            if (string.IsNullOrEmpty(ExtraData)) break;
                            if (!int.TryParse(ExtraData, out int secs)) break;

                            if (secs > 0)
                            {
                                if (interactionCountHelper == 1)
                                {
                                    secs--;
                                    interactionCountHelper = 0;

                                    bool active = true;
                                    if (baseItemData.InteractionType == InteractionType.banzaicounter)
                                        active = GetRoom().GetBanzai().isBanzaiActive;
                                    else if (baseItemData.InteractionType == InteractionType.COUNTER)
                                        active = GetRoom().GetSoccer().GameIsStarted;
                                    else if (baseItemData.InteractionType == InteractionType.freezetimer)
                                        active = GetRoom().GetFreeze().GameIsStarted;

                                    if (!active) break;
                                    ExtraData = secs.ToString();
                                    UpdateState();
                                }
                                else
                                    interactionCountHelper++;

                                UpdateCounter = 1;
                            }
                            else
                            {
                                UpdateCounter = 0;
                                UpdateNeeded = false;
                                if (baseItemData.InteractionType == InteractionType.banzaicounter)
                                    GetRoom().GetBanzai().BanzaiEnd();
                                else if (baseItemData.InteractionType == InteractionType.COUNTER)
                                    GetRoom().GetSoccer().StopGame();
                                else if (baseItemData.InteractionType == InteractionType.freezetimer)
                                    GetRoom().GetFreeze().StopGame();
                            }
                            break;
                        }

                    case InteractionType.banzaitele:
                        ExtraData = string.Empty;
                        UpdateState();
                        break;

                    case InteractionType.banzaifloor:
                        if (value == 3)
                        {
                            if (interactionCountHelper == 1)
                            {
                                interactionCountHelper = 0;
                                switch (team)
                                {
                                    case TEAM.BLUE: ExtraData = "11"; break;
                                    case TEAM.GREEN: ExtraData = "8"; break;
                                    case TEAM.RED: ExtraData = "5"; break;
                                    case TEAM.YELLOW: ExtraData = "14"; break;
                                }
                            }
                            else
                            {
                                ExtraData = "";
                                interactionCountHelper++;
                            }
                            UpdateState();
                            interactionCount++;
                            UpdateCounter = interactionCount < 16 ? 1 : 0;
                        }
                        break;

                    case InteractionType.banzaipuck:
                        if (interactionCount > 4) { interactionCount++; UpdateCounter = 1; }
                        else { interactionCount = 0; UpdateCounter = 0; }
                        break;

                    case InteractionType.FREEZE_TILE:
                        if (InteractingUser > 0)
                        {
                            ExtraData = "11000";
                            UpdateState(false, true);
                            GetRoom().GetFreeze().onFreezeTiles(this, freezePowerUp);
                            InteractingUser = 0;
                            interactionCountHelper = 0;
                        }
                        break;

                    case InteractionType.PRESSURE_PAD:
                        ExtraData = "1";
                        UpdateState();
                        break;

                    case InteractionType.WIRED_EFFECT:
                    case InteractionType.WIRED_TRIGGER:
                    case InteractionType.WIRED_CONDITION:
                        if (ExtraData == "1") { ExtraData = "0"; UpdateState(false, true); }
                        break;

                    case InteractionType.CANNON:
                        {
                            if (ExtraData != "1") break;

                            var targetSquares = new List<Point>();
                            Point targetStart;

                            switch (Rotation)
                            {
                                case 0: targetStart = new Point(GetX - 1, GetY); break;
                                case 2: targetStart = new Point(GetX, GetY - 1); break;
                                case 4: targetStart = new Point(GetX + 2, GetY); break;
                                case 6: targetStart = new Point(GetX, GetY + 2); break;
                                default: targetStart = Coordinate; break;
                            }

                            targetSquares.Add(targetStart);
                            for (int i = 1; i <= 3; i++)
                            {
                                Point sq = Rotation == 0 ? new Point(targetStart.X - i, targetStart.Y)
                                         : Rotation == 2 ? new Point(targetStart.X, targetStart.Y - i)
                                         : Rotation == 4 ? new Point(targetStart.X + i, targetStart.Y)
                                         : new Point(targetStart.X, targetStart.Y + i);
                                if (!targetSquares.Contains(sq)) targetSquares.Add(sq);
                            }

                            foreach (Point sq in targetSquares)
                            {
                                var affected = _room.GetGameMap().GetRoomUsers(sq)?.ToList();
                                if (affected == null || affected.Count == 0) continue;

                                foreach (RoomUser target in affected)
                                {
                                    if (target == null || target.IsBot || target.IsPet) continue;
                                    if (target.GetClient() == null || target.GetClient().GetHabbo() == null) continue;
                                    if (_room.CheckRights(target.GetClient(), true)) continue;

                                    target.ApplyEffect(EffectsList.Twinkle);
                                    target.GetClient().SendMessage(new RoomNotificationComposer(
                                        "Expulsado de la Sala",
                                        "Usted fue golpeado por la bola del cañon directo hacia la salida!",
                                        "room_kick_cannonball"));
                                    target.ApplyEffect(0);
                                    _room.GetRoomUserManager().RemoveUserFromRoom(target.GetClient(), true);
                                }
                            }

                            ExtraData = "2";
                            UpdateState(false, true);
                            break;
                        }
                }
            }
            catch (Exception e)
            {
                Logging.LogException(e.ToString());
            }
        }

        // ─────────────────────────────────────
        //  Update helpers
        // ─────────────────────────────────────
        public void RequestUpdate(int cycles, bool setUpdate)
        {
            UpdateCounter = cycles;
            if (setUpdate && !_updateNeeded) // solo si no está ya encolado
                UpdateNeeded = true;
        }

        public void ReqUpdate(int cycles)
        {
            if (UpdateCounter > 0) return;
            UpdateCounter = cycles;
            GetRoom().GetRoomItemHandler().QueueRoomItemUpdate(this);
        }

        public void UpdateState() => UpdateState(true, true);

        public void UpdateState(bool inDb, bool inRoom)
        {
            if (GetRoom() == null) return;
            //Console.WriteLine($"[UpdateState] Item={GetBaseItem().ItemName} ExtraData={ExtraData} inDb={inDb} inRoom={inRoom} IsFloorItem={IsFloorItem}");
            if (inDb) GetRoom().GetRoomItemHandler().UpdateItem(this);
            if (inRoom)
            {
                if (IsFloorItem)
                    GetRoom().SendMessage(new ObjectUpdateComposer(this, UserID));
                else
                    GetRoom().SendMessage(new ItemUpdateComposer(this, UserID));
            }
        }

        // ─────────────────────────────────────
        //  Base item / Room
        // ─────────────────────────────────────
        public void ResetBaseItem()
        {
            _data = null;
            _data = GetBaseItem();
        }

        public ItemData GetBaseItem()
        {
            if (_data == null)
            {
                ItemData d = null;
                if (PolarEnvironment.GetGame().GetItemManager().GetItem(BaseItem, out d))
                    _data = d;
            }
            return _data;
        }

        public Room GetRoom()
        {
            if (_room != null) return _room;
            PolarEnvironment.GetGame().GetRoomManager().TryGetRoom(RoomId, out Room r);
            return r;
        }

        // ─────────────────────────────────────
        //  Walk events / Furni collision
        // ─────────────────────────────────────
        public void UserFurniCollision(RoomUser user)
        {
            if (user?.GetClient()?.GetHabbo() == null) return;
            GetRoom().GetWired().TriggerEvent(Wired.WiredBoxType.TriggerUserFurniCollision, user.GetClient().GetHabbo(), this);
        }

        public void UserWalksOnFurni(RoomUser user)
        {
            if (user?.GetClient()?.GetHabbo() == null) return;

            GameClient session = user.GetClient();
            var baseItemData = GetBaseItem();

            if (baseItemData.InteractionType == InteractionType.COMODIN)
            {
                Comodin comodin = ComodinManager.getComodin(Id);
                if (comodin != null)
                {
                    string[] data = comodin.Action.Split(':');
                    PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(session, data[0], data[1]);

                    if (session.GetRoleplay().TutorialStep == 18 && GetRoom().PhoneStoreEnabled && GetRoom().Type.Equals("public"))
                        PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(session, "compose_tutorial|19");
                    else if (session.GetRoleplay().TutorialStep == 23 && GetRoom().BuyCarEnabled && GetRoom().Type.Equals("public"))
                        PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(session, "compose_tutorial|25");
                    else if (session.GetRoleplay().TutorialStep == 27 && GetRoom().MallEnabled && GetRoom().Type.Equals("public"))
                        PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(session, "compose_tutorial|29");
                    return;
                }
            }

            if (baseItemData.InteractionType == InteractionType.CARNEW)
            {
                // FIX: variables 'itemfurni', 'corp', 'itemnm' eliminadas — se asignaban pero nunca se leían
                bool found = false;
                foreach (Vehicle vehicle in VehicleManager.Vehicles.Values)
                {
                    if (found) break;
                    var tile = user.GetClient().GetHabbo().CurrentRoom
                        .GetRoomItemHandler().GetFloor
                        .FirstOrDefault(x =>
                            x.GetBaseItem().ItemName.ToLower() == vehicle.ItemName &&
                            x.Coordinate == user.GetClient().GetRoomUser().Coordinate);
                    if (tile != null) found = true;
                }

                if (!found)
                {
                    user.GetClient().SendWhisper("¡Debes estar sobre un vehículo para conducir!", 1);
                    return;
                }

                PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(user.GetClient(), "compose_carnew|open");
            }

            if (baseItemData.InteractionType == InteractionType.TENT ||
                baseItemData.InteractionType == InteractionType.TENT_SMALL)
                GetRoom().AddUserToTent(Id, user, this);

            GetRoom().GetWired().TriggerEvent(Wired.WiredBoxType.TriggerWalkOnFurni, user.GetClient().GetHabbo(), this);
            user.LastItem = this;
        }

        public void UserWalksOffFurni(RoomUser user)
        {
            if (user?.GetClient()?.GetHabbo() == null) return;

            var baseItemData = GetBaseItem();
            if (baseItemData.InteractionType == InteractionType.TENT ||
                baseItemData.InteractionType == InteractionType.TENT_SMALL)
                GetRoom().RemoveUserFromTent(Id, user, this);

            GetRoom().GetWired().TriggerEvent(Wired.WiredBoxType.TriggerWalkOffFurni, user.GetClient().GetHabbo(), this);
        }

        // ─────────────────────────────────────
        //  GetBedTiles — FIX: catch vacío loggeado
        // ─────────────────────────────────────
        public List<Point> GetBedTiles(Point point, out Point square)
        {
            square = Coordinate;

            if (GetRoom() == null || GetRoom().GetGameMap() == null)
                return new List<Point>();

            if (GetAffectedTiles.Count < 6)
                return GetAffectedTiles;

            List<Point> tilesLeft = new List<Point>();
            List<Point> tilesRight = new List<Point>();

            if (Rotation == 0)
            {
                tilesLeft = GetAffectedTiles.Where(x => x.X == GetX).ToList();
                tilesRight = GetAffectedTiles.Where(x => x.X == GetX + 1).ToList();
            }
            else if (Rotation == 2)
            {
                tilesLeft = GetAffectedTiles.Where(x => x.Y == GetY).ToList();
                tilesRight = GetAffectedTiles.Where(x => x.Y == GetY + 1).ToList();
            }

            if (tilesLeft.Contains(point))
                return tilesLeft;

            if (tilesRight.Contains(point))
            {
                square = Rotation == 0
                    ? new Point(GetX + 1, GetY)
                    : new Point(GetX, GetY + 1);
                return tilesRight;
            }

            return new List<Point>();
        }
    }
}