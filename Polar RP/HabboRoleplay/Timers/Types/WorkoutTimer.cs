using System.Linq;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.Utilities;
using Polar.HabboHotel.Quests;
using System;
using Polar.Core;
using Polar.HabboHotel.Users.Effects;

namespace Polar.HabboRoleplay.Timers.Types
{
    /// <summary>
    /// Workout timer
    /// </summary>
    public class WorkoutTimer : RoleplayTimer
    {
        public WorkoutTimer(string Type, GameClient Client, int Time, bool Forever, object[] Params)
            : base(Type, Client, Time, Forever, Params)
        {
            // Convert 80 seconds to milliseconds
            TimeLeft = 80 * 1000;
        }

        /// <summary>
        /// Executes workout tick
        /// </summary>
        public override void Execute()
        {
            try
            {
                if (base.Client == null || base.Client.GetHabbo() == null || base.Client.GetRoleplay() == null)
                {
                    base.EndTimer();
                    return;
                }

                if (base.Client.GetRoomUser() == null || base.Client.GetRoomUser().GetRoom() == null)
                {
                    base.Client.GetRoleplay().IsWorkingOut = false;
                    base.EndTimer();
                    return;
                }

                int EffectId = EffectsList.CrossTrainer;
                int ItemId = (int)Params[0];
                bool Strength = (bool)Params[1];

                if (Strength)
                    EffectId = EffectsList.Treadmill;

                Item Treadmill = base.Client.GetRoomUser().GetRoom().GetRoomItemHandler().GetItem(ItemId);

                if (Treadmill == null || !base.Client.GetRoleplay().IsWorkingOut || Treadmill.Coordinate != base.Client.GetRoomUser().Coordinate)
                {
                    RoleplayManager.Shout(base.Client, "*¡Deja de funcionar mientras porque ha bajado de la máquina de entrenamiento!*", 4);
                    if (base.Client.GetRoomUser().CurrentEffect == EffectId)
                        base.Client.GetRoomUser().ApplyEffect(EffectsList.None);

                    base.Client.GetRoleplay().IsWorkingOut = false;
                    base.EndTimer();
                    return;
                }

                if (!base.Client.GetRoomUser().GetRoom().GymEnabled)
                {
                    if (base.Client.GetRoomUser().CurrentEffect == EffectId)
                        base.Client.GetRoomUser().ApplyEffect(EffectsList.None);

                    base.Client.GetRoleplay().IsWorkingOut = false;
                    base.EndTimer();
                    return;
                }

                if (base.Client.GetRoomUser().IsAsleep)
                {
                    RoleplayManager.Shout(base.Client, "*Se detiene porque se ha quedado dormido*", 4);
                    if (base.Client.GetRoomUser().CurrentEffect == EffectId)
                        base.Client.GetRoomUser().ApplyEffect(EffectsList.None);
                    base.Client.GetRoleplay().IsWorkingOut = false;
                    base.EndTimer();
                    return;
                }

                if (base.Client.GetRoleplay().CurEnergy <= 0)
                {
                    base.Client.SendWhisper("¡Se quedó sin energía! Usted es demasiado débil para seguir entrenando", 1);

                    if (base.Client.GetRoomUser().CurrentEffect == EffectId)
                        base.Client.GetRoomUser().ApplyEffect(EffectsList.None);

                    base.Client.GetRoleplay().IsWorkingOut = false;
                    base.EndTimer();
                    return;
                }

                if (base.Client.GetRoomUser().CurrentEffect != EffectId)
                    base.Client.GetRoomUser().ApplyEffect(EffectId);

                if (RoleplayManager.WorkoutCAPTCHABox)
                {
                    if (base.Client.GetRoleplay().CaptchaSent)
                        return;

                    if (!base.Client.GetRoleplay().CaptchaSent && base.Client.GetRoleplay().CaptchaTime >= Convert.ToInt32(RoleplayData.GetData("captcha", "workoutinterval")))
                    {
                        base.Client.GetRoleplay().CreateCaptcha("¡Introduzca el código en la caja para continuar entrenando!");
                        return;
                    }
                }

                TimeCount++;
                TimeLeft -= 1000;

                if (TimeLeft > 0)
                    return;

                CryptoRandom Random = new CryptoRandom();

                int AmountToAdd;
                int Exp;

                if (base.Client.GetHabbo().VIPRank > 0 || base.Client.GetHabbo().GetClubManager().HasSubscription("habbo_club") || base.Client.GetHabbo().GetClubManager().HasSubscription("habbo_vip"))
                {
                    AmountToAdd = Random.Next(1, 15) * 8;
                    Exp = AmountToAdd * 9;
                }
                else
                {
                    AmountToAdd = Random.Next(1, 8);
                    Exp = AmountToAdd * 6;
                }

               // int Exp = AmountToAdd * 4;

                LevelManager.AddLevelEXP(base.Client, Exp);

                if (Strength)
                {
                    if (base.Client.GetHabbo().VIPRank > 0 || base.Client.GetHabbo().GetClubManager().HasSubscription("habbo_club") || base.Client.GetHabbo().GetClubManager().HasSubscription("habbo_vip"))
                    {
                        base.Client.SendWhisper("¡Por ser VIP se te duplica la fuerza!", 1);
                    }
                        LevelManager.AddStrengthEXP(base.Client, AmountToAdd);
                }
                else
                {
                    if (base.Client.GetHabbo().VIPRank > 0 || base.Client.GetHabbo().GetClubManager().HasSubscription("habbo_club") || base.Client.GetHabbo().GetClubManager().HasSubscription("habbo_vip"))
                    {
                        base.Client.SendWhisper("¡Por ser VIP se te duplica la stamina!", 1);
                    }
                    LevelManager.AddStaminaEXP(base.Client, AmountToAdd);
                }

                PolarEnvironment.GetGame().GetQuestManager().ProgressUserQuest(Client, QuestType.WORKOUT);

                int EnergyLoss = Random.Next(RoleplayManager.WorkoutCAPTCHABox ? 2 : 4, RoleplayManager.WorkoutCAPTCHABox ? 4 : 8);

                if (base.Client.GetRoleplay().CurEnergy - EnergyLoss <= 0)
                    base.Client.GetRoleplay().CurEnergy = 0;
                else
                    base.Client.GetRoleplay().CurEnergy -= EnergyLoss;

                base.Client.SendWhisper("Pierdes " + EnergyLoss + " Energía para entrenar", 1);

                if (Strength && base.Client.GetRoleplay().Strength >= RoleplayManager.StrengthCap)
                {
                    base.Client.SendWhisper("Ha alcanzado el nivel máximo de resistencia de: " + RoleplayManager.StrengthCap + "!", 1);
                    base.Client.GetRoleplay().IsWorkingOut = false;

                    if (base.Client.GetRoomUser().CurrentEffect == EffectId)
                        base.Client.GetRoomUser().ApplyEffect(EffectsList.None);

                    base.EndTimer();
                    return;
                }

                if (!Strength && base.Client.GetRoleplay().Stamina >= RoleplayManager.StaminaCap)
                {
                    base.Client.SendWhisper("Ha alcanzado el nivel de resistencia máxima de: " + RoleplayManager.StaminaCap + "!", 1);
                    base.Client.GetRoleplay().IsWorkingOut = false;

                    if (base.Client.GetRoomUser().CurrentEffect == EffectId)
                        base.Client.GetRoomUser().ApplyEffect(EffectsList.None);

                    base.EndTimer();
                    return;
                }
                // Convert 80 seconds to milliseconds
                TimeLeft = 80 * 1000;
                return;
            }
            catch (Exception e)
            {
                Logging.LogRPTimersError("Error in Execute() void: " + e);
                base.EndTimer();
            }
        }
    }
}