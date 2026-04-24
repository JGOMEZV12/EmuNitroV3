using System;
using System.Buffers;

namespace Polar.Communication.WebSocket
{
    internal static class EncodeDecode
    {
        // Opcodes WebSocket RFC 6455
        private const byte OPCODE_TEXT = 0x01;
        private const byte OPCODE_BINARY = 0x02;
        private const byte OPCODE_CLOSE = 0x08;
        private const byte OPCODE_PING = 0x09;
        private const byte OPCODE_PONG = 0x0A;

        private const byte FIN_BIT = 0x80;
        private const byte MASK_BIT = 0x80;

        /// <summary>
        /// Codifica un mensaje como frame WebSocket binario (RFC 6455).
        /// </summary>
        internal static byte[] EncodeMessage(byte[] message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            int length = message.Length;

            // ✅ FIX #1: Calcular el tamaño exacto del header de una vez,
            //           sin allocar un array de 10 bytes siempre.
            int headerSize;
            if (length <= 125)
                headerSize = 2;
            else if (length <= 65535)
                headerSize = 4;
            else
                headerSize = 10;

            byte[] response = new byte[headerSize + length];

            // FIN=1 + opcode binario (0x82 = FIN | BINARY)
            response[0] = FIN_BIT | OPCODE_BINARY;

            if (length <= 125)
            {
                response[1] = (byte)length;
            }
            else if (length <= 65535)
            {
                response[1] = 126;
                response[2] = (byte)(length >> 8);
                response[3] = (byte)length;
            }
            else
            {
                // ✅ FIX #2: Escribir los 8 bytes de longitud correctamente (big-endian).
                //           El original solo escribía 4 bytes en posiciones 6-9,
                //           dejando los primeros 4 en cero pero sin expresarlo bien.
                response[1] = 127;
                response[2] = 0; // Los 4 bytes altos siempre 0 (length es int, max 2GB)
                response[3] = 0;
                response[4] = 0;
                response[5] = 0;
                response[6] = (byte)(length >> 24);
                response[7] = (byte)(length >> 16);
                response[8] = (byte)(length >> 8);
                response[9] = (byte)length;
            }

            // ✅ FIX #3: Buffer.BlockCopy en lugar de dos loops manuales.
            Buffer.BlockCopy(message, 0, response, headerSize, length);

            return response;
        }

        /// <summary>
        /// Decodifica un frame WebSocket enmascarado o sin máscara (RFC 6455).
        /// Devuelve null si el frame es inválido, incompleto, o es un frame de control
        /// que no contiene datos de aplicación (PING, PONG, CLOSE).
        /// </summary>
        internal static byte[] DecodeMessage(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 2)
                return null;

            try
            {
                byte firstByte = bytes[0];
                byte secondByte = bytes[1];

                // ✅ FIX #4: Validar FIN bit y opcode.
                //           El original ignoraba completamente el primer byte.
                bool fin = (firstByte & FIN_BIT) != 0;
                byte opcode = (byte)(firstByte & 0x0F);

                // Fragmentación no soportada (FIN=0 implica fragmento)
                if (!fin)
                    return null;

                // Ignorar frames de control sin datos de aplicación
                if (opcode == OPCODE_CLOSE || opcode == OPCODE_PING || opcode == OPCODE_PONG)
                    return null;

                // Solo aceptar text o binary (o continuation frame 0x00)
                if (opcode != OPCODE_TEXT && opcode != OPCODE_BINARY && opcode != 0x00)
                    return null;

                bool masked = (secondByte & MASK_BIT) != 0;
                int dataLength = secondByte & 0x7F;

                int headerEnd; // índice donde termina el header (antes de máscara y datos)

                if (dataLength == 126)
                {
                    if (bytes.Length < 4) return null;

                    // ✅ FIX #5: Usar ushort para la lectura de 2 bytes, sin cast peligroso.
                    dataLength = (bytes[2] << 8) | bytes[3];
                    headerEnd = 4;
                }
                else if (dataLength == 127)
                {
                    if (bytes.Length < 10) return null;

                    // ✅ FIX #6: El original truncaba silenciosamente la longitud de 64-bit
                    //           a los últimos 4 bytes sin advertir. Ahora leemos correctamente
                    //           los 8 bytes y verificamos que cabe en un int (límite práctico).
                    long longLength =
                        ((long)bytes[2] << 56) | ((long)bytes[3] << 48) |
                        ((long)bytes[4] << 40) | ((long)bytes[5] << 32) |
                        ((long)bytes[6] << 24) | ((long)bytes[7] << 16) |
                        ((long)bytes[8] << 8) | (long)bytes[9];

                    if (longLength > int.MaxValue)
                    {
                        Console.WriteLine($"[EncodeDecode] Frame demasiado grande: {longLength} bytes");
                        return null;
                    }

                    dataLength = (int)longLength;
                    headerEnd = 10;
                }
                else
                {
                    headerEnd = 2;
                }

                // Verificar longitud total del frame
                int expectedLength = headerEnd + (masked ? 4 : 0) + dataLength;
                if (bytes.Length < expectedLength)
                    return null;

                byte[] decoded = new byte[dataLength];

                if (masked)
                {
                    // ✅ FIX #7: LINQ en el loop original creaba un enumerador y llamaba
                    //           ElementAt() por CADA BYTE — O(n²) en la práctica.
                    //           Ahora copiamos la máscara a un array de 4 bytes y hacemos
                    //           XOR directo sobre el buffer — O(n).
                    int maskStart = headerEnd;
                    int dataStart = maskStart + 4;

                    byte m0 = bytes[maskStart];
                    byte m1 = bytes[maskStart + 1];
                    byte m2 = bytes[maskStart + 2];
                    byte m3 = bytes[maskStart + 3];

                    for (int i = 0; i < dataLength; i++)
                    {
                        // Seleccionar byte de máscara según posición mod 4
                        byte mask = (i & 3) switch
                        {
                            0 => m0,
                            1 => m1,
                            2 => m2,
                            _ => m3
                        };
                        decoded[i] = (byte)(bytes[dataStart + i] ^ mask);
                    }
                }
                else
                {
                    Buffer.BlockCopy(bytes, headerEnd, decoded, 0, dataLength);
                }

                return decoded;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EncodeDecode] ERROR decodificando frame: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Construye un frame de control PONG en respuesta a un PING.
        /// </summary>
        internal static byte[] EncodePong(byte[] pingPayload)
        {
            // PONG debe devolver el mismo payload que el PING (RFC 6455 §5.5.3)
            int length = pingPayload?.Length ?? 0;
            byte[] frame = new byte[2 + length];
            frame[0] = FIN_BIT | OPCODE_PONG;
            frame[1] = (byte)length;

            if (length > 0)
                Buffer.BlockCopy(pingPayload, 0, frame, 2, length);

            return frame;
        }

        /// <summary>
        /// Construye un frame CLOSE (opcode 0x08) con código de estado opcional.
        /// </summary>
        internal static byte[] EncodeClose(ushort statusCode = 1000)
        {
            byte[] frame = new byte[4];
            frame[0] = FIN_BIT | OPCODE_CLOSE;
            frame[1] = 2; // payload: 2 bytes de status code
            frame[2] = (byte)(statusCode >> 8);
            frame[3] = (byte)statusCode;
            return frame;
        }
    }
}