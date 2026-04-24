using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Timers;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Jobs.General
{
    class SendhomeCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_corp_send_home"; }
        }

        public string Parameters
        {
            get { return "%user% %minutes%"; }
        }

        public string Description
        {
            get { return "Sends one of your workers home."; }
        }

        public async Task Execute(GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Variables
            int SendHomeTime;
            int Bubble = 4;
            #endregion

            #region Conditions
            if (Params.Length != 3)
            {
                Session.SendWhisper("Por favor ingrese el nombre de usuario y el tiempo asignado para enviar a casa!", 1);
                return;
            }

            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);
            if (TargetClient == null)
            {
                Session.SendWhisper("Se ha producido un error al intentar encontrar a ese usuario, tal vez estén sin conexión.", 1);
                return;
            }

            if (Session.GetRoleplay().TryGetCooldown("sendhome"))
                return;

            if (!Session.GetHabbo().GetPermissions().HasRight("corporation_rights"))
            {
                if (!GroupManager.HasJobCommand(Session, "sendhome"))
                {
                    Session.SendWhisper("Usted no es un rango lo suficientemente alto en su corporación para usar este comando!", 1);
                    return;
                }

                if (TargetClient == Session)
                {
                    Session.SendWhisper("¡No puedes enviarte a casa!");
                    return;
                }

                if (Session.GetRoleplay().JobId != TargetClient.GetRoleplay().JobId)
                {
                    Session.SendWhisper("¡Este ciudadano no trabaja para usted!", 1);
                    return;
                }

                if (TargetClient.GetRoleplay().JobRank >= Session.GetRoleplay().JobRank)
                {
                    Session.SendWhisper("¡No puedes enviar a ese ciudadano a casa!", 1);
                    return;
                }
                Bubble = 4;
            }
            if (Session.GetHabbo().GetPermissions().HasRight("corporation_rights"))
                Bubble = 23;
            #endregion

            if (int.TryParse(Params[2], out SendHomeTime))
            {
                if (SendHomeTime > 30)
                {
                    Session.SendWhisper("¡Usted no puede enviar a su trabajador por más de 30 minutos!", 1);
                    return;
                }

                if (TargetClient.GetRoleplay().IsWorking)
                {
                    WorkManager.RemoveWorkerFromList(TargetClient);
                    TargetClient.GetRoleplay().IsWorking = false;
                    TargetClient.GetHabbo().Poof();
                }

                TargetClient.GetRoleplay().SendHomeTimeLeft = SendHomeTime;
                TargetClient.SendWhisper("Te han puesto en suspensión por " + SendHomeTime + " minutos, ¡No puedes trabajar hasta que suspensión haya sido completada!", 1);

                if (!TargetClient.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("sendhome"))
                    TargetClient.GetRoleplay().TimerManager.CreateTimer("sendhome", 1000, false);

                if (Bubble == 4)
                    Session.Shout("*Le pone suspensión a " + TargetClient.GetHabbo().Username + " por " + SendHomeTime + " minutos*", Bubble);
                else
                    Session.Shout("*Utiliza sus poderes divinos para enviar a " + TargetClient.GetHabbo().Username + " por suspensión por " + SendHomeTime + " minutos*", Bubble);

                Session.GetRoleplay().CooldownManager.CreateCooldown("sendhome", 1000, 5);
            }
            else
            {
                Session.SendWhisper("Por favor inserte un número para el tiempo asignado", 1);
                return;
            }
        }
    }
}