using System;
using Polar.Database.Interfaces;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Handshake;
using Polar.HabboHotel.Users;

namespace Polar.Communication.Packets.Incoming.Handshake
{
    public class UniqueIDEvent : IPacketEvent
    {
        private static int HASH_LENGTH = 64;
        public void Parse(GameClient Session, ClientPacket Packet)
        {
            String storedMachineId = Packet.PopString();
            //Packet.PopString();
            //Packet.PopString();

            if (storedMachineId.Length > HASH_LENGTH)
            {
                storedMachineId = storedMachineId.Substring(0, HASH_LENGTH);
            }


            Session.MachineId = storedMachineId;


            Session.SendMessage(new SetUniqueIdComposer(storedMachineId));
        }
    }
}