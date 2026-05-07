using System;
using System.Collections.Concurrent;
using Polar.HabboHotel.Items.Wired.Config;
using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Wired.Boxes.Add_ons
{
    class AddonUserVariableBox : IWiredItem, IWiredCustomData
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.AddonUserVariable;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        private string _variableName = "";
        private bool _hasValue = false;
        private int _availability = 0;

        public AddonUserVariableBox(Room instance, Item item)
        {
            Instance = instance;
            Item = item;
            SetItems = new ConcurrentDictionary<int, Item>();
        }

        public void HandleSave(ClientPacket packet)
        {
            int paramsCount = packet.PopInt();
            _hasValue = paramsCount > 0 && packet.PopInt() == 1;
            _availability = paramsCount > 1 ? packet.PopInt() : 0;
            _variableName = packet.PopString();
            StringData = _variableName;
        }

        public string GetWiredData() => JsonConvert.SerializeObject(new { variableName = _variableName, hasValue = _hasValue, availability = _availability });
        public void LoadWiredData(string wiredData) {
            if (string.IsNullOrEmpty(wiredData)) return;
            try {
                var data = JsonConvert.DeserializeObject<dynamic>(wiredData);
                _variableName = data.variableName;
                _hasValue = data.hasValue;
                _availability = data.availability;
            } catch { _variableName = wiredData; }
            StringData = _variableName;
        }

        public void Serialize(ServerPacket packet) {
            packet.WriteBoolean(false); packet.WriteInteger(0); packet.WriteInteger(0);
            packet.WriteInteger(Item.GetBaseItem().SpriteId); packet.WriteInteger(Item.Id);
            packet.WriteString(_variableName); packet.WriteInteger(2);
            packet.WriteInteger(_hasValue ? 1 : 0); packet.WriteInteger(_availability);
            packet.WriteInteger(0); packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(0); packet.WriteInteger(0);
        }

        public bool Execute(params object[] @params) => true;
    }
}
