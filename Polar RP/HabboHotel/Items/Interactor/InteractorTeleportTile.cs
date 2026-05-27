using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Interactor
{
    /// <summary>
    /// Teleport tile — walkable surface that triggers teleportation when stepped on.
    /// Mirrors Java InteractionTeleportTile (extends InteractionTeleport).
    /// OnPlace / OnRemove / OnTrigger idénticos al teleport normal.
    /// La diferencia está en OnWalk: se activa al pisar, sin necesidad de click.
    /// </summary>
    public class InteractorTeleportTile : IFurniInteractor
    {
        // ── OnPlace / OnRemove / OnTrigger ────────────────────────────────────────
        // Idénticos al InteractorTeleport estándar — reutilizamos la misma lógica.

        public void OnPlace(GameClient session, Item item)
        {
            item.ExtraData = "0";

            if (item.InteractingUser != 0)
            {
                RoomUser user = item.GetRoom()?.GetRoomUserManager()
                                    .GetRoomUserByHabbo(item.InteractingUser);
                if (user != null)
                {
                    user.ClearMovement(true);
                    user.AllowOverride = false;
                    user.CanWalk = true;
                }
                item.InteractingUser = 0;
            }

            if (item.InteractingUser2 != 0)
            {
                RoomUser user = item.GetRoom()?.GetRoomUserManager()
                                    .GetRoomUserByHabbo(item.InteractingUser2);
                if (user != null)
                {
                    user.ClearMovement(true);
                    user.AllowOverride = false;
                    user.CanWalk = true;
                }
                item.InteractingUser2 = 0;
            }
        }

        public void OnRemove(GameClient session, Item item)
        {
            item.ExtraData = "0";

            if (item.InteractingUser != 0)
            {
                RoomUser user = item.GetRoom()?.GetRoomUserManager()
                                    .GetRoomUserByHabbo(item.InteractingUser);
                user?.UnlockWalking();
                item.InteractingUser = 0;
            }

            if (item.InteractingUser2 != 0)
            {
                RoomUser user = item.GetRoom()?.GetRoomUserManager()
                                    .GetRoomUserByHabbo(item.InteractingUser2);
                user?.UnlockWalking();
                item.InteractingUser2 = 0;
            }
        }

        /// <summary>
        /// Click/trigger normal — el tile también acepta activación manual,
        /// igual que el teleport estándar.
        /// </summary>
        public void OnTrigger(GameClient session, Item item, int request, bool hasRights)
        {
            if (item?.GetRoom() == null || session?.GetHabbo() == null) return;

            RoomUser user = item.GetRoom().GetRoomUserManager()
                                .GetRoomUserByHabbo(session.GetHabbo().Id);
            if (user == null) return;

            user.LastInteraction = PolarEnvironment.GetUnixTimestamp();

            if (user.Coordinate == item.Coordinate || user.Coordinate == item.SquareInFront)
            {
                if (item.InteractingUser != 0) return;

                if (!user.CanWalk ||
                    session.GetHabbo().IsTeleporting ||
                    session.GetHabbo().TeleporterId != 0 ||
                    (user.LastInteraction + 2) - PolarEnvironment.GetUnixTimestamp() < 0)
                    return;

                user.TeleDelay = 2;
                item.InteractingUser = user.GetClient().GetHabbo().Id;
            }
            else if (user.CanWalk)
            {
                user.MoveTo(item.SquareInFront);
            }
        }

        /// <summary>
        /// OnWalk — el tile activa el teleport al ser pisado, sin necesidad de click.
        /// Mirrors Java InteractionTeleportTile.onWalkOn().
        /// Llamado desde RoomUserManager cuando el usuario llega al tile.
        /// </summary>
        public void OnWalk(GameClient session, Item item)
        {
            if (item?.GetRoom() == null || session?.GetHabbo() == null) return;

            RoomUser user = item.GetRoom().GetRoomUserManager()
                                .GetRoomUserByHabbo(session.GetHabbo().Id);
            if (user == null) return;

            // Mirrors Java: canWalkOn → siempre true para TeleportTile
            // Mirrors Java: !habbo.getRoomUnit().isTeleporting
            if (session.GetHabbo().IsTeleporting) return;

            // Detener al usuario en su posición actual (setGoalLocation = currentLocation)
            //user.SetGoalLocation(user.Coordinate);

            // Iniciar la secuencia de teleport (delay 1000ms, igual que Java)
            StartTeleportSequence(item, user, session, 1000);
        }

        public void OnWiredTrigger(Item item) { }

        // ── Teleport sequence ─────────────────────────────────────────────────────

        /// <summary>
        /// Inicia la secuencia de teleport con el delay indicado.
        /// Mirrors Java InteractionTeleport.startTeleport(room, habbo, delay).
        /// </summary>
        private static void StartTeleportSequence(Item item, RoomUser user, GameClient session, int delayMs)
        {
            if (item.InteractingUser != 0) return; // ya en uso

            session.GetHabbo().IsTeleporting = true;
            session.GetHabbo().TeleporterId = item.Id;
            session.GetHabbo().TeleportingRoomID = item.GetRoom().Id;

            item.InteractingUser = session.GetHabbo().Id;
            item.ExtraData = "2"; // animación activa
            item.UpdateState(false, true);

            // Encolar el tick de procesamiento del teleport
            item.RequestUpdate(delayMs / 500, true); // 500ms por tick ≈ 2 ticks para 1000ms
        }
    }
}