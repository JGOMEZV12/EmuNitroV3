using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Notifications;
using Polar.HabboHotel.GameClients;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Gangs
{
    class GangBackupCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_gang_backup"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Envía un mensaje a los miembros de su pandilla que solicitan refuerzos."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Variables
            Group Gang = GroupManager.GetGang(Session.GetRoleplay().GangId);
            GroupRank GangRank = GroupManager.GetGangRank(Session.GetRoleplay().GangId, Session.GetRoleplay().GangRank);
            #endregion

            #region Conditions
            if (Gang == null)
            {
                Session.SendWhisper("¡No eres parte de ninguna pandilla!", 1);
                return;
            }

            if (Gang.Id <= 1000)
            {
                Session.SendWhisper("¡No eres parte de ninguna pandilla!", 1);
                return;
            }

            if (!GroupManager.HasGangCommand(Session, "gbackup"))
            {
                Session.SendWhisper("¡No tienes permiso para usar este comando!", 1);
                return;
            }

            if (Session.GetRoleplay().TryGetCooldown("gbackup"))
                return;
            #endregion

            #region Execute
            foreach (int Member in Gang.Members.Keys)
            {
                GameClient Client = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Member);

                if (Client == null)
                    continue;

                Client.SendWhisper("[PANDILLA] " + Session.GetHabbo().Username + " Necesita refuerzo en " + Room.Name + " [RoomID: " + Room.RoomId + "] ¡Ayudalo!", 34);
            }

            Session.GetRoleplay().CooldownManager.CreateCooldown("gbackup", 1000, 5);
            #endregion
        }
    }
}