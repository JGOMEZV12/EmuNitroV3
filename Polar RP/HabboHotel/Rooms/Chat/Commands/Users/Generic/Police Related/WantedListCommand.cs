using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.Communication.Packets.Outgoing.Notifications;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Police
{
    class WantedListCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_police_related_wanted_list"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Proporciona una lista de todos los usuarios buscados"; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            StringBuilder Message = new StringBuilder().Append("--- Lista de buscados por la policía ---\n\n Cada estrella representa los delitos que está persona ha cometido o por lo menos uno de ellos.\n\n");

            if (RoleplayManager.WantedList.Count <= 0)
                Message.Append("Nadie está en esta lista por el momento\n");

            lock (RoleplayManager.WantedList.Values)
            {
                foreach (var Wanted in RoleplayManager.WantedList.Values)
                {
                    StringBuilder WantedStar = new StringBuilder();
                    if (Wanted.WantedLevel == 1) WantedStar.Append("¥ Razón: Robo/posesión de armas/acto sexual");
                    if (Wanted.WantedLevel == 2) WantedStar.Append("¥¥ Razón: Homicidio");
                    if (Wanted.WantedLevel == 3) WantedStar.Append("¥¥¥ Razón: Terrorismo");
                    if (Wanted.WantedLevel == 4) WantedStar.Append("¥¥¥¥ Razón: Robo de bovéda");
                    if (Wanted.WantedLevel == 5) WantedStar.Append("¥¥¥¥¥ Razón: Una amenaza global");
                    if (Wanted.WantedLevel == 6) WantedStar.Append("¥¥¥¥¥¥ Razón: Rebelde global");
                    Message.Append(PolarEnvironment.GetHabboById(Convert.ToInt32(Wanted.UserId)).Username + ": " + WantedStar + ".\n");
                    Message.Append("_________________________\nEstá en: [Sala: " + Wanted.LastSeenRoom + "]\n\n");
                }
            }
            Session.SendMessage(new MOTDNotificationComposer(Message.ToString()));
        }
    }
}