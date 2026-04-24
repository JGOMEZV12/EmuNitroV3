using System.Drawing;
using Polar.Core;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.Rooms.Games.Teams;
using System.Collections.Concurrent;
using Polar.HabboHotel.Pathfinding;

namespace Polar.HabboHotel.Rooms
{
    public class Gamemap : IDisposable
    {
        private Room _room;
        private RoomModel mStaticModel;
        private RoomModel _staticModel;
        private DynamicRoomModel _dynamicModel;

        public bool DiagonalEnabled;

        // Arrays del mapa — se reinicializan en GenerateMaps
        public byte[,] GameMap { get; private set; }
        public byte[,] EffectMap { get; private set; }
        public byte[,] mUserOnMap { get; private set; }
        public byte[,] mSquareTaking { get; private set; }
        public double[,] _itemHeightmap;

        // Índices de datos — accedidos en hot paths, ConcurrentDictionary por thread-safety
        private ConcurrentDictionary<Point, List<int>> _coordinatedItems;
        private ConcurrentDictionary<Point, List<RoomUser>> _userMap;

        // ✅ FIX #15: teamMap se creaba dentro de HandleGameItemRegistration como
        //   new Dictionary<> en CADA llamada — es decir, en cada ítem que se añade
        //   al mapa (GenerateMaps, AddToMap, etc.). Esta tabla es completamente estática.
        //   Declarada aquí como campo readonly estático: se crea UNA sola vez en el
        //   ClassLoader y se reutiliza para siempre.
        private static readonly IReadOnlyDictionary<InteractionType, TEAM> _teamMap =
            new Dictionary<InteractionType, TEAM>
            {
                [InteractionType.FOOTBALL_GOAL_RED] = TEAM.RED,
                [InteractionType.footballcounterred] = TEAM.RED,
                [InteractionType.banzaiscorered] = TEAM.RED,
                [InteractionType.banzaigatered] = TEAM.RED,
                [InteractionType.freezeredcounter] = TEAM.RED,
                [InteractionType.FREEZE_RED_GATE] = TEAM.RED,
                [InteractionType.FOOTBALL_GOAL_GREEN] = TEAM.GREEN,
                [InteractionType.footballcountergreen] = TEAM.GREEN,
                [InteractionType.banzaiscoregreen] = TEAM.GREEN,
                [InteractionType.banzaigategreen] = TEAM.GREEN,
                [InteractionType.freezegreencounter] = TEAM.GREEN,
                [InteractionType.FREEZE_GREEN_GATE] = TEAM.GREEN,
                [InteractionType.FOOTBALL_GOAL_BLUE] = TEAM.BLUE,
                [InteractionType.footballcounterblue] = TEAM.BLUE,
                [InteractionType.banzaiscoreblue] = TEAM.BLUE,
                [InteractionType.banzaigateblue] = TEAM.BLUE,
                [InteractionType.freezebluecounter] = TEAM.BLUE,
                [InteractionType.FREEZE_BLUE_GATE] = TEAM.BLUE,
                [InteractionType.FOOTBALL_GOAL_YELLOW] = TEAM.YELLOW,
                [InteractionType.footballcounteryellow] = TEAM.YELLOW,
                [InteractionType.banzaiscoreyellow] = TEAM.YELLOW,
                [InteractionType.banzaigateyellow] = TEAM.YELLOW,
                [InteractionType.freezeyellowcounter] = TEAM.YELLOW,
                [InteractionType.FREEZE_YELLOW_GATE] = TEAM.YELLOW,
            };

        // ✅ FIX #16: Lock dedicado para mutaciones en _userMap y _coordinatedItems.
        //   ConcurrentDictionary hace el diccionario thread-safe, pero la LIST dentro
        //   de cada valor NO es thread-safe. Las lambda de AddOrUpdate pueden ejecutarse
        //   en distintos hilos concurrentemente sobre la misma lista → corrupción.
        //   El lock sólo protege la mutación de la lista, no la lectura del diccionario.
        private readonly object _userMapLock = new object();
        private readonly object _coordItemLock = new object();

        public Gamemap(Room room)
        {
            _room = room;
            DiagonalEnabled = true;

            mStaticModel = PolarEnvironment.GetGame().GetRoomManager().GetModel(room.ModelName, room.Id);
            if (mStaticModel == null)
                throw new Exception("No modeldata found for roomID " + room.Id);

            _staticModel = mStaticModel;
            _dynamicModel = new DynamicRoomModel(mStaticModel);

            InitializeArrays();

            _userMap = new ConcurrentDictionary<Point, List<RoomUser>>();
            _coordinatedItems = new ConcurrentDictionary<Point, List<int>>();
        }

        private void InitializeArrays()
        {
            int sizeX = Model.MapSizeX;
            int sizeY = Model.MapSizeY;

            GameMap = new byte[sizeX, sizeY];
            mUserOnMap = new byte[sizeX, sizeY];
            mSquareTaking = new byte[sizeX, sizeY];
            EffectMap = new byte[sizeX, sizeY];
            _itemHeightmap = new double[sizeX, sizeY];
        }

        #region User Management

        public void AddUserToMap(RoomUser user, Point coord)
        {
            if (user == null) return;

            // ✅ FIX #16 aplicado: lock protege la lista interna
            lock (_userMapLock)
            {
                _userMap.AddOrUpdate(coord,
                    _ => new List<RoomUser> { user },
                    (_, list) =>
                    {
                        if (!list.Contains(user)) list.Add(user);
                        return list;
                    });
            }

            if (ValidTile(coord.X, coord.Y))
                mUserOnMap[coord.X, coord.Y] = 1;
        }

