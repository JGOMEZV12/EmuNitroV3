using System.Collections.Generic;
using System.Data;
using log4net;
using Polar.Database.Interfaces;

namespace Polar.HabboHotel.Global
{
    public class LanguageLocale
    {
        private static readonly ILog log = LogManager.GetLogger("Polar.HabboHotel.Global.LanguageLocale");

        private readonly Dictionary<string, string> _values = new Dictionary<string, string>();

        public LanguageLocale() => Init();

        public void Init()
        {
            _values.Clear();

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT `key`, `value` FROM `server_locale`");
                DataTable table = dbClient.getTable();
                if (table == null) return;

                foreach (DataRow row in table.Rows)
                    _values[row["key"].ToString()] = row["value"].ToString();
            }
        }

        public string TryGetValue(string key)
            => _values.TryGetValue(key, out string val) ? val : $"[Missing locale: {key}]";
    }
}
