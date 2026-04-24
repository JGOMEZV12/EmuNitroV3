using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.HabboHotel.Rooms;
using System.Data;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Owners
{
    class UnBanCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_ban_undo"; }
        }

        public string Parameters
        {
            get { return "%username%"; }
        }

        public string Description
        {
            get { return "Desbanea el nombre de usuario (removes their user/mac/ip ban)."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("Vaya, se te olvidó elegir un usuario de destino");
                return;
            }

            using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT * FROM `users` WHERE `username` = '" + Params[1] + "' LIMIT 1");
                DataRow Row = dbClient.getRow();

                if (Row == null)
                {
                    Session.SendWhisper("Lo sentimos, este nombre de usuario no se pudo encontrar en la base de datos.", 1);
                    return;
                }

                int UserId = Convert.ToInt32(Row["id"]);
                string UserName = Row["username"].ToString();
                string LastIp = Row["ip_last"].ToString();
                string MachineId = Row["machine_id"].ToString();

                dbClient.SetQuery("SELECT count(id) FROM `bans` WHERE `value` = '" + UserId + "' OR `value` = '" + UserName + "' OR `value` = '" + MachineId + "'");
                int Count = dbClient.getInteger();

                if (Count < 1)
                {
                    Session.SendWhisper("Lo sentimos, pero este usuario no ha sido prohibido de ninguna manera", 1);
                    return;
                }
                else
                {
                    dbClient.RunQuery("DELETE FROM `bans` WHERE `value` = '" + LastIp + "' OR `value` = '" + UserName + "' OR `value` = '" + MachineId + "'");
                    PolarEnvironment.GetGame().GetModerationManager().ReCacheBans();
                    Session.SendWhisper("Desbanea a '" + UserName + "'*", 1);
                    return;
                }
            }
        }
    }
}
