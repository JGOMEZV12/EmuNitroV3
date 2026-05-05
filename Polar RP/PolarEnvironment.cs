using System.Diagnostics;
using System.Globalization;
using System.Collections.Concurrent;
using System.Text;
using System.Reflection;
using Polar.Core;
using Polar.HabboHotel;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Users;
using Polar.HabboHotel.Users.UserDataManagement;
using Polar.Net;
using Polar.Utilities;
using log4net;
using Polar.Communication.Encryption.Keys;
using Polar.Communication.Encryption;
using System.Threading.Tasks;
using Polar.Database.Interfaces;
using Polar.Database;
using Polar.HabboRoleplay.Bots.Manager;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Farming;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using System.Net;
using System.Text.RegularExpressions;
using Polar.HabboRoleplay.Web.Util.ChatRoom;
using ConnectionManager;
using Newtonsoft.Json;
using System.Web;

namespace Polar
{
    public static class PolarEnvironment
    {
        private static readonly ILog log = LogManager.GetLogger("Polar.PolarEnvironment");

        public const string PrettyVersion = "Polar Server RP";
        public const string PrettyBuild = "2.1.2";
        public static bool IsLive;
        public static ConfigurationData _configuration;
        private static Encoding _defaultEncoding;
        private static ConnectionHandling _connectionManager;
        private static Game _game;
        private static DatabaseManager _manager;
        public static ConfigData ConfigData;
        public static MusSocket MusSystem;
        public static CultureInfo CultureInfo;

        public static bool Event = false;
        public static DateTime lastEvent;
        public static DateTime ServerStarted;

        private static readonly List<char> Allowedchars = new List<char>(new[]
            {
                'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l',
                'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x',
                'y', 'z', '1', '2', '3', '4', '5', '6', '7', '8', '9', '0', '-', '.'
            });

        private static ConcurrentDictionary<int, Habbo> _usersCached = new ConcurrentDictionary<int, Habbo>();

        public static string SWFRevision = "";



        private static HttpClient client2;
        public static HttpClient Client2
        {
            get
            {
                if (client2 == null)
                    client2 = new HttpClient();

                return client2;
            }
        }
        // En PolarEnvironment.cs
       /* public static void SendMs(string message = "")
        {
            try
            {
                var WebHookId = ExtraSettings.WEBHOOK_ID_COMBAT;
                var WebHookToken = ExtraSettings.WEBHOOK_TOKEN_COMBAT;
                string EndPoint = string.Format("https://discordapp.com/api/webhooks/{0}/{1}", WebHookId, WebHookToken);

                var SuccessWebHook = new
                {
                    username = "⭐ Bot | HetosRP ⭐",
                    content = message,
                    avatar_url = "https://cdn.shopify.com/s/files/1/0185/5092/products/persons-0041_large.png?v=1369543932"
                };

                var content = new StringContent(JsonConvert.SerializeObject(SuccessWebHook), Encoding.UTF8, "application/json");

                // Usar Task.Run para evitar bloqueos y establecer timeout
                Task.Run(async () =>
                {
                    using (var client = new HttpClient())
                    {
                        client.Timeout = TimeSpan.FromSeconds(3);
                        await client.PostAsync(EndPoint, content);
                    }
                }).Wait(TimeSpan.FromSeconds(3)); // Timeout máximo de 3 segundos
            }
            catch (AggregateException ex)
            {
                // Manejar errores de timeout
                if (ex.InnerException is TaskCanceledException)
                    Console.WriteLine("[SendMs] Timeout al enviar a Discord");
                else
                    Console.WriteLine($"[SendMs] Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SendMs] Error: {ex.Message}");
            }
        }*/
        public static void SendMs2(string message = "", string imagex = "", string Desc = "", string oFooter = "", string Figure = "", bool isEmbeds = false)
        {
            var WebHookId = ExtraSettings.WEBHOOK_ID;
            var WebHookToken = ExtraSettings.WEBHOOK_TOKEN;
            string EndPoint = string.Format("https://discordapp.com/api/webhooks/{0}/{1}", WebHookId, WebHookToken);
            var content = new StringContent(JsonConvert.SerializeObject(string.Empty), Encoding.UTF8, "application/json");
            const string colorGreen = "80E61F";
            const string colorPurple = "C61FE6";

            if (isEmbeds == true)
            {
                var SuccessWebHook = new
                {
                    username = PolarEnvironment.GetConfig().data["Webhook_Username"],
                    content = message,
                    avatar_url = RoleplayManager.AVATARIMG + Figure,
                    embeds = new List<object>
                {
                    new
                    {
                        title = "",
                        url="",
                        description=Desc,
                        image = new
                        {
                            url = imagex
                        },
                        footer = new
                        {
                            text = oFooter,
                            iconurl = "",
                        },
                        color= int.Parse(colorGreen, System.Globalization.NumberStyles.HexNumber)
                    }
                }
                };

                content = new StringContent(JsonConvert.SerializeObject(SuccessWebHook), Encoding.UTF8, "application/json");
            }
            else
            {
                var SuccessWebHook = new
                {
                    username = "Hetos RP",
                    content = message,
                    avatar_url = "https://cdn.shopify.com/s/files/1/0185/5092/products/persons-0041_large.png?v=1369543932"
                };

                content = new StringContent(JsonConvert.SerializeObject(SuccessWebHook), Encoding.UTF8, "application/json");
            }
            //Console.WriteLine(JsonConvert.SerializeObject(SuccessWebHook));

            Client2.PostAsync(EndPoint, content).Wait();
        }

