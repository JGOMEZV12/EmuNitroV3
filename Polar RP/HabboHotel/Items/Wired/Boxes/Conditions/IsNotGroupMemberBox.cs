using System;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;

namespace Polar.HabboHotel.Items.Wired.Boxes.Conditions
{
    class IsNotGroupMemberBox : IWiredItem, IWiredCustomData
    {
        private const int GROUP_CURRENT_ROOM = 0;
        private const int GROUP_SELECTED = 1;
        private const int QUANTIFIER_ALL = 0;
        private const int QUANTIFIER_ANY = 1;

        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.ConditionIsNotGroupMember;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        private int userSource = WiredBoxTypeUtility.SOURCE_TRIGGER;
        private int groupType = GROUP_CURRENT_ROOM;
        private int selectedGroupId = 0;
        private int quantifier = QUANTIFIER_ALL;

        public IsNotGroupMemberBox(Room instance, Item item)
        {
            Instance = instance;
            Item = item;
            SetItems = new ConcurrentDictionary<int, Item>();
        }

        public void HandleSave(ClientPacket packet)
        {
            int paramsCount = packet.PopInt();
            int rawUserSource = paramsCount > 0 ? packet.PopInt() : WiredBoxTypeUtility.SOURCE_TRIGGER;
            int rawGroupType = paramsCount > 1 ? packet.PopInt() : GROUP_CURRENT_ROOM;
            int rawSelectedGroup = paramsCount > 2 ? packet.PopInt() : 0;
            int rawQuantifier = paramsCount > 3 ? packet.PopInt() : QUANTIFIER_ALL;

            string strParam = packet.PopString();

            Console.WriteLine($"[IsNotGroupMemberBox] HandleSave — userSource={rawUserSource}, groupType={rawGroupType}, selectedGroupId={rawSelectedGroup}, quantifier={rawQuantifier}");

            this.userSource = rawUserSource;
            this.groupType = rawGroupType == GROUP_SELECTED ? GROUP_SELECTED : GROUP_CURRENT_ROOM;
            this.selectedGroupId = Math.Max(0, rawSelectedGroup);
            this.quantifier = rawQuantifier == QUANTIFIER_ANY ? QUANTIFIER_ANY : QUANTIFIER_ALL;
        }

        public string GetWiredData()
        {
            return JsonConvert.SerializeObject(new JsonData
            {
                userSource = this.userSource,
                groupType = this.groupType,
                selectedGroupId = this.selectedGroupId,
                quantifier = this.quantifier
            });
        }

        public void LoadWiredData(string wiredData)
        {
            ResetSettings();

            if (string.IsNullOrEmpty(wiredData)) return;

            try
            {
                if (wiredData.StartsWith("{"))
                {
                    var data = JsonConvert.DeserializeObject<JsonData>(wiredData);
                    if (data == null) return;

                    this.userSource = data.userSource;
                    this.groupType = data.groupType == GROUP_SELECTED ? GROUP_SELECTED : GROUP_CURRENT_ROOM;
                    this.selectedGroupId = Math.Max(0, data.selectedGroupId);
                    this.quantifier = data.quantifier == QUANTIFIER_ANY ? QUANTIFIER_ANY : QUANTIFIER_ALL;
                }
                else
                {
                    // Retrocompatibilidad: formato viejo era solo userSource
                    if (int.TryParse(wiredData, out int old))
                        this.userSource = old;
                }
            }
            catch
            {
                ResetSettings();
            }
        }

        public void Serialize(ServerPacket packet)
        {
            packet.WriteBoolean(false);
            packet.WriteInteger(5);
            packet.WriteInteger(0);
            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            packet.WriteString("");
            packet.WriteInteger(4);
            packet.WriteInteger(this.userSource);
            packet.WriteInteger(this.groupType);
            packet.WriteInteger(this.selectedGroupId);
            packet.WriteInteger(this.quantifier);
            packet.WriteInteger(0);
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(0);
            packet.WriteInteger(0);
        }

        public bool Execute(params object[] Params)
        {
            if (Params.Length == 0) return false;

            Habbo Player = Params[0] as Habbo;
            if (Player == null) return false;

            int targetGroupId = ResolveTargetGroupId();
            if (targetGroupId == 0) return false;

            if (Instance.RoomData.Group.IsMember(Player.Id))
                return false;
            // NOT: el jugador NO debe ser miembro del grupo
            return true;
        }

        private int ResolveTargetGroupId()
        {
            if (groupType == GROUP_SELECTED)
                return selectedGroupId;

            return Instance.RoomData?.Group?.Id ?? 0;
        }

        private void ResetSettings()
        {
            this.userSource = WiredBoxTypeUtility.SOURCE_TRIGGER;
            this.groupType = GROUP_CURRENT_ROOM;
            this.selectedGroupId = 0;
            this.quantifier = QUANTIFIER_ALL;
        }

        private class JsonData
        {
            public int userSource { get; set; }
            public int groupType { get; set; }
            public int selectedGroupId { get; set; }
            public int quantifier { get; set; }
        }
    }
}