using log4net;
using Newtonsoft.Json;
using Polar.Core;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Roleplay.Web;
using Polar.HabboHotel.Roleplay.Web.Incoming.General;
using Polar.HabboHotel.Roleplay.Web.Incoming.Others;
using Polar.HabboHotel.Roleplay.Web.Outgoing;
using Polar.HabboHotel.Roleplay.Web.Outgoing.Default;
using Polar.HabboHotel.Roleplay.Web.Outgoing.Misc;
using Polar.HabboHotel.Roleplay.Web.Outgoing.Purse;
using Polar.HabboHotel.Roleplay.Web.OutGoing.Misc;
using Polar.HabboRoleplay.Web.Outgoing.Statistics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;

// ⚠️  CAMBIO ARQUITECTÓNICO:
//     Fleck (WebSocketServer / IWebSocketConnection) ha sido eliminado por completo.
//     El WebEventManager ya NO abre ningún puerto propio.
//     Todo el tráfico llega por el mismo puerto del juego a través de GamePacketParser,
//     que detecta si el payload decodificado es JSON y lo delega aquí.
//
//     Impactos en el resto del proyecto:
//       • Eliminar la dependencia NuGet de Fleck si no se usa en otro lugar.
//       • Quitar la entrada "ws.tcp.port" del config (o mantenerla como no-op).
//       • Cualquier código que referencie IWebSocketConnection como tipo de socket
//         debe migrar a ConnectionInformation (ver alias WsConn abajo).
//       • Los eventos IWebEvent reciben ahora ConnectionInformation en lugar de
//         IWebSocketConnection — actualiza las implementaciones concretas.

namespace Polar.HabboHotel.Roleplay.Web
{
    // ────────────────────────────────────────────────────────────────
    //  Alias de conveniencia para no reescribir cada firma
    // ────────────────────────────────────────────────────────────────
    using WsConn = ConnectionManager.ConnectionInformation;

    // ────────────────────────────────────────────────────────────────
    //  WebSocketUser  (igual que antes pero con WsConn en lugar de IWebSocketConnection)
    // ────────────────────────────────────────────────────────────────
    public class WebSocketUser : IDisposable
    {
        public int    Id         { get; private set; }
        public string Username   { get; set; }
        public bool   Closing    { get; set; }
        public WsConn Connection { get; private set; }
        private bool _disposed;

        public WebSocketUser(int id, string username, WsConn connection)
        {
            Id         = id;
            Username   = username ?? string.Empty;
            Closing    = false;
            Connection = connection;
        }

        public void Dispose()
        {
            if (_disposed) return;
            Id         = 0;
            Username   = null;
            Closing    = true;
            Connection = null;
            _disposed  = true;
        }
    }

    // ────────────────────────────────────────────────────────────────
    //  WebEventManager  (sin Fleck — dispatcher puro)
    // ────────────────────────────────────────────────────────────────
    public sealed class WebEventManager : IDisposable
    {
        private static readonly ILog log =
            LogManager.GetLogger("Polar.HabboHotel.Roleplayer.Web.WebEventManager");

        // Clave: ConnectionInformation del cliente  →  WebSocketUser
        public  ConcurrentDictionary<WsConn, WebSocketUser>  _webSockets;
        private ConcurrentDictionary<string, IWebEvent>      _webEvents;

        // Índice userId → set de conexiones activas (thread-safe sin List<T> mutable)
        private readonly ConcurrentDictionary<int, ConcurrentDictionary<WsConn, byte>>
            _userSocketIndex;

        private bool _disposed;

        // ────────────────────────────────────────────────
        //  Constructor  (ya no lee config de puertos ni SSL)
        // ────────────────────────────────────────────────
        public WebEventManager()
        {
            _webSockets      = new ConcurrentDictionary<WsConn, WebSocketUser>();
            _webEvents       = new ConcurrentDictionary<string, IWebEvent>();
            _userSocketIndex = new ConcurrentDictionary<int, ConcurrentDictionary<WsConn, byte>>();

            RegisterIncoming();
            RegisterOutgoing();
        }

