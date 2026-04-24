using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Bots;
using Polar.HabboRoleplay.Bots.Manager;
using Polar.HabboHotel.Rooms;

namespace Polar.Communication.Packets.Outgoing.Users
{
    internal class UserTagsComposer : ServerPacket
    {
        public int UserId { get; }
        public GameClient Session { get; }
        public UserTagsComposer(int UserId, GameClient Session)
            : base(ServerPacketHeader.UserTagsMessageComposer)
        {
            this.UserId = UserId;
            this.Session = Session;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            if (UserId > 1000000)
            {
                int BotId = UserId - 1000000;
                var Bot = RoleplayBotManager.GetCachedBotById(BotId);

                if (Bot != null)
                {
                    packet.WriteInteger(UserId);
                    packet.WriteInteger(0);//Count of the tags.
                    {
                        //Append a string.
                    }
                }
            }
            else
            {
                Room room = PolarEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
                if (room != null)
                {
                    RoomUser roomUserByHabbo = room.GetRoomUserManager().GetRoomUserByHabbo(UserId);
                    if ((roomUserByHabbo != null) && !roomUserByHabbo.IsBot)
                    {
                        packet.WriteInteger(roomUserByHabbo.GetClient().GetHabbo().Id);
                        packet.WriteInteger(roomUserByHabbo.GetClient().GetHabbo().Tags.Count);
                        foreach (string str in roomUserByHabbo.GetClient().GetHabbo().Tags)
                        {
                            packet.WriteString(str);
                        }
                        if (Session.GetHabbo().Tags.Count >= 5)
                            PolarEnvironment.GetGame()
                                .GetAchievementManager()
                                .ProgressAchievement(roomUserByHabbo.GetClient(), "ACH_AvatarTags", 5, false);
                    }

                }
            }

        }
    }
}
