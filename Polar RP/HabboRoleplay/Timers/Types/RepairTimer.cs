using System;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.Pathfinding;
using Polar.Database.Interfaces;
using System.Linq;
using Polar.HabboHotel.Rooms;
using System.Drawing;
using Polar.HabboHotel.Items;
using Polar.Core;
using Polar.HabboHotel.Users.Effects;

namespace Polar.HabboRoleplay.Timers.Types
{
    /// <summary>
    /// Fence Repair timer
    /// </summary>
    public class RepairTimer : RoleplayTimer
    {
        public RepairTimer(string Type, GameClient Client, int Time, bool Forever, object[] Params)
            : base(Type, Client, Time, Forever, Params)
        {
            // 30 seconds convert to milliseconds
            TimeLeft = 30000;
        }

        /// <summary>
        /// Fence Repair timer
        /// </summary>
        public override void Execute()
        {
            try
            {
                if (base.Client == null || base.Client.GetHabbo() == null || base.Client.GetRoleplay() == null || base.Client.GetRoomUser() == null || base.Client.GetRoomUser().GetRoom() == null || !JailbreakManager.FenceBroken)
                {
                    base.EndTimer();
                    return;
                }

                int ItemId = (int)Params[0];
                Item BTile = base.Client.GetRoomUser().GetRoom().GetRoomItemHandler().GetItem(ItemId);
                if (!RoleplayManager.GenerateRoom(base.Client.GetHabbo().CurrentRoomId, out Room Room))
                    return;

                string MyCity = Room.City;

                HabboRoleplay.RPRoom.RPRoom Data;
                int ToJail = PolarEnvironment.GetGame().GetRPRoomManager().TryToGetJail(MyCity, out Data);
                int ToJailback = PolarEnvironment.GetGame().GetRPRoomManager().TryToGetJailBack(MyCity, out Data);


                if (BTile == null || BTile.Coordinate != base.Client.GetRoomUser().Coordinate || Room.RoomId != Convert.ToInt32(ToJail))
                {
                    if (base.Client.GetRoomUser().CurrentEffect == EffectsList.SunnyD)
                        base.Client.GetRoomUser().ApplyEffect(EffectsList.None);

                    RoleplayManager.Shout(base.Client, "*Detiene la reparación de la valla*", 4);
                    base.EndTimer();
                    return;
                }

                TimeLeft -= 1000;

                if (TimeLeft > 0)
                    return;

                if (base.Client.GetRoomUser().CurrentEffect == EffectsList.SunnyD)
                    base.Client.GetRoomUser().ApplyEffect(EffectsList.None);

                RoleplayManager.Shout(base.Client, "*Reparación exitosa de la valla [REVISA LOS LUGARES CERCANOS EN BUSCA DE FUGITIVOS Y REGRESALOS A LA PRISIÓN]*", 4);
                JailbreakManager.JailbreakActivated = false;
                JailbreakManager.FenceBroken = false;
                JailbreakManager.GenerateFence(Room);
                base.EndTimer();
                return;
            }
            catch (Exception e)
            {
                Logging.LogRPTimersError("Error in Execute() void: " + e);
                base.EndTimer();
            }
        }
    }
}