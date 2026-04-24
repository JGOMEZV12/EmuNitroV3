using System;
using System.Linq;
using System.Text;
using System.Data;
using System.Collections.Generic;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;

using Polar.Utilities;
using Polar.HabboHotel.Cache;

namespace Polar.Communication.Packets.Outgoing.Moderation
{
    internal class ModeratorRoomChatlogComposer : ServerPacket
    {
        public Room Room { get; }
        public ModeratorRoomChatlogComposer(Room room)
            : base(ServerPacketHeader.ModeratorRoomChatlogMessageComposer)
        {
            this.Room = room;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteByte(1);
            packet.WriteShort(2);//Count
            packet.WriteString("roomName");
            packet.WriteByte(2);
            packet.WriteString(Room.Name);
            packet.WriteString("roomId");
            packet.WriteByte(1);
            packet.WriteInteger(Room.Id);

            DataTable Table = null;
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT * FROM `chatlogs` WHERE `room_id` = @rid ORDER BY `id` DESC LIMIT 250");
                dbClient.AddParameter("rid", Room.Id);
                Table = dbClient.getTable();
            }

            packet.WriteShort(Table.Rows.Count);
            if (Table != null)
            {
                foreach (DataRow Row in Table.Rows)
                {
                    using (UserCache Habbo = PolarEnvironment.GetGame().GetCacheManager().GenerateUser(Convert.ToInt32(Row["user_id"])))
                    {

                        if (Habbo == null)
                        {
                            packet.WriteString(UnixTimestamp.FromUnixTimestamp(Convert.ToInt32(Row["timestamp"])).ToShortTimeString());
                            packet.WriteInteger(-1);
                            packet.WriteString("Unknown User");
                            packet.WriteString(string.IsNullOrWhiteSpace(Convert.ToString(Row["message"])) ? "*user sent a blank message*" : Convert.ToString(Row["message"]));
                            packet.WriteBoolean(false);
                        }
                        else
                        {
                            packet.WriteString(UnixTimestamp.FromUnixTimestamp(Convert.ToInt32(Row["timestamp"])).ToShortTimeString());
                            packet.WriteInteger(Habbo.Id);
                            packet.WriteString(Habbo.Username);
                            packet.WriteString(string.IsNullOrWhiteSpace(Convert.ToString(Row["message"])) ? "*user sent a blank message*" : Convert.ToString(Row["message"]));
                            packet.WriteBoolean(false);
                        }
                    }
                }
            }
        }
    }
}
