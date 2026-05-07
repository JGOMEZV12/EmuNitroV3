using System;
using System.Collections.Generic;
using System.Linq;
using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Wired.Boxes.Selectors
{
    class WiredEffectFurniNeighborhood : WiredEffectVariableSelectorBase
    {
        public WiredEffectFurniNeighborhood(Room instance, Item item) : base(instance, item) { }
        public override WiredBoxType Type => WiredBoxType.SelectorFurniNeighborhood;

        public override void HandleSave(ClientPacket packet)
        {
            packet.PopInt(); packet.PopString();
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

            var matched = new List<Item>();
            foreach (var item in SetItems.Values)
            {
                matched.AddRange(Instance.GetRoomItemHandler().GetFloor
                    .Where(i => Math.Abs(i.GetX - item.GetX) <= 1 && Math.Abs(i.GetY - item.GetY) <= 1 && i.Id != item.Id));
            }

            context.SelectedItems = matched.Distinct().ToList();
            return true;
        }
    }
}
