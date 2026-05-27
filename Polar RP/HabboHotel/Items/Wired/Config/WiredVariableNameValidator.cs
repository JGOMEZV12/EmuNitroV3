using Polar.HabboHotel.Items.Wired.Boxes.Add_ons;
using Polar.HabboHotel.Rooms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Polar.HabboHotel.Items.Wired
{
    static class WiredVariableNameValidator
    {
        public const int MIN_NAME_LENGTH = 1;
        public const int MAX_NAME_LENGTH = 40;
        private static readonly Regex ValidNamePattern = new Regex("^[A-Za-z0-9_]+$", RegexOptions.Compiled);

        public static string NormalizeForSave(string value)
        {
            if (value == null) return "";
            return Regex.Replace(value
                .Replace("\t", "")
                .Replace("\r", "")
                .Replace("\n", ""), @"\s+", "_");
        }

        public static string NormalizeLegacy(string value)
        {
            string normalized = NormalizeForSave(value);

            int eqIdx = normalized.IndexOf('=');
            if (eqIdx >= 0)
                normalized = normalized.Substring(0, eqIdx);

            while (normalized.StartsWith("@") || normalized.StartsWith("~"))
                normalized = normalized.Substring(1);

            if (normalized.Length > MAX_NAME_LENGTH)
                normalized = normalized.Substring(0, MAX_NAME_LENGTH);

            return normalized;
        }

        public static void ValidateDefinitionName(Room room, int currentItemId, string variableName)
        {
            string normalized = NormalizeForSave(variableName);

            if (normalized.Length < MIN_NAME_LENGTH || normalized.Length > MAX_NAME_LENGTH)
                throw new Exception("wiredfurni.error.variables.name_length");

            if (!ValidNamePattern.IsMatch(normalized))
                throw new Exception("wiredfurni.error.variables.name_syntax");

            if (IsNameInUse(room, currentItemId, normalized))
                throw new Exception("wiredfurni.error.variables.name_uniq");
        }

        private static bool IsNameInUse(Room room, int currentItemId, string variableName)
        {
            if (room == null || string.IsNullOrEmpty(variableName)) return false;

            foreach (var item in room.GetRoomItemHandler().GetFloor)
            {
                if (item == null || item.Id == currentItemId) continue;

                // Buscar otros AddonSetVariableBox en la sala
                var wired = room.GetWired().TryGet(item.Id, out var box) ? box : null;
                if (wired is AddonSetVariableBox varBox &&
                    varBox.VariableName.Equals(variableName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
