using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.Groups;
using Polar.Communication.Packets.Outgoing.Groups;
using Polar.Communication.Packets.Outgoing.Rooms.Permissions;
using Polar.HabboHotel.GameClients;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Gangs
{
    class GangKickCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_gang_leave"; }
        }

        public string Parameters
        {
            get { return "%username%"; }
        }

        public string Description
        {
            get { return "Te permite salir de tu pandilla actual."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {

            if (Params.Length != 1)
            {
                Session.SendWhisper("Por favor, introduzca un nombre de usuario.", 1);
                return;
            }

            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);

            if (TargetClient == Session)
            {
                Session.SendWhisper("¡No puedes usar este comando en ti!", 1);
                return;
            }

            if (TargetClient == null || TargetClient.GetHabbo() == null || TargetClient.GetRoleplay() == null)
            {
                Session.SendWhisper("No se pudo encontrar a este usuario, quizás estén desconectados.", 1);
                return;
            }

            #region Variables
            Group Group = GroupManager.GetGang(TargetClient.GetRoleplay().GangId);
            #endregion

            #region Conditions
            if (Group == null)
            {
                Session.SendWhisper("¡No tiene una pandilla para expulsarlo!", 1);
                return;
            }

            if (Group.Id <= 1000)
            {
                Session.SendWhisper("No tiene una pandilla para expulsarlo!", 1);
                return;
            }

            if (Group.CreatorId == TargetClient.GetHabbo().Id)
            {
                Session.SendWhisper("No puedes simplemente expulsar al jefe de tu pandilla.", 1);
                return;
            }
            #endregion

            #region Execute
            Session.Shout("*Expulsa de su pandilla '" + Group.Name + "' a "+ TargetClient.GetHabbo().Username+"*", 4);
            TargetClient.GetRoleplay().GangId = 0;
            TargetClient.GetRoleplay().GangRank = 0;
            TargetClient.GetRoleplay().GangRequest = 0;

            if (Group.RoomId == Room.Id && (Group.AdminOnlyDeco == 0 || Group.IsAdmin(TargetClient.GetHabbo().Id)))
            {
                TargetClient.GetRoomUser().RemoveStatus("flatctrl 1");
                TargetClient.GetRoomUser().UpdateNeeded = true;
                TargetClient.SendMessage(new YouAreControllerComposer(0));
            }

            Group NewGang = GroupManager.GetGang(1000);
            NewGang.AddNewMember(TargetClient.GetHabbo().Id);
            NewGang.SendPackets(TargetClient);

            foreach (int member in Group.Members.Keys)
            {
                GameClient Client = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(member);

                if (Client == null)
                    continue;

                Client.SendWhisper("["+ Group.Name.ToUpper() +"] " + Session.GetHabbo().Username + " acaba de expulsar de la pandilla a "+ TargetClient.GetHabbo().Username+"!", 34);
            }
            #endregion
        }
    }
}