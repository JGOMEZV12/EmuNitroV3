using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace ConsoleWriter
{
    public class Writer
    {
        private static bool mDisabled;
        private static readonly object fileLock = new object(); // Objeto para el bloqueo

        public static bool DisabledState
        {
            get { return mDisabled; }
            set { mDisabled = value; }
        }

        public static void WriteLine(string Line, ConsoleColor Colour = ConsoleColor.Gray)
        {
            if (!mDisabled)
            {
                Console.ForegroundColor = Colour;
                Console.WriteLine("HORA: [" + DateTime.Now.ToString("HH:mm:ss") + "] " + Line);
            }
        }

        public static void WriteProductData(string ProductData)
        {
            WriteToFile("productdata.txt", ProductData);
        }

        public static void LogException(string logText)
        {
            WriteToFile("Logs/exceptions.txt", logText + "\r\n\r\n");
        }

        public static void LogCriticalException(string logText)
        {
            WriteToFile("Logs/criticalexceptions.txt", logText + "\r\n\r\n");
        }

        public static void LogMySQLError(string logText)
        {
            WriteToFile("Logs/mysql_error.txt", logText + "\r\n\r\n");
        }

        public static void LogRPTimersError(string logText)
        {
            WriteToFile("Logs/rp_timers_errors.txt", logText + "\r\n\r\n");
        }

        public static void LogRPGamesError(string logText)
        {
            WriteToFile("Logs/rp_games_errors.txt", logText + "\r\n\r\n");
        }

        public static void LogRPBotError(string logText)
        {
            WriteToFile("Logs/rp_bots_errors.txt", logText + "\r\n\r\n");
        }

        public static void LogWebSocketError(string logText)
        {
            WriteToFile("Logs/rp_websocket_errors.txt", logText + "\r\n\r\n");
        }

        public static void LogWiredException(string logText)
        {
            WriteToFile("Logs/wiredexceptions.txt", logText + "\r\n\r\n");
        }

        public static void LogCacheException(string logText)
        {
            WriteToFile("Logs/cacheexceptions.txt", logText + "\r\n\r\n");
        }

        public static void LogPathfinderException(string logText)
        {
            WriteToFile("Logs/pathfinderexceptions.txt", logText + "\r\n\r\n");
        }

        public static void LogThreadException(string Exception, string Threadname)
        {
            WriteToFile("Logs/threaderror.txt", "Error en el hilo " + Threadname + ": \r\n" + Exception + "\r\n\r\n");
        }

        public static void LogQueryError(Exception Exception, string query)
        {
            WriteToFile("Logs/MySQLerrors.txt", "Error en el query: \r\n" + query + "\r\n" + Exception + "\r\n\r\n");
        }

        public static void LogPacketException(string Packet, string Exception)
        {
            WriteToFile("Logs/packeterror.txt", "Error en el paquete " + Packet + ": \r\n" + Exception + "\r\n\r\n");
        }

        public static void HandleException(Exception pException, string pLocation)
        {
            var ExceptionData = new StringBuilder();
            ExceptionData.AppendLine("Excepción registrada " + DateTime.Now.ToString() + " en " + pLocation + ":");
            ExceptionData.AppendLine(pException.ToString());
            if (pException.InnerException != null)
            {
                ExceptionData.AppendLine("Inner exception:");
                ExceptionData.AppendLine(pException.InnerException.ToString());
            }
            if (pException.HelpLink != null)
            {
                ExceptionData.AppendLine("Help link:");
                ExceptionData.AppendLine(pException.HelpLink);
            }
            if (pException.Source != null)
            {
                ExceptionData.AppendLine("Source:");
                ExceptionData.AppendLine(pException.Source);
            }
            if (pException.Data != null)
            {
                ExceptionData.AppendLine("Data:");
                foreach (DictionaryEntry Entry in pException.Data)
                {
                    ExceptionData.AppendLine("  Key: " + Entry.Key + "Value: " + Entry.Value);
                }
            }
            if (pException.Message != null)
            {
                ExceptionData.AppendLine("Message:");
                ExceptionData.AppendLine(pException.Message);
            }
            if (pException.StackTrace != null)
            {
                ExceptionData.AppendLine("Stack trace:");
                ExceptionData.AppendLine(pException.StackTrace);
            }
            ExceptionData.AppendLine();
            ExceptionData.AppendLine();
            LogException(ExceptionData.ToString());
        }

        public static void DisablePrimaryWriting(bool ClearConsole)
        {
            mDisabled = true;
            if (ClearConsole)
                Console.Clear();
        }

        public static void WriteToFile(string path, string content)
        {
            lock (fileLock) // Uso de bloqueo para acceso exclusivo
            {
                try
                {
                    string fullPath = Path.Combine(Polar.PolarEnvironment.PatchDir, path);
                    using (FileStream Writer = new FileStream(fullPath, FileMode.Append, FileAccess.Write, FileShare.None))
                    {
                        byte[] Msg = Encoding.ASCII.GetBytes(Environment.NewLine + content);
                        Writer.Write(Msg, 0, Msg.Length);
                    }
                }
                catch (IOException e)
                {
                    WriteLine("No se pudo escribir en el archivo: " + e.Message + " en " + path);
                }
                catch (Exception e)
                {
                    WriteLine("Error desconocido al escribir en el archivo: " + e.Message + " en " + path);
                }
            }
        }

        private static void WriteCallback(IAsyncResult callback)
        {
            var stream = (FileStream)callback.AsyncState;
            stream.EndWrite(callback);
            stream.Dispose();
        }
    }
}
