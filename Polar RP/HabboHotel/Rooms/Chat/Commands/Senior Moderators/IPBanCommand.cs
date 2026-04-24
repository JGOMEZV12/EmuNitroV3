using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.Utilities;
using Polar.HabboHotel.Users;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Moderation;
using Polar.Database.Interfaces;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Moderators.Seniors
{
    class IPBanCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_ban_ip"; }
        }

        public string Parameters
        {
            get { return "%username%"; }
        }

        public string Description
        {
            get { return "expulsar La propiedad intelectual y la cuenta ban a otro usuario."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("Ingrese el nombre de usuario del usuario que desea prohibir.", 1);
                return;
            }

            Habbo Habbo = PolarEnvironment.GetHabboByUsername(Params[1]);
            if (Habbo == null)
            {
                Session.SendWhisper("Se ha producido un error al encontrar ese usuario en la base de datos.", 1);
                return;
            }

            if (Habbo.GetPermissions().HasRight("mod_tool") && !Session.GetHabbo().GetPermissions().HasRight("mod_ban_any"))
            {
                Session.SendWhisper("Vaya, no puedes prohibir a ese usuario.", 1);
                return;
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
                Reason = "Razón no declarada.";

            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Username);

            Session.SendWhisper("baneado con exito '" + Username + "' por '" + Reason + "'!", 1);
        
            if (!string.IsNullOrEmpty(IPAddress))
                PolarEnvironment.GetGame().GetModerationManager().BanUser(Session.GetHabbo().Username, ModerationBanType.IP, IPAddress, Reason, Expire);
            PolarEnvironment.GetGame().GetModerationManager().BanUser(Session.GetHabbo().Username, ModerationBanType.USERNAME, Habbo.Username, Reason, Expire);

            if (TargetClient != null)
            {
                TargetClient.Disconnect(true);
            }
        }
    }
}