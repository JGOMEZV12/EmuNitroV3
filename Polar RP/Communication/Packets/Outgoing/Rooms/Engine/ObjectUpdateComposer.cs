using Polar.HabboHotel.Items;
using System;

namespace Polar.Communication.Packets.Outgoing.Rooms.Engine
{
    internal class ObjectUpdateComposer : ServerPacket
    {
        public ObjectUpdateComposer(Item item, int userId)
            : base(ServerPacketHeader.ObjectUpdateMessageComposer)
        {
            WriteInteger(item.Id);
            WriteInteger(item.GetBaseItem().SpriteId);
            WriteInteger(item.GetX);
            WriteInteger(item.GetY);
            WriteInteger(item.Rotation);
            WriteString(item.GetZ.ToString("G", System.Globalization.CultureInfo.InvariantCulture));

            var interactionType = item.Data.InteractionType;
            if (interactionType == InteractionType.TROPHY || interactionType == InteractionType.CRACKABLE || item.GetBaseItem().ItemName == "gnome_box")
                WriteString("1.0");
            else if (item.Data.Walkable || item.Data.IsSeat)
                WriteString(item.GetZ.ToString("G", System.Globalization.CultureInfo.InvariantCulture));
            else
                WriteString(string.Empty);

            if (interactionType == InteractionType.GIFT)
            {
                string[] giftData = item.ExtraData?.Split((char)5) ?? Array.Empty<string>();
                if (giftData.Length >= 7 && int.TryParse(giftData[6], out int giftStyle))
                    WriteInteger(giftStyle * 1000 + giftStyle);
                else
                    WriteInteger(0);
            }
            else if (interactionType == InteractionType.MUSIC_DISC)
            {
                if (int.TryParse(item.ExtraData, out int songId))
                    WriteInteger(songId);
                else
                    WriteInteger(0);
            }
            else
            {
                WriteInteger(0);
            }

            ItemBehaviourUtility.GenerateExtradata(item, this);

            WriteInteger(-1); // expires
            WriteInteger(0);  // usagePolicy
            WriteInteger(userId);
        }
    }
}