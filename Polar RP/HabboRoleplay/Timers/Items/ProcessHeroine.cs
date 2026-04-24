using Polar.HabboHotel.GameClients;
using Polar.Core;
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
    public class ProcessHeroine : RoleplayTimer
    {
        private const string ExtraDataEnProgreso = "1";
        private const string ExtraDataDisponible = "0";
        public ProcessHeroine(string Type, GameClient Client, int Time, bool Forever, object[] Params)
            : base(Type, Client, Time, Forever, Params)
        {
            TimeLeft = RoleplayManager.ProcessHeroineTime * 1000;
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
                Item? bTile = room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "ktchn_c15_microwave" && x.Coordinate == base.Client.GetRoomUser().SquareInFront);

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
                    return;
                }

                // Verificar si el jugador tiene suficiente dinero para realizar la acción
                if (base.Client.GetHabbo().Credits < 20000)
                {
                    base.Client.SendWhisper("¡No tienes 20.00$ para invertir!", 6);
                    return;
                }

                // Verificar si el jugador está ocupado trabajando
                if (base.Client.GetRoleplay().IsWorkingOut)
                {
                    base.Client.SendWhisper("Deja de trabajar para poder hacer heroína", 6);
                    return;
                }


                if (bTile == null)
                {
                    base.Client.SendWhisper("¡Debes pararte frente al microondas para cocinar la heroína!", 1);
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

                    base.Client.Shout("* Comienza a fabricar la heroína [-20.000$ Inversión]*", 6);
                    base.Client.GetHabbo().Credits -= 20000;
                    base.Client.GetHabbo().UpdateCreditsBalance();

                    user.CanWalk = false;

                    // Aplicar efecto de fabricación
                    if (user.CurrentEffect != 546 && base.Client.GetRoleplay().EquippedWeapon == null)
                        user.ApplyEffect(546);

                    // Verificar si todo sigue válido
                    if (base.Client?.GetHabbo() == null || base.Client.GetRoleplay() == null || bTile == null)
                        return;
                }


                TimeCount++;

                TimeLeft -= 1000;

                if (TimeLeft > 0)
                {
                    if (TimeCount == 60)
                    {
                        base.Client.SendWhisper("¡Casi termina la fabricación de la heroina!", 1);
                        TimeCount = 0;
                    }
                    return;
                }

                #region Cumple timer
                // Restablecer efecto después de la fabricación
                if (user.CurrentEffect != 546 && base.Client.GetRoleplay().EquippedWeapon == null)
                    user.ApplyEffect(0);

                ChooseReward(base.Client);
                bTile.ExtraData = ExtraDataDisponible;
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
                base.Client.SendWhisper("¡No puedes hacer esto mientras estás muerto!", 6);
                base.EndTimer();
                return;
            }

            var random = new CryptoRandom();
            int totalCraftingItems = CraftingManager.CraftableItems.Count;
            int chance = random.Next(1, 5);
            int secondChance = random.Next(1, 5);

            if (secondChance < 4 && chance > totalCraftingItems)
                chance = random.Next(1, totalCraftingItems + 1);

            #region Recompensa: Heroína
            // Si el jugador tiene una buena suerte, se le otorga heroína
            if (chance > 1 && chance <= 6)
            {
                int amount = random.Next(10, 15);

                session.GetRoleplay().Heroina += amount;
                session.Shout("*¡Felicidades fabricaste! " + amount + "cc de Heroína*", 7);
                session.SendWhisper("*¡Ve a tu casa y guárdala en el baúl! [TODO ESTO ES ILEGAL]*", 1);
                session.GetRoleplay().RefreshStatDialogue();
                base.EndTimer();
            }
            #endregion
        }
    }
}