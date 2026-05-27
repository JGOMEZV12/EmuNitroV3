using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Rooms;
using Polar.HabboRoleplay.Bots.Manager.TimerHandlers;
using Polar.HabboRoleplay.Food;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboRoleplay.Timers.Types;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using Polar.Utilities;
using static Polar.HabboRoleplay.Bots.Manager.TimerHandlers.TimerHandlerManager;

namespace Polar.HabboRoleplay.Bots.Types
{
    public class FoodServerBot : RoleplayBotAI
    {
        int VirtualId;
        CryptoRandom Rand;
        public bool CheckForOtherWorkers;
        public int OnDutyCheckInterval;
        public int CurOnDutyCheckTime;

        // FIX: ServingQueue mantenido por compatibilidad con IBotHandler
        public ConcurrentDictionary<GameClient, ConcurrentDictionary<object, object>> ServingQueue
            = new ConcurrentDictionary<GameClient, ConcurrentDictionary<object, object>>();

        // FIX: ConcurrentQueue — se accede desde el tick del bot (ProcessNextInQueue)
        //      y desde OnUserSay/OnUserShout que pueden ejecutarse en otro hilo.
        private readonly ConcurrentQueue<(Food.Food Food, GameClient Client)> _pendingOrders
            = new ConcurrentQueue<(Food.Food, GameClient)>();

        private const int MaxQueueSize = 5;

        // FIX: flag de "estoy sirviendo" separado de WalkingToItem para evitar
        //      que cambios externos en WalkingToItem confundan el estado del bot.
        private volatile bool _isServing = false;

        // FIX: tick de watchdog — si _isServing lleva demasiados ticks sin limpiarse,
        //      se fuerza la liberación del bot.
        private int _servingWatchdogTicks = 0;
        private const int MaxServingTicks = 60; // 60 s a 1 tick/s

