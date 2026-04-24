using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.HabboHotel.Cache;
using Polar.HabboRoleplay.Bots;
using Polar.HabboRoleplay.Bots.Manager;
using Polar.HabboHotel.Users;
using Polar.HabboHotel.Groups;
using Polar.Communication.Packets.Outgoing.Users;
using Polar.Database.Interfaces;
using Polar.Communication.Packets.Outgoing.Groups;
using Polar.HabboRoleplay.Web.Outgoing.Statistics;

namespace Polar.Communication.Packets.Incoming.Users
{
    internal class OpenPlayerProfileEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            int userID = Packet.PopInt();
            /*// Fix
                        Session.SendNotification("Esta función ha sido deshabilitada por los Administradores.");
                        return;*/

            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_userprofile", "" + userID);
            /*Boolean IsMe = Packet.PopBoolean();

            if (userID >= 5000000)
            {
                Group Group = GroupManager.GetGang(userID - 5000000);
                Session.SendMessage(new GroupInfoComposer(Group, Session, true));
                return;
            }

            if (userID > 1000000)
            {
                RoleplayBot Bot = RoleplayBotManager.GetCachedBotById(userID - 1000000);

                List<Group> BotGroups = new List<Group>();
                if (Bot.Corporation > 0)
                {
                    Group Job = GroupManager.GetJob(Bot.Corporation);

                    if (Job != null)
                        BotGroups.Add(Job);
                }
                else
                {
                    Group Job = GroupManager.GetJob(1);

                    if (Job != null)
                        BotGroups.Add(Job);
                }

                Group Gang = GroupManager.GetGang(1000);

                if (Gang != null)
                    BotGroups.Add(Gang);

                int BotFriendCount = 0;

                using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("SELECT COUNT(0) FROM `rp_bots_friendships` WHERE `bot_id` = '" + Bot.Id + "'");
                    BotFriendCount = dbClient.getInteger();
                }
                //Session.SendMessage(new ProfileInformationComposer(Session, BotGroups, BotFriendCount, Bot));
                return;
            }

            UserCache targetData = PolarEnvironment.GetGame().GetCacheManager().GenerateUser(userID);
            

            int friendCount = 0;
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT COUNT(*) FROM `messenger_friendships` WHERE (`user_one_id` = @userid OR `user_two_id` = @userid)");
                dbClient.AddParameter("userid", userID);
                friendCount = dbClient.getInteger();
            }
            PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(Session, "compose_mostrarperfil|" + targetData.Id);
            //Session.SendMessage(new ProfileInformationComposer(targetData, Session));*/

        }
    }
}
