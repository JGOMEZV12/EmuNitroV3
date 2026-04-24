using System;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboRoleplay.Comodin
{
    /// <summary>
    /// Structure for comodin
    /// </summary>
    public class Comodin
    {
        #region Variables
        public int ID;
        public int FurniID;
        public int RoomID;
        public string Action;
        #endregion

        /// <summary>
        /// Comodin constructor
        /// </summary>
        public Comodin(int ID, int FurniID, int RoomID, string Action)
        {
            this.ID = ID;
            this.FurniID = FurniID;
            this.RoomID = RoomID;
            this.Action = Action;
        }
    }
}
