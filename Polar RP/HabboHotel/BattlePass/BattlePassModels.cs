using System;
using System.Collections.Generic;

namespace Polar.HabboHotel.BattlePass
{
    public class BattlePassSeason
    {
        public int Id { get; set; }
        public int Chapter { get; set; }
        public int Season { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }

        public BattlePassSeason(int id, int chapter, int season, string name, string description, DateTime startDate, DateTime endDate, bool isActive)
        {
            Id = id;
            Chapter = chapter;
            Season = season;
            Name = name;
            Description = description;
            StartDate = startDate;
            EndDate = endDate;
            IsActive = isActive;
        }
    }

    public class BattlePassReward
    {
        public int Level { get; set; }
        public string NormalRewardType { get; set; }
        public string NormalRewardValue { get; set; }
        public string NormalRewardIcon { get; set; }
        public string VipRewardType { get; set; }
        public string VipRewardValue { get; set; }
        public string VipRewardIcon { get; set; }

        public BattlePassReward(int level, string nType, string nValue, string nIcon, string vType, string vValue, string vIcon)
        {
            Level = level;
            NormalRewardType = nType;
            NormalRewardValue = nValue;
            NormalRewardIcon = nIcon;
            VipRewardType = vType;
            VipRewardValue = vValue;
            VipRewardIcon = vIcon;
        }
    }

    public class BattlePassCategory
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public string Description { get; set; }
        public List<BattlePassChallenge> Challenges { get; set; }

        public BattlePassCategory(int id, string name, string icon, string description)
        {
            Id = id;
            Name = name;
            Icon = icon;
            Description = description;
            Challenges = new List<BattlePassChallenge>();
        }
    }

    public class BattlePassChallenge
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public string Description { get; set; }
        public int XpReward { get; set; }
        public int TotalProgress { get; set; }
        public string GoalType { get; set; }

        public BattlePassChallenge(int id, int categoryId, string name, string icon, string description, int xpReward, int totalProgress, string goalType)
        {
            Id = id;
            CategoryId = categoryId;
            Name = name;
            Icon = icon;
            Description = description;
            XpReward = xpReward;
            TotalProgress = totalProgress;
            GoalType = goalType;
        }
    }

    public class BattlePassUserData
    {
        public int UserId { get; set; }
        public int Level { get; set; }
        public int Exp { get; set; }
        public Dictionary<int, int> ChallengeProgress { get; set; }
        public List<int> ClaimedNormalRewards { get; set; }
        public List<int> ClaimedVipRewards { get; set; }

        public List<int> ClaimedChallenges { get; set; }

        public BattlePassUserData(int userId, int level, int exp)
        {
            UserId = userId;
            Level = level;
            Exp = exp;
            ChallengeProgress = new Dictionary<int, int>();
            ClaimedNormalRewards = new List<int>();
            ClaimedVipRewards = new List<int>();
            ClaimedChallenges = new List<int>();
        }
    }
}