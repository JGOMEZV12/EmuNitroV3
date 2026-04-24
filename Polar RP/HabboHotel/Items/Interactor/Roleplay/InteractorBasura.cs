using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Polar.Utilities;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.Items.Crafting;
using Polar.Communication.Packets.Outgoing.Inventory.Furni;
using Polar.HabboHotel.Users.Effects;
using MoreLinq;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.Rooms.Pathfinding;

namespace Polar.HabboHotel.Items.Interactor
{
    public class InteractorBasura : IFurniInteractor
    {
        private const int MaxTrashCount = 15;
        private const int TrashRequiredForAction = 10;
        private const int MinCraftingChance = 1;
        private const int MaxCraftingChance = 5;
        private const int MinMoneyReward = 1000;
        private const int MaxMoneyReward = 2500;
        private const int DefaultReward = 500;

        public void OnPlace(GameClient session, Item item) { }

        public void OnRemove(GameClient session, Item item) { }

        public void OnTrigger(GameClient session, Item item, int request, bool hasRights)
        {
            if (session == null || session.GetHabbo() == null || session.GetRoomUser() == null)
                return;

            var user = item.GetRoom().GetRoomUserManager().GetRoomUserByHabbo(session.GetHabbo().Id);

            // FIX: Verificar user antes de acceder a user.CanWalk
            if (user == null)
                return;

            if (!Gamemap.TilesTouching(item.GetX, item.GetY, user.Coordinate.X, user.Coordinate.Y))
            {
                if (item.ExtraData == "0" || item.ExtraData == "1")
                {
                    if (user.CanWalk)
                        user.MoveTo(item.SquareInFront);
                }
                return;
            }

            if (session.GetRoleplay().BasuTrashCount < MaxTrashCount)
            {
                session.SendWhisper($"¡Tienes {session.GetRoleplay().BasuTrashCount}/15 para poder descargar la basura necesitas {TrashRequiredForAction}/10!", 1);
                return;
            }

            if (!GroupManager.HasJobCommand(session, "basurero"))
            {
                session.SendWhisper("Sólo un trabajador del basurero puede tirar la basura", 1);
                return;
            }

            if (!session.GetRoleplay().NearItem("hween_c15_rubbish", 1))
            {
                session.SendWhisper("¡Debes estar frente a la pila de basura y hacer clic sobre ella!");
                return;
            }

            if (item.ExtraData == "")
                item.ExtraData = "0";

            if (item.ExtraData == "0")
            {
                int minutes = 1;

                user.ClearMovement(true);
                user.SetRot(Rotation.Calculate(user.Coordinate.X, user.Coordinate.Y, item.GetX, item.GetY), false);

                item.ExtraData = "1";
                item.UpdateState(false, true);
                item.RequestUpdate(50 * minutes, true);

                session.Shout("*Hala palanca del camión y procede a descargar la basura...*", 4);
                session.GetRoleplay().BasuTrashCount -= TrashRequiredForAction;
                session.GetHabbo().UpdateCreditsBalance();

                PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(session,
                    "compose_basurero|" +
                    "showinfo|" +
                    session.GetHabbo().Username + "|" +
                    session.GetHabbo().Username + "|" +
                    session.GetRoleplay().BasuTrashCount + "/15|" +
                    session.GetRoleplay().IsBasuChofer);

                new Thread(() =>
                {
                    user.CanWalk = false;

                    if (user.CurrentEffect != 10 && session.GetRoleplay().EquippedWeapon == null)
                        user.ApplyEffect(10);

                    Thread.Sleep(5000);

                    if (user.CurrentEffect == 10 && session.GetRoleplay().EquippedWeapon == null)
                        user.ApplyEffect(0);

                    if (session != null && session.GetRoleplay() != null && session.GetHabbo() != null)
                        ChooseReward(session);

                    user.CanWalk = true;
                }).Start();
            }
            else
            {
                session.SendWhisper("¡ESPERE POR FAVOR O USE OTRA VERTEDERO...!", 1);
            }
        }

        public void OnWiredTrigger(Item item) { }

        public void ChooseReward(GameClient session)
        {
            var random = new CryptoRandom();
            int totalCraftingItems = CraftingManager.CraftableItems.Count;
            int chance = random.Next(MinCraftingChance, MaxCraftingChance);
            int secondChance = random.Next(MinCraftingChance, MaxCraftingChance);

            if (secondChance < 4 && chance > totalCraftingItems)
                chance = random.Next(1, totalCraftingItems + 1);

            #region Money Reward
            if (chance > 1 && chance <= 6)
            {
                int amount = random.Next(MinMoneyReward, MaxMoneyReward);
                session.GetHabbo().Credits += amount;
                session.GetHabbo().UpdateCreditsBalance();
                session.SendWhisper($"*¡Felicidades ganaste! ${amount} por la basura recolectada*", 4);
            }
            #endregion

            #region No Reward
            else
            {
                session.SendWhisper("*¡Puras porquerías que no podemos reciclar para la comunidad, ten 500$!*", 4);
                session.GetHabbo().Credits += DefaultReward;
                session.GetHabbo().UpdateCreditsBalance();
            }
            #endregion
        }
    }
}
