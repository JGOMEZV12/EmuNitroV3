using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Administrators
{
    class RoomReleaseCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_admin_room_release"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Libera a cualquier persona en la habitación de la cárcel si son encarcelados."; }
        }

        public async Task Execute(GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Variables
            int JailedUsers = 0;
            #endregion

            #region Conditions
            foreach (RoomUser User in Room.GetRoomUserManager().GetRoomUsers())
            {
                if (User == null)
                    continue;

                if (User.IsBot)
                    continue;

                if (User.GetClient() == null)
                    continue;

                if (User.GetClient().GetRoleplay() == null)
                    continue;

                if (!User.GetClient().GetRoleplay().IsJailed)
                    continue;

                JailedUsers++;
            }

            if (JailedUsers <= 0)
            {
                Session.SendWhisper("¡No hay usuarios encarcelados en esta sala!", 1);
                return;
            }
            #endregion

            #region Execute
            else
            {
                foreach (RoomUser User in Room.GetRoomUserManager().GetRoomUsers())
                {
                    if (User == null)
                        continue;

                    if (User.IsBot)
                        continue;

                    if (User.GetClient() == null)
                        continue;

                    if (User.GetClient().GetRoleplay() == null)
                        continue;

                    if (!User.GetClient().GetRoleplay().IsJailed)
                        continue;

                    GameClient TargetClient = User.GetClient();

                    TargetClient.GetRoleplay().IsJailed = false;
                    TargetClient.GetRoleplay().JailedTimeLeft = -5;
                    TargetClient.SendWhisper("¡Un administrador te ha liberado de la cárcel!", 1);
                }

                Session.Shout("*Usa sus poderes divinos y libera a toda persona encarcelada en la habitación*", 23);
                return;
            }
            #endregion
        }
    }
}