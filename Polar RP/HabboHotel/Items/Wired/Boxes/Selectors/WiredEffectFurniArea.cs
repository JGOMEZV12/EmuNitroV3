using System;
using System.Collections.Generic;
using System.Linq;
using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Wired.Boxes.Selectors
{
    class WiredEffectFurniArea : WiredEffectVariableSelectorBase
    {
        public WiredEffectFurniArea(Room instance, Item item) : base(instance, item) { }
        public override WiredBoxType Type => WiredBoxType.SelectorFurniArea;

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
            if (SetItems.Count < 2) return false;

            var items = SetItems.Values.ToList();
            int minX = items.Min(i => i.GetX);
            int maxX = items.Max(i => i.GetX);
            int minY = items.Min(i => i.GetY);
            int maxY = items.Max(i => i.GetY);

            context.SelectedItems = Instance.GetRoomItemHandler().GetFloor
                .Where(i => i.GetX >= minX && i.GetX <= maxX && i.GetY >= minY && i.GetY <= maxY)
                .ToList();

            return true;
        }
    }
}
