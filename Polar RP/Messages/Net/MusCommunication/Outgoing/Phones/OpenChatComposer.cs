
using Newtonsoft.Json;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Users.Messenger;
using Polar.HabboRoleplay.PhoneChat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Polar.Messages.Net.MusCommunication.Outgoing.Phones
{
    class OpenChatComposer : MusPacketEvent
    {
        // ID can be APPID or WebID (Check if user have acces to this ID)
        public OpenChatComposer(int ID = 0, int UserID = 0, string ChatName = null)
        {
            List<ErrorStructure> E = new List<ErrorStructure>();
            E.Add(new ErrorStructure { Error = "true", Code = "5000", Message = "No se encontraron datos para enviar." });

            // Incoming Information to send to Client
            PacketName = "event_getuser";
            PacketData = JsonConvert.SerializeObject(E);

            List<ResultStructure> L = new List<ResultStructure>();

            GameClient Client = null;
            if (PolarEnvironment.GetGame() != null && PolarEnvironment.GetGame().GetClientManager() != null)
                Client = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(UserID);

            if (Client != null)
            {
                if (Client.GetRoleplay().Phone <= 0)
                    return;

                if (!Client.GetRoleplay().OwnedPhonesApps.ContainsKey(ID))
                    return;

                #region Execute
                // Filtramos
                ChatName = Regex.Replace(ChatName, "<(.|\\n)*?>", string.Empty);
                string DisplayName = ChatName;
                int ChatUserID = 0;
                string SendData = "";
                #region Messages
                List<PhoneChat> Chattings = PolarEnvironment.GetGame().GetPhoneChatManager().GetPhoneChatsByMyID(Client.GetHabbo().Id);
                foreach (PhoneChat PChattings in Chattings)
                {
                    if (PChattings.EmisorName == ChatName || PChattings.ReceptorName == ChatName)
                    {
                        if (PChattings.EmisorName != Client.GetHabbo().Username)
                        {
                            if (ChatUserID == 0)
                                ChatUserID = PChattings.EmisorId;

                            SendData += "<div class=\"Msg_Container\">";
                            SendData += "<div class=\"Msg_From\" title=\"" + PChattings.TimeStamp + "\">" + Regex.Replace(PChattings.Msg, "::br::", "<br>") + "</div>";
                            SendData += "</div>";
                        }
                        else
                        {
                            if (ChatUserID == 0)
                                ChatUserID = PChattings.ReceptorId;

                            SendData += "<div class=\"Msg_Container\">";
                            SendData += "<div class=\"Msg_To\" title=\"" + PChattings.TimeStamp + "\">" + Regex.Replace(PChattings.Msg, "::br::", "<br>") + "</div>";
                            SendData += "</div>";
                        }
                    }
                }
                #endregion

                // Verificamos si el chat es un Amigo
                List<MessengerBuddy> Friend = (from TG in Client.GetHabbo().GetMessenger().GetFriends().ToList() where TG != null && TG.mUsername == ChatName select TG).ToList();
                if (Friend == null || Friend.Count <= 0)
                {
                    // Si no está en la lista de amigos buscamos su número.
                    DisplayName = PolarEnvironment.GetGame().GetClientManager().GetNumberById(ChatUserID);
                }

                Client.GetRoleplay().LastChat = ChatName;

                L.Add(new ResultStructure { DisplayName = DisplayName, Bubbles = SendData });
                #endregion

                PacketData = JsonConvert.SerializeObject(L);
            }
        }

        private class ResultStructure
        {
            public string DisplayName { get; set; }
            public string Bubbles { get; set; }
        }

        private class ErrorStructure
        {
            public string Error { get; set; }
            public string Code { get; set; }
            public string Message { get; set; }
        }
    }
}
