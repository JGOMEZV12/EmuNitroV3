using Polar.Communication.Packets.Outgoing;
using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;
using Polar.Communication.Packets.Incoming;

namespace Polar.HabboHotel.Items.Wired.Boxes.Effects
{
    class BotCommunicatesToAllBox : IWiredItem
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type { get { return WiredBoxType.EffectBotCommunicatesToAllBox; } }
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        public BotCommunicatesToAllBox(Room Instance, Item Item)
        {
            this.Instance = Instance;
            this.Item = Item;
            this.SetItems = new ConcurrentDictionary<int, Item>();
        }

        public void HandleSave(ClientPacket Packet)
        {
            int IntCount = Packet.PopInt();    // cuántos ints vienen (= 1)
            int ChatMode = Packet.PopInt();    // 0 = talk, 1 = shout

            // botSource NO viene del cliente en este wired, va siempre en 0
            int BotSource = 0;

            string ChatConfig = Packet.PopString(); // "botName\tmessage"

            Console.WriteLine($"[BotTalk] HandleSave recibido:");
            Console.WriteLine($"  IntCount:   {IntCount}");
            Console.WriteLine($"  ChatMode:   {ChatMode}");
            Console.WriteLine($"  ChatConfig: '{ChatConfig}'");

            if (!ChatConfig.Contains("\t"))
            {
                Console.WriteLine($"  [ERROR] ChatConfig no contiene TAB, abortando.");
                return;
            }

            string[] parts = ChatConfig.Split('\t');
            if (parts.Length != 2)
            {
                Console.WriteLine($"  [ERROR] Se esperaban 2 partes, se recibieron {parts.Length}");
                return;
            }

            Console.WriteLine($"  BotName:  '{parts[0]}'");
            Console.WriteLine($"  Message:  '{parts[1]}'");

            this.StringData = ChatMode + ";" + BotSource + ";" + parts[0] + ";" + parts[1];

            Console.WriteLine($"  StringData guardado: '{this.StringData}'");

            if (this.SetItems.Count > 0)
                this.SetItems.Clear();
        }
        public void Serialize(ServerPacket Packet)
        {
            // Parsear StringData para reconstruir lo que el cliente espera
            string botName = "";
            string message = "";
            int mode = 0;
            int botSource = 0;

            if (!string.IsNullOrEmpty(this.StringData))
            {
                string[] parts = this.StringData.Split(';');
                if (parts.Length == 4)
                {
                    mode = int.Parse(parts[0]);
                    botSource = int.Parse(parts[1]);
                    botName = parts[2];
                    message = parts[3];
                }
            }

            Packet.WriteBoolean(false);
            Packet.WriteInteger(5);
            Packet.WriteInteger(0);
            Packet.WriteInteger(Item.GetBaseItem().SpriteId);
            Packet.WriteInteger(Item.Id);
            Packet.WriteString(botName + "\t" + message);  // formato esperado por el cliente
            Packet.WriteInteger(2);
            Packet.WriteInteger(mode);
            Packet.WriteInteger(botSource);
            Packet.WriteInteger(0);
            Packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            Packet.WriteInteger(0); // delay
            Packet.WriteInteger(0);
        }
        public bool Execute(params object[] Params)
        {
            if (string.IsNullOrEmpty(this.StringData))
                return false;

            string[] parts = this.StringData.Split(';');
            if (parts.Length != 4)
                return false;

            int mode = int.Parse(parts[0]);
            string botName = parts[2];
            string message = parts[3];

            RoomUser botUser = this.Instance.GetRoomUserManager().GetBotByName(botName);
            if (botUser == null)
                return false;

            if (mode == 1)
                botUser.Chat(message, true, 2, "black");
            else
                botUser.Chat(message, false, 2, "black");

            return true;
        }
    }
}
