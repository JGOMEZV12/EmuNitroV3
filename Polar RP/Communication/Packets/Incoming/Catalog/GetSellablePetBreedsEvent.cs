using Polar.Communication.Packets.Outgoing.Catalog;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms.AI;
using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.Items;

namespace Polar.Communication.Packets.Incoming.Catalog
{
    public class GetSellablePetBreedsEvent : IPacketEvent
    {
        public void Parse(GameClient Session, ClientPacket Packet)
        {
            string Type = Packet.PopString();
            string PacketType = "";
            //Console.WriteLine($"GetSellablePetBreeds Type recibido: '{Type}'");
            int PetId = PolarEnvironment.GetGame().GetCatalog().GetPetRaceManager().GetPetId(Type, out PacketType);
            Session.SendMessage(new SellablePetBreedsComposer(PacketType, PetId, PolarEnvironment.GetGame().GetCatalog().GetPetRaceManager().GetRacesForRaceId(PetId)));
        }
    }
}