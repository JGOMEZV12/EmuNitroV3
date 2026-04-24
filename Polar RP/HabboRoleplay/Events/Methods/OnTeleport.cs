using System;
using System.Threading;
using System.Collections.Generic;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Turfs;
using Polar.Communication.Packets.Outgoing.Polls;
using Polar.HabboHotel.Polls;
using System.Linq;

namespace Polar.HabboRoleplay.Events.Methods
{
    /// <summary>
    /// Triggered when the user teleports
    /// </summary>
    public class OnTeleport : IEvent
    {
        /// <summary>
        /// Responds to the event
        /// </summary>
        public void Execute(object Source, object[] Params)
        {
            GameClient Client = (GameClient)Source;

            if (Client == null || Client.GetHabbo() == null)
                return;

            if (Client.GetHabbo()._disconnected)
                return;

            BotInteractionCheck(Client, Params);
        }

        #region Bot Interaction Check
        /// <summary>
        /// Checks for any possible interactions with bots in room
        /// </summary>
        /// <param name="Client"></param>
        /// <param name="Params"></param>
        public void BotInteractionCheck(GameClient Client, object[] Params)
        {
            Room Room = Client.GetHabbo().CurrentRoom;
            if (Room == null) return;

            List<RoomUser> Bots = Room.GetRoomUserManager().GetBotList().ToList();

            foreach (RoomUser Bot in Bots)
            {
                if (!Bot.IsBot)
                    continue;

                if (!Bot.IsRoleplayBot)
                    continue;

                if (!Bot.GetBotRoleplay().Deployed)
                    continue;

                Bot.GetBotRoleplayAI().OnUserUseTeleport(Client, Params);
            }
        }
        #endregion
    }
}