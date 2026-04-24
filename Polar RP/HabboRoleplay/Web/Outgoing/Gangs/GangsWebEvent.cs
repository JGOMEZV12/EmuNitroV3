using ConnectionManager;
﻿using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Data;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.Groups;
using Polar.Communication.Packets.Outgoing.Groups;
using Polar.Communication.Packets.Outgoing.Messenger;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Users;
using Polar.HabboRoleplay.Turfs;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using Polar.Net;
// Agregar un alias para evitar la ambigüedad
using Group = Polar.HabboHotel.Groups.Group;

namespace Polar.HabboHotel.Roleplay.Web.Outgoing.Misc
{
    /// <summary>
    /// GangsWebEvent class.
    /// </summary>
    class GangsWebEvent : IWebEvent
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

            string Action = (Data.Contains(',') ? Data.Split(',')[0] : Data);

            switch (Action)
            {
                #region Open
                case "open":
                    {
                        var habbo = Client?.GetHabbo();
                        if (habbo == null)
                            return;

                        // Gang List & Stats
                        var htmlBuilder = new StringBuilder();
                        var tabsBuilder = new StringBuilder();
                        string hasGang = "False";

                        List<Group> groups = PolarEnvironment.GetGame().GetGroupManager().GetGangsForUser(habbo.Id);
                        if (groups != null && groups.Count > 0)
                            hasGang = "True";

                        string buttonText = $"Crear banda ($ {string.Format("{0:N0}", RoleplayManager.GangsPrice)})";

                        #region HTML
                        htmlBuilder.Append("<div class=\"heading\">Lista de Bandas</div>");
                        htmlBuilder.Append($"<input id=\"GA_Search\" type=\"text\" data-lpignore=\"true\" class=\"dark-input-text flex-1 mr-1\" value=\"\" maxlength=\"50\" autocomplete=\"off\" placeholder=\"Buscar bandas por nombre\" style=\"width: 628px\">");
                        htmlBuilder.Append("<br><br>");
                        htmlBuilder.Append("<div id=\"GA_List\" class=\"-m-1 flex flex-wrap\">");

                        foreach (Group group in PolarEnvironment.GetGame().GetGroupManager().GangsG.ToList())
                        {
                            if (group.Id == 1000)
                                continue;

                            htmlBuilder.Append("<div class=\"flex bg-dark-1 m-1 rounded-lg p-1 pl-2 items-center\" style=\"width: 45.5%;\">");
                            htmlBuilder.Append("<div class=\"mr-2\">");
                            htmlBuilder.Append($"<p>{group.Name}</p>");
                            htmlBuilder.Append($"<p>{group.Members.Count()} miembro(s)</p>");
                            htmlBuilder.Append("</div>");
                            htmlBuilder.Append($"<div class=\"bg-dark-2 px-2 py-1 rounded-lg ml-auto data-gang\" data-balloon=\"Ver info\" data-balloon-pos=\"left\" data-gang=\"{group.Id}\">");
                            htmlBuilder.Append($"<img src=\"{RoleplayManager.HotelUrl}/group-badge/badge/{group.GetBadge()}\" draggable=\"false\" ondragstart=\"return false;\" style=\"cursor: pointer;\">");
                            htmlBuilder.Append("</div>");
                            htmlBuilder.Append("</div>");
                        }

                        htmlBuilder.Append("</div>");
                        #endregion

                        #region TABS
                        if (hasGang == "True" && groups != null && groups.Count > 0)
                        {
                            tabsBuilder.Append("<div id=\"GA_My_Members\" class=\"Tabbed_tab_1apzZ GA_My_Members Tabbed_selected_3aJyT\" style=\"min-width: 75px;\">Miembros</div>");

                            if (groups[0].IsAdmin(habbo.Id) || (habbo.GetPermissions()?.HasRight("group_management_override") == true))
                            {
                                tabsBuilder.Append("<div id=\"GA_My_Ranks\" class=\"Tabbed_tab_1apzZ GA_My_Ranks\" style=\"min-width: 75px;\">Rangos</div>");
                            }

                            if ((groups[0].IsAdmin(habbo.Id) || groups[0].IsMember(habbo.Id)) ||
                                (habbo.GetPermissions()?.HasRight("corporation_rights") == true))
                            {
                                tabsBuilder.Append("<div id=\"GA_My_Requests\" class=\"Tabbed_tab_1apzZ GA_My_Requests\" style=\"min-width: 75px;\">Solicitudes</div>");
                            }

                            tabsBuilder.Append("<div id=\"GA_My_Stats\" class=\"Tabbed_tab_1apzZ GA_My_Stats\" style=\"min-width: 75px;\">Estad&iacute;sticas</div>");

                            if (groups[0].IsAdmin(habbo.Id) || (habbo.GetPermissions()?.HasRight("group_management_override") == true))
                            {
                                tabsBuilder.Append("<div id=\"GA_My_Edit\" class=\"Tabbed_tab_1apzZ GA_My_Edit\" style=\"min-width: 75px;\">Editar</div>");
                            }
                        }
                        #endregion

                        Socket.SendWS( $"compose_gang|open|{htmlBuilder.ToString()}|{hasGang}|{buttonText}|{tabsBuilder.ToString()}");
                    }
                    break;
                #endregion

                #region Close
                case "close":
                    {
                        Socket.SendWS( "compose_gang|close");
                    }
                    break;
                #endregion

                #region My / New
                case "mynew":
                    {
                        var habbo = Client?.GetHabbo();
                        if (habbo == null)
                            return;

                        List<Group> groups = PolarEnvironment.GetGame().GetGroupManager().GetGangsForUser(habbo.Id);
                        bool hasGang = groups != null && groups.Count > 0;

                        if (!hasGang)
                        {
                            Socket.SendWS( "compose_gang|new_gang|");
                        }
                        else
                        {
                            bool isAdmin = groups[0].IsAdmin(habbo.Id) ||
                                         (habbo.GetPermissions()?.HasRight("group_management_override") == true);
                            var htmlBuilder = new StringBuilder();
                            var tabsBuilder = new StringBuilder();

                            #region HTML
                            htmlBuilder.Append("<div>");
                            htmlBuilder.Append("<div class=\"-m-2\">");

                            var allRanks = groups[0].Ranks.OrderByDescending(o => o.Value.RankId).ToList();

                            foreach (var rank in allRanks)
                            {
                                htmlBuilder.Append("<div class=\"m-2\">");
                                htmlBuilder.Append("<div class=\"heading relative group\">");
                                htmlBuilder.Append($"<div>{rank.Value.Name}</div>");

                                if (isAdmin)
                                {
                                    htmlBuilder.Append("<div class=\"absolute pin-t pin-r mr-1 h-full hidden group-hover:block\">");
                                    htmlBuilder.Append("<div class=\"flex items-center h-full\">");
                                    htmlBuilder.Append($"<div data-rank=\"{rank.Value.RankId}\" data-action=\"settings\" class=\"cursor-pointer-r px-1\">");
                                    htmlBuilder.Append($"<img src=\"{RoleplayManager.CdnURL}/ws_resources/images/settings.png\">");
                                    htmlBuilder.Append("</div>");
                                    htmlBuilder.Append("</div>");
                                    htmlBuilder.Append("</div>");
                                }

                                htmlBuilder.Append("</div>");
                                htmlBuilder.Append("<div class=\"flex flex-wrap -m-1 justify-center\">");

                                foreach (var member in groups[0].GetAllMembersDict.Where(m => m.Value.UserRank == rank.Value.RankId))
                                {
                                    string name = PolarEnvironment.GetGame().GetClientManager().GetNameById(Convert.ToInt32(member.Value.UserId)) ?? "Desconocido";
                                    string look = PolarEnvironment.GetGame().GetClientManager().GetLookById(Convert.ToInt32(member.Value.UserId)) ?? "";

                                    htmlBuilder.Append("<div class=\"bg-dark-4 rounded m-1 group cursor-pointer-r\">");
                                    htmlBuilder.Append("<div class=\"m-px relative\">");
                                    htmlBuilder.Append("<div class=\"overflow-hidden bg-light-05 rounded-t\" style=\"height: 55px;\">");
                                    htmlBuilder.Append($"<center><div class=\"figure-H_RWF_0\" style=\"background-image: url(&quot;{RoleplayManager.AVATARIMG}{look}&quot;); width: 64px; height: 110px; margin-top: -20px;\"></div></center>");
                                    htmlBuilder.Append("</div>");

                                    bool isMember = groups[0].IsMember(habbo.Id);
                                    bool canAscDesc = GroupManager.HasJobCommand(Client, "ascdesc");
                                    bool canFire = GroupManager.HasJobCommand(Client, "fire");
                                    bool itsMe = member.Value.UserId == habbo.Id;
                                    bool itsSup = member.Value.UserRank >= (groups[0].Members.ContainsKey(habbo.Id) ? groups[0].Members[habbo.Id]?.UserRank ?? 0 : 0);

                                    if ((isAdmin || (isMember && (canAscDesc || canFire) && !itsSup)) ||
                                        (habbo.GetPermissions()?.HasRight("corporation_rights") == true))
                                    {
                                        htmlBuilder.Append("<div class=\"absolute pin-b bg-dark-5 w-full hidden group-hover:block\" style=\"padding-top: 4px;padding-bottom: 4px;\">");
                                        htmlBuilder.Append("<div class=\"flex justify-around\">");
                                        htmlBuilder.Append($"<div data-user=\"{member.Value.UserId}\" data-action=\"up\" class=\"cursor-pointer-r px-1\">");
                                        htmlBuilder.Append($"<img src=\"{RoleplayManager.CdnURL}/ws_resources/images/up-arrow.png\">");
                                        htmlBuilder.Append("</div>");
                                        htmlBuilder.Append($"<div data-user=\"{member.Value.UserId}\" data-action=\"down\" class=\"cursor-pointer-r px-1\">");
                                        htmlBuilder.Append($"<img src=\"{RoleplayManager.CdnURL}/ws_resources/images/down-arrow.png\">");
                                        htmlBuilder.Append("</div>");

                                        if (isAdmin || canFire || (habbo.GetPermissions()?.HasRight("corporation_rights") == true))
                                        {
                                            htmlBuilder.Append($"<div data-user=\"{member.Value.UserId}\" data-action=\"cross\" class=\"cursor-pointer-r px-1\">");
                                            htmlBuilder.Append($"<img src=\"{RoleplayManager.CdnURL}/ws_resources/images/cross.png\">");
                                            htmlBuilder.Append("</div>");
                                        }

                                        htmlBuilder.Append("</div>");
                                        htmlBuilder.Append("</div>");
                                    }

                                    htmlBuilder.Append("</div>");
                                    htmlBuilder.Append($"<div class=\"text-center py-1\">{name}</div>");
                                    htmlBuilder.Append("</div>");
                                }

                                htmlBuilder.Append("</div>");
                                htmlBuilder.Append("</div>");
                            }

                            htmlBuilder.Append("</div>");
                            htmlBuilder.Append("</div>");
                            #endregion

                            #region TABS
                            tabsBuilder.Append("<div id=\"GA_My_Members\" class=\"Tabbed_tab_1apzZ GA_My_Members Tabbed_selected_3aJyT\" style=\"min-width: 75px;\">Miembros</div>");

                            if ((habbo.GetPermissions()?.HasRight("corporation_rights") == true) ||
                                (habbo.GetPermissions()?.HasRight("group_management_override") == true))
                            {
                                tabsBuilder.Append("<div id=\"GA_My_Ranks\" class=\"Tabbed_tab_1apzZ GA_My_Ranks\" style=\"min-width: 75px;\">Rangos</div>");
                                tabsBuilder.Append("<div id=\"GA_My_Requests\" class=\"Tabbed_tab_1apzZ GA_My_Requests\" style=\"min-width: 75px;\">Solicitudes</div>");
                            }
                            else
                            {
                                if (groups[0].IsAdmin(habbo.Id))
                                {
                                    tabsBuilder.Append("<div id=\"GA_My_Ranks\" class=\"Tabbed_tab_1apzZ GA_My_Ranks\" style=\"min-width: 75px;\">Rangos</div>");
                                }

                                if (groups[0].IsAdmin(habbo.Id) || groups[0].IsMember(habbo.Id))
                                {
                                    tabsBuilder.Append("<div id=\"GA_My_Requests\" class=\"Tabbed_tab_1apzZ GA_My_Requests\" style=\"min-width: 75px;\">Solicitudes</div>");
                                }
                            }

                            tabsBuilder.Append("<div id=\"GA_My_Stats\" class=\"Tabbed_tab_1apzZ GA_My_Stats\" style=\"min-width: 75px;\">Estad&iacute;sticas</div>");

                            if (groups[0].IsAdmin(habbo.Id) || (habbo.GetPermissions()?.HasRight("group_management_override") == true))
                            {
                                tabsBuilder.Append("<div id=\"GA_My_Edit\" class=\"Tabbed_tab_1apzZ GA_My_Edit\" style=\"min-width: 75px;\">Editar</div>");
                            }
                            #endregion

                            Socket.SendWS( $"compose_gang|my_gang|{htmlBuilder.ToString()}|{tabsBuilder.ToString()}");
                        }
                    }
                    break;
                #endregion

                #region Create
                case "create":
                    {
                        #region Conditions
                        if (Client.GetRoleplay().TryGetCooldown("ga_create", true))
                            return;

                        var habbo = Client?.GetHabbo();
                        if (habbo == null)
                            return;

                        #region Principal Conditions
                        if (Client.GetRoleplay().IsWorking)
                        {
                            Client.SendWhisper("¡No puedes hacer eso mientras estás trabajando!", 1);
                            return;
                        }
                        if (Client.GetRoleplay().IsDead)
                        {
                            Client.SendWhisper("¡No puedes hacer eso mientras estás muert@!", 1);
                            return;
                        }
                        if (Client.GetRoleplay().IsJailed)
                        {
                            Client.SendWhisper("¡No puedes hacer eso mientras estás encarcelad@!", 1);
                            return;
                        }
                        if (Client.GetRoleplay().Level < 2)
                        {
                            Socket.SendWS( "compose_gang|msg_error|¡Necesitas al menos Nivel 2 para pertenecer a una banda!");
                            return;
                        }
                        if (GroupManager.HasJobCommand(Client, "law"))
                        {
                            Socket.SendWS( "compose_gang|msg_error|¡No puedes pertenecer a una banda y ser policía a la vez!");
                            return;
                        }
                        #endregion

                        List<Group> groups = PolarEnvironment.GetGame().GetGroupManager().GetGangsForUser(habbo.Id);
                        if (groups?.Count > 0)
                            return;

                        string[] receivedData = Data.Split(',');
                        if (receivedData.Length < 6)
                            return;

                        string gangName = receivedData[1];
                        string color1 = receivedData[3];
                        string color2 = receivedData[4];
                        string badge = receivedData[5];

                        // Filtramos por seguridad
                        gangName = Regex.Replace(gangName, "<(.|\\n)*?>", string.Empty);
                        color1 = Regex.Replace(color1, "<(.|\\n)*?>", string.Empty);
                        color2 = Regex.Replace(color2, "<(.|\\n)*?>", string.Empty);
                        badge = Regex.Replace(badge, "<(.|\\n)*?>", string.Empty);

                        if (string.IsNullOrEmpty(gangName) || gangName.Length < 3)
                        {
                            Socket.SendWS( "compose_gang|msg_error|El nombre de tu banda debe tener al menos 3 caracteres.");
                            return;
                        }
                        if (gangName.Length > 11)
                        {
                            Socket.SendWS( "compose_gang|msg_error|El nombre no debe ser mayor a 11 caracteres.");
                            return;
                        }
                        if (!Regex.IsMatch(gangName, @"^[a-zA-Z0-9]+$"))
                        {
                            Socket.SendWS( "compose_gang|msg_error|¡No se aceptan caracteres especiales! Solo números y letras.");
                            return;
                        }

                        if (!int.TryParse(receivedData[2], out int accessType))
                        {
                            Socket.SendWS( "compose_gang|msg_error|Ha ocurrido un problema al obtener la Información del tipo de acceso de la banda.");
                            return;
                        }

                        if (habbo.Credits < RoleplayManager.GangsPrice)
                        {
                            Socket.SendWS( $"compose_gang|msg_error|No tienes $ {string.Format("{0:N0}", RoleplayManager.GangsPrice)} para crear una banda.");
                            return;
                        }
                        #endregion

                        #region Execute
                        if (!PolarEnvironment.GetGame().GetGroupManager().TryCreateGroup(habbo, gangName, "", 0, badge, color1, color2, out Group group))
                        {
                            Socket.SendWS( "compose_gang|msg_error|Ocurrió un problema al intentar crear la banda. Contacta con un Administrador.");
                            return;
                        }

                        habbo.Credits -= RoleplayManager.GangsPrice;
                        habbo.UpdateCreditsBalance();

                        RoleplayManager.Shout(Client, $"*Ha declarado la creación de una nueva banda llamada {group.Name}*", 5);
                        Socket.SendWS( "compose_gang|msg_success|Banda creada exitosamente. Ahora podrás gestionarla desde aquí.");
                        PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_gang", "mynew");
                        Client.GetRoleplay().CooldownManager.CreateCooldown("ga_create", 1000, 10);
                        #endregion
                    }
                    break;
                #endregion

                #region Rank Tools
                case "rank_tools":
                    {
                        var habbo = Client?.GetHabbo();
                        if (habbo == null)
                            return;

                        List<Group> groups = PolarEnvironment.GetGame().GetGroupManager().GetGangsForUser(habbo.Id);
                        if (groups == null || groups.Count <= 0)
                            return;

                        #region Check if is not Admin
                        if (!groups[0].IsAdmin(habbo.Id) && !(habbo.GetPermissions()?.HasRight("group_management_override") == true))
                            return;
                        #endregion

                        string[] receivedData = Data.Split(',');
                        if (receivedData.Length < 3)
                            return;

                        if (!int.TryParse(receivedData[1], out int getRank))
                            return;

                        string wsAction = receivedData[2];
                        var allRanks = groups[0].Ranks.ToList();
                        var allMembers = groups[0].GetAllMembersDict;

                        // Validamos si existe el rango
                        if (!allRanks.Any(x => x.Value.RankId == getRank))
                            return;

                        switch (wsAction)
                        {
                            case "up":
                                {
                                    int newRank = getRank + 1;

                                    // Validamos si existe un rango superior. 
                                    if (!allRanks.Any(x => x.Value.RankId == newRank))
                                        return;

                                    using (var db = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                                    {
                                        db.RunQuery($"CALL `ModifRank`({getRank}, {newRank}, {groups[0].Id});");

                                        // Actualizar en memoria
                                        foreach (var r in allRanks.Where(x => x.Value.RankId == newRank))
                                            r.Value.RankId = 0;
                                        foreach (var r in allRanks.Where(x => x.Value.RankId == getRank))
                                            r.Value.RankId = newRank;
                                        foreach (var r in allRanks.Where(x => x.Value.RankId == 0))
                                            r.Value.RankId = getRank;

                                        // Actualizar miembros
                                        foreach (var m in allMembers.Where(x => x.Value.UserRank == newRank))
                                            m.Value.UserRank = 0;
                                        foreach (var m in allMembers.Where(x => x.Value.UserRank == getRank))
                                            m.Value.UserRank = newRank;
                                        foreach (var m in allMembers.Where(x => x.Value.UserRank == 0))
                                            m.Value.UserRank = getRank;
                                    }

                                    PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_gang", "mynew");
                                }
                                break;

                            case "down":
                                {
                                    int newRank = getRank - 1;

                                    // Validamos si existe un rango inferior. 
                                    if (!allRanks.Any(x => x.Value.RankId == newRank))
                                        return;

                                    using (var db = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                                    {
                                        db.RunQuery($"CALL `ModifRank`({getRank}, {newRank}, {groups[0].Id});");

                                        // Actualizar en memoria
                                        foreach (var r in allRanks.Where(x => x.Value.RankId == newRank))
                                            r.Value.RankId = 0;
                                        foreach (var r in allRanks.Where(x => x.Value.RankId == getRank))
                                            r.Value.RankId = newRank;
                                        foreach (var r in allRanks.Where(x => x.Value.RankId == 0))
                                            r.Value.RankId = getRank;

                                        // Actualizar miembros
                                        foreach (var m in allMembers.Where(x => x.Value.UserRank == newRank))
                                            m.Value.UserRank = 0;
                                        foreach (var m in allMembers.Where(x => x.Value.UserRank == getRank))
                                            m.Value.UserRank = newRank;
                                        foreach (var m in allMembers.Where(x => x.Value.UserRank == 0))
                                            m.Value.UserRank = getRank;
                                    }

                                    PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_gang", "mynew");
                                }
                                break;

                            case "cross":
                                {
                                    // Si tiene un solo rango
                                    if (allRanks.Count <= 1)
                                        return;

                                    // Si hay miembros en ese rango, los cambiamos al rankid = 1
                                    foreach (var member in allMembers.Where(x => x.Value.UserRank == getRank))
                                    {
                                        member.Value.UserRank = 1;
                                    }

                                    // Borramos de DB
                                    using (var db = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                                    {
                                        db.RunQuery($"CALL `DropRank`({getRank}, {groups[0].Id});");
                                    }

                                    // Borramos del Diccionario
                                    PolarEnvironment.GetGame().GetGroupManager().DeleteGroupRank(groups[0].Id, getRank);

                                    // Recalcular rangos superiores
                                    int uprank = getRank + 1;
                                    foreach (var r in allRanks.Where(x => x.Value.RankId >= uprank))
                                        r.Value.RankId = r.Value.RankId - 1;

                                    foreach (var m in allMembers.Where(x => x.Value.UserRank >= uprank))
                                        m.Value.UserRank = m.Value.UserRank - 1;

                                    PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_gang", "mynew");
                                }
                                break;

                            case "settings":
                                {
                                    GroupRank thisRank = GroupManager.GetJobRank(groups[0].Id, getRank);
                                    if (thisRank == null)
                                        return;

                                    var htmlBuilder = new StringBuilder();

                                    #region HTML
                                    htmlBuilder.Append("<div class=\"heading\">Nombre del Rango</div>");
                                    htmlBuilder.Append("<div class=\"flex\">");
                                    htmlBuilder.Append($"<input id=\"GangRankNewName\" type=\"text\" data-lpignore=\"true\" class=\"dark-input-text flex-1 mr-1\" value=\"{thisRank.Name}\">");
                                    htmlBuilder.Append("</div>");
                                    htmlBuilder.Append("<br>");
                                    htmlBuilder.Append("<div class=\"flex\">");
                                    htmlBuilder.Append($"<button data-rank=\"{getRank}\" data-action=\"SaveRank\" class=\"dark-button\" style=\"width: 100%;\">Cambiar nombre</button>");
                                    htmlBuilder.Append("</div>");
                                    htmlBuilder.Append("<br>");
                                    htmlBuilder.Append("<div class=\"heading\">Permisos</div>");
                                    htmlBuilder.Append("<div class=\"flex\">");
                                    htmlBuilder.Append("<table class=\"dark\">");
                                    htmlBuilder.Append("<tr class=\"dark2\">");
                                    htmlBuilder.Append("<th class=\"dark2\">Ascender/Descender</th>");
                                    htmlBuilder.Append("<th class=\"dark2\">Reclutar</th>");
                                    htmlBuilder.Append("<th class=\"dark2\">Expulsar</th>");
                                    htmlBuilder.Append("<th class=\"dark2\">Invitar</th>");
                                    htmlBuilder.Append("</tr>");
                                    htmlBuilder.Append("<tr class=\"dark2\">");

                                    // Ascender/Descender
                                    htmlBuilder.Append("<td class=\"dark2\">");
                                    htmlBuilder.Append($"<div data-rank=\"{getRank}\" data-action=\"ascdesc\" class=\"cursor-pointer-r px-1\">");
                                    htmlBuilder.Append($"<img src=\"{RoleplayManager.CdnURL}/ws_resources/images/{(thisRank.HasCommand("ascdesc") ? "up-arrow.png" : "cross.png")}\">");
                                    htmlBuilder.Append("</div>");
                                    htmlBuilder.Append("</td>");

                                    // Reclutar
                                    htmlBuilder.Append("<td class=\"dark2\">");
                                    htmlBuilder.Append($"<div data-rank=\"{getRank}\" data-action=\"hire\" class=\"cursor-pointer-r px-1\">");
                                    htmlBuilder.Append($"<img src=\"{RoleplayManager.CdnURL}/ws_resources/images/{(thisRank.HasCommand("hire") ? "up-arrow.png" : "cross.png")}\">");
                                    htmlBuilder.Append("</div>");
                                    htmlBuilder.Append("</td>");

                                    // Expulsar
                                    htmlBuilder.Append("<td class=\"dark2\">");
                                    htmlBuilder.Append($"<div data-rank=\"{getRank}\" data-action=\"fire\" class=\"cursor-pointer-r px-1\">");
                                    htmlBuilder.Append($"<img src=\"{RoleplayManager.CdnURL}/ws_resources/images/{(thisRank.HasCommand("fire") ? "up-arrow.png" : "cross.png")}\">");
                                    htmlBuilder.Append("</div>");
                                    htmlBuilder.Append("</td>");

                                    // Invitar
                                    htmlBuilder.Append("<td class=\"dark2\">");
                                    htmlBuilder.Append($"<div data-rank=\"{getRank}\" data-action=\"invite\" class=\"cursor-pointer-r px-1\">");
                                    htmlBuilder.Append($"<img src=\"{RoleplayManager.CdnURL}/ws_resources/images/{(thisRank.HasCommand("invite") ? "up-arrow.png" : "cross.png")}\">");
                                    htmlBuilder.Append("</div>");
                                    htmlBuilder.Append("</td>");

                                    htmlBuilder.Append("</tr>");
                                    htmlBuilder.Append("</table>");
                                    htmlBuilder.Append("</div>");
                                    htmlBuilder.Append("<br>");
                                    #endregion

                                    Socket.SendWS( $"compose_gang|ranks|{htmlBuilder.ToString()}|");
                                }
                                break;
                        }
                    }
                    break;
                #endregion

                #region Member Tools
                case "member_tools":
                    {
                        var habbo = Client?.GetHabbo();
                        if (habbo == null)
                            return;

                        List<Group> groups = PolarEnvironment.GetGame().GetGroupManager().GetGangsForUser(habbo.Id);
                        if (groups == null || groups.Count <= 0)
                            return;

                        string[] receivedData = Data.Split(',');
                        if (receivedData.Length < 3)
                            return;

                        if (!int.TryParse(receivedData[1], out int getUserId))
                            return;

                        string wsAction = receivedData[2];

                        bool isAdmin = groups[0].IsAdmin(habbo.Id) ||
                                     groups[0].IsMember(habbo.Id) ||
                                     (habbo.GetPermissions()?.HasRight("group_management_override") == true);

                        // Validamos si existe el usuario para acciones que no sean accept/decline
                        if (wsAction != "accept" && wsAction != "decline" &&
                            !groups[0].IsMember(getUserId) && !groups[0].IsAdmin(getUserId))
                            return;

                        switch (wsAction)
                        {
                            case "up":
                            case "down":
                            case "cross":
                                {
                                    bool isMember = groups[0].IsMember(habbo.Id);
                                    bool canAscDesc = GroupManager.HasJobCommand(Client, "ascdesc");
                                    bool canFire = GroupManager.HasJobCommand(Client, "fire");
                                    bool itsMe = groups[0].Members.ContainsKey(getUserId) &&
                                                groups[0].Members[getUserId]?.UserId == habbo.Id;
                                    bool itsSup = groups[0].Members.ContainsKey(getUserId) &&
                                                groups[0].Members.ContainsKey(habbo.Id) &&
                                                groups[0].Members[getUserId]?.UserRank >= groups[0].Members[habbo.Id]?.UserRank;

                                    // Check permissions based on action
                                    switch (wsAction)
                                    {
                                        case "up":
                                        case "down":
                                            if (!(habbo.GetPermissions()?.HasRight("corporation_rights") == true))
                                            {
                                                if (itsMe)
                                                    return;

                                                if ((!isAdmin && !(isMember && canAscDesc && !itsSup)))
                                                    return;
                                            }
                                            break;

                                        case "cross":
                                            if (!(habbo.GetPermissions()?.HasRight("corporation_rights") == true))
                                            {
                                                if (itsMe)
                                                    return;

                                                if ((!isAdmin && !(isMember && canFire)) || itsMe)
                                                    return;
                                            }
                                            break;
                                    }

                                    if (wsAction == "up" || wsAction == "down")
                                    {
                                        if (!groups[0].Members.ContainsKey(getUserId))
                                            return;

                                        int getRank = groups[0].Members[getUserId].UserRank;
                                        int newRank = wsAction == "up" ? getRank + 1 : getRank - 1;

                                        if (wsAction == "down" && groups[0].IsAdmin(getUserId) &&
                                            !isAdmin && !(habbo.GetPermissions()?.HasRight("corporation_rights") == true))
                                        {
                                            Client.SendWhisper("No puedes bajar de rango al líder de la Banda");
                                            return;
                                        }

                                        // Validamos si existe el rango
                                        if (!groups[0].Ranks.Any(x => x.Value.RankId == newRank))
                                            return;

                                        using (var db = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                                        {
                                            db.RunQuery($"CALL `ModifMember`({getRank}, {newRank}, {groups[0].Id}, {getUserId});");
                                            groups[0].Members[getUserId].UserRank = newRank;
                                        }

                                        // Notificar al target
                                        GameClient targetSession = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(getUserId);
                                        if (targetSession != null)
                                        {
                                            string actionText = wsAction == "up" ? "ascendid@" : "degradad@";
                                            RoleplayManager.Shout(targetSession, $"*Ha sido {actionText} en su banda {groups[0].Name}*", 5);
                                            targetSession.SendWhisper($"Has sido {actionText} de rango en tu banda.", 1);
                                        }
                                    }
                                    else if (wsAction == "cross")
                                    {
                                        if (groups[0].IsAdmin(getUserId) && !(habbo.GetPermissions()?.HasRight("corporation_rights") == true))
                                        {
                                            Client.SendWhisper("No puedes despedir al líder de la Banda");
                                            return;
                                        }

                                        GameClient targetSession = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(getUserId);

                                        string extraInfo = groups[0].IsAdmin(getUserId)
                                            ? $"Se te revocado el liderázgo de {groups[0].Name}"
                                            : $"Se te ha expulsado de la banda {groups[0].Name}";

                                        if (groups[0].IsAdmin(getUserId))
                                            groups[0].TakeAdmin(getUserId);

                                        if (groups[0].IsMember(getUserId))
                                        {
                                            if (targetSession != null)
                                            {
                                                targetSession.GetRoleplay().GangId = 0;
                                                targetSession.GetRoleplay().GangRank = 0;
                                            }
                                            groups[0].DeleteMember(getUserId);
                                        }

                                        // Notificar al target
                                        if (targetSession != null)
                                        {
                                            if (!string.IsNullOrEmpty(extraInfo))
                                                targetSession.SendNotification(extraInfo);

                                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(targetSession, "event_group", "open");
                                            RoleplayManager.Shout(targetSession, $"*Ha sido expulsado de la banda {groups[0].Name}*", 5);
                                        }
                                    }

                                    PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_gang", "mynew");
                                }
                                break;

                            case "accept":
                            case "decline":
                                {
                                    if (!groups[0].IsAdmin(habbo.Id) && !groups[0].IsMember(habbo.Id) &&
                                        !(habbo.GetPermissions()?.HasRight("group_management_override") == true))
                                        return;

                                    if (wsAction == "accept")
                                    {
                                        List<GroupMember> administrators = groups[0].Members.Values
                                            .Where(x => x.IsAdmin)
                                            .OrderBy(x => x.UserId)
                                            .ToList();

                                        string vipLider = PolarEnvironment.GetUserInfoBy("rank_vip", "id",
                                            administrators.Count > 0 ? administrators[0].UserId.ToString() : "0");
                                        int limitMembers = RoleplayManager.GangsMaxMembers;
                                        if (vipLider == "1")
                                            limitMembers += 5;
                                        else if (vipLider == "2")
                                            limitMembers += 10;

                                        if (groups[0].GetAllMembersDict.Count >= limitMembers)
                                        {
                                            Client.SendWhisper("¡Has alcanzado el máximo de miembros admitidos en una banda!", 1);
                                            return;
                                        }

                                        GameClient targetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(getUserId);
                                        List<Group> targetGangs = PolarEnvironment.GetGame().GetGroupManager().GetGangsForUser(getUserId);

                                        if (targetGangs == null)
                                        {
                                            Client.SendWhisper("Ocurrió un problema al buscar las bandas de esa persona. Intentalo más tarde.", 1);
                                            return;
                                        }

                                        if (PolarEnvironment.GetGame().GetClientManager().GetLevelById(getUserId) < 2)
                                        {
                                            Client.SendWhisper("¡Esa persona es Nivel 1! Necesita al menos Nivel 2 para pertenecer a una banda.", 1);
                                            return;
                                        }

                                        if (GroupManager.HasJobCommand(targetClient, "law"))
                                        {
                                            Client.SendWhisper("¡Esa persona es policía! No puedes aceptarla en tu banda.", 1);
                                            return;
                                        }

                                        if (targetGangs.Count == 0)
                                        {
                                            // Si está On
                                            if (targetClient != null)
                                            {
                                                if (groups[0].GroupType == GroupType.LOCKED)
                                                {
                                                    if (!groups[0].HasRequest(getUserId))
                                                        return;

                                                    Habbo targetHabbo = PolarEnvironment.GetHabboById(getUserId);
                                                    if (targetHabbo == null)
                                                    {
                                                        Client.SendNotification("Oops, ha ocurrido un problema al buscar al usuario, es probable que se haya desconectado. ¡El proceso lo dejó en la Lista de Solicitudes de Banda!");
                                                        return;
                                                    }

                                                    groups[0].HandleRequest(getUserId, true);
                                                    Client.SendMessage(new GroupMemberUpdatedComposer(groups[0].Id, targetHabbo, 4));
                                                }

                                                if (groups[0].HasChat)
                                                {
                                                    targetClient.SendMessage(new FriendListUpdateComposer(groups[0], 0));
                                                }

                                                targetClient.GetRoleplay().GangId = groups[0].Id;
                                                targetClient.GetRoleplay().GangRank = 1;
                                                groups[0].UpdateGangMember(targetClient.GetHabbo().Id);

                                                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(targetClient, "event_group", "open");
                                                RoleplayManager.Shout(Client, $"*Admite la entrada a {PolarEnvironment.GetUsernameById(getUserId)} en su banda {groups[0].Name}*", 5);
                                                RoleplayManager.Shout(targetClient, $"*Ha sido admitido en la banda {groups[0].Name}*", 5);
                                                targetClient.SendNotification($"¡Felicitaciones! Han aceptado tu solicitud en la banda {groups[0].Name}");
                                            }
                                            // Si está Off
                                            else
                                            {
                                                groups[0].HandleRequest(getUserId, true);
                                                RoleplayManager.Shout(Client, $"*Admite la entrada a {PolarEnvironment.GetUsernameById(getUserId)} en su banda {groups[0].Name}*", 5);
                                            }
                                        }
                                        else
                                        {
                                            Client.SendWhisper("Esta persona ya pertenece a otra banda. Pídele que la abandone o rechaza su solicitud.", 1);
                                        }
                                    }
                                    else if (wsAction == "decline")
                                    {
                                        groups[0].HandleRequest(getUserId, false);
                                        GameClient targetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(getUserId);
                                        if (targetClient != null)
                                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(targetClient, "event_group", "open");
                                    }

                                    PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_gang", "requests");
                                }
                                break;
                        }
                    }
                    break;
                #endregion

                #region Ranks Tab
                case "ranks":
                    {
                        var habbo = Client?.GetHabbo();
                        if (habbo == null)
                            return;

                        List<Group> groups = PolarEnvironment.GetGame().GetGroupManager().GetGangsForUser(habbo.Id);
                        if (groups == null || groups.Count <= 0)
                            return;

                        #region Check if is not Admin
                        if (!groups[0].IsAdmin(habbo.Id) && !(habbo.GetPermissions()?.HasRight("group_management_override") == true))
                            return;
                        #endregion

                        var htmlBuilder = new StringBuilder();

                        #region HTML
                        htmlBuilder.Append("<div class=\"heading\">Agregar Rango</div>");
                        htmlBuilder.Append("<div class=\"flex\">");
                        htmlBuilder.Append("<input id=\"GA_InputRank\" type=\"text\" data-lpignore=\"true\" class=\"dark-input-text flex-1 mr-1\" value=\"\" maxlength=\"50\" autocomplete=\"off\">");
                        htmlBuilder.Append("<button id=\"GA_My_AddRank\" class=\"dark-button\">Agregar</button>");
                        htmlBuilder.Append("</div>");
                        #endregion

                        Socket.SendWS( $"compose_gang|ranks|{htmlBuilder.ToString()}");
                    }
                    break;
                #endregion

                #region Manage
                case "manage":
                    {
                        var habbo = Client?.GetHabbo();
                        if (habbo == null)
                            return;

                        List<Group> groups = PolarEnvironment.GetGame().GetGroupManager().GetGangsForUser(habbo.Id);
                        if (groups == null || groups.Count <= 0)
                            return;

                        #region Check if is not Admin
                        if (!groups[0].IsAdmin(habbo.Id) && !(habbo.GetPermissions()?.HasRight("group_management_override") == true))
                            return;
                        #endregion

                        string[] receivedData = Data.Split(',');
                        if (receivedData.Length < 2)
                            return;

                        string wsAction = receivedData[1];
                        string dataString = "";

                        if (wsAction != "open" && receivedData.Length > 2)
                        {
                            dataString = receivedData[2];

                            if (string.IsNullOrWhiteSpace(dataString))
                                return;
                        }

                        switch (wsAction)
                        {
                            case "open":
                                Socket.SendWS( "compose_gang|ranks|");
                                break;

                            case "addrank":
                                if (dataString.Length > 50)
                                {
                                    Socket.SendWS( "compose_gang|msg_error|El nombre del rango es demasiado largo.");
                                    return;
                                }
                                if (!Regex.IsMatch(dataString, @"^[a-zA-Z0-9]+$"))
                                {
                                    Socket.SendWS( "compose_gang|msg_error|¡No se aceptan caracteres especiales! Solo números y letras.");
                                    return;
                                }

                                if (groups[0].Ranks.Count >= 8)
                                {
                                    Socket.SendWS( "compose_gang|msg_error|Límite de 8 rangos alcanzados.");
                                    return;
                                }

                                int newRank = groups[0].Ranks.Count + 1;
                                string[] workrooms = groups[0].RoomId.ToString().Split(',');

                                groups[0].AddRank(groups[0].Id, newRank, dataString, "", "", 0, null, workrooms, 0);
                                Socket.SendWS( "compose_gang|msg_success|Rango agregado exitosamente.");
                                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_gang", "mynew");
                                break;
                        }
                    }
                    break;
                #endregion

                #region Edit Rank
                case "editrank":
                    {
                        var habbo = Client?.GetHabbo();
                        if (habbo == null)
                            return;

                        List<Group> groups = PolarEnvironment.GetGame().GetGroupManager().GetGangsForUser(habbo.Id);
                        if (groups == null || groups.Count <= 0)
                            return;

                        #region Check if is not Admin
                        if (!groups[0].IsAdmin(habbo.Id) && !(habbo.GetPermissions()?.HasRight("group_management_override") == true))
                            return;
                        #endregion

                        string[] receivedData = Data.Split(',');
                        if (receivedData.Length < 3)
                            return;

                        if (!int.TryParse(receivedData[2], out int getRank))
                            return;

                        string wsAction = receivedData[1];

                        // Validamos si existe el rango
                        if (!groups[0].Ranks.Any(x => x.Value.RankId == getRank))
                            return;

                        switch (wsAction)
                        {
                            case "saverank":
                                if (receivedData.Length < 4)
                                    return;

                                string rankName = receivedData[3];
                                if (string.IsNullOrWhiteSpace(rankName))
                                {
                                    Socket.SendWS( "compose_gang|msg_error|Ese nombre de rango no es válido.");
                                    return;
                                }
                                if (rankName.Length > 50)
                                {
                                    Socket.SendWS( "compose_gang|msg_error|Ese nombre de rango es demasiado largo.");
                                    return;
                                }

                                groups[0].UpdateJobSettings(getRank, rankName, 0, 0);
                                Socket.SendWS( "compose_gang|msg_success|Cambios guardados satisfactoriamente.");
                                break;

                            case "permissions":
                                if (receivedData.Length < 4)
                                    return;

                                string typeCmd = receivedData[3];
                                if (typeCmd != "ascdesc" && typeCmd != "hire" && typeCmd != "fire" && typeCmd != "invite")
                                    return;

                                groups[0].UpdateJobCommads(getRank, typeCmd);
                                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_gang", $"rank_tools,{getRank},settings");
                                break;
                        }
                    }
                    break;
                #endregion

                #region Invitations Receiveds
                case "invitations_re":
                    {
                        var habbo = Client?.GetHabbo();
                        if (habbo == null)
                            return;

                        var htmlBuilder = new StringBuilder();

                        #region HTML
                        htmlBuilder.Append("<div>");
                        htmlBuilder.Append("<div class=\"m-2\">");
                        htmlBuilder.Append("<div class=\"heading relative group\">");
                        htmlBuilder.Append("<div>Estas bandas te han invitado a un&iacute;rteles</div>");
                        htmlBuilder.Append("</div>");

                        int counter = 0;

                        DataTable phOwn = null;
                        using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                        {
                            dbClient.SetQuery("SELECT * FROM `rp_gangs_requests` WHERE `user_id` = @userId");
                            dbClient.AddParameter("userId", habbo.Id);
                            phOwn = dbClient.getTable();

                            if (phOwn != null)
                            {
                                foreach (DataRow row in phOwn.Rows)
                                {
                                    if (!int.TryParse(row["gang_id"]?.ToString(), out int gangId))
                                        continue;

                                    Group gang = GroupManager.GetGang(gangId);
                                    if (gang == null)
                                        continue;

                                    counter++;
                                    htmlBuilder.Append("<div class=\"flex flex-wrap -m-1 justify-center\">");
                                    htmlBuilder.Append("<div class=\"bg-dark-4 rounded m-1 group cursor-pointer-r\" style=\"width: 23%;\">");
                                    htmlBuilder.Append("<div class=\"m-px relative\">");
                                    htmlBuilder.Append("<div class=\"overflow-hidden bg-light-05 rounded-t\">");
                                    htmlBuilder.Append($"<center><div class=\"figure-H_RWF_0\" style=\"background-image: url({RoleplayManager.HotelUrl}/group-badge/badge/{gang.GetBadge()}); width: 64px; height: 110px; margin-top: -20px;\"></div></center>");
                                    htmlBuilder.Append("</div>");
                                    htmlBuilder.Append("<div class=\"absolute pin-b bg-dark-5 w-full group-hover:block\" style=\"padding-top: 4px;padding-bottom: 4px;\">");
                                    htmlBuilder.Append("<div class=\"flex justify-around\">");
                                    htmlBuilder.Append($"<div data-gang=\"{gang.Id}\" data-action=\"accept\" class=\"cursor-pointer-r px-1\">");
                                    htmlBuilder.Append($"<img src=\"{RoleplayManager.CdnURL}/ws_resources/images/up-arrow.png\">");
                                    htmlBuilder.Append("</div>");
                                    htmlBuilder.Append($"<div data-gang=\"{gang.Id}\" data-action=\"decline\" class=\"cursor-pointer-r px-1\">");
                                    htmlBuilder.Append($"<img src=\"{RoleplayManager.CdnURL}/ws_resources/images/cross.png\">");
                                    htmlBuilder.Append("</div>");
                                    htmlBuilder.Append("</div>");
                                    htmlBuilder.Append("</div>");
                                    htmlBuilder.Append("</div>");
                                    htmlBuilder.Append($"<div class=\"text-center py-1\">{gang.Name}</div>");
                                    htmlBuilder.Append("</div>");

                                    htmlBuilder.Append("<div class=\"bg-dark-4 rounded m-1 group\" style=\"width: 69%;padding: 5px;\">");
                                    htmlBuilder.Append("<p>Estad&iacute;sticas principales de la banda:</p>");
                                    htmlBuilder.Append("<br>");
                                    htmlBuilder.Append("<div style=\"max-height: 108px;overflow: auto;display: inline-flex;\">");
                                    htmlBuilder.Append("<div class=\"gang_inf_box\">");
                                    htmlBuilder.Append("<div><b>Asesinatos</b></div>");
                                    htmlBuilder.Append($"<div>{string.Format("{0:N0}", gang.GangKills)}</div>");
                                    htmlBuilder.Append("</div>");
                                    htmlBuilder.Append("<div class=\"gang_inf_box\">");
                                    htmlBuilder.Append("<div><b>Barrios capturados</b></div>");
                                    htmlBuilder.Append($"<div>{string.Format("{0:N0}", gang.GangTurfsTaken)}</div>");
                                    htmlBuilder.Append("</div>");
                                    htmlBuilder.Append("<div class=\"gang_inf_box\">");
                                    htmlBuilder.Append("<div><b>Barrios defendidos</b></div>");
                                    htmlBuilder.Append($"<div>{string.Format("{0:N0}", gang.GangTurfsDefended)}</div>");
                                    htmlBuilder.Append("</div>");
                                    htmlBuilder.Append("<div class=\"gang_inf_box\">");
                                    htmlBuilder.Append("<div><b>Riqueza</b></div>");
                                    htmlBuilder.Append($"<div>$ {string.Format("{0:N0}", gang.Balance)}</div>");
                                    htmlBuilder.Append("</div>");
                                    htmlBuilder.Append("</div>");
                                    htmlBuilder.Append("<br><br>");
                                    htmlBuilder.Append("<i>M&aacute;s info. en el Perfil de la banda.</i>");
                                    htmlBuilder.Append("</div>");
                                    htmlBuilder.Append("</div>");
                                }
                            }
                        }

                        if (counter <= 0)
                            htmlBuilder.Append("<center><b style='color:red'>No tienes ninguna invitación de banda pendiente.</b></center>");

                        htmlBuilder.Append("</div>");
                        htmlBuilder.Append("</div>");
                        #endregion

                        Socket.SendWS( $"compose_gang|invitations_re|{htmlBuilder.ToString()}");
                    }
                    break;
                #endregion

                #region Invitations Sendeds
                case "invitations_se":
                    {
                        var habbo = Client?.GetHabbo();
                        if (habbo == null)
                            return;

                        List<Group> groups = PolarEnvironment.GetGame().GetGroupManager().GetGangsForUser(habbo.Id);
                        if (groups == null || groups.Count <= 0)
                        {
                            Socket.SendWS( "compose_gang|invitations_se|<center><b style='color:red'>No perteneces a ninguna banda para poder enviar invitaciones.</b></center>");
                            return;
                        }

                        bool canInvite = groups[0].IsAdmin(habbo.Id) ||
                                       groups[0].IsMember(habbo.Id) ||
                                       (habbo.GetPermissions()?.HasRight("group_management_override") == true);

                        var htmlBuilder = new StringBuilder();
                        var allRequest = groups[0].GetRequests.ToList();

                        #region HTML
                        htmlBuilder.Append("<div>");
                        htmlBuilder.Append("<div class=\"m-2\">");

                        if (canInvite)
                        {
                            htmlBuilder.Append("<div class=\"heading relative group\">");
                            htmlBuilder.Append("<div>Invitar miembros a tu banda</div>");
                            htmlBuilder.Append("</div>");
                            htmlBuilder.Append("<div class=\"flex\">");
                            htmlBuilder.Append("<input id=\"Input_G_I_S_User\" type=\"text\" data-lpignore=\"true\" class=\"dark-input-text flex-1 mr-1\" value=\"\" maxlength=\"50\" autocomplete=\"off\" placeholder=\"Escribe aquí el nombre de la persona que deseas invitar\">");
                            htmlBuilder.Append("<button id=\"GA_Send_Inv\" class=\"dark-button\">Invitar</button>");
                            htmlBuilder.Append("</div>");
                            htmlBuilder.Append("<br>");
                        }

                        htmlBuilder.Append("<div class=\"heading relative group\">");
                        htmlBuilder.Append("<div>Invitaciones pendientes</div>");
                        htmlBuilder.Append("</div>");
                        htmlBuilder.Append("<table id=\"financelist\">");

                        foreach (var requestItem in allRequest)
                        {
                            string requestString = requestItem.ToString();
                            if (!int.TryParse(requestString, out int userId))
                                continue;

                            string name = PolarEnvironment.GetGame().GetClientManager().GetNameById(userId) ?? "Desconocido";
                            string look = PolarEnvironment.GetGame().GetClientManager().GetLookById(userId) ?? "";

                            using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                            {
                                dbClient.SetQuery("SELECT * FROM `rp_gangs_requests` WHERE `gang_id` = @id AND `user_id` = @userid LIMIT 1");
                                dbClient.AddParameter("id", groups[0].Id);
                                dbClient.AddParameter("userid", userId);

                                var row = dbClient.getRow();
                                if (row != null)
                                {
                                    htmlBuilder.Append("<tr>");
                                    htmlBuilder.Append("<td>");
                                    htmlBuilder.Append($"<img src=\"{RoleplayManager.AVATARIMG}{look}&headonly=1\">");
                                    htmlBuilder.Append($"{name} fue invitad@ a la banda");
                                    htmlBuilder.Append("</td>");
                                    htmlBuilder.Append("</tr>");
                                }
                            }
                        }

                        htmlBuilder.Append("</table>");
                        htmlBuilder.Append("</div>");
                        htmlBuilder.Append("</div>");
                        #endregion

                        Socket.SendWS( $"compose_gang|invitations_se|{htmlBuilder.ToString()}");
                    }
                    break;
                #endregion

                #region Invitations Sending
                case "send_invitation":
                    {
                        #region Conditions
                        var habbo = Client?.GetHabbo();
                        if (habbo == null)
                            return;

                        List<Group> groups = PolarEnvironment.GetGame().GetGroupManager().GetGangsForUser(habbo.Id);
                        if (groups == null || groups.Count <= 0)
                        {
                            Socket.SendWS( "compose_gang|invitations_se|<center><b style='color:red'>No perteneces a ninguna banda para poder enviar invitaciones.</b></center>");
                            return;
                        }

                        #region Check if is not Admin
                        if (!groups[0].IsAdmin(habbo.Id) && !groups[0].IsMember(habbo.Id) &&
                            !(habbo.GetPermissions()?.HasRight("group_management_override") == true))
                            return;
                        #endregion

                        string[] receivedData = Data.Split(',');
                        if (receivedData.Length < 2)
                            return;

                        string username = receivedData[1];

                        // Filtramos por seguridad
                        username = Regex.Replace(username, "<(.|\\n)*?>", string.Empty);

                        if (groups[0].GroupType == GroupType.OPEN)
                        {
                            Socket.SendWS( "compose_gang|msg_error|¡El acceso a tu banda es abierto! No puedes enviar invitaciones así. Cambia el modo de acceso.");
                            return;
                        }

                        if (string.IsNullOrEmpty(username))
                        {
                            Socket.SendWS( "compose_gang|msg_error|Debes ingresar un nombre de usuario.");
                            return;
                        }

                        if (username.ToLower() == habbo.Username.ToLower())
                        {
                            Socket.SendWS( "compose_gang|msg_error|¡No puedes enviarte invitaciones a ti mism@!");
                            return;
                        }

                        Habbo targetHabbo = PolarEnvironment.GetHabboByUsername(username);
                        if (targetHabbo == null)
                        {
                            Socket.SendWS( "compose_gang|msg_error|No se encontró ningún usuario con ese nombre.");
                            return;
                        }

                        if (groups[0].HasRequest(targetHabbo.Id))
                        {
                            Socket.SendWS( $"compose_gang|msg_error|Ya se ha enviado una invitación a {targetHabbo.Username}.");
                            return;
                        }

                        if (targetHabbo.GetClient() != null && targetHabbo.GetClient().GetRoleplay().Level < 2)
                        {
                            Socket.SendWS( "compose_gang|msg_error|¡Esa persona es Nivel 1! Necesita al menos Nivel 2 para pertenecer a una banda.");
                            return;
                        }

                        if (targetHabbo.GetClient() != null && GroupManager.HasJobCommand(targetHabbo.GetClient(), "law"))
                        {
                            Socket.SendWS( "compose_gang|msg_error|¡Esa persona es policía! No puedes invitarla a tu banda.");
                            return;
                        }
                        #endregion

                        #region Execute
                        groups[0].AddNewMember(targetHabbo.Id, 1, true);

                        if (targetHabbo.GetClient() != null)
                        {
                            targetHabbo.GetClient().SendMessage(new GroupInfoComposer(groups[0], targetHabbo.GetClient()));
                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(targetHabbo.GetClient(), "event_group", "open");
                        }

                        Socket.SendWS( $"compose_gang|msg_success|Invitación enviada a {targetHabbo.Username} correctamente.");
                        Client.Shout($"*Invita a {targetHabbo.Username} unirse a la pandilla: '{groups[0].Name}'*", 4);

                        if (targetHabbo.GetClient() != null)
                        {
                            targetHabbo.GetClient().SendWhisper($"Para unirte a la pandilla: '{groups[0].Name}' escribe ':aceptar pandilla' para unirte debes aportar 2.000$ para gastos", 34);
                            targetHabbo.GetClient().GetRoleplay().OfferManager.CreateOffer("pandilla", targetHabbo.Id, groups[0].Id);
                        }

                        PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_gang", "invitations_se");
                        #endregion
                    }
                    break;
                #endregion

                #region Invitations Receiveds Tools
                case "inv_re_tools":
                    {
                        if (Client.GetRoleplay().TryGetCooldown("inv_re_tools", true))
                            return;

                        var habbo = Client?.GetHabbo();
                        if (habbo == null)
                            return;

                        string[] receivedData = Data.Split(',');
                        if (receivedData.Length < 3)
                            return;

                        if (!int.TryParse(receivedData[1], out int getGang))
                            return;

                        string wsAction = receivedData[2];
                        Group gang = GroupManager.GetGang(getGang);
                        if (gang == null)
                            return;

                        switch (wsAction)
                        {
                            case "accept":
                                {
                                    if (Client.GetRoleplay().Level < 2)
                                    {
                                        Socket.SendWS( "compose_gang|msg_error|¡Necesitas al menos Nivel 2 para pertenecer a una banda.");
                                        return;
                                    }

                                    if (GroupManager.HasJobCommand(Client, "law"))
                                    {
                                        Socket.SendWS( "compose_gang|msg_error|¡No puedes pertenecer a una banda y ser policía a la vez!");
                                        return;
                                    }

                                    List<Group> userGangs = PolarEnvironment.GetGame().GetGroupManager().GetGangsForUser(habbo.Id);
                                    if (userGangs != null && userGangs.Count > 0)
                                    {
                                        Socket.SendWS( "compose_gang|msg_error|¡Ya perteneces a una banda! No puedes ser miembro de más de una a la vez.");
                                        return;
                                    }

                                    List<GroupMember> administrators = gang.Members.Values
                                        .Where(x => x.IsAdmin)
                                        .OrderBy(x => x.UserId)
                                        .ToList();

                                    string vipLider = PolarEnvironment.GetUserInfoBy("rank_vip", "id",
                                        administrators.Count > 0 ? administrators[0].UserId.ToString() : "0");
                                    int limitMembers = RoleplayManager.GangsMaxMembers;
                                    if (vipLider == "1")
                                        limitMembers += 5;
                                    else if (vipLider == "2")
                                        limitMembers += 10;

                                    if (gang.GetAllMembersDict.Count >= limitMembers)
                                    {
                                        Client.SendWhisper("¡La banda está llena! Ya no hay espacio para un/a nuev@ integrante", 1);
                                        return;
                                    }

                                    if (gang.GroupType == GroupType.LOCKED)
                                    {
                                        if (!gang.HasRequest(habbo.Id))
                                            return;

                                        Habbo userHabbo = PolarEnvironment.GetHabboById(habbo.Id);
                                        if (userHabbo == null)
                                        {
                                            Client.SendNotification("Oops, ha ocurrido un problema al buscar tu información.");
                                            return;
                                        }

                                        gang.HandleRequest(habbo.Id, true);
                                        Client.SendMessage(new GroupMemberUpdatedComposer(gang.Id, userHabbo, 4));
                                    }

                                    Client.GetRoleplay().GangId = gang.Id;
                                    Client.GetRoleplay().GangRank = 1;
                                    gang.UpdateGangMember(habbo.Id);

                                    RoleplayManager.Shout(Client, $"*Ha aceptado la invitación de ingreso a la banda {gang.Name}*", 5);
                                    Socket.SendWS( $"compose_gang|msg_success|Has aceptado ingresar a la banda {gang.Name}");

                                    PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_group", "open");
                                    PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_gang", "mynew");
                                    PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_gang", "invitations_re");
                                }
                                break;

                            case "decline":
                                {
                                    gang.HandleRequest(habbo.Id, false);
                                    RoleplayManager.Shout(Client, $"*Ha rechazado la invitación de ingreso a la banda {gang.Name}*", 5);
                                    Socket.SendWS( $"compose_gang|msg_success|Has rechazado ingresar a la banda {gang.Name}");

                                    PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_group", "open");
                                    PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_gang", "invitations_re");
                                }
                                break;
                        }

                        Client.GetRoleplay().CooldownManager.CreateCooldown("inv_re_tools", 1000, 5);
                    }
                    break;
                #endregion

                #region Requests
                case "requests":
                    {
                        var habbo = Client?.GetHabbo();
                        if (habbo == null)
                            return;

                        List<Group> groups = PolarEnvironment.GetGame().GetGroupManager().GetGangsForUser(habbo.Id);
                        if (groups == null || groups.Count <= 0)
                            return;

                        #region Check if is not Admin
                        if (!groups[0].IsAdmin(habbo.Id) && !groups[0].IsMember(habbo.Id) &&
                            !(habbo.GetPermissions()?.HasRight("group_management_override") == true))
                            return;
                        #endregion

                        var htmlBuilder = new StringBuilder();
                        var allRequest = groups[0].GetRequests.ToList();

                        #region HTML
                        htmlBuilder.Append("<div>");
                        htmlBuilder.Append("<div class=\"-m-2\">");
                        htmlBuilder.Append("<div class=\"m-2\">");
                        htmlBuilder.Append("<div class=\"heading relative group\">");
                        htmlBuilder.Append("<div>Solicitudes</div>");
                        htmlBuilder.Append("</div>");

                        int counter = 0;
                        foreach (var requestItem in allRequest)
                        {
                            string requestString = requestItem.ToString();
                            if (!int.TryParse(requestString, out int userId))
                                continue;

                            string name = PolarEnvironment.GetGame().GetClientManager().GetNameById(userId) ?? "Desconocido";
                            string look = PolarEnvironment.GetGame().GetClientManager().GetLookById(userId) ?? "";

                            using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                            {
                                dbClient.SetQuery("SELECT * FROM rp_stats WHERE gang_request = @id AND id = @userid LIMIT 1");
                                dbClient.AddParameter("id", groups[0].Id);
                                dbClient.AddParameter("userid", userId);

                                var row = dbClient.getRow();
                                if (row != null)
                                {
                                    counter++;

                                    htmlBuilder.Append("<div class=\"flex flex-wrap -m-1 justify-center\">");
                                    htmlBuilder.Append("<div class=\"bg-dark-4 rounded m-1 group cursor-pointer-r\" style=\"width: 23%;\">");
                                    htmlBuilder.Append("<div class=\"m-px relative\">");
                                    htmlBuilder.Append("<div class=\"overflow-hidden bg-light-05 rounded-t\">");
                                    htmlBuilder.Append($"<center><div class=\"figure-H_RWF_0\" style=\"background-image: url(&quot;{RoleplayManager.AVATARIMG}{look}&quot;); width: 64px; height: 110px; margin-top: -20px;\"></div></center>");
                                    htmlBuilder.Append("</div>");
                                    htmlBuilder.Append("<div class=\"absolute pin-b bg-dark-5 w-full group-hover:block\" style=\"padding-top: 4px;padding-bottom: 4px;\">");
                                    htmlBuilder.Append("<div class=\"flex justify-around\">");
                                    htmlBuilder.Append($"<div data-user=\"{userId}\" data-action=\"accept\" class=\"cursor-pointer-r px-1\">");
                                    htmlBuilder.Append($"<img src=\"{RoleplayManager.CdnURL}/ws_resources/images/up-arrow.png\">");
                                    htmlBuilder.Append("</div>");
                                    htmlBuilder.Append($"<div data-user=\"{userId}\" data-action=\"decline\" class=\"cursor-pointer-r px-1\">");
                                    htmlBuilder.Append($"<img src=\"{RoleplayManager.CdnURL}/ws_resources/images/cross.png\">");
                                    htmlBuilder.Append("</div>");
                                    htmlBuilder.Append("</div>");
                                    htmlBuilder.Append("</div>");
                                    htmlBuilder.Append("</div>");
                                    htmlBuilder.Append($"<div class=\"text-center py-1\">{name}</div>");
                                    htmlBuilder.Append("</div>");

                                    htmlBuilder.Append("<div class=\"bg-dark-4 rounded m-1 group\" style=\"width: 69%;padding: 5px;\">");
                                    htmlBuilder.Append("<p>Estad&iacute;sticas principales de " + name + ":</p>");
                                    htmlBuilder.Append("<br>");
                                    htmlBuilder.Append("<div style=\"max-height: 108px;overflow: auto;display: inline-flex;\">");

                                    // Nivel
                                    htmlBuilder.Append("<div class=\"gang_inf_box\">");
                                    htmlBuilder.Append("<div><b>Nivel</b></div>");
                                    htmlBuilder.Append($"<div>{string.Format("{0:N0}", row["level"])}</div>");
                                    htmlBuilder.Append("</div>");

                                    // Reputación
                                    htmlBuilder.Append("<div class=\"gang_inf_box\">");
                                    htmlBuilder.Append("<div><b>Reputación</b></div>");
                                    htmlBuilder.Append($"<div>{string.Format("{0:N0}", row["stamina"])} / {string.Format("{0:N0}", row["stamina_exp"])}</div>");
                                    htmlBuilder.Append("</div>");

                                    // Fuerza
                                    htmlBuilder.Append("<div class=\"gang_inf_box\">");
                                    htmlBuilder.Append("<div><b>Fuerza</b></div>");
                                    htmlBuilder.Append($"<div>{string.Format("{0:N0}", row["strength"])}</div>");
                                    htmlBuilder.Append("</div>");

                                    // Arrestos realizados
                                    htmlBuilder.Append("<div class=\"gang_inf_box\">");
                                    htmlBuilder.Append("<div><b>Arrestos realizados</b></div>");
                                    htmlBuilder.Append($"<div>{string.Format("{0:N0}", row["arrests"])}</div>");
                                    htmlBuilder.Append("</div>");

                                    // Veces arrestado
                                    htmlBuilder.Append("<div class=\"gang_inf_box\">");
                                    htmlBuilder.Append("<div><b>Veces arrestado</b></div>");
                                    htmlBuilder.Append($"<div>{string.Format("{0:N0}", row["arrested"])}</div>");
                                    htmlBuilder.Append("</div>");

                                    htmlBuilder.Append("</div>");
                                    htmlBuilder.Append("<br><br>");
                                    htmlBuilder.Append("<i>M&aacute;s info. en el Perfil del usuario.</i>");
                                    htmlBuilder.Append("</div>");
                                    htmlBuilder.Append("</div>");
                                }
                            }
                        }

                        if (counter <= 0)
                            htmlBuilder.Append("<center><b style='color:red'>No hay solicitudes nuevas.</b></center>");

                        htmlBuilder.Append("</div>");
                        htmlBuilder.Append("</div>");
                        htmlBuilder.Append("</div>");
                        #endregion

                        Socket.SendWS( $"compose_gang|requests|{htmlBuilder.ToString()}");
                    }
                    break;
                #endregion

                #region Edit Tab
                case "edit":
                    {
                        var habbo = Client?.GetHabbo();
                        if (habbo == null)
                            return;

                        List<Group> groups = PolarEnvironment.GetGame().GetGroupManager().GetGangsForUser(habbo.Id);
                        if (groups == null || groups.Count <= 0)
                            return;

                        #region Check if is not Admin
                        if (!groups[0].IsAdmin(habbo.Id) && !(habbo.GetPermissions()?.HasRight("corporation_rights") == true))
                            return;
                        #endregion

                        var htmlBuilder = new StringBuilder();

                        #region HTML
                        htmlBuilder.Append("<div class=\"mb-2\">");
                        htmlBuilder.Append("<div class=\"heading\">Retirar dinero de la Banda</div>");
                        htmlBuilder.Append("<div class=\"flex\">");
                        htmlBuilder.Append("<input id=\"GA_Withdraw\" type=\"number\" data-lpignore=\"true\" class=\"dark-input-text flex-1 mr-1\" value=\"\" autocomplete=\"off\" placeholder=\"Cantidad de dinero a retirar\">");
                        htmlBuilder.Append("<button id =\"GA_Withdraw_Btn\" data-action=\"Withdraw\" class=\"dark-button\">Retirar</button>");
                        htmlBuilder.Append("</div>");

                        if (groups[0].CreatorId == habbo.Id || (habbo.GetPermissions()?.HasRight("roleplay_corp_manager") == true))
                        {
                            htmlBuilder.Append("<br>");
                            htmlBuilder.Append("<div class=\"heading\">Transferir banda</div>");
                            htmlBuilder.Append("<i>Al transferir el mando a otro miembro de la banda, perderás todas las herramientas de administrador en ella.</i><br>");
                            htmlBuilder.Append("<div class=\"flex\">");
                            htmlBuilder.Append("<select id=\"GA_Edit_Trans_U\" class=\"dark-button\" style=\"width: 100%;margin-right:5px\">");
                            htmlBuilder.Append("<option value=\"0\" style=\"color:black\">Seleccionar miembro:</option>");

                            foreach (var member in groups[0].GetAllMembersDict)
                            {
                                if (member.Value.UserId == habbo.Id)
                                    continue;

                                string name = PolarEnvironment.GetGame().GetClientManager().GetNameById(Convert.ToInt32(member.Value.UserId)) ?? "Desconocido";
                                htmlBuilder.Append($"<option value=\"{name}\" style=\"color:black\">{name}</option>");
                            }

                            htmlBuilder.Append("</select>");
                            htmlBuilder.Append("<button id =\"GA_Edit_Trans\" class=\"dark-button\" data-action=\"Transfer\">Transferir</button>");
                            htmlBuilder.Append("</div>");

                            htmlBuilder.Append("<br>");
                            htmlBuilder.Append("<div class=\"heading\">Nombre de la Banda</div>");
                            htmlBuilder.Append("<div class=\"flex\">");
                            htmlBuilder.Append($"<input id=\"GA_Edit_Name\" type=\"text\" data-lpignore=\"true\" class=\"dark-input-text flex-1 mr-1\" value=\"{groups[0].Name}\" maxlength=\"50\" autocomplete=\"off\" placeholder=\"Escribe aqu&iacute; un nombre para tu banda\">");
                            htmlBuilder.Append("<button id =\"GA_Edit_Name_Btn\" data-action=\"EditName\" class=\"dark-button\">Cambiar nombre</button>");
                            htmlBuilder.Append("</div>");
                        }

                        string selected1 = (groups[0].GroupType == GroupType.LOCKED) ? "selected" : "";
                        string selected2 = (groups[0].GroupType == GroupType.OPEN) ? "selected" : "";

                        htmlBuilder.Append("<br>");
                        if (groups[0].CreatorId == habbo.Id || (habbo.GetPermissions()?.HasRight("roleplay_corp_manager") == true))
                        {
                            htmlBuilder.Append("<div class=\"heading\">Tipo de acceso</div>");
                            htmlBuilder.Append("<div class=\"flex\">");
                            htmlBuilder.Append("<select id=\"GA_Edit_Type\" class=\"dark-button\" style=\"width: 100%\">");
                            htmlBuilder.Append($"<option value=\"1\" style=\"color:black\" {selected1}>Por invitaci&oacute;n</option>");
                            htmlBuilder.Append($"<option value=\"0\" style=\"color:black\" {selected2}>Abierto (Cualquiera puede unirse)</option>");
                            htmlBuilder.Append("</select>");
                            htmlBuilder.Append("</div>");

                            htmlBuilder.Append("<br>");
                            htmlBuilder.Append("<div class=\"heading\">Eliminar banda</div>");
                            htmlBuilder.Append("<i>Al eliminar tu banda todos los datos serán borrados y los miembros expulsados. (No es reversible.)</i><br>");
                            htmlBuilder.Append("<div class=\"flex\">");
                            htmlBuilder.Append("<button id =\"GA_Delete_Btn\" class=\"dark-button\" data-action=\"Delete\" style=\"width: 100%\">Eliminar</button>");
                            htmlBuilder.Append("</div>");
                        }

                        htmlBuilder.Append("</div>");
                        #endregion

                        Socket.SendWS( $"compose_gang|edit|{htmlBuilder.ToString()}");
                    }
                    break;
                #endregion

                #region Edition
                case "edition":
                    {
                        if (Client.GetRoleplay().TryGetCooldown("gang_edit", true))
                            return;

                        var habbo = Client?.GetHabbo();
                        if (habbo == null)
                            return;

                        List<Group> groups = PolarEnvironment.GetGame().GetGroupManager().GetGangsForUser(habbo.Id);
                        if (groups == null || groups.Count <= 0)
                            return;

                        #region Check if is not Admin
                        if (!groups[0].IsAdmin(habbo.Id) && !(habbo.GetPermissions()?.HasRight("corporation_rights") == true))
                            return;
                        #endregion

                        string[] receivedData = Data.Split(',');
                        if (receivedData.Length < 2)
                            return;

                        string wsAction = receivedData[1];

                        switch (wsAction)
                        {
                            case "withdraw":
                                if (receivedData.Length < 3)
                                    return;

                                if (!int.TryParse(receivedData[2], out int cant))
                                {
                                    Socket.SendWS( "compose_gang|msg_error|Cantidad de dinero inválida.");
                                    return;
                                }

                                List<GroupMember> administrators = groups[0].Members.Values
                                    .Where(x => x.IsAdmin)
                                    .OrderBy(x => x.UserId)
                                    .ToList();

                                if (administrators.Count <= 0)
                                {
                                    Socket.SendWS( "compose_gang|msg_error|Esta empresa pertenece al Gobierno y no es posible retirar dinero.");
                                    return;
                                }

                                if (groups[0].Balance < cant)
                                {
                                    Socket.SendWS( "compose_gang|msg_error|La banda no cuenta con esa cantidad en su Riqueza para retirar.");
                                    return;
                                }

                                if (cant <= 0)
                                {
                                    Socket.SendWS( "compose_gang|msg_error|Debes retirar una cantidad mayor a $0.");
                                    return;
                                }

                                habbo.Credits += cant;
                                habbo.UpdateCreditsBalance();

                                groups[0].Balance -= cant;
                                groups[0].SetBussines(groups[0].Balance);

                                RoleplayManager.Shout(Client, $"*Ha retirado $ {string.Format("{0:N0}", cant)} de la riqueza de la banda {groups[0].Name}*", 5);
                                Socket.SendWS( "compose_gang|msg_success|Retiro realizado exitosamente.");
                                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_gang", "edit");
                                break;

                            case "newname":
                                if (receivedData.Length < 3)
                                    return;

                                if (groups[0].IsAdmin(habbo.Id) && groups[0].CreatorId != habbo.Id)
                                {
                                    if (!(habbo.GetPermissions()?.HasRight("roleplay_corp_manager") == true))
                                    {
                                        Client.SendWhisper("Oops, solo el dueño de la mafia puede cambiar el nombre!", 1);
                                        return;
                                    }
                                }

                                string newName = receivedData[2];
                                newName = Regex.Replace(newName, "<(.|\\n)*?>", string.Empty);

                                if (newName.Length < 3 || string.IsNullOrEmpty(newName))
                                {
                                    Socket.SendWS( "compose_gang|msg_error|El nombre de tu banda debe tener al menos 3 caracteres.");
                                    return;
                                }

                                groups[0].UpdateGroupName(newName);
                                Socket.SendWS( "compose_gang|msg_success|Nombre de la banda actualizado correctamente.");
                                break;

                            case "newtype":
                                if (receivedData.Length < 3)
                                    return;

                                if (groups[0].IsAdmin(habbo.Id) && groups[0].CreatorId != habbo.Id)
                                {
                                    if (!(habbo.GetPermissions()?.HasRight("roleplay_corp_manager") == true))
                                    {
                                        Client.SendWhisper("Oops, solo el dueño de la mafia puede modificarla!", 1);
                                        return;
                                    }
                                }

                                if (!int.TryParse(receivedData[2], out int newType))
                                {
                                    Socket.SendWS( "compose_gang|msg_error|Tipo de acceso inválido.");
                                    return;
                                }

                                groups[0].UpdateGangAccessType(newType);
                                Socket.SendWS( "compose_gang|msg_success|Tipo de acceso a la banda actualizado correctamente.");
                                break;

                            case "transfer":
                                if (receivedData.Length < 3)
                                    return;

                                if (groups[0].IsAdmin(habbo.Id) && groups[0].CreatorId != habbo.Id)
                                {
                                    if (!(habbo.GetPermissions()?.HasRight("roleplay_corp_manager") == true))
                                    {
                                        Client.SendWhisper("Oops, solo el dueño de la mafia puede transferirla!", 1);
                                        return;
                                    }
                                }

                                string newAdmin = receivedData[2];
                                newAdmin = Regex.Replace(newAdmin, "<(.|\\n)*?>", string.Empty);

                                Habbo target = PolarEnvironment.GetHabboByUsername(newAdmin);
                                if (target == null)
                                {
                                    Socket.SendWS( "compose_gang|msg_error|No se encontró a ningún usuario con ese nombre.");
                                    return;
                                }

                                if (target == habbo)
                                {
                                    Socket.SendWS( "compose_gang|msg_error|¡Tú ya eres el líder!");
                                    return;
                                }

                                if (!groups[0].IsMember(target.Id))
                                {
                                    Socket.SendWS( "compose_gang|msg_error|¡Esa persona no es miembro de tu banda!");
                                    return;
                                }

                                var allMembers = groups[0].GetAllMembersDict;
                                int getRank = groups[0].Members[target.Id].UserRank;
                                int newRank = groups[0].Members[habbo.Id].UserRank;

                                using (var db = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                                {
                                    db.RunQuery($"CALL `ModifMember`({newRank}, {getRank}, {groups[0].Id}, {habbo.Id});");
                                    allMembers.Where(x => x.Value.UserId == habbo.Id).ToList().ForEach(x => x.Value.UserRank = getRank);

                                    db.RunQuery($"CALL `ModifMember`({getRank}, {newRank}, {groups[0].Id}, {target.Id});");
                                    allMembers.Where(x => x.Value.UserId == target.Id).ToList().ForEach(x => x.Value.UserRank = newRank);
                                }

                                groups[0].MakeAdmin(target.Id);
                                groups[0].MakeOwner(target.Id);

                                Socket.SendWS( $"compose_gang|msg_success|Banda transferida con éxito. Ahora pertenece a {target.Username}");
                                RoleplayManager.Shout(Client, $"*Le ha transferido el mandato a {target.Username} de la banda {groups[0].Name}*", 5);
                                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_gang", "mynew");

                                if (target.GetClient() != null)
                                {
                                    RoleplayManager.Shout(target.GetClient(), $"*Obtiene el mandato de la banda {groups[0].Name}*", 5);
                                    target.GetClient().SendNotification($"¡Ahora eres el líder de la banda {groups[0].Name}! Vuelve a dar clic a la pestaña \"Mi banda\" del panel para ver las nuevas herramientas administrativas.");
                                }
                                break;

                            case "delete":
                                if (groups[0].IsAdmin(habbo.Id) && groups[0].CreatorId != habbo.Id)
                                {
                                    if (!(habbo.GetPermissions()?.HasRight("roleplay_corp_manager") == true))
                                    {
                                        Client.SendWhisper("Oops, solo el dueño de la mafia puede eliminarla!", 1);
                                        return;
                                    }
                                }

                                foreach (Room room in PolarEnvironment.GetGame().GetRoomManager().GetRooms())
                                {
                                    if (room?.Group?.Id != groups[0].Id)
                                        continue;

                                    room.Group = null;
                                    room.RoomData.Group = null;
                                }

                                PolarEnvironment.GetGame().GetGroupManager().DeleteGroup(groups[0].Id);

                                using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                                {
                                    dbClient.RunQuery($"DELETE FROM `rp_gangs` WHERE `id` = '{groups[0].Id}'");
                                    dbClient.RunQuery($"DELETE FROM `rp_gangs_requests` WHERE `gang_id` = '{groups[0].Id}'");
                                    dbClient.RunQuery($"UPDATE `rooms` SET `group_id` = '0' WHERE `group_id` = '{groups[0].Id}' LIMIT 1");
                                    dbClient.RunQuery($"DELETE FROM `groups_logs` WHERE `group_id` = '{groups[0].Id}'");
                                    dbClient.RunQuery($"DELETE FROM `rp_gangs_ranks` WHERE `gang` = '{groups[0].Id}'");
                                    dbClient.RunQuery($"UPDATE `rp_stats` SET `gang_id` = '0' WHERE `gang_id` = '{groups[0].Id}'");
                                    dbClient.RunQuery($"UPDATE `items_groups` SET `group_id` = '0' WHERE `group_id` = '{groups[0].Id}'");
                                }

                                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_gang", "mynew");
                                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_gang", "close");

                                foreach (Room room in PolarEnvironment.GetGame().GetRoomManager().GetRooms())
                                {
                                    if (room?.Group?.Id != groups[0].Id)
                                        continue;

                                    PolarEnvironment.GetGame().GetRoomManager().UnloadRoom(room, true);
                                }

                                Client.SendNotification("Banda eliminada satisfactoriamente.");
                                Client.GetRoleplay().GangId = 0;
                                break;

                            case "savegang":
                                if (habbo.Credits < (RoleplayManager.GangsPrice / 4))
                                {
                                    Socket.SendWS( "compose_gang|msg_error|No tienes el dinero suficiente para pagar tu deuda.");
                                    return;
                                }

                                habbo.Credits -= (RoleplayManager.GangsPrice / 4);
                                habbo.UpdateCreditsBalance();

                                groups[0].Balance = 0;
                                groups[0].SetBussines(groups[0].Balance);
                                Socket.SendWS( "compose_gang|msg_success|Deuda pagada exitosamente. Tu banda ya puede seguir operando.");
                                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_gang", "edit");
                                RoleplayManager.Shout(Client, $"*Ha pagado la deuda de su banda {groups[0].Name} salvándola de la bancarota*", 5);
                                break;
                        }

                        Client.GetRoleplay().CooldownManager.CreateCooldown("gang_edit", 1000, 10);
                    }
                    break;
                #endregion

                #region Stats
                case "stats":
                    {
                        var habbo = Client?.GetHabbo();
                        if (habbo == null)
                            return;

                        List<Group> groups = PolarEnvironment.GetGame().GetGroupManager().GetGangsForUser(habbo.Id);
                        if (groups == null || groups.Count <= 0)
                            return;

                        var htmlBuilder = new StringBuilder();

                        #region HTML
                        htmlBuilder.Append("<div class=\"flex mb-2 items-center justify-center\">");
                        htmlBuilder.Append("<div id=\"Tool_Colours\" class=\"mr-3 colours-3DCMW_0\">");
                        htmlBuilder.Append($"<img src=\"{RoleplayManager.HotelUrl}/group-badge/badge/{groups[0].GetBadge()}\" draggable=\"false\" ondragstart=\"return false;\">");
                        htmlBuilder.Append("</div>");
                        htmlBuilder.Append("<div id=\"Tool_Text\" class=\"text-3xl font-bold uppercase text-white border-b-4 border-dark-3\">");
                        htmlBuilder.Append(groups[0].Name);
                        htmlBuilder.Append("</div>");
                        htmlBuilder.Append("</div>");

                        if (groups[0].BankRuptcy)
                        {
                            htmlBuilder.Append("<div class=\"heading\" style=\"background-color:red\">¡Tu banda est&aacute; en banca rota!</div>");
                            htmlBuilder.Append("<div class=\"-m-1 flex flex-wrap justify-around\">");
                            htmlBuilder.Append("Tu banda no puede seguir gozando de beneficios económicos estando en banca rota. Puedes pagar la deuda para salvarla o bien, eliminarla.");
                            htmlBuilder.Append($"<button id =\"GA_Save\" data-action=\"SaveGang\" class=\"dark-button\" style=\"width: 99%;\">Salvar banda ($ {string.Format("{0:N0}", (RoleplayManager.GangsPrice / 4))})</button>");
                            htmlBuilder.Append("</div><br>");
                        }

                        List<GroupMember> administrators = groups[0].Members.Values
                            .Where(x => x.IsAdmin)
                            .OrderBy(x => x.UserId)
                            .ToList();

                        string founder = "Desconocido";
                        if (administrators.Count > 0)
                        {
                            founder = PolarEnvironment.GetGame().GetClientManager().GetNameById(Convert.ToInt32(administrators[0].UserId)) ?? "Desconocido";
                        }

                        htmlBuilder.Append("<div class=\"heading\">Estad&iacute;sticas</div>");
                        htmlBuilder.Append("<div class=\"-m-1 flex flex-wrap justify-around\">");

                        // Líder
                        htmlBuilder.Append("<div class=\"flex bg-dark-1 m-1 rounded-lg p-1 pl-2 items-center\" style=\"width: 45%;\">");
                        htmlBuilder.Append("<div class=\"mr-2\">L&iacute;der</div>");
                        htmlBuilder.Append($"<div class=\"bg-dark-2 px-2 py-1 rounded-lg ml-auto\">{founder}</div>");
                        htmlBuilder.Append("</div>");

                        // Fundado el
                        htmlBuilder.Append("<div class=\"flex bg-dark-1 m-1 rounded-lg p-1 pl-2 items-center\" style=\"width: 45%;\">");
                        htmlBuilder.Append("<div class=\"mr-2\">Fundado el</div>");
                        htmlBuilder.Append($"<div class=\"Shifts bg-dark-2 px-2 py-1 rounded-lg ml-auto\">{PolarEnvironment.UnixTimeStampToDateTime(groups[0].CreateTime).ToString("dd MMMM\\, yyyy")}</div>");
                        htmlBuilder.Append("</div>");

                        // Riqueza
                        htmlBuilder.Append("<div class=\"flex bg-dark-1 m-1 rounded-lg p-1 pl-2 items-center\" style=\"width: 45%;\">");
                        htmlBuilder.Append("<div class=\"mr-2\">Riqueza</div>");
                        htmlBuilder.Append($"<div class=\"bg-dark-2 px-2 py-1 rounded-lg ml-auto\">$ {string.Format("{0:N0}", groups[0].Balance)}</div>");
                        htmlBuilder.Append("</div>");

                        // Barrios en posesión
                        int newTurfsCount = 0;
                        List<Turf> tf = PolarEnvironment.GetGame().GetGangTurfsManager().getTurfsbyGang(groups[0].Id);
                        if (tf != null)
                            newTurfsCount = tf.Count;

                        htmlBuilder.Append("<div class=\"flex bg-dark-1 m-1 rounded-lg p-1 pl-2 items-center\" style=\"width: 45%;\">");
                        htmlBuilder.Append("<div class=\"mr-2\">Barrios en posesi&oacute;n</div>");
                        htmlBuilder.Append($"<div class=\"Shifts bg-dark-2 px-2 py-1 rounded-lg ml-auto\">{string.Format("{0:N0}", newTurfsCount)}</div>");
                        htmlBuilder.Append("</div>");

                        // Asesinatos
                        htmlBuilder.Append("<div class=\"flex bg-dark-1 m-1 rounded-lg p-1 pl-2 items-center\" style=\"width: 45%;\">");
                        htmlBuilder.Append("<div class=\"mr-2\">Asesinatos</div>");
                        htmlBuilder.Append($"<div class=\"bg-dark-2 px-2 py-1 rounded-lg ml-auto\">{string.Format("{0:N0}", groups[0].GangKills)}</div>");
                        htmlBuilder.Append("</div>");

                        // Asesinatos a policías
                        htmlBuilder.Append("<div class=\"flex bg-dark-1 m-1 rounded-lg p-1 pl-2 items-center\" style=\"width: 45%;\">");
                        htmlBuilder.Append("<div class=\"mr-2\">Asesinatos a polic&iacute;as</div>");
                        htmlBuilder.Append($"<div class=\"Shifts bg-dark-2 px-2 py-1 rounded-lg ml-auto\">{string.Format("{0:N0}", groups[0].GangCopKills)}</div>");
                        htmlBuilder.Append("</div>");

                        // Muertes
                        htmlBuilder.Append("<div class=\"flex bg-dark-1 m-1 rounded-lg p-1 pl-2 items-center\" style=\"width: 45%;\">");
                        htmlBuilder.Append("<div class=\"mr-2\">Muertes</div>");
                        htmlBuilder.Append($"<div class=\"bg-dark-2 px-2 py-1 rounded-lg ml-auto\">{string.Format("{0:N0}", groups[0].GangDeaths)}</div>");
                        htmlBuilder.Append("</div>");

                        // Barrios capturados
                        htmlBuilder.Append("<div class=\"flex bg-dark-1 m-1 rounded-lg p-1 pl-2 items-center\" style=\"width: 45%;\">");
                        htmlBuilder.Append("<div class=\"mr-2\">Barrios capturados</div>");
                        htmlBuilder.Append($"<div class=\"bg-dark-2 px-2 py-1 rounded-lg ml-auto\">{string.Format("{0:N0}", groups[0].GangTurfsTaken)}</div>");
                        htmlBuilder.Append("</div>");

                        // Barrios defendidos
                        htmlBuilder.Append("<div class=\"flex bg-dark-1 m-1 rounded-lg p-1 pl-2 items-center\" style=\"width: 45%;\">");
                        htmlBuilder.Append("<div class=\"mr-2\">Barrios defendidos</div>");
                        htmlBuilder.Append($"<div class=\"Shifts bg-dark-2 px-2 py-1 rounded-lg ml-auto\">{string.Format("{0:N0}", groups[0].GangTurfsDefended)}</div>");
                        htmlBuilder.Append("</div>");

                        // Medicamentos producidos
                        htmlBuilder.Append("<div class=\"flex bg-dark-1 m-1 rounded-lg p-1 pl-2 items-center\" style=\"width: 45%;\">");
                        htmlBuilder.Append("<div class=\"mr-2\">Medicamentos producidos</div>");
                        htmlBuilder.Append($"<div class=\"Shifts bg-dark-2 px-2 py-1 rounded-lg ml-auto\">{string.Format("{0:N0}", groups[0].MediPacks)}</div>");
                        htmlBuilder.Append("</div>");

                        htmlBuilder.Append("</div>");

                        htmlBuilder.Append("<br><div class=\"heading\">Historial de Actividades</div>");
                        htmlBuilder.Append("<div class=\"-m-1 flex flex-wrap justify-around\" style=\"margin-bottom: 5px;height: 252px;max-height: 252px;overflow: auto;\">");

                        List<GroupLogs> logs = groups[0].getAllLogs();
                        if (logs != null && logs.Count > 0)
                        {
                            htmlBuilder.Append("<table id=\"financelist\">");

                            foreach (var log in logs)
                            {
                                Habbo hbo = PolarEnvironment.GetHabboById(log.UserId);
                                if (hbo == null)
                                    continue;

                                htmlBuilder.Append("<tr>");
                                htmlBuilder.Append("<td>");
                                htmlBuilder.Append($"{log.Action} ({log.TimeStamp.ToString("dd\\/MM\\/yyyy")})");
                                htmlBuilder.Append("</td>");
                                htmlBuilder.Append("</tr>");
                                htmlBuilder.Append("<tr>");
                                htmlBuilder.Append("</tr>");
                            }

                            htmlBuilder.Append("</table>");
                        }

                        htmlBuilder.Append("</div>");
                        htmlBuilder.Append("</div>");
                        #endregion

                        Socket.SendWS( $"compose_gang|stats|{htmlBuilder.ToString()}");
                    }
                    break;
                #endregion

                #region Search Gang
                case "search":
                    {
                        string[] receivedData = Data.Split(',');
                        if (receivedData.Length < 2)
                            return;

                        string search = receivedData[1];

                        if (search.Length <= 0 || string.IsNullOrEmpty(search))
                        {
                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_gang", "open");
                            return;
                        }

                        var htmlBuilder = new StringBuilder();
                        int counter = 0;

                        foreach (Group group in PolarEnvironment.GetGame().GetGroupManager().GangsG.Where(x => x.Name.ToLower().Contains(search.ToLower())).ToList())
                        {
                            if (group.GType != 3)
                                continue;

                            counter++;
                            htmlBuilder.Append("<div class=\"flex bg-dark-1 m-1 rounded-lg p-1 pl-2 items-center\" style=\"width: 45.5%;\">");
                            htmlBuilder.Append("<div class=\"mr-2\">");
                            htmlBuilder.Append($"<p>{group.Name}</p>");
                            htmlBuilder.Append($"<p>{group.GetAllMembersDict.Count()} miembro(s)</p>");
                            htmlBuilder.Append("</div>");
                            htmlBuilder.Append($"<div class=\"bg-dark-2 px-2 py-1 rounded-lg ml-auto data-gang\" data-balloon=\"Ver info\" data-balloon-pos=\"left\" data-gang=\"{group.Id}\">");
                            htmlBuilder.Append($"<img src=\"{RoleplayManager.HotelUrl}/group-badge/badge/{group.GetBadge()}\" draggable=\"false\" ondragstart=\"return false;\" style=\"cursor: pointer;\">");
                            htmlBuilder.Append("</div>");
                            htmlBuilder.Append("</div>");
                        }

                        if (counter <= 0)
                            htmlBuilder.Append($"<center><b style='color:red'>No se encontraron resultados para \"{search}\"</b></center>");

                        Socket.SendWS( $"compose_gang|gang_list|{htmlBuilder.ToString()}|");
                    }
                    break;
                #endregion

                #region View
                case "view":
                    {
                        string[] receivedData = Data.Split(',');
                        if (receivedData.Length < 2)
                            return;

                        if (!int.TryParse(receivedData[1], out int getGangId))
                            return;

                        Group theGroup = GroupManager.GetGang(getGangId);
                        if (theGroup == null)
                            return;

                        var htmlBuilder = new StringBuilder();

                        #region HTML
                        htmlBuilder.Append("<div>");
                        htmlBuilder.Append("<div class=\"-m-2\">");

                        var allRanks = theGroup.Ranks.OrderByDescending(o => o.Value.RankId).ToList();

                        foreach (var rank in allRanks)
                        {
                            htmlBuilder.Append("<div class=\"m-2\">");
                            htmlBuilder.Append("<div class=\"heading relative group\">");
                            htmlBuilder.Append($"<div>{rank.Value.Name}</div>");
                            htmlBuilder.Append("</div>");

                            htmlBuilder.Append("<div class=\"flex flex-wrap -m-1 justify-center\">");

                            foreach (var member in theGroup.GetAllMembersDict.Where(m => m.Value.UserRank == rank.Value.RankId))
                            {
                                string name = PolarEnvironment.GetGame().GetClientManager().GetNameById(Convert.ToInt32(member.Value.UserId)) ?? "Desconocido";
                                string look = PolarEnvironment.GetGame().GetClientManager().GetLookById(Convert.ToInt32(member.Value.UserId)) ?? "";

                                htmlBuilder.Append("<div class=\"bg-dark-4 rounded m-1 group cursor-pointer-r\">");
                                htmlBuilder.Append("<div class=\"m-px relative\">");
                                htmlBuilder.Append("<div class=\"overflow-hidden bg-light-05 rounded-t\" style=\"height: 55px;\">");
                                htmlBuilder.Append($"<center><div class=\"figure-H_RWF_0\" style=\"background-image: url(&quot;{RoleplayManager.AVATARIMG}{look}&quot;); width: 64px; height: 110px; margin-top: -20px;\"></div></center>");
                                htmlBuilder.Append("</div>");
                                htmlBuilder.Append("</div>");
                                htmlBuilder.Append($"<div class=\"text-center py-1\">{name}</div>");
                                htmlBuilder.Append("</div>");
                            }

                            htmlBuilder.Append("</div>");
                            htmlBuilder.Append("</div>");
                        }

                        htmlBuilder.Append("</div>");
                        htmlBuilder.Append("</div>");
                        #endregion

                        Socket.SendWS( $"compose_gang|view|{htmlBuilder.ToString()}|{getGangId}");
                    }
                    break;
                #endregion

                #region View Stats
                case "view_stats":
                    {
                        string[] receivedData = Data.Split(',');
                        if (receivedData.Length < 2)
                            return;

                        if (!int.TryParse(receivedData[1], out int getGangId))
                            return;

                        Group theGroup = GroupManager.GetGang(getGangId);
                        if (theGroup == null)
                            return;

                        var htmlBuilder = new StringBuilder();

                        #region HTML
                        htmlBuilder.Append("<div class=\"flex mb-2 items-center justify-center\">");
                        htmlBuilder.Append("<div id=\"Tool_Colours\" class=\"mr-3 colours-3DCMW_0\">");
                        htmlBuilder.Append($"<img src=\"{RoleplayManager.HotelUrl}/group-badge/badge/{theGroup.GetBadge()}\" draggable=\"false\" ondragstart=\"return false;\">");
                        htmlBuilder.Append("</div>");
                        htmlBuilder.Append("<div id=\"Tool_Text\" class=\"text-3xl font-bold uppercase text-white border-b-4 border-dark-3\">");
                        htmlBuilder.Append(theGroup.Name);
                        htmlBuilder.Append("</div>");
                        htmlBuilder.Append("</div>");

                        string founder = "Desconocido";
                        List<GroupMember> administrators = theGroup.Members.Values
                            .Where(x => x.IsAdmin)
                            .OrderBy(x => x.UserId)
                            .ToList();

                        if (administrators.Count > 0)
                        {
                            founder = PolarEnvironment.GetGame().GetClientManager().GetNameById(Convert.ToInt32(administrators[0].UserId)) ?? "Desconocido";
                        }

                        htmlBuilder.Append("<div class=\"heading\">Estad&iacute;sticas</div>");
                        htmlBuilder.Append("<div class=\"-m-1 flex flex-wrap justify-around\">");

                        // Líder
                        htmlBuilder.Append("<div class=\"flex bg-dark-1 m-1 rounded-lg p-1 pl-2 items-center\" style=\"width: 45%;\">");
                        htmlBuilder.Append("<div class=\"mr-2\">L&iacute;der</div>");
                        htmlBuilder.Append($"<div class=\"bg-dark-2 px-2 py-1 rounded-lg ml-auto\">{founder}</div>");
                        htmlBuilder.Append("</div>");

                        // Fundado el
                        htmlBuilder.Append("<div class=\"flex bg-dark-1 m-1 rounded-lg p-1 pl-2 items-center\" style=\"width: 45%;\">");
                        htmlBuilder.Append("<div class=\"mr-2\">Fundado el</div>");
                        htmlBuilder.Append($"<div class=\"Shifts bg-dark-2 px-2 py-1 rounded-lg ml-auto\">{PolarEnvironment.UnixTimeStampToDateTime(theGroup.CreateTime).ToString("dd MMMM\\, yyyy")}</div>");
                        htmlBuilder.Append("</div>");

                        // Riqueza
                        htmlBuilder.Append("<div class=\"flex bg-dark-1 m-1 rounded-lg p-1 pl-2 items-center\" style=\"width: 45%;\">");
                        htmlBuilder.Append("<div class=\"mr-2\">Riqueza</div>");
                        htmlBuilder.Append($"<div class=\"bg-dark-2 px-2 py-1 rounded-lg ml-auto\">$ {string.Format("{0:N0}", theGroup.Balance)}</div>");
                        htmlBuilder.Append("</div>");

                        // Barrios en posesión
                        int newTurfsCount = 0;
                        List<Turf> tf = PolarEnvironment.GetGame().GetGangTurfsManager().getTurfsbyGang(theGroup.Id);
                        if (tf != null)
                            newTurfsCount = tf.Count;

                        htmlBuilder.Append("<div class=\"flex bg-dark-1 m-1 rounded-lg p-1 pl-2 items-center\" style=\"width: 45%;\">");
                        htmlBuilder.Append("<div class=\"mr-2\">Barrios en posesi&oacute;n</div>");
                        htmlBuilder.Append($"<div class=\"Shifts bg-dark-2 px-2 py-1 rounded-lg ml-auto\">{string.Format("{0:N0}", newTurfsCount)}</div>");
                        htmlBuilder.Append("</div>");

                        // Asesinatos
                        htmlBuilder.Append("<div class=\"flex bg-dark-1 m-1 rounded-lg p-1 pl-2 items-center\" style=\"width: 45%;\">");
                        htmlBuilder.Append("<div class=\"mr-2\">Asesinatos</div>");
                        htmlBuilder.Append($"<div class=\"bg-dark-2 px-2 py-1 rounded-lg ml-auto\">{string.Format("{0:N0}", theGroup.GangKills)}</div>");
                        htmlBuilder.Append("</div>");

                        // Asesinatos a policías
                        htmlBuilder.Append("<div class=\"flex bg-dark-1 m-1 rounded-lg p-1 pl-2 items-center\" style=\"width: 45%;\">");
                        htmlBuilder.Append("<div class=\"mr-2\">Asesinatos a polic&iacute;as</div>");
                        htmlBuilder.Append($"<div class=\"Shifts bg-dark-2 px-2 py-1 rounded-lg ml-auto\">{string.Format("{0:N0}", theGroup.GangCopKills)}</div>");
                        htmlBuilder.Append("</div>");

                        // Muertes
                        htmlBuilder.Append("<div class=\"flex bg-dark-1 m-1 rounded-lg p-1 pl-2 items-center\" style=\"width: 45%;\">");
                        htmlBuilder.Append("<div class=\"mr-2\">Muertes</div>");
                        htmlBuilder.Append($"<div class=\"bg-dark-2 px-2 py-1 rounded-lg ml-auto\">{string.Format("{0:N0}", theGroup.GangDeaths)}</div>");
                        htmlBuilder.Append("</div>");

                        // Barrios capturados
                        htmlBuilder.Append("<div class=\"flex bg-dark-1 m-1 rounded-lg p-1 pl-2 items-center\" style=\"width: 45%;\">");
                        htmlBuilder.Append("<div class=\"mr-2\">Barrios capturados</div>");
                        htmlBuilder.Append($"<div class=\"bg-dark-2 px-2 py-1 rounded-lg ml-auto\">{string.Format("{0:N0}", theGroup.GangTurfsTaken)}</div>");
                        htmlBuilder.Append("</div>");

                        // Barrios defendidos
                        htmlBuilder.Append("<div class=\"flex bg-dark-1 m-1 rounded-lg p-1 pl-2 items-center\" style=\"width: 45%;\">");
                        htmlBuilder.Append("<div class=\"mr-2\">Barrios defendidos</div>");
                        htmlBuilder.Append($"<div class=\"Shifts bg-dark-2 px-2 py-1 rounded-lg ml-auto\">{string.Format("{0:N0}", theGroup.GangTurfsDefended)}</div>");
                        htmlBuilder.Append("</div>");

                        // Medicamentos producidos
                        htmlBuilder.Append("<div class=\"flex bg-dark-1 m-1 rounded-lg p-1 pl-2 items-center\" style=\"width: 45%;\">");
                        htmlBuilder.Append("<div class=\"mr-2\">Medicamentos producidos</div>");
                        htmlBuilder.Append($"<div class=\"Shifts bg-dark-2 px-2 py-1 rounded-lg ml-auto\">{string.Format("{0:N0}", theGroup.MediPacks)}</div>");
                        htmlBuilder.Append("</div>");

                        htmlBuilder.Append("</div>");

                        htmlBuilder.Append("<br><div class=\"heading\">Historial de Actividades</div>");
                        htmlBuilder.Append("<div class=\"-m-1 flex flex-wrap justify-around\" style=\"margin-bottom: 5px;height: 252px;max-height: 252px;overflow: auto;\">");

                        List<GroupLogs> logs = theGroup.getAllLogs();
                        if (logs != null && logs.Count > 0)
                        {
                            htmlBuilder.Append("<table id=\"financelist\">");

                            foreach (var log in logs)
                            {
                                Habbo hbo = PolarEnvironment.GetHabboById(log.UserId);
                                if (hbo == null)
                                    continue;

                                htmlBuilder.Append("<tr>");
                                htmlBuilder.Append("<td>");
                                htmlBuilder.Append($"{log.Action} ({log.TimeStamp.ToString("dd\\/MM\\/yyyy")})");
                                htmlBuilder.Append("</td>");
                                htmlBuilder.Append("</tr>");
                                htmlBuilder.Append("<tr>");
                                htmlBuilder.Append("</tr>");
                            }

                            htmlBuilder.Append("</table>");
                        }

                        htmlBuilder.Append("</div>");
                        htmlBuilder.Append("</div>");
                        #endregion

                        Socket.SendWS( $"compose_gang|view_stats|{htmlBuilder.ToString()}|{getGangId}");
                    }
                    break;
                #endregion

                #region Bank Capturing Window
                case "bank_cap_w":
                    {
                        string[] receivedData = Data.Split(',');
                        if (receivedData.Length < 4)
                            return;

                        if (!int.TryParse(receivedData[1], out int roomId) ||
                            !int.TryParse(receivedData[2], out int userAttackId))
                            return;

                        if (!RoleplayManager.GenerateRoom(roomId, out Room room))
                            return;

                        string turfName = receivedData[3];
                        GameClient targetSession = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(userAttackId);

                        if (targetSession == null)
                        {
                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_gang", "bank_cap_off");
                            room.BankCapturing = false;
                            return;
                        }

                        #region Info
                        var infoBuilder = new StringBuilder();
                        infoBuilder.Append("<div class=\"mt-2\"> Robando por <span class=\"font-bold pointer-events-auto cursor-pointer hover:underline\">");
                        infoBuilder.Append(targetSession.GetHabbo()?.Username ?? "Desconocido");
                        infoBuilder.Append(" </span></div>");
                        #endregion

                        int per = 0;

                        if (RoleplayManager.BankCapTime > 0)
                        {
                            per = ((RoleplayManager.BankCapTime - targetSession.GetRoleplay().LoadingTimeLeft) * 100) / RoleplayManager.BankCapTime;
                        }

                        if (per >= 100)
                        {
                            per = 100;
                            room.BankCapturing = false;
                        }
                        else if (!targetSession.GetRoleplay().BankCapturing)
                        {
                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_gang", "bank_cap_off");
                            room.BankCapturing = false;
                            return;
                        }

                        string sendData = $"{turfName}|{infoBuilder.ToString()}|Robando {per}%|{per}|";
                        Socket.SendWS( $"compose_gang|capturing|{sendData}");

                        if (per >= 100)
                        {
                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_gang", "bank_cap_off");
                            room.BankCapturing = false;

                            foreach (RoomUser user in room.GetRoomUserManager().GetRoomUsers().ToList())
                            {
                                if (user?.GetClient() == null)
                                    continue;
                                //user.GetClient().GetRoleplay().bankRobTimer.EndTimer();
                                //user.GetClient().GetRoleplay().TimerManager.ActiveTimers["bankrob"]?.EndTimer();
                            }
                        }
                    }
                    break;
                #endregion

                #region Bank Capturing Off
                case "bank_cap_off":
                    {
                        Socket.SendWS( "compose_gang|capturing_off");
                    }
                    break;
                #endregion

                #region Turf Capturing Window
                case "turf_cap_w":
                    {
                        string[] receivedData = Data.Split(',');
                        if (receivedData.Length < 4)
                            return;

                        if (!int.TryParse(receivedData[1], out int roomId) ||
                            !int.TryParse(receivedData[2], out int userAttackId))
                            return;

                        if (!RoleplayManager.GenerateRoom(roomId, out Room room))
                            return;

                        string turfName = receivedData[3];
                        List<Group> gangAttack = PolarEnvironment.GetGame().GetGroupManager().GetGangsForUser(userAttackId);
                        if (gangAttack == null || gangAttack.Count <= 0)
                            return;

                        string gangAttackedName = room?.Group?.Name ?? "nadie";

                        #region Info
                        var infoBuilder = new StringBuilder();
                        infoBuilder.Append("<div> Controlado por <span class=\"font-bold pointer-events-auto cursor-pointer hover:underline\">");
                        infoBuilder.Append(gangAttackedName);
                        infoBuilder.Append("</span><br></div>");
                        infoBuilder.Append("<div class=\"mt-2\"> Atacado por <span class=\"font-bold pointer-events-auto cursor-pointer hover:underline\">");
                        infoBuilder.Append(gangAttack[0].Name);
                        infoBuilder.Append("</span></div>");
                        #endregion

                        int per = 0;
                        GameClient targetSession = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(userAttackId);

                        if (targetSession == null)
                        {
                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_gang", "turf_cap_off");
                            room.TurfCapturing = false;
                            return;
                        }

                        if (RoleplayManager.TurfCapTime > 0)
                        {
                            per = ((RoleplayManager.TurfCapTime - targetSession.GetRoleplay().LoadingTimeLeft) * 100) / RoleplayManager.TurfCapTime;
                        }

                        if (per >= 100)
                        {
                            per = 100;
                            room.TurfCapturing = false;
                        }
                        else if (!targetSession.GetRoleplay().TurfCapturing)
                        {
                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_gang", "turf_cap_off");
                            room.TurfCapturing = false;
                            return;
                        }

                        string sendData = $"{turfName}|{infoBuilder.ToString()}|Capturando {per}%|{per}|";
                        Socket.SendWS( $"compose_gang|capturing|{sendData}");

                        if (per >= 100)
                        {
                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_gang", "turf_cap_off");
                            room.TurfCapturing = false;

                            foreach (RoomUser user in room.GetRoomUserManager().GetRoomUsers().ToList())
                            {
                                if (user?.GetClient() == null)
                                    continue;

                                user.GetClient().GetRoleplay().TurfCapturing = false;
                                user.GetClient().GetRoleplay().CapturingTurf = null;
                            }
                        }
                    }
                    break;
                #endregion

                #region Turf Capturing Off
                case "turf_cap_off":
                    {
                        Socket.SendWS( "compose_gang|capturing_off");
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