using MySqlConnector;
using Polar.Communication.Interfaces;
using Polar.Communication.Packets.Outgoing;
using Polar.Communication.Packets.Outgoing.Inventory.Furni;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.Core;
using Polar.Database.Interfaces;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Items.Data.Moodlight;
using Polar.HabboHotel.Items.Data.RentableSpace;
using Polar.HabboHotel.Items.Data.Toner;
using Polar.HabboHotel.Items.Wired;
using Polar.HabboHotel.Pathfinding;
using Polar.Utilities;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Polar.HabboHotel.Rooms
{
    public sealed class RoomItemHandling : IDisposable
    {
        private Room _room;

        public int HopperCount;
        public int JukeboxCount;
        private bool mGotRollers;
        private int mRollerSpeed;
        private int mRollerCycle;

        // ── Colecciones principales ────────────────────────────────────────────────
        // FIX: _movedItems almacena ÚLTIMA versión del ítem. Si el mismo ítem se mueve
        //      dos veces antes de SaveFurniture, sólo nos interesa la posición final.
        //      ConcurrentDictionary ya hace eso con el indexer [id] = item.
        private ConcurrentDictionary<int, Item> _movedItems;
        private ConcurrentDictionary<int, Item> _rollers;
        private ConcurrentDictionary<int, Item> _wallItems;
        public ConcurrentDictionary<int, Item> _floorItems;

        // ── Roller state (sólo mutado desde el hilo del tick de sala) ─────────────
        private readonly List<int> rollerItemsMoved;
        private readonly List<int> rollerUsersMoved;
        private readonly List<ServerPacket> rollerMessages;

        // FIX: ConcurrentQueue es lock-free para Enqueue/TryDequeue
        private ConcurrentQueue<Item> _roomItemUpdateQueue;

        public bool usedwiredscorebord;

        // FIX: StringBuilder reutilizable para el batch SQL de SaveFurniture.
        private readonly StringBuilder _saveSb = new StringBuilder(4096);

        public RoomItemHandling(Room room)
        {
            _room = room;
            HopperCount = 0;
            JukeboxCount = 0;
            mGotRollers = false;
            mRollerSpeed = 4;
            mRollerCycle = 0;
            _movedItems = new ConcurrentDictionary<int, Item>();
            _rollers = new ConcurrentDictionary<int, Item>();
            _wallItems = new ConcurrentDictionary<int, Item>();
            _floorItems = new ConcurrentDictionary<int, Item>();

            rollerItemsMoved = new List<int>(8);
            rollerUsersMoved = new List<int>(8);
            rollerMessages = new List<ServerPacket>(8);

            _roomItemUpdateQueue = new ConcurrentQueue<Item>();
            usedwiredscorebord = false;
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  ROLLERS
        // ══════════════════════════════════════════════════════════════════════════

        public void TryAddRoller(int itemId, Item roller) => _rollers.TryAdd(itemId, roller);

        public bool GotRollers
        {
            get => mGotRollers;
            set => mGotRollers = value;
        }

        public void QueueRoomItemUpdate(Item item) => _roomItemUpdateQueue.Enqueue(item);
        public void SetSpeed(int p) => mRollerSpeed = p;

        // ══════════════════════════════════════════════════════════════════════════
        //  SCOREBOARD
        // ══════════════════════════════════════════════════════════════════════════

        public void UpdateWiredScoreBord()
        {
            var messages = new List<ServerPacket>();
            foreach (Item scoreitem in _floorItems.Values)
            {
                if (scoreitem.GetBaseItem().InteractionType == InteractionType.WIRED_HIGHSCORE)
                    messages.Add(new ObjectUpdateComposer(scoreitem, _room.OwnerId));
            }
            _room.SendMessage(messages);
        }

        internal void ScorebordChangeCheck()
        {
            if (_room.WiredScoreFirstBordInformation.Count != 3)
                return;

            var now = DateTime.Now;
            int todayDay = Convert.ToInt32(now.ToString("MMddyyyy"));
            int todayMonth = Convert.ToInt32(now.ToString("MM"));
            int todayWeek = CultureInfo.GetCultureInfo("Nl-nl").Calendar
                                 .GetWeekOfYear(now, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

            bool dayChanged = todayDay != _room.WiredScoreFirstBordInformation[0];
            bool monthChanged = todayMonth != _room.WiredScoreFirstBordInformation[1];
            bool weekChanged = todayWeek != _room.WiredScoreFirstBordInformation[2];

            _room.WiredScoreFirstBordInformation[0] = todayDay;
            _room.WiredScoreFirstBordInformation[1] = todayMonth;
            _room.WiredScoreFirstBordInformation[2] = todayWeek;

            if (dayChanged) _room.WiredScoreBordDay.Clear();
            if (monthChanged) _room.WiredScoreBordMonth.Clear();
            if (weekChanged) _room.WiredScoreBordWeek.Clear();
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  WALL POSITION
        // ══════════════════════════════════════════════════════════════════════════

        public string? WallPositionCheck(string wallPosition)
        {
            try
            {
                if (wallPosition.Contains(Convert.ToChar(13))) return null;
                if (wallPosition.Contains(Convert.ToChar(9))) return null;

                var posD = wallPosition.Split(' ');
                if (posD[2] != "l" && posD[2] != "r") return null;

                var widD = posD[0].Substring(3).Split(',');
                int widthX = int.Parse(widD[0]);
                int widthY = int.Parse(widD[1]);
                if (widthX < -1000 || widthY < -1 || widthX > 700 || widthY > 700) return null;

                var lenD = posD[1].Substring(2).Split(',');
                int lengthX = int.Parse(lenD[0]);
                int lengthY = int.Parse(lenD[1]);
                if (lengthX < -1 || lengthY < -1000 || lengthX > 700 || lengthY > 700) return null;

                return $":w={widthX},{widthY} l={lengthX},{lengthY} {posD[2]}";
            }
            catch { return null; }
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  LOAD FURNITURE
        // ══════════════════════════════════════════════════════════════════════════

        public void LoadFurniture(bool fromDB = false)
        {
            _floorItems.Clear();
            _wallItems.Clear();

            var items = ItemLoader.GetItemsForRoom(_room.Id, _room);

            var itemsToFixUserId = new List<int>(8);
            var itemsToMoveToInv = new List<Item>(4);
            var wallPosToReset = new List<int>(4);

            foreach (var item in items)
            {
                if (item == null) continue;

                if (item.UserID == 0)
                    itemsToFixUserId.Add(item.Id);

                if (item.IsFloorItem)
                {
                    if (!_room.GetGameMap().ValidTile(item.GetX, item.GetY))
                    {
                        itemsToMoveToInv.Add(item);
                        continue;
                    }
                    _floorItems.TryAdd(item.Id, item);
                }
                else if (item.IsWallItem)
                {
                    if (string.IsNullOrWhiteSpace(item.wallCoord))
                    {
                        wallPosToReset.Add(item.Id);
                        item.wallCoord = ":w=0,2 l=11,53 l";
                    }
                    else
                    {
                        try
                        {
                            item.wallCoord = WallPositionCheck($":{item.wallCoord.Split(':')[1]}");
                        }
                        catch
                        {
                            wallPosToReset.Add(item.Id);
                            item.wallCoord = ":w=0,2 l=11,53 l";
                        }
                    }
                    _wallItems.TryAdd(item.Id, item);
                }
            }

            if (itemsToFixUserId.Count > 0)
            {
                using var db = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
                db.SetQuery($"UPDATE `items` SET `user_id` = {_room.OwnerId} " +
                            $"WHERE `id` IN ({string.Join(",", itemsToFixUserId)})");
                db.RunQuery();
            }

            if (itemsToMoveToInv.Count > 0)
            {
                using var db = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
                db.SetQuery($"UPDATE `items` SET `room_id` = '0' " +
                            $"WHERE `id` IN ({string.Join(",", itemsToMoveToInv.Select(i => i.Id))})");
                db.RunQuery();

                foreach (var item in itemsToMoveToInv)
                {
                    var client = PolarEnvironment.GetGame().GetClientManager()
                                     .GetClientByUserID(item.UserID);
                    if (client == null) continue;
                    client.GetHabbo().GetInventoryComponent()
                          .AddNewItem(item.Id, item.BaseItem, item.ExtraData,
                                      item.GroupId, true, true, item.LimitedNo, item.LimitedTot);
                    client.GetHabbo().GetInventoryComponent().UpdateItems(false);
                }
            }

            if (wallPosToReset.Count > 0)
            {
                using var db = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
                db.SetQuery($"UPDATE `items` SET `wall_pos` = ':w=0,2 l=11,53 l' " +
                            $"WHERE `id` IN ({string.Join(",", wallPosToReset)})");
                db.RunQuery();
            }

            foreach (Item floorItem in _floorItems.Values)
            {
                var itype = floorItem.GetBaseItem().InteractionType;
                if (floorItem.IsRoller) mGotRollers = true;
                else if (floorItem.GetBaseItem().InteractionType == InteractionType.MOODLIGHT)
                {
                    if (_room.MoodlightData == null)
                        _room.MoodlightData = new MoodlightData(floorItem.Id);
                }
                else if (floorItem.GetBaseItem().InteractionType == InteractionType.TONER)
                {
                    if (_room.TonerData == null)
                        _room.TonerData = new TonerData(floorItem.Id);
                }
                else if (itype == InteractionType.HOPPER) HopperCount++;
                else if (itype == InteractionType.JUKEBOX) JukeboxCount++;
            }

            _room?.GetWired()?.LoadWiredBoxes(_floorItems.Values);
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  GET ITEM
        // ══════════════════════════════════════════════════════════════════════════

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Item GetItem(int pId)
        {
            if (_floorItems.TryGetValue(pId, out Item floor)) return floor;
            if (_wallItems.TryGetValue(pId, out Item wall)) return wall;
            return null;
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  REMOVE FURNITURE
        // ══════════════════════════════════════════════════════════════════════════

        public void RemoveFurniture(GameClient session, int pId, bool wasPicked = true)
        {
            Item item = GetItem(pId);
            if (item == null) return;

            if (item.GetBaseItem().InteractionType == InteractionType.FOOTBALL_GATE)
                _room.GetSoccer().UnRegisterGate(item);

            if (item.GetBaseItem().InteractionType != InteractionType.GIFT)
                item.Interactor.OnRemove(session, item);

            var itype = item.GetBaseItem().InteractionType;
            var iname = item.GetBaseItem().ItemName.ToLower();

            if (itype == InteractionType.GUILD_GATE ||
                itype == InteractionType.BASURERO ||
                itype == InteractionType.SLIDING_DOORS ||
                itype == InteractionType.SHOWER ||
                itype == InteractionType.TRASH_CAN ||
                itype == InteractionType.PEPSIMACHINE ||
                itype == InteractionType.COMIDAMACHINE ||
                itype == InteractionType.TRAGAMONEDAS ||
                itype == InteractionType.BASURAENTREGA ||
                itype == InteractionType.MINERIA ||
                itype == InteractionType.CAJERORUBY ||
                itype == InteractionType.CARAMELOMACHINE ||
                itype == InteractionType.AGUAENERGY ||
                iname == "olympics_c16_treadmill" ||
                iname == "olympics_c16_crosstrainer" ||
                iname == "olympics_c16_trampoline")
            {
                item.UpdateCounter = 0;
                item.UpdateNeeded = false;
            }

            RemoveRoomItem(item);
        }

        public void RemoveRoomItem(Item item)
        {
            if (item.IsFloorItem)
                _room.SendMessage(new ObjectRemoveComposer(item, item.UserID));
            else if (item.IsWallItem)
                _room.SendMessage(new ItemRemoveComposer(item, item.UserID));

            if (item.IsWallItem)
            {
                _wallItems.TryRemove(item.Id, out _);
            }
            else
            {
                _floorItems.TryRemove(item.Id, out _);
                _room.GetGameMap().RemoveFromMap(item);
            }

            // FIX: notificar a WiredComponent al quitar un ítem wired de la sala.
            // Sin esto, el box seguía registrado en _byCoord y _byType y continuaba
            // ejecutándose aunque el furni ya no estuviera en la sala.
            if (item.IsWired)
                _room.GetWired()?.TryRemove(item.Id);

            RemoveItem(item);
            _room.GetGameMap().GenerateMaps();
            _room.GetRoomUserManager().UpdateUserStatusses();
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  ROLLER CYCLE
        // ══════════════════════════════════════════════════════════════════════════

        private List<ServerPacket> CycleRollers()
        {
            if (!mGotRollers) return rollerMessages;

            if (mRollerCycle < mRollerSpeed && mRollerSpeed != 0)
            {
                mRollerCycle++;
                return rollerMessages;
            }

            rollerItemsMoved.Clear();
            rollerUsersMoved.Clear();
            rollerMessages.Clear();

            foreach (var roller in _rollers.Values)
            {
                if (roller == null) continue;

                var nextSquare = roller.SquareInFront;
                var itemsOnRoller = _room.GetGameMap().GetRoomItemForSquare(roller.GetX, roller.GetY, roller.GetZ);
                var itemsOnNext = _room.GetGameMap().GetAllRoomItemForSquare(nextSquare.X, nextSquare.Y);

                if (itemsOnRoller.Count > 10)
                    itemsOnRoller.RemoveRange(10, itemsOnRoller.Count - 10);

                bool nextSquareIsRoller = itemsOnNext.Any(
                    x => x.GetBaseItem().InteractionType == InteractionType.ROLLER);

                bool nextRollerClear = true;
                double nextZ = 0.0;
                bool nextRoller = false;

                foreach (var item in itemsOnNext)
                {
                    if (!item.IsRoller) continue;
                    if (item.TotalHeight > nextZ) nextZ = item.TotalHeight;
                    nextRoller = true;
                }

                if (nextRoller)
                {
                    foreach (var item in itemsOnNext)
                    {
                        if (item.TotalHeight > nextZ)
                            nextRollerClear = false;
                    }
                }

                foreach (Item rItem in itemsOnRoller)
                {
                    if (rItem == null) continue;

                    GameClient session = PolarEnvironment.GetGame().GetClientManager()
                                             .GetClientByUserID(rItem.UserID);

                    if (session != null)
                    {
                        bool flag = true;
                        var affectedTiles = Gamemap.GetAffectedTiles(
                            rItem.GetBaseItem().Length, rItem.GetBaseItem().Width,
                            nextSquare.X, nextSquare.Y, rItem.Rotation);

                        foreach (ThreeDCoord tile in affectedTiles.Values)
                        {
                            if (!_room.GetGameMap().ValidTile(tile.X, tile.Y) ||
                                (_room.GetGameMap().SquareHasUsers(tile.X, tile.Y) &&
                                 !rItem.GetBaseItem().IsSeat) ||
                                (!_room.CheckTerrain(session, tile.X, tile.Y) &&
                                 _room.OwnerId != session.GetHabbo().Id &&
                                 !_room.CheckRights(session, false, true)))
                            {
                                flag = false;
                                break;
                            }
                        }

                        if (flag &&
                            !rollerItemsMoved.Contains(rItem.Id) &&
                            (_room.GetGameMap().CanRollItemHere(nextSquare.X, nextSquare.Y, session) ||
                             _room.CheckRights(session, false, true)) &&
                            nextRollerClear &&
                            roller.GetZ < rItem.GetZ &&
                            _room.GetRoomUserManager().GetUserForSquare(nextSquare.X, nextSquare.Y) == null)
                        {
                            nextZ = nextSquareIsRoller
                                ? rItem.GetZ
                                : rItem.GetZ - roller.GetBaseItem().Height;

                            rollerMessages.Add(UpdateItemOnRoller(rItem, nextSquare, roller.Id, nextZ));
                            rollerItemsMoved.Add(rItem.Id);
                        }
                    }
                    else
                    {
                        if (!rollerItemsMoved.Contains(rItem.Id) &&
                            _room.GetGameMap().CanRollItemHere(nextSquare.X, nextSquare.Y, null) &&
                            nextRollerClear &&
                            roller.GetZ < rItem.GetZ &&
                            _room.GetRoomUserManager().GetUserForSquare(nextSquare.X, nextSquare.Y) == null)
                        {
                            nextZ = nextSquareIsRoller
                                ? rItem.GetZ
                                : rItem.GetZ - roller.GetBaseItem().Height;

                            rollerMessages.Add(UpdateItemOnRoller(rItem, nextSquare, roller.Id, nextZ));
                            rollerItemsMoved.Add(rItem.Id);
                        }
                    }
                }

                var rollerUser = _room.GetGameMap().GetRoomUsers(roller.Coordinate).FirstOrDefault();

                if (rollerUser != null &&
                    !rollerUser.IsWalking &&
                    nextRollerClear &&
                    _room.GetGameMap().IsValidStep(
                        rollerUser,
                        new Vector2D(roller.GetX, roller.GetY),
                        new Vector2D(nextSquare.X, nextSquare.Y),
                        true, false, true) &&
                    _room.GetGameMap().CanRollItemHere(nextSquare.X, nextSquare.Y) &&
                    _room.GetGameMap().GetFloorStatus(nextSquare) != 0 &&
                    !rollerUsersMoved.Contains(rollerUser.HabboId))
                {
                    nextZ = nextSquareIsRoller
                        ? rollerUser.Z
                        : rollerUser.Z - roller.GetBaseItem().Height;

                    rollerUser.isRolling = true;
                    rollerUser.rollerDelay = 1;
                    rollerMessages.Add(UpdateUserOnRoller(rollerUser, nextSquare, roller.Id, nextZ));
                    rollerUsersMoved.Add(rollerUser.HabboId);
                }
            }

            mRollerCycle = 0;
            return rollerMessages;
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  ROLLER UPDATE HELPERS
        // ══════════════════════════════════════════════════════════════════════════

        public ServerPacket UpdateItemOnRoller(Item pItem, Point nextCoord, int pRolledID, double nextZ)
        {
            var msg = new ServerPacket(ServerPacketHeader.SlideObjectBundleMessageComposer);
            msg.WriteInteger(pItem.GetX);
            msg.WriteInteger(pItem.GetY);
            msg.WriteInteger(nextCoord.X);
            msg.WriteInteger(nextCoord.Y);
            msg.WriteInteger(1);
            msg.WriteInteger(pItem.Id);
            msg.WriteString(pItem.GetZ.ToString());
            msg.WriteString(nextZ.ToString());
            msg.WriteInteger(0);
            SetFloorItem(pItem, nextCoord.X, nextCoord.Y, nextZ);
            return msg;
        }

        public ServerPacket UpdateUserOnRoller(RoomUser pUser, Point pNextCoord, int pRollerID, double nextZ)
        {
            if (pUser == null) throw new ArgumentNullException(nameof(pUser));

            var msg = new ServerPacket(ServerPacketHeader.SlideObjectBundleMessageComposer);
            msg.WriteInteger(pUser.X);
            msg.WriteInteger(pUser.Y);
            msg.WriteInteger(pNextCoord.X);
            msg.WriteInteger(pNextCoord.Y);
            msg.WriteInteger(0);
            msg.WriteInteger(pRollerID);
            msg.WriteInteger(2);
            msg.WriteInteger(pUser.VirtualId);
            msg.WriteString(pUser.Z.ToString());
            msg.WriteString(nextZ.ToString());

            var gameMap = _room.GetGameMap();
            int mapWidth = gameMap.GameMap.GetLength(0);
            int mapHeight = gameMap.GameMap.GetLength(1);

            bool currentValid = (uint)pUser.X < (uint)mapWidth && (uint)pUser.Y < (uint)mapHeight;
            bool nextValid = (uint)pNextCoord.X < (uint)mapWidth && (uint)pNextCoord.Y < (uint)mapHeight;

            if (!currentValid || !nextValid)
            {
                Logging.LogCriticalException(
                    $"UpdateUserOnRoller: coords fuera de rango. " +
                    $"Usuario=({pUser.X},{pUser.Y}) Next=({pNextCoord.X},{pNextCoord.Y}) " +
                    $"Mapa={mapWidth}x{mapHeight}");
                return msg;
            }

            gameMap.UpdateUserMovement(
                new Point(pUser.X, pUser.Y),
                new Point(pNextCoord.X, pNextCoord.Y),
                pUser);

            gameMap.GameMap[pUser.X, pUser.Y] = 1;
            pUser.X = pNextCoord.X;
            pUser.Y = pNextCoord.Y;
            pUser.Z = nextZ;
            gameMap.GameMap[pUser.X, pUser.Y] = 0;

            if (pUser.GetClient()?.GetHabbo() != null)
            {
                foreach (Item iItem in _room.GetGameMap().GetRoomItemForSquare(pNextCoord.X, pNextCoord.Y))
                {
                    if (iItem == null) continue;
                    _room.GetWired().TriggerEvent(
                        WiredBoxType.TriggerWalkOnFurni, pUser.GetClient().GetHabbo(), iItem);
                }

                Item rollerItem = GetItem(pRollerID);
                if (rollerItem != null)
                    _room.GetWired().TriggerEvent(
                        WiredBoxType.TriggerWalkOffFurni, pUser.GetClient().GetHabbo(), rollerItem);
            }

            return msg;
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  SAVE FURNITURE
        // ══════════════════════════════════════════════════════════════════════════

        public void SaveFurniture()
        {
            try
            {
                if (_movedItems.IsEmpty) return;

                var oldMoved = System.Threading.Interlocked.Exchange(
                    ref _movedItems,
                    new ConcurrentDictionary<int, Item>());

                if (oldMoved.IsEmpty) return;

                var snapshot = oldMoved.Values.ToList();

                foreach (Item item in snapshot)
                {
                    if (string.IsNullOrEmpty(item.ExtraData)) continue;
                    var itype = item.GetBaseItem().InteractionType;
                    var iname = item.GetBaseItem().ItemName.ToLower();
                    if (itype == InteractionType.SHOWER ||
                        itype == InteractionType.GUILD_GATE ||
                        itype == InteractionType.SLIDING_DOORS ||
                        iname == "olympics_c16_treadmill" ||
                        iname == "olympics_c16_crosstrainer")
                        item.ExtraData = "0";
                }

                _saveSb.Clear();
                _saveSb.Append(
                    "INSERT INTO `items` (`id`,`x`,`y`,`z`,`rot`,`extra_data`,`wall_pos`) VALUES ");

                bool first = true;
                foreach (Item item in snapshot)
                {
                    if (!first) _saveSb.Append(',');
                    first = false;

                    var iname = item.GetBaseItem().ItemName;
                    bool isSpecialWall = iname.Contains("wallpaper_single") ||
                                         iname.Contains("floor_single") ||
                                         iname.Contains("landscape_single");

                    string wallPos = (item.IsWallItem && !isSpecialWall)
                        ? MySqlHelper.EscapeString(item.wallCoord ?? "")
                        : "";
                    string extraData = MySqlHelper.EscapeString(item.ExtraData ?? "");

                    _saveSb.Append('(')
                           .Append(item.Id).Append(",'")
                           .Append(item.GetX).Append("','")
                           .Append(item.GetY).Append("','")
                           .Append(item.GetZ.ToString(CultureInfo.InvariantCulture)).Append("','")
                           .Append(item.Rotation).Append("','")
                           .Append(extraData).Append("','")
                           .Append(wallPos).Append("')");
                }

                _saveSb.Append(
                    " ON DUPLICATE KEY UPDATE " +
                    "`x`=VALUES(`x`), `y`=VALUES(`y`), `z`=VALUES(`z`), " +
                    "`rot`=VALUES(`rot`), " +
                    "`extra_data`=IF(VALUES(`extra_data`)='',`extra_data`,VALUES(`extra_data`)), " +
                    "`wall_pos`=IF(VALUES(`wall_pos`)='',`wall_pos`,VALUES(`wall_pos`))");

                using IQueryAdapter db = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
                db.SetQuery(_saveSb.ToString());
                db.RunQuery();
            }
            catch (Exception e)
            {
                Logging.LogCriticalException(
                    $"Error al guardar muebles sala {_room?.RoomId}. Stack: {e}");
            }
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  SET FLOOR ITEM  (colocación/movimiento por usuario)
        // ══════════════════════════════════════════════════════════════════════════

        public bool SetFloorItem(GameClient session, Item item, int newX, int newY, int newRot,
            bool newItem, bool onRoller, bool sendMessage,
            bool updateRoomUserStatuses = false, bool toDB = true,
            Item spaceItem = null, bool placedByRoleplay = false)
        {
            if (item == null) { Logging.LogCriticalException("SetFloorItem: Item null"); return false; }
            if (_room == null) { Logging.LogCriticalException("SetFloorItem: _room null"); return false; }

            bool hasValidSession = session?.GetHabbo() != null;
            bool needsReAdd = false;

            if (newItem && item.ExtraData == "")
                item.ExtraData = "0";

            if (newItem && item.IsWired)
            {
                if (_room.HideWired && _room.CheckRights(session, true, false))
                {
                    _room.HideWired = false;
                    session.SendWhisper("La zona ha vuelto a mostrar los Wireds.", 1);
                    _room.SendMessage(_room.HideWiredMessages(false));
                }

                if (item.GetBaseItem().WiredType == WiredBoxType.EffectRegenerateMaps &&
                    GetFloor.Any(x => x.GetBaseItem().WiredType == WiredBoxType.EffectRegenerateMaps))
                    return false;
            }

            List<Item> itemsOnTile = GetFurniObjects(newX, newY);

            if (item.GetBaseItem().InteractionType == InteractionType.ROLLER &&
                itemsOnTile.Any(x =>
                    x.GetBaseItem().InteractionType == InteractionType.ROLLER &&
                    x.Id != item.Id))
                return false;

            if (!newItem)
                needsReAdd = _room.GetGameMap().RemoveFromMap(item);

            var affectedTiles = Gamemap.GetAffectedTiles(
                item.GetBaseItem().Length, item.GetBaseItem().Width, newX, newY, newRot);

            if (!placedByRoleplay)
            {
                if (!_room.GetGameMap().ValidTile(newX, newY) ||
                    (_room.GetGameMap().SquareHasUsers(newX, newY) &&
                     !item.GetBaseItem().Walkable && !item.GetBaseItem().IsSeat))
                {
                    if (needsReAdd) _room.GetGameMap().AddToMap(item);
                    return false;
                }

                foreach (ThreeDCoord tile in affectedTiles.Values)
                {
                    if (!_room.GetGameMap().ValidTile(tile.X, tile.Y) ||
                        (_room.GetGameMap().SquareHasUsers(tile.X, tile.Y) &&
                         !item.GetBaseItem().IsSeat && hasValidSession) ||
                        (hasValidSession &&
                         !session.GetRoleplay().isParking &&
                         !_room.CheckTerrain(session, tile.X, tile.Y) &&
                         _room.OwnerId != session.GetHabbo().Id &&
                         !_room.CheckRights(session, false, true)))
                    {
                        if (needsReAdd) _room.GetGameMap().AddToMap(item);
                        return false;
                    }
                }
            }

            if (spaceItem != null && hasValidSession)
            {
                bool canPlaceHere = !affectedTiles.Values
                    .Select(x => new Point(x.X, x.Y))
                    .Any(x => !spaceItem.GetAffectedTiles.Contains(x));

                if (_room.OwnerId == session.GetHabbo().Id ||
                    _room.CheckRights(session) ||
                    session.GetHabbo().GetPermissions().HasRight("room_item_place_exchange_anywhere"))
                    canPlaceHere = true;

                if (!canPlaceHere) return false;
            }

            double newZ = _room.GetGameMap().Model.SqFloorHeight[newX, newY];

            if (hasValidSession && session.GetHabbo().DebugStacking)
                newZ = session.GetHabbo().StackHeight;

            if (!onRoller && !placedByRoleplay)
            {
                if (_room.GetGameMap().Model.SqState[newX, newY] != SquareState.OPEN &&
                    !item.GetBaseItem().IsSeat && !item.GetBaseItem().Walkable)
                    return false;

                foreach (ThreeDCoord tile in affectedTiles.Values)
                {
                    if (_room.GetGameMap().Model.SqState[tile.X, tile.Y] != SquareState.OPEN &&
                        !item.GetBaseItem().IsSeat && !item.GetBaseItem().Walkable)
                    {
                        if (needsReAdd) _room.GetGameMap().AddToMap(item);
                        return false;
                    }
                }

                if (!item.GetBaseItem().IsSeat && !item.IsRoller && !item.GetBaseItem().Walkable)
                {
                    foreach (ThreeDCoord tile in affectedTiles.Values)
                    {
                        if (_room.GetGameMap().GetRoomUsers(new Point(tile.X, tile.Y)).Count > 0)
                        {
                            if (needsReAdd) _room.GetGameMap().AddToMap(item);
                            return false;
                        }
                    }
                }
            }

            var itemsComplete = new List<Item>(itemsOnTile);
            foreach (ThreeDCoord tile in affectedTiles.Values)
            {
                var temp = GetFurniObjects(tile.X, tile.Y);
                if (temp != null) itemsComplete.AddRange(temp);
            }

            foreach (Item i in itemsComplete)
            {
                if (i == null || i.Id == item.Id) continue;

                if (i.GetBaseItem().InteractionType == InteractionType.STACKTOOL &&
                    hasValidSession && session.GetHabbo().StackHeight == 0)
                {
                    newZ = i.GetZ;
                    break;
                }

                if (i.TotalHeight > newZ)
                    newZ = (hasValidSession && session.GetHabbo().StackHeight != 0)
                        ? session.GetHabbo().StackHeight
                        : i.TotalHeight;
            }

            if (!onRoller)
            {
                foreach (Item roomItem in itemsComplete)
                {
                    if (roomItem != null && roomItem.Id != item.Id &&
                        roomItem.GetBaseItem() != null && !roomItem.GetBaseItem().Stackable)
                    {
                        if (needsReAdd) { UpdateItem(item); _room.GetGameMap().AddToMap(item); }
                        return false;
                    }
                }
            }

            if (newRot != 0 && newRot != 2 && newRot != 4 &&
                newRot != 6 && newRot != 8 && !item.GetBaseItem().ExtraRot)
                newRot = 0;


            item.Rotation = newRot;

            int oldX = item.GetX;
            int oldY = item.GetY;

            item.SetState(newX, newY, newZ, affectedTiles);

            if (!onRoller && hasValidSession)
                item.Interactor.OnPlace(session, item);

            if (newItem)
            {
                if (_floorItems.ContainsKey(item.Id))
                {
                    if (hasValidSession)
                        session.SendNotification(PolarEnvironment.GetGame().GetLanguageLocale()
                            .TryGetValue("room_item_placed"));
                    _room.GetGameMap().RemoveFromMap(item);
                    return true;
                }

                if (item.IsFloorItem) _floorItems.TryAdd(item.Id, item);
                else if (item.IsWallItem) _wallItems.TryAdd(item.Id, item);

                if (sendMessage)
                    _room.SendMessage(new ObjectAddComposer(item, _room));
            }
            else
            {
                UpdateItem(item);
                if (!onRoller && sendMessage)
                    _room.SendMessage(new ObjectUpdateComposer(item, item.UserID));
            }

            _room.GetGameMap().AddToMap(item);

            // FIX: si es un wired que se mueve (no es nuevo), actualizar WiredComponent.
            // Sin esto, el box seguía registrado en las coordenadas viejas y continuaba
            // ejecutándose aunque el furni ya estuviera en otro tile.
            if (item.IsWired && !newItem)
            {
                var wired = _room.GetWired();
                if (wired != null)
                {
                    wired.TryRemoveByCoord(item.Id, oldX, oldY); // <-- coordenada vieja
                    wired.LoadWiredBoxes(new[] { item });
                }
            }

            if (item.GetBaseItem().InteractionType == InteractionType.FOOTBALL &&
                item.GetRoom()?.GotSoccer() == true && hasValidSession)
            {
                RoomUser user = item.GetRoom().GetRoomUserManager()
                                    .GetRoomUserByHabbo(session.GetHabbo().Id);
                if (user != null)
                    item.GetRoom().GetSoccer().MoveBall(item, newX, newY, user);
            }

            if (item.GetBaseItem().IsSeat)
                updateRoomUserStatuses = true;

            if (updateRoomUserStatuses)
                _room.GetRoomUserManager().UpdateUserStatusses();

            if (item.GetBaseItem().InteractionType == InteractionType.TENT ||
                item.GetBaseItem().InteractionType == InteractionType.TENT_SMALL)
            {
                _room.RemoveTent(item.Id, item);
                _room.AddTent(item.Id);
            }

            using IQueryAdapter db = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
            db.SetQuery(
                "UPDATE `items` SET `room_id` = @rid, `x` = @x, `y` = @y, " +
                "`z` = @z, `rot` = @rot WHERE `id` = @id");
            db.AddParameter("rid", _room.RoomId);
            db.AddParameter("x", item.GetX);
            db.AddParameter("y", item.GetY);
            db.AddParameter("z", item.GetZ);
            db.AddParameter("rot", item.Rotation);
            db.AddParameter("id", item.Id);
            db.RunQuery();

            return true;
        }

        public List<Item> GetFurniObjects(int x, int y) =>
            _room.GetGameMap().GetCoordinatedItems(new Point(x, y));

        // ══════════════════════════════════════════════════════════════════════════
        //  SET FLOOR ITEM  (roller overload)
        // ══════════════════════════════════════════════════════════════════════════

        public bool SetFloorItem(Item item, int newX, int newY, double newZ)
        {
            if (_room == null) return false;

            _room.GetGameMap().RemoveFromMap(item);
            item.SetState(newX, newY, newZ,
                Gamemap.GetAffectedTiles(
                    item.GetBaseItem().Length, item.GetBaseItem().Width,
                    newX, newY, item.Rotation));

            if (item.GetBaseItem().InteractionType == InteractionType.TONER &&
                _room.TonerData == null)
                _room.TonerData = new TonerData(item.Id);

            UpdateItem(item);
            _room.GetGameMap().AddItemToMap(item, true);
            return true;
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  SET WALL ITEM
        // ══════════════════════════════════════════════════════════════════════════

        public bool SetWallItem(GameClient session, Item item)
        {
            if (!item.IsWallItem || _wallItems.ContainsKey(item.Id)) return false;
            if (_floorItems.ContainsKey(item.Id))
            {
                session.SendNotification(PolarEnvironment.GetGame().GetLanguageLocale()
                    .TryGetValue("room_item_placed"));
                return true;
            }

            item.Interactor.OnPlace(session, item);

            if (item.GetBaseItem().InteractionType == InteractionType.MOODLIGHT &&
                _room.MoodlightData == null)
            {
                _room.MoodlightData = new MoodlightData(item.Id);
                item.ExtraData = _room.MoodlightData.GenerateExtraData();
            }

            using IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
            dbClient.SetQuery(
                "UPDATE `items` SET `room_id` = @rid, `x` = @x, `y` = @y, " +
                "`z` = @z, `rot` = @rot, `wall_pos` = @wpos WHERE `id` = @id");
            dbClient.AddParameter("rid", _room.RoomId);
            dbClient.AddParameter("x", item.GetX);
            dbClient.AddParameter("y", item.GetY);
            dbClient.AddParameter("z", item.GetZ);
            dbClient.AddParameter("rot", item.Rotation);
            dbClient.AddParameter("wpos", item.wallCoord);
            dbClient.AddParameter("id", item.Id);
            dbClient.RunQuery();

            _wallItems.TryAdd(item.Id, item);
            _room.SendMessage(new ItemAddComposer(item));
            return true;
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  UPDATE ITEM / REMOVE ITEM
        // ══════════════════════════════════════════════════════════════════════════

        // FIX: indexer en vez de TryAdd — actualiza con la posición más reciente
        public void UpdateItem(Item item)
        {
            if (item == null) return;
            _movedItems[item.Id] = item;
        }

        // FIX: TryRemove directo sin ContainsKey previo
        public void RemoveItem(Item item)
        {
            if (item == null) return;
            _movedItems.TryRemove(item.Id, out _);
            _rollers.TryRemove(item.Id, out _);
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  ON CYCLE
        // ══════════════════════════════════════════════════════════════════════════

        public void OnCycle()
        {
            if (GotRollers)
            {
                try { _room.SendMessage(CycleRollers()); }
                catch { GotRollers = false; }
            }

            if (_roomItemUpdateQueue.IsEmpty) return;

            var addItems = new List<Item>(8);
            int maxProcessed = 200; // cap de seguridad anti-loop infinito
            int processed = 0;

            while (_roomItemUpdateQueue.TryDequeue(out Item item) && processed++ < maxProcessed)
            {
                item.ProcessUpdates();
                if (item.UpdateCounter > 0)
                    addItems.Add(item);
            }

            foreach (var item in addItems)
                if (item != null) _roomItemUpdateQueue.Enqueue(item);
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  REMOVE ITEMS / CLEAR ITEMS
        // ══════════════════════════════════════════════════════════════════════════

        public List<Item> RemoveItems(GameClient session)
        {
            var items = new List<Item>();

            foreach (Item item in GetWallAndFloor.ToList())
            {
                if (item == null) continue;

                if (item.IsFloorItem)
                {
                    _floorItems.TryRemove(item.Id, out _);
                    session.GetHabbo().GetInventoryComponent()._floorItems.TryAdd(item.Id, item);
                    _room.SendMessage(new ObjectRemoveComposer(item, item.UserID));
                }
                else if (item.IsWallItem)
                {
                    _wallItems.TryRemove(item.Id, out _);
                    session.GetHabbo().GetInventoryComponent()._wallItems.TryAdd(item.Id, item);
                    _room.SendMessage(new ItemRemoveComposer(item, item.UserID));
                }

                // FIX: limpiar wired del índice si corresponde
                if (item.IsWired)
                    _room.GetWired()?.TryRemove(item.Id);

                session.SendMessage(new FurniListAddComposer(item));
                items.Add(item);
            }

            _room.GetGameMap().GenerateMaps();
            return items;
        }

        public List<Item> ClearItems(GameClient session)
        {
            var items = new List<Item>();
            foreach (Item item in GetWallAndFloor.ToList())
            {
                if (item == null) continue;
                if (item.IsFloorItem) session.SendMessage(new ObjectRemoveComposer(item, 0));
                else if (item.IsWallItem) session.SendMessage(new ItemRemoveComposer(item, 0));

                // FIX: limpiar wired del índice si corresponde
                if (item.IsWired)
                    _room.GetWired()?.TryRemove(item.Id);

                items.Add(item);
            }
            return items;
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  PROPIEDADES
        // ══════════════════════════════════════════════════════════════════════════

        public ICollection<Item> GetFloor => _floorItems?.Values;
        public ICollection<Item> GetWall => _wallItems?.Values;
        public IEnumerable<Item> GetWallAndFloor => _floorItems.Values.Concat(_wallItems.Values);
        public ICollection<Item> GetRollers() => _rollers.Values;

        public Item GetFirstHighscore()
        {
            foreach (var item in _floorItems.Values)
                if (item.GetBaseItem().InteractionType == InteractionType.WIRED_HIGHSCORE)
                    return item;
            return null;
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  CHECK POS ITEM
        // ══════════════════════════════════════════════════════════════════════════

        public bool CheckPosItem(GameClient session, Item item, int newX, int newY,
            int newRot, bool newItem, bool sendNotify = true)
        {
            try
            {
                var tiles = Gamemap.GetAffectedTiles(
                    item.GetBaseItem().Length, item.GetBaseItem().Width, newX, newY, newRot);

                if (!_room.GetGameMap().ValidTile(newX, newY)) return false;

                int doorX = _room.GetGameMap().Model.DoorX;
                int doorY = _room.GetGameMap().Model.DoorY;

                if (newX == doorX && newY == doorY) return false;

                foreach (ThreeDCoord coord in tiles.Values)
                {
                    if (coord.X == doorX && coord.Y == doorY) return false;
                    if (!_room.GetGameMap().ValidTile(coord.X, coord.Y)) return false;
                    if (_room.GetGameMap().Model.SqState[coord.X, coord.Y] != SquareState.OPEN)
                        return false;
                }

                double num = _room.GetGameMap().Model.SqFloorHeight[newX, newY];
                if (item.Rotation == newRot && item.GetX == newX &&
                    item.GetY == newY && item.GetZ != num) return false;

                if (_room.GetGameMap().Model.SqState[newX, newY] != SquareState.OPEN)
                    return false;

                var allItems = new List<Item>(GetFurniObjects(newX, newY) ?? new List<Item>());
                foreach (ThreeDCoord coord in tiles.Values)
                {
                    var sub = GetFurniObjects(coord.X, coord.Y);
                    if (sub != null) allItems.AddRange(sub);
                }

                foreach (Item i in allItems)
                    if (i.Id != item.Id && !i.GetBaseItem().Stackable) return false;

                return true;
            }
            catch { return false; }
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  DISPOSE
        // ══════════════════════════════════════════════════════════════════════════

        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            foreach (Item item in GetWallAndFloor.ToList())
                item?.Destroy();

            _floorItems?.Clear();
            _wallItems?.Clear();
            _movedItems?.Clear();

            _room = null;
            _floorItems = null;
            _wallItems = null;
            _movedItems = null;
            _roomItemUpdateQueue = null;
        }
    }
}