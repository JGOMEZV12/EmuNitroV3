using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Polar.HabboHotel.Items.Wired.Boxes.Effects
{
    class SetVariableBox : IWiredItem
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.EffectSetVariable;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        public SetVariableBox(Room instance, Item item)
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
            string varValue = data[1];

            if (Instance.WiredVariables.ContainsKey(varName))
                Instance.WiredVariables[varName] = varValue;
            else
                Instance.WiredVariables.TryAdd(varName, varValue);

            return true;
        }
    }
}
