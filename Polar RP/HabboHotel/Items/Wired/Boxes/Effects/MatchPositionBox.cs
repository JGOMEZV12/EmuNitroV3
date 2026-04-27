using Polar.HabboHotel.Items.Wired;
using System.Collections.Generic;
using Polar.Communication.Packets.Outgoing;
using System;
using System.Collections.Concurrent;
using System.Linq;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.Core;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Wired.Boxes.Effects
{
    internal class MatchPositionBox : IWiredItem, IWiredCycle
    {
        private int _delay;
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.EffectMatchPosition;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }

        public int Delay
        {
            get => _delay;
            set
            {
                _delay = value;
                TickCount = value + 1;
            }
        }

        public int TickCount { get; set; }
        private bool _requested;
        public string ItemsData { get; set; }

        public MatchPositionBox(Room instance, Item item)
        {
            Instance = instance;
            Item = item;
            SetItems = new ConcurrentDictionary<int, Item>();
            TickCount = Delay;
            _requested = false;
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
            packet.WriteInteger(this is IWiredCycle cycle ? cycle.Delay : 0);
        }
        public bool Execute(params object[] @params)
        {
            if (!_requested)
            {
                TickCount = Delay;
                _requested = true;
            }
            return true;
        }

        public bool OnCycle()
        {
            if (!_requested || string.IsNullOrEmpty(StringData) || SetItems.Count == 0)
            {
                _requested = false;
                return false;
            }

            string[] parts = StringData.Split(';');
            if (parts.Length < 5) { _requested = false; return false; }

            bool matchState = parts[0] == "1";
            bool matchDirection = parts[1] == "1";
            bool matchPosition = parts[2] == "1";
            bool matchAltitude = parts[3] == "1";

            if (string.IsNullOrEmpty(ItemsData)) { _requested = false; return false; }

            foreach (string entry in ItemsData.Split(';'))
            {
                if (string.IsNullOrEmpty(entry)) continue;

                string[] entryParts = entry.Split(':');
                if (entryParts.Length != 2) continue;

                if (!int.TryParse(entryParts[0], out int itemId)) continue;
                Item item = Instance.GetRoomItemHandler().GetItem(itemId);
                if (item == null) continue;

                string[] coords = entryParts[1].Split(',');
                if (coords.Length < 5) continue;

                if (!int.TryParse(coords[0], out int x)) continue;
                if (!int.TryParse(coords[1], out int y)) continue;
                if (!double.TryParse(coords[2], System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out double z)) continue;
                if (!int.TryParse(coords[3], out int rotation)) continue;
                string extraData = coords[4];

                if (matchState)
                    SetState(item, extraData);

                if (matchDirection && !matchPosition)
                    SetRotation(item, rotation);

                if (matchPosition)
                    SetPosition(item, x, y, z);

                if (matchDirection && matchPosition)
                    SetRotation(item, rotation);
            }

            _requested = false;
            return true;
        }
        private void SetState(Item item, string extradata)
        {
            if (item.ExtraData == extradata)
                return;

            if (item.GetBaseItem().InteractionType == InteractionType.DICE)
                return;

            item.ExtraData = extradata;
            item.UpdateState(false, true);
        }

        private void SetRotation(Item item, int rotation)
        {
            if (item.Rotation == rotation)
                return;

            item.Rotation = rotation;
            item.UpdateState(false, true);
        }

        private void SetPosition(Item item, int coordX, int coordY, double coordZ)
        {
            Instance.SendMessage(new SlideObjectBundleComposer(item.GetX, item.GetY, item.GetZ, coordX, coordY, coordZ, 0, 0, item.Id));
            Instance.GetRoomItemHandler().SetFloorItem(item, coordX, coordY, coordZ);
        }
    }
}