        // Init ya no arranca ningún servidor; se mantiene por compatibilidad de llamada.
        public void Init() { /* sin-op: el servidor es GameSocketManager */ }

        // ────────────────────────────────────────────────
        //  PUNTO DE ENTRADA PRINCIPAL
        //  Llamado por GamePacketParser cuando detecta JSON
        // ────────────────────────────────────────────────

        /// <summary>
        /// Procesa un mensaje JSON llegado por el canal WebSocket del juego.
        /// Debe llamarse desde GamePacketParser.ProcessDecodedData() cuando
        /// el primer byte del payload decodificado sea '{' (0x7B).
        /// </summary>
        public void HandleIncomingJson(WsConn connection, string json)
        {
            if (connection == null || string.IsNullOrEmpty(json)) return;
            OnSocketMessage(connection, json);
        }

        /// <summary>
        /// Registra la conexión WebSocket de un cliente en cuanto se detecta
        /// que es WebSocket (después del handshake en GamePacketParser).
        /// Llamado desde GamePacketParser tras completar el handshake.
        /// </summary>
        public void OnSocketAdd(WsConn connection, int userId)
        {
            if (connection == null || userId <= 0) return;
            try
            {
                // Registrar directamente — si ya existe para este userId, reemplazar
                // La conexión TCP es siempre la misma para el mismo usuario
                var wsUser = new WebSocketUser(userId, "", connection);
                _webSockets[connection] = wsUser;
                
                // Actualizar índice
                var set = _userSocketIndex.GetOrAdd(userId,
                    _ => new ConcurrentDictionary<WsConn, byte>());
                set.TryAdd(connection, 0);

                log.Info($"WebSocket registered: UserId={userId}");
            }
            catch (Exception ex) { log.Error("OnSocketAdd error", ex); }
        }


        public void OnSocketRemove(WsConn connection)
        {
            if (connection == null) return;

            // Solo limpiar esta conexión específica del índice/diccionario.
            // NO cerramos otras conexiones del mismo usuario — podrían ser
            // cambios de sala donde la conexión TCP sigue activa.
            if (!_webSockets.TryRemove(connection, out var user)) return;

            try
            {
                RemoveFromIndex(user.Id, connection);
                user.Closing = true;
                user.Dispose();
                log.Info($"WebSocket removed: UserId={user.Id}");
            }
            catch (Exception ex) { log.Error("OnSocketRemove error", ex); }
        }

        // ────────────────────────────────────────────────
        //  Register events
        // ────────────────────────────────────────────────
        public void RegisterIncoming()
        {
            _webEvents.TryAdd("event_retrieveconnectingstatistics", new RetrieveStatsWebEvent());
            _webEvents.TryAdd("event_pong",                         new PongWebEvent());
        }

