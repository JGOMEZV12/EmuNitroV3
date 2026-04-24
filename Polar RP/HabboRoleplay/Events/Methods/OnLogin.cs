using System;
using System.Threading;
using System.Collections.Generic;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Items;
using Polar.Communication.Packets.Outgoing.Navigator;
using Polar.Core;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using Polar.HabboHotel.Users.Effects;
using Polar.Communication.Packets.Outgoing.Rooms.Session;
using Polar.HabboHotel.Users;

namespace Polar.HabboRoleplay.Events.Methods
{
    public class OnLogin : IEvent
    {
        /// <summary>
        /// Responds to the event
        /// </summary>
        public void Execute(object Source, object[] Params)
        {
            GameClient Client = (GameClient)Source;
            if (Client == null || Client.GetRoleplay() == null || Client.GetHabbo() == null)
                return;

           

            /*if (Client.GetHabbo().CurrentRoomId != Client.GetHabbo().HomeRoom)
                Client.SendMessage(new RoomForwardComposer(Client.GetHabbo().HomeRoom == 0 ? 1 : Client.GetHabbo().HomeRoom));
            */
            if (Client.GetRoleplay().OriginalOutfit == null)
                Client.GetRoleplay().OriginalOutfit = Client.GetHabbo().Look;

            DeathCheck(Client);
            CuffCheck(Client);
            JailCheck(Client);
            WantedCheck(Client);
            StunCheck(Client);
            NoobCheck(Client);
            PhoneCheck(Client);
            //SocketConnection(Client);
            CheckWS(Client);


            if (!RoleplayManager.PreLoadedRooms)
            {
                RoleplayManager.PreLoadedRooms = true;
                PolarEnvironment.GetGame().GetRoomManager().PreLoadRooms();
            }
        }

        #region Check WS Connect
        public void CheckWS(GameClient Client)
        {
            if (!PolarEnvironment.GetGame().GetWebEventManager().SocketReady(Client, true))
            {
                //Console.WriteLine("Los WebSockets NO conectaron");
                //Client.SendMessage(new BroadcastMessageAlertComposer("¡Los WebSockets NO conectaron!\n\nEstos son necesarios para que pueas visualizar Ventanas Roleplay, tus stats, y muchas más herramientas dentro del servidor.\n\nIntenta reiniciar el Client para intentar reconectar.\nSi el problema persiste, asegurate de no teneer algún programa externo que lo bloquee."));
                //Client.SendNotification("¡Los WebSockets NO conectaron!\n\nEstos son necesarios para que pueas visualizar Ventanas Roleplay, tus stats, y muchas más herramientas dentro del servidor.\n\nIntenta reiniciar el Client para intentar reconectar.\nSi el problema persiste, asegurate de no teneer algún programa externo que lo bloquee.");
                Logging.WriteLine(Client.GetHabbo().Username + " se ha conectado.", ConsoleColor.DarkGreen);
            }
            else
            {
                //Client.SendMessage(new BroadcastMessageAlertComposer("¡Los WebSockets NO. conectaron!\nEstos son necesarios para que pueas visualizar Ventanas Roleplay, tus stats, y muchas más herramientas dentro del servidor.\nIntenta reiniciar el Client para intentar reconectar.\nSi el problema persiste, asegurate de no teneer algún programa externo que lo bloquee."));
                SocketConnection(Client);
                Logging.WriteLine(Client.GetHabbo().Username + " se ha conectado. (+WS)", ConsoleColor.DarkGreen);
            }
        }
        #endregion

        #region SocketConnection
        public void SocketConnection(GameClient Client)
        {
            //Client.GetRoleplay().RefreshStatDialogue();
            Client.GetRoleplay().InitWSDialogues();
            Client.GetRoleplay().InitStatDialogue();
            //Logging.WriteLine(Client.GetHabbo().Username + " [ID:" + Client.GetHabbo().Id + "]" + " Se ha conectado " + "[SalaID:" + Client.GetHabbo().HomeRoom + "] ", ConsoleColor.DarkGreen);
        }
        #endregion

        #region CuffCheck
        public void CuffCheck(GameClient Client)
        {
            if (!Client.GetRoleplay().Cuffed)
                return;
            if (!Client.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("cuff"))
                Client.GetRoleplay().TimerManager.CreateTimer("cuff", 1000, true);
        }
        #endregion

