using System;

namespace Polar.HabboHotel.Groups
{
    public class GroupMember
    {
        #region Variables
        public int GroupId { get; set; }
        public int UserId { get; set; }

        public int UserRank { get; set; }
        public bool IsAdmin { get; set; }
        #endregion

        public GroupMember(int GroupId, int UserId, int UserRank, bool IsAdmin)
        {
            this.GroupId = GroupId;
            this.UserId = UserId;
            this.UserRank = UserRank;
            this.IsAdmin = IsAdmin;
        }
    }
}