using System;
using System.Linq;
using System.Text;
using System.Data;
using System.Collections.Generic;

using Polar.HabboHotel.Users;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Cache;
using Polar.Utilities;

namespace Polar.Communication.Packets.Outgoing.Moderation
{
    internal class ModeratorUserChatlogComposer : ServerPacket
    {
        public int UserId { get; }
        public ModeratorUserChatlogComposer(int UserId)
            : base(ServerPacketHeader.ModeratorUserChatlogMessageComposer)
        {
            this.UserId = UserId;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(UserId);
           packet.WriteString(PolarEnvironment.GetGame().GetClientManager().GetNameById(UserId));
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT `room_id`, `entry_timestamp`, `exit_timestamp` FROM `user_roomvisits` WHERE `user_id` = " + UserId + " ORDER BY `entry_timestamp` DESC LIMIT 5");
                DataTable Visits = dbClient.getTable();

                if (Visits != null)
                {
                    packet.WriteInteger(Visits.Rows.Count);
                    foreach (DataRow Visit in Visits.Rows)
                    {
                        string RoomName = "Unknown";

                        Room Room;
                        if (PolarEnvironment.GetGame().GetRoomManager().LoadRoom(Convert.ToInt32(Visit["room_id"]), out Room) && Room != null)
                        {
                            RoomName = Room.Name;
                        }

                        packet.WriteByte(1);
                        packet.WriteShort(2);//Count
                       packet.WriteString("roomName");
                        packet.WriteByte(2);
                       packet.WriteString(RoomName); // room name
                       packet.WriteString("roomId");
                        packet.WriteByte(1);
                        packet.WriteInteger(Convert.ToInt32(Visit["room_id"]));

                        DataTable Chatlogs = null;
                        if ((Double)Visit["exit_timestamp"] <= 0)
                        {
                            Visit["exit_timestamp"] = PolarEnvironment.GetUnixTimestamp();
                        }

                        dbClient.SetQuery("SELECT `user_id`, `timestamp`, `message` FROM `chatlogs` WHERE `room_id` = " + Convert.ToInt32(Visit["room_id"]) + " AND `timestamp` > " + (Double)Visit["entry_timestamp"] + " AND `timestamp` < " + (Double)Visit["exit_timestamp"] + " ORDER BY timestamp DESC LIMIT 150");
                        Chatlogs = dbClient.getTable();

                        if (Chatlogs != null)
                        {
                            packet.WriteShort(Chatlogs.Rows.Count);
                            foreach (DataRow Log in Chatlogs.Rows)
                            {
                                using (UserCache Habbo = PolarEnvironment.GetGame().GetCacheManager().GenerateUser(Convert.ToInt32(Log["user_id"])))
                                {

                                    if (Habbo == null)
                                        continue;

                                    packet.WriteString(UnixTimestamp.FromUnixTimestamp(Convert.ToInt32(Log["timestamp"])).ToShortTimeString());
                                    packet.WriteInteger(Habbo.Id);
                                    packet.WriteString(Habbo.Username);
                                    packet.WriteString(string.IsNullOrWhiteSpace(Convert.ToString(Log["message"])) ? "*user sent a blank message*" : Convert.ToString(Log["message"]));
                                    packet.WriteBoolean(false);
                                }
                            }
                        }
                        else
                            packet.WriteInteger(0);
                    }
                }
                else
                    packet.WriteInteger(0);
            }
        }
    }
}