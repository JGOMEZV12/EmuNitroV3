using Polar.HabboHotel.Items.Wired;
using System.Collections.Generic;
using Polar.Communication.Packets.Outgoing;
using System;
using System.Linq;
using System.Collections.Concurrent;
using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;

namespace Polar.HabboHotel.Items.Wired.Boxes.Effects
{
    class SetRotationBox : IWiredItem
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type { get { return WiredBoxType.EffectSetRotation; } }
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        public SetRotationBox(Room instance, Item item)
        {
            this.Instance = instance;
            this.Item = item;
            this.SetItems = new ConcurrentDictionary<int, Item>();
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

        public bool Execute(params object[] Params)
        {
            if (string.IsNullOrEmpty(StringData)) return false;

            string[] parts = StringData.Split(';');
            if (parts.Length < 2) return false;

            if (Params == null || Params.Length == 0) return false;
            Habbo Player = Params[0] as Habbo;
            if (Player?.GetClient()?.GetRoomUser() == null) return false;

            // rotationDirection: -1=none, 0-7=fixed, 8=clockwise, 9=counterclockwise
            if (int.TryParse(parts[1], out int rotDir) && rotDir >= 0)
            {
                int newRot = rotDir;
                if (rotDir == 8) // clockwise
                    newRot = (Player.GetClient().GetRoomUser().RotBody + 2) % 8;
                else if (rotDir == 9) // counter-clockwise
                    newRot = (Player.GetClient().GetRoomUser().RotBody + 6) % 8;

                Player.GetClient().GetRoomUser().SetRot(newRot, false);
            }

            return true;
        }
    }
}
