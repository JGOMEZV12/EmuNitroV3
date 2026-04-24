using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.Database.Interfaces;
using Polar.Utilities;
using Polar.HabboHotel.Users;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Moderation;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Moderators
{
    class MuteCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_mute"; }
        }

        public string Parameters
        {
            get { return "%username% %time%"; }
        }

        public string Description
        {
            get { return "Silencia a un usuario."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("Introduce el nombre de usuario y tiempo (max 600 segundos).", 1);
                return;
            }

            Habbo Habbo = PolarEnvironment.GetHabboByUsername(Params[1]);
            if (Habbo == null)
            {
                Session.SendWhisper("No se ha padido encontrar en la base de datos.", 1);
                return;
            }

            if (Habbo.GetPermissions().HasRight("mod_tool") && !Session.GetHabbo().GetPermissions().HasRight("mod_mute_any"))
            {
                Session.SendWhisper("Vaya, no puedes silenciar a ese usuario.", 1);
                return;
            }

            double Time;
            if (double.TryParse(Params[2], out Time))
            {
                if (Time > 900 && !Session.GetHabbo().GetPermissions().HasRight("mod_mute_limit_override"))
                    Time = 900;

                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.RunQuery("UPDATE `users` SET `time_muted` = '" + Time + "' WHERE `id` = '" + Habbo.Id + "' LIMIT 1");
                }

                if (Habbo.GetClient() != null)
                {
                    Habbo.TimeMuted = Time;
                    DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(PolarEnvironment.GetUnixTimestamp() + Time).ToLocalTime();

                    Habbo.GetClient().SendWhisper("Usted ha sido muteado " + origin.ToString("dd-MM-yyyy H:mm:ss") + ". Por el bien de la comunidad, manténgase alejado de cualquier chat negativo o promocionar otro sitio web. Leer a través de la forma y los términos de " + PolarEnvironment.GetConfig().data["hotel.name"] + " lo ayudará a evitar este problema en el futuro.");
                }

                Session.SendWhisper("Haz muteado correctamente a " + Habbo.Username + " por " + Time + " segundos.", 1);
            }
            else
                Session.SendWhisper("Ingrese un número entero válido.", 1);
        }
    }
}