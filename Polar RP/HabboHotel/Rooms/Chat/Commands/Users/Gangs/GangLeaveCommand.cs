using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.Groups;
using Polar.Communication.Packets.Outgoing.Groups;
using Polar.Communication.Packets.Outgoing.Rooms.Permissions;
using Polar.HabboHotel.GameClients;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Gangs
{
    class GangLeaveCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_gang_leave"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Te permite salir de tu pandilla actual."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Variables
            Group Group = GroupManager.GetGang(Session.GetRoleplay().GangId);
            #endregion

            #region Conditions
            if (Group == null)
            {
                Session.SendWhisper("¡No tienes una pandilla para irte!", 1);
                return;
            }

            if (Group.Id <= 1000)
            {
                Session.SendWhisper("No tienes una pandilla para irte!", 1);
                return;
            }

            if (Group.CreatorId == Session.GetHabbo().Id)
            {
                Session.SendWhisper("No puedes simplemente abandonar tu pandilla, ¡debes eliminarlo o transferirlo!", 1);
                return;
            }
            #endregion

            #region Execute
            Session.Shout("*Abandona a su pandilla '" + Group.Name + "'*", 4);
            Session.GetRoleplay().GangId = 0;
            Session.GetRoleplay().GangRank = 0;
            Session.GetRoleplay().GangRequest = 0;

            if (Group.RoomId == Room.Id && (Group.AdminOnlyDeco == 0 || Group.IsAdmin(Session.GetHabbo().Id)))
            {
                Session.GetRoomUser().RemoveStatus("flatctrl 1");
                Session.GetRoomUser().UpdateNeeded = true;
                Session.SendMessage(new YouAreControllerComposer(0));
            }

            Group NewGang = GroupManager.GetGang(1000);
            NewGang.AddNewMember(Session.GetHabbo().Id);
            NewGang.SendPackets(Session);

            foreach (int member in Group.Members.Keys)
            {
                GameClient Client = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(member);

                if (Client == null)
                    continue;

                Client.SendWhisper("["+ Group.Name.ToUpper() +"] " + Session.GetHabbo().Username + " acaba de dejar la pandilla!", 34);
            }
            #endregion
        }
    }
}