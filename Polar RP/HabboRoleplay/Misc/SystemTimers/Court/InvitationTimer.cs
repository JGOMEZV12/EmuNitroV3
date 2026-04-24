using System;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboHotel.Items;
using Polar.Core;
using Polar.HabboHotel.Rooms;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;

namespace Polar.HabboRoleplay.Timers.Types
{
    /// <summary>
    /// Countdown to court of appeals invitation
    /// </summary>
    public class InvitationTimer : SystemRoleplayTimer
    {
        public InvitationTimer(string Type, int Time, bool Forever, object[] Params) 
            : base(Type, Time, Forever, Params)
        {
            // 3 minutes converted to milliseconds
            int InvitationTime = Convert.ToInt32(RoleplayData.GetData("court", "invitationtime"));
            TimeLeft = InvitationTime * 60000;
            TimeCount = 0;
        }

        /// <summary>
        /// Court of appeals invitation
        /// </summary>
        public override void Execute()
        {
            try
            {
                if (!RoleplayManager.CourtTrialIsStarting)
                {
                    base.EndTimer();
                    return;
                }

                GameClient Defendant = RoleplayManager.Defendant;
                int CourtRoomId = Convert.ToInt32(RoleplayData.GetData("court", "roomid"));
                int OutsideCourtRoomId = Convert.ToInt32(RoleplayData.GetData("court", "outsideroomid"));

                if (Defendant == null || Defendant.LoggingOut || Defendant.GetHabbo() == null || Defendant.GetRoleplay() == null)
                {
                    lock (RoleplayManager.InvitedUsersToJuryDuty)
                    {
                        foreach (GameClient client in RoleplayManager.InvitedUsersToJuryDuty)
                        {
                            if (client == null || client.GetHabbo() == null)
                                continue;

                            client.SendWhisper("¡La sesión de servicio del jurado ha sido cancelada! ¡Disculpas por cualquier inconveniente!", 26);
                        }
                    }

                    RoleplayManager.CourtVoteEnabled = false;
                    RoleplayManager.InnocentVotes = 0;
                    RoleplayManager.GuiltyVotes = 0;

                    RoleplayManager.CourtJuryTime = 0;
                    RoleplayManager.CourtTrialIsStarting = false;
                    RoleplayManager.CourtTrialStarted = false;
                    RoleplayManager.Defendant = null;
                    RoleplayManager.InvitedUsersToJuryDuty.Clear();

                    base.EndTimer();
                    return;
                }

                TimeCount++;
                TimeLeft -= 1000;

                if (TimeLeft > 0)
                {
                    if (TimeCount == 60)
                    {
                        if (Defendant != null)
                            Defendant.SendWhisper("Su ensayo comenzará en " + (TimeLeft / 60000) + " minuto(s). ¡Estar preparado!", 1);

                        lock (RoleplayManager.InvitedUsersToJuryDuty)
                        {
                            foreach (GameClient client in RoleplayManager.InvitedUsersToJuryDuty)
                            {
                                if (client == null || client.GetHabbo() == null)
                                    continue;

                                client.SendWhisper("Usted ha sido invitado en la sala de la corte (" + CourtRoomId + "  / Fuera de la sala del tribunal: " + OutsideCourtRoomId + ") Para participar en el jurado. Tienes " + (TimeLeft / 60000) + " Minuto (s) para ir allí!", 26);
                                client.SendMessage(new RoomBubbleNotificationComposer("jury_invitation", "¡Has sido invitado a la corte! Tienes " + (TimeLeft / 60000) + " minute(s) para ir, cumple con tu deber", ""));
                            }
                        }
                        TimeCount = 0;
                    }
                    return;
                }

                lock (RoleplayManager.InvitedUsersToJuryDuty)
                {
                    foreach (GameClient Client in RoleplayManager.InvitedUsersToJuryDuty)
                    {
                        if (Client == null || Client.GetHabbo() == null || Client.GetHabbo().CurrentRoom == null || Client.GetRoleplay() == null || !RoleplayManager.InvitedUsersToJuryDuty.Contains(Client))
                            continue;

                        if (Client.GetHabbo().CurrentRoomId != CourtRoomId)
                            RoleplayManager.InvitedUsersToRemove.Add(Client);

                        if (Client.GetHabbo().CurrentRoomId == CourtRoomId)
                        {
                            RoleplayManager.SpawnChairs(Client, "gothic_stool*4");

                            if (Client.GetRoomUser() != null)
                                Client.GetRoomUser().Frozen = true;
                        }
                    }
                }

                lock (RoleplayManager.InvitedUsersToRemove)
                {
                    foreach (GameClient Client in RoleplayManager.InvitedUsersToRemove)
                    {
                        if (Client == null || Client.GetHabbo() == null || Client.GetHabbo().CurrentRoom == null || Client.GetRoleplay() == null)
                            continue;

                        RoleplayManager.InvitedUsersToJuryDuty.Remove(Client);
                    }

                    RoleplayManager.InvitedUsersToRemove.Clear();
                }

                if (Defendant != null)
                    Defendant.SendWhisper("Su juicio ha comenzado! Usted será enviado a la Sala de la Corte en breve ...", 1);

                RoleplayManager.CourtTrialStarted = true;
                RoleplayManager.CourtTrialIsStarting = false;
                base.EndTimer();
                return;
            }
            catch (Exception e)
            {
                Logging.LogRPTimersError("Error in Execute() void: " + e);
                base.EndTimer();
            }
        }
    }
}