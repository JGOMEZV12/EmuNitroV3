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

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.General
{
    class ChangeClassCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_change_class"; }
        }

        public string Parameters
        {
            get { return "%class%"; }
        }

        public string Description
        {
            get { return "Le permite cambiar su clase si todavía está en el nivel 1."; }
        }

        public async Task Execute(GameClient Session, Room Room, string[] Params)
        {
            #region Variables
            bool RunQuery = false;
            #endregion

            #region Conditions
            if (Params.Length == 1)
            {
                Session.SendWhisper("Porfavor introduzca':cambiarclase [type]' mas el [type] es 'armero', 'ciudadano, 'peleador', or 'list' Si no está seguro", 1);
                return;
            }

            if (Session.GetRoleplay().Level > 1)
            {
                Session.SendWhisper("¡Demasiado tarde, ya no estás en el nivel 1!", 1);
                return;
            }

            if (Session.GetRoleplay().PermanentClass)
            {
                Session.SendWhisper("¡Ya has elegido tu clase permanente, lo siento!", 1);
                return;
            }

            if (Session.GetRoleplay().Class.ToLower() == Params[1].ToLower())
            {
                Session.SendWhisper("Ya eres un " + Session.GetRoleplay().Class + "!", 1);
                return;
            }
            #endregion

            #region Execute
            switch (Params[1].ToLower())
            {
                case "list":
                    {
                        StringBuilder Message = new StringBuilder().Append("----- " + PolarEnvironment.GetConfig().data["hotel.name"] + " Clases -----\n\n");
                        Message.Append("¡Los civiles reciben más dinero cuando completan un ciclo de trabajo!\n\n");
                        Message.Append("peleadors cause slightly more damage with their fists (:hit command)!\n\n");
                        Message.Append("armeros cause slightly more damage with gun weapons (:shoot command)!\n\n");
                        Message.Append("Choose wisely as you cannot change your class ever again!");
                        Session.SendNotification(Message.ToString());
                        break;
                    }
                case "armero":
                    {
                        if (Params.Length > 2)
                        {
                            if (Params[2].ToLower() == "yes")
                            {
                                Session.GetRoleplay().PermanentClass = true;
                                Session.GetRoleplay().Class = "Armero";
                                Session.GetHabbo().Motto = "Armero de " + PolarEnvironment.GetConfig().data["hotel.name"];
                                Session.GetHabbo().Poof(true);
                                RunQuery = true;
                            }
                            else
                                Session.SendWhisper("Are you sure you want to be a armero? If so, please type ':changeclass armero yes'", 1);
                        }
                        else
                            Session.SendWhisper("Are you sure you want to be a armero? If so, please type ':changeclass armero yes'", 1);

                        break;
                    }
                case "peleador":
                    {
                        if (Params.Length > 2)
                        {
                            if (Params[2].ToLower() == "yes")
                            {
                                Session.GetRoleplay().PermanentClass = true;
                                Session.GetRoleplay().Class = "peleador";
                                Session.GetHabbo().Motto = "peleador";
                                Session.GetHabbo().Poof(true);
                                RunQuery = true;
                            }
                            else
                                Session.SendWhisper("¿Estás seguro de que quieres ser un peleadorr? Si es así, escribe ':changeclass peleador yes'", 1);
                        }
                        else
                            Session.SendWhisper("¿Estás seguro de que quieres ser un peleadorr? Si es así, escribe ':changeclass peleador yes'", 1);

                        break;
                    }
                case "ciudadano":
                    {
                        if (Params.Length > 2)
                        {
                            if (Params[2].ToLower() == "yes")
                            {
                                Session.GetRoleplay().PermanentClass = true;
                                Session.GetRoleplay().Class = "ciudadano";
                                Session.GetHabbo().Motto = "ciudadano";
                                Session.GetHabbo().Poof(true);
                                RunQuery = true;
                            }
                            else
                                Session.SendWhisper("¿Estás seguro de que quieres ser un ciudadano? Si es así, escribe ':changeclass ciudadano yes'", 1);
                        }
                        else
                            Session.SendWhisper("¿Estás seguro de que quieres ser un ciudadano? Si es así, escribe ':changeclass ciudadano yes'", 1);
                        break;
                    }
                default:
                    {
                        Session.SendWhisper("Slecciona uno de estos 'armero', 'peleador', 'ciudadano', o 'list' para ver cualidades", 1);
                        break;
                    }
            }

            if (RunQuery)
            {
                using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("UPDATE `users` set `motto` = @class WHERE `id` = @userid LIMIT 1");
                    dbClient.AddParameter("class", Session.GetRoleplay().Class);
                    dbClient.AddParameter("userid", Session.GetHabbo().Id);
                    dbClient.RunQuery();

                    dbClient.SetQuery("UPDATE `rp_stats` set `class` = @class, `permanent_class` = @permanent_class WHERE `id` = @userid LIMIT 1");
                    dbClient.AddParameter("class", Session.GetRoleplay().Class);
                    dbClient.AddParameter("permanent_class", PolarEnvironment.BoolToEnum(Session.GetRoleplay().PermanentClass));
                    dbClient.AddParameter("userid", Session.GetHabbo().Id);
                    dbClient.RunQuery();
                }

                Session.SendNotification("Has cambiado tu clase con éxito " + Session.GetRoleplay().Class + "!");
                return;
            }
            #endregion
        }
    }
}