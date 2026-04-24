using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboHotel.Rooms.Chat.Commands.SpecialRights
{
    class SummonPetsCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_summon_pets"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Invoca a todos los usuarios transformados."; }
        }

        public async Task Execute(GameClients.GameClient Session, Room Room, string[] Params)
        {
            #region Variables
            int Count = 0;
            #endregion

            #region Execute
            lock (PolarEnvironment.GetGame().GetClientManager().GetClients)
            {
                foreach (GameClient Client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                {
                    if (Client == null || Client.GetHabbo() == null || Client.GetRoleplay() == null)
                        continue;

                    if (Client == Session)
                        continue;

                    if (Client.GetHabbo().PetId <= 0)
                        continue;

                    if (Client.GetRoleplay().IsDead)
                    {
                        Client.GetRoleplay().IsDead = false;
                        Client.GetRoleplay().ReplenishStats(true);
                        Client.GetHabbo().Poof();
                    }

                    if (Client.GetRoleplay().IsJailed)
                    {
                        Client.GetRoleplay().IsJailed = false;
                        Client.GetRoleplay().JailedTimeLeft = 0;
                        Client.GetHabbo().Poof();
                    }

                    Count++;
                    RoleplayManager.SendUserOld2(Client, Room.Id, "Usted ha sido convocado por " + Session.GetHabbo().Username + "!");
                }
                if (Count > 0)
                    Session.Shout("*Utiliza sus poderes divinos para convocar a todos los usuarios transformados", 23);
                else
                    Session.SendWhisper("Lo sentimos, pero no hubo usuarios transformados para convocar", 1);
            }
            #endregion
        }
    }
}