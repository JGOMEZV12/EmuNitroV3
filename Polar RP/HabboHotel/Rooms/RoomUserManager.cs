using log4net;
using Polar.Communication.Packets.Outgoing.Handshake;
using Polar.Communication.Packets.Outgoing.Rooms.Avatar;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.Communication.Packets.Outgoing.Rooms.Permissions;
using Polar.Communication.Packets.Outgoing.Rooms.Session;
using Polar.Core;
using Polar.Database.Interfaces;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Pathfinding;
using Polar.HabboHotel.Rooms.AI;
using Polar.HabboHotel.Rooms.Games.Teams;
using Polar.HabboHotel.Rooms.Pathfinding;
using Polar.HabboRoleplay.Bots.Manager;
using Polar.HabboRoleplay.Gambling;
using Polar.HabboRoleplay.Misc;
using Polar.Utilities;
using System.Collections.Concurrent;
using System.Data;
using System.Drawing;
using System.Xml.Linq;

namespace Polar.HabboHotel.Rooms
{
    public class RoomUserManager
    {
        private readonly Room _room;
        public ConcurrentDictionary<int, RoomUser> _users;
        public ConcurrentDictionary<int, RoomUser> _bots;
        private ConcurrentDictionary<int, RoomUser> _pets;
        // ✅ FIX #1: OrdinalIgnoreCase en el constructor evita duplicados por capitalización
        //            sin depender de .ToLower() manual en cada llamada.
        private ConcurrentDictionary<string, RoomUser> _usersByUsername;
        private ConcurrentDictionary<int, RoomUser> _usersByUserID;

        public int primaryPrivateUserID;
        public int secondaryPrivateUserID;
        public int userCount;
        private int petCount;

        // ✅ FIX WALK-2: UpdateUserCount hacía un UPDATE a BD en cada ciclo de sala.
        //   Con salas activas esto genera presión de BD y bloquea brevemente el hilo del ciclo.
        //   Se persiste solo cada ~30s (60 ticks a 500ms) en lugar de cada tick.
        private int _userCountSaveTick = 0;
        private const int UserCountSaveInterval = 60;

        private static readonly ILog log = LogManager.GetLogger("Polar.HabboHotel.Rooms.Room");

        public RoomUserManager(Room room)
        {
            this._room = room;
            this._users = new ConcurrentDictionary<int, RoomUser>();
            this._pets = new ConcurrentDictionary<int, RoomUser>();
            this._bots = new ConcurrentDictionary<int, RoomUser>();
            this._usersByUsername = new ConcurrentDictionary<string, RoomUser>(StringComparer.OrdinalIgnoreCase);
            this._usersByUserID = new ConcurrentDictionary<int, RoomUser>();
            this.primaryPrivateUserID = 0;
            this.secondaryPrivateUserID = 0;
            this.petCount = 0;
            this.userCount = 0;
        }

        public void Dispose()
        {
            this._users?.Clear();
            this._pets?.Clear();
            this._bots?.Clear();
            this._usersByUsername?.Clear();
            this._usersByUserID?.Clear();

            this._users = null;
            this._pets = null;
            this._bots = null;
            this._usersByUsername = null;
            this._usersByUserID = null;
        }

        public RoomUser DeployBot(RoomBot Bot, Pet PetData)
        {
            var BotUser = new RoomUser(0, _room.RoomId, primaryPrivateUserID++, _room);
            Bot.VirtualId = primaryPrivateUserID;

            int PersonalID = secondaryPrivateUserID++;
            BotUser.InternalRoomID = PersonalID;
            _users.TryAdd(PersonalID, BotUser);

            DynamicRoomModel Model = _room.GetGameMap().Model;

            if ((Bot.X > 0 && Bot.Y > 0) && Bot.X < Model.MapSizeX && Bot.Y < Model.MapSizeY)
            {
                BotUser.SetPos(Bot.X, Bot.Y, Bot.Z);
                BotUser.SetRot(Bot.Rot, false);
            }
            else
            {
                Bot.X = Model.DoorX;
                Bot.Y = Model.DoorY;
                BotUser.SetPos(Model.DoorX, Model.DoorY, Model.DoorZ);
                BotUser.SetRot(Model.DoorOrientation, false);
            }

            BotUser.BotData = Bot;
            BotUser.BotAI = Bot.GenerateBotAI(BotUser.VirtualId);

            if (BotUser.IsPet)
            {
                BotUser.BotAI.Init(Bot.BotId, BotUser.VirtualId, _room.RoomId, BotUser, _room);
                BotUser.PetData = PetData;
                BotUser.PetData.VirtualId = BotUser.VirtualId;
            }
            else
            {
                BotUser.BotAI.Init(Bot.BotId, BotUser.VirtualId, _room.RoomId, BotUser, _room);
            }

            BotUser.UpdateNeeded = true;
            _room.SendMessage(new UsersComposer(BotUser));

            if (BotUser.IsPet)
            {
                // ✅ FIX #2: ContainsKey + TryAdd tiene TOCTOU race condition en ConcurrentDictionary.
                //            El indexer assignment es atómico (upsert).
                _pets[BotUser.PetData.PetId] = BotUser;
                petCount++;
            }
            else if (BotUser.IsBot)
            {
                _bots[BotUser.BotData.BotId] = BotUser;
                _room.SendMessage(new DanceComposer(BotUser, BotUser.BotData.DanceId));
            }

            return BotUser;
        }

        public void RemoveBot(int VirtualId, bool Kicked)
        {
            RoomUser User = GetRoomUserByVirtualId(VirtualId);
            if (User == null || !User.IsBot) return;

            if (User.IsPet)
            {
                _pets.TryRemove(User.PetData.PetId, out _);
                petCount--;
            }
            else
            {
                _bots.TryRemove(User.BotData.Id, out _);
            }

            User.BotAI.OnSelfLeaveRoom(Kicked);
            _room.SendMessage(new UserRemoveComposer(User.VirtualId));
            _users?.TryRemove(User.InternalRoomID, out _);
            onRemove(User);
        }

        public RoomUser GetUserForSquare(int x, int y)
        {
            return _room.GetGameMap().GetRoomUsers(new Point(x, y)).FirstOrDefault();
        }

