using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.BattlePass;
using Polar.Communication.Packets.Outgoing.BattlePass;
using Polar.Database.Interfaces;
using System;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Users.Inventory;
using Polar.Communication.Packets.Outgoing.Inventory.Purse;
using Polar.Communication.Packets.Outgoing.Inventory.Furni;

namespace Polar.Communication.Packets.Incoming.BattlePass
{
    public class ClaimBattlePassRewardEvent : IPacketEvent
    {
        public void Parse(GameClient session, ClientPacket packet)
        {
            if (session == null || session.GetHabbo() == null || session.GetRoleplay() == null)
                return;

            int level = packet.PopInt();
            bool isVip = packet.PopBoolean();

            BattlePassManager manager = PolarEnvironment.GetGame().GetBattlePassManager();
            var userData = session.GetRoleplay().BattlePassData;

            if (userData.Level < level)
            {
                session.SendWhisper("Aún no has alcanzado el nivel " + level + ".", 1);
                return;
            }

            if (isVip && session.GetHabbo().VIPRank <= 0)
            {
                session.SendWhisper("Necesitas ser VIP para reclamar esta recompensa.", 1);
                return;
            }

            if (!manager.Rewards.ContainsKey(level)) return;
            var reward = manager.Rewards[level];

            if (isVip)
            {
                if (userData.ClaimedVipRewards.Contains(level))
                {
                    session.SendWhisper("Ya has reclamado esta recompensa VIP.", 1);
                    return;
                }
                manager.GiveReward(session, reward.VipRewardType, reward.VipRewardValue);
                userData.ClaimedVipRewards.Add(level);

                // ✅ Si la recompensa VIP tiene XP asociado, darlo
                if (reward.VipRewardType.ToLower() == "exp" && int.TryParse(reward.VipRewardValue, out int vipExp))
                    manager.AwardExp(session, vipExp);
            }
            else
            {
                if (userData.ClaimedNormalRewards.Contains(level))
                {
                    session.SendWhisper("Ya has reclamado esta recompensa normal.", 1);
                    return;
                }
                manager.GiveReward(session, reward.NormalRewardType, reward.NormalRewardValue);
                userData.ClaimedNormalRewards.Add(level);

                // ✅ Si la recompensa normal tiene XP asociado, darlo
                if (reward.NormalRewardType.ToLower() == "exp" && int.TryParse(reward.NormalRewardValue, out int normalExp))
                    manager.AwardExp(session, normalExp);
            }

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("INSERT INTO `battlepass_user_rewards` (`user_id`, `level`, `type`) VALUES (@uid, @lvl, @type)");
                dbClient.AddParameter("uid", session.GetHabbo().Id);
                dbClient.AddParameter("lvl", level);
                dbClient.AddParameter("type", isVip ? "vip" : "normal");
                dbClient.RunQuery();
            }

            session.SendMessage(new BattlePassComposer(session, manager));
        }
    }
}
