using System;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Games.Teams;
using Polar.HabboHotel.Users;

namespace Polar.HabboHotel.Items.Wired.Boxes.Conditions
{
    class ActorIsInTeamBox : IWiredItem, IWiredCustomData
    {
        private const int QUANTIFIER_ALL = 0;
        private const int QUANTIFIER_ANY = 1;

        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.ConditionActorIsInTeamBox;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        private int teamColor = 1; // 1=RED, 2=GREEN, 3=BLUE, 4=YELLOW
        private int userSource = WiredBoxTypeUtility.SOURCE_TRIGGER;
        private int quantifier = QUANTIFIER_ALL;

        public ActorIsInTeamBox(Room instance, Item item)
        {
            Instance = instance;
            Item = item;
            SetItems = new ConcurrentDictionary<int, Item>();
            StringData = "";
        }

        public void HandleSave(ClientPacket packet)
        {
            int paramsCount = packet.PopInt();
            int rawTeam = paramsCount > 0 ? packet.PopInt() : 1;
            int rawSource = paramsCount > 1 ? packet.PopInt() : WiredBoxTypeUtility.SOURCE_TRIGGER;
            int rawQuant = paramsCount > 2 ? packet.PopInt() : QUANTIFIER_ALL;

            string strParam = packet.PopString();

            Console.WriteLine($"[ActorIsInTeamBox] HandleSave — team={rawTeam}, userSource={rawSource}, quantifier={rawQuant}");

            this.teamColor = rawTeam;
            this.userSource = rawSource;
            this.quantifier = rawQuant == QUANTIFIER_ANY ? QUANTIFIER_ANY : QUANTIFIER_ALL;
            this.StringData = this.teamColor.ToString();
        }

        public string GetWiredData()
        {
            return JsonConvert.SerializeObject(new JsonData
            {
                teamColor = this.teamColor,
                userSource = this.userSource,
                quantifier = this.quantifier
            });
        }

        public void LoadWiredData(string wiredData)
        {
            this.teamColor = 1;
            this.userSource = WiredBoxTypeUtility.SOURCE_TRIGGER;
            this.quantifier = QUANTIFIER_ALL;

            if (string.IsNullOrEmpty(wiredData)) return;

            if (wiredData.StartsWith("{"))
            {
                var data = JsonConvert.DeserializeObject<JsonData>(wiredData);
                if (data == null) return;

                this.teamColor = data.teamColor;
                this.userSource = data.userSource;
                this.quantifier = data.quantifier == QUANTIFIER_ANY ? QUANTIFIER_ANY : QUANTIFIER_ALL;
            }
            else
            {
                // Retrocompatibilidad: formato viejo era solo el teamColor
                if (int.TryParse(wiredData, out int old))
                    this.teamColor = old;

                this.userSource = WiredBoxTypeUtility.SOURCE_TRIGGER;
                this.quantifier = QUANTIFIER_ANY;
            }

            this.StringData = this.teamColor.ToString();
        }

        public void Serialize(ServerPacket packet)
        {
            packet.WriteBoolean(false);
            packet.WriteInteger(5);
            packet.WriteInteger(0);
            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            packet.WriteString("");
            packet.WriteInteger(3);
            packet.WriteInteger(this.teamColor);
            packet.WriteInteger(this.userSource);
            packet.WriteInteger(this.quantifier);
            packet.WriteInteger(0);
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(0);
            packet.WriteInteger(0);
        }

        public bool Execute(params object[] Params)
        {
            if (Params.Length == 0 || Instance == null) return false;

            Habbo Player = Params[0] as Habbo;
            if (Player == null) return false;

            RoomUser User = Instance.GetRoomUserManager().GetRoomUserByHabbo(Player.Id);
            if (User == null) return false;

            return teamColor switch
            {
                1 => User.Team == TEAM.RED,
                2 => User.Team == TEAM.GREEN,
                3 => User.Team == TEAM.BLUE,
                4 => User.Team == TEAM.YELLOW,
                _ => false
            };
        }

        private class JsonData
        {
            public int teamColor { get; set; }
            public int userSource { get; set; }
            public int quantifier { get; set; }
        }
    }
}