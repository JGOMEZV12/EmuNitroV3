using System.Collections.Generic;
using System.Linq;
using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Wired.Boxes.Selectors
{
    class WiredEffectUsersOnFurni : WiredEffectVariableSelectorBase
    {
        public WiredEffectUsersOnFurni(Room instance, Item item) : base(instance, item) { }
        public override WiredBoxType Type => WiredBoxType.EffectUsersOnFurni;

        public override void HandleSave(ClientPacket packet)
        {
            packet.PopInt(); // paramsCount
            packet.PopString(); // stringData

            SetItems.Clear();
            int count = packet.PopInt();
            for (int i = 0; i < count; i++)
            {
                var item = Instance.GetRoomItemHandler().GetItem(packet.PopInt());
                if (item != null) SetItems.TryAdd(item.Id, item);
            }
            _delay = packet.PopInt();
        }

        public override bool Execute(params object[] @params)
        {
            if (@params.Length == 0 || !(@params[0] is WiredContext context)) return false;

            var result = new List<Polar.HabboHotel.Users.Habbo>();
            foreach (var item in SetItems.Values)
            {
                var usersOnTile = Instance.GetRoomUserManager().GetRoomUsers()
                    .Where(u => u.X == item.GetX && u.Y == item.GetY)
                    .Select(u => u.GetClient()?.GetHabbo())
                    .Where(h => h != null);

                result.AddRange(usersOnTile);
            }

            context.SelectedUsers = result.Distinct().ToList();
            return true;
        }
    }
}
