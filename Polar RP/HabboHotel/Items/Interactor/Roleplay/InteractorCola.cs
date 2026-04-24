using System.Drawing;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Polls;
using Polar.HabboHotel.Pathfinding;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboHotel.Items.Interactor
{
    public class InteractorCola : IFurniInteractor
    {
        public void OnPlace(GameClient Session, Item Item) { }

        public void OnRemove(GameClient Session, Item Item) { }

        public void OnTrigger(GameClient Session, Item Item, int Request, bool HasRights)
        {
            // FIX: Validar Session y sus dependencias antes de usarlas
            if (Session == null || Session.GetHabbo() == null || Session.GetRoleplay() == null)
                return;

            if (Session.GetRoleplay().CurEnergy >= Session.GetRoleplay().MaxEnergy)
            {
                Session.Shout("* " + Session.GetHabbo().Username + " ¡La maquina se ha comido el billete pero no ha dado la Pepsi!", 15);
                Session.SendWhisper("Al parecer no tienes más sed (Tu barra de energía esta llena)");
                Session.GetHabbo().Credits -= 70;
                Session.GetHabbo().UpdateCreditsBalance();
                RoleplayManager.GiveMoneyToCompany(12, Session, "supermarket", true, 70);
                return;
            }

            if (Session.GetHabbo().Credits < 70)
            {
                Session.Shout("* Uff olvidé guardar dinero en mi billetera, no puedo comprar una Pepsi *", 15);
                return;
            }

            if (Session.GetRoomUser() == null)
                return;

            var User = Session.GetRoomUser();

            if (Rooms.Gamemap.TilesTouching(Item.GetX, Item.GetY, User.X, User.Y))
            {
                Session.GetRoleplay().UsingCola = true;
                Session.Shout("* Toma una Pepsi y gracias a los 35,0 Gr de azúcar, se siente más enérgico [-70$] *", 11);
                Session.GetRoleplay().CurEnergy += 50;
                Session.GetHabbo().Credits -= 70;
                Session.GetHabbo().UpdateCreditsBalance();
                User.MoveTo(Item.SquareInFront);
                Session.GetRoleplay().IsWorking = false;
                User.CarryItem(55);
                RoleplayManager.GiveMoneyToCompany(12, Session, "supermarket", true, 70);
            }
        }

        public void OnWiredTrigger(Item Item) { }
    }
}