        public void RemoveUserFromMap(RoomUser user, Point coord)
        {
            if (user == null) return;

            bool isEmpty = false;
            lock (_userMapLock)
            {
                if (!_userMap.TryGetValue(coord, out var list)) return;

                list.RemoveAll(u => u?.VirtualId == user.VirtualId);

                if (list.Count == 0)
                {
                    _userMap.TryRemove(coord, out _);
                    isEmpty = true;
                }
            }

            if (isEmpty && ValidTile(coord.X, coord.Y))
                mUserOnMap[coord.X, coord.Y] = 0;
        }

        public void UpdateUserMovement(Point oldCoord, Point newCoord, RoomUser user)
        {
            RemoveUserFromMap(user, oldCoord);
            AddUserToMap(user, newCoord);
        }

        public bool MapGotUser(Point coord)
        {
            return _userMap.TryGetValue(coord, out var users) && users.Count > 0;
        }

        public bool MapGotUser(Point coord, bool checkingInvisible, bool isInvisible)
        {
            if (!_userMap.TryGetValue(coord, out var users) || users.Count == 0)
                return false;

            if (!checkingInvisible) return true;

            lock (_userMapLock)
            {
                for (int i = 0; i < users.Count; i++)
                {
                    var u = users[i];
                    if (u != null && !u.IsBot && IsUserVisible(u, isInvisible))
                        return true;
                }
            }
            return false;
        }

        private static bool IsUserVisible(RoomUser user, bool isInvisible)
        {
            if (user.IsBot)
            {
                var botRp = user.GetBotRoleplay();
                return botRp != null && !botRp.Invisible;
            }
            var rp = user.GetClient()?.GetRoleplay();
            return rp != null && (!rp.Invisible || isInvisible);
        }

        public List<RoomUser> GetRoomUsers(Point coord)
        {
            if (_userMap.TryGetValue(coord, out var users))
            {
                lock (_userMapLock)
                {
                    return new List<RoomUser>(users);
                }
            }
            return new List<RoomUser>();
        }

        #endregion

        #region Teleportation

        public void TeleportToSquare(RoomUser user, Point point)
        {
            if (user == null || !ValidTile(point.X, point.Y)) return;

            UpdateUserStateAndPosition(user, point, GetHeightForSquare(point));
            UpdateUserOrientation(user, point);
            ResetUserMovement(user);
        }

        public void TeleportToItem(RoomUser user, Item item)
        {
            if (user == null || item == null) return;

            var point = new Point(item.GetX, item.GetY);
            UpdateUserStateAndPosition(user, point, item.GetZ);
            user.RotBody = item.Rotation;
            user.RotHead = item.Rotation;
            ResetUserMovement(user);
        }

        private void UpdateUserStateAndPosition(RoomUser user, Point newPoint, double newZ)
        {
            if (ValidTile(user.X, user.Y))
                GameMap[user.X, user.Y] = user.SqState;

            UpdateUserMovement(user.Coordinate, newPoint, user);

            user.X = newPoint.X;
            user.Y = newPoint.Y;
            user.Z = newZ;

            user.SqState = GameMap[newPoint.X, newPoint.Y];
            if (ValidTile(newPoint.X, newPoint.Y))
                GameMap[newPoint.X, newPoint.Y] = 1;
        }

        private void UpdateUserOrientation(RoomUser user, Point point)
        {
            if (GetHighestItemForSquare(point, out Item item))
            {
                user.RotBody = item.Rotation;
                user.RotHead = item.Rotation;
            }
        }

        private static void ResetUserMovement(RoomUser user)
        {
            user.GoalX = user.X;
            user.GoalY = user.Y;
            user.SetStep = false;
            user.IsWalking = false;
            user.UpdateNeeded = true;
        }

        #endregion

        #region Map Generation

        public void GenerateMaps(bool checkLines = true)
        {
            ClearMaps();

            if (checkLines && CheckAndExpandMapIfNeeded())
                return;

            InitializeBaseMap();
            ProcessAllItems();
            UpdateUserPositions();
            EnsureDoorAccessible();
        }

        private bool CheckAndExpandMapIfNeeded()
        {
            Item[] items = _room.GetRoomItemHandler().GetFloor.ToArray();
            int maxX = 0, maxY = 0;

            foreach (Item item in items)
            {
                if (item == null) continue;
                if (item.GetX > maxX) maxX = item.GetX;
                if (item.GetY > maxY) maxY = item.GetY;
            }

            if (maxY > Model.MapSizeY - 1 || maxX > Model.MapSizeX - 1)
            {
                Model.SetMapsize(
                    Math.Max(maxX + 7, Model.MapSizeX),
                    Math.Max(maxY + 7, Model.MapSizeY));
                GenerateMaps(false);
                return true;
            }

            return false;
        }

        private void ClearMaps()
        {
            int sizeX = Model.MapSizeX;
            int sizeY = Model.MapSizeY;

            GameMap = new byte[sizeX, sizeY];
            mUserOnMap = new byte[sizeX, sizeY];
            EffectMap = new byte[sizeX, sizeY];
            mSquareTaking = new byte[sizeX, sizeY];
            _itemHeightmap = new double[sizeX, sizeY];
        }

        private void InitializeBaseMap()
        {
            for (int y = 0; y < Model.MapSizeY; y++)
                for (int x = 0; x < Model.MapSizeX; x++)
                    SetDefaultValue(x, y);
        }

        private void ProcessAllItems()
        {
            foreach (Item item in _room.GetRoomItemHandler().GetFloor.ToArray())
            {
                if (item != null) AddItemToMap(item, true, true);
            }
        }

        private void UpdateUserPositions()
        {
            if (_room.RoomBlockingEnabled) return;

            foreach (RoomUser user in _room.GetRoomUserManager().GetUserList())
            {
                if (user != null) UpdateUserMapPosition(user);
            }
        }

