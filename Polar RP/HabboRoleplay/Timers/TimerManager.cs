using System;
using System.Linq;
using System.Collections.Concurrent;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Timers.Types;
using Polar.HabboRoleplay.Timers.Types.Items;
using Polar.HabboRoleplay.Bots;
using Polar.HabboRoleplay.Web.Util.ChatRoom;
using Polar.HabboHotel.Rooms;
using Polar.HabboRoleplay.Bots.Manager;

namespace Polar.HabboRoleplay.Timers
{
    public class TimerManager
    {
        /// <summary>
        /// The client
        /// </summary>
        public GameClient Client;

        /// <summary>
        /// Contains all running timers
        /// </summary>
        public ConcurrentDictionary<string, RoleplayTimer> ActiveTimers;

        /// <summary>
        /// Constructs our manager
        /// </summary>
        public TimerManager(GameClient Client)
        {
            this.Client = Client;
            ActiveTimers = new ConcurrentDictionary<string, RoleplayTimer>();

            // Start up our Forever timers
            CreateTimer("hunger", 1000, true);
            CreateTimer("sida", 1000, true);
            CreateTimer("hygiene", 1000, true);
            CreateTimer("animo", 1000, true);
            CreateTimer("poop", 1000, true);
            CreateTimer("conditioncheck", 1000, true);
            CreateTimer("interest", 1000, true);
        }
        /// <summary>
        /// Creates a timer
        /// </summary>
        public RoleplayTimer CreateTimer(string Type, int Time, bool Forever, params object[] Params)
        {
            if (ActiveTimers.ContainsKey(Type))
                return null;

            RoleplayTimer Timer = GetTimerFromType(Type, Time, Forever, Params);

            if (Timer == null)
                return null;

            ActiveTimers.TryAdd(Type, Timer);

            return Timer;
        }

        /// <summary>
        /// Returns a new timer based on the type
        /// </summary>
        /// <param name="TypeOfTimer"></param>
        private RoleplayTimer GetTimerFromType(string TypeOfTimer, int Time, bool Forever, object[] Params)
        {
            switch (TypeOfTimer)
            {
                case "hunger":
                    return new HungerTimer(TypeOfTimer, Client, Time, Forever, Params);
                case "sida":
                    return new SidaTimer(TypeOfTimer, Client, Time, Forever, Params);
                case "hygiene":
                    return new HygieneTimer(TypeOfTimer, Client, Time, Forever, Params);
                case "animo":
                    return new AnimoTimer(TypeOfTimer, Client, Time, Forever, Params);
                case "poop":
                    return new PoopTimer(TypeOfTimer, Client, Time, Forever, Params);
                case "conditioncheck":
                    return new ConditionCheckTimer(TypeOfTimer, Client, Time, Forever, Params);
                case "heal":
                    return new HealTimer(TypeOfTimer, Client, Time, Forever, Params);
                case "curar":
                    return new HealTimer(TypeOfTimer, Client, Time, Forever, Params);
                case "shower":
                    return new ShowerTimer(TypeOfTimer, Client, Time, Forever, Params);
                case "cagar":
                    return new CagarTimer(TypeOfTimer, Client, Time, Forever, Params);
                case "work":
                    return new WorkTimer(TypeOfTimer, Client, Time, Forever, Params);
                case "processcocaina":
                    return new ProcessCocaina(TypeOfTimer, Client, Time, Forever, Params);
                case "processheroine":
                    return new ProcessHeroine(TypeOfTimer, Client, Time, Forever, Params);
                case "processweed":
                    return new ProcessWeed(TypeOfTimer, Client, Time, Forever, Params);
                case "sendhome":
                    return new SendhomeTimer(TypeOfTimer, Client, Time, Forever, Params);
                case "death":
                    return new DeathTimer(TypeOfTimer, Client, Time, Forever, Params);
    
                case "jail":
                    return new JailTimer(TypeOfTimer, Client, Time, Forever, Params);
                case "stun":
                    return new StunTimer(TypeOfTimer, Client, Time, Forever, Params);
                case "spray":
                    return new SprayTimer(TypeOfTimer, Client, Time, Forever, Params);
                case "cuff":
                    return new CuffTimer(TypeOfTimer, Client, Time, Forever, Params);
                case "wanted":
                    return new WantedTimer(TypeOfTimer, Client, Time, Forever, Params);
                case "probation":
                    return new ProbationTimer(TypeOfTimer, Client, Time, Forever, Params);
                case "turfcapture":
                    return new TurfCaptureTimer(TypeOfTimer, Client, Time, Forever, Params);
                case "workout":
                    return new WorkoutTimer(TypeOfTimer, Client, Time, Forever, Params);
                case "interest":
                    return new InterestTimer(TypeOfTimer, Client, Time, Forever, Params);
                case "noob":
                    return new NoobTimer(TypeOfTimer, Client, Time, Forever, Params);
                case "inmunity":
                    return new InmunityTimer(TypeOfTimer, Client, Time, Forever, Params);
                case "repair":
                    return new RepairTimer(TypeOfTimer, Client, Time, Forever, Params);
                case "robbery":
                    return new RobberyTimer(TypeOfTimer, Client, Time, Forever, Params);
                case "bankrob":
                    return new bankRobTimer(TypeOfTimer, Client, Time, Forever, Params);
                case "learning":
                    return new learningTimer(TypeOfTimer, Client, Time, Forever, Params);
                case "robtienda":
                    return new RobartiendaTimer(TypeOfTimer, Client, Time, Forever, Params);
                case "atmrob":
                    return new ATMRobTimer(TypeOfTimer, Client, Time, Forever, Params);
                case "general":
                    return new GeneralTimer(TypeOfTimer, Client, Time, Forever, Params);
                case "fuel":
                    return new FuelTimer(TypeOfTimer, Client, Time, Forever, Params);
                case "vehiclejob":
                    return new VehicleJobTimer(TypeOfTimer, Client, Time, Forever, Params);
                default:
                    return null;
            }
        }

        /// <summary>
        /// Ends a specific timer by type
        /// </summary>
        public void EndTimer(string type)
        {
            if (ActiveTimers.TryRemove(type, out RoleplayTimer timer))
            {
                timer.EndTimer();
            }
        }

        /// <summary>
        /// Ends all of the timers
        /// </summary>
        public void EndAllTimers()
        {
            lock (ActiveTimers.Values)
            {
                foreach (RoleplayTimer Timer in ActiveTimers.Values)
                    Timer.EndTimer();
            }
        }
    }

    public class BotTimerManager
    {
        /// <summary>
        /// The bot
        /// </summary>
        public RoleplayBot CachedBot;

        /// <summary>
        /// Contains all running timers
        /// </summary>
        public ConcurrentDictionary<string, BotRoleplayTimer> ActiveTimers;

        /// <summary>
        /// Constructs our manager
        /// </summary>
        public BotTimerManager(RoleplayBot CachedBot)
        {
            this.CachedBot = CachedBot;
            ActiveTimers = new ConcurrentDictionary<string, BotRoleplayTimer>();

            if (this.CachedBot == null)
                return;

            if (this.CachedBot.DRoomUser == null)
                return;

            if (this.CachedBot.RoamBot)
                this.CachedBot.MoveRandomly();

        }

        /// <summary>
        /// Creates a timer
        /// </summary>
        public BotRoleplayTimer CreateTimer(string Type, RoleplayBot CachedBot, int Time, bool Forever, params object[] Params)
        {
            // Si ya existe un timer de este tipo, terminarlo y removerlo antes de crear el nuevo
            if (ActiveTimers.TryRemove(Type, out BotRoleplayTimer existing))
            {
                try { existing.EndTimer(); } catch { }
            }
 
            BotRoleplayTimer Timer = GetTimerFromType(Type, CachedBot, Time, Forever, Params);
 
            if (Timer == null)
                return null;
 
            ActiveTimers.TryAdd(Type, Timer);
 
            return Timer;
        }
 
        /// <summary>
        /// Returns a new timer based on the type
        /// </summary>
        /// <param name="TypeOfTimer"></param>
        private BotRoleplayTimer GetTimerFromType(string TypeOfTimer, RoleplayBot CachedBot, int Time, bool Forever, object[] Params)
        {
            switch (TypeOfTimer)
            {
                // Default Bots
                case "startwork":
                    return new StartWorkTimer(TypeOfTimer, CachedBot, Time, Forever, Params);
                case "stopwork":
                    return new StopWorkTimer(TypeOfTimer, CachedBot, Time, Forever, Params);

                // Hospital Bots
                case "discharge":
                    return new DischargeTimer(TypeOfTimer, CachedBot, Time, Forever, Params);

                // Serving Bots
                case "serving":
                    return new ServingTimer(TypeOfTimer, CachedBot, Time, Forever, Params);

                // Gun Store Bots
                case "deliverywait":
                    return new DeliveryWaitTimer(TypeOfTimer, CachedBot, Time, Forever, Params);
                case "pickupdelivery":
                    return new PickupDeliveryTimer(TypeOfTimer, CachedBot, Time, Forever, Params);

                // Delivery Bots
                case "startdelivery":
                    return new StartDeliveryTimer(TypeOfTimer, CachedBot, Time, Forever, Params);
                case "stopdelivery":
                    return new StopDeliveryTimer(TypeOfTimer, CachedBot, Time, Forever, Params);

                // Thug Bots
                case "attack":
                    return new AttackTimer(TypeOfTimer, CachedBot, Time, Forever, Params);
                case "botdeath":
                    return new BotDeathTimer(TypeOfTimer, CachedBot, Time, Forever, Params);

                // Jury Bots
                case "jury":
                    return new JuryTimer(TypeOfTimer, CachedBot, Time, Forever, Params);

                default:
                    return null;
            }
        }

        /// <summary>
        /// Ends all of the timers
        /// </summary>
        public void EndAllTimers()
        {
            lock (ActiveTimers.Values)
            {
                foreach (BotRoleplayTimer Timer in ActiveTimers.Values)
                    Timer.EndTimer();
            }
        }
    }

    public class SystemTimerManager
    {
        /// <summary>
        /// Contains all running timers
        /// </summary>
        public ConcurrentDictionary<string, SystemRoleplayTimer> ActiveTimers;

        /// <summary>
        /// Constructs our manager
        /// </summary>
        public SystemTimerManager()
        {
            ActiveTimers = new ConcurrentDictionary<string, SystemRoleplayTimer>();
            CreateTimer("farmingspace", 1000, true);
            CreateTimer("daynight", 5000, true);
        }

        /// <summary>
        /// Creates a timer
        /// </summary>
        public SystemRoleplayTimer CreateTimer(string Type, int Time, bool Forever, params object[] Params)
        {
            if (ActiveTimers.ContainsKey(Type))
                return null;

            SystemRoleplayTimer Timer = GetTimerFromType(Type, Time, Forever, Params);

            if (Timer == null)
                return null;

            ActiveTimers.TryAdd(Type, Timer);

            return Timer;
        }

        /// <summary>
        /// Returns a new timer based on the type
        /// </summary>
        /// <param name="TypeOfTimer"></param>
        private SystemRoleplayTimer GetTimerFromType(string TypeOfTimer, int Time, bool Forever, object[] Params)
        {
            switch (TypeOfTimer)
            {
                case "farmingspace":
                    return new FarmingSpaceTimer(TypeOfTimer, Time, Forever, Params);
                case "jailbreak":
                    return new JailbreakTimer(TypeOfTimer, Time, Forever, Params);
                case "dynamite":
                    return new DynamiteTimer(TypeOfTimer, Time, Forever, Params);
                case "matchingpoll":
                    return new MatchingPollTimer(TypeOfTimer, Time, Forever, Params);
                case "texasholdem":
                    return new TexasHoldEmTimer(TypeOfTimer, Time, Forever, Params);
                case "juryinvitation":
                    return new InvitationTimer(TypeOfTimer, Time, Forever, Params);
                case "websocketchatmanager":
                    return new WebSocketChatManagerMainTimer(TypeOfTimer, Time, Forever, Params);
                case "daynight":
                    return new DayNightCycleTimer(TypeOfTimer, Time, Forever, Params);
                case "nuking":
                    return new NukeTimer(TypeOfTimer, Time, Forever, Params);
                case "nuking_bd":
                    return new BreakdownTimer(TypeOfTimer, Time, Forever, Params);
                case "purge":
                    return new PurgeTimer(TypeOfTimer, Time, Forever, Params);
                default:
                    return null;
            }
        }

        /// <summary>
        /// Ends all of the timers
        /// </summary>
        public void EndAllTimers()
        {
            lock (ActiveTimers.Values)
            {
                foreach (SystemRoleplayTimer Timer in ActiveTimers.Values)
                    Timer.EndTimer();
            }
        }
    }
}