using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using log4net;
using Polar.Database.Interfaces;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Items;

namespace Polar.HabboHotel.BattlePass
{
    public class BattlePassManager
    {
        private static readonly ILog log = LogManager.GetLogger("Polar.HabboHotel.BattlePass.BattlePassManager");

        public BattlePassSeason CurrentSeason { get; private set; }
        public Dictionary<int, int> Levels { get; private set; }
        public Dictionary<int, BattlePassReward> Rewards { get; private set; }
        public List<BattlePassCategory> Categories { get; private set; }
        public List<KeyValuePair<int, int>> Leaderboard { get; private set; }
        public Dictionary<int, BattlePassChallenge> Challenges { get; private set; }

        public BattlePassManager()
        {
            Levels = new Dictionary<int, int>();
            Rewards = new Dictionary<int, BattlePassReward>();
            Categories = new List<BattlePassCategory>();
            Challenges = new Dictionary<int, BattlePassChallenge>();
            Leaderboard = new List<KeyValuePair<int, int>>();
        }

        public void Initialize(IQueryAdapter dbClient)
        {
            LoadSeason(dbClient);
            LoadLevels(dbClient);
            LoadRewards(dbClient);
            LoadChallenges(dbClient);
            UpdateLeaderboard(dbClient);
            log.Info("BattlePass Manager -> LOADED");
        }

        private void LoadSeason(IQueryAdapter dbClient)
        {
            dbClient.SetQuery("SELECT * FROM `battlepass_seasons` WHERE `is_active` = '1' LIMIT 1");
            DataRow row = dbClient.getRow();
            if (row != null)
            {
                CurrentSeason = new BattlePassSeason(
                    Convert.ToInt32(row["id"]),
                    Convert.ToInt32(row["chapter"]),
                    Convert.ToInt32(row["season"]),
                    Convert.ToString(row["name"]),
                    Convert.ToString(row["description"]),
                    Convert.ToDateTime(row["start_date"]),
                    Convert.ToDateTime(row["end_date"]),
                    true
                );
            }
        }

        private void LoadLevels(IQueryAdapter dbClient)
        {
            Levels.Clear();
            dbClient.SetQuery("SELECT * FROM `battlepass_levels` ORDER BY `level` ASC");
            DataTable table = dbClient.getTable();
            if (table != null)
            {
                foreach (DataRow row in table.Rows)
                {
                    Levels.Add(Convert.ToInt32(row["level"]), Convert.ToInt32(row["exp_required"]));
                }
            }
        }

        private void LoadRewards(IQueryAdapter dbClient)
        {
            Rewards.Clear();
            dbClient.SetQuery("SELECT * FROM `battlepass_rewards` ORDER BY `level` ASC");
            DataTable table = dbClient.getTable();
            if (table != null)
            {
                foreach (DataRow row in table.Rows)
                {
                    int level = Convert.ToInt32(row["level"]);
                    Rewards.Add(level, new BattlePassReward(
                        level,
                        Convert.ToString(row["normal_reward_type"]),
                        Convert.ToString(row["normal_reward_value"]),
                        Convert.ToString(row["normal_reward_icon"]),
                        Convert.ToString(row["vip_reward_type"]),
                        Convert.ToString(row["vip_reward_value"]),
                        Convert.ToString(row["vip_reward_icon"])
                    ));
                }
            }
        }

        public void UpdateLeaderboard(IQueryAdapter dbClient)
        {
            Leaderboard.Clear();
            dbClient.SetQuery("SELECT `user_id`, `exp` FROM `battlepass_user_data` ORDER BY `exp` DESC, `level` DESC LIMIT 100");
            DataTable table = dbClient.getTable();
            if (table != null)
            {
                foreach (DataRow row in table.Rows)
                {
                    Leaderboard.Add(new KeyValuePair<int, int>(Convert.ToInt32(row["user_id"]), Convert.ToInt32(row["exp"])));
                }
            }
        }

        private void LoadChallenges(IQueryAdapter dbClient)
        {
            Categories.Clear();
            Challenges.Clear();

            dbClient.SetQuery("SELECT * FROM `battlepass_categories` ORDER BY `id` ASC");
            DataTable catTable = dbClient.getTable();
            if (catTable != null)
            {
                foreach (DataRow row in catTable.Rows)
                {
                    int id = Convert.ToInt32(row["id"]);
                    Categories.Add(new BattlePassCategory(
                        id,
                        Convert.ToString(row["name"]),
                        Convert.ToString(row["icon"]),
                        Convert.ToString(row["description"])
                    ));
                }
            }

            dbClient.SetQuery("SELECT * FROM `battlepass_challenges` ORDER BY `id` ASC");
            DataTable chalTable = dbClient.getTable();
            if (chalTable != null)
            {
                foreach (DataRow row in chalTable.Rows)
                {
                    int id = Convert.ToInt32(row["id"]);
                    int catId = Convert.ToInt32(row["category_id"]);
                    BattlePassChallenge challenge = new BattlePassChallenge(
                        id,
                        catId,
                        Convert.ToString(row["name"]),
                        Convert.ToString(row["icon"]),
                        Convert.ToString(row["description"]),
                        Convert.ToInt32(row["xp_reward"]),
                        Convert.ToInt32(row["total_progress"]),
                        Convert.ToString(row["goal_type"])
                    );

                    Challenges.Add(id, challenge);
                    var category = Categories.FirstOrDefault(c => c.Id == catId);
                    if (category != null)
                    {
                        category.Challenges.Add(challenge);
                    }
                }
            }
        }

