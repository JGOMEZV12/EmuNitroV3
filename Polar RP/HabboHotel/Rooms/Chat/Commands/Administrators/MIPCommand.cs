using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.Database.Interfaces;
using Polar.Utilities;
using Polar.HabboHotel.Users;
using Polar.HabboHotel.GameClients;

using Polar.HabboHotel.Moderation;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Administrators
{
    class MIPCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_ban_machine_ip"; }
        }

        public string Parameters
        {
            get { return "%username%"; }
        }

        public string Description
        {
            get { return "La prohibición de la máquina, la prohibición de IP y la prohibición de la cuenta de otro usuario."; }
        }

        public Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {

            if (Params.Length == 1)
            {
                Session.SendWhisper("Ingrese el nombre de usuario del usuario que desea utilizar mac ban & IP ban.", 1);
                return Task.CompletedTask;
            }

            Habbo Habbo = PolarEnvironment.GetHabboByUsername(Params[1]);
            if (Habbo == null)
            {
                Session.SendWhisper("Se ha producido un error al encontrar ese usuario en la base de datos.", 1);
                return Task.CompletedTask;
            }

            if (Habbo.GetPermissions().HasRight("mod_tool") && !Session.GetHabbo().GetPermissions().HasRight("mod_ban_any"))
            {
                Session.SendWhisper("No puedes darle ban a este usuario", 1);
                return Task.CompletedTask;
            }

            String IPAddress = String.Empty;
            Double Expire = PolarEnvironment.GetUnixTimestamp() + 78892200;
            string Username = Habbo.Username;

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.RunQuery("UPDATE `user_info` SET `bans` = `bans` + '1' WHERE `user_id` = '" + Habbo.Id + "' LIMIT 1");

                dbClient.SetQuery("SELECT `ip_last` FROM `users` WHERE `id` = '" + Habbo.Id + "' LIMIT 1");
                IPAddress = dbClient.getString();
            }

            string Reason = null;
            if (Params.Length >= 3)
                Reason = CommandManager.MergeParams(Params, 2);
            else
                Reason = "INVESTIGAR CASO, NO SE ESPECIFICÓ RAZÓN";

            if (!string.IsNullOrEmpty(IPAddress))
                PolarEnvironment.GetGame().GetModerationManager().BanUser(Session.GetHabbo().Username, ModerationBanType.IP, IPAddress, Reason, Expire);
            PolarEnvironment.GetGame().GetModerationManager().BanUser(Session.GetHabbo().Username, ModerationBanType.USERNAME, Habbo.Username, Reason, Expire);

            if (!string.IsNullOrEmpty(Habbo.MachineId))
                PolarEnvironment.GetGame().GetModerationManager().BanUser(Session.GetHabbo().Username, ModerationBanType.MACHINE, Habbo.MachineId, Reason, Expire);

            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Username);

            if (TargetClient == null)
            {
                Session.SendWhisper("¡Este usuario no pudo ser encontrado!", 1);
                return Task.CompletedTask;
            }

            PolarEnvironment.GetGame().GetClientManager().StaffWhisperAlert("He baneado al usuario: '" + Username + "' por: '" + Reason + "'", Session);
            TargetClient.Disconnect(true);

            return Task.CompletedTask;
        }
    }
}