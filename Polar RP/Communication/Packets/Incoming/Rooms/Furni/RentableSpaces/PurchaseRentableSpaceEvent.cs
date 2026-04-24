using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Items;
using Polar.Communication.Packets.Outgoing.Rooms.Furni.RentableSpaces;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Houses;
using Polar.Communication.Packets.Outgoing.Rooms.Session;
using Polar.HabboRoleplay.Farming;
using Polar.HabboHotel.Groups;

namespace Polar.Communication.Packets.Incoming.Rooms.Furni.RentableSpaces
{
    internal class PurchaseRentableSpaceEvent : IPacketEvent
    {
        public void Parse(GameClient Session, ClientPacket Packet)
        {
            /*int ItemId = Packet.PopInt();
            var Room = Session.GetHabbo().CurrentRoom;
            Item Item = null;

            if (Room != null)
                Item = Room.GetRoomItemHandler().GetItem(ItemId);

            if (Item != null)
            {
                if (Item.GetBaseItem().Id == 3618)
                    ProcessHousePurchase(Session, Item);
                else if (Item.RentableSpaceData != null && Item.RentableSpaceData.FarmingSpace != null)
                    ProcessFarmingPurchase(Session, Item);
                else if (Item.RentableSpaceData != null)
                {
                    if (CanPurchase(Session, Item))
                    {
                        ProcessPurchase(Session, Item);
                        return;
                    }
                    else
                    {
                        Session.SendNotification("¡Ya has comprado un terreno en esta habitación!");
                        return;
                    }
                }
                else
                {
                    Session.SendNotification("Algo sucedió, la compra no se pudo hacer ahora mismo.");
                    return;
                }
            }
        }

        public void ProcessFarmingPurchase(GameClient Session, Item Item)
        {
            if (Item != null && Item.RentableSpaceData != null && Item.RentableSpaceData.FarmingSpace != null)
            {
                if (Item.RentableSpaceData.FarmingSpace.OwnerId <= 0)
                {
                    lock (FarmingManager.FarmingSpaces)
                    {
                        bool HasSpace = FarmingManager.FarmingSpaces.Values.Where(x => x != null && x.OwnerId == Session.GetHabbo().Id).ToList().Count > 0;

                        if (!HasSpace)
                        {
                            if (!GroupManager.HasJobCommand(Session, "farming"))
                            {
                                Session.SendNotification("¿Sólo un agricultor puede alquilar una parcela de tierra!");
                                return;
                            }

                            if (!Session.GetRoleplay().IsWorking)
                            {
                                Session.SendNotification("¡Usted debe estar trabajando para comprar una parcela de tierra!");
                                return;
                            }

                            if (!Session.GetRoleplay().FarmingStats.HasPlantSatchel && !Session.GetRoleplay().FarmingStats.HasSeedSatchel)
                            {
                                Session.SendNotification("¡Antes de que usted pueda comenzar a cultivar, usted necesita comprar una planta Satchel y la semilla Satchel en el supermercado! (ID:111)");
                                return;
                            }

                            int Cost = Item.RentableSpaceData.FarmingSpace.Cost;

                            Session.GetHabbo().Credits -= Cost;
                            Session.GetHabbo().UpdateCreditsBalance();

                            Item.RentableSpaceData.FarmingSpace.BuySpace(Session, Item);
                            Session.Shout("*Compra el espacio agrícola por $" + Cost + "*", 4);
                            Session.SendMessage(new RentableSpaceComposer(Item, Session));
                            return;
                        }
                        else
                        {
                            Session.SendWhisper("¡Usted no puede comprar más de una parcela en un momento!", 1);
                            return;
                        }
                    }
                }
                else
                {
                    Session.SendNotification("¡Este espacio agrícola no está a la venta!");
                    return;
                }
            }
        }


        public void ProcessHousePurchase(GameClient Session, Item Item)
        {
            var House = PolarEnvironment.GetGame().GetHouseManager().GetHouseBySignItem(Item);

            if (House != null)
            {
                if (CanPurchase(Session, House))
                {
                    Item.RentableSpaceData.Enabled = true;
                    Item.RentableSpaceData.OwnerId = Session.GetHabbo().Id;

                    if (House.OwnerId != Session.GetHabbo().Id)
                    {
                        int Cost = GetCost(House);

                        Session.GetHabbo().Credits -= Cost;
                        Session.GetHabbo().UpdateCreditsBalance();
                        House.BuyHouse(Session, Cost);
                        Session.Shout("*Compra la casa por $" + Cost + "*", 4);
                    }
                    else
                    {
                        House.BuyHouse(Session, 0);
                        Session.Shout("*Toma su casa del mercado*", 4);
                    }

                    UnloadRoom(House);
                    Session.SendMessage(new RentableSpaceComposer(Item, Session));
                }
                else
                {
                    Session.SendNotification("Usted puede solamente poseer una casa a la vez");
                    return;
                }
            }
        }

        public bool CanPurchase(GameClient Session, Item Item)
        {
            if (Item.RentableSpaceData.Enabled == true)
                return false;

            var Room = Item.GetRoom();
            var SpaceItems = Room.GetRoomItemHandler().GetFloor.Where(x => x.GetBaseItem().InteractionType == InteractionType.RENTABLE_SPACE).ToList();

            bool AlreadyOwns = false;
            if (SpaceItems.Count > 1)
            {
                foreach (var item in SpaceItems)
                {
                    if (item.RentableSpaceData == null)
                        continue;

                    if (item.RentableSpaceData.OwnerId == Session.GetHabbo().Id)
                    { 
                        AlreadyOwns = true;
                        break;
                    }
                }
            }

            if (AlreadyOwns)
                return false;
            else
                return true;
        }

        public bool CanPurchase(GameClient Session, House House)
        {
            if (!House.ForSale)
                return false;

            if (House.OwnerId == Session.GetHabbo().Id)
                return true;

            if (PolarEnvironment.GetGame().GetHouseManager().HouseList.Values.Where(x => x.OwnerId == Session.GetHabbo().Id).ToList().Count > 0)
                return false;

            return true;
        }

        public void ProcessPurchase(GameClient Session, Item Item)
        {
            int Cost = GetCost(Item);

            Session.GetHabbo().Credits -= Cost;
            Session.GetHabbo().UpdateCreditsBalance();

            Item.RentableSpaceData.Enabled = true;
            Item.RentableSpaceData.OwnerId = Session.GetHabbo().Id;
            Item.RentableSpaceData.TimeLeft = (30 * 24 * 60 * 60); // 30 Days in Seconds
            Item.RentableSpaceData.UpdateData();

            Session.Shout("*Compra la parcela de terreno por $" + Cost + "*", 4);
            Session.SendMessage(new RentableSpaceComposer(Item, Session));
        }

        public int GetCost(Item Item)
        {
            if (Item == null)
                return 2000;

            int Cost;
            string ItemName = Item.GetBaseItem().ItemName;

            switch (ItemName.ToLower())
            {
                // 3x4 Space
                case "hblooza_spacerent3x4":
                    {
                        Cost = 250;
                        break;
                    }
                // 5x5 Space
                case "hblooza_spacerent5x5":
                    {
                        Cost = 500;
                        break;
                    }
                // 6x6 Space
                case "hblooza_spacerent6x6":
                    {
                        Cost = 1000;
                        break;
                    }
                // 7x7 Space
                case "hblooza_spacerent7x7":
                    {
                        Cost = 2000;
                        break;
                    }
                // Any Other Size
                default:
                    {
                        Cost = 2000;
                        break;
                    }
            }
            return Cost;
        }

        public int GetCost(House House)
        {
            if (House != null)
                return House.Cost;
            else
                return 20000;
        }

        public void UnloadRoom(House House)
        {
            if (House == null)
                return;

            Room Room = null;
            if (!PolarEnvironment.GetGame().GetRoomManager().TryGetRoom(House.RoomId, out Room))
                return;

            List<RoomUser> UsersToReturn = Room.GetRoomUserManager().GetRoomUsers().ToList();

            PolarEnvironment.GetGame().GetRoomManager().UnloadRoom(Room, true);

            foreach (RoomUser User in UsersToReturn)
            {
                if (User == null || User.GetClient() == null)
                    continue;

				User.GetClient().SendMessage(new RoomForwardComposer(House.Sign.RoomId));
                //RoleplayManager.SendUser(User.GetClient(), House.Sign.RoomId, "La casa acaba de ser comprada, por lo que fueron enviados de vuelta");
            }
            */

            return;

        }
    }
}
