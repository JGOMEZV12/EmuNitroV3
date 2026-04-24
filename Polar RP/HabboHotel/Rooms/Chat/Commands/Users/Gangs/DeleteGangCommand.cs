using Polar.Database.Interfaces;
using System.Linq;
using System.Collections.Generic;
using Polar.Communication.Packets.Outgoing.Moderation;
using Polar.Communication.Packets.Outgoing.Messenger;
using Polar.Communication.Packets.Outgoing.Rooms.Session;
using Polar.HabboHotel.Groups;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Gangs
{
    class DeleteGangCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_gang_backup"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Eliminar pandilla"; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {

            Room = Session.GetHabbo().CurrentRoom;
            if (Room == null)
                return;

            #region Variables
            Group Group = GroupManager.GetGang(Room.Group.Id);
            #endregion

            if (Room.Group == null)
            {
                Session.SendWhisper("Vaya, no hay un grupo aquí?");
                return;
            }

            if (Group.Id < 1000)
            {
                Session.SendWhisper("No es una pandilla");
                return;
            }

            if (Group.Id < 1000)
            {
                Session.SendWhisper("No es una pandilla");
                return;
            }

            if (Group.IsAdmin(Session.GetHabbo().Id) == false)
            {
                Session.SendWhisper("No eres administrador para eliminar la pandilla");
                return;
            }

            bool IsOwner = false;
            if (Group.CreatorId == Session.GetHabbo().Id)
                IsOwner = true;

            if (!IsOwner)
            {
                Session.SendWhisper("No creaste esta pandilla, asi que no podras borrarla");
                return;
            }

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.RunQuery("DELETE FROM `rp_gangs` WHERE `id` = '" + Room.Group.Id + "'");
            }

            Session.GetRoleplay().GangId = 0;
            Session.GetRoleplay().GangRank = 0;
            Session.GetRoleplay().GangRequest = 0;

            PolarEnvironment.GetGame().GetGroupManager().DeleteGroup(Room.RoomData.Group.Id);

            Room.Group = null;
            Room.RoomData.Group = null;

            Room R = null;
            if (!PolarEnvironment.GetGame().GetRoomManager().TryGetRoom(Room.Id, out R))
                return;

            Session.SendNotification("Éxito, grupo eliminado.");
            List<RoomUser> UsersToReturn = Room.GetRoomUserManager().GetRoomUsers().ToList();

            PolarEnvironment.GetGame().GetRoomManager().UnloadRoom(R, true);

            foreach (RoomUser User in UsersToReturn)
            {
                if (User == null || User.GetClient() == null)
                    continue;

            }
            return;
        }
    }
}