        public BattlePassUserData GetUserData(int userId, IQueryAdapter dbClient)
        {
            dbClient.SetQuery("SELECT * FROM `battlepass_user_data` WHERE `user_id` = @uid LIMIT 1");
            dbClient.AddParameter("uid", userId);
            DataRow row = dbClient.getRow();

            BattlePassUserData userData;
            if (row != null)
            {
                userData = new BattlePassUserData(
                    userId,
                    Convert.ToInt32(row["level"]),
                    Convert.ToInt32(row["exp"])
                );
            }
            else
            {
                userData = new BattlePassUserData(userId, 1, 0);
                dbClient.SetQuery("INSERT INTO `battlepass_user_data` (`user_id`, `level`, `exp`) VALUES (@uid, 1, 0)");
                dbClient.AddParameter("uid", userId);
                dbClient.RunQuery();
            }

            // Load progress
            dbClient.SetQuery("SELECT `challenge_id`, `current_progress`, `claimed` FROM `battlepass_user_challenges` WHERE `user_id` = @uid");
            dbClient.AddParameter("uid", userId);
            DataTable progressTable = dbClient.getTable();
            if (progressTable != null)
            {
                foreach (DataRow pRow in progressTable.Rows)
                {
                    int chalId = Convert.ToInt32(pRow["challenge_id"]);
                    userData.ChallengeProgress[chalId] = Convert.ToInt32(pRow["current_progress"]);
                    if (Convert.ToInt32(pRow["claimed"]) == 1)
                        userData.ClaimedChallenges.Add(chalId);
                }
            }
            // Load claimed rewards
            dbClient.SetQuery("SELECT `level`, `type` FROM `battlepass_user_rewards` WHERE `user_id` = @uid");
            dbClient.AddParameter("uid", userId);
            DataTable rewardsTable = dbClient.getTable();
            if (rewardsTable != null)
            {
                foreach (DataRow rRow in rewardsTable.Rows)
                {
                    int level = Convert.ToInt32(rRow["level"]);
                    string type = Convert.ToString(rRow["type"]);
                    if (type == "normal") userData.ClaimedNormalRewards.Add(level);
                    else userData.ClaimedVipRewards.Add(level);
                }
            }

            return userData;
        }

        public void AwardExp(GameClient session, int amount)
        {
            if (session == null || session.GetRoleplay() == null) return;
            var userData = session.GetRoleplay().BattlePassData;

            userData.Exp += amount;

            bool leveledUp = false;
            while (Levels.ContainsKey(userData.Level + 1) && userData.Exp >= Levels[userData.Level + 1])
            {
                userData.Level++;
                leveledUp = true;
                session.SendWhisper("¡Has subido al nivel " + userData.Level + " del BattlePass!", 1);
            }

            if (leveledUp)
            {
                ClaimEligibleRewards(session);
            }

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                SaveUserData(userData, dbClient);
            }

            session.SendMessage(new Communication.Packets.Outgoing.BattlePass.BattlePassComposer(session, this));
        }

