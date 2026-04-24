using System;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.Core;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;

namespace Polar.HabboRoleplay.Timers.Types
{
    /// <summary>
    /// Makes the citizens cleanliness decrease over time
    /// </summary>
    public class AnimoTimer : RoleplayTimer
    {
        public AnimoTimer(string Type, GameClient Client, int Time, bool Forever, object[] Params)
            : base(Type, Client, Time, Forever, Params)
        {
        }

        /// <summary>
        /// Disminuye el ánimo del usuario y controla su energía
        /// </summary>
        public override void Execute()
        {
            try
            {
                // Validaciones iniciales
                if (base.Client == null || base.Client.GetHabbo() == null || base.Client.GetRoleplay() == null || base.Client.GetRoomUser() == null)
                {
                    base.EndTimer();
                    return;
                }

                var roomUser = base.Client.GetRoomUser();
                var roleplay = base.Client.GetRoleplay();

                // Comprobaciones de estado
                if (roomUser.IsAsleep || roleplay.StaffOnDuty || roleplay.AmbassadorOnDuty || roleplay.InShower || IsInTutorial())
                    return;

                TimeCount++;

                // Solo ejecutamos cada 600 ciclos
                if (TimeCount < 600)
                    return;

                TimeCount = 0;

                // Disminuir Animo si es necesario
                if (roleplay.Animo == 0)
                {
                    ApplySadEffectAndEnergy();
                    return;
                }

                // Disminuir el ánimo y verificar si el ánimo llega a 0
                DecreaseAnimo();

                // Si el ánimo es 0, aplicar efectos
                if (roleplay.Animo <= 0)
                {
                    ApplySadEffectAndMessage();
                }
            }
            catch (Exception e)
            {
                Logging.LogRPTimersError($"Error in Execute() void: {e}");
                base.EndTimer();
            }
        }

        /// <summary>
        /// Verifica si el jugador está en un tutorial
        /// </summary>
        private bool IsInTutorial()
        {
            return base.Client.GetHabbo().CurrentRoom != null && base.Client.GetHabbo().CurrentRoom.TutorialEnabled;
        }

        /// <summary>
        /// Disminuye el ánimo del usuario y gestiona la energía si el ánimo es 0
        /// </summary>
        private void ApplySadEffectAndEnergy()
        {
            base.Client.GetRoomUser().ApplyEffect(911); // Efecto de tristeza
            int AmountOfEnergy = Random.Next(1, 5);

            // Disminuir energía si el ánimo es 0
            if (base.Client.GetRoleplay().CurEnergy - AmountOfEnergy <= 0)
                base.Client.GetRoleplay().CurEnergy = 0;
            else
                base.Client.GetRoleplay().CurEnergy -= AmountOfEnergy;

            // Mensaje de advertencia
            base.Client.SendMessage(new RoomBubbleNotificationComposer("triste", "¡Estás triste! por favor juega, ten relaciones o ve a un prostíbulo", ""));
        }

        /// <summary>
        /// Disminuye el ánimo del usuario
        /// </summary>
        private void DecreaseAnimo()
        {
            int AmountOfAnimo = Random.Next(1, 3);
            var roleplay = base.Client.GetRoleplay();

            if (roleplay.Animo - AmountOfAnimo <= 0)
                roleplay.Animo = 0;
            else
                roleplay.Animo -= AmountOfAnimo;
        }

        /// <summary>
        /// Aplica el efecto de tristeza y un mensaje cuando el ánimo llega a 0
        /// </summary>
        private void ApplySadEffectAndMessage()
        {
            base.Client.GetRoomUser().ApplyEffect(911); // Efecto de tristeza
            base.Client.SendMessage(new RoomBubbleNotificationComposer("triste", "¡Te sientes muy triste! Haz algo divertido para poder seguir realizando actividades", ""));
        }
    }
}