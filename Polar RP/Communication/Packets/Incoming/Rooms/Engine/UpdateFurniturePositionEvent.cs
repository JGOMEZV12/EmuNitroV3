using System.Linq;
using System.Collections.Generic;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Items;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;

namespace Polar.Communication.Packets.Incoming.Rooms.Engine
{
    internal class UpdateFurniturePositionEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            if (Session == null || Session.GetHabbo() == null || !Session.GetHabbo().InRoom)
                return;

            int ItemId = Packet.PopInt();
            if (ItemId == 0)
                return;

            Room Room;

            if (PolarEnvironment.GetGame() == null || PolarEnvironment.GetGame().GetRoomManager() == null ||
                !PolarEnvironment.GetGame().GetRoomManager().TryGetRoom(Session.GetHabbo().CurrentRoomId, out Room) || Room == null)
                return;


            int x = Packet.PopInt();
            int y = Packet.PopInt();
            double z = Packet.PopInt() / 10000.0;
            int Rotation = Packet.PopInt();

            bool HasRights = false;
          
            Item Item = Room.GetRoomItemHandler()?.GetItem(ItemId);
            if (Item == null || Item.GetBaseItem() == null)
                return;
            if (Room.GetRoomItemHandler() == null)
                return;

            Item = Room.GetRoomItemHandler().GetItem(ItemId);
            if (Item == null)
                return;

            var OldSquares = Item.GetAffectedTiles;
            List<RoomUser> UsersToUpdate = new List<RoomUser>();

            foreach (var square in OldSquares)
            {
                if (Room.GetGameMap() != null && Room.GetGameMap().SquareHasUsers(square.X, square.Y))
                {
                    foreach (var user in Room.GetGameMap().GetRoomUsers(square))
                    {
                        if (!user.IsWalking && !UsersToUpdate.Contains(user))
                        {
                            UsersToUpdate.Add(user);
                        }
                    }
                }
            }

            if ((Item.GetBaseItem().InteractionType == InteractionType.PURCHASABLE_CLOTHING || Item.GetBaseItem().InteractionType == InteractionType.CRAFTING || Item.GetBaseItem().InteractionType == InteractionType.CRAFTING || Item.GetBaseItem().ItemName.ToLower() == "fxbox_fx192") && !Session.GetHabbo().GetPermissions().HasRight("mod_tool"))
            {
                Session.SendMessage(new ObjectUpdateComposer(Item, Session.GetHabbo().Id));
                return;
            }
            else
            {
                if (!Room.GetRoomItemHandler().SetFloorItem(Session, Item, x, y, Rotation, false, false, true, false, true))
                {
                    Room.SendMessage(new ObjectUpdateComposer(Item, Session.GetHabbo().Id));
                    return;
                }

                // Aplicar la Z que vino del packet
                Item.SetState(Item.GetX, Item.GetY, z, Gamemap.GetAffectedTiles(Item.GetBaseItem().Length, Item.GetBaseItem().Width, Item.GetX, Item.GetY, Item.Rotation));
                Room.GetRoomItemHandler().UpdateItem(Item);
                Room.SendMessage(new ObjectUpdateComposer(Item, Session.GetHabbo().Id));

                using (var db = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    db.SetQuery("UPDATE `items` SET `z` = @z WHERE `id` = @id");
                    db.AddParameter("z", z);
                    db.AddParameter("id", Item.Id);
                    db.RunQuery();
                }
            }

            if (!Item.GetBaseItem().IsSeat)
            {
                foreach (var square in Item.GetAffectedTiles)
                {
                    if (Room.GetGameMap().SquareHasUsers(square.X, square.Y))
                    {
                        foreach (var user in Room.GetGameMap().GetRoomUsers(square))
                        {
                            if (!user.IsWalking)
                                user.SetPos(user.Coordinate.X, user.Coordinate.Y, Room.GetGameMap().GetHeightForSquare(square));
                        }
                    }
                }
                if (UsersToUpdate.Count > 0)
                {
                    foreach (var user in UsersToUpdate)
                        user.SetPos(user.Coordinate.X, user.Coordinate.Y, Room.GetGameMap().GetHeightForSquare(new System.Drawing.Point(user.Coordinate.X, user.Coordinate.Y)));
                }
            }
        }
    }
}