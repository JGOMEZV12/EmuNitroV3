using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;

namespace Polar.HabboHotel.Items.Wired.Boxes.Effects
{
    class ShowMessageBox : IWiredItem, IWiredCycle, IWiredCustomData
    {
        private const int VISIBILITY_SOURCE_USERS = 0;
        private const int VISIBILITY_ALL_ROOM_USERS = 1;
        private const int DEFAULT_BUBBLE = 34; // WIRED bubble

        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.EffectShowMessage;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        public int Delay
        {
            get => _delay;
            set { _delay = value; TickCount = value + 1; }
        }
        public int TickCount { get; set; }
        private int _delay;

        private string message = "";
        private int userSource = WiredBoxTypeUtility.SOURCE_TRIGGER;
        private int visibilitySelection = VISIBILITY_SOURCE_USERS;
        private int bubbleStyle = DEFAULT_BUBBLE;

        public ShowMessageBox(Room instance, Item item)
        {
            Instance = instance;
            Item = item;
            SetItems = new ConcurrentDictionary<int, Item>();
        }

        public void HandleSave(ClientPacket packet)
        {
            // orden: intParams → string → delay → (sin furnis)
            int paramsCount = packet.PopInt();
            int rawUserSource = paramsCount > 0 ? packet.PopInt() : WiredBoxTypeUtility.SOURCE_TRIGGER;
            int rawVisibility = paramsCount > 1 ? packet.PopInt() : VISIBILITY_SOURCE_USERS;
            int rawBubble = paramsCount > 2 ? packet.PopInt() : DEFAULT_BUBBLE;

            string rawMessage = packet.PopString();
            int rawDelay = packet.PopInt();

            Console.WriteLine($"[ShowMessageBox] HandleSave — userSource={rawUserSource}, visibility={rawVisibility}, bubble={rawBubble}, delay={rawDelay}, message='{rawMessage}'");

            this.userSource = rawUserSource;
            this.visibilitySelection = rawVisibility == VISIBILITY_ALL_ROOM_USERS
                ? VISIBILITY_ALL_ROOM_USERS
                : VISIBILITY_SOURCE_USERS;
            this.bubbleStyle = rawBubble;
            this.message = rawMessage;
            this.StringData = rawMessage;
            this.Delay = rawDelay;
        }

        public string GetWiredData()
        {
            return JsonConvert.SerializeObject(new JsonData
            {
                message = this.message,
                delay = this.Delay,
                userSource = this.userSource,
                visibilitySelection = this.visibilitySelection,
                bubbleStyle = this.bubbleStyle
            });
        }

        public void LoadWiredData(string wiredData)
        {
            this.message = "";
            this.userSource = WiredBoxTypeUtility.SOURCE_TRIGGER;
            this.visibilitySelection = VISIBILITY_SOURCE_USERS;
            this.bubbleStyle = DEFAULT_BUBBLE;
            this.Delay = 0;

            if (string.IsNullOrEmpty(wiredData)) return;

            if (wiredData.StartsWith("{"))
            {
                var data = JsonConvert.DeserializeObject<JsonData>(wiredData);
                if (data == null) return;

                this.message = data.message ?? "";
                this.Delay = data.delay;
                this.userSource = data.userSource;
                this.visibilitySelection = data.visibilitySelection == VISIBILITY_ALL_ROOM_USERS
                    ? VISIBILITY_ALL_ROOM_USERS
                    : VISIBILITY_SOURCE_USERS;
                this.bubbleStyle = data.bubbleStyle;
            }
            else
            {
                // Retrocompatibilidad: "delay\tmessage"
                var parts = wiredData.Split('\t');
                if (parts.Length >= 2 && int.TryParse(parts[0], out int delay))
                {
                    this.Delay = delay;
                    this.message = parts[1];
                }

                this.userSource = WiredBoxTypeUtility.SOURCE_TRIGGER;
                this.visibilitySelection = VISIBILITY_SOURCE_USERS;
                this.bubbleStyle = DEFAULT_BUBBLE;
            }

            this.StringData = this.message;
        }

