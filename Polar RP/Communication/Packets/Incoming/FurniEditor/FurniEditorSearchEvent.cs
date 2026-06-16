using System;
using System.Collections.Generic;
using System.Data;
using Polar.Core;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.FurniEditor;
using Polar.Database.Interfaces;

namespace Polar.Communication.Packets.Incoming.FurniEditor
{
    public class FurniEditorSearchEvent : IPacketEvent
    {
        private const int PageSize = 20;

        public void Parse(GameClient session, ClientPacket packet)
        {
            if (!session.GetHabbo().GetPermissions().HasRight("acc_catalogfurni"))
            {
                session.SendMessage(new FurniEditorResultComposer(false, "No permission"));
                return;
            }

            string query = packet.PopString();
            string type = packet.PopString(); // Nitro: "all", "s", "i", "floor", "wall"
            int page = packet.PopInt();

            if (query != null && query.Length > 100)
                query = query.Substring(0, 100);

            if (page < 1) page = 1;

            int offset = (page - 1) * PageSize;

            var whereParts = new List<string>();
            var paramValues = new List<(string name, object value)>();

            if (!string.IsNullOrEmpty(query))
            {
                if (int.TryParse(query, out int numericQuery))
                {
                    whereParts.Add("(id = @nq1 OR sprite_id = @nq2 OR item_name LIKE @sq1 OR public_name LIKE @sq2)");
                    paramValues.Add(("@nq1", numericQuery));
                    paramValues.Add(("@nq2", numericQuery));
                    paramValues.Add(("@sq1", $"%{query}%"));
                    paramValues.Add(("@sq2", $"%{query}%"));
                }
                else
                {
                    whereParts.Add("(item_name LIKE @sq1 OR public_name LIKE @sq2)");
                    paramValues.Add(("@sq1", $"%{query}%"));
                    paramValues.Add(("@sq2", $"%{query}%"));
                }
            }

            if (!string.IsNullOrEmpty(type) && type.ToLower() != "all")
            {
                string filterType = "s";
                if (type.ToLower().StartsWith("i") || type.ToLower() == "wall")
                    filterType = "i";

                whereParts.Add("type = @type");
                paramValues.Add(("@type", filterType));
            }

            string whereClause = whereParts.Count > 0
                ? "WHERE " + string.Join(" AND ", whereParts)
                : "";

            // Use string formatting for LIMIT/OFFSET to avoid parameter driver issues in some MySQL clients
            string countSql = $"SELECT COUNT(*) FROM `{DatabaseCompatibility.FurnitureTable}` {whereClause}";
            string dataSql = $"SELECT * FROM `{DatabaseCompatibility.FurnitureTable}` {whereClause} ORDER BY id ASC LIMIT {PageSize} OFFSET {offset}";

            int total = 0;
            var items = new List<Dictionary<string, object>>();

            try
            {
                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery(countSql);
                    foreach (var (name, value) in paramValues)
                        dbClient.AddParameter(name, value);

                    total = dbClient.getInteger();
                }

                if (total > 0)
                {
                    using (IQueryAdapter dbData = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                    {
                        dbData.SetQuery(dataSql);
                        foreach (var (name, value) in paramValues)
                            dbData.AddParameter(name, value);

                        DataTable dt = dbData.getTable();
                        if (dt != null)
                        {
                            foreach (DataRow row in dt.Rows)
                                items.Add(FurniEditorHelper.ReadFullItem(row));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.LogException($"[FurniEditorSearchEvent] {ex}");
                session.SendMessage(new FurniEditorResultComposer(false, "Internal error"));
                return;
            }

            session.SendMessage(new FurniEditorSearchComposer(items, total, page));
        }
    }
}