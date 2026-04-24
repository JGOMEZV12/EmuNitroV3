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
    class MearCommand : IChatCommand
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
            get { return "Orina en alguna calle"; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions

            if (Session.GetRoleplay().TryGetCooldown("mear"))
                return;

            if (Session.GetRoleplay().IsDead)
            {
                Session.SendWhisper("¡No puedes mear si estas muerto!", 1);
                return;
            }

            if (Session.GetRoleplay().Poop > 100)
            {
                Session.SendWhisper("¡No tienes ganas de mear, ni cagar!", 1);
                return;
            }

            if (Room.ShootEnabled)
            {
                Session.SendWhisper("¿Que te sucede? No puedes hacer esto aquí, ve a la calle o usa un inodoro", 1);
                return;
            }

            if (Session.GetRoleplay().EquippedWeapon != null)
            {
                Session.SendWhisper("¿Como piensas cagar/mear con las manos ocupadas? GUARDA EL ARMA", 1);
                return;
            }
            #endregion

            #region Execute
            Session.Shout("* Saca sus partes intimas y comienza hacer sus necesidades [-30 Ganas de ir] [-50 Higiene] por no lavarse las manos* ", 19);
            Session.GetRoleplay().Poop += 30;
            Session.GetRoleplay().Hygiene -= 50;
            #endregion
        }
    }
}