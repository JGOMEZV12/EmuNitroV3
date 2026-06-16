using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using Polar.Core;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.FurniEditor;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Items;

namespace Polar.Communication.Packets.Incoming.FurniEditor
{
    public class FurniEditorUpdateEvent : IPacketEvent
    {
        public void Parse(GameClient session, ClientPacket packet)
        {
            if (!session.GetHabbo().GetPermissions().HasRight("acc_catalogfurni"))
            {
                session.SendMessage(new FurniEditorResultComposer(false, "No permission"));
                return;
            }

            int id = packet.PopInt();
            string jsonFieldsStr = packet.PopString();

            if (id <= 0)
            {
                session.SendMessage(new FurniEditorResultComposer(false, "Invalid item ID"));
                return;
            }

            JsonDocument json;
            try
            {
                json = JsonDocument.Parse(jsonFieldsStr);
            }
            catch
            {
                session.SendMessage(new FurniEditorResultComposer(false, "Invalid JSON data"));
                return;
            }

            using (json)
            {
                var root = json.RootElement;

                if (root.ValueKind != JsonValueKind.Object)
                {
                    session.SendMessage(new FurniEditorResultComposer(false, "Invalid JSON data"));
                    return;
                }

                var setClauses = new StringBuilder();
                var paramNames = new List<string>();
                var paramValues = new List<object>();
                int paramIndex = 0;

                foreach (JsonProperty prop in root.EnumerateObject())
                {
                    string jsKey = prop.Name;

                    if (!FurniEditorHelper.FieldMap.TryGetValue(jsKey, out string dbColumn))
                        continue;
                    if (!FurniEditorHelper.AllowedUpdateFields.Contains(dbColumn))
                        continue;

                    if (setClauses.Length > 0) setClauses.Append(", ");
                    string paramName = $"@p{paramIndex++}";
                    setClauses.Append($"`{dbColumn}` = {paramName}");
                    paramNames.Add(paramName);

                    object val;
                    switch (prop.Value.ValueKind)
                    {
                        case JsonValueKind.True:
                            val = "1";
                            break;
                        case JsonValueKind.False:
                            val = "0";
                            break;
                        case JsonValueKind.Number:
                            string numStr = prop.Value.GetRawText();
                            if (numStr.Contains('.'))
                                val = prop.Value.GetDouble();
                            else
                                val = prop.Value.GetInt32();
                            break;
                        default:
                            val = prop.Value.GetString() ?? "";
                            break;
                    }
                    paramValues.Add(val);
                }

                if (setClauses.Length == 0)
                {
                    session.SendMessage(new FurniEditorResultComposer(false, "No valid fields to update"));
                    return;
                }

                string sql = $"UPDATE `{DatabaseCompatibility.FurnitureTable}` SET {setClauses} WHERE id = @id";

                try
                {
                    using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                    {
                        dbClient.SetQuery(sql);
                        for (int i = 0; i < paramNames.Count; i++)
                            dbClient.AddParameter(paramNames[i], paramValues[i]);
                        dbClient.AddParameter("@id", id);
                        dbClient.RunQuery();
                    }
                }
                catch (Exception ex)
                {
                    Logging.LogException($"[FurniEditorUpdateEvent] {ex}");
                    session.SendMessage(new FurniEditorResultComposer(false, "Internal error"));
                    return;
                }
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await PolarEnvironment.GetGame().GetItemManager().InitAsync();

                    if (PolarEnvironment.GetGame().GetItemManager().GetItem(id, out ItemData newData))
                    {
                        var activeRooms = PolarEnvironment.GetGame().GetRoomManager().GetRooms();
                        foreach (var room in activeRooms)
                        {
                            var itemsToUpdate = room.GetRoomItemHandler().GetWallAndFloor
                                .Where(i => i.BaseItem == id).ToList();

                            if (itemsToUpdate.Count == 0) continue;

                            foreach (var item in itemsToUpdate)
                            {
                                room.GetGameMap().RemoveFromMap(item, false);
                                item.Data = newData;
                                room.GetGameMap().AddItemToMap(item, false, false);

                                if (item.IsFloorItem)
                                    room.SendMessage(new ObjectUpdateComposer(item, item.UserID));
                                else
                                    room.SendMessage(new ItemUpdateComposer(item, item.UserID));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logging.LogException($"[FurniEditorUpdateEvent] Update live instances failed: {ex}");
                }
            });

            session.SendMessage(new FurniEditorResultComposer(true, "Item updated", id));
        }
    }
}