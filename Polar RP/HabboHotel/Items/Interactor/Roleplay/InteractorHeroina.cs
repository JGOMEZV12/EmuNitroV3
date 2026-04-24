using Polar.HabboHotel.Rooms.Pathfinding;
using Polar.Utilities;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboHotel.Items.Interactor
{
    internal class InteractorHeroina : IFurniInteractor
    {
        public void OnPlace(GameClient session, Item item)
        {
        }

        public void OnRemove(GameClient session, Item item)
        {
        }

        public void OnTrigger(GameClient session, Item item, int request, bool hasRights)
        {
            // Verificar si la sesión o el usuario son nulos
            if (session == null || session.GetHabbo() == null || session.GetRoleplay() == null)
            {
                session?.SendWhisper("Error: sesión inválida", 6);
                return;
            }

            // Verificar que el jugador esté en la sala correcta
            if (session.GetHabbo().CurrentRoomId != 16)
            {
                session.SendWhisper("¡No estás en la sala del prostíbulo!", 6);
                return;
            }

            // Verificar si el jugador tiene el trabajo correcto
            if (session.GetRoleplay().JobId != 15)
            {
                session.SendWhisper("¡No perteneces al trabajo del prostíbulo!", 6);
                return;
            }

            // Verificar si el jugador está muerto
            if (session.GetRoleplay().IsDead)
            {
                session.SendWhisper("¡No puedes hacer esto mientras estás muerto!", 6);
                return;
            }

            // Obtener la sala y el ítem relacionado con el microondas
            Room room = session.GetHabbo().CurrentRoom;
            Item? bTile = room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "ktchn_c15_microwave" && x.Coordinate == session.GetRoomUser().SquareInFront);

            // Obtener al usuario dentro de la sala
            RoomUser? user = item.GetRoom()?.GetRoomUserManager()?.GetRoomUserByHabbo(session.GetHabbo().Id);
            if (user == null)
            {
                session.SendWhisper("¡No se pudo encontrar al usuario en la sala!", 6);
                return;
            }

            // Verificar si el usuario está en la misma coordenada que el ítem
            if (!Gamemap.TilesTouching(item.GetX, item.GetY, user.Coordinate.X, user.Coordinate.Y))
            {
                if (item.ExtraData == "0" || item.ExtraData == "1")
                {
                    if (user.CanWalk)
                        user.MoveTo(item.SquareInFront);
                }
                return;
            }

            // Verificar si el jugador tiene suficiente dinero para realizar la acción
            if (session.GetHabbo().Credits < 20000)
            {
                session.SendWhisper("¡No tienes 20.00$ para invertir!", 6);
                return;
            }

            // Verificar si el jugador está ocupado trabajando
            if (session.GetRoleplay().IsWorkingOut)
            {
                session.SendWhisper("Deja de trabajar para poder hacer heroína", 6);
                return;
            }

            // Verificar si el jugador está frente al microondas
            if (bTile == null)
            {
                session.SendWhisper("¡Debes pararte frente al microondas para cocinar la heroína!", 1);
                return;
            }

            // Si el ítem tiene un valor extra vacío o "0", procedemos a realizar la acción
            if (string.IsNullOrEmpty(item.ExtraData))
                item.ExtraData = "0";

            if (item.ExtraData == "0")
            {
                int minutes = 1;

                user.ClearMovement(true);
                user.SetRot(Rotation.Calculate(user.Coordinate.X, user.Coordinate.Y, item.GetX, item.GetY), false);

                // Cambiar el estado del ítem para indicar que la fabricación está en curso
                item.ExtraData = "1";
                item.UpdateState(false, true);
                item.RequestUpdate(100 * minutes, true);

                session.Shout("* Comienza a fabricar la heroína [-20.000$ Inversión]*", 6);
                session.GetHabbo().Credits -= 20000;
                session.GetHabbo().UpdateCreditsBalance();

                user.CanWalk = false;
                session.GetRoleplay().HRidCoordinate = session.GetRoomUser().SquareInFront;
                session.GetRoleplay().HRidProcess = room;
                session.GetRoleplay().HRidItem = item;
                session.GetRoleplay().ProcessHeroine = true;
                session.GetRoleplay().LoadingTimeLeft = RoleplayManager.ProcessHeroineTime;
                session.GetRoleplay().TimerManager.CreateTimer("general", 1000, true);
            }
            else
            {
                session.SendWhisper("¡ESPERE POR FAVOR...!", 6);
            }
        }

        // Método que se ejecuta cuando el item es activado por un trigger (no se utiliza en este caso)
        public void OnWiredTrigger(Item item)
        {
        }

    }
}