        private void UpdateUserMapPosition(RoomUser user)
        {
            if (!ValidTile(user.X, user.Y)) return;

            user.SqState = GameMap[user.X, user.Y];
            GameMap[user.X, user.Y] = 0;
            mUserOnMap[user.X, user.Y] = 1;
        }

        private void EnsureDoorAccessible()
        {
            try
            {
                if (ValidTile(Model.DoorX, Model.DoorY))
                    GameMap[Model.DoorX, Model.DoorY] = 3;
            }
            catch { /* Ignorar errores de índice */ }
        }

        private void SetDefaultValue(int x, int y)
        {
            if (!ValidTile(x, y)) return;

            GameMap[x, y] = 0;
            EffectMap[x, y] = 0;
            _itemHeightmap[x, y] = 0.0;

            if (x == Model.DoorX && y == Model.DoorY)
                GameMap[x, y] = 3;
            else if (Model.SqState[x, y] == SquareState.OPEN)
                GameMap[x, y] = 1;
            else if (Model.SqState[x, y] == SquareState.SEAT)
                GameMap[x, y] = 2;
        }

        #endregion

        #region Item Management

        public void AddToMap(Item item) => AddItemToMap(item, true, true);

        public void UpdateMapForItem(Item item)
        {
            RemoveFromMap(item, false);
            AddToMap(item);
        }

        public bool AddItemToMap(Item item, bool handleGameItem = true, bool newItem = true)
        {
            if (item == null) return false;

            if (handleGameItem) HandleGameItemRegistration(item);

            if (item.GetBaseItem().Type != 's') return true;

            foreach (Point coord in item.GetCoords)
                AddCoordinatedItem(item, coord);

            if (!CheckMapBounds(item)) return false;

            return ConstructMapForAllCoordinates(item);
        }

        public bool AddItemToMap(Item item, bool newItem = true) =>
            AddItemToMap(item, true, newItem);

        private bool ConstructMapForAllCoordinates(Item item)
        {
            bool success = true;
            foreach (Point coord in item.GetCoords)
                if (!ConstructMapForItem(item, coord)) success = false;
            return success;
        }

        private bool CheckMapBounds(Item item)
        {
            bool needs = false;
            if (item.GetX > Model.MapSizeX - 1) { Model.AddX(); needs = true; }
            if (item.GetY > Model.MapSizeY - 1) { Model.AddY(); needs = true; }

            if (needs) { GenerateMaps(false); return false; }
            return true;
        }

        private void HandleGameItemRegistration(Item item)
        {
            AddSpecialItems(item);

            // ✅ FIX #15 aplicado: usa el campo estático readonly en vez de new Dictionary<>
            if (_teamMap.TryGetValue(item.GetBaseItem().InteractionType, out TEAM team))
            {
                if (!_room.GetRoomItemHandler().GetFloor.Contains(item))
                    _room.GetGameManager().AddFurnitureToTeam(item, team);
            }
            else if (item.GetBaseItem().InteractionType == InteractionType.freezeexit)
            {
                _room.GetFreeze().AddExitTile(item);
            }
            else if (item.GetBaseItem().InteractionType == InteractionType.ROLLER)
            {
                if (!_room.GetRoomItemHandler().GetRollers().Contains(item))
                    _room.GetRoomItemHandler().TryAddRoller(item.Id, item);
            }
        }

        private void AddSpecialItems(Item item)
        {
            switch (item.GetBaseItem().InteractionType)
            {
                case InteractionType.FOOTBALL_GATE:
                    _room.GetSoccer().RegisterGate(item);
                    InitializeGateFigure(item);
                    break;
                case InteractionType.banzaifloor:
                    _room.GetBanzai().AddTile(item, item.Id);
                    break;
                case InteractionType.banzaipyramid:
                    _room.GetGameItemHandler().AddPyramid(item, item.Id);
                    break;
                case InteractionType.banzaitele:
                    _room.GetGameItemHandler().AddTeleport(item, item.Id);
                    item.ExtraData = "";
                    break;
                case InteractionType.banzaipuck:
                    _room.GetBanzai().AddPuck(item);
                    break;
                case InteractionType.FOOTBALL:
                    _room.GetSoccer().AddBall(item);
                    break;
                case InteractionType.FREEZE_TILE_BLOCK:
                    _room.GetFreeze().AddFreezeBlock(item);
                    break;
                case InteractionType.FREEZE_TILE:
                    _room.GetFreeze().AddFreezeTile(item);
                    break;
                case InteractionType.freezeexit:
                    _room.GetFreeze().AddExitTile(item);
                    break;
            }
        }

        private static void InitializeGateFigure(Item gate)
        {
            if (string.IsNullOrEmpty(gate.ExtraData))
            {
                gate.Gender = "M";
                gate.Figure = GetDefaultFigureForTeam(gate.team);
            }
            else
            {
                var parts = gate.ExtraData.Split(':');
                if (parts.Length >= 2)
                {
                    gate.Gender = parts[0];
                    gate.Figure = parts[1];
                }
            }
        }

        private static string GetDefaultFigureForTeam(TEAM team) => team switch
        {
            TEAM.YELLOW => "lg-275-93.hr-115-61.hd-207-14.ch-265-93.sh-305-62",
            TEAM.RED => "lg-275-96.hr-115-61.hd-180-3.ch-265-96.sh-305-62",
            TEAM.GREEN => "lg-275-102.hr-115-61.hd-180-3.ch-265-102.sh-305-62",
            TEAM.BLUE => "lg-275-108.hr-115-61.hd-180-3.ch-265-108.sh-305-62",
            _ => string.Empty
        };

