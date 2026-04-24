using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;

using Polar.Utilities;
using Polar.HabboHotel.Users;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Moderation;
using Polar.Database.Interfaces;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Moderators
{
    class BanCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_ban"; }
        }

        public string Parameters
        {
            get { return "%username% %length% %reason% "; }
        }

        public string Description
        {
            get { return "Quite un interruptor de reglas de la ciudad por un tiempo fijo."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            // Verificar parámetros mínimos (usuario y duración)
            if (Params.Length < 3)
            {
                Session.SendWhisper("Introduzca el nombre de usuario y la duración de la prohibición.\nEjemplo: :ban usuario 60 razón", 1);
                return;
            }

            Habbo Habbo = PolarEnvironment.GetHabboByUsername(Params[1]);
            if (Habbo == null)
            {
                Session.SendWhisper("Se ha producido un error al encontrar ese usuario en la base de datos.", 1);
                return;
            }

            if (Habbo.GetPermissions().HasRight("mod_soft_ban") && !Session.GetHabbo().GetPermissions().HasRight("mod_ban_any"))
            {
                Session.SendWhisper("Vaya, no puedes prohibir ese usuario.", 1);
                return;
            }

            Double Expire = 0;
            string Minutes = Params[2];

            if (Minutes == "perm")
                Expire = PolarEnvironment.GetUnixTimestamp() + 78892200;
            else
            {
                // Validar que Minutes sea un número
                if (!double.TryParse(Minutes, out double minutesValue))
                {
                    Session.SendWhisper("La duración debe ser un número en minutos o 'perm' para permanente.", 1);
                    return;
                }
                Expire = PolarEnvironment.GetUnixTimestamp() + (minutesValue * 60);
            }

            string Reason = "Razón no declarada";
            if (Params.Length >= 4)
                Reason = CommandManager.MergeParams(Params, 3);

            string Username = Habbo.Username;
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.RunQuery("UPDATE `user_info` SET `bans` = `bans` + '1' WHERE `user_id` = '" + Habbo.Id + "' LIMIT 1");
            }

            PolarEnvironment.GetGame().GetModerationManager().BanUser(Session.GetHabbo().Username, ModerationBanType.USERNAME, Habbo.Username, Reason, Expire);

            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Username);
            if (TargetClient != null)
            {
                TargetClient.Disconnect(true);
            }

            // Mostrar mensaje de confirmación
            if (Minutes == "perm")
            {
                Session.SendWhisper($"Has expulsado con éxito al usuario '{Username}' Permanentemente por: '{Reason}'!", 1);
            }
            else
            {
                int totalMinutes = Convert.ToInt32(Minutes);
                int hours = totalMinutes / 60;
                int minutesLeft = totalMinutes % 60;

                if (hours > 0)
                {
                    Session.SendWhisper($"Has expulsado con éxito al usuario '{Username}' por {hours} horas y {minutesLeft} Minuto(s) por: '{Reason}'!", 1);
                }
                else
                {
                    Session.SendWhisper($"El usuario fue expulsado exitosamente '{Username}' por {minutesLeft} Minuto(s) por: '{Reason}'!", 1);
                }
            }
        }
    }
}