using System.Collections.Generic;
using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Wired.Boxes.Selectors
{
    class WiredEffectUsersPicks : WiredEffectVariableSelectorBase
    {
        public WiredEffectUsersPicks(Room instance, Item item) : base(instance, item) { }
        public override WiredBoxType Type => WiredBoxType.EffectUsersPicks;

        public override void HandleSave(ClientPacket packet)
        {
            packet.PopInt(); packet.PopString(); packet.PopInt();
            _delay = packet.PopInt();
        }

        public override bool Execute(params object[] @params)
        {
            // Just return true, maintaining current selection
            return true;
        }
    }
}