        private bool ConstructMapForItem(Item item, Point coord)
        {
            try
            {
                if (!ValidTile(coord.X, coord.Y)) return false;

                if (Model.SqState[coord.X, coord.Y] == SquareState.BLOCKED)
                    Model.OpenSquare(coord.X, coord.Y, item.GetZ);

                if (_itemHeightmap[coord.X, coord.Y] <= item.TotalHeight)
                {
                    _itemHeightmap[coord.X, coord.Y] =
                        item.TotalHeight - _dynamicModel.SqFloorHeight[item.GetX, item.GetY];

                    UpdateEffectMap(item, coord);
                    UpdateGameMap(item, coord);
                }

                if (item.GetBaseItem().InteractionType == InteractionType.BED ||
                    item.GetBaseItem().InteractionType == InteractionType.TENT_SMALL)
                    GameMap[coord.X, coord.Y] = 3;

                return true;
            }
            catch (Exception ex)
            {
                Logging.HandleException(ex, "Gamemap.ConstructMapForItem");
                return false;
            }
        }

        private void UpdateEffectMap(Item item, Point coord)
        {
            EffectMap[coord.X, coord.Y] = item.GetBaseItem().InteractionType switch
            {
                InteractionType.POOL => 1,
                InteractionType.NORMAL_SKATES => 2,
                InteractionType.ICE_SKATES => 3,
                InteractionType.lowpool => 4,
                InteractionType.haloweenpool => 5,
                _ => 0
            };
        }

        private void UpdateGameMap(Item item, Point coord)
        {
            var baseItem = item.GetBaseItem();

            if (baseItem.Walkable || IsOpenGate(item))
            {
                if (GameMap[coord.X, coord.Y] != 3)
                    GameMap[coord.X, coord.Y] = 1;
            }
            else if (baseItem.IsSeat ||
                     baseItem.InteractionType == InteractionType.BED ||
                     baseItem.InteractionType == InteractionType.TENT_SMALL)
            {
                GameMap[coord.X, coord.Y] = 3;
            }
            else
            {
                // Un ítem no-caminable solo marca como bloqueado (0) si no hay
                // una silla (3) o asiento del modelo (2) debajo.
                byte current = GameMap[coord.X, coord.Y];
                if (current != 3 && current != 2)
                    GameMap[coord.X, coord.Y] = 0;
            }
        }

        private bool IsOpenGate(Item item) =>
            item.GetZ <= Model.SqFloorHeight[item.GetX, item.GetY] + 0.1 &&
            item.GetBaseItem().InteractionType == InteractionType.GATE &&
            item.ExtraData == "1";

        public bool RemoveFromMap(Item item, bool handleGameItem)
        {
            if (item == null) return false;

            if (handleGameItem) RemoveSpecialItem(item);

            bool isRemoved = false;
            foreach (Point coord in item.GetCoords)
                if (RemoveCoordinatedItem(item, coord)) isRemoved = true;

            // ✅ FIX #18: Antes usaba ConcurrentDictionary<Point, List<Item>> como
            //   estructura temporal, con ContainsKey redundante en el loop de escritura
            //   y en el loop de lectura (for each Key + ContainsKey(key) siempre true).
            //   Reemplazado por Dictionary<Point, List<Item>> local — no hay concurrencia
            //   aquí porque es una operación de regeneración que ocurre de forma serializada.
            var affectedCoords = new Dictionary<Point, List<Item>>();
            foreach (Point tile in item.GetCoords)
            {
                SetDefaultValue(tile.X, tile.Y);

                if (_coordinatedItems.TryGetValue(tile, out var ids))
                    affectedCoords[tile] = GetItemsFromIds(ids);
            }

            foreach (var (coord, subItems) in affectedCoords)
                foreach (Item subItem in subItems)
                    ConstructMapForItem(subItem, coord);

            return isRemoved;
        }

        public bool RemoveFromMap(Item item) => RemoveFromMap(item, true);

        private void RemoveSpecialItem(Item item)
        {
            switch (item.GetBaseItem().InteractionType)
            {
                case InteractionType.FOOTBALL_GATE: _room.GetSoccer().UnRegisterGate(item); break;
                case InteractionType.banzaifloor: _room.GetBanzai().RemoveTile(item.Id); break;
                case InteractionType.banzaipuck: _room.GetBanzai().RemovePuck(item.Id); break;
                case InteractionType.banzaipyramid: _room.GetGameItemHandler().RemovePyramid(item.Id); break;
                case InteractionType.banzaitele: _room.GetGameItemHandler().RemoveTeleport(item.Id); break;
                case InteractionType.FOOTBALL: _room.GetSoccer().RemoveBall(item.Id); break;
                case InteractionType.FREEZE_TILE: _room.GetFreeze().RemoveFreezeTile(item.Id); break;
                case InteractionType.FREEZE_TILE_BLOCK: _room.GetFreeze().RemoveFreezeBlock(item.Id); break;
                case InteractionType.freezeexit: _room.GetFreeze().RemoveExitTile(item.Id); break;
            }
        }

        #endregion

        #region Coordinated Items Management

        public void AddCoordinatedItem(Item item, Point coord)
        {
            // ✅ FIX #16 aplicado: lock protege la lista interna de _coordinatedItems
            lock (_coordItemLock)
            {
                _coordinatedItems.AddOrUpdate(coord,
                    _ => new List<int> { item.Id },
                    (_, list) =>
                    {
                        if (!list.Contains(item.Id)) list.Add(item.Id);
                        return list;
                    });
            }
        }

        public List<Item> GetCoordinatedItems(Point coord)
        {
            return _coordinatedItems.TryGetValue(coord, out var itemIds)
                ? GetItemsFromIds(itemIds)
                : new List<Item>();
        }

