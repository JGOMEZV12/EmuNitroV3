using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;

namespace Polar.HabboHotel.Items.Wired.Boxes.Conditions
{
    class ActorHasHandItemBox : IWiredItem, IWiredCustomData
    {
        protected const int QUANTIFIER_ALL = 0;
        protected const int QUANTIFIER_ANY = 1;

        public Room Instance { get; set; }
        public Item Item { get; set; }
        public virtual WiredBoxType Type => WiredBoxType.ConditionActorHasHandItemBox;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        protected int handItem = 0;
        protected int userSource = WiredBoxTypeUtility.SOURCE_TRIGGER;
        protected int quantifier = QUANTIFIER_ALL;

        public ActorHasHandItemBox(Room instance, Item item)
        {
            Instance = instance;
            Item = item;
            SetItems = new ConcurrentDictionary<int, Item>();
        }

        public void HandleSave(ClientPacket packet)
        {
            int paramsCount = packet.PopInt();
            int rawHandItem = paramsCount > 0 ? packet.PopInt() : 0;
            int rawSource = paramsCount > 1 ? packet.PopInt() : WiredBoxTypeUtility.SOURCE_TRIGGER;
            int rawQuant = paramsCount > 2 ? packet.PopInt() : QUANTIFIER_ALL;

            string strParam = packet.PopString();

            Console.WriteLine($"[ActorHasHandItemBox] HandleSave — handItem={rawHandItem}, userSource={rawSource}, quantifier={rawQuant}, str='{strParam}'");

            this.handItem = NormalizeHandItem(rawHandItem);
            this.userSource = rawSource;
            this.quantifier = NormalizeQuantifier(rawQuant);
            this.StringData = this.handItem.ToString();
        }

        public string GetWiredData()
        {
            return JsonConvert.SerializeObject(new JsonData
            {
                handItemId = this.handItem,
                userSource = this.userSource,
                quantifier = this.quantifier
            });
        }

        public void LoadWiredData(string wiredData)
        {
            this.handItem = 0;
            this.userSource = WiredBoxTypeUtility.SOURCE_TRIGGER;
            this.quantifier = QUANTIFIER_ALL;

            if (string.IsNullOrEmpty(wiredData)) return;

            if (wiredData.StartsWith("{"))
            {
                var data = JsonConvert.DeserializeObject<JsonData>(wiredData);
                if (data == null) return;

                this.handItem = NormalizeHandItem(data.handItemId);
                this.userSource = data.userSource;
                this.quantifier = NormalizeQuantifier(data.quantifier);
            }
            else
            {
                if (int.TryParse(wiredData, out int old))
                    this.handItem = NormalizeHandItem(old);
            }

            this.StringData = this.handItem.ToString();
        }

        public void Serialize(ServerPacket packet)
        {
            packet.WriteBoolean(false);
            packet.WriteInteger(5);
            packet.WriteInteger(0);
            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            packet.WriteString("");
            packet.WriteInteger(3);
            packet.WriteInteger(this.handItem);
            packet.WriteInteger(this.userSource);
            packet.WriteInteger(this.quantifier);
            packet.WriteInteger(0);
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(0);
            packet.WriteInteger(0);
        }

        public virtual bool Execute(params object[] Params)
        {
            if (Params.Length == 0 || Instance == null) return false;

            Habbo Player = Params[0] as Habbo;
            if (Player == null) return false;

            RoomUser User = Instance.GetRoomUserManager().GetRoomUserByHabbo(Player.Id);
            if (User == null) return false;

            return User.CarryItemID == this.handItem;
        }

        protected int NormalizeHandItem(int value) => Math.Max(0, value);
        protected int NormalizeQuantifier(int value) => value == QUANTIFIER_ANY ? QUANTIFIER_ANY : QUANTIFIER_ALL;

        protected class JsonData
        {
            public int handItemId { get; set; }
            public int userSource { get; set; }
            public int quantifier { get; set; }
        }
    }
}