        public void Serialize(ServerPacket packet)
        {
            packet.WriteBoolean(false);
            packet.WriteInteger(0); // sin furnis
            packet.WriteInteger(0);
            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            packet.WriteString(this.message);
            packet.WriteInteger(3);
            packet.WriteInteger(this.userSource);
            packet.WriteInteger(this.visibilitySelection);
            packet.WriteInteger(this.bubbleStyle);
            packet.WriteInteger(0);
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(this.Delay);
            packet.WriteInteger(0); // invalidTriggers
        }

        public bool Execute(params object[] Params)
        {
            if (Params == null || Params.Length == 0) return false;

            Habbo Player = Params[0] as Habbo;
            if (Player == null || Player.GetClient() == null || string.IsNullOrWhiteSpace(this.message))
                return false;

            if (Player.CurrentRoom == null) return false;

            RoomUser User = Player.CurrentRoom.GetRoomUserManager().GetRoomUserByHabbo(Player.Id);
            if (User == null) return false;

            string msg = BuildMessage(Player);

            if (visibilitySelection == VISIBILITY_ALL_ROOM_USERS)
            {
                // Enviar a todos en la sala
                foreach (var roomUser in Instance.GetRoomUserManager().GetRoomUsers().ToList())
                {
                    var habbo = roomUser?.GetClient()?.GetHabbo();
                    if (habbo == null) continue;

                    RoomUser target = Instance.GetRoomUserManager().GetRoomUserByHabbo(habbo.Id);
                    if (target != null)
                        habbo.GetClient().SendMessage(new WhisperComposer(target.VirtualId, msg, 0, bubbleStyle));
                }
            }
            else
            {
                Player.GetClient().SendMessage(new WhisperComposer(User.VirtualId, msg, 0, bubbleStyle));
            }

            return true;
        }

        public bool OnCycle() => true;

        private string BuildMessage(Habbo player)
        {
            string msg = this.message;

            msg = msg.Replace("%USERNAME%", player.Username);
            msg = msg.Replace("%ROOMNAME%", player.CurrentRoom?.Name ?? "");
            msg = msg.Replace("%USERCOUNT%", player.CurrentRoom?.UserCount.ToString() ?? "0");
            msg = msg.Replace("%USERSONLINE%", PolarEnvironment.GetGame().GetClientManager().Count.ToString());
            msg = msg.Replace("{username}", player.Username);

            var rp = player.GetClient()?.GetRoleplay();
            if (rp != null)
            {
                msg = msg.Replace("{job}", Polar.HabboHotel.Groups.GroupManager.GetJob(rp.JobId)?.Name ?? "Ninguno");
                msg = msg.Replace("{gang}", Polar.HabboHotel.Groups.GroupManager.GetGang(rp.GangId)?.Name ?? "Ninguna");
                msg = msg.Replace("{health}", rp.CurHealth.ToString());
                msg = msg.Replace("{maxhealth}", rp.MaxHealth.ToString());
                msg = msg.Replace("{armor}", rp.Armor.ToString());
                msg = msg.Replace("{energy}", rp.CurEnergy.ToString());
                msg = msg.Replace("{maxenergy}", rp.MaxEnergy.ToString());
                msg = msg.Replace("{hunger}", rp.Hunger.ToString());
                msg = msg.Replace("{hygiene}", rp.Hygiene.ToString());
                msg = msg.Replace("{poop}", rp.Poop.ToString());
                msg = msg.Replace("{level}", rp.Level.ToString());
                msg = msg.Replace("{exp}", rp.LevelEXP.ToString());
                msg = msg.Replace("{money}", player.Credits.ToString());
                msg = msg.Replace("{bank}", rp.BankChequings.ToString());
                msg = msg.Replace("{intelligence}", rp.Intelligence.ToString());
                msg = msg.Replace("{strength}", rp.Strength.ToString());
                msg = msg.Replace("{stamina}", rp.Stamina.ToString());
            }

            return msg;
        }

        private class JsonData
        {
            public string message { get; set; }
            public int delay { get; set; }
            public int userSource { get; set; }
            public int visibilitySelection { get; set; }
            public int bubbleStyle { get; set; }
        }
    }
}