using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;


namespace Polar.Communication.Packets.Incoming.Misc
{
    internal class SetFriendBarStateEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            bool blocked = true;

            if (blocked)
                return;

            if (Session == null || Session.GetHabbo() == null)
                return;

            //Session.GetHabbo().FriendbarState = FriendBarStateUtility.GetEnum(Packet.PopInt());
            //Session.SendMessage(new SoundSettingsComposer(Session.GetHabbo().ClientVolume, Session.GetHabbo().ChatPreference, Session.GetHabbo().AllowMessengerInvites, Session.GetHabbo().FocusPreference, FriendBarStateUtility.GetInt(Session.GetHabbo().FriendbarState)));
        }
    }
}
