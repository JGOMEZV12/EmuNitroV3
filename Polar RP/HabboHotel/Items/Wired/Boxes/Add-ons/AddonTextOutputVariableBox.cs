using System;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Wired.Boxes.Add_ons
{
    class AddonTextOutputVariableBox : IWiredItem, IWiredCustomData
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.AddonTextOutputVariable;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        private string _variableToken = "";
        private string _placeholder = "";

        public AddonTextOutputVariableBox(Room instance, Item item)
        {
            Instance = instance;
            Item = item;
            SetItems = new ConcurrentDictionary<int, Item>();
        }

        public void HandleSave(ClientPacket packet)
        {
            packet.PopInt();
            string raw = packet.PopString();
            string[] parts = raw.Split('\t');
            _variableToken = parts[0];
            _placeholder = parts.Length > 1 ? parts[1] : "";
            StringData = raw;
        }

        public string GetWiredData() => JsonConvert.SerializeObject(new { variableToken = _variableToken, placeholder = _placeholder });
        public void LoadWiredData(string wiredData) {
            if (string.IsNullOrEmpty(wiredData)) return;
            try {
                var data = JsonConvert.DeserializeObject<dynamic>(wiredData);
                _variableToken = data.variableToken;
                _placeholder = data.placeholder;
            } catch { _variableToken = wiredData; }
            StringData = _variableToken + "\t" + _placeholder;
        }

        public void Serialize(ServerPacket packet) {
            packet.WriteBoolean(false); packet.WriteInteger(0); packet.WriteInteger(0);
            packet.WriteInteger(Item.GetBaseItem().SpriteId); packet.WriteInteger(Item.Id);
            packet.WriteString(StringData); packet.WriteInteger(0);
            packet.WriteInteger(0); packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(0); packet.WriteInteger(0);
        }

        public bool Execute(params object[] @params) => true;
    }
}
