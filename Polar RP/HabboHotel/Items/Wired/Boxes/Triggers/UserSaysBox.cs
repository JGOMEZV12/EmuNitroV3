using Polar.Communication.Packets.Outgoing;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Newtonsoft.Json;

using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;

namespace Polar.HabboHotel.Items.Wired.Boxes.Triggers
{
    class UserSaysBox : IWiredItem, IWiredCustomData
    {
        private const int MATCH_CONTAINS = 0;
        private const int MATCH_EXACT = 1;
        private const int MATCH_ALL_WORDS = 2;

        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type { get { return WiredBoxType.TriggerUserSays; } }
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        // Nuevos campos del formato Java
        private bool hideMessage = false;
        private bool ownerOnly = false;
        private string key = "";
        private int matchMode = MATCH_CONTAINS;

        public UserSaysBox(Room Instance, Item Item)
        {
            this.Instance = Instance;
            this.Item = Item;
            this.StringData = "";
            this.SetItems = new ConcurrentDictionary<int, Item>();
        }

        public void HandleSave(ClientPacket Packet)
        {
            int IntCount = Packet.PopInt();
            int MatchMode = Packet.PopInt();
            int HideMessage = Packet.PopInt();
            int OwnerOnly = Packet.PopInt();
            string Message = Packet.PopString();

            Console.WriteLine($"[UserSaysBox] HandleSave — IntCount={IntCount}, MatchMode={MatchMode}, HideMessage={HideMessage}, OwnerOnly={OwnerOnly}, Message='{Message}'");

            this.matchMode = NormalizeMatchMode(MatchMode);
            this.hideMessage = HideMessage == 1;
            this.ownerOnly = OwnerOnly == 1;
            this.key = Message;
            this.BoolData = this.ownerOnly;
            this.StringData = Message;
        }

        // Carga desde wired_data (nuevo formato JSON o viejo formato tab)
        public string GetWiredData()
        {
            return JsonConvert.SerializeObject(new JsonData
            {
                hideMessage = this.hideMessage,
                ownerOnly = this.ownerOnly,
                key = this.key,
                matchMode = this.matchMode
            });
        }

        public void LoadWiredData(string wiredData)
        {
            if (string.IsNullOrEmpty(wiredData)) return;

            if (wiredData.StartsWith("{"))
            {
                var data = JsonConvert.DeserializeObject<JsonData>(wiredData);
                this.hideMessage = data.hideMessage;
                this.ownerOnly = data.ownerOnly;
                this.key = data.key ?? "";
                this.matchMode = NormalizeMatchMode(data.matchMode);
            }
            else
            {
                // Retrocompatibilidad formato viejo con \t
                string[] parts = wiredData.Split('\t');
                if (parts.Length == 2)
                {
                    this.ownerOnly = parts[0] == "1";
                    this.key = parts[1];
                    this.hideMessage = false;
                    this.matchMode = MATCH_CONTAINS;
                }
            }

            this.BoolData = this.ownerOnly;
            this.StringData = this.key;
        }

        public void Serialize(ServerPacket Packet)
        {
            Packet.WriteBoolean(false);
            Packet.WriteInteger(5);
            Packet.WriteInteger(0);
            Packet.WriteInteger(Item.GetBaseItem().SpriteId);
            Packet.WriteInteger(Item.Id);
            Packet.WriteString(this.key);
            Packet.WriteInteger(3);
            Packet.WriteInteger(this.matchMode);
            Packet.WriteInteger(this.hideMessage ? 1 : 0);
            Packet.WriteInteger(this.ownerOnly ? 1 : 0);
            Packet.WriteInteger(0);
            Packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            Packet.WriteInteger(0);
            Packet.WriteInteger(0);
        }

