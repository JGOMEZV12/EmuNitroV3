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
    class RoomRestoreCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_admin_room_restore"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Libera a cualquier persona en la habitación del hospital si está muerta."; }
        }

        public async Task Execute(GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Variables
            int DeadUsers = 0;
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

                if (!User.GetClient().GetRoleplay().IsDead)
                    continue;

                DeadUsers++;
            }

            if (DeadUsers <= 0)
            {
                Session.SendWhisper("No hay usuarios muertos en esta sala!", 1);
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

                    if (!User.GetClient().GetRoleplay().IsDead)
                        continue;


                    GameClient TargetClient = User.GetClient();

                    TargetClient.GetRoleplay().IsDead = false;
                    TargetClient.GetRoleplay().DeadTimeLeft = 0;
                    TargetClient.GetRoleplay().ReplenishStats(true);
                    TargetClient.SendWhisper("Un administrador le ha restaurado del hospital!", 1);
                }

                Session.Shout("*Utiliza sus poderes divinos y restaura a cualquier persona muerta en la habitación*", 23);
                return;
            }
            #endregion
        }
    }
}