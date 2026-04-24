using System;
using System.Data;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Users;
using Polar.Database.Interfaces;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Notifications;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Managers
{
    class NameCheckCommand :IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_check_name"; }
        }

        public string Parameters
        {
            get { return "%username%"; }
        }

        public string Description
        {
            get { return "Comprueba si antiguos nombres de usuario han sido marcados."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("Introduzca el nombre de usuario del usuario que desea comprobar.", 1);
                return;
            }

            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                int UserId = 0;
                string Username = Params[1];

                if (TargetClient == null)
                {
                    dbClient.SetQuery("SELECT `id`, `username` FROM `users` where `username` = '" + Username + "' LIMIT 1");
                    DataRow Row = dbClient.getRow();

                    if (Row == null)
                    {
                        Session.SendWhisper("¡Lo siento! ¡Esta persona no existe!", 1);
                        return;
                    }

                    UserId = Convert.ToInt32(Row["id"]);
                    Username = Row["username"].ToString();
                }
                else
                {
                    UserId = TargetClient.GetHabbo().Id;
                    Username = TargetClient.GetHabbo().Username;
                }

                dbClient.SetQuery("SELECT `new_name`, `old_name` FROM `logs_client_namechange` WHERE `user_id` = '" + UserId + "'");
                DataTable Table = dbClient.getTable();

                if (Table.Rows.Count == 0)
                {
                    Session.SendWhisper("Sorry! This user has never changed their name before!", 1);
                    return;
                }

                StringBuilder Message = new StringBuilder();
                Message.Append("----- " + Username + "'s nombre cambiado -----\n");
                Message.Append("Old Name ---> New Name\n\n");

                foreach (DataRow Row in Table.Rows)
                {
                    string OldName = Row["old_name"].ToString();
                    string NewName = Row["new_name"].ToString();

                    Message.Append(OldName + " ---> " + NewName + "\n");
                }
                Session.SendMessage(new MOTDNotificationComposer(Message.ToString()));
            }
        }
    }
}
