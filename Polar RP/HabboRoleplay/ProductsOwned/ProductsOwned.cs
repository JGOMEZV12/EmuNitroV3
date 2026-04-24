using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Items;

using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboRoleplay.ProductOwned
{
    public class ProductsOwned
    {
        public int Id;
        public int ProductId;
        public int UserId;
        public string Extradata;

        public ProductsOwned(int Id, int ProductId, int UserId, string Extradata)
        {
            this.Id = Id;
            this.ProductId = ProductId;
            this.UserId = UserId;
            this.Extradata = Extradata;
        }
        
    }
}
