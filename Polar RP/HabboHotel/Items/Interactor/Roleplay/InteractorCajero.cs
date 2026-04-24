using System.Drawing;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Polls;
using Polar.HabboHotel.Pathfinding;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboHotel.Items.Interactor
{
    public class InteractorCajero : IFurniInteractor
    {
        public void OnPlace(GameClient session, Item item) { }

        public void OnRemove(GameClient session, Item item) { }

        public void OnTrigger(GameClient session, Item item, int request, bool hasRights)
        {
            // FIX: Validar session y sus dependencias antes de usarlas
            if (session == null || session.GetHabbo() == null || session.GetRoleplay() == null)
                return;

            if (session.GetRoleplay().TryGetCooldown("cajerodiamonds", true))
            {
                session.SendWhisper("Debe esperar hasta que puedas hacer esto nuevamente!", 1);
                return;
            }

            if (session.GetHabbo().Diamonds < 20)
            {
                session.SendWhisper("Necesitas 20 rubies para usar este cajero. ¡Cómpralos en la web!", 34);
                return;
            }

            if (session.GetRoomUser() == null)
                return;

            var user = session.GetRoomUser();

            if (Rooms.Gamemap.TilesTouching(item.GetX, item.GetY, user.X, user.Y))
            {
                session.GetRoleplay().UsingCajero = true;
                session.Shout("* Introduce 20 rubies en la ranura y los cambia por: $100.000 *", 11);
                session.GetHabbo().Diamonds -= 20;
                session.GetHabbo().UpdateDiamondsBalance();
                session.GetHabbo().Credits += 100000;
                session.GetHabbo().UpdateCreditsBalance();
                user.MoveTo(item.SquareInFront);
                RoleplayManager.GiveMoneyToCompany(9, session, "bank", true, 150);
                session.GetRoleplay().CooldownManager.CreateCooldown("cajerodiamonds", 1000, 5);
            }
            else
            {
                session.SendWhisper("¡Debes estar frente al cajero para usarlo!", 1);
            }
        }

        public void OnWiredTrigger(Item item) { }
    }
}
