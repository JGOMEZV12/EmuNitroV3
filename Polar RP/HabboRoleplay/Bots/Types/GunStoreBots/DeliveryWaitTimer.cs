using System;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Bots;
using Polar.HabboHotel.Items;
using System.Linq;
using System.Threading;
using Polar.HabboRoleplay.Bots.Manager;
using System.Drawing;

namespace Polar.HabboRoleplay.Timers.Types
{
    public class DeliveryWaitTimer : BotRoleplayTimer
    {
        public bool DeliveryArrived { get; set; }

        public DeliveryWaitTimer(string Type, RoleplayBot CachedBot, int Time, bool Forever, object[] Params)
            : base(Type, CachedBot, Time, Forever, Params)
        {
            TimeCount  = 0;
            TimeCount2 = 0;
            this.DeliveryArrived = false;
        }

        public override void Execute()
        {
            try
            {
                if (base.CachedBot == null || base.CachedBot.DRoomUser == null || base.CachedBot.DRoom == null
                    || !RoleplayManager.CalledDelivery || RoleplayManager.DeliveryWeapon == null)
                {
                    base.EndTimer();
                    return;
                }

                TimeCount++;
                if (TimeCount < 1000) return;

                if (!this.DeliveryArrived)
                {
                    RoleplayBot Bot = RoleplayBotManager.GetCachedBotByAI(RoleplayBotAIType.DELIVERY);
                    if (Bot == null)
                    {
                        base.CachedBot.DRoomUser.Chat("El bot de entrega está ocupado en este momento, ¡lo siento!", true);
                        base.EndTimer();
                        return;
                    }

                    Item Item = null;
                    Bot.GetStopWorkItem(base.CachedBot.DRoom, out Item);
                    if (Item == null)
                    {
                        base.CachedBot.DRoomUser.Chat("El bot de entrega está ocupado en este momento, ¡lo siento!", true);
                        base.EndTimer();
                        return;
                    }

                    RoleplayBotManager.DeployBotByAI(RoleplayBotAIType.DELIVERY, "workitem", base.CachedBot.DRoom.Id);
                    this.DeliveryArrived = true;
                }

                if (base.CachedBot.DRoom.GetRoomItemHandler().GetFloor
                    .Where(x => x.GetBaseItem().InteractionType == InteractionType.DELIVERY_BOX).ToList().Count <= 0)
                    return;

                TimeCount2++;
                if (TimeCount2 < 200) return;

                RoleplayManager.CalledDelivery = false;
                HandleDelivery();
                base.EndTimer();
            }
            catch
            {
                base.EndTimer();
            }
        }

        public void HandleDelivery()
        {
            if (!base.CachedBot.DRoomUser.GetBotRoleplayAI().OnDuty) return;
            if (base.CachedBot.DRoom == null) return;

            // FIX Bug 3: null check ANTES de usar Item
            var Item = base.CachedBot.DRoom.GetRoomItemHandler().GetFloor
                .FirstOrDefault(x => x.GetBaseItem().Id == 8029);

            if (Item == null) return;

            var Point = new System.Drawing.Point(Item.SquareBehind.X, Item.SquareBehind.Y);
            object[] Params = { Point, Item };

            base.CachedBot.DRoomUser.Chat("Es hora de recibir la entrega", true);
            base.CachedBot.DRoomUser.GetBotRoleplay().WalkingToItem = true;
            base.CachedBot.DRoomUser.MoveTo(Point);
            base.CachedBot.DRoomUser.GetBotRoleplay().TimerManager
                .CreateTimer("pickupdelivery", CachedBot, 10, true, Params);
        }
    }
}