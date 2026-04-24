using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using MoreLinq;
using Polar.HabboHotel.Items;
using Polar.Communication.Packets.Outgoing.Inventory.Furni;


namespace Polar.Communication.Packets.Incoming.Inventory.Furni
{
    internal class RequestFurniInventoryEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            // Materializar UNA sola vez para evitar doble enumeración
            List<Item> Items = Session.GetHabbo().GetInventoryComponent().GetWallAndFloor.ToList();
            bool CraftingCheck = Session.GetRoleplay().CraftingCheck;

            if (!Items.Any())
            {
                // Sin items: enviar lista vacía con pages=1, page=0
                Session.SendMessage(new FurniListComposer(new List<Item>(), 1, 0, CraftingCheck));
                return;
            }

            // Calcular pages DESPUÉS de saber que hay items
            int page = 0;
            int pages = ((Items.Count - 1) / 700) + 1;

            foreach (ICollection<Item> batch in Items.Batch(700))
            {
                Session.SendMessage(new FurniListComposer(batch.ToList(), pages, page, CraftingCheck));
                page++;
            }
        }
    }
}