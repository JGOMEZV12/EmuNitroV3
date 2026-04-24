using Polar.Communication.Packets.Outgoing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;
using Polar.Communication.Packets.Incoming;

namespace Polar.HabboHotel.Items.Wired.Boxes.Effects
{
    internal class RegenerateMapsBox : IWiredItem
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }

        public WiredBoxType Type
        {
            get { return WiredBoxType.EffectRegenerateMaps; }
        }

        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        public RegenerateMapsBox(Room instance, Item item)
        {
            this.Instance = instance;
            this.Item = item;
            this.StringData = "";
            this.SetItems = new ConcurrentDictionary<int, Item>();
        }

        public void HandleSave(ClientPacket Packet)
        {
            int Unknown = Packet.PopInt();
            string Unknown2 = Packet.PopString();
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
            if (this is IWiredCycle)
            {
                Packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
                Packet.WriteInteger(0);
                Packet.WriteInteger(((IWiredCycle)this).Delay);
            }
            else
            {
                Packet.WriteInteger(0);
                Packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
                Packet.WriteInteger(0);
            }
        }
        public bool Execute(params object[] Params)
        {
            if (Instance == null)
                return false;

            TimeSpan TimeSinceRegen = DateTime.Now - Instance.lastRegeneration;

            if (TimeSinceRegen.TotalMinutes > 1)
            {
                Instance.GetGameMap().GenerateMaps();
                Instance.lastRegeneration = DateTime.Now;
                return true;
            }

            return false;
        }
    }
}
