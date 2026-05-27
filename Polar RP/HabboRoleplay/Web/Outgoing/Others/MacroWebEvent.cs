using ConnectionManager;
using Nancy.Session;
using Newtonsoft.Json;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Roleplay.Web.Outgoing.Misc;
using Polar.HabboHotel.Rooms;
using Polar.HabboRoleplay.Misc;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Polar.HabboHotel.Roleplay.Web.Outgoing.Misc
{
    // ================================================================
    //  Modelo interno de macro
    // ================================================================
    internal sealed class UserMacro
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Keybind { get; set; }
        public List<string> Commands { get; set; } = new List<string>();
        public int DelayMs { get; set; } = 300;
        public int LoopCount { get; set; } = 1;
        public string Category { get; set; } = "General";
        public bool Active { get; set; } = true;
    }

    // ================================================================
    //  MacroWebEvent
    //
    //  Protocolo WebSocket (mismo patrón que FoodWebEvent):
    //
    //  Cliente → Servidor:
    //    "event_macros"  → se registra en WebEventManager y llama Execute()
    //    Data = "<accion>,<json_payload>"
    //
    //  Acciones entrantes:
    //    open               → devuelve lista de macros del usuario
    //    create,<MacroJson> → crea un macro nuevo
    //    update,<MacroJson> → edita un macro existente (requiere "id")
    //    delete,<id>        → elimina un macro por id
    //    execute,<id>       → ejecuta la secuencia de comandos en el chat
    //
    //  Servidor → Cliente  (Socket.Send):
    //    compose_macros|open|<MacrosJson>
    //    compose_macros|created|<MacroJson>
    //    compose_macros|updated|<MacroJson>
    //    compose_macros|deleted|<id>
    //    compose_macros|executed|<id>
    //    compose_macros|error|<mensaje>
    // ================================================================
    public class MacroWebEvent : IWebEvent
    {
        // ── Límites ──────────────────────────────────────────────────
        private const int MaxMacrosPerUser = 20;
        private const int MaxCommandsPerMacro = 15;
        private const int MaxNameLength = 64;
        private const int MaxDelayMs = 5000;
        private const int MinDelayMs = 0;
        private const int MaxLoops = 100;

        // ── Categorías permitidas ─────────────────────────────────────
        private static readonly HashSet<string> AllowedCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "General", "Chat", "Movimiento", "Emotes", "Combate", "Roles"
        };

        // ================================================================
        //  Execute — punto de entrada del WebSocket
        // ================================================================
        public void Execute(GameClient Client, string Data, ConnectionInformation Socket)
        {
             //PolarEnvironment.GetGame().GetWebEventManager().OnSocketError("MacroWebEvent Execute llamado: " + Data, "debug");

            if (Client == null || Socket == null) return;

            var wem = PolarEnvironment.GetGame().GetWebEventManager();
            if (!wem.SocketReady(Client, true) || !wem.SocketReady(Socket)) return;

            if (string.IsNullOrWhiteSpace(Data))
            {
                Send(Socket, "error", "Datos vacíos");
                return;
            }

            // Separar acción del payload (máximo 2 partes)
            string action;
            string payload;
            int comma = Data.IndexOf(',');
            if (comma < 0) { action = Data.Trim(); payload = string.Empty; }
            else { action = Data.Substring(0, comma).Trim(); payload = Data.Substring(comma + 1); }

            int userId = Client.GetHabbo().Id;

            try
            {
                switch (action.ToLowerInvariant())
                {
                    case "open": HandleOpen(Socket, userId); break;
                    case "create": HandleCreate(Socket, Client, payload); break;
                    case "update": HandleUpdate(Socket, Client, payload); break;
                    case "delete": HandleDelete(Socket, userId, payload); break;
                    case "execute": HandleExecute(Socket, Client, payload); break;
                    case "execute_cmd": HandleExecuteCmd(Socket, Client, payload); break;
                    default:
                        Send(Socket, "error", $"Acción desconocida: {action}");
                        break;
                }
            }
            catch (Exception ex)
            {
                PolarEnvironment.GetGame().GetWebEventManager()
                    .OnSocketError(ex.Message, ex.ToString());
                Send(Socket, "error", "Error interno del servidor");
            }
        }

        private void HandleExecuteCmd(ConnectionInformation socket, GameClient client, string payload)
        {
            int comma = payload.IndexOf(',');
            if (comma < 0) return;

            string cmd = payload.Substring(comma + 1).Trim();
            if (string.IsNullOrEmpty(cmd)) return;

            Task.Run(async () =>
            {
                try
                {
                    if (cmd.StartsWith(":", StringComparison.CurrentCulture))
                    {
                        // ✅ Es comando — parsear directamente
                        await PolarEnvironment.GetGame().GetChatManager().GetCommands().Parse(client, cmd);
                    }
                    else
                    {
                        // ✅ No es comando — simular escritura en chat pasando por el flujo completo
                        // Resolver "x" como LastCommand
                        string finalCmd = cmd;
                        if (cmd == "x" && client.GetRoleplay()?.LastCommand != "")
                            finalCmd = client.GetRoleplay().LastCommand;

                        // Si después de resolver sigue siendo comando, parsear
                        if (finalCmd.StartsWith(":", StringComparison.CurrentCulture))
                        {
                            await PolarEnvironment.GetGame().GetChatManager().GetCommands().Parse(client, finalCmd);
                            return;
                        }

                        // Si es texto normal, enviarlo como chat visible en la sala
                        if (!RoleplayManager.GenerateRoom(client.GetRoomUser()?.RoomId ?? 0, out Room room))
                            return;

                        var roomUser = room.GetRoomUserManager().GetRoomUserByHabbo(client.GetHabbo().Id);
                        if (roomUser == null) return;

                        // Pasar por el mismo flujo que ChatEvent usa
                        var roleplay = client.GetRoleplay();
                        if (roleplay != null)
                        {
                            if (roleplay.IsWorking && HabboHotel.Groups.GroupManager.HasJobCommand(client, "guide"))
                                roomUser.OnChat(37, finalCmd, false, string.Empty);
                            else if (roleplay.StaffOnDuty && client.GetHabbo().GetPermissions().HasRight("mod_tool"))
                                roomUser.OnChat(23, finalCmd, false, string.Empty);
                            else if (roleplay.AmbassadorOnDuty && client.GetHabbo().GetPermissions().HasRight("ambassador"))
                                roomUser.OnChat(37, finalCmd, false, string.Empty);
                            else if (roleplay.CurHealth > 25 && roleplay.CurHealth <= 40 && !roleplay.IsDead)
                                roomUser.OnChat(5, finalCmd, false, string.Empty);
                            else if (roleplay.CurHealth <= 25 && !roleplay.IsDead)
                                roomUser.OnChat(3, finalCmd, false, string.Empty);
                            else if (roleplay.IsDead)
                                roomUser.OnChat(3, "[ " + finalCmd + " ]", false, string.Empty);
                            else
                                roomUser.OnChat(roomUser.LastBubble, finalCmd, false, string.Empty);
                        }
                        else
                        {
                            roomUser.OnChat(roomUser.LastBubble, finalCmd, false, string.Empty);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"HandleExecuteCmd Error: {ex.Message}");
                }
            });
        }
        // ================================================================
        //  OPEN — devuelve todos los macros del usuario
        // ================================================================
        private void HandleOpen(ConnectionInformation socket, int userId)
        {
            var macros = GetUserMacros(userId);
            Send(socket, "open", JsonConvert.SerializeObject(macros));
        }

        // ================================================================
        //  CREATE — inserta un nuevo macro en MySQL
        // ================================================================
        private void HandleCreate(ConnectionInformation socket, GameClient client, string payload)
        {
            int userId = client.GetHabbo().Id;

            if (!TryParseMacro(payload, out var macro, out string err))
            { Send(socket, "error", err); return; }

            // Límite por usuario
            if (CountUserMacros(userId) >= MaxMacrosPerUser)
            { Send(socket, "error", $"Límite de {MaxMacrosPerUser} macros alcanzado"); return; }

            // Keybind único por usuario
            if (!string.IsNullOrEmpty(macro.Keybind) && KeybindExists(userId, macro.Keybind, 0))
            { Send(socket, "error", $"La tecla '{macro.Keybind}' ya está en uso"); return; }

            macro.UserId = userId;
            macro.Id = InsertMacro(macro);

            Send(socket, "created", JsonConvert.SerializeObject(macro));
        }

        // ================================================================
        //  UPDATE — modifica un macro existente
        // ================================================================
        private void HandleUpdate(ConnectionInformation socket, GameClient client, string payload)
        {
            int userId = client.GetHabbo().Id;

            if (!TryParseMacro(payload, out var macro, out string err))
            { Send(socket, "error", err); return; }

            if (macro.Id <= 0)
            { Send(socket, "error", "ID de macro inválido"); return; }

            // Verificar que el macro pertenece al usuario
            if (!MacroBelongsToUser(macro.Id, userId))
            { Send(socket, "error", "No tienes permiso para editar este macro"); return; }

            // Keybind único (excluye el propio macro)
            if (!string.IsNullOrEmpty(macro.Keybind) && KeybindExists(userId, macro.Keybind, macro.Id))
            { Send(socket, "error", $"La tecla '{macro.Keybind}' ya está en uso"); return; }

            macro.UserId = userId;
            UpdateMacro(macro);

            Send(socket, "updated", JsonConvert.SerializeObject(macro));
        }

        // ================================================================
        //  DELETE — elimina un macro
        // ================================================================
        private void HandleDelete(ConnectionInformation socket, int userId, string payload)
        {
            if (!int.TryParse(payload.Trim(), out int macroId) || macroId <= 0)
            { Send(socket, "error", "ID inválido"); return; }

            if (!MacroBelongsToUser(macroId, userId))
            { Send(socket, "error", "No tienes permiso para eliminar este macro"); return; }

            DeleteMacro(macroId, userId);
            Send(socket, "deleted", macroId.ToString());
        }

        // ================================================================
        //  EXECUTE — ejecuta la secuencia de comandos en el chat del Habbo
        // ================================================================
        private void HandleExecute(ConnectionInformation socket, GameClient client, string payload)
        {
            int userId = client.GetHabbo().Id;

            if (!int.TryParse(payload.Trim(), out int macroId) || macroId <= 0)
            { Send(socket, "error", "ID inválido"); return; }

            var macro = GetMacroById(macroId, userId);
            if (macro == null)
            { Send(socket, "error", "Macro no encontrado"); return; }

            if (!macro.Active)
            { Send(socket, "error", "El macro está inactivo"); return; }

            if (!RoleplayManager.GenerateRoom(client.GetRoomUser()?.RoomId ?? 0, out Room room))
            { Send(socket, "error", "No estás en ninguna sala"); return; }

            var roomUser = room.GetRoomUserManager().GetRoomUserByHabbo(userId);
            if (roomUser == null)
            { Send(socket, "error", "No se encontró tu personaje en la sala"); return; }

            int loopCount = Math.Clamp(macro.LoopCount, 1, MaxLoops);
            var commands = macro.Commands
                                 .Where(c => !string.IsNullOrWhiteSpace(c))
                                 .Take(MaxCommandsPerMacro)
                                 .ToList();

            // ✅ Sin await, sin delay — disparo inmediato en hilo separado
            Task.Run(async () =>
            {
                for (int loop = 0; loop < loopCount; loop++)
                {
                    foreach (var originalCmd in commands)
                    {
                        try
                        {
                            string cmd = originalCmd;

                            // Resolver "x" como LastCommand
                            if (cmd == "x" && client.GetRoleplay()?.LastCommand != "")
                                cmd = client.GetRoleplay().LastCommand;

                            if (cmd.StartsWith(":", StringComparison.CurrentCulture))
                            {
                                // ✅ Comando — parsear sin await para no bloquear
                                _ = PolarEnvironment.GetGame().GetChatManager()
                                        .GetCommands().Parse(client, cmd);
                            }
                            else
                            {
                                // ✅ Texto normal — mismo flujo que ChatEvent
                                var roleplay = client.GetRoleplay();
                                if (roleplay != null)
                                {
                                    if (roleplay.IsDead)
                                        roomUser.OnChat(3, "[ " + cmd + " ]", false, string.Empty);
                                    else if (roleplay.CurHealth <= 25)
                                        roomUser.OnChat(3, cmd, false, string.Empty);
                                    else if (roleplay.CurHealth <= 40)
                                        roomUser.OnChat(5, cmd, false, string.Empty);
                                    else
                                        roomUser.OnChat(roomUser.LastBubble, cmd, false, string.Empty);
                                }
                                else
                                {
                                    roomUser.OnChat(roomUser.LastBubble, cmd, false, string.Empty);
                                }
                            }
                        }
                        catch { }
                    }
                }
            });

            Send(socket, "executed", macroId.ToString());
        }

        // ================================================================
        //  Validación y parseo del JSON del macro
        // ================================================================
        private bool TryParseMacro(string json, out UserMacro macro, out string error)
        {
            macro = null; error = null;

            if (string.IsNullOrWhiteSpace(json))
            { error = "Payload vacío"; return false; }

            try { macro = JsonConvert.DeserializeObject<UserMacro>(json); }
            catch { error = "JSON inválido"; return false; }

            if (macro == null)
            { error = "JSON inválido"; return false; }

            // Nombre
            if (string.IsNullOrWhiteSpace(macro.Name))
            { error = "El nombre es obligatorio"; return false; }
            macro.Name = macro.Name.Trim();
            if (macro.Name.Length > MaxNameLength)
            { error = $"El nombre no puede superar {MaxNameLength} caracteres"; return false; }

            // Comandos
            if (macro.Commands == null || macro.Commands.Count == 0)
            { error = "Se requiere al menos un comando"; return false; }
            if (macro.Commands.Count > MaxCommandsPerMacro)
            { error = $"Máximo {MaxCommandsPerMacro} comandos por macro"; return false; }
            macro.Commands = macro.Commands.Select(c => c?.Trim()).Where(c => !string.IsNullOrEmpty(c)).ToList();

            // Delay y loops
            macro.DelayMs = Math.Clamp(macro.DelayMs <= 0 ? 300 : macro.DelayMs, MinDelayMs, MaxDelayMs);
            macro.LoopCount = Math.Clamp(macro.LoopCount <= 0 ? 1 : macro.LoopCount, 1, MaxLoops);

            // Categoría
            if (string.IsNullOrWhiteSpace(macro.Category) || !AllowedCategories.Contains(macro.Category))
                macro.Category = "General";

            return true;
        }

        // ================================================================
        //  Helpers de base de datos MySQL
        // ================================================================
        private List<UserMacro> GetUserMacros(int userId)
        {
            var list = new List<UserMacro>();

            using var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
            dbClient.SetQuery(
                "SELECT `id`,`name`,`keybind`,`commands`,`delay_ms`,`loop_count`,`category`,`active` " +
                "FROM `user_macros` WHERE `user_id` = @uid ORDER BY `id` DESC");
            dbClient.AddParameter("uid", userId);

            var table = dbClient.getTable();
            if (table == null) return list;

            foreach (DataRow row in table.Rows)
                list.Add(RowToMacro(row, userId));

            return list;
        }

        private UserMacro GetMacroById(int macroId, int userId)
        {
            using var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
            dbClient.SetQuery(
                "SELECT `id`,`name`,`keybind`,`commands`,`delay_ms`,`loop_count`,`category`,`active` " +
                "FROM `user_macros` WHERE `id` = @id AND `user_id` = @uid LIMIT 1");
            dbClient.AddParameter("id", macroId);
            dbClient.AddParameter("uid", userId);

            var row = dbClient.getRow();
            return row == null ? null : RowToMacro(row, userId);
        }

        private int InsertMacro(UserMacro m)
        {
            using var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
            dbClient.SetQuery(
                "INSERT INTO `user_macros` (`user_id`,`name`,`keybind`,`commands`,`delay_ms`,`loop_count`,`category`,`active`) " +
                "VALUES (@uid,@name,@kb,@cmds,@delay,@loops,@cat,1)");
            dbClient.AddParameter("uid", m.UserId);
            dbClient.AddParameter("name", m.Name);
            dbClient.AddParameter("kb", m.Keybind ?? "");
            dbClient.AddParameter("cmds", JsonConvert.SerializeObject(m.Commands));
            dbClient.AddParameter("delay", m.DelayMs);
            dbClient.AddParameter("loops", m.LoopCount);
            dbClient.AddParameter("cat", m.Category);
            return (int)dbClient.InsertQuery();
        }

        private void UpdateMacro(UserMacro m)
        {
            using var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
            dbClient.SetQuery(
                "UPDATE `user_macros` SET `name`=@name,`keybind`=@kb,`commands`=@cmds," +
                "`delay_ms`=@delay,`loop_count`=@loops,`category`=@cat,`active`=@active " +
                "WHERE `id`=@id AND `user_id`=@uid");
            dbClient.AddParameter("name", m.Name);
            dbClient.AddParameter("kb", m.Keybind ?? "");
            dbClient.AddParameter("cmds", JsonConvert.SerializeObject(m.Commands));
            dbClient.AddParameter("delay", m.DelayMs);
            dbClient.AddParameter("loops", m.LoopCount);
            dbClient.AddParameter("cat", m.Category);
            dbClient.AddParameter("active", m.Active ? 1 : 0);
            dbClient.AddParameter("id", m.Id);
            dbClient.AddParameter("uid", m.UserId);
            dbClient.RunQuery();
        }

        private void DeleteMacro(int macroId, int userId)
        {
            using var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
            dbClient.SetQuery("DELETE FROM `user_macros` WHERE `id`=@id AND `user_id`=@uid");
            dbClient.AddParameter("id", macroId);
            dbClient.AddParameter("uid", userId);
            dbClient.RunQuery();
        }

        private int CountUserMacros(int userId)
        {
            using var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
            dbClient.SetQuery("SELECT COUNT(*) FROM `user_macros` WHERE `user_id`=@uid");
            dbClient.AddParameter("uid", userId);
            return dbClient.getInteger();
        }

        private bool MacroBelongsToUser(int macroId, int userId)
        {
            using var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
            dbClient.SetQuery("SELECT COUNT(*) FROM `user_macros` WHERE `id`=@id AND `user_id`=@uid");
            dbClient.AddParameter("id", macroId);
            dbClient.AddParameter("uid", userId);
            return dbClient.getInteger() > 0;
        }
        private bool KeybindExists(int userId, string keybind, int excludeId)
        {
            using var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
            dbClient.SetQuery(
                "SELECT COUNT(*) FROM `user_macros` " +
                "WHERE `user_id`=@uid AND `keybind`=@kb AND `id`<>@excl AND `active`=1");
            dbClient.AddParameter("uid", userId);
            dbClient.AddParameter("kb", keybind);
            dbClient.AddParameter("excl", excludeId);
            return dbClient.getInteger() > 0;
        }


        // ================================================================
        private static UserMacro RowToMacro(DataRow row, int userId)
{
    List<string> cmds;
    try { cmds = JsonConvert.DeserializeObject<List<string>>(row["commands"]?.ToString() ?? "[]") ?? new List<string>(); }
    catch { cmds = new List<string>(); }

    return new UserMacro
    {
        Id = Convert.ToInt32(row["id"]),
        UserId = userId,
        Name = row["name"]?.ToString() ?? "",
        Keybind = row["keybind"]?.ToString() ?? "",
        Commands = cmds,
        DelayMs = Convert.ToInt32(row["delay_ms"]),
        LoopCount = Convert.ToInt32(row["loop_count"]),
        Category = row["category"]?.ToString() ?? "General",
        Active = Convert.ToInt32(row["active"]) == 1
    };
}

// ================================================================
//  Envío al socket — formato: "compose_macros|<tipo>|<data>"
// ================================================================
private static void Send(ConnectionInformation socket, string type, string data)
{
    if (socket == null) return;
    try
    {
        var raw = System.Text.Encoding.UTF8.GetBytes($"compose_macros|{type}|{data}");
        socket.SendData(raw);
    }
    catch { /* ignorar si el socket se cerró */ }
}
    }
}