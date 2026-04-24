using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace Polar.Communication.Packets.Incoming
{
    public sealed class ClientPacket : IDisposable
    {
        // ✅ FIX #1: Usar ArrayPool para reducir presión en GC, igual que ServerPacket.
        //           En servidores con miles de paquetes/seg, evitar new byte[] cada vez
        //           es crítico para el rendimiento.
        private byte[] _body;
        private int _pointer;
        private int _bodyLength;   // longitud real (el array rentado puede ser mayor)
        private bool _isPooled;    // ¿el array viene del pool?

        // ✅ FIX #2: int + Interlocked para thread-safety en Dispose,
        //           igual que _isConnected en ConnectionInformation.
        private int _disposed;

        private static readonly Encoding Utf8 = new UTF8Encoding(
            encoderShouldEmitUTF8Identifier: false,
            throwOnInvalidBytes: false);

        public int Id { get; private set; }

        // ✅ FIX #3: Header era una propiedad duplicada de Id — eliminada la redundancia.
        public int RemainingLength => _bodyLength - _pointer;

        // ── Constructor principal ──────────────────────────────────────────────

        public ClientPacket(int messageId, byte[] body)
        {
            Init(messageId, body, pooled: false);
        }

        /// <summary>
        /// Constructor que acepta un array rentado del ArrayPool para evitar
        /// allocaciones innecesarias. El caller cede la propiedad del array.
        /// </summary>
        public ClientPacket(int messageId, byte[] pooledBuffer, int bodyLength)
        {
            if (pooledBuffer == null) throw new ArgumentNullException(nameof(pooledBuffer));
            if (bodyLength < 0 || bodyLength > pooledBuffer.Length)
                throw new ArgumentOutOfRangeException(nameof(bodyLength));

            Id = messageId;
            _body = pooledBuffer;
            _bodyLength = bodyLength;
            _pointer = 0;
            _isPooled = true;
            _disposed = 0;
        }

        // ── Init / Reset ───────────────────────────────────────────────────────

        public void Init(int messageId, byte[] body, bool pooled = false)
        {
            if (Volatile.Read(ref _disposed) == 1)
                throw new ObjectDisposedException(nameof(ClientPacket));

            // Devolver buffer anterior al pool si corresponde
            ReturnBuffer();

            Id = messageId;
            _body = body ?? Array.Empty<byte>();
            _bodyLength = _body.Length;
            _pointer = 0;
            _isPooled = pooled;
        }

        // ── Read primitives ────────────────────────────────────────────────────

        /// <summary>
        /// Lee exactamente <paramref name="count"/> bytes.
        /// Lanza si no hay suficientes datos (evita corrupción silenciosa).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] ReadBytes(int count)
        {
            ThrowIfDisposed();

            // ✅ FIX #4: Antes el método silenciaba la lectura inválida cambiando
            //           el tamaño. Eso puede corromper el parsing de paquetes
            //           subsiguientes. Ahora lanzamos explícitamente.
            if (count < 0 || count > RemainingLength)
                throw new InvalidOperationException(
                    $"[Packet {Id}] ReadBytes: requested {count} bytes but only {RemainingLength} remain.");

            byte[] result = new byte[count];
            Buffer.BlockCopy(_body, _pointer, result, 0, count);
            _pointer += count;
            return result;
        }

        /// <summary>
        /// Intenta leer hasta <paramref name="count"/> bytes sin lanzar.
        /// Útil cuando el tamaño puede ser variable o incompleto.
        /// </summary>
        public bool TryReadBytes(int count, out byte[] data)
        {
            ThrowIfDisposed();

            if (count < 0 || count > RemainingLength)
            {
                data = Array.Empty<byte>();
                return false;
            }

            data = new byte[count];
            Buffer.BlockCopy(_body, _pointer, data, 0, count);
            _pointer += count;
            return true;
        }

        public byte[] ReadFixedValue()
        {
            ThrowIfDisposed();

            if (RemainingLength < 2)
                return Array.Empty<byte>();

            // ✅ FIX #5: Leer el short directamente sin allocar byte[] extra.
            short len = ReadInt16Direct();
            if (len <= 0 || len > RemainingLength)
                return Array.Empty<byte>();

            return ReadBytes(len);
        }

        public string PopString()
        {
            ThrowIfDisposed();
            byte[] raw = ReadFixedValue();
            return raw.Length == 0 ? string.Empty : Utf8.GetString(raw);
        }

        public bool PopBoolean()
        {
            ThrowIfDisposed();

            if (RemainingLength < 1)
                return false;

            // ✅ FIX #6: El original comparaba byte con Convert.ToChar(1), mezcla
            //           de tipos incorrecta. Comparamos directamente con byte 1.
            return _body[_pointer++] == 1;
        }

        public int PopInt()
        {
            ThrowIfDisposed();

            if (RemainingLength < 4)
                return 0;

            // ✅ FIX #7: Leer directamente del buffer sin allocar byte[] intermedio.
            int value = (_body[_pointer] << 24)
                      | (_body[_pointer + 1] << 16)
                      | (_body[_pointer + 2] << 8)
                      | _body[_pointer + 3];
            _pointer += 4;
            return value;
        }

        public short PopShort()
        {
            ThrowIfDisposed();

            if (RemainingLength < 2)
                return 0;

            return ReadInt16Direct();
        }

        public long PopLong()
        {
            ThrowIfDisposed();

            if (RemainingLength < 8)
                return 0L;

            long value = ((long)_body[_pointer] << 56)
                       | ((long)_body[_pointer + 1] << 48)
                       | ((long)_body[_pointer + 2] << 40)
                       | ((long)_body[_pointer + 3] << 32)
                       | ((long)_body[_pointer + 4] << 24)
                       | ((long)_body[_pointer + 5] << 16)
                       | ((long)_body[_pointer + 6] << 8)
                       | (long)_body[_pointer + 7];
            _pointer += 8;
            return value;
        }

        // ── Decode helpers (públicos para compatibilidad) ──────────────────────

        /// <remarks>
        /// ✅ FIX #8: El check original <c>(v[0] | v[1] | v[2] | v[3]) &lt; 0</c>
        /// nunca puede ser verdadero porque bytes son 0–255 (siempre positivos).
        /// La guardia era un no-op. Eliminada; el método ahora decodifica siempre.
        /// </remarks>
        public static int DecodeInt32(byte[] v)
        {
            if (v == null || v.Length < 4)
                return 0;

            return (v[0] << 24) | (v[1] << 16) | (v[2] << 8) | v[3];
        }

        public static short DecodeInt16(byte[] v)
        {
            if (v == null || v.Length < 2)
                return 0;

            return (short)((v[0] << 8) | v[1]);
        }

        // ── Peek / Skip ────────────────────────────────────────────────────────

        /// <summary>Lee un int sin avanzar el puntero.</summary>
        public int PeekInt()
        {
            ThrowIfDisposed();

            if (RemainingLength < 4)
                return 0;

            return (_body[_pointer] << 24)
                 | (_body[_pointer + 1] << 16)
                 | (_body[_pointer + 2] << 8)
                 | _body[_pointer + 3];
        }

        /// <summary>Avanza el puntero <paramref name="count"/> bytes.</summary>
        public void Skip(int count)
        {
            ThrowIfDisposed();

            if (count < 0 || count > RemainingLength)
                throw new InvalidOperationException(
                    $"[Packet {Id}] Skip: cannot skip {count} bytes, only {RemainingLength} remain.");

            _pointer += count;
        }

        // ── Debug ──────────────────────────────────────────────────────────────

        public override string ToString()
        {
            if (_disposed == 1)
                return $"[{Id}] DISPOSED";

            // Mostrar solo los primeros 64 bytes para no saturar logs
            int preview = Math.Min(_bodyLength, 64);
            string hex = BitConverter.ToString(_body, 0, preview);
            string suffix = _bodyLength > 64 ? "..." : string.Empty;
            return $"[{Id}] Length: {_bodyLength} | Hex: {hex}{suffix}";
        }

        // ── IDisposable ────────────────────────────────────────────────────────

        public void Dispose()
        {
            // ✅ FIX #2 aplicado: solo ejecuta una vez aunque llamen múltiples threads.
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            ReturnBuffer();

            Id = 0;
            _pointer = 0;
            _bodyLength = 0;

            GC.SuppressFinalize(this);
        }

        ~ClientPacket()
        {
            Dispose();
        }

        // ── Privados ───────────────────────────────────────────────────────────

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private short ReadInt16Direct()
        {
            short value = (short)((_body[_pointer] << 8) | _body[_pointer + 1]);
            _pointer += 2;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _disposed) == 1)
                throw new ObjectDisposedException(nameof(ClientPacket));
        }

        private void ReturnBuffer()
        {
            if (_isPooled && _body != null)
            {
                ArrayPool<byte>.Shared.Return(_body);
                _isPooled = false;
            }
            _body = null;
        }
    }
}