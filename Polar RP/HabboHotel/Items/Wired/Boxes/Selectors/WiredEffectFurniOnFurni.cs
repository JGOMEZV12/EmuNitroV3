using System.Collections.Generic;
using System.Linq;
using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Wired.Boxes.Selectors
{
    class WiredEffectFurniOnFurni : WiredEffectVariableSelectorBase
    {
        public WiredEffectFurniOnFurni(Room instance, Item item) : base(instance, item) { }
        public override WiredBoxType Type => WiredBoxType.SelectorFurniOnFurni;

        public override void HandleSave(ClientPacket packet)
        {
            packet.PopInt();
            packet.PopString();
            SetItems.Clear();
            int count = packet.PopInt();
            for (int i = 0; i < count; i++)
            {
                var it = Instance.GetRoomItemHandler().GetItem(packet.PopInt());
                if (it != null) SetItems.TryAdd(it.Id, it);
            }
            _delay = packet.PopInt();
        }

        public override bool Execute(params object[] @params)
        {
            if (@params.Length == 0 || !(@params[0] is WiredContext context)) return false;

            var result = new List<Item>();
            foreach (var item in SetItems.Values)
            {
                result.AddRange(Instance.GetRoomItemHandler().GetFloor
                    .Where(i => i.GetX == item.GetX && i.GetY == item.GetY && i.Id != item.Id));
            }

            context.SelectedItems = result.Distinct().ToList();
            return true;
        }
    }
}