        public bool AddAvatarToRoom(GameClient Session)
        {
            if (_room == null || Session == null || Session.GetHabbo().CurrentRoom == null)
                return false;

            RoomUser User = new RoomUser(Session.GetHabbo().Id, _room.RoomId, primaryPrivateUserID++, _room);

            if (User == null || User.GetClient() == null)
                return false;

            User.UserId = Session.GetHabbo().Id;
            Session.GetHabbo().TentId = 0;

            int PersonalID = secondaryPrivateUserID++;
            User.InternalRoomID = PersonalID;
            Session.GetHabbo().CurrentRoomId = _room.RoomId;

            if (!this._users.TryAdd(PersonalID, User))
                return false;

            string Username = Session.GetHabbo().Username;
            int UserId = Session.GetHabbo().Id;

            // ✅ FIX #3: ContainsKey+TryRemove+TryAdd tiene race condition.
            //            Indexer assignment es atómico en ConcurrentDictionary.
            this._usersByUsername[Username.ToLower()] = User;
            this._usersByUserID[UserId] = User;

            DynamicRoomModel Model = _room.GetGameMap().Model;
            if (Model == null) return false;

            if (!_room.PetMorphsAllowed && Session.GetHabbo().PetId != 0)
                Session.GetHabbo().PetId = 0;

            if (Session.GetRoleplay().InsideTaxi || Session.GetRoleplay().InsideBus ||
                (!Session.GetHabbo().IsTeleporting && !Session.GetHabbo().IsHopping))
            {
                if (!Model.DoorIsValid())
                {
                    Point Square = _room.GetGameMap().GetRandomWalkableSquare();
                    Model.DoorX = Square.X;
                    Model.DoorY = Square.Y;
                    Model.DoorZ = _room.GetGameMap().GetHeightForSquareFromData(Square);
                }

                #region Roleplay last spawn coordination
                if (!Session.GetRoleplay().AntiArrowCheck)
                {
                    object[] Coords = Session.GetRoleplay().LastCoordinates.Split(',');
                    int LastX = Convert.ToInt32(Coords[0]);
                    int LastY = Convert.ToInt32(Coords[1]);
                    double LastZ = Convert.ToDouble(Coords[2]);
                    int LastRot = Convert.ToInt32(Coords[3]);

                    if (_room.GetGameMap().IsInMap(LastX, LastY))
                    {
                        if (LastX == 0 && LastY == 0)
                        {
                            User.SetPos(Model.DoorX, Model.DoorY, Model.DoorZ);
                            User.SetRot(Model.DoorOrientation, false);
                        }
                        else
                        {
                            User.SetPos(LastX, LastY, LastZ);
                            User.SetRot(LastRot, false);
                            UpdateUserStatus(User, false);
                        }
                    }
                    else
                    {
                        User.SetPos(Model.DoorX, Model.DoorY, Model.DoorZ);
                        User.SetRot(Model.DoorOrientation, false);
                    }
                }
                else
                {
                    User.SetPos(Model.DoorX, Model.DoorY, Model.DoorZ);
                    User.SetRot(Model.DoorOrientation, false);
                }
                #endregion

                #region Roleplay Exiting Houses
                if (Session.GetRoleplay().ExitingHouse)
                {
                    int LastX = Session.GetRoleplay().HouseX;
                    int LastY = Session.GetRoleplay().HouseY;
                    double LastZ = Session.GetRoleplay().HouseZ;

                    if (LastX == 0 && LastY == 0)
                    {
                        User.SetPos(Model.DoorX, Model.DoorY, Model.DoorZ);
                        User.SetRot(Model.DoorOrientation, false);
                    }
                    else
                    {
                        User.SetPos(LastX, LastY, LastZ);
                        User.SetRot(Model.DoorOrientation, false);
                    }

                    UpdateUserStatus(User, false);
                    Session.GetRoleplay().ExitingHouse = false;
                    Session.GetRoleplay().HouseX = 0;
                    Session.GetRoleplay().HouseY = 0;
                    Session.GetRoleplay().HouseZ = 0;
                }
                #endregion

                #region Tutorial check
                if (User.GetClient().GetRoleplay().TutorialStep < RoleplayManager.LastTutorialStep &&
                    !User.GetClient().GetRoleplay().InTutorial)
                {
                    User.GetClient().GetRoleplay().InTutorial = true;
                    int step = User.GetClient().GetRoleplay().TutorialStep;

                    if (step == 13 && _room.WardrobeEnabled && _room.Type.Equals("public"))
                        PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(User.GetClient(), "compose_tutorial|13");
                    else if (step == 18 && _room.PhoneStoreEnabled && _room.Type.Equals("public"))
                        PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(User.GetClient(), "compose_tutorial|18");
                    else if (step == 23 && _room.BuyCarEnabled && _room.Type.Equals("public"))
                        PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(User.GetClient(), "compose_tutorial|24");
                    else if (step == 27 && _room.MallEnabled && _room.Type.Equals("public"))
                        PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(User.GetClient(), "compose_tutorial|28");
                    else
                        PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(User.GetClient(), "compose_my_tutorial|" + step);
                }
                #endregion
            }
            else if (!User.IsBot && (User.GetClient().GetHabbo().IsTeleporting || User.GetClient().GetHabbo().IsHopping))
            {
                Item Item = null;
                if (Session.GetHabbo().IsTeleporting)
                    Item = _room.GetRoomItemHandler().GetItem(Session.GetHabbo().TeleporterId);
                else if (Session.GetHabbo().IsHopping)
                    Item = _room.GetRoomItemHandler().GetItem(Session.GetHabbo().HopperId);

                if (Item != null)
                {
                    if (Session.GetHabbo().IsTeleporting)
                    {
                        Item.ExtraData = "2";
                        Item.UpdateState(false, true);
                        User.SetPos(Item.GetX, Item.GetY, Item.GetZ);
                        User.SetRot(Item.Rotation, false);
                        Item.InteractingUser2 = Session.GetHabbo().Id;
                        Item.ExtraData = "0";
                        Item.UpdateState(false, true);
                    }
                    else if (Session.GetHabbo().IsHopping)
                    {
                        Item.ExtraData = "1";
                        Item.UpdateState(false, true);
                        User.SetPos(Item.GetX, Item.GetY, Item.GetZ);
                        User.SetRot(Item.Rotation, false);
                        User.AllowOverride = false;
                        Item.InteractingUser2 = Session.GetHabbo().Id;
                        Item.ExtraData = "2";
                        Item.UpdateState(false, true);
                    }
                }
                else
                {
                    User.SetPos(Model.DoorX, Model.DoorY, Model.DoorZ - 1);
                    User.SetRot(Model.DoorOrientation, false);
                }
            }

            #region Invisible command
            if (User.GetClient()?.GetRoleplay() != null)
            {
                if (User.GetClient().GetRoleplay().Invisible && !_room.TutorialEnabled)
                {
                    User.GetClient().SendMessage(new UserRemoveComposer(User.GetClient().GetRoomUser().VirtualId));
                    RoleplayManager.SendDelayedWhisper(User.GetClient(), "Reminder: tu eres invisible", 1);

                    foreach (RoomUser roomUser in GetUserList())
                    {
                        if (roomUser?.GetClient()?.GetHabbo() == null) continue;

                        string cansee = "";
                        if (roomUser.GetClient().GetRoleplay().Invisible &&
                            roomUser.GetClient().GetHabbo().Username != User.GetClient().GetHabbo().Username)
                        {
                            User.GetClient().SendMessage(new UsersComposer(roomUser));
                            roomUser.GetClient().SendMessage(new UsersComposer(User));
                            cansee += roomUser.GetClient().GetHabbo().Username + ", ";
                            RoleplayManager.SendDelayedWhisper(User.GetClient(),
                                "El usuario invisible " + User.GetClient().GetHabbo().Username +
                                " Ha entrado en la habitación y puede verte", 1);
                            continue;
                        }

                        if (roomUser.GetClient().GetHabbo().Username == User.GetClient().GetHabbo().Username)
                        {
                            RoleplayManager.SendDelayedWhisper(User.GetClient(),
                                "Los siguientes usuarios pueden ver que son invisibles: " + cansee, 1);
                            continue;
                        }

                        if (!roomUser.GetClient().GetRoleplay().Invisible)
                            roomUser.GetClient().SendMessage(new UserRemoveComposer(User.VirtualId));
                    }
                }
                else
                {
                    if (_room.TutorialEnabled)
                        Session.SendMessage(new UsersComposer(Session.GetRoomUser()));
                    else
                        _room.SendMessage(new UsersComposer(User));
                }
            }
            #endregion

            if (_room.CheckRights(Session, true))
            {
                User.SetStatus("flatctrl", "useradmin");
                Session.SendMessage(new YouAreOwnerComposer());
                Session.SendMessage(new YouAreControllerComposer(5));
            }
            else if (_room.CheckRights(Session, false) && _room.Group == null)
            {
                User.SetStatus("flatctrl", "1");
                Session.SendMessage(new YouAreControllerComposer(1));
            }
            else if (_room.Group != null && _room.CheckRights(Session, false, true))
            {
                User.SetStatus("flatctrl", "3");
                Session.SendMessage(new YouAreControllerComposer(3));
            }
            else
            {
                Session.SendMessage(new YouAreNotControllerComposer());
            }

            User.UpdateNeeded = true;

            foreach (RoomUser Bot in this._bots.Values.ToList())
            {
                if (Bot?.BotAI == null) continue;
                Bot.BotAI.OnUserEnterRoom(User);
            }

            Session.GetRoleplay().UpdateInteractingUserDialogues();
            Session.GetRoleplay().RefreshStatDialogue();
            Session.GetRoleplay().InitWSDialogues();
            Session.GetRoleplay().InitStatDialogue();

            if (Session.GetRoleplay().Phone > 0)
            {
                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_phone", "load_apps");
                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_phone", "show_button");
            }

            return true;
        }

        public void RemoveUserFromRoom(GameClient Session, bool NotifyClient, bool NotifyKick = false, bool IdleKicked = false)
        {
            try
            {
                if (_room == null || Session?.GetHabbo() == null) return;

                if (NotifyKick && !IdleKicked)
                    Session.SendMessage(new GenericErrorComposer(4008));

                if (NotifyClient)
                    Session.SendMessage(new CloseConnectionComposer());

                if (Session.GetHabbo().TentId > 0)
                    Session.GetHabbo().TentId = 0;

                RoomUser User = GetRoomUserByHabbo(Session.GetHabbo().Id);
                if (User == null) return;

                if (User.RidingHorse)
                {
                    User.RidingHorse = false;
                    RoomUser UserRiding = GetRoomUserByVirtualId(User.HorseID);
                    if (UserRiding != null)
                    {
                        UserRiding.RidingHorse = false;
                        UserRiding.HorseID = 0;
                    }
                }

                if (User.Team != TEAM.NONE)
                {
                    TeamManager Team = this._room.GetTeamManagerForFreeze();
                    if (Team != null)
                    {
                        Team.OnUserLeave(User);
                        User.Team = TEAM.NONE;
                        if (User.GetClient().GetHabbo().Effects().CurrentEffect != 0)
                            User.GetClient().GetHabbo().Effects().ApplyEffect(0);
                    }
                }

                this._usersByUserID.TryRemove(User.UserId, out _);
                this._usersByUsername.TryRemove(Session.GetHabbo().Username.ToLower(), out _);

                RemoveRoomUser(User);

                if (User.CurrentItemEffect != ItemEffectType.NONE)
                    Session.GetHabbo().Effects().CurrentEffect = -1;

                if (this._room.HasActiveTrade(Session.GetHabbo().Id))
                    this._room.TryStopTrade(Session.GetHabbo().Id);

                Session.GetHabbo().GetMessenger()?.OnStatusChanged(true);
                User.Dispose();
            }
            catch (Exception e)
            {
                Logging.LogException(e.ToString());
            }
        }

