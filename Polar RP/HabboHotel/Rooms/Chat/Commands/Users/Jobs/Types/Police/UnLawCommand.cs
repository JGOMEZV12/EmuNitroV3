using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Users;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Jobs.Types.Police
{
    class UnLawCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_police_law_undo"; }
        }

        public string Parameters
        {
            get { return "%user%"; }
        }

        public string Description
        {
            get { return "Elimina a un ciudadano de la lista de buscados.  :nobuscar usuario"; }
        }

        public async Task Execute(GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            if (Params.Length == 1)
            {
                Session.SendWhisper("Vaya, se le olvidó ingresar un nombre de usuario", 1);
                return;
            }

            Habbo Target = PolarEnvironment.GetHabboByUsername(Params[1]);

            if (Target == null)
            {
                Session.SendWhisper("Se ha producido un error al intentar encontrar a ese usuario, tal vez estén sin conexión.", 1);
                return;
            }

            var RoomUser = Session.GetRoomUser();

            if (RoomUser == null)
                return;

            if (!GroupManager.HasJobCommand(Session, "unlaw"))
            {
                Session.SendWhisper("Sólo un oficial de policía puede utilizar este comando", 1);
                return;
            }

            if (!Session.GetRoleplay().IsWorking)
            {
                Session.SendWhisper("¡Debes estar trabajando para usar este comando!", 1);
                return;
            }

            if (!RoleplayManager.WantedList.ContainsKey(Target.Id))
            {
                Session.SendWhisper("This citizen is not wanted!", 1);
                return;
            }

            if (Target.GetClient() != null && Target.GetClient().GetRoomUser() != null)
            {
                if (Target.GetClient().GetRoomUser().IsAsleep)
                {
                    Session.SendWhisper("Usted no puede deshacer a alguien que no está jugando el juego ahora mismo.", 1);
                    return;
                }
            }
            #endregion

            #region Execute
            Wanted Junk;
            RoleplayManager.WantedList.TryRemove(Target.Id, out Junk);
            Session.Shout("*Quita a " + Target.Username + " De la Lista de peligrosos, limpia su nombre*", 37);

            if (Target.GetClient() != null)
                Target.GetClient().GetRoleplay().IsWanted = false;
            #endregion
        }
    }
}