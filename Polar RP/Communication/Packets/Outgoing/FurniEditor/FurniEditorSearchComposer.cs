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
            WriteInteger(total);
            WriteInteger(page);
            WriteInteger(items?.Count ?? 0);

            if (items != null)
            {
                foreach (var item in items)
                {
                    // 14 campos base (igual que en detail)
                    WriteInteger(GetInt(item, "id"));
                    WriteInteger(GetInt(item, "sprite_id"));
                    WriteString(GetStr(item, "item_name"));
                    WriteString(GetStr(item, "public_name"));
                    WriteString(GetStr(item, "type", "s"));
                    WriteInteger(GetInt(item, "width", 1));
                    WriteInteger(GetInt(item, "length", 1));
                    WriteFloat64(GetDbl(item, "stack_height", 0.0));      // double binario
                    WriteBoolean(GetBool(item, "allow_stack", true));      // mismo WriteBoolean que detail
                    WriteBoolean(GetBool(item, "allow_walk", false));
                    WriteBoolean(GetBool(item, "allow_sit", false));
                    WriteBoolean(GetBool(item, "allow_lay", false));
                    WriteString(GetStr(item, "interaction_type"));
                    WriteInteger(GetInt(item, "interaction_modes_count"));

                    // 13 campos extendidos
                    WriteBoolean(GetBool(item, "allow_gift", true));
                    WriteBoolean(GetBool(item, "allow_trade", true));
                    WriteBoolean(GetBool(item, "allow_recycle", true));
                    WriteBoolean(GetBool(item, "allow_marketplace_sell", true));
                    WriteBoolean(GetBool(item, "allow_inventory_stack", true));
                    WriteString(GetStr(item, "vending_ids"));
                    WriteString(GetStr(item, "customparams"));
                    WriteInteger(GetInt(item, "effect_id_male"));
                    WriteInteger(GetInt(item, "effect_id_female"));
                    WriteString(GetStr(item, "clothing_on_walk"));
                    WriteString(GetStr(item, "multiheight"));
                    WriteString(GetStr(item, "description"));
                }
            }
        }

        // Copia exacta de WriteFloat64 del detail
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

        // Helpers (iguales a los del detail)
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