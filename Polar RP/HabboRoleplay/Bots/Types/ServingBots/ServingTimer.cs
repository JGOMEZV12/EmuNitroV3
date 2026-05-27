using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Pathfinding;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Pathfinding;
using Polar.HabboRoleplay.Bots;
using Polar.HabboRoleplay.Bots.Types;
using Polar.HabboRoleplay.Food;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.RoleplayUsers;
using System;
using System.Drawing;

namespace Polar.HabboRoleplay.Timers.Types
{
    public class ServingTimer : BotRoleplayTimer
    {
        // ── Configuración ─────────────────────────────────────────────────────────
        // GraceTicks: ticks que esperamos antes de empezar a verificar posición.
        // Permite que el bot empiece a moverse antes de que el timer compruebe.
        private const int GraceTicks = 6;
        private const int MaxWaitTicks = 25;

        // ── Estado interno ────────────────────────────────────────────────────────
        private int _graceTicks = 0;
        private int _waitTicks = 0;
        private bool _done = false;   // evita que Execute() corra más de una vez tras completarse

        // FIX: propiedad pública para que FoodServerBot.OnTimerTick pueda consultarla
        public bool ServeCompleted { get; private set; } = false;

        public ServingTimer(string Type, RoleplayBot CachedBot, int Time, bool Forever, object[] Params)
            : base(Type, CachedBot, Time, Forever, Params)
        {
            TimeCount = 0;
        }

        // ── Execute (llamado cada tick mientras el timer esté activo) ─────────────
        public override void Execute()
        {
            // FIX: guard al principio — si ya terminamos (éxito o error) no hacer nada.
            //      Evita que un tick tardío vuelva a ejecutar la lógica.
            if (_done) return;

            try
            {
                // ── Validar referencias mínimas del bot ──────────────────────────
                if (base.CachedBot?.DRoomUser == null || base.CachedBot.DRoom == null)
                {
                    Abort(null);
                    return;
                }

                // ── Desempaquetar parámetros ─────────────────────────────────────
                // Params[5] = FoodServerBot (referencia al bot AI para notificar fin)
                GameClient client = (GameClient)Params[0];
                Food.Food food = (Food.Food)Params[1];
                Point servePoint = (Point)Params[2];
                Point userPoint = (Point)Params[3];
                string realName = (string)Params[4];
                FoodServerBot botAI = Params.Length > 5 ? (FoodServerBot)Params[5] : null;

                // ── Validar cliente ───────────────────────────────────────────────
                if (client == null || client.LoggingOut ||
                    client.GetRoleplay() == null || client.GetRoomUser() == null)
                {
                    Abort(null);
                    return;
                }

                // ── Validar que el cliente todavía necesita comida ────────────────
                if (!NeedsFilling(client))
                {
                    Abort(null);
                    return;
                }

                // ── Grace period: dar tiempo al bot para empezar a moverse ────────
                if (_graceTicks < GraceTicks)
                {
                    _graceTicks++;
                    return;
                }

                // ── Verificar que el cliente no se movió ──────────────────────────
                // FIX: antes si el cliente se movía un pixel, se abortaba sin avisar.
                //      Ahora damos un margen de 1 tile de tolerancia.
                var clientCoord = client.GetRoomUser().Coordinate;
                if (Math.Abs(clientCoord.X - userPoint.X) > 1 ||
                    Math.Abs(clientCoord.Y - userPoint.Y) > 1)
                {
                    Abort(client, "¡Te has movido de tu sitio! Pide de nuevo cuando estés sentado.");
                    return;
                }

                // ── Esperar a que el bot llegue al punto de servicio ──────────────
                if (base.CachedBot.DRoomUser.Coordinate != servePoint)
                {
                    _waitTicks++;

                    // Re-enviar movimiento cada 4 ticks para no saturar el pathfinder
                    if (_waitTicks % 4 == 0)
                        base.CachedBot.DRoomUser.MoveTo(servePoint);

                    if (_waitTicks >= MaxWaitTicks)
                        Abort(client, "Lo siento " + client.GetHabbo().Username +
                                      ", no logré llegar. ¡Inténtalo de nuevo!");
                    return;
                }

                // ── El bot llegó — servir ─────────────────────────────────────────
                _done = true;

                // Girar hacia el cliente
                int rot = Rotation.Calculate(
                    base.CachedBot.DRoomUser.Coordinate.X,
                    base.CachedBot.DRoomUser.Coordinate.Y,
                    client.GetRoomUser().Coordinate.X,
                    client.GetRoomUser().Coordinate.Y);
                base.CachedBot.DRoomUser.SetRot(rot, false);

                base.CachedBot.DRoomUser.Chat(
                    "Aquí tienes " + client.GetHabbo().Username +
                    ", espero que disfrutes de tu " + realName + ".", true);

                // Colocar el ítem de comida
                PlaceFoodFurni(food, client);

                // Reacción del cliente
                client.GetRoomUser().OnChat(
                    client.GetRoomUser().LastBubble, "¡Gracias! ", false, string.Empty);

                // FIX: notificar al bot AI que terminamos ANTES de GoHome().
                //      Así FoodServerBot.OnServeFinished limpia WalkingToItem y
                //      lleva al bot a casa de forma controlada.
                ServeCompleted = true;
                botAI?.OnServeFinished();

                // Terminar el timer de forma limpia
                base.EndTimer();
            }
            catch (Exception ex)
            {
                Core.Logging.LogException("[ServingTimer] " + ex);

                // FIX: ante cualquier excepción, garantizar que el bot queda libre
                _done = true;
                ServeCompleted = true;

                FoodServerBot botAI = Params?.Length > 5 ? Params[5] as FoodServerBot : null;
                botAI?.OnServeFinished();

                base.EndTimer();
            }
        }

