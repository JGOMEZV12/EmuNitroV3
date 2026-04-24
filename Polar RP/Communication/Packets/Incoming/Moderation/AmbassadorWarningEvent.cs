using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using Polar.HabboHotel.GameClients;

namespace Polar.Communication.Packets.Incoming.Moderation
{
    internal class AmbassadorWarningEvent : IPacketEvent
    {
        public void Parse(GameClient Session, ClientPacket Packet)
        {
            if (Session == null || Session.GetHabbo() == null || !Session.GetHabbo().GetPermissions().HasRight("ambassador"))
                return;

            int UserId = Packet.PopInt();

            GameClient Client = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(UserId);
            if (Client == null)
                return;
            else if (Client.GetHabbo().GetPermissions().HasRight("mod_tool"))
                return;

            Session.GetHabbo().Sussurrando = UserId;
            PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(Session, "sussurrar;activo");
            Client.SendMessage(new RoomNotificationComposer("${notification.ambassador.alert.warning.title}", "${notification.ambassador.alert.warning.message}", "", "ok", "event:"));
        }
    }
}
