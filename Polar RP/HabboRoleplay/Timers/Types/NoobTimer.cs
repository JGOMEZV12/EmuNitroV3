using System;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.Core;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;

namespace Polar.HabboRoleplay.Timers.Types
{
    /// <summary>
    /// Noob timer
    /// </summary>
    public class NoobTimer : RoleplayTimer
    {
        public NoobTimer(string Type, GameClient Client, int Time, bool Forever, object[] Params)
            : base(Type, Client, Time, Forever, Params)
        {
            // 15 minutos convertidos a milisegundos
            TimeLeft = base.Client.GetRoleplay().NoobTimeLeft * 60000;
        }

        /// <summary>
        /// Aumenta el hambre del usuario y maneja la expiración de la protección de noob.
        /// </summary>
        public override void Execute()
        {
            try
            {
                // Verificación de datos fundamentales para evitar errores
                if (base.Client == null ||
                    base.Client.GetHabbo() == null ||
                    base.Client.GetRoleplay() == null ||
                    !base.Client.GetRoleplay().IsNoob)
                {
                    base.EndTimer();
                    return;
                }

                // Validaciones de sala
                var currentRoom = base.Client.GetHabbo().CurrentRoom;
                if (currentRoom == null || base.Client.GetHabbo().CurrentRoomId <= 0)
                {
                    base.EndTimer();
                    return;
                }

                var roomUser = base.Client.GetRoomUser();
                if (roomUser == null || roomUser.IsAsleep)
                {
                    return; // Si está dormido, no hacer nada
                }

                // Decrementa el tiempo
                TimeCount++;
                TimeLeft -= 1000;

                // Condiciones para notificar el tiempo restante
                if (TimeLeft > 0)
                {
                    if (TimeCount == 60)
                    {
                        int minutesLeft = TimeLeft / 60000;
                        base.Client.SendWhisper($"¡Tienes {minutesLeft} minuto(s) hasta que pierdas tu Protección de Dios!", 1);
                        TimeCount = 0; // Reinicia el contador para mantener el intervalo
                    }
                    return;
                }

                // Notificación cuando la inmunidad termina
                base.Client.SendWhisper("¡La inmunidad se ha terminado, defiende tu mismo de las amenazas de la ciudad!", 1);
                base.Client.GetRoleplay().IsNoob = false; // Finaliza la protección de noob
                base.Client.GetRoleplay().NoobTimeLeft = 0;
                base.EndTimer(); // Detiene el temporizador
            }
            catch (Exception e)
            {
                // Manejo de excepciones
                Logging.LogRPTimersError("Error in Execute() void: " + e.Message);
                base.EndTimer(); // Detiene el temporizador en caso de error
            }
        }
    }

}