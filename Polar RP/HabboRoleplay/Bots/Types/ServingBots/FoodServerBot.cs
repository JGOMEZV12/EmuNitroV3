using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using Polar.Utilities;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Food;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using Polar.Communication.Packets.Outgoing.Notifications;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboRoleplay.Combat;
using Polar.HabboHotel.Quests;
using System.Collections.Concurrent;
using static Polar.HabboRoleplay.Bots.Manager.TimerHandlers.TimerHandlerManager;
using Polar.HabboRoleplay.Bots.Manager.TimerHandlers;
using Polar.HabboRoleplay.Timers.Types;

namespace Polar.HabboRoleplay.Bots.Types
{
    public class FoodServerBot : RoleplayBotAI
    {
        int VirtualId;
        CryptoRandom Rand;
        public bool CheckForOtherWorkers;
        public int OnDutyCheckInterval;
        public int CurOnDutyCheckTime;
        public ConcurrentDictionary<GameClient, ConcurrentDictionary<object, object>> ServingQueue
            = new ConcurrentDictionary<GameClient, ConcurrentDictionary<object, object>>();

        // Cola de pedidos pendientes mientras el bot está ocupado
        private readonly Queue<(Food.Food Food, GameClient Client)> _pendingOrders
            = new Queue<(Food.Food, GameClient)>();

        private const int MaxQueueSize = 5;

        public FoodServerBot(int VirtualId)
        {
            this.OnDuty = true;
            this.CheckForOtherWorkers = true;
            this.CurOnDutyCheckTime = 0;
            this.VirtualId = VirtualId;
            Rand = new CryptoRandom();
        }

        public override void OnDeployed(GameClient Client)   => this.StartActivities();
        public override void OnDeath(GameClient Client)      { }
        public override void OnArrest(GameClient Client)     { }
        public override void OnAttacked(GameClient Client)   { }

        public override void OnUserLeaveRoom(GameClient Client)
        {
            if (!OnDuty) return;
            RemoveFromQueue(Client);
        }

        public override void OnUserEnterRoom(GameClient Client)
        {
            if (!OnDuty) return;
        }

        public override void OnUserUseTeleport(GameClient Client, object[] Params)
        {
            if (!OnDuty || Client == null || Client.GetRoomUser() == null) return;
            if (Client == GetBotRoleplay().UserFollowing || Client == GetBotRoleplay().UserAttacking)
                GetBotRoleplay().StartTeleporting(GetRoomUser(), GetRoom(), Params);
        }

        public override void OnUserSay(RoomUser User, string Message)
        {
            if (!OnDuty) return;
            var client = User.GetClient();
            if (client == null) return;
            HandleRequest(client, Message);
        }

        public override void OnUserShout(RoomUser User, string Message)
        {
            if (!OnDuty) return;
            if (User.GetClient() == null) return;
            HandleRequest(User.GetClient(), Message);
        }

        public override void OnMessaged(GameClient Client, string Message)
        {
            if (!OnDuty) return;
        }

        public override void OnTimerTick()
        {
            IBotHandler ServingHandler;
            if (this.GetBotData().TryGetHandler(Handlers.FOODSERVE, out ServingHandler))
            {
                if (ServingHandler.Active) return;
                ServingHandler.ExecuteHandler(this.ServingQueue);
            }

            // FIX: Procesar la cola desde aquí, en el tick del bot, NO desde dentro
            // del Execute() del ServingTimer. Así el timer anterior ya terminó del todo
            // antes de que creemos el siguiente.
            ProcessNextInQueue();
        }

        /// <summary>
        /// Revisa si el ServingTimer terminó y hay pedidos pendientes.
        /// Se llama desde OnTimerTick, siempre fuera del Execute del timer.
        /// </summary>
        private void ProcessNextInQueue()
        {
            // Solo actuar si el bot está libre
            if (GetBotRoleplay().WalkingToItem) return;
            if (_pendingOrders.Count == 0) return;

            // Verificar que el timer anterior realmente terminó
            // (ya no está en ActiveTimers o su ServeCompleted == true)
            if (GetBotRoleplay().TimerManager.ActiveTimers.TryGetValue("serving", out var existingTimer))
            {
                // Si el timer sigue activo, esperar al siguiente tick
                if (existingTimer is ServingTimer st && !st.ServeCompleted) return;
            }

            // Limpiar entradas inválidas de la cola
            while (_pendingOrders.Count > 0)
            {
                var next = _pendingOrders.Peek();
                bool valid = next.Client != null
                    && !next.Client.LoggingOut
                    && next.Client.GetRoleplay() != null
                    && next.Client.GetRoomUser() != null
                    && next.Client.GetRoleplay().Hunger > 0;

                if (!valid) { _pendingOrders.Dequeue(); continue; }
                break;
            }

            if (_pendingOrders.Count == 0) return;

            var order = _pendingOrders.Dequeue();
            Whisper(order.Client, "¡Es tu turno, " + order.Client.GetHabbo().Username + "!");
            BeginServingFood(order.Food, order.Client);
        }

