#region

using System;

#endregion

namespace Polar
{
    public abstract class AbstractBar
    {
        /// <summary>
        /// Prints a simple message 
        /// </summary>
        /// <param name="msg">Message to print</param>
        public void PrintMessage(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("  {0}", msg);
            Console.Write("\r".PadLeft(Console.WindowWidth - Console.CursorLeft - 1));
        }

        public abstract void Step();
    }
}