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

        public void HandleSave(ClientPacket Packet)
        {
            int IntCount = Packet.PopInt();
            int MovementDirection = Packet.PopInt();
            int RotationDirection = Packet.PopInt();
            int UserSource = Packet.PopInt();
            string Unknown = Packet.PopString();
            int Delay = Packet.PopInt();

            this.StringData = $"{MovementDirection};{RotationDirection};{UserSource}";
            // this.Delay = Delay; // si implementás IWiredCycle
        }

        public void Serialize(ServerPacket Packet)
        {
            if (string.IsNullOrEmpty(StringData)) StringData = "-1;-1;0";
            string[] parts = StringData.Split(';');
            int movDir = parts.Length > 0 ? int.Parse(parts[0]) : -1;
            int rotDir = parts.Length > 1 ? int.Parse(parts[1]) : -1;
            int userSrc = parts.Length > 2 ? int.Parse(parts[2]) : 0;

            Packet.WriteBoolean(false);
            Packet.WriteInteger(5);
            Packet.WriteInteger(0);
            Packet.WriteInteger(Item.GetBaseItem().SpriteId);
            Packet.WriteInteger(Item.Id);
            Packet.WriteString("");
            Packet.WriteInteger(3);
            Packet.WriteInteger(movDir);
            Packet.WriteInteger(rotDir);
            Packet.WriteInteger(userSrc);
            Packet.WriteInteger(0);
            Packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            Packet.WriteInteger(0); // delay
            Packet.WriteInteger(0);
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
