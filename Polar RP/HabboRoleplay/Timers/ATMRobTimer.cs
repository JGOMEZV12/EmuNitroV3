using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users.Effects;
using Polar.Core;

namespace Polar.HabboRoleplay.Timers
{
    public class ATMRobTimer : RoleplayTimer
    {

        public ATMRobTimer(string Type, GameClient Session, int Time, bool Forever, object[] Params)
    : base(Type, Session, Time, Forever, Params)
        {
            if (base.Client == null || base.Client.GetRoleplay() == null)
            {
                throw new ArgumentNullException("El cliente o su rol no están inicializados correctamente.");
            }

            TimeLeft = base.Client.GetRoleplay().ATMRobTimeLeft * 1000; // 180 segs => 3 mins.
        }

        public override void Execute()
        {
            try
            {
                // Verifica que base.Client y sus propiedades no sean nulas antes de usarlas
                if (base.Client == null ||
                    base.Client.GetHabbo() == null ||
                    base.Client.GetRoleplay() == null ||
                    base.Client.GetRoleplay().BreakGeneralTimer ||
                    base.Client.GetRoleplay().IsDead ||
                    base.Client.GetRoleplay().IsJailed ||
                    base.Client.GetRoomUser() == null)
                {
                    RoleplayManager.Shout(base.Client, "*Ha dejado de robar el Cajero*", 5);

                    var roomUser = base.Client.GetRoomUser();
                    if (roomUser?.CurrentEffect == EffectsList.Twinkle)
                    {
                        roomUser.ApplyEffect(0);
                    }

                    if (base.Client.GetRoleplay() != null)
                    {
                        base.Client.GetRoleplay().ATMRobbery = false;
                        base.Client.GetRoleplay().BreakGeneralTimer = false;
                    }

                    base.EndTimer();
                    return;
                }

                var roomUserCheck = base.Client.GetRoomUser();
                if (roomUserCheck == null || roomUserCheck.IsAsleep)
                {
                    return;
                }

                TimeCount++;
                TimeLeft -= 1000;

                if (base.Client.GetRoleplay() != null)
                {
                    base.Client.GetRoleplay().ATMRobTimeLeft--;
                }

                #region Conditions

                if (TimeLeft > 0)
                {
                    if (TimeCount == 60)
                    {
                        RoleplayManager.Shout(base.Client, "*Te quedan aún [" +
                            (base.Client.GetRoleplay()?.ATMRobTimeLeft / 60) + " Minutos restantes]*", 4);
                        TimeCount = 0;
                    }

                    if (TimeLeft == 120000)
                    {
                        base.Client.Shout("*Golpeando el cajero rápidamente, causando un pequeño daño*");
                    }
                    else if (TimeLeft == 60000)
                    {
                        base.Client.Shout("*Se está llenando el paquete*");
                    }
                    return;
                }

                #endregion

                #region Execute
                Random rnd = new Random();
                int money = rnd.Next(0, 2000);
                RoleplayManager.Shout(base.Client, "*Robó satisfactoriamente el cajero [+$" + money + "]*");
                RoleplayManager.GiveMoney(base.Client, money);

                // BattlePass Challenge Integration
                PolarEnvironment.GetGame().GetBattlePassManager().ProgressChallenge(base.Client, "rob_atm", 1);

                #region Bank Company Balance
                RoleplayManager.TakeMoneyFromCompany(4, money);
                RoleplayManager.TakeMoneyFromCompany(6, money);
                #endregion Bank Company Balance

                if (base.Client.GetRoleplay() != null)
                {
                    base.Client.GetRoleplay().ATMRobbery = false;
                    base.Client.GetRoleplay().ATMRobTimeLeft = 0;
                }
                base.EndTimer();
                #endregion
            }
            catch (Exception e)
            {
                Logging.LogRPTimersError("Error in Execute() void: " + e);
                base.EndTimer();
            }
        }
    }
}