        public void RegisterOutgoing()
        {
            _webEvents.TryAdd("event_macro",            new MacroWebEvent());
            _webEvents.TryAdd("event_psv",              new PSVWebEvent());
            _webEvents.TryAdd("event_sendjsalert",      new SendNotificationWebEvent());
            _webEvents.TryAdd("event_htmlpage",         new HtmlPageWebEvent());
            _webEvents.TryAdd("event_updateonlinecount",new OnlineCountWebEvent());
            _webEvents.TryAdd("event_buscados",         new WantedWebEvent());
            _webEvents.TryAdd("event_manejar",          new ManejarWebEvent());
            _webEvents.TryAdd("event_bounty",           new BountyWebEvent());
            _webEvents.TryAdd("event_feedcomposer",     new LiveFeedComposer());
            _webEvents.TryAdd("event_vip",              new VIPWebEvent());
            _webEvents.TryAdd("event_changename",       new ChangeNameWebEvent());
            _webEvents.TryAdd("event_phone",            new PhoneWebEvent());
            _webEvents.TryAdd("event_characterbar",     new RetrieveUStatsWebEvent());
            _webEvents.TryAdd("event_charweapons",      new RetrieveUWeapons());
            _webEvents.TryAdd("event_atm",              new ATMWebEvent());
            _webEvents.TryAdd("event_restaurant",       new FoodWebEvent());
            _webEvents.TryAdd("event_house",            new HousesWebEvent());
            _webEvents.TryAdd("event_apart",            new ApartmentsWebEvent());
            _webEvents.TryAdd("event_purge",            new PurgeWebEvent());
            _webEvents.TryAdd("item",                   new ItemWebEvent());
            _webEvents.TryAdd("event_item",             new ItemWebEvent());
            _webEvents.TryAdd("event_shop",             new WeaponsWebEvent());
            _webEvents.TryAdd("event_skins",            new WSkinWebEvent());
            _webEvents.TryAdd("event_wizard",           new HechizosWebEvent());
            _webEvents.TryAdd("event_products",         new ProductsWebEvent());
            _webEvents.TryAdd("event_initwsdialogues",  new InitWSDialogues());
            _webEvents.TryAdd("event_actions",          new ActionWebEvent());
            _webEvents.TryAdd("event_moves",            new MoveWebEvent());
            _webEvents.TryAdd("event_timerdialogue",    new TimerDialogueWebEvent());
            _webEvents.TryAdd("event_captcha",          new CaptchaWebEvent());
            _webEvents.TryAdd("event_business",         new BusinessWebEvent());
            _webEvents.TryAdd("event_commands",         new CommandsWebEvent());
            _webEvents.TryAdd("event_gang",             new GangsWebEvent());
            _webEvents.TryAdd("event_stats",            new StatsWebEvent());
            _webEvents.TryAdd("event_target",           new TargetWebEvent());
            _webEvents.TryAdd("event_group",            new GroupsWebEvent());
            _webEvents.TryAdd("event_vehicle",          new VehiclesWebEvent());
            _webEvents.TryAdd("event_camionero",        new CamioneroWebEvent());
            _webEvents.TryAdd("event_basurero",         new BasureroWebEvent());
            _webEvents.TryAdd("event_armero",           new ArmeroWebEvent());
            _webEvents.TryAdd("event_hospital",         new HospitalWebEvent());
            _webEvents.TryAdd("event_driving",          new DrivingWebEvent());
            _webEvents.TryAdd("event_tutorial",         new TutorialWebEvent());
            _webEvents.TryAdd("event_purse",            new PurseWebEvent());
            _webEvents.TryAdd("event_userprofile",      new ProfileWebEvent());
            _webEvents.TryAdd("event_mapa",             new MapaWebEvent());
            _webEvents.TryAdd("event_taxi",             new TaxiWebEvent());
            _webEvents.TryAdd("sendnote",               new SendNoteVoice());
        }

        // ────────────────────────────────────────────────
        //  Despacho de mensajes entrantes
        // ────────────────────────────────────────────────
        private void OnSocketMessage(WsConn connection, string data)
        {
            try
            {
                if (string.IsNullOrEmpty(data)) return;
                if (data == "ping" || data == "pong") return;

                var received = JsonConvert.DeserializeObject<WebEvent>(data);
                if (received == null || string.IsNullOrEmpty(received.EventName)) return;

                var client = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(received.UserId);
                if (client == null || client.LoggingOut) return;
                if (client.GetHabbo().TokenId != received.Token) return;

                if (!_webSockets.TryGetValue(connection, out _)) return;

                if (_webEvents.TryGetValue(received.EventName, out var webEvent))
                    webEvent.Execute(client, received.ExtraData, connection);
                else
                    log.Debug($"Unrecognized Web Event: '{received.EventName}'");
            }
            catch (JsonException)
            {
                log.Debug($"Invalid JSON: {data?.Substring(0, Math.Min(100, data?.Length ?? 0))}");
            }
            catch (Exception ex) when (ex is IOException || ex is SocketException) { }
            catch (Exception ex) { log.Error("OnSocketMessage error", ex); }
        }

