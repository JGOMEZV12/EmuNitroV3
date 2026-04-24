using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Rooms;

namespace Polar.Communication.Packets.Incoming.Misc
{
    class UpdateMagicTileEvent : IPacketEvent
    {
        public void Parse(GameClient Session, ClientPacket Packet)
        {
            if (Session != null && Session.GetHabbo() != null)
            {
                int ItemId = Packet.PopInt();
                int HeightToSet = Packet.PopInt();
                Room room = PolarEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
                if (room?.CheckRights(Session) ?? false)
                {
                    Item item = room.GetRoomItemHandler().GetItem(ItemId);
                    if ((item != null && item.GetBaseItem().InteractionType == InteractionType.STACKTOOL))
                    {
                        if (HeightToSet > 5000)
                        {
                            HeightToSet = 5000;
                        }
                        if (HeightToSet < 0)
                        {
                            HeightToSet = 0;
                        }

                        double TotalZ = (double)(HeightToSet / 100.00);

                        item.SetState(item.GetX, item.GetY, TotalZ, item.GetAffectedTiles2);

                        room.SendMessage(new ObjectUpdateComposer(item, item.UserID));
                    }
                }
            }

        }
    }
}
