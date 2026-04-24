using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Rooms;
using Polar.HabboRoleplay.Misc;
using Polar.Core;

namespace Polar.HabboRoleplay.Gambling
{
    public class TexasHoldEmItem
    {
        public int RoomId;
        public int ItemId;     
        public int X;
        public int Y;
        public double Z;
        public int Rotation;

        public Item Furni;
        public bool Rolled;
        public int Value;

        public TexasHoldEmItem(int RoomId, int ItemId, int X, int Y, double Z, int Rotation)
        {
            this.RoomId = RoomId;
            this.ItemId = ItemId;
            this.X = X;
            this.Y = Y;
            this.Z = Z;
            this.Rotation = Rotation;
            this.Furni = null;
            this.Rolled = false;
            this.Value = 0;
        }

        public void SpawnDice()
        {
            try
            {
                //Logging.LogRPGamesDebug("Attempting to generate room with ID: " + this.RoomId);
                if (!RoleplayManager.GenerateRoom(this.RoomId, out Room Room))
                {
                    Logging.LogRPGamesError("Failed to generate room with ID: " + this.RoomId);
                    return;
                }

                //Logging.LogRPGamesDebug("Attempting to place item with ID: " + this.ItemId);
                this.Furni = RoleplayManager.PlaceItemToRoom(null, this.ItemId, 0, this.X, this.Y, this.Z, this.Rotation, false, this.RoomId, false, "0", false, "", null, null, this);
                if (this.Furni == null)
                {
                    Logging.LogRPGamesError("Failed to place item with ID: " + this.ItemId);
                    return;
                }

                //Logging.LogRPGamesDebug("Dice spawned successfully in room: " + this.RoomId);
            }
            catch (Exception e)
            {
                Logging.LogRPGamesError("Error in SpawnDice(): " + e);
            }
        }
    }
}
