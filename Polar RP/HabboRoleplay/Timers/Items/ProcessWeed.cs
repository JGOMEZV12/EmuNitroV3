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
    public class ProcessWeed : RoleplayTimer
    {
        private const int InversionCosto = 1000;
        private const int EfectoWeed = 595;
        private const string ExtraDataEnProgreso = "1";
        private const string ExtraDataDisponible = "0";
        public ProcessWeed(string Type, GameClient Client, int Time, bool Forever, object[] Params)
            : base(Type, Client, Time, Forever, Params)
        {
            TimeLeft = RoleplayManager.ProcessWeedTime * 1000;
        }

        /// <summary>
        /// Disminuye el ánimo del usuario y controla su energía
        /// </summary>
        public override void Execute()
        {
            try
            {
                #region Variables
                Room Room = base.Client.GetHabbo().CurrentRoom;
                Turf Turf = PolarEnvironment.GetGame().GetGangTurfsManager().getTurfbyRoom(base.Client.GetHabbo().CurrentRoomId);
                Item bTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "weed_krauter" && x.Coordinate == base.Client.GetRoomUser().SquareInFront);
                #endregion

                // Validaciones iniciales
                if (base.Client == null ||
                    base.Client.GetHabbo() == null ||
                    base.Client.GetRoleplay() == null ||
                    base.Client.GetRoleplay().BreakGeneralTimer ||
                    base.Client.GetRoleplay().IsDead ||
                    base.Client.GetRoleplay().IsJailed ||
                    base.Client.GetRoomUser() == null)
                {
                    if (base.Client.GetRoleplay() != null)
                    {
                        bTile.ExtraData = "0";
                        base.Client.GetRoleplay().BreakGeneralTimer = false;
                    }

                    base.EndTimer();
                    return;
                }


                if (Turf == null)
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
                    base.Client.SendWhisper("¡Debes pararte frente a la planta de marihuana!", 1);
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

                    base.Client.Shout("* Comienza a cuidar la planta y cosechar las semillas [-"+InversionCosto+"$ Inversión]*", 6);
                    base.Client.SendWhisper("Por favor, espera " + (TimeLeft / 60000) + " minutos, no te podrás mover.", 1);
                    base.Client.GetHabbo().Credits -= InversionCosto;
                    base.Client.GetHabbo().UpdateCreditsBalance();

                    user.CanWalk = false;

                    // Usar operador null-conditional y validar
                    if (user.CurrentEffect != EfectoWeed && base.Client?.GetRoleplay()?.EquippedWeapon == null)
                        user.ApplyEffect(EfectoWeed);

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
                        base.Client.SendWhisper("¡Casi termina la fabricación de la marihuana!", 1);
                        TimeCount = 0;
                    }
                    return;
                }

                #region Cumple timer

                // Revalidar después del sleep (el usuario pudo desconectarse)
                if (user?.CurrentEffect != EfectoWeed && base.Client?.GetRoleplay()?.EquippedWeapon == null)
                    user?.ApplyEffect(0);

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

            #region Weed
            else if (chance > 1 && chance <= 6)
            {
                int Amount = Random.Next(1, 10);

                session.GetRoleplay().Weedmateria += Amount;
                session.Shout("*¡Felicidades cosechaste! " + Amount + " semillas de marihuana *", 6);
                session.SendWhisper("*¡utiliza la mesa para preparar los porros! [TODO ESTO ES ILEGAL]*", 1);
            }
            #endregion

            #region No Reward
            else
            {
                session.SendWhisper("*¡Esta planta no tiene semillas que ofrecer! intentalo más tarde*", 1);
            }
            #endregion
        }
    }
}