using Polar.Core;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;
using Polar.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace Polar.Communication.Packets.Outgoing.Rooms.Engine
{
    internal class ObjectAddComposer : ServerPacket
    {
        public ObjectAddComposer(Item Item, Room Room)
            : base(ServerPacketHeader.ObjectAddMessageComposer)
        {
            base.WriteInteger(Item.Id);
            base.WriteInteger(Item.GetBaseItem().SpriteId);
            base.WriteInteger(Item.GetX);
            base.WriteInteger(Item.GetY);
            base.WriteInteger(Item.Rotation);
            base.WriteString(Item.GetZ.ToString("G", System.Globalization.CultureInfo.InvariantCulture));

            if (Item.Data.InteractionType == InteractionType.TROPHY ||
                Item.Data.InteractionType == InteractionType.CRACKABLE ||
                Item.GetBaseItem().ItemName == "gnome_box")
                base.WriteString("1.0");
            else if (Item.Data.Walkable || Item.Data.IsSeat)
                base.WriteString(Item.GetZ.ToString("G", System.Globalization.CultureInfo.InvariantCulture));
            else
                base.WriteString(String.Empty);

            // _extra: Gift usa estilo, MusicDisc usa songId, default = 1
            if (Item.Data.InteractionType == InteractionType.GIFT)
            {
                string[] giftData = Item.ExtraData?.Split((char)5) ?? Array.Empty<string>();
                if (giftData.Length >= 7 && int.TryParse(giftData[6], out int giftStyle))
                    base.WriteInteger(giftStyle * 1000 + giftStyle);
                else
                    base.WriteInteger(1);
            }
            else if (Item.Data.InteractionType == InteractionType.MUSIC_DISC)
            {
                if (int.TryParse(Item.ExtraData, out int songId))
                    base.WriteInteger(songId);
                else
                    base.WriteInteger(1);
            }
            else
            {
                base.WriteInteger(1); // default, no 0
            }

            // _data
        

                if (Item.LimitedNo > 0)
                {
                    base.WriteInteger(1);
                    base.WriteInteger(256);
                    base.WriteString(Item.ExtraData ?? "");
                    base.WriteInteger(Item.LimitedNo);
                    base.WriteInteger(Item.LimitedTot);
                }
                else if (Item.Data.InteractionType == InteractionType.INFO_TERMINAL)
                {
                    base.WriteInteger(0);
                    base.WriteInteger(1);
                    base.WriteInteger(1);
                    base.WriteString("internalLink");
                    base.WriteString(Item.ExtraData ?? "");
                }
                else if (Item.Data.InteractionType == InteractionType.FX_PROVIDER)
                {
                    base.WriteInteger(0);
                    base.WriteInteger(1);
                    base.WriteInteger(1);
                    base.WriteString("effectId");
                    base.WriteString(Item.ExtraData ?? "");
                }
                else if (Item.Data.InteractionType == InteractionType.PINATA)
                {
                    base.WriteInteger(0);
                    base.WriteInteger(7);
                    base.WriteString("6");
                    base.WriteInteger(string.IsNullOrEmpty(Item.ExtraData) ? 0 : int.Parse(Item.ExtraData));
                    base.WriteInteger(100);
                }
                else if (Item.Data.InteractionType == InteractionType.PINATATRIGGERED)
                {
                    base.WriteInteger(0);
                    base.WriteInteger(7);
                    base.WriteString("0");
                    base.WriteInteger(string.IsNullOrEmpty(Item.ExtraData) ? 0 : int.Parse(Item.ExtraData));
                    base.WriteInteger(1);
                }
                else if (Item.Data.InteractionType == InteractionType.MAGICEGG)
                {
                    base.WriteInteger(0);
                    base.WriteInteger(7);
                    base.WriteString(Item.ExtraData ?? "");
                    base.WriteInteger(string.IsNullOrEmpty(Item.ExtraData) ? 0 : int.Parse(Item.ExtraData));
                    base.WriteInteger(23);
                }
                else if (Item.Data.InteractionType == InteractionType.MAGICCHEST)
                {
                    base.WriteInteger(0);
                    base.WriteInteger(7);
                    base.WriteString(Item.ExtraData ?? "");
                    base.WriteInteger(string.IsNullOrEmpty(Item.ExtraData) ? 0 : int.Parse(Item.ExtraData));
                    base.WriteInteger(1);
                }
                else
                {
                    try
                    {
                        ItemBehaviourUtility.GenerateExtradata(Item, this);
                    }
                    catch (Exception ex)
                    {
                        Logging.WriteLine($"[GenerateExtradata FAIL] Item {Item.Id} tipo {Item.Data.InteractionType} ExtraData='{Item.ExtraData}': {ex}", ConsoleColor.DarkGray);
                        base.WriteInteger(0);  // tipo legacy
                        base.WriteString("");  // string vacío
                    }
                }


            base.WriteInteger(-1); // _expires

            // _usagePolicy - lógica del Java de referencia
            if (Item.Data.InteractionType == InteractionType.TELEPORT ||
                Item.Data.InteractionType == InteractionType.SWITCH ||
                Item.Data.InteractionType == InteractionType.VENDING_MACHINE ||
                Item.Data.InteractionType == InteractionType.INFO_TERMINAL ||
                Item.Data.InteractionType == InteractionType.POSTIT)
                base.WriteInteger(2);
            else if (Item.GetBaseItem().Modes > 1)
                base.WriteInteger(1);
            else
                base.WriteInteger(0);

            base.WriteInteger(Item.UserID);
            base.WriteInteger(Item.Data.Stackable ? 1 : 0);
            base.WriteInteger(Item.Data.IsSeat ? 1 : 0);
            base.WriteInteger(0); // allowLay
            base.WriteInteger(Item.Data.Walkable ? 1 : 0);
            base.WriteInteger(Item.Data.Width);
            base.WriteInteger(Item.Data.Length);
            base.WriteInteger(0); // teleportTargetId
            base.WriteString(Room.OwnerName ?? ""); // _username
        }
    }
}