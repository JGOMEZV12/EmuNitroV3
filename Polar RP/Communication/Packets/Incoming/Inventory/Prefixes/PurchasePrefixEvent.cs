using Polar.Communication.Packets.Outgoing.Inventory.Prefixes;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using Polar.Communication.Packets.Outgoing.Users;
using Polar.Core;
using Polar.Database.Interfaces;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Users;
using System;
using System.Data;

namespace Polar.Communication.Packets.Incoming.Inventory.Prefixes
{
    public class PurchasePrefixEvent : IPacketEvent
    {
        public void Parse(GameClient session, ClientPacket packet)
        {
            string text = packet.PopString();
            string color = packet.PopString();
            string icon = packet.PopString();
            string effect = packet.PopString();

            var habbo = session.GetHabbo();
            if (habbo == null) return;

            // Load settings
            int maxLength = GetSettingInt("max_length", 15);
            int minRank = GetSettingInt("min_rank_to_buy", 1);
            int priceCredits = GetSettingInt("price_credits", 5);
            int pricePoints = GetSettingInt("price_points", 0);
            int pointsType = GetSettingInt("points_type", 0);

            // Validate text
            text = text.Trim();
            if (string.IsNullOrEmpty(text) || text.Length > maxLength)
            {
                session.SendMessage(new RoomBubbleNotificationComposer("", $"Prefix text is invalid or too long (max {maxLength} characters)."));
                return;
            }

            // Validate color (hex or comma-separated hex per letter)
            foreach (string part in color.Split(','))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(part.Trim(), @"^#[0-9A-Fa-f]{6}$"))
                {
                    session.SendMessage(new RoomBubbleNotificationComposer("", "Invalid color format."));
                    return;
                }
            }

            // Check rank
            if (habbo.Rank < minRank)
            {
                session.SendMessage(new RoomBubbleNotificationComposer("", "Your rank is too low to purchase prefixes."));
                return;
            }

            // Check blacklist
            if (IsBlacklisted(text))
            {
                session.SendMessage(new RoomBubbleNotificationComposer("", "This prefix contains a blocked word."));
                return;
            }

            // Check credits
            if (priceCredits > 0 && habbo.Credits < priceCredits)
            {
                session.SendMessage(new RoomBubbleNotificationComposer("", "Not enough credits."));
                return;
            }

            // Check points
            if (pricePoints > 0 && habbo.Diamonds < pricePoints)
            {
                session.SendMessage(new RoomBubbleNotificationComposer("", "Not enough points."));
                return;
            }

            // Deduct currency
            if (priceCredits > 0)
            {
                habbo.Credits -= -priceCredits;
                habbo.UpdateCreditsBalance();
            }

            if (pricePoints > 0)
            {
                habbo.Diamonds -= -pricePoints;
                habbo.UpdateDiamondsBalance();
            }

            // Sanitize icon and effect
            icon = (icon ?? "").Trim();
            effect = (effect ?? "").Trim();

            // Create and persist prefix synchronously to get the ID
            var prefix = new UserPrefix(habbo.Id, text, color, icon, effect);
            prefix.Save();

            habbo.GetInventoryComponent().GetPrefixesComponent().AddPrefix(prefix);

            session.SendMessage(new PrefixReceivedComposer(prefix));
        }

        private static int GetSettingInt(string key, int defaultValue)
        {
            try
            {
                using IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
                dbClient.SetQuery("SELECT `value` FROM custom_prefix_settings WHERE key_name = @key LIMIT 1");
                dbClient.AddParameter("@key", key);

                DataRow row = dbClient.getRow();
                if (row != null)
                    return Convert.ToInt32(row["value"]);
            }
            catch (Exception ex)
            {
                Logging.LogException($"[PurchasePrefixEvent] GetSettingInt({key}): {ex}");
            }
            return defaultValue;
        }

        private static bool IsBlacklisted(string text)
        {
            string lower = text.ToLowerInvariant();
            try
            {
                using IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
                dbClient.SetQuery("SELECT word FROM custom_prefix_blacklist");

                DataTable dt = dbClient.getTable();
                if (dt == null) return false;

                foreach (DataRow row in dt.Rows)
                {
                    string word = row["word"]?.ToString().ToLowerInvariant() ?? "";
                    if (!string.IsNullOrEmpty(word) && lower.Contains(word))
                        return true;
                }
            }
            catch (Exception ex)
            {
                Logging.LogException($"[PurchasePrefixEvent] IsBlacklisted: {ex}");
            }
            return false;
        }
    }
}