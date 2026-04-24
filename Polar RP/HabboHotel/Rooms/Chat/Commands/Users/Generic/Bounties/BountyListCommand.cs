using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.Communication.Packets.Outgoing.Notifications;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.Users;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Bounties
{
    class BountyListCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_bounty_list"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Ver lista de recompensa"; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            StringBuilder Message = new StringBuilder().Append("--- Lista de recompensa ---\n\n");

            if (BountyManager.BountyUsers.Count <= 0)
                Message.Append("No hay recompensa ahora.\n");

            lock (BountyManager.BountyUsers.Values)
            {
                foreach (Bounty Bounty in BountyManager.BountyUsers.Values)
                {
                    if (PolarEnvironment.GetUnixTimestamp() > Bounty.ExpiryTimeStamp)
                    {
                        BountyManager.RemoveBounty(Bounty.UserId, true);
                        Habbo BountyOwner = PolarEnvironment.GetHabboById(Convert.ToInt32(Bounty.AddedBy));

                        if (BountyOwner == null || BountyOwner.GetClient() == null)
                            continue;

                        BountyOwner.GetClient().SendWhisper("La recompensa que usted fijó encendido " + PolarEnvironment.GetHabboById(Convert.ToInt32(Bounty.UserId)).Username + " ha expirado", 1);
                    }

                    TimeSpan Difference = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(Bounty.ExpiryTimeStamp).Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(PolarEnvironment.GetUnixTimestamp()));

                    Message.Append("NOMBRE: " + PolarEnvironment.GetHabboById(Convert.ToInt32(Bounty.UserId)).Username + " - termina en " + ((int)Difference.TotalMinutes < 0 ? 0 : (int)Difference.TotalMinutes) + " minutos\n");
                    Message.Append("RECOMPENSA: $" + String.Format("{0:N0}", Bounty.Reward) + "\n\n");
                }
            }
            Session.SendMessage(new MOTDNotificationComposer(Message.ToString()));
        }
    }
}