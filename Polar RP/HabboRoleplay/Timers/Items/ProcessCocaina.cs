using Polar.HabboHotel.GameClients;
using Polar.Core;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Rooms;
using Polar.HabboRoleplay.Turfs;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.Items.Crafting;
using Polar.Utilities;

namespace Polar.HabboRoleplay.Timers.Types.Items
{
    /// <summary>
    /// Makes the citizens cleanliness decrease over time
    /// </summary>
    public class ProcessCocaina : RoleplayTimer
    {
        private const int InversionCosto = 15000;
        private const int EfectoCocaina = 546;
        private const string SacoAlcaloideItemName = "xmas_sackdru";
        private const string ExtraDataEnProgreso = "1";
        private const string ExtraDataDisponible = "0";
        public ProcessCocaina(string Type, GameClient Client, int Time, bool Forever, object[] Params)
            : base(Type, Client, Time, Forever, Params)
        {
            TimeLeft = RoleplayManager.ProcessCocaineTime * 1000;
        }

        /// <summary>
        /// Disminuye el ánimo del usuario y controla su energía
        /// </summary>
        public override void Execute()
        {
            try
            {
                // Validaciones iniciales
                if (base.Client == null || base.Client.GetHabbo() == null || base.Client.GetRoleplay() == null || base.Client.GetRoomUser() == null)
                {
                    base.EndTimer();
                    return;
                }

                Room room = base.Client.GetHabbo()?.CurrentRoom;
                Turf turf = room != null ? PolarEnvironment.GetGame().GetGangTurfsManager()?.getTurfbyRoom(base.Client.GetHabbo().CurrentRoomId) : null;
                Group gang = base.Client.GetRoleplay()?.GangId != null ? GroupManager.GetGang(base.Client.GetRoleplay().GangId) : null;
                Item? bTile = room?.GetRoomItemHandler()?.GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.Equals(SacoAlcaloideItemName, StringComparison.OrdinalIgnoreCase) && x.Coordinate == base.Client.GetRoomUser()?.SquareInFront);

                if (turf == null)
                {
                    base.Client.SendWhisper("¡No estás en ningún territorio!", 6);
                    base.EndTimer();
                    return;
                }

                RoomUser? user = bTile.GetRoom()?.GetRoomUserManager()?.GetRoomUserByHabbo(base.Client.GetHabbo().Id);
                if (user == null)
                {
                    base.Client.SendWhisper("¡No se pudo encontrar al usuario en la sala!", 6);
                    base.EndTimer();
                    return;
                }

                if (!Gamemap.TilesTouching(bTile.GetX, bTile.GetY, user.Coordinate.X, user.Coordinate.Y))
                {
                    if (bTile.ExtraData == ExtraDataDisponible || bTile.ExtraData == ExtraDataEnProgreso)
                        if (user.CanWalk)
                            user.MoveTo(bTile.SquareInFront);
                    base.EndTimer();
                    return;
                }

                if (base.Client.GetHabbo().Credits < InversionCosto)
                {
                    base.Client.SendWhisper($"¡No tienes {InversionCosto}$ para invertir!", 6);
                    base.EndTimer();
                    return;
                }

                if (base.Client.GetRoleplay().IsDead)
                {
                    base.Client.SendWhisper("¡No puedes hacer esto mientras estás muerto!", 6);
                    base.EndTimer();
                    return;
                }

                if (base.Client.GetRoleplay().IsWorkingOut)
                {
                    base.Client.SendWhisper("Deja de trabajar para poder hacer cocaína", 6);
                    base.EndTimer();
                    return;
                }

                if (bTile == null)
                {
                    base.Client.SendWhisper("¡Debes pararte frente al saco de alcaloide!", 1);
                    base.EndTimer();
                    return;
                }

                if (string.IsNullOrEmpty(bTile.ExtraData))
                    bTile.ExtraData = ExtraDataDisponible;

                if (bTile.ExtraData == ExtraDataDisponible)
                {
                    // Cambiar el estado del item a "en progreso"
                    bTile.ExtraData = ExtraDataEnProgreso;
                    bTile.UpdateState(false, true);
                    bTile.RequestUpdate(TimeLeft, true);

                    base.Client.Shout("* Comienza a fabricar el clorhidrato de cocaína [-15.000$ Inversión]*", 6);
                    base.Client.SendWhisper("Por favor, espera " + (TimeLeft / 60000) + " minutos, no te podrás mover.", 1);
                    base.Client.GetHabbo().Credits -= InversionCosto;
                    base.Client.GetHabbo().UpdateCreditsBalance();

                    user.CanWalk = false;

                    // Usar operador null-conditional y validar
                    if (user.CurrentEffect != EfectoCocaina && base.Client?.GetRoleplay()?.EquippedWeapon == null)
                        user.ApplyEffect(EfectoCocaina);
                    // Verificar si todo sigue válido
                    if (base.Client?.GetHabbo() == null || base.Client.GetRoleplay() == null || bTile == null)
                    {
                        bTile.ExtraData = ExtraDataDisponible;
                        base.EndTimer();
                        return;
                    }
                }


                TimeCount++;

                TimeLeft -= 1000;

                if (TimeLeft > 0)
                {
                    if (TimeCount == 60)
                    {
                        base.Client.SendWhisper("¡Casi termina la fabricación de la cocaina!", 1);
                        TimeCount = 0;
                    }
                    return;
                }

                #region Cumple timer

                // Revalidar después del sleep (el usuario pudo desconectarse)
                if (user?.CurrentEffect != EfectoCocaina && base.Client?.GetRoleplay()?.EquippedWeapon == null)
                    user?.ApplyEffect(0);

                ChooseReward(base.Client);
                bTile.ExtraData = "0";
                bTile.UpdateState(false, true);

                user.CanWalk = true; // Usar null-conditional
                base.EndTimer();
                #endregion


            }
            catch (Exception e)
            {
                Logging.LogRPTimersError($"Error in Execute() void: {e}");
                base.EndTimer();
            }
        }

        public void ChooseReward(GameClient session)
        {
            if (session?.GetRoleplay()?.IsDead == true)
            {
                session.SendWhisper("¡No puedes hacer esto mientras estás muerto!", 6);
                base.EndTimer();
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
                base.EndTimer();
            }
        }
    }
}