
using Newtonsoft.Json;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Users.Messenger;
using Polar.HabboRoleplay.PhoneChat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Polar.Messages.Net.MusCommunication.Outgoing.Web
{
    class SendUserRoomsComposer : MusPacketEvent
    {
        // Listo, ya la parte del emu está, solo llama el packet en api
        public SendUserRoomsComposer()
        {
            List<ErrorStructure> E = new List<ErrorStructure>();
            E.Add(new ErrorStructure { Error = "true", Code = "5000", Message = "No se encontraron datos para enviar." });

            // Incoming Information to send to Client
            PacketName = "event_getusersrooms";
            PacketData = JsonConvert.SerializeObject(E);

            List<ResultStructure> L = new List<ResultStructure>();
            // Vamos a hacer mientras que logre responder, ahorita volvemos aqui a reestructurar la respuesta.
            SubArrayStructure SubArray = new SubArrayStructure { id = 1, roomname = "Hola", users = 5};

            L.Add(new ResultStructure { str = "Rooms = {id = 1, roomname = 'Hola', users = 5}, {id = 5, roomname = 'sala', users = 2}"});// Para mandar un sub array no es asi
            // Creo que tendrías que crear tro ResultStructure más eso es lo que estaba pensando y meterlo bajo rooms =  result2 exacto
            PacketData = JsonConvert.SerializeObject(L);
        }

        private class ResultStructure
        {
            public string str { get; set; }
        }

        private class SubArrayStructure
        {
            public int id { get; set; }
            public string roomname { get; set; }
            public int users { get; set; }
        }

        private class ErrorStructure
        {
            public string Error { get; set; }
            public string Code { get; set; }
            public string Message { get; set; }
        }
    }
}
