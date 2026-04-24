using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Cache;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.Items.Data.Toner;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;
using Polar.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Polar.HabboHotel.Items
{
    class ItemBehaviourUtility
    {
        public static void GenerateExtradata(Item Item, ServerPacket Message)
        {
            switch (Item.GetBaseItem().InteractionType)
            {
                case InteractionType.GUILD_ITEM:
                case InteractionType.GUILD_GATE:
                case InteractionType.GUILD_FORUM:
                    Group group = null;
                    if (Item.GroupId > 0)
                        group = GroupManager.GetJob(Item.GroupId);

                    if (group == null)
                    {
                        Message.WriteInteger(0);
                        Message.WriteString(Item.ExtraData ?? string.Empty);
                    }
                    else
                    {
                        Message.WriteInteger(2);
                        Message.WriteInteger(5);
                        Message.WriteString(Item.ExtraData ?? string.Empty);
                        Message.WriteString(group.Id.ToString());
                        Message.WriteString(group.Badge ?? string.Empty);
                        Message.WriteString(group.Colour1 ?? string.Empty);
                        Message.WriteString(group.Colour2 ?? string.Empty);
                    }
                    break;

                case InteractionType.BACKGROUND:
                case InteractionType.INFORMATION_TERMINAL:
                    // 1. Calcular header: 1 = datos extra presentes, +256 si es limitado
                    int header = 1;
                    if (Item.LimitedNo > 0) // o item.GetBaseItem().IsLimited
                        header += 256;

                    Message.WriteInteger(header);

                    // 2. Obtener pares clave-valor desde ExtraData o desde un diccionario
                    Dictionary<string, string> values = new Dictionary<string, string>();

                    if (!string.IsNullOrEmpty(Item.ExtraData) && Item.ExtraData.Contains('\t'))
                    {
                        string[] parts = Item.ExtraData.Split('\t');
                        // Asegurar número par de partes
                        for (int i = 0; i < parts.Length - 1; i += 2)
                        {
                            values[parts[i]] = parts[i + 1];
                        }
                    }

                    // Si además hay datos específicos (ej. limited sells, limited stack)
                    if (Item.LimitedNo > 0)
                    {
                        values["limited_sells"] = Item.LimitedTot.ToString();
                        values["limited_stack"] = Item.LimitedTot.ToString();
                    }

                    // 3. Escribir cantidad de pares y los pares
                    Message.WriteInteger(values.Count);
                    foreach (var kvp in values)
                    {
                        Message.WriteString(kvp.Key);
                        Message.WriteString(kvp.Value);
                    }
                    break;

                case InteractionType.GIFT:
                    string[] extraData = Item.ExtraData?.Split(Convert.ToChar(5)) ?? Array.Empty<string>();
                    if (extraData.Length != 7)
                    {
                        Message.WriteInteger(0);
                        Message.WriteString(Item.ExtraData ?? string.Empty);
                    }
                    else
                    {
                        if (!int.TryParse(extraData[6], out int giftStyle))
                            giftStyle = 0;
                        int style = giftStyle * 1000 + giftStyle;

                        using (UserCache purchaser = PolarEnvironment.GetGame().GetCacheManager().GenerateUser(Convert.ToInt32(extraData[2])))
                        {
                            if (purchaser == null)
                            {
                                Message.WriteInteger(0);
                                Message.WriteString(Item.ExtraData ?? string.Empty);
                            }
                            else
                            {
                                Message.WriteInteger(style);
                                Message.WriteInteger(1);
                                Message.WriteInteger(6);
                                Message.WriteString("EXTRA_PARAM");
                                Message.WriteString(string.Empty);
                                Message.WriteString("MESSAGE");
                                Message.WriteString(extraData[1]);
                                Message.WriteString("PURCHASER_NAME");
                                Message.WriteString(purchaser.Username ?? string.Empty);
                                Message.WriteString("PURCHASER_FIGURE");
                                Message.WriteString(purchaser.Look ?? string.Empty);
                                Message.WriteString("PRODUCT_CODE");
                                Message.WriteString("A1 KUMIANKKA");
                                Message.WriteString("state");
                                Message.WriteString(Item.MagicRemove ? "1" : "0");
                            }
                        }
                    }
                    break;

                case InteractionType.FARMING:
                    int cracks = 0;
                    int cracks_max = 4;

                    int.TryParse(Item.ExtraData, out cracks);

                    string state = "0";

                    if (cracks >= 4)
                        state = "8";
                    else if (cracks >= 3)
                        state = "6";
                    else if (cracks >= 2)
                        state = "4";
                    else if (cracks >= 1)
                        state = "2";

                    Message.WriteInteger(7);
                    Message.WriteString(state);
                    Message.WriteInteger(cracks);
                    Message.WriteInteger(cracks_max);
                    break;

                case InteractionType.CRACKABLE_EGG:
                    Message.WriteInteger(7);
                    Message.WriteString("8");
                    Message.WriteInteger(9);
                    Message.WriteInteger(12);
                    break;

                case InteractionType.MANNEQUIN:
                    Message.WriteInteger(1);
                    Message.WriteInteger(3);
                    if (!string.IsNullOrEmpty(Item.ExtraData) && Item.ExtraData.Contains(Convert.ToChar(5).ToString()))
                    {
                        string[] Stuff = Item.ExtraData.Split(Convert.ToChar(5));
                        Message.WriteString("GENDER");
                        Message.WriteString(Stuff[0]);
                        Message.WriteString("FIGURE");
                        Message.WriteString(Stuff[1]);
                        Message.WriteString("OUTFIT_NAME");
                        Message.WriteString(Stuff[2]);
                    }
                    else
                    {
                        Message.WriteString("GENDER");
                        Message.WriteString("");
                        Message.WriteString("FIGURE");
                        Message.WriteString("");
                        Message.WriteString("OUTFIT_NAME");
                        Message.WriteString("");
                    }
                    break;

                case InteractionType.TONER:
                    if (Item.RoomId != 0 && Item.GetRoom() != null)
                    {
                        if (Item.GetRoom().TonerData == null)
                            Item.GetRoom().TonerData = new TonerData(Item.Id);

                        Message.WriteInteger(5);
                        Message.WriteInteger(4);
                        Message.WriteInteger(Item.GetRoom().TonerData.Enabled);
                        Message.WriteInteger(Item.GetRoom().TonerData.Hue);
                        Message.WriteInteger(Item.GetRoom().TonerData.Saturation);
                        Message.WriteInteger(Item.GetRoom().TonerData.Lightness);
                    }
                    else
                    {
                        Message.WriteInteger(0);
                        Message.WriteString(string.Empty);
                    }
                    break;

                case InteractionType.BADGE_DISPLAY:
                    Message.WriteInteger(2);
                    Message.WriteInteger(4);

                    string[] BadgeData = string.IsNullOrEmpty(Item.ExtraData)
                        ? Array.Empty<string>()
                        : Item.ExtraData.Split(Convert.ToChar(9));

                    if (!string.IsNullOrEmpty(Item.ExtraData) && Item.ExtraData.Contains(Convert.ToChar(9).ToString()) && BadgeData.Length >= 3)
                    {
                        Message.WriteString("0");
                        Message.WriteString(BadgeData[0]);
                        Message.WriteString(BadgeData[1]);
                        Message.WriteString(BadgeData[2]);
                    }
                    else
                    {
                        Message.WriteString("0");
                        Message.WriteString("DEV");
                        Message.WriteString("Sledmore");
                        Message.WriteString("13-13-1337");
                    }
                    break;

                case InteractionType.TELEVISION:
                    Message.WriteInteger(1);
                    Message.WriteInteger(1);
                    Message.WriteString("THUMBNAIL_URL");

                    var tv = PolarEnvironment.GetGame().GetTelevisionManager().TelevisionList
                        .OrderBy(x => Guid.NewGuid()).FirstOrDefault();
                    Message.WriteString("/youtubethumbnail.php?img=" + (tv?.YouTubeId ?? string.Empty));
                    break;

                case InteractionType.LOVELOCK:
                    if (!string.IsNullOrEmpty(Item.ExtraData) && Item.ExtraData.Contains(Convert.ToChar(5).ToString()))
                    {
                        var EData = Item.ExtraData.Split((char)5);
                        Message.WriteInteger(2);
                        Message.WriteInteger(EData.Length);
                        for (int i = 0; i < EData.Length; i++)
                        {
                            Message.WriteString(EData[i]);
                        }
                    }
                    else
                    {
                        Message.WriteInteger(0);
                        Message.WriteString("0");
                    }
                    break;

                case InteractionType.MONSTERPLANT_SEED:
                    Message.WriteInteger(1);
                    Message.WriteInteger(1);
                    Message.WriteString("rarity");
                    Message.WriteString("1");
                    break;

                default:
                    Message.WriteInteger(0); // Legacy StuffData Type
                    Message.WriteString(Item.GetBaseItem().InteractionType != InteractionType.FOOTBALL_GATE ? Item.ExtraData : string.Empty);
                    break;
            }
        }

        public static void GenerateWallExtradata(Item Item, ServerPacket Message)
        {
            switch (Item.GetBaseItem().InteractionType)
            {
                default:
                    Message.WriteString(Item.ExtraData);
                    break;

                case InteractionType.POSTIT:
                    Message.WriteString(string.IsNullOrEmpty(Item.ExtraData) ? string.Empty : Item.ExtraData.Split(' ')[0]);
                    break;
            }
        }
    }
}