        public void BeginServingFood(Food.Food Food, GameClient Client)
        {
            if (!OnDuty)                          return;
            if (Client?.GetRoleplay() == null)    return;
            if (Client.GetRoomUser() == null)     return;
            if (Client.LoggingOut)                return;
            if (Client.GetRoleplay().Hunger <= 0) return;

            string RealName = char.ToUpper(Food.Name[0]) + Food.Name.Substring(1);

            var userRoomUser = Client.GetRoomUser();
            var UserPoint    = new System.Drawing.Point(userRoomUser.X, userRoomUser.Y);
            var ServePoint   = GetBestServePoint(userRoomUser, UserPoint);

            if (ServePoint == System.Drawing.Point.Empty)
            {
                Whisper(Client, "No puedo llegar a tu mesa, " + Client.GetHabbo().Username + ". ¡Intenta sentarte en otro lugar!");
                return;
            }

            // Limpiar timer anterior de forma segura antes de crear el nuevo
            if (GetBotRoleplay().TimerManager.ActiveTimers.TryRemove("serving", out var oldTimer))
            {
                try { oldTimer.EndTimer(); } catch { }
            }

            GetBotRoleplay().WalkingToItem = true;
            GetRoomUser().Chat("¡Claro que sí " + Client.GetHabbo().Username + "! Sirvo una porción de " + RealName + ", ya voy.", true);

            object[] Params = { Client, Food, ServePoint, UserPoint, RealName };
            GetRoomUser().MoveTo(ServePoint);
            GetBotRoleplay().TimerManager.CreateTimer("serving", GetBotRoleplay(), 1000, true, Params);
        }

        private System.Drawing.Point GetBestServePoint(RoomUser userRoomUser, System.Drawing.Point userPoint)
        {
            var room    = GetRoom();
            var gameMap = room?.GetGameMap();
            if (gameMap == null) return System.Drawing.Point.Empty;

            var candidates = new System.Drawing.Point[]
            {
                new System.Drawing.Point(userRoomUser.SquareBehind.X, userRoomUser.SquareBehind.Y),
                new System.Drawing.Point(userPoint.X,     userPoint.Y - 1),
                new System.Drawing.Point(userPoint.X,     userPoint.Y + 1),
                new System.Drawing.Point(userPoint.X - 1, userPoint.Y),
                new System.Drawing.Point(userPoint.X + 1, userPoint.Y),
                new System.Drawing.Point(userPoint.X - 1, userPoint.Y - 1),
                new System.Drawing.Point(userPoint.X + 1, userPoint.Y - 1),
                new System.Drawing.Point(userPoint.X - 1, userPoint.Y + 1),
                new System.Drawing.Point(userPoint.X + 1, userPoint.Y + 1),
            };

            foreach (var c in candidates)
            {
                if (c == userPoint) continue;
                if (gameMap.CanWalk(c.X, c.Y, false)) return c;
            }
            return System.Drawing.Point.Empty;
        }

        public override void HandleRequest(GameClient Client, string Message)
        {
            if (!OnDuty) return;
            if (RespondToSpeech(Client, Message)) return;

            string Name     = GetBotRoleplay().Name.ToLower();
            string msgLower = Message.ToLower();

            if (msgLower.Contains("gracias") || msgLower.Contains("thank you") || msgLower.Contains("thanks"))
            {
                GetRoomUser().Chat("¡De nada, " + Client.GetHabbo().Username + "! Fue un placer servirte.", true);
                GoHome();
                return;
            }

            if (msgLower.StartsWith("servir "))
            {
                string[] Parts = Message.Split(' ');
                if (Parts.Length < 2) return;

                if (Client.GetRoleplay().Hunger <= 0)
                {
                    Whisper(Client, "¡No tienes hambre en absoluto, " + Client.GetHabbo().Username + "!");
                    return;
                }

                string DesiredFood = Parts[1].ToLower();
                var Food = FoodManager.GetFoodTwo(DesiredFood);

                if (Food == null)
                {
                    Whisper(Client, "Esa comida no existe. Escribe 'menu' para ver las opciones.");
                    return;
                }

                if (!FoodManager.CanServe(Client.GetRoomUser()))
                {
                    Whisper(Client, "Por favor, siéntate en una mesa vacía para que pueda servirte.");
                    return;
                }

                // Bot libre → servir directamente
                if (!GetBotRoleplay().WalkingToItem)
                {
                    BeginServingFood(Food, Client);
                    return;
                }

                // Bot ocupado → encolar
                if (IsAlreadyQueued(Client))
                {
                    Whisper(Client, "Ya estás en la fila, " + Client.GetHabbo().Username + ". Posición: " + GetQueuePosition(Client) + ".");
                    return;
                }

                if (_pendingOrders.Count >= MaxQueueSize)
                {
                    Whisper(Client, "Lo siento, " + Client.GetHabbo().Username + ", estamos muy ocupados. ¡Inténtalo en un momento!");
                    return;
                }

                _pendingOrders.Enqueue((Food, Client));
                Whisper(Client, "¡Anotado, " + Client.GetHabbo().Username + "! Estás en la fila. Posición: " + _pendingOrders.Count + ".");
                return;
            }

            if (msgLower == Name)
            {
                GetRoomUser().Chat("Hey " + Client.GetHabbo().Username + ", ¿necesitas algo?", true);
                return;
            }

            switch (msgLower)
            {
                case "food":
                case "hunger":
                case "servir":
                case "comida":
                case "menu":
                    PolarEnvironment.GetGame().GetWebEventManager()
                        .ExecuteWebEvent(Client, "event_restaurant", "openfood");
                    break;
            }
        }

        public override void StopActivities()
        {
            if (!OnDuty) return;
            EndTimerSafe("trabajar");
            EndTimerSafe("serving");
            _pendingOrders.Clear();
            GetRoomUser().Chat("He terminado por hoy. ¡Hasta luego!", true);
            OnDuty = false;
            GetBotRoleplay().WalkingToItem = false;
            if (GetBotRoleplay().WorkUniform != "none")
                GetRoom().SendMessage(new UsersComposer(GetRoomUser()));
            Item Item;
            if (GetBotRoleplay().GetStopWorkItem(this.GetRoom(), out Item))
            {
                GetRoomUser().MoveTo(new System.Drawing.Point(Item.GetX, Item.GetY));
                GetBotRoleplay().TimerManager.CreateTimer("notrabajar", GetBotRoleplay(), 10, true, null);
            }
        }

        public override void StartActivities()
        {
            if (OnDuty) return;
            EndTimerSafe("notrabajar");
            GetBotRoleplay().Invisible = false;
            GetRoom().SendMessage(new UsersComposer(GetRoomUser()));
            GetRoomUser().Chat("Bien, ¡hora de volver al trabajo!", true);
            OnDuty = true;
            if (GetBotRoleplay().WorkUniform != "none")
                GetRoom().SendMessage(new UsersComposer(GetRoomUser()));
            Item Item;
            if (GetBotRoleplay().GetStopWorkItem(this.GetRoom(), out Item))
            {
                var ip = new System.Drawing.Point(Item.GetX, Item.GetY);
                if (GetRoomUser().Coordinate == ip)
                {
                    Item.ExtraData = "2";
                    Item.UpdateState(false, true);
                    Item.RequestUpdate(2, true);
                }
            }
            GetRoomUser().MoveTo(new System.Drawing.Point(GetBotRoleplay().oX, GetBotRoleplay().oY));
            GetBotRoleplay().TimerManager.CreateTimer("trabajar", GetBotRoleplay(), 10, true, null);
        }

        // ─── Helpers ──────────────────────────────────────────────────────────

        public void Whisper(GameClient Client, string Message)
        {
            if (GetRoomUser() == null) return;
            Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, Message, 0, 2));
        }

        public void GoHome()
        {
            if (GetRoomUser() == null || GetBotRoleplay() == null) return;
            var home = new System.Drawing.Point(GetBotRoleplay().oX, GetBotRoleplay().oY);
            if (GetRoomUser().Coordinate != home)
                GetRoomUser().MoveTo(home);
        }

        private void RemoveFromQueue(GameClient client)
        {
            var temp = new List<(Food.Food, GameClient)>(_pendingOrders);
            _pendingOrders.Clear();
            foreach (var e in temp)
                if (e.Item2 != client)
                    _pendingOrders.Enqueue(e);
        }

        private bool IsAlreadyQueued(GameClient client)
        {
            foreach (var e in _pendingOrders)
                if (e.Client == client) return true;
            return false;
        }

        private int GetQueuePosition(GameClient client)
        {
            int pos = 1;
            foreach (var e in _pendingOrders)
            {
                if (e.Client == client) return pos;
                pos++;
            }
            return pos;
        }

        private void EndTimerSafe(string key)
        {
            var rp = GetBotRoleplay();
            if (rp?.TimerManager?.ActiveTimers == null) return;
            if (rp.TimerManager.ActiveTimers.TryGetValue(key, out var timer))
                timer.EndTimer();
        }
    }
}