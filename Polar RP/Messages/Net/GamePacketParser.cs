using Polar.Communication.Packets.Incoming;
using Polar.Communication.WebSocket;
using Polar.Core;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Roleplay.Web;
using Polar.Utilities;
using SharedPacketLib;
using System;
using System.Security.Cryptography;
using System.Text;
using Polar.Utilities;
// ─────────────────────────────────────────────────────────────────────────────
//  CAMBIOS RESPECTO A LA VERSIÓN ORIGINAL
//
//  1. Se eliminó la dependencia de Nancy (ya no se usa).
//  2. PolicyRequest() ahora extrae la clave con IndexOf para mayor robustez.
//  3. ProcessDecodedData() detecta si el payload es JSON (primer byte == '{')
//     y lo delega a WebEventManager.HandleIncomingJson() en lugar de
//     intentar parsearlo como paquete binario Habbo.
//  4. Tras el handshake WebSocket se notifica a WebEventManager.OnSocketAdd()
//     para que registre la conexión en su índice.
//  5. Dispose() notifica a WebEventManager.OnSocketRemove() para limpiar el índice.
// ─────────────────────────────────────────────────────────────────────────────

namespace Polar.Net
{
    public class GamePacketParser : IDataParser, IDisposable, ICloneable
    {
        private GameClient currentClient;
        public event HandlePacket OnNewPacket;

        private bool   _halfDataRecieved = false;
        private byte[] _halfData         = null;
        private bool   _isWebSocket         = false;
        private bool   _isWebEventConnection = false; // true si conectó con path /events
        private byte[] _webSocketBuffer  = new byte[0];

