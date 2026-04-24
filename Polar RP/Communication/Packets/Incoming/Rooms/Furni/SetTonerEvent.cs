using System;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Items;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.Database.Interfaces;

namespace Polar.Communication.Packets.Incoming.Rooms.Furni
{
    internal class SetTonerEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            Room Room;
            if (!PolarEnvironment.GetGame().GetRoomManager().TryGetRoom(Session.GetHabbo().CurrentRoomId, out Room))
                return;

            if (!Room.CheckRights(Session, false))
                return;

            int itemId = Packet.PopInt();
            Item Item = Room.GetRoomItemHandler().GetItem(itemId);
            if (Item == null)
                return;

            int hue = Packet.PopInt() % 256;
            int saturation = Packet.PopInt() % 256;
            int brightness = Packet.PopInt() % 256;

            string[] extraParts = Item.ExtraData.Split(':');
            string prefix = extraParts.Length > 0 ? extraParts[0] : "";
            Item.ExtraData = $"{prefix}:{hue}:{saturation}:{brightness}";

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("UPDATE room_items_toner SET enabled = '1', data1=@data1 ,data2=@data2,data3=@data3 WHERE id=" + Item.Id + " LIMIT 1");
                dbClient.AddParameter("data1", hue);
                dbClient.AddParameter("data3", saturation);
                dbClient.AddParameter("data2", brightness);
                dbClient.RunQuery();
            }

            Room.TonerData.Hue = hue;
            Room.TonerData.Saturation = saturation;
            Room.TonerData.Lightness = brightness;
            Room.TonerData.Enabled = 1;
            Room.SendMessage(new ObjectUpdateComposer(Item, Item.UserID));
            Item.UpdateState();
        }
    }
}