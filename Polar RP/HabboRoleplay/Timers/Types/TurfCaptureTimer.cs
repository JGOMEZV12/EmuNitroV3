using System;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.Groups;
using Polar.Database.Interfaces;
using Polar.Core;
using System.Collections.Generic;
using System.Linq;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboRoleplay.Timers.Types
{
    /// <summary>
    /// Turf capture timer
    /// </summary>
    public class TurfCaptureTimer : RoleplayTimer
    {
        public TurfCaptureTimer(string Type, GameClient Client, int Time, bool Forever, object[] Params)
            : base(Type, Client, Time, Forever, Params)
        {
            // 5 minutos convertidos a milisegundos
            TimeLeft = RoleplayManager.TurfCapTime * 1000;
        }

        /// <summary>
        /// Temporizador para la captura de un territorio
        /// </summary>
        public override void Execute()
        {
            try
            {
                // Validación de cliente y estado del jugador
                if (base.Client == null || base.Client.GetHabbo() == null || base.Client.GetRoleplay() == null ||
                    base.Client.GetRoleplay().IsDead || base.Client.GetRoleplay().IsJailed ||
                    base.Client.GetRoomUser() == null || base.Client.GetRoomUser().IsWalking ||
                    base.Client.GetRoleplay().BreakGeneralTimer)
                {
                    // Cancelación del proceso si el jugador se mueve o tiene un estado inválido
                    CancelTurfCapture();
                    return;
                }

                // Reiniciar el timer de ruptura general
                base.Client.GetRoleplay().BreakGeneralTimer = false;

                // Terminar el temporizador si el jugador no pertenece a una banda
                if (base.Client.GetRoleplay().GangId == 1000)
                {
                    base.EndTimer();
                    return;
                }

                #region Actualización de estadísticas de bandas
                // Crear cooldown para evitar repetición rápida de acciones
                base.Client.GetRoleplay().CooldownManager.CreateCooldown("capturing", 1000, 60);

                if (base.Client.GetRoomUser()?.GetRoom()?.Group != null)
                {
                    var group = base.Client.GetRoomUser().GetRoom().Group;
                    group.GangTurfsDefended++;
                    group.UpdateStat(group.Id, "gang_turfs_defend", group.GangTurfsDefended);
                }
                #endregion

                Group Gang = GroupManager.GetGang(base.Client.GetRoleplay().GangId);
                Room Room = base.Client.GetHabbo().CurrentRoom;
                if (Gang == null)
                {
                    base.EndTimer();
                    return;
                }

                // Verificaciones adicionales de estado del jugador
                var roomUser = base.Client.GetRoomUser();
                if (roomUser == null || roomUser.IsAsleep)
                    return;

                // Decrementar tiempo
                TimeCount++;
                TimeLeft -= 1000;
                base.Client.GetRoleplay().LoadingTimeLeft--;

                // Enviar notificación del tiempo restante cada 60 segundos
                if (TimeLeft > 0)
                {
                    if (TimeCount == 60)
                    {
                        RoleplayManager.Shout(base.Client, $"*Se acerca a capturar el barrio de pandillas [{(TimeLeft / 60000)} Minutos restantes]*", 4);
                        TimeCount = 0;
                    }
                    return;
                }

                // Lógica de finalización de la captura
                CompleteTurfCapture(Gang, Room);

            }
            catch (Exception e)
            {
                // Manejo de excepciones
                Logging.LogRPTimersError($"Error in Execute() void: {e.Message}");
                base.EndTimer();
            }
        }

        /// <summary>
        /// Cancela la captura de territorio y notifica al jugador.
        /// </summary>
        private void CancelTurfCapture()
        {
            var usersToReturn = base.Client.GetRoomUser().GetRoom().GetRoomUserManager().GetRoomUsers().ToList();
            foreach (var user in usersToReturn)
            {
                if (user?.GetClient() != null)
                {
                    PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(user.GetClient(), "event_gang", "turf_cap_off");
                }
            }

            base.Client.SendWhisper("¡Se ha cancelado la captura del territorio por haberte movido de lugar!", 1);
            base.Client.GetRoomUser().GetRoom().TurfCapturing = false;
            base.Client.GetRoleplay().TurfCapturing = false;
            base.Client.GetRoleplay().CapturingTurf = null;
            base.Client.GetRoleplay().BreakGeneralTimer = false;
            base.EndTimer();
        }

        /// <summary>
        /// Completa la captura del territorio y actualiza los recursos de la banda.
        /// </summary>
        private void CompleteTurfCapture(Group Gang, Room Room)
        {
            // Actualización de la puntuación de la banda
            Gang.GangScore += new Utilities.CryptoRandom().Next(5, 25);
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("UPDATE `rp_gangs` SET `gang_score` = @gangscore WHERE `id` = @id");
                dbClient.AddParameter("gangscore", Gang.GangScore);
                dbClient.AddParameter("id", Gang.Id);
                dbClient.RunQuery();
            }

            // Anunciar la captura y premiar al jugador
            RoleplayManager.Shout(base.Client, $"*Captura con éxito el barrio de pandillas en nombre de {Gang.Name} [+10 paquetes de medicina para pandilla]*", 4);
            base.Client.SendWhisper("Ganaste 50+ paquetes de medicina para la pandilla y [1+] dinamita para ti");
            base.Client.GetRoleplay().Dynamite += 1;

            // BattlePass Challenge Integration
            PolarEnvironment.GetGame().GetBattlePassManager().ProgressChallenge(base.Client, "capture_turf", 1);

            // Actualizar la banda con los nuevos recursos
            Gang.MediPacks += 50;

            // Notificar a los miembros de la banda atacada
            NotifyGangMembersOfCapture();

            List<Group> MyGang = PolarEnvironment.GetGame().GetGroupManager().GetGangsForUser(base.Client.GetHabbo().Id);
            if (MyGang != null && MyGang.Count > 0)
                RoleplayManager.ClaimTurf(base.Client, Room, MyGang[0]);

            base.Client.GetRoleplay().TurfCapturing = false;
            base.Client.GetRoleplay().LoadingTimeLeft = 0;
        }

        /// <summary>
        /// Notifica a los miembros de la banda atacada sobre la captura del territorio.
        /// </summary>
        private void NotifyGangMembersOfCapture()
        {
            var room = base.Client.GetHabbo().CurrentRoom;
            foreach (var client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
            {
                if (client?.GetHabbo() == null)
                    continue;

                var theGroup = PolarEnvironment.GetGame().GetGroupManager().GetGangsForUser(client.GetHabbo().Id);
                if (theGroup?.Count > 0 && theGroup[0] == room.Group && !client.GetRoleplay().DisableRadio)
                {
                    client.SendWhisper($"[RADIO] ¡El territorio {room.Name}! ¡Ha sido capturado!", 30);
                }
            }
        }
    }

}