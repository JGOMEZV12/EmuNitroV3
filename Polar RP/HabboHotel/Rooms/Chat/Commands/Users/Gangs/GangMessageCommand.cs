using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.Groups;


namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Gangs
{
    class GangMessageCommand : IChatCommand
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
            string Message = CommandManager.MergeParams(Params, 1);
            #endregion

            if (Params.Length == 1)
            {
                Session.SendWhisper("Ejecuta bien el comando :gmensaje texto");
                return;
            }

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

            #endregion

            #region Execute

            PolarEnvironment.GetGame().GetClientManager().sendGangMsg(Session.GetRoleplay().GangId, Message + "\n\n - " + Session.GetHabbo().Username);
            #endregion
        }
    }
}