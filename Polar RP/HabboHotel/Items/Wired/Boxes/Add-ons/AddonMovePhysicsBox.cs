using System;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Instance;

namespace Polar.HabboHotel.Items.Wired.Boxes.Add_ons
{
    class AddonMovePhysicsBox : IWiredItem, IWiredCustomData
    {
        public const int SOURCE_ALL_ROOM = 900;

        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.AddonMovePhysics;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        private bool keepAltitude = false;
        private bool moveThroughFurni = false;
        private bool moveThroughUsers = false;
        private bool blockByFurni = false;
        private int moveThroughFurniSource = WiredSourceUtil.SOURCE_TRIGGER;
        private int blockByFurniSource = WiredSourceUtil.SOURCE_TRIGGER;
        private int moveThroughUsersSource = WiredSourceUtil.SOURCE_TRIGGER;

        public bool KeepAltitude => keepAltitude;
        public bool MoveThroughFurni => moveThroughFurni;
        public bool MoveThroughUsers => moveThroughUsers;
        public bool BlockByFurni => blockByFurni;
        public int MoveThroughFurniSource => moveThroughFurniSource;
        public int MoveThroughUsersSource => moveThroughUsersSource;
        public int BlockByFurniSource => blockByFurniSource;

        public AddonMovePhysicsBox(Room instance, Item item)
        {
            Instance = instance;
            Item = item;
            SetItems = new ConcurrentDictionary<int, Item>();
            StringData = "";
        }

        public void HandleSave(ClientPacket packet)
        {
            // intParams → string (sin furnis)
            int paramsCount = packet.PopInt();

            bool rawKeepAlt = ReadFlag(packet, paramsCount, 0);
            bool rawMoveFurni = ReadFlag(packet, paramsCount, 1);
            bool rawMoveUsers = ReadFlag(packet, paramsCount, 2);
            bool rawBlockFurni = ReadFlag(packet, paramsCount, 3);
            int rawMoveFurniSrc = ReadInt(packet, paramsCount, 4, WiredSourceUtil.SOURCE_TRIGGER);
            int rawBlockFurniSrc = ReadInt(packet, paramsCount, 5, WiredSourceUtil.SOURCE_TRIGGER);
            int rawMoveUsersSrc = ReadInt(packet, paramsCount, 6, WiredSourceUtil.SOURCE_TRIGGER);

            string strParam = packet.PopString();

            Console.WriteLine($"[AddonMovePhysicsBox] HandleSave — keepAlt={rawKeepAlt}, moveFurni={rawMoveFurni}, moveUsers={rawMoveUsers}, blockFurni={rawBlockFurni}");

            this.keepAltitude = rawKeepAlt;
            this.moveThroughFurni = rawMoveFurni;
            this.moveThroughUsers = rawMoveUsers;
            this.blockByFurni = rawBlockFurni;
            this.moveThroughFurniSource = NormalizeSource(rawMoveFurniSrc);
            this.blockByFurniSource = NormalizeSource(rawBlockFurniSrc);
            this.moveThroughUsersSource = NormalizeSource(rawMoveUsersSrc);
        }

        public string GetWiredData()
        {
            return JsonConvert.SerializeObject(new JsonData
            {
                keepAltitude = this.keepAltitude,
                moveThroughFurni = this.moveThroughFurni,
                moveThroughUsers = this.moveThroughUsers,
                blockByFurni = this.blockByFurni,
                moveThroughFurniSource = this.moveThroughFurniSource,
                blockByFurniSource = this.blockByFurniSource,
                moveThroughUsersSource = this.moveThroughUsersSource
            });
        }

        public void LoadWiredData(string wiredData)
        {
            Reset();

            if (string.IsNullOrEmpty(wiredData)) return;

            if (wiredData.StartsWith("{"))
            {
                var data = JsonConvert.DeserializeObject<JsonData>(wiredData);
                if (data == null) return;

                this.keepAltitude = data.keepAltitude;
                this.moveThroughFurni = data.moveThroughFurni;
                this.moveThroughUsers = data.moveThroughUsers;
                this.blockByFurni = data.blockByFurni;
                this.moveThroughFurniSource = NormalizeSource(data.moveThroughFurniSource);
                this.blockByFurniSource = NormalizeSource(data.blockByFurniSource);
                this.moveThroughUsersSource = NormalizeSource(data.moveThroughUsersSource);
            }
            else
            {
                // Retrocompatibilidad: "val\tval\tval\t..."
                var parts = wiredData.Split('\t');
                this.keepAltitude = ReadLegacyFlag(parts, 0);
                this.moveThroughFurni = ReadLegacyFlag(parts, 1);
                this.moveThroughUsers = ReadLegacyFlag(parts, 2);
                this.blockByFurni = ReadLegacyFlag(parts, 3);
                this.moveThroughFurniSource = NormalizeSource(ReadLegacyInt(parts, 4, WiredSourceUtil.SOURCE_TRIGGER));
                this.blockByFurniSource = NormalizeSource(ReadLegacyInt(parts, 5, WiredSourceUtil.SOURCE_TRIGGER));
                this.moveThroughUsersSource = NormalizeSource(ReadLegacyInt(parts, 6, WiredSourceUtil.SOURCE_TRIGGER));
            }
        }

        public void Serialize(ServerPacket packet)
        {
            packet.WriteBoolean(false);
            packet.WriteInteger(0);
            packet.WriteInteger(0);
            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            packet.WriteString("");
            packet.WriteInteger(7);
            packet.WriteInteger(keepAltitude ? 1 : 0);
            packet.WriteInteger(moveThroughFurni ? 1 : 0);
            packet.WriteInteger(moveThroughUsers ? 1 : 0);
            packet.WriteInteger(blockByFurni ? 1 : 0);
            packet.WriteInteger(moveThroughFurniSource);
            packet.WriteInteger(blockByFurniSource);
            packet.WriteInteger(moveThroughUsersSource);
            packet.WriteInteger(0);
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(0);
            packet.WriteInteger(0);
        }

        public bool Execute(params object[] @params) => true;

        private void Reset()
        {
            keepAltitude = false;
            moveThroughFurni = false;
            moveThroughUsers = false;
            blockByFurni = false;
            moveThroughFurniSource = WiredSourceUtil.SOURCE_TRIGGER;
            moveThroughUsersSource = WiredSourceUtil.SOURCE_TRIGGER;
            blockByFurniSource = WiredSourceUtil.SOURCE_TRIGGER;
        }

        private static bool ReadFlag(ClientPacket packet, int count, int index) =>
            count > index && packet.PopInt() == 1;

        private static int ReadInt(ClientPacket packet, int count, int index, int fallback) =>
            count > index ? packet.PopInt() : fallback;

        private static bool ReadLegacyFlag(string[] parts, int index) =>
            ReadLegacyInt(parts, index, 0) == 1;

        private static int ReadLegacyInt(string[] parts, int index, int fallback)
        {
            if (parts.Length <= index) return fallback;
            return int.TryParse(parts[index], out int v) ? v : fallback;
        }

        private static int NormalizeSource(int value) =>
            value == SOURCE_ALL_ROOM ||
            value == WiredSourceUtil.SOURCE_TRIGGER ||
            value == WiredSourceUtil.SOURCE_SELECTOR
                ? value
                : WiredSourceUtil.SOURCE_TRIGGER;

        private class JsonData
        {
            public bool keepAltitude { get; set; }
            public bool moveThroughFurni { get; set; }
            public bool moveThroughUsers { get; set; }
            public bool blockByFurni { get; set; }
            public int moveThroughFurniSource { get; set; }
            public int blockByFurniSource { get; set; }
            public int moveThroughUsersSource { get; set; }
        }
    }
}