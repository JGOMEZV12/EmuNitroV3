using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Polar.Utilities;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.Items.Crafting;
using Polar.HabboHotel.Rooms.Pathfinding;
using Polar.HabboHotel.Pathfinding;
using Polar.HabboHotel.Users.Effects;
using MoreLinq;

namespace Polar.HabboHotel.Items.Interactor
{
    public class InteractorTragamonedas : IFurniInteractor
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

            // FIX: La validación y el cobro ahora son consistentes (ambos 1000$)
            if (Session.GetHabbo().Credits < 1000)
            {
                Session.SendWhisper("¡No tiene 1000$ para jugar!", 1);
                return;
            }

            if (Session.GetRoleplay().IsWorkingOut)
            {
                Session.SendWhisper("Deja de trabajar para poder usar la máquina tragamonedas", 1);
                return;
            }

            if (!Session.GetRoleplay().NearItem("bling11_slot", 1))
            {
                Session.SendWhisper("¡Debes estar frente a la máquina tragamonedas!");
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
                    Item.RequestUpdate(50 * Minutes, true);
                    Session.SendWhisper("*Hala la palanca de la máquina tragamonedas y espera resultado... [-1000$]*", 4);
                    Session.GetHabbo().Credits -= 1000;
                    Session.GetHabbo().UpdateCreditsBalance();

                    new Thread(() =>
                    {
                        User.CanWalk = false;

                        if (User.CurrentEffect != 10 && Session.GetRoleplay().EquippedWeapon == null)
                            User.ApplyEffect(EffectsList.Twinkle);

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
                    Session.SendWhisper("¡ESPERE POR FAVOR...! Cuando la máquina cambie de color puede tirar de ella", 1);
            }
        }

        public void OnWiredTrigger(Item Item) { }

        public void ChooseReward(GameClient Session)
        {
            var Random = new CryptoRandom();
            int TotalCraftingItems = CraftingManager.CraftableItems.Count;
            int Chance = Random.Next(1, 5);
            int SecondChance = Random.Next(1, 5);

            if (SecondChance < 4 && Chance > TotalCraftingItems)
                Chance = Random.Next(1, TotalCraftingItems + 1);

            else if (Chance > 1 && Chance <= 6)
            {
                int Amount = Random.Next(50, 2000);
                Session.GetHabbo().Credits += Amount;
                Session.GetHabbo().UpdateCreditsBalance();
                Session.SendWhisper("*¡Felicidades ganaste! $" + Amount + " en la máquina tragamonedas*", 4);
                RoleplayManager.GiveMoneyToCompany(12, Session, "casino", true, 1000);
            }
            else
            {
                Session.SendWhisper("*¡PERDISTE! La máquina tragamonedas se ha comido tu dinero*", 4);
                // FIX: GiveMoneyToCompany movido dentro del else para que solo se llame al perder
                RoleplayManager.GiveMoneyToCompany(23, Session, "casino", true, 1000);
            }
        }
    }
}
