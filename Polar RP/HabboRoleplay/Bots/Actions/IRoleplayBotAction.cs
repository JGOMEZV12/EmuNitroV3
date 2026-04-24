using Polar.HabboHotel.Rooms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.HabboRoleplay.Bots.Actions
{
    public interface IRoleplayBotAction
    {

        /// <summary>
        /// Starts the RoleplayBotAction
        /// </summary>
        void StartAction();

        /// <summary>
        /// Stops the RoleplayBotAction
        /// </summary>
        void StopAction();

        /// <summary>
        /// Ticks action every second
        /// </summary>
        void TickAction();

        /// <summary>
        /// Callback action tick
        /// </summary>
        /// <param name="State"></param>
        void ContinueAction(object State);

    }
}
