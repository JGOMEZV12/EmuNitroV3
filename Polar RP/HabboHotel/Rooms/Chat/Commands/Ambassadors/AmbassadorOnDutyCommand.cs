using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Users.Effects;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Ambassadors
{
    class AmbassadorOnDutyCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_ambassadorduty_on"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "trabaja como embajador."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Session.GetRoleplay().AmbassadorOnDuty)
            {
                Session.SendWhisper("Ya estas como embajador", 1);
                return;
            }

            if (Session.GetRoleplay().EquippedWeapon != null)
            {
                Session.SendWhisper("Para trabajar como un embajador debes guardar el arma.", 1);
                return;
            }

            Session.GetRoleplay().AmbassadorOnDuty = true;
            Session.GetRoleplay().IsWorking = false;

            if (Session.GetRoomUser() != null)
            {
                if (Session.GetRoomUser().CurrentEffect != EffectsList.Ambassador)
                    Session.GetRoomUser().ApplyEffect(EffectsList.Ambassador);
            }

            PolarEnvironment.GetGame().GetClientManager().AmbassadorWhisperAlert("Este usuario esta trabajando como embajador", Session);
        }
    }
}
