using System;
using Polar.Utilities;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.Items.Crafting;
using MoreLinq;
using Polar.HabboRoleplay.Turfs;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.Rooms.Pathfinding;

namespace Polar.HabboHotel.Items.Interactor
{
    internal class InteractorCocaina : IFurniInteractor
    {
        private const int InversionCosto = 15000;
        private const int TiempoFabricacion = 2;
        private const int EfectoCocaina = 546;
        private const string SacoAlcaloideItemName = "xmas_sackdru";
        private const string ExtraDataEnProgreso = "1";
        private const string ExtraDataDisponible = "0";

        public void OnPlace(GameClient session, Item item) { }

        public void OnRemove(GameClient session, Item item) { }

        public void OnTrigger(GameClient session, Item item, int request, bool hasRights)
        {
            if (session?.GetHabbo() == null || session?.GetRoleplay() == null)
                return;

            if (!session.GetHabbo().CurrentRoom.TurfEnabled)
            {
                session.SendWhisper("¡Esta sala no es un territorio!", 6);
                return;
            }

            Room room = session.GetHabbo()?.CurrentRoom;
            Turf turf = room != null ? PolarEnvironment.GetGame().GetGangTurfsManager()?.getTurfbyRoom(session.GetHabbo().CurrentRoomId) : null;
            Group gang = session.GetRoleplay()?.GangId != null ? GroupManager.GetGang(session.GetRoleplay().GangId) : null;
            Item bTile = room?.GetRoomItemHandler()?.GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.Equals(SacoAlcaloideItemName, StringComparison.OrdinalIgnoreCase) && x.Coordinate == session.GetRoomUser()?.SquareInFront);

            if (turf == null)
            {
                session.SendWhisper("¡No estás en ningún territorio!", 6);
                return;
            }

            RoomUser user = item.GetRoom()?.GetRoomUserManager()?.GetRoomUserByHabbo(session.GetHabbo().Id);
            if (user == null)
            {
                session.SendWhisper("¡No se pudo encontrar al usuario en la sala!", 6);
                return;
            }

            if (!Gamemap.TilesTouching(item.GetX, item.GetY, user.Coordinate.X, user.Coordinate.Y))
            {
                if (item.ExtraData == ExtraDataDisponible || item.ExtraData == ExtraDataEnProgreso)
                    if (user.CanWalk)
                        user.MoveTo(item.SquareInFront);
                return;
            }

            if (session.GetHabbo().Credits < InversionCosto)
            {
                session.SendWhisper($"¡No tienes {InversionCosto}$ para invertir!", 6);
                return;
            }

            if (session.GetRoleplay().IsDead)
            {
                session.SendWhisper("¡No puedes hacer esto mientras estás muerto!", 6);
                return;
            }

            if (session.GetRoleplay().IsWorkingOut)
            {
                session.SendWhisper("Deja de trabajar para poder hacer cocaína", 6);
                return;
            }

            if (bTile == null)
            {
                session.SendWhisper("¡Debes pararte frente al saco de alcaloide!", 1);
                return;
            }

            if (string.IsNullOrEmpty(item.ExtraData))
                item.ExtraData = ExtraDataDisponible;

            if (item.ExtraData == ExtraDataDisponible)
                ProcesarFabricacion(session, user, item);
            else
                session.SendWhisper("¡ESPERE POR FAVOR...!", 6);
        }

        private void ProcesarFabricacion(GameClient session, RoomUser user, Item item)
        {
            int minutos = TiempoFabricacion;

            user.ClearMovement(true);
            user.SetRot(Rotation.Calculate(user.Coordinate.X, user.Coordinate.Y, item.GetX, item.GetY), false);

            // FIX: Eliminada la validación redundante — ya se validó en OnTrigger
            item.ExtraData = ExtraDataEnProgreso;
            item.UpdateState(false, true);
            item.RequestUpdate(1000 * minutos, true);

            session.Shout("* Comienza a fabricar el clorhidrato de cocaína [-15.000$ Inversión]*", 6);
            session.SendWhisper("Por favor, espera 2 minutos, no te podrás mover.", 1);
            session.GetHabbo().Credits -= InversionCosto;
            session.GetHabbo().UpdateCreditsBalance();

            user.CanWalk = false;

            session.GetRoleplay().HRidCoordinate = session.GetRoomUser().SquareInFront;
            session.GetRoleplay().HRidProcess = session.GetHabbo().CurrentRoom;
            session.GetRoleplay().HRidItem = item;
            session.GetRoleplay().ProcessCocaine = true;
            session.GetRoleplay().LoadingTimeLeft = RoleplayManager.ProcessCocaineTime;
            session.GetRoleplay().TimerManager.CreateTimer("general", 1000, true);
        }

        public void OnWiredTrigger(Item item) { }

        public void ChooseReward(GameClient session)
        {
            if (session?.GetRoleplay()?.IsDead == true)
            {
                session.SendWhisper("¡No puedes hacer esto mientras estás muerto!", 6);
                return;
            }

            var random = new CryptoRandom();
            int totalCraftingItems = CraftingManager.CraftableItems.Count;
            int chance = random.Next(1, 5);
            int secondChance = random.Next(1, 5);

            if (secondChance < 4 && chance > totalCraftingItems)
                chance = random.Next(1, totalCraftingItems + 1);

            if (chance > 1 && chance <= 6)
            {
                int amount = random.Next(10, 15);
                session.GetRoleplay().Cocaine += amount;
                session.Shout($"*¡Felicidades fabricaste! {amount}g de cocaína*", 7);
                session.SendWhisper("*¡Ve a tu casa y guárdala en el baúl! [TODO ESTO ES ILEGAL]*", 1);
                session.GetRoleplay().RefreshStatDialogue();
            }
        }
    }
}
