using Polar.Communication.Packets.Outgoing;
using Polar.Communication.Packets.Outgoing.Rooms.Avatar;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.Core;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Pathfinding;
using Polar.HabboHotel.Rooms.AI;
using Polar.HabboHotel.Rooms.Chat.Commands;
using Polar.HabboHotel.Rooms.Games.Freeze;
using Polar.HabboHotel.Rooms.Games.Teams;
using Polar.HabboRoleplay.Bots;
using Polar.HabboRoleplay.Misc;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Polar.HabboHotel.Rooms
{
    public class RoomUser
    {
        // Agregar estos campos a RoomUser
        public long LastStepTick = 0;      // timestamp del último paso (ms)
        public int MsPerStep = 430;
        // ────────────────────────────────────────────────
        //  Campos de estado del usuario
        // ────────────────────────────────────────────────
        public bool AllowOverride;
        public BotAI BotAI;
        public RoleplayBotAI RPBotAI;
        public int lastpathcount = 0;
        public RoomBot BotData;
        public RoleplayBot RPBotData;
        public string LoaderVideoId;
        public RoomUser Attacker;
        public int boolcount = 0;
        public bool SamePath = false;
        public int StepCount = 0;
        public int PathCounter;
        public bool DiagMove = false;
        public bool UserOnBall = false;
        public bool UserHandlingBall = false;
        public bool CanWalk;
        public int CarryItemID;
        public int CarryTimer;
        public int ChatSpamCount = 0;
        public int ChatSpamTicks = 16;
        public ItemEffectType CurrentItemEffect;
        public int DanceId;
        public bool ConstruitEnable = false;
        public bool ConstruitZMode = false;
        public double ConstruitHeigth = 1.0;
        public bool FastWalking = false;
        public bool SuperFastWalking = false;
        public int FreezeCounter;
        public int FreezeLives;
        public bool Freezed;
        public bool Frozen;
        public int GateId;
        public int GoalX;
        public int GoalY;
        public int HabboId;
        public int HorseID = 0;
        public int IdleTime;
        public bool InteractingGate;
        public int InternalRoomID;
        public bool IsAsleep;
        public bool IsWalking;
        public int LastBubble = 0;
        public double LastInteraction;
        public Item LastItem = null;
        public int LockedTilesCount;
        public int DistancePath = 0;
        public int LastRotBody;

        public List<Vector2D> Path = new List<Vector2D>();
        public bool PathRecalcNeeded = false;
        public int PathStep = 1;
        public Pet PetData;

        public int PrevTime;
        public bool RidingHorse = false;
        public bool RidingCar = false;
        public int RoomId;
        public int RotBody;
        public int RotHead;
        public bool SetStep;
        public int SetX;
        public int SetY;
        public double SetZ;
        public double SignTime;
        public byte SqState;

        // ✅ FIX #1: Dictionary<string,string> no es thread-safe. RoomUser se accede desde
        //   el tick de sala (hilo del ThreadPool), desde handlers de paquetes (otro hilo) y
        //   desde comandos. Aunque en la práctica muchas operaciones se serializan por sala,
        //   los accesos a Statusses desde distintos hilos sin lock pueden causar corrupción.
        //   Se mantiene Dictionary<> por compatibilidad con el resto del codebase, pero se
        //   documenta la limitación. Si se necesita acceso truly concurrente, migrar a
        //   ConcurrentDictionary<string,string>.
        public Dictionary<string, string> Statusses;

        public int TeleDelay;
        public bool TeleportEnabled;
        public bool UpdateNeeded;
        public int VirtualId;

        // ✅ FIX #2: CheckBoosting se instanciaba como campo pero NUNCA se usaba en ningún
        //   método del archivo. Se elimina para evitar allocación innecesaria en cada RoomUser.
        //   Si se necesita en el futuro, añadir como propiedad lazy o inyectarlo.
        // CheckBoosting BoostingCheck = new CheckBoosting();  ← ELIMINADO

        public int X;
        public int Y;
        public double Z;

        public FreezePowerUp banzaiPowerUp;
        public bool isLying = false;
        public bool isSitting = false;
        private GameClient mClient;
        public Room mRoom;
        public bool moonwalkEnabled = false;
        public bool shieldActive;
        public int shieldCounter;
        public TEAM Team;
        public bool FreezeInteracting;
        public int UserId;
        public bool IsJumping;
        public bool ReverseWalk;
        public bool WalkSpeed;
        public bool isRolling = false;
        public int rollerDelay = 0;
        public bool IsDispose;
        public int LLPartner = 0;
        public double TimeInRoom = 0;
        public bool ForceSit = false;
        public bool ForceLay = false;

        private bool _trading = false;

        // ────────────────────────────────────────────────
        //  Constructor
        // ────────────────────────────────────────────────
        public RoomUser(int habboId, int roomId, int virtualId, Room room)
        {
            Freezed = false;
            HabboId = habboId;
            RoomId = roomId;
            VirtualId = virtualId;
            IdleTime = 0;
            X = Y = 0;
            Z = 0;
            PrevTime = 0;
            RotHead = RotBody = 0;
            UpdateNeeded = true;
            Statusses = new Dictionary<string, string>();
            TeleDelay = -1;
            mRoom = room;
            AllowOverride = false;
            CanWalk = true;
            SqState = 3;
            InternalRoomID = 0;
            CurrentItemEffect = ItemEffectType.NONE;
            IsDispose = false;
            FreezeLives = 0;
            InteractingGate = false;
            GateId = 0;
            LastInteraction = 0;
            LockedTilesCount = 0;
            IsJumping = false;
            TimeInRoom = 0;
            _trading = false;
        }

        // ────────────────────────────────────────────────
        //  Propiedades
        // ────────────────────────────────────────────────
        public bool IsRoleplayBot => RPBotData != null && IsBot;

        public Point Coordinate => new Point(X, Y);

        public bool IsPet => IsBot && BotData.IsPet;

        public int CurrentEffect
        {
            get
            {
                var effects = GetClient()?.GetHabbo()?.Effects();
                return effects?.CurrentEffect ?? 0;
            }
        }

        public bool IsDancing => DanceId >= 1;

        public bool NeedsAutokick
        {
            get
            {
                if (IsBot) return false;
                if (GetClient()?.GetHabbo() == null) return true;
                if (GetClient().GetHabbo().GetPermissions().HasRight("mod_tool") ||
                    GetRoom().OwnerId == HabboId) return false;
                return IdleTime >= 7200;
            }
        }

        public bool IsTrading
        {
            get => _trading;
            set => _trading = value;
        }

        public bool IsBot => BotData != null;

        // ────────────────────────────────────────────────
        //  Métodos de identificación
        // ────────────────────────────────────────────────
        public string GetUsername()
        {
            if (IsBot) return string.Empty;
            return GetClient()?.GetHabbo()?.Username
                   ?? PolarEnvironment.GetUsernameById(HabboId);
        }

        public RoleplayBot GetBotRoleplay() => IsBot ? RPBotData : null;
        public RoleplayBotAI GetBotRoleplayAI() => IsBot ? RPBotAI : null;

        // ────────────────────────────────────────────────
        //  Idle / Dispose
        // ────────────────────────────────────────────────
        public void UnIdle(bool forcedWakeup = false)
        {
            if (!IsBot)
            {
                if (GetClient() != null && GetClient().GetHabbo() != null)
                    GetClient().GetHabbo().TimeAFK = 0;
            }

            IdleTime = 0;

            if (!IsAsleep) return;

            IsAsleep = false;
            GetRoom().SendMessage(new SleepComposer(this, false));

            var rp = GetClient()?.GetRoleplay();
            if (rp != null && !rp.IsJailed && !rp.IsDead)
            {
                RoleplayManager.GetLookAndMotto(GetClient(), "poof");
                GetClient().GetHabbo().Poof(true);
            }
        }

        public void Dispose()
        {
            Statusses.Clear();
            IsDispose = true;
            mRoom = null;
            mClient = null;
        }

        // ────────────────────────────────────────────────
        //  Chat
        // ────────────────────────────────────────────────
        public void Chat(string message, bool shout = true, int bubble = 0, string colour = "")
        {
            if (GetRoom() == null) return;
            if (!IsBot) return;

            var userList = GetRoom().GetRoomUserManager()?.GetUserList();
            if (userList == null) return;

            foreach (RoomUser user in userList.ToList())
            {
                // ✅ FIX #3: Antes se usaba "return" dentro del foreach en lugar de "continue".
                //   "return" sale del método completo al encontrar el primer usuario nulo o
                //   con GetHabbo()==null, dejando de notificar a todos los demás usuarios.
                //   "continue" salta sólo ese usuario y procesa los restantes.
                if (user == null || user.IsBot) continue;
                if (user.GetClient()?.GetHabbo() == null) continue;

                /*if (IsPet)
                 {
                     //if (!user.GetClient().GetHabbo().AllowPetSpeech) continue;
                     user.GetClient().SendMessage(new ChatComposer(VirtualId, message, 0, 0, string.Empty));
                 }
                 else
                 {*/
                int effectiveBubble = bubble == 0 ? 2 : bubble;
                if (!shout)
                    user.GetClient().SendMessage(new ChatComposer(VirtualId, message, 0, effectiveBubble, colour));
                else
                    user.GetClient().SendMessage(new ShoutComposer(VirtualId, message, 0, effectiveBubble, colour));
                //}
            }
        }

        // ────────────────────────────────────────────────
        //  Spam / Flood
        // ────────────────────────────────────────────────
        public void HandleSpamTicks()
        {
            if (ChatSpamTicks < 0) return;

            ChatSpamTicks--;
            if (ChatSpamTicks == -1)
                ChatSpamCount = 0;
        }

        public bool IncrementAndCheckFlood(out int muteTime, bool isSystemMessage = false)
        {
            muteTime = 0;
            if (isSystemMessage) return false;
            ChatSpamCount++;

            if (ChatSpamTicks == -1)
            {
                ChatSpamTicks = 8;
                return false;
            }

            if (ChatSpamCount < 6) return false;

            var perms = GetClient().GetHabbo().GetPermissions();
            if (perms.HasRight("events_staff")) muteTime = 3;
            else if (perms.HasRight("gold_vip")) muteTime = 7;
            else if (perms.HasRight("silver_vip")) muteTime = 10;
            else muteTime = 15;

            GetClient().GetHabbo().FloodTime = PolarEnvironment.GetUnixTimestamp() + muteTime;
            ChatSpamCount = 0;
            return true;
        }

        // ────────────────────────────────────────────────
        //  OnChat
        // ────────────────────────────────────────────────
        public void OnChat(int bubble, string message, bool shout, string colour, bool isSystemMessage = false)
        {
            if (GetClient()?.GetHabbo() == null || mRoom == null || message == null)
                return;

            if (!isSystemMessage && mRoom.GetWired() != null)
            {
                if (mRoom.GetWired().TriggerEvent(Items.Wired.WiredBoxType.TriggerUserSays, GetClient().GetHabbo(), message))
                { ChatSpamCount = 0; return; }

                if (mRoom.GetWired().TriggerEvent(Items.Wired.WiredBoxType.TriggerUserSaysCommand, GetClient().GetHabbo(), message))
                { ChatSpamCount = 0; return; }
            }

            if (mRoom.WordFilterList.Count > 0 &&
                mRoom.GetFilter() != null &&
                !GetClient().GetHabbo().GetPermissions().HasRight("word_filter_override"))
            {
                message = mRoom.GetFilter().CheckMessage(message);
            }

            GetClient().GetHabbo().HasSpoken = true;

            ServerPacket packet;
            var habbo = GetClient().GetHabbo();

            string finalMessage = message;

            if (habbo.Translating)
            {
                string lg1 = habbo.FromLanguage.ToLower();
                string lg2 = habbo.ToLanguage.ToLower();
                string translated = PolarEnvironment.translate(finalMessage, lg1, lg2)
                                    + $" [{lg1.ToUpper()} -> {lg2.ToUpper()}]";
                int emotion = PolarEnvironment.GetGame().GetChatManager().GetEmotions().GetEmotionsForText(finalMessage);
                //GetRoom().SendMessage(new UserNameChangeComposer(habbo, true));
                packet = shout
                    ? new ShoutComposer(VirtualId, translated, emotion, bubble, colour)
                    : (ServerPacket)new ChatComposer(VirtualId, translated, emotion, bubble, colour);
                //GetRoom().SendMessage(new UserNameChangeComposer(habbo, true));
            }
            else
            {
                //GetRoom().SendMessage(new UserNameChangeComposer(habbo, true));
                int emotion = PolarEnvironment.GetGame().GetChatManager().GetEmotions().GetEmotionsForText(finalMessage);
                packet = shout
                    ? new ShoutComposer(VirtualId, finalMessage, emotion, bubble, colour)
                    : (ServerPacket)new ChatComposer(VirtualId, finalMessage, emotion, bubble, colour);
                //GetRoom().SendMessage(new UserNameChangeComposer(habbo, true));
            }

            var roomUserMgr = mRoom.GetRoomUserManager();
            if (roomUserMgr != null)
            {
                var senderClient = GetClient(); // ✅ FIX: usar GetClient() en lugar del campo mClient directamente
                var rp = senderClient?.GetRoleplay();
                int senderId = senderClient?.GetHabbo()?.Id ?? 0;

                //GetRoom().SendMessage(new UserNameChangeComposer(habbo, true));
                foreach (RoomUser user in roomUserMgr.GetRoomUsers().ToList())
                {
                    if (user?.GetClient()?.GetHabbo() == null) continue;

                    // ✅ FIX: mClient podía ser null — ahora usamos senderId resuelto de forma segura arriba
                    if (senderId > 0 && user.GetClient().GetHabbo().MutedUsers.Contains(senderId)) continue;

                    if (rp?.Invisible == true && user.GetClient().GetRoleplay()?.Invisible != true)
                        continue;

                    user.GetClient().SendMessage(packet);
                }
                //GetRoom().SendMessage(new UserNameChangeComposer(habbo, true));
            }


            // Respuestas de bots
            foreach (RoomUser user in mRoom.GetRoomUserManager().GetUserList().ToList())
            {
                if (!user.IsBot) continue;

                if (user.GetBotRoleplayAI() != null)
                    user.GetBotRoleplayAI().OnUserSay(this, message);
                else if (user.BotAI != null) // ✅ FIX: BotAI podía ser null si el bot no tiene AI asignada
                    user.BotAI.OnUserSay(this, message);
            }
        }

        // ────────────────────────────────────────────────
        //  Colour codes
        // ────────────────────────────────────────────────
        // ✅ FIX #4: UsingColourCode y ReplaceColourCode usaban Split(' ')[0] y luego
        //   múltiples Contains separados. Simplificado con un array de códigos.
        private static readonly string[] _colourCodes =
            { "@red@", "@blue@", "@purple@", "@green@", "@cyan@" };

        public bool UsingColourCode(string message)
        {
            string first = message.Split(' ')[0].ToLower();
            return _colourCodes.Any(c => first.Contains(c));
        }

        public string ReplaceColourCode(string message)
        {
            string first = message.Split(' ')[0].ToLower();
            foreach (string code in _colourCodes)
                if (first.Contains(code))
                    return message.Replace(code, string.Empty);
            return message;
        }

        // ────────────────────────────────────────────────
        //  Name packets (VIP)
        // ────────────────────────────────────────────────

        public void SendNameColourPacket()
        {
            if (IsBot || GetClient()?.GetHabbo() == null) return;
            var habbo = GetClient().GetHabbo();
            GetRoom()?.SendMessage(new UserNameChangeComposer(RoomId, VirtualId, habbo.GetDisplayName()));
        }

        public void SendMeCommandPacket()
        {
            if (IsBot || GetClient()?.GetHabbo() == null) return;
            var habbo = GetClient().GetHabbo();
            GetRoom()?.SendMessage(new UserNameChangeComposer(RoomId, VirtualId, "*" + habbo.GetDisplayName()));
        }

        public void SendNamePacket()
        {
            if (IsBot || GetClient()?.GetHabbo() == null) return;
            var habbo = GetClient().GetHabbo();
            GetRoom()?.SendMessage(new UserNameChangeComposer(RoomId, VirtualId, habbo.GetDisplayName()));
        }

        // ────────────────────────────────────────────────
        //  Movimiento
        // ────────────────────────────────────────────────
        public void ClearMovement(bool update)
        {
            IsWalking = false;
            Statusses.Remove("mv");
            GoalX = GoalY = 0;
            SetStep = false;
            SetX = SetY = 0;
            SetZ = 0;
            PathCounter = 0;
            if (update) UpdateNeeded = true;
        }

        public void MoveTo(Point c) => MoveTo(c.X, c.Y);
        public void MoveTo(int pX, int pY) => MoveTo(pX, pY, false);

        public void MoveTo(int pX, int pY, bool pOverride)
        {
            if (ForceLay || ForceSit) return;

            if (TeleportEnabled)
            {
                var currentPt = new Point(X, Y);
                var newPt = new Point(pX, pY);
                if (currentPt == newPt) return;

                List<Item> items = GetRoom().GetGameMap().GetAllRoomItemForSquare(pX, pY);

                // Bloquear salida de cama si el destino no es otra cama
                if (isLying || Statusses.ContainsKey("lay"))
                {
                    var bed = items.FirstOrDefault(x => x?.GetBaseItem().IsBed() == true);
                    if (bed != null && bed.GetX == currentPt.X && bed.GetY == currentPt.Y)
                        return;
                }

                // Limpiar estados de sit/lay antes de moverse
                if (isSitting || Statusses.ContainsKey("sit"))
                {
                    RemoveStatus("sit");
                    isSitting = false;
                }
                if (isLying || Statusses.ContainsKey("lay"))
                {
                    RemoveStatus("lay");
                    isLying = false;
                }

                UnIdle();
                GoalX = pX;
                GoalY = pY;
                this.AllowOverride = pOverride;
                PathRecalcNeeded = true;
                FreezeInteracting = false;

                GetRoom().SendMessage(GetRoom().GetRoomItemHandler().UpdateUserOnRoller(
                    this, newPt, 0,
                    GetRoom().GetGameMap().SqAbsoluteHeight(pX, pY, items)));

                if (items.Count > 0)
                {
                    // ✅ FIX #5: Antes se llamaba a .Where().Count() > 0 para verificar
                    //   y luego .Where().First() para obtener — doble scan.
                    //   Reemplazado con FirstOrDefault en una sola pasada.
                    var bed = items.FirstOrDefault(x => x?.GetBaseItem().IsBed() == true);
                    var chair = items.FirstOrDefault(x => x?.GetBaseItem().IsSeat == true);

                    if (bed != null)
                    {
                        Statusses.Add("lay", Utilities.TextHandling.GetString(bed.GetBaseItem().Height) + " null");
                        X = bed.GetX; Y = bed.GetY; Z = bed.GetZ;
                        RotHead = RotBody = bed.Rotation;
                        GetRoom().GetGameMap().UpdateUserMovement(currentPt, new Point(bed.GetX, bed.GetY), this);
                    }
                    else if (chair != null)
                    {
                        Statusses.Add("sit", Utilities.TextHandling.GetString(chair.GetBaseItem().Height));
                        Z = chair.GetZ;
                        RotHead = RotBody = chair.Rotation;
                    }
                }

                UpdateNeeded = true;
                return;
            }

            if (!IsBot &&
                GetRoom().GetGameMap().SquareHasUsers(pX, pY, true, GetClient().GetRoleplay().Invisible) &&
                !pOverride &&
                (X != pX && Y != pY))
                return;

            if (Frozen) return;

            UnIdle();
            GoalX = pX;
            GoalY = pY;
            this.AllowOverride = pOverride;
            PathRecalcNeeded = true;
            FreezeInteracting = false;
        }

        public void MoveDriving(int pX, int pY, RoomUser chofer)
        {
            UnIdle();
            GoalX = pX; GoalY = pY;
            PathRecalcNeeded = true;
            FreezeInteracting = false;

            string pasajeros = chofer.GetClient().GetRoleplay().Pasajeros;
            foreach (string psj in pasajeros.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries))
            {
                GameClient pj = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(psj);
                if (pj?.GetRoomUser() == null) continue;

                var ru = pj.GetRoomUser();
                ru.UnIdle();
                ru.GoalX = pX; ru.GoalY = pY;
                ru.PathRecalcNeeded = true;
                ru.FreezeInteracting = false;
            }
        }

        public void UnlockWalking()
        {
            AllowOverride = false;
            CanWalk = true;
        }

        public void SetPos(int pX, int pY, double pZ)
        {
            X = pX; Y = pY; Z = pZ;
        }

        public void CarryItem(int item)
        {
            CarryItemID = item;
            CarryTimer = item > 0 ? 240 : 0;
            GetRoom().SendMessage(new CarryObjectComposer(VirtualId, item));
        }

        public void SetRot(int rotation, bool headOnly)
        {
            if (Statusses.ContainsKey("lay") || IsWalking) return;

            int diff = RotBody - rotation;
            RotHead = RotBody;

            if (Statusses.ContainsKey("sit") || headOnly)
            {
                if (RotBody == 2 || RotBody == 4 || RotBody == 0 || RotBody == 6)
                {
                    if (diff > 0) RotHead = RotBody - 1;
                    else if (diff < 0) RotHead = RotBody + 1;
                }
            }
            else if (diff <= -2 || diff >= 2)
            {
                RotHead = RotBody = rotation;
            }
            else
            {
                RotHead = rotation;
            }

            UpdateNeeded = true;
        }

        // ────────────────────────────────────────────────
        //  Status helpers
        // ────────────────────────────────────────────────
        public bool HasStatus(string key) => Statusses.ContainsKey(key);

        public void SetStatus(string Key, string Value = "")
        {
            if (Statusses.ContainsKey(Key))
            {
                Statusses[Key] = Value;
            }
            else
            {
                AddStatus(Key, Value);
            }
        }

        public void AddStatus(string Key, string Value)
        {
            Statusses[Key] = Value;
        }

        public void RemoveStatus(string key) => Statusses.Remove(key);

        // ────────────────────────────────────────────────
        //  Efectos
        // ────────────────────────────────────────────────
        public void ApplyEffect(int effectId)
        {
            // ✅ FIX #6: Había dos guards: primero "if (IsBot)" con SendMessage, luego
            //   "if (IsBot || ...)" que nunca se alcanzaba si era bot (ya habría retornado).
            //   Reestructurado para que la lógica de bot y de usuario sean ramas claras.
            if (IsBot)
            {
                mRoom.SendMessage(new AvatarEffectComposer(VirtualId, effectId));
                return;
            }

            GetClient()?.GetHabbo()?.Effects()?.ApplyEffect(effectId);
        }

        // ────────────────────────────────────────────────
        //  Squares helpers
        // ────────────────────────────────────────────────
        // ✅ FIX #7: Las cuatro propiedades SquareInFront/Behind/Left/Right tenían exactamente
        //   la misma estructura if/else if por rotación. Extraída a un método privado genérico
        //   con offsets parametrizados — elimina ~80 líneas de código duplicado.
        //   Tabla de offsets por rotación (índice = rotación / 2):
        //     rot=0 (norte):  front=(0,-1), behind=(0,+1), left=(+1,0), right=(-1,0)
        //     rot=2 (este):   front=(+1,0), behind=(-1,0), left=(0,-1), right=(0,+1)
        //     rot=4 (sur):    front=(0,+1), behind=(0,-1), left=(-1,0), right=(+1,0)
        //     rot=6 (oeste):  front=(-1,0), behind=(+1,0), left=(0,+1), right=(0,-1)
        private static readonly (int dx, int dy)[][] _squareOffsets =
        {
            // índice 0 → rot=0, 1 → rot=2, 2 → rot=4, 3 → rot=6
            // orden: [front, behind, left, right]
            new[] { (0,-1), (0,+1), (+1, 0), (-1, 0) }, // norte
            new[] { (+1,0), (-1,0), ( 0,-1), ( 0,+1) }, // este
            new[] { (0,+1), (0,-1), (-1, 0), (+1, 0) }, // sur
            new[] { (-1,0), (+1,0), ( 0,+1), ( 0,-1) }, // oeste
        };

        private Point GetRelativeSquare(int offsetIndex)
        {
            int rotIdx = (RotBody / 2) % 4;
            var (dx, dy) = _squareOffsets[rotIdx][offsetIndex];
            return new Point(X + dx, Y + dy);
        }

        public Point SquareInFront => GetRelativeSquare(0);
        public Point SquareBehind => GetRelativeSquare(1);
        public Point SquareLeft => GetRelativeSquare(2);
        public Point SquareRight => GetRelativeSquare(3);

        public Point GetUniqueSpot(int spot) => spot switch
        {
            1 => SquareBehind,
            2 => SquareInFront,
            3 => SquareRight,
            4 => SquareLeft,
            _ => new Point(0, 0)
        };

        // ────────────────────────────────────────────────
        //  Client / Room accessors
        // ────────────────────────────────────────────────
        public GameClient GetClient()
        {
            if (IsBot) return null;

            // ✅ FIX #8: mClient se guarda en cache tras la primera resolución.
            //   Antes se volvía a buscar por UserID en cada llamada si mClient era null,
            //   pero nunca se asignaba el resultado — lo que hacía que cada llamada
            //   con mClient==null fuera O(n) en el ClientManager.
            //   Ahora se asigna correctamente tras resolver.
            if (mClient == null)
                mClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(HabboId);

            return mClient;
        }

        public Room GetRoom()
        {
            // ✅ FIX #9: Antes: if (mRoom == null) { if (TryGetRoom(...)) return mRoom; } return mRoom;
            //   Si mRoom es null y TryGetRoom falla, devolvía null sin asignar.
            //   Si TryGetRoom tiene éxito, mRoom queda asignado por ref y se devuelve correctamente.
            //   Simplificado — TryGetRoom asigna mRoom vía out, luego se retorna mRoom (null o no).
            if (mRoom == null)
                PolarEnvironment.GetGame().GetRoomManager().TryGetRoom(RoomId, out mRoom);

            return mRoom;
        }

        // ────────────────────────────────────────────────
        //  Execute (comando de ítem)
        // ────────────────────────────────────────────────
        public void Execute(GameClients.GameClient session, Room room, string[] @params)
        {
            if (!int.TryParse(Convert.ToString(@params[1]), out _))
            {
                session.SendWhisper("coloque un item válido.", 1);
                return;
            }

            RoomUser user = session.GetRoomUser();
            user?.CarryItem(1014);
        }
    }

    // ────────────────────────────────────────────────────
    //  Enums y helpers
    // ────────────────────────────────────────────────────
    public enum ItemEffectType
    {
        NONE,
        SWIM,
        SwimLow,
        SwimHalloween,
        Iceskates,
        Normalskates,
        PublicPool,
    }

    public static class ByteToItemEffectEnum
    {
        public static ItemEffectType Parse(byte b) => b switch
        {
            1 => ItemEffectType.SWIM,
            2 => ItemEffectType.Normalskates,
            3 => ItemEffectType.Iceskates,
            4 => ItemEffectType.SwimLow,
            5 => ItemEffectType.SwimHalloween,
            6 => ItemEffectType.PublicPool,
            _ => ItemEffectType.NONE,
        };
    }
}