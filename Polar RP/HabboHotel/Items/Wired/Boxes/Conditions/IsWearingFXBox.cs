using Polar.HabboHotel.Rooms.Instance;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;

namespace Polar.HabboHotel.Items.Wired.Boxes.Conditions
{
    class IsWearingFXBox : IWiredItem, IWiredCustomData
    {
        protected const int QUANTIFIER_ALL = 0;
        protected const int QUANTIFIER_ANY = 1;

        public Room Instance { get; set; }
        public Item Item { get; set; }
        public virtual WiredBoxType Type => WiredBoxType.ConditionIsWearingFX;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        protected int effectId = 0;
        protected int userSource = WiredSourceUtil.SOURCE_TRIGGER;
        protected int quantifier = QUANTIFIER_ANY;

        public IsWearingFXBox(Room instance, Item item)
        {
            Instance = instance;
            Item = item;
            SetItems = new ConcurrentDictionary<int, Item>();
            StringData = "";
        }

        public void HandleSave(ClientPacket packet)
        {
            int paramsCount = packet.PopInt();
            int rawEffect = paramsCount > 0 ? packet.PopInt() : 0;
            int rawSource = paramsCount > 1 ? packet.PopInt() : WiredSourceUtil.SOURCE_TRIGGER;
            int rawQuant = paramsCount > 2 ? packet.PopInt() : QUANTIFIER_ANY;

            string strParam = packet.PopString();

            Console.WriteLine($"[IsWearingFXBox] HandleSave — effectId={rawEffect}, userSource={rawSource}, quantifier={rawQuant}");

            this.effectId = rawEffect;
            this.userSource = rawSource;
            this.quantifier = NormalizeQuantifier(rawQuant);
            this.StringData = this.effectId.ToString();
        }

        public string GetWiredData()
        {
            return JsonConvert.SerializeObject(new JsonData
            {
                effectId = this.effectId,
                userSource = this.userSource,
                quantifier = this.quantifier
            });
        }

        public void LoadWiredData(string wiredData)
        {
            this.effectId = 0;
            this.userSource = WiredSourceUtil.SOURCE_TRIGGER;
            this.quantifier = QUANTIFIER_ANY;

            if (string.IsNullOrEmpty(wiredData)) return;

            if (wiredData.StartsWith("{"))
            {
                var data = JsonConvert.DeserializeObject<JsonData>(wiredData);
                if (data == null) return;

                this.effectId = data.effectId;
                this.userSource = data.userSource;
                this.quantifier = NormalizeQuantifier(data.quantifier);
            }
            else
            {
                // Retrocompatibilidad: formato viejo era solo el effectId
                if (int.TryParse(wiredData, out int old))
                    this.effectId = old;

                this.userSource = WiredSourceUtil.SOURCE_TRIGGER;
                this.quantifier = QUANTIFIER_ANY;
            }

            this.StringData = this.effectId.ToString();
        }

        public void Serialize(ServerPacket packet)
        {
            packet.WriteBoolean(true); // Java usa true aquí
            packet.WriteInteger(5);
            packet.WriteInteger(0);
            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            packet.WriteString("");
            packet.WriteInteger(3);
            packet.WriteInteger(this.effectId);
            packet.WriteInteger(this.userSource);
            packet.WriteInteger(this.quantifier);
            packet.WriteInteger(0);
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(0);
            packet.WriteInteger(0);
        }

        public virtual bool Execute(params object[] Params)
        {
            if (Params.Length == 0) return false;

            Habbo Player = Params[0] as Habbo;
            if (Player == null) return false;

            return MatchesEffect(Player);
        }

        protected bool MatchesEffect(Habbo player)
        {
            if (player.Effects() == null) return false;
            return player.Effects().CurrentEffect == this.effectId;
        }

        protected int NormalizeQuantifier(int value) =>
            value == QUANTIFIER_ANY ? QUANTIFIER_ANY : QUANTIFIER_ALL;

        protected class JsonData
        {
            public int effectId { get; set; }
            public int userSource { get; set; }
            public int quantifier { get; set; }
        }
    }
}