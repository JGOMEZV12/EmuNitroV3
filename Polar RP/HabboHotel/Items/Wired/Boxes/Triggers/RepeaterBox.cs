using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Wired.Boxes.Triggers
{
    internal class RepeaterBox : IWiredItem, IWiredCycle, IWiredCustomData
    {
        private const int DEFAULT_DELAY = 10 * 500; // 5 segundos en ms
        private const int MIN_DELAY = 500;

        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.TriggerRepeat;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        // repeatTime en ms, igual que Java
        private int repeatTime = DEFAULT_DELAY;

        // Delay en ticks (repeatTime / 500) — usado por IWiredCycle
        public int Delay
        {
            get => repeatTime / 500;
            set
            {
                repeatTime = Math.Max(value * 500, MIN_DELAY);
                TickCount = Delay;
            }
        }

        public int TickCount { get; set; }

        public RepeaterBox(Room instance, Item item)
        {
            Instance = instance;
            Item = item;
            SetItems = new ConcurrentDictionary<int, Item>();
            TickCount = Delay;
        }

        public void HandleSave(ClientPacket packet)
        {
            int intCount = packet.PopInt();
            int ticks = packet.PopInt(); // el cliente manda ticks, no ms

            Console.WriteLine($"[RepeaterBox] HandleSave — intCount={intCount}, ticks={ticks}");

            int newRepeatTime = ticks * 500;
            if (newRepeatTime < MIN_DELAY) newRepeatTime = MIN_DELAY;

            repeatTime = newRepeatTime;
            TickCount = Delay;
        }

        public string GetWiredData()
        {
            return JsonConvert.SerializeObject(new JsonData { repeatTime = this.repeatTime });
        }

        public void LoadWiredData(string wiredData)
        {
            if (string.IsNullOrEmpty(wiredData)) return;

            if (wiredData.StartsWith("{"))
            {
                var data = JsonConvert.DeserializeObject<JsonData>(wiredData);
                repeatTime = data?.repeatTime ?? DEFAULT_DELAY;
            }
            else
            {
                // Retrocompatibilidad: el viejo formato guardaba el delay en ticks
                if (int.TryParse(wiredData, out int ticks))
                    repeatTime = ticks * 500;
            }

            if (repeatTime < MIN_DELAY)
                repeatTime = 20 * 500;

            TickCount = Delay;
        }

        public void Serialize(ServerPacket packet)
        {
            packet.WriteBoolean(false);
            packet.WriteInteger(5);
            packet.WriteInteger(0);
            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            packet.WriteString("");
            packet.WriteInteger(1);
            packet.WriteInteger(repeatTime / 500); // ticks al cliente
            packet.WriteInteger(0);
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(0);
        }

        public bool Execute(params object[] @params) => true;

        public bool OnCycle()
        {
            var avatars = Instance.GetRoomUserManager().GetRoomUsers().ToList();
            var effects = Instance.GetWired().GetEffects(this);
            var conditions = Instance.GetWired().GetConditions(this);
            var addons = Instance.GetWired().GetTriggers(this)
                                     .Where(x => x.Type.ToString().StartsWith("Addon")).ToList();

            var limitAddon = addons.FirstOrDefault(x => x.Type == WiredBoxType.AddonExecutionLimit);
            if (limitAddon != null && !limitAddon.Execute()) return false;

            var randomAddon = addons.FirstOrDefault(x => x.Type == WiredBoxType.AddonRandom);
            if (randomAddon != null && !randomAddon.Execute()) return false;

            bool hasOrEval = addons.Any(x => x.Type == WiredBoxType.AddonOrEval);

            if (conditions.Count > 0)
            {
                bool conditionsMet;
                if (hasOrEval)
                {
                    conditionsMet = conditions.Any(c =>
                        avatars.Any(a => a?.GetClient()?.GetHabbo() != null && c.Execute(a.GetClient().GetHabbo())));
                }
                else
                {
                    conditionsMet = conditions.All(c =>
                        avatars.Any(a => a?.GetClient()?.GetHabbo() != null && c.Execute(a.GetClient().GetHabbo())));
                }

                if (!conditionsMet) return false;

                foreach (var condition in conditions)
                    Instance.GetWired().OnEvent(condition.Item);
            }

            bool hasExecuteInOrder = addons.Any(x => x.Type == WiredBoxType.AddonExecuteInOrder);
            bool hasRandomEffect = addons.Any(x => x.Type == WiredBoxType.AddonRandomEffect);
            bool hasUnseen = addons.Any(x => x.Type == WiredBoxType.AddonUnseen);

            if (hasRandomEffect)
            {
                var randomBox = addons.FirstOrDefault(x => x.Type == WiredBoxType.AddonRandomEffect);
                if (randomBox == null || !randomBox.Execute()) return false;

                var selectedBox = Instance.GetWired().GetRandomEffect(effects.ToList());
                if (selectedBox != null && selectedBox.Execute())
                    Instance.GetWired().OnEvent(selectedBox.Item);

                Instance.GetWired().OnEvent(randomBox.Item);
            }
            else if (hasUnseen)
            {
                var unseenBox = addons.FirstOrDefault(x => x.Type == WiredBoxType.AddonUnseen);
                if (unseenBox != null && unseenBox.Execute(effects.ToList()))
                    Instance.GetWired().OnEvent(unseenBox.Item);
            }
            else if (hasExecuteInOrder)
            {
                foreach (var effect in effects.OrderBy(x => x.Item.GetZ).ToList())
                {
                    if (!effect.Execute()) break;
                    Instance.GetWired().OnEvent(effect.Item);
                }
            }
            else
            {
                foreach (var effect in effects.ToList())
                {
                    if (!effect.Execute()) continue;
                    Instance.GetWired().OnEvent(effect.Item);
                }
            }

            TickCount = Delay;
            return true;
        }

        private class JsonData
        {
            public int repeatTime { get; set; }
        }
    }
}