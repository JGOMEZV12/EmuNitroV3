using Polar.Communication.Interfaces;
using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace Polar.Communication.Packets.Outgoing
{
    public class ServerPacket : IServerPacket, IDisposable
    {
        private readonly Encoding _encoding;
        private byte[] _buffer;
        private int _position;
        private int _disposed; // ✅ FIX #1: int + Interlocked en lugar de bool, igual que ClientPacket
        private readonly int _initialCapacity;

        public int Id { get; private set; }
        public int Length => _position;
        public int Capacity => _buffer?.Length ?? 0;

        private const int DefaultInitialCapacity = 64;
        private const int MaxPacketSize = 10_000_000; // 10MB

        public ServerPacket(int id) : this(id, DefaultInitialCapacity) { }

        public ServerPacket(int id, int initialCapacity)
        {
            if (initialCapacity <= 0)
                initialCapacity = DefaultInitialCapacity;

            _encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);
            _initialCapacity = initialCapacity;
            _buffer = ArrayPool<byte>.Shared.Rent(initialCapacity);
            _position = 0;
            _disposed = 0;
            Id = id;

            WriteShort(id);
        }

        // ── Core Write Methods ─────────────────────────────────────────────────

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureCapacity(int additionalBytes)
        {
            int required = _position + additionalBytes;

            if (required > MaxPacketSize)
                throw new InvalidOperationException(
                    $"Packet size exceeds maximum allowed ({MaxPacketSize} bytes)");

            if (required <= _buffer.Length)
                return;

            int newCapacity = Math.Max(_buffer.Length * 2, required);
            byte[] newBuffer = ArrayPool<byte>.Shared.Rent(newCapacity);
            Buffer.BlockCopy(_buffer, 0, newBuffer, 0, _position);
            ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = newBuffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteByte(byte value)
        {
            ThrowIfDisposed();
            EnsureCapacity(1);
            _buffer[_position++] = value;
        }

        public void WriteByte(int value) => WriteByte((byte)value);

        public void WriteBytes(byte[] bytes, bool isBigEndian = false)
        {
            ThrowIfDisposed();

            if (bytes == null || bytes.Length == 0)
                return;

            EnsureCapacity(bytes.Length);

            if (isBigEndian)
            {
                for (int i = bytes.Length - 1; i >= 0; i--)
                    _buffer[_position++] = bytes[i];
            }
            else
            {
                Buffer.BlockCopy(bytes, 0, _buffer, _position, bytes.Length);
                _position += bytes.Length;
            }
        }

        public void WriteBytes(ReadOnlySpan<byte> bytes, bool isBigEndian = false)
        {
            ThrowIfDisposed();

            if (bytes.Length == 0)
                return;

            EnsureCapacity(bytes.Length);

            if (isBigEndian)
            {
                for (int i = bytes.Length - 1; i >= 0; i--)
                    _buffer[_position++] = bytes[i];
            }
            else
            {
                bytes.CopyTo(new Span<byte>(_buffer, _position, bytes.Length));
                _position += bytes.Length;
            }
        }

        // ── Data Type Methods ──────────────────────────────────────────────────

        public void WriteString(string value)
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(value))
            {
                WriteShort(0);
                return;
            }

            int byteCount = _encoding.GetByteCount(value);
            WriteShort(byteCount);

            if (byteCount == 0)
                return;

            EnsureCapacity(byteCount);
            _position += _encoding.GetBytes(value, 0, value.Length, _buffer, _position);
        }

        public void WriteString(string value, int fixedLength)
        {
            ThrowIfDisposed();

            if (fixedLength <= 0)
                return;

            EnsureCapacity(fixedLength);

            if (string.IsNullOrEmpty(value))
            {
                Array.Clear(_buffer, _position, fixedLength);
                _position += fixedLength;
                return;
            }

            // ✅ FIX #2: Codificar directo al buffer sin allocar byte[] intermedio
            int encoded = _encoding.GetBytes(value, 0, value.Length, _buffer, _position);
            int bytesToWrite = Math.Min(encoded, fixedLength);

            // Si encodó más de fixedLength, truncar (ya está escrito en el buffer)
            int padding = fixedLength - bytesToWrite;
            if (padding > 0)
                Array.Clear(_buffer, _position + bytesToWrite, padding);

            _position += fixedLength;
        }

        public void WriteShort(int value) => WriteShort((short)value);

        public void WriteShort(short value)
        {
            ThrowIfDisposed();
            EnsureCapacity(2);
            _buffer[_position++] = (byte)(value >> 8);
            _buffer[_position++] = (byte)value;
        }

        public void WriteUShort(ushort value) => WriteShort((short)value);

        public void WriteInteger(int value)
        {
            ThrowIfDisposed();
            EnsureCapacity(4);
            _buffer[_position++] = (byte)(value >> 24);
            _buffer[_position++] = (byte)(value >> 16);
            _buffer[_position++] = (byte)(value >> 8);
            _buffer[_position++] = (byte)value;
        }

        public void WriteUInteger(uint value) => WriteInteger((int)value);

        public void WriteLong(long value)
        {
            ThrowIfDisposed();
            EnsureCapacity(8);
            _buffer[_position++] = (byte)(value >> 56);
            _buffer[_position++] = (byte)(value >> 48);
            _buffer[_position++] = (byte)(value >> 40);
            _buffer[_position++] = (byte)(value >> 32);
            _buffer[_position++] = (byte)(value >> 24);
            _buffer[_position++] = (byte)(value >> 16);
            _buffer[_position++] = (byte)(value >> 8);
            _buffer[_position++] = (byte)value;
        }

        public void WriteULong(ulong value) => WriteLong((long)value);

        public void WriteBoolean(bool value) => WriteByte(value ? (byte)1 : (byte)0);

        public void WriteDouble(double value, string format = "0.0")
        {
            ThrowIfDisposed();
            WriteString(value.ToString(format, System.Globalization.CultureInfo.InvariantCulture));
        }

        public void WriteFloat(float value, string format = "0.0") => WriteDouble(value, format);

        // ── Array / Collection Methods ─────────────────────────────────────────

        public void WriteIntArray(int[] array)
        {
            ThrowIfDisposed();

            if (array == null) { WriteInteger(0); return; }

            WriteInteger(array.Length);
            foreach (int v in array)
                WriteInteger(v);
        }

        public void WriteStringArray(string[] array)
        {
            ThrowIfDisposed();

            if (array == null) { WriteInteger(0); return; }

            WriteInteger(array.Length);
            foreach (string v in array)
                WriteString(v);
        }

        public void WriteByteArray(byte[] array)
        {
            ThrowIfDisposed();

            if (array == null) { WriteInteger(0); return; }

            WriteInteger(array.Length);
            WriteBytes(array, false);
        }

        // ── Packet Assembly ────────────────────────────────────────────────────

        public byte[] GetBytes()
        {
            ThrowIfDisposed();

            int totalLength = 4 + _position;

            if (totalLength > MaxPacketSize)
                throw new InvalidOperationException($"Packet too large: {totalLength} bytes");

            byte[] finalPacket = new byte[totalLength];

            // 4 bytes longitud (BIG-ENDIAN)
            finalPacket[0] = (byte)(_position >> 24);
            finalPacket[1] = (byte)(_position >> 16);
            finalPacket[2] = (byte)(_position >> 8);
            finalPacket[3] = (byte)_position;

            if (_position > 0)
                Buffer.BlockCopy(_buffer, 0, finalPacket, 4, _position);

            return finalPacket;
        }

        public byte[] GetBytesWithoutLength()
        {
            ThrowIfDisposed();

            byte[] packet = new byte[_position];
            Buffer.BlockCopy(_buffer, 0, packet, 0, _position);
            return packet;
        }

        public ReadOnlySpan<byte> GetBody()
        {
            ThrowIfDisposed();
            return new ReadOnlySpan<byte>(_buffer, 0, _position);
        }

        // ── Utility Methods ────────────────────────────────────────────────────

        public void Clear()
        {
            ThrowIfDisposed();
            _position = 0;
            Id = 0;
            WriteShort(Id);
        }

        public void Reset(int newId)
        {
            ThrowIfDisposed();
            _position = 0;
            Id = newId;
            WriteShort(newId);
        }

        public void TrimExcess()
        {
            ThrowIfDisposed();

            if (_buffer.Length > _position * 2 && _buffer.Length > _initialCapacity)
            {
                byte[] newBuffer = ArrayPool<byte>.Shared.Rent(_position);
                Buffer.BlockCopy(_buffer, 0, newBuffer, 0, _position);
                ArrayPool<byte>.Shared.Return(_buffer);
                _buffer = newBuffer;
            }
        }

        public string ToDebugString()
        {
            if (_disposed == 1) return "[DISPOSED]";

            int preview = Math.Min(_position, 32);
            string hex = BitConverter.ToString(_buffer, 0, preview);
            return $"[{Id}] Length: {_position} bytes | Hex: {hex}{(_position > 32 ? "..." : "")}";
        }

        public string GetBodyAsString()
        {
            ThrowIfDisposed();
            return _position == 0 ? string.Empty : _encoding.GetString(_buffer, 0, _position);
        }

        // ── IDisposable ────────────────────────────────────────────────────────

        // ✅ FIX #3: Finalizer llamando Dispose puede corromper el ArrayPool si el
        //           GC lo llama en un thread distinto mientras otro usa el buffer.
        //           Con Interlocked.Exchange garantizamos ejecución única y segura.
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            if (_buffer != null)
            {
                ArrayPool<byte>.Shared.Return(_buffer);
                _buffer = null;
            }

            _position = 0;
            Id = 0;
            GC.SuppressFinalize(this);
        }

        ~ServerPacket() => Dispose();

        // ── Static Helpers ─────────────────────────────────────────────────────

        public static byte[] CreateQuickPacket(int id, Action<ServerPacket> writer)
        {
            using var packet = new ServerPacket(id);
            writer?.Invoke(packet);
            return packet.GetBytes();
        }

        public static byte[] CreateQuickStringPacket(int id, string message)
        {
            using var packet = new ServerPacket(id);
            packet.WriteString(message);
            return packet.GetBytes();
        }

        public static byte[] EncodeShort(short value) =>
            new byte[] { (byte)(value >> 8), (byte)value };

        public static byte[] EncodeInt(int value) =>
            new byte[] { (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value };

        public static byte[] EncodeLong(long value) =>
            new byte[]
            {
                (byte)(value >> 56), (byte)(value >> 48), (byte)(value >> 40), (byte)(value >> 32),
                (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8),  (byte)value
            };

        // ── Private Helpers ────────────────────────────────────────────────────

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _disposed) == 1)
                throw new ObjectDisposedException(nameof(ServerPacket));
        }
    }
}