using Polar.Database.Interfaces;
using Polar.HabboHotel.Cache;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.Users;
using Polar.HabboRoleplay.Bots;
using Polar.HabboRoleplay.Bots.Manager;
using Polar.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace Polar.Communication.Packets.Outgoing.Users
{
    internal class ProfileInformationComposer : ServerPacket
    {

        public ProfileInformationComposer(UserCache habboInfo, GameClient viewer)
    : base(ServerPacketHeader.ProfileInformationMessageComposer)
        {
            if (habboInfo == null)
                return;

            // Achievement score
            int achievementScore = 0;
            Habbo habbo = PolarEnvironment.GetHabboById(habboInfo.Id);

            if (habbo != null)
            {
                achievementScore = habbo.GetStats().AchievementPoints;
            }
            else
            {
                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("SELECT `achievement_score` FROM `users_settings` WHERE `user_id` = @userId LIMIT 1");
                    dbClient.AddParameter("userId", habboInfo.Id);
                    achievementScore = dbClient.getInteger();
                }
            }

            // Friend count
            int friendCount;
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT COUNT(0) FROM `messenger_friendships` WHERE (`user_one_id` = @userId OR `user_two_id` = @userId)");
                dbClient.AddParameter("userId", habboInfo.Id);
                friendCount = dbClient.getInteger();
            }

            DateTime accountCreated = new DateTime(1970, 1, 1, 0, 0, 0, 0)
                .AddSeconds(habboInfo.AccountCreated).ToLocalTime();

            // Is friend / friend request - null safe
            bool isFriend = false;
            bool friendRequestSent = false;

            if (viewer?.GetHabbo()?.GetMessenger() != null)
            {
                isFriend = viewer.GetHabbo().GetMessenger().FriendshipExists(habboInfo.Id);
                friendRequestSent = viewer.GetHabbo().GetMessenger().RequestExists(habboInfo.Id);
            }

            base.WriteInteger(habboInfo.Id);
            base.WriteString(habboInfo.Username ?? "");
            base.WriteString(habboInfo.Look ?? "");
            base.WriteString(habboInfo.Motto ?? "");
            base.WriteString(accountCreated.ToString("dd-MM-yyyy"));
            base.WriteInteger(achievementScore);
            base.WriteInteger(friendCount);
            base.WriteBoolean(isFriend);
            base.WriteBoolean(friendRequestSent);
            base.WriteBoolean(PolarEnvironment.EnumToBool(Convert.ToString(habboInfo.Online)) ||
                              PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(habboInfo.Id) != null);

            // Guilds
            int userId = habbo?.Id ?? habboInfo.Id;
            List<Group> guilds = new List<Group>();

            List<Group> gangs = PolarEnvironment.GetGame().GetGroupManager().GetGangsForUser(userId);
            List<Group> jobs = PolarEnvironment.GetGame().GetGroupManager().GetJobsForUser(userId);

            if (gangs != null) guilds.AddRange(gangs.Where(g => g != null));
            if (jobs != null) guilds.AddRange(jobs.Where(g => g != null));

            base.WriteInteger(guilds.Count);
            foreach (Group group in guilds)
            {
                if (group == null)
                    continue;

                // Roleplay data - solo disponible si el usuario está online
                int jobId = 0;
                int gangId = 0;

                if (habboInfo?.GetRoleplay() != null)
                {
                    jobId = habboInfo.GetRoleplay().JobId;
                    gangId = habboInfo.GetRoleplay().GangId;
                }

                bool isFavourite = group.IsGang
                    ? group.Id == gangId
                    : group.Id == jobId;

                base.WriteInteger(group.Id);
                base.WriteString(group.Name ?? "");
                base.WriteString(group.Badge ?? "");
                base.WriteString(group.Colour1 ?? "");
                base.WriteString(group.Colour2 ?? "");
                base.WriteBoolean(isFavourite);
                base.WriteInteger(group.CreatorId);
                base.WriteBoolean(false);
            }

            base.WriteInteger(Convert.ToInt32(PolarEnvironment.GetUnixTimestamp() - habboInfo.LastOnline));
            base.WriteBoolean(true);
        }

        public ProfileInformationComposer(GameClient Session, List<Group> Groups, int friendCount, RoleplayBot Bot)
            : base(ServerPacketHeader.ProfileInformationMessageComposer)
        {
            int FakeBotId = Bot.Id + 1000000;
            Habbo Habbo = PolarEnvironment.GetHabboById(1);

            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(Habbo.AccountCreated).ToLocalTime();

            base.WriteInteger(FakeBotId);
            base.WriteString(Bot.Name);
            base.WriteString(Bot.Figure);
            base.WriteString("ciudadano");
            base.WriteString(origin.ToString("dd/MM/yyyy"));
            base.WriteInteger(0); // Achievement
            base.WriteInteger(friendCount); // Friend Count
            base.WriteBoolean(Session.GetRoleplay().FriendsWithBot(Bot.Id)); //  Is friend
            base.WriteBoolean(false); // Sent friend request
            base.WriteBoolean(true);

            base.WriteInteger(Groups.Count);
            foreach (Group Group in Groups)
            {
                base.WriteInteger(Group.Id);
                base.WriteString(Group.Name);
                base.WriteString(Group.Badge);
                // packet.WriteString(PolarEnvironment.GetGame().GetGroupManager().GetGroupColour(Group.Colour1, true));
                ///packet.WriteString(PolarEnvironment.GetGame().GetGroupManager().GetGroupColour(Group.Colour2, false));
                base.WriteString(Group.Colour1);//PlusEnvironment.GetGame().GetGroupManager().GetGroupColour(1, true)
                base.WriteString(Group.Colour2);//Group.Colour2
                if (Group.Id == Bot.Corporation)
                    base.WriteBoolean(true);
                else
                    base.WriteBoolean(false);
                base.WriteInteger(0);
                base.WriteBoolean(Group != null ? Group.ForumEnabled : true);
            }

            base.WriteInteger(0);
            base.WriteBoolean(true);
        }
    }
}