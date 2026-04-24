using System;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboRoleplay.Bots;
using Polar.HabboRoleplay.Bots.Manager;
using Polar.HabboHotel.Items;
using System.Linq;
using System.Drawing;
using Polar.HabboHotel.Pathfinding;
using System.Threading;
using Polar.HabboHotel.Rooms;
using Polar.Utilities;
using System.Collections.Generic;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;

namespace Polar.HabboRoleplay.Timers.Types
{
    /// <summary>
    /// Death timer
    /// </summary>
    public class BotJailTimer : BotRoleplayTimer
    {
        public BotJailTimer(string Type, RoleplayBot CachedBot, int Time, bool Forever, object[] Params)
            : base(Type, CachedBot, Time, Forever, Params)
        {
            TimeLeft = 1000 * 60;
            TimeCount = 0;
        }

        /// <summary>
        /// Begins death sequence
        /// </summary>
        public override void Execute()
        {
            try
            {

                if (base.CachedBot == null || base.CachedBot.DRoomUser == null || base.CachedBot.DRoom == null)
                {
                    base.EndTimer();
                    return;
                }
                if (!base.CachedBot.DRoomUser.GetBotRoleplay().Dead)
                {
                    base.EndTimer();
                    return;
                }

                if (base.CachedBot.DRoomUser == null)
                    return;

                TimeLeft -= 1000;

                if (TimeLeft > 0)
                    return;

                base.CachedBot.DRoomUser.Chat("*Se libera de la cárcel*", true);
                base.CachedBot.DRoomUser.GetBotRoleplay().Jailed = false;

                RoleplayManager.GenerateRoom(1, out Room RandRoom, false);
                RoleplayBotManager.TransportDeployedBot(CachedBot.DRoomUser, RandRoom.Id, false);

                if (base.CachedBot.DRoomUser.GetBotRoleplay().RoamBot)
                    base.CachedBot.MoveRandomly();

                base.EndTimer();
                return;
            }
            catch
            {
                base.EndTimer();
            }
        }
    }
}