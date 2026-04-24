using Polar.Communication.Packets.Outgoing;
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Wired.Boxes.Conditions
{
    internal class FurniMatchStateAndPositionBox : IWiredItem
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type { get { return WiredBoxType.ConditionMatchStateAndPosition; } }
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        public FurniMatchStateAndPositionBox(Room Instance, Item Item)
        {
            this.Instance = Instance;
            this.Item = Item;
            this.SetItems = new ConcurrentDictionary<int, Item>();
        }

        public void HandleSave(ClientPacket Packet)
        {
            if (this.SetItems.Count > 0)
                this.SetItems.Clear();

            int Unknown = Packet.PopInt();
            int State = Packet.PopInt();
            int Direction = Packet.PopInt();
            int Placement = Packet.PopInt();
            string Unknown2 = Packet.PopString();

            int FurniCount = Packet.PopInt();
            for (int i = 0; i < FurniCount; i++)
            {
                Item SelectedItem = Instance.GetRoomItemHandler().GetItem(Packet.PopInt());
                if (SelectedItem != null)
                    SetItems.TryAdd(SelectedItem.Id, SelectedItem);
            }

            this.StringData = State + ";" + Direction + ";" + Placement;
        }

        
        public void Serialize(ServerPacket Packet)
        {
            Packet.WriteBoolean(false);
            Packet.WriteInteger(100);
            Packet.WriteInteger(SetItems.Count);
            foreach (Item Item in SetItems.Values.ToList())
            {
                Packet.WriteInteger(Item.Id);
            }
            Packet.WriteInteger(Item.GetBaseItem().SpriteId);
            Packet.WriteInteger(Item.Id);
            Packet.WriteString(StringData);
            if (String.IsNullOrEmpty(StringData)) StringData = "0;0;0";
            Packet.WriteInteger(3);
            Packet.WriteInteger(int.Parse(StringData.Split(';')[0]));
            Packet.WriteInteger(int.Parse(StringData.Split(';')[1]));
            Packet.WriteInteger(int.Parse(StringData.Split(';')[2]));
            Packet.WriteInteger(0);
            Packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
        }
        public bool Execute(params object[] Params)
        {
            if (Params.Length == 0)
                return false;

            if (string.IsNullOrEmpty(this.StringData) || this.StringData == "0;0;0" || this.SetItems.Count == 0)
                return false;

            // FIX: Null-check de ItemsData antes de Split
            if (string.IsNullOrEmpty(this.ItemsData))
                return false;

            foreach (Item Item in this.SetItems.Values.ToList())
            {
                if (Item == null)
                    continue;

                if (!Instance.GetRoomItemHandler().GetFloor.Contains(Item))
                    continue;

                foreach (string I in this.ItemsData.Split(';'))
                {
                    if (string.IsNullOrEmpty(I))
                        continue;

                    Item II = Instance.GetRoomItemHandler().GetItem(Convert.ToInt32(I.Split(':')[0]));
                    if (II == null)
                        continue;

                    string[] partsString = I.Split(':');
                    string[] part = partsString[1].Split(',');

                    if (int.Parse(this.StringData.Split(';')[0]) == 1) // State
                    {
                        try
                        {
                            if (II.ExtraData != part[4].ToString())
                                return false;
                        }
                        catch { }
                    }

                    if (int.Parse(this.StringData.Split(';')[1]) == 1) // Direction
                    {
                        try
                        {
                            if (II.Rotation != Convert.ToInt32(part[3]))
                                return false;
                        }
                        catch { }
                    }

                    if (int.Parse(this.StringData.Split(';')[2]) == 1) // Position
                    {
                        try
                        {
                            if (II.GetX != Convert.ToInt32(part[0]) || II.GetY != Convert.ToInt32(part[1]) ||
                                II.GetZ != Convert.ToDouble(part[2]))
                                return false;
                        }
                        catch { }
                    }
                }
            }
            return true;
        }
    }
}
