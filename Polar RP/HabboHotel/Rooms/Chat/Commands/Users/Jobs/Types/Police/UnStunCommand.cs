using System.Drawing;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Jobs.Types.Police
{
    class UnStunCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_police_stun_undo"; }
        }

        public string Parameters
        {
            get { return "%user%"; }
        }

        public string Description
        {
            get { return "Desparaliza a una persona que ha sufrido alguno de estos ataques."; }
        }

        public async Task Execute(GameClient Session, Rooms.Room Room, string[] Params)
        {

            #region Conditions
            if (Params.Length == 1)
            {
                Session.SendWhisper("Debes ingresar el nombre de la persona.", 1);
                return;
            }

            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);
            if (TargetClient == null)
            {
                Session.SendWhisper("Ha ocurrido un error al buscar a la persona, probablemente esté desconectada.", 1);
                return;
            }

            RoomUser RoomUser = Session.GetRoomUser();
            RoomUser TargetUser = Room.GetRoomUserManager().GetRoomUserByHabbo(TargetClient.GetHabbo().Username);
            if (TargetUser == null)
            {
                Session.SendWhisper("Ha ocurrido un error al buscar a la persona, probablemente esté desconectada o no está en esta zona.", 1);
                return;
            }

            if (!Session.GetHabbo().GetPermissions().HasRight("corporation_rights"))
            {
                if (TargetClient.GetHabbo().Id == Session.GetHabbo().Id)
                {
                    Session.SendWhisper("No puedes hacerte eso a ti mism@.", 1);
                    return;
                }
                if (GroupManager.HasJobCommand(TargetClient, "law"))
                {
                    Session.SendWhisper("¡No puedes hacer eso entre compañeros de trabajo!", 1);
                    return;
                }
            }

            if (!GroupManager.HasJobCommand(Session, "unstun") && !Session.GetRoleplay().PoliceTrial && !Session.GetHabbo().GetPermissions().HasRight("corporation_rights"))
            {
                Session.SendWhisper("¡Solo un oficial de policía puede hacer eso!", 1);
                return;
            }

            if (!Session.GetRoleplay().IsWorking && !Session.GetRoleplay().PoliceTrial && !Session.GetHabbo().GetPermissions().HasRight("corporation_rights"))
            {
                Session.SendWhisper("Debes estar trabajando para hacer eso.", 1);
                return;
            }
            /*if (Session != TargetClient && TargetClient.GetHabbo().Rank > 3 && TargetClient.GetHabbo().Rank >= Session.GetHabbo().Rank)
            {
                Session.SendWhisper("((No puedes hacerle eso a un miembro de la administración))", 1);
                return;
            }
            if (TargetClient.GetRoleplay().DrivingCar)
            {
                Session.SendWhisper("¡No puedes hacerle eso a una persona mientras va conduciendo!", 1);
                return;
            }
            if (TargetClient.GetRoleplay().Pasajero)
            {
                Session.SendWhisper("¡No puedes hacerle eso a una persona mientras va de pasajer@!", 1);
                return;
            }*/
            if (!TargetClient.GetRoomUser().Frozen)
            {
                Session.SendWhisper("No puedes desparalizar a alguien que no está paralizado.", 1);

                return;
            }

            if (TargetUser.IsAsleep)
            {
                Session.SendWhisper("¡No puedes desparalizar a un usuario ausente!", 1);
                return;
            }
            #endregion

            #region Execute
            Point ClientPos = new Point(RoomUser.X, RoomUser.Y);
            Point TargetClientPos = new Point(TargetUser.X, TargetUser.Y);
            double Distance = RoleplayManager.GetDistanceBetweenPoints2D(ClientPos, TargetClientPos);

            if (Distance <= 1)
            {
                RoleplayManager.Shout(Session, "*Ayuda a " + TargetClient.GetHabbo().Username + ", dándole tiempo para recuperarse de su aturdimiento*", 37);
                TargetClient.GetRoleplay().TimerManager.ActiveTimers["stun"].EndTimer();
                TargetClient.GetRoomUser().Frozen = false;
                TargetClient.GetRoomUser().CanWalk = true;
                TargetClient.GetRoleplay().IsStun = false;
                TargetClient.GetRoleplay().Paralized = false;
                return;
            }
            else
            {
                Session.SendWhisper("Debes estar más cerca de la persona.", 1);

                return;
            }
            #endregion
        }
    }
}