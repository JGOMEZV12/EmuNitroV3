using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polar.HabboHotel.GameClients;

namespace Polar.HabboHotel.Items.Interactor
{
    class InteractorCameraPicture : IFurniInteractor
    {
        public string GetJsonData(Item item)
        {
            var defaultData = "{\"t\":\"0\",\"u\":\"1\", \"n\":\"Lucas\",\"s\":\"1\",\"url\":\"http://habbocamera.dev/pictures/10.png\", \"w\": \"http://habbocamera.dev/pictures/10.png\", \"m\": \"lalalala\"}";
            var ExtraData = item.ExtraData;

            if(ExtraData == "")
                return defaultData;

            return item.ExtraData;

        }
        public void OnPlace(GameClient Session, Item Item)
        {
        }

        public void OnRemove(GameClient Session, Item Item)
        {
        }

        public void OnTrigger(GameClient Session, Item Item, int Request, bool HasRights)
        {
        }

        public void OnWiredTrigger(Item Item)
        {
        }
    }
}