        private void onRemove(RoomUser user)
        {
            try
            {
                GameClient session = user.GetClient();
                if (session == null) return;

                // ✅ FIX #4: Antes se construía una lista de bots escaneando TODOS los usuarios
                //            con GetUserList() — O(n) innecesario cuando ya existe _bots.
                //            Usar _bots directamente es O(b) donde b = número de bots.
                List<RoomUser> PetsToRemove = new List<RoomUser>();

                foreach (RoomUser Bot in _bots.Values.ToList())
                {
                    if (Bot?.BotAI == null) continue;

                    Bot.BotAI.OnUserLeaveRoom(session);

                    if (Bot.GetBotRoleplayAI() != null)
                        Bot.GetBotRoleplayAI().OnUserLeaveRoom(session);

                    if (Bot.IsPet && Bot.PetData.OwnerId == user.UserId && !_room.CheckRights(session, true))
                    {
                        if (!PetsToRemove.Contains(Bot))
                            PetsToRemove.Add(Bot);
                    }
                }

                foreach (RoomUser toRemove in PetsToRemove)
                {
                    if (toRemove == null) continue;
                    if (user.GetClient()?.GetHabbo()?.GetInventoryComponent() == null) continue;

                    user.GetClient().GetHabbo().GetInventoryComponent().TryAddPet(toRemove.PetData);
                    RemoveBot(toRemove.VirtualId, false);
                }

                _room.GetGameMap().RemoveUserFromMap(user, new Point(user.X, user.Y));
            }
            catch (Exception e)
            {
                Logging.LogCriticalException(e.ToString());
            }
        }

        public void ClearUsers(GameClient Session)
        {
            foreach (RoomUser user in GetUserList())
            {
                if (user == null) continue;
                Session.SendMessage(new UserRemoveComposer(user.VirtualId));
            }
        }

        public void RemoveRoomUser(RoomUser user)
        {
            if (user.SetStep)
                _room.GetGameMap().GameMap[user.SetX, user.SetY] = user.SqState;
            else
                _room.GetGameMap().GameMap[user.X, user.Y] = user.SqState;

            _room.GetGameMap().RemoveUserFromMap(user, new Point(user.X, user.Y));
            _room.SendMessage(new UserRemoveComposer(user.VirtualId));
            this._users.TryRemove(user.InternalRoomID, out _);
            user.InternalRoomID = -1;
            onRemove(user);
        }

        public bool TryGetPet(int PetId, out RoomUser Pet) => _pets.TryGetValue(PetId, out Pet);
        public bool TryGetBot(int BotId, out RoomUser Bot) => _bots.TryGetValue(BotId, out Bot);
        public RoomUser GetBotByName(string Name) {

            bool foundBot = _bots.Any(x => x.Value.BotData != null && x.Value.BotData.Name.ToLower() == Name.ToLower());
            if (foundBot)
            {
                int id = _bots.FirstOrDefault(x => x.Value.BotData != null && x.Value.BotData.Name.ToLower() == Name.ToLower()).Value.BotData.Id;

                return _bots[id];
            }
            else
            {

                return RoleplayBotManager.GetDeployedBotByName(Name);
            }
            return null;

        }

        public void UpdateUserCount(int count)
        {
            userCount = count;
            _room.RoomData.UsersNow = count;

            // ✅ FIX WALK-2: Solo persistir en BD cada UserCountSaveInterval ticks.
            //   El contador en memoria se actualiza siempre (para lógica interna),
            //   pero el UPDATE a BD se limita para no bloquear el hilo del ciclo.
            _userCountSaveTick++;
            if (_userCountSaveTick < UserCountSaveInterval) return;
            _userCountSaveTick = 0;

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("UPDATE `rooms` SET `users_now` = @count WHERE `id` = @roomId LIMIT 1");
                dbClient.AddParameter("count", count);
                dbClient.AddParameter("roomId", _room.RoomId);
                dbClient.RunQuery();
            }
        }

        public RoomUser GetRoomUserByVirtualId(int VirtualId)
        {
            if (_users == null) return null;
            _users.TryGetValue(VirtualId, out RoomUser user);
            return user;
        }

        public RoomUser GetRoomUserByHabboId(int pId)
        {
            _usersByUserID.TryGetValue(pId, out RoomUser user);
            return user;
        }

        public RoomUser GetRoomUserByHabbo(int Id)
        {
            // ✅ FIX #6: "if (this == null)" — nunca puede ser verdadero en C#, eliminado.
            //            LINQ scan sobre GetUserList() era O(n) con cadenas de null-checks.
            //            Usar el índice directo _usersByUserID es O(1).
            _usersByUserID.TryGetValue(Id, out RoomUser user);
            return user;
        }

        public List<RoomUser> GetRoomUsers()
        {
            // ✅ FIX #7: Devolvía null cuando LINQ no encontraba nada — callers hacen .Count
            //            sin null-check y explotan con NRE. Siempre devolver lista (nunca null).
            return GetUserList().Where(x => x != null && !x.IsBot).ToList();
        }

        public List<RoomUser> GetRoleplayBots()
        {
            // ✅ FIX #7 aplicado: nunca devolver null
            return GetUserList()
                .Where(x => x != null && x.IsBot && x.IsRoleplayBot && x.GetBotRoleplay() != null)
                .ToList();
        }

        public List<RoomUser> GetRoomUserByRank(int minRank)
        {
            return GetUserList()
                .Where(x => x?.GetClient()?.GetHabbo()?.Rank >= minRank)
                .ToList();
        }

        public List<RoomUser> GetRoomUserBySpecialRights()
        {
            return GetUserList()
                .Where(x => x?.GetClient()?.GetHabbo()?.VIPRank > 0)
                .ToList();
        }

        public RoomUser GetRoomUserByHabbo(string pName)
        {
            RoomUser User = GetUserList().FirstOrDefault(x =>
                x?.GetClient()?.GetHabbo() != null &&
                x.GetClient().GetRoleplay() != null &&
                x.GetClient().GetHabbo().Username.Equals(pName, StringComparison.OrdinalIgnoreCase));

            if (User == null) return null;
            return User.GetClient().GetRoleplay().Invisible ? null : User;
        }

        public RoomUser GetRoleplayBotByName(string pName)
        {
            return GetUserList().FirstOrDefault(x =>
                x != null && x.IsBot && x.IsRoleplayBot &&
                x.GetBotRoleplay()?.Name.Equals(pName, StringComparison.OrdinalIgnoreCase) == true);
        }

        public List<Pet> GetPets()
        {
            var Pets = new List<Pet>();
            foreach (RoomUser User in _pets.Values.ToList())
            {
                if (User?.IsPet == true)
                    Pets.Add(User.PetData);
            }
            return Pets;
        }

        public void SerializeStatusUpdates()
        {
            ICollection<RoomUser> RoomUsers = GetUserList();
            if (RoomUsers == null) return;

            // ✅ FIX #8: List<T>.Contains() es O(n) — con 100 usuarios son ~5000 comparaciones.
            //            HashSet<T>.Contains() es O(1). Reemplazado por HashSet para deduplicar.
            var seen = new HashSet<RoomUser>();
            var toUpdate = new List<RoomUser>();

            foreach (RoomUser User in RoomUsers)
            {
                if (User == null || !User.UpdateNeeded) continue;
                if (!seen.Add(User)) continue;

                User.UpdateNeeded = false;
                toUpdate.Add(User);
            }

            if (toUpdate.Count > 0)
                _room.SendMessage(new UserUpdateComposer(toUpdate));
        }

        public List<RoomUser> GetBots()
        {
            return _bots.Values.Where(u => u != null && u.IsBot).ToList();
        }

