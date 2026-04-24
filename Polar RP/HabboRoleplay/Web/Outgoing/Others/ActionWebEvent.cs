using ConnectionManager;
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Polar.Core;
using Polar.HabboHotel.GameClients;


namespace Polar.HabboHotel.Roleplay.Web.Incoming.Others
{
    /// <summary>
    /// ATMWebEvent class.
    /// </summary>
    class ActionWebEvent : IWebEvent
    {
        /// <summary>
        /// Executes socket data.
        /// </summary>
        /// <param name="Client"></param>
        /// <param name="Data"></param>
        /// <param name="Socket"></param>
        public void Execute(GameClient Session, string Data, ConnectionInformation Socket)
        {
            if (!PolarEnvironment.GetGame().GetWebEventManager().SocketReady(Session, true) || !PolarEnvironment.GetGame().GetWebEventManager().SocketReady(Socket))
                return;

            string Action = (Data.Contains(',') ? Data.Split(',')[0] : Data);
            string[] ReceivedData = Data.Split(',');
            string User = ReceivedData[1];

            switch (Action.ToLower())
            {

                #region Abrazar
                //case "abr":
                case "1":
                    {
                        PolarEnvironment.GetGame().GetChatManager().GetCommands().Parse(Session, ":abrazar " + User);
                    }
                    break;
                #endregion

                #region Besar
                //case "kiss":
                case "2":
                    {
                        PolarEnvironment.GetGame().GetChatManager().GetCommands().Parse(Session, ":besar " + User);
                    }
                    break;
                #endregion

                #region Nalgada
                //case "nalguear":
                case "6":
                    {
                        PolarEnvironment.GetGame().GetChatManager().GetCommands().Parse(Session, ":nalgada " + User);
                    }
                    break;
                #endregion

                #region Cachetada
                //case "abofetear":
                case "3":
                    {
                        PolarEnvironment.GetGame().GetChatManager().GetCommands().Parse(Session, ":cachetada " + User);
                    }
                    break;
                #endregion

                #region Golpear
                //case "golpear":
                case "8":
                    {
                        PolarEnvironment.GetGame().GetChatManager().GetCommands().Parse(Session, ":golpe " + User);
                    }
                    break;

                #endregion

                #region Robar
                //case "robar":
                case "7":
                    {
                        PolarEnvironment.GetGame().GetChatManager().GetCommands().Parse(Session, ":robar " + User);
                    }
                    break;
                #endregion

                #region Violar
                //case "violar":
                case "5":
                    {
                        PolarEnvironment.GetGame().GetChatManager().GetCommands().Parse(Session, ":violar " + User);
                    }
                    break;
                #endregion

                #region Escupir
                //case "escupir":
                case "4":
                    {
                        PolarEnvironment.GetGame().GetChatManager().GetCommands().Parse(Session, ":escupir " + User);
                    }
                    break;
                #endregion

                default:
                    break;
            }

        }
    }
}