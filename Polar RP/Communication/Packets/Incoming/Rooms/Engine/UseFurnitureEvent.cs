using System;
using System.Linq;
using System.Collections.Generic;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Items.Wired;
using Polar.Communication.Packets.Outgoing.Rooms.Furni;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.HabboRoleplay.Farming;
using Polar.Database.Interfaces;
using Polar.HabboHotel.GameClients;

namespace Polar.Communication.Packets.Incoming.Rooms.Engine
{
    internal class UseFurnitureEvent : IPacketEvent
    {
        public void Parse(GameClient Session, ClientPacket Packet)
        {
            if (Session == null || Session.GetHabbo() == null || !Session.GetHabbo().InRoom)
                return;
            if (Session.GetRoleplay().DrivingCar || Session.GetRoleplay().Pasajero)
                return;

            Room Room;
            if (!PolarEnvironment.GetGame().GetRoomManager().TryGetRoom(Session.GetHabbo().CurrentRoomId, out Room))
                return;

            int itemID = Packet.PopInt();
            int request = Packet.PopInt();

            Item Item = Room.GetRoomItemHandler().GetItem(itemID);
            if (Item == null) return;

            HandleFurniInteraction(Session, Room, Item, request);
        }

        // ✅ Método estático compartido — usado también por ClickFurniEvent
        public static void HandleFurniInteraction(GameClient Session, Room Room, Item Item, int request)
        {
            bool hasRights = Room.CheckRights(Session, false, true);
            bool MyTerrain = Room.CheckTerrain(Session, Item.GetX, Item.GetY);

            if (Item.GetBaseItem().ItemName.ToLower() == "fxbox_fx192")
            {
                FarmingManager.EquipWateringCan(Session, Item);
                return;
            }

            if (Item.GetBaseItem().ItemName.ToLower() == "nest_dirt")
            {
                FarmingManager.PlantSeed(Session, Item);
                return;
            }

            if (Item.GetBaseItem().InteractionType == InteractionType.banzaitele)
                return;

            if (Item.GetBaseItem().InteractionType == InteractionType.TONER)
            {
                if (!Room.CheckRights(Session, false)) return;
                Room.TonerData.Enabled = Room.TonerData.Enabled == 0 ? 1 : 0;
                Room.SendMessage(new ObjectUpdateComposer(Item, Item.UserID));
                Item.UpdateState();
                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                    dbClient.RunQuery("UPDATE `room_items_toner` SET `enabled` = '" + Room.TonerData.Enabled + "' LIMIT 1");
                return;
            }

            if (Item.Data.InteractionType == InteractionType.GNOME_BOX && Item.UserID == Session.GetHabbo().Id)
                Session.SendMessage(new GnomeBoxComposer(Item.Id));

            bool Toggle = true;
            if (Item.GetBaseItem().InteractionType == InteractionType.WF_FLOOR_SWITCH_1 ||
                Item.GetBaseItem().InteractionType == InteractionType.WF_FLOOR_SWITCH_2)
            {
                RoomUser User = Item.GetRoom().GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);
                if (User == null) return;
                if (!Gamemap.TilesTouching(Item.GetX, Item.GetY, User.X, User.Y))
                    Toggle = false;
            }

            Item.Interactor.OnTrigger(Session, Item, request, hasRights);

            if (Toggle)
                Item.GetRoom().GetWired().TriggerEvent(WiredBoxType.TriggerStateChanges, Session.GetHabbo(), Item);
        }
    }
}