        public bool RemoveCoordinatedItem(Item item, Point coord)
        {
            lock (_coordItemLock)
            {
                if (!_coordinatedItems.TryGetValue(coord, out var itemIds))
                    return false;

                bool removed = itemIds.Remove(item.Id);

                if (itemIds.Count == 0)
                    _coordinatedItems.TryRemove(coord, out _);

                return removed;
            }
        }

        public List<Item> GetItemsFromIds(List<int> input)
        {
            if (input == null || input.Count == 0) return new List<Item>();

            // ✅ FIX #19: Antes: input.Distinct() + items.Contains(item) — O(n²).
            //   Usando HashSet<int> para deduplicar ids en O(1), y no hace falta
            //   items.Contains(item) si ya los ids son únicos.
            var seen = new HashSet<int>(input.Count);
            var items = new List<Item>(input.Count);

            try
            {
                foreach (int id in input)
                {
                    if (!seen.Add(id)) continue;
                    Item? item = _room.GetRoomItemHandler().GetItem(id);
                    if (item != null) items.Add(item);
                }
            }
            catch (Exception e)
            {
                Logging.LogCriticalException("Error in GetItemsFromIds: " + e);
            }

            return items;
        }

        #endregion

        #region Tile and Movement Validation

        public bool ValidTile(int x, int y) =>
            x >= 0 && y >= 0 && x < Model.MapSizeX && y < Model.MapSizeY;

        public bool CanWalk(int x, int y, bool @override = false)
        {
            if (!ValidTile(x, y)) return false;
            return @override || mUserOnMap[x, y] == 0;
        }

        public bool SquareHasUsers(int x, int y)
        {
            if (!ValidTile(x, y) || mUserOnMap[x, y] == 0) return false;
            return MapGotUser(new Point(x, y));
        }

        public bool SquareHasUsers(int x, int y, bool checkingInvisible = false, bool isInvisible = false) =>
            MapGotUser(new Point(x, y), checkingInvisible, isInvisible);

        public bool ItemCanBePlacedHere(int x, int y)
        {
            if (_dynamicModel.MapSizeX - 1 < x || _dynamicModel.MapSizeY - 1 < y ||
                (x == _dynamicModel.DoorX && y == _dynamicModel.DoorY))
                return false;
            return GameMap[x, y] == 1;
        }

        public bool SquareIsOpen(int x, int y, bool pOverride)
        {
            if (_dynamicModel.MapSizeX - 1 < x || _dynamicModel.MapSizeY - 1 < y) return false;
            return CanWalk(GameMap[x, y], pOverride);
        }

        public bool ItemCanMove(Item item, Point moveTo)
        {
            List<ThreeDCoord> points = Gamemap.GetAffectedTiles(
                item.GetBaseItem().Length,
                item.GetBaseItem().Width,
                moveTo.X, moveTo.Y,
                item.Rotation).Values.ToList();

            if (points.Count == 0) return true;

            foreach (ThreeDCoord coord in points)
            {
                if (coord.X >= Model.MapSizeX || coord.Y >= Model.MapSizeY) return false;
                if (!SquareIsOpen(coord.X, coord.Y, false)) return false;
            }
            return true;
        }

        public bool IsValidStep(RoomUser user, Vector2D from, Vector2D to, bool endOfPath, bool @override,
            bool roller = false, bool isInvisible = false, bool diagMove = false)
        {
            if (!ValidTile(to.X, to.Y)) return false;
            if (@override) return true;

            // Bloqueo por usuarios
            if (!_room.RoomBlockingEnabled && SquareHasUsers(to.X, to.Y, true, isInvisible))
            {
                // Solo permitimos el paso si es el mismo usuario (evita auto-bloqueo al clicar tu sitio)
                var usersOnTile = GetRoomUsers(new Point(to.X, to.Y));
                if (!usersOnTile.Any(u => u != null && u.VirtualId == user.VirtualId))
                {
                    // Bots respetan usuarios si es el destino final o si el usuario no es él mismo
                    return false;
                }
            }

            List<Item> items = GetAllRoomItemForSquare(to.X, to.Y);

            Item? gate = items.FirstOrDefault(x => x?.GetBaseItem().InteractionType == InteractionType.GUILD_GATE);
            if (gate != null)
            {
                if (user.IsBot)
                {
                    OpenGate(gate);
                    return true;
                }
                return HandleGroupGateAccess(user, gate);
            }

            if (items.Count > 0 && HasSpecialItemsBlockingMovement(items, new Point(to.X, to.Y), endOfPath))
                return false;

            bool isChair = false;
            double highestZ = -1;
            foreach (Item item in items)
            {
                if (item == null) continue;
                if (item.GetZ > highestZ)
                {
                    highestZ = item.GetZ;
                    isChair = item.GetBaseItem().IsSeat;
                }
            }

            byte tileState = GameMap[to.X, to.Y];
            // 0 = BLOQUEADO, 1 = ABIERTO, 2 = ASIENTO MODELO, 3 = ASIENTO ÍTEM / CAMA
            if (tileState == 0) return false;

            // Siempre permitir el destino final si hay un asiento o cama
            if (endOfPath && (tileState == 2 || tileState == 3)) return true;

            // Bloquear paso si es un asiento del modelo (estado 2)
            if (tileState == 2) return false;

            // Bloquear paso si es asiento de ítem (estado 3) PERO el ítem más alto no es el asiento (ej: mesa encima)
            if (tileState == 3 && !isChair) return false;

            if (!roller && GetHeightDifference(from, to) > 1.5) return false;
            if (diagMove && !IsValidDiagonalMove(from, to)) return false;

            // Colisión final con otros usuarios (no con uno mismo)
            if (endOfPath)
            {
                var other = _room.GetRoomUserManager().GetUserForSquare(to.X, to.Y);
                if (other != null && other.VirtualId != user.VirtualId && !other.IsWalking) return false;
            }

            return true;
        }

