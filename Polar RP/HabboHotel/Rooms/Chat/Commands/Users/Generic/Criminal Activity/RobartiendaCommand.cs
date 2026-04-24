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
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Criminal
{
    class RobartiendaCommand : IChatCommand
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
            get { return "Dejar o robar tiendas."; }
        }

        public RobartiendaCommand(bool stopRobCommand = false)
        {
            _stopRobCommand = stopRobCommand;
        }

        public async Task Execute(GameClient Session, Rooms.Room Room, string[] Params)
        {

            #region Conditions
            if (_stopRobCommand == true)
            {
                Session.SendWhisper("Mejor no robaré.");
                Session.GetRoleplay().BreakGeneralTimer = true;
                Session.GetRoleplay().RobartiendaRobbery = false;
            }
            Wanted NewWanted;
            if (Session.GetRoleplay().Level <= 5)
            {
                Session.SendWhisper("¡Tienes que ser minimo nivel 5, para robar una tienda!", 1);
                return;
            }

            if (Session.GetRoleplay().IsDead)
            {
                Session.SendWhisper("¡No puedes robar mientras estás muerto!", 1);
                return;
            }

            if (Session.GetRoleplay().IsJailed)
            {
                Session.SendWhisper("¡No puedes robar mientras estás en la cárcel!", 1);
                return;
            }

            if (Session.GetRoleplay().StaffOnDuty || Session.GetRoleplay().AmbassadorOnDuty)
            {
                Session.SendWhisper("¡No puedes robar mientras estás de servicio!", 1);
                return;
            }

            if (Session.GetRoleplay().EquippedWeapon != null)
            {
                Session.SendWhisper("Guarda el arma, levantarás sospecha...", 1);
                return;
            }

            if (!Session.GetRoleplay().NearItem("boutique_cashreg", 1))
            {
                Session.SendWhisper("¡Debes estar frente a la caja registradora de una tienda!");
                return;
            }

            if (Session.GetRoleplay().DrivingCar)
            {
                Session.SendWhisper("¡No puedes robar mientras conduces un vehículo!", 1);
                return;
            }

            if (Session.GetRoleplay().IsNoob)
            {
                Session.SendWhisper("¡No puedes completar esta acción mientras estás bajo la protección de Dios por ser nuevo!", 1);
                return;
            }

            if (Session.GetRoleplay().TryGetCooldown("robotienda"))
                return;

            if (!Session.GetRoleplay().MultiCoolDown.ContainsKey("robotienda"))
            {
                Session.GetRoleplay().MultiCoolDown.Add("robotienda", 0);
            }
            if (Session.GetRoleplay().MultiCoolDown["robotienda"] > 0)
            {
                Session.SendWhisper("Tienes que esperar hasta que puedas robar la tienda otra vez! [" + Session.GetRoleplay().MultiCoolDown["robotienda"] + "/300]");
                return;
            }
            #endregion
            string RoomId = Session.GetHabbo().CurrentRoomId.ToString() != "0" ? Session.GetHabbo().CurrentRoomId.ToString() : "Unknown";

            NewWanted = new Wanted(Convert.ToUInt32(Session.GetHabbo().Id), RoomId, 1);
            #region Execute
            Session.GetRoleplay().RobartiendaRobbery = true;
            RoleplayManager.Shout(Session, "* Mira Hacia los lados, viendo que no haya nadie y empieza a robar *");
            //Session.SendWhisper("Tienes 3 minutos para robar la tienda");
            Session.GetRoleplay().EffectSeconds = 150;
            Session.GetRoleplay().IsWanted = true;
            Session.GetRoleplay().WantedLevel = 5;
            Session.GetRoleplay().WantedTimeLeft = 15;
            Session.GetRoomUser().ApplyEffect(59);
            Session.GetRoleplay().TimerManager.CreateTimer("robtienda", 500, false);
            RoleplayManager.WantedList.TryAdd(Session.GetHabbo().Id, NewWanted);
            if (Session.GetRoleplay().WebSocketConnection != null)
                PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(Session, "compose_wanted_stars|" + Session.GetRoleplay().WantedLevel);

            PolarEnvironment.GetGame().GetClientManager().JailAlert("[CENTRAL] [" + Session.GetHabbo().Username + "] Uhh, la tienda: " + Session.GetHabbo().CurrentRoom.RoomData.Name + " está siendo robada, respondan todas las unidades");
            #endregion
          
        }
    }
}