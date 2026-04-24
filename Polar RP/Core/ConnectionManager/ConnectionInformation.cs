using Polar.Communication.WebSocket;  // ✅ FIX #1: Eliminado el using duplicado
using SharedPacketLib;
using System;
using System.Net.Sockets;
using Polar.Core;

namespace ConnectionManager
{
    public class ConnectionInformation : IDisposable
    {
        private readonly string _ip;
        private readonly int _connectionID;

        // ✅ FIX #2: int con Interlocked en lugar de bool para operaciones atómicas
        // 0 = desconectado, 1 = conectado
        private int _isConnected;

        private readonly AsyncCallback _sendCallback;

        public IDataParser parser;
        public event ConnectionChange connectionClose;
        private Socket _dataSocket;

        // ✅ FIX #3: Cada operación BeginReceive usa su propio buffer para evitar
        //           corrupción de datos por reutilización concurrente del mismo array.
        private readonly int _bufferSize;

        // ✅ FIX #4: Campo privado, expuesto como propiedad pública de solo lectura
        //           para que Game.cs pueda leerlo sin poder modificarlo externamente.
        private DateTime _lastReceiveUtc;
        public DateTime LastReceiveUtc => this._lastReceiveUtc;

        public delegate void ConnectionChange(ConnectionInformation information);
        public bool IsWebSocket;

        // ✅ FIX #5: Timeout de inactividad con valor sensato por defecto (2 min)
        private static readonly TimeSpan DefaultInactivityThreshold = TimeSpan.FromMinutes(2);
        public TimeSpan InactivityThreshold { get; set; } = DefaultInactivityThreshold;

        public ConnectionInformation(IDataParser parser, Socket dataStream, string ip, int connectionID)
        {
            this.parser = parser;
            this._bufferSize = GameSocketManagerStatics.BUFFER_SIZE;
            this._dataSocket = dataStream;
            this._isConnected = 0;

            try
            {
                this._dataSocket.SendTimeout = 0;
                this._dataSocket.ReceiveTimeout = 0;
                this._dataSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

                // Ajuste fino de keepalive (Windows): 60s idle, retry cada 10s
                // var keepAlive = new byte[12];
                // BitConverter.GetBytes((uint)1).CopyTo(keepAlive, 0);
                // BitConverter.GetBytes((uint)60000).CopyTo(keepAlive, 4);
                // BitConverter.GetBytes((uint)10000).CopyTo(keepAlive, 8);
                // this._dataSocket.IOControl(IOControlCode.KeepAliveValues, keepAlive, null);
            }
            catch
            {
                // Si no se pueden fijar opciones, continuar con defaults
            }

            this._sendCallback = new AsyncCallback(this.SentData);
            this._ip = ip;
            this._connectionID = connectionID;
            this._lastReceiveUtc = DateTime.UtcNow;
        }

        public string getIp() => this._ip;

        public int getConnectionID() => _connectionID;

        public void disconnect()
        {
            // ✅ FIX #6: Interlocked.Exchange garantiza que disconnect() solo
            //           ejecuta UNA vez aunque lo llamen múltiples threads.
            if (Interlocked.Exchange(ref this._isConnected, 0) == 0)
                return;

            try
            {
                if (this._dataSocket != null)
                {
                    try
                    {
                        if (this._dataSocket.Connected)
                        {
                            this._dataSocket.Shutdown(SocketShutdown.Both);
                            this._dataSocket.Close();
                        }
                    }
                    catch { }

                    this._dataSocket.Dispose();
                    this._dataSocket = null;
                }

                if (this.parser != null)
                {
                    this.parser.Dispose();
                    this.parser = null;
                }

                // ✅ FIX #7: Capturar y limpiar el evento ANTES de invocarlo
                //           para evitar invocaciones dobles si disconnect() se
                //           llama de nuevo desde el handler.
                ConnectionChange handler = this.connectionClose;
                this.connectionClose = null;
                handler?.Invoke(this);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public void startPacketProcessing()
        {
            // ✅ FIX #8: La lógica estaba invertida en el original.
            //           CompareExchange: solo entra si estaba en 0 (desconectado)
            //           y lo pone en 1 (conectado) de forma atómica.
            if (Interlocked.CompareExchange(ref this._isConnected, 1, 0) != 0)
                return;

            try
            {
                this._lastReceiveUtc = DateTime.UtcNow;

                // ✅ FIX #3 aplicado: buffer nuevo por llamada BeginReceive
                byte[] buffer = new byte[this._bufferSize];
                this._dataSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, this._incomingDataPacket, buffer);
            }
            catch
            {
                this.disconnect();
            }
        }

        private void _incomingDataPacket(IAsyncResult iAr)
        {
            if (this._isConnected == 0) return;

            int length = 0;

            try
            {
                length = this._dataSocket.EndReceive(iAr);
            }
            catch
            {
                this.disconnect();
                return;
            }

            if (length == 0)
            {
                this.disconnect();
                return;
            }

            // Actualizar timestamp de último dato recibido
            this._lastReceiveUtc = DateTime.UtcNow;

            if (this.parser == null) return;

            try
            {
                // ✅ FIX #3: Recuperar el buffer del state en lugar del campo compartido
                byte[] usedBuffer = (byte[])iAr.AsyncState;
                byte[] packet = new byte[length];
                Array.Copy(usedBuffer, packet, length);

                this.parser.handlePacketData(packet);
            }
            catch (Exception ex)
            {
                Logging.LogCriticalException($"[ConnectionInformation] Error en parser (conn={_connectionID}, ip={_ip}): {ex}");
                this.disconnect();
                return;
            }

            // ✅ FIX #9: BeginReceive fuera del finally para no enmascarar
            //           excepciones del parser y evitar continuar si hubo error.
            try
            {
                if (this._isConnected == 1)
                {
                    byte[] newBuffer = new byte[this._bufferSize];
                    this._dataSocket.BeginReceive(newBuffer, 0, newBuffer.Length, SocketFlags.None, new AsyncCallback(this._incomingDataPacket), newBuffer);
                }
            }
            catch
            {
                this.disconnect();
            }
        }

        public bool IsInactive()
        {
            try
            {
                return (DateTime.UtcNow - this._lastReceiveUtc) > this.InactivityThreshold;
            }
            catch
            {
                return false;
            }
        }

        public void SendData(byte[] packet, int offset, int length)
        {
            if (this._isConnected == 0) return;

            try
            {
                if (this.IsWebSocket)
                {
                    byte[] fragment = new byte[length];
                    Buffer.BlockCopy(packet, offset, fragment, 0, length);

                    byte[] encoded = EncodeDecode.EncodeMessage(fragment);
                    this._dataSocket.BeginSend(encoded, 0, encoded.Length, SocketFlags.None, _sendCallback, encoded);
                }
                else
                {
                    this._dataSocket.BeginSend(packet, offset, length, SocketFlags.None, _sendCallback, null);
                }
            }
            catch
            {
                this.disconnect();
            }
        }

        public void SendData(byte[] packet)
        {
            if (packet == null) return;
            SendData(packet, 0, packet.Length);
        }

        private void SentData(IAsyncResult iAr)
        {
            try
            {
                this._dataSocket.EndSend(iAr);
            }
            catch
            {
                this.disconnect();
            }
        }

        public void Dispose()
        {
            if (this._isConnected == 1)
                this.disconnect();
        }
    }
}