        public double GetHeightDifference(Vector2D from, Vector2D to) =>
            SqAbsoluteHeight(to.X, to.Y) - SqAbsoluteHeight(from.X, from.Y);

        private bool IsValidDiagonalMove(Vector2D from, Vector2D to)
        {
            int dx = to.X - from.X;
            int dy = to.Y - from.Y;

            if (dx == 0 || dy == 0) return true; // No es diagonal

            // En Arcturus, el movimiento diagonal es más permisivo (smooth).
            // Se permite si al menos uno de los dos tiles adyacentes es caminable.
            // Esto evita que el usuario se "trabe" al rodear esquinas.
            bool adj1 = ValidTile(from.X + dx, from.Y) && GameMap[from.X + dx, from.Y] != 0;
            bool adj2 = ValidTile(from.X, from.Y + dy) && GameMap[from.X, from.Y + dy] != 0;

            return adj1 || adj2;
        }

        private bool HandleGroupGateAccess(RoomUser user, Item gate)
        {
            if (user.IsBot) { OpenGate(gate); return true; }

            Group? group = gate.GroupId < 1000
                ? GroupManager.GetJob(gate.GroupId)
                : GroupManager.GetGang(gate.GroupId);

            if (group == null || user.GetClient()?.GetHabbo() == null)
                return false;

            if (gate.GroupId < 1000)
            {
                GroupRank? rank = GroupManager.GetJobRank(group.Id, 1);
                if (rank?.HasCommand("arrest") == true &&
                    user.GetClient().GetRoleplay()?.PoliceTrial == true)
                {
                    OpenGate(gate);
                    return true;
                }
            }

            // ✅ FIX #21: Operador precedencia bug en original:
            //   a && b || c && d && e
            //   se parseaba como (a && b) || c || (d && e)
            //   en vez de la semántica obvia de tres condiciones independientes.
            //   Paréntesis explícitos para hacer la intención inequívoca.
            var rp = user.GetClient().GetRoleplay();
            var habbo = user.GetClient().GetHabbo();
            bool hasAccess =
                (group.IsMember(habbo.Id) && rp?.IsWorking == true) ||
                habbo.GetPermissions().HasRight("corporation_rights") ||
                (GroupManager.HasJobCommand(user.GetClient(), "guide") && rp?.IsWorking == true);

            if (hasAccess) { OpenGate(gate); return true; }

            user.Path?.Clear();
            user.PathRecalcNeeded = false;
            return false;
        }

        private static void OpenGate(Item gate)
        {
            gate.ExtraData = "1";
            gate.UpdateState(false, true);
            gate.RequestUpdate(4, true);
        }

        private bool HasSpecialItemsBlockingMovement(List<Item> items, Point to, bool endOfPath)
        {
            if (items.Any(i => i?.GetBaseItem().InteractionType == InteractionType.GUILD_GATE ||
                               i?.GetBaseItem().InteractionType == InteractionType.SLIDING_DOORS))
                return true;

            var bed = items.FirstOrDefault(i => i?.GetBaseItem().IsBed() == true);
            if (bed != null)
            {
                List<Point> bedTiles = bed.GetBedTiles(new Point(to.X, to.Y), out _);
                if (bedTiles.Any(p => SquareHasUsers(p.X, p.Y))) return true;
                if (!endOfPath) return true;
            }

            return false;
        }

        private static bool IsTileWalkable(byte tileState, bool endOfPath)
        {
            if (tileState == 0) return false;
            if (tileState == 2 && !endOfPath) return false;
            if (tileState == 3 && !endOfPath) return false;
            return true;
        }

        private double GetHeightDifference(Point from, Point to) =>
            SqAbsoluteHeight(to.X, to.Y) - SqAbsoluteHeight(from.X, from.Y);

        private bool IsValidDiagonalMove(Point from, Point to)
        {
            int dx = to.X - from.X;
            int dy = to.Y - from.Y;

            return (dx, dy) switch
            {
                (-1, -1) => GameMap[to.X + 1, to.Y] == 1 || GameMap[to.X, to.Y + 1] == 1,
                (1, -1) => GameMap[to.X - 1, to.Y] == 1 || GameMap[to.X, to.Y + 1] == 1,
                (1, 1) => GameMap[to.X - 1, to.Y] == 1 || GameMap[to.X, to.Y - 1] == 1,
                (-1, 1) => GameMap[to.X + 1, to.Y] == 1 || GameMap[to.X, to.Y - 1] == 1,
                _ => true
            };
        }

        public static bool CanWalk(byte state, bool @override) =>
            @override || state == 1 || state == 3;

        #endregion

        #region Height and Item Retrieval

        public double SqAbsoluteHeight(int x, int y)
        {
            if (_coordinatedItems.TryGetValue(new Point(x, y), out var itemIds))
                return SqAbsoluteHeight(x, y, GetItemsFromIds(itemIds));

            return _dynamicModel.SqFloorHeight[x, y];
        }

