using System.Collections.Generic;
using System.Data;
using Polar.Core;
using Polar.Database.Interfaces;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.FurniEditor;

namespace Polar.Communication.Packets.Incoming.FurniEditor
{
    public class FurniEditorInteractionsEvent : IPacketEvent
    {
        private static volatile List<string> _cachedInteractions = null;
        private static readonly object _lock = new object();

        public void Parse(GameClient session, ClientPacket packet)
        {
            if (!session.GetHabbo().GetPermissions().HasRight("acc_catalogfurni"))
            {
                session.SendMessage(new FurniEditorResultComposer(false, "No permission"));
                return;
            }

            if (_cachedInteractions == null)
            {
                lock (_lock)
                {
                    if (_cachedInteractions == null)
                    {
                        var list = new List<string>();

                        using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                        {
                            dbClient.SetQuery(
                                "SELECT DISTINCT `interaction_type` FROM `items_base` " +
                                "WHERE `interaction_type` != '' ORDER BY `interaction_type` ASC");

                            DataTable table = dbClient.getTable();

                            if (table != null)
                            {
                                foreach (DataRow row in table.Rows)
                                    list.Add(row["interaction_type"].ToString());
                            }
                        }

                        _cachedInteractions = list;
                    }
                }
            }

            session.SendMessage(new FurniEditorInteractionsComposer(_cachedInteractions));
        }
    }
}