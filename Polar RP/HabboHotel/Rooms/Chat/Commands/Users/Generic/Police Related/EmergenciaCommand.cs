using System;
using System.Linq;
using System.Text;
using Polar.Utilities;
using System.Collections.Generic;

using Polar.Communication.Packets.Outgoing.Guides;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Guides;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Moderation;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Police
{
    class EmergenciaCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_police_related_call_police"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "llama a un paramedico"; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {

            if (Groups.GroupManager.HasJobCommand(Session, "emergencia") && Session.GetRoleplay().IsWorking)
            {
                Session.SendWhisper("¡No puedes enviar una llamada de auxilio al hospital si estás trabajando!", 1);
                return;
            }

            if (Session.GetRoleplay().IsJailed)
            {
                Session.SendWhisper("¡No puedes enviar una llamada de ayuda si estás encarcelado!", 1);
                return;
            }

            if (Session.GetRoleplay().MultiCoolDown["emergencia"] > 0)
            {
                Session.SendWhisper("Tienes que esperar hasta que puedas robar el atm otra vez! [" + Session.GetRoleplay().MultiCoolDown["atm_robbery"] + "/900]");
                return;
            }

            Session.SendWhisper("¡Tu solicitud de ayuda ha sido enviada!", 1);
            Session.GetRoleplay().MultiCoolDown["emergencia"] = 100;
            PolarEnvironment.GetGame().GetClientManager().EmergenciaAlert("[RADIO HOSPITAL] [" + Session.GetHabbo().Username + "] Requiere servicio médico en " + Session.GetHabbo().CurrentRoom.RoomData.Name + " ID: " + Room.Id + "!");

        }
    }
}