using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

// FurniTypeDoesntMatchBox.cs
namespace Polar.HabboHotel.Items.Wired.Boxes.Conditions
{
    class FurniTypeDoesntMatchBox : FurniTypeMatchesBox
    {
        public override WiredBoxType Type => WiredBoxType.ConditionFurniTypeDoesntMatch;

        public FurniTypeDoesntMatchBox(Room instance, Item item)
            : base(instance, item) { }

        public override bool Execute(params object[] Params)
        {
            Refresh();
            // NOT: niega el resultado
            return quantifier == QUANTIFIER_ANY
                ? !EvaluateAllMatches(Params)
                : !EvaluateAnyMatches(Params);
        }
    }
}