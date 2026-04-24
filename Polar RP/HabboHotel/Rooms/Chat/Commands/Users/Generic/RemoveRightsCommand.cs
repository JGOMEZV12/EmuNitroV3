using System;
using System.Linq;
using System.Text;
using System.Drawing;
using Polar.Utilities;
using System.Collections.Generic;
using Polar.HabboRoleplay.Timers;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using Polar.Communication.Packets.Outgoing.Rooms.Permissions;
using Polar.Communication.Packets.Outgoing.Rooms.Settings;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Cache;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic
{
    class RemoveRightsCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_rights"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Leer."; }
        }

        public async Task Execute(GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("el comando es: ':darpermisos x'.", 1);
                return;
            }
            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);

            if (TargetClient == Session)
            {
                Session.SendWhisper("¡No puedes usar este comando en ti!", 1);
                return;
            }

            if (TargetClient == null || TargetClient.GetHabbo() == null || TargetClient.GetRoleplay() == null)
            {
                Session.SendWhisper("No se pudo encontrar a este usuario, quizás estén desconectados.", 1);
                return;
            }
            if (!Room.CheckRights(Session, true))
            {
                Session.SendWhisper("¡No eres el dueño de la sala!", 1);
                return;
            }

                int UserId = TargetClient.GetHabbo().Id;
                if (UserId > 0 && Room.UsersWithRights.Contains(UserId))
                {
                    RoomUser User = Room.GetRoomUserManager().GetRoomUserByHabbo(UserId);
                    if (User != null && !User.IsBot)
                    {
                        User.RemoveStatus("flatctrl 1");
                        User.UpdateNeeded = true;


                        User.GetClient().SendMessage(new YouAreControllerComposer(0));
                    }

                    using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                    {
                        dbClient.SetQuery("DELETE FROM `room_rights` WHERE `user_id` = @uid AND `room_id` = @rid LIMIT 1");
                        dbClient.AddParameter("uid", UserId);
                        dbClient.AddParameter("rid", Room.Id);
                        dbClient.RunQuery();
                    }

                    if (Room.UsersWithRights.Contains(UserId))
                        Room.UsersWithRights.Remove(UserId);

                    Session.SendMessage(new FlatControllerRemovedComposer(Room.Id, UserId));
                }
            Session.SendWhisper("¡Se le ha removido los permisos en la sala a "+TargetClient.GetHabbo().Username+"!", 1);
        }
    }
}