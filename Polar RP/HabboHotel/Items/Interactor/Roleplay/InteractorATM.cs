using System.Drawing;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Polls;
using Polar.HabboHotel.Pathfinding;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Interactor
{
    public class InteractorATM : IFurniInteractor
    {
        // Métodos que no tienen lógica, se dejan vacíos
        public void OnPlace(GameClient session, Item item) { }

        public void OnRemove(GameClient session, Item item) { }

        public void OnTrigger(GameClient session, Item item, int request, bool hasRights)
        {
            // Verificar si el usuario está presente en la sala
            var user = session.GetRoomUser();
            if (user == null) return;

            // Verificar si el WebSocket está activo
            if (session.GetRoleplay().WebSocketConnection != null)
            {
                // Si el usuario está en la misma casilla del cajero, abrir la interfaz Web
                if (Gamemap.TilesTouching(item.GetX, item.GetY, user.X, user.Y))
                {
                    session.GetRoleplay().UsingAtm = true;
                    PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(session, "event_atm", "open");
                }
                else
                {
                    // Si no está en la misma casilla, moverlo hacia el cajero
                    user.MoveTo(item.SquareInFront);
                }
            }
            else
            {
                // En caso de que el WebSocket no esté activo, usar el cajero de respaldo
                session.SendWhisper("Puesto que nuestro servidor WebSocket general está desconectado, puede utilizar nuestro cajero automático de respaldo!", 1);

                // Si el usuario está en la misma casilla, mostrar el poll
                if (Gamemap.TilesTouching(item.GetX, item.GetY, user.X, user.Y))
                {
                    string pollName = $"{PolarEnvironment.GetConfig().data["hotel.name"]} ATM";
                    string pollInvitation = $"{PolarEnvironment.GetConfig().data["hotel.name"]} ATM";
                    string pollThanks = $"Gracias por usar nuestro {PolarEnvironment.GetConfig().data["hotel.name"]} ATM Cajero";

                    // Crear y enviar la encuesta al usuario
                    var atmPoll = new Polls.Poll(500000, 0, pollName, pollInvitation, pollThanks, "", 1, null);
                    session.SendMessage(new SuggestPollMessageComposer(atmPoll));
                }
                else
                {
                    // Si no está en la misma casilla, moverlo hacia el cajero
                    user.MoveTo(item.SquareInFront);
                }
            }
        }

        public void OnWiredTrigger(Item item) { }
    }
}
