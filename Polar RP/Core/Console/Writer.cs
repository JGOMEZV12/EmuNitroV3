#region

using System;
using System.Collections;
using System.IO;
using System.Text;

#endregion

namespace Polar.WriterxD
{
    public class WriterxD
    {
        private static bool _mDisabled;

        public static bool DisabledState
        {
            get { return _mDisabled; }
            set { _mDisabled = value; }
        }

        public static void WriteLine(string line, ConsoleColor colour = ConsoleColor.Yellow)
        {
            if (_mDisabled)
                return;

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("[" + DateTime.Now + "] ");
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.Write("Orion Errors Manager");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("] ");

            Console.Write(">> ");
            Console.ForegroundColor = colour;
            Console.WriteLine(line);
            Console.ForegroundColor = ConsoleColor.DarkGray;
        }

        public static void LogException(string logText)
        {
            WriteToFile("Logs\\ExceptionsLog.txt", logText + "\r\n\r\n");
            WriteLine("Una excepción fue registrada.", ConsoleColor.Red);
        }

        public static void LogCriticalException(string logText)
        {
            WriteToFile("Logs\\ExceptionsLog.txt", logText + "\r\n\r\n");
            WriteLine("Se registró una excepción crítica.", ConsoleColor.Red);
        }

        public static void LogCacheError(string logText)
        {
            WriteToFile("Logs\\ErrorLog.txt", logText + "\r\n\r\n");
            WriteLine("Se registró un error de almacenamiento en caché.", ConsoleColor.Red);
        }

        public static void LogMessage(string logText, bool output = true)
        {
            WriteToFile("Logs\\CommonLog.txt", logText + "\r\n\r\n");

            if(output)
                Console.WriteLine(logText);
        }

        public static void LogDDOS(string logText)
        {
            WriteToFile("Logs\\DDosLog.txt", logText + "\r\n\r\n");
            WriteLine(logText, ConsoleColor.Red);
        }

        public static void LogThreadException(string exception, string threadName)
        {
            WriteToFile("Logs\\ErrorLog.txt", string.Concat("Error en thread ", threadName, ": \r\n", exception, "\r\n\r\n"));
            WriteLine("Se registró un error de subproceso, en el hilo: " + threadName, ConsoleColor.Red);
        }

        public static void LogQueryError(Exception exception, string query)
        {
            WriteToFile("Logs\\MySQLErrors.txt", string.Concat("The query error was in: \r\n", query, "\r\n", exception, "\r\n\r\n"));
            WriteLine("Se registró una excepción MySQL.", ConsoleColor.Red);
        }

        public static void LogPacketException(string packet, string exception)
        {
            WriteToFile("Logs\\PacketLogError.txt", "Error in packet " + packet + ": \r\n" + exception + "\r\n\r\n");
            WriteLine("Se registró una excepción de paquete.", ConsoleColor.Red);
        }

        public static void HandleException(Exception pException, string pLocation)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(string.Concat("Excepción registrada ", DateTime.Now.ToString(), " en ", pLocation, ":"));
            stringBuilder.AppendLine(pException.ToString());
            if (pException.InnerException != null)
            {
                stringBuilder.AppendLine("Inner exception:");
                stringBuilder.AppendLine(pException.InnerException.ToString());
            }
            if (pException.HelpLink != null)
            {
                stringBuilder.AppendLine("Help link:");
                stringBuilder.AppendLine(pException.HelpLink);
            }
            if (pException.Source != null)
            {
                stringBuilder.AppendLine("Source:");
                stringBuilder.AppendLine(pException.Source);
            }
            stringBuilder.AppendLine("Data:");
            foreach (DictionaryEntry dictionaryEntry in pException.Data)
                stringBuilder.AppendLine(string.Concat("  Key: ", dictionaryEntry.Key, "Value: ", dictionaryEntry.Value));
            stringBuilder.AppendLine("Message:");
            stringBuilder.AppendLine(pException.Message);
            if (pException.StackTrace != null)
            {
                stringBuilder.AppendLine("Stack trace:");
                stringBuilder.AppendLine(pException.StackTrace);
            }
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            LogException(stringBuilder.ToString());
        }

        public static void DisablePrimaryWriting(bool clearConsole)
        {
            _mDisabled = true;
            /*
            if (clearConsole)
                Console.Clear();
            */
        }

        public static void LogShutdown(StringBuilder builder)
        {
            WriteToFile("Logs\\shutdownlog.txt", builder.ToString());
        }

        private static void WriteToFile(string path, string content)
        {
            try
            {
                File.AppendAllText(path, Environment.NewLine + content, Encoding.ASCII);
            }
            catch
            {
            }
        }
    }
}