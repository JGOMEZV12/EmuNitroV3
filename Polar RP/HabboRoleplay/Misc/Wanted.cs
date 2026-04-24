using System;
using System.Threading;
using System.Drawing;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Rooms;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using Polar.Communication.Packets.Outgoing.Notifications;
using Polar.Communication.Packets.Outgoing.Navigator;
using Polar.HabboRoleplay.Weapons;
using Polar.Database.Interfaces;

namespace Polar.HabboRoleplay.Misc
{
    public class Wanted
    {
        /// <summary>
        /// User ID of the user who is wanted
        /// </summary>
        public uint UserId;

        /// <summary>
        /// Last seen room name of the wanted user
        /// </summary>
        public string LastSeenRoom;

        /// <summary>
        /// Wanted level assigned to the wanted user
        /// </summary>
        public int WantedLevel;

        /// <summary>
        /// Wanted variable
        /// </summary>
        /// <param name="UserId"></param>
        /// <param name="LastSeenRoom"></param>
        /// <param name="WantedLevel"></param>
        public Wanted(uint UserId, string LastSeenRoom, int WantedLevel)
        {
            this.UserId = UserId;
            this.LastSeenRoom = LastSeenRoom;
            this.WantedLevel = WantedLevel;
        }
    }
}