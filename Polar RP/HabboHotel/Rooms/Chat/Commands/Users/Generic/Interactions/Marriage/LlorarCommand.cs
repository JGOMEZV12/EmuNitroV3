using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Groups;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Weapons;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Interactions.Marriage
{
    class LlorarCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_interactions_marry"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Comienza a llorar."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            if (Session.GetRoleplay().CurEnergy <= 50)
            {
                Session.SendWhisper("* no tienes energía siquiera para llorar *", 1);
                return;
            }

            if (Session.GetRoleplay().Animo > 0)
            {
                Session.SendWhisper("No puedes llorar más, estás muy triste...", 1);
                return;
            }
            #endregion

            #region Execute
            Session.SendWhisper("¡Acabas de bajar tu felicidad!", 1);

            Session.GetRoleplay().CurEnergy -= 50;
            Session.GetRoleplay().Animo -= 10;
            Session.GetRoomUser().ApplyEffect(911);
            Session.Shout("*Baja su cabeza y comienza a llorar [-10 Animo]", 4);
            #endregion
        }
    }
}