        // ────────────────────────────────────────────────
        //  ExecuteWebEvent
        // ────────────────────────────────────────────────
        public bool ExecuteWebEvent(GameClient client, string eventName, string receivedData)
        {
            if (string.IsNullOrEmpty(eventName)) return false;

            var connection = GetUsersConnection(client);
            if (connection == null) return false;

            try
            {
                if (!_webEvents.TryGetValue(eventName, out var webEvent)) return false;

                webEvent.Execute(client, receivedData, connection);
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is SocketException) { return false; }
            catch (Exception ex) { log.Error("ExecuteWebEvent error", ex); return false; }
        }

        // ────────────────────────────────────────────────
        //  Envío de datos al cliente
        //  Reemplaza IWebSocketConnection.Send(string)
        // ────────────────────────────────────────────────

        /// <summary>
        /// Envía un string JSON al cliente. ConnectionInformation.SendData ya
        /// se encarga del framing WebSocket via EncodeDecode.EncodeMessage().
        /// </summary>
        public void SendDataDirect(GameClient user, string data)
        {
            if (user?.GetHabbo() == null || user.LoggingOut || string.IsNullOrEmpty(data)) return;

            var connection = GetUsersConnection(user);
            if (connection == null) return;

            try
            {
                byte[] raw = Encoding.UTF8.GetBytes(data);
                connection.SendData(raw);
            }
            catch (Exception ex) when (ex is IOException || ex is SocketException) { }
            catch (Exception ex) { log.Error("SendDataDirect error", ex); }
        }

        public bool SendDataDirectFast(WsConn connection, string data)
        {
            if (connection == null || string.IsNullOrEmpty(data)) return false;
            try
            {
                byte[] raw = Encoding.UTF8.GetBytes(data);
                connection.SendData(raw);
                return true;
            }
            catch { return false; }
        }

        // ────────────────────────────────────────────────
        //  Broadcast
        // ────────────────────────────────────────────────
        public void BroadCastWebEvent(string eventName, string extraData)
        {
            if (string.IsNullOrEmpty(eventName)) return;

            if (!_webEvents.TryGetValue(eventName, out var webEvent))
            {
                log.Warn($"Broadcast of unknown event: {eventName}");
                return;
            }

            foreach (var kv in _webSockets)
            {
                try
                {
                    if (!kv.Value.Closing)
                        webEvent.Execute(null, extraData, kv.Key);
                }
                catch (Exception ex) when (ex is IOException || ex is SocketException) { continue; }
                catch (Exception ex) { log.Debug($"Broadcast error: {ex.Message}"); }
            }
        }

        public void BroadCastWebData(string data)
        {
            if (string.IsNullOrEmpty(data)) return;

            byte[] raw = Encoding.UTF8.GetBytes(data);
            foreach (var kv in _webSockets)
            {
                try
                {
                    if (SocketReady(kv.Key))
                        kv.Key.SendData(raw);
                }
                catch (Exception ex) when (ex is IOException || ex is SocketException) { continue; }
                catch (Exception ex) { log.Error("BroadCastWebData error", ex); }
            }
        }

        // ────────────────────────────────────────────────
        //  SocketReady
        // ────────────────────────────────────────────────
        public bool SocketReady(WsConn? connection)
        {
            if (connection == null || !connection.IsWebSocket) return false;
            if (_webSockets.TryGetValue(connection, out var user) && user.Closing) return false;
            return true;
        }

        public bool SocketReady(GameClient user, bool checkLogout = false)
        {
            if (user?.GetHabbo() == null || user.GetRoleplay() == null) return false;
            if (checkLogout && user.LoggingOut) return false;
            return SocketReady(GetUsersConnection(user));
        }

