using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Groups;
using Polar.Communication.Packets.Outgoing.Groups;

namespace Polar.Communication.Packets.Incoming.Groups
{
    internal class GetGroupInfoEvent : IPacketEvent
    {
        public void Parse(GameClient session, ClientPacket packet)
        {
            int groupId = packet.PopInt();
            bool newWindow = packet.PopBoolean();

            Group group = null;


            if (groupId < 1000)
                group = GroupManager.GetJob(groupId);
            else
                group = GroupManager.GetGang(groupId);

            if (group == null)
                return;

            //session.SendMessage(new GroupInfoComposer(group, session, newWindow));     
        }
    }
}
