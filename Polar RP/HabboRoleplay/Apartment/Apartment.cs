using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Items;

using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboRoleplay.Apartments
{
    public class Apartment
    {
        public int ID;
        public string ModelName;
        public int Tiles;
        public string Image;
        public int Price; // <= 100 = PL | > 100 Credits

        public Apartment(int ID, string ModelName, int Tiles, string Image, int Price)
        {
            this.ID = ID;
            this.ModelName = ModelName;
            this.Tiles = Tiles;
            this.Image = Image;
            this.Price = Price;
        }

    }
}
