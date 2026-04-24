using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Collections.Generic;
using System.Threading.Tasks;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Notifications;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Administrators
{
    class BlackListCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_blacklist"; }
        }

        public string Parameters
        {
            get { return "%user%"; }
        }

        public string Description
        {
            get { return "Añade el usuario a la lista negra."; }
        }

        public async Task Execute(GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                if (BlackListManager.BlackList.Count <= 0)
                {
                    Session.SendWhisper("No hay usuarios en la lista negra!", 1);
                    return;
                }
                else
                {
                    StringBuilder Message = new StringBuilder().Append("--- Usuarios actuales en la lista negra ---\n\n");

                    foreach (var user in BlackListManager.BlackList)
                    {
                        Message.Append(PolarEnvironment.GetHabboById(user).Username + "\n");
                    }
                    Session.SendMessage(new MOTDNotificationComposer(Message.ToString()));
                }
            }

            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);
            if (TargetClient == null)
            {
                using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("SELECT `id`,`username` FROM `users` where `username` = '" + Params[1] + "' LIMIT 1");
                    var UserRow = dbClient.getRow();

                    if (UserRow == null)
                    {
                        Session.SendWhisper("¡Lo siento! Esta persona no existe!", 1);
                        return;
                    }

                    int UserId = Convert.ToInt32(UserRow["id"]);
                    string Username = UserRow["username"].ToString();

                    if (BlackListManager.BlackList.Contains(UserId))
                    {
                        Session.SendWhisper("Esta persona ya está en la lista negra!", 1);
                        return;
                    }
                    else
                    {
                        BlackListManager.AddBlackList(UserId);
                        Session.Shout("*Utiliza sus poderes divinos para añadir a " + Username + " a la lista negra*", 23);
                        return;
                    }
                }
            }
            else
            {
                if (BlackListManager.BlackList.Contains(TargetClient.GetHabbo().Id))
                {
                    Session.SendWhisper("Esta persona ya está en la lista negra!", 1);
                    return;
                }
                else
                {
                    if (TargetClient.GetRoleplay().IsWorking && GroupManager.HasJobCommand(TargetClient, "guide"))
                        TargetClient.GetRoleplay().IsWorking = false;

                    BlackListManager.AddBlackList(TargetClient.GetHabbo().Id);
                    Session.Shout("*Utiliza sus poderes divinos para añadir a " + TargetClient.GetHabbo().Username + " a la lista negra*", 23);
                    TargetClient.SendNotification("Usted acaba de ser agregado a la lista negra por " + Session.GetHabbo().Username + "!");
                    return;
                }
            }
        }
    }
}