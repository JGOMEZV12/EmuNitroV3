using Polar.HabboHotel.Items.Wired;
using Polar.Communication.Packets.Outgoing;
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using Polar.HabboHotel.Rooms.Chat.Commands;

namespace Polar.HabboHotel.Items.Wired.Boxes.Triggers
{
    class UserSaysCommandBox : IWiredItem
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type { get { return WiredBoxType.TriggerUserSaysCommand; } }
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        public UserSaysCommandBox(Room Instance, Item Item)
        {
            this.Instance = Instance;
            this.Item = Item;
            this.StringData = "";
            this.SetItems = new ConcurrentDictionary<int, Item>();
        }

                public void HandleSave(ClientPacket packet)
        {
            int paramsCount = packet.PopInt();
            for (int i = 0; i < paramsCount; i++) packet.PopInt();

            this.StringData = packet.PopString();

            if (this.SetItems != null) this.SetItems.Clear();
            int itemsCount = packet.PopInt();
            for (int i = 0; i < itemsCount; i++)
            {
                Item item = Instance.GetRoomItemHandler().GetItem(packet.PopInt());
                if (item != null) this.SetItems.TryAdd(item.Id, item);
            }

            int delay = packet.PopInt();
            if (this is IWiredCycle cycle) cycle.Delay = delay;
        }


                                public void Serialize(ServerPacket packet)
        {
            packet.WriteBoolean(false);
            packet.WriteInteger(100);
            packet.WriteInteger(SetItems?.Count ?? 0);
            foreach (var item in SetItems?.Values.ToList() ?? new List<Item>()) packet.WriteInteger(item.Id);
            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            packet.WriteString(StringData ?? "");
            packet.WriteInteger(0); // Params count
            packet.WriteInteger(0); // Categorical
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(0);
        }
        public bool Execute(params object[] Params)
        {
            Habbo Player = (Habbo)Params[0];
            if (Player == null || Player.CurrentRoom == null || !Player.InRoom)
                return false;

            RoomUser User = Player.CurrentRoom.GetRoomUserManager().GetRoomUserByHabbo(Player.Id);
            if (User == null)
                return false;

            if ((BoolData && Instance.OwnerId != Player.Id) || string.IsNullOrWhiteSpace(this.StringData))
                return false;

            IChatCommand ChatCommand = null;
            if (!PolarEnvironment.GetGame().GetChatManager().GetCommands().TryGetCommand(this.StringData.Replace(":", "").ToLower(), out ChatCommand))
                return false;

            if (Player.IChatCommand == ChatCommand)
            {
                Player.WiredInteraction = true;
                ICollection<IWiredItem> Effects = Instance.GetWired().GetEffects(this);
                ICollection<IWiredItem> Conditions = Instance.GetWired().GetConditions(this);

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
                if (Conditions.Count > 0 && !Conditions.Any(c => c.Execute(Player))) return false;
            }
            else
            {
                foreach (IWiredItem Condition in Conditions.ToList())
                {
                    if (!Condition.Execute(Player))
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
                if (SelectedBox != null && SelectedBox.Execute(Player))
                    Instance.GetWired().OnEvent(SelectedBox.Item);

                Instance.GetWired().OnEvent(RandomBox.Item);
            }
            else if (hasUnseenAddon)
            {
                IWiredItem unseenBox = addons.FirstOrDefault(x => x.Type == WiredBoxType.AddonUnseen);
                if (unseenBox != null && unseenBox.Execute(Effects.ToList(), Player))
                    Instance.GetWired().OnEvent(unseenBox.Item);
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
                    if (!Effect.Execute(Player))
                        continue;

                    Instance.GetWired().OnEvent(Effect.Item);
                }
            }

                return true;
            }

            return false;
        }
    }
}
