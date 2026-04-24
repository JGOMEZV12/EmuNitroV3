using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Items.Data.RentableSpace;

namespace Polar.Communication.Packets.Outgoing.Rooms.Furni.RentableSpaces
{
    public class RentableSpaceComposer : ServerPacket
    {
        public Item Item { get; }
        public GameClient Session { get; }
        public RentableSpaceComposer(Item item, GameClient session) : base(ServerPacketHeader.RentableSpaceMessageComposer)
        {
            this.Item = item;
            this.Session = session;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            /*int Cost = GetCost(Item);

            if (Item != null)
            {
                var RentableSpaceData = Item.RentableSpaceData;

                if (RentableSpaceData != null)
                {
                    packet.WriteBoolean(RentableSpaceData.Enabled);
                    packet.WriteInteger(0);
                    packet.WriteInteger(-1); // nothing??
                    packet.WriteString(PolarEnvironment.GetHabboById(RentableSpaceData.OwnerId).Username);
                    packet.WriteInteger(RentableSpaceData.TimeLeft);
                    packet.WriteInteger(Cost); // Rentable Space Cost

                    var House = PolarEnvironment.GetGame().GetHouseManager().GetHouseBySignItem(Item);

                    if (House != null)
                    {
                        if (House.OwnerId == Session.GetHabbo().Id)
                        {
                            if (!House.ForSale)
                            {
                                Session.SendWhisper("Hello " + Session.GetHabbo().Username + "! If you would like to sell your house, use the ':setprice [amount]' command and then click 'Sell Back'!");
                                return;
                            }
                            else
                            {
                                Session.SendWhisper("Hello " + Session.GetHabbo().Username + "! Your house is already for sale for $" + House.Cost + "! Use the ':setprice [amount]' command to change the price of the house!");
                                return;
                            }
                        }
                    }
                }
                else
                    WriteNullData(packet);
            }
            else
                WriteNullData(packet);*/
            return;
        }

    /*    public void WriteNullData(ServerPacket packet)
        {
            packet.WriteBoolean(true);
            packet.WriteInteger(-1);
            packet.WriteInteger(-1);
            packet.WriteString("HoloRP");
            packet.WriteInteger(360); 
            packet.WriteInteger(GetCost(null));
        }

        public int GetCost(Item Item)
        {
            if (Item == null)
                return 2000;

            if (Item.RentableSpaceData.FarmingSpace != null)
                return Item.RentableSpaceData.FarmingSpace.Cost;

            if (Item.GetBaseItem().Id == 3618)
            {
                var House = PolarEnvironment.GetGame().GetHouseManager().GetHouseBySignItem(0);

                if (House != null)
                    return House.Cost;
                else
                    return 20000;
            }

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
        }*/
    }
}