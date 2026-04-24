using System;
using System.Linq;
using System.Text;
using System.Data;
using System.Diagnostics;
using System.Collections.Generic;

using Polar.Communication.Packets.Outgoing.Users;
using Polar.Communication.Packets.Outgoing.Notifications;

using Polar.Communication.Packets.Outgoing.Handshake;
using Polar.Communication.Packets.Outgoing.Quests;
using Polar.HabboHotel.Items;
using Polar.Communication.Packets.Outgoing.Inventory.Furni;
using Polar.Communication.Packets.Outgoing.Catalog;
using Polar.HabboHotel.Quests;
using Polar.HabboHotel.Rooms;
using System.Threading;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Rooms.Avatar;
using Polar.Communication.Packets.Outgoing.Pets;
using Polar.Communication.Packets.Outgoing.Messenger;
using Polar.HabboHotel.Users.Messenger;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using Polar.Communication.Packets.Outgoing.Availability;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.General
{
    class ChangeUClassCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_change_user_class"; }
        }

        public string Parameters
        {
            get { return "%user% %class%"; }
        }

        public string Description
        {
            get { return "Permite cambiar la clase de un usuario determinado."; }
        }

        public async Task Execute(GameClient Session, Room Room, string[] Params)
        {
            #region Variables
            bool RunQuery = false;
            #endregion

            #region Conditions
            if (Params.Length < 3)
            {
                Session.SendWhisper("Please type ':changeclass [user] [type]' where [type] is 'armero', 'ciudadano, 'peleador', or 'list' if you are unsure!", 1);
                return;
            }

            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);

            if (TargetClient == null)
            {
                Session.SendWhisper("No se encuentra el usuarios", 1);
                return;
            }

            if (TargetClient.GetRoomUser() == null || TargetClient.GetRoomUser().RoomId != Room.Id)
            {
                Session.SendWhisper("No esta en la misma sala que tu", 1);
                return;
            }

            string Class = Params[2].ToString().ToLower();

            if (TargetClient.GetRoleplay().Class.ToLower() == Class)
            {
                Session.SendWhisper("Listo se ha cambiado a " + Class + "!", 1);
                return;
            }
            #endregion

            #region Execute
            switch (Class)
            {
                case "armero":
                    {
                        TargetClient.GetRoleplay().Class = "armero";
                        TargetClient.GetHabbo().Motto = "Armero de " + PolarEnvironment.GetConfig().data["hotel.name"];
                        TargetClient.GetHabbo().Poof(true);
                        RunQuery = true;
                        break;
                    }
                case "peleador":
                    {
                        TargetClient.GetRoleplay().Class = "peleador";
                        TargetClient.GetHabbo().Motto = "Peleador de " + PolarEnvironment.GetConfig().data["hotel.name"];
                        TargetClient.GetHabbo().Poof(true);
                        RunQuery = true;
                        break;
                    }
                case "ciudadano":
                    {
                        TargetClient.GetRoleplay().Class = "ciudadano";
                        TargetClient.GetHabbo().Motto = "Ciudadano de " + PolarEnvironment.GetConfig().data["hotel.name"];
                        TargetClient.GetHabbo().Poof(true);
                        RunQuery = true;
                        break;
                    }
                default:
                    {
                        Session.SendWhisper("Para cambiar la clase escribe 'armero', 'peleador', 'ciudadano', o 'list' para asignarle", 1);
                        break;
                    }
            }

            if (RunQuery)
            {
                #region Update Class in database
                using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("UPDATE `users` set `motto` = @class WHERE `id` = @userid LIMIT 1");
                    dbClient.AddParameter("class", TargetClient.GetRoleplay().Class);
                    dbClient.AddParameter("userid", TargetClient.GetHabbo().Id);
                    dbClient.RunQuery();

                    dbClient.SetQuery("UPDATE `rp_stats` set `class` = @class WHERE `id` = @userid LIMIT 1");
                    dbClient.AddParameter("class", TargetClient.GetRoleplay().Class);
                    dbClient.AddParameter("userid", TargetClient.GetHabbo().Id);
                    dbClient.RunQuery();
                }
                #endregion
            }
            Session.Shout("*Utiliza sus poderes divinos para cambiar  " + TargetClient.GetHabbo().Username + "'s la clase a '" + Class + "'*", 23);
            return;
            #endregion
        }
    }
}