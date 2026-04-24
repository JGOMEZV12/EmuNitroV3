using System;
using System.Diagnostics;
using log4net;
using Polar.Database.Interfaces;
using System.Threading;

namespace Polar.HabboHotel.Global
{
    public static class ServerStatusUpdater
    {
        private static readonly ILog log = LogManager.GetLogger("Polar.HabboHotel.Global.ServerUpdater");

        private const int UpdateIntervalSecs = 30;

        private static int       _userPeak;
        private static Timer     _timer;
        private static Stopwatch _watch = new Stopwatch();

        public static void Init()
        {
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT userpeak FROM server_status");
                _userPeak = dbClient.getInteger();
            }

            _watch.Start();
            _timer = new Timer(OnTick, null,
                TimeSpan.FromSeconds(UpdateIntervalSecs),
                TimeSpan.FromSeconds(UpdateIntervalSecs));

            UpdateTitle(0, 0);
        }

        private static void OnTick(object _) => UpdateOnlineUsers();

        public static void Process()
        {
            if (_watch.ElapsedMilliseconds < 10000) return;
            _watch.Restart();
            UpdateOnlineUsers();
        }

        private static void UpdateOnlineUsers()
        {
            if (PolarEnvironment.GetGame()?.GetClientManager() == null) return;

            int users = PolarEnvironment.GetGame().GetClientManager().Count;
            int rooms = PolarEnvironment.GetGame().GetRoomManager().Count;

            if (users > _userPeak) _userPeak = users;

            UpdateTitle(users, rooms);

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                PolarEnvironment.GetGame().GetWebEventManager()
                    .BroadCastWebEvent("event_updateonlinecount", users.ToString());

                dbClient.SetQuery(
                    "UPDATE `server_status` SET `users_online` = @users, `loaded_rooms` = @rooms, `userpeak` = @peak LIMIT 1");
                dbClient.AddParameter("users", users);
                dbClient.AddParameter("rooms", rooms);
                dbClient.AddParameter("peak",  _userPeak);
                dbClient.RunQuery();
            }
        }

        private static void UpdateTitle(int users, int rooms)
        {
            TimeSpan up = DateTime.Now - PolarEnvironment.ServerStarted;
            string hotel = PolarEnvironment.GetConfig().data["hotel.name"];
            Console.Title = $"Polar Server RP [{hotel}] » [{users}] ON » [{rooms}] SALAS » [{up.Days}] DÍA(S) » [{up.Hours}] HORA(S)";
        }

        public static void Dispose()
        {
            _timer?.Dispose();
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                dbClient.RunQuery("UPDATE `server_status` SET `users_online` = '0', `rooms_loaded` = '0', `environment_status` = '0'");
        }
    }
}
