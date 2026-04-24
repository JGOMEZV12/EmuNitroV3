using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboHotel.Rooms;
using Polar.Core;

namespace Polar.HabboRoleplay.Timers
{
    public class RobartiendaTimer : RoleplayTimer
    {

        public RobartiendaTimer(string Type, GameClient Session, int Time, bool Forever, object[] Params)
            : base(Type, Session, Time, Forever, Params)
        {

            int time = 180;
            TimeLeft = time * 500;

        }

        public override void Execute()
        {
            try
            {
                if (base.Client == null || base.Client.GetHabbo() == null || base.Client.GetRoleplay() == null || base.Client.GetRoleplay().IsDead || base.Client.GetRoleplay().IsJailed || base.Client.GetRoomUser() == null/* || base.Client.GetRoomUser().IsWalking*/ || base.Client.GetRoleplay().BreakGeneralTimer)
                {
                    RoleplayManager.Shout(base.Client, "*Ha dejado de robar la tienda*", 5);

                    base.Client.GetRoleplay().BreakGeneralTimer = false;
                    base.Client.GetRoleplay().RobartiendaRobbery = false;
                    base.EndTimer();
                    return;
                }

                TimeCount++;

                TimeLeft -= 500;

                #region Conditions
                if (base.Client == null)
                { base.EndTimer(); return; }

                if (TimeLeft == 2 * 60000)
                {
                    base.Client.Shout("* Revisa el mostrador en busca de dinero *");
                }
                else if (TimeLeft == 1 * 60000)
                {
                    base.Client.Shout("* Al parecer hay algo de dinero *");
                }
                if (TimeLeft > 0)
                {
                    if (TimeCount == 60)
                    {
                        int minutesRemaining = TimeLeft / 60;
                        base.Client.SendWhisper("Usted tiene poco tiempo para terminar de robar la tienda, es probable que venga la policía");
                        TimeCount = 0;
                    }
                    return;
                }

                #endregion

                #region Execute
                Random rnd = new Random();
                int money = rnd.Next(300, 700);
                RoleplayManager.Shout(base.Client, "* Logró encontrar dinero en el mostrador, obteniendo [+$" + money + "]*");

                RoleplayManager.GiveMoney(base.Client, money);
                // BattlePass Challenge Integration
                PolarEnvironment.GetGame().GetBattlePassManager().ProgressChallenge(base.Client, "rob_store", 1);
                #region Bank Company Balance
                if (base.Client.GetHabbo().CurrentRoom.SupermarketEnabled)
                {
                    RoleplayManager.TakeMoneyFromCompany(12, money); 
                }
                #endregion Bank Company Balance

                base.Client.GetRoleplay().RobartiendaRobbery = false;

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
