using System.Drawing;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Polls;
using Polar.HabboHotel.Pathfinding;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Interactor
{
    public class InteractorAgua : IFurniInteractor
    {
        private const int CostoBebida = 150;
        private const int EnergiaRestaurada = 30;

        public void OnPlace(GameClient session, Item item) { }

        public void OnRemove(GameClient session, Item item) { }

        public void OnTrigger(GameClient session, Item item, int request, bool hasRights)
        {
            // FIX: Validar session y sus dependencias antes de usarlas
            if (session == null || session.GetHabbo() == null || session.GetRoleplay() == null)
                return;

            if (session.GetRoleplay().CurEnergy >= session.GetRoleplay().MaxEnergy)
            {
                session.SendWhisper("Al parecer no tienes más sed (Tu barra de energía está llena).");
                return;
            }

            if (session.GetHabbo().Credits < CostoBebida)
            {
                session.Shout("* Uff, olvidé guardar dinero en mi billetera para el bebedero *", 15);
                return;
            }

            if (session.GetRoomUser() == null)
            {
                session.SendWhisper("No puedes interactuar sin estar en una sala.");
                return;
            }

            var user = session.GetRoomUser();

            if (Gamemap.TilesTouching(item.GetX, item.GetY, user.X, user.Y))
            {
                session.GetRoleplay().UsingAgua = true;
                session.Shout("* Se inclina en el bebedero y bebe agua [-150$] *", 11);
                session.GetRoleplay().CurEnergy += EnergiaRestaurada;
                session.GetHabbo().Credits -= CostoBebida;
                session.GetHabbo().UpdateCreditsBalance();
                user.MoveTo(item.SquareInFront);
                session.GetRoleplay().IsWorking = false;
                RoleplayManager.GiveMoneyToCompany(12, session, "supermarket", true, CostoBebida);
            }
            else
            {
                session.SendWhisper("¡Debes estar más cerca del bebedero para beber agua!");
            }
        }

        public void OnWiredTrigger(Item item) { }
    }
}