        public void UpdateUserStatusses()
        {
            foreach (RoomUser user in GetUserList())
            {
                if (user == null) continue;
                UpdateUserStatus(user, false);
            }
        }

        private bool isValid(RoomUser user)
        {
            if (user == null) return false;
            if (user.IsBot) return true;
            if (user.GetClient()?.GetHabbo() == null) return false;
            if (user.GetClient().GetHabbo().CurrentRoomId != _room.RoomId) return false;
            return true;
        }

        public void OnCycle()
        {
            int userCounter = 0;
            var usersToRemove = new List<RoomUser>();

            try
            {
                ProcessTonerEffect();

                foreach (RoomUser user in _users.Values)
                {
                    if (user == null) continue;

                    if (!isValid(user))
                    {
                        HandleInvalidUser(user);
                        continue;
                    }

                    ProcessCaptureEvents(user);
                    UpdateBasicUserState(user);
                    ProcessUserMovementOptimized(user, usersToRemove);

                    if (user.IsBot && user.BotAI != null)
                        user.BotAI.OnTimerTick();
                    else
                        userCounter++;

                    UpdateUserEffect(user, user.X, user.Y);
                }

                RemoveMarkedUsers(usersToRemove);

                if (userCount != userCounter)
                    UpdateUserCount(userCounter);
            }
            catch (Exception e)
            {
                Logging.LogCriticalException($"Affected Room - ID: {_room?.Id ?? 0} - {e}");
            }
        }

        private void ProcessTonerEffect()
        {
            if (_room == null || !_room.DiscoMode || _room.TonerData == null || _room.TonerData.Enabled != 1)
                return;

            Item tonerItem = _room.GetRoomItemHandler().GetItem(_room.TonerData.ItemId);
            if (tonerItem == null) return;

            _room.TonerData.Hue = PolarEnvironment.GetRandomNumber(0, 255);
            _room.TonerData.Saturation = PolarEnvironment.GetRandomNumber(0, 255);
            _room.TonerData.Lightness = PolarEnvironment.GetRandomNumber(0, 255);

            _room.SendMessage(new ObjectUpdateComposer(tonerItem, _room.OwnerId));
            tonerItem.UpdateState();
        }

        private void HandleInvalidUser(RoomUser user)
        {
            if (user.GetClient() != null)
                RemoveUserFromRoom(user.GetClient(), false, false);
            else
                RemoveRoomUser(user);
        }

        private void ProcessCaptureEvents(RoomUser user)
        {
            if (user.GetClient() == null) return;

            if (_room.TurfCapturing)
                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(
                    user.GetClient(), "event_gang",
                    $"turf_cap_w,{_room.Id},{_room.TurfUserAtackerId},{_room.Name}");

            if (_room.BankCapturing)
                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(
                    user.GetClient(), "event_gang",
                    $"bank_cap_w,{_room.Id},{_room.TurfUserAtackerId},{_room.Name}");
        }

        private void UpdateBasicUserState(RoomUser user)
        {
            user.IdleTime++;
            user.HandleSpamTicks();

            if (!user.IsBot && !user.IsAsleep && user.IdleTime >= 600)
            {
                user.IsAsleep = true;
                _room.SendMessage(new SleepComposer(user, true));

                var rp = user.GetClient()?.GetRoleplay();
                if (rp != null && !rp.IsJailed && !rp.IsDead)
                {
                    rp.BreakGeneralTimer = true;
                    user.GetClient().GetHabbo().Motto = "[DORMIDO] " + rp.Class;
                    user.GetClient().GetHabbo().Poof(true);
                }
            }

            if (user.CarryItemID > 0)
            {
                user.CarryTimer--;
                if (user.CarryTimer <= 0) user.CarryItem(0);
            }

            if (_room.GotFreeze()) _room.GetFreeze().CycleUser(user);

            if (user.isRolling)
            {
                if (user.rollerDelay <= 0)
                {
                    UpdateUserStatus(user, false);
                    user.isRolling = false;
                }
                else
                {
                    user.rollerDelay--;
                }
            }

            if (user.RidingHorse) user.ApplyEffect(77);
        }

        private void ProcessUserMovementOptimized(RoomUser user, List<RoomUser> usersToRemove)
        {
            bool invalidStep = false;

            if (user.SetStep)
            {
                HandleSetStep(user, ref invalidStep, usersToRemove);
                user.SetStep = false;
            }

            if (user.PathRecalcNeeded)
                RecalculateUserPathOptimized(user);

            if (user.IsWalking && !user.Freezed)
                ProcessWalkingOptimized(user, ref invalidStep);
            else
                CleanupMovementStatus(user);
        }

        private void HandleSetStep(RoomUser user, ref bool invalidStep, List<RoomUser> usersToRemove)
        {
            var from = new Vector2D(user.X, user.Y);
            var to = new Vector2D(user.SetX, user.SetY);
            bool isFinalStep = (user.GoalX == user.SetX && user.GoalY == user.SetY);

            if (_room.GetGameMap().IsValidStep(user, from, to, isFinalStep, user.AllowOverride))
            {
                if (!user.RidingHorse)
                    _room.GetGameMap().UpdateUserMovement(
                        new Point(user.Coordinate.X, user.Coordinate.Y),
                        new Point(user.SetX, user.SetY), user);

                foreach (Item item in _room.GetGameMap().GetCoordinatedItems(new Point(user.X, user.Y)))
                    item.UserWalksOffFurni(user);

                if (!user.IsBot || !user.RidingHorse)
                {
                    user.X = user.SetX;
                    user.Y = user.SetY;
                    user.Z = user.SetZ;
                }

                if (!user.IsBot && user.RidingHorse)
                {
                    RoomUser horse = GetRoomUserByVirtualId(user.HorseID);
                    if (horse != null) { horse.X = user.SetX; horse.Y = user.SetY; }
                }

                foreach (Item item in _room.GetGameMap().GetCoordinatedItems(new Point(user.X, user.Y)))
                    item.UserWalksOnFurni(user);

                SaveUserCoordinates(user);
                UpdateUserStatus(user, true);

                if (isFinalStep || (user.X == user.GoalX && user.Y == user.GoalY))
                    StopWalking(user);
            }
            else
            {
                invalidStep = true;
            }
        }

        private void RecalculateUserPathOptimized(RoomUser user)
        {
            // ✅ FIX WALK-3: Si hay un SetStep pendiente (el usuario se está moviendo al tile
            //   SetX/SetY pero aún no ha llegado), el path nuevo debe partir desde SetX/SetY,
            //   no desde X/Y (posición pre-confirmada). Si partimos desde X/Y y luego el step
            //   se confirma, el primer nodo del path apunta al tile anterior — causando un
            //   micro-retroceso visual (el "tirón" más común al hacer clic rápido).
            int startX = user.SetStep ? user.SetX : user.X;
            int startY = user.SetStep ? user.SetY : user.Y;

            if (user.Path == null) user.Path = new List<Vector2D>();

            PathFinder.FindPath(user, _room.GetGameMap().DiagonalEnabled,
                _room.GetGameMap(), new Vector2D(startX, startY),
                new Vector2D(user.GoalX, user.GoalY), user.Path);

            if (user.Path.Count > 0)
            {
                user.PathStep = 1;
                user.IsWalking = true;
            }
            else
            {
                user.IsWalking = false;
                user.Path?.Clear();
                user.PathStep = 0;
            }

            user.PathRecalcNeeded = false;
        }

        private void ProcessWalkingOptimized(RoomUser user, ref bool invalidStep)
        {
            if (user.Path == null || user.Path.Count == 0) { StopWalking(user); return; }

            bool atDestination = (user.X == user.GoalX && user.Y == user.GoalY);
            if (atDestination || invalidStep || user.PathStep > user.Path.Count) { StopWalking(user); return; }

            int stepIndex = (user.Path.Count - user.PathStep);
            if (stepIndex < 0 || stepIndex >= user.Path.Count) { StopWalking(user); return; }

            Vector2D nextStep = user.Path[stepIndex];
            user.PathStep++;

            ApplyFastWalkingOptimized(user, ref nextStep, ref stepIndex);
            ExecuteMovementOptimized(user, nextStep);
        }