        public void ProgressChallenge(GameClient session, string goalType, int progress = 1)
        {
            if (session == null || session.GetRoleplay() == null) return;
            var userData = session.GetRoleplay().BattlePassData;

            bool changed = false;
            foreach (var challenge in Challenges.Values.Where(c => c.GoalType.Equals(goalType, StringComparison.OrdinalIgnoreCase)))
            {
                int current = 0;
                userData.ChallengeProgress.TryGetValue(challenge.Id, out current);

                if (current >= challenge.TotalProgress) continue;

                int newProgress = Math.Min(challenge.TotalProgress, current + progress);
                userData.ChallengeProgress[challenge.Id] = newProgress;
                changed = true;

                if (newProgress >= challenge.TotalProgress)
                {
                    session.SendWhisper("¡Has completado el desafío: " + challenge.Name + "!", 1);
                    AwardExp(session, challenge.XpReward);
                }
            }

            if (changed)
            {
                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    SaveUserData(userData, dbClient);
                }
                session.SendMessage(new Communication.Packets.Outgoing.BattlePass.BattlePassComposer(session, this));
            }
        }

        public void ClaimEligibleRewards(GameClient session)
        {
            if (session == null || session.GetRoleplay() == null) return;
            var userData = session.GetRoleplay().BattlePassData;
            bool isVip = session.GetHabbo().VIPRank > 0;
            bool claimedAnything = false;

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                foreach (var reward in Rewards.Values.Where(r => r.Level <= userData.Level))
                {
                    // Normal Reward
                    if (!userData.ClaimedNormalRewards.Contains(reward.Level))
                    {
                        GiveReward(session, reward.NormalRewardType, reward.NormalRewardValue);
                        userData.ClaimedNormalRewards.Add(reward.Level);
                        dbClient.SetQuery("INSERT INTO `battlepass_user_rewards` (`user_id`, `level`, `type`) VALUES (@uid, @lvl, 'normal')");
                        dbClient.AddParameter("uid", session.GetHabbo().Id);
                        dbClient.AddParameter("lvl", reward.Level);
                        dbClient.RunQuery();
                        claimedAnything = true;
                    }

                    // VIP Reward
                    if (isVip && !userData.ClaimedVipRewards.Contains(reward.Level))
                    {
                        GiveReward(session, reward.VipRewardType, reward.VipRewardValue);
                        userData.ClaimedVipRewards.Add(reward.Level);
                        dbClient.SetQuery("INSERT INTO `battlepass_user_rewards` (`user_id`, `level`, `type`) VALUES (@uid, @lvl, 'vip')");
                        dbClient.AddParameter("uid", session.GetHabbo().Id);
                        dbClient.AddParameter("lvl", reward.Level);
                        dbClient.RunQuery();
                        claimedAnything = true;
                    }
                }
            }

            if (claimedAnything)
            {
                session.SendMessage(new Communication.Packets.Outgoing.BattlePass.BattlePassComposer(session, this));
            }
        }

        public void GiveReward(GameClient session, string type, string value)
        {
            switch (type.ToLower())
            {
                case "credits":
                    if (int.TryParse(value, out int credits))
                    {
                        session.GetHabbo().Credits += credits;
                        session.GetHabbo().UpdateCreditsBalance();
                    }
                    break;
                case "duckets":
                    if (int.TryParse(value, out int duckets))
                    {
                        session.GetHabbo().Duckets += duckets;
                        session.GetHabbo().UpdateDucketsBalance();
                    }
                    break;
                case "diamonds":
                    if (int.TryParse(value, out int diamonds))
                    {
                        session.GetHabbo().Diamonds += diamonds;
                        session.GetHabbo().UpdateDiamondsBalance();
                    }
                    break;
                case "item":
                    if (int.TryParse(value, out int baseId))
                    {
                        if (PolarEnvironment.GetGame().GetItemManager().GetItem(baseId, out ItemData itemData))
                        {
                            Item item = ItemFactory.CreateSingleItemNullable(itemData, session.GetHabbo(), "", "");
                            if (item != null)
                            {
                                session.GetHabbo().GetInventoryComponent().TryAddItem(item);
                                session.SendMessage(new Communication.Packets.Outgoing.Inventory.Furni.FurniListNotificationComposer(item.Id, 1));
                                session.SendMessage(new Communication.Packets.Outgoing.Inventory.Furni.FurniListAddComposer(item));
                                session.SendMessage(new Communication.Packets.Outgoing.Inventory.Furni.FurniListUpdateComposer());
                            }
                        }
                    }
                    break;
                case "exp":
                    if (int.TryParse(value, out int exp))
                    {
                        AwardExp(session, exp);
                    }
                    break;
                case "badge":
                    session.GetHabbo().GetBadgeComponent().GiveBadge(value, true, session);
                    break;
                case "vip":
                    if (int.TryParse(value, out int days))
                    {
                        session.GetHabbo().GetClubManager().AddOrExtendSubscription("habbo_vip", days * 24 * 3600, session);
                        session.GetHabbo().GetBadgeComponent().GiveBadge("HC1", true, session);
                        session.GetHabbo().GetPermissions().Init(session.GetHabbo());
                        session.SendMessage(new Communication.Packets.Outgoing.Users.ScrSendUserInfoComposer(session.GetHabbo()));
                        session.GetHabbo().GetClubManager().ReloadSubscription(session);

                        if (session.GetHabbo().VIPRank < 1)
                        {
                            session.GetHabbo().VIPRank = 1;
                            using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                                dbClient.RunQuery("UPDATE `users` SET `rank_vip` = '1' WHERE `id` = '" + session.GetHabbo().Id + "'");
                        }
                    }
                    break;
            }
        }

        public void SaveUserData(BattlePassUserData userData, IQueryAdapter dbClient)
        {
            dbClient.SetQuery("UPDATE `battlepass_user_data` SET `level` = @lvl, `exp` = @exp WHERE `user_id` = @uid");
            dbClient.AddParameter("lvl", userData.Level);
            dbClient.AddParameter("exp", userData.Exp);
            dbClient.AddParameter("uid", userData.UserId);
            dbClient.RunQuery();

            // Save progress (simplified, maybe inefficient but works for now)
            foreach (var progress in userData.ChallengeProgress)
            {
                bool isClaimed = userData.ClaimedChallenges.Contains(progress.Key);

                dbClient.SetQuery("REPLACE INTO `battlepass_user_challenges` (`user_id`, `challenge_id`, `current_progress`, `claimed`) VALUES (@uid, @cid, @prog, @claimed)");
                dbClient.AddParameter("uid", userData.UserId);
                dbClient.AddParameter("cid", progress.Key);
                dbClient.AddParameter("prog", progress.Value);
                dbClient.AddParameter("claimed", isClaimed ? 1 : 0);
                dbClient.RunQuery();
            }
        }
    }
}