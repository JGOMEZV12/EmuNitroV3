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
    class GiveRightsCommand : IChatCommand
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

            if (Room.UsersWithRights.Contains(TargetClient.GetHabbo().Id))
            {
                Session.SendNotification(PolarEnvironment.GetGame().GetLanguageLocale().TryGetValue("room_rights_has_rights_error"));
                return;
            }

            Room.UsersWithRights.Add(TargetClient.GetHabbo().Id);

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.RunQuery("INSERT INTO `room_rights` (`room_id`,`user_id`) VALUES ('" + Room.RoomId + "','" + TargetClient.GetHabbo().Id + "')");
            }

            RoomUser RoomUser = Room.GetRoomUserManager().GetRoomUserByHabbo(TargetClient.GetHabbo().Id);
            if (RoomUser != null && !RoomUser.IsBot)
            {
                RoomUser.SetStatus("flatctrl 1", "");
                RoomUser.UpdateNeeded = true;
                if (RoomUser.GetClient() != null)
                    RoomUser.GetClient().SendMessage(new YouAreControllerComposer(1));

                Session.SendMessage(new FlatControllerAddedComposer(Room.RoomId, RoomUser.GetClient().GetHabbo().Id, RoomUser.GetClient().GetHabbo().Username));
            }
            else
            {
                using (UserCache User = PolarEnvironment.GetGame().GetCacheManager().GenerateUser(TargetClient.GetHabbo().Id))
                {
                    if (User != null)
                        Session.SendMessage(new FlatControllerAddedComposer(Room.RoomId, User.Id, User.Username));
                }
            }
            Session.SendWhisper("¡Se le ha concedido permisos en la sala a "+TargetClient.GetHabbo().Username+"!", 1);
        }
    }
}