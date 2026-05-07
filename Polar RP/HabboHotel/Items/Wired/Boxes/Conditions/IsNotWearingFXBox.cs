using Polar.Communication.Packets.Outgoing;
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;

namespace Polar.HabboHotel.Items.Wired.Boxes.Conditions
{
    class IsNotWearingFXBox : IsWearingFXBox
    {
        public override WiredBoxType Type => WiredBoxType.ConditionIsNotWearingFX;

        public IsNotWearingFXBox(Room instance, Item item)
            : base(instance, item) { }

        public override bool Execute(params object[] Params)
        {
            if (Params.Length == 0) return false;

            Habbo Player = Params[0] as Habbo;
            if (Player == null) return false;

            return !MatchesEffect(Player);
        }
    }
}