using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Items.Wired.Config;
using Polar.HabboHotel.Rooms;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Polar.HabboHotel.Items.Wired.Boxes.Add_ons
{
    class AddonVariableReferenceBox : IWiredItem, IWiredCustomData
    {

        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.AddonVariableReference;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        private string variableName = "";
        private int sourceRoomId = 0;
        private string sourceRoomName = "";
        private int sourceVariableItemId = 0;
        private string sourceVariableName = "";
        private int sourceTargetType = WiredVariableReferenceSupport.TARGET_USER;
        private bool hasValue = false;
        private bool readOnly = true;

        public string VariableName => variableName;
        public int SourceRoomId => sourceRoomId;
        public string SourceRoomName => sourceRoomName;
        public int SourceVariableItemId => sourceVariableItemId;
        public string SourceVariableName => sourceVariableName;
        public int SourceTargetType => sourceTargetType;
        public bool HasValue => hasValue;
        public bool IsReadOnly => readOnly;
        public int Availability => WiredVariableReferenceSupport.SHARED_AVAILABILITY;
        public bool IsUserReference => sourceTargetType == WiredVariableReferenceSupport.TARGET_USER;
        public bool IsRoomReference => sourceTargetType == WiredVariableReferenceSupport.TARGET_ROOM;

        public AddonVariableReferenceBox(Room instance, Item item)
        {
            Instance = instance;
            Item = item;
            SetItems = new ConcurrentDictionary<int, Item>();
            StringData = "";
        }

        public void HandleSave(ClientPacket packet)
        {
            int paramsCount = packet.PopInt();
            for (int i = 0; i < paramsCount; i++) packet.PopInt();

            string strParam = packet.PopString();
            var config = ParseConfigData(strParam);
            string normalizedName = WiredVariableNameValidator.NormalizeForSave(config.variableName);

            try { WiredVariableNameValidator.ValidateDefinitionName(Instance, Item.Id, normalizedName); }
            catch (Exception ex) { Console.WriteLine($"[AddonVariableReferenceBox] Validation: {ex.Message}"); return; }

            if (config.sourceRoomId <= 0 || config.sourceVariableItemId <= 0)
            {
                Console.WriteLine("[AddonVariableReferenceBox] Missing source variable");
                return;
            }

            var definition = WiredVariableReferenceSupport.FindSharedDefinition(
                Instance, config.sourceRoomId, config.sourceVariableItemId, config.sourceTargetType);

            if (definition == null)
            {
                Console.WriteLine("[AddonVariableReferenceBox] Invalid source variable");
                return;
            }

            this.variableName = normalizedName;
            this.sourceRoomId = definition.RoomId;
            this.sourceRoomName = SanitizeLabel(definition.RoomName);
            this.sourceVariableItemId = definition.ItemId;
            this.sourceVariableName = definition.Name;
            this.sourceTargetType = definition.TargetType;
            this.hasValue = definition.HasValue;
            this.readOnly = config.readOnly;
            this.StringData = this.variableName;
        }

        public string GetWiredData()
        {
            return JsonConvert.SerializeObject(new JsonData
            {
                variableName = this.variableName,
                sourceRoomId = this.sourceRoomId,
                sourceRoomName = this.sourceRoomName,
                sourceVariableItemId = this.sourceVariableItemId,
                sourceVariableName = this.sourceVariableName,
                sourceTargetType = this.sourceTargetType,
                hasValue = this.hasValue,
                readOnly = this.readOnly
            });
        }

        public void LoadWiredData(string wiredData)
        {
            Reset();
            if (string.IsNullOrEmpty(wiredData) || !wiredData.StartsWith("{")) return;

            var data = JsonConvert.DeserializeObject<JsonData>(wiredData);
            if (data == null) return;

            this.variableName = WiredVariableNameValidator.NormalizeLegacy(data.variableName ?? "");
            this.sourceRoomId = Math.Max(0, data.sourceRoomId);
            this.sourceRoomName = SanitizeLabel(data.sourceRoomName);
            this.sourceVariableItemId = Math.Max(0, data.sourceVariableItemId);
            this.sourceVariableName = WiredVariableNameValidator.NormalizeLegacy(data.sourceVariableName ?? "");
            this.sourceTargetType = NormalizeTargetType(data.sourceTargetType);
            this.hasValue = data.hasValue;
            this.readOnly = data.readOnly;
            this.StringData = this.variableName;
        }

        public void Serialize(ServerPacket packet)
        {
            packet.WriteBoolean(false);
            packet.WriteInteger(0);
            packet.WriteInteger(0);
            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            packet.WriteString(BuildEditorPayload());
            packet.WriteInteger(0);
            packet.WriteInteger(0);
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(0);
            packet.WriteInteger(0);
        }

        public bool Execute(params object[] @params) => true;

        private string BuildEditorPayload()
        {
            var roomOptions = new List<RoomEditorData>();

            foreach (var option in WiredVariableReferenceSupport.LoadRoomOptions(Instance))
            {
                var variables = new List<VariableEditorData>();
                foreach (var def in option.Variables)
                    variables.Add(new VariableEditorData { itemId = def.ItemId, name = def.Name, targetType = def.TargetType, hasValue = def.HasValue });

                roomOptions.Add(new RoomEditorData { roomId = option.RoomId, roomName = option.RoomName, variables = variables });
            }

            return JsonConvert.SerializeObject(new EditorPayload
            {
                variableName = this.variableName,
                sourceRoomId = this.sourceRoomId,
                sourceRoomName = this.sourceRoomName,
                sourceVariableItemId = this.sourceVariableItemId,
                sourceVariableName = this.sourceVariableName,
                sourceTargetType = this.sourceTargetType,
                readOnly = this.readOnly,
                rooms = roomOptions
            });
        }

        private void Reset()
        {
            variableName = "";
            sourceRoomId = 0;
            sourceRoomName = "";
            sourceVariableItemId = 0;
            sourceVariableName = "";
            sourceTargetType = WiredVariableReferenceSupport.TARGET_USER;
            hasValue = false;
            readOnly = true;
            StringData = "";
        }

        private static ConfigData ParseConfigData(string value)
        {
            if (string.IsNullOrEmpty(value) || !value.StartsWith("{")) return new ConfigData();
            var config = JsonConvert.DeserializeObject<ConfigData>(value);
            return config ?? new ConfigData();
        }

        private static int NormalizeTargetType(int value) =>
            value == WiredVariableReferenceSupport.TARGET_ROOM
                ? WiredVariableReferenceSupport.TARGET_ROOM
                : WiredVariableReferenceSupport.TARGET_USER;

        private static string SanitizeLabel(string value) =>
            value == null ? "" : value.Trim().Replace("\t", "").Replace("\r", "").Replace("\n", "");

        private class JsonData
        {
            public string variableName { get; set; }
            public int sourceRoomId { get; set; }
            public string sourceRoomName { get; set; }
            public int sourceVariableItemId { get; set; }
            public string sourceVariableName { get; set; }
            public int sourceTargetType { get; set; }
            public bool hasValue { get; set; }
            public bool readOnly { get; set; }
        }

        private class ConfigData
        {
            public string variableName { get; set; } = "";
            public int sourceRoomId { get; set; } = 0;
            public int sourceVariableItemId { get; set; } = 0;
            public int sourceTargetType { get; set; } = WiredVariableReferenceSupport.TARGET_USER;
            public bool readOnly { get; set; } = true;
        }

        private class EditorPayload : ConfigData
        {
            public string sourceRoomName { get; set; }
            public string sourceVariableName { get; set; }
            public List<RoomEditorData> rooms { get; set; }
        }

        private class RoomEditorData
        {
            public int roomId { get; set; }
            public string roomName { get; set; }
            public List<VariableEditorData> variables { get; set; }
        }

        private class VariableEditorData
        {
            public int itemId { get; set; }
            public string name { get; set; }
            public int targetType { get; set; }
            public bool hasValue { get; set; }
        }
    }
}