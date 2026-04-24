using System;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboRoleplay.Events
{
    public interface IEvent
    {
        /// <summary>
        /// Responds to the event
        /// </summary>
        void Execute(object Source, object[] Params);
    }
}