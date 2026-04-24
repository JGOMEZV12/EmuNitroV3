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
    class HelpCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_staff_related_help"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Pide ayuda a los staff."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("Por favor escriba un mensaje para que el miembro del personal vea!", 1);
                return;
            }

            if (PolarEnvironment.GetGame().GetModerationTool().UsersHasPendingTicket(Session.GetHabbo().Id))
            {
                Session.SendMessage(new BroadcastMessageAlertComposer("Actualmente tienes un ticket pendiente, por favor, espera la respuesta de un moderador."));
                return;
            }

            string Message = CommandManager.MergeParams(Params, 1);

            if (Message.Length <= 10)
            {
                Session.SendWhisper("Escriba un mensaje más descriptivo para que el miembro del personal vea!", 1);
                return;
            }

            PolarEnvironment.GetGame().GetModerationTool().SendNewTicket(Session, 25, 0, Message, null);
            Session.SendMessage(new RoomNotificationComposer("help_ticket_submit", "message", "¡Su boleto de soporte ha sido enviado!"));

            PolarEnvironment.GetGame().GetClientManager().ModAlert("¡Se ha presentado un nuevo ticket de soporte!");
            PolarEnvironment.GetGame().GetClientManager().StaffWhisperAlert("Hey, revisa en la MOD TOOLS 'Ticket Browser' Hay un reporte nuevo de un usuario", Session);
        }
    }
}