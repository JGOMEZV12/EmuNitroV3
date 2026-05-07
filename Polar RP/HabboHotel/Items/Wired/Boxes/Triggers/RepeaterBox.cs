using Polar.Communication.Packets.Outgoing;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Items.Wired;
using Polar.HabboHotel.Items;

namespace Polar.HabboHotel.Items.Wired.Boxes.Triggers
{
    internal class RepeaterBox : IWiredItem, IWiredCycle
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.TriggerRepeat;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }

        public int Delay
        {
            get => _delay;
            set
            {
                _delay = value;
                TickCount = value;
            }
        }

        public int TickCount { get; set; }
        public string ItemsData { get; set; }

        private int _delay;

        public RepeaterBox(Room instance, Item item)
        {
            Instance = instance;
            Item = item;
            SetItems = new ConcurrentDictionary<int, Item>();
        }

        public void HandleSave(ClientPacket packet)
        {
            int unknown = packet.PopInt();
            int delay = packet.PopInt();

            Delay = delay;
            TickCount = delay;
        }

        
        public void Serialize(ServerPacket Packet)
        {
            Packet.WriteBoolean(false);
            Packet.WriteInteger(100);
            Packet.WriteInteger(SetItems.Count);
            foreach (Item Item in SetItems.Values.ToList())
            {
                Packet.WriteInteger(Item.Id);
            }
            Packet.WriteInteger(Item.GetBaseItem().SpriteId);
            Packet.WriteInteger(Item.Id);
            Packet.WriteString(StringData);

            Packet.WriteInteger(this is IWiredCycle ? 1 : 0);
            if (this is IWiredCycle)
            {
                IWiredCycle Cycle = (IWiredCycle)this;
                Packet.WriteInteger(Cycle.Delay);
            }
            Packet.WriteInteger(0);
            Packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
        }
        public bool Execute(params object[] @params)
        {
            return true;
        }

        public bool OnCycle()
        {
            WiredContext context = new WiredContext();
            ICollection<IWiredItem> effects = Instance.GetWired().GetEffects(this);
            ICollection<IWiredItem> conditions = Instance.GetWired().GetConditions(this);
            ICollection<IWiredItem> selectors = Instance.GetWired().GetSelectors(this);

            // Execute Selectors
            foreach (var selector in selectors)
            {
                selector.Execute(context);
                Instance.GetWired().OnEvent(selector.Item);
            }

            ICollection<RoomUser> avatars = Instance.GetRoomUserManager().GetRoomUsers().ToList();
            if (context.SelectedUsers.Count == 0)
            {
                foreach (var user in avatars)
                {
                    if (user.GetClient()?.GetHabbo() != null)
                        context.SelectedUsers.Add(user.GetClient().GetHabbo());
                }
            }

            // Extra Addons
            var addons = Instance.GetWired().GetTriggers(this).Where(x => x.Type.ToString().StartsWith("Addon")).ToList();

            // Execution Limit Addon
            var limitAddon = addons.FirstOrDefault(x => x.Type == WiredBoxType.AddonExecutionLimit);
            if (limitAddon != null && !limitAddon.Execute()) return false;

            // Random Addon
            var randomAddon = addons.FirstOrDefault(x => x.Type == WiredBoxType.AddonRandom);
            if (randomAddon != null && !randomAddon.Execute()) return false;

            // Condition Evaluation
            bool hasOrEval = addons.Any(x => x.Type == WiredBoxType.AddonOrEval);
            bool conditionsMet = false;

            if (conditions.Count > 0)
            {
                if (hasOrEval)
                {
                    conditionsMet = conditions.Any(c => context.SelectedUsers.Any(a => c.Execute(a, context)));
                }
                else
                {
                    conditionsMet = true;
                    foreach (IWiredItem condition in conditions)
                    {
                        bool anyAvatarMet = context.SelectedUsers.Any(a => condition.Execute(a, context));
                        if (!anyAvatarMet)
                        {
                            conditionsMet = false;
                            break;
                        }
                    }
                }

                if (!conditionsMet) return false;

                foreach (var condition in conditions)
                    Instance.GetWired().OnEvent(condition.Item);
            }

            // Effect Execution
            bool hasExecuteInOrder = addons.Any(x => x.Type == WiredBoxType.AddonExecuteInOrder);
            bool hasRandomEffectAddon = addons.Any(x => x.Type == WiredBoxType.AddonRandomEffect);
            bool hasUnseenAddon = addons.Any(x => x.Type == WiredBoxType.AddonUnseen);

            if (hasRandomEffectAddon)
            {
                IWiredItem randomBox = addons.FirstOrDefault(x => x.Type == WiredBoxType.AddonRandomEffect);
                if (randomBox == null || !randomBox.Execute())
                    return false;

                IWiredItem selectedBox = Instance.GetWired().GetRandomEffect(effects.ToList());
                if (selectedBox != null && selectedBox.Execute(context))
                    Instance.GetWired().OnEvent(selectedBox.Item);

                Instance.GetWired().OnEvent(randomBox.Item);
            }
            else if (hasUnseenAddon)
            {
                IWiredItem unseenBox = addons.FirstOrDefault(x => x.Type == WiredBoxType.AddonUnseen);
                if (unseenBox != null && unseenBox.Execute(effects.ToList(), context))
                    Instance.GetWired().OnEvent(unseenBox.Item);
            }
            else if (hasExecuteInOrder)
            {
                foreach (IWiredItem effect in effects.OrderBy(x => x.Item.GetZ).ToList())
                {
                    if (!effect.Execute()) break;
                    Instance.GetWired().OnEvent(effect.Item);
                }
            }
            else
            {
                foreach (IWiredItem effect in effects.ToList())
                {
                    if (!effect.Execute())
                        continue;

                    Instance?.GetWired().OnEvent(effect.Item);
                }
            }

            TickCount = Delay;
            return true;
        }
    }
}
