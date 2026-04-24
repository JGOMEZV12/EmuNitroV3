using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Polar.Utilities;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Pathfinding;
using Polar.HabboHotel.Items.Crafting;
using Polar.Communication.Packets.Outgoing.Inventory.Furni;
using Polar.HabboHotel.Users.Effects;
using MoreLinq;

namespace Polar.HabboHotel.Items.Interactor
{
    public class InteractorMineria : IFurniInteractor
    {
        public void OnPlace(GameClient Session, Item Item) { }

        public void OnRemove(GameClient Session, Item Item) { }

        public void OnTrigger(GameClient Session, Item Item, int Request, bool HasRights)
        {
            // FIX: Validar también GetHabbo() antes de usarlo
            if (Session == null || Session.GetHabbo() == null)
                return;

            RoomUser User = Item.GetRoom().GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);
            if (User == null)
                return;

            if (!Gamemap.TilesTouching(Item.GetX, Item.GetY, User.Coordinate.X, User.Coordinate.Y))
            {
                if (Item.ExtraData == "0" || Item.ExtraData == "1")
                    if (User.CanWalk)
                        User.MoveTo(Item.SquareInFront);
                return;
            }

            if (Session.GetHabbo().Credits < 10)
            {
                Session.SendWhisper("¡No tiene 10$ para poder minar!", 1);
                return;
            }

            if (!Session.GetRoleplay().NearItem("prison_stone", 1))
            {
                Session.SendWhisper("¡Debes estar frente a las piedras!");
                return;
            }
            else
            {
                if (Item.ExtraData == "")
                    Item.ExtraData = "0";

                if (Item.ExtraData == "0")
                {
                    int Minutes = 1;

                    User.ClearMovement(true);
                    User.SetRot(Rotation.Calculate(User.Coordinate.X, User.Coordinate.Y, Item.GetX, Item.GetY), false);

                    Item.ExtraData = "1";
                    Item.UpdateState(false, true);
                    Item.RequestUpdate(120 * Minutes, true);
                    Session.SendWhisper("*Comienza a picar la roca, en busca de minerales y ganancias [-10$ Costo de producción]*", 27);
                    Session.GetHabbo().Credits -= 10;
                    Session.GetHabbo().UpdateCreditsBalance();

                    new Thread(() =>
                    {
                        User.CanWalk = false;

                        if (User.CurrentEffect != 10 && Session.GetRoleplay().EquippedWeapon == null)
                            User.ApplyEffect(594);

                        Thread.Sleep(5000);

                        if (User.CurrentEffect != 10 && Session.GetRoleplay().EquippedWeapon == null)
                            User.ApplyEffect(0);

                        if (Session != null && Session.GetRoleplay() != null && Session.GetHabbo() != null)
                            ChooseReward(Session);
                        if (User != null)
                            User.CanWalk = true;
                    }).Start();
                }
                else
                    Session.SendWhisper("¡ESPERE POR FAVOR...! mientras se recuperan los minerales de la mina", 1);
            }
        }

        public void OnWiredTrigger(Item Item) { }

        public void ChooseReward(GameClient Session)
        {
            var Random = new CryptoRandom();
            int TotalCraftingItems = CraftingManager.CraftableItems.Count;
            int Chance = Random.Next(1, 4);
            int SecondChance = Random.Next(1, 4);

            if (SecondChance < 4 && Chance > TotalCraftingItems)
                Chance = Random.Next(1, TotalCraftingItems + 1);

            else if (Chance > 1 && Chance <= 7)
            {
                int Amount = Random.Next(10, 200);
                Session.GetHabbo().Credits += Amount;
                Session.GetHabbo().UpdateCreditsBalance();
                Session.SendWhisper("*¡Felicidades ganaste! $" + Amount + " por encontrar minerales en esta roca*", 27);
            }
            else
            {
                Session.SendWhisper("*¡La roca que minaste no contiene nada!*", 4);
            }
        }
    }
}
