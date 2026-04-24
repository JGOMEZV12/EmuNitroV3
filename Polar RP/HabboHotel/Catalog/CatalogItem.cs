using System;
using System.Collections.Generic;
using System.Data;
using Polar.Core;
using Polar.HabboHotel.Items;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.GameClients;

namespace Polar.HabboHotel.Catalog
{
    public class CatalogItem
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public ItemData Data { get; set; }
        public string Name { get; set; }
        public int PageId { get; set; }
        public int Amount { get; set; }
        public int CostCredits { get; set; }
        public int CostPixels { get; set; }
        public int CostDiamonds { get; set; }
        public string ExtraData { get; set; }
        public string Badge { get; set; }
        public bool OfferActive { get; set; } // Renombrado de HaveOffer
        public int OfferId { get; set; }
        public bool IsLimited { get; set; }
        public int LimitedEditionStack { get; set; }
        public int LimitedEditionSells { get; set; }

        public CatalogItem(int Id, int ItemId, ItemData Data, string CatalogName, int PageId, int CostCredits,
            int CostPixels, int CostDiamonds, int Amount, int LimitedEditionSells, int LimitedEditionStack,
            bool OfferActive, string ExtraData, string Badge, int offerId)
        {
            this.Id = Id;
            this.ItemId = ItemId;
            this.Data = Data;
            this.Name = CatalogName;
            this.PageId = PageId;
            this.CostCredits = CostCredits;
            this.CostPixels = CostPixels;
            this.CostDiamonds = CostDiamonds;
            this.Amount = Amount;
            this.LimitedEditionSells = LimitedEditionSells;
            this.LimitedEditionStack = LimitedEditionStack;
            this.IsLimited = (LimitedEditionStack > 0);
            this.OfferActive = OfferActive;
            this.ExtraData = ExtraData ?? string.Empty;
            this.Badge = Badge ?? string.Empty;
            this.OfferId = offerId;
        }

        // Propiedad para compatibilidad (mismo nombre que en CatalogPage)
        public int PageID => PageId;

        // Propiedad para compatibilidad (mismo nombre antiguo)
        public bool HaveOffer => OfferActive;

        public ItemData GetBaseItem(int itemId)
        {
            ItemData itemData;
            if (!PolarEnvironment.GetGame().GetItemManager().GetItem(itemId, out itemData))
            {
                if (this.Name != "room_ad_plus_badge")
                {
                    //Console.WriteLine($"UNKNOWN ItemId: {itemId}");
                }
                return null;
            }

            return itemData;
        }

        // Versión mejorada que usa el ItemId de la instancia
        public ItemData GetBaseItem()
        {
            return GetBaseItem(this.ItemId);
        }

        public void SerializeClub(ServerPacket Message, GameClient Session)
        {
            Message.WriteInteger(Id);
            Message.WriteString(Name);
            Message.WriteBoolean(false); // IsRentable
            Message.WriteInteger(CostCredits);

            if (CostDiamonds > 0)
            {
                Message.WriteInteger(CostDiamonds);
                Message.WriteInteger(5); // Tipo moneda: Diamantes
            }
            else
            {
                Message.WriteInteger(CostPixels);
                Message.WriteInteger(0); // Tipo moneda: Duckets
            }

            Message.WriteBoolean(true); // Se puede regalar

            int days = 0;
            int months = 0;

            if (Data?.InteractionType != null)
            {
                switch (Data.InteractionType)
                {
                    case InteractionType.club_1_month:
                        months = 1;
                        break;
                    case InteractionType.club_3_month:
                        months = 3;
                        break;
                    case InteractionType.club_6_month:
                        months = 6;
                        break;
                }

                days = 31 * months;
            }

            DateTime future = DateTime.Now;
            if (Session?.GetHabbo()?.GetClubManager()?.HasSubscription("habbo_vip") == true)
            {
                double expire = Session.GetHabbo().GetClubManager().GetSubscription("habbo_vip").ExpireTime;
                double timeLeft = expire - PolarEnvironment.GetUnixTimestamp();
                int totalDaysLeft = (int)Math.Ceiling(timeLeft / 86400);
                future = DateTime.Now.AddDays(totalDaysLeft);
            }

            Session?.GetHabbo()?.GetClubManager()?.ReloadSubscription(Session);
            future = future.AddDays(days);

            Message.WriteInteger(months); // months
            Message.WriteInteger(days); // days
            Message.WriteBoolean(true);
            Message.WriteInteger(days); // wtf
            Message.WriteInteger(future.Year); // year
            Message.WriteInteger(future.Month); // month
            Message.WriteInteger(future.Day); // day
        }

        public int ExtradataInt
        {
            get
            {
                if (int.TryParse(this.ExtraData, out int result))
                    return result;
                return 0;
            }
        }

        // Método para validar si el item está disponible
        public bool IsAvailable()
        {
            if (!OfferActive) return false;
            if (IsLimited && LimitedEditionSells >= LimitedEditionStack) return false;
            if (Amount <= 0) return false;
            return true;
        }

        // Método para calcular el costo total para una cantidad específica
        public (int credits, int pixels, int diamonds) CalculateTotalCost(int quantity)
        {
            quantity = Math.Max(1, Math.Min(quantity, 100)); // Limitar entre 1 y 100
            return (CostCredits * quantity, CostPixels * quantity, CostDiamonds * quantity);
        }

        public override string ToString()
        {
            return $"CatalogItem [Id: {Id}, Name: {Name}, ItemId: {ItemId}, PageId: {PageId}, OfferId: {OfferId}, Active: {OfferActive}]";
        }
    }
}