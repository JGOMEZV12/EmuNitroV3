using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Jobs.Types.Police
{
    class UnCuffCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_police_cuff_undo"; }
        }

        public string Parameters
        {
            get { return "%user%"; }
        }

        public string Description
        {
            get { return "Quita las esposas al usuario si ya está esposado."; }
        }

        public async Task Execute(GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            if (Params.Length == 1)
            {
                Session.SendWhisper("Vaya, se le olvidó ingresar un nombre de usuario", 1);
                return;
            }

            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);
            if (TargetClient == null)
            {
                Session.SendWhisper("Se ha producido un error al intentar encontrar a ese usuario, tal vez estén sin conexión.", 1);
                return;
            }

            RoomUser RoomUser = Session.GetRoomUser();
            RoomUser TargetUser = Room.GetRoomUserManager().GetRoomUserByHabbo(TargetClient.GetHabbo().Username);
            if (TargetUser == null)
            {
                Session.SendWhisper("Se ha producido un error al encontrar a ese usuario, tal vez no estén en línea o en esta sala.", 1);
                return;
            }

            if (!GroupManager.HasJobCommand(Session, "cuff") && !Session.GetRoleplay().PoliceTrial)
            {
                Session.SendWhisper("Sólo un oficial de policía puede utilizar este comando", 1);
                return;
            }

            if (!Session.GetRoleplay().IsWorking && !Session.GetRoleplay().PoliceTrial)
            {
                Session.SendWhisper("¡Debes estar trabajando para usar este comando!", 1);
                return;
            }

            if (!TargetClient.GetRoleplay().Cuffed)
            {
                Session.SendWhisper("¡No puedes desbloquear a alguien que no está esposado!", 1);
                return;
            }

            if (TargetUser.IsAsleep)
            {
                Session.SendWhisper("No puedes desbloquear a alguien que no está jugando el juego ahora mismo", 1);
                return;
            }
            #endregion

            #region Execute
            Point ClientPos = new Point(RoomUser.X, RoomUser.Y);
            Point TargetClientPos = new Point(TargetUser.X, TargetUser.Y);
            double Distance = RoleplayManager.GetDistanceBetweenPoints2D(ClientPos, TargetClientPos);

            if (Distance <= 1)
            {
                Session.Shout("*Saca su llave de esposas de su bolsillo y quitan las esposas a " + TargetClient.GetHabbo().Username + "'s*", 37);
                TargetClient.GetRoleplay().Cuffed = false;
                return;
            }
            else
            {
                Session.SendWhisper("You must get closer to this citizen in order to uncuff them!", 1);
                return;
            }
            #endregion
        }
    }
}