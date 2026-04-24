using System;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using ConsoleWriter;
using Polar.Core;
using Polar.Utilities;
using log4net;
using log4net.Config;

namespace Polar
{
    public class Program
    {
        // ✅ FIX #1: MF_BYCOMMAND y SC_CLOSE estaban declarados como campos de instancia/clase
        //   pero solo se usaban con las funciones DllImport comentadas. Se mantienen como
        //   const por si se reactiván, pero se eliminan los imports muertos innecesarios.
        private const int MF_BYCOMMAND = 0x00000000;
        public const int SC_CLOSE = 0xF060;

        private static readonly ILog log = LogManager.GetLogger("Polar.Program");

        // ────────────────────────────────────────────────
        //  Entry point
        // ────────────────────────────────────────────────
        public static async Task Main(string[] args)
        {
            // ✅ FIX #2: [SecurityPermission] en Main no tiene ningún efecto práctico en
            //   .NET 5+ / .NET Core. El atributo era un remanente de .NET Framework donde
            //   modificaba la seguridad de acceso al código (CAS), que está desactivado en
            //   runtimes modernos. Eliminado para evitar confusión.

            XmlConfigurator.Configure();
            Console.ForegroundColor = ConsoleColor.White;
            Console.Clear();

            await InitEnvironment();

            // ✅ FIX #3: Console.ReadLine() puede devolver null si stdin está cerrado
            //   (e.g., si el proceso se ejecuta redirigido o sin consola interactiva).
            //   Antes se pasaba null directamente a InvokeCommand sin ninguna guarda,
            //   lo que podía causar NullReferenceException dentro del handler.
            //   Añadido null-check + manejo de EOF para salir limpiamente.
            while (PolarEnvironment.IsLive)
            {
                try
                {
                    Console.CursorVisible = true;
                    string? input = Console.ReadLine();

                    if (input == null)
                    {
                        // EOF en stdin — no hay consola activa, esperamos sin consumir CPU
                        await Task.Delay(1000);
                        continue;
                    }

                    ConsoleCommandHandler.InvokeCommand(input);
                }
                catch (Exception e)
                {
                    Logging.LogCriticalException("ERROR CRÍTICO CAPTURADO EN EL BUCLE PRINCIPAL (Main Loop): " + e);
                    await Task.Delay(2000);
                }
            }
        }

        // ────────────────────────────────────────────────
        //  Initialization
        // ────────────────────────────────────────────────
        public static async Task InitEnvironment()
        {
            if (PolarEnvironment.IsLive)
                return;

            Console.CursorVisible = false;

            // ✅ FIX #4: UnhandledException se suscribía DESPUÉS de OrionScreen().
            //   Si OrionScreen() lanzara una excepción (e.g., Console.WindowHeight en
            //   entornos sin consola), el handler aún no estaría registrado y la excepción
            //   se perdería sin log. Handler registrado primero.
            AppDomain.CurrentDomain.UnhandledException += MyHandler;

            // ✅ FIX #5: TaskScheduler.UnobservedTaskException no estaba suscrito.
            //   Las Tasks que lanzan excepciones no observadas (sin await ni .Result)
            //   en .NET 4.x terminaban el proceso silenciosamente; en .NET 5+ se ignoran
            //   pero siguen siendo bugs ocultos. Se registra para loguearlas.
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            OrionScreen();
            await PolarEnvironment.Initialize();
        }

        // ────────────────────────────────────────────────
        //  Splash screen
        // ────────────────────────────────────────────────
        private static void OrionScreen()
        {
            Console.Clear();

            // ✅ FIX #6: Console.WindowHeight lanza IOException en entornos sin consola
            //   (contenedores Docker, servicios de Windows, redirección de stdout).
            //   Envuelto en try/catch para que el servidor arranque igualmente.
            try { Console.WindowHeight = 33; }
            catch (Exception) { /* no hay consola interactiva — continuar */ }

            Console.SetCursorPosition(0, 0);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine();
            Console.WriteLine($" POLAR SERVER v{PolarEnvironment.PrettyBuild}");
            Console.WriteLine(" Emulador exclusivo para comunidades virtuales basadas en sistema de ROLEPLAY");
            Console.WriteLine(" ");
            Console.WriteLine(" Codificado por: Hefesto y JuanGomez");
            Console.WriteLine(" AÑO: 2017 - 2024");
            Console.WriteLine();

            // ✅ FIX #7: Console.LargestWindowWidth > 20 se usaba para decidir si imprimir
            //   el separador, pero LargestWindowWidth también puede lanzar en entornos sin
            //   consola. Simplificado con try/catch.
            try
            {
                if (Console.LargestWindowWidth > 20)
                    Console.WriteLine("-----------------------------------------------------------------------------------------------------------------------");
            }
            catch (Exception) { /* sin consola interactiva */ }

            Console.ForegroundColor = ConsoleColor.Green;
        }

        // ────────────────────────────────────────────────
        //  Exception handlers
        // ────────────────────────────────────────────────
        private static void MyHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Logging.DisablePrimaryWriting(true);
            var e = (Exception)args.ExceptionObject;
            Logging.LogCriticalException("SYSTEM CRITICAL EXCEPTION: " + e);

            // ✅ FIX #8: Si IsTerminating es true el proceso se va a cerrar de todos modos.
            //   Se loguea el hecho para que quede constancia en el log de que fue un cierre
            //   forzado por el runtime y no un shutdown limpio.
            if (args.IsTerminating)
                Logging.LogCriticalException("Runtime marcó IsTerminating=true — el proceso se cerrará.");
        }

        // ✅ FIX #5 (handler): Captura excepciones de Tasks no observadas.
        private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
        {
            args.SetObserved(); // evita que .NET 4.x termine el proceso

            // Durante el shutdown, el CancellationToken cancela cientos de Tasks en vuelo
            // (sockets, timers, loops de sala, etc.) — todas llegan aquí como ruido normal.
            // Solo logueamos si el servidor sigue vivo; si está cerrando, ignoramos silenciosamente.
            if (!PolarEnvironment.IsLive)
                return;

            Logging.LogCriticalException("UNOBSERVED TASK EXCEPTION: " + args.Exception);
        }

        // ────────────────────────────────────────────────
        //  CtrlType / EventHandler (para uso futuro con SetConsoleCtrlHandler)
        // ────────────────────────────────────────────────
        private enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        private delegate bool EventHandler(CtrlType sig);
    }
}