        private void ApplyFastWalkingOptimized(RoomUser user, ref Vector2D nextStep, ref int stepIndex)
        {
            if (user.IsBot && user.FastWalking && user.BotData != null)
            {
                if (!user.BotData.Name.Contains("#")) return;
                string passengerName = user.BotData.Name.Split('#')[1];
                var client = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(passengerName);
                if (client != null)
                {
                    RoomUser passenger = client.GetRoomUser();
                    if (passenger?.FastWalking == true && passenger.Path != null &&
                        passenger.PathStep < passenger.Path.Count)
                    {
                        int pIdx = (passenger.Path.Count - passenger.PathStep) - 1;
                        if (pIdx >= 0 && pIdx < passenger.Path.Count)
                        {
                            user.PathStep += (stepIndex - pIdx);
                            nextStep = passenger.Path[pIdx];
                            stepIndex = pIdx;
                        }
                    }
                }
                return;
            }

            if (!ShouldApplyFastWalking(user) || stepIndex <= 0) return;

            int skip = GetFastWalkSkipCount(user);
            if (skip <= 0) return;

            int newIdx = stepIndex - skip;
            if (newIdx < 0) newIdx = 0;

            Vector2D skipped = user.Path[newIdx];
            if (skipped.X != nextStep.X || skipped.Y != nextStep.Y)
            {
                user.PathStep += (stepIndex - newIdx);
                nextStep = skipped;
                stepIndex = newIdx;
            }
        }

        private bool ShouldApplyFastWalking(RoomUser user)
        {
            if (user.IsBot) return user.FastWalking;
            var rp = user.GetClient()?.GetRoleplay();
            if (rp == null) return false;
            return user.SuperFastWalking || rp.DrivingCar || rp.HighOffCocaine || rp.HighOffHeroina;
        }

        private int GetFastWalkSkipCount(RoomUser user)
        {
            if (user.IsBot) return 1;
            var rp = user.GetClient()?.GetRoleplay();
            if (rp == null) return 0;
            if (rp.DrivingCar) return Math.Min(rp.FastCarNew, 3);
            if (user.SuperFastWalking || rp.HighOffCocaine) return 2;
            if (rp.HighOffHeroina) return 3;
            return 0;
        }

        private void ExecuteMovementOptimized(RoomUser user, Vector2D nextStep)
        {
            int nextX = nextStep.X;
            int nextY = nextStep.Y;
            if (nextX == user.X && nextY == user.Y) return;

            bool isFinalStep = (user.GoalX == nextX && user.GoalY == nextY);
            bool isDiagonal = (user.X != nextX && user.Y != nextY);
            if (!_room.GetGameMap().IsValidStep(user,
                    new Vector2D(user.X, user.Y), new Vector2D(nextX, nextY),
                    isFinalStep, user.AllowOverride, false, false, isDiagonal)) return;

            double nextZ = _room.GetGameMap().SqAbsoluteHeight(nextX, nextY);

            if (!user.IsBot && (user.isSitting || user.isLying))
            {
                user.Z += 0.35;
                user.isSitting = false;
                user.isLying = false;
                user.UpdateNeeded = true;
            }

            user.Statusses.Remove("lay");
            user.Statusses.Remove("sit");

            if (!user.IsBot && !user.IsPet && user.GetClient() != null)
            {
                var habbo = user.GetClient().GetHabbo();
                if (habbo.IsTeleporting) { habbo.IsTeleporting = false; habbo.TeleporterId = 0; }
                else if (habbo.IsHopping) { habbo.IsHopping = false; habbo.HopperId = 0; }
            }

            string statusValue = $"{nextX},{nextY},{TextHandling.GetString(nextZ)}";

            if (!user.IsBot && user.RidingHorse && !user.IsPet)
            {
                RoomUser horse = GetRoomUserByVirtualId(user.HorseID);
                if (horse != null)
                {
                    horse.SetStatus("mv", statusValue);
                    horse.UpdateNeeded = true;
                }
                user.SetStatus("mv", $"{nextX},{nextY},{TextHandling.GetString(nextZ + 1)}");
            }
            else
            {
                user.SetStatus("mv", statusValue);
            }

            user.UpdateNeeded = true;

            int newRot = Rotation.Calculate(user.X, user.Y, nextX, nextY, user.moonwalkEnabled);
            user.RotBody = newRot;
            user.RotHead = newRot;

            user.SetStep = true;
            user.SetX = nextX;
            user.SetY = nextY;
            user.SetZ = nextZ;

            UpdateUserEffect(user, nextX, nextY);
        }

        private void StopWalking(RoomUser user)
        {
            user.IsWalking = false;
            user.Path?.Clear();
            user.PathStep = 0;

            if (user.Statusses.ContainsKey("mv"))
            {
                user.RemoveStatus("mv");
                user.UpdateNeeded = true;
            }

            user.Statusses.Remove("sign");

            if (user.IsBot && user.BotData?.TargetUser > 0)
            {
                if (user.CarryItemID > 0)
                {
                    RoomUser target = _room.GetRoomUserManager().GetRoomUserByHabbo(user.BotData.TargetUser);
                    if (target != null && Gamemap.TilesTouching(user.X, user.Y, target.X, target.Y))
                    {
                        user.SetRot(Rotation.Calculate(user.X, user.Y, target.X, target.Y), false);
                        target.SetRot(Rotation.Calculate(target.X, target.Y, user.X, user.Y), false);
                        target.CarryItem(user.CarryItemID);
                    }
                }
                user.CarryItem(0);
                user.BotData.TargetUser = 0;
            }

            if (user.RidingHorse && !user.IsPet && !user.IsBot)
            {
                RoomUser horse = GetRoomUserByVirtualId(user.HorseID);
                if (horse != null)
                {
                    horse.IsWalking = false;
                    if (horse.Statusses.ContainsKey("mv"))
                    {
                        horse.RemoveStatus("mv");
                        horse.UpdateNeeded = true;
                    }
                }
            }
        }

        private void CleanupMovementStatus(RoomUser user)
        {
            if (!user.Statusses.ContainsKey("mv")) return;

            user.RemoveStatus("mv");
            user.UpdateNeeded = true;

            if (user.RidingHorse)
            {
                RoomUser horse = GetRoomUserByVirtualId(user.HorseID);
                if (horse?.Statusses.ContainsKey("mv") == true)
                {
                    horse.RemoveStatus("mv");
                    horse.UpdateNeeded = true;
                }
            }
        }

        private void SaveUserCoordinates(RoomUser user)
        {
            var rp = user.GetClient()?.GetRoleplay();
            if (rp != null)
                rp.LastCoordinates = $"{user.X},{user.Y},{user.Z},{user.RotBody}";
        }

        private void RemoveMarkedUsers(List<RoomUser> usersToRemove)
        {
            foreach (var user in usersToRemove)
            {
                var client = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(user.HabboId);
                if (client != null) RemoveUserFromRoom(client, true);
                else RemoveRoomUser(user);
            }
        }

