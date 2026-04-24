using System;
using System.Linq;
using System.Text;
using System.Data;
using System.Diagnostics;
using System.Collections.Generic;

using Polar.Communication.Packets.Outgoing.Users;
using Polar.Communication.Packets.Outgoing.Notifications;

using Polar.Communication.Packets.Outgoing.Handshake;
using Polar.Communication.Packets.Outgoing.Quests;
using Polar.HabboHotel.Items;
using Polar.Communication.Packets.Outgoing.Inventory.Furni;
using Polar.Communication.Packets.Outgoing.Catalog;
using Polar.HabboHotel.Quests;
using Polar.HabboHotel.Rooms;
using System.Threading;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Rooms.Avatar;
using Polar.Communication.Packets.Outgoing.Pets;
using Polar.Communication.Packets.Outgoing.Messenger;
using Polar.HabboHotel.Users.Messenger;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using Polar.Communication.Packets.Outgoing.Availability;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboRoleplay.Timers;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.General
{
    class InmunidadCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_inmunidad"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Activa/Desactiva la inmunidad."; }
        }

        public async Task Execute(GameClient Session, Room Room, string[] Params)
        {

            #region Conditions

            #region Basic Conditions
            if (Session.GetRoleplay().Cuffed)
            {
                Session.SendWhisper("No puedes hacer eso mientras estás esposad@", 1);
                return;
            }
            if (Session.GetRoomUser() == null || !Session.GetRoomUser().CanWalk)
            {
                Session.SendWhisper("Al parecer no puedes ni moverte para hacer eso.", 1);
                return;
            }
            if (Session.GetRoleplay().IsDead)
            {
                Session.SendWhisper("¡No puedes hacer esto mientras estás muert@!", 1);
                return;
            }
            if (Session.GetRoleplay().IsJailed)
            {
                Session.SendWhisper("¡No puedes hacer eso mientras estás muert@!", 1);
                return;
            }
            #endregion

            #region Conditions Checks
            if (!Session.GetRoomUser().GetRoom().RoomData.SafeZoneEnabled)
            {
                Session.SendWhisper("Debes estar en una zona segura para hacer eso.", 1);
                return;
            }
            if (RoleplayManager.PurgeStarted)
            {
                Session.SendWhisper("¡No puedes hacer eso durante la purga!", 1);
                return;
            }
            if (Session.GetRoleplay().IsWanted)
            {
                Session.SendWhisper("No puedes hacer eso mientras estás en la lista de buscados.", 1);
                return;
            }
            if (Session.GetRoleplay().IsWorking && GroupManager.HasJobCommand(Session, "law"))
            {
                Session.SendWhisper("No puedes hacer eso mientras estás trabajando de policía", 1);
                return;
            }
            if (Session.GetRoleplay().CamCargId == 3 || Session.GetRoleplay().CamCargId == 4)
            {
                Session.SendWhisper("No puedes hacer eso mientras transportas cargamentos ilegales.", 1);
                return;
            }
            #endregion
            #endregion

            if (!(Session.GetRoleplay().IsWorking && Session.GetRoleplay().JobId == 3))
            {
                if (Session.GetRoleplay().IsNoob)
                {
                    if (Session.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("noob"))
                        Session.GetRoleplay().TimerManager.ActiveTimers["noob"].EndTimer();

                    Session.GetRoleplay().IsNoob = false;
                }
                else
                {
                    Session.GetRoleplay().IsNoob = true;
                    Session.GetRoleplay().NoobTimeLeft = 5;
                    Session.GetRoleplay().TimerManager.CreateTimer("noob", 1000, true);
                }

                Session.SendWhisper("Inmunidad: " + (Session.GetRoleplay().IsNoob == true ? "activa" : "desactivada"), 1);

            }
            else
            {
                Session.SendWhisper("¡Lo siento, mientras que estés trabajando de policía la inmunidad no te afecta!", 1);
                return;
            }
        }
    }
}