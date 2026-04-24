using System.Drawing;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Polls;
using Polar.HabboHotel.Pathfinding;

namespace Polar.HabboHotel.Items.Interactor
{
    public class InteractorComida : IFurniInteractor
    {
        public void OnPlace(GameClient Session, Item Item) { }

        public void OnRemove(GameClient Session, Item Item) { }

        public void OnTrigger(GameClient Session, Item Item, int Request, bool HasRights)
        {
            // FIX: Validar Session y sus dependencias antes de usarlas
            if (Session == null || Session.GetHabbo() == null || Session.GetRoleplay() == null)
                return;

            if (Session.GetRoleplay().Hunger < 0)
            {
                Session.Shout("* Mejor no comeré, No tengo hambre *", 1);
                return;
            }

            if (Session.GetHabbo().Credits < 100)
            {
                Session.Shout("* Uff olvidé guardar dinero en mi billetera, no puedo comer aquí *", 15);
                return;
            }

            if (Session.GetRoomUser() == null)
                return;

            var User = Session.GetRoomUser();

            if (Rooms.Gamemap.TilesTouching(Item.GetX, Item.GetY, User.X, User.Y))
            {
                Session.GetRoleplay().UsingCola = true;
                Session.Shout("* Toma su sándwich de jamón y comienza a comer [-30 Hambre] [-100$] *", 5);
                Session.GetRoleplay().Hunger -= 30;
                Session.GetHabbo().Credits -= 100;
                Session.GetHabbo().UpdateCreditsBalance();
                User.MoveTo(Item.SquareInFront);
                User.CarryItem(71);
            }
        }

        public void OnWiredTrigger(Item Item) { }
    }
}
