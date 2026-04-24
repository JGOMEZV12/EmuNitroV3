using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Rooms;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboRoleplay.Gambling
{
    public class TexasHoldEmPlayer
    {
        /// <summary>
        /// The user id.
        /// </summary>
        public int UserId;

        /// <summary>
        /// The current bet.
        /// </summary>
        public int CurrentBet;

        /// <summary>
        /// The total amount.
        /// </summary>
        public int TotalAmount;

        /// <summary>
        /// Texas Hold 'Em variables
        /// </summary>
        /// <param name="UserId"></param>
        /// <param name="CurrentBet"></param>
        /// <param name="TotalAmount"></param>
        public TexasHoldEmPlayer(int UserId, int CurrentBet, int TotalAmount)
        {
            this.UserId = UserId;
            this.CurrentBet = CurrentBet;
            this.TotalAmount = TotalAmount;
        }
    }
}
