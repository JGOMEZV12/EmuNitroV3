using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;
using Polar.HabboHotel.Cache;

namespace Polar.Communication.Packets.Outgoing.Rooms.Settings
{
    internal class GetRoomBannedUsersComposer : ServerPacket
    {
        public Room Instance { get; }
        public GetRoomBannedUsersComposer(Room Instance)
            : base(ServerPacketHeader.GetRoomBannedUsersMessageComposer)
        {
            this.Instance = Instance;
            Compose(this);
        }
        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(Instance.Id);

            packet.WriteInteger(Instance.BannedUsers().Count);//Count
            foreach (int Id in Instance.BannedUsers().ToList())
            {
                using (UserCache Data = PolarEnvironment.GetGame().GetCacheManager().GenerateUser(Id))
                {

                    if (Data == null)
                    {
                        packet.WriteInteger(0);
                        packet.WriteString("Unknown Error");
                    }
                    else
                    {
                        packet.WriteInteger(Data.Id);
                        packet.WriteString(Data.Username);
                    }
                }
            }
        }
    }
}
