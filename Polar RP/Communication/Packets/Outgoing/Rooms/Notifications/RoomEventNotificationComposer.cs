using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms;

namespace Polar.Communication.Packets.Outgoing.Rooms.Notifications
{
    internal class RoomEventNotificationComposer : ServerPacket
    {
        public string Type { get; }
        public string Message { get; }
        public GameClient Session { get; }
        public Room Room { get; }
        public int Amount { get; }
        public RoomEventNotificationComposer(GameClient session, string type, string message, int amount = 0, Room room = null)
            : base(ServerPacketHeader.RoomNotificationMessageComposer)
        {
            Session = session;
            Message = message;
            Type = type;
            Room = room;
            Amount = amount;
            Compose(this);


        }
        public void Compose(ServerPacket packet)
        {

                if (Type == "death")
                {
                    packet.WriteString("room_death_axe"); // Image
                    packet.WriteInteger(4);
                    packet.WriteString("title");
                    packet.WriteString("Oops... ¡Usted murió!"); // Title
                    packet.WriteString("message");
                    packet.WriteString("Parece que acabas de morir!\n\n Actualmente está siendo transportado al hospital."); // Message
                    packet.WriteString("linkUrl");
                    packet.WriteString("event:"); // Should clicking the button do something?
                    packet.WriteString("linkTitle");
                    packet.WriteString("Entendido"); // Button Message
                }
                else if (Type == "jail")
                {
                    packet.WriteString("room_jail_prison"); // Image
                    packet.WriteInteger(4);
                    packet.WriteString("title");
                    packet.WriteString("Vaya ... ¡Usted ha sido arrestado!"); // Title
                    packet.WriteString("message");
                    packet.WriteString("Has sido arrestado por " + Session.GetHabbo().Username + " por " + Amount + " minuto(s)!\n\n En este momento estás siendo transportado a la cárcel."); // Message
                    packet.WriteString("linkUrl");
                    packet.WriteString("event:"); // Should clicking the button do something?
                    packet.WriteString("linkTitle");
                    packet.WriteString("Entendido"); // Button Message
                }
                else if (Type == "brawl")
                {
                    packet.WriteString("room_kick_cannonball"); // Image
                    packet.WriteInteger(4);
                    packet.WriteString("title");
                    packet.WriteString("Knocked Out!"); // Title
                    packet.WriteString("message");
                    packet.WriteString("Haz sido eliminado\n\n Suerte la próxima"); // Message
                    packet.WriteString("linkUrl");
                    packet.WriteString("event:"); // Should clicking the button do something?
                    packet.WriteString("linkTitle");
                    packet.WriteString("Entendido"); // Button Message
                }
                else
                {
                    packet.WriteString("eventoheticos"); // Image
                    packet.WriteInteger(4);
                    packet.WriteString("title");
                    packet.WriteString(PolarEnvironment.GetGame().GetLanguageLocale().TryGetValue("alert_event_title"));
                    packet.WriteString("message");
                    packet.WriteString("<b>" + Session.GetHabbo().Username + "</b> Está organizando algunos eventos. ¡Los Premios y Puntos de Evento serán entregados a los ganadores!\n\n" +
                        "Detalles: " + Message + "\n\n<i>Estos eventos son supervisados por el staff de " + PolarEnvironment.GetConfig().data["hotel.name"] + ".</i>");
                    packet.WriteString("linkUrl");
                    packet.WriteString("event:navigator/goto/" + Session.GetHabbo().CurrentRoomId);
                    packet.WriteString("linkTitle");
                    packet.WriteString("Ir a '" + Session.GetHabbo().CurrentRoom.Name + " (" + Session.GetHabbo().CurrentRoom.Id + ")'!");
                }
            }
    }
}
