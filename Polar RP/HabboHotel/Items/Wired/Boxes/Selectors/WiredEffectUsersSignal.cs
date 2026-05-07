using System.Collections.Generic;
using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Wired.Boxes.Selectors
{
    class WiredEffectUsersSignal : WiredEffectVariableSelectorBase
    {
        public WiredEffectUsersSignal(Room instance, Item item) : base(instance, item) { }
        public override WiredBoxType Type => WiredBoxType.EffectUsersSignal;

        public override void HandleSave(ClientPacket packet)
        {
            packet.PopInt(); packet.PopString(); packet.PopInt();
            _delay = packet.PopInt();
        }

        public override bool Execute(params object[] @params)
        {
            if (@params.Length == 0 || !(@params[0] is WiredContext context)) return false;
            if (context.Triggerer != null) context.SelectedUsers = new List<Polar.HabboHotel.Users.Habbo> { context.Triggerer };
            return true;
        }
    }
}
