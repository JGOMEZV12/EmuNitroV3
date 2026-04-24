using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Inventory.Purse;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Users;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using Polar.Communication.Packets.Outgoing.Rooms;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using Polar.Communication.Packets.Outgoing.Rooms.Session;

namespace Polar.HabboHotel.Rooms
{
    class CheckBoosting
    {
        public List<int> XCoordinates = new List<int>();
        public List<int> YCoordinates = new List<int>();
        //private int SpotDetection;
        private GameClient mClient;
        int HabboId;

        public void BoostingMovement(Room room, GameClient RecieveClient, int IdOfHabbo, int x, int y)
        {
            this.HabboId = IdOfHabbo;
            this.mClient = RecieveClient;

            this.XCoordinates.Add(x);
            this.YCoordinates.Add(y);

            if (this.XCoordinates.Count == 20)
            {
                for (int i = XCoordinates.Count - 1; i > (XCoordinates.Count / 2); i--)
                {
                    if (XCoordinates[i] == XCoordinates[i - 1])
                    {
                        continue;
                    }
                    else
                    {
                        GetClient().GetHabbo().BoostingCheck = false;
                        return;
                    }
                }
                room.GetRoomUserManager().RemoveUserFromRoom(RecieveClient, true, true, true);
                GetClient().SendMessage(new RoomReadyComposer("ADM", 3, "You Have Been Kicked for Boosting ! Better Luck Next Time"));
            }
        }

        public GameClient GetClient()
        {
            if (this.mClient == null)
                this.mClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(HabboId);
            return mClient;
        }

    }
}
