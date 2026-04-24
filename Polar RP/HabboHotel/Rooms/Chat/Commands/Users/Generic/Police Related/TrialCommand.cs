using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.Communication.Packets.Outgoing.Notifications;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Police
{
    class TrialCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_police_related_court_trial"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Solicita un juicio."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions

            if (RoleplayManager.CourtTrialStarted || RoleplayManager.CourtTrialIsStarting)
            {
                Session.SendWhisper("Lo sentimos, se está llevando a cabo un juicio" + (RoleplayManager.Defendant != null && RoleplayManager.Defendant.GetHabbo() != null ? " solicitado por " + RoleplayManager.Defendant.GetHabbo().Username : "") + ". intenta más tarde", 1);
                return;
            }

            if (!Session.GetRoleplay().IsJailed)
            {
                Session.SendWhisper("¡Usted no puede solicitar un juicio en el tribunal mientras no está encarcelado!", 1);
                return;
            }

            if (Session.GetRoleplay().Trialled)
            {
                Session.SendWhisper("Lo sentimos, pero ya has solicitado una prueba. ¡Por favor, inténtelo de nuevo más tarde!", 1);
                return;
            }

            if (Session.GetRoleplay().JailedTimeLeft < 11)
            {
                Session.SendWhisper("Lo siento, solo los reclusos encarcelados que tienen 11 o más minutos restantes en prisión pueden solicitar un juicio.", 1);
                return;
            }

            #endregion

            #region Execute

            List<GameClients.GameClient> RandomUsers = (from Client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList() where Client != null && Client.GetHabbo() != null orderby new Utilities.CryptoRandom().Next() select Client).ToList();

            lock (RandomUsers)
            {
                foreach (var client in RandomUsers.Take(12))
                {
                    if (client == null || client.GetHabbo() == null || client.GetRoomUser() == null || client.GetRoleplay() == null || client.GetRoomUser().IsAsleep || 
                        client.GetRoleplay().IsJailed || client.GetRoleplay().IsDead || client.GetRoleplay().IsWanted ||
                        client.GetRoleplay().IsWorkingOut)
                        continue;

                    RoleplayManager.InvitedUsersToJuryDuty.Add(client);
                    client.SendWhisper("Has sido invitado en la sala de la corte (" + Convert.ToInt32(RoleplayData.GetData("court", "roomid")) + " / Fuera de la sala de tribunal:  " + Convert.ToInt32(RoleplayData.GetData("court", "outsideroomid")) + ") Para tomar parte del deber de jurado. Tienes: " + Convert.ToInt32(RoleplayData.GetData("court", "invitationtime")) + " Minutos para ir allí", 33);
                    //client.SendMessage(new RoomNotificationComposer("jury_invitation", "message", "¡Has sido invitado a la corte! Tienes " + Convert.ToInt32(RoleplayData.GetData("court", "invitationtime")) + " Minuto (s) para ir allí"));
                }
            }

            Session.SendWhisper("Usted ha solicitado un juicio. por favor espera " + Convert.ToInt32(RoleplayData.GetData("court", "invitationtime")) + " Minutos mientras que hacemos una invitación del deber del jurado a 12 ciudadanos aleatorios en línea", 1);

            Session.GetRoleplay().Trialled = true;
            RoleplayManager.CourtTrialIsStarting = true;
            RoleplayManager.Defendant = Session;
            RoleplayManager.TimerManager.CreateTimer("juryinvitation", 1000, false);

            #endregion
        }
    }
}