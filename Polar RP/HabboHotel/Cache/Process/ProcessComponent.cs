using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections.Generic;

using log4net;
using Polar.HabboHotel.Cache;
using Polar.HabboHotel.Users;
using Polar.Core;

namespace Polar.HabboHotel.Cache.Process
{
    sealed class ProcessComponent
    {
        private Timer _timer = null;
        private bool _timerRunning = false;
        private bool _timerLagging = false;
        private bool _disabled = false;

        private AutoResetEvent _resetEvent = new AutoResetEvent(true);

        private static int _runtimeInSec = 1200;

        public void Init() => this._timer = new Timer(new TimerCallback(Run), null, _runtimeInSec * 1000, _runtimeInSec * 1000);

        public void Run(object State)
        {
            try
            {
                if (this._disabled)
                    return;

                if (this._timerRunning)
                {
                    this._timerLagging = true;
                    return;
                }

                this._resetEvent.Reset();

                // BEGIN CODE
                List<UserCache> CacheList = PolarEnvironment.GetGame().GetCacheManager().GetUserCache().ToList();
                if (CacheList.Count > 0)
                {
                    foreach (UserCache Cache in CacheList)
                    {
                        try
                        {
                            if (Cache == null)
                                continue;

                            UserCache Temp = null;

                            if (Cache.isExpired())
                                PolarEnvironment.GetGame().GetCacheManager().TryRemoveUser(Cache.Id, out Temp);

                            Temp = null;
                        }
                        catch (Exception e)
                        {
                            Logging.LogCacheException(e.ToString());
                        }
                    }
                }

                CacheList = null;

                    List<Habbo> CachedUsers = PolarEnvironment.GetUsersCached().ToList();
                    if (CachedUsers.Count > 0)
                    {
                        foreach (Habbo Data in CachedUsers)
                        {
                            try
                            {
                                if (Data == null)
                                    continue;

                                Habbo Temp = null;

                                if (Data.CacheExpired())
                                    PolarEnvironment.RemoveFromCache(Data.Id, out Temp);

                                if (Temp != null)
                                    Temp.Dispose();

                                Temp = null;
                            }
                            catch (Exception e)
                            {
                                Logging.LogCacheException(e.ToString());
                            }
                        }
                    }

                CachedUsers = null;
                // END CODE

                // Reset the values
                this._timerRunning = false;
                this._timerLagging = false;

                this._resetEvent.Set();
            }
            catch (Exception e) { Logging.LogCacheException(e.ToString()); }
        }

        /// <summary>
        /// Stops the timer and disposes everything.
        /// </summary>
        public void Dispose()
        {
            // Wait until any processing is complete first.
            try
            {
                this._resetEvent.WaitOne(TimeSpan.FromMinutes(5.0));
            }
            catch { } // give up

            // Set the timer to disabled
            this._disabled = true;

            // Dispose the timer to disable it.
            try
            {
                if (this._timer != null)
                    this._timer.Dispose();
            }
            catch { }

            // Remove reference to the timer.
            this._timer = null;
        }
    }
}