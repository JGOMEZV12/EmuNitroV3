using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Items;
using Polar.Communication.Packets.Outgoing.Rooms.FloorPlan;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;

namespace Polar.Communication.Packets.Incoming.Rooms.FloorPlan
{
    internal class FloorPlanEditorRoomPropertiesEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            if (!Session.GetHabbo().InRoom)
                return;

            Room Room = Session.GetHabbo().CurrentRoom;

            DynamicRoomModel model = Room?.GetGameMap().Model;
            if (model == null)
                return;


            List<Point> Squares = new List<Point>();
            Room.GetRoomItemHandler().GetFloor.ToList().ForEach(Item =>
            {
                Item.GetCoords.ForEach(Point =>
                {
                    if (!Squares.Contains(Point))
                        Squares.Add(Point);
                });
            });

            ICollection<Item> FloorItems = Room.GetRoomItemHandler().GetFloor;

            Session.SendMessage(new FloorPlanFloorMapComposer(Squares));
            Session.SendMessage(new FloorPlanSendDoorComposer(Room.GetGameMap().Model.DoorX, Room.GetGameMap().Model.DoorY, Room.GetGameMap().Model.DoorOrientation));
            Session.SendMessage(new RoomVisualizationSettingsComposer(Room.WallThickness, Room.FloorThickness, PolarEnvironment.EnumToBool(Room.Hidewall.ToString())));

            Squares.Clear();
            Squares = null;
        }
    }
}
