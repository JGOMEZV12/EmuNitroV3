using Polar.Communication.Packets.Outgoing.HabboCamera;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets;
using Polar;

namespace Akiled.Communication.Packets.Incoming.HabboCamera
{
    class RequestCameraConfigurationEvent : IPacketEvent
    {
        public void Parse(GameClient Session, ClientPacket Packet) =>
           Session.SendMessage(new SetCameraPicturePriceComposer(Convert.ToInt32(PolarEnvironment.GetConfig().data["camera.price.coins"]), Convert.ToInt32(PolarEnvironment.GetConfig().data["camera.price.duckets"]), Convert.ToInt32(PolarEnvironment.GetConfig().data["camera.price.publish"])));
    }
}
