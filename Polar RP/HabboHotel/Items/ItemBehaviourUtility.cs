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
            if (Item.LimitedNo > 0)
            {
                Message.WriteInteger(0x100);
                Message.WriteInteger(1);
                Message.WriteString(Item.ExtraData ?? "");
                Message.WriteInteger(Item.LimitedNo);
                Message.WriteInteger(Item.LimitedTot);
                return;
            }

            switch (Item.GetBaseItem().InteractionType)
            {
                case InteractionType.WIRED_HIGHSCORE:
                    {
                        string itemName = Item.GetBaseItem().ItemName;
                        string typePart = itemName?.Split('*').ElementAtOrDefault(1);

                        if (typePart == null) break;

                        // scoreType: classic=2, mostwin=1, perteam=0
                        int scoreType;
                        if (itemName.StartsWith("highscore_classic")) scoreType = 2;
                        else if (itemName.StartsWith("highscore_mostwin")) scoreType = 1;
                        else scoreType = 0; // perteam

                        // clearType: alltime=0, day=1, week=2, month=3
                        // El nombre es "highscore_X*N" donde N: 1=alltime, 2=day, 3=week, 4=month
                        int clearType = typePart switch
                        {
                            "1" => 0, // alltime
                            "2" => 1, // day
                            "3" => 2, // week
                            "4" => 3, // month
                            _ => 0
                        };

                        var room = Item.GetRoom();

                        Dictionary<int, KeyValuePair<int, string>> scoreData = clearType switch
                        {
                            1 => room?.WiredScoreBordDay ?? new(),
                            2 => room?.WiredScoreBordWeek ?? new(),
                            3 => room?.WiredScoreBordMonth ?? new(),
                            _ => new() // alltime — ajusta si tienes un WiredScoreBordAllTime
                        };

                        var sorted = scoreData
                            .OrderByDescending(i => i.Value.Key)
                            .Select(i => i.Value)
                            .Take(50)
                            .ToList();

                        Message.WriteInteger(6);
                        Message.WriteString(Item.ExtraData ?? string.Empty);
                        Message.WriteInteger(scoreType);
                        Message.WriteInteger(clearType);
                        Message.WriteInteger(sorted.Count);

                        foreach (var row in sorted)
                        {
                            Message.WriteInteger(row.Key);    // score
                            Message.WriteInteger(1);          // user count por row
                            Message.WriteString(row.Value ?? string.Empty);
                        }

                        break;
                    }
                case InteractionType.GUILD_ITEM:
                case InteractionType.GUILD_GATE:
                case InteractionType.GUILD_FORUM:
                    {
                        Group group = null;
                        if (Item.GroupId > 0)
                        {
                            group = GroupManager.GetJob(Item.GroupId);
                            if (group == null)
                                group = GroupManager.GetGang(Item.GroupId);
                        }

                        bool isLimited = Item.LimitedNo > 0;

                        if (group != null)
                        {
                            Message.WriteInteger(2 + (isLimited ? 256 : 0));
                            Message.WriteInteger(5);
                            Message.WriteString(Item.ExtraData ?? string.Empty);
                            Message.WriteString(group.Id.ToString());
                            Message.WriteString(group.Badge ?? string.Empty);
                            Message.WriteString(group.Colour1 ?? string.Empty);
                            Message.WriteString(group.Colour2 ?? string.Empty);
                        }
                        else
                        {
                            Message.WriteInteger(isLimited ? 256 : 0);
                            Message.WriteString(Item.ExtraData ?? string.Empty);
                        }

                        if (isLimited)
                        {
                            Message.WriteInteger(Item.LimitedNo);
                            Message.WriteInteger(Item.LimitedTot);
                        }
                        break;
                    }

                case InteractionType.BACKGROUND:
                case InteractionType.INFORMATION_TERMINAL:
                    Message.WriteInteger(1);
                    Dictionary<string, string> values = new Dictionary<string, string>();
                    if (!string.IsNullOrEmpty(Item.ExtraData) && Item.ExtraData.Contains('\t'))
                    {
                        string[] parts = Item.ExtraData.Split('\t');
                        for (int i = 0; i < parts.Length - 1; i += 2)
                        {
                            values[parts[i]] = parts[i + 1];
                        }
                    }
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
                        int style = 0;
                        if (int.TryParse(extraData[0], out int colorId) && int.TryParse(extraData[6], out int ribbonId))
                            style = (colorId * 1000) + ribbonId;

                        using (UserCache purchaser = PolarEnvironment.GetGame().GetCacheManager().GenerateUser(Convert.ToInt32(extraData[2])))
                        {
                            if (purchaser == null)
                            {
                                Message.WriteInteger(0);
                                Message.WriteString(Item.ExtraData ?? string.Empty);
                            }
                            else
                            {
                                Message.WriteInteger(1);
                                Message.WriteInteger(6);
                                Message.WriteString("EXTRA_PARAM");
                                Message.WriteString(style.ToString());
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
                    int.TryParse(Item.ExtraData, out cracks);
                    Message.WriteInteger(7);
                    Message.WriteString(cracks >= 4 ? "8" : (cracks * 2).ToString());
                    Message.WriteInteger(cracks);
                    Message.WriteInteger(4);
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
                        Message.WriteString("GENDER"); Message.WriteString(Stuff[0]);
                        Message.WriteString("FIGURE"); Message.WriteString(Stuff[1]);
                        Message.WriteString("OUTFIT_NAME"); Message.WriteString(Stuff[2]);
                    }
                    else
                    {
                        Message.WriteString("GENDER"); Message.WriteString("");
                        Message.WriteString("FIGURE"); Message.WriteString("");
                        Message.WriteString("OUTFIT_NAME"); Message.WriteString("");
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
                    string[] BadgeData = string.IsNullOrEmpty(Item.ExtraData) ? Array.Empty<string>() : Item.ExtraData.Split(Convert.ToChar(9));
                    if (BadgeData.Length >= 3)
                    {
                        Message.WriteString("0");
                        Message.WriteString(BadgeData[0]);
                        Message.WriteString(BadgeData[1]);
                        Message.WriteString(BadgeData[2]);
                    }
                    else
                    {
                        Message.WriteString("0"); Message.WriteString("DEV"); Message.WriteString("Sledmore"); Message.WriteString("13-13-1337");
                    }
                    break;

                case InteractionType.TELEVISION:
                    Message.WriteInteger(1);
                    Message.WriteInteger(1);
                    Message.WriteString("THUMBNAIL_URL");
                    var tv = PolarEnvironment.GetGame().GetTelevisionManager().TelevisionList.OrderBy(x => Guid.NewGuid()).FirstOrDefault();
                    Message.WriteString("/youtubethumbnail.php?img=" + (tv?.YouTubeId ?? string.Empty));
                    break;

                case InteractionType.LOVELOCK:
                    if (!string.IsNullOrEmpty(Item.ExtraData) && Item.ExtraData.Contains(Convert.ToChar(5).ToString()))
                    {
                        var EData = Item.ExtraData.Split((char)5);
                        Message.WriteInteger(2);
                        Message.WriteInteger(EData.Length);
                        for (int i = 0; i < EData.Length; i++) Message.WriteString(EData[i]);
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
                    Message.WriteInteger(0);
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

        public static void WriteInventoryItem(Item item, ServerPacket packet)
        {
            packet.WriteInteger(item.Id);
            packet.WriteString(item.GetBaseItem().Type.ToString().ToUpper());
            packet.WriteInteger(item.Id);
            packet.WriteInteger(item.GetBaseItem().SpriteId);

            string itemName = item.GetBaseItem().ItemName;

            if (itemName == "floor" || itemName == "landscape" || itemName == "song_disk" || itemName == "wallpaper" || itemName == "poster")
            {
                switch (itemName)
                {
                    case "landscape": packet.WriteInteger(4); break;
                    case "floor": packet.WriteInteger(3); break;
                    case "wallpaper": packet.WriteInteger(2); break;
                    case "poster": packet.WriteInteger(6); break;
                    case "song_disk": packet.WriteInteger(8); break;
                    default: packet.WriteInteger(1); break;
                }
                packet.WriteInteger(0);
                packet.WriteString(item.ExtraData ?? string.Empty);
            }
            else
            {
                if (itemName == "gnome_box")
                    packet.WriteInteger(13);
                else if (item.GetBaseItem().InteractionType == InteractionType.GIFT)
                {
                    string[] parts = item.ExtraData?.Split(Convert.ToChar(5)) ?? Array.Empty<string>();
                    if (parts.Length >= 7 && int.TryParse(parts[0], out int colorId) && int.TryParse(parts[6], out int ribbonId))
                        packet.WriteInteger((colorId * 1000) + ribbonId);
                    else
                        packet.WriteInteger(1);
                }
                else
                    packet.WriteInteger(1);

                GenerateExtradata(item, packet);
            }

            packet.WriteBoolean(item.GetBaseItem().AllowEcotronRecycle);
            packet.WriteBoolean(item.GetBaseItem().AllowTrade);
            packet.WriteBoolean(item.LimitedNo == 0 && item.GetBaseItem().AllowInventoryStack); // Nitro order: 3. Stackable
            packet.WriteBoolean(item.GetBaseItem().AllowMarketplaceSell); // Nitro order: 4. Sellable
            packet.WriteInteger(-1); // secondsToExpire
            packet.WriteBoolean(false); // hasRentPeriodStarted
            packet.WriteInteger(-1); // roomId

            if (!item.IsWallItem)
            {
                packet.WriteString(string.Empty); // slotId
                if (itemName == "song_disk")
                {
                    int trackId = 0;
                    if (!string.IsNullOrEmpty(item.ExtraData))
                    {
                        string[] lines = item.ExtraData.Split('\n');
                        int.TryParse(lines[lines.Length - 1], out trackId);
                    }
                    packet.WriteInteger(trackId);
                }
                else if (item.GetBaseItem().InteractionType == InteractionType.GIFT)
                {
                    string[] parts = item.ExtraData?.Split(Convert.ToChar(5)) ?? Array.Empty<string>();
                    if (parts.Length >= 7 && int.TryParse(parts[0], out int colorId) && int.TryParse(parts[6], out int ribbonId))
                        packet.WriteInteger((colorId * 1000) + ribbonId);
                    else
                        packet.WriteInteger(1);
                }
                else
                    packet.WriteInteger(1);
            }
        }
    }
}