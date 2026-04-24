using Polar.Core;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Users;
using Polar.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Polar.Communication.Packets.Outgoing.Rooms.Engine
{
    internal class ObjectUpdateComposer : ServerPacket
    {
        public Item Item { get; }
        public int UserId { get; }

        public ObjectUpdateComposer(Item item, int userId)
            : base(ServerPacketHeader.ObjectUpdateMessageComposer)
        {
            this.Item = item;
            this.UserId = userId;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(Item.Id);
            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.GetX);
            packet.WriteInteger(Item.GetY);
            packet.WriteInteger(Item.Rotation);
            packet.WriteString(Item.GetZ.ToString("G", System.Globalization.CultureInfo.InvariantCulture));

            if (Item.Data.InteractionType == InteractionType.TROPHY ||
                Item.Data.InteractionType == InteractionType.CRACKABLE ||
                Item.GetBaseItem().ItemName == "gnome_box")
                packet.WriteString("1.0");
            else if (Item.Data.Walkable || Item.Data.IsSeat)
                packet.WriteString(Item.GetZ.ToString("G", System.Globalization.CultureInfo.InvariantCulture));
            else
                packet.WriteString(String.Empty);

            // _extra
            if (Item.Data.InteractionType == InteractionType.GIFT)
            {
                string[] giftData = Item.ExtraData?.Split((char)5) ?? Array.Empty<string>();
                if (giftData.Length >= 7 && int.TryParse(giftData[6], out int giftStyle))
                    packet.WriteInteger(giftStyle * 1000 + giftStyle);
                else
                    packet.WriteInteger(0);
            }
            else if (Item.Data.InteractionType == InteractionType.MUSIC_DISC)
            {
                if (int.TryParse(Item.ExtraData, out int songId))
                    packet.WriteInteger(songId);
                else
                    packet.WriteInteger(0);
            }
            else
            {
                packet.WriteInteger(0); // default es 0 en FloorItemUpdate
            }

            // _data
            try
            {

                if (Item.LimitedNo > 0)
                {
                    packet.WriteInteger(1);
                    packet.WriteInteger(256);
                    packet.WriteString(Item.ExtraData ?? "");
                    packet.WriteInteger(Item.LimitedNo);
                    packet.WriteInteger(Item.LimitedTot);
                }
                else if (Item.Data.InteractionType == InteractionType.INFO_TERMINAL)
                {
                    packet.WriteInteger(0);
                    packet.WriteInteger(1);
                    packet.WriteInteger(1);
                    packet.WriteString("internalLink");
                    packet.WriteString(Item.ExtraData ?? "");
                }
                else if (Item.Data.InteractionType == InteractionType.FX_PROVIDER)
                {
                    packet.WriteInteger(0);
                    packet.WriteInteger(1);
                    packet.WriteInteger(1);
                    packet.WriteString("effectId");
                    packet.WriteString(Item.ExtraData ?? "");
                }
                else if (Item.Data.InteractionType == InteractionType.PINATA)
                {
                    packet.WriteInteger(0);
                    packet.WriteInteger(7);
                    packet.WriteString("6");
                    packet.WriteInteger(string.IsNullOrEmpty(Item.ExtraData) ? 0 : int.Parse(Item.ExtraData));
                    packet.WriteInteger(100);
                }
                else if (Item.Data.InteractionType == InteractionType.PINATATRIGGERED)
                {
                    packet.WriteInteger(0);
                    packet.WriteInteger(7);
                    packet.WriteString("0");
                    packet.WriteInteger(string.IsNullOrEmpty(Item.ExtraData) ? 0 : int.Parse(Item.ExtraData));
                    packet.WriteInteger(1);
                }
                else if (Item.Data.InteractionType == InteractionType.MAGICEGG)
                {
                    packet.WriteInteger(0);
                    packet.WriteInteger(7);
                    packet.WriteString(Item.ExtraData ?? "");
                    packet.WriteInteger(string.IsNullOrEmpty(Item.ExtraData) ? 0 : int.Parse(Item.ExtraData));
                    packet.WriteInteger(23);
                }
                else if (Item.Data.InteractionType == InteractionType.MAGICCHEST)
                {
                    packet.WriteInteger(0);
                    packet.WriteInteger(7);
                    packet.WriteString(Item.ExtraData ?? "");
                    packet.WriteInteger(string.IsNullOrEmpty(Item.ExtraData) ? 0 : int.Parse(Item.ExtraData));
                    packet.WriteInteger(1);
                }
                else
                {
                    try
                    {
                        ItemBehaviourUtility.GenerateExtradata(Item, packet);
                    }
                    catch (Exception ex)
                    {
                        Logging.WriteLine($"[GenerateExtradata FAIL] Item {Item.Id} tipo {Item.Data.InteractionType} ExtraData='{Item.ExtraData}': {ex}", ConsoleColor.DarkGray);
                        packet.WriteInteger(0);  // tipo legacy
                        packet.WriteString("");  // string vacío
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR _data] Item {Item.Id} tipo {Item.Data.InteractionType}: {ex.Message}");
                // escribir data vacía para no corromper el paquete
                packet.WriteInteger(0);
                packet.WriteString("");
            }

            packet.WriteInteger(-1);  // _expires
            packet.WriteInteger(0);   // _usagePolicy siempre 0 en update
            packet.WriteInteger(UserId);
            packet.WriteInteger(Item.Data.Stackable ? 1 : 0);
            packet.WriteInteger(Item.Data.IsSeat ? 1 : 0);
            packet.WriteInteger(0);   // allowLay
            packet.WriteInteger(Item.Data.Walkable ? 1 : 0);
            packet.WriteInteger(Item.Data.Width);
            packet.WriteInteger(Item.Data.Length);
            packet.WriteInteger(0);   // teleportTargetId
            // sin _username al final
        }
    }
}