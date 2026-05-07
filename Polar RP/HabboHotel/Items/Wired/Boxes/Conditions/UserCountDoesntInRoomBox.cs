using Polar.Communication.Packets.Outgoing;
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Wired.Boxes.Conditions
{
    class UserCountDoesntInRoomBox : UserCountInRoomBox
    {
        public override WiredBoxType Type => WiredBoxType.ConditionUserCountDoesntInRoom;

        public UserCountDoesntInRoomBox(Room instance, Item item)
            : base(instance, item) { }

        public override bool Execute(params object[] Params)
        {
            int count = Instance.UserCount;
            return !(count >= this.lowerLimit && count <= this.upperLimit);
        }
    }
}