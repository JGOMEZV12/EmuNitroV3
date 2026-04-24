using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Notifications;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Jobs.General
{
    class CorpInfoCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_corp_info"; }
        }

        public string Parameters
        {
            get { return "%corpid%"; }
        }

        public string Description
        {
            get { return "Información sobre la empresa."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Variables
            Group Corp = null;
            GroupRank CorpRank = null;
            #endregion

            #region Conditions
            if (Params.Length == 1)
            {
                Corp = GroupManager.GetJob(Session.GetRoleplay().JobId);
                CorpRank = GroupManager.GetJobRank(Session.GetRoleplay().JobId, Session.GetRoleplay().JobRank);
            }
            else
            {
                int CorpId;
                if (int.TryParse(Params[1], out CorpId))
                {
                    Corp = GroupManager.GetJob(CorpId);
                    CorpRank = GroupManager.GetJobRank(CorpId, 1);
                }
                else
                {
                    Session.SendWhisper("Ingrese un ID corporativo válido!Tipo ':empresas' para todos los cuerpos!", 1);
                    return;
                }
            }

            if (Corp == null)
            {
                Session.SendWhisper("¡Lo sentimos, esta identificación corporativa no existe!", 1);
                return;
            }

            if (Corp.Id <= 1)
            {
                if (Session.GetRoleplay().JobId <= 1)
                    Session.SendWhisper("Usted está actualmente desempleado!", 1);
                else
                    Session.SendWhisper("Lo siento, pero no puedes ver las estadísticas de la organización de desempleados!", 1);
                return;
            }
            #endregion

            #region Execute
            StringBuilder Message = new StringBuilder();
            Message.Append("----- " + Corp.Name + " -----\n\n");
            Message.Append("Descripción: " + Corp.Description + "\n");

            if (Corp.Ranks.ContainsKey(6))
            {
                if (Corp.Members.Values.Where(x => x.UserRank == 6).ToList().Count > 0)
                {
                    List<int> Members = Corp.Members.Values.Where(x => x.UserRank == 6).Select(x => x.UserId).ToList();
                    List<string> Names = Members.Select(x => PolarEnvironment.GetHabboById(x) == null ? "SKIPTHIS" : PolarEnvironment.GetHabboById(x).Username).Where(x => x != "SKIPTHIS").ToList();
                    Message.Append("Creador por: " + String.Join(", ", Names) + "\n\n");
                }
                else
                    Message.Append("Creado por: Nadie\n\n");
            }
            else
                Message.Append("\n");

            foreach (int JobRank in Corp.Ranks.Keys.Where(x => x != 6))
            {
                GroupRank Rank = Corp.Ranks[JobRank];
                if (Corp.Members.Values.Where(x => x.UserRank == JobRank).ToList().Count > 0)
                {
                    List<int> Members = Corp.Members.Values.Where(x => x.UserRank == JobRank).Select(x => x.UserId).ToList();
                    List<string> Names = Members.Select(x => PolarEnvironment.GetHabboById(x) == null ? "SKIPTHIS" : PolarEnvironment.GetHabboById(x).Username).Where(x => x != "SKIPTHIS").ToList();
                    Message.Append(Rank.Name + "(s): " + String.Join(", ", Names) + "\n\n");
                }
                else
                    Message.Append(Rank.Name + "(s): Hay vacantes\n\n");
            }

            Session.SendMessage(new MOTDNotificationComposer(Message.ToString()));
            #endregion
        }
    }
}