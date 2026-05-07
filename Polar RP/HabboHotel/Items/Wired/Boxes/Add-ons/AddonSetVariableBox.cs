using System;
using System.Collections.Concurrent;
using Polar.HabboHotel.Items.Wired.Config;
using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Wired.Boxes.Add_ons
{
    class AddonSetVariableBox : IWiredItem, IWiredCustomData
    {
        public const int CODE = 70;
        public const int AVAILABILITY_ROOM = 0;
        public const int AVAILABILITY_PERMANENT = 10;
        public const int AVAILABILITY_SHARED = 11;

        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.AddonSetVariable;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        private string variableName = "";
        private bool hasValue = false;
        private int availability = AVAILABILITY_ROOM;

        public string VariableName => variableName;
        public bool HasValue => hasValue;
        public int Availability => availability;

        public bool IsPermanent => availability == AVAILABILITY_PERMANENT || availability == AVAILABILITY_SHARED;
        public bool IsShared => availability == AVAILABILITY_SHARED;

        public AddonSetVariableBox(Room instance, Item item)
        {
            Instance = instance;
            Item = item;
            SetItems = new ConcurrentDictionary<int, Item>();
            StringData = "";
        }

        public void HandleSave(ClientPacket packet)
        {
            int paramsCount = packet.PopInt();
            int rawHasValue = paramsCount > 0 ? packet.PopInt() : 0;
            int rawAvail = paramsCount > 1 ? packet.PopInt() : AVAILABILITY_ROOM;

            string rawName = packet.PopString();
            string normalized = WiredVariableNameValidator.NormalizeForSave(rawName);

            Console.WriteLine($"[AddonSetVariableBox] HandleSave — name='{normalized}', hasValue={rawHasValue}, availability={rawAvail}");

            // Validar nombre
            try
            {
                WiredVariableNameValidator.ValidateDefinitionName(Instance, Item.Id, normalized);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AddonSetVariableBox] Validation error: {ex.Message}");
                return;
            }

            this.variableName = normalized;
            this.hasValue = rawHasValue == 1;
            this.availability = NormalizeAvailability(rawAvail);
            this.StringData = this.variableName;
        }

        public string GetWiredData()
        {
            return JsonConvert.SerializeObject(new JsonData
            {
                variableName = this.variableName,
                hasValue = this.hasValue,
                availability = this.availability
            });
        }

        public void LoadWiredData(string wiredData)
        {
            this.variableName = "";
            this.hasValue = false;
            this.availability = AVAILABILITY_ROOM;

            if (string.IsNullOrEmpty(wiredData)) return;

            if (wiredData.StartsWith("{"))
            {
                var data = JsonConvert.DeserializeObject<JsonData>(wiredData);
                if (data == null) return;

                this.variableName = WiredVariableNameValidator.NormalizeLegacy(data.variableName ?? "");
                this.hasValue = data.hasValue;
                this.availability = NormalizeAvailability(data.availability);
            }
            else
            {
                // Retrocompatibilidad: formato viejo era solo el nombre
                this.variableName = WiredVariableNameValidator.NormalizeLegacy(wiredData);
            }

            this.StringData = this.variableName;
        }

        public void Serialize(ServerPacket packet)
        {
            packet.WriteBoolean(false);
            packet.WriteInteger(0);
            packet.WriteInteger(0);
            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            packet.WriteString(this.variableName);
            packet.WriteInteger(2);
            packet.WriteInteger(this.hasValue ? 1 : 0);
            packet.WriteInteger(this.availability);
            packet.WriteInteger(0);
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(0);
            packet.WriteInteger(0);
        }

        public bool Execute(params object[] @params) => true;

        private static int NormalizeAvailability(int value) =>
            value == AVAILABILITY_PERMANENT || value == AVAILABILITY_SHARED
                ? value
                : AVAILABILITY_ROOM;

        private class JsonData
        {
            public string variableName { get; set; }
            public bool hasValue { get; set; }
            public int availability { get; set; }
        }
    }

    // ── WiredVariableNameValidator ───────────────────────────────────────────
   
}