        public void UpdateUserStatus(RoomUser User, bool cyclegameitems)
        {
            if (User == null) return;

            try
            {
                if (User.IsBot) cyclegameitems = false;

                if (User.SignTime <= 0 && User.Statusses.ContainsKey("sign"))
                {
                    User.Statusses.Remove("sign");
                    User.UpdateNeeded = true;
                }

                double newZ;
                List<Item> ItemsOnSquare = _room.GetGameMap().GetAllRoomItemForSquare(User.X, User.Y);

                DynamicRoomModel Model = _room.GetGameMap()?.Model;
                if (Model == null) return;

                // ✅ Validar bounds antes de acceder a los arrays del modelo
                if (User.X < 0 || User.Y < 0 || User.X >= Model.MapSizeX || User.Y >= Model.MapSizeY)
                    return;

                if (ItemsOnSquare != null && ItemsOnSquare.Count != 0)
                {
                    newZ = _room.GetGameMap().SqAbsoluteHeight(User.X, User.Y, ItemsOnSquare)
                           + (User.RidingHorse && !User.IsPet ? 1 : 0);
                }
                else
                    newZ = Model.SqFloorHeight[User.X, User.Y];

                if (newZ != User.Z && !User.IsWalking)
                {
                    if (User.isSitting && User.Statusses.ContainsKey("sit") && User.Statusses["sit"] == "1.0")
                    {
                        // Solo ajustar si es un seat del modelo, no de ítem
                        // Los seats de ítem se manejan en el foreach con Item.GetZ
                        User.Z = newZ - 0.35;
                        User.UpdateNeeded = true;
                    }
                    else if (!User.isSitting && !User.isLying)
                    {
                        User.Z = newZ;
                        User.UpdateNeeded = true;
                    }
                }


                if (Model.SqState[User.X, User.Y] == SquareState.SEAT)
                {
                    if (!User.isSitting || User.Z != Model.SqFloorHeight[User.X, User.Y] || User.RotBody != Model.SqSeatRot[User.X, User.Y] || !User.Statusses.ContainsKey("sit"))
                    {
                        User.Statusses.Remove("sit");
                        User.Statusses.Remove("lay");
                        User.Statusses.Add("sit", "1.0");

                        User.isSitting = true;
                        User.isLying = false;
                        User.Z = Model.SqFloorHeight[User.X, User.Y];
                        User.RotHead = Model.SqSeatRot[User.X, User.Y];
                        User.RotBody = Model.SqSeatRot[User.X, User.Y];
                        User.UpdateNeeded = true;
                    }
                }

                bool foundFurniture = false;
                if (ItemsOnSquare == null || ItemsOnSquare.Count == 0)
                {
                    User.LastItem = null;
                }
                else
                {
                    foreach (Item Item in ItemsOnSquare.ToList())
                    {
                        if (Item == null) continue;

                        if (Item.GetBaseItem().IsSeat)
                        {
                            // Procesar interacción especial del seat si la tiene
                            if (Item.GetBaseItem().InteractionType == InteractionType.CAGAR)
                            {
                                if (User.Coordinate.X == Item.GetX && User.Coordinate.Y == Item.GetY)
                                {
                                    if (User.GetClient()?.GetRoleplay() == null) goto seatDone;

                                    if (User.GetClient().GetRoleplay().Poop >= 100)
                                    {
                                        User.GetClient().SendWhisper("Su vejiga ya está en un máximo de 100", 1);
                                        User.GetClient().GetRoleplay().IsWorking = false;
                                        User.MoveTo(Item.SquareInFront.X, Item.SquareInFront.Y);
                                        return;
                                    }
                                    if (Item.InteractingUser != 0)
                                    {
                                        User.GetClient().SendWhisper("Este toilet ya está en uso por alguien más!", 1);
                                        User.MoveTo(Item.SquareInFront.X, Item.SquareInFront.Y);
                                        return;
                                    }
                                    if (Item.ExtraData == "0" || Item.ExtraData == "")
                                    {
                                        Item.ExtraData = "1";
                                        Item.UpdateState(false, true);
                                        Item.RequestUpdate(1, true);
                                    }
                                    if (Item.ExtraData == "1" && !User.GetClient().GetRoleplay().InCagar)
                                    {
                                        User.ClearMovement(true);
                                        Item.InteractingUser = User.GetClient().GetHabbo().Id;
                                        User.GetClient().GetRoleplay().InCagar = true;
                                        RoleplayManager.Shout(User.GetClient(), "*Comienza a defecar o orinar en el toilet, huele a rayos*", 4);
                                        User.GetClient().GetRoleplay().IsWorking = false;
                                        User.GetClient().GetRoleplay().TimerManager.CreateTimer("cagar", 1000, false, Item.Id);
                                    }
                                }
                            }

                            seatDone:
                            double seatZ = Item.GetZ; // ← Z real del sofá, sin contar cojines
                            if (!User.isSitting || User.Z != seatZ || User.RotBody != Item.Rotation || !User.Statusses.ContainsKey("sit"))
                            {
                                User.Statusses.Remove("sit");
                                User.Statusses.Remove("lay");
                                User.Statusses.Add("sit", TextHandling.GetString(Item.GetBaseItem().Height));
                                User.isSitting = true;
                                User.isLying = false;
                                User.Z = seatZ; // ← solo la Z del sofá
                                User.RotHead = Item.Rotation;
                                User.RotBody = Item.Rotation;
                                User.UpdateNeeded = true;
                            }
                            foundFurniture = true;
                            break;
                        }

                        switch (Item.GetBaseItem().InteractionType)
                        {
                            #region Roleplay

                            #region Shower
                            case InteractionType.SHOWER:
                                {
                                    if (User.Coordinate.X == Item.GetX && User.Coordinate.Y == Item.GetY)
                                    {
                                        if (User.GetClient()?.GetRoleplay() == null) continue;

                                        Room Room;
                                        if (!PolarEnvironment.GetGame().GetRoomManager().TryGetRoom(User.GetClient().GetHabbo().CurrentRoomId, out Room))
                                            return;

                                        if (User.GetClient().GetRoleplay().Hygiene >= 100)
                                        {
                                            User.GetClient().SendWhisper("Su Higiene ya está en un máximo de 100", 1);
                                            User.GetClient().GetRoleplay().IsWorking = false;
                                            User.MoveTo(Item.SquareInFront.X, Item.SquareInFront.Y);
                                            return;
                                        }

                                        if (Item.InteractingUser != 0)
                                        {
                                            User.GetClient().SendWhisper("Esta ducha ya está en uso por alguien más! Lo siento, no puedes unirte a ellos!", 1);
                                            User.MoveTo(Item.SquareInFront.X, Item.SquareInFront.Y);
                                            return;
                                        }

                                        if (Item.ExtraData == "0" || Item.ExtraData == "")
                                        {
                                            Item.ExtraData = "1";
                                            Item.UpdateState(false, true);
                                            Item.RequestUpdate(1, true);
                                        }

                                        if (Item.ExtraData == "1" && !User.GetClient().GetRoleplay().InShower)
                                        {
                                            User.ClearMovement(true);
                                            Item.InteractingUser = User.GetClient().GetHabbo().Id;
                                            User.GetClient().GetRoleplay().InShower = true;
                                            RoleplayManager.Shout(User.GetClient(), "*Comienza a tomar una buena ducha caliente*", 4);
                                            User.GetClient().GetRoleplay().IsWorking = false;
                                            User.GetClient().GetRoleplay().TimerManager.CreateTimer("shower", 1000, false, Item.Id);
                                        }
                                    }
                                    break;
                                }
                            #endregion

                            #region Whisper Tile
                            case InteractionType.WHISPER_TILE:
                                {
                                    if (!User.IsBot && User.Coordinate.X == Item.GetX && User.Coordinate.Y == Item.GetY)
                                    {
                                        if (Item.WhisperTileData == null)
                                        {
                                            User.GetClient().SendWhisper("¡Vaya, parece que los datos de susurros están rotos!", 1);
                                            break;
                                        }

                                        if (!string.IsNullOrEmpty(Item.WhisperTileData.Message) && User.GetClient() != null)
                                            User.GetClient().SendWhisper(Item.WhisperTileData.Message, 34);
                                    }
                                    break;
                                }
                            #endregion

                            #endregion

                            #region Beds & Tents
                            case InteractionType.BED:
                            case InteractionType.BEDEFFECT:
                            case InteractionType.TENT_SMALL:
                                {
                                    if (!User.isLying || User.Z != Item.GetZ || User.RotBody != Item.Rotation || !User.Statusses.ContainsKey("lay"))
                                    {
                                        User.Statusses.Remove("lay");
                                        User.Statusses.Remove("sit");
                                        User.Statusses.Add("lay", TextHandling.GetString(Item.GetBaseItem().Height) + " null");

                                        User.isLying = true;
                                        User.isSitting = false;
                                        User.Z = Item.GetZ;
                                        User.RotHead = Item.Rotation;
                                        User.RotBody = Item.Rotation;
                                        User.UpdateNeeded = true;
                                    }
                                    foundFurniture = true;

                                    if (Item.GetBaseItem().InteractionType == InteractionType.BEDEFFECT && !User.IsBot)
                                    {
                                        if (Item.GetBaseItem().EffectId == 0 && User.GetClient().GetHabbo().Effects().CurrentEffect == 0)
                                            return;

                                        User.GetClient().GetHabbo().Effects().ApplyEffect(Item.GetBaseItem().EffectId);
                                        Item.ExtraData = "1";
                                        Item.UpdateState(false, true);
                                        Item.RequestUpdate(2, true);
                                    }

                                    User.RotHead = Item.Rotation;
                                    User.RotBody = Item.Rotation;
                                    User.UpdateNeeded = true;
                                    break;
                                }
                            #endregion

                            #region Banzai Gates
                            case InteractionType.banzaigategreen:
                            case InteractionType.banzaigateblue:
                            case InteractionType.banzaigatered:
                            case InteractionType.banzaigateyellow:
                                {
                                    if (cyclegameitems)
                                    {
                                        int effectID = Convert.ToInt32(Item.team + 32);
                                        TeamManager t = User.GetClient().GetHabbo().CurrentRoom.GetTeamManagerForBanzai();

                                        if (User.Team == TEAM.NONE)
                                        {
                                            if (t.CanEnterOnTeam(Item.team))
                                            {
                                                if (User.Team != TEAM.NONE) t.OnUserLeave(User);
                                                User.Team = Item.team;
                                                t.AddUser(User);
                                                if (User.GetClient().GetHabbo().Effects().CurrentEffect != effectID)
                                                    User.GetClient().GetHabbo().Effects().ApplyEffect(effectID);
                                            }
                                        }
                                        else if (User.Team != TEAM.NONE && User.Team != Item.team)
                                        {
                                            t.OnUserLeave(User);
                                            User.Team = TEAM.NONE;
                                            User.GetClient().GetHabbo().Effects().ApplyEffect(0);
                                        }
                                        else
                                        {
                                            t.OnUserLeave(User);
                                            if (User.GetClient().GetHabbo().Effects().CurrentEffect == effectID)
                                                User.GetClient().GetHabbo().Effects().ApplyEffect(0);
                                            User.Team = TEAM.NONE;
                                        }
                                    }
                                    break;
                                }
                            #endregion

                            #region Freeze Gates
                            case InteractionType.FREEZE_YELLOW_GATE:
                            case InteractionType.FREEZE_RED_GATE:
                            case InteractionType.FREEZE_GREEN_GATE:
                            case InteractionType.FREEZE_BLUE_GATE:
                                {
                                    if (User.IsBot || User.GetClient()?.GetRoleplay() == null)
                                        break;

                                    if (TexasHoldEmManager.GameList.Count > 0)
                                    {
                                        TexasHoldEm Game = TexasHoldEmManager.GameList.Values
                                            .FirstOrDefault(x => x.JoinGate?.Furni == Item);

                                        if (Game != null)
                                        {
                                            if (Game.GameStarted)
                                                User.GetClient().SendWhisper("Lo siento pero ya hay un juego de Texas Hold 'Em!", 1);
                                            else
                                                Game.AddPlayerToGame(User.GetClient().GetHabbo().Id);
                                            break;
                                        }
                                    }

                                    if (cyclegameitems)
                                    {
                                        int effectID = Convert.ToInt32(Item.team + 39);
                                        TeamManager t = User.GetClient().GetHabbo().CurrentRoom.GetTeamManagerForFreeze();

                                        if (User.Team == TEAM.NONE)
                                        {
                                            if (t.CanEnterOnTeam(Item.team))
                                            {
                                                if (User.Team != TEAM.NONE) t.OnUserLeave(User);
                                                User.Team = Item.team;
                                                t.AddUser(User);
                                                if (User.GetClient().GetHabbo().Effects().CurrentEffect != effectID)
                                                    User.GetClient().GetHabbo().Effects().ApplyEffect(effectID);
                                            }
                                        }
                                        else if (User.Team != TEAM.NONE && User.Team != Item.team)
                                        {
                                            t.OnUserLeave(User);
                                            User.Team = TEAM.NONE;
                                            User.GetClient().GetHabbo().Effects().ApplyEffect(0);
                                        }
                                        else
                                        {
                                            t.OnUserLeave(User);
                                            if (User.GetClient().GetHabbo().Effects().CurrentEffect == effectID)
                                                User.GetClient().GetHabbo().Effects().ApplyEffect(0);
                                            User.Team = TEAM.NONE;
                                        }
                                    }
                                    break;
                                }
                            #endregion

                            #region Banzai Teles
                            case InteractionType.banzaitele:
                                {
                                    if (User.Statusses.ContainsKey("mv"))
                                        _room.GetGameItemHandler().onTeleportRoomUserEnter(User, Item);
                                    break;
                                }
                            #endregion

                            #region Effects
                            case InteractionType.EFFECT:
                                {
                                    if (!User.IsBot && Item?.GetBaseItem() != null &&
                                        User.GetClient()?.GetHabbo()?.Effects() != null)
                                    {
                                        if (Item.GetBaseItem().EffectId == 0 && User.GetClient().GetHabbo().Effects().CurrentEffect == 0)
                                            return;

                                        User.GetClient().GetHabbo().Effects().ApplyEffect(Item.GetBaseItem().EffectId);
                                        Item.ExtraData = "1";
                                        Item.UpdateState(false, true);
                                        Item.RequestUpdate(2, true);
                                    }
                                    break;
                                }
                            #endregion

                            #region Arrows
                            case InteractionType.ARROW:
                                {
                                    if (User.GoalX == Item.GetX && User.GoalY == Item.GetY)
                                    {
                                        if (User.GetClient()?.GetHabbo() == null || User.GetClient().GetHabbo().IsTeleporting)
                                            continue;

                                        Room Room = _room;

                                        if (!User.IsBot)
                                        {
                                            var rp = User.GetClient().GetRoleplay();
                                            if (rp.IsJailed && !Room.IsPrison && !Room.IsPrison2 && !rp.Jailbroken)
                                            {
                                                User.GetClient().SendWhisper("¡No puedes usar flechas para escapar mientras estás encarcelado!", 1);
                                                break;
                                            }
                                            if (rp.IsDead)
                                            {
                                                User.GetClient().SendWhisper("¡No puedes usar flechas mientras estás muerto!", 1);
                                                break;
                                            }

                                            // Finalizar captura de banco/turf si usa flechas
                                            if (rp.BankCapturing || rp.TurfCapturing || rp.ATMRobbery || rp.Robbery)
                                            {
                                                if (rp.BankCapturing) { rp.BankCapturing = false; Room.BankCapturing = false; }
                                                if (rp.TurfCapturing) { rp.TurfCapturing = false; Room.TurfCapturing = false; }
                                                if (rp.ATMRobbery) rp.ATMRobbery = false;
                                                if (rp.Robbery) rp.Robbery = false;

                                                rp.BreakGeneralTimer = true;
                                                rp.TimerManager.EndTimer("bankrob");
                                                rp.TimerManager.EndTimer("turfcapture");
                                                rp.TimerManager.EndTimer("atmrob");

                                                User.GetClient().SendWhisper("¡Has abandonado la zona y la acción ha sido cancelada!", 1);
                                            }
                                            User.ClearMovement(true);
                                        }

                                        if (!ItemTeleporterFinder.IsTeleLinked(Item.Id, Room))
                                        {
                                            User.UnlockWalking();
                                        }
                                        else
                                        {
                                            int LinkedTele = ItemTeleporterFinder.GetLinkedTele(Item.Id, Room);
                                            int TeleRoomId = ItemTeleporterFinder.GetTeleRoomId(LinkedTele, Room);

                                            if (TeleRoomId == Room.RoomId)
                                            {
                                                Item TargetItem = Room.GetRoomItemHandler().GetItem(LinkedTele);
                                                if (TargetItem == null)
                                                {
                                                    User.GetClient()?.SendWhisper("¡Eh, esa flecha no está bien!", 1);
                                                    break;
                                                }

                                                if (User.GetClient()?.GetRoleplay()?.Chofer == true)
                                                {
                                                    foreach (string psjs in User.GetClient().GetRoleplay().Pasajeros
                                                        .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                                                    {
                                                        GameClient PJ = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(psjs);
                                                        if (PJ?.GetRoleplay()?.ChoferName == User.GetClient().GetHabbo().Username)
                                                        {
                                                            Room.GetGameMap().TeleportToItem(PJ.GetRoomUser(), TargetItem);
                                                            PJ.SendMessage(new UserRemoveComposer(PJ.GetRoomUser().VirtualId));
                                                        }
                                                    }
                                                }
                                                Room.GetGameMap().TeleportToItem(User, TargetItem);
                                            }
                                            else
                                            {
                                                if (!User.IsBot && User.GetClient()?.GetHabbo() != null)
                                                {
                                                    if (User.GetClient().GetRoleplay()?.Chofer == true)
                                                    {
                                                        foreach (string psjs in User.GetClient().GetRoleplay().Pasajeros
                                                            .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                                                        {
                                                            GameClient PJ = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(psjs);
                                                            if (PJ?.GetRoleplay()?.ChoferName == User.GetClient().GetHabbo().Username)
                                                            {
                                                                PJ.GetHabbo().IsTeleporting = true;
                                                                PJ.GetHabbo().TeleportingRoomID = TeleRoomId;
                                                                PJ.GetHabbo().TeleporterId = LinkedTele;
                                                                PJ.SendMessage(new UserRemoveComposer(PJ.GetRoomUser().VirtualId));
                                                                RoleplayManager.SendUserNew(PJ, TeleRoomId);
                                                            }
                                                        }
                                                    }
                                                    User.GetClient().GetHabbo().IsTeleporting = true;
                                                    User.GetClient().GetHabbo().TeleportingRoomID = TeleRoomId;
                                                    User.GetClient().GetHabbo().TeleporterId = LinkedTele;
                                                    RoleplayManager.SendUserNew(User.GetClient(), TeleRoomId);
                                                }
                                            }
                                        }
                                    }
                                    break;
                                }
                            #endregion

                            #region Arrows2
                            case InteractionType.ARROW2:
                                {
                                    if (User.GoalX == Item.GetX && User.GoalY == Item.GetY)
                                    {
                                        if (User.GetClient()?.GetHabbo() == null || User.GetClient().GetHabbo().IsTeleporting)
                                            continue;

                                        Room Room = _room;

                                        if (!User.IsBot)
                                        {
                                            var rp = User.GetClient().GetRoleplay();
                                            if (rp.IsJailed && !Room.IsPrison && !Room.IsPrison2 && !rp.Jailbroken)
                                            {
                                                User.GetClient().SendWhisper("¡No puedes usar flechas para escapar mientras estás encarcelado!", 1);
                                                break;
                                            }
                                            if (rp.IsDead)
                                            {
                                                User.GetClient().SendWhisper("¡No puedes usar flechas mientras estás muerto!", 1);
                                                break;
                                            }

                                            // Finalizar captura de banco/turf si usa flechas
                                            if (rp.BankCapturing || rp.TurfCapturing || rp.ATMRobbery || rp.Robbery)
                                            {
                                                if (rp.BankCapturing) { rp.BankCapturing = false; Room.BankCapturing = false; }
                                                if (rp.TurfCapturing) { rp.TurfCapturing = false; Room.TurfCapturing = false; }
                                                if (rp.ATMRobbery) rp.ATMRobbery = false;
                                                if (rp.Robbery) rp.Robbery = false;

                                                rp.BreakGeneralTimer = true;
                                                rp.TimerManager.EndTimer("bankrob");
                                                rp.TimerManager.EndTimer("turfcapture");
                                                rp.TimerManager.EndTimer("atmrob");

                                                User.GetClient().SendWhisper("¡Has abandonado la zona y la acción ha sido cancelada!", 1);
                                            }
                                            User.ClearMovement(true);
                                        }

                                        if (!ItemTeleporterFinder.IsTeleLinked(Item.Id, Room))
                                        {
                                            User.UnlockWalking();
                                        }
                                        else
                                        {
                                            int LinkedTele = ItemTeleporterFinder.GetLinkedTele(Item.Id, Room);
                                            int TeleRoomId = ItemTeleporterFinder.GetTeleRoomId(LinkedTele, Room);

                                            if (TeleRoomId == Room.RoomId)
                                            {
                                                Item TargetItem = Room.GetRoomItemHandler().GetItem(LinkedTele);
                                                if (TargetItem == null)
                                                {
                                                    User.GetClient()?.SendWhisper("¡Eh, esa flecha no está bien!", 1);
                                                    break;
                                                }

                                                if (User.GetClient()?.GetRoleplay()?.Chofer == true)
                                                {
                                                    foreach (string psjs in User.GetClient().GetRoleplay().Pasajeros
                                                        .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                                                    {
                                                        GameClient PJ = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(psjs);
                                                        if (PJ?.GetRoleplay()?.ChoferName == User.GetClient().GetHabbo().Username)
                                                        {
                                                            Room.GetGameMap().TeleportToItem(PJ.GetRoomUser(), TargetItem);
                                                            PJ.SendMessage(new UserRemoveComposer(PJ.GetRoomUser().VirtualId));
                                                        }
                                                    }
                                                }
                                                Room.GetGameMap().TeleportToItem(User, TargetItem);
                                            }
                                            else
                                            {
                                                if (!User.IsBot && User.GetClient()?.GetHabbo() != null)
                                                {
                                                    if (User.GetClient().GetRoleplay()?.Chofer == true)
                                                    {
                                                        foreach (string psjs in User.GetClient().GetRoleplay().Pasajeros
                                                            .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                                                        {
                                                            GameClient PJ = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(psjs);
                                                            if (PJ?.GetRoleplay()?.ChoferName == User.GetClient().GetHabbo().Username)
                                                            {
                                                                PJ.GetHabbo().IsTeleporting = true;
                                                                PJ.GetHabbo().TeleportingRoomID = TeleRoomId;
                                                                PJ.GetHabbo().TeleporterId = LinkedTele;
                                                                PJ.SendMessage(new UserRemoveComposer(PJ.GetRoomUser().VirtualId));
                                                                RoleplayManager.SendUserNew(PJ, TeleRoomId);
                                                            }
                                                        }
                                                    }
                                                    User.GetClient().GetHabbo().IsTeleporting = true;
                                                    User.GetClient().GetHabbo().TeleportingRoomID = TeleRoomId;
                                                    User.GetClient().GetHabbo().TeleporterId = LinkedTele;
                                                    RoleplayManager.SendUserNew2(User.GetClient(), TeleRoomId);
                                                }
                                            }
                                        }
                                    }
                                    break;
                                }
                            #endregion

                            default:
                                break;
                        }
                    }
                }

                if (!foundFurniture && !User.IsWalking)
                {
                    if (User.isSitting && User.Statusses.ContainsKey("sit") && User.Statusses["sit"] != "1.0")
                    {
                        User.isSitting = false;
                        User.Statusses.Remove("sit");
                        User.Z = newZ;
                        User.UpdateNeeded = true;
                    }
                    else if (User.isLying && User.Statusses.ContainsKey("lay") && !User.Statusses["lay"].StartsWith("1.0"))
                    {
                        User.isLying = false;
                        User.Statusses.Remove("lay");
                        User.Z = newZ;
                        User.UpdateNeeded = true;
                    }
                }

                if (User.isSitting && User.TeleportEnabled)
                {
                    User.Z -= 0.35;
                    User.UpdateNeeded = true;
                }

                if (cyclegameitems)
                {
                    if (_room.GotSoccer()) _room.GetSoccer().OnUserWalk(User);
                    if (_room.GotBanzai()) _room.GetBanzai().OnUserWalk(User);
                    if (_room.GotFreeze()) _room.GetFreeze().OnUserWalk(User);
                }
            }
            catch (Exception e)
            {
                Logging.LogException(e.ToString());
            }
        }
        private void UpdateUserEffect(RoomUser User, int x, int y)
        {
            if (User == null || User.IsBot || User.GetClient()?.GetHabbo() == null) return;

            try
            {
                byte effectByte = _room.GetGameMap().EffectMap[x, y];
                if (effectByte > 0)
                {
                    if (User.GetClient().GetHabbo().Effects().CurrentEffect == 0)
                        User.CurrentItemEffect = ItemEffectType.NONE;

                    ItemEffectType type = ByteToItemEffectEnum.Parse(effectByte);
                    if (type == User.CurrentItemEffect) return;

                    switch (type)
                    {
                        case ItemEffectType.Iceskates:
                            User.GetClient().GetHabbo().Effects().ApplyEffect(
                                User.GetClient().GetHabbo().Gender == "M" ? 38 : 39);
                            User.CurrentItemEffect = ItemEffectType.Iceskates;
                            break;
                        case ItemEffectType.Normalskates:
                            User.GetClient().GetHabbo().Effects().ApplyEffect(
                                User.GetClient().GetHabbo().Gender == "M" ? 55 : 56);
                            User.CurrentItemEffect = type;
                            break;
                        case ItemEffectType.SWIM:
                            User.GetClient().GetHabbo().Effects().ApplyEffect(29);
                            User.CurrentItemEffect = type;
                            break;
                        case ItemEffectType.SwimLow:
                            User.GetClient().GetHabbo().Effects().ApplyEffect(30);
                            User.CurrentItemEffect = type;
                            break;
                        case ItemEffectType.SwimHalloween:
                            User.GetClient().GetHabbo().Effects().ApplyEffect(37);
                            User.CurrentItemEffect = type;
                            break;
                        case ItemEffectType.NONE:
                            User.GetClient().GetHabbo().Effects().ApplyEffect(-1);
                            User.CurrentItemEffect = type;
                            break;
                    }
                }
                else if (User.CurrentItemEffect != ItemEffectType.NONE && effectByte == 0)
                {
                    User.GetClient().GetHabbo().Effects().ApplyEffect(-1);
                    User.CurrentItemEffect = ItemEffectType.NONE;
                }
            }
            catch { }
        }

        public int PetCount => petCount;

        public ICollection<RoomUser> GetBotList() => _bots.Values;

        public List<RoomUser> GetUserList()
        {
            if (_users == null) return new List<RoomUser>();
            return _users.Values.ToList();
        }

        public int SquareInFront(int X, int Y, int RotBody, string find)
        {
            if (RotBody == 0) Y--;
            else if (RotBody == 2) X++;
            else if (RotBody == 4) Y++;
            else if (RotBody == 6) X--;
            return find == "x" ? X : Y;
        }
    }
}