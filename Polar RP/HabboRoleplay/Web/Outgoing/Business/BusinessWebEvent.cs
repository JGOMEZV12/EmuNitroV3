using ConnectionManager;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Groups;
using Polar.HabboHotel.Groups;
using Polar.Database.Interfaces;
using System.Text.RegularExpressions;
using Polar.HabboHotel.Users;
using Group = Polar.HabboHotel.Groups.Group;
using System.Data;
using System.Text;
using Polar.Net;
using System.Collections.Generic;

namespace Polar.HabboHotel.Roleplay.Web.Outgoing.Misc
{
    /// <summary>
    /// BusinessWebEvent class.
    /// </summary>
    class BusinessWebEvent : IWebEvent
    {
        /// <summary>
        /// Executes socket data.
        /// </summary>
        /// <param name="Client"></param>
        /// <param name="Data"></param>
        /// <param name="Socket"></param>
        public void Execute(GameClient Client, string Data, ConnectionInformation Socket)
        {
            if (!PolarEnvironment.GetGame().GetWebEventManager().SocketReady(Client, true) || !PolarEnvironment.GetGame().GetWebEventManager().SocketReady(Socket))
                return;

            if (Client?.GetRoomUser() == null)
                return;

            string Action = (Data.Contains(',') ? Data.Split(',')[0] : Data);
            //Generamos la Sala
            if (!RoleplayManager.GenerateRoom(Client.GetRoomUser().RoomId, out Room Room))
                return;

            switch (Action)
            {
                #region Open My
                case "open_my":
                    {
                        if (Client.GetRoleplay().JobId <= 0)
                        {
                            Socket.SendWS( "compose_business|create|");
                        }
                        else
                        {
                            Client.GetRoleplay().ViewMyCorp = true;
                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_business", "open_room");
                        }
                    }
                    break;
                #endregion

                #region Close My
                case "close_my":
                    {
                        Client.GetRoleplay().ViewMyCorp = false;
                        Client.GetRoleplay().ViewCorpId = 0;
                        Socket.SendWS( "compose_business|close|");
                    }
                    break;
                #endregion

                #region Open Room
                case "open_room":
                    {
                        Group Group = null;

                        #region Get Group
                        var habbo = Client?.GetHabbo();
                        if (habbo == null)
                            return;

                        if (Client.GetRoleplay().ViewMyCorp)
                        {
                            if (Client.GetRoleplay().JobId <= 0) // Por si acaso
                                return;

                            Group = Room.Group ?? PolarEnvironment.GetGame().GetGroupManager().GetJobByID(Client.GetRoleplay().JobId);
                        }
                        else
                        {
                            if (Room.Group == null || Room.Group.GType == 1) //GType 2 = Secundarios
                                return;

                            Group = Room.Group;
                        }

                        if (Group == null)
                            return;
                        #endregion

                        if (Group.GType == 2)
                        {
                            string Founder = "Gobierno";
                            string Tabs = "";
                            List<GroupMember> Administrators = Group.Members.Values.Where(x => x.IsAdmin).OrderBy(x => x.UserId).ToList();

                            #region Get Founder Name
                            if (Administrators.Count > 0)
                            {
                                Founder = PolarEnvironment.GetGame().GetClientManager().GetNameById(Convert.ToInt32(Administrators[0].UserId)) ?? "Desconocido";
                            }
                            #endregion

                            #region Check if is Admin
                            bool isAdmin = Group.IsAdmin(habbo.Id);
                            bool isMember = Group.IsMember(habbo.Id);
                            bool hasGroupOverride = habbo.GetPermissions()?.HasRight("group_management_override") == true;

                            if (isAdmin || isMember || hasGroupOverride)
                            {
                                Tabs += "<div class=\"tab-2ddeR_0 Request_Tab\">Solicitudes</div>";
                                Tabs += "<div class=\"tab-2ddeR_0 Manage_Tab\">Gestionar</div>";
                            }
                            #endregion

                            Client.GetRoleplay().ViewCorpId = Group.Id;

                            string SendData = "";
                            SendData += Group.Name + ";";
                            SendData += Group.Badge + ";";
                            SendData += Tabs + ";"; // Tabs
                            SendData += Group.Members.Count + ";";
                            SendData += Group.Requests.Count + ";";
                            SendData += Group.Ranks.Count + ";";
                            SendData += Group.Balance + ";";
                            SendData += Founder + ";";
                            SendData += (isAdmin || (habbo.GetPermissions()?.HasRight("corporation_rights") == true) ? "True;" : "False;");
                            SendData += (!isAdmin && !isMember && !hasGroupOverride ? "False;" : "True;");
                            Socket.SendWS( "compose_business|open|" + SendData);
                        }
                        else
                        {
                            // Gangs
                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_gang", "open");
                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_gang", "view_stats," + Group.Id);
                        }
                    }
                    break;
                #endregion

                #region Employees
                case "employees":
                    {
                        Group Group = GetTargetGroup(Client, Room);
                        if (Group == null)
                            return;

                        var habbo = Client?.GetHabbo();
                        if (habbo == null)
                            return;

                        bool isAdmin = Group.IsAdmin(habbo.Id) || (habbo.GetPermissions()?.HasRight("group_management_override") == true);
                        var htmlBuilder = new StringBuilder();

                        #region HTML
                        htmlBuilder.Append("<div>");
                        htmlBuilder.Append("<div class=\"-m-2\">");

                        var AllRanks = Group.Ranks.OrderByDescending(o => o.Value.RankId).ToList();

                        foreach (var rank in AllRanks)
                        {
                            // Rank box
                            htmlBuilder.Append("<div class=\"m-2\">");
                            // Rank Header
                            htmlBuilder.Append("<div class=\"heading relative group\">");
                            htmlBuilder.Append("<div>").Append(rank.Value.Name).Append("</div>");

                            if (isAdmin && !Group.Name.Contains("Policia"))
                            {
                                htmlBuilder.Append("<div class=\"absolute pin-t pin-r mr-1 h-full hidden group-hover:block\">");
                                htmlBuilder.Append("<div class=\"flex items-center h-full\">");

                                htmlBuilder.Append("<div data-rank=\"").Append(rank.Value.RankId).Append("\" data-action=\"settings\" class=\"cursor-pointer-r px-1\">");
                                htmlBuilder.Append("<img src=\"").Append(RoleplayManager.CdnURL).Append("/ws_resources/images/settings.png\">");
                                htmlBuilder.Append("</div>");

                                if (!Group.Name.Contains("Policia"))
                                {
                                    htmlBuilder.Append("<div data-rank=\"").Append(rank.Value.RankId).Append("\" data-action=\"up\" class=\"cursor-pointer-r px-1\">");
                                    htmlBuilder.Append("<img src=\"").Append(RoleplayManager.CdnURL).Append("/ws_resources/images/up-arrow.png\">");
                                    htmlBuilder.Append("</div>");

                                    htmlBuilder.Append("<div data-rank=\"").Append(rank.Value.RankId).Append("\" data-action=\"down\" class=\"cursor-pointer-r px-1\">");
                                    htmlBuilder.Append("<img src=\"").Append(RoleplayManager.CdnURL).Append("/ws_resources/images/down-arrow.png\">");
                                    htmlBuilder.Append("</div>");

                                    htmlBuilder.Append("<div data-rank=\"").Append(rank.Value.RankId).Append("\" data-action=\"cross\" class=\"cursor-pointer-r px-1\">");
                                    htmlBuilder.Append("<img src=\"").Append(RoleplayManager.CdnURL).Append("/ws_resources/images/cross.png\">");
                                    htmlBuilder.Append("</div>");
                                }

                                htmlBuilder.Append("</div>");
                                htmlBuilder.Append("</div>");
                            }

                            htmlBuilder.Append("</div>");

                            // User box
                            htmlBuilder.Append("<div class=\"flex flex-wrap -m-1 justify-center\">");

                            foreach (var member in Group.GetAllMembersDict.Where(m => m.Value.UserRank == rank.Value.RankId))
                            {
                                string Name = PolarEnvironment.GetGame().GetClientManager().GetNameById(Convert.ToInt32(member.Value.UserId)) ?? "Desconocido";
                                string Look = PolarEnvironment.GetGame().GetClientManager().GetLookById(Convert.ToInt32(member.Value.UserId)) ?? "";

                                // User info
                                htmlBuilder.Append("<div class=\"bg-dark-4 rounded m-1 group cursor-pointer-r\">");
                                htmlBuilder.Append("<div class=\"m-px relative\">");
                                htmlBuilder.Append("<div class=\"overflow-hidden bg-light-05 rounded-t\" style=\"height: 55px;\">");
                                htmlBuilder.Append("<center><div class=\"figure-H_RWF_0\" style=\"background-image: url(&quot;").Append(RoleplayManager.AVATARIMG).Append(Look).Append("&quot;); width: 64px; height: 110px; margin-top: -20px;\"></div></center>");
                                htmlBuilder.Append("</div>");

                                bool IsMember = Group.IsMember(habbo.Id);
                                bool CanAscDesc = GroupManager.HasJobCommand(Client, "ascdesc");
                                bool CanFire = GroupManager.HasJobCommand(Client, "fire");

                                if (((isAdmin || (IsMember && (CanAscDesc || CanFire))) && member.Value.UserId != habbo.Id) || (habbo.GetPermissions()?.HasRight("corporation_rights") == true))
                                {
                                    htmlBuilder.Append("<div class=\"absolute pin-b bg-dark-5 w-full hidden group-hover:block\" style=\"padding-top: 4px;padding-bottom: 4px;\">");
                                    htmlBuilder.Append("<div class=\"flex justify-around\">");
                                    htmlBuilder.Append("<div data-user=\"").Append(member.Value.UserId).Append("\" data-action=\"up\" class=\"cursor-pointer-r px-1\">");
                                    htmlBuilder.Append("<img src=\"").Append(RoleplayManager.CdnURL).Append("/ws_resources/images/up-arrow.png\">");
                                    htmlBuilder.Append("</div>");
                                    htmlBuilder.Append("<div data-user=\"").Append(member.Value.UserId).Append("\" data-action=\"down\" class=\"cursor-pointer-r px-1\">");
                                    htmlBuilder.Append("<img src=\"").Append(RoleplayManager.CdnURL).Append("/ws_resources/images/down-arrow.png\">");
                                    htmlBuilder.Append("</div>");

                                    if (isAdmin || CanFire || (habbo.GetPermissions()?.HasRight("corporation_rights") == true))
                                    {
                                        htmlBuilder.Append("<div data-user=\"").Append(member.Value.UserId).Append("\" data-action=\"cross\" class=\"cursor-pointer-r px-1\">");
                                        htmlBuilder.Append("<img src=\"").Append(RoleplayManager.CdnURL).Append("/ws_resources/images/cross.png\">");
                                        htmlBuilder.Append("</div>");
                                    }

                                    htmlBuilder.Append("</div>");
                                    htmlBuilder.Append("</div>");
                                }

                                htmlBuilder.Append("</div>");
                                htmlBuilder.Append("<div class=\"text-center py-1\">").Append(Name).Append("</div>");
                                htmlBuilder.Append("</div>");
                            }

                            htmlBuilder.Append("</div>");
                            // End User box
                            htmlBuilder.Append("</div>");
                        }

                        htmlBuilder.Append("</div>");
                        htmlBuilder.Append("</div>");
                        #endregion

                        Socket.SendWS( "compose_business|employees|" + htmlBuilder.ToString());
                    }
                    break;
                #endregion

                #region Rank Tools
                case "rank_tools":
                    {
                        Group Group = GetTargetGroup(Client, Room);
                        if (Group == null)
                            return;

                        var habbo = Client?.GetHabbo();
                        if (habbo == null)
                            return;

                        // Policía no se puede editar
                        if (Group.Name.Contains("Policia"))
                            return;

                        #region Check if is not Admin
                        if (!Group.IsAdmin(habbo.Id) && !(habbo.GetPermissions()?.HasRight("group_management_override") == true))
                            return;
                        #endregion

                        string[] ReceivedData = Data.Split(',');
                        if (ReceivedData.Length < 3)
                            return;

                        if (!int.TryParse(ReceivedData[1], out int GetRank))
                            return;

                        string WSAction = ReceivedData[2];

                        var AllRanks = Group.Ranks.ToList();
                        var AllMembers = Group.GetAllMembersDict;

                        // Validamos si existe el rango
                        if (!AllRanks.Any(x => x.Value.RankId == GetRank))
                            return;

                        switch (WSAction)
                        {
                            case "up":
                                {
                                    int newrank = GetRank + 1;

                                    // Validamos si existe un rango superior. 
                                    if (!AllRanks.Any(x => x.Value.RankId == newrank))
                                        return;

                                    using (var DB = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                                    {
                                        DB.RunQuery("CALL `ModifRankE`(" + GetRank + ", " + newrank + ", " + Group.Id + ");");

                                        // Actualizar en memoria
                                        foreach (var r in AllRanks.Where(x => x.Value.RankId == newrank))
                                            r.Value.RankId = 0;
                                        foreach (var r in AllRanks.Where(x => x.Value.RankId == GetRank))
                                            r.Value.RankId = newrank;
                                        foreach (var r in AllRanks.Where(x => x.Value.RankId == 0))
                                            r.Value.RankId = GetRank;

                                        // Actualizar miembros
                                        foreach (var m in AllMembers.Where(x => x.Value.UserRank == newrank))
                                            m.Value.UserRank = 0;
                                        foreach (var m in AllMembers.Where(x => x.Value.UserRank == GetRank))
                                            m.Value.UserRank = newrank;
                                        foreach (var m in AllMembers.Where(x => x.Value.UserRank == 0))
                                            m.Value.UserRank = GetRank;
                                    }

                                    PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_business", "employees");
                                }
                                break;

                            case "down":
                                {
                                    int newrank = GetRank - 1;

                                    // Validamos si existe un rango inferior. 
                                    if (!AllRanks.Any(x => x.Value.RankId == newrank))
                                        return;

                                    using (var DB = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                                    {
                                        DB.RunQuery("CALL `ModifRankE`(" + GetRank + ", " + newrank + ", " + Group.Id + ");");

                                        // Actualizar en memoria
                                        foreach (var r in AllRanks.Where(x => x.Value.RankId == newrank))
                                            r.Value.RankId = 0;
                                        foreach (var r in AllRanks.Where(x => x.Value.RankId == GetRank))
                                            r.Value.RankId = newrank;
                                        foreach (var r in AllRanks.Where(x => x.Value.RankId == 0))
                                            r.Value.RankId = GetRank;

                                        // Actualizar miembros
                                        foreach (var m in AllMembers.Where(x => x.Value.UserRank == newrank))
                                            m.Value.UserRank = 0;
                                        foreach (var m in AllMembers.Where(x => x.Value.UserRank == GetRank))
                                            m.Value.UserRank = newrank;
                                        foreach (var m in AllMembers.Where(x => x.Value.UserRank == 0))
                                            m.Value.UserRank = GetRank;
                                    }

                                    PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_business", "employees");
                                }
                                break;

                            case "cross":
                                {
                                    // Si tiene un solo rango
                                    if (AllRanks.Count <= 1)
                                        return;

                                    // Si hay miembros en ese rango, los cambiamos al rankid = 1
                                    foreach (var member in AllMembers.Where(x => x.Value.UserRank == GetRank))
                                    {
                                        member.Value.UserRank = 1;
                                    }

                                    // Borramos de DB
                                    using (var DB = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                                    {
                                        DB.RunQuery("CALL `DropRankE`(" + GetRank + ", " + Group.Id + ");");
                                    }

                                    // Borramos del Diccionario
                                    PolarEnvironment.GetGame().GetGroupManager().DeleteGroupRank(Group.Id, GetRank);

                                    // Recalcular rangos superiores
                                    int uprank = GetRank + 1;
                                    foreach (var r in AllRanks.Where(x => x.Value.RankId >= uprank))
                                        r.Value.RankId = r.Value.RankId - 1;

                                    foreach (var m in AllMembers.Where(x => x.Value.UserRank >= uprank))
                                        m.Value.UserRank = m.Value.UserRank - 1;

                                    PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_business", "employees");
                                }
                                break;

                            case "settings":
                                {
                                    GroupRank ThisRank = GroupManager.GetJobRank(Group.Id, GetRank);
                                    if (ThisRank == null)
                                        return;

                                    var htmlBuilder = new StringBuilder();

                                    #region HTML
                                    htmlBuilder.Append("<div class=\"heading\">Nombre del Puesto</div>");
                                    htmlBuilder.Append("<div class=\"flex\">");
                                    htmlBuilder.Append("<input id=\"RankNewName\" type=\"text\" data-lpignore=\"true\" class=\"dark-input-text flex-1 mr-1\" value=\"").Append(ThisRank.Name).Append("\">");
                                    htmlBuilder.Append("</div>");
                                    htmlBuilder.Append("<br>");

                                    htmlBuilder.Append("<div class=\"heading\" style=\"width: 34%;float: left;\">Paga del Puesto</div>");
                                    htmlBuilder.Append("<div class=\"heading\" style=\"width: 45%;float: right;\">Jornada de Tiempo (Minutos)</div>");

                                    htmlBuilder.Append("<div class=\"flex\" style=\"width: 45%;float:left;\">");
                                    htmlBuilder.Append("<input id=\"RankPay\" type=\"number\" data-lpignore=\"true\" class=\"dark-input-text flex-1 mr-1\" min=\"0\" value=\"").Append(ThisRank.Pay).Append("\">");
                                    htmlBuilder.Append("</div>");

                                    htmlBuilder.Append("<br>");
                                    htmlBuilder.Append("<div class=\"flex\">");
                                    htmlBuilder.Append("<button data-rank=\"").Append(GetRank).Append("\" data-action=\"SaveRank\" class=\"dark-button\" style=\"width: 100%;\">Guardar Cambios</button>");
                                    htmlBuilder.Append("</div>");

                                    htmlBuilder.Append("<br>");
                                    htmlBuilder.Append("<div class=\"heading\">Permisos</div>");
                                    htmlBuilder.Append("<div class=\"flex\">");
                                    htmlBuilder.Append("<table class=\"dark\">");
                                    htmlBuilder.Append("<tr class=\"dark2\">");
                                    htmlBuilder.Append("<th class=\"dark2\">Ascender/Descender</th>");
                                    htmlBuilder.Append("<th class=\"dark2\">Contratar</th>");
                                    htmlBuilder.Append("<th class=\"dark2\">Despedir</th>");
                                    htmlBuilder.Append("<th class=\"dark2\">Abastecer</th>");
                                    htmlBuilder.Append("</tr>");
                                    htmlBuilder.Append("<tr class=\"dark2\">");
                                    htmlBuilder.Append("<td class=\"dark2\">");
                                    htmlBuilder.Append("<div data-rank=\"").Append(GetRank).Append("\" data-action=\"ascdesc\" class=\"cursor-pointer-r px-1\">");
                                    htmlBuilder.Append("<img src=\"").Append(RoleplayManager.CdnURL).Append("/ws_resources/images/").Append(ThisRank.HasCommand("ascdesc") ? "up-arrow.png" : "cross.png").Append("\">");
                                    htmlBuilder.Append("</div>");
                                    htmlBuilder.Append("</td>");
                                    htmlBuilder.Append("<td class=\"dark2\">");
                                    htmlBuilder.Append("<div data-rank=\"").Append(GetRank).Append("\" data-action=\"hire\" class=\"cursor-pointer-r px-1\">");
                                    htmlBuilder.Append("<img src=\"").Append(RoleplayManager.CdnURL).Append("/ws_resources/images/").Append(ThisRank.HasCommand("hire") ? "up-arrow.png" : "cross.png").Append("\">");
                                    htmlBuilder.Append("</div>");
                                    htmlBuilder.Append("</td>");
                                    htmlBuilder.Append("<td class=\"dark2\">");
                                    htmlBuilder.Append("<div data-rank=\"").Append(GetRank).Append("\" data-action=\"fire\" class=\"cursor-pointer-r px-1\">");
                                    htmlBuilder.Append("<img src=\"").Append(RoleplayManager.CdnURL).Append("/ws_resources/images/").Append(ThisRank.HasCommand("fire") ? "up-arrow.png" : "cross.png").Append("\">");
                                    htmlBuilder.Append("</div>");
                                    htmlBuilder.Append("</td>");
                                    htmlBuilder.Append("<td class=\"dark2\">");
                                    htmlBuilder.Append("<div data-rank=\"").Append(GetRank).Append("\" data-action=\"supply\" class=\"cursor-pointer-r px-1\">");
                                    htmlBuilder.Append("<img src=\"").Append(RoleplayManager.CdnURL).Append("/ws_resources/images/").Append(ThisRank.HasCommand("supply") ? "up-arrow.png" : "cross.png").Append("\">");
                                    htmlBuilder.Append("</div>");
                                    htmlBuilder.Append("</td>");
                                    htmlBuilder.Append("</tr>");
                                    htmlBuilder.Append("</table>");
                                    htmlBuilder.Append("</div>");
                                    htmlBuilder.Append("<br>");
                                    #endregion

                                    string Male = !string.IsNullOrEmpty(ThisRank.MaleFigure) ? "." + ThisRank.MaleFigure : "";
                                    string Female = !string.IsNullOrEmpty(ThisRank.FemaleFigure) ? "." + ThisRank.FemaleFigure : "";

                                    string SendData = htmlBuilder.ToString() + "|" + Male + "|" + Female + "|" + GetRank;
                                    Socket.SendWS( "compose_business|settings|" + SendData);
                                }
                                break;
                        }
                    }
                    break;
                #endregion

                #region Member Tools
                case "member_tools":
                    {
                        Group Group = GetTargetGroup(Client, Room);
                        if (Group == null)
                            return;

                        var habbo = Client?.GetHabbo();
                        if (habbo == null)
                            return;

                        string[] ReceivedData = Data.Split(',');
                        if (ReceivedData.Length < 3)
                            return;

                        if (!int.TryParse(ReceivedData[1], out int GetUserId))
                            return;

                        string WSAction = ReceivedData[2];

                        // Validamos si existe el usuario
                        if (!Group.IsMember(GetUserId) && !Group.IsAdmin(GetUserId))
                            return;

                        bool isAdmin = Group.IsAdmin(habbo.Id) || Group.IsMember(habbo.Id) ||
                                       (habbo.GetPermissions()?.HasRight("group_management_override") == true);
                        bool IsMember = Group.IsMember(habbo.Id);
                        bool CanAscDesc = GroupManager.HasJobCommand(Client, "ascdesc");
                        bool CanFire = GroupManager.HasJobCommand(Client, "fire");

                        // Check permissions based on action
                        switch (WSAction)
                        {
                            case "up":
                            case "down":
                                if (!Client.GetHabbo().GetPermissions().HasRight("corporation_rights"))
                                {
                                    if (Group.Members[GetUserId].UserId == habbo.Id)
                                        return;

                                    if ((!isAdmin && !(IsMember && CanAscDesc)) || Group.Members[GetUserId].UserId == habbo.Id)
                                        return;
                                }
                                break;

                            case "cross":
                                if (!Client.GetHabbo().GetPermissions().HasRight("corporation_rights"))
                                {
                                    if (Group.Members[GetUserId].UserId == habbo.Id)
                                        return;

                                    if ((!isAdmin && !(IsMember && CanFire)) || Group.Members[GetUserId].UserId == habbo.Id)
                                        return;
                                }
                                break;
                        }

                        switch (WSAction)
                        {
                            case "up":
                                {
                                    int GetRank = Group.Members[GetUserId].UserRank;
                                    int newrank = GetRank + 1;
                                    List<GroupMember> Administrators = Group.Members.Values.Where(x => x.IsAdmin).OrderBy(x => x.UserId).ToList();

                                    if (newrank >= 6 && Administrators.Count > 0)
                                    {
                                        Client.SendWhisper("¡No pueden haber dos Administradores en una misma Empresa!", 1);
                                        return;
                                    }

                                    if (newrank >= 6 && Administrators.Count <= 0 && !habbo.GetPermissions().HasRight("corporation_rights"))
                                    {
                                        Client.SendWhisper("No puedes asignar como Fundador a un empleado. ¡Debe comprar la empresa!", 1);
                                        return;
                                    }

                                    // Validamos si existe un rango superior. 
                                    if (!Group.Ranks.Any(x => x.Value.RankId == newrank))
                                        return;

                                    using (var DB = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                                    {
                                        DB.RunQuery("CALL `ModifMemberE`(" + GetRank + ", " + newrank + ", " + Group.Id + ", " + GetUserId + ");");
                                        Group.Members[GetUserId].UserRank = newrank;
                                    }

                                    #region Mensaje de aviso al Target
                                    GameClient TargetSession = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(GetUserId);
                                    if (TargetSession != null)
                                    {
                                        RoleplayManager.Shout(TargetSession, "*Ha sido ascendid@ de puesto en su trabajo " + Group.Name + "*", 5);
                                        TargetSession.SendWhisper("¡Buenas noticias! Has sido ascendid@ de puesto.", 1);

                                        if (TargetSession.GetRoleplay().IsWorking)
                                            TargetSession.SendWhisper("Puedes dejar de trabajar y :trabajar cuando gustes para recibir tu nuevo puesto.", 1);

                                        if (Group.IsAdmin(GetUserId))
                                        {
                                            TargetSession.GetRoleplay().JobId = Group.Id;
                                        }
                                    }
                                    #endregion

                                    PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_business", "employees");
                                }
                                break;

                            case "down":
                                {
                                    int GetRank = Group.Members[GetUserId].UserRank;
                                    int newrank = GetRank - 1;

                                    if (Group.IsAdmin(GetUserId) && !habbo.GetPermissions().HasRight("corporation_rights"))
                                    {
                                        Client.SendWhisper("No puedes bajar de rango al Propietario de la Empresa");
                                        return;
                                    }

                                    // Validamos si existe un rango inferior. 
                                    if (!Group.Ranks.Any(x => x.Value.RankId == newrank))
                                        return;

                                    using (var DB = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                                    {
                                        DB.RunQuery("CALL `ModifMemberE`(" + GetRank + ", " + newrank + ", " + Group.Id + ", " + GetUserId + ");");
                                        Group.Members[GetUserId].UserRank = newrank;
                                    }

                                    #region Mensaje de aviso al Target
                                    GameClient TargetSession = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(GetUserId);
                                    if (TargetSession != null)
                                    {
                                        RoleplayManager.Shout(TargetSession, "*Ha sido degradad@ de puesto en su trabajo " + Group.Name + "*", 5);
                                        TargetSession.SendWhisper("Has sido degradad@ de puesto.", 1);

                                        if (TargetSession.GetRoleplay().IsWorking)
                                        {
                                            WorkManager.RemoveWorkerFromList(TargetSession);
                                            TargetSession.GetRoleplay().IsWorking = false;
                                            TargetSession.GetHabbo().Poof();
                                        }
                                    }
                                    #endregion

                                    PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_business", "employees");
                                }
                                break;

                            case "cross":
                                {
                                    if (Group.IsAdmin(GetUserId) && !habbo.GetPermissions().HasRight("corporation_rights"))
                                    {
                                        Client.SendWhisper("No puedes despedir al Propietario de la Empresa");
                                        return;
                                    }

                                    GameClient TargetSession = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(GetUserId);

                                    // Si están ON, validamos si se encuentra trabajando.
                                    if (TargetSession != null && TargetSession.GetRoleplay().IsWorking)
                                    {
                                        WorkManager.RemoveWorkerFromList(TargetSession);
                                        TargetSession.GetRoleplay().IsWorking = false;
                                        TargetSession.GetHabbo().Poof();
                                    }

                                    string ExtraInf = Group.IsAdmin(GetUserId)
                                        ? "Se te ha retirado el Cargo Fundador en " + Group.Name
                                        : "Se te ha retirado el trabajo de " + Group.Name;

                                    if (Group.IsAdmin(GetUserId))
                                        Group.TakeAdmin(GetUserId);

                                    if (Group.IsMember(GetUserId))
                                    {
                                        if (TargetSession != null)
                                        {
                                            TargetSession.GetRoleplay().TimeWorked = 0;
                                            TargetSession.GetRoleplay().JobId = 1;
                                            TargetSession.GetRoleplay().JobRank = 1;
                                            TargetSession.GetRoleplay().JobRequest = 0;

                                            var Job = GroupManager.GetJob(TargetSession.GetRoleplay().JobId);
                                            if (Job != null)
                                            {
                                                Job.AddNewMember(TargetSession.GetHabbo().Id);
                                                Job.SendPackets(TargetSession);
                                            }
                                        }
                                        Group.DeleteMember(GetUserId);
                                    }

                                    // Si está ON, recibe alerta.
                                    if (TargetSession != null)
                                    {
                                        if (!string.IsNullOrEmpty(ExtraInf))
                                            TargetSession.SendNotification(ExtraInf);

                                        PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(TargetSession, "event_group", "open");
                                        RoleplayManager.Shout(TargetSession, "*Ha sido despedido del trabajo " + Group.Name + "*", 5);
                                    }

                                    PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_business", "employees");
                                }
                                break;
                        }
                    }
                    break;
                #endregion

                #region Requests
                case "requests":
                    {
                        Group Group = GetTargetGroup(Client, Room);
                        if (Group == null)
                            return;

                        var habbo = Client?.GetHabbo();
                        if (habbo == null)
                            return;

                        #region Check permissions
                        bool hasPermission = Group.IsAdmin(habbo.Id) ||
                                           Group.IsMember(habbo.Id) ||
                                           GroupManager.HasJobCommand(Client, "hire") ||
                                           (habbo.GetPermissions()?.HasRight("corporation_rights") == true);

                        if (!hasPermission)
                            return;
                        #endregion

                        // Convertir GetRequests a List<int>
                        var AllRequest = new List<int>();
                        foreach (var request in Group.GetRequests)
                        {
                            if (int.TryParse(request.ToString(), out int userId))
                            {
                                AllRequest.Add(userId);
                            }
                        }

                        var htmlBuilder = new StringBuilder();

                        #region HTML
                        htmlBuilder.Append("<div>");
                        htmlBuilder.Append("<div class=\"-m-2\">");
                        htmlBuilder.Append("<div class=\"m-2\">");
                        htmlBuilder.Append("<div class=\"heading relative group\">");
                        htmlBuilder.Append("<div>Solicitudes (").Append(AllRequest.Count).Append(")</div>");
                        htmlBuilder.Append("</div>");

                        if (AllRequest.Count > 0)
                        {
                            // Obtener todos los datos en una sola query
                            var requestsData = GetRequestsData(Group.Id, AllRequest); // Ahora pasamos List<int>

                            foreach (var userId in AllRequest)  // userId ya es int
                            {
                                string Name = PolarEnvironment.GetGame().GetClientManager().GetNameById(userId) ?? "Desconocido";
                                string Look = PolarEnvironment.GetGame().GetClientManager().GetLookById(userId) ?? "";

                                requestsData.TryGetValue(userId, out DataRow row);

                                htmlBuilder.Append("<div class=\"flex flex-wrap -m-1 justify-center\">");
                                htmlBuilder.Append("<div class=\"bg-dark-4 rounded m-1 group cursor-pointer-r\" style=\"width: 23%;\">");
                                htmlBuilder.Append("<div class=\"m-px relative\">");
                                htmlBuilder.Append("<div class=\"overflow-hidden bg-light-05 rounded-t\">");
                                htmlBuilder.Append("<center><div class=\"figure-H_RWF_0\" style=\"background-image: url(&quot;").Append(RoleplayManager.AVATARIMG).Append(Look).Append("&quot;); width: 64px; height: 110px; margin-top: -20px;\"></div></center>");
                                htmlBuilder.Append("</div>");
                                htmlBuilder.Append("<div class=\"absolute pin-b bg-dark-5 w-full group-hover:block\" style=\"padding-top: 4px;padding-bottom: 4px;\">");
                                htmlBuilder.Append("<div class=\"flex justify-around\">");
                                htmlBuilder.Append("<div data-user=\"").Append(userId).Append("\" data-action=\"accept\" class=\"cursor-pointer-r px-1\">");
                                htmlBuilder.Append("<img src=\"").Append(RoleplayManager.CdnURL).Append("/ws_resources/images/up-arrow.png\">");
                                htmlBuilder.Append("</div>");
                                htmlBuilder.Append("<div data-user=\"").Append(userId).Append("\" data-action=\"decline\" class=\"cursor-pointer-r px-1\">");
                                htmlBuilder.Append("<img src=\"").Append(RoleplayManager.CdnURL).Append("/ws_resources/images/cross.png\">");
                                htmlBuilder.Append("</div>");
                                htmlBuilder.Append("</div>");
                                htmlBuilder.Append("</div>");
                                htmlBuilder.Append("</div>");
                                htmlBuilder.Append("<div class=\"text-center py-1\">").Append(Name).Append("</div>");
                                htmlBuilder.Append("</div>");

                                htmlBuilder.Append("<div class=\"bg-dark-4 rounded m-1 group\" style=\"width: 68%;padding: 5px;\">");
                                htmlBuilder.Append("<div style=\"max-height: 108px;overflow: auto;\">");
                                htmlBuilder.Append("<b>Pa&iacute;s: </b>");
                                htmlBuilder.Append("<div class=\"bsn_s_country\" style=\"display: inline-block;\">").Append(row?["ws_region"]?.ToString() ?? "N/A").Append("</div>");
                                htmlBuilder.Append("<br>");
                                htmlBuilder.Append("<b>Horas Libres: </b>");
                                htmlBuilder.Append("<div class=\"bsn_s_hours\" style=\"display: inline-block;\">").Append(row?["ws_hours"]?.ToString() ?? "0").Append("</div> Hr(s)");
                                htmlBuilder.Append("<br>");
                                htmlBuilder.Append("<b>M&aacute;s Informaci&oacute;n:</b><br>");
                                htmlBuilder.Append("<div class=\"bsn_s_info\" style=\"display: inline-block;\">");
                                htmlBuilder.Append(row?["ws_desc"]?.ToString() ?? "Sin información");
                                htmlBuilder.Append("</div>");
                                htmlBuilder.Append("</div>");
                                htmlBuilder.Append("</div>");
                                htmlBuilder.Append("</div>");
                            }
                        }
                        else
                        {
                            htmlBuilder.Append("<center>No hay solicitudes nuevas.</center>");
                        }

                        htmlBuilder.Append("</div>");
                        htmlBuilder.Append("</div>");
                        htmlBuilder.Append("</div>");
                        #endregion

                        Socket.SendWS( "compose_business|requests|" + htmlBuilder.ToString());
                    }
                    break;
                #endregion

                #region Request Tools
                case "request_tools":
                    {
                        Group Group = GetTargetGroup(Client, Room);
                        if (Group == null)
                            return;

                        var habbo = Client?.GetHabbo();
                        if (habbo == null)
                            return;

                        #region Check permissions
                        bool hasPermission = Group.IsAdmin(habbo.Id) ||
                                           Group.IsMember(habbo.Id) ||
                                           GroupManager.HasJobCommand(Client, "hire") ||
                                           (habbo.GetPermissions()?.HasRight("corporation_rights") == true);

                        if (!hasPermission)
                            return;
                        #endregion

                        string[] ReceivedData = Data.Split(',');
                        if (ReceivedData.Length < 3)
                            return;

                        if (!int.TryParse(ReceivedData[1], out int GetUserId))
                            return;

                        string WSAction = ReceivedData[2];

                        if (!Group.HasRequest(GetUserId))
                            return;

                        switch (WSAction)
                        {
                            case "accept":
                                {
                                    GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(GetUserId);

                                    // Obtenemos los Trabajos del Target
                                    List<Group> Jobs = PolarEnvironment.GetGame().GetGroupManager().GetJobsForUser(GetUserId);
                                    if (Jobs == null)
                                    {
                                        Client.SendWhisper("Ocurrió un problema al buscar los Trabajos de esa persona. Intentalo más tarde.", 1);
                                        return;
                                    }

                                    if (Group.Name.Contains("Policia"))
                                    {
                                        List<Group> Groups = PolarEnvironment.GetGame().GetGroupManager().GetGangsForUser(GetUserId);
                                        if (Groups != null && Groups.Count > 0)
                                        {
                                            Client.SendWhisper("¡Esa persona es miembro de una banda! No puede ser contratada como policía.", 1);
                                            return;
                                        }
                                    }

                                    // Si está On
                                    if (TargetClient != null)
                                    {
                                        #region DirectJoin
                                        if (Group.GroupType == GroupType.LOCKED)
                                        {
                                            if (!Group.HasRequest(GetUserId))
                                                return;

                                            Habbo targetHabbo = PolarEnvironment.GetHabboById(GetUserId);
                                            if (targetHabbo == null)
                                            {
                                                Client.SendNotification("Oops, ha ocurrido un problema al buscar al usuario, es probable que se haya desconectado. ¡El proceso lo dejó en la Lista de Solicitudes de Empleo!");
                                                return;
                                            }

                                            Group.HandleRequest(GetUserId, true);
                                            Client.SendMessage(new GroupMemberUpdatedComposer(Group.Id, targetHabbo, 4));
                                        }
                                        #endregion

                                        // Actualizamos Información del Rank del User
                                        TargetClient.GetRoleplay().JobId = Group.Id;
                                        TargetClient.GetRoleplay().JobRank = 1;
                                        Group.UpdateJobMember(TargetClient.GetHabbo().Id);

                                        PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(TargetClient, "event_group", "open");
                                        RoleplayManager.Shout(Client, "*Contrata a " + PolarEnvironment.GetUsernameById(GetUserId) + " en " + Group.Name + "*", 5);
                                        RoleplayManager.Shout(TargetClient, "*Ha sido contratado en " + Group.Name + "*", 5);
                                        TargetClient.SendNotification("¡Felicitaciones! Han aceptado tu solicitud de empleo en " + Group.Name);
                                    }
                                    // Si está Off
                                    else
                                    {
                                        Group.HandleRequest(GetUserId, true);
                                        RoleplayManager.Shout(Client, "*Contrata a " + PolarEnvironment.GetUsernameById(GetUserId) + " en " + Group.Name + "*", 5);
                                    }

                                    PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_business", "requests");
                                }
                                break;

                            case "decline":
                                {
                                    Group.HandleRequest(GetUserId, false);
                                    PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_business", "requests");
                                }
                                break;
                        }
                    }
                    break;
                #endregion

                #region Manage
                case "manage":
                    {
                        Group Group = GetTargetGroup(Client, Room);
                        if (Group == null)
                            return;

                        var habbo = Client?.GetHabbo();
                        if (habbo == null)
                            return;

                        // Empresas oficiales del RP no son gestionables desde client. Solo DB.
                        #region Check if is not Admin
                        if (!Group.IsAdmin(habbo.Id) && !(habbo.GetPermissions()?.HasRight("group_management_override") == true))
                            return;
                        #endregion

                        string[] ReceivedData = Data.Split(',');
                        if (ReceivedData.Length < 2)
                            return;

                        string WSAction = ReceivedData[1];
                        string DataString = "";

                        if (WSAction != "open" && ReceivedData.Length > 2)
                        {
                            DataString = ReceivedData[2];

                            if (string.IsNullOrWhiteSpace(DataString))
                                return;
                        }

                        switch (WSAction)
                        {
                            case "open":
                                Socket.SendWS( "compose_business|manage|");
                                break;

                            case "addrank":
                                if (DataString.Length > 50)
                                {
                                    Client.SendWhisper("El nombre del puesto es demasiado largo.", 1);
                                    return;
                                }
                                if (Group.Ranks.Count >= 8)
                                {
                                    Socket.SendWS( "compose_gang|msg_error|Límite de 8 rangos alcanzados.");
                                    return;
                                }

                                int NewRank = Group.Ranks.Count + 1;
                                string[] workrooms = Group.RoomId.ToString().Split(',');
                                Group.AddRank(Group.Id, NewRank, DataString, "", "", 5, null, workrooms, 0);
                                Socket.SendWS( "compose_business|manage|");
                                break;

                            case "savelogo":
                                if (DataString.Length > 255)
                                {
                                    Client.SendWhisper("La URL del logo es demasiado larga.", 1);
                                    return;
                                }

                                Group.UpdateJobBadge(DataString);
                                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_business", "open_room");
                                Socket.SendWS( "compose_business|manage|");
                                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_group", "open");
                                break;
                        }
                    }
                    break;
                #endregion

                #region Edit Rank
                case "editrank":
                    {
                        Group Group = GetTargetGroup(Client, Room);
                        if (Group == null)
                            return;

                        var habbo = Client?.GetHabbo();
                        if (habbo == null)
                            return;

                        // Empresas policía no se puede editar en rangos
                        if (Group.Name.Contains("Policia"))
                            return;

                        #region Check if is not Admin
                        if (!Group.IsAdmin(habbo.Id) && !(habbo.GetPermissions()?.HasRight("group_management_override") == true))
                            return;
                        #endregion

                        string[] ReceivedData = Data.Split(',');
                        if (ReceivedData.Length < 3)
                            return;

                        if (!int.TryParse(ReceivedData[2], out int GetRank))
                            return;

                        string WSAction = ReceivedData[1];

                        // Validamos si existe el rango
                        if (!Group.Ranks.Any(x => x.Value.RankId == GetRank))
                            return;

                        switch (WSAction)
                        {
                            case "saverank":
                                if (ReceivedData.Length < 6)
                                    return;

                                string RankName = ReceivedData[3];
                                if (string.IsNullOrWhiteSpace(RankName))
                                {
                                    Client.SendWhisper("Ese nombre de puesto no es válido.", 1);
                                    return;
                                }
                                if (RankName.Length > 50)
                                {
                                    Client.SendWhisper("Ese nombre de puesto es demasiado largo.", 1);
                                    return;
                                }

                                if (!int.TryParse(ReceivedData[4], out int RankPay) || !int.TryParse(ReceivedData[5], out int RankTimer))
                                {
                                    Client.SendWhisper("La paga y el Tiempo de trabajo deben ser números enteros.", 1);
                                    return;
                                }

                                if (RankPay < 0)
                                {
                                    Client.SendWhisper("La paga no puede ser negativa.", 1);
                                    return;
                                }

                                if (RankTimer < 5)
                                {
                                    Client.SendWhisper("El Tiempo de trabajo mínimo es de 5 minutos.", 1);
                                    return;
                                }

                                Group.UpdateJobSettings(GetRank, RankName, RankPay, RankTimer);
                                Client.SendWhisper("Cambios guardados satisfactoriamente.", 1);
                                break;

                            case "savelook":
                                if (ReceivedData.Length < 4)
                                    return;

                                string NewLook = ReceivedData[3];
                                if (string.IsNullOrWhiteSpace(NewLook))
                                {
                                    Client.SendWhisper("Uniforme inválido.", 1);
                                    return;
                                }

                                try
                                {
                                    string[] LookPart = NewLook.Split('=');
                                    string Gender = LookPart.Length > 1 ? LookPart[1] : "";
                                    string[] GetParts = NewLook.Split('&');
                                    string Figure = GetParts.Length > 0 ? GetParts[0] : "";

                                    if ((Gender != "M" && Gender != "F") || string.IsNullOrWhiteSpace(Figure))
                                    {
                                        Client.SendWhisper("Uniforme inválido.", 1);
                                        return;
                                    }

                                    Group.UpdateJobLooks(GetRank, Figure, Gender);
                                    Client.SendWhisper("Ropa guardada satisfactoriamente.", 1);
                                }
                                catch (Exception)
                                {
                                    Client.SendWhisper("((No se pudo obtener la información del Uniforme))", 1);
                                }
                                break;

                            case "permissions":
                                if (ReceivedData.Length < 4)
                                    return;

                                string TypeCMD = ReceivedData[3];
                                if (TypeCMD != "ascdesc" && TypeCMD != "hire" && TypeCMD != "fire" && TypeCMD != "supply")
                                    return;

                                Group.UpdateJobCommads(GetRank, TypeCMD);
                                Client.SendWhisper("Permisos guardados satisfactoriamente.", 1);
                                break;
                        }
                    }
                    break;
                #endregion

                #region Create (comentado)
                case "create":
                    // Código comentado se mantiene igual
                    break;
                #endregion

                #region Supply (comentado)
                case "supply":
                    // Código comentado se mantiene igual
                    break;
                #endregion

                #region Finance List
                case "finance":
                    {
                        if (Client.GetRoleplay().ViewCorpId <= 0)
                            return;

                        Group Group = PolarEnvironment.GetGame().GetGroupManager().GetJobByID(Client.GetRoleplay().ViewCorpId);
                        if (Group == null)
                            return;

                        var habbo = Client?.GetHabbo();
                        if (habbo == null)
                            return;

                        bool isAdmin = Group.IsAdmin(habbo.Id) ||
                                      (habbo.GetPermissions()?.HasRight("group_management_override_finances") == true);

                        var htmlBuilder = new StringBuilder();
                        htmlBuilder.Append("<div>");
                        htmlBuilder.Append("<div class=\"mb-2\">");
                        htmlBuilder.Append("<div class=\"heading\">Donar a la Empresa</div>");
                        htmlBuilder.Append("<div class=\"-m-1 flex flex-wrap justify-around\" style=\"margin-bottom: 5px;display:flex;\">");
                        htmlBuilder.Append("<input id=\"Bus_Input_Donation\" type=\"number\" data-lpignore=\"true\" class=\"dark-input-text flex-1 mr-1\" value=\"\" autocomplete=\"off\" placeholder=\"Cantidad de dinero a donar\">");
                        htmlBuilder.Append("<button id=\"Bus_Btn_Donation\" class=\"dark-button\">Donar</button></div>");

                        if (isAdmin)
                        {
                            htmlBuilder.Append("<div class=\"heading\">Retirar de la Empresa (Solo Fundador)</div>");
                            htmlBuilder.Append("<div class=\"-m-1 flex flex-wrap justify-around\" style=\"margin-bottom: 5px;display:flex;\">");
                            htmlBuilder.Append("<input id=\"Bus_Input_Withdraw\" type=\"number\" data-lpignore=\"true\" class=\"dark-input-text flex-1 mr-1\" value=\"\" autocomplete=\"off\" placeholder=\"Cantidad de dinero a retirar\">");
                            htmlBuilder.Append("<button id=\"Bus_Btn_Withdraw\" class=\"dark-button\">Retirar</button></div>");
                        }

                        htmlBuilder.Append("</div>");
                        htmlBuilder.Append("</div>");

                        Socket.SendWS( "compose_business|finance|" + htmlBuilder.ToString());
                    }
                    break;
                #endregion

                #region Finance Buttons
                case "finan":
                    {
                        if (Client.GetRoleplay().TryGetCooldown("finan", true))
                            return;

                        if (Client.GetRoleplay().ViewCorpId <= 0)
                            return;

                        Group Group = PolarEnvironment.GetGame().GetGroupManager().GetJobByID(Client.GetRoleplay().ViewCorpId);
                        if (Group == null)
                            return;

                        var habbo = Client?.GetHabbo();
                        if (habbo == null)
                            return;

                        bool isAdmin = Group.IsAdmin(habbo.Id) ||
                                      (habbo.GetPermissions()?.HasRight("group_management_override_finances") == true);

                        string[] ReceivedData = Data.Split(',');
                        if (ReceivedData.Length < 3)
                            return;

                        string act = Regex.Replace(ReceivedData[1], "<(.|\\n)*?>", string.Empty);
                        if (!int.TryParse(ReceivedData[2], out int cant))
                        {
                            Socket.SendWS( "compose_business|msg_error|Cantidad de dinero inválida.");
                            return;
                        }

                        switch (act)
                        {
                            case "donation":
                                List<GroupMember> Administrators = Group.Members.Values.Where(x => x.IsAdmin).OrderBy(x => x.UserId).ToList();
                                if (habbo.Rank < 3 && Administrators.Count <= 0)
                                {
                                    Socket.SendWS( "compose_business|msg_error|Esta empresa pertenece al Gobierno y no necesita donaciones.");
                                    return;
                                }

                                if (Client.GetRoleplay().Level < 2)
                                {
                                    Socket.SendWS( "compose_business|msg_error|Necesitas ser al menos nivel 2 para donar.");
                                    return;
                                }

                                if (habbo.Credits < cant)
                                {
                                    Socket.SendWS( "compose_business|msg_error|No tienes esa cantidad de dinero para donar.");
                                    return;
                                }

                                if (cant < 100)
                                {
                                    Socket.SendWS( "compose_business|msg_error|Debes donar al menos una cantidad de $100.");
                                    return;
                                }

                                habbo.Credits -= cant;
                                habbo.UpdateCreditsBalance();
                                Group.Balance += cant;
                                Group.SetBussines(Group.Balance);

                                RoleplayManager.Shout(Client, "*Ha donado $ " + string.Format("{0:N0}", cant) + " a la empresa " + Group.Name + "*", 5);
                                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_business", "finance");
                                Socket.SendWS( "compose_business|msg_success|Donación realizada exitosamente.");
                                break;

                            case "withdraw":
                                Administrators = Group.Members.Values.Where(x => x.IsAdmin).OrderBy(x => x.UserId).ToList();
                                if (Administrators.Count <= 0)
                                {
                                    Socket.SendWS( "compose_business|msg_error|Esta empresa pertenece al Gobierno y no es posible retirar dinero.");
                                    return;
                                }

                                if (Group.Name.Contains("Policia") && !habbo.GetPermissions().HasRight("group_management_override_finances"))
                                {
                                    Socket.SendWS( "compose_business|msg_error|No tienes permitido retirar dinero de esta empresa.");
                                    return;
                                }

                                if (!isAdmin)
                                {
                                    Socket.SendWS( "compose_business|msg_error|Solo el fundador de la empresa puede hacer eso.");
                                    return;
                                }

                                if (Group.Balance < cant)
                                {
                                    Socket.SendWS( "compose_business|msg_error|La empresa no cuenta con esa cantidad en su Banco para retirar.");
                                    return;
                                }

                                if (cant <= 0)
                                {
                                    Socket.SendWS( "compose_business|msg_error|Debes retirar una cantidad mayor a $0.");
                                    return;
                                }

                                habbo.Credits += cant;
                                habbo.UpdateCreditsBalance();
                                Group.Balance -= cant;
                                Group.SetBussines(Group.Balance);

                                RoleplayManager.Shout(Client, "*Ha retirado $ " + string.Format("{0:N0}", cant) + " de la empresa " + Group.Name + "*", 5);
                                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_business", "finance");
                                Group.AddLog(habbo.Id, habbo.Username + " retira $" + cant, cant);
                                Socket.SendWS( "compose_business|msg_success|Retiro realizado exitosamente.");
                                break;
                        }

                        Client.GetRoleplay().CooldownManager.CreateCooldown("finan", 1000, 30);
                    }
                    break;
                    #endregion
            }
        }

        #region Métodos auxiliares
        private Group GetTargetGroup(GameClient Client, Room Room)
        {
            if (Client == null)
                return null;

            if (Client.GetRoleplay().ViewMyCorp)
            {
                if (Client.GetRoleplay().JobId <= 0)
                    return null;

                return PolarEnvironment.GetGame().GetGroupManager().GetJobByID(Client.GetRoleplay().JobId);
            }
            else
            {
                if (Room.Group == null || Room.Group.GType != 2) // GType 1 = Empresa
                    return null;

                return Room.Group;
            }
        }

        private Dictionary<int, DataRow> GetRequestsData(int groupId, List<int> requestIds)  // Cambiado de List<string> a List<int>
        {
            var requestsData = new Dictionary<int, DataRow>();

            if (requestIds == null || requestIds.Count == 0)
                return requestsData;

            using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                // Crear los nombres de parámetros dinámicamente
                var paramNames = new List<string>();
                for (int i = 0; i < requestIds.Count; i++)
                {
                    paramNames.Add($"@id{i}");
                }

                // Construir la consulta SQL correctamente
                string query = $"SELECT * FROM `rp_jobs_requests` WHERE `job_id` = @groupId AND `user_id` IN ({string.Join(",", paramNames)})";

                dbClient.SetQuery(query);
                dbClient.AddParameter("groupId", groupId);

                // Agregar cada parámetro
                for (int i = 0; i < requestIds.Count; i++)
                {
                    dbClient.AddParameter($"id{i}", requestIds[i]);
                }

                var dataTable = dbClient.getTable();
                if (dataTable != null)
                {
                    foreach (DataRow row in dataTable.Rows)
                    {
                        int userId = Convert.ToInt32(row["user_id"]);
                        requestsData[userId] = row;
                    }
                }
            }

            return requestsData;
        }
        #endregion

        // ── Helper: envía texto como frame WebSocket usando ConnectionInformation
        private static void SendWS(ConnectionInformation socket, string message)
        {
            if (socket == null || string.IsNullOrEmpty(message)) return;
            socket.SendData(System.Text.Encoding.UTF8.GetBytes(message));
        }

    }
}