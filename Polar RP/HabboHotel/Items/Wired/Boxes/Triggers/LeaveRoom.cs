using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;
using System;
using System.Collections.Concurrent;

namespace Polar.HabboHotel.Items.Wired.Boxes.Triggers
{
    class UserLeavesRoomBox : IWiredItem, IWiredCustomData
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.TriggerLeaveRoom;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        private string username = "";

        public UserLeavesRoomBox(Room Instance, Item Item)
        {
            this.Instance = Instance;
            this.Item = Item;
            this.StringData = "";
            this.SetItems = new ConcurrentDictionary<int, Item>();
        }

        public void HandleSave(ClientPacket Packet)
        {
            string Username = Packet.PopString();

            Console.WriteLine($"[UserLeavesRoomBox] HandleSave — Username='{Username}'");

            this.username = Username;
            this.StringData = Username;
        }

        public string GetWiredData()
        {
            return JsonConvert.SerializeObject(new JsonData { username = this.username });
        }

        public void LoadWiredData(string wiredData)
        {
            this.StringData = "";

            if (string.IsNullOrEmpty(wiredData)) return;

            if (wiredData.StartsWith("{"))
            {
                var data = JsonConvert.DeserializeObject<JsonData>(wiredData);
                if (data != null)
                    this.StringData = data.username ?? "";
            }
            else
            {
                // Retrocompatibilidad: el dato era directamente el username
                this.StringData = wiredData;
            }
        }

        public void Serialize(ServerPacket Packet)
        {
            Packet.WriteBoolean(false);
            Packet.WriteInteger(5);
            Packet.WriteInteger(0);
            Packet.WriteInteger(Item.GetBaseItem().SpriteId);
            Packet.WriteInteger(Item.Id);
            Packet.WriteString(this.username);
            Packet.WriteInteger(0);
            Packet.WriteInteger(0);
            Packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            Packet.WriteInteger(0);
            Packet.WriteInteger(0);
        }

        public bool Execute(params object[] Params)
        {
            Habbo Player = (Habbo)Params[0];
            if (Player == null) return false;

            // Si hay username filtrado, solo ejecuta para ese usuario
            if (!string.IsNullOrEmpty(this.username))
            {
                if (!Player.Username.Equals(this.username, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            ICollection<IWiredItem> Effects = Instance.GetWired().GetEffects(this);
            ICollection<IWiredItem> Conditions = Instance.GetWired().GetConditions(this);

            foreach (IWiredItem Condition in Conditions.ToList())
            {
                if (!Condition.Execute(Player)) return false;
                Instance.GetWired().OnEvent(Condition.Item);
            }

            foreach (IWiredItem Effect in Effects.ToList())
            {
                if (!Effect.Execute(Player)) continue;
                Instance.GetWired().OnEvent(Effect.Item);
            }

            return true;
        }

        private class JsonData
        {
            public string username { get; set; }
        }
    }
}