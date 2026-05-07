using System;
using System.Collections.Generic;
using System.Linq;
using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Wired.Boxes.Selectors
{
    class WiredEffectUsersNeighborhood : WiredEffectVariableSelectorBase
    {
        public WiredEffectUsersNeighborhood(Room instance, Item item) : base(instance, item) { }
        public override WiredBoxType Type => WiredBoxType.SelectorUsersNeighborhood;

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

            var matched = new List<Polar.HabboHotel.Users.Habbo>();
            foreach (var item in SetItems.Values)
            {
                matched.AddRange(Instance.GetRoomUserManager().GetRoomUsers()
                    .Where(u => Math.Abs(u.X - item.GetX) <= 1 && Math.Abs(u.Y - item.GetY) <= 1)
                    .Select(u => u.GetClient()?.GetHabbo())
                    .Where(h => h != null));
            }

            context.SelectedUsers = matched.Distinct().ToList();
            return true;
        }
    }
}
