using Polar.HabboHotel.GameClients;

namespace Polar.HabboHotel.Items.Interactor
{
    public class InteractorIgnore : IFurniInteractor
    {
        public void OnPlace(GameClient Session, Item Item)
        {
        }

        public void OnRemove(GameClient Session, Item Item)
        {
        }

        public void OnTrigger(GameClient Session, Item Item, int Request, bool UserHasRights)
        {
        }
        public void OnWiredTrigger(Item Ball)
        {
        }
    }
}
