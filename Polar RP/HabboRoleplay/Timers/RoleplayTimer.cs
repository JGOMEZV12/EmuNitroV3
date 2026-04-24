using System;
using System.Threading;
using Polar.HabboHotel.GameClients;
using Polar.Utilities;
using Polar.Core;
using Polar.HabboRoleplay.Bots;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboRoleplay.Timers
{
    public abstract class RoleplayTimer
    {
        /// <summary>
        /// The client
        /// </summary>
        public GameClient Client;

        /// <summary>
        /// The timer
        /// </summary>
        private Timer Timer;

        /// <summary>
        /// The type of timer
        /// </summary>
        public string Type;

        /// <summary>
        /// Time interval
        /// </summary>
        private int Time;

        /// <summary>
        /// Random number generator
        /// </summary>
        public CryptoRandom Random = new CryptoRandom();

        /// <summary>
        /// Represents if the timer should last forever
        /// </summary>
        private bool Forever;

        /// <summary>
        /// Represents the time left if specified
        /// </summary>
        public int TimeLeft = 0;

        /// <summary>
        /// Represents the amount of times the timer has looped
        /// </summary>
        public int TimeCount = 0;

        /// <summary>
        /// Represents the amount of times the timer has looped 2
        /// </summary>
        public int TimeCount2 = 0;

        /// <summary>
        /// Represents the original time
        /// </summary>
        public int OriginalTime = 0;

        /// <summary>
        /// Represents any special data
        /// </summary>
        public object[] Params;

        /// <summary>
        /// Constructor
        /// </summary>
        public RoleplayTimer(string Type, GameClient Client, int Time, bool Forever, object[] Params)
        {
            this.Type = Type;
            this.Client = Client;
            this.Time = Time;
            this.Forever = Forever;
            this.Params = Params;
            this.Timer = new Timer(Finished, null, Time, Time);
        }

        /// <summary>
        /// Called when the timer finishes/ticks
        /// </summary>
        private void Finished(object State)
        {
            try
            {
                Execute();

                if (Forever && Timer != null)
                {
                    Timer.Change(Time, Time);
                    return;
                }

                if (TimeLeft <= 0)
                    EndTimer();
            }
            catch (Exception e)
            {
                Logging.LogRPTimersError("Se ha producido un error al intentar finalizar un temporizador: " + e);
                EndTimer();
            }
        }

        /// <summary>
        /// Ends our timer
        /// </summary>
        public void EndTimer()
        {
            try
            {
                if (Timer == null)
                    return;

                Timer.Change(Timeout.Infinite, Timeout.Infinite);
                Timer.Dispose();
                Timer = null;

                if (Client != null && Client.GetRoleplay() != null)
                {
                    RoleplayTimer Junk;
                    Client.GetRoleplay().TimerManager.ActiveTimers.TryRemove(Type, out Junk);

                    // Limpiamos WS
                    if (Client.GetRoleplay().WebSocketConnection != null)
                    {
                        switch (Type)
                        {
                            #region Wanted Stars
                            case "wanted":
                                PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(Client, "compose_wanted_stars|0");// Enviamos con Nivel de busqueda 0
                                break;
                            #endregion

                            #region Default
                            default:
                                break;
                                #endregion
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logging.LogRPTimersError("Error in EndTimer() void: " + e);
            }
        }

        /// <summary>
        /// Called when the timer finishes/ticks
        /// </summary>
        public abstract void Execute();
    }

    public abstract class BotRoleplayTimer
    {
        public RoleplayBot CachedBot;
        private Timer      _timer;
        public  string     Type;
        private int        _time;
        private bool       _forever;

        // Flag atómico: evita que Finished() reprograme el timer después de EndTimer()
        private volatile bool _stopped = false;

        public CryptoRandom Random      = new CryptoRandom();
        public int          TimeLeft    = 0;
        public int          TimeCount   = 0;
        public int          TimeCount2  = 0;
        public int          OriginalTime = 0;
        public object[]     Params;

        public BotRoleplayTimer(string Type, RoleplayBot CachedBot, int Time, bool Forever, object[] Params)
        {
            this.Type      = Type;
            this._time     = Time;
            this._forever  = Forever;
            this.CachedBot = CachedBot;
            this.Params    = Params;
            // dueTime = Time para el primer tick, period = Timeout.Infinite
            // (el reprogramado lo hace Finished() manualmente, controlado por _stopped)
            this._timer = new Timer(Finished, null, Time, Timeout.Infinite);
        }

        private void Finished(object State)
        {
            // Si ya se detuvo, no hacer nada
            if (_stopped) return;

            try
            {
                Execute();
            }
            catch (Exception e)
            {
                Logging.LogRPTimersError("Error en BotRoleplayTimer.Execute(): " + e);
                EndTimer();
                return;
            }

            // Reprogramar solo si sigue activo y es forever
            if (!_stopped && _forever)
            {
                try { _timer?.Change(_time, Timeout.Infinite); }
                catch { /* timer ya dispuesto */ }
            }
            else if (!_stopped && TimeLeft <= 0)
            {
                EndTimer();
            }
        }

        public void EndTimer()
        {
            // Marcar como detenido primero para que Finished() no reprograme
            _stopped = true;

            try
            {
                if (_timer != null)
                {
                    _timer.Change(Timeout.Infinite, Timeout.Infinite);
                    _timer.Dispose();
                    _timer = null;
                }
            }
            catch { }

            // Remover del diccionario — usar el bot directamente en lugar de DRoomUser
            // para evitar el null check que antes impedía la remoción
            try
            {
                var timerManager = CachedBot?.TimerManager
                    ?? CachedBot?.DRoomUser?.GetBotRoleplay()?.TimerManager;

                if (timerManager?.ActiveTimers != null && Type != null)
                {
                    timerManager.ActiveTimers.TryRemove(Type, out _);
                }
            }
            catch (Exception e)
            {
                Logging.LogRPTimersError("Error en BotRoleplayTimer.EndTimer() al remover: " + e);
            }
        }

        public abstract void Execute();
    }
    public abstract class SystemRoleplayTimer
    {
        /// <summary>
        /// The timer
        /// </summary>
        private Timer Timer;

        /// <summary>
        /// The type of timer
        /// </summary>
        public string Type;

        /// <summary>
        /// Time interval
        /// </summary>
        private int Time;

        /// <summary>
        /// Random number generator
        /// </summary>
        public CryptoRandom Random = new CryptoRandom();

        /// <summary>
        /// Represents if the timer should last forever
        /// </summary>
        private bool Forever;

        /// <summary>
        /// Represents the time left if specified
        /// </summary>
        public int TimeLeft = 0;

        /// <summary>
        /// Represents the amount of times the timer has looped
        /// </summary>
        public int TimeCount = 0;

        /// <summary>
        /// Represents the amount of times the timer has looped
        /// </summary>
        public int TimeCount2 = 0;

        /// <summary>
        /// Represents the original time
        /// </summary>
        public int OriginalTime = 0;

        /// <summary>
        /// Represents any special data
        /// </summary>
        public object[] Params;

        /// <summary>
        /// Constructor
        /// </summary>
        public SystemRoleplayTimer(string Type, int Time, bool Forever, object[] Params)
        {
            this.Type = Type;
            this.Time = Time;
            this.Forever = Forever;
            this.Params = Params;
            this.Timer = new Timer(Finished, null, Time, Time);
        }

        /// <summary>
        /// Called when the timer finishes/ticks
        /// </summary>
        private void Finished(object State)
        {
            try
            {
                Execute();

                if (Forever && Timer != null)
                {
                    Timer.Change(Time, Time);
                    return;
                }

                if (TimeLeft <= 0)
                    EndTimer();
            }
            catch (Exception e)
            {
                Logging.LogRPTimersError("Se ha producido un error al intentar finalizar un temporizador: " + e);
                EndTimer();
            }
        }

        /// <summary>
        /// Ends our timer
        /// </summary>
        public void EndTimer()
        {
            try
            {
                if (Timer == null)
                    return;

                Timer.Change(Timeout.Infinite, Timeout.Infinite);
                Timer.Dispose();
                Timer = null;

                if (RoleplayManager.TimerManager != null)
                {
                    SystemRoleplayTimer Junk;
                    RoleplayManager.TimerManager.ActiveTimers.TryRemove(Type, out Junk);
                }
            }
            catch (Exception e)
            {
                Logging.LogRPTimersError("Error in EndTimer() void: " + e);
            }
        }

        /// <summary>
        /// Called when the timer finishes/ticks
        /// </summary>
        public abstract void Execute();
    }
}