        public static string PatchDir;
        public static async Task Initialize()
        {
            PatchDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "/";
            #region Precheck
            ServerStarted = DateTime.Now;
            _defaultEncoding = Encoding.Default;
            Console.Title = "Polar Server RP | Cargando...";
            #endregion Precheck

            #region Database Connection
            CultureInfo = CultureInfo.CreateSpecificCulture("en-GB");
            try
            {
                _configuration = new ConfigurationData(PatchDir + "Settings/config.ini");

                _manager = new DatabaseManager(
                    uint.Parse(GetConfig().data["db.pool.maxsize"]),
                    uint.Parse(GetConfig().data["db.pool.minsize"]),
                    GetConfig().data["db.hostname"],
                    uint.Parse(GetConfig().data["db.port"]),
                    GetConfig().data["db.username"],
                    GetConfig().data["db.password"],
                    GetConfig().data["db.name"]);

                int num = 0;
                while (!_manager.IsConnected())
                {
                    ++num;
                    await Task.Delay(5000);
                    if (num > 10)
                    {
                        Logging.WriteLine("Error al conectar con el Mysql Server.");
                        Console.ReadKey(true);
                        Environment.Exit(1);
                        return;
                    }
                }


                //log.Info("Conectado con la base de datos");
                Out.WriteLine("¡CONECTADO A LA BASE DE DATOS CORRECTAMENTE!", "Polar.Boot", ConsoleColor.Green);
				 DatabaseCompatibility.Initialize();
                 Polar.HabboHotel.Items.Wired.WiredMigration.Run();
                #endregion Database Connection

                //Reset our statistics first.
                int randomInt = ((System.Diagnostics.Debugger.IsAttached) ? 0 : 1);
                using (IQueryAdapter dbClient = GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.RunQuery("TRUNCATE `catalog_marketplace_data`");
                    dbClient.RunQuery("UPDATE `rooms` SET `users_now` = '0' WHERE `users_now` > '0';");
                    dbClient.RunQuery("UPDATE `users` SET `online` = '0' WHERE `online` = '1'");
                    dbClient.RunQuery("UPDATE `server_status` SET `users_online` = '0', `loaded_rooms` = '0', `environment_status` = '" + randomInt + "'");

                    // Limpiamos registros de vehículos de Empresas
                    dbClient.RunQuery("DELETE FROM `items` WHERE id IN (SELECT `furni_id` FROM `rp_vehicles_owned` WHERE `rp_vehicles_owned`.`owner` = '0')");
                    dbClient.RunQuery("DELETE FROM `rp_vehicles_owned` WHERE `owner` = '0'");
                    dbClient.RunQuery("DELETE FROM `items` WHERE `user_id` = '0' AND `room_id` = '0'");
                }
                AbstractBar bar = new AnimatedBar();
                const int wait = 15, end = 5;
                //Get the configuration & Game set.
                ConfigData = new ConfigData();
                _game = new Game();
                await _game.InitializeAsync();
                _game.StartGameLoop();

                //Have our encryption ready.
                Progress(bar, wait, end, "Cargando Habbo Encryption...");
                HabboEncryptionV2.Initialize(new RSAKeys());

                //Make sure MUS is working.
                Progress(bar, wait, end, "Cargando Mus System...");
                MusSystem = new MusSocket(GetConfig().data["mus.tcp.bindip"], int.Parse(GetConfig().data["mus.tcp.port"]), GetConfig().data["mus.tcp.allowedaddr"].Split(Convert.ToChar(";")), 0);

                //Accept connections.
                Progress(bar, wait, end, "Cargando Administrador de conexiones...");
                _connectionManager = new ConnectionHandling(int.Parse(GetConfig().data["game.tcp.port"]), int.Parse(GetConfig().data["game.tcp.conlimit"]), int.Parse(GetConfig().data["game.tcp.conperip"])/*, GetConfig().data["game.tcp.enablenagles"].ToLower() == "true"*/);
                //_connectionManager.init();



                TimeSpan TimeUsed = DateTime.Now - ServerStarted;

                Console.WriteLine();

                Out.WriteLine("¡POLAR EMULADOR ROLEPLAY!   TIEMPO DE ENCENDIDO: " + TimeUsed.Seconds + " segundos, " + TimeUsed.Milliseconds + " milisegundos", "Polar.Boot", ConsoleColor.Cyan);
                IsLive = true;
            }
            catch (KeyNotFoundException e)
            {
                Logging.WriteLine("Compruebe su archivo de configuración. Parece que algunos valores faltan.", ConsoleColor.Red);
                Logging.WriteLine("Presione cualquier tecla para cerrar...");
                Logging.WriteLine(e.ToString());
                Console.ReadKey(true);
                Environment.Exit(1);
                return;
            }
            catch (InvalidOperationException e)
            {
                Logging.WriteLine("Error al inicializar ORIONRP: " + e.Message, ConsoleColor.Red);
                Logging.WriteLine("Presione cualquier tecla para cerrar...");
                Console.ReadKey(true);
                Environment.Exit(1);
                return;
            }
            catch (Exception e)
            {
                Logging.WriteLine("Error fatal durante el inicio: " + e, ConsoleColor.Red);
                Logging.WriteLine("Presione una tecla para salir");

                Console.ReadKey();
                Environment.Exit(1);
            }


            RoleplayBotManager.Initialize(false);

        }

