using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.Communication.Packets.Outgoing.Notifications;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Weapons;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.General
{
    class StopTranslateCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_translate_undo"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Detiene la traducción del mensaje."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (!Session.GetHabbo().Translating)
            {
                Session.SendWhisper("Ya tienes la traducción deshabilitada!", 1);
                return;
            }

            Session.SendWhisper("Dejó de traducir desde " + Session.GetHabbo().FromLanguage.ToUpper() + " to " + Session.GetHabbo().ToLanguage.ToUpper() + "!", 1);

            Session.GetHabbo().Translating = false;
            Session.GetHabbo().FromLanguage = "";
            Session.GetHabbo().ToLanguage = "";
        }
    }
}