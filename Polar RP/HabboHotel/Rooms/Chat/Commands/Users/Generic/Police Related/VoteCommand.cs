using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.Communication.Packets.Outgoing.Notifications;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Police
{
    class VoteCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_police_related_court_vote"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Voto si el acusado es declarado culpable o inocente."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions

            if (!RoleplayManager.CourtTrialStarted)
            {
                Session.SendWhisper("Lo siento, pero no puedes votar ahora mismo. ¡Por favor, inténtelo de nuevo más tarde!", 1);
                return;
            }

            if (!RoleplayManager.InvitedUsersToJuryDuty.Contains(Session))
            {
                Session.SendWhisper("Lo siento, pero no puedes votar porque nunca has sido invitado a participar en el jurado", 1);
                return;
            }

            if (!RoleplayManager.CourtVoteEnabled)
            {
                Session.SendWhisper("Lo siento, pero aún no puedes votar. ¡Por favor, inténtelo de nuevo más tarde!", 1);
                return;
            }

            if (Session.GetRoleplay().TryGetCooldown("juryvote", false))
            {
                Session.SendWhisper("Ya votaste", 1);
                return;
            }

            #endregion

            #region Execute

            string Type = Params[1];

            switch(Type)
            {
                case "inno":
                case "innocent":
                case "inocente":
                    {
                        RoleplayManager.InnocentVotes++;
                        Session.SendWhisper("¡Su voto ha sido emitido! Por favor espera...", 1);

                        Session.GetRoleplay().CooldownManager.CreateCooldown("juryvote", 1000, 60);
                        break;
                    }

                case "guilty":
                case "culpable":
                    {
                        RoleplayManager.GuiltyVotes++;
                        Session.SendWhisper("¡Su voto ha sido emitido! Por favor espera...", 1);

                        Session.GetRoleplay().CooldownManager.CreateCooldown("juryvote", 1000, 60);
                        break;
                    }

                default:
                    {
                        Session.SendWhisper("¡Acción no válida! Usted debe decidir si el acusado es encontrado 'inocente' o 'culpable' de todos los crímenes!", 1);
                        break;
                    }
            }

            #endregion
        }
    }
}