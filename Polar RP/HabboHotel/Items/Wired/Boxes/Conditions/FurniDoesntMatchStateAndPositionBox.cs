using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;
using Polar.Properties;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// FurniDoesntMatchStateAndPositionBox.cs
namespace Polar.HabboHotel.Items.Wired.Boxes.Conditions
{
    internal class FurniDoesntMatchStateAndPositionBox : FurniMatchStateAndPositionBox
    {
        public override WiredBoxType Type => WiredBoxType.ConditionDontMatchStateAndPosition;

        public FurniDoesntMatchStateAndPositionBox(Room instance, Item item)
            : base(instance, item) { }

        public override bool Execute(params object[] Params)
        {
            Refresh();
            // Java NOT: empty settings → false (al contrario que el positivo)
            if (settings.Count == 0) return false;

            return quantifier == QUANTIFIER_ANY ? !EvaluateAny() : !EvaluateAll();
        }
    }
}