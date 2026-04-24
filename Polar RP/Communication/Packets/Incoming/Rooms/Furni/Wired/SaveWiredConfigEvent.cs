using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Items.Wired;
using Polar.Communication.Packets.Outgoing.Rooms.Furni.Wired;

namespace Polar.Communication.Packets.Incoming.Rooms.Furni.Wired
{
    internal class SaveWiredConfigEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            if (Session == null || Session.GetHabbo() == null)
                return;

            if (!Session.GetHabbo().InRoom)
                return;

            int ItemId = Packet.PopInt();

            

            Room Room = Session.GetHabbo().CurrentRoom;
            if (Room == null)
                return;

            if (!Room.CheckRights(Session, false) && !Room.CheckRights(Session, true))
                return;

            Item SelectedItem = Room.GetRoomItemHandler().GetItem(ItemId);
            if (SelectedItem == null)
                return;

            if (!Session.GetHabbo().CurrentRoom.GetWired().TryGet(ItemId, out var Box))
                return;

            if (Box.Type == WiredBoxType.EffectGiveUserBadge && !Session.GetHabbo().GetPermissions().HasRight("room_item_wired_rewards"))
            {
                Session.SendNotification("No tienes los permisos correctos para hacer esto.");
                return;
            }

            Box.HandleSave(Packet);
            Session.GetHabbo().CurrentRoom.GetWired().SaveBox(Box);
            Session.SendMessage(new HideWiredConfigComposer());
        }
    }
}
