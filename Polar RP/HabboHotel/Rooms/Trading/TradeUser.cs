using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Items;
using System.Collections.Generic;

namespace Polar.HabboHotel.Rooms
{
    public class TradeUser
    {
        public int UserId;
        private readonly int RoomId;
        private bool Accepted;
        public List<Item> OfferedItems;

        public bool HasAccepted
        {
            get
            {
                return this.Accepted;
            }
            set
            {
                this.Accepted = value;
            }
        }

        public TradeUser(int UserId, int RoomId)
        {
            this.UserId = UserId;
            this.RoomId = RoomId;
            this.Accepted = false;
            this.OfferedItems = new List<Item>();
        }

        public RoomUser GetRoomUser()
        {
            Room room = PolarEnvironment.GetGame().GetRoomManager().GetRoom(this.RoomId);
            if (room == null)
                return (RoomUser)null;
            else
                return room.GetRoomUserManager().GetRoomUserByHabboId(this.UserId);
        }

        public GameClient GetClient()
        {
            return PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(this.UserId);
        }
    }
}