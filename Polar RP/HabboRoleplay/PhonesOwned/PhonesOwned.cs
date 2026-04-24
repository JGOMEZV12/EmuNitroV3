using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Items;

using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboRoleplay.PhoneOwned
{
    public class PhonesOwned
    {
        public int Id;
        public uint PhoneId;
        public int OwnerId;
        public string PhoneNumber;

        public PhonesOwned(int Id, uint PhoneId, int OwnerId, string PhoneNumber)
        {
            this.Id = Id;
            this.PhoneId = PhoneId;
            this.OwnerId = OwnerId;
            this.PhoneNumber = PhoneNumber;
        }
        
    }
}
