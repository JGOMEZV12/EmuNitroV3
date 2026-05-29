using Polar.Communication.Packets.Outgoing;
using Polar.Core;
using Polar.HabboHotel.Catalog.Utilities;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Items.Utilities;
using System;
using System.Collections.Generic;

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
        public bool OfferActive { get; set; }
        public int OfferId { get; set; }
        public bool IsLimited { get; set; }
        public int LimitedEditionStack { get; set; }
        public int LimitedEditionSells { get; set; }

        public int PageID => PageId;
        public bool HaveOffer => OfferActive;

        public CatalogItem(int Id, int ItemId, ItemData Data, string CatalogName, int PageId,
            int CostCredits, int CostPixels, int CostDiamonds, int Amount,
            int LimitedEditionSells, int LimitedEditionStack,
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

        // ══════════════════════════════════════════════════════════════════════
        //  SERIALIZE
        //  Orden exacto que lee CatalogPageMessageOfferData en el cliente React:
        //
        //  readInt()     offerId
        //  readString()  localizationId
        //  readBoolean() rent
        //  readInt()     priceCredits
        //  readInt()     priceActivityPoints
        //  readInt()     priceActivityPointsType
        //  readBoolean() giftable
        //  readInt()     totalProducts
        //    → CatalogPageMessageProductData x N
        //  readInt()     clubLevel
        //  readBoolean() bundlePurchaseAllowed
        //  readBoolean() isPet
        //  readString()  previewImage
        // ══════════════════════════════════════════════════════════════════════
        public void Serialize(ServerPacket message)
        {
            // offerId
            message.WriteInteger(Id);

            // localizationId
            message.WriteString(Name ?? string.Empty);

            // rent
            message.WriteBoolean(false);

            // priceCredits
            message.WriteInteger(CostCredits);

            // priceActivityPoints + priceActivityPointsType
            if (CostDiamonds > 0)
            {
                message.WriteInteger(CostDiamonds);
                message.WriteInteger(5); // diamonds
            }
            else
            {
                message.WriteInteger(CostPixels);
                message.WriteInteger(0); // duckets
            }

            // giftable
            message.WriteBoolean(ItemUtility.CanGiftItem(this));

            // ── productos (CatalogPageMessageProductData) ─────────────────────
            // El cliente React lee por cada producto:
            //   readString() → productType  ("s", "e", "b", "r")
            //   if badge:
            //     readString() → productClassname
            //   else:
            //     readInt()    → furniSpriteId
            //     readString() → extraParam
            //     readInt()    → productCount
            //     readBoolean()→ isLimited
            //     if isLimited:
            //       readInt()  → limitedStack
            //       readInt()  → limitedSells remaining

            HashSet<ItemData> items = GetBaseItems();
            message.WriteInteger(items.Count);

            foreach (ItemData item in items)
            {
                string itemType = item.Type.ToString().ToLower();
                message.WriteString(itemType);

                if (itemType == "b") // badge
                {
                    message.WriteString(item.ItemName ?? string.Empty);
                }
                else
                {
                    // furniSpriteId
                    message.WriteInteger(item.SpriteId);

                    // extraParam — según tipo de item
                    message.WriteString(GetExtraParam(item));

                    // productCount
                    message.WriteInteger(GetItemAmount(item.Id));

                    // isLimited
                    message.WriteBoolean(IsLimited);
                    if (IsLimited)
                    {
                        message.WriteInteger(LimitedEditionStack);
                        message.WriteInteger(Math.Max(0, LimitedEditionStack - LimitedEditionSells));
                    }
                }
            }

            // clubLevel
            message.WriteInteger(0);

            // bundlePurchaseAllowed — true si tiene más de un producto (bundle)
            message.WriteBoolean(items.Count > 1);

            // isPet — true si el item es un bot/pet tipo "r"
            bool isPet = Data != null && Data.Type.ToString().ToLower() == "r";
            message.WriteBoolean(isPet);

            // previewImage — classname del furni, el cliente lo usa como "product"
            // para buscar en furnidata. Es el campo que causaba el crash "product undefined".
            message.WriteString(Data?.ItemName ?? string.Empty);
        }

        // ── ExtraParam por tipo de item ───────────────────────────────────────
        private string GetExtraParam(ItemData item)
        {
            string itemType = item.Type.ToString().ToLower();

            if (Name.Contains("wallpaper_single") ||
                Name.Contains("floor_single") ||
                Name.Contains("landscape_single"))
            {
                // Tercera parte del nombre: "wallpaper_single_101" → "101"
                var parts = Name.Split('_');
                return parts.Length >= 3 ? parts[2] : string.Empty;
            }

            if (itemType == "r" && item.ItemName.Contains("bot"))
            {
                // Bot: buscar "figure:" en extradata separado por ";"
                if (!string.IsNullOrEmpty(ExtraData))
                {
                    foreach (string s in ExtraData.Split(';'))
                    {
                        if (s.StartsWith("figure:", StringComparison.OrdinalIgnoreCase))
                            return s.Replace("figure:", string.Empty);
                    }
                }
                return ExtraData ?? string.Empty;
            }

            if (itemType == "r")
                return ExtraData ?? string.Empty;

            if (item.ItemName.Equals("poster", StringComparison.OrdinalIgnoreCase))
                return ExtraData ?? string.Empty;

            if (Name.StartsWith("SONG ", StringComparison.OrdinalIgnoreCase))
                return ExtraData ?? string.Empty;

            return string.Empty;
        }

        // ── SerializeClub ─────────────────────────────────────────────────────
        public void SerializeClub(ServerPacket Message, GameClient Session)
        {
            Message.WriteInteger(Id);
            Message.WriteString(Name);
            Message.WriteBoolean(false);
            Message.WriteInteger(CostCredits);

            if (CostDiamonds > 0)
            {
                Message.WriteInteger(CostDiamonds);
                Message.WriteInteger(5);
            }
            else
            {
                Message.WriteInteger(CostPixels);
                Message.WriteInteger(0);
            }

            Message.WriteBoolean(true);

            int days = 0, months = 0;
            if (Data?.InteractionType != null)
            {
                switch (Data.InteractionType)
                {
                    case InteractionType.club_1_month: months = 1; break;
                    case InteractionType.club_3_month: months = 3; break;
                    case InteractionType.club_6_month: months = 6; break;
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

            Message.WriteInteger(months);
            Message.WriteInteger(days);
            Message.WriteBoolean(true);
            Message.WriteInteger(days);
            Message.WriteInteger(future.Year);
            Message.WriteInteger(future.Month);
            Message.WriteInteger(future.Day);
        }

        // ── GetBaseItems ──────────────────────────────────────────────────────
        public ItemData GetBaseItem(int itemId)
        {
            if (!PolarEnvironment.GetGame().GetItemManager().GetItem(itemId, out ItemData itemData))
                return null;
            return itemData;
        }

        public ItemData GetBaseItem() => GetBaseItem(ItemId);

        public HashSet<ItemData> GetBaseItems()
        {
            var items = new HashSet<ItemData>();
            string[] itemIds = ItemId.ToString().Split(';');

            foreach (string rawId in itemIds)
            {
                if (string.IsNullOrEmpty(rawId)) continue;
                string cleanId = rawId.Contains(":") ? rawId.Split(':')[0] : rawId;
                if (!int.TryParse(cleanId, out int identifier) || identifier <= 0) continue;
                if (PolarEnvironment.GetGame().GetItemManager().GetItem(identifier, out ItemData data))
                    items.Add(data);
            }

            return items;
        }

        public int GetItemAmount(int itemId) =>
            itemId == ItemId ? Math.Max(1, Amount) : 1;

        // ── Helpers ───────────────────────────────────────────────────────────
        public int ExtradataInt
        {
            get { int.TryParse(ExtraData, out int r); return r; }
        }

        public bool IsAvailable()
        {
            if (!OfferActive) return false;
            if (IsLimited && LimitedEditionSells >= LimitedEditionStack) return false;
            if (Amount <= 0) return false;
            return true;
        }

        public (int credits, int pixels, int diamonds) CalculateTotalCost(int quantity)
        {
            quantity = Math.Max(1, Math.Min(quantity, 100));
            return (CostCredits * quantity, CostPixels * quantity, CostDiamonds * quantity);
        }

        public override string ToString() =>
            $"CatalogItem [Id:{Id}, Name:{Name}, ItemId:{ItemId}, PageId:{PageId}]";
    }
}