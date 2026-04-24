using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.HabboHotel.Groups
{
    public class GroupLogs
    {
        public int GroupId { get; set; }
        public int UserId { get; set; }
        public string Action { get; set; }
        public int Cant { get; set; }
        public DateTime TimeStamp { get; set; }

        public GroupLogs(int GroupId, int UserId, string Action, int Cant, DateTime TimeStamp)
        {
            this.GroupId = GroupId;
            this.UserId = UserId;
            this.Action = Action;
            this.Cant = Cant;
            this.TimeStamp = TimeStamp;
        }
    }
}
