using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;

namespace Polar.HabboHotel.Items.Wired.Boxes.Add_ons
{
    class AddonFilterUsersByVariableBox : IWiredItem
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.AddonFilterUsersByVariable;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        private string _variableToken = "";
        private string _referenceVariableToken = "";
        private int _comparison = 0;
        private int _referenceMode = 0;
        private int _referenceConstantValue = 0;

        public AddonFilterUsersByVariableBox(Room instance, Item item)
        {
            Instance = instance;
            Item = item;
            SetItems = new ConcurrentDictionary<int, Item>();
        }

        public void HandleSave(ClientPacket packet)
        {
            int paramsCount = packet.PopInt();
            _comparison = paramsCount > 0 ? packet.PopInt() : 0;
            _referenceMode = paramsCount > 1 ? packet.PopInt() : 0;
            _referenceConstantValue = paramsCount > 2 ? packet.PopInt() : 0;

            string raw = packet.PopString();
            string[] parts = raw.Split('\t');
            _variableToken = parts.Length > 0 ? parts[0] : "";
            _referenceVariableToken = parts.Length > 1 ? parts[1] : "";
        }

        public void Serialize(ServerPacket packet)
        {
            packet.WriteBoolean(false);
            packet.WriteInteger(0);
            packet.WriteInteger(0);
            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            packet.WriteString(_variableToken + "\t" + _referenceVariableToken);
            packet.WriteInteger(3);
            packet.WriteInteger(_comparison);
            packet.WriteInteger(_referenceMode);
            packet.WriteInteger(_referenceConstantValue);
            packet.WriteInteger(0);
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(0);
            packet.WriteInteger(0);
        }

        public bool Execute(params object[] @params)
        {
            if (@params.Length == 0 || !(@params[0] is WiredContext context)) return true;

            int referenceValue = _referenceMode == 0 ? _referenceConstantValue :
                WiredVariableResolver.ResolveValue(Instance, _referenceVariableToken, context.Triggerer, null, context) ?? 0;

            context.SelectedUsers = context.SelectedUsers.Where(u => {
                int? val = WiredVariableResolver.ResolveValue(Instance, _variableToken, u, null, context);
                return MatchesComparison(val, referenceValue);
            }).ToList();

            return true;
        }

        private bool MatchesComparison(int? value, int reference)
        {
            if (value == null) return false;
            switch (_comparison)
            {
                case 0: return value == reference;
                case 1: return value != reference;
                case 2: return value > reference;
                case 3: return value < reference;
                default: return false;
            }
        }
    }
}
