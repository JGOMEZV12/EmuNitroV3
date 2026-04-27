using Polar.Communication.Packets.Outgoing;
using System;
using System.Collections.Generic;

namespace Polar.Communication.Packets.Outgoing.FurniEditor
{
    public class FurniEditorSearchComposer : ServerPacket
    {
        public FurniEditorSearchComposer(List<Dictionary<string, object>> items, int total, int page)
            : base(ServerPacketHeader.FurniEditorSearchComposer)
        {
            // Nitro expects [Int: total] [Int: count] -> then items
            WriteInteger(total);
            WriteInteger(items?.Count ?? 0);

            if (items != null)
            {
                foreach (var item in items)
                {
                    // 14 Base Fields (FurniItemData)
                    WriteInteger(GetInt(item, "id"));
                    int offId = GetInt(item, "offer_id", -1);
                    WriteInteger(offId <= 0 ? GetInt(item, "sprite_id") : offId);
                    WriteString(GetStr(item, "item_name"));
                    WriteString(GetStr(item, "public_name"));
                    WriteString(GetStr(item, "type", "s"));
                    WriteInteger(GetInt(item, "width", 1));
                    WriteInteger(GetInt(item, "length", 1));
                    WriteFloat64(GetDbl(item, "stack_height", 0.0));
                    WriteBoolean(GetBool(item, "allow_stack", true));
                    WriteBoolean(GetBool(item, "allow_walk", false));
                    WriteBoolean(GetBool(item, "allow_sit", false));
                    WriteBoolean(GetBool(item, "allow_lay", false));
                    WriteString(GetStr(item, "interaction_type"));
                    WriteInteger(GetInt(item, "interaction_modes_count"));

                    // NOTE: Search results in Nitro V3 ONLY include the 14 base fields.
                    // Extended fields like allow_gift, vending_ids, etc. are part of FurniDetailData
                }
            }
        }

        private void WriteFloat64(double value)
        {
            long bits = BitConverter.DoubleToInt64Bits(value);
            WriteByte((byte)(bits >> 56));
            WriteByte((byte)(bits >> 48));
            WriteByte((byte)(bits >> 40));
            WriteByte((byte)(bits >> 32));
            WriteByte((byte)(bits >> 24));
            WriteByte((byte)(bits >> 16));
            WriteByte((byte)(bits >> 8));
            WriteByte((byte)bits);
        }

        private static int GetInt(Dictionary<string, object> d, string key, int def = 0)
        {
            if (d == null || !d.TryGetValue(key, out object val) || val == null) return def;
            try { return Convert.ToInt32(val); } catch { return def; }
        }
        private static string GetStr(Dictionary<string, object> d, string key, string def = "")
        {
            if (d == null || !d.TryGetValue(key, out object val) || val == null) return def;
            return val.ToString();
        }
        private static double GetDbl(Dictionary<string, object> d, string key, double def = 0.0)
        {
            if (d == null || !d.TryGetValue(key, out object val) || val == null) return def;
            try { return Convert.ToDouble(val, System.Globalization.CultureInfo.InvariantCulture); } catch { return def; }
        }
        private static bool GetBool(Dictionary<string, object> d, string key, bool def = false)
        {
            if (d == null || !d.TryGetValue(key, out object val) || val == null) return def;
            if (val is bool b) return b;
            if (val is int i) return i != 0;
            if (val is long l) return l != 0;
            string s = val.ToString().Trim().ToLowerInvariant();
            return s == "1" || s == "true";
        }
    }
}