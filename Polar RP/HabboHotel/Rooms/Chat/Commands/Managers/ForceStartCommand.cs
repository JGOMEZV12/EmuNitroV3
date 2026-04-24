using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;
using System.Threading;
using Polar.Core;
using Polar.Communication.Packets.Outgoing.Guides;
using Polar.HabboHotel.Groups;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Managers
{
    internal class ForceStartCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get
            {
                return "command_force_start";
            }
        }
        public string Parameters
        {
            get
            {
                return "%event%";
            }
        }
        public string Description
        {
            get
            {
                return "Forzr inicio del evento deseado!";
            }
        }
        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("Introduzca un tipo de evento!", 1);
                return;
            }

            string Message = Params[1].ToString().ToLower();

            switch (Message)
            {
                #region Purge

                case "purge":
                case "purga":
                    {
                        if (RoleplayManager.PurgeStarted)
                        {
                            Session.SendWhisper("El evento de purga ya se ha iniciado!");
                            break;
                        }

                        try
                        {
                            lock (PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                            {
                                foreach (GameClient client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                                {
                                    if (client == null)
                                        continue;

                                    if (client.GetHabbo() == null)
                                        continue;

                                    if (client.GetHabbo().CurrentRoom == null)
                                        continue;

                                    if (client.GetRoomUser() == null)
                                        continue;                                    

                                    if (client.GetRoleplay() == null)
                                        continue;

                                    int Counter = 11;

                                    new Thread(() =>
                                    {
                                        while (Counter > 0)
                                        {
                                            if (client != null)
                                            {
                                                if (Counter == 11)
                                                    client.SendWhisper("El tiempo de purga va a comenzar momentáneamente!", 1);
                                                else
                                                    client.SendWhisper("El tiempo de purga comenzará en " + Counter + " segundos!", 1);
                                            }

                                            Counter--;
                                            Thread.Sleep(1000);

                                            if (Counter == 0)
                                            {
                                                client.SendWhisper("¡El modo de purga ha sido activado! Todos los delitos son legales.", 34);
                                                RoleplayManager.WantedList.Clear();

                                                if (client.GetRoleplay().IsJailed)
                                                {
                                                    client.GetRoleplay().IsJailed = false;
                                                    client.GetRoleplay().JailedTimeLeft = 0;
                                                }

                                                if (GroupManager.HasJobCommand(client, "guide"))
                                                {
                                                    WorkManager.RemoveWorkerFromList(client);
                                                    client.GetRoleplay().IsWorking = false;
                                                    client.GetHabbo().Poof();

                                                    PolarEnvironment.GetGame().GetGuideManager().RemoveGuide(client);
                                                    client.SendMessage(new HelperToolConfigurationComposer(client));

                                                    #region End Existing Calls
                                                    if (client.GetRoleplay().GuideOtherUser != null)
                                                    {
                                                        client.GetRoleplay().GuideOtherUser.SendMessage(new OnGuideSessionDetachedComposer(0));
                                                        client.GetRoleplay().GuideOtherUser.SendMessage(new OnGuideSessionDetachedComposer(1));
                                                        if (client.GetRoleplay().GuideOtherUser.GetRoleplay() != null)
                                                        {
                                                            client.GetRoleplay().GuideOtherUser.GetRoleplay().Sent911Call = false;
                                                            client.GetRoleplay().GuideOtherUser.GetRoleplay().GuideOtherUser = null;
                                                        }

                                                        client.GetRoleplay().GuideOtherUser = null;
                                                        client.SendMessage(new OnGuideSessionDetachedComposer(0));
                                                        client.SendMessage(new OnGuideSessionDetachedComposer(1));
                                                    }
                                                    #endregion

                                                }
                                                RoleplayManager.PurgeStarted = true; // let the fun begin
                                            }
                                        }

                                    }).Start();
                                }
                            }
                            break;
                        }
                        catch(Exception e)
                        {
                            lock (PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                            {
                                foreach (var client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                                {
                                    if (client == null)
                                        continue;

                                    if (client.GetHabbo() == null)
                                        continue;

                                    if (client.GetHabbo().CurrentRoom == null)
                                        continue;

                                    if (client.GetRoomUser() == null)
                                        continue;

                                    client.SendWhisper("Sorry, an error occoured whilst starting 'Purge Time' - it will be investigated by HoloRP's technician team!");
                                }
                            }

                            Logging.LogRPGamesError("Error in starting purge void: " + e);
                            break;
                        }
                    }

                #endregion

                default:
                    {
                        Session.SendWhisper("Este evento no existe o esta desactivado!", 1);
                        break;
                    }
            }
        }
    }
}
