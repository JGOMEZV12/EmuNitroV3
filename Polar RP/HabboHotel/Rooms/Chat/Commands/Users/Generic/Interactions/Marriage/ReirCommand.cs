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
    class ReirCommand : IChatCommand
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
            get { return "Comienza a reir."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            if (Session.GetRoleplay().CurEnergy <= 15)
            {
                Session.SendWhisper("* no tienes energía siquiera para reir *", 1);
                return;
            }

            if (Session.GetRoleplay().Animo < 0)
            {
                Session.SendWhisper("¿Como te vas a reir si estás triste? haz algo divertido para subir tu animo", 1);
                return;
            }

            if (Session.GetRoleplay().Animo >= 100)
            {
                Session.SendWhisper("Ya estás feliz", 1);
                return;
            }
            #endregion

            #region Execute
            Session.SendWhisper("¡Acabas de subir tu felicidad!", 1);
            Session.GetRoleplay().CurEnergy -= 25;
            Session.GetRoomUser().ApplyEffect(907);
            Session.GetRoleplay().Animo += 50;
            Session.Shout("*jajajajajajajajajaja ¡Que risa!", 4);
            #endregion
        }
    }
}