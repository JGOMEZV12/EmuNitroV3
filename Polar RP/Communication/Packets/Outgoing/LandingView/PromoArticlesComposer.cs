using System;
using System.Data;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.LandingView;
using Polar.HabboHotel.LandingView.Promotions;


namespace Polar.Communication.Packets.Outgoing.LandingView
{
    internal class PromoArticlesComposer : ServerPacket
    {
        public ICollection<Promotion> LandingPromotions { get; }

        public PromoArticlesComposer(ICollection<Promotion> LandingPromotions)
            : base(ServerPacketHeader.PromoArticlesMessageComposer)
        {
            this.LandingPromotions = LandingPromotions;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(LandingPromotions.Count);//Count
            foreach (Promotion Promotion in LandingPromotions.ToList())
            {
                packet.WriteInteger(Promotion.Id); //ID
                packet.WriteString(Promotion.Title); //Title
                packet.WriteString(Promotion.Text); //Text
                packet.WriteString(Promotion.ButtonText); //Button text
                packet.WriteInteger(Promotion.ButtonType); //Link type 0 and 3
                packet.WriteString(Promotion.ButtonLink); //Link to article
                packet.WriteString(Promotion.ImageLink); //Image link
            }
        }
    }
}