        public GamePacketParser(GameClient me)
        {
            this.currentClient = me;
            this.OnNewPacket   = null;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Entrada principal
        // ─────────────────────────────────────────────────────────────────────
        public void handlePacketData(byte[] Data, bool deciphered = false)
        {
            try
            {
                if (this.OnNewPacket == null || Data == null || Data.Length == 0)
                    return;

                // 1. Handshake WebSocket (empieza con "GET")
                if (Data.Length >= 2 && Data[0] == 71 && Data[1] == 69)
                {
                    CompleteWebSocketHandshake(Data);
                    return;
                }

                // 2. Política Flash ("<p")
                if ((Data.Length == 23 || Data.Length == 22) &&
                     Data[0] == 60 && Data[1] == 112)
                {
                    var policyBytes = Encoding.UTF8.GetBytes(GetXmlPolicy());
                    currentClient.GetConnection().SendData(policyBytes, 0, policyBytes.Length);
                    return;
                }

                // 3. Canal WebSocket: acumular + decodificar frames
                if (this._isWebSocket)
                {
                    ProcessWebSocketData(Data);
                    return;
                }

                // 4. TCP normal
                ProcessNormalData(Data, deciphered);
            }
            catch (Exception e)
            {
                Logging.LogCriticalException($"[GamePacketParser] Error procesando datos: {e}");
                this._halfDataRecieved = false;
                this._halfData = null;
                this._webSocketBuffer = new byte[0];
                // NO desconectar aquí — dejar que el flujo normal lo haga
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Handshake WebSocket
        // ─────────────────────────────────────────────────────────────────────
        private void CompleteWebSocketHandshake(byte[] Data)
        {
            // Detectar si es una conexión del WEM (nxs.js) por el path "/events"
            string request = Encoding.UTF8.GetString(Data);
            if (request.Contains("GET /events ") || request.Contains("GET /events\n"))
            {
                _isWebEventConnection = true;
                // Sacar del ClientManager: /events no es un usuario del juego
                PolarEnvironment.GetGame().GetClientManager()
                    .removeConnection(currentClient.ConnectionID);
            }

            PolicyRequest(Data);
            this._isWebSocket = true;
            this.currentClient.GetConnection().IsWebSocket = true;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Buffer acumulativo + decodificación de frames WS
        // ─────────────────────────────────────────────────────────────────────
        private void ProcessWebSocketData(byte[] Data)
        {
            // Concatenar nuevos datos al buffer pendiente
            byte[] newBuffer = new byte[_webSocketBuffer.Length + Data.Length];
            Buffer.BlockCopy(_webSocketBuffer, 0, newBuffer, 0,                   _webSocketBuffer.Length);
            Buffer.BlockCopy(Data,             0, newBuffer, _webSocketBuffer.Length, Data.Length);
            _webSocketBuffer = newBuffer;

            int bufferPosition = 0;
            while (bufferPosition < _webSocketBuffer.Length)
            {
                byte[] decodedFrame = DecodeSingleWebSocketFrame(
                    _webSocketBuffer, bufferPosition, out int frameLength);

                if (decodedFrame == null || frameLength <= 0)
                    break; // frame incompleto, esperar más datos

                ProcessDecodedData(decodedFrame);
                bufferPosition += frameLength;
            }

            // Conservar datos no procesados
            if (bufferPosition >= _webSocketBuffer.Length)
            {
                _webSocketBuffer = new byte[0];
            }
            else if (bufferPosition > 0)
            {
                byte[] remaining = new byte[_webSocketBuffer.Length - bufferPosition];
                Buffer.BlockCopy(_webSocketBuffer, bufferPosition, remaining, 0, remaining.Length);
                _webSocketBuffer = remaining;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Decodificación de un frame WebSocket RFC 6455
        // ─────────────────────────────────────────────────────────────────────
        private byte[] DecodeSingleWebSocketFrame(byte[] data, int startPosition, out int frameLength)
        {
            frameLength = 0;
            if (data.Length - startPosition < 2) return null;

            try
            {
                byte secondByte  = data[startPosition + 1];
                bool masked      = (secondByte & 0x80) != 0;
                int  dataLength  = secondByte & 127;
                int  indexFirstMask = 2;

                if (dataLength == 126)
                {
                    if (data.Length - startPosition < 4) return null;
                    dataLength      = (data[startPosition + 2] << 8) | data[startPosition + 3];
                    indexFirstMask  = 4;
                }
                else if (dataLength == 127)
                {
                    if (data.Length - startPosition < 10) return null;
                    dataLength = (data[startPosition + 6] << 24) | (data[startPosition + 7] << 16)
                               | (data[startPosition + 8] <<  8) |  data[startPosition + 9];
                    indexFirstMask = 10;
                }

                frameLength = indexFirstMask + (masked ? 4 : 0) + dataLength;
                if (data.Length - startPosition < frameLength) return null;

                byte[] decoded;
                if (masked)
                {
                    int  maskStart = startPosition + indexFirstMask;
                    byte[] mask    = new byte[4];
                    Buffer.BlockCopy(data, maskStart, mask, 0, 4);

                    decoded = new byte[dataLength];
                    int dataStart = maskStart + 4;
                    for (int i = 0; i < dataLength; i++)
                        decoded[i] = (byte)(data[dataStart + i] ^ mask[i % 4]);
                }
                else
                {
                    decoded = new byte[dataLength];
                    Buffer.BlockCopy(data, startPosition + indexFirstMask, decoded, 0, dataLength);
                }

                return decoded;
            }
            catch { return null; }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Payload decodificado:
        //   - Conexión /events (nxs.js) → siempre JSON → WebEventManager
        //   - Conexión normal  (Nitro)  → siempre binario → ProcessNormalData
        // ─────────────────────────────────────────────────────────────────────
        private void ProcessDecodedData(byte[] decodedData)
        {
            if (decodedData == null || decodedData.Length == 0) return;

            if (_isWebEventConnection)
            {
                // Esta conexión es del browser (nxs.js) — solo JSON
                if (decodedData[0] != 0x7B) return; // ignorar no-JSON
                string json = Encoding.UTF8.GetString(decodedData);
                var wem = PolarEnvironment.GetGame()?.GetWebEventManager();
                if (wem == null) return;
                TryRegisterWebSocketUser(wem, currentClient.GetConnection(), json);
                wem.HandleIncomingJson(currentClient.GetConnection(), json);
                return;
            }

            // Conexión normal de Nitro → paquete Habbo binario
            ProcessNormalData(decodedData, true);
        }

        private void TryRegisterWebSocketUser(
            Polar.HabboHotel.Roleplay.Web.WebEventManager wem,
            ConnectionManager.ConnectionInformation conn, string json)
        {
            try
            {
                if (wem.GetSocketsUserID(conn) > 0) return;
                var partial = Newtonsoft.Json.JsonConvert
                    .DeserializeAnonymousType(json, new { userId = 0 });
                if (partial?.userId > 0)
                    wem.OnSocketAdd(conn, partial.userId);
            }
            catch { }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Procesamiento de datos TCP/binario normales
        // ─────────────────────────────────────────────────────────────────────
        private void ProcessNormalData(byte[] Data, bool deciphered)
        {
            if (this.OnNewPacket == null) return;

            // Reunir datos parciales previos
            if (this._halfDataRecieved)
            {
                byte[] full = new byte[this._halfData.Length + Data.Length];
                Buffer.BlockCopy(this._halfData, 0, full, 0,                   this._halfData.Length);
                Buffer.BlockCopy(Data,           0, full, this._halfData.Length, Data.Length);
                this._halfDataRecieved = false;
                this._halfData         = null;
                ProcessNormalData(full, true);
                return;
            }

            // RC4 solo para TCP
            if (currentClient is { RC4Client: { } } && !deciphered && !_isWebSocket)
            {
                currentClient.RC4Client.Decrypt(ref Data);
                deciphered = true;
            }

            int position = 0;
            while (position < Data.Length)
            {
                if (this.OnNewPacket == null) break;

                if (Data.Length - position < 4)
                {
                    this._halfData         = new byte[Data.Length - position];
                    Buffer.BlockCopy(Data, position, this._halfData, 0, this._halfData.Length);
                    this._halfDataRecieved = true;
                    break;
                }

                uint packetLength = (uint)HabboEncoding.DecodeInt32(new byte[] {
                    Data[position], Data[position + 1], Data[position + 2], Data[position + 3]
                });

                if (packetLength < 2 || packetLength > 1_000_000)
                {
                    packetLength = BitConverter.ToUInt32(Data, position);
                    if (packetLength < 2 || packetLength > 1_000_000)
                    {
                        position++;
                        continue;
                    }
                }

                if (Data.Length - position - 4 < packetLength)
                {
                    this._halfData         = new byte[Data.Length - position];
                    Buffer.BlockCopy(Data, position, this._halfData, 0, this._halfData.Length);
                    this._halfDataRecieved = true;
                    break;
                }

                byte[] Packet = new byte[packetLength];
                Buffer.BlockCopy(Data, position + 4, Packet, 0, (int)packetLength);

                if (Packet.Length >= 2)
                {
                    ushort Header  = (ushort)HabboEncoding.DecodeInt16(Packet);
                    byte[] Content = new byte[Packet.Length - 2];
                    if (Content.Length > 0)
                        Buffer.BlockCopy(Packet, 2, Content, 0, Content.Length);

                    ClientPacket Message = new ClientPacket(Header, Content);

                    var handler = this.OnNewPacket;
                    if (handler != null)
                        handler.Invoke(Message);
                    else
                        break;
                }

                position += 4 + (int)packetLength;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────────────────────────────────
        private static string GetXmlPolicy() =>
            "<?xml version=\"1.0\"?>\r\n" +
            "<!DOCTYPE cross-domain-policy SYSTEM \"/xml/dtds/cross-domain-policy.dtd\">\r\n" +
            "<cross-domain-policy>\r\n" +
            "<allow-access-from domain=\"*\" to-ports=\"1-31111\" />\r\n" +
            "</cross-domain-policy>\x0";

        private void PolicyRequest(byte[] packet)
        {
            string data = Encoding.UTF8.GetString(packet);

            int keyIndex = data.IndexOf("ey:", StringComparison.Ordinal);
            if (keyIndex < 0) return;

            string keyLine = data.Substring(keyIndex + 3);
            int    lineEnd = keyLine.IndexOfAny(new[] { '\r', '\n' });
            string key     = lineEnd >= 0 ? keyLine.Substring(0, lineEnd).Trim() : keyLine.Trim();

            string longKey    = key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            byte[] hashBytes  = SHA1.Create().ComputeHash(Encoding.ASCII.GetBytes(longKey));
            string acceptKey  = Convert.ToBase64String(hashBytes);

            string response =
                "HTTP/1.1 101 Switching Protocols\r\n" +
                "Upgrade: websocket\r\n"               +
                "Connection: Upgrade\r\n"              +
                "Sec-WebSocket-Accept: " + acceptKey   + "\r\n\r\n";

            byte[] responseBytes = Encoding.UTF8.GetBytes(response);
            currentClient.GetConnection().SendData(responseBytes, 0, responseBytes.Length);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  IDisposable / ICloneable
        // ─────────────────────────────────────────────────────────────────────
        public void Dispose()
        {
            // Si era una conexión /events, limpiar del WebEventManager
            if (_isWebEventConnection)
            {
                try { PolarEnvironment.GetGame()?.GetWebEventManager()
                          ?.OnSocketRemove(currentClient?.GetConnection()); }
                catch { }
            }
            this.OnNewPacket = null;
            GC.SuppressFinalize(this);
        }

        public object Clone() => new GamePacketParser(this.currentClient);

        public delegate void HandlePacket(ClientPacket message);
    }
}