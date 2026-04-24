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
using Polar.Utilities;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Jobs.Types.Police
{
    class FlashBangCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_police_flashbang"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Paraliza todos los usuarios deseados en una habitación con el fin de detener en sus pasos."; }
        }

        public async Task Execute(GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions

            RoomUser RoomUser = Session.GetRoomUser();

            if (!GroupManager.HasJobCommand(Session, "flashbang"))
            {
                Session.SendWhisper("¡Sólo un teniente de policía puede usar este comando!", 1);
                return;
            }

            if (!Session.GetRoleplay().IsWorking)
            {
                Session.SendWhisper("¡Debes estar trabajando para usar este comando!", 1);
                return;
            }

            List<RoomUser> WantedUsers = Room.GetRoomUserManager().GetRoomUsers().Where(x => x != null && x.GetClient() != null && x.GetClient().GetHabbo() != null && x.GetClient().GetRoleplay() != null && RoleplayManager.WantedList.ContainsKey(x.UserId)).ToList();
            if (WantedUsers.Count <= 0)
            {
                Session.SendWhisper("No hay ningún usuario deseado en esta sala", 1);
                return;
            }

            if (Session.GetRoleplay().TryGetCooldown("flashbang"))
                return;

            Point ClientPos = new Point(RoomUser.Coordinate.X, RoomUser.Coordinate.Y);

            #endregion

            #region Execute

            lock (Room.GetRoomUserManager().GetRoomUsers())
            {
                foreach (RoomUser User in Room.GetRoomUserManager().GetRoomUsers())
                {
                    if (User == null)
                        continue;

                    if (User.GetClient() == null)
                        continue;

                    if (User.GetClient().GetHabbo() == null)
                        continue;

                    if (User.GetClient().GetRoleplay() == null)
                        continue;

                    if (Session.GetHabbo().Id == User.UserId)
                        continue;

                    if (User.IsAsleep)
                        continue;

                    if (!RoleplayManager.WantedList.ContainsKey(User.UserId))
                        continue;

                    Point TargetClientPos = new Point(User.Coordinate.X, User.Coordinate.Y);
                    double Distance = RoleplayManager.GetDistanceBetweenPoints2D(ClientPos, TargetClientPos);

                    if (Distance <= 10)
                    {
                        User.GetClient().GetRoleplay().TimerManager.CreateTimer("stun", 1000, false);
                        User.GetClient().GetRoleplay().UpdateTimerDialogue("Flash-Bang", "add", 1000, 60);
                        //User.GetClient().SendMessage(new FloodControlComposer(15));

                        if (User.GetClient().GetRoleplay().InsideTaxi)
                            User.GetClient().GetRoleplay().InsideTaxi = false;

                        User.Frozen = true;
                        User.CanWalk = false;
                        User.ClearMovement(true);
                    }
                }
            }

            Session.Shout("*Lanza una flashbang a todos los sospechosos buscados en la sala y los marea a todos*", 37);
            Session.GetRoleplay().CooldownManager.CreateCooldown("flashbang", 1000, 60);
            return;

            #endregion
        }
    }
}