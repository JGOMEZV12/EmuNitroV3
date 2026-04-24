using ConnectionManager;
﻿using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms;
using Polar.HabboRoleplay.Misc;
using Polar.Net;
using Polar.HabboHotel.Groups;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Users;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;

// Agregar un alias para evitar la ambigüedad
using Group = Polar.HabboHotel.Groups.Group;

namespace Polar.HabboHotel.Roleplay.Web.Outgoing.Misc
{
    /// <summary>
    /// GroupsWebEvent class.
    /// </summary>
    class GroupsWebEvent : IWebEvent
    {
        /// <summary>
        /// Executes socket data.
        /// </summary>
        /// <param name="Client"></param>
        /// <param name="Data"></param>
        /// <param name="Socket"></param>
        public void Execute(GameClient Client, string Data, ConnectionInformation Socket)
        {
            if (!PolarEnvironment.GetGame().GetWebEventManager().SocketReady(Client, true) ||
                !PolarEnvironment.GetGame().GetWebEventManager().SocketReady(Socket))
                return;

            if (Client?.GetRoomUser() == null || Client.GetRoomUser().RoomId <= 0)
                return;

            string Action = (Data.Contains(',') ? Data.Split(',')[0] : Data);

            //Generamos la Sala
            if (!RoleplayManager.GenerateRoom(Client.GetRoomUser().RoomId, out Room Room))
                return;

            switch (Action)
            {
                #region Open
                case "open":
                    {
                        if (Room?.Group == null)
                            return;

                        string Founder = "Ninguno";
                        List<GroupMember> Administrators = Room.Group.Members.Values
                            .Where(x => x.IsAdmin)
                            .OrderBy(x => x.UserId)
                            .ToList();

                        if (Administrators.Count > 0)
                        {
                            var adminUser = PolarEnvironment.GetHabboById(Administrators[0].UserId);
                            Founder = adminUser?.Username ?? PolarEnvironment.GetUsernameById(Administrators[0].UserId) ?? "Desconocido";
                        }

                        string SendData = $"{Room.Group.Badge};{Room.Group.Name};{Room.Group.GType};{Room.Group.GroupType};" +
                                          $"{(Room.Group.IsAdmin(Client.GetHabbo()?.Id ?? 0) ? "True" : "False")};" +
                                          $"{Room.Group.IsMember(Client.GetHabbo()?.Id ?? 0)};{Room.Group.HasRequest(Client.GetHabbo()?.Id ?? 0)};";
                        Socket.SendWS( "compose_group|open|" + SendData);
                        Client.GetRoleplay().GroupRoom = true;
                    }
                    break;
                #endregion

                #region Close
                case "close":
                    {
                        Client.GetRoleplay().GroupRoom = false;
                        Socket.SendWS( "compose_group|close|");
                        break;
                    }
                #endregion

                #region Send
                case "send":
                    {
                        if (Room?.Group == null)
                            return;

                        var habbo = Client.GetHabbo();
                        if (habbo == null)
                            return;

                        if (Client.GetRoleplay().TryGetCooldown("groupinfo"))
                            return;

                        if (Client.GetRoleplay().DrivingCar)
                        {
                            Client.SendWhisper("No puedes hacer eso mientras conduces.", 1);
                            return;
                        }

                        Client.GetRoleplay().CooldownManager.CreateCooldown("groupinfo", 1000, 5);

                        string Founder = PolarEnvironment.GetUsernameById(Room.Group.CreatorId) ?? "Desconocido";
                        bool isOwner = Room.Group.IsAdmin(habbo.Id);
                        bool isMember = Room.Group.IsMember(habbo.Id);

                        // Si no es dueño del Grupo
                        if (!isOwner)
                        {
                            #region If is Job (GType < 3)
                            if (Room.Group.GType < 3)
                            {
                                List<Group> Jobs = PolarEnvironment.GetGame().GetGroupManager().GetJobsForUserDict(habbo.Id);

                                if (Jobs == null)
                                {
                                    Client.SendWhisper("((Ha ocurrido un Error al Obtener información de tus Trabajos. Contacte con un Administrador. [3]))", 1);
                                    return;
                                }

                                if (Room.Group.Name.Contains("Policía"))
                                {
                                    List<Group> Groups = PolarEnvironment.GetGame().GetGroupManager().GetGangsForUser(habbo.Id);
                                    if (Groups?.Count > 0)
                                    {
                                        Client.SendWhisper("¡Eres miembro de una banda! No puedes ser contratad@ como policía.", 1);
                                        return;
                                    }
                                }

                                // Si no es Miembro
                                if (!isMember)
                                {
                                    if (Room.Group.GroupType == GroupType.OPEN)
                                    {
                                        #region Special Levels Requirement
                                        if (Room.Group.Name.Contains("Armas") && Client.GetRoleplay().Level < 3)
                                        {
                                            Client.SendWhisper("((Necesitas al menos nivel 3 para ser Fabricante de Armas))", 1);
                                            return;
                                        }
                                        if (Room.Group.Name.Contains("Hospital") && Client.GetRoleplay().Level < 2)
                                        {
                                            Client.SendWhisper("((Necesitas al menos nivel 2 para ser Médico))", 1);
                                            return;
                                        }
                                        if (Room.Group.Name.Contains("Policia") && Client.GetRoleplay().Level < 15)
                                        {
                                            Client.SendWhisper("((Necesitas al menos nivel 15 para ser Policia))", 1);
                                            return;
                                        }
                                        #endregion

                                        #region IsWorking
                                        if (Client.GetRoleplay().IsWorking)
                                        {
                                            WorkManager.RemoveWorkerFromList(Client);
                                            Client.GetRoleplay().IsWorking = false;
                                            habbo.Poof();
                                        }
                                        #endregion

                                        GroupRank Rank = GroupManager.GetJobRank(Room.Group.Id, 1);
                                        if (Rank == null)
                                            return;

                                        #region Extra Cost
                                        if (Room.Group.GType == 2 && Rank.Pay > 0)
                                        {
                                            if (habbo.Credits < Rank.Pay)
                                            {
                                                Client.SendWhisper($"Necesitas $ {Rank.Pay} de cooperación para poder unirte a este trabajo.");
                                                return;
                                            }
                                        }
                                        #endregion

                                        #region DirectJoin
                                        /*if (Room.Group.HasChat)
                                        {
                                            var clientForChat = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(habbo.Id);
                                            if (clientForChat != null)
                                            {
                                                clientForChat.SendMessage(new FriendListUpdateComposer(Room.Group, 0));
                                            }
                                        }*/

                                        // Actualizamos Información del Rank del User
                                        Client.GetRoleplay().TimeWorked = 0;
                                        Client.GetRoleplay().JobId = Room.Group.Id;
                                        Client.GetRoleplay().JobRank = 1;
                                        Client.GetRoleplay().JobRequest = 0;

                                        Room.Group.AddNewMember(habbo.Id);
                                        Room.Group.UpdateJobMember(habbo.Id);
                                        Room.Group.SendPackets(Client);
                                        #endregion

                                        RoleplayManager.Shout(Client, $"*Ha conseguido el Trabajo de {Room.Group.Name}*", 5);
                                        Client.SendWhisper("¡Muy bien! Ahora tienes comandos nuevos para tu nuevo trabajo. Usa :ayuda para consultarlos.", 1);

                                        if (Room.Group.GType == 2 && Rank.Pay > 0)
                                        {
                                            habbo.Credits -= Rank.Pay;
                                            habbo.UpdateCreditsBalance();
                                            Client.SendWhisper($"Has pagado $ {Rank.Pay} de cooperación para el trabajo.", 1);
                                        }
                                    }
                                    else if (Room.Group.GroupType == GroupType.LOCKED)
                                    {
                                        if (Room.Group.HasRequest(habbo.Id))
                                        {
                                            Client.SendWhisper("¡Ya has mandado una Solicitud! Por favor espera a que sea respondida.", 1);
                                            return;
                                        }

                                        GroupRank Rank = GroupManager.GetJobRank(Room.Group.Id, 1);
                                        if (Rank == null)
                                            return;

                                        string SendDatas = $"{Room.Group.Name};{Room.Group.Badge};{Founder};{Rank.Name};{Rank.Pay:C};0 minutos;{Room.Group.GType};";
                                        Socket.SendWS( "compose_group|solicitud|" + SendDatas);
                                        Client.GetRoleplay().GroupRoom = true;
                                        return;
                                    }
                                }
                                // Dejar Grupo (Ya es miembro)
                                else
                                {
                                    #region IsWorking
                                    if (Client.GetRoleplay().IsWorking)
                                    {
                                        WorkManager.RemoveWorkerFromList(Client);
                                        Client.GetRoleplay().IsWorking = false;
                                        habbo.Poof();
                                    }
                                    #endregion

                                    string extraInfo = "";
                                    int userId = habbo.Id;

                                    if (Jobs.Count > 0 && Jobs[0].IsAdmin(userId))
                                    {
                                        extraInfo = $"Se te ha retirado el Cargo Fundador en {Jobs[0].Name}";
                                    }
                                    else
                                    {
                                        extraInfo = $"Se te ha retirado el trabajo de {Jobs[0]?.Name ?? "trabajo"}";
                                    }

                                    if (Jobs.Count > 0 && Jobs[0].IsMember(userId))
                                    {
                                        Client.GetRoleplay().TimeWorked = 0;
                                        Client.GetRoleplay().JobId = 1;
                                        Client.GetRoleplay().JobRank = 1;
                                        Client.GetRoleplay().JobRequest = 0;

                                        var defaultJob = GroupManager.GetJob(1);
                                        if (defaultJob != null)
                                        {
                                            defaultJob.AddNewMember(habbo.Id);
                                            defaultJob.SendPackets(Client);
                                        }
                                    }

                                    if (Jobs.Count > 0 && Jobs[0].IsAdmin(userId))
                                    {
                                        Jobs[0].TakeAdmin(userId);
                                    }

                                    RoleplayManager.Shout(Client, $"*Ha renunciado a su trabajo de {Room.Group.Name}*", 5);
                                    Client.SendWhisper(extraInfo, 1);
                                }
                                RoleplayManager.CheckCorpCarp(Client);
                            }
                            #endregion

                            #region If is Gang (GType >= 3)
                            else
                            {
                                List<Group> Gangs = PolarEnvironment.GetGame().GetGroupManager().GetGangsForUser(habbo.Id);

                                if (Gangs == null)
                                {
                                    Client.SendWhisper("((Ha ocurrido un Error al Obtener información de tus bandas. Contacte con un Administrador. [3]))", 1);
                                    return;
                                }

                                // Si no es Miembro
                                if (!isMember)
                                {
                                    if (GroupManager.HasJobCommand(Client, "law"))
                                    {
                                        Client.SendWhisper("¡No puedes pertenecer a una banda y ser policía a la vez!", 1);
                                        return;
                                    }

                                    if (Room.Group.GroupType == GroupType.OPEN)
                                    {
                                        //Client.SendWhisper("¡Muy bien! Ahora perteneces a una nueva banda. ((Da clic en su emblema para ver más info.))", 1);
                                        if (Gangs.Count <= 0)
                                        {
                                            #region DirectJoin
                                            /*if (Room?.Group?.HasChat == true)
                                            {
                                                var clientForChat = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(habbo.Id);
                                                if (clientForChat != null)
                                                {
                                                    clientForChat.SendMessage(new FriendListUpdateComposer(Room.Group, 0));
                                                }
                                            }*/

                                            Client.GetRoleplay().GangId = Room.Group.Id;
                                            Client.GetRoleplay().GangRank = 1;
                                            Client.GetRoleplay().GangRequest = 0;
                                            Room.Group.AddNewMember(habbo.Id);
                                            Room.Group.SendPackets(Client);
                                            #endregion

                                            RoleplayManager.Shout(Client, $"*Ha ingresado a la banda {Room.Group.Name}*", 5);
                                            Client.SendWhisper("¡Muy bien! Ahora perteneces a una nueva banda. ((Da clic en su emblema para ver más info.))", 1);
                                        }
                                    }
                                    else if (Room.Group.GroupType == GroupType.LOCKED)
                                    {
                                        if (Room.Group.HasRequest(habbo.Id))
                                        {
                                            Client.SendWhisper("¡Ya has mandado una Solicitud! Por favor espera a que sea respondida.", 1);
                                            return;
                                        }

                                        RoleplayManager.Shout(Client, $"*Ha solicitado ingresar a la banda {Room.Group.Name}*", 5);
                                        Client.SendMessage(new RoomNotificationComposer("gang_request_warning", "message",
                                            "¡Bien Hecho!\nAhora debes esperar a que aprueben tu solicitud.\n\n" +
                                            "Toma en cuenta que si te encuentras en otra banda; el Líder de la nueva banda " +
                                            "no podrá aceptarte hasta que abandones dicha banda anterior."));

                                        Client.GetRoleplay().GangRequest = Room.Group.Id;
                                        Room.Group.Requests.Add(habbo.Id);

                                        using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                                        {
                                            dbClient.SetQuery("UPDATE `rp_stats` SET `gang_request` = @gangId WHERE `id` = @userId LIMIT 1");
                                            dbClient.AddParameter("gangId", Room.Group.Id);
                                            dbClient.AddParameter("userId", habbo.Id);
                                            dbClient.RunQuery();
                                        }
                                    }
                                }
                                // Dejar Grupo (Ya es miembro)
                                else
                                {
                                    int userId = habbo.Id;
                                    if (Gangs.Count > 0 && Gangs[0].IsAdmin(userId))
                                    {
                                        Client.SendWhisper("No puedes abandonar tu propia banda sin dejar a alguien al mando. " +
                                                          "O bien, puedes eliminarla desde tu panel de gestión.", 1);
                                        return;
                                    }

                                    Client.GetRoleplay().GangId = 0;
                                    Client.GetRoleplay().GangRank = 0;
                                    Client.GetRoleplay().GangRequest = 0;

                                    if (Gangs.Count > 0)
                                    {
                                        Gangs[0].DeleteMember(userId);
                                        if (Gangs[0].IsAdmin(userId))
                                            Gangs[0].TakeAdmin(userId);
                                    }

                                    RoleplayManager.Shout(Client, $"*Ha abandonado la banda {Room.Group.Name}*", 5);
                                    Client.SendWhisper("Has abandonado tu banda", 1);

                                    string SendDatax = $"{Room.Group.Badge};{Room.Group.Name};{Room.Group.GType};{Room.Group.GroupType};False;False;False;";
                                    Socket.SendWS( "compose_group|open|" + SendDatax);
                                    Client.GetRoleplay().GroupRoom = true;
                                    return;
                                }
                            }
                            #endregion
                        }
                        else
                        {
                            // Gestionar (Es dueño)
                            Client.GetRoleplay().ViewMyCorp = true;
                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_business", "open_room");
                        }

                        // Actualizar interfaz
                        string SendData = $"{Room.Group.Badge};{Room.Group.Name};{Room.Group.GType};{Room.Group.GroupType};" +
                                          $"{(Room.Group.IsAdmin(habbo.Id) ? "True" : "False")};{Room.Group.IsMember(habbo.Id)};{Room.Group.HasRequest(habbo.Id)};";
                        Socket.SendWS( "compose_group|open|" + SendData);
                        Client.GetRoleplay().GroupRoom = true;
                    }
                    break;
                #endregion

                #region Request
                case "request":
                    {
                        if (Room?.Group == null)
                            return;

                        var habbo = Client.GetHabbo();
                        if (habbo == null)
                            return;

                        if (Client.GetRoleplay().TryGetCooldown("grouprequest"))
                            return;

                        Client.GetRoleplay().CooldownManager.CreateCooldown("grouprequest", 1000, 5);

                        if (Room.Group.HasRequest(habbo.Id))
                        {
                            Socket.SendWS( "compose_group|error|¡Ya has mandado una solicitud!");
                            return;
                        }

                        if (Room.Group.Name.Contains("Policia"))
                        {
                            List<Group> Groups = PolarEnvironment.GetGame().GetGroupManager().GetGangsForUser(habbo.Id);
                            if (Groups?.Count > 0)
                            {
                                Socket.SendWS( "compose_group|error|¡Eres miembro de una banda! No puedes ser contratad@ como policía.");
                                return;
                            }
                        }

                        string[] ReceivedData = Data.Split(',');
                        if (ReceivedData.Length < 4)
                        {
                            Socket.SendWS( "compose_group|error|Datos incompletos.");
                            return;
                        }

                        if (!int.TryParse(ReceivedData[1], out int Hours))
                        {
                            Socket.SendWS( "compose_group|error|Debe ingresar una hora válida.");
                            return;
                        }

                        string Desc = ReceivedData.Length > 2 ? ReceivedData[2] : "";
                        string Region = ReceivedData.Length > 3 ? ReceivedData[3] : "";

                        if (Desc.Length > 250)
                        {
                            Socket.SendWS( "compose_group|error|La explicación no debe exceder los 250 Caracteres.");
                            return;
                        }

                        if (Region.Length > 10)
                        {
                            Socket.SendWS( "compose_group|error|País Erróneo.");
                            return;
                        }

                        // Filter
                        Desc = Regex.Replace(Desc, "<(.|\\n)*?>", string.Empty);
                        Region = Regex.Replace(Region, "<(.|\\n)*?>", string.Empty);

                        RoleplayManager.Shout(Client, $"*Ha enviado una solicitud de Empleo a {Room.Group.Name}*", 5);
                        Client.SendMessage(new RoomNotificationComposer("job_request_warning", "message",
                            "¡Bien Hecho!\nAhora debes esperar a que aprueben tu solicitud.\n\n" +
                            "Toma en cuenta que si te encuentras en otro trabajo que también requirió " +
                            "solicitud de empleo; el Fundador del nuevo trabajo no podrá aceptarte " +
                            "hasta que renuncies a dicho trabajo anterior."));

                        Client.GetRoleplay().JobRequest = Room.Group.Id;
                        Room.Group.Requests.Add(habbo.Id);

                        using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                        {
                            dbClient.SetQuery("UPDATE `rp_stats` SET `job_request` = @jobId WHERE `id` = @userId LIMIT 1");
                            dbClient.AddParameter("jobId", Room.Group.Id);
                            dbClient.AddParameter("userId", habbo.Id);
                            dbClient.RunQuery();

                            // Insertar en la tabla de solicitudes de trabajo
                            dbClient.SetQuery("INSERT INTO `rp_jobs_requests` (job_id, user_id, ws_desc, ws_hours, ws_region) VALUES (@jobId, @userId, @desc, @hours, @region)");
                            dbClient.AddParameter("jobId", Room.Group.Id);
                            dbClient.AddParameter("userId", habbo.Id);
                            dbClient.AddParameter("desc", Desc);
                            dbClient.AddParameter("hours", Hours);
                            dbClient.AddParameter("region", Region);
                            dbClient.RunQuery();
                        }

                        Socket.SendWS( "compose_group|close_rq|");
                        Socket.SendWS( "compose_group|close");
                        Socket.SendWS( "compose_group|open");
                    }
                    break;
                    #endregion
            }
        }

        // ── Helper: envía texto como frame WebSocket usando ConnectionInformation
        private static void SendWS(ConnectionInformation socket, string message)
        {
            if (socket == null || string.IsNullOrEmpty(message)) return;
            socket.SendData(System.Text.Encoding.UTF8.GetBytes(message));
        }

    }
}