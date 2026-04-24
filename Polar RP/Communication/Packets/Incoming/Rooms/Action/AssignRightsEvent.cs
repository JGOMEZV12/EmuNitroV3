using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.Core;
using Polar.HabboHotel.Rooms;

using Polar.Communication.Packets.Outgoing.Rooms.Permissions;
using Polar.Communication.Packets.Outgoing.Rooms.Settings;
using Polar.HabboHotel.Users;

using Polar.Database.Interfaces;
using Polar.HabboHotel.Cache;

namespace Polar.Communication.Packets.Incoming.Rooms.Action
{
    internal class AssignRightsEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            if (Session == null || Session.GetHabbo() == null)
                return;

            int UserId = Packet.PopInt();

            Room Room = null;
            if (!PolarEnvironment.GetGame().GetRoomManager().TryGetRoom(Session.GetHabbo().CurrentRoomId, out Room))
                return;

            if (!Room.CheckRights(Session, true))
                return;

            if (Room.UsersWithRights.Contains(UserId))
            {
                Session.SendNotification(PolarEnvironment.GetGame().GetLanguageLocale().TryGetValue("room_rights_has_rights_error"));
                return;
            }

            Room.UsersWithRights.Add(UserId);

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.RunQuery("INSERT INTO `room_rights` (`room_id`,`user_id`) VALUES ('" + Room.RoomId + "','" + UserId + "')");
            }

            RoomUser RoomUser = Room.GetRoomUserManager().GetRoomUserByHabbo(UserId);
            if (RoomUser != null && !RoomUser.IsBot)
            {
                RoomUser.SetStatus("flatctrl 1", "");
                RoomUser.UpdateNeeded = true;
                if (RoomUser.GetClient() != null)
                    RoomUser.GetClient().SendMessage(new YouAreControllerComposer(1));

                Session.SendMessage(new FlatControllerAddedComposer(Room.RoomId, RoomUser.GetClient().GetHabbo().Id, RoomUser.GetClient().GetHabbo().Username));
            }
            else
            {
                using (UserCache User = PolarEnvironment.GetGame().GetCacheManager().GenerateUser(UserId))
                {
                    if (User != null)
                        Session.SendMessage(new FlatControllerAddedComposer(Room.RoomId, User.Id, User.Username));
                }
            }
        }
    }
}
