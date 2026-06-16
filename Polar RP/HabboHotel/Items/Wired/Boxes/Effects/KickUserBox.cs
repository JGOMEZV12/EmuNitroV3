using Polar.HabboHotel.Items.Wired;
using Polar.Communication.Packets.Outgoing;
using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboHotel.Items.Wired.Boxes.Effects
{
    class KickUserBox : IWiredItem, IWiredCycle
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type { get { return WiredBoxType.EffectKickUser; } }
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public int TickCount { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public int Delay { get; set; }
        public string ItemsData { get; set; }
        private Queue _toKick;

        public KickUserBox(Room instance, Item item)
        {
            this.Instance = instance;
            this.Item = item;
            this.SetItems = new ConcurrentDictionary<int, Item>();
            this.TickCount = Delay;
            this._toKick = new Queue();
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
            if (Params.Length != 1)
                return false;

            Habbo Player = (Habbo)Params[0];
            if (Player == null)
                return false;

            if (this.TickCount <= 0)
                this.TickCount = 3;

            if (!this._toKick.Contains(Player))
            {
                RoomUser User = Instance.GetRoomUserManager().GetRoomUserByHabbo(Player.Id);
                if (User == null)
                    return false;

                if (Player.GetPermissions().HasRight("mod_tool") || this.Instance.OwnerId == Player.Id)
                {
                    Player.GetClient().SendMessage(new WhisperComposer(User.VirtualId, "Wired Kick Exception: Unkickable Player", 0, 0));
                    return false;
                }

                this._toKick.Enqueue(Player);
                Player.GetClient().SendMessage(new WhisperComposer(User.VirtualId, this.StringData, 0, 0));
            }
            return true;
        }

        public bool OnCycle()
        {
            if (Instance == null)
                return false;

            if (this._toKick.Count == 0)
            {
                this.TickCount = 3;
                return true;
            }

            lock (this._toKick.SyncRoot)
            {
                while (this._toKick.Count > 0)
                {
                    Habbo Player = (Habbo)this._toKick.Dequeue();
                    if (Player == null || !Player.InRoom || Player.CurrentRoom != Instance)
                        continue;

                    var House = PolarEnvironment.GetGame().GetHouseManager().GetHouseByInsideRoom(Player.CurrentRoom.RoomId);
                    var ApartInside = PolarEnvironment.GetGame().GetApartmentOwnedManager().GetApartmentByInsideRoom(Player.CurrentRoom.RoomId);

                    #region Casa
                    if (House != null)
                    {
                        Player.GetClient().GetRoleplay().ExitingHouse = true;
                        Player.GetClient().GetRoleplay().HouseX = House.DoorX;
                        Player.GetClient().GetRoleplay().HouseY = House.DoorY;
                        Player.GetClient().GetRoleplay().HouseZ = House.DoorZ;
                        RoleplayManager.SendUserOld(Player.GetClient(), House.RoomId, "Te han echado de la casa.");
                    }
                    #endregion

                    #region Apartament
                    else if (ApartInside != null)
                    {
                        RoleplayManager.SendUserOld(Player.GetClient(), ApartInside.LobbyId, "Te han echado del apartamento.");
                    }
                    #endregion
                }
            }

            this.TickCount = 3;
            return true;
        }
    }
}
