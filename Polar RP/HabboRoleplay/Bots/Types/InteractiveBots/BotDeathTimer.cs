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
using Polar.HabboHotel.Users.Inventory.Bots;

namespace Polar.HabboRoleplay.Timers.Types
{
    /// <summary>
    /// Death timer
    /// </summary>
    public class BotDeathTimer : BotRoleplayTimer
    {
        public BotDeathTimer(string Type, RoleplayBot CachedBot, int Time, bool Forever, object[] Params) 
            : base(Type, CachedBot, Time, Forever, Params)
        {
            TimeLeft = 1000 * 20;
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

                if (!base.CachedBot.DRoomUser.Frozen)
                    base.CachedBot.DRoomUser.Frozen = true;

                TimeLeft -= 1000;

                if (TimeLeft > 0)
                    return;

                if (base.CachedBot.DRoomUser.Frozen)
                    base.CachedBot.DRoomUser.Frozen = false;

                base.CachedBot.DRoomUser.Chat("*Recupera la conciencia*", true);
                //RoleplayManager.SpawnChairs(null, "val14_wchair", base.CachedBot.DRoomUser);
                RoomUser Bot = RoleplayBotManager.GetDeployedBotById(base.CachedBot.Id);
                base.CachedBot.DRoomUser.GetBotRoleplay().Dead = false;

                if (base.CachedBot.DRoom != null)
                    base.CachedBot.DRoom.SendMessage(new UsersComposer(base.CachedBot.DRoomUser));

                if (base.CachedBot.DRoomUser.GetBotRoleplay().RoamBot)
                    base.CachedBot.MoveRandomly();

                RoleplayBotManager.TransportDeployedBot(Bot, Bot.GetBotRoleplay().OSpawnId, true);

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