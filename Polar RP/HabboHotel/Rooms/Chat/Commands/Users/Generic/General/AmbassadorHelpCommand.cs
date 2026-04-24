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
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.General
{
    class AmbassadorHelpCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_ambassador_related_help"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Solicitud de ayuda por parte de un embajador."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("Por favor escriba un mensaje para que el embajador lo vea!", 1);
                return;
            }

            if (Session.GetRoleplay().TryGetCooldown("ambassadorhelp"))
                return;

            List<GameClient> AvailableAmbassadors = PolarEnvironment.GetGame().GetClientManager().GetClients.Where(x => x != null && x.GetHabbo() != null && x.GetHabbo().GetPermissions() != null && x.GetHabbo().GetPermissions().HasRight("ambassador")).ToList();
            if (AvailableAmbassadors.Count <= 0)
            {
                Session.SendWhisper("Lo sentimos, pero no hay embajadores en línea de manejo de billetes de ayuda ahora mismo!", 1);
                return;
            }
            string Message = CommandManager.MergeParams(Params, 1);

            if (Message.Length <= 10)
            {
                Session.SendWhisper("¡Escriba un mensaje más descriptivo para que el embajador lo vea!", 1);
                return;
            }

            lock (PolarEnvironment.GetGame().GetClientManager().GetClients)
            {
                foreach (var client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                {
                    if (client == null || client.GetHabbo() == null || client.GetRoleplay() == null)
                        continue;

                    if (!client.GetHabbo().GetPermissions().HasRight("ambassador"))
                        continue;

                    client.SendWhisper("[¡ATENCIÓN EMBAJADORES] " + Session.GetHabbo().Username + " Solicita ayuda en '" + Room.Name + " (ID: " + Room.RoomId + ")' Con su mensaje descriptivo: '" + Message + "'", 37);
                }
            }

            Session.SendMessage(new RoomNotificationComposer("help_ticket_submit", "message", "¡Su boleto de ayuda ha sido enviado!"));
            Session.GetRoleplay().CooldownManager.CreateCooldown("ambassadorhelp", 1000, 30);
        }
    }
}