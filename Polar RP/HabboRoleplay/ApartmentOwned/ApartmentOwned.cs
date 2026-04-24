using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Items;

using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboRoleplay.ApartmentsOwned
{
    public class ApartmentOwned
    {
        public int ID;
        public int ApartId;
        public int RoomId;
        public int LobbyId;
        public int Owner;
        public bool ForSale;
        public int Price;
        public string PaymentType;
        public bool FloorEditor;

        public ApartmentOwned(int ID, int ApartId, int RoomId, int LobbyId, int Owner, bool ForSale, int Price, string PaymentType, bool FloorEditor)
        {
            this.ID = ID;
            this.ApartId = ApartId;
            this.RoomId = RoomId;
            this.LobbyId = LobbyId;
            this.Owner = Owner;
            this.ForSale = ForSale;
            this.Price = Price;
            this.PaymentType = PaymentType;
            this.FloorEditor = FloorEditor;
        }

    }
}
