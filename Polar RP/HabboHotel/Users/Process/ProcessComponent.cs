using System;
using System.Threading;
using log4net;
using Polar.Communication.Packets.Outgoing.Handshake;
using Polar.Database.Interfaces;

namespace Polar.HabboHotel.Users.Process
{
    internal sealed class ProcessComponent
    {
        private static readonly ILog log = LogManager.GetLogger("Polar.HabboHotel.Users.Process.ProcessComponent");
        private static readonly int IntervalMs = 1000;

        private Habbo _player;
        private Timer _timer;
        private bool _timerRunning;
        private bool _timerLagging;
        private bool _disabled;
        private readonly AutoResetEvent _resetEvent = new AutoResetEvent(true);

        public bool Init(Habbo player)
        {
            if (player == null || _player != null)
                return false;

            _player = player;
            _timer = new Timer(Run, null, IntervalMs, IntervalMs);
            return true;
        }

        public void Run(object state)
        {
            if (_disabled) return;

            // Captura local: evita que Dispose() anule _player mientras ejecutamos
            Habbo player = _player;
            if (player == null) return;

            if (_timerRunning)
            {
                _timerLagging = true;
                log.Warn($"<Player {player.Id}> Server can't keep up, Player timer is lagging behind.");
                return;
            }

            _timerRunning = true; // debe activarse ANTES del try
            _resetEvent.Reset();

            try
            {
                TickMuteCounters(player);
                TickConsoleSpam(player);

                player.TimeAFK += 1;

                TickDailyRespect(player);
                ResetScriptWarnings(player);

                var client = player.GetClient();
                if (client != null)
                    PolarEnvironment.GetGame().GetAchievementManager()
                        .ProgressAchievement(client, "ACH_AllTimeHotelPresence", 1);

                player.Effects()?.CheckEffectExpiry(player);
            }
            catch (Exception ex)
            {
                log.Error($"<Player {player.Id}> Exception in ProcessComponent.Run: {ex}");
            }
            finally
            {
                _timerRunning = false;
                _timerLagging = false;
                _resetEvent.Set();
            }
        }

        // ─── Private tick helpers ────────────────────────────────────────────────

        private void TickMuteCounters(Habbo player)
        {
            if (player.TimeMuted > 0.0)
                player.TimeMuted -= 1.0;

            var rp = player.GetClient()?.GetRoleplay();
            if (rp != null && rp.VIPBanned > 0)
                rp.VIPBanned--;
        }

        private void TickConsoleSpam(Habbo player)
        {
            if (player.MessengerSpamTime > 0)
                player.MessengerSpamTime -= 60.0;

            if (player.MessengerSpamTime <= 0)
                player.MessengerSpamCount = 0;
        }

        private void TickDailyRespect(Habbo player)
        {
            string today = DateTime.Today.ToString("MM/dd");
            if (player.GetStats().RespectsTimestamp == today)
                return;

            player.GetStats().RespectsTimestamp = today;
            player.GetStats().DailyRespectPoints = 3;
            player.GetStats().DailyPetRespectPoints = 3;

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.RunQuery(
                    $"UPDATE `user_stats` SET `dailyRespectPoints` = '3', `respectsTimestamp` = '{today}' " +
                    $"WHERE `id` = '{player.Id}' LIMIT 1");
            }

            player.GetClient()?.SendMessage(new UserObjectComposer(player));
        }

        private void ResetScriptWarnings(Habbo player)
        {
            if (player.GiftPurchasingWarnings < 15) player.GiftPurchasingWarnings = 0;
            if (player.MottoUpdateWarnings < 15) player.MottoUpdateWarnings = 0;
            if (player.ClothingUpdateWarnings < 15) player.ClothingUpdateWarnings = 0;
        }

        public void Dispose()
        {
            _disabled = true;

            // Detener el timer primero para que no dispare más ticks
            try { _timer?.Change(Timeout.Infinite, Timeout.Infinite); }
            catch { }

            // Esperar a que termine cualquier tick en curso
            try { _resetEvent.WaitOne(TimeSpan.FromMinutes(5.0)); }
            catch { /* give up waiting */ }

            try { _timer?.Dispose(); }
            catch { }

            _timer = null;
            _player = null;
        }
    }
}