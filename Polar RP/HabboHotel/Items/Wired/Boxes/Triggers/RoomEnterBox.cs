using Polar.Communication.Packets.Outgoing;
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;

namespace Polar.HabboHotel.Items.Wired.Boxes.Triggers
{
    class RoomEnterBox : IWiredItem
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type { get { return WiredBoxType.TriggerRoomEnter; } }
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        public RoomEnterBox(Room Instance, Item Item)
        {
            this.Instance = Instance;
            this.Item = Item;
            this.SetItems = new ConcurrentDictionary<int, Item>();
        }

        public void HandleSave(ClientPacket Packet)
        {
            int Unknown = Packet.PopInt();
            string User = Packet.PopString();

            this.StringData = User;
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
        public bool Execute(params object[] Params)
        {
            // FIX: Validar Player antes de usarlo
            Habbo Player = (Habbo)Params[0];
            if (Player == null)
                return false;

            WiredContext context = new WiredContext(Player);
            ICollection<IWiredItem> Effects = Instance.GetWired().GetEffects(this);
            ICollection<IWiredItem> Conditions = Instance.GetWired().GetConditions(this);
            ICollection<IWiredItem> selectors = Instance.GetWired().GetSelectors(this);

            // Execute Selectors
            foreach (var selector in selectors)
            {
                selector.Execute(context);
                Instance.GetWired().OnEvent(selector.Item);
            }

            Instance.GetWired().OnEvent(Item);

            if (!string.IsNullOrWhiteSpace(StringData) && Player.Username != StringData)
                return false;

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
            if (hasOrEval)
            {
                if (Conditions.Count > 0 && !Conditions.Any(c => context.SelectedUsers.Any(u => c.Execute(u, context)))) return false;
            }
            else
            {
                foreach (IWiredItem Condition in Conditions.ToList())
                {
                    bool conditionMet = context.SelectedUsers.Any(u => Condition.Execute(u, context));
                    if (!conditionMet)
                        return false;

                    Instance.GetWired().OnEvent(Condition.Item);
                }
            }

            // Effect Execution
            bool hasExecuteInOrder = addons.Any(x => x.Type == WiredBoxType.AddonExecuteInOrder);
            bool HasRandomEffectAddon = addons.Any(x => x.Type == WiredBoxType.AddonRandomEffect);
            bool hasUnseenAddon = addons.Any(x => x.Type == WiredBoxType.AddonUnseen);

            if (HasRandomEffectAddon)
            {
                IWiredItem RandomBox = addons.FirstOrDefault(x => x.Type == WiredBoxType.AddonRandomEffect);
                if (RandomBox == null || !RandomBox.Execute())
                    return false;

                IWiredItem SelectedBox = Instance.GetWired().GetRandomEffect(Effects.ToList());
                if (SelectedBox != null && SelectedBox.Execute(context))
                    Instance.GetWired().OnEvent(SelectedBox.Item);

                Instance.GetWired().OnEvent(RandomBox.Item);
            }
            else if (hasUnseenAddon)
            {
                IWiredItem unseenBox = addons.FirstOrDefault(x => x.Type == WiredBoxType.AddonUnseen);
                if (unseenBox != null && unseenBox.Execute(Effects.ToList(), context))
                    Instance.GetWired().OnEvent(unseenBox.Item);
            }
            else if (hasExecuteInOrder)
            {
                foreach (IWiredItem Effect in Effects.OrderBy(x => x.Item.GetZ).ToList())
                {
                    if (!Effect.Execute(context)) break;
                    Instance.GetWired().OnEvent(Effect.Item);
                }
            }
            else
            {
                foreach (IWiredItem Effect in Effects.ToList())
                {
                    if (!Effect.Execute(context))
                        continue;

                    Instance.GetWired().OnEvent(Effect.Item);
                }
            }

            return true;
        }
    }
}
