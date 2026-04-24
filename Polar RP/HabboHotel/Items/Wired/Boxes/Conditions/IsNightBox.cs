using Polar.Communication.Packets.Outgoing;
using System;
using System.Linq;
using System.Collections.Concurrent;
using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboHotel.Items.Wired.Boxes.Conditions
{
    class IsNightBox : IWiredItem
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type { get { return WiredBoxType.ConditionIsNight; } }
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        public IsNightBox(Room instance, Item item)
        {
            this.Instance = instance;
            this.Item = item;
            this.SetItems = new ConcurrentDictionary<int, Item>();
        }

        public void HandleSave(ClientPacket Packet)
        {
            int Unknown = Packet.PopInt();
        }

        
        public void Serialize(ServerPacket Packet)
        {
            Packet.WriteBoolean(false);
            Packet.WriteInteger(100);
            Packet.WriteInteger(SetItems.Count);
            foreach (Item Item in SetItems.Values.ToList())
            {
                Packet.WriteInteger(Item.Id);
            }
            Packet.WriteInteger(Item.GetBaseItem().SpriteId);
            Packet.WriteInteger(Item.Id);
            Packet.WriteString(StringData);
            Packet.WriteInteger(0);
            Packet.WriteInteger(0);
            Packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
        }
        public bool Execute(params object[] Params)
        {
            // Se asume que RoleplayManager.DayNightSystem maneja el estado global
            // En este emulador, solemos verificar la hora o una flag global.
            // Para Polar RP, vamos a usar el estado de DayNightSystem si existe o implementarlo.
            // Mirando RoleplayManager.cs, hay una flag DayNightSystem.

            // Si el sistema de día/noche está desactivado, esta condición siempre devuelve false para noche?
            // O mejor, miramos la hora actual del servidor si no hay una flag de "es noche".

            int Hour = DateTime.Now.Hour;
            return (Hour >= 20 || Hour <= 6); // Noche de 8pm a 6am
        }
    }
}
