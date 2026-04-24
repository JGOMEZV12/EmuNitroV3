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
            if (SetItems.Count > 0)
                SetItems.Clear();

            int IntCount = packet.PopInt();
            int State = packet.PopInt();
            int Direction = packet.PopInt();
            int Position = packet.PopInt();
            int Altitude = packet.PopInt();   // ← faltaba
            int FurniSource = packet.PopInt();  // ← faltaba
            string Unknown = packet.PopString();

            int furniCount = packet.PopInt();
            for (int i = 0; i < furniCount; i++)
            {
                Item selectedItem = Instance.GetRoomItemHandler().GetItem(packet.PopInt());
                if (selectedItem != null)
                    SetItems.TryAdd(selectedItem.Id, selectedItem);
            }

            // Guardar posición actual de cada item seleccionado (snapshot)
            // formato: "itemId:x,y,z,rotation,extradata;"
            var sb = new System.Text.StringBuilder();
            foreach (var item in SetItems.Values)
                sb.Append($"{item.Id}:{item.GetX},{item.GetY},{item.GetZ},{item.Rotation},{item.ExtraData};");
            ItemsData = sb.ToString();

            StringData = $"{State};{Direction};{Position};{Altitude};{FurniSource}";
            Delay = packet.PopInt();
        }

        public void Serialize(ServerPacket Packet)
        {
            if (string.IsNullOrEmpty(StringData)) StringData = "0;0;0;0;0";
            string[] parts = StringData.Split(';');
            int state = parts.Length > 0 ? int.Parse(parts[0]) : 0;
            int direction = parts.Length > 1 ? int.Parse(parts[1]) : 0;
            int position = parts.Length > 2 ? int.Parse(parts[2]) : 0;
            int altitude = parts.Length > 3 ? int.Parse(parts[3]) : 0;
            int furniSource = parts.Length > 4 ? int.Parse(parts[4]) : 0;

            Packet.WriteBoolean(false);
            Packet.WriteInteger(100);
            Packet.WriteInteger(SetItems.Count);
            foreach (Item item in SetItems.Values.ToList())
                Packet.WriteInteger(item.Id);

            Packet.WriteInteger(Item.GetBaseItem().SpriteId);
            Packet.WriteInteger(Item.Id);
            Packet.WriteString("");
            Packet.WriteInteger(5);
            Packet.WriteInteger(state);
            Packet.WriteInteger(direction);
            Packet.WriteInteger(position);
            Packet.WriteInteger(altitude);
            Packet.WriteInteger(furniSource);
            Packet.WriteInteger(0);
            Packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            Packet.WriteInteger(this.Delay);
            Packet.WriteInteger(0);
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
