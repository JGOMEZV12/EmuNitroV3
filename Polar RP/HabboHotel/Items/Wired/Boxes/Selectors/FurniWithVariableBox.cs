using System.Collections.Concurrent;
using System.Collections.Generic;
using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Wired.Boxes.Selectors
{
    /// <summary>
    /// Selects furni items that have a given variable (or a variable matching a comparison).
    /// Mirrors WiredEffectFurniWithVariable → WiredEffectVariableSelectorBase (TARGET_FURNI).
    /// </summary>
    class FurniWithVariableBox : IWiredItem, IWiredCustomData
    {
        // comparison constants
        private const int CMP_GREATER_THAN           = 0;
        private const int CMP_GREATER_THAN_OR_EQUAL  = 1;
        private const int CMP_EQUAL                  = 2;
        private const int CMP_LESS_THAN_OR_EQUAL     = 3;
        private const int CMP_LESS_THAN              = 4;
        private const int CMP_NOT_EQUAL              = 5;

        // reference mode
        private const int REF_CONSTANT = 0;
        private const int REF_VARIABLE = 1;

        // reference target types
        private const int TARGET_USER    = 0;
        private const int TARGET_FURNI   = 1;
        private const int TARGET_CONTEXT = 2;
        private const int TARGET_ROOM    = 3;

        // furni source for secondary selected items
        private const int SOURCE_SECONDARY_SELECTED = 101;

        public Room   Instance { get; set; }
        public Item   Item     { get; set; }
        public WiredBoxType Type => WiredBoxType.SelectorFurniWithVariable;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool   BoolData   { get; set; }
        public string ItemsData  { get; set; }

        private bool      selectByValue          = false;
        private int       comparison             = CMP_EQUAL;
        private int       referenceMode          = REF_CONSTANT;
        private int       referenceConstantValue = 0;
        private int       referenceTargetType    = TARGET_USER;
        private int       referenceUserSource    = 0; // SOURCE_TRIGGER
        private int       referenceFurniSource   = 0;
        private bool      filterExisting         = false;
        private bool      invert                 = false;
        private string    variableToken          = "";
        private int       variableItemId         = 0;
        private string    referenceVariableToken = "";
        private int       referenceVariableItemId = 0;
        private List<int> selectedItemIds        = new List<int>();
        private int       delay                  = 0;

        public FurniWithVariableBox(Room Instance, Item Item)
        {
            this.Instance  = Instance;
            this.Item      = Item;
            this.SetItems  = new ConcurrentDictionary<int, Item>();
            this.StringData = "";
        }

        public void HandleSave(ClientPacket Packet)
        {
            // intParams: [selectByValue, comparison, referenceMode, referenceConstantValue,
            //             referenceTargetType, referenceUserSource, referenceFurniSource,
            //             filterExisting, invert]
            // stringParam: variableToken\treferenceVariableToken
            // furniIds (optional secondary items)
            // delay
            int paramCount  = Packet.PopInt();
            int[] intParams = new int[paramCount];
            for (int i = 0; i < paramCount; i++) intParams[i] = Packet.PopInt();

            string strParam = Packet.PopString();

            int furniCount = Packet.PopInt();
            this.selectedItemIds = new List<int>();
            for (int i = 0; i < furniCount; i++) this.selectedItemIds.Add(Packet.PopInt());

            this.delay = Packet.PopInt();

            this.selectByValue          = intParams.Length > 0 && intParams[0] == 1;
            this.comparison             = NormalizeComparison(intParams.Length > 1 ? intParams[1] : CMP_EQUAL);
            this.referenceMode          = NormalizeReferenceMode(intParams.Length > 2 ? intParams[2] : REF_CONSTANT);
            this.referenceConstantValue = intParams.Length > 3 ? intParams[3] : 0;
            this.referenceTargetType    = NormalizeReferenceTargetType(intParams.Length > 4 ? intParams[4] : TARGET_USER);
            this.referenceUserSource    = intParams.Length > 5 ? intParams[5] : 0;
            this.referenceFurniSource   = NormalizeReferenceFurniSource(intParams.Length > 6 ? intParams[6] : 0);
            this.filterExisting         = intParams.Length > 7 && intParams[7] == 1;
            this.invert                 = intParams.Length > 8 && intParams[8] == 1;

            // stringParam is tab-delimited: variableToken\treferenceVariableToken
            string[] parts             = strParam != null ? strParam.Split('\t') : new string[0];
            this.variableToken         = NormalizeVariableToken(parts.Length > 0 ? parts[0] : "");
            this.referenceVariableToken = NormalizeVariableToken(parts.Length > 1 ? parts[1] : "");
            this.variableItemId        = GetCustomItemId(this.variableToken);
            this.referenceVariableItemId = GetCustomItemId(this.referenceVariableToken);
            this.StringData            = strParam ?? "";
        }

        public string GetWiredData()
        {
            return JsonConvert.SerializeObject(new JsonData
            {
                selectByValue           = this.selectByValue,
                comparison              = this.comparison,
                referenceMode           = this.referenceMode,
                referenceConstantValue  = this.referenceConstantValue,
                referenceTargetType     = this.referenceTargetType,
                referenceUserSource     = this.referenceUserSource,
                referenceFurniSource    = this.referenceFurniSource,
                filterExisting          = this.filterExisting,
                invert                  = this.invert,
                variableToken           = this.variableToken,
                variableItemId          = this.variableItemId,
                referenceVariableToken  = this.referenceVariableToken,
                referenceVariableItemId = this.referenceVariableItemId,
                selectedItemIds         = this.selectedItemIds,
                delay                   = this.delay
            });
        }

        public void LoadWiredData(string wiredData)
        {
            ResetData();
            if (string.IsNullOrEmpty(wiredData) || !wiredData.StartsWith("{")) return;

            var data = JsonConvert.DeserializeObject<JsonData>(wiredData);
            if (data == null) return;

            this.selectByValue          = data.selectByValue;
            this.comparison             = NormalizeComparison(data.comparison);
            this.referenceMode          = NormalizeReferenceMode(data.referenceMode);
            this.referenceConstantValue = data.referenceConstantValue;
            this.referenceTargetType    = NormalizeReferenceTargetType(data.referenceTargetType);
            this.referenceUserSource    = data.referenceUserSource;
            this.referenceFurniSource   = NormalizeReferenceFurniSource(data.referenceFurniSource);
            this.filterExisting         = data.filterExisting;
            this.invert                 = data.invert;
            this.variableToken          = NormalizeVariableToken(data.variableToken ?? (data.variableItemId > 0 ? data.variableItemId.ToString() : ""));
            this.referenceVariableToken = NormalizeVariableToken(data.referenceVariableToken ?? (data.referenceVariableItemId > 0 ? data.referenceVariableItemId.ToString() : ""));
            this.variableItemId         = GetCustomItemId(this.variableToken);
            this.referenceVariableItemId = GetCustomItemId(this.referenceVariableToken);
            this.selectedItemIds        = data.selectedItemIds ?? new List<int>();
            this.delay                  = data.delay;
            this.StringData             = this.variableToken + "\t" + this.referenceVariableToken;
        }

        public void Serialize(ServerPacket Packet)
        {
            bool showSecondaryPicker = this.selectByValue
                && this.referenceMode == REF_VARIABLE
                && this.referenceTargetType == TARGET_FURNI
                && this.referenceFurniSource == SOURCE_SECONDARY_SELECTED;

            Packet.WriteBoolean(false);
            Packet.WriteInteger(20); // MAXIMUM_FURNI_SELECTION
            int itemCount = showSecondaryPicker ? this.selectedItemIds.Count : 0;
            Packet.WriteInteger(itemCount);
            if (showSecondaryPicker)
                foreach (int id in this.selectedItemIds) Packet.WriteInteger(id);

            Packet.WriteInteger(Item.GetBaseItem().SpriteId);
            Packet.WriteInteger(Item.Id);
            Packet.WriteString(this.variableToken + "\t" + this.referenceVariableToken);
            Packet.WriteInteger(9);
            Packet.WriteInteger(this.selectByValue          ? 1 : 0);
            Packet.WriteInteger(this.comparison);
            Packet.WriteInteger(this.referenceMode);
            Packet.WriteInteger(this.referenceConstantValue);
            Packet.WriteInteger(this.referenceTargetType);
            Packet.WriteInteger(this.referenceUserSource);
            Packet.WriteInteger(this.referenceFurniSource);
            Packet.WriteInteger(this.filterExisting         ? 1 : 0);
            Packet.WriteInteger(this.invert                 ? 1 : 0);
            Packet.WriteInteger(0);
            Packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            Packet.WriteInteger(this.delay);
            Packet.WriteInteger(0);
        }

        public bool Execute(params object[] Params) => false;

        private void ResetData()
        {
            this.selectByValue           = false;
            this.comparison              = CMP_EQUAL;
            this.referenceMode           = REF_CONSTANT;
            this.referenceConstantValue  = 0;
            this.referenceTargetType     = TARGET_USER;
            this.referenceUserSource     = 0;
            this.referenceFurniSource    = 0;
            this.filterExisting          = false;
            this.invert                  = false;
            this.variableToken           = "";
            this.variableItemId          = 0;
            this.referenceVariableToken  = "";
            this.referenceVariableItemId = 0;
            this.selectedItemIds         = new List<int>();
            this.delay                   = 0;
            this.StringData              = "";
        }

        private static int NormalizeComparison(int v)
        {
            switch (v)
            {
                case CMP_GREATER_THAN:
                case CMP_GREATER_THAN_OR_EQUAL:
                case CMP_LESS_THAN_OR_EQUAL:
                case CMP_LESS_THAN:
                case CMP_NOT_EQUAL:
                    return v;
                default:
                    return CMP_EQUAL;
            }
        }

        private static int NormalizeReferenceMode(int v)   => v == REF_VARIABLE ? REF_VARIABLE : REF_CONSTANT;
        private static int NormalizeReferenceTargetType(int v)
        {
            switch (v)
            {
                case TARGET_FURNI:
                case TARGET_CONTEXT:
                case TARGET_ROOM:
                    return v;
                default:
                    return TARGET_USER;
            }
        }

        private static int NormalizeReferenceFurniSource(int v)
        {
            switch (v)
            {
                case SOURCE_SECONDARY_SELECTED:
                case 2: // SOURCE_SELECTOR
                case 3: // SOURCE_SIGNAL
                    return v;
                default:
                    return 0; // SOURCE_TRIGGER
            }
        }

        private static string NormalizeVariableToken(string token)
        {
            if (string.IsNullOrEmpty(token)) return "";
            string t = token.Trim();
            if (t.StartsWith("custom:") || t.StartsWith("internal:")) return t;
            if (int.TryParse(t, out int id) && id > 0) return "custom:" + id;
            return "";
        }

        private static int GetCustomItemId(string token)
        {
            if (token == null || !token.StartsWith("custom:")) return 0;
            int.TryParse(token.Substring("custom:".Length), out int id);
            return id;
        }

        private class JsonData
        {
            public bool      selectByValue           { get; set; }
            public int       comparison              { get; set; }
            public int       referenceMode           { get; set; }
            public int       referenceConstantValue  { get; set; }
            public int       referenceTargetType     { get; set; }
            public int       referenceUserSource     { get; set; }
            public int       referenceFurniSource    { get; set; }
            public bool      filterExisting          { get; set; }
            public bool      invert                  { get; set; }
            public string    variableToken           { get; set; }
            public int       variableItemId          { get; set; }
            public string    referenceVariableToken  { get; set; }
            public int       referenceVariableItemId { get; set; }
            public List<int> selectedItemIds         { get; set; }
            public int       delay                   { get; set; }
        }
    }
}