        public static bool EnumToBool(string Enum)
        {
            return (Enum == "1");
        }

        public static string BoolToEnum(bool Bool)
        {
            return (Bool == true ? "1" : "0");
        }

        public static int GetRandomNumber(int Min, int Max)
        {
            return RandomNumber.GenerateNewRandom(Min, Max);
        }

        public static int GetRandomNumberMulti(int Min, int Max) => RandomNumber.GenerateLockedRandom(Min, Max);

        public static int GetUnixTimestamp() => (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        /* public static double GetUnixTimestamp()
         {
             TimeSpan ts = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0));
             return ts.TotalSeconds;
         }
         */
        internal static int GetIUnixTimestamp()
        {
            var ts = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0));
            var unixTime = ts.TotalSeconds;
            return Convert.ToInt32(unixTime);
        }

        public static long Now()
        {
            TimeSpan ts = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0));
            double unixTime = ts.TotalMilliseconds;
            return (long)unixTime;
        }

        public static long UnixTimeNow()
        {
            var timeSpan = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0));
            return (long)timeSpan.TotalSeconds;
        }

        public static string FilterFigure(string figure)
        {
            foreach (char character in figure)
            {
                if (!isValid(character))
                    return "sh-3338-93.ea-1406-62.hr-831-49.ha-3331-92.hd-180-7.ch-3334-93-1408.lg-3337-92.ca-1813-62";
            }

            return figure;
        }

        private static bool isValid(char character)
        {
            return Allowedchars.Contains(character);
        }

        public static bool IsValidAlphaNumeric(string inputStr)
        {
            inputStr = inputStr.ToLower();
            if (string.IsNullOrEmpty(inputStr))
            {
                return false;
            }

            for (int i = 0; i < inputStr.Length; i++)
            {
                if (!isValid(inputStr[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public static void Progress(AbstractBar bar, int wait, int end, string message)
        {
            bar.PrintMessage(message);
            for (var cont = 0; cont < end; cont++)
                bar.Step();
        }

        public static string GetUsernameById(int Id)
        {
            #region Old (OFF)
            /*
            string Name = "Ninguno";

            GameClient Client = GetGame().GetClientManager().GetClientByUserID(UserId);
            if (Client != null && Client.GetHabbo() != null)
                return Client.GetHabbo().Username;

            UserCache User = PlusEnvironment.GetGame().GetCacheManager().GenerateUser(UserId);
            if (User != null)
                return User.Username;

            using (IQueryAdapter dbClient = PlusEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT `username` FROM `users` WHERE id = @id LIMIT 1");
                dbClient.AddParameter("id", UserId);
                Name = dbClient.getString();
            }

            if (string.IsNullOrEmpty(Name))
                Name = "Ninguno";

            return Name;
            */
            #endregion

            GameClient client = GetGame().GetClientManager().GetClientByUserID(Id);

            if (client != null && client.GetHabbo() != null)
                return client.GetHabbo().Username;

            string username;
            using (IQueryAdapter dbClient = GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT username FROM users WHERE id = @id LIMIT 1");
                dbClient.AddParameter("id", Id);
                username = dbClient.getString();
            }

            return username;
        }
        public static string GetUserInfoBy(string info, string by, string data)
        {
            string get = null;
            using (IQueryAdapter dbClient = GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT `" + info + "` FROM `users` WHERE  `" + by + "` =  '" + data + "' LIMIT 1");
                get = dbClient.getString();
            }

            return get;
        }

        public static string GetUserIdByPhoneNumber(string phonenumber)
        {
            string get = null;
            using (IQueryAdapter dbClient = GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT `user_id` FROM `rp_phones_owned` WHERE  `phone_number` =  '" + phonenumber + "' LIMIT 1");
                get = dbClient.getString();
            }

            return get;
        }
        public static Habbo GetHabboById(int UserId)
        {
            try
            {
                GameClient Client = GetGame()?.GetClientManager()?.GetClientByUserID(UserId);
                if (Client != null)
                {
                    Habbo User = Client.GetHabbo();
                    if (User != null && User.Id > 0)
                    {
                        if (_usersCached.ContainsKey(UserId))
                            _usersCached.TryRemove(UserId, out User);
                        return User;
                    }
                }
                else
                {
                    try
                    {
                        if (_usersCached.ContainsKey(UserId))
                            return _usersCached[UserId];
                        else
                        {
                            UserData data = UserDataFactory.GetUserData(UserId);
                            if (data != null)
                            {
                                Habbo Generated = data.user;
                                if (Generated != null)
                                {
                                    Generated.InitInformation(data);
                                    _usersCached.TryAdd(UserId, Generated);
                                    return Generated;
                                }
                            }
                        }
                    }
                    catch { return null; }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public static Habbo GetHabboByUsername(String UserName)
        {
            try
            {
                using (IQueryAdapter dbClient = GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("SELECT `id` FROM `users` WHERE `username` = @user LIMIT 1");
                    dbClient.AddParameter("user", UserName);
                    int id = dbClient.getInteger();
                    if (id > 0)
                        return GetHabboById(Convert.ToInt32(id));
                }
                return null;
            }
            catch { return null; }
        }



        public static async Task PerformShutDown(bool Crashed = true, bool restart = false)
        {
            Console.Clear();
            //log.Info("Servidor apagándose...");
            Out.WriteLine("Servidor apagándose...", "Polar.Messages.Net", ConsoleColor.Red);
            Console.Title = "POLAR RP: Apagando...";

            if (!Crashed)
                PolarEnvironment.GetGame().GetClientManager().SendMessage(new Communication.Packets.Outgoing.Rooms.Notifications.RoomNotificationComposer("Maintenance Alert!", PolarEnvironment.GetGame().GetLanguageLocale().TryGetValue("shutdown_alert"), "disconnection", "ok", "event:"));
            else
                PolarEnvironment.GetGame().GetClientManager().SendMessage(new RoomNotificationComposer("Se apagará el servidor un momento", PolarEnvironment.GetGame().GetLanguageLocale().TryGetValue("shutdown_alert_crash"), "disconnection", "ok", "event:"));

            await GetGame().DestroyAsync();

            await Task.Delay(Crashed ? 6500 : 2500);

            //GetConnectionManager().Destroy();//Stop listening.

            if (_connectionManager != null)
                _connectionManager.destroy();

           // GetGame().GetPacketManager().UnregisterAll();//Unregister the packets.
            //GetGame().GetPacketManager().WaitForAllToComplete();
            GetGame().GetClientManager().CloseAll();
            await Task.Delay(5000);


            FarmingManager.UpdateAllFarmingSpaces();

            RoleplayManager.TimerManager.EndAllTimers();
            RoleplayManager.TimerManager = null;

            //Close all connections
            using (IQueryAdapter dbClient = _manager.GetQueryReactor())
            {
                dbClient.RunQuery("TRUNCATE `catalog_marketplace_data`");
                dbClient.RunQuery("UPDATE `users` SET online = '0', `auth_ticket` = '0'");
                dbClient.RunQuery("UPDATE `users` SET vip_points = '0' WHERE vip_points < 1");
                dbClient.RunQuery("UPDATE `rooms` SET `users_now` = '0' WHERE `users_now` > '0'");
                dbClient.RunQuery("UPDATE `server_status` SET `users_online` = '0', `loaded_rooms` = '0', `environment_status` = '" + (Crashed ? 3 : 0) + "'");
            }

            IsLive = false;

            WebSocketChatManager.StopAllChats();

            // log.Info("ORION RP: ha cerrado correctamente.");
            Out.WriteLine("POLAR RP: ha cerrado correctamente.", "Polar.Messages.Net", ConsoleColor.Green);
            Console.Title = "POLAR RP: Apagado correctamente";

            if (restart)
            {
                Task.Run(async () =>
                {
                    Console.Clear();
                    Console.WriteLine();

                    Out.WriteLine("El servidor se está " + (restart ? "reiniciando" : "apagando") + "....", "Polar.Messages.Net", ConsoleColor.Red);

                    // Esperar 5 segundos antes de reiniciar
                    await Task.Delay(5000);

                    if (restart)
                    {
                        try
                        {
                            // Obtener el nombre del archivo ejecutable actual
                            string executable = Assembly.GetEntryAssembly().Location;

                            // Asegurarse de que el proceso se cierre antes de reiniciar
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = executable,
                                CreateNoWindow = true,
                                UseShellExecute = false // Ejecutar sin usar el shell
                            });

                            // Terminar el proceso actual
                            Environment.Exit(0); // Esto cerrará el proceso actual
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error al reiniciar el proceso: " + ex.Message);
                        }
                    }

                }).Wait();
            }


            await Task.Delay(1000);
            Environment.Exit(0);

        }

        public static ConfigurationData GetConfig()
        {
            return _configuration;
        }

        public static ConfigData GetDBConfig()
        {
            return ConfigData;
        }

        public static Encoding GetDefaultEncoding()
        {
            return _defaultEncoding;
        }

        public static ConnectionHandling GetConnectionManager()
        {
            return _connectionManager;
        }

        public static Game GetGame()
        {
            return _game;
        }

        public static DatabaseManager GetDatabaseManager()
        {
            return _manager;
        }

        public static ICollection<Habbo> GetUsersCached()
        {
            return _usersCached.Values;
        }

        public static bool RemoveFromCache(int Id, out Habbo Data)
        {
            return _usersCached.TryRemove(Id, out Data);
        }

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        public static Int64 DateTimeToUnixTimeStamp(DateTime target)
        {
            var d = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return Convert.ToInt64((target - d).TotalSeconds);
        }

        public static string translate(String input, string from, string to)
        {
            var fromLanguage = from;
            var toLanguage = to;
            var url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={fromLanguage}&tl={toLanguage}&dt=t&q={HttpUtility.UrlEncode(input)}";
            var webclient = new WebClient
            {
                Encoding = System.Text.Encoding.UTF8
            };
            var result = webclient.DownloadString(url);
            try
            {
                result = result.Substring(4, result.IndexOf("\"", 4
                    , StringComparison.Ordinal) - 4);
                return result;
            }
            catch (Exception e1)
            {
                return "error";
            }


        }
        /// <summary>
        /// Translate Text using Google Translate API’s
        /// Google URL – http://www.google.com/translate_t?hl=en&ie=UTF8&text={0}&langpair={1}
        /// </summary>
        /// <param name=”input”>Input string</param>
        /// <param name=”languagePair”>2 letter Language Pair, delimited by “|”.
        /// E.g. “ar|en” language pair means to translate from Arabic to English</param>
        /// <returns>Translated to String</returns>
        public static string TranslateText(string input, string languagePair)
        {
            try
            {
                input = input.Replace(".", ",").Replace("!", ",") /*.Replace("/", ",").Replace("\\", ",").Replace("<", ",").Replace(">", ",").Replace(")", ",").Replace("(", ",").Replace("*", ",")*/;

                // Decode from UTF-8
                byte[] bytes = Encoding.Default.GetBytes(input);
                input = Encoding.GetEncoding(1252).GetString(bytes);

                string URL = String.Format("http://www.google.com/translate_t?hl=en&ie=UTF8&text={0}&langpair={1}", input, languagePair);
                string Result;

                using (WebClient webClient = new WebClient())
                {
                    webClient.Encoding = Encoding.GetEncoding(1252);

                    Result = webClient.DownloadString(URL);
                    Result = Regex.Split(Result, "<span id=result_box")[1];
                    Result = Regex.Split(Result, "</span>")[0];
                    Result = Regex.Split(Result, "#fff'\">")[1];
                }

                return Result.Trim();
            }
            catch
            {
                return input;
            }
        }
    }
}