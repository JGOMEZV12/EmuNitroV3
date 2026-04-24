using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Games;
using Polar.HabboHotel.Rooms.Games.Teams;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Developers
{
    class EnableCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_enable"; }
        }

        public string Parameters
        {
            get { return "%effectid%"; }
        }

        public string Description
        {
            get { return "Changes your current enable effect to the chosen effect id."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("You must enter an effect ID!", 1);
                return;
            }

            if (!Room.EnablesEnabled && !Session.GetHabbo().GetPermissions().HasRight("mod_tool"))
            {
                Session.SendWhisper("Oops, it appears that the room owner has disabled the ability to use the enable command in here.", 1);
                return;
            }

            RoomUser ThisUser = Session.GetRoomUser();
            if (ThisUser == null)
                return;

            if (ThisUser.RidingHorse)
            {
                Session.SendWhisper("You cannot enable effects whilst riding a horse!", 1);
                return;
            }
            else if (ThisUser.isLying)
            {
                Session.SendWhisper("You cannot use this command while laying down!", 1);
                return;
            }

            int EffectId = 0;
            if (!int.TryParse(Params[1], out EffectId))
            {
                Session.SendWhisper("Please enter a number!", 1);
                return;
            }

            if (EffectId > int.MaxValue || EffectId < int.MinValue)
            {
                Session.SendWhisper("Please enter a valid effect id!", 1);
                return;
            }

            ThisUser.ApplyEffect(EffectId);
        }
    }
}
