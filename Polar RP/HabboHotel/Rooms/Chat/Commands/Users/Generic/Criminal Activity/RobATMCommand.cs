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
using Polar.HabboHotel.Items;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Criminal
{
    class RobATMCommand : IChatCommand
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
            get { return "Roba cajeros automaticos."; }
        }

        public RobATMCommand(bool stopRobCommand = false)
        {
            _stopRobCommand = stopRobCommand;
        }

        public async Task Execute(GameClient Session, Rooms.Room Room, string[] Params)
        {

            #region Conditions
            if (_stopRobCommand == true)
            {
                Session.SendWhisper("Has canceado el robo al cajero.");
                Session.GetRoleplay().BreakGeneralTimer = true;
                Session.GetRoleplay().ATMRobbery = false;
            }

            if (Session.GetRoleplay().Level < 3)
            {
                Session.SendWhisper("¡Tienes que ser minimo nivel 3, para robar el banco o cajero!", 1);
                return;
            }

            if (Session.GetRoleplay().IsDead)
            {
                Session.SendWhisper("¡No puedes robar a alguien mientras estás muerto!", 1);
                return;
            }

            if (Session.GetRoleplay().IsJailed)
            {
                Session.SendWhisper("¡No puedes robar a alguien mientras estás en la cárcel!", 1);
                return;
            }

            if (Session.GetRoleplay().StaffOnDuty || Session.GetRoleplay().AmbassadorOnDuty)
            {
                Session.SendWhisper("¡No puedes robar a alguien mientras estás de servicio!", 1);
                return;
            }

            if (Session.GetRoleplay().ATMRobbery)
            {
                Session.SendWhisper("Ya está robando el cajero automático.");
                return;
            }

            if (!Session.GetRoleplay().NearItem("atm_moneymachine", 1))
            {
                Session.SendWhisper("No estas cerca de un cajero!");
                return;
            }

            if (!Room.RobEnabled && !RoleplayManager.PurgeStarted)
            {
                Session.SendWhisper("No se puede robar en esta habitación", 1);
                return;
            }

            if (Session.GetRoleplay().DrivingCar)
            {
                Session.SendWhisper("¡No puedes robar a alguien mientras conduces un vehículo!", 1);
                return;
            }

            if (Session.GetRoleplay().IsNoob)
            {
                Session.SendWhisper("¡No puedes completar esta acción mientras estás bajo la protección de Dios por ser nuevo!", 1);
                return;
            }

            if (Session.GetRoleplay().TryGetCooldown("atmrobbery", true))
            {
                Session.SendWhisper("Por favor espera un poco para hacer eso nuevamente.", 1);
                return;
            }

            if (Session.GetRoleplay().PassiveMode)
            {
                Session.SendWhisper("No puedes realizar acciones ilegales en modo pasivo.", 1);
                return;
            }

            #endregion

            #region Execute

            Session.GetRoleplay().ATMRobbery = true;
            Session.GetRoleplay().ATMRobTimeLeft = RoleplayManager.ATMRobTime;

            if (!Session.GetRoleplay().WantedFor.Contains("robbing"))
                Session.GetRoleplay().WantedFor = Session.GetRoleplay().WantedFor + "robbing, ";

            Session.GetRoleplay().IsWanted = true;
            Session.GetRoleplay().WantedLevel = 1;
            Session.GetRoleplay().WantedTimeLeft = 5;

            RoleplayManager.Shout(Session, "*Saca su palanca y empieza a golpear el cajero*");
            Session.SendWhisper("Debes esperar " + (Session.GetRoleplay().ATMRobTimeLeft / 60) + " minuto(s)...", 1);
            Session.GetRoleplay().EffectSeconds = 300;
            Session.GetRoomUser().ApplyEffect(8);        
            Session.GetRoleplay().TimerManager.CreateTimer("atmrob", 1000, true);
            Session.GetRoleplay().CooldownManager.CreateCooldown("atmrobbery", 1000, 5);
/*
            string RoomId = Session.GetHabbo().CurrentRoomId.ToString() != "0" ? Session.GetHabbo().CurrentRoomId.ToString() : "Unknown";

            Wanted NewWanted = new Wanted(Convert.ToUInt32(Session.GetHabbo().Id), RoomId, 1);

            RoleplayManager.WantedList.TryAdd(Session.GetHabbo().Id, NewWanted);

            if (Session.GetRoleplay().WebSocketConnection != null)
                PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(Session, "compose_wanted_stars|" + Session.GetRoleplay().WantedLevel);
            */
            PolarEnvironment.GetGame().GetClientManager().JailAlert("[RADIO Alert] [" + Session.GetHabbo().Username + "] Uhh, Nos han alertado que un cajero esta siendo robado " + Session.GetHabbo().CurrentRoom.RoomData.Name + "! Respondan unidades!");
            #endregion
        }
    }
}