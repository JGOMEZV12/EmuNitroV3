using MoreLinq;
using Polar.Communication.Packets.Outgoing.Inventory.Furni;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Users;
using Polar.HabboHotel.Users.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;



namespace Polar.Communication.Packets.Incoming.Inventory.Furni
{
    internal class RequestFurniDeleteItems : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            int referenceItemId = Packet.PopInt();
            int amount = Packet.PopInt();

            if (Session == null || Session.GetHabbo() == null) return;
            if (!Session.GetHabbo().InRoom) return;

            Habbo habbo = Session.GetHabbo();
            InventoryComponent inventory = habbo.GetInventoryComponent();

            // Obtener todos los items como IEnumerable<Item>
            IEnumerable<Item> allItems = inventory.GetItems;

            // Buscar el item de referencia
            Item referenceItem = allItems.FirstOrDefault(x => x.Id == referenceItemId);
            if (referenceItem == null) return;

            // Contador de items borrados
            int removedCount = 0;

            using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                // Buscar items del mismo tipo
                foreach (Item furni in allItems)
                {
                    // Si es del mismo tipo que el item de referencia
                    if (furni.BaseItem == referenceItem.BaseItem)
                    {
                        // Borrar de la BD
                        dbClient.RunQuery("DELETE FROM `items` WHERE `id` = '" + furni.Id + "' AND `user_id` = '" + habbo.Id + "' LIMIT 1");

                        // Borrar del inventario
                        inventory.RemoveItem(furni.Id);

                        removedCount++;

                        // Si ya borramos la cantidad solicitada, terminar
                        if (removedCount >= amount)
                            break;
                    }
                }
            }

            // Actualizar inventario
            if (removedCount > 0)
            {
                Session.SendMessage(new FurniListUpdateComposer());
            }
        }
    }
}
