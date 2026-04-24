using Polar.Communication.Packets.Outgoing;
using Polar.Database.Interfaces;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users.Messenger;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Polar.HabboHotel.Navigator
{
    internal static class NavigatorHandler
    {
        public static void Search(ServerPacket Message, SearchResultList SearchResult, string SearchData, GameClient Session, int FetchLimit)
        {
            switch (SearchResult.CategoryType)
            {
                default:
                    Message.WriteInteger(0);
                    break;

                // ── Búsqueda por texto / filtros ──────────────────────────────
                case NavigatorCategoryType.QUERY:
                    HandleQuery(Message, SearchData);
                    break;

                // ── Salas destacadas ──────────────────────────────────────────
                case NavigatorCategoryType.FEATURED:
                {
                    var rooms = PolarEnvironment.GetGame().GetNavigator()
                        .GetFeaturedRooms()
                        .Select(f => PolarEnvironment.GetGame().GetRoomManager().GenerateRoomData(f.RoomId))
                        .Where(d => d != null)
                        .Distinct()
                        .ToList();
                    WriteRooms(Message, rooms);
                    break;
                }

                case NavigatorCategoryType.POPULAR:
                {
                    var rooms = PolarEnvironment.GetGame().GetRoomManager().GetPopularRooms(-1, FetchLimit);
                    WriteRooms(Message, rooms);
                    break;
                }

                case NavigatorCategoryType.RECOMMENDED:
                {
                    var rooms = PolarEnvironment.GetGame().GetRoomManager().GetRecommendedRooms(FetchLimit);
                    WriteRooms(Message, rooms);
                    break;
                }

                case NavigatorCategoryType.CATEGORY:
                {
                    var rooms = PolarEnvironment.GetGame().GetRoomManager().GetRoomsByCategory(SearchResult.Id, FetchLimit);
                    WriteRooms(Message, rooms);
                    break;
                }

                // ── Salas del usuario ─────────────────────────────────────────
                case NavigatorCategoryType.MY_ROOMS:
                    WriteRooms(Message, Session.GetHabbo().UsersRooms);
                    break;

                case NavigatorCategoryType.MY_FAVORITES:
                {
                    var rooms = Session.GetHabbo().FavoriteRooms
                        .Cast<int>()
                        .Select(id => PolarEnvironment.GetGame().GetRoomManager().GenerateRoomData(id))
                        .Where(d => d != null)
                        .Distinct()
                        .Take(FetchLimit)
                        .ToList();
                    WriteRooms(Message, rooms);
                    break;
                }

                case NavigatorCategoryType.MY_GROUPS:
                {
                    var rooms = PolarEnvironment.GetGame().GetGroupManager()
                        .GetGroupsForUser(Session.GetHabbo().Id)
                        .Where(g => g != null)
                        .Select(g => PolarEnvironment.GetGame().GetRoomManager().GenerateRoomData(g.RoomId))
                        .Where(d => d != null)
                        .Distinct()
                        .Take(FetchLimit)
                        .ToList();
                    WriteRooms(Message, rooms);
                    break;
                }

                case NavigatorCategoryType.MY_FRIENDS_ROOMS:
                {
                    var rooms = Session.GetHabbo().GetMessenger()
                        .GetFriends()
                        .Where(b => b != null && b.InRoom && b.UserId != Session.GetHabbo().Id)
                        .Select(b => b.CurrentRoom?.RoomData)
                        .Where(d => d != null)
                        .Distinct()
                        .ToList();
                    WriteRooms(Message, rooms);
                    break;
                }

                case NavigatorCategoryType.MY_RIGHTS:
                {
                    var rooms = new List<RoomData>();
                    using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                    {
                        dbClient.SetQuery("SELECT `room_id` FROM `room_rights` WHERE `user_id` = @uid LIMIT @limit");
                        dbClient.AddParameter("uid", Session.GetHabbo().Id);
                        dbClient.AddParameter("limit", FetchLimit);
                        DataTable table = dbClient.getTable();
                        if (table != null)
                        {
                            foreach (DataRow row in table.Rows)
                            {
                                RoomData data = PolarEnvironment.GetGame().GetRoomManager()
                                    .GenerateRoomData(Convert.ToInt32(row["room_id"]));
                                if (data != null && !rooms.Contains(data))
                                    rooms.Add(data);
                            }
                        }
                    }
                    WriteRooms(Message, rooms);
                    break;
                }

                case NavigatorCategoryType.TOP_PROMOTIONS:
                {
                    var rooms = PolarEnvironment.GetGame().GetRoomManager().GetOnGoingRoomPromotions(16, FetchLimit);
                    WriteRooms(Message, rooms);
                    break;
                }

                case NavigatorCategoryType.PROMOTION_CATEGORY:
                {
                    var rooms = PolarEnvironment.GetGame().GetRoomManager().GetPromotedRooms(SearchResult.Id, FetchLimit);
                    WriteRooms(Message, rooms);
                    break;
                }
            }
        }

        // ─── Helpers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Escribe la lista de salas en el paquete.
        /// </summary>
        private static void WriteRooms(ServerPacket msg, ICollection<RoomData> rooms)
        {
            msg.WriteInteger(rooms.Count);
            foreach (RoomData data in rooms)
                RoomAppender.WriteRoom(msg, data, data.Promotion);
        }

        /// <summary>
        /// Maneja los distintos tipos de búsqueda por texto (owner:, tag:, group:, roomname:, texto libre).
        /// Una sola conexión a DB por búsqueda usando JOIN con rp_rooms.
        /// </summary>
        private static void HandleQuery(ServerPacket msg, string searchData)
        {
            if (string.IsNullOrWhiteSpace(searchData))
            {
                msg.WriteInteger(0);
                return;
            }

            string lower = searchData.ToLower();

            if (lower.StartsWith("owner:"))
            {
                string owner = searchData.Substring(6);
                var results = FetchRoomsWithRP(
                    "SELECT r.* FROM rooms r " +
                    "JOIN users u ON u.id = r.owner " +
                    "WHERE u.username = @p AND r.state != 'invisible' " +
                    "ORDER BY r.users_now DESC LIMIT 50",
                    ("p", owner));
                WriteRooms(msg, results);
            }
            else if (lower.StartsWith("tag:"))
            {
                string tag = searchData.Substring(4);
                var matches = PolarEnvironment.GetGame().GetRoomManager().SearchTaggedRooms(tag);
                WriteRooms(msg, matches.ToList());
            }
            else if (lower.StartsWith("group:"))
            {
                string group = searchData.Substring(6);
                var matches = PolarEnvironment.GetGame().GetRoomManager().SearchGroupRooms(group);
                WriteRooms(msg, matches.ToList());
            }
            else
            {
                // roomname: o texto libre
                string query = lower.StartsWith("roomname:")
                    ? "%" + searchData.Split(new[] { ':' }, 2)[1] + "%"
                    : "%" + searchData + "%";

                var results = FetchRoomsWithRP(
                    "SELECT r.id,r.caption,r.description,r.roomtype,r.owner,r.state,r.category," +
                    "r.users_now,r.users_max,r.model_name,r.score,r.allow_pets,r.allow_pets_eat," +
                    "r.room_blocking_disabled,r.allow_hidewall,r.password,r.wallpaper,r.floor," +
                    "r.landscape,r.floorthick,r.wallthick,r.mute_settings,r.kick_settings," +
                    "r.ban_settings,r.chat_mode,r.chat_speed,r.chat_size,r.trade_settings," +
                    "r.group_id,r.tags,r.push_enabled,r.pull_enabled,r.enables_enabled," +
                    "r.respect_notifications_enabled,r.pet_morphs_allowed,r.spush_enabled,r.spull_enabled " +
                    "FROM rooms r " +
                    "WHERE r.caption LIKE @p AND r.state != 'invisible' " +
                    "ORDER BY r.users_now DESC LIMIT 50",
                    ("p", query));
                WriteRooms(msg, results);
            }
        }

        /// <summary>
        /// Ejecuta una query sobre rooms y carga los datos de rp_rooms en una sola conexión
        /// usando un LEFT JOIN, eliminando la query por-sala que antes abría N conexiones.
        /// </summary>
        private static List<RoomData> FetchRoomsWithRP(string sql, params (string name, object value)[] parameters)
        {
            var results = new List<RoomData>();
            DataTable roomTable = null;
            DataTable rpTable   = null;

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery(sql);
                foreach (var (name, value) in parameters)
                    dbClient.AddParameter(name, value);
                roomTable = dbClient.getTable();

                if (roomTable == null || roomTable.Rows.Count == 0)
                    return results;

                // Cargar todos los rp_rooms de una vez con IN (ids)
                var ids = string.Join(",",
                    roomTable.Rows.Cast<DataRow>().Select(r => Convert.ToInt32(r["id"])));

                dbClient.SetQuery($"SELECT * FROM `rp_rooms` WHERE `id` IN ({ids})");
                rpTable = dbClient.getTable();
            }

            // Indexar rp_rooms por id para O(1) lookup
            var rpIndex = new Dictionary<int, DataRow>();
            if (rpTable != null)
                foreach (DataRow row in rpTable.Rows)
                    rpIndex[Convert.ToInt32(row["id"])] = row;

            foreach (DataRow row in roomTable.Rows)
            {
                int id = Convert.ToInt32(row["id"]);
                rpIndex.TryGetValue(id, out DataRow rpRow);

                RoomData data = PolarEnvironment.GetGame().GetRoomManager().FetchRoomData(id, row, rpRow);
                if (data != null && !results.Contains(data))
                    results.Add(data);
            }

            return results;
        }
    }
}
