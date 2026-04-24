using Polar;
using Polar.Core;
using Polar.Database.Interfaces;
using Polar.HabboHotel.GameClients;

namespace Polar.HabboHotel.Subscriptions
{
    public class Subscription

    {
        private readonly string Caption;
        private int TimeExpire;
        private int ActivateTimes;
        private readonly int UserId;

        internal string SubscriptionId
        {
            get
            {
                return this.Caption;
            }
        }

        internal int ExpireTime
        {
            get
            {
                return this.TimeExpire;
            }


        }


        internal int ActivateTime
        {
            get
            {
                return this.ActivateTimes;
            }
        }

        internal Subscription(string caption, int timeExpire, int activateTimes)
        {
            this.Caption = caption;
            this.TimeExpire = timeExpire;
            this.ActivateTimes = activateTimes;
        }

        internal bool IsValid()
        {
            return this.TimeExpire > PolarEnvironment.GetUnixTimestamp();
            

        }

        internal void SetEndTime(int time)
        {
            this.TimeExpire = time;

            GameClient Client = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(this.UserId);

            using (IQueryAdapter adapter = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                adapter.SetQuery("UPDATE `users` SET `rank_vip` = '0', `colour` = '000000' WHERE `id` = '" + Client.GetHabbo().Id + "'");
                adapter.RunQuery();
            }

        }

        internal void ExtendSubscription(int Time)
        {
            try
            {
                this.TimeExpire = (this.TimeExpire + Time);
            }
            catch
            {
            }
        }
    }
}
