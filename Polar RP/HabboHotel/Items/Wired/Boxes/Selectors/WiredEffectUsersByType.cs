using System.Collections.Generic;
using System.Linq;
using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Wired.Boxes.Selectors
{
    class WiredEffectUsersByType : WiredEffectVariableSelectorBase
    {
        public WiredEffectUsersByType(Room instance, Item item) : base(instance, item) { }
        public override WiredBoxType Type => WiredBoxType.SelectorUsersByType;

        public override void HandleSave(ClientPacket packet)
        {
            packet.PopInt(); packet.PopString(); packet.PopInt();
            _delay = packet.PopInt();
        }

        public override bool Execute(params object[] @params)
        {
            if (@params.Length == 0 || !(@params[0] is WiredContext context)) return false;
            context.SelectedUsers = Instance.GetRoomUserManager().GetRoomUsers()
                .Select(u => u.GetClient()?.GetHabbo())
                .Where(h => h != null)
                .ToList();
            return true;
        }
    }
}
