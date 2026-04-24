using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.Communication.Packets.Outgoing.Rooms.Session;
using Polar.Communication.Packets.Outgoing.Messenger;

namespace Polar.Communication.Packets.Incoming.Messenger
{
    internal class FindNewFriendsEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            // Fix
            Session.SendNotification("Esta función ha sido deshabilitada por los desarrolladores.");
            return;
            /*
            Room Instance = PolarEnvironment.GetGame().GetRoomManager().TryGetRandomLoadedRoom();

            if (Instance != null)
            {
                Session.SendMessage(new FindFriendsProcessResultComposer(true));
                Session.SendMessage(new RoomForwardComposer(Instance.Id));
            }
            else
            {
                Session.SendMessage(new FindFriendsProcessResultComposer(false));
            }*/
            
        }
    }
}
