using Polar.HabboHotel.BattlePass;
using Polar.HabboHotel.GameClients;
using System.Linq;

namespace Polar.Communication.Packets.Outgoing.BattlePass
{
    public class BattlePassComposer : ServerPacket
    {
        public BattlePassComposer(GameClient session, BattlePassManager manager)
            : base(ServerPacketHeader.BattlePassMessageComposer)
        {
            var userData = session.GetRoleplay().BattlePassData;
            var season = manager.CurrentSeason;
            int ranking = manager.Leaderboard.FindIndex(x => x.Key == session.GetHabbo().Id) + 1;
            if (ranking == 0) ranking = manager.Leaderboard.Count + 1;

            // Header
            WriteInteger(season?.Chapter ?? 1);
            WriteInteger(season?.Season ?? 1);
            WriteString(season?.Name ?? "");               // nuevo
            WriteString(season?.Description ?? "");        // nuevo
            WriteString(season?.StartDate.ToString("dd/MM/yyyy") ?? ""); // nuevo
            WriteString(season?.EndDate.ToString("dd/MM/yyyy") ?? "");   // nuevo
            WriteInteger(0); // o el valor real si lo tienes
            WriteInteger(userData.Level);
            WriteInteger(userData.Exp);
            WriteInteger(manager.Levels.ContainsKey(userData.Level + 1) ? manager.Levels[userData.Level + 1] : 0);
            WriteInteger(ranking);
            WriteBoolean(session.GetHabbo().VIPRank > 0);

            // Rewards
            WriteInteger(manager.Rewards.Count);
            foreach (var reward in manager.Rewards.Values)
            {
                WriteInteger(reward.Level);
                WriteString(reward.NormalRewardType);   // normalRewardName
                WriteString(reward.NormalRewardIcon);   // normalRewardIcon (el parser saltea Value)
                WriteBoolean(userData.ClaimedNormalRewards.Contains(reward.Level)); // normalClaimed
                WriteString(reward.VipRewardType);      // vipRewardName
                WriteString(reward.VipRewardIcon);      // vipRewardIcon
                WriteBoolean(userData.ClaimedVipRewards.Contains(reward.Level));    // vipClaimed
                WriteBoolean(false);                    // isVipOnly (agregar campo si aplica)
            }

            // Categories
            WriteInteger(manager.Categories.Count);
            foreach (var category in manager.Categories)
            {
                WriteInteger(category.Id);
                WriteString(category.Name);
                WriteString(category.Description); // ⚠️ invertido respecto al original
                WriteString(category.Icon);        // ⚠️ invertido respecto al original

                WriteInteger(category.Challenges.Count);
                foreach (var challenge in category.Challenges)
                {
                    WriteInteger(challenge.Id);
                    WriteString(challenge.Name);
                    WriteString(challenge.Description);
                    WriteInteger(userData.ChallengeProgress.ContainsKey(challenge.Id)
                        ? userData.ChallengeProgress[challenge.Id] : 0);
                    WriteInteger(challenge.TotalProgress);
                    WriteInteger(challenge.XpReward);
                    WriteString(challenge.Icon);
                    WriteBoolean(userData.ClaimedChallenges.Contains(challenge.Id)); // ← FALTABA ESTO
                }
            }
        
        }
    }
}