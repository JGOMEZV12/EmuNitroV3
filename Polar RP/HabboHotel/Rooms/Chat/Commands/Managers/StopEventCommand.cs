using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;
using Polar.Core;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Managers
{
    internal class StopEventCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get
            {
                return "command_stop_event";
            }
        }
        public string Parameters
        {
            get
            {
                return "%name%";
            }
        }
        public string Description
        {
            get
            {
                return "para un evento";
            }
        }
        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1 && Params[0] != "checklottery")
            {
                Session.SendWhisper("Por favor ingrese un tipo de evento!", 1);
                return;
            }

            string Message;

            if (Params[0] == "checklottery")
                Message = "lottery";
            else
                Message = Params[1].ToString().ToLower();

            switch (Message)
            {
                #region Purga

                case "purga":
                    {
                       if (!RoleplayManager.PurgeStarted)
                       {
                           Session.SendWhisper("¡Se ha detenido la purga!");
                           break;
                       }

                       try
                       {
                           lock (PolarEnvironment.GetGame().GetClientManager().GetClients)
                           {
                               foreach (GameClient client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                               {
                                   if (client == null)
                                       continue;

                                   if (client.GetHabbo() == null)
                                       continue;

                                   client.SendWhisper("Se ha detenido la purga, esperamos la hayas disfrutado", 34);
                               }
                           }

                           RoleplayManager.PurgeStarted = false;
                       }
                       catch(Exception e)
                       {
                           Logging.LogCriticalException("Errores al detener purga: " + e);
                       }
                       break;
                   }

                #endregion

                #region Lottery
                case "lottery":
                    {
                        if (!LotteryManager.LotteryFull())
                        {
                            Session.SendWhisper("¡No todos los boletos han sido vendidos! [" + LotteryManager.LotteryTickets.Count + "/" + LotteryManager.TicketLimit + "]", 1);
                            break;
                        }
                        else
                        {
                            int Winner = LotteryManager.GetWinner();
                            LotteryManager.GivePrize(Winner);
                            LotteryManager.ClearLottery();
                            Session.SendWhisper("¡La Lotería ha sido despejada con éxito!", 1);
                        }

                        break;
                    }
                #endregion


                default:
                    {
                        Session.SendWhisper("¡Este tipo de evento no existe o está deshabilitado!", 1);
                        break;
                    }
            }
        }
    }
}
