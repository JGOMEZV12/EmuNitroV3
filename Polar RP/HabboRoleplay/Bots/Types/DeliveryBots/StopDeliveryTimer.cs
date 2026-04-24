using System;
using System.Drawing;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboRoleplay.Bots;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Rooms;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.HabboRoleplay.Bots.Manager;

namespace Polar.HabboRoleplay.Timers.Types
{
    /// <summary>
    /// Stop delivery timer
    /// </summary>
    public class StopDeliveryTimer : BotRoleplayTimer
    {
        public StopDeliveryTimer(string Type, RoleplayBot CachedBot, int Time, bool Forever, object[] Params) 
            : base(Type, CachedBot, Time, Forever, Params)
        {
            TimeCount = 0;
        }
 
        /// <summary>
        /// Stops the delivery bot
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

                Item Item = (Item)Params[0];

                if (Item != null)
                {
                    var Point = new Point(Item.GetX, Item.GetY);
                    if (base.CachedBot.DRoomUser.Coordinate != Point)
                        return;

                    Item.ExtraData = "2";
                    Item.UpdateState(false, true);
                    Item.RequestUpdate(2, true);
                }

                
                RoleplayBotManager.EjectDeployedBot(base.CachedBot.DRoomUser, base.CachedBot.DRoom);
                base.CachedBot.Invisible = true;

                base.EndTimer();
            }
            catch
            {
                base.EndTimer();
            }
        }
    }
}