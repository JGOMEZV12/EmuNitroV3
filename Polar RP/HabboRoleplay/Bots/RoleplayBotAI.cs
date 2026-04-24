using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.AI;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using Polar.HabboRoleplay.Timers;
using System.Collections.Concurrent;
using static Polar.HabboRoleplay.Bots.Manager.TimerHandlers.TimerHandlerManager;
using Polar.HabboRoleplay.Bots.Manager.TimerHandlers;

namespace Polar.HabboRoleplay.Bots
{
    public abstract class RoleplayBotAI
    {

        #region Variables
        public int BaseId;
        private int RoomId;
        private int RoomUserId;
        private Room Room;
        private RoomUser RoomUser;
        public bool OnDuty;
        int SpeechCount = 0;
        public int ArmPiecesTo = 0;
        public int ArmUserTo = 0;
        #endregion

        public void Init(int BaseId, RoleplayBot Bot, int RoomUserId, int RoomId, RoomUser RoomUser, Room Room)
        {
            this.BaseId = BaseId;
            this.RoomUserId = RoomUserId;
            this.RoomId = RoomId;
            this.RoomUser = RoomUser;
            this.Room = Room;
        }

        public Room GetRoom() => Room;
        public RoomUser GetRoomUser() => RoomUser;

        public RoleplayBot GetBotData()
        {
            RoomUser User = GetRoomUser();
            if (User == null) return null;
            return GetRoomUser().RPBotData;
        }

        public RoleplayBot GetBotRoleplay()
        {
            RoomUser User = GetRoomUser();
            if (User == null) return null;
            return User.RPBotData;
        }

        public void Dispose()
        {
            BaseId = 0;
            RoomId = 0;
            RoomUserId = 0;
            Room = null;
            RoomUser = null;
        }

        public bool RespondToSpeech(GameClient Client, string Message)
        {
            if (this.GetBotRoleplay() == null) return false;
            if (Client == null || Client.GetRoleplay() == null || Client.GetHabbo() == null) return false;
            if (this.GetBotRoleplay().Responses == null) return false;
            if (!this.GetBotRoleplay().Responses.ContainsKey(Message.ToLower())) return false;

            var Speech = this.GetBotRoleplay().Responses[Message.ToLower()];
            string Response = Speech.Response.Replace("%user%", Client.GetHabbo().Username);

            if (Speech.Type == "whisper")
                Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, Response, 0, 2));
            else
                GetRoomUser().Chat(Response, Speech.Type == "shout", Speech.Bubble);

            return true;
        }

        /// <summary>
        /// Termina un timer de forma segura sin lanzar excepcion si no existe.
        /// </summary>
        protected void EndTimerSafe(string key)
        {
            var rp = GetBotRoleplay();
            if (rp?.TimerManager?.ActiveTimers == null) return;
            if (rp.TimerManager.ActiveTimers.TryGetValue(key, out var timer))
                timer.EndTimer();
        }

        public void RandomSpeechTick()
        {
            if (this == null || this.GetBotRoleplay() == null || this.GetBotRoleplay().RandomSpeech == null)
                return;
            if (this.GetBotRoleplay().RandomSpeech.Count == 0) return;
            if (this.GetBotRoleplay().Dead) return;

            if (SpeechCount < this.GetBotRoleplay().RandomSpeechTimer)
            {
                SpeechCount++;
                return;
            }

            SpeechCount = 0;
            string Message = "";

            if (this.GetBotRoleplay().RandomSpeech.Count > 1)
            {
                Random Random = new Random();
                int MessageCount = Random.Next(0, this.GetBotRoleplay().RandomSpeech.Count);
                Message = this.GetBotRoleplay().RandomSpeech[MessageCount].Message;
            }
            else
                Message = this.GetBotRoleplay().RandomSpeech[0].Message;

            if (GetRoomUser() != null)
                GetRoomUser().Chat(Message, true, 4);
        }

        public virtual void OnTimerTick()
        {
            if (IsNull()) return;

            #region Teleporting
            if (this.GetBotRoleplay().Teleporting)
            {
                if (this.GetBotRoleplay().ActiveHandlers.ContainsKey(Handlers.TELEPORT))
                {
                    IBotHandler Teleport = this.GetBotRoleplay().ActiveHandlers[Handlers.TELEPORT];
                    if (Teleport.Active)
                    {
                        Teleport.ExecuteHandler();
                        return;
                    }
                }
                else
                    this.GetBotRoleplay().Teleporting = false;
                return;
            }
            #endregion

            #region Roaming
            if (this.GetBotRoleplay().RoamBot)
                this.GetBotRoleplay().HandleRoaming();
            #endregion
        }

        public virtual bool IsNull()
        {
            if (this.GetBotRoleplay() == null) return true;
            if (this.GetBotRoleplay().DRoomUser == null) return true;
            if (this.GetRoomUser() == null) return true;
            if (this.GetRoomUser().GetRoom() == null) return true;
            return false;
        }

        public virtual void StopActivities()
        {
            GetBotRoleplay().Attacking = false;
            GetBotRoleplay().Teleporting = false;
            GetBotRoleplay().StopAllHandlers();
        }

        public virtual void OnDeath(GameClient Client) { }

        public abstract void OnDeployed(GameClient Client);
        public abstract void OnArrest(GameClient Client);
        public abstract void OnAttacked(GameClient Client);
        public abstract void OnMessaged(GameClient Client, string Message);
        public abstract void OnUserLeaveRoom(GameClient Client);
        public abstract void OnUserEnterRoom(GameClient Client);
        public abstract void OnUserUseTeleport(GameClient Client, object[] Params);
        public abstract void OnUserSay(RoomUser User, string Message);
        public abstract void OnUserShout(RoomUser User, string Message);
        public abstract void HandleRequest(GameClient Client, string Message);
        public abstract void StartActivities();
    }
}