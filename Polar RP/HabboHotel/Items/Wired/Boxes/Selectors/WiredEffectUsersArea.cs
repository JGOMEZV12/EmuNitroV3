using System;
using System.Collections.Generic;
using System.Linq;
using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Wired.Boxes.Selectors
{
    class WiredEffectUsersArea : WiredEffectVariableSelectorBase
    {
        public WiredEffectUsersArea(Room instance, Item item) : base(instance, item) { }
        public override WiredBoxType Type => WiredBoxType.SelectorUsersArea;

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

            context.SelectedUsers = Instance.GetRoomUserManager().GetRoomUsers()
                .Where(u => u.X >= minX && u.X <= maxX && u.Y >= minY && u.Y <= maxY)
                .Select(u => u.GetClient()?.GetHabbo())
                .Where(h => h != null)
                .ToList();

            return true;
        }
    }
}
