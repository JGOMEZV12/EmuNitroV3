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
using Polar.HabboHotel.Users.Effects;
using MoreLinq;
using Polar.HabboHotel.Groups;

namespace Polar.HabboHotel.Items.Interactor
{
    public class InteractorRecolector : IFurniInteractor
    {
        public void OnPlace(GameClient Session, Item Item) { }

        public void OnRemove(GameClient Session, Item Item) { }

        public void OnTrigger(GameClient Session, Item Item, int Request, bool HasRights)
        {
            // FIX: Validar también GetHabbo() antes de usarlo
            if (Session == null || Session.GetHabbo() == null)
                return;

            if (!GroupManager.HasJobCommand(Session, "basurero"))
            {
                Session.SendWhisper("Sólo un trabajador del basurero puede recoger la basura", 1);
                return;
            }

            if (!Session.GetRoleplay().IsWorking)
            {
                Session.SendWhisper("¡Debes estar trabajando para realizar esta acción!", 1);
                return;
            }

            if (Session.GetRoleplay().BasuTrashCount > 15)
            {
                Session.SendWhisper("¡Ya tienes el camión lleno de basura, ve a descargar en la central de desechos!", 1);
                return;
            }

            RoomUser User = Item.GetRoom().GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);
            if (User == null)
                return;

            if (!Gamemap.TilesTouching(Item.GetX, Item.GetY, User.Coordinate.X, User.Coordinate.Y))
            {
                if (Item.ExtraData == "" || Item.ExtraData == "0")
                    if (User.CanWalk)
                        User.MoveTo(Item.SquareInFront);
            }
            else
            {
                if (Item.ExtraData == "")
                    Item.ExtraData = "0";

                if (Item.ExtraData == "0")
                {
                    int Minutes = 5;

                    User.ClearMovement(true);
                    User.SetRot(Rotation.Calculate(User.Coordinate.X, User.Coordinate.Y, Item.GetX, Item.GetY), false);

                    Item.ExtraData = "1";
                    Item.UpdateState(false, true);
                    Item.RequestUpdate(135 * Minutes, true);
                    Session.Shout("*Agarra la basura y comienza a subirla al camión*", 4);

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
                    Session.SendWhisper("¡Vaya, parece que ya el camión de basura paso por acá!", 1);
            }
        }

        public void OnWiredTrigger(Item Item) { }

        public void ChooseReward(GameClient Session)
        {
            var Random = new CryptoRandom();
            int TotalCraftingItems = CraftingManager.CraftableItems.Count;
            int Chance = Random.Next(1, 101);
            int SecondChance = Random.Next(1, 101);

            if (SecondChance < 4 && Chance > TotalCraftingItems)
                Chance = Random.Next(1, TotalCraftingItems + 1);

            #region Drugs
            else if (Chance > TotalCraftingItems && Chance <= 20)
            {
                int Amount;

                if (Chance > 25)
                {
                    Amount = Random.Next(1, 5);
                    Session.GetRoleplay().Cocaine += Amount;
                    Session.GetRoleplay().CurHealth -= 10;
                    Session.GetRoleplay().BasuTrashCount += 1;
                    Session.Shout("*Buscando en la basura consiguió: " + Amount + "g de cocaina pero se cortó con un vidrio [-10 Salud] [+1 basura]*", 4);
                }
                else if (Chance <= 10 && Chance > 5)
                {
                    Amount = Random.Next(1, 20);
                    Session.GetRoleplay().Cigarettes += Amount;
                    Session.GetRoleplay().Hygiene -= 5;
                    Session.GetRoleplay().BasuTrashCount += 1;
                    Session.Shout("*Después de buscar y recoger la basura encuentra: " + Amount + " cigarrillos  [-5 Higiene] [+1 basura]*", 4);
                }
                else
                {
                    Amount = Random.Next(1, 10);
                    Session.GetRoleplay().Weed += Amount;
                    Session.GetRoleplay().Hygiene -= 5;
                    Session.GetRoleplay().BasuTrashCount += 1;
                    Session.Shout("*Buscando en la basura se encontró " + Amount + "g de marihuana  [-5 Higiene] [+1 basura]*", 4);
                }
            }
            #endregion

            #region Money
            // FIX: Condición imposible corregida (Chance > 2 && Chance <= 1 nunca se cumple)
            else if (Chance > 65 && Chance <= 80)
            {
                int Amount = Random.Next(200, 1000);

                Session.GetHabbo().Credits += Amount;
                Session.GetHabbo().UpdateCreditsBalance();
                Session.GetRoleplay().Hygiene -= 10;
                Session.GetRoomUser().ApplyEffect(10);
                Session.GetRoleplay().CurHealth -= 5;
                Session.GetRoleplay().BasuTrashCount += 1;
                Session.Shout("*Ha conseguido: $" + Amount + " pero se cortó con un vidrio roto. [-10 vida] [-5 Higiene] [+1 basura]*", 4);
            }
            #endregion

            #region No Reward
            else
            {
                Session.GetRoleplay().Hygiene -= 5;
                Session.GetRoleplay().BasuTrashCount += 1;
                Session.Shout("*Recoge la basura de manera exitosa [-5 Higiene] [1+ Basura]*", 4);
                Session.SendWhisper("Continua recorriendo la ciudad en busca de más basura");
            }
            #endregion
        }
    }
}
