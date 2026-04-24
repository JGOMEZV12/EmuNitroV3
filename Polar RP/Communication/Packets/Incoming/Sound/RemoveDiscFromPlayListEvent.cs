using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.Communication.Packets.Outgoing.Sound;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;

namespace Polar.Communication.Packets.Incoming.Sound
{
    class RemoveDiscFromPlayListEvent : IPacketEvent
    {
        public void Parse(GameClient Session, ClientPacket Packet)
        {
            var room = Session.GetHabbo().CurrentRoom;
            if (!room.CheckRights(Session))
                return;
            var itemindex = Packet.PopInt();

            var trax = room.GetTraxManager();
            if (trax.Playlist.Count < itemindex)
            {
                goto error;
            }

            var item = trax.Playlist[itemindex];
            if (!trax.RemoveDisc(item))
                goto error;

            return;
        error:
            Session.SendMessage(new RoomNotificationComposer("", "�Oops! No ha sido posible quitar el disco", "error", "", ""));
        }
    }
}
