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
            string type = packet.PopString();
            int page = packet.PopInt();

            if (query.Length > 100)
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

            if (!string.IsNullOrEmpty(type))
            {
                whereParts.Add("type = @type");
                paramValues.Add(("@type", type));
            }

            string whereClause = whereParts.Count > 0
                ? "WHERE " + string.Join(" AND ", whereParts)
                : "";

            string countSql = $"SELECT COUNT(*) FROM items_base {whereClause}";
            string dataSql = $"SELECT * FROM items_base {whereClause} ORDER BY id ASC LIMIT @limit OFFSET @offset";

            int total = 0;
            var items = new List<Dictionary<string, object>>();

            try
            {
                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery(countSql);
                    foreach (var (name, value) in paramValues)
                        dbClient.AddParameter(name, value);

                    DataTable countTable = dbClient.getTable();
                    if (countTable != null && countTable.Rows.Count > 0)
                        total = Convert.ToInt32(countTable.Rows[0][0]);
                }

                if (total > 0)
                {
                    using (IQueryAdapter dbData = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                    {
                        dbData.SetQuery(dataSql);
                        foreach (var (name, value) in paramValues)
                            dbData.AddParameter(name, value);
                        dbData.AddParameter("@limit", PageSize);
                        dbData.AddParameter("@offset", offset);

                        DataTable dt = dbData.getTable();
                        if (dt != null && dt.Rows.Count > 0)
                        {
                            foreach (DataRow row in dt.Rows)
                                items.Add(FurniEditorHelper.ReadFullItem(row)); // ← usa el helper
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