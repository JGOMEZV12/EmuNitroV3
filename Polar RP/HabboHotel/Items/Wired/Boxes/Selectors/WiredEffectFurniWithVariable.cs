using System.Collections.Generic;
using System.Linq;
using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Wired.Boxes.Selectors
{
    class WiredEffectFurniWithVariable : WiredEffectVariableSelectorBase
    {
        public WiredEffectFurniWithVariable(Room instance, Item item) : base(instance, item) { }
        public override WiredBoxType Type => WiredBoxType.SelectorFurniWithVariable;

        public override void HandleSave(ClientPacket packet)
        {
            int paramsCount = packet.PopInt();
            _selectByValue = paramsCount > 0 && packet.PopInt() == 1;
            _comparison = paramsCount > 1 ? packet.PopInt() : 0;
            _referenceMode = paramsCount > 2 ? packet.PopInt() : 0;
            _referenceConstantValue = paramsCount > 3 ? packet.PopInt() : 0;
            _referenceTargetType = paramsCount > 4 ? packet.PopInt() : 0;
            _referenceUserSource = paramsCount > 5 ? packet.PopInt() : 0;
            _referenceFurniSource = paramsCount > 6 ? packet.PopInt() : 0;
            _filterExisting = paramsCount > 7 && packet.PopInt() == 1;
            _invert = paramsCount > 8 && packet.PopInt() == 1;

            string raw = packet.PopString();
            string[] parts = raw.Split('\t');
            _variableToken = parts.Length > 0 ? parts[0] : "";
            _referenceVariableToken = parts.Length > 1 ? parts[1] : "";

            packet.PopInt();
            _delay = packet.PopInt();
        }

        public override bool Execute(params object[] @params)
        {
            if (@params.Length == 0 || !(@params[0] is WiredContext context)) return false;

            var matched = new List<Item>();
            var referenceValue = GetReferenceValue(context);

            foreach (var it in Instance.GetRoomItemHandler().GetFloor)
            {
                if (_selectByValue)
                {
                    int? val = WiredVariableResolver.ResolveValue(Instance, _variableToken, null, it, context);
                    if (!MatchesComparison(val, referenceValue)) continue;
                }
                matched.Add(it);
            }

            context.SelectedItems = matched;
            return true;
        }
    }
}
