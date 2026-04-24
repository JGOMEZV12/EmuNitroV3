using System;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboRoleplay.VehiclesJobs
{
    /// <summary>
    /// Structure for weapons
    /// </summary>
    public class VehicleJobs
    {
        #region Variables
        public int ID;
        public int RoomID;
        public int BaseItem;
        public int X;
        public int Y;
        public double Z;
        public int Rot;
        public int JobID;
        #endregion

        /// <summary>
        /// VehicleJobs constructor
        /// </summary>
        public VehicleJobs(int ID, int RoomID, int BaseItem, int X, int Y, double Z, int Rot, int JobID)
        {
            this.ID = ID;
            this.RoomID = RoomID;
            this.BaseItem = BaseItem;
            this.X = X;
            this.Y = Y;
            this.Z = Z;
            this.Rot = Rot;
            this.JobID = JobID;
        }
    }
}
