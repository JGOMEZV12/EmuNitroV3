using Polar.HabboHotel.Subscriptions;
using Polar.Communication.Packets.Outgoing.Handshake;
using Polar.Database.Interfaces;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Users.UserDataManagement;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using Polar.Communication.Packets.Outgoing.Users;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;

namespace Polar.HabboHotel.Subscriptions
{
    public class ClubManager
    {
        private readonly int UserId;
        private readonly Dictionary<string, Subscription> Subscriptions;

        internal ClubManager(int userID, UserData userData)
        {
            this.UserId = userID;
            this.Subscriptions = userData.subscriptions;
        }

        internal void Clear()
        {
            this.Subscriptions.Clear();
        }

        internal Subscription GetSubscription(string SubscriptionId)
        {
            if (this.Subscriptions.ContainsKey(SubscriptionId))
            {
                return this.Subscriptions[SubscriptionId];
            }
            else
            {
                return (Subscription)null;
            }
        }

        internal bool HasSubscription(string SubscriptionId)
        {
            if (!this.Subscriptions.ContainsKey(SubscriptionId))
            {
                return false;
            }

            Subscription subscription = this.Subscriptions[SubscriptionId];
            return subscription.IsValid();
        }
        internal void TimeExpired(string SubscriptionId, int DurationSeconds, GameClient Session)
        {
            // Add null check for Session and Habbo
            if (Session?.GetHabbo() == null)
                return;

            // Declare two dates
            var prevDate = new DateTime(1970, 1, 1).AddSeconds(DurationSeconds).ToLocalTime();
            var today = DateTime.Now;

            //get difference of two dates
            var diffOfDates = prevDate - today;

            if (diffOfDates.Days == 0)
            {
                using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.RunQuery("UPDATE `users` SET `rank_vip` = '0', `colour` = '' WHERE `id` = '" + Session.GetHabbo().Id + "'");
                    dbClient.RunQuery("DELETE FROM `user_subscriptions` WHERE `user_id` = '" + Session.GetHabbo().Id + "'");
                }

                ReloadSubscription(Session);
                Session.GetHabbo().VIPRank = 0;
                Session.GetHabbo().NameColor = "";

                // Add null check for room user
                var roomUser = Session.GetRoomUser();
                if (roomUser?.GetRoom() != null)
                {
                    Session.SendMessage(new UserNameChangeComposer(Session.GetRoomUser().GetRoom().Id, Session.GetRoomUser().VirtualId, Session.GetHabbo().Username));
                }

                // Add null check for roleplay
                var roleplay = Session.GetRoleplay();
                if (roleplay != null)
                {
                    Session.GetRoleplay().MaxHealth = Session.GetRoleplay().MaxHealth - 150;
                    Session.GetRoleplay().MaxEnergy = Session.GetRoleplay().MaxEnergy - 150;
                }

                Session.GetHabbo().GetPermissions().Init(Session.GetHabbo());
                Session.SendMessage(new ScrSendUserInfoComposer(Session.GetHabbo()));
                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_purse", "hc");
            }
        }
        internal void AddOrExtendSubscription(string SubscriptionId, int DurationSeconds, GameClient Session)
        {
            SubscriptionId = SubscriptionId.ToLower();

            var clientByUserId = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(UserId);
            if (!this.Subscriptions.ContainsKey(SubscriptionId))
            {
                int unixTimestamp = (int)PolarEnvironment.GetUnixTimestamp();
                int timeExpire = (int)PolarEnvironment.GetUnixTimestamp() + DurationSeconds;
                string SubscriptionType = SubscriptionId;
                Subscription subscription2 = new Subscription(SubscriptionId, timeExpire, unixTimestamp);
                Session.GetHabbo().NameColor = "B3A61F";
                Session.SendMessage(new RoomNotificationComposer("HC2_HHSG", "message", "¡Compraste un mes del " + PolarEnvironment.GetConfig().data["hotel.name"] + " CLUB! Recibiste <b>250.000 créditos</b> de regalo por suscribirte al " + PolarEnvironment.GetConfig().data["hotel.name"] + " Club por favor, escribe  <b>:vipcommands</b> para ver tus comandos VIs."));
                using (IQueryAdapter adapter = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    adapter.SetQuery("INSERT INTO user_subscriptions (user_id,subscription_id,timestamp_activated,timestamp_expire) VALUES (" + this.UserId + ",'" + SubscriptionType + "'," + unixTimestamp + "," + timeExpire + ")");
                    adapter.RunQuery();
                    adapter.SetQuery("UPDATE `users` SET `colour` = 'B3A61F' WHERE `id` = '" + Session.GetHabbo().Id + "'");
                    adapter.RunQuery();
                    adapter.SetQuery("UPDATE `users` SET `rank_vip` = '1' WHERE `id` = '" + Session.GetHabbo().Id + "'");
                    adapter.RunQuery();
                }

                Session.GetHabbo().VIPRank = 1; 

                if (!Session.GetHabbo().GetBadgeComponent().HasBadge("HC8"))
                    Session.GetHabbo().GetBadgeComponent().GiveBadge("HC8", true, Session);

                Session.GetHabbo().Credits += 250000;
                Session.GetHabbo().UpdateCreditsBalance();
                if (!(Session.GetRoleplay().MaxHealth >= 300 && Session.GetRoleplay().MaxEnergy >= 300))
                {
                    Session.GetRoleplay().MaxHealth = Session.GetRoleplay().MaxHealth + 150;
                    Session.GetRoleplay().MaxEnergy = Session.GetRoleplay().MaxEnergy + 150;
                }
                Session.GetRoleplay().Caramelos += 500;
                Session.GetRoleplay().RefreshStatDialogue();
                this.Subscriptions.Add(subscription2.SubscriptionId.ToLower(), subscription2);
                PolarEnvironment.GetGame().GetAchievementManager().TryProgressHabboClubAchievements(clientByUserId);
                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_purse", "hc");
                //Session.SendMessage(new UserNameChangeComposer(Session.GetRoomUser().GetRoom().Id, Session.GetRoomUser().VirtualId, "[VIP]" + Session.GetHabbo().Username));
            }
            else
            {
                Subscription subscription = this.Subscriptions[SubscriptionId];

                if (subscription.IsValid())
                {
                    subscription.ExtendSubscription(DurationSeconds);
                }
                else
                {
                    subscription.SetEndTime((int)PolarEnvironment.GetUnixTimestamp() + DurationSeconds);
                }
                Session.GetHabbo().NameColor = "B3A61F";
                Session.GetHabbo().VIPRank = 1; ;
                Session.SendMessage(new RoomNotificationComposer("HC2_HHSG", "message", "¡Compraste un mes del " + PolarEnvironment.GetConfig().data["hotel.name"] + " CLUB! escribe  <b>:vipcommands</b> para ver tus comandos VIP"));
                using (IQueryAdapter adapter = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    adapter.SetQuery("UPDATE user_subscriptions SET timestamp_expire = " + subscription.ExpireTime + " WHERE user_id = " + this.UserId + " AND subscription_id = '" + subscription.SubscriptionId + "'");
                    adapter.RunQuery();
                    adapter.SetQuery("UPDATE `users` SET `colour` = 'B3A61F' WHERE `id` = '" + Session.GetHabbo().Id + "'");
                    adapter.RunQuery();
                    adapter.SetQuery("UPDATE `users` SET `rank_vip` = '1' WHERE `id` = '" + Session.GetHabbo().Id + "'");
                    adapter.RunQuery();
                }

                Session.GetHabbo().VIPRank = 1;
                PolarEnvironment.GetGame().GetAchievementManager().TryProgressHabboClubAchievements(clientByUserId);
                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_purse", "hc");
                //Session.SendMessage(new UserNameChangeComposer(Session.GetRoomUser().GetRoom().Id, Session.GetRoomUser().VirtualId, "[VIP]" + Session.GetHabbo().Username));
            }
        }


        internal void ReloadSubscription(GameClient Session)
        {
            Session.SendMessage(new UserRightsComposer(Session.GetHabbo()));
        }
    }
}