using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Games;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.Communication.Packets.Outgoing.GameCenter
{
    internal class LoadGameComposer : ServerPacket
    {
        public GameData GameData { get; }
        public GameClient Session { get; }
        public string SSOTicket { get; }

        public LoadGameComposer(GameClient Session, GameData GameData, string SSOTicket)
            : base(ServerPacketHeader.LoadGameMessageComposer)
        {
            this.GameData = GameData;
            this.SSOTicket = SSOTicket;
            this.Session = Session;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(GameData.GameId);
            packet.WriteString(Session.GetHabbo().Id.ToString());
            packet.WriteString(GameData.ResourcePath + GameData.GameSWF);
            packet.WriteString("best");
            packet.WriteString("showAll");
            packet.WriteInteger(60);//FPS?
            packet.WriteInteger(10);
            packet.WriteInteger(8);
            packet.WriteInteger(6);//Asset count
            packet.WriteString("assetUrl");
            packet.WriteString(GameData.ResourcePath + GameData.GameAssets);
            packet.WriteString("habboHost");
            packet.WriteString("http://fuseus-private-httpd-fe-1");
            packet.WriteString("accessToken");
            packet.WriteString(SSOTicket);
            packet.WriteString("gameServerHost");
            packet.WriteString(GameData.GameServerHost); 
            packet.WriteString("gameServerPort");
            packet.WriteString(GameData.GameServerPort);
            packet.WriteString("socketPolicyPort");
            packet.WriteString(GameData.GameServerHost);
        }
    }
}
