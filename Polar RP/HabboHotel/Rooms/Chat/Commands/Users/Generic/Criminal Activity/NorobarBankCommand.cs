using System;
using System.Linq;
using System.Text;
using System.Drawing;
using Polar.Utilities;
using System.Collections.Generic;
using Polar.HabboRoleplay.Timers;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Turfs;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Criminal
{
    class NorobarBankCommand : IChatCommand
    {
        private readonly bool _stopRobCommand;
        public string PermissionRequired
        {
            get { return "command_criminal_activity_rob"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Dejar de robar el banco."; }
        }

        public NorobarBankCommand(bool stopRobCommand = false)
        {
            _stopRobCommand = stopRobCommand;
        }


        public async Task Execute(GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Variables
            Group Gang = GroupManager.GetGang(Session.GetRoleplay().GangId);
            Turf Turf = TurfManager.GetTurf(Room.RoomId);
            //bool InsideTurf = false;
            #endregion

            #region Conditions
            if (Session.GetHabbo().CurrentRoomId != 28) {
                Session.SendWhisper("¡Para dejar de robar el banco necesitas estar en a sala #28 y entrar en la boveda!", 1);
                return;
            }

            if (Session.GetRoleplay().Level <= 5)
            {
                Session.SendWhisper("¡Tienes que ser minimo nivel 5, para robar el banco!", 1);
                return;

            }

            if (Session.GetRoleplay().Robbery == false)

            {
                Session.SendWhisper("¡No estás robando el banco!");
                return;
            }

            if (Session.GetRoleplay().GangId <= 0)
            {
                Session.SendWhisper("¡No perteneces a ninguna Pandilla para dejar de robar el banco!", 1);
                return;
            }

            if (Session.GetRoleplay().IsDead)
            {
                Session.SendWhisper("¡No puedes dejar de robar a alguien mientras estás muerto!", 1);
                return;
            }

            if (RoleplayManager.PurgeStarted)
            {
                Session.SendWhisper("¡Solo puedes dejar derobar la bovéda cuando no esta sellada! pero se ha activado la arma y la policía te busca. HUYE DE INMEDIATO", 1);
                return;
            }

            if (!Room.RobEnabled && !RoleplayManager.PurgeStarted)
            {
                Session.SendWhisper("No se puede usar este comando en este momento", 1);
                return;
            }

            if (Session.GetRoleplay().DrivingCar)
            {
                Session.SendWhisper("¡No puedes dejar de robar a alguien mientras conduces un vehículo!", 1);
                return;
            }

            if (Session.GetRoleplay().IsNoob)
            {
                Session.SendWhisper("¡No puedes completar esta acción mientras estás bajo la protección de Dios por ser nuevo!", 1);
                return;
            }

            if (Session.GetRoleplay().TryGetCooldown("robbery"))
                return;

            if (!Session.GetRoleplay().MultiCoolDown.ContainsKey("robbery"))
            {
                Session.GetRoleplay().MultiCoolDown.Add("robbery", 0);
            }
            if (Session.GetRoleplay().MultiCoolDown["robbery"] > 0)
            {
                Session.SendWhisper("Debe esperar hasta que pueda robar el banco otra vez! [" + Session.GetRoleplay().MultiCoolDown["robbery"] + "/900]");
                return;
            }
            #endregion

            #region Execute
            //Session.GetRoleplay().bankRobTimer = new bankRobTimer(Session);
            Session.GetRoleplay().Robbery = false;
            Session.GetRoleplay().IsWanted = true;
            Session.GetRoleplay().CurEnergy -= 20;
			Session.RobberyUsers -= 1;
            RoleplayManager.Shout(Session, "*Dejas de robar el banco*");
            #endregion
        }
    }
}