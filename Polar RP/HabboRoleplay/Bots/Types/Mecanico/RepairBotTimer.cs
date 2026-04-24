using System;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboRoleplay.Bots;
using Polar.HabboHotel.Items;
using Polar.HabboRoleplay.Food;
using System.Linq;
using System.Drawing;
using Polar.HabboHotel.Pathfinding;
using Polar.HabboHotel.Rooms.Pathfinding;

namespace Polar.HabboRoleplay.Timers.Types
{
    /// <summary>
    /// Serving timer
    /// </summary>
    public class RepairBotTimer : BotRoleplayTimer
    {
        public RepairBotTimer(string Type, RoleplayBot CachedBot, int Time, bool Forever, object[] Params) 
            : base(Type, CachedBot, Time, Forever, Params)
        {
            TimeCount = 0;
        }
 
        /// <summary>
        /// Begins serving food/drink process
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

                GameClient Client = (GameClient)Params[0];
                Food.Food Food = (Food.Food)Params[1];
                Point ServePoint = (Point)Params[2];
                Point UserPoint = (Point)Params[3];
                string RealName = (string)Params[4];

                bool StatCheck = false;
                if (base.CachedBot.DRoomUser.GetBotRoleplay().AIType == RoleplayBotAIType.DRINKSERVER)
                {
                    if (Client.GetRoleplay().CurEnergy < Client.GetRoleplay().MaxEnergy)
                    if (Client.GetRoleplay().CurAlcohol < Client.GetRoleplay().MaxAlcohol)
                            StatCheck = true;
                }
                else
                {
                    if (Client.GetRoleplay().Hunger > 0)
                        StatCheck = true;
                }

                if (Client != null && Client.GetRoomUser() != null && Client.GetRoleplay() != null && !Client.LoggingOut && StatCheck)
                {
                    if (Client.GetRoomUser().Coordinate != UserPoint)
                    {
                        base.CachedBot.DRoomUser.GetBotRoleplay().WalkingToItem = false;
                        InitiateGoHome();
                        base.EndTimer();
                    }

                    if (base.CachedBot.DRoomUser.Coordinate != ServePoint)
                        return;

                    int Rot = Rotation.Calculate(Client.GetRoomUser().Coordinate.X, Client.GetRoomUser().Coordinate.Y, base.CachedBot.DRoomUser.Coordinate.X, base.CachedBot.DRoomUser.Coordinate.Y);
                    base.CachedBot.DRoomUser.SetRot(Rot, false);
                    base.CachedBot.DRoomUser.Chat("Aqui tienes " + Client.GetHabbo().Username + " Espero que disfrutes de tu " + RealName + ".", true);

                    BeginPlacingFoodFurni(Food, Client, UserPoint);
                    InitiateGoHome();
                }

                base.CachedBot.DRoomUser.GetBotRoleplay().WalkingToItem = false;
                InitiateGoHome();
                base.EndTimer();
            }
            catch
            {
                base.EndTimer();
            }
        }

        public void BeginPlacingFoodFurni(Food.Food Food, GameClient Client, Point UserPoint)
        {
            if (Client == null)
                return;

            if (Client.GetRoomUser() == null)
                return;

            if (base.CachedBot.DRoom == null)
                return;

            double MaxHeight = 0.0;

            Item ItemInFront;
            if (base.CachedBot.DRoom.GetGameMap().GetHighestItemForSquare(Client.GetRoomUser().SquareInFront, out ItemInFront))
            {
                if (ItemInFront != null)
                    MaxHeight = ItemInFront.TotalHeight;
            }
            base.CachedBot.DRoomUser.SetRot(Client.GetRoomUser().RotBody, false);
            RoleplayManager.PlaceItemToRoom(Client, Food.ItemId, 0, Client.GetRoomUser().SquareInFront.X, Client.GetRoomUser().SquareInFront.Y, MaxHeight, Client.GetRoomUser().RotBody, false, base.CachedBot.DRoom.Id, false, Food.ExtraData);
        }

        public void InitiateGoHome()
        {
            if (!base.CachedBot.DRoomUser.GetBotRoleplayAI().OnDuty)
                return;

            var Point = new Point(base.CachedBot.DRoomUser.GetBotRoleplay().oX, base.CachedBot.DRoomUser.GetBotRoleplay().oY);

            if (base.CachedBot.DRoomUser.Coordinate != Point)
                base.CachedBot.DRoomUser.MoveTo(Point);
        }
    }
}