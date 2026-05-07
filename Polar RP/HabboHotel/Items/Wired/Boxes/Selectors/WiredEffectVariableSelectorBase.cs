using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;

namespace Polar.HabboHotel.Items.Wired.Boxes.Selectors
{
    abstract class WiredEffectVariableSelectorBase : IWiredItem
    {
        public const int COMPARISON_EQUAL = 0;
        public const int COMPARISON_NOT_EQUAL = 1;
        public const int COMPARISON_GREATER_THAN = 2;
        public const int COMPARISON_LESS_THAN = 3;

        public const int REFERENCE_CONSTANT = 0;
        public const int REFERENCE_VARIABLE = 1;

        public const int TARGET_USER = 0;
        public const int TARGET_FURNI = 1;

        public Room Instance { get; set; }
        public Item Item { get; set; }
        public abstract WiredBoxType Type { get; }
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        protected bool _selectByValue = false;
        protected int _comparison = COMPARISON_EQUAL;
        protected int _referenceMode = REFERENCE_CONSTANT;
        protected int _referenceConstantValue = 0;
        protected int _referenceTargetType = TARGET_USER;
        protected int _referenceUserSource = 0;
        protected int _referenceFurniSource = 0;
        protected bool _filterExisting = false;
        protected bool _invert = false;

        protected string _variableToken = "";
        protected string _referenceVariableToken = "";
        protected int _delay = 0;

        public WiredEffectVariableSelectorBase(Room instance, Item item)
        {
            Instance = instance;
            Item = item;
            SetItems = new ConcurrentDictionary<int, Item>();
        }

        public abstract void HandleSave(ClientPacket packet);

        public virtual void Serialize(ServerPacket packet)
        {
            packet.WriteBoolean(false);
            packet.WriteInteger(5);
            packet.WriteInteger(SetItems.Count);
            foreach (var item in SetItems.Values) packet.WriteInteger(item.Id);
            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            packet.WriteString(_variableToken + "\t" + _referenceVariableToken);
            packet.WriteInteger(9);
            packet.WriteInteger(_selectByValue ? 1 : 0);
            packet.WriteInteger(_comparison);
            packet.WriteInteger(_referenceMode);
            packet.WriteInteger(_referenceConstantValue);
            packet.WriteInteger(_referenceTargetType);
            packet.WriteInteger(_referenceUserSource);
            packet.WriteInteger(_referenceFurniSource);
            packet.WriteInteger(_filterExisting ? 1 : 0);
            packet.WriteInteger(_invert ? 1 : 0);
            packet.WriteInteger(0);
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(_delay);
            packet.WriteInteger(0);
        }

        public abstract bool Execute(params object[] @params);

        protected int GetReferenceValue(WiredContext ctx)
        {
            if (_referenceMode == REFERENCE_CONSTANT) return _referenceConstantValue;

            Habbo targetUser = null;
            Item targetItem = null;

            if (_referenceTargetType == TARGET_USER)
            {
                if (_referenceUserSource == WiredBoxTypeUtility.SOURCE_TRIGGER) targetUser = ctx.Triggerer;
                else if (_referenceUserSource == WiredBoxTypeUtility.SOURCE_SELECTED) targetUser = ctx.SelectedUsers.FirstOrDefault();
            }
            else
            {
                if (_referenceFurniSource == WiredBoxTypeUtility.SOURCE_TRIGGER) targetItem = ctx.SelectedItems.FirstOrDefault(); // placeholder
                else if (_referenceFurniSource == WiredBoxTypeUtility.SOURCE_SELECTED) targetItem = ctx.SelectedItems.FirstOrDefault();
            }

            return WiredVariableResolver.ResolveValue(Instance, _referenceVariableToken, targetUser, targetItem, ctx) ?? 0;
        }

        protected bool MatchesComparison(int? value, int reference)
        {
            if (value == null) return false;
            switch (_comparison)
            {
                case COMPARISON_EQUAL: return value == reference;
                case COMPARISON_NOT_EQUAL: return value != reference;
                case COMPARISON_GREATER_THAN: return value > reference;
                case COMPARISON_LESS_THAN: return value < reference;
                default: return false;
            }
        }
    }
}