        public double SqAbsoluteHeight(int x, int y, List<Item> itemsOnSquare)
        {
            try
            {
                double highestStack = 0;
                double deductable = 0;
                bool deduct = false;
                bool hasSeat = false;

                if (itemsOnSquare != null)
                {
                    foreach (Item item in itemsOnSquare)
                    {
                        if (item == null) continue;

                        // Registrar si hay algún seat en el tile
                        bool isBedSeat = item.GetBaseItem().IsSeat ||
                                         item.GetBaseItem().InteractionType == InteractionType.BED ||
                                         item.GetBaseItem().InteractionType == InteractionType.TENT_SMALL;

                        if (isBedSeat) hasSeat = true;

                        if (item.TotalHeight <= highestStack) continue;

                        highestStack = item.TotalHeight;

                        if (isBedSeat)
                        {
                            deduct = true;
                            deductable = item.GetBaseItem().Height;
                        }
                        else
                        {
                            deduct = false;
                        }
                    }

                    // Si el ítem más alto NO es seat pero hay un seat debajo,
                    // el usuario se sienta en el seat y no debe subir por el cojín
                    if (!deduct && hasSeat)
                    {
                        // Buscar el seat más alto para deducir su altura
                        foreach (Item item in itemsOnSquare)
                        {
                            if (item == null) continue;
                            bool isBedSeat = item.GetBaseItem().IsSeat ||
                                             item.GetBaseItem().InteractionType == InteractionType.BED ||
                                             item.GetBaseItem().InteractionType == InteractionType.TENT_SMALL;
                            if (isBedSeat)
                            {
                                deduct = true;
                                deductable = item.GetBaseItem().Height;
                                highestStack = item.TotalHeight; // usar la altura del seat, no del cojín
                                break;
                            }
                        }
                    }
                }

                double floor = Model.SqFloorHeight[x, y];
                double stack = highestStack - floor;
                if (deduct) stack -= deductable;
                if (stack < 0) stack = 0;
                return floor + stack;
            }
            catch (Exception e)
            {
                Logging.HandleException(e, "Gamemap.SqAbsoluteHeight");
                return 0;
            }
        }

        public bool GetHighestItemForSquare(Point square, out Item item)
        {
            item = null;
            List<Item> items = GetAllRoomItemForSquare(square.X, square.Y);
            if (items.Count == 0) return false;

            double highestZ = -1;
            foreach (Item u in items)
            {
                if (u == null) continue;
                if (u.TotalHeight > highestZ) { highestZ = u.TotalHeight; item = u; }
            }
            return item != null;
        }

        public double GetHeightForSquare(Point coord)
        {
            if (GetHighestItemForSquare(coord, out Item rItem) && rItem != null)
                return rItem.TotalHeight;
            return 0.0;
        }

        public List<Item> GetAllRoomItemForSquare(int pX, int pY)
        {
            return _coordinatedItems.TryGetValue(new Point(pX, pY), out var ids)
                ? GetItemsFromIds(ids)
                : new List<Item>();
        }

        public List<Item> GetRoomItemForSquare(int pX, int pY, double minZ)
        {
            var result = new List<Item>();
            if (!_coordinatedItems.TryGetValue(new Point(pX, pY), out var ids)) return result;

            foreach (Item item in GetItemsFromIds(ids))
                if (item.GetZ > minZ && item.GetX == pX && item.GetY == pY)
                    result.Add(item);

            return result;
        }

        public List<Item> GetRoomItemForSquare(int pX, int pY)
        {
            var result = new List<Item>();
            if (!_coordinatedItems.TryGetValue(new Point(pX, pY), out var ids)) return result;

            foreach (Item item in GetItemsFromIds(ids))
                if (item.Coordinate.X == pX && item.Coordinate.Y == pY)
                    result.Add(item);

            return result;
        }

        #endregion

        #region Utility Methods

        public Point GetRandomWalkableSquare()
        {
            try
            {
                // ✅ FIX #22: Antes: GetWalkableSquares().ToList() + redundant null check
                //   (ToList() nunca devuelve null) + validación de índice redundante.
                //   Simplificado: el único caso borde real es lista vacía.
                var squares = GetWalkableSquares()
                    .Where(p => p.X != StaticModel.DoorX || p.Y != StaticModel.DoorY)
                    .ToList();

                if (squares.Count == 0) return new Point(0, 0);

                return squares[PolarEnvironment.GetRandomNumber(0, squares.Count - 1)];
            }
            catch
            {
                return new Point(0, 0);
            }
        }

        private IEnumerable<Point> GetWalkableSquares()
        {
            for (int y = 0; y < GameMap.GetLength(1); y++)
                for (int x = 0; x < GameMap.GetLength(0); x++)
                    if (GameMap[x, y] == 1)
                        yield return new Point(x, y);
        }

        public Point GetRandomWalkableSquare(int x, int y)
        {
            int rx = PolarEnvironment.GetRandomNumber(x - 5, x + 5);
            int ry = PolarEnvironment.GetRandomNumber(y - 5, y + 5);

            if (Model.DoorX == rx || Model.DoorY == ry || !CanWalk(rx, ry))
                return new Point(x, y);

            return new Point(rx, ry);
        }

        public bool IsInMap(int x, int y)
        {
            // ✅ FIX #23: Antes llamaba a GetWalkableSquares().ToList() completo para
            //   verificar si UN punto es caminable — O(n) scan + allocación de lista completa
            //   por cada comprobación. Para una sala de 64×64 = 4096 iteraciones por llamada.
            //   Ahora: comprobación directa O(1) en los arrays ya calculados.
            if (!ValidTile(x, y)) return false;
            if (x == StaticModel.DoorX && y == StaticModel.DoorY) return false;
            return GameMap[x, y] == 1;
        }

