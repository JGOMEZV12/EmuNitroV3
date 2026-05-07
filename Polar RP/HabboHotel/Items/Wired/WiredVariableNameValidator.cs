using System;
using System.Text.RegularExpressions;

namespace Polar.HabboHotel.Items.Wired
{
    public static class WiredVariableNameValidator
    {
        private static readonly Regex ValidNameRegex = new Regex("^[A-Za-z0-9_]+$", RegexOptions.Compiled);

        public static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";

            return value.Replace("\t", "")
                        .Replace("\r", "")
                        .Replace("\n", "")
                        .Replace(" ", "_");
        }

        public static bool IsValid(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            if (name.Length < 1 || name.Length > 40) return false;
            return ValidNameRegex.IsMatch(name);
        }
    }
}
