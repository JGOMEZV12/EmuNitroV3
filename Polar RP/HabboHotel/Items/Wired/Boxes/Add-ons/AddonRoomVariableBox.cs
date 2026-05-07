using System;
using System.Collections.Concurrent;
using System.Linq;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Wired.Boxes.Add_ons
{
    class AddonRoomVariableBox : IWiredItem
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.AddonRoomVariable;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; } // Permanent availability
        public string ItemsData { get; set; }

        public AddonRoomVariableBox(Room instance, Item item)
        {
            Instance = instance;
            Item = item;
            SetItems = new ConcurrentDictionary<int, Item>();
            StringData = "";
            Instance.GetRoomVariableManager().RegisterDefinition(this);
        }

        public void HandleSave(ClientPacket packet)
        {
            int intCount = packet.PopInt();
            int availability = packet.PopInt(); // 1 = Room Active, 10 = Permanent, 11 = Shared

            string name = WiredVariableNameValidator.Normalize(packet.PopString());
            if (!WiredVariableNameValidator.IsValid(name)) return;

            StringData = name;
            BoolData = availability >= 10;

            Instance.GetRoomVariableManager().RegisterDefinition(this);
        }

        public void Serialize(ServerPacket packet)
        {
            packet.WriteBoolean(false);
            packet.WriteInteger(0);
            packet.WriteInteger(0);
            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            packet.WriteString(StringData);
            packet.WriteInteger(1); // Params count
            packet.WriteInteger(BoolData ? 10 : 1); // availability
            packet.WriteInteger(int.TryParse(Instance.GetRoomVariableManager().GetValue(StringData), out int val) ? val : 0);
            packet.WriteInteger(0);
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(0);
            packet.WriteInteger(0);
        }

        public bool Execute(params object[] @params)
        {
            return false;
        }
    }
}
