using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Groups;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Weapons;
using System.Drawing;
using Polar.HabboHotel.Users.Effects;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Interactions.Basic
{
    class MasturbarseCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_interactions_kiss"; }
        }

        public string Parameters
        {
            get { return "%username%"; }
        }

        public string Description
        {
            get { return "Saca tus genitales y comienza a masturbarte"; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions

            if (Session.GetRoleplay().TryGetCooldown("masturbarse"))
                return;

            if (Session.GetRoleplay().IsDead)
            {
                Session.SendWhisper("¡No puedes masturbarte si estas muerto!", 1);
                return;
            }

            if (Session.GetRoleplay().EquippedWeapon != null)
            {
                Session.SendWhisper("¿Como piensas masturbarte con las manos ocupadas? GUARDA EL ARMA", 1);
                return;
            }

            if (Session.GetRoleplay().PassiveMode)
            {
                Session.SendWhisper("No puedes realizar acciones sexuales en modo pasivo.", 1);
                return;
            }
            #endregion

            #region Execute
            if (Session.GetHabbo().Gender.ToLower().StartsWith("m"))
            {
                Session.Shout("*Agarra su pene y comienza a masturbarlo [-5 Energía] [-20 Higiene]*", 16);
            }
            if (Session.GetHabbo().Gender.ToLower().StartsWith("f"))
            {
                Session.Shout("*Comienza a tocar con los dedos su vagina [-5 Energía] [-20 Higiene]*", 16);
            }
            Session.GetRoleplay().Hygiene -= 50;
            Session.GetRoleplay().SexTimer = 15;
            Session.GetRoomUser().ApplyEffect(507);
            #endregion
        }
    }
}