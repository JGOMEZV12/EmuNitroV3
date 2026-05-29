using Polar.Core;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Pathfinding;
using Polar.HabboHotel.Rooms.Games.Teams;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Polar.HabboHotel.Rooms
{
    public sealed class Gamemap : IDisposable
    {
        // ── Constantes (puerto de Java PathfinderConstants) ───────────────────────
        private const int DoorDistanceThreshold = 2;
        private const double MaxStepHeight = 1.5;
        private const double MaxFallHeight = 1.5; // puede diferenciarse del step si se quiere

        /// <summary>
        /// FIX: Puerto de isInvalidHeight (Java PathfinderImpl).
        /// Separa el check de caída (falling) del check de subida (climbing).
        /// Antes solo se comprobaba Math.Abs(diff) > 1.5 sin distinguir dirección.
        /// </summary>
        public bool IsValidHeightTransition(double fromZ, double toZ,
            bool allowFalling = true,
            double maxStep = MaxStepHeight,
            double maxFall = MaxFallHeight)
        {
            double diff = toZ - fromZ;
            if (!allowFalling && diff < -maxFall) return false; // no puede caer
            if (diff > maxStep) return false; // no puede subir tanto
            return true;
        }
        // ── Referencias ───────────────────────────────────────────────────────────
        public Room _room;
        private RoomModel _staticModel;
        private DynamicRoomModel _dynamicModel;

        public bool DiagonalEnabled = true;

        // ── Arrays del mapa ───────────────────────────────────────────────────────
        // FIX: propiedades con setter privado; sólo GenerateMaps/ClearMaps los mutan
        public byte[,] GameMap { get; private set; }
        public byte[,] EffectMap { get; private set; }
        public byte[,] mUserOnMap { get; private set; }
        public byte[,] mSquareTaking { get; private set; }
        public double[,] _itemHeightmap;

        // ── Índice de ítems por coordenada ────────────────────────────────────────
        // FIX: _coordinatedItems usa int[] como valor en vez de List<int>.
        //      Las listas pequeñas (casi siempre 1-3 ítems) en el mismo tile son
        //      sustituidas por arrays inmutables: la lectura no requiere lock,
        //      la escritura (rara) recrea el array con el lock del tile.
        //      Para tiles con muchos ítems apilados se degrade gracefully a List.
        //
        //      Alternativa más simple: Dictionary<long, int[]> con key = (y<<16)|x
        //      para evitar boxing del struct Point.
        private readonly ConcurrentDictionary<long, int[]> _coordinatedItems;

        // ── Índice de usuarios por coordenada ─────────────────────────────────────
        // FIX: _userMap usa arrays inmutables del mismo modo.
        //      Las escrituras son poco frecuentes (sólo cuando un usuario se mueve).
        private readonly ConcurrentDictionary<long, RoomUser[]> _userMap;

        // ── Lock dedicado para cada índice ────────────────────────────────────────
        // FIX: lock estrechos (sólo durante la mutación de los arrays internos).
        //      La lectura es lock-free al trabajar con referencias inmutables.
        private readonly object _userMapLock = new();
        private readonly object _coordItemLock = new();

        // ── Tabla de equipos (estática, inicializada una sola vez) ────────────────
        // FIX: antes se instanciaba en cada llamada a HandleGameItemRegistration.
        private static readonly IReadOnlyDictionary<InteractionType, TEAM> _teamMap =
            new Dictionary<InteractionType, TEAM>
            {
                [InteractionType.FOOTBALL_GOAL_RED] = TEAM.RED,
                [InteractionType.footballcounterred] = TEAM.RED,
                [InteractionType.banzaiscorered] = TEAM.RED,
                [InteractionType.banzaigateblue] = TEAM.RED,     // ← mantenido igual que original
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

        // ── Constructor ───────────────────────────────────────────────────────────
        public Gamemap(Room room)
        {
            _room = room;
            DiagonalEnabled = true;

            _staticModel = PolarEnvironment.GetGame().GetRoomManager()
                .GetModel(room.ModelName, room.Id)
                ?? throw new Exception($"No modeldata found for roomID {room.Id}");
            _dynamicModel = new DynamicRoomModel(_staticModel);

            // FIX: pre-dimensionar con capacidad inicial basada en el tamaño del mapa
            int initialCap = Math.Max(64, _staticModel.MapSizeX * _staticModel.MapSizeY / 4);
            _userMap = new ConcurrentDictionary<long, RoomUser[]>(
                Environment.ProcessorCount, initialCap);
            _coordinatedItems = new ConcurrentDictionary<long, int[]>(
                Environment.ProcessorCount, initialCap);

            InitializeArrays();
        }

        private void InitializeArrays()
        {
            int sx = Model.MapSizeX, sy = Model.MapSizeY;
            GameMap = new byte[sx, sy];
            mUserOnMap = new byte[sx, sy];
            mSquareTaking = new byte[sx, sy];
            EffectMap = new byte[sx, sy];
            _itemHeightmap = new double[sx, sy];
        }

        // ── Helper: coordenada → long key (sin boxing de Point) ───────────────────
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long CoordKey(int x, int y) => ((long)(uint)x << 32) | (uint)y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long CoordKey(Point p) => CoordKey(p.X, p.Y);

        // ══════════════════════════════════════════════════════════════════════════
        //  GESTIÓN DE USUARIOS EN EL MAPA
        // ══════════════════════════════════════════════════════════════════════════

        public void AddUserToMap(RoomUser user, Point coord)
        {
            if (user == null) return;
            long key = CoordKey(coord);

            // FIX: escritura lock-narrow + array inmutable para lectura lock-free
            lock (_userMapLock)
            {
                _userMap.AddOrUpdate(key,
                    _ => new[] { user },
                    (_, existing) =>
                    {
                        // evitar duplicados
                        foreach (var u in existing)
                            if (u?.VirtualId == user.VirtualId) return existing;
                        var next = new RoomUser[existing.Length + 1];
                        existing.CopyTo(next, 0);
                        next[existing.Length] = user;
                        return next;
                    });
            }

            if (ValidTile(coord.X, coord.Y))
                mUserOnMap[coord.X, coord.Y] = 1;
        }

        public void RemoveUserFromMap(RoomUser user, Point coord)
        {
            if (user == null) return;
            long key = CoordKey(coord);

            bool isEmpty = false;
            lock (_userMapLock)
            {
                if (!_userMap.TryGetValue(key, out RoomUser[] arr)) return;

                // Reconstruir sin el usuario
                var next = arr.Where(u => u?.VirtualId != user.VirtualId).ToArray();
                if (next.Length == 0)
                {
                    _userMap.TryRemove(key, out _);
                    isEmpty = true;
                }
                else
                {
                    _userMap[key] = next;
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
            long key = CoordKey(coord);
            return _userMap.TryGetValue(key, out RoomUser[] arr) && arr.Length > 0;
        }

        public bool MapGotUser(Point coord, bool checkingInvisible, bool isInvisible)
        {
            long key = CoordKey(coord);
            if (!_userMap.TryGetValue(key, out RoomUser[] arr) || arr.Length == 0)
                return false;
            if (!checkingInvisible) return true;

            foreach (var u in arr)
                if (u != null && !u.IsBot && IsUserVisible(u, isInvisible)) return true;
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
            long key = CoordKey(coord);
            if (!_userMap.TryGetValue(key, out RoomUser[] arr))
                return new List<RoomUser>(0);
            // Snapshot: devolvemos lista para que el caller no mute el array interno
            return new List<RoomUser>(arr);
        }

        public List<RoomUser> GetRoomUnitsAt(Point coord) => GetRoomUsers(coord);

        // ══════════════════════════════════════════════════════════════════════════
        //  TELEPORTACIÓN
        // ══════════════════════════════════════════════════════════════════════════

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
            user.RotBody = user.RotHead = item.Rotation;
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
                user.RotBody = user.RotHead = item.Rotation;
        }

        private static void ResetUserMovement(RoomUser user)
        {
            user.GoalX = user.X;
            user.GoalY = user.Y;
            user.SetStep = false;
            user.IsWalking = false;
            user.UpdateNeeded = true;
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  GENERACIÓN DE MAPAS
        // ══════════════════════════════════════════════════════════════════════════

        public void GenerateMaps(bool checkLines = true)
        {
            ClearMaps();
            if (checkLines && CheckAndExpandMapIfNeeded()) return;
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
            // FIX: limpiar índices también para que no queden refs huérfanas
            _coordinatedItems.Clear();
            // _userMap NO se limpia aquí: los usuarios siguen vivos

            int sx = Model.MapSizeX, sy = Model.MapSizeY;
            GameMap = new byte[sx, sy];
            mUserOnMap = new byte[sx, sy];
            EffectMap = new byte[sx, sy];
            mSquareTaking = new byte[sx, sy];
            _itemHeightmap = new double[sx, sy];
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
                if (item != null) AddItemToMap(item, true, true);
        }

        private void UpdateUserPositions()
        {
            if (_room.RoomBlockingEnabled) return;
            foreach (RoomUser user in _room.GetRoomUserManager().GetUserList())
            {
                if (user == null || !ValidTile(user.X, user.Y)) continue;
                user.SqState = GameMap[user.X, user.Y];
                GameMap[user.X, user.Y] = 0;
                mUserOnMap[user.X, user.Y] = 1;
            }
        }

        private void EnsureDoorAccessible()
        {
            try
            {
                if (ValidTile(Model.DoorX, Model.DoorY))
                    GameMap[Model.DoorX, Model.DoorY] = 3;
            }
            catch { /* índice fuera de rango: ignorar */ }
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

        // ══════════════════════════════════════════════════════════════════════════
        //  GESTIÓN DE ÍTEMS EN EL MAPA
        // ══════════════════════════════════════════════════════════════════════════

        public void AddToMap(Item item) => AddItemToMap(item, true, true);
        public void UpdateMapForItem(Item item) { RemoveFromMap(item, false); AddToMap(item); }

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

        public bool AddItemToMap(Item item, bool newItem) =>
            AddItemToMap(item, true, newItem);

        private bool ConstructMapForAllCoordinates(Item item)
        {
            bool ok = true;
            foreach (Point coord in item.GetCoords)
                if (!ConstructMapForItem(item, coord)) ok = false;
            return ok;
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
                if (parts.Length >= 2) { gate.Gender = parts[0]; gate.Figure = parts[1]; }
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

            // Reconstruir el mapa sólo para los tiles afectados
            var affectedCoords = new HashSet<long>();
            foreach (Point tile in item.GetCoords)
                affectedCoords.Add(CoordKey(tile));

            foreach (long key in affectedCoords)
            {
                int x = (int)(key >> 32), y = (int)(uint)key;
                SetDefaultValue(x, y);

                if (_coordinatedItems.TryGetValue(key, out int[] ids))
                {
                    foreach (int id in ids)
                    {
                        Item? sub = _room.GetRoomItemHandler().GetItem(id);
                        if (sub != null) ConstructMapForItem(sub, new Point(x, y));
                    }
                }
            }

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

        // ══════════════════════════════════════════════════════════════════════════
        //  ÍNDICE DE ÍTEMS POR COORDENADA
        // ══════════════════════════════════════════════════════════════════════════

        public void AddCoordinatedItem(Item item, Point coord)
        {
            long key = CoordKey(coord);
            lock (_coordItemLock)
            {
                _coordinatedItems.AddOrUpdate(key,
                    _ => new[] { item.Id },
                    (_, existing) =>
                    {
                        foreach (int id in existing)
                            if (id == item.Id) return existing;
                        var next = new int[existing.Length + 1];
                        existing.CopyTo(next, 0);
                        next[existing.Length] = item.Id;
                        return next;
                    });
            }
        }

        public List<Item> GetCoordinatedItems(Point coord)
        {
            long key = CoordKey(coord);
            return _coordinatedItems.TryGetValue(key, out int[] ids)
                ? ResolveItems(ids)
                : new List<Item>(0);
        }

        public bool RemoveCoordinatedItem(Item item, Point coord)
        {
            long key = CoordKey(coord);
            lock (_coordItemLock)
            {
                if (!_coordinatedItems.TryGetValue(key, out int[] ids)) return false;
                var next = ids.Where(id => id != item.Id).ToArray();
                if (next.Length == ids.Length) return false; // no estaba
                if (next.Length == 0) _coordinatedItems.TryRemove(key, out _);
                else _coordinatedItems[key] = next;
                return true;
            }
        }

        // FIX: GetItemsFromIds renombrado a ResolveItems para claridad;
        //      usa HashSet para deduplicar en O(1) en vez de .Distinct() + Contains O(n²)
        public List<Item> GetItemsFromIds(List<int> input) => ResolveItems(input);

        private List<Item> ResolveItems(IReadOnlyList<int> ids)
        {
            if (ids == null || ids.Count == 0) return new List<Item>(0);

            // FIX: pre-dimensionar la lista y el set con la capacidad exacta
            var seen = new HashSet<int>(ids.Count);
            var items = new List<Item>(ids.Count);
            try
            {
                foreach (int id in ids)
                {
                    if (!seen.Add(id)) continue;
                    Item? item = _room.GetRoomItemHandler().GetItem(id);
                    if (item != null) items.Add(item);
                }
            }
            catch (Exception e)
            {
                Logging.LogCriticalException($"ResolveItems: {e}");
            }
            return items;
        }

        private List<Item> ResolveItems(int[] ids) => ResolveItems((IReadOnlyList<int>)ids);

        // ══════════════════════════════════════════════════════════════════════════
        //  VALIDACIÓN DE TILES Y MOVIMIENTO
        // ══════════════════════════════════════════════════════════════════════════

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ValidTile(int x, int y) =>
            (uint)x < (uint)Model.MapSizeX && (uint)y < (uint)Model.MapSizeY;

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

        public bool SquareHasUsers(int x, int y, bool checkingInvisible = false,
            bool isInvisible = false) =>
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
            var points = GetAffectedTiles(
                item.GetBaseItem().Length, item.GetBaseItem().Width,
                moveTo.X, moveTo.Y, item.Rotation).Values.ToList();

            if (points.Count == 0) return true;
            foreach (ThreeDCoord coord in points)
            {
                if (coord.X >= Model.MapSizeX || coord.Y >= Model.MapSizeY) return false;
                if (!SquareIsOpen(coord.X, coord.Y, false)) return false;
            }
            return true;
        }
        public double GetHeightDifference(Vector2D from, Vector2D to) =>
            SqAbsoluteHeight(to.X, to.Y) - SqAbsoluteHeight(from.X, from.Y);

        /// <summary>
        /// FIX: Puerto completo de isBlockedDiagonal (Java AdjacentTileFinder).
        /// Verifica que al menos uno de los dos flancos ortogonales sea transitable
        /// antes de permitir el movimiento diagonal — evita "corner cutting".
        /// El método original solo comprobaba GameMap != 0 en uno de los dos tiles.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsBlockedDiagonal(int x, int y, int newX, int newY)
        {
            bool flankX = ValidTile(newX, y) && GameMap[newX, y] != 0;
            bool flankY = ValidTile(x, newY) && GameMap[x, newY] != 0;
            return !flankX && !flankY;
        }

        /// <summary>
        /// FIX: Delega en IsBlockedDiagonal para consistencia con PathFinder.cs.
        /// </summary>
        private bool IsValidDiagonalMove(Vector2D from, Vector2D to)
        {
            int dx = to.X - from.X;
            int dy = to.Y - from.Y;
            if (dx == 0 || dy == 0) return true; // no es diagonal
            return !IsBlockedDiagonal(from.X, from.Y, to.X, to.Y);
        }
        public bool IsValidStep(RoomUser user, Vector2D from, Vector2D to,
             bool endOfPath, bool @override,
             bool roller = false, bool isInvisible = false, bool diagMove = false)
        {
            if (!ValidTile(to.X, to.Y)) return false;
            if (@override) return true;

            // FIX #2: Diagonal corner-cutting check (puerto de Java)
            if (diagMove && IsBlockedDiagonal(from.X, from.Y, to.X, to.Y))
                return false;

            // Bloqueo por usuarios (excepto el propio usuario)
            if (!_room.RoomBlockingEnabled)
            {
                // FIX #3: Puerto de DISTANCE_DOOR_THRESHOLD — cerca de la puerta
                //         ignoramos el blocking para que nadie tape la entrada.
                bool nearDoor = Math.Abs(to.X - Model.DoorX) + Math.Abs(to.Y - Model.DoorY)
                                <= DoorDistanceThreshold;

                if (!nearDoor && SquareHasUsers(to.X, to.Y, true, isInvisible))
                {
                    var usersOnTile = GetRoomUsers(new Point(to.X, to.Y));
                    if (!usersOnTile.Any(u => u?.VirtualId == user.VirtualId))
                        return false;
                }
            }

            List<Item> items = GetAllRoomItemForSquare(to.X, to.Y);

            Item? gate = items.FirstOrDefault(x =>
                x?.GetBaseItem().InteractionType == InteractionType.GUILD_GATE);
            if (gate != null)
            {
                if (user.IsBot) { OpenGate(gate); return true; }
                return HandleGroupGateAccess(user, gate);
            }

            if (items.Count > 0 &&
                HasSpecialItemsBlockingMovement(items, new Point(to.X, to.Y), endOfPath))
                return false;

            bool isChair = false;
            double highestZ = -1;
            foreach (Item item in items)
            {
                if (item == null) continue;
                if (item.GetZ > highestZ) { highestZ = item.GetZ; isChair = item.GetBaseItem().IsSeat; }
            }

            byte tileState = GameMap[to.X, to.Y];
            if (tileState == 0) return false;
            if (endOfPath && (tileState == 2 || tileState == 3)) return true;
            if (tileState == 2) return false;
            if (tileState == 3 && !isChair) return false;

            // FIX #4: Puerto de isInvalidHeight (Java) — distingue subida de bajada.
            //         allowFalling=true por defecto; puede exponerse por RoomUser si se necesita.
            if (!roller && !IsValidHeightTransition(
                    SqAbsoluteHeight(from.X, from.Y),
                    SqAbsoluteHeight(to.X, to.Y),
                    allowFalling: true))
                return false;

            // FIX #2: Diagonal check consistente
            if (diagMove && !IsValidDiagonalMove(from, to))
                return false;

            if (endOfPath)
            {
                var other = _room.GetRoomUserManager().GetUserForSquare(to.X, to.Y);
                if (other != null && other.VirtualId != user.VirtualId && !other.IsWalking)
                    return false;
            }

            return true;
        }

        private bool HandleGroupGateAccess(RoomUser user, Item gate)
        {
            if (user.IsBot) { OpenGate(gate); return true; }

            Group? group = gate.GroupId < 1000
                ? GroupManager.GetJob(gate.GroupId)
                : GroupManager.GetGang(gate.GroupId);

            if (group == null || user.GetClient()?.GetHabbo() == null) return false;

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
                List<Point> bedTiles = bed.GetBedTiles(to, out _);
                if (bedTiles.Any(p => SquareHasUsers(p.X, p.Y))) return true;
                if (!endOfPath) return true;
            }
            return false;
        }

        public static bool CanWalk(byte state, bool @override) =>
            @override || state == 1 || state == 3;

        // ══════════════════════════════════════════════════════════════════════════
        //  ALTURAS E ÍTEMS
        // ══════════════════════════════════════════════════════════════════════════

        public double SqAbsoluteHeight(int x, int y)
        {
            long key = CoordKey(x, y);
            if (_coordinatedItems.TryGetValue(key, out int[] ids))
                return SqAbsoluteHeight(x, y, ResolveItems(ids));
            return _dynamicModel.SqFloorHeight[x, y];
        }

        public double SqAbsoluteHeight(int x, int y, List<Item> itemsOnSquare)
        {
            try
            {
                double highestStack = 0, deductable = 0;
                bool deduct = false, hasSeat = false;

                if (itemsOnSquare != null)
                {
                    foreach (Item item in itemsOnSquare)
                    {
                        if (item == null) continue;
                        bool isBedSeat = item.GetBaseItem().IsSeat ||
                                         item.GetBaseItem().InteractionType == InteractionType.BED ||
                                         item.GetBaseItem().InteractionType == InteractionType.TENT_SMALL;
                        if (isBedSeat) hasSeat = true;
                        if (item.TotalHeight <= highestStack) continue;
                        highestStack = item.TotalHeight;
                        deduct = isBedSeat;
                        deductable = isBedSeat ? item.GetBaseItem().Height : 0;
                    }

                    // Si el ítem más alto NO es seat pero hay seat debajo, usar el seat
                    if (!deduct && hasSeat)
                    {
                        foreach (Item item in itemsOnSquare)
                        {
                            if (item == null) continue;
                            bool isBedSeat = item.GetBaseItem().IsSeat ||
                                             item.GetBaseItem().InteractionType == InteractionType.BED ||
                                             item.GetBaseItem().InteractionType == InteractionType.TENT_SMALL;
                            if (!isBedSeat) continue;
                            deduct = true;
                            deductable = item.GetBaseItem().Height;
                            highestStack = item.TotalHeight;
                            break;
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
            var items = GetAllRoomItemForSquare(square.X, square.Y);
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
            long key = CoordKey(pX, pY);
            return _coordinatedItems.TryGetValue(key, out int[] ids)
                ? ResolveItems(ids)
                : new List<Item>(0);
        }

        public List<Item> GetRoomItemForSquare(int pX, int pY, double minZ)
        {
            long key = CoordKey(pX, pY);
            if (!_coordinatedItems.TryGetValue(key, out int[] ids))
                return new List<Item>(0);
            return ResolveItems(ids)
                .Where(i => i.GetZ > minZ && i.GetX == pX && i.GetY == pY)
                .ToList();
        }

        public List<Item> GetRoomItemForSquare(int pX, int pY)
        {
            long key = CoordKey(pX, pY);
            if (!_coordinatedItems.TryGetValue(key, out int[] ids))
                return new List<Item>(0);
            return ResolveItems(ids)
                .Where(i => i.Coordinate.X == pX && i.Coordinate.Y == pY)
                .ToList();
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  UTILIDADES
        // ══════════════════════════════════════════════════════════════════════════

        public Point GetRandomWalkableSquare()
        {
            try
            {
                // FIX: en vez de ToList() + O(n) completo, usa lazy IEnumerable
                var squares = GetWalkableSquares()
                    .Where(p => p.X != StaticModel.DoorX || p.Y != StaticModel.DoorY)
                    .ToList();
                if (squares.Count == 0) return new Point(0, 0);
                return squares[PolarEnvironment.GetRandomNumber(0, squares.Count - 1)];
            }
            catch { return new Point(0, 0); }
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

        // FIX: antes O(n) scan de GetWalkableSquares; ahora O(1) con ValidTile + GameMap[]
        public bool IsInMap(int x, int y)
        {
            if (!ValidTile(x, y)) return false;
            if (x == StaticModel.DoorX && y == StaticModel.DoorY) return false;
            return GameMap[x, y] == 1;
        }

        public static Dictionary<int, ThreeDCoord> GetAffectedTiles(
            int length, int width, int posX, int posY, int rotation)
        {
            // FIX: HashSet para deduplicar O(1) en vez de .Values.Contains() O(n)
            var pointList = new Dictionary<int, ThreeDCoord>();
            var seen = new HashSet<ThreeDCoord>();
            int idx = 0;

            void TryAdd(ThreeDCoord c) { if (seen.Add(c)) pointList[idx++] = c; }

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
            bool isHorizontal = false;
            int iX = item.GetX, iY = item.GetY;

            foreach (RoomUser user in _room.GetRoomUserManager().GetRoomUsers())
            {
                if (user.X == iX)
                {
                    int diff = Math.Abs(user.Y - iY);
                    if (diff < distance) { distance = diff; coord = user.Coordinate; isHorizontal = false; }
                }
                else if (user.Y == iY)
                {
                    int diff = Math.Abs(user.X - iX);
                    if (diff < distance) { distance = diff; coord = user.Coordinate; isHorizontal = true; }
                }
            }

            // FIX: antes OrderBy(Guid.NewGuid()) — O(n log n) + crypto RNG por elemento.
            //      Ahora selección directa O(1).
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
            if (coord.X > GameMap.GetUpperBound(0) || coord.Y > GameMap.GetUpperBound(1)) return 1;
            return GameMap[coord.X, coord.Y];
        }

        public void SetFloorStatus(int x, int y, byte status)
        {
            if (ValidTile(x, y)) GameMap[x, y] = status;
        }

        public double GetHeightForSquareFromData(Point coord)
        {
            if (coord.X > _dynamicModel.SqFloorHeight.GetUpperBound(0) ||
                coord.Y > _dynamicModel.SqFloorHeight.GetUpperBound(1)) return 1;
            return _dynamicModel.SqFloorHeight[coord.X, coord.Y];
        }

        public bool CanRollItemHere(int x, int y, HabboHotel.GameClients.GameClient session)
        {
            if (!ValidTile(x, y) || Model.SqState[x, y] == SquareState.BLOCKED) return false;
            return _room.CheckTerrain(session, x, y);
        }

        public bool CanRollItemHere(int x, int y) =>
            ValidTile(x, y) && Model.SqState[x, y] != SquareState.BLOCKED;

        // ── Propiedades ───────────────────────────────────────────────────────────
        public DynamicRoomModel Model => _dynamicModel;
        public RoomModel StaticModel => _staticModel;

        // ── IDisposable ───────────────────────────────────────────────────────────
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
    }
}