        public static Dictionary<int, ThreeDCoord> GetAffectedTiles(
            int length, int width, int posX, int posY, int rotation)
        {
            // ✅ FIX #24: Antes: PointList.Values.Contains(coord) en cada iteración.
            //   Dictionary.Values es una colección sin índice — .Contains() es O(n).
            //   Con un ítem de 4×4 esto es hasta 16 * 16 = 256 comparaciones O(n²).
            //   Reemplazado por HashSet<ThreeDCoord> como lookup set auxiliar — O(1) add/contains.
            var pointList = new Dictionary<int, ThreeDCoord>();
            var seen = new HashSet<ThreeDCoord>();
            int idx = 0;

            void TryAdd(ThreeDCoord c)
            {
                if (seen.Add(c))
                    pointList[idx++] = c;
            }

            if (length > 1)
            {
                if (rotation == 0 || rotation == 4)
                {
                    for (int i = 1; i < length; i++)
                    {
                        TryAdd(new ThreeDCoord(posX, posY + i, i));
                        for (int j = 1; j < width; j++)
                            TryAdd(new ThreeDCoord(posX + j, posY + i, Math.Max(i, j)));
                    }
                }
                else if (rotation == 2 || rotation == 6)
                {
                    for (int i = 1; i < length; i++)
                    {
                        TryAdd(new ThreeDCoord(posX + i, posY, i));
                        for (int j = 1; j < width; j++)
                            TryAdd(new ThreeDCoord(posX + i, posY + j, Math.Max(i, j)));
                    }
                }
            }

            if (width > 1)
            {
                if (rotation == 0 || rotation == 4)
                {
                    for (int i = 1; i < width; i++)
                    {
                        TryAdd(new ThreeDCoord(posX + i, posY, i));
                        for (int j = 1; j < length; j++)
                            TryAdd(new ThreeDCoord(posX + i, posY + j, Math.Max(i, j)));
                    }
                }
                else if (rotation == 2 || rotation == 6)
                {
                    for (int i = 1; i < width; i++)
                    {
                        TryAdd(new ThreeDCoord(posX, posY + i, i));
                        for (int j = 1; j < length; j++)
                            TryAdd(new ThreeDCoord(posX + j, posY + i, Math.Max(i, j)));
                    }
                }
            }

            TryAdd(new ThreeDCoord(posX, posY, 0));
            return pointList;
        }

        public Point GetChaseMovement(Item item)
        {
            int distance = 99;
            Point coord = new Point(0, 0);
            int iX = item.GetX;
            int iY = item.GetY;
            bool isHorizontal = false;

            foreach (RoomUser user in _room.GetRoomUserManager().GetRoomUsers())
            {
                if (user.X == item.GetX)
                {
                    int diff = Math.Abs(user.Y - item.GetY);
                    if (diff < distance)
                    {
                        distance = diff;
                        coord = user.Coordinate;
                        isHorizontal = false;
                    }
                }
                else if (user.Y == item.GetY)
                {
                    int diff = Math.Abs(user.X - item.GetX);
                    if (diff < distance)
                    {
                        distance = diff;
                        coord = user.Coordinate;
                        isHorizontal = true;
                    }
                }
            }

            // ✅ FIX #25: Antes: OrderBy(x => Guid.NewGuid()) para shuffle aleatorio.
            //   Crear un Guid por elemento es extremadamente caro (crypto RNG).
            //   Reemplazado por selección directa de un lado aleatorio.
            if (distance > 5)
            {
                var sides = item.GetSides();
                return sides.Count == 0
                    ? item.Coordinate
                    : sides[PolarEnvironment.GetRandomNumber(0, sides.Count - 1)];
            }

            if (isHorizontal) return new Point(iX > coord.X ? iX - 1 : iX + 1, iY);
            if (distance < 99) return new Point(iX, iY > coord.Y ? iY - 1 : iY + 1);

            return item.Coordinate;
        }

        public RoomUser? SquareHasUserNear(int x, int y, int distance = 0)
        {
            if (SquareHasUsers(x - 1, y)) return _room.GetRoomUserManager().GetUserForSquare(x - 1, y);
            if (SquareHasUsers(x + 1, y)) return _room.GetRoomUserManager().GetUserForSquare(x + 1, y);
            if (SquareHasUsers(x, y - 1)) return _room.GetRoomUserManager().GetUserForSquare(x, y - 1);
            if (SquareHasUsers(x, y + 1)) return _room.GetRoomUserManager().GetUserForSquare(x, y + 1);
            return null;
        }

        public static bool TilesTouching(Point p1, Point p2) =>
            TilesTouching(p1.X, p1.Y, p2.X, p2.Y);

        public static bool TilesTouching(int x1, int y1, int x2, int y2) =>
            Math.Abs(x1 - x2) <= 1 && Math.Abs(y1 - y2) <= 1;

        public static int TileDistance(int x1, int y1, int x2, int y2) =>
            Math.Abs(x1 - x2) + Math.Abs(y1 - y2);

        public byte GetFloorStatus(Point coord)
        {
            if (coord.X > GameMap.GetUpperBound(0) || coord.Y > GameMap.GetUpperBound(1))
                return 1;
            return GameMap[coord.X, coord.Y];
        }

        public void SetFloorStatus(int x, int y, byte status)
        {
            if (ValidTile(x, y)) GameMap[x, y] = status;
        }

        public double GetHeightForSquareFromData(Point coord)
        {
            if (coord.X > _dynamicModel.SqFloorHeight.GetUpperBound(0) ||
                coord.Y > _dynamicModel.SqFloorHeight.GetUpperBound(1))
                return 1;
            return _dynamicModel.SqFloorHeight[coord.X, coord.Y];
        }

        public bool CanRollItemHere(int x, int y, HabboHotel.GameClients.GameClient session)
        {
            if (!ValidTile(x, y) || Model.SqState[x, y] == SquareState.BLOCKED) return false;
            return _room.CheckTerrain(session, x, y);
        }

        public bool CanRollItemHere(int x, int y) =>
            ValidTile(x, y) && Model.SqState[x, y] != SquareState.BLOCKED;

        #endregion

        #region Properties

        public DynamicRoomModel Model => _dynamicModel;
        public RoomModel StaticModel => _staticModel;

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _userMap?.Clear();
            _coordinatedItems?.Clear();
            _dynamicModel?.Destroy();

            GameMap = null;
            EffectMap = null;
            mUserOnMap = null;
            mSquareTaking = null;
            _itemHeightmap = null;
            _room = null;
        }

        #endregion
    }
}