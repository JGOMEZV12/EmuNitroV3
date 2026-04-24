using System;
using Polar.Utilities;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.Items.Crafting;
using Polar.HabboHotel.Rooms.Pathfinding;
using MoreLinq;
using Polar.HabboRoleplay.Turfs;

namespace Polar.HabboHotel.Items.Interactor
{
    public class InteractorWeedMateria : IFurniInteractor
    {
        public void OnPlace(GameClient Session, Item Item) { }

        public void OnRemove(GameClient Session, Item Item) { }

        public void OnTrigger(GameClient Session, Item Item, int Request, bool HasRights)
        {
            // FIX: No llamar métodos sobre Session si es null
            if (Session == null || Session.GetHabbo() == null || Session.GetRoleplay() == null)
                return;

            if (!Session.GetHabbo().CurrentRoom.TurfEnabled)
            {
                Session.SendWhisper("¡Está sala no es un territorio!", 6);
                return;
            }

            Room Room = Session.GetHabbo().CurrentRoom;
            Turf Turf = PolarEnvironment.GetGame().GetGangTurfsManager().getTurfbyRoom(Session.GetHabbo().CurrentRoomId);
            Item BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "weed_krauter" && x.Coordinate == Session.GetRoomUser().SquareInFront);

            if (Turf == null)
            {
                Session.SendWhisper("¡No estas en ningun territorio!", 6);
                return;
            }

            RoomUser User = Item.GetRoom()?.GetRoomUserManager()?.GetRoomUserByHabbo(Session.GetHabbo().Id);
            if (User == null)
            {
                Session.SendWhisper("¡No se pudo encontrar al usuario en la sala!", 6);
                return;
            }

            if (!Gamemap.TilesTouching(Item.GetX, Item.GetY, User.Coordinate.X, User.Coordinate.Y))
            {
                if (Item.ExtraData == "0" || Item.ExtraData == "1")
                    if (User.CanWalk)
                        User.MoveTo(Item.SquareInFront);
                return;
            }

            if (Session.GetHabbo().Credits < 1000)
            {
                Session.SendWhisper("¡No tienes 1000$ para invertir!", 6);
                return;
            }

            if (Session.GetRoleplay().IsWorkingOut)
            {
                Session.SendWhisper("Deja de trabajar para poder cosechar las semillas de marihuana", 6);
                return;
            }

            if (BTile == null)
            {
                Session.SendWhisper("¡Debes estar frente a la planta de marihuana!", 1);
                return;
            }

            if (string.IsNullOrEmpty(Item.ExtraData))
                Item.ExtraData = "0";

            if (Item.ExtraData == "0")
            {
                int Minutes = 2;

                User.ClearMovement(true);
                User.SetRot(Rotation.Calculate(User.Coordinate.X, User.Coordinate.Y, Item.GetX, Item.GetY), false);

                Item.ExtraData = "1";
                Item.UpdateState(false, true);
                Item.RequestUpdate(1000 * Minutes, true);
                Session.Shout("* Comienza a cuidar la planta y cosechar las semillas [-1000$ Inversión]*", 6);
                Session.SendWhisper("Por favor, espera 2 minutos", 1);
                Session.GetHabbo().Credits -= 1000;
                Session.GetHabbo().UpdateCreditsBalance();

                User.CanWalk = false;
                Session.GetRoleplay().HRidCoordinate = Session.GetRoomUser().SquareInFront;
                Session.GetRoleplay().HRidProcess = Room;
                Session.GetRoleplay().HRidItem = Item;
                Session.GetRoleplay().ProcessWeed = true;
                Session.GetRoleplay().LoadingTimeLeft = RoleplayManager.ProcessWeedTime;
                Session.GetRoleplay().TimerManager.CreateTimer("general", 1000, true);
            }
            else
                Session.SendWhisper("¡ESPERE POR FAVOR, LA PLANTA AÚN NO SE HA RECUPERADO...!", 6);
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
                int Amount = Random.Next(1, 10);
                Session.GetRoleplay().Weedmateria += Amount;
                Session.Shout("*¡Felicidades cosechaste! " + Amount + " semillas de marihuana *", 6);
                Session.SendWhisper("*¡utiliza la mesa para preparar los porros! [TODO ESTO ES ILEGAL]*", 1);
            }
            else
            {
                Session.SendWhisper("*¡Esta planta no tiene semillas que ofrecer! intentalo más tarde*", 1);
            }
        }
    }
}
