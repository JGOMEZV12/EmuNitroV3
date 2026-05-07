using System.Collections.Concurrent;
using System.Linq;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;

namespace Polar.HabboHotel.Items.Wired.Boxes.Triggers
{
    internal class UserFurniCollision : IWiredItem
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.TriggerUserFurniCollision;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        public UserFurniCollision(Room instance, Item item)
        {
            Instance = instance;
            Item = item;
            StringData = "";
            SetItems = new ConcurrentDictionary<int, Item>();
        }

        public void HandleSave(ClientPacket packet)
        {
            packet.PopInt();    // intCount = 0
            packet.PopString(); // string vacío
        }

        public void Serialize(ServerPacket packet)
        {
            packet.WriteBoolean(false);
            packet.WriteInteger(5); // MAXIMUM_FURNI_SELECTION
            packet.WriteInteger(0); // sin furnis
            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            packet.WriteString("");
            packet.WriteInteger(0); // sin intParams
            packet.WriteInteger(0);
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(0);
            packet.WriteInteger(0);
        }

        public bool Execute(params object[] Params)
        {
            Habbo Player = Params.Length > 0 ? Params[0] as Habbo : null;
            Item source = Params.Length > 1 ? Params[1] as Item : null;

            if (Player == null || source == null) return false;

            var Effects = Instance.GetWired().GetEffects(this);
            var Conditions = Instance.GetWired().GetConditions(this);
            var addons = Instance.GetWired().GetTriggers(this)
                                     .Where(x => x.Type.ToString().StartsWith("Addon")).ToList();

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
                foreach (var Condition in Conditions.ToList())
                {
                    if (!Condition.Execute(Player)) return false;
                    Instance.GetWired().OnEvent(Condition.Item);
                }
            }

            bool hasExecuteInOrder = addons.Any(x => x.Type == WiredBoxType.AddonExecuteInOrder);
            bool hasRandomEffect = addons.Any(x => x.Type == WiredBoxType.AddonRandomEffect);
            bool hasUnseen = addons.Any(x => x.Type == WiredBoxType.AddonUnseen);

            if (hasRandomEffect)
            {
                var RandomBox = addons.FirstOrDefault(x => x.Type == WiredBoxType.AddonRandomEffect);
                if (RandomBox == null || !RandomBox.Execute()) return false;

                var SelectedBox = Instance.GetWired().GetRandomEffect(Effects.ToList());
                if (SelectedBox != null && SelectedBox.Execute(Player))
                    Instance.GetWired().OnEvent(SelectedBox.Item);

                Instance.GetWired().OnEvent(RandomBox.Item);
            }
            else if (hasUnseen)
            {
                var unseenBox = addons.FirstOrDefault(x => x.Type == WiredBoxType.AddonUnseen);
                if (unseenBox != null && unseenBox.Execute(Effects.ToList(), Player))
                    Instance.GetWired().OnEvent(unseenBox.Item);
            }
            else if (hasExecuteInOrder)
            {
                foreach (var Effect in Effects.OrderBy(x => x.Item.GetZ).ToList())
                {
                    if (!Effect.Execute(Player)) break;
                    Instance.GetWired().OnEvent(Effect.Item);
                }
            }
            else
            {
                foreach (var Effect in Effects.ToList())
                {
                    if (!Effect.Execute(Player)) continue;
                    Instance.GetWired().OnEvent(Effect.Item);
                }
            }

            Instance.GetWired().OnEvent(Item);
            return true;
        }
    }
}