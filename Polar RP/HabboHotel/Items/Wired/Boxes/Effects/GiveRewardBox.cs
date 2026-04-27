using Polar.HabboHotel.Items.Wired;
using Polar.Communication.Packets.Outgoing;
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using Polar.Communication.Packets.Outgoing.Inventory.Furni;
using Polar.Communication.Packets.Outgoing.Catalog;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;

namespace Polar.HabboHotel.Items.Wired.Boxes.Effects
{
    class GiveRewardBox : IWiredItem
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type { get { return WiredBoxType.EffectGiveReward; } }
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        public GiveRewardBox(Room instance, Item item)
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
            if (Params == null || Params.Length == 0)
                return false;

            Habbo Owner = PolarEnvironment.GetHabboById(Item.UserID);
            if (Owner == null || !Owner.GetPermissions().HasRight("room_item_wired_rewards"))
                return false;

            Habbo Player = (Habbo)Params[0];
            if (Player == null || Player.GetClient() == null)
                return false;

            if (Player.CurrentRoom == null)
                return false;

            RoomUser User = Player.CurrentRoom.GetRoomUserManager().GetRoomUserByHabbo(Player.Username);
            if (User == null)
                return false;

            if (string.IsNullOrEmpty(StringData))
                return false;

            int amountLeft = int.Parse(this.StringData.Split('-')[2]);
            int often = int.Parse(this.StringData.Split('-')[1]);
            bool unique = this.BoolData;
            bool premied = false;

            if (amountLeft == 1)
            {
                Player.GetClient().SendNotification("Ya no hay mas premios, vuelve mas tarde.");
                return true;
            }

            foreach (var dataStr in (this.StringData.Split('-')[0]).Split(';'))
            {
                var dataArray = dataStr.Split(',');
                var isbadge = dataArray[0] == "0";
                var code = dataArray[1];
                var percentage = int.Parse(dataArray[2]);
                var random = PolarEnvironment.GetRandomNumber(0, 100);

                if (!unique && percentage < random)
                    continue;

                premied = true;

                if (isbadge)
                {
                    if (Player.GetBadgeComponent().HasBadge(code))
                        Player.GetClient().SendMessage(new WhisperComposer(User.VirtualId, "¡Oops, al parecer ya has recibido esta placa!", 0, User.LastBubble));
                    else
                    {
                        Player.GetBadgeComponent().GiveBadge(code, true, Player.GetClient());
                        // FIX: Params[2] validado antes de acceder
                        string badgeParam = Params.Length > 2 ? Params[2]?.ToString() ?? "" : "";
                        Player.GetClient().SendMessage(new RoomBubbleNotificationComposer("badge/" + badgeParam, "Acabas de recibir una placa!", "/inventory/open/badge"));
                    }
                }
                else
                {
                    ItemData ItemData = null;

                    if (!PolarEnvironment.GetGame().GetItemManager().GetItem(int.Parse(code), out ItemData))
                    {
                        Player.GetClient().SendMessage(new WhisperComposer(User.VirtualId, "No se pudo obtener Item ID: " + code, 0, User.LastBubble));
                        return false;
                    }

                    Item Item = ItemFactory.CreateSingleItemNullable(ItemData, Player.GetClient().GetHabbo(), "", "", 0, 0, 0);

                    if (Item != null)
                    {
                        Player.GetClient().GetHabbo().GetInventoryComponent().TryAddItem(Item);
                        Player.GetClient().SendMessage(new FurniListNotificationComposer(Item.Id, 1));
                        Player.GetClient().SendMessage(new PurchaseOKComposer());
                        Player.GetClient().SendMessage(new FurniListAddComposer(Item));
                        Player.GetClient().SendMessage(new FurniListUpdateComposer());
                        Player.GetClient().SendNotification("¡Has recibido un regalo! Revisa tu inventario.");
                    }
                }
            }

            if (!premied)
            {
                Player.GetClient().SendNotification("Suerte la proxima vez :(");
            }
            else if (amountLeft > 1)
            {
                amountLeft--;
                // FIX: StringData se reconstruye correctamente — asignar a Split()[i] no hace nada
                var parts = this.StringData.Split('-');
                parts[2] = amountLeft.ToString();
                this.StringData = string.Join("-", parts);
            }

            return true;
        }
    }
}
