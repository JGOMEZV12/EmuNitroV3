using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Administrators
{
    class UnBlackListCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_blacklist_undo"; }
        }

        public string Parameters
        {
            get { return "%user%"; }
        }

        public string Description
        {
            get { return "Elimina al usuario de la lista negra."; }
        }

        public async Task Execute(GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("Por favor, ingrese el nombre de usuario de la persona a la que desea anular la lista.", 1);
                return;
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

                    if (!BlackListManager.BlackList.Contains(UserId))
                    {
                        Session.SendWhisper("¡Esta persona no ha sido incluida en la lista negra!", 1);
                        return;
                    }
                    else
                    {
                        BlackListManager.RemoveBlackList(UserId);
                        Session.Shout("*Utiliza sus poderes divinos para eliminar a " + Username + " de la lista negra*", 23);
                        return;
                    }
                }
            }
            else
            {
                if (!BlackListManager.BlackList.Contains(TargetClient.GetHabbo().Id))
                {
                    Session.SendWhisper("This person has not been blacklisted!", 1);
                    return;
                }
                else
                {
                    BlackListManager.RemoveBlackList(TargetClient.GetHabbo().Id);
                    Session.Shout("*Utiliza sus poderes divinos para eliminar a " + TargetClient.GetHabbo().Username + " de la lista negra*", 23);
                    TargetClient.SendNotification("Acaba de ser eliminado de la lista negra por " + Session.GetHabbo().Username + "!");
                    return;
                }
            }
        }
    }
}