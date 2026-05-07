using System;
using System.Collections.Concurrent;
using System.Linq;
using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;
using Polar.HabboHotel.Users.Badges;

namespace Polar.HabboHotel.Items.Wired.Boxes.Conditions
{
    class IsWearingBadgeBox : IWiredItem, IWiredCustomData
    {
        protected const int QUANTIFIER_ALL = 0;
        protected const int QUANTIFIER_ANY = 1;

        public Room Instance { get; set; }
        public Item Item { get; set; }
        public virtual WiredBoxType Type => WiredBoxType.ConditionIsWearingBadge;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        protected string badge = "";
        protected int userSource = WiredBoxTypeUtility.SOURCE_TRIGGER;
        protected int quantifier = QUANTIFIER_ANY;

        public IsWearingBadgeBox(Room instance, Item item)
        {
            Instance = instance;
            Item = item;
            SetItems = new ConcurrentDictionary<int, Item>();
            StringData = "";
        }

        public void HandleSave(ClientPacket packet)
        {
            int paramsCount = packet.PopInt();
            int rawSource = paramsCount > 0 ? packet.PopInt() : WiredBoxTypeUtility.SOURCE_TRIGGER;
            int rawQuant = paramsCount > 1 ? packet.PopInt() : QUANTIFIER_ANY;

            string rawBadge = packet.PopString();

            Console.WriteLine($"[IsWearingBadgeBox] HandleSave — badge='{rawBadge}', userSource={rawSource}, quantifier={rawQuant}");

            this.badge = rawBadge;
            this.userSource = rawSource;
            this.quantifier = NormalizeQuantifier(rawQuant);
            this.StringData = this.badge;
        }

        public string GetWiredData()
        {
            return JsonConvert.SerializeObject(new JsonData
            {
                badge = this.badge,
                userSource = this.userSource,
                quantifier = this.quantifier
            });
        }

        public void LoadWiredData(string wiredData)
        {
            this.badge = "";
            this.userSource = WiredBoxTypeUtility.SOURCE_TRIGGER;
            this.quantifier = QUANTIFIER_ANY;

            if (string.IsNullOrEmpty(wiredData)) return;

            if (wiredData.StartsWith("{"))
            {
                var data = JsonConvert.DeserializeObject<JsonData>(wiredData);
                if (data == null) return;

                this.badge = data.badge ?? "";
                this.userSource = data.userSource;
                this.quantifier = NormalizeQuantifier(data.quantifier);
            }
            else
            {
                // Retrocompatibilidad: formato viejo era solo el badge code
                this.badge = wiredData;
                this.userSource = WiredBoxTypeUtility.SOURCE_TRIGGER;
                this.quantifier = QUANTIFIER_ANY;
            }

            this.StringData = this.badge;
        }

        public void Serialize(ServerPacket packet)
        {
            packet.WriteBoolean(false);
            packet.WriteInteger(5);
            packet.WriteInteger(0);
            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            packet.WriteString(this.badge);
            packet.WriteInteger(2);
            packet.WriteInteger(this.userSource);
            packet.WriteInteger(this.quantifier);
            packet.WriteInteger(0);
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(0);
            packet.WriteInteger(0);
        }

        public virtual bool Execute(params object[] Params)
        {
            if (Params.Length == 0 || string.IsNullOrEmpty(this.badge)) return false;

            Habbo Player = Params[0] as Habbo;
            if (Player == null) return false;

            return IsWearingBadge(Player);
        }

        protected bool IsWearingBadge(Habbo player)
        {
            return player.GetBadgeComponent().GetBadges()
                .Any(b => b.Slot > 0 && b.Code.Equals(this.badge, StringComparison.OrdinalIgnoreCase));
        }

        protected int NormalizeQuantifier(int value) =>
            value == QUANTIFIER_ANY ? QUANTIFIER_ANY : QUANTIFIER_ALL;

        protected class JsonData
        {
            public string badge { get; set; }
            public int userSource { get; set; }
            public int quantifier { get; set; }
        }
    }
}