using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Instance;
using Polar.HabboHotel.Users;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using Polar.HabboRoleplay.Misc;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Polar.HabboHotel.Items.Wired.Boxes.Effects
{
    internal class KickUserBox : IWiredItem, IWiredCycle, IWiredCustomData
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.EffectKickUser;
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
        private int _userSource = WiredSourceUtil.SOURCE_TRIGGER;
        private bool _requested;
        private readonly ConcurrentQueue<Habbo> _toKick = new ConcurrentQueue<Habbo>();

        public KickUserBox(Room instance, Item item)
        {
            Instance = instance;
            Item = item;
            SetItems = new ConcurrentDictionary<int, Item>();
            TickCount = Delay;
            _requested = false;
        }

        // ── Serialización ──────────────────────────────────────────────────────

        public string GetWiredData()
        {
            return JsonConvert.SerializeObject(new JsonData
            {
                message = StringData ?? "",
                delay = Delay,
                userSource = _userSource
            });
        }

        public void LoadWiredData(string wiredData)
        {
            StringData = "";
            _userSource = WiredSourceUtil.SOURCE_TRIGGER;
            Delay = 0;

            if (string.IsNullOrEmpty(wiredData)) return;

            if (wiredData.StartsWith("{"))
            {
                var data = JsonConvert.DeserializeObject<JsonData>(wiredData);
                if (data == null) return;

                StringData = data.message ?? "";
                _userSource = data.userSource;
                Delay = data.delay;
            }
            else
            {
                // Retrocompatibilidad con formato viejo (tab-separated)
                var parts = wiredData.Split('\t');
                if (parts.Length >= 1 && int.TryParse(parts[0], out int delay))
                    Delay = delay;
                if (parts.Length >= 2)
                    StringData = parts[1];

                _userSource = WiredSourceUtil.SOURCE_TRIGGER;
            }

            TickCount = Delay;
        }

        // ── Packet handling ────────────────────────────────────────────────────

        public void HandleSave(ClientPacket packet)
        {
            SetItems.Clear();

            int intCount = packet.PopInt();
            _userSource = intCount > 0 ? packet.PopInt() : WiredSourceUtil.SOURCE_TRIGGER;
            for (int i = 1; i < intCount; i++) packet.PopInt(); // consumir extras

            StringData = packet.PopString();
            Delay = packet.PopInt();
        }

        public void Serialize(ServerPacket packet)
        {
            packet.WriteBoolean(false);
            packet.WriteInteger(5);
            packet.WriteInteger(0); // sin furni seleccionable
            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            packet.WriteString(StringData ?? "");
            packet.WriteInteger(1);          // un parámetro int
            packet.WriteInteger(_userSource);
            packet.WriteInteger(0);
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(Delay);
            packet.WriteInteger(0);
        }

        // ── Lógica ─────────────────────────────────────────────────────────────

        public bool Execute(params object[] Params)
        {
            if (Params.Length < 1) return false;

            Habbo player = Params[0] as Habbo;
            if (player == null) return false;

            RoomUser user = Instance.GetRoomUserManager().GetRoomUserByHabbo(player.Id);
            if (user == null) return false;

            // Inmunes: mods y dueño de sala
            if (player.GetPermissions().HasRight("mod_tool") || Instance.OwnerId == player.Id)
            {
                player.GetClient().SendMessage(
                    new WhisperComposer(user.VirtualId,
                        "Wired Kick Exception: Unkickable Player", 0, 0));
                return false;
            }

            // Evitar duplicados en cola
            if (_toKick.Contains(player)) return true;

            if (!string.IsNullOrEmpty(StringData))
                player.GetClient().SendMessage(
                    new WhisperComposer(user.VirtualId, StringData, 0, 0));

            _toKick.Enqueue(player);

            if (!_requested)
            {
                TickCount = Delay;
                _requested = true;
            }

            return true;
        }

        public bool OnCycle()
        {
            if (Instance == null) return false;
            if (!_requested) return false;
            if (_toKick.IsEmpty)
            {
                _requested = false;
                return true;
            }

            while (_toKick.TryDequeue(out Habbo player))
            {
                if (player == null || !player.InRoom || player.CurrentRoom != Instance)
                    continue;

                var house = PolarEnvironment.GetGame()
                    .GetHouseManager().GetHouseByInsideRoom(player.CurrentRoom.RoomId);
                var apart = PolarEnvironment.GetGame()
                    .GetApartmentOwnedManager().GetApartmentByInsideRoom(player.CurrentRoom.RoomId);

                if (house != null)
                {
                    player.GetClient().GetRoleplay().ExitingHouse = true;
                    player.GetClient().GetRoleplay().HouseX = house.DoorX;
                    player.GetClient().GetRoleplay().HouseY = house.DoorY;
                    player.GetClient().GetRoleplay().HouseZ = house.DoorZ;
                    RoleplayManager.SendUserOld(player.GetClient(), house.RoomId,
                        "Te han echado de la casa.");
                }
                else if (apart != null)
                {
                    RoleplayManager.SendUserOld(player.GetClient(), apart.LobbyId,
                        "Te han echado del apartamento.");
                }
                else
                {
                    // Sala normal — kick estándar
                    Instance.GetRoomUserManager()
                        .RemoveUserFromRoom(player.GetClient(), true);
                }
            }

            _requested = false;
            return true;
        }

        // ── DTO JSON ───────────────────────────────────────────────────────────

        private class JsonData
        {
            public string message { get; set; }
            public int delay { get; set; }
            public int userSource { get; set; }
        }
    }
}