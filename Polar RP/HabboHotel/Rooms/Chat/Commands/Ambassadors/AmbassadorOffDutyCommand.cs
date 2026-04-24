using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Users.Effects;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Ambassadors
{
    class AmbassadorOffDutyCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_ambassadorduty_off"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "dejar de trabajar como embajador."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (!Session.GetRoleplay().AmbassadorOnDuty)
            {
                Session.SendWhisper("ya dejaste de trabajar", 1);
                return;
            }

            Session.GetRoleplay().AmbassadorOnDuty = false;

            if (Session.GetRoomUser() != null)
            {
                if (Session.GetRoomUser().CurrentEffect == EffectsList.Ambassador)
                    Session.GetRoomUser().ApplyEffect(EffectsList.None);
            }

            PolarEnvironment.GetGame().GetClientManager().AmbassadorWhisperAlert("Deja de trabajar como embajador", Session);
        }
    }
}