        public bool Execute(params object[] Params)
        {
            Habbo Player = (Habbo)Params[0];
            if (Player == null || Player.CurrentRoom == null || !Player.InRoom)
                return false;

            RoomUser User = Player.CurrentRoom.GetRoomUserManager().GetRoomUserByHabbo(Player.Id);
            if (User == null)
                return false;

            string Message = Convert.ToString(Params[1]);

            if (string.IsNullOrWhiteSpace(Message))
                return false;

            if (this.matchMode != MATCH_ALL_WORDS && string.IsNullOrWhiteSpace(this.key))
                return false;

            if (this.ownerOnly && Instance.OwnerId != Player.Id)
                return false;

            if (!MatchesText(Message))
                return false;

            Player.WiredInteraction = true;

            if (this.hideMessage)
                Player.GetClient().SendMessage(new WhisperComposer(User.VirtualId, Message, 0, 0));

            ICollection<IWiredItem> Effects = Instance.GetWired().GetEffects(this);
            ICollection<IWiredItem> Conditions = Instance.GetWired().GetConditions(this);

            var addons = Instance.GetWired().GetTriggers(this).Where(x => x.Type.ToString().StartsWith("Addon")).ToList();

            var limitAddon = addons.FirstOrDefault(x => x.Type == WiredBoxType.AddonExecutionLimit);
            if (limitAddon != null && !limitAddon.Execute()) return false;

            var randomAddon = addons.FirstOrDefault(x => x.Type == WiredBoxType.AddonRandom);
            if (randomAddon != null && !randomAddon.Execute()) return false;

            bool hasOrEval = addons.Any(x => x.Type == WiredBoxType.AddonOrEval);
            if (hasOrEval)
            {
                if (Conditions.Count > 0 && !Conditions.Any(c => c.Execute(Player))) return false;
            }
            else
            {
                foreach (IWiredItem Condition in Conditions.ToList())
                {
                    if (!Condition.Execute(Player)) return false;
                    Instance.GetWired().OnEvent(Condition.Item);
                }
            }

            bool hasExecuteInOrder = addons.Any(x => x.Type == WiredBoxType.AddonExecuteInOrder);
            bool hasRandomEffect = addons.Any(x => x.Type == WiredBoxType.AddonRandomEffect);

            if (hasRandomEffect)
            {
                IWiredItem RandomBox = addons.FirstOrDefault(x => x.Type == WiredBoxType.AddonRandomEffect);
                if (RandomBox == null || !RandomBox.Execute()) return false;

                IWiredItem SelectedBox = Instance.GetWired().GetRandomEffect(Effects.ToList());
                if (SelectedBox == null || !SelectedBox.Execute()) return false;

                Instance.GetWired().OnEvent(RandomBox.Item);
                Instance.GetWired().OnEvent(SelectedBox.Item);
            }
            else if (hasExecuteInOrder)
            {
                foreach (IWiredItem Effect in Effects.OrderBy(x => x.Item.GetZ).ToList())
                {
                    if (!Effect.Execute(Player)) break;
                    Instance.GetWired().OnEvent(Effect.Item);
                }
            }
            else
            {
                foreach (IWiredItem Effect in Effects.ToList())
                {
                    if (!Effect.Execute(Player)) continue;
                    Instance.GetWired().OnEvent(Effect.Item);
                }
            }

            return true;
        }

        private bool MatchesText(string text)
        {
            string normalizedText = text.ToLower().Trim();
            string normalizedKey = this.key.ToLower().Trim();

            switch (this.matchMode)
            {
                case MATCH_EXACT:
                    return normalizedText == normalizedKey;
                case MATCH_ALL_WORDS:
                    return !string.IsNullOrEmpty(normalizedText);
                case MATCH_CONTAINS:
                default:
                    return normalizedText.Contains(normalizedKey);
            }
        }

        private int NormalizeMatchMode(int value)
        {
            if (value < MATCH_CONTAINS || value > MATCH_ALL_WORDS)
                return MATCH_CONTAINS;
            return value;
        }

        private class JsonData
        {
            public bool hideMessage { get; set; }
            public bool ownerOnly { get; set; }
            public string key { get; set; }
            public int matchMode { get; set; }
        }
    }
}