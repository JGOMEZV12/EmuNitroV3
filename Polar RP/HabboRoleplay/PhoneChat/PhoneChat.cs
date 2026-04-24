using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Items;

using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboRoleplay.PhoneChat
{
    public class PhoneChat
    {
        public int ID;
        public int Type;//1 = msg 2 = Whats
        public int EmisorId;
        public string EmisorName;
        public int ReceptorId;
        public string ReceptorName;
        public string Msg;
        public DateTime TimeStamp;

        public PhoneChat(int ID, int Type, int EmisorId, string EmisorName, int ReceptorId, string ReceptorName, string Msg, DateTime TimeStamp)
        {
            this.ID = ID;
            this.Type = Type;
            this.EmisorId = EmisorId;
            this.EmisorName = EmisorName;
            this.ReceptorId = ReceptorId;
            this.ReceptorName = ReceptorName;
            this.Msg = Msg;
            this.TimeStamp = TimeStamp;
        }
        
    }
}
