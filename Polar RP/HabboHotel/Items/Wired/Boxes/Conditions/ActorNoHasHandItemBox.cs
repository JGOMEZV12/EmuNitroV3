using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;

namespace Polar.HabboHotel.Items.Wired.Boxes.Conditions
{
    class NotActorHasHandItemBox : ActorHasHandItemBox
    {
        public override WiredBoxType Type => WiredBoxType.ConditionNotActorHasHandItemBox;

        public NotActorHasHandItemBox(Room instance, Item item)
            : base(instance, item) { }

        public override bool Execute(params object[] Params)
        {
            if (Params.Length == 0 || Instance == null) return false;

            Habbo Player = Params[0] as Habbo;
            if (Player == null) return false;

            RoomUser User = Instance.GetRoomUserManager().GetRoomUserByHabbo(Player.Id);
            if (User == null) return false;

            bool hasItem = User.CarryItemID == this.handItem;

            return quantifier == QUANTIFIER_ANY ? !hasItem : !hasItem;
        }
    }
}