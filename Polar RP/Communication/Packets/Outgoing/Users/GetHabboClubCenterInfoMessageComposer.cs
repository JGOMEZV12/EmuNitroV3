using System;
using Polar.HabboHotel.GameClients;

namespace Polar.Communication.Packets.Outgoing.Inventory.Purse
{
    internal class GetHabboClubCenterInfoMessageComposer : ServerPacket
    {
        public GameClient Session { get; }
        public GetHabboClubCenterInfoMessageComposer(GameClient session) : base(ServerPacketHeader.HabboClubCenterInfoMessageComposer)
        {
            this.Session = session;
            this.Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            origin = origin.AddSeconds(Session.GetHabbo().AccountCreated).ToLocalTime();

            string time = origin.ToString();

            packet.WriteInteger(2005);//streakduration in days 
            packet.WriteString(time);//joindate 
            packet.WriteInteger(0);
            packet.WriteInteger(0);//this should be a double 
            packet.WriteInteger(0);//unused 
            packet.WriteInteger(0);//unused 
            packet.WriteInteger(10);//spentcredits 
            packet.WriteInteger(20);//streakbonus 
            packet.WriteInteger(10);//spentcredits 
            packet.WriteInteger(60);//next pay in minutes
        }
    }
}