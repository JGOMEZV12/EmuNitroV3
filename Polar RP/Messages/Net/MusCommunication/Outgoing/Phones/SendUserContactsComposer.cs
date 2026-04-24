
using Newtonsoft.Json;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Users.Messenger;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Polar.Messages.Net.MusCommunication.Outgoing.Phones
{
    class SendUserContactsComposer : MusPacketEvent
    {
        // ID can be APPID or WebID (Check if user have acces to this ID)
        public SendUserContactsComposer(int ID = 0, int UserID = 0)
        {
            List<ErrorStructure> E = new List<ErrorStructure>();
            E.Add(new ErrorStructure { Error = "true", Code = "5000", Message = "No se encontraron datos para enviar." });

            // Incoming Information to send to Client
            PacketName = "event_sendusercontacts";
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

                List<MessengerBuddy> Contacts = Client.GetHabbo().GetMessenger().GetFriends().ToList();
                foreach (MessengerBuddy Buddy in Contacts)
                {
                    if (Buddy != null)
                    {
                        string Number = PolarEnvironment.GetGame().GetClientManager().GetNumberById(Buddy.UserId);
                        L.Add(new ResultStructure { Username = Buddy.mUsername, Look = Buddy.mLook , PhoneNumber  = Number});
                    }
                }

                PacketData = JsonConvert.SerializeObject(L);
            }
        }

        private class ResultStructure
        {
            public string Username { get; set; }
            public string Look { get; set; }
            public string PhoneNumber { get; set; }
        }

        private class ErrorStructure
        {
            public string Error { get; set; }
            public string Code { get; set; }
            public string Message { get; set; }
        }
    }
}
