using System;
using System.Collections.Generic;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Pathfinding;
using Polar.Database.Interfaces;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboRoleplay.Turfs
{
    public class Turf
    {
        public Item Flag;

        public int RoomId;
        public int GangId;
        public int BeginX;
        public int BeginY;
        public int EndX;
        public int EndY;
        public int FlagX;
        public int FlagY;
        public List<ThreeDCoord> CaptureSquares;

        public bool FlagSpawned;

        public Turf(int RoomId, int GangId)
        {
            this.RoomId = RoomId;
            this.GangId = GangId;

            this.CaptureSquares = TurfCaptureSquares();
            this.FlagSpawned = false;
        }

        public List<ThreeDCoord> TurfCaptureSquares()
        {
            return RoleplayManager.GenerateMap(this.BeginX, this.BeginY, this.EndX, this.EndY);
        }

        public void SpawnFlag()
        {
            try
            {
                Misc.RoleplayManager.GenerateRoom(this.RoomId, out Room Room, false);
                if (Room != null && this.FlagSpawned)
                {
                    foreach (var Item in Room.GetRoomItemHandler().GetFloor)
                    {
                        if (Item.Id == this.Flag.Id)
                            Room.GetRoomItemHandler().RemoveFurniture(null, Item.Id);
                    }
                }
                var Gang = HabboHotel.Groups.GroupManager.GetGang(this.GangId);

                if (Gang == null)
                    this.UpdateTurf(1000);
                else
                {
                    this.Flag = Misc.RoleplayManager.PlaceItemToRoom(null, 4253, this.GangId, this.FlagX, this.FlagY, 0, 0, false, this.RoomId, false);
                    this.FlagSpawned = true;
                }
            }
            catch
            {
            }
        }

        public void UpdateTurf(int NewGang)
        {
            try
            {
                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.RunQuery("UPDATE `rp_gangs_turfs` SET `gang_id` = '" + NewGang + "' WHERE `gang_id` = '" + this.GangId + "' AND `room_id` = '" + this.RoomId + "'");
                }

                this.GangId = NewGang;
                this.SpawnFlag();
            }
            catch
            {
            }
        }
    }
}
