using Polar.Communication.Packets.Outgoing;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;
using Polar.HabboHotel.Items.Wired;
using Polar.HabboHotel.Items;

// FIX: Eliminados using duplicados de Packets.Incoming, Rooms, Users

namespace Polar.HabboHotel.Items.Wired.Boxes.Conditions
{
    internal class TriggererNotOnFurniBox : TriggererOnFurniBox
    {
        public TriggererNotOnFurniBox(Room instance, Item item)
            : base(instance, item) { }

        public override WiredBoxType Type => WiredBoxType.ConditionTriggererNotOnFurni;

        public override bool Execute(params object[] Params)
        {
            Habbo player = Params.Length > 0 ? Params[0] as Habbo : null;
            if (player == null) return false;

            RoomUser user = player.CurrentRoom?.GetRoomUserManager().GetRoomUserByHabbo(player.Username);
            if (user == null) return false;

            if (SetItems.Count == 0) return true;

            var itemsOnSquare = Instance.GetGameMap().GetAllRoomItemForSquare(user.X, user.Y);

            if (Quantifier == QUANTIFIER_ANY)
                return !itemsOnSquare.Any(i => SetItems.ContainsKey(i.Id));

            return !SetItems.Keys.All(id => itemsOnSquare.Any(i => i.Id == id));
        }
    }
}
