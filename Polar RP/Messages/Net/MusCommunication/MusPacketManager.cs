using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Polar.HabboHotel.GameClients;
using log4net;
using Polar.Messages.Net.MusCommunication.Incoming.Handshake;
using Polar.Messages.Net.MusCommunication.Outgoing.Handshake;
using Polar.Messages.Net.MusCommunication.Incoming.Phones;
using Polar.Messages.Net.MusCommunication.Outgoing.Phones;
using Polar.Messages.Net.MusCommunication.Incoming.Web;
using Polar.Messages.Net.MusCommunication.Outgoing.Web;

namespace Polar.Messages.Net.MusCommunication
{
    public sealed class MusPacketManager
    {
        private static readonly ILog log = LogManager.GetLogger("Polar.Messages.Net.MusCommunication");

        /// <summary>
        ///     Testing the Task code
        /// </summary>
        private readonly bool IgnoreTasks = true;

        /// <summary>
        ///     The maximum time a task can run for before it is considered dead
        ///     (can be used for debugging any locking issues with certain areas of code)
        /// </summary>
        private readonly int MaximumRunTimeInSec = 300; // 5 minutes

        /// <summary>
        ///     Should the handler throw errors or log and continue.
        /// </summary>
        private readonly bool ThrowUserErrors = false;

        /// <summary>
        ///     The task factory which is used for running Asynchronous tasks, in this case we use it to execute packets.
        /// </summary>
        private readonly TaskFactory _eventDispatcher;

        private ConcurrentDictionary<string, IMusPacketEvent> _Packets;
        private ConcurrentDictionary<string, MusPacketEvent> _PacketsOut;

        /// <summary>
        ///     Currently running tasks to keep track of what the current load is
        /// </summary>
        private readonly ConcurrentDictionary<int, Task> _runningTasks;

        public MusPacketManager()
        {
            this._Packets = new ConcurrentDictionary<string, IMusPacketEvent>();
            this._PacketsOut = new ConcurrentDictionary<string, MusPacketEvent>();

            this._eventDispatcher = new TaskFactory(TaskCreationOptions.PreferFairness, TaskContinuationOptions.None);
            this._runningTasks = new ConcurrentDictionary<int, Task>();

            this.RegisterIncoming();
            this.RegisterOutgoing();
        }

        public async void TryExecutePacket(MusConnection MUS, MusPacketEvent Packet)
        {
            IMusPacketEvent Pak = null;

            if (!_Packets.TryGetValue(Packet.PacketName, out Pak))
            {
                if (System.Diagnostics.Debugger.IsAttached)
                    log.Debug("PAQUETE MUS DESCONOCIDO: " + Packet.ToString());
                return;
            }
            else
            {
                log.Debug("PAQUETE MUS RECIBIDO: [" + Packet.PacketName + "]");
            }

            if (!IgnoreTasks)
                await ExecutePacketAsync(MUS, Packet, Pak);
            else
                await Pak.Parse(MUS, Packet);
        }

        private async Task ExecutePacketAsync(MusConnection MUS, MusPacketEvent Packet, IMusPacketEvent Pak)
        {
            DateTime Start = DateTime.Now;

            var CancelSource = new CancellationTokenSource();
            CancellationToken Token = CancelSource.Token;

            Task t = _eventDispatcher.StartNew(async () =>
            {
                await Pak.Parse(MUS, Packet);
                Token.ThrowIfCancellationRequested();
            }, Token);

            _runningTasks.TryAdd(t.Id, t);

            try
            {
                if (!t.Wait(MaximumRunTimeInSec * 1000, Token))
                {
                    CancelSource.Cancel();
                }
            }
            catch (AggregateException ex)
            {
                foreach (Exception e in ex.Flatten().InnerExceptions)
                {
                    if (ThrowUserErrors)
                    {
                        throw e;
                    }
                    else
                    {
                        //log.Fatal("Unhandled Error: " + e.Message + " - " + e.StackTrace);
                        //Session.Disconnect();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                //Session.Disconnect();
            }
            finally
            {
                Task RemovedTask = null;
                _runningTasks.TryRemove(t.Id, out RemovedTask);

                CancelSource.Dispose();

                //log.Debug("Event took " + (DateTime.Now - Start).Milliseconds + "ms to complete.");
            }
        }

        public void WaitForAllToComplete()
        {
            foreach (Task t in this._runningTasks.Values.ToList())
            {
                t.Wait();
            }
        }

        public void UnregisterAll()
        {
            this._Packets.Clear();
        }

        private void RegisterIncoming()
        {
            #region Handshake
            this._Packets.TryAdd("event_ping", new PingEvent());
            #endregion

            #region Phones
            this._Packets.TryAdd("event_getusercontacts", new GetUserContactsEvent());
            this._Packets.TryAdd("event_showphoneerror", new ShowPhoneErrorEvent());
            this._Packets.TryAdd("event_sendmessage", new SendMessageEvent());
            this._Packets.TryAdd("event_sendcommand", new CommandEvent());
            this._Packets.TryAdd("event_getmessages", new GetMessagesEvent());
            this._Packets.TryAdd("event_openchat", new OpenChatEvent());
            #endregion

            #region Web
            this._Packets.TryAdd("event_getusersrooms", new GetUserRoomsEvent());
            this._Packets.TryAdd("event_disconnectuser", new DiscconectUserEvent());
            this._Packets.TryAdd("event_giverubies", new GiveRBEvent());
            this._Packets.TryAdd("event_guns", new GunEvent());
            this._Packets.TryAdd("event_deductpl", new DeductPLEvent());
            this._Packets.TryAdd("event_givevip", new GiveVIPEvent());
            this._Packets.TryAdd("event_givefurni", new GiveFurniEvent());
            this._Packets.TryAdd("event_command", new CommandEvent());
            #endregion
        }

        private void RegisterOutgoing()
        {
            #region Handshake
            this._PacketsOut.TryAdd("composer_pong", new PongComposer());
            #endregion

            #region Phones
            this._PacketsOut.TryAdd("composer_sendusercontacts", new SendUserContactsComposer());
            this._PacketsOut.TryAdd("compose_openchat", new OpenChatComposer());
            #endregion

            #region Web
            this._PacketsOut.TryAdd("compose_senduserrooms", new SendUserRoomsComposer());
            #endregion
        }
    }
}