        // ── Colocar el furni de comida ────────────────────────────────────────────

        private void PlaceFoodFurni(Food.Food food, GameClient client)
        {
            if (client?.GetRoomUser() == null || base.CachedBot.DRoom == null) return;

            var squareInFront = client.GetRoomUser().SquareInFront;

            double maxHeight = 0.0;
            if (base.CachedBot.DRoom.GetGameMap()
                    .GetHighestItemForSquare(squareInFront, out Item topItem) && topItem != null)
                maxHeight = topItem.TotalHeight;

            base.CachedBot.DRoomUser.SetRot(client.GetRoomUser().RotBody, false);

            RoleplayManager.PlaceItemToRoom(
                client, food.ItemId, 0,
                squareInFront.X, squareInFront.Y,
                maxHeight,
                client.GetRoomUser().RotBody,
                false, base.CachedBot.DRoom.Id, false, food.ExtraData, true);
        }

        // ── Abortar (error / cliente inválido) ────────────────────────────────────

        private void Abort(GameClient client, string message = null)
        {
            if (_done) return;
            _done = true;

            if (client != null && message != null)
                Whisper(client, message);

            ServeCompleted = true;

            // Notificar al bot AI para liberar WalkingToItem e ir a casa
            FoodServerBot botAI = Params?.Length > 5 ? Params[5] as FoodServerBot : null;
            botAI?.OnServeFinished();

            base.EndTimer();
        }

        // ── NeedsFilling ──────────────────────────────────────────────────────────

        private bool NeedsFilling(GameClient client)
        {
            var rp = client.GetRoleplay();
            if (base.CachedBot.DRoomUser.GetBotRoleplay().AIType == RoleplayBotAIType.DRINKSERVER)
                return rp.CurEnergy < rp.MaxEnergy || rp.CurAlcohol < rp.MaxAlcohol;
            return rp.Hunger > 0;
        }

        // ── Whisper helper ────────────────────────────────────────────────────────

        private void Whisper(GameClient client, string message)
        {
            if (base.CachedBot?.DRoomUser == null || client == null) return;
            client.SendMessage(new WhisperComposer(
                base.CachedBot.DRoomUser.VirtualId, message, 0, 2));
        }
    }
}