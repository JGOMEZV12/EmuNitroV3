using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Groups;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Weapons;
using System.Drawing;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Interactions.Basic
{
    class MeCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_interactions_me"; }
        }

        public string Parameters
        {
            get { return "%message%"; }
        }

        public string Description
        {
            get { return "mensajes de rol"; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            if (Params.Length == 1)
            {
                Session.SendWhisper("Introduzca un mensaje para enviar.", 1);
                return;
            }

            if (Session.GetRoleplay().TryGetCooldown("me"))
                return;

            #endregion

            #region Execute
            
            string Message = CommandManager.MergeParams(Params, 1);
            string word;
            if (!Session.GetHabbo().GetPermissions().HasRight("word_filter_override"))
                Message = PolarEnvironment.GetGame().GetChatManager().GetFilter().IsUnnaceptableWord(Message, out word) ? "Spam" : Message;

            Session.Shout("*" + Message + "*", 3);
            if (Session == null)
                return;
            if (Session.GetRoleplay() == null)
                return;
            if (Session.GetRoleplay().CooldownManager == null)
                return;
            Session.GetRoleplay().CooldownManager.CreateCooldown("me", 1000, 3);

            #endregion
        }
    }
}