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
    class RoomHealCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_room_heal"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Cura a todos los usuarios en la misma habitación que tú."; }
        }

        public async Task Execute(GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Execute
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

                GameClient TargetClient = User.GetClient();

                if (TargetClient.GetRoomUser() != null)
                    TargetClient.GetRoomUser().ApplyEffect(0);

                if (TargetClient.GetRoleplay().IsDead)
                    continue;

                TargetClient.GetRoleplay().CurEnergy = TargetClient.GetRoleplay().MaxEnergy;
                 TargetClient.GetRoleplay().CurHealth = TargetClient.GetRoleplay().MaxHealth;
                 TargetClient.GetHabbo().Credits -= 200;
                 TargetClient.GetHabbo().UpdateCreditsBalance();
                 TargetClient.SendWhisper("Un administrador te ha sanado y por esto tu colaboras con 200$ para el cuidado de calles", 1);
            }

            Session.Shout("*He curado la sala*", 23);
            return;
            #endregion
        }
    }
}