        // ────────────────────────────────────────────────
        //  Lookups
        // ────────────────────────────────────────────────
        public WsConn? GetUsersConnection(GameClient user)
        {
            if (user?.GetHabbo() == null || user.LoggingOut) return null;
            int userId = user.GetHabbo().Id;

            // 1. Buscar por índice
            if (_userSocketIndex.TryGetValue(userId, out var set))
                foreach (var conn in set.Keys)
                    if (conn != null && conn.IsWebSocket) return conn;

            // 2. Buscar en diccionario
            foreach (var kv in _webSockets)
                if (kv.Value?.Id == userId) return kv.Key;

            // 3. Fallback directo — la conexión del GameClient
            var direct = user.GetConnection();
            if (direct?.IsWebSocket == true) return direct;

            return null;
        }


        public int GetSocketsUserID(WsConn connection)
        {
            // En el sistema unificado el userId viene del token JWT/WebEvent,
            // no de la URL path. Consultamos el índice inverso.
            foreach (var kv in _userSocketIndex)
            {
                if (kv.Value.ContainsKey(connection))
                    return kv.Key;
            }
            return 0;
        }

        public List<GameClient> GetSocketsClient(WsConn connection)
        {
            int uid = GetSocketsUserID(connection);
            if (uid <= 0) return new List<GameClient>();

            var mgr = PolarEnvironment.GetGame()?.GetClientManager();
            if (mgr == null) return new List<GameClient>();

            return mgr.GetClients
                .Where(c => c != null && !c.LoggingOut &&
                            c.GetHabbo()?.Id == uid &&
                            SocketReady(c))
                .ToList();
        }

        public ConcurrentDictionary<WsConn, WebSocketUser> GetConnectedUsers() => _webSockets;

        public void DeactivateSocket(WsConn connection)
        {
            if (connection == null) return;

            if (_webSockets.TryRemove(connection, out var user))
            {
                int uid = user.Id;
                user.Closing = true;
                user.Dispose();
                RemoveFromIndex(uid, connection);
            }
        }

        public void CloseSocketByGameClient(int userId)
        {
            if (userId <= 0) return;
            try { CloseSimilarSockets(userId); }
            catch (Exception ex) { log.Debug($"Error closing socket for user {userId}: {ex.Message}"); }
        }

        public void CloseSimilarSockets(int id)
        {
            if (id <= 0) return;
            foreach (var conn in GetSimilarSockets(id))
                DeactivateSocket(conn);
        }

        public List<WsConn> GetSimilarSockets(int id)
        {
            if (_userSocketIndex.TryGetValue(id, out var set))
                return set.Keys.ToList();

            return _webSockets
                .Where(kv => kv.Value?.Id == id)
                .Select(kv => kv.Key)
                .ToList();
        }

        public void OnSocketError(string error, string exception) =>
            Logging.LogWebSocketError(error, exception);

        // ────────────────────────────────────────────────
        //  Índice thread-safe
        // ────────────────────────────────────────────────
        private void AddToIndex(int userId, WsConn connection)
        {
            var set = _userSocketIndex.GetOrAdd(userId,
                _ => new ConcurrentDictionary<WsConn, byte>());
            set.TryAdd(connection, 0);
        }

        private void RemoveFromIndex(int userId, WsConn connection)
        {
            if (!_userSocketIndex.TryGetValue(userId, out var set)) return;
            set.TryRemove(connection, out _);
            if (set.IsEmpty)
                _userSocketIndex.TryRemove(userId, out _);
        }

        // ────────────────────────────────────────────────
        //  IDisposable
        // ────────────────────────────────────────────────
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            foreach (var conn in _webSockets.Keys.ToList())
                DeactivateSocket(conn);

            _webSockets.Clear();
            _userSocketIndex.Clear();
            _webEvents.Clear();

            log.Info("WebEventManager disposed.");
        }
    }
}