        public FoodServerBot(int VirtualId)
        {
            this.OnDuty = true;
            this.CheckForOtherWorkers = true;
            this.CurOnDutyCheckTime = 0;
            this.VirtualId = VirtualId;
            Rand = new CryptoRandom();
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        public override void OnDeployed(GameClient Client) => StartActivities();
        public override void OnDeath(GameClient Client) { }
        public override void OnArrest(GameClient Client) { }
        public override void OnAttacked(GameClient Client) { }

        public override void OnUserLeaveRoom(GameClient Client)
        {
            if (!OnDuty) return;
            // FIX: limpiar al cliente de la cola cuando se va de la sala,
            //      evitando que el bot intente servir a alguien que ya no está.
            RemoveFromQueue(Client);

            // Si el cliente que se fue era a quien le estábamos sirviendo,
            // liberar el bot inmediatamente.
            if (_isServing)
            {
                var rp = GetBotRoleplay();
                if (rp != null)
                {
                    // Comprobar si el timer "serving" apunta a este cliente
                    if (rp.TimerManager.ActiveTimers.TryGetValue("serving", out var timer) &&
                        timer is ServingTimer st && st.Params != null &&
                        st.Params.Length > 0 && st.Params[0] == Client)
                    {
                        AbortCurrentServing("El cliente se ha ido.");
                    }
                }
            }
        }

        public override void OnUserEnterRoom(GameClient Client) { }

        public override void OnUserUseTeleport(GameClient Client, object[] Params)
        {
            if (!OnDuty || Client == null || Client.GetRoomUser() == null) return;
            if (Client == GetBotRoleplay().UserFollowing || Client == GetBotRoleplay().UserAttacking)
                GetBotRoleplay().StartTeleporting(GetRoomUser(), GetRoom(), Params);
        }

        public override void OnUserSay(RoomUser User, string Message)
        {
            if (!OnDuty) return;
            var client = User?.GetClient();
            if (client == null) return;
            HandleRequest(client, Message);
        }

        public override void OnUserShout(RoomUser User, string Message)
        {
            if (!OnDuty) return;
            var client = User?.GetClient();
            if (client == null) return;
            HandleRequest(client, Message);
        }

        public override void OnMessaged(GameClient Client, string Message) { }

        // ── Tick ──────────────────────────────────────────────────────────────────

        public override void OnTimerTick()
        {
            IBotHandler ServingHandler;
            if (GetBotData().TryGetHandler(Handlers.FOODSERVE, out ServingHandler))
            {
                if (ServingHandler.Active) return;
                ServingHandler.ExecuteHandler(ServingQueue);
            }

            // FIX: watchdog — si _isServing lleva demasiado tiempo activo,
            //      probablemente el timer se perdió. Forzar liberación.
            if (_isServing)
            {
                _servingWatchdogTicks++;
                if (_servingWatchdogTicks >= MaxServingTicks)
                {
                    Core.Logging.LogException(
                        $"[FoodServerBot] Watchdog activado en sala {GetRoom()?.Id}. Forzando liberación.");
                    AbortCurrentServing("Tiempo de servicio excedido.");
                }
                // Mientras esté sirviendo, no procesar la cola
                return;
            }

            _servingWatchdogTicks = 0;
            ProcessNextInQueue();
        }

        // ── Cola de pedidos ───────────────────────────────────────────────────────

        /// <summary>
        /// Llamado desde ServingTimer cuando termina (éxito o error).
        /// Libera el flag de "sirviendo" de forma segura.
        /// </summary>
        public void OnServeFinished()
        {
            _isServing = false;
            _servingWatchdogTicks = 0;

            var rp = GetBotRoleplay();
            if (rp != null) rp.WalkingToItem = false;

            GoHome();
        }

        private void ProcessNextInQueue()
        {
            // FIX: descartar entradas inválidas de la cola antes de procesar
            while (_pendingOrders.TryPeek(out var peeked))
            {
                if (IsClientValid(peeked.Client)) break;
                _pendingOrders.TryDequeue(out _);
            }

            if (!_pendingOrders.TryDequeue(out var order)) return;

            Whisper(order.Client, "¡Es tu turno, " + order.Client.GetHabbo().Username + "!");
            BeginServingFood(order.Food, order.Client);
        }

        // ── Servir comida ─────────────────────────────────────────────────────────

        public void BeginServingFood(Food.Food Food, GameClient Client)
        {
            if (!OnDuty) return;
            if (!IsClientValid(Client)) return;
            if (Client.GetRoleplay().Hunger <= 0) return;

            string realName = char.ToUpper(Food.Name[0]) + Food.Name.Substring(1);

            var userRoomUser = Client.GetRoomUser();
            var userPoint = new Point(userRoomUser.X, userRoomUser.Y);
            var servePoint = GetBestServePoint(userRoomUser, userPoint);

            if (servePoint == Point.Empty)
            {
                Whisper(Client, "No puedo llegar a tu mesa, " + Client.GetHabbo().Username +
                                ". ¡Intenta sentarte en otro lugar!");
                return;
            }

            // FIX: marcar _isServing ANTES de crear el timer, no dentro de él,
            //      para que ProcessNextInQueue no encadene otro pedido de inmediato.
            _isServing = true;
            _servingWatchdogTicks = 0;

            var rp = GetBotRoleplay();
            if (rp != null) rp.WalkingToItem = true;

            // Limpiar timer anterior de forma segura
            EndTimerSafe("serving");

            GetRoomUser().Chat(
                "¡Claro que sí " + Client.GetHabbo().Username +
                "! Sirvo una porción de " + realName + ", ya voy.", true);

            GetRoomUser().MoveTo(servePoint);

            object[] Params = { Client, Food, servePoint, userPoint, realName, this };
            rp?.TimerManager.CreateTimer("serving", rp, 1000, true, Params);
        }

        private Point GetBestServePoint(RoomUser userRoomUser, Point userPoint)
        {
            var gameMap = GetRoom()?.GetGameMap();
            if (gameMap == null) return Point.Empty;

            var candidates = new Point[]
            {
                new Point(userRoomUser.SquareBehind.X, userRoomUser.SquareBehind.Y),
                new Point(userPoint.X,     userPoint.Y - 1),
                new Point(userPoint.X,     userPoint.Y + 1),
                new Point(userPoint.X - 1, userPoint.Y),
                new Point(userPoint.X + 1, userPoint.Y),
                new Point(userPoint.X - 1, userPoint.Y - 1),
                new Point(userPoint.X + 1, userPoint.Y - 1),
                new Point(userPoint.X - 1, userPoint.Y + 1),
                new Point(userPoint.X + 1, userPoint.Y + 1),
            };

            foreach (var c in candidates)
            {
                if (c == userPoint) continue;
                if (gameMap.CanWalk(c.X, c.Y, false)) return c;
            }
            return Point.Empty;
        }

        // ── HandleRequest ─────────────────────────────────────────────────────────

        public override void HandleRequest(GameClient Client, string Message)
        {
            if (!OnDuty) return;
            if (RespondToSpeech(Client, Message)) return;

            string name = GetBotRoleplay().Name.ToLower();
            string msgLower = Message.ToLower();

            if (msgLower.Contains("gracias") || msgLower.Contains("thank you") || msgLower.Contains("thanks"))
            {
                GetRoomUser().Chat("¡De nada, " + Client.GetHabbo().Username + "! Fue un placer servirte.", true);
                GoHome();
                return;
            }

            if (msgLower.StartsWith("servir "))
            {
                string[] parts = Message.Split(' ');
                if (parts.Length < 2) return;

                if (!IsClientValid(Client) || Client.GetRoleplay().Hunger <= 0)
                {
                    Whisper(Client, "¡No tienes hambre en absoluto, " + Client.GetHabbo().Username + "!");
                    return;
                }

                string desiredFood = parts[1].ToLower();
                var food = FoodManager.GetFoodTwo(desiredFood);

                if (food == null)
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
                if (!_isServing)
                {
                    BeginServingFood(food, Client);
                    return;
                }

                // Bot ocupado → encolar
                if (IsAlreadyQueued(Client))
                {
                    Whisper(Client, "Ya estás en la fila, " + Client.GetHabbo().Username +
                                    ". Posición: " + GetQueuePosition(Client) + ".");
                    return;
                }

                // FIX: contar el tamaño de ConcurrentQueue de forma segura
                int queueCount = _pendingOrders.Count;
                if (queueCount >= MaxQueueSize)
                {
                    Whisper(Client, "Lo siento, " + Client.GetHabbo().Username +
                                    ", estamos muy ocupados. ¡Inténtalo en un momento!");
                    return;
                }

                _pendingOrders.Enqueue((food, Client));
                Whisper(Client, "¡Anotado, " + Client.GetHabbo().Username +
                                "! Estás en la fila. Posición: " + (_pendingOrders.Count) + ".");
                return;
            }

            if (msgLower == name)
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

        // ── Start / Stop ──────────────────────────────────────────────────────────

        public override void StopActivities()
        {
            if (!OnDuty) return;

            // Vaciar cola antes de apagar
            while (_pendingOrders.TryDequeue(out _)) { }

            EndTimerSafe("trabajar");
            EndTimerSafe("serving");

            _isServing = false;
            _servingWatchdogTicks = 0;

            var rp = GetBotRoleplay();
            if (rp != null) rp.WalkingToItem = false;

            GetRoomUser().Chat("He terminado por hoy. ¡Hasta luego!", true);
            OnDuty = false;

            if (rp?.WorkUniform != "none")
                GetRoom().SendMessage(new UsersComposer(GetRoomUser()));

            if (GetBotRoleplay().GetStopWorkItem(GetRoom(), out Item item))
            {
                GetRoomUser().MoveTo(new Point(item.GetX, item.GetY));
                GetBotRoleplay().TimerManager.CreateTimer("notrabajar", GetBotRoleplay(), 10, true, null);
            }
        }

        public override void StartActivities()
        {
            if (OnDuty) return;

            EndTimerSafe("notrabajar");

            var rp = GetBotRoleplay();
            if (rp != null) rp.Invisible = false;

            GetRoom().SendMessage(new UsersComposer(GetRoomUser()));
            GetRoomUser().Chat("Bien, ¡hora de volver al trabajo!", true);
            OnDuty = true;

            if (rp?.WorkUniform != "none")
                GetRoom().SendMessage(new UsersComposer(GetRoomUser()));

            if (GetBotRoleplay().GetStopWorkItem(GetRoom(), out Item item))
            {
                var ip = new Point(item.GetX, item.GetY);
                if (GetRoomUser().Coordinate == ip)
                {
                    item.ExtraData = "2";
                    item.UpdateState(false, true);
                    item.RequestUpdate(2, true);
                }
            }

            GetRoomUser().MoveTo(new Point(GetBotRoleplay().oX, GetBotRoleplay().oY));
            GetBotRoleplay().TimerManager.CreateTimer("trabajar", GetBotRoleplay(), 10, true, null);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        public void Whisper(GameClient Client, string Message)
        {
            var ru = GetRoomUser();
            if (ru == null) return;
            Client?.SendMessage(new WhisperComposer(ru.VirtualId, Message, 0, 2));
        }

        public void GoHome()
        {
            var ru = GetRoomUser();
            var rp = GetBotRoleplay();
            if (ru == null || rp == null) return;

            var home = new Point(rp.oX, rp.oY);
            if (ru.Coordinate != home)
                ru.MoveTo(home);
        }

        // FIX: abortar el servicio actual de forma centralizada
        private void AbortCurrentServing(string reason = null)
        {
            EndTimerSafe("serving");
            _isServing = false;
            _servingWatchdogTicks = 0;

            var rp = GetBotRoleplay();
            if (rp != null) rp.WalkingToItem = false;

            GoHome();
        }

        private bool IsClientValid(GameClient client) =>
            client != null &&
            !client.LoggingOut &&
            client.GetRoleplay() != null &&
            client.GetRoomUser() != null;

        private void RemoveFromQueue(GameClient client)
        {
            // FIX: ConcurrentQueue no tiene Remove — reconstruir filtrando
            var temp = new List<(Food.Food, GameClient)>();
            while (_pendingOrders.TryDequeue(out var entry))
                if (entry.Client != client) temp.Add(entry);
            foreach (var e in temp)
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
            return -1;
        }

        private void EndTimerSafe(string key)
        {
            var rp = GetBotRoleplay();
            if (rp?.TimerManager?.ActiveTimers == null) return;
            if (rp.TimerManager.ActiveTimers.TryRemove(key, out var timer))
            {
                try { timer.EndTimer(); } catch { }
            }
        }
    }
}