using Polar.Communication.Packets.Outgoing.Inventory.Purse;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Rooms;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using System.Threading.Tasks;
using Polar.Core;

namespace Polar.HabboHotel.Items
{
    public class CrackableItem
    {
        public UInt32 ItemId;
        public List<CrackableRewards> Rewards;

        public CrackableItem(DataRow dRow)
        {
            ItemId = Convert.ToUInt32(dRow["item_baseid"]);
            var rewardsString = (string)dRow["rewards"];

            Rewards = new List<CrackableRewards>();
            foreach (var reward in rewardsString.Split(';'))
            {
                var rewardType = reward.Split(',')[0];
                var rewardItem = reward.Split(',')[1];
                var rewardLevel = uint.Parse(reward.Split(',')[2]);
                Rewards.Add(new CrackableRewards(ItemId, rewardType, rewardItem, rewardLevel));
            }
        }
    }

    public class CrackableRewards
    {
        public UInt32 CrackableId, CrackableLevel;
        public String CrackableRewardType, CrackableReward;

        public CrackableRewards(uint crackableId, string crackableRewardType, string crackableReward, uint crackableLevel)
        {
            CrackableId = crackableId;
            CrackableRewardType = crackableRewardType;
            CrackableReward = crackableReward;
            CrackableLevel = crackableLevel;
        }
    }

    public class CrackableManager
    {
        public Dictionary<Int32, CrackableItem> Crackable;

        public void Initialize(IQueryAdapter dbClient)
        {
            Crackable = new Dictionary<Int32, CrackableItem>();
            dbClient.SetQuery("SELECT * FROM catalog_crackable_rewards");
            var table = dbClient.getTable();
            foreach (DataRow dRow in table.Rows)
            {
                if (Crackable.ContainsKey(Convert.ToInt32(dRow["item_baseid"]))) continue;
                Crackable.Add(Convert.ToInt32(dRow["item_baseid"]), new CrackableItem(dRow));
            }
        }


        private List<CrackableRewards> GetRewardsByLevel(int itemId, int level)
        {
            var rewards = new List<CrackableRewards>();
            foreach (var reward in Crackable[itemId].Rewards.Where(furni => furni.CrackableLevel == level)) rewards.Add(reward);
            return rewards;
        }

        public void ReceiveCrackableReward(RoomUser user, Room room, Item item)
        {

            if (room == null || item == null) return;
            if (item.GetBaseItem().InteractionType != InteractionType.PINATA && item.GetBaseItem().InteractionType != InteractionType.PINATATRIGGERED && item.GetBaseItem().InteractionType != InteractionType.MAGICEGG && item.GetBaseItem().InteractionType != InteractionType.MAGICCHEST) return;
            if (!Crackable.ContainsKey(item.GetBaseItem().Id)) return;
            CrackableItem crackable;
            Crackable.TryGetValue(item.GetBaseItem().Id, out crackable);
            if (crackable == null) return;
            int x = item.GetX, y = item.GetY;
            room.GetRoomItemHandler().RemoveFurniture(user.GetClient(), item.Id);
            var level = 0;
            var rand = new Random().Next(0, 100);
            if (rand >= 95) level = 5;                   // 005% de probabilidad de que salga nivel 5
            else if (rand >= 85 && rand < 95) level = 4; // 010% de probabilidad de que salga nivel 4
            else if (rand >= 65 && rand < 85) level = 3; // 020% de probabilidad de que salga nivel 3
            else if (rand >= 35 && rand < 65) level = 2; // 030% de probabilidad de que salga nivel 2
            else level = 1;                              // 035% de probabilidad de que salga nivel 1
                                                         // 100%

            var possibleRewards = GetRewardsByLevel((int)crackable.ItemId, level);
            var reward = possibleRewards[new Random().Next(0, (possibleRewards.Count - 1))];

            Task.Run(() =>
            {
                using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {

                    #region REWARD TYPES
                    switch (reward.CrackableRewardType)
                    {
                        #region NORMAL ITEMS REWARD
                        case "item":
                            goto ItemType;
                        #endregion

                        #region CREDITS REWARD
                        case "credits":
                        case "coins":
                        case "creditos":
                            {
                                user.GetClient().GetHabbo().Credits += int.Parse(reward.CrackableReward);
                                user.GetClient().SendMessage(new CreditBalanceComposer(user.GetClient().GetHabbo().Credits));
                                user.GetClient().SendMessage(new RoomBubbleNotificationComposer("cred", "Acabas de ganar " + int.Parse(reward.CrackableReward) + " créditos en la piñata.", ""));
                                user.GetClient().SendMessage(new RoomBubbleNotificationComposer("award", "Acabas de sacar un " + rand + " en los dados. ¡Enhorabuena!", ""));
                                room.GetRoomItemHandler().RemoveFurniture(user.GetClient(), item.Id);
                                dbClient.RunQuery("DELETE FROM items WHERE id = " + item.Id);
                                return;
                            }
                        #endregion

                        #region DUCKETS REWARD
                        case "duckets":
                            {
                                user.GetClient().GetHabbo().Duckets += int.Parse(reward.CrackableReward);
                                user.GetClient().SendMessage(new HabboActivityPointNotificationComposer(user.GetClient().GetHabbo().Duckets, user.GetClient().GetHabbo().Duckets));
                                user.GetClient().SendMessage(new RoomBubbleNotificationComposer("duckets", "Acabas de ganar " + int.Parse(reward.CrackableReward) + " duckets en la piñata.", ""));
                                user.GetClient().SendMessage(new RoomBubbleNotificationComposer("award", "Acabas de sacar un " + rand + " en los dados. ¡Enhorabuena!", ""));
                                room.GetRoomItemHandler().RemoveFurniture(user.GetClient(), item.Id);
                                dbClient.RunQuery("DELETE FROM items WHERE id = " + item.Id);
                                return;
                            }
                        #endregion

                        #region DIAMONDS REWARD
                        case "diamonds":
                        case "diamantes":
                            {
                                user.GetClient().GetHabbo().Diamonds += int.Parse(reward.CrackableReward);
                                user.GetClient().SendMessage(new HabboActivityPointNotificationComposer(user.GetClient().GetHabbo().Diamonds, 0, 5));
                                user.GetClient().SendMessage(new RoomBubbleNotificationComposer("diamonds", "Acabas de ganar " + int.Parse(reward.CrackableReward) + " diamantes en la piñata.", ""));
                                user.GetClient().SendMessage(new RoomBubbleNotificationComposer("award", "Acabas de sacar un " + rand + " en los dados. ¡Enhorabuena!", ""));
                                room.GetRoomItemHandler().RemoveFurniture(user.GetClient(), item.Id);
                                dbClient.RunQuery("DELETE FROM items WHERE id = " + item.Id);
                                return;
                            }
                        #endregion

                        #region HONOR REWARD
                        case "events":
                        case "event":
                        case "eventpoints":
                            {
                                user.GetClient().GetHabbo().EventPoints += int.Parse(reward.CrackableReward);
                                user.GetClient().SendMessage(new HabboActivityPointNotificationComposer(user.GetClient().GetHabbo().EventPoints, 0, 103));
                                user.GetClient().SendMessage(new RoomBubbleNotificationComposer("honor", "Acabas de ganar " + int.Parse(reward.CrackableReward) + " Event Points en la piñata.", ""));
                                user.GetClient().SendMessage(new RoomBubbleNotificationComposer("award", "Acabas de sacar un " + rand + " en los dados. ¡Enhorabuena!", ""));
                                room.GetRoomItemHandler().RemoveFurniture(user.GetClient(), item.Id);
                                dbClient.RunQuery("DELETE FROM items WHERE id = " + item.Id);
                                return;
                            }
                        #endregion

                        #region BADGE REWARD
                        case "badge":
                            {
                                if (user.GetClient().GetHabbo().GetBadgeComponent().HasBadge(reward.CrackableReward)) return;
                                user.GetClient().SendMessage(new RoomBubbleNotificationComposer("award", "Acabas de ganar la placa: " + int.Parse(reward.CrackableReward) + ".", ""));
                                user.GetClient().SendMessage(new RoomBubbleNotificationComposer("award", "Acabas de sacar un " + rand + " en los dados. ¡Enhorabuena!", ""));
                                user.GetClient().GetHabbo().GetBadgeComponent().GiveBadge(reward.CrackableReward, true, user.GetClient());
                                room.GetRoomItemHandler().RemoveFurniture(user.GetClient(), item.Id);
                                dbClient.RunQuery("DELETE FROM items WHERE id = " + item.Id);
                                return;
                            }
                            #endregion
                    }
                    #endregion

                    ItemType:
                    /*user.GetClient().SendMessage(new OpenGiftComposer(item.Data, item.ExtraData, item, true)); // custom tocan2
                    item.MagicRemove = true; // custom tocan2
                    room.SendMessage(new ObjectUpdateComposer(item, Convert.ToInt32(user.GetClient().GetHabbo().Id))); //custom tocan2*/
                    room.GetRoomItemHandler().RemoveFurniture(user.GetClient(), item.Id);
                    dbClient.RunQuery("UPDATE items SET base_item = " + int.Parse(reward.CrackableReward) + ", extra_data = '' WHERE id = " + item.Id);
                    item.BaseItem = int.Parse(reward.CrackableReward);
                    item.ResetBaseItem();
                    item.ExtraData = string.Empty;
                    if (!room.GetRoomItemHandler().SetFloorItem(user.GetClient(), item, item.GetX, item.GetY, item.Rotation, true, false, true))
                    {
                        dbClient.RunQuery("UPDATE items SET room_id = 0 WHERE id = " + item.Id);
                        user.GetClient().GetHabbo().GetInventoryComponent().UpdateItems(true);
                    }
                }

                user.GetClient().SendMessage(new RoomBubbleNotificationComposer("award", "Acabas de sacar un " + rand + " en los dados. ¡Enhorabuena!", ""));
            });
        }

    }
}