#region

using System;

#endregion

namespace Polar
{
    internal class Out
    {
        /// <summary>
        /// Writes the line.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="header">The header.</param>
        /// <param name="color">The color.</param>
        public static void WriteLine(string Line = "", string header = "", ConsoleColor color = ConsoleColor.Black)
        {
            Console.ForegroundColor = color;
            if (color == ConsoleColor.Red)
            {
                Console.Write(" [ERROR]");
            }

            Console.Write(" HORA: [" + DateTime.Now.ToString("HH:mm:ss") + "] » " + Line);
            if (header != "")
            {
                Console.Write("[");
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write(header);
                Console.ForegroundColor = color;
                Console.Write("] ");
            }

            //Console.Write(">> ");
            Console.ForegroundColor = color;
            Console.WriteLine("");
            Console.ForegroundColor = color;
        }
    }
}