        #region DeathCheck
        /// <summary>
        /// Checks if the client is dead, if so send the user to hospital
        /// </summary>
        public void DeathCheck(GameClient Client)
        {
            if (!Client.GetRoleplay().IsDead)
                return;

            string MyCity = "heticosrp";

            HabboRoleplay.RPRoom.RPRoom Data;
            int HospitalRID = PolarEnvironment.GetGame().GetRPRoomManager().TryToGetHospital(MyCity, out Data);

            if (Client.GetHabbo().CurrentRoomId != HospitalRID)
            {
                RoleplayManager.SendUser(Client, HospitalRID);
                //Client.SendNotification("¡No puedes dejar el hospital mientras estás muerto!");
            }
            RoleplayManager.GetLookAndMotto(Client);
            RoleplayManager.SpawnBeds(Client, "hosptl_bed");

            if (!Client.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("death"))
                Client.GetRoleplay().TimerManager.CreateTimer("death", 1000, true);
        }
        #endregion

        #region JailCheck
        /// <summary>
        /// Checks if the client is jailed, if so send the user to jail
        /// </summary>
        public void JailCheck(GameClient Client)
        {
            if (!Client.GetRoleplay().IsJailed)
                return;

            if (JailbreakManager.JailbreakActivated)
            {
                Client.GetRoleplay().Jailbroken = true;
                Client.SendNotification("¡Alguien ha iniciado un jailbreak mientras estabas offline! ¡Corre mejor antes de que te pillen!");
                return;
            }

            if (Client.GetRoleplay().IsWanted || Client.GetRoleplay().WantedLevel != 0 || Client.GetRoleplay().WantedTimeLeft != 0)
            {
                Client.GetRoleplay().IsWanted = false;
                Client.GetRoleplay().WantedLevel = 0;
                Client.GetRoleplay().WantedTimeLeft = 0;
            }

            string MyCity = "heticosrp";

            HabboRoleplay.RPRoom.RPRoom Data;
            int ToRoomId = PolarEnvironment.GetGame().GetRPRoomManager().TryToGetJail(MyCity, out Data);

            if (Client.GetHabbo().HomeRoom != ToRoomId)
                Client.GetHabbo().HomeRoom = ToRoomId;

            RoleplayManager.SendUser(Client, ToRoomId, "");

            if (!Client.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("jail"))
                Client.GetRoleplay().TimerManager.CreateTimer("jail", 1000, true);
           
        }
        #endregion

        #region StuNCheck
        /// <summary>
        /// Checks if the client is jailed, if so send the user to jail
        /// </summary>
        public void StunCheck(GameClient Client)
        {
            if (Client.GetRoleplay().IsStun == false)
                return;

            if (Client.GetRoleplay().TryGetCooldown("stun"))
            {
                Client.GetRoleplay().IsStun = true;
                Client.GetRoleplay().IsJailed = true;

                if (!Client.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("jail"))
                    Client.GetRoleplay().TimerManager.CreateTimer("jail", 1000, true);
            }
         
        }
        #endregion

        #region Wanted Check
        /// <summary>
        /// Checks if the user is wanted
        /// </summary>
        /// <param name="Client"></param>
        public void WantedCheck(GameClient Client)
        {
            // WS Wanted Stars
            if (Client.GetRoleplay().WebSocketConnection != null)
            {
                if (Client.GetRoleplay().WantedLevel > 0)
                {
                    Wanted NewWanted = new Wanted(Convert.ToUInt32(Client.GetHabbo().Id), "Desconocida", Client.GetRoleplay().WantedLevel);
                    Client.GetRoleplay().TimerManager.CreateTimer("wanted", 1000, false);
                    RoleplayManager.WantedList.TryAdd(Client.GetHabbo().Id, NewWanted);
                    PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(Client, "compose_wanted_stars|" + Client.GetRoleplay().WantedLevel);
                }
            }

            if (!Client.GetRoleplay().IsJailed)
                return;
            if (!Client.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("jail"))
                Client.GetRoleplay().TimerManager.CreateTimer("jail", 1000, true);
        }
        #endregion

        #region NoobCheck

        /// <summary>
        /// Checks if the client is jailed, if so send the user to jail
        /// </summary>
        public void NoobCheck(GameClient Client)
        {
            if (!Client.GetRoleplay().IsNoob)
                return;

            Client.GetRoleplay().TimerManager.CreateTimer("noob", 1000, true);
        }
        #endregion

        #region PhoneCheck
        /// <summary>
        /// Checks if the client is dead, if so send the user to hospital
        /// </summary>
        /// 
        public void PhoneCheck(GameClient Client)
        {
            if (Client.GetRoleplay().Phone <= 0)
                return;

            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_phone", "load_apps");
            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_phone", "show_button");
        }
        #endregion
    }
}