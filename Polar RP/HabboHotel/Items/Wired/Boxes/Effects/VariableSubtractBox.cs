using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;
using System;
using System.Collections.Concurrent;

namespace Polar.HabboHotel.Items.Wired.Boxes.Effects
{
    class VariableSubtractBox : IWiredItem
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.EffectVariableSubtract;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        public VariableSubtractBox(Room instance, Item item)
        {
            Instance = instance;
            Item = item;
            SetItems = new ConcurrentDictionary<int, Item>();
            StringData = "";
        }

        public void HandleSave(ClientPacket packet)
        {
            int unknown = packet.PopInt();
            string data = packet.PopString();
            StringData = data;
        }

        public void Serialize(ServerPacket packet)
        {
            packet.WriteBoolean(false);
            packet.WriteInteger(0);
            packet.WriteInteger(0);
            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            packet.WriteString(StringData);
            packet.WriteInteger(0);
            packet.WriteInteger(0);
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
        }

        public bool Execute(params object[] @params)
        {
            if (string.IsNullOrEmpty(StringData)) return false;

            string[] data = StringData.Split('\t');
            if (data.Length < 2) return false;

            string varName = data[0];
            if (!int.TryParse(data[1], out int subValue)) return false;

            Habbo player = (Habbo)@params[0];
            if (player != null)
            {
                string userValStr = Instance.GetUserVariableManager().GetValue(player.Id, varName);
                if (int.TryParse(userValStr, out int userVal))
                {
                    Instance.GetUserVariableManager().SetValue(player.Id, varName, (userVal - subValue).ToString());
                }
            }

            string roomValStr = Instance.GetRoomVariableManager().GetValue(varName);
            if (int.TryParse(roomValStr, out int roomVal))
            {
                Instance.GetRoomVariableManager().SetValue(varName, (roomVal - subValue).ToString());
            }

            foreach (var item in SetItems.Values)
            {
                string furniValStr = Instance.GetFurniVariableManager().GetValue(item.Id, varName);
                if (int.TryParse(furniValStr, out int furniVal))
                {
                    Instance.GetFurniVariableManager().SetValue(item.Id, varName, (furniVal - subValue).ToString());
                }
            }

            string currentStr = "0";
            if (Instance.WiredVariables.TryGetValue(varName, out string val))
                currentStr = val;

            if (int.TryParse(currentStr, out int currentVal))
            {
                int newVal = currentVal - subValue;
                Instance.WiredVariables[varName] = newVal.ToString();
            }

            return true;
        }
    }
}
