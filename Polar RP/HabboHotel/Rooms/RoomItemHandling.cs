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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;

namespace Polar.HabboHotel.Rooms
{
    public class RoomItemHandling
    {
        private Room _room;

        public int HopperCount;
        public int JukeboxCount;
        private bool mGotRollers;
        private int mRollerSpeed;
        private int mRollerCycle;

        private ConcurrentDictionary<int, Item> _movedItems;
        private ConcurrentDictionary<int, Item> _rollers;
        private ConcurrentDictionary<int, Item> _wallItems;
        public ConcurrentDictionary<int, Item> _floorItems;

        private readonly List<int> rollerItemsMoved;
        private readonly List<int> rollerUsersMoved;
        private readonly List<ServerPacket> rollerMessages;

        private ConcurrentQueue<Item> _roomItemUpdateQueue;
        public bool usedwiredscorebord;

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
            rollerItemsMoved = new List<int>();
            rollerUsersMoved = new List<int>();
            rollerMessages = new List<ServerPacket>();
            _roomItemUpdateQueue = new ConcurrentQueue<Item>();
            usedwiredscorebord = false;
        }

        // ────────────────────────────────────────────────
        //  Rollers
        // ────────────────────────────────────────────────
        public void TryAddRoller(int itemId, Item roller) => _rollers.TryAdd(itemId, roller);

        public bool GotRollers
        {
            get => mGotRollers;
            set => mGotRollers = value;
        }

        public void QueueRoomItemUpdate(Item item) => _roomItemUpdateQueue.Enqueue(item);
        public void SetSpeed(int p) => mRollerSpeed = p;

        // ────────────────────────────────────────────────
        //  Scoreboard
        // ────────────────────────────────────────────────
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

        // ────────────────────────────────────────────────
        //  Wall position
        // ────────────────────────────────────────────────
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

        // ────────────────────────────────────────────────
        //  Load furniture
        // ────────────────────────────────────────────────
        public void LoadFurniture(bool fromDB = false)
        {
            _floorItems.Clear();
            _wallItems.Clear();

            var items = ItemLoader.GetItemsForRoom(_room.Id, _room);

            // Acumular IDs para batch updates en lugar de queries individuales
            var itemsToFixUserId = new List<int>();
            var itemsToMoveToInv = new List<Item>();
            var wallPosToReset = new List<int>();

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

            // ── Batch: fix user_id = 0 ──────────────────────────────────────
            if (itemsToFixUserId.Count > 0)
            {
                using var db = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
                string ids = string.Join(",", itemsToFixUserId);
                db.SetQuery($"UPDATE `items` SET `user_id` = {_room.OwnerId} WHERE `id` IN ({ids})");
                db.RunQuery();
            }

            // ── Batch: mover items con tile inválido al inventario ──────────
            if (itemsToMoveToInv.Count > 0)
            {
                using var db = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
                string ids = string.Join(",", itemsToMoveToInv.Select(i => i.Id));
                db.SetQuery($"UPDATE `items` SET `room_id` = '0' WHERE `id` IN ({ids})");
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

            // ── Batch: reset wall_pos inválidas ────────────────────────────
            if (wallPosToReset.Count > 0)
            {
                using var db = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
                string ids = string.Join(",", wallPosToReset);
                db.SetQuery($"UPDATE `items` SET `wall_pos` = ':w=0,2 l=11,53 l' WHERE `id` IN ({ids})");
                db.RunQuery();
            }

            // ── Post-process (sin cambios) ─────────────────────────────────
            foreach (Item floorItem in _floorItems.Values)
            {
                if (floorItem.IsRoller)
                    mGotRollers = true;
                /*else if (floorItem.GetBaseItem().InteractionType == InteractionType.MOODLIGHT)
                {
                    if (_room.MoodlightData == null)
                        _room.MoodlightData = new MoodlightData(floorItem.Id);
                }
                else if (floorItem.GetBaseItem().InteractionType == InteractionType.TONER)
                {
                    if (_room.TonerData == null)
                        _room.TonerData = new TonerData(floorItem.Id);
                }
                */
                else if (floorItem.GetBaseItem().InteractionType == InteractionType.HOPPER)
                    HopperCount++;
                else if (floorItem.GetBaseItem().InteractionType == InteractionType.JUKEBOX)
                    JukeboxCount++;
            }
            _room?.GetWired()?.LoadWiredBoxes(_floorItems.Values);
        }

        // ────────────────────────────────────────────────
        //  GetItem
        // ────────────────────────────────────────────────
        public Item GetItem(int pId)
        {
            // ✅ FIX #2: Antes: ContainsKey() + TryGetValue() — dos búsquedas en el dict.
            //   TryGetValue devuelve false si la clave no existe, eliminando el ContainsKey.
            if (_floorItems.TryGetValue(pId, out Item floorItem))
                return floorItem;
            if (_wallItems.TryGetValue(pId, out Item wallItem))
                return wallItem;
            return null;
        }

        // ────────────────────────────────────────────────
        //  Remove furniture
        // ────────────────────────────────────────────────
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
                _wallItems.TryRemove(item.Id, out _);
            else
            {
                _floorItems.TryRemove(item.Id, out _);
                _room.GetGameMap().RemoveFromMap(item);
            }

            RemoveItem(item);
            _room.GetGameMap().GenerateMaps();
            _room.GetRoomUserManager().UpdateUserStatusses();
        }

