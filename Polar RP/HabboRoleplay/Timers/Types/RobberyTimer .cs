using System.Linq;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.Utilities;
using Polar.HabboHotel.Quests;
using Polar.HabboHotel.Groups;
using System;
using Polar.Core;
using Polar.HabboHotel.Users.Effects;

namespace Polar.HabboRoleplay.Timers.Types
{
    /// <summary>
    /// Workout timer
    /// </summary>
    public class RobberyTimer : RoleplayTimer
    {
        private const int InitialTimeLeft = 20 * 60000; // 20 minutes in milliseconds

        public RobberyTimer(string Type, GameClient Client, int Time, bool Forever, object[] Params)
            : base(Type, Client, Time, Forever, Params)
        {
            TimeLeft = InitialTimeLeft;
        }

        /// <summary>
        /// Executes robbery tick
        /// </summary>
        public override void Execute()
        {
            try
            {
                if (base.Client?.GetHabbo() == null || base.Client.GetRoleplay() == null)
                {
                    base.EndTimer();
                    return;
                }

                var roomUser = base.Client.GetRoomUser();
                if (roomUser?.GetRoom() == null)
                {
                    base.Client.GetRoleplay().Robbery = false;
                    base.EndTimer();
                    return;
                }

                TimeLeft -= 60000;

                if (TimeLeft > 0)
                {
                    int minutesRemaining = TimeLeft / 60000;
                    Client.SendWhisper($"Usted tiene {minutesRemaining} minuto(s) para terminar el robo!");
                    return;
                }

                ExecuteRobbery();
            }
            catch (Exception e)
            {
                Logging.LogRPTimersError($"Error in Execute() void: {e}");
                base.EndTimer();
            }
        }

        private void ExecuteRobbery()
        {
            Group company = GroupManager.GetJob(9);
            if (company == null) return;

            Random rnd = new Random();
            int maxRobberyAmount = company.Balance < 50000 ? company.Balance / 2 : 50000;
            int money = rnd.Next(500, maxRobberyAmount);

            company.Balance -= money;

            if (company.Balance <= 0)
            {
                Client.SendWhisper("La bóveda tiene $0. Esto significa que ya fue completamente robada y no ganaste nada robando el banco.");
                EndRobbery();
                return;
            }

            RoleplayManager.Shout(Client, $"*Terminó su robo al banco [+$ {money}]");
            RoleplayManager.GiveMoney(Client, money);

            // Update Bank Company Balance
            RoleplayManager.TakeMoneyFromCompany(9, money);

            Client.SendWhisper($"Dinero restante en la bóveda: ${company.Balance}.");
            EndRobbery();
        }

        private void EndRobbery()
        {
            Client.GetRoleplay().Robbery = false;
            base.EndTimer();
        }
    }

}