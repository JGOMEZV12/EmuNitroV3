using System;
using System.Threading;
using Polar.HabboRoleplay.Bots;

namespace Polar.HabboRoleplay.RoleplayUsers.Offers
{
    public class RoleplayOffer
    {
        public string Type;
        public int OffererId;
        public int Cost;
        public object[] Params;

        public RoleplayOffer(string Type, int OffererId, int Cost, object[] Params)
        {
            this.Type = Type;
            this.OffererId = OffererId;
            this.Cost = Cost;
            this.Params = Params;
        }
    }
}