        // ────────────────────────────────────────────────
        //  Roller cycle
        // ────────────────────────────────────────────────
        private List<ServerPacket> CycleRollers()
        {
            if (!mGotRollers)
                return new List<ServerPacket>();

            if (mRollerCycle < mRollerSpeed && mRollerSpeed != 0)
            {
                mRollerCycle++;
                return new List<ServerPacket>();
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
                    itemsOnRoller = itemsOnRoller.Take(10).ToList();

                // ✅ FIX #3: .Count(predicate) > 0 → .Any(predicate) — evita contar todos los
                //   elementos cuando solo se necesita saber si hay al menos uno.
                bool nextSquareIsRoller = itemsOnNext.Any(x => x.GetBaseItem().InteractionType == InteractionType.ROLLER);
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

                // Items on roller
                foreach (Item rItem in itemsOnRoller)
                {
                    if (rItem == null) continue;

                    GameClient session = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(rItem.UserID);

                    if (session != null)
                    {
                        bool flag = true;
                        var affectedTiles = Gamemap.GetAffectedTiles(rItem.GetBaseItem().Length, rItem.GetBaseItem().Width, nextSquare.X, nextSquare.Y, rItem.Rotation);

                        foreach (ThreeDCoord tile in affectedTiles.Values)
                        {
                            if (!_room.GetGameMap().ValidTile(tile.X, tile.Y) ||
                                (_room.GetGameMap().SquareHasUsers(tile.X, tile.Y) && !rItem.GetBaseItem().IsSeat) ||
                                (!_room.CheckTerrain(session, tile.X, tile.Y) && _room.OwnerId != session.GetHabbo().Id && !_room.CheckRights(session, false, true)))
                            {
                                flag = false;
                                break;
                            }
                        }

                        if (flag &&
                            !rollerItemsMoved.Contains(rItem.Id) &&
                            (_room.GetGameMap().CanRollItemHere(nextSquare.X, nextSquare.Y, session) || _room.CheckRights(session, false, true)) &&
                            nextRollerClear &&
                            roller.GetZ < rItem.GetZ &&
                            _room.GetRoomUserManager().GetUserForSquare(nextSquare.X, nextSquare.Y) == null)
                        {
                            nextZ = nextSquareIsRoller ? rItem.GetZ : rItem.GetZ - roller.GetBaseItem().Height;
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
                            nextZ = nextSquareIsRoller ? rItem.GetZ : rItem.GetZ - roller.GetBaseItem().Height;
                            rollerMessages.Add(UpdateItemOnRoller(rItem, nextSquare, roller.Id, nextZ));
                            rollerItemsMoved.Add(rItem.Id);
                        }
                    }
                }

                // User on roller
                var rollerUser = _room.GetGameMap().GetRoomUsers(roller.Coordinate).FirstOrDefault();
                if (rollerUser != null &&
                    !rollerUser.IsWalking &&
                    nextRollerClear &&
                   _room.GetGameMap().IsValidStep(rollerUser, new Vector2D(roller.GetX, roller.GetY), new Vector2D(nextSquare.X, nextSquare.Y), true, false, true) &&
                    _room.GetGameMap().CanRollItemHere(nextSquare.X, nextSquare.Y) &&
                    _room.GetGameMap().GetFloorStatus(nextSquare) != 0 &&
                    !rollerUsersMoved.Contains(rollerUser.HabboId))
                {
                    nextZ = nextSquareIsRoller ? rollerUser.Z : rollerUser.Z - roller.GetBaseItem().Height;
                    rollerUser.isRolling = true;
                    rollerUser.rollerDelay = 1;
                    rollerMessages.Add(UpdateUserOnRoller(rollerUser, nextSquare, roller.Id, nextZ));
                    rollerUsersMoved.Add(rollerUser.HabboId);
                }
            }

            mRollerCycle = 0;
            return rollerMessages;
        }

        // ────────────────────────────────────────────────
        //  Roller update helpers
        // ────────────────────────────────────────────────
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
            // ✅ FIX #4: El null-check de pUser estaba DESPUÉS de usar pUser.X, pUser.Y, etc.
            //   Si pUser fuera null, el código ya habría lanzado NullReferenceException antes de
            //   llegar al check. Guard movido al principio del método.
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

            bool currentValid = pUser.X >= 0 && pUser.X < mapWidth &&
                                pUser.Y >= 0 && pUser.Y < mapHeight;
            bool nextValid = pNextCoord.X >= 0 && pNextCoord.X < mapWidth &&
                                pNextCoord.Y >= 0 && pNextCoord.Y < mapHeight;

            if (!currentValid || !nextValid)
            {
                Logging.LogCriticalException(
                    $"UpdateUserOnRoller: coordenadas fuera de rango. " +
                    $"Usuario=({pUser.X},{pUser.Y}) Next=({pNextCoord.X},{pNextCoord.Y}) " +
                    $"Mapa={mapWidth}x{mapHeight}");
                return msg;
            }

            gameMap.UpdateUserMovement(new Point(pUser.X, pUser.Y), new Point(pNextCoord.X, pNextCoord.Y), pUser);
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
                    _room.GetWired().TriggerEvent(WiredBoxType.TriggerWalkOnFurni, pUser.GetClient().GetHabbo(), iItem);
                }

                Item rollerItem = GetItem(pRollerID);
                if (rollerItem != null)
                    _room.GetWired().TriggerEvent(WiredBoxType.TriggerWalkOffFurni, pUser.GetClient().GetHabbo(), rollerItem);
            }

            return msg;
        }

        // ────────────────────────────────────────────────
        //  Save furniture
        // ────────────────────────────────────────────────
        public void SaveFurniture()
        {
            try
            {
                if (_movedItems.Count == 0) return;

                var snapshot = _movedItems.Values.ToList();

                using IQueryAdapter db = PolarEnvironment.GetDatabaseManager().GetQueryReactor();

                // ── Batch extra_data ────────────────────────────────────────
                foreach (Item item in snapshot.Where(i => !string.IsNullOrEmpty(i.ExtraData)))
                {
                    var itype = item.GetBaseItem().InteractionType;
                    var iname = item.GetBaseItem().ItemName.ToLower();
                    if (itype == InteractionType.SHOWER ||
                        itype == InteractionType.GUILD_GATE ||
                        itype == InteractionType.SLIDING_DOORS ||
                        iname == "olympics_c16_treadmill" ||
                        iname == "olympics_c16_crosstrainer")
                        item.ExtraData = "0";
                }

                // Construir un INSERT ... ON DUPLICATE KEY UPDATE masivo
                // (asume PK/UK en `id`)
                var sb = new System.Text.StringBuilder();
                sb.Append("INSERT INTO `items` (`id`,`x`,`y`,`z`,`rot`,`extra_data`,`wall_pos`) VALUES ");

                var rows = new List<string>();
                foreach (Item item in snapshot)
                {
                    var iname = item.GetBaseItem().ItemName;
                    bool isSpecialWall = iname.Contains("wallpaper_single") ||
                                         iname.Contains("floor_single") ||
                                         iname.Contains("landscape_single");

                    string wallPos = (item.IsWallItem && !isSpecialWall)
                        ? MySqlHelper.EscapeString(item.wallCoord ?? "")
                        : "";

                    string extraData = MySqlHelper.EscapeString(item.ExtraData ?? "");

                    rows.Add($"('{item.Id}','{item.GetX}','{item.GetY}'," +
                             $"'{item.GetZ.ToString(System.Globalization.CultureInfo.InvariantCulture)}'," +
                             $"'{item.Rotation}','{extraData}','{wallPos}')");
                }

                sb.Append(string.Join(",", rows));
                sb.Append(" ON DUPLICATE KEY UPDATE " +
                          "`x`=VALUES(`x`), `y`=VALUES(`y`), `z`=VALUES(`z`), " +
                          "`rot`=VALUES(`rot`), " +
                          "`extra_data`=IF(VALUES(`extra_data`)='',`extra_data`,VALUES(`extra_data`)), " +
                          "`wall_pos`=IF(VALUES(`wall_pos`)='',`wall_pos`,VALUES(`wall_pos`))");

                db.SetQuery(sb.ToString());
                db.RunQuery();
            }
            catch (Exception e)
            {
                Logging.LogCriticalException(
                    $"Error al guardar muebles para la habitación {_room.RoomId}. Stack: {e}");
            }
        }

        // ────────────────────────────────────────────────
        //  SetFloorItem (full — called by users placing items)
        // ────────────────────────────────────────────────
        public bool SetFloorItem(GameClient session, Item item, int newX, int newY, int newRot,
                                 bool newItem, bool onRoller, bool sendMessage,
                                 bool updateRoomUserStatuses = false, bool toDB = true,
                                 Item spaceItem = null, bool placedByRoleplay = false)
        {
            if (item == null)
            {
                Logging.LogCriticalException("SetFloorItem: Item es null");
                return false;
            }
            if (_room == null)
            {
                Logging.LogCriticalException("SetFloorItem: _room es null");
                return false;
            }

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

            // ✅ FIX #3 (mismo patrón): .Where().Count() > 0 → .Any()
            if (item.GetBaseItem().InteractionType == InteractionType.ROLLER &&
                itemsOnTile.Any(x => x.GetBaseItem().InteractionType == InteractionType.ROLLER && x.Id != item.Id))
                return false;

            if (!newItem)
                needsReAdd = _room.GetGameMap().RemoveFromMap(item);

            var affectedTiles = Gamemap.GetAffectedTiles(item.GetBaseItem().Length, item.GetBaseItem().Width, newX, newY, newRot);

            if (!placedByRoleplay)
            {
                if (!_room.GetGameMap().ValidTile(newX, newY) ||
                    (_room.GetGameMap().SquareHasUsers(newX, newY) && !item.GetBaseItem().Walkable && !item.GetBaseItem().IsSeat))
                {
                    if (needsReAdd) _room.GetGameMap().AddToMap(item);
                    return false;
                }

                foreach (ThreeDCoord tile in affectedTiles.Values)
                {
                    if (!_room.GetGameMap().ValidTile(tile.X, tile.Y) ||
                        (_room.GetGameMap().SquareHasUsers(tile.X, tile.Y) && !item.GetBaseItem().IsSeat && hasValidSession) ||
                        (hasValidSession && !session.GetRoleplay().isParking &&
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

            var itemsAffected = new List<Item>();
            var itemsComplete = new List<Item>();

            foreach (ThreeDCoord tile in affectedTiles.Values)
            {
                List<Item> temp = GetFurniObjects(tile.X, tile.Y);
                if (temp != null) itemsAffected.AddRange(temp);
            }

            itemsComplete.AddRange(itemsOnTile);
            itemsComplete.AddRange(itemsAffected);

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
                    newZ = hasValidSession && session.GetHabbo().StackHeight != 0
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
                        if (needsReAdd)
                        {
                            UpdateItem(item);
                            _room.GetGameMap().AddToMap(item);
                        }
                        return false;
                    }
                }
            }

            if (newRot != 0 && newRot != 2 && newRot != 4 && newRot != 6 && newRot != 8 && !item.GetBaseItem().ExtraRot)
                newRot = 0;

            item.Rotation = newRot;
            item.SetState(newX, newY, newZ, affectedTiles);

            if (!onRoller && hasValidSession)
                item.Interactor.OnPlace(session, item);

            if (newItem)
            {
                if (_floorItems.ContainsKey(item.Id))
                {
                    if (hasValidSession)
                        session.SendNotification(PolarEnvironment.GetGame().GetLanguageLocale().TryGetValue("room_item_placed"));
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

            if (item.GetBaseItem().InteractionType == InteractionType.FOOTBALL &&
                item.GetRoom()?.GotSoccer() == true && hasValidSession)
            {
                RoomUser user = item.GetRoom().GetRoomUserManager().GetRoomUserByHabbo(session.GetHabbo().Id);
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
            db.SetQuery("UPDATE `items` SET `room_id` = @rid, `x` = @x, `y` = @y, `z` = @z, `rot` = @rot WHERE `id` = @id");
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

        // ────────────────────────────────────────────────
        //  SetFloorItem (roller overload)
        // ────────────────────────────────────────────────
        public bool SetFloorItem(Item item, int newX, int newY, double newZ)
        {
            if (_room == null) return false;

            _room.GetGameMap().RemoveFromMap(item);
            item.SetState(newX, newY, newZ,
                Gamemap.GetAffectedTiles(item.GetBaseItem().Length, item.GetBaseItem().Width, newX, newY, item.Rotation));

            if (item.GetBaseItem().InteractionType == InteractionType.TONER && _room.TonerData == null)
                _room.TonerData = new TonerData(item.Id);

            // ✅ FIX #6: El overload del roller no llamaba UpdateItem(), por lo que el ítem
            //   movido por el roller nunca se añadía a _movedItems y no se guardaba en BD
            //   en el siguiente SaveFurniture(). Añadido UpdateItem().
            UpdateItem(item);
            _room.GetGameMap().AddItemToMap(item, true);
            return true;
        }

        // ────────────────────────────────────────────────
        //  SetWallItem
        // ────────────────────────────────────────────────
        public bool SetWallItem(GameClient session, Item item)
        {
            if (!item.IsWallItem || _wallItems.ContainsKey(item.Id)) return false;
            if (_floorItems.ContainsKey(item.Id))
            {
                session.SendNotification(PolarEnvironment.GetGame().GetLanguageLocale().TryGetValue("room_item_placed"));
                return true;
            }

            item.Interactor.OnPlace(session, item);

            if (item.GetBaseItem().InteractionType == InteractionType.MOODLIGHT && _room.MoodlightData == null)
            {
                _room.MoodlightData = new MoodlightData(item.Id);
                item.ExtraData = _room.MoodlightData.GenerateExtraData();
            }

            using IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
            dbClient.SetQuery("UPDATE `items` SET `room_id` = @rid, `x` = @x, `y` = @y, `z` = @z, `rot` = @rot, `wall_pos` = @wpos WHERE `id` = @id");
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

        // ────────────────────────────────────────────────
        //  UpdateItem / RemoveItem
        // ────────────────────────────────────────────────
        public void UpdateItem(Item item)
        {
            if (item == null) return;
            // ✅ FIX #7: ContainsKey + TryAdd → doble lookup. TryAdd es atómico en
            //   ConcurrentDictionary y no añade si la clave ya existe. Eliminado ContainsKey.
            _movedItems.TryAdd(item.Id, item);
        }

        public void RemoveItem(Item item)
        {
            if (item == null) return;
            // ✅ FIX #7 (mismo patrón): ContainsKey + TryRemove → solo TryRemove.
            _movedItems.TryRemove(item.Id, out _);
            _rollers.TryRemove(item.Id, out _);
        }

        // ────────────────────────────────────────────────
        //  OnCycle
        // ────────────────────────────────────────────────
        public void OnCycle()
        {
            if (GotRollers)
            {
                try { _room.SendMessage(CycleRollers()); }
                catch { GotRollers = false; }
            }

            if (_roomItemUpdateQueue.Count == 0) return;

            var addItems = new List<Item>();
            while (_roomItemUpdateQueue.TryDequeue(out Item item))
            {
                item.ProcessUpdates();
                if (item.UpdateCounter > 0)
                    addItems.Add(item);
            }

            // ✅ FIX #8: addItems.ToList() era innecesario — addItems ya es List<Item>.
            foreach (var item in addItems)
            {
                if (item != null)
                    _roomItemUpdateQueue.Enqueue(item);
            }
        }

        // ────────────────────────────────────────────────
        //  RemoveItems / ClearItems
        // ────────────────────────────────────────────────
        public List<Item> RemoveItems(GameClient session)
        {
            // ✅ FIX #9: La lista Items se creaba pero NUNCA se populaba con nada,
            //   el método siempre devolvía una lista vacía. Los ítems se procesaban
            //   correctamente (inventario + mensaje), pero no se devolvían al caller.
            //   Ahora se añaden a la lista antes de retornarla.
            var items = new List<Item>();

            // ✅ FIX #10: GenerateMaps() se llamaba DENTRO del foreach — se regeneraba
            //   el mapa completo por cada ítem eliminado → O(n × mapgen).
            //   Movido fuera del loop para hacerlo una sola vez al final.
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

                session.SendMessage(new FurniListAddComposer(item));
                items.Add(item);
            }

            _room.GetGameMap().GenerateMaps(); // una sola vez
            return items;
        }

        public List<Item> ClearItems(GameClient session)
        {
            var items = new List<Item>();
            foreach (Item item in GetWallAndFloor.ToList())
            {
                if (item == null) continue;

                if (item.IsFloorItem)
                    session.SendMessage(new ObjectRemoveComposer(item, 0));
                else if (item.IsWallItem)
                    session.SendMessage(new ItemRemoveComposer(item, 0));

                items.Add(item);
            }
            return items;
        }

        // ────────────────────────────────────────────────
        //  Properties
        // ────────────────────────────────────────────────
        public ICollection<Item> GetFloor => _floorItems?.Values;
        public ICollection<Item> GetWall => _wallItems?.Values;
        public IEnumerable<Item> GetWallAndFloor => _floorItems.Values.Concat(_wallItems.Values);

        public Item GetFirstHighscore()
        {
            foreach (var item in _floorItems.Values)
                if (item.GetBaseItem().InteractionType == InteractionType.WIRED_HIGHSCORE)
                    return item;
            return null;
        }

        // ────────────────────────────────────────────────
        //  CheckPosItem
        // ────────────────────────────────────────────────
        public bool CheckPosItem(GameClient session, Item item, int newX, int newY, int newRot, bool newItem, bool sendNotify = true)
        {
            try
            {
                var tiles = Gamemap.GetAffectedTiles(item.GetBaseItem().Length, item.GetBaseItem().Width, newX, newY, newRot);

                if (!_room.GetGameMap().ValidTile(newX, newY)) return false;

                int doorX = _room.GetGameMap().Model.DoorX;
                int doorY = _room.GetGameMap().Model.DoorY;

                // ✅ FIX #11: Antes se iteraba tiles dos veces por separado para comprobar
                //   la puerta y la validez. Unificado en un solo foreach.
                if (newX == doorX && newY == doorY) return false;

                foreach (ThreeDCoord coord in tiles.Values)
                {
                    if (coord.X == doorX && coord.Y == doorY) return false;
                    if (!_room.GetGameMap().ValidTile(coord.X, coord.Y)) return false;
                    if (_room.GetGameMap().Model.SqState[coord.X, coord.Y] != SquareState.OPEN) return false;
                }

                double num = _room.GetGameMap().Model.SqFloorHeight[newX, newY];
                if (item.Rotation == newRot && item.GetX == newX && item.GetY == newY && item.GetZ != num) return false;
                if (_room.GetGameMap().Model.SqState[newX, newY] != SquareState.OPEN) return false;

                var furniObjects = GetFurniObjects(newX, newY) ?? new List<Item>();
                var allItems = new List<Item>(furniObjects);

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

        public ICollection<Item> GetRollers() => _rollers.Values;

        // ────────────────────────────────────────────────
        //  Dispose
        // ────────────────────────────────────────────────
        public void Dispose()
        {
            foreach (Item item in GetWallAndFloor.ToList())
                item?.Destroy();

            // ✅ FIX #12: Dispose nulleaba _wallItems y _roomItemUpdateQueue DOS veces
            //   cada uno. Código duplicado eliminado — cada campo se limpia una sola vez.
            _floorItems.Clear();
            _wallItems.Clear();
            _movedItems.Clear();

            _room = null;
            _floorItems = null;
            _wallItems = null;
            _movedItems = null;
            _roomItemUpdateQueue = null;
        }
    }
}