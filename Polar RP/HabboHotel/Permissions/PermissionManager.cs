using System;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using log4net;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Users;

namespace Polar.HabboHotel.Permissions
{
    public sealed class PermissionManager
    {
        private static readonly ILog log = LogManager.GetLogger("Polar.HabboHotel.Permissions.PermissionManager");

        private readonly Dictionary<int, Permission> Permissions = new Dictionary<int, Permission>();
        private readonly Dictionary<string, PermissionCommand> _commands = new Dictionary<string, PermissionCommand>();
        private readonly Dictionary<int, PermissionGroup> PermissionGroups = new Dictionary<int, PermissionGroup>();
        private readonly Dictionary<int, List<string>> PermissionGroupRights = new Dictionary<int, List<string>>();
        private readonly Dictionary<int, List<string>> PermissionSubscriptionRights = new Dictionary<int, List<string>>();

        public void Init()
        {
            Permissions.Clear();
            _commands.Clear();
            PermissionGroups.Clear();
            PermissionGroupRights.Clear();
            PermissionSubscriptionRights.Clear();

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT * FROM `permissions`");
                DataTable table = dbClient.getTable();
                if (table != null)
                {
                    foreach (DataRow row in table.Rows)
                    {
                        int id = Convert.ToInt32(row["id"]);
                        Permissions.Add(id, new Permission(
                            id,
                            Convert.ToString(row["permission"]),
                            Convert.ToString(row["description"])));
                    }
                }
            }

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT * FROM `permissions_commands`");
                DataTable table = dbClient.getTable();
                if (table != null)
                {
                    foreach (DataRow row in table.Rows)
                    {
                        string command = Convert.ToString(row["command"]);
                        _commands.Add(command, new PermissionCommand(
                            command,
                            Convert.ToInt32(row["group_id"]),
                            Convert.ToInt32(row["subscription_id"])));
                    }
                }
            }

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                // ── BUG CORREGIDO: antes leía strings literales "name","description" etc.
                //    en lugar de los valores reales de la fila
                dbClient.SetQuery("SELECT * FROM `permissions_groups`");
                DataTable table = dbClient.getTable();
                if (table != null)
                {
                    foreach (DataRow row in table.Rows)
                    {
                        int id = Convert.ToInt32(row["id"]);
                        PermissionGroups.Add(id, new PermissionGroup(
                            Convert.ToString(row["name"]),
                            Convert.ToString(row["description"]),
                            Convert.ToString(row["badge_code"]),
                            Convert.ToString(row["prefix"]),
                            Convert.ToString(row["prefix_color"])));
                    }
                }
            }

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT * FROM `permissions_rights`");
                DataTable table = dbClient.getTable();
                if (table != null)
                {
                    foreach (DataRow row in table.Rows)
                    {
                        int groupId = Convert.ToInt32(row["group_id"]);
                        int permissionId = Convert.ToInt32(row["permission_id"]);

                        if (!PermissionGroups.ContainsKey(groupId)) continue;
                        if (!Permissions.TryGetValue(permissionId, out Permission perm)) continue;

                        if (!PermissionGroupRights.ContainsKey(groupId))
                            PermissionGroupRights[groupId] = new List<string>();

                        PermissionGroupRights[groupId].Add(perm.PermissionName);
                    }
                }
            }

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT * FROM `permissions_subscriptions`");
                DataTable table = dbClient.getTable();
                if (table != null)
                {
                    foreach (DataRow row in table.Rows)
                    {
                        int permissionId = Convert.ToInt32(row["permission_id"]);
                        int subscriptionId = Convert.ToInt32(row["subscription_id"]);

                        if (!Permissions.TryGetValue(permissionId, out Permission perm)) continue;

                        if (!PermissionSubscriptionRights.ContainsKey(subscriptionId))
                            PermissionSubscriptionRights[subscriptionId] = new List<string>();

                        PermissionSubscriptionRights[subscriptionId].Add(perm.PermissionName);
                    }
                }
            }

            log.Info($"Permissions: {Permissions.Count} perms | {PermissionGroups.Count} groups | {PermissionGroupRights.Count} group rights | {PermissionSubscriptionRights.Count} subscription rights");
        }

        // ── Grupos ───────────────────────────────────────────────────────────────

        public bool TryGetGroup(int id, out PermissionGroup group)
            => PermissionGroups.TryGetValue(id, out group);

        /// <summary>
        /// Obtiene el prefijo del grupo de rango del jugador.
        /// Devuelve string.Empty si el grupo no existe o no tiene prefijo.
        /// </summary>
        public string GetPrefixForPlayer(Habbo player)
        {
            if (TryGetGroup(player.Rank, out PermissionGroup group) && group.hasPrefix)
                return group.prefix;
            return string.Empty;
        }

        /// <summary>
        /// Obtiene el color del prefijo del grupo de rango del jugador.
        /// </summary>
        public string GetPrefixColorForPlayer(Habbo player)
        {
            if (TryGetGroup(player.Rank, out PermissionGroup group))
                return group.prefixColor;
            return string.Empty;
        }

        // ── Permisos ─────────────────────────────────────────────────────────────

        public List<string> GetPermissionsForPlayer(Habbo player)
        {
            var permissionSet = new List<string>();

            if (PermissionGroupRights.TryGetValue(player.Rank, out List<string> groupRights))
                permissionSet.AddRange(groupRights);

            if (PermissionSubscriptionRights.TryGetValue(player.VIPRank, out List<string> subRights))
                permissionSet.AddRange(subRights);

            return permissionSet;
        }

        public List<string> GetCommandsForPlayer(Habbo player)
        {
            return _commands
                .Where(x => player.Rank >= x.Value.GroupId && player.VIPRank >= x.Value.SubscriptionId)
                .Select(x => x.Key)
                .ToList();
        }
    }
}