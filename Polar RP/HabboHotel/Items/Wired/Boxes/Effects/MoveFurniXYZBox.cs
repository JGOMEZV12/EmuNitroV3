using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.HabboHotel.Rooms;
using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Linq;

namespace Polar.HabboHotel.Items.Wired.Boxes.Effects
{
    class MoveFurniXYZBox : IWiredItem, IWiredCycle
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.EffectMoveFurniXYZ; // Reusing for compatibility if needed
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        public int Delay { get; set; }
        public int TickCount { get; set; }
        private long _next = 0;
        private bool _requested = false;

        public MoveFurniXYZBox(Room instance, Item item)
        {
            Instance = instance;
            Item = item;
            SetItems = new ConcurrentDictionary<int, Item>();
            StringData = "0;0;0;0"; // X;Y;Z;Rot
        }

        public void HandleSave(ClientPacket packet)
        {
            int unknown = packet.PopInt();
            int x = packet.PopInt();
            int y = packet.PopInt();
            string z = packet.PopString();
            int rot = packet.PopInt();
            int furniCount = packet.PopInt();

            SetItems.Clear();
            for (int i = 0; i < furniCount; i++)
            {
                Item selected = Instance.GetRoomItemHandler().GetItem(packet.PopInt());
                if (selected != null) SetItems.TryAdd(selected.Id, selected);
            }

            StringData = $"{x};{y};{z};{rot}";
            Delay = packet.PopInt();
        }

        public void Serialize(ServerPacket packet)
        {
            string[] data = StringData.Split(';');
            packet.WriteBoolean(false);
            packet.WriteInteger(100);
            packet.WriteInteger(SetItems.Count);
            foreach (Item item in SetItems.Values) packet.WriteInteger(item.Id);
            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            packet.WriteString("");
            packet.WriteInteger(3);
            packet.WriteInteger(int.Parse(data[0]));
            packet.WriteInteger(int.Parse(data[1]));
            packet.WriteInteger(int.Parse(data[3]));
            packet.WriteString(data[2]);
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(Delay);
            packet.WriteInteger(0);
        }

        public bool Execute(params object[] @params)
        {
            if (SetItems.Count == 0) return false;
            _next = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (Delay * 500);
            _requested = true;
            TickCount = Delay;
            return true;
        }

        public bool OnCycle()
        {
            if (!_requested || _next > DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()) return false;

            string[] data = StringData.Split(';');
            int moveX = int.Parse(data[0]);
            int moveY = int.Parse(data[1]);
            double moveZ = double.Parse(data[2]);
            int moveRot = int.Parse(data[3]);

            foreach (Item item in SetItems.Values.ToList())
            {
                if (item == null || !Instance.GetRoomItemHandler().GetFloor.Contains(item)) continue;

                int nextX = item.GetX + moveX;
                int nextY = item.GetY + moveY;
                double nextZ = item.GetZ + moveZ;
                int nextRot = item.Rotation + moveRot;
                while (nextRot > 7) nextRot -= 8;
                while (nextRot < 0) nextRot += 8;

                if (Instance.GetGameMap().CanRollItemHere(nextX, nextY))
                {
                    Instance.SendMessage(new SlideObjectBundleComposer(item.GetX, item.GetY, item.GetZ, nextX, nextY, nextZ, 0, 0, item.Id));
                    Instance.GetRoomItemHandler().SetFloorItem(item, nextX, nextY, nextZ);
                    item.Rotation = nextRot;
                    item.UpdateState();
                }
            }

            _requested = false;
            return true;
        }
    }
}
