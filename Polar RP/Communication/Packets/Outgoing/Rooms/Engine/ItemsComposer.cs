using System.Collections.Generic;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Items;

namespace Polar.Communication.Packets.Outgoing.Rooms.Engine
{
    internal class ItemsComposer : ServerPacket
    {
        public ItemsComposer(Item[] objects, Room room)
            : base(ServerPacketHeader.ItemsMessageComposer)
        {
            // Owners: una sola pasada
            var owners = new Dictionary<int, string>(objects.Length);
            foreach (var item in objects)
            {
                if (item == null) continue;
                if (!owners.ContainsKey(item.UserID))
                    owners[item.UserID] = item.Username;
            }

            WriteInteger(owners.Count);
            foreach (var owner in owners)
            {
                WriteInteger(owner.Key);
                WriteString(owner.Value);
            }

            // Items: filtrar nulls antes de escribir el count
            var validItems = new List<Item>(objects.Length);
            foreach (var item in objects)
            {
                if (item != null) validItems.Add(item);
            }

            WriteInteger(validItems.Count);
            foreach (var item in validItems)
            {
                WriteWallItem(item, item.UserID);
            }
        }

        private void WriteWallItem(Item item, int userId)
        {
            WriteString(item.Id.ToString());
            WriteInteger(item.Data.SpriteId);
            WriteString(item.wallCoord ?? string.Empty);
            ItemBehaviourUtility.GenerateWallExtradata(item, this);
            WriteInteger(-1);                              // expires
            WriteInteger(item.Data.Modes > 1 ? 1 : 0);   // usagePolicy
            WriteInteger(userId);
            WriteInteger(item.Data.Stackable ? 1 : 0);
            WriteInteger(item.Data.IsSeat ? 1 : 0);
            WriteInteger(0);                               // allowLay
            WriteInteger(item.Data.Walkable ? 1 : 0);
            WriteInteger(item.Data.Width);
            WriteInteger(item.Data.Length);
            WriteInteger(0);                               // teleportTargetId
        }
    }
}