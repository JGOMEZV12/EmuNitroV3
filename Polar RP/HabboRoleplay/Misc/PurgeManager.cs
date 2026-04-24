using System;
using System.Data;
using System.Collections.Concurrent;
using System.Collections.Generic;
using log4net;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Items;
using Polar.Core;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.HabboHotel.Items.Data.Moodlight;
using Polar.HabboHotel.GameClients;
using System.Linq;

namespace Polar.HabboRoleplay.Misc
{
    public static class PurgeManager
    {
        
        /// <summary>
        /// Set the payday checks
        /// </summary>
        public static void SetTime(int TimeCount)
        {
            foreach (GameClient client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
            {
                if (client == null || client.GetHabbo() == null || client.GetRoleplay() == null)
                    continue;
                int TimeLeft = RoleplayManager.PurgeTime - TimeCount;
                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(client, "event_purge", "timer," + TimeLeft);
            }
        }
    }
}