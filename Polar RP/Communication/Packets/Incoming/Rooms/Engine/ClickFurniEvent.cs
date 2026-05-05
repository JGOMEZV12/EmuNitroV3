using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Items.Wired;
using Polar.HabboHotel.Rooms;
using System;
using System.Collections.Concurrent;

namespace Polar.Communication.Packets.Incoming.Rooms.Engine
{
    internal class ClickFurniEvent : IPacketEvent
    {
        private const string CLICK_TILE_INTERACTION = "room_invisible_click_tile";

        // key = "userId:itemId", value = timestamp del primer click
        private static readonly ConcurrentDictionary<string, long> _firstClick
            = new ConcurrentDictionary<string, long>();

        // Tiempo mínimo entre el 1er y 2do click (ms)
        private const long MIN_MS = 100;
        // Tiempo máximo para que el 2do click cuente como doble click (ms)
        private const long MAX_MS = 800;

        public void Parse(GameClient Session, ClientPacket Packet)
        {
            if (Session?.GetHabbo() == null) return;

            Room room = Session.GetHabbo().CurrentRoom;
            if (room == null) return;

            int itemId = Math.Abs(Packet.PopInt());
            int unknown = Packet.PopInt();

            Item item = room.GetRoomItemHandler().GetItem(itemId);
            if (item == null) return;

            string key = $"{Session.GetHabbo().Id}:{itemId}";
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            if (_firstClick.TryGetValue(key, out long firstTime))
            {
                long diff = now - firstTime;

                if (diff >= MIN_MS && diff <= MAX_MS && item.GetBaseItem().InteractionType == InteractionType.MULTI_HEIGHT)
                {
                    // ✅ Doble click válido — actúa y resetea
                    _firstClick.TryRemove(key, out _);
                    UseFurnitureEvent.HandleFurniInteraction(Session, room, item, unknown);
                }
                else
                {
                    // Muy rápido o muy tarde — reinicia el contador
                    _firstClick[key] = now;
                }
            }
            else
            {
                // Primer click — guardar timestamp
                _firstClick[key] = now;
            }

            // Wired siempre se dispara
            room.GetWired().TriggerEvent(WiredBoxType.TriggerStateChanges, Session.GetHabbo(), item);

            if (IsClickTileItem(item))
                room.GetWired().TriggerEvent(WiredBoxType.TriggerWalkOnFurni, Session.GetHabbo(), item);
        }

        private static bool IsClickTileItem(Item item)
        {
            if (item?.GetBaseItem() == null) return false;
            string interaction = item.GetBaseItem().InteractionType.ToString();
            return string.Equals(interaction, CLICK_TILE_INTERACTION, StringComparison.OrdinalIgnoreCase);
        }
    }
}