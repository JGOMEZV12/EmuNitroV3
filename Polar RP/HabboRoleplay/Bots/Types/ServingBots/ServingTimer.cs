using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Pathfinding;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Pathfinding;
using Polar.HabboRoleplay.Bots;
using Polar.HabboRoleplay.Bots.Types;
using Polar.HabboRoleplay.Food;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.RoleplayUsers;
using System;
using System.Drawing;

namespace Polar.HabboRoleplay.Timers.Types
{
    public class ServingTimer : BotRoleplayTimer
    {
        private const int GraceTicks = 6;
        private const int MaxWaitTicks = 25;

        private int _graceTicks = 0;
        private int _waitTicks = 0;
        private bool _served = false;

        public bool ServeCompleted { get; private set; } = false;

        public ServingTimer(string Type, RoleplayBot CachedBot, int Time, bool Forever, object[] Params)
            : base(Type, CachedBot, Time, Forever, Params)
        {
            TimeCount = 0;
        }

        public override void Execute()
        {
            try
            {
                if (_served || ServeCompleted) return;

                if (base.CachedBot == null || base.CachedBot.DRoomUser == null || base.CachedBot.DRoom == null)
                { Abort(null); return; }

                GameClient Client = (GameClient)Params[0];
                Food.Food Food = (Food.Food)Params[1];
                Point ServePoint = (Point)Params[2];
                Point UserPoint = (Point)Params[3];
                string RealName = (string)Params[4];

                if (Client == null || Client.LoggingOut ||
                    Client.GetRoleplay() == null || Client.GetRoomUser() == null)
                { Abort(null); return; }

                if (!NeedsFilling(Client))
                { Abort(null); return; }

                if (_graceTicks < GraceTicks)
                { _graceTicks++; return; }

                if (Client.GetRoomUser().Coordinate != UserPoint)
                {
                    Abort(Client, "¡Te has movido de tu sitio! Pide de nuevo cuando estés sentado.");
                    return;
                }

                if (base.CachedBot.DRoomUser.Coordinate != ServePoint)
                {
                    _waitTicks++;
                    if (_waitTicks % 4 == 0)
                        base.CachedBot.DRoomUser.MoveTo(ServePoint);
                    if (_waitTicks >= MaxWaitTicks)
                        Abort(Client, "Lo siento " + Client.GetHabbo().Username + ", no logré llegar. ¡Inténtalo de nuevo!");
                    return;
                }

                _served = true;

                int rot = Rotation.Calculate(
                    base.CachedBot.DRoomUser.Coordinate.X,
                    base.CachedBot.DRoomUser.Coordinate.Y,
                    Client.GetRoomUser().Coordinate.X,
                    Client.GetRoomUser().Coordinate.Y);

                base.CachedBot.DRoomUser.SetRot(rot, false);
                base.CachedBot.DRoomUser.Chat(
                    "Aquí tienes " + Client.GetHabbo().Username +
                    ", espero que disfrutes de tu " + RealName + ".", true);

                BeginPlacingFoodFurni(Food, Client);

                Client.GetRoomUser().OnChat(Client.GetRoomUser().LastBubble, "¡Gracias! ", false, string.Empty);

                // IMPORTANTE: Liberar flag ANTES de GoHome
                if (base.CachedBot?.DRoomUser != null)
                    base.CachedBot.DRoomUser.GetBotRoleplay().WalkingToItem = false;

                GoHome();

                ServeCompleted = true;
                base.EndTimer();
            }
            catch (Exception ex)
            {
                Polar.Core.Logging.LogException(ex.ToString());
                if (base.CachedBot?.DRoomUser != null)
                    base.CachedBot.DRoomUser.GetBotRoleplay().WalkingToItem = false;
                GoHome();
                ServeCompleted = true;
                base.EndTimer();
            }
        }

        private bool NeedsFilling(GameClient Client)
        {
            var rp = Client.GetRoleplay();
            if (base.CachedBot.DRoomUser.GetBotRoleplay().AIType == RoleplayBotAIType.DRINKSERVER)
                return rp.CurEnergy < rp.MaxEnergy || rp.CurAlcohol < rp.MaxAlcohol;
            return rp.Hunger > 0;
        }

        public void BeginPlacingFoodFurni(Food.Food Food, GameClient Client)
        {
            if (Client?.GetRoomUser() == null || base.CachedBot.DRoom == null) return;

            double maxHeight = 0.0;
            if (base.CachedBot.DRoom.GetGameMap()
                    .GetHighestItemForSquare(Client.GetRoomUser().SquareInFront, out Item itemInFront))
            {
                if (itemInFront != null)
                    maxHeight = itemInFront.TotalHeight;
            }

            base.CachedBot.DRoomUser.SetRot(Client.GetRoomUser().RotBody, false);
            RoleplayManager.PlaceItemToRoom(
                Client, Food.ItemId, 0,
                Client.GetRoomUser().SquareInFront.X,
                Client.GetRoomUser().SquareInFront.Y,
                maxHeight,
                Client.GetRoomUser().RotBody,
                false, base.CachedBot.DRoom.Id, false, Food.ExtraData, true);
        }

        public void GoHome()
        {
            try
            {
                if (base.CachedBot?.DRoomUser == null) return;

                var botRp = base.CachedBot.DRoomUser.GetBotRoleplay();
                if (botRp == null) return;

                if (base.CachedBot.DRoomUser.X != botRp.oX || base.CachedBot.DRoomUser.Y != botRp.oY)
                {
                    base.CachedBot.DRoomUser.MoveTo(botRp.oX, botRp.oY, false);
                }
            }
            catch { }
        }

        private void Whisper(GameClient Client, string Message)
        {
            if (base.CachedBot?.DRoomUser == null) return;
            Client.SendMessage(new WhisperComposer(base.CachedBot.DRoomUser.VirtualId, Message, 0, 2));
        }

        private void Abort(GameClient Client, string Message = null)
        {
            if (Client != null && Message != null)
                Whisper(Client, Message);

            if (base.CachedBot?.DRoomUser != null)
                base.CachedBot.DRoomUser.GetBotRoleplay().WalkingToItem = false;

            GoHome();

            ServeCompleted = true;
            base.EndTimer();
        }
    }
}