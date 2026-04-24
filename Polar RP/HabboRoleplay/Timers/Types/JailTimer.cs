using System;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboRoleplay.Misc;
using Polar.Core;
using Polar.HabboHotel.Rooms;
using Polar.HabboRoleplay.RPRoom;

namespace Polar.HabboRoleplay.Timers.Types
{
    /// <summary>
    /// Prison timer
    /// </summary>
    public class JailTimer : RoleplayTimer
    {
        public JailTimer(string Type, GameClient Client, int Time, bool Forever, object[] Params)
            : base(Type, Client, Time, Forever, Params)
        {
            // Convert to milliseconds
            OriginalTime = 5;
            TimeLeft = Client.GetRoleplay().JailedTimeLeft * 60000;

            Client.GetRoleplay().UpdateTimerDialogue("Jail-Timer", "add", Client.GetRoleplay().JailedTimeLeft, OriginalTime);
        }

        /// <summary>
        /// Prison timer
        /// </summary>
        public override void Execute()
        {
            try
            {
                if (base.Client == null || base.Client.GetHabbo() == null || base.Client.GetRoleplay() == null)
                {
                    base.EndTimer();
                    return;
                }

                if (base.Client.GetRoomUser() == null)
                    return;

                HandlePassiveMode();
                HandleJailState();

                if (base.Client.GetRoleplay().Jailbroken)
                    return;

                TimeCount++;
                TimeLeft -= 1000;

                HandlePrisonCellState();
                UpdateWantedState();

                if (TimeLeft > 0)
                {
                    DecrementJailedTime();
                    return;
                }

                if (base.Client.GetRoomUser() != null)
                {
                    if (!RoleplayManager.GenerateRoom(Client.GetRoomUser().RoomId, out Room Room))
                        return;
                    string MyCity = Room.City;

                    HabboRoleplay.RPRoom.RPRoom Data;
                    int ToJail = PolarEnvironment.GetGame().GetRPRoomManager().TryToGetJail(MyCity, out Data);
                    int ToJailback = PolarEnvironment.GetGame().GetRPRoomManager().TryToGetJail(MyCity, out Data);

                    RoleplayManager.SendUser(base.Client, ToJailback, "");
                    //RoleplayManager.SpawnChairs(base.Client, "comodin_carr2");

                }

                ReleaseFromJail();
            }
            catch (Exception e)
            {
                Logging.LogRPTimersError($"Error in Execute() void: {e}");
                base.EndTimer();
            }
        }

        // Handle the passive mode of the client
        private void HandlePassiveMode()
        {
            if (base.Client.GetRoleplay().PassiveMode)
            {
                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(base.Client, "event_psv", "forceoff");
            }
        }

        // Handle the state when the user is in jail or stunned
        private void HandleJailState()
        {
            if (!base.Client.GetRoleplay().IsJailed)
            {
                ;
                Client.GetRoleplay().UpdateTimerDialogue("Jail-Timer", "remove", Client.GetRoleplay().JailedTimeLeft, OriginalTime);
                if (!RoleplayManager.GenerateRoom(Client.GetRoomUser().RoomId, out Room Room))
                    return;
                string MyCity = Room.City;

                HabboRoleplay.RPRoom.RPRoom Data;
                int ToJail = PolarEnvironment.GetGame().GetRPRoomManager().TryToGetJail(MyCity, out Data);
                int ToJailback = PolarEnvironment.GetGame().GetRPRoomManager().TryToGetJailBack(MyCity, out Data);

                RoleplayManager.SendUser(base.Client, ToJailback, "");
                RoleplayManager.Shout(base.Client, "*Cumple su condena en prisión y es puest@ en libertad*", 4);


                ProcessReleaseFromJail();
            }
        }

        // Process the release of the player from jail and update the necessary states
        private void ProcessReleaseFromJail()
        {
            ResetJailStates();
            UpdateWantedStars();
        }

        // Reset the jail-related states after the player is released
        private void ResetJailStates()
        {
            base.Client.GetRoleplay().WantedFor = "";
            base.Client.GetRoleplay().Trialled = false;
            base.Client.GetRoleplay().Jailbroken = false;
            base.Client.GetRoleplay().IsJailed = false;
            base.Client.GetRoleplay().IsStun = false;
            base.Client.GetRoleplay().Paralized = false;
            base.Client.GetRoleplay().JailedTimeLeft = 0;
            base.Client.GetRoleplay().InState = false;
            base.Client.GetHabbo().Poof(true);
            RoleplayManager.GetLookAndMotto(base.Client);
            base.EndTimer();
        }

        // Update the look of the client when they are released from jail
        private void UpdateJailLook()
        {
            base.Client.GetHabbo().Look = Client.GetRoleplay().OriginalOutfit;
            base.Client.GetHabbo().Motto = "Ciudadan@";
        }

        // Handle the prison cell state and spawn necessary items
        private void HandlePrisonCellState()
        {
            if (!base.Client.GetRoleplay().InState)
            {
                RoleplayManager.GetLookAndMotto(Client);
                RoleplayManager.SpawnBeds(Client, "bed_silo_one");
                base.Client.GetRoleplay().InState = true;
            }
        }

        // Update the wanted status and related stars
        private void UpdateWantedState()
        {
            if (RoleplayManager.WantedList.ContainsKey(base.Client.GetHabbo().Id))
            {
                Wanted Junk;
                RoleplayManager.WantedList.TryRemove(base.Client.GetHabbo().Id, out Junk);
            }

            if (base.Client.GetRoleplay().IsWanted || base.Client.GetRoleplay().WantedLevel != 0 || base.Client.GetRoleplay().WantedTimeLeft != 0)
            {
                base.Client.GetRoleplay().IsWanted = false;
                base.Client.GetRoleplay().WantedLevel = 0;
                base.Client.GetRoleplay().WantedTimeLeft = 0;

                UpdateWantedStars();
            }
        }

        // Send updates to the WebSocket about the wanted stars
        private void UpdateWantedStars()
        {
            if (base.Client.GetRoleplay().WebSocketConnection != null)
                PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(base.Client, "compose_wanted_stars|" + base.Client.GetRoleplay().WantedLevel);
        }

        // Decrement the jailed time and update the timer dialogue
        private void DecrementJailedTime()
        {
            if (TimeCount == 60)
            {
                Client.GetRoleplay().JailedTimeLeft--;
                Client.GetRoleplay().UpdateTimerDialogue("Jail-Timer", "decrement", Client.GetRoleplay().JailedTimeLeft, OriginalTime);
                base.Client.SendWhisper($"Tienes {base.Client.GetRoleplay().JailedTimeLeft} minuto(s) para ser liberado", 1);
                TimeCount = 0;
            }
        }

        // Release the player from jail when their time is up
        private void ReleaseFromJail()
        {
            RoleplayManager.Shout(base.Client, "*Se libera de la cárcel ya que han cumplido su condena*", 4);
            UpdateJailLook();
            ResetJailStates();
        }
    }

}