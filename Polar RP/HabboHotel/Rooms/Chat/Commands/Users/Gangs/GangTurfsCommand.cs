using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;

using Polar.Communication.Packets.Outgoing.Notifications;
using Polar.HabboHotel.Rooms;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Turfs;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.GameClients;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Gangs
{
    class GangTurfsCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_gang_turfs"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Proporciona una lista de todos los barrios de pandillas capturables."; }
        }

        public async Task Execute(GameClient Session, Room Room, string[] Params)
        {
            StringBuilder Message = new StringBuilder().Append("--- Barrios ---\n\n");

            var turfsManager = PolarEnvironment.GetGame().GetGangTurfsManager();

            if (turfsManager == null || turfsManager.getTurfs() == null || turfsManager.getTurfs().Count == 0)
            {
                Message.Append("No hay barrios disponibles.\n");
                Session.SendMessage(new MOTDNotificationComposer(Message.ToString()));
                return;
            }

            List<Turf> turfs;
            lock (turfsManager.getTurfs())
            {
                turfs = new List<Turf>(turfsManager.getTurfs());
            }

            foreach (var Turf in turfs)
            {
                if (Turf == null)
                    continue;

                if (RoleplayManager.GenerateRoom(Turf.RoomId, out Room TurfRoom, false) && TurfRoom != null)
                {
                    string gangName = "Ninguna pandilla";

                    if (Turf.GangId > 0)
                    {
                        Group Gang = GroupManager.GetGang(Turf.GangId);
                        if (Gang != null && !string.IsNullOrEmpty(Gang.Name))
                        {
                            gangName = Gang.Name;
                        }
                        else
                        {
                            gangName = "Pandilla desconocida";
                        }
                    }

                    Message.Append(TurfRoom.Name + " [RoomID: " + Turf.RoomId + "] --- Controlado por: " + gangName + "\n");
                    Message.Append("\n");
                }
            }

            if (turfs.Count == 0 || Message.ToString().EndsWith("--- Barrios ---\n\n"))
            {
                Message.Append("No hay barrios para mostrar.\n");
            }

            Session.SendMessage(new MOTDNotificationComposer(Message.ToString()));
        }
    }
}