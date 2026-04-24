using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Polar.HabboHotel.Groups;
using System.Threading.Tasks;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms;
using System.ServiceModel;
using Polar.Core;

namespace Polar.HabboRoleplay.Timers.Types
{
    public class bankRobTimer : RoleplayTimer
    {

        public bankRobTimer(string Type, GameClient Session, int Time, bool Forever, object[] Params)
    : base(Type, Session, Time, Forever, Params)
        {
            if (base.Client == null || base.Client.GetRoleplay() == null)
            {
                throw new ArgumentNullException("El cliente o su rol no están inicializados correctamente.");
            }

            // 5 minutos convertidos a milisegundos
            TimeLeft = RoleplayManager.BankCapTime * 1000;
        }

        public override void Execute()
        {
            try
            {
                if (base.Client == null ||
                    base.Client.GetHabbo() == null ||
                    base.Client.GetRoleplay() == null ||
                    base.Client.GetRoleplay().IsDead ||
                    base.Client.GetRoleplay().IsJailed ||
                    base.Client.GetRoomUser() == null)
                {
                    var roomUser = base.Client.GetRoomUser();
                    if (roomUser != null)
                    {
                        var roomUsers = roomUser.GetRoom()?.GetRoomUserManager()?.GetRoomUsers()?.ToList();
                        if (roomUsers != null)
                        {
                            foreach (var user in roomUsers)
                            {
                                if (user?.GetClient() != null)
                                {
                                    PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(
                                        user.GetClient(), "event_gang", "bank_cap_off");
                                }
                            }
                        }
                    }

                    base.Client.SendWhisper("Oops! Has dejado de robar.");
                    var clientRoleplay = base.Client.GetRoleplay();
                    if (clientRoleplay != null)
                    {
                        clientRoleplay.BankCapturing = false;
                        clientRoleplay.LoadingTimeLeft = 0;
                        clientRoleplay.Robbery = false;
                    }

                    base.Client.GetRoomUser().GetRoom().BankCapturing = false;
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

                var clientRoleplayUpdate = base.Client.GetRoleplay();
                if (clientRoleplayUpdate != null)
                {
                    clientRoleplayUpdate.LoadingTimeLeft--;
                }

                #region Conditions

                if (TimeLeft > 0)
                {
                    if (TimeCount == 60)
                    {
                        RoleplayManager.Shout(base.Client, "*Se acerca a robar el banco [" +
                            (TimeLeft / 60000) + " Minutos restantes]*", 4);
                        TimeCount = 0;
                    }
                    return;
                }

                #endregion

                #region Execute
                var company = GroupManager.GetJob(9);
                if (company == null)
                {
                    return;
                }

                Random rnd = new Random();
                int maxrob = (company.Balance < 30000) ? (company.Balance / 2) : 30000;
                int money = rnd.Next(5000, maxrob);

                if (company.Balance <= 50000)
                {
                    base.Client.SendWhisper("Oops! La bóveda actualmente no cuenta con fondos mínimos para el robo.");
                    return;
                }

                company.Balance -= money;

                if (company.Balance <= 0)
                {
                    base.Client.SendWhisper("La bóveda tiene $0, significa que ya fue completamente robada. ¡No ganaste nada robando el banco!");
                    if (clientRoleplayUpdate != null)
                    {
                        clientRoleplayUpdate.Robbery = false;
                    }
                    base.EndTimer();
                    return;
                }

                RoleplayManager.Shout(base.Client, "*Terminó su robo al banco [+$" + money + "]*");
                RoleplayManager.GiveMoney(base.Client, money);

                // BattlePass Challenge Integration
                PolarEnvironment.GetGame().GetBattlePassManager().ProgressChallenge(base.Client, "rob_bank", 1);

                #region Bank Company Balance
                RoleplayManager.TakeMoneyFromCompany(9, money);
                #endregion Bank Company Balance

                base.Client.SendWhisper("Dinero restante de la bóveda: $" + company.Balance + "!");
                if (clientRoleplayUpdate != null)
                {
                    clientRoleplayUpdate.Robbery = false;
                    clientRoleplayUpdate.BankCapturing = false;
                    clientRoleplayUpdate.LoadingTimeLeft = 0;
                }

                // Set 5 hour cooldown for the vault
                RoleplayManager.VaultCooldowns[base.Client.GetRoomUser().RoomId] = DateTime.Now.AddHours(5);

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
