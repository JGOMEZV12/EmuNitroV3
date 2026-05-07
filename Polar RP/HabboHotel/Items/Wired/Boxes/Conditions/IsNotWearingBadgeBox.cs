using Polar.Communication.Packets.Outgoing;
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;
using Polar.HabboHotel.Users.Badges;

namespace Polar.HabboHotel.Items.Wired.Boxes.Conditions
{
    class IsNotWearingBadgeBox : IsWearingBadgeBox
    {
        public override WiredBoxType Type => WiredBoxType.ConditionIsNotWearingBadge;

        public IsNotWearingBadgeBox(Room instance, Item item)
            : base(instance, item) { }

        public override bool Execute(params object[] Params)
        {
            if (Params.Length == 0 || string.IsNullOrEmpty(this.badge)) return false;

            Habbo Player = Params[0] as Habbo;
            if (Player == null) return false;

            return !IsWearingBadge(Player);
        }
    }
}