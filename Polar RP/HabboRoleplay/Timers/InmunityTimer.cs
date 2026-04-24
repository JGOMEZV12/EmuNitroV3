using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Polar.Messages;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms;
using Polar.Core;

namespace Polar.HabboRoleplay.Timers
{
    public class InmunityTimer : RoleplayTimer
    {
        public InmunityTimer(string Type, GameClient Client, int Time, bool Forever, object[] Params)
            : base(Type, Client, Time, Forever, Params)
        {
            TimeLeft = 3 * 1000; // Tiempo inicial: 3 segundos
        }

        public override void Execute()
        {
            try
            {
                // Validaciones iniciales para evitar excepciones
                if (base.Client == null ||
                    base.Client.GetHabbo() == null ||
                    base.Client.GetRoleplay() == null ||
                    (base.Client.GetRoleplay().IsWorking && base.Client.GetRoleplay().JobId == 3))
                {
                    base.EndTimer();
                    return;
                }

                // Validaciones de la sala actual del cliente
                var currentRoom = base.Client.GetHabbo().CurrentRoom;
                if (currentRoom == null || base.Client.GetHabbo().CurrentRoomId <= 0)
                {
                    base.EndTimer();
                    return;
                }

                var roleplayData = base.Client.GetRoleplay();
                roleplayData.IsNoob = true;

                TimeCount++;
                TimeLeft -= 1000;

                #region Conditions
                if (TimeLeft > 0)
                {
                    int secondsRemaining = TimeLeft / 1000;

                    // Enviar mensaje al usuario solo al inicio o cada segundo (según necesidad)
                    if (TimeCount == 1)
                    {
                        base.Client.SendWhisper($"*Te quedan [{secondsRemaining}] segundos de inmunidad, ten paciencia...*");
                        TimeCount = 0; // Reinicia el contador para mantener el intervalo
                    }

                    return;
                }
                #endregion

                #region Execute
                // Finaliza la inmunidad
                roleplayData.IsNoob = false;
                base.Client.SendWhisper("*¡Tu inmunidad ha terminado!*");
                base.EndTimer();
                #endregion
            }
            catch (Exception e)
            {
                Logging.LogRPTimersError("Error en Execute() void: " + e.Message);
                base.EndTimer();
            }
        }
    }

}
