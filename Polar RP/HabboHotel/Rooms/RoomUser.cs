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
        // ────────────────────────────────────────────────
        //  Campos de movimiento avanzado
        // ────────────────────────────────────────────────
        public long LastStepTick = 0;
        public int MsPerStep = 430;

        // ────────────────────────────────────────────────
        //  Campos de estado
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

        public Dictionary<string, string> Statusses;

        public int TeleDelay;
        public bool TeleportEnabled;
        public bool UpdateNeeded;
        public int VirtualId;

        public int X;
        public int Y;
        public double Z;

        // FIX CYCLE-2: tile donde se procesó por última vez el efecto de suelo
        public int LastEffectX = -1;
        public int LastEffectY = -1;

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
        //  Spam — sentinel separado para evitar confusión
        // ────────────────────────────────────────────────
        // FIX SPAM: antes ChatSpamTicks == -1 se usaba como "expirado" Y como "resetear",
        // lo que creaba una lógica circular confusa. Ahora un bool dedicado lo hace explícito.
        private bool _spamWindowExpired = false;

        // ────────────────────────────────────────────────
        //  Offsets de squares relativos (norte/este/sur/oeste)
        // ────────────────────────────────────────────────
        // FIX #7: tabla estática en lugar de 4 métodos con if/else duplicado
        private static readonly (int dx, int dy)[][] _squareOffsets =
        {
            new[] { (0,-1), (0,+1), (+1, 0), (-1, 0) }, // rot=0  norte
            new[] { (+1,0), (-1,0), ( 0,-1), ( 0,+1) }, // rot=2  este
            new[] { (0,+1), (0,-1), (-1, 0), (+1, 0) }, // rot=4  sur
            new[] { (-1,0), (+1,0), ( 0,+1), ( 0,-1) }, // rot=6  oeste
        };

        // FIX DRIVE: array estático en lugar de new[] cada llamada a Split
        private static readonly char[] _pasajeroSep = { ';' };

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
        public bool IsDancing => DanceId >= 1;
        public bool IsBot => BotData != null;

        public int CurrentEffect
        {
            get
            {
                var effects = GetClient()?.GetHabbo()?.Effects();
                return effects?.CurrentEffect ?? 0;
            }
        }

        public bool NeedsAutokick
        {
            get
            {
                if (IsBot) return false;
                if (GetClient()?.GetHabbo() == null) return true;
                if (GetClient().GetHabbo().GetPermissions().HasRight("mod_tool") ||
                    GetRoom()?.OwnerId == HabboId) return false;
                return IdleTime >= 7200;
            }
        }

        public bool IsTrading
        {
            get => _trading;
            set => _trading = value;
        }

        // ────────────────────────────────────────────────
        //  Identificación
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
                var h = GetClient()?.GetHabbo();
                if (h != null) h.TimeAFK = 0;
            }

            IdleTime = 0;
            if (!IsAsleep) return;

            IsAsleep = false;
            GetRoom()?.SendMessage(new SleepComposer(this, false));

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
            LastEffectX = -1;
            LastEffectY = -1;
            mRoom = null;
            mClient = null;
        }

        // ────────────────────────────────────────────────
        //  Chat (bot)
        // ────────────────────────────────────────────────
        public void Chat(string message, bool shout = true, int bubble = 0, string colour = "")
        {
            if (GetRoom() == null || !IsBot) return;

            // FIX CHAT-1: GetUserList() ya devuelve una nueva lista — no hace falta .ToList()
            var userList = GetRoom().GetRoomUserManager()?.GetUserList();
            if (userList == null) return;

            foreach (RoomUser user in userList)
            {
                // FIX CHAT-2: "return" → "continue" para no cortar el bucle al primer null
                if (user == null || user.IsBot) continue;
                if (user.GetClient()?.GetHabbo() == null) continue;

                int effectiveBubble = bubble == 0 ? 2 : bubble;
                if (!shout)
                    user.GetClient().SendMessage(new ChatComposer(VirtualId, message, 0, effectiveBubble, colour));
                else
                    user.GetClient().SendMessage(new ShoutComposer(VirtualId, message, 0, effectiveBubble, colour));
            }
        }

        // ────────────────────────────────────────────────
        //  Spam / Flood
        // ────────────────────────────────────────────────
        public void HandleSpamTicks()
        {
            // FIX SPAM: usar bool explícito en lugar de sentinel -1
            if (_spamWindowExpired) return;

            ChatSpamTicks--;
            if (ChatSpamTicks <= 0)
            {
                ChatSpamTicks = 0;
                _spamWindowExpired = true;
            }
        }

        public bool IncrementAndCheckFlood(out int muteTime, bool isSystemMessage = false)
        {
            muteTime = 0;
            if (isSystemMessage) return false;

            ChatSpamCount++;

            // FIX SPAM: ventana expirada → resetear y permitir
            if (_spamWindowExpired)
            {
                ChatSpamTicks = 8;
                _spamWindowExpired = false;
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

            var habbo = GetClient().GetHabbo();
            string finalMessage = message;

            ServerPacket packet;
            if (habbo.Translating)
            {
                string lg1 = habbo.FromLanguage.ToLower();
                string lg2 = habbo.ToLanguage.ToLower();
                string translated = PolarEnvironment.translate(finalMessage, lg1, lg2)
                                    + $" [{lg1.ToUpper()} -> {lg2.ToUpper()}]";
                int emotion = PolarEnvironment.GetGame().GetChatManager().GetEmotions().GetEmotionsForText(finalMessage);
                packet = shout
                    ? new ShoutComposer(VirtualId, translated, emotion, bubble, colour)
                    : (ServerPacket)new ChatComposer(VirtualId, translated, emotion, bubble, colour);
            }
            else
            {
                int emotion = PolarEnvironment.GetGame().GetChatManager().GetEmotions().GetEmotionsForText(finalMessage);
                packet = shout
                    ? new ShoutComposer(VirtualId, finalMessage, emotion, bubble, colour)
                    : (ServerPacket)new ChatComposer(VirtualId, finalMessage, emotion, bubble, colour);
            }

            var roomUserMgr = mRoom.GetRoomUserManager();
            if (roomUserMgr != null)
            {
                var senderClient = GetClient();
                var rp = senderClient?.GetRoleplay();
                int senderId = senderClient?.GetHabbo()?.Id ?? 0;

                // FIX ONCHAT-1: un solo snapshot para usuarios Y bots — evita dos .ToList()
                var snapshot = roomUserMgr.GetUserList();

                foreach (RoomUser user in snapshot)
                {
                    if (user == null) continue;

                    if (!user.IsBot)
                    {
                        if (user.GetClient()?.GetHabbo() == null) continue;
                        if (senderId > 0 && user.GetClient().GetHabbo().MutedUsers.Contains(senderId)) continue;
                        if (rp?.Invisible == true && user.GetClient().GetRoleplay()?.Invisible != true) continue;
                        user.GetClient().SendMessage(packet);
                    }
                    else
                    {
                        // FIX ONCHAT-2: bots procesados en el mismo bucle
                        if (user.GetBotRoleplayAI() != null)
                            user.GetBotRoleplayAI().OnUserSay(this, message);
                        else if (user.BotAI != null)
                            user.BotAI.OnUserSay(this, message);
                    }
                }
            }
        }

        // ────────────────────────────────────────────────
        //  Colour codes
        // ────────────────────────────────────────────────
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
        //  Name packets
        // ────────────────────────────────────────────────
        public void SendNameColourPacket()
        {
            if (IsBot || GetClient()?.GetHabbo() == null) return;
            GetRoom()?.SendMessage(new UserNameChangeComposer(RoomId, VirtualId, GetClient().GetHabbo().GetDisplayName()));
        }

        public void SendMeCommandPacket()
        {
            if (IsBot || GetClient()?.GetHabbo() == null) return;
            GetRoom()?.SendMessage(new UserNameChangeComposer(RoomId, VirtualId, "*" + GetClient().GetHabbo().GetDisplayName()));
        }

        public void SendNamePacket()
        {
            if (IsBot || GetClient()?.GetHabbo() == null) return;
            GetRoom()?.SendMessage(new UserNameChangeComposer(RoomId, VirtualId, GetClient().GetHabbo().GetDisplayName()));
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

                if (isLying || Statusses.ContainsKey("lay"))
                {
                    var bed = items.FirstOrDefault(x => x?.GetBaseItem().IsBed() == true);
                    if (bed != null && bed.GetX == currentPt.X && bed.GetY == currentPt.Y)
                        return;
                }

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
                AllowOverride = pOverride;
                PathRecalcNeeded = true;
                FreezeInteracting = false;

                GetRoom().SendMessage(GetRoom().GetRoomItemHandler().UpdateUserOnRoller(
                    this, newPt, 0,
                    GetRoom().GetGameMap().SqAbsoluteHeight(pX, pY, items)));

                if (items.Count > 0)
                {
                    // FIX #5: un solo FirstOrDefault en lugar de Where+Count+First
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

            // FIX MOVETO-NULL: GetRoleplay() puede ser null si el cliente se está desconectando
            if (!IsBot)
            {
                var rp = GetClient()?.GetRoleplay();
                if (rp != null &&
                    GetRoom().GetGameMap().SquareHasUsers(pX, pY, true, rp.Invisible) &&
                    !pOverride &&
                    (X != pX || Y != pY))   // FIX LOGIC: AND → OR (basta con que cambié una coordenada)
                    return;
            }

            if (Frozen) return;

            UnIdle();
            GoalX = pX;
            GoalY = pY;
            AllowOverride = pOverride;
            PathRecalcNeeded = true;
            FreezeInteracting = false;
        }

        public void MoveDriving(int pX, int pY, RoomUser chofer)
        {
            UnIdle();
            GoalX = pX;
            GoalY = pY;
            PathRecalcNeeded = true;
            FreezeInteracting = false;

            string pasajeros = chofer.GetClient().GetRoleplay().Pasajeros;

            // FIX DRIVE: Split(char) en lugar de Split(string[]) — evita array allocation
            foreach (string psj in pasajeros.Split(_pasajeroSep, StringSplitOptions.RemoveEmptyEntries))
            {
                GameClient pj = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(psj);
                if (pj?.GetRoomUser() == null) continue;

                var ru = pj.GetRoomUser();
                ru.UnIdle();
                ru.GoalX = pX;
                ru.GoalY = pY;
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

            // FIX SETROT: Math.Sign más claro que diff positivo/negativo manual
            int sign = Math.Sign(RotBody - rotation);
            RotHead = RotBody;

            if (Statusses.ContainsKey("sit") || headOnly)
            {
                if (RotBody == 0 || RotBody == 2 || RotBody == 4 || RotBody == 6)
                {
                    if (sign > 0) RotHead = RotBody - 1;
                    else if (sign < 0) RotHead = RotBody + 1;
                }
            }
            else if (Math.Abs(RotBody - rotation) >= 2)
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

        public void SetStatus(string key, string value = "")
        {
            Statusses[key] = value;
        }

        public void AddStatus(string key, string value)
        {
            Statusses[key] = value;
        }

        public void RemoveStatus(string key) => Statusses.Remove(key);

        // ────────────────────────────────────────────────
        //  Efectos
        // ────────────────────────────────────────────────
        public void ApplyEffect(int effectId)
        {
            // FIX #6: ramas bot/usuario claramente separadas
            if (IsBot)
            {
                mRoom?.SendMessage(new AvatarEffectComposer(VirtualId, effectId));
                return;
            }
            GetClient()?.GetHabbo()?.Effects()?.ApplyEffect(effectId);
        }

        // ────────────────────────────────────────────────
        //  Square helpers
        // ────────────────────────────────────────────────
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

            // FIX #8: cachear resultado tras primera resolución
            if (mClient == null)
                mClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(HabboId);

            return mClient;
        }

        public Room GetRoom()
        {
            // FIX #9: evitar búsqueda si RoomId es 0 (nunca va a encontrar nada)
            if (mRoom == null && RoomId > 0)
                PolarEnvironment.GetGame().GetRoomManager().TryGetRoom(RoomId, out mRoom);

            return mRoom;
        }

        // ────────────────────────────────────────────────
        //  Execute
        // ────────────────────────────────────────────────
        public void Execute(GameClients.GameClient session, Room room, string[] @params)
        {
            if (!int.TryParse(Convert.ToString(@params[1]), out _))
            {
                session.SendWhisper("coloque un item válido.", 1);
                return;
            }

            session.GetRoomUser()?.CarryItem(1014);
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