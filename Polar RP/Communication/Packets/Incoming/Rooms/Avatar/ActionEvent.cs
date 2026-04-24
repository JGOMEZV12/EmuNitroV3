using System;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Rooms.Avatar;

namespace Polar.Communication.Packets.Incoming.Rooms.Avatar
{
    internal class ActionEvent : IPacketEvent
    {
        public void Parse(GameClient Session, ClientPacket Packet)
        {
            if (!Session.GetHabbo().InRoom)
                return;

            if (Session.GetRoleplay().DrivingCar || Session.GetRoleplay().Pasajero || Session.GetRoleplay().EquippedWeapon != null
                || Session.GetRoleplay().WateringCan || Session.GetRoleplay().IsDead)
                return;

            int Action = Packet.PopInt();

            Room Room = null;
            if (!PolarEnvironment.GetGame().GetRoomManager().TryGetRoom(Session.GetHabbo().CurrentRoomId, out Room))
                return;

            RoomUser User = Room.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);
            if (User == null)
                return;

            if (Action != 1 && Action != 7)
                User.UnIdle();

            if (Action == 3) // giggle, it removes the gun enable
            {
                return;
            }
            else if (Action == 5) // idle
            {
                if (!Session.GetHabbo().GetPermissions().HasCommand("command_idle"))
                {
                    Session.SendWhisper("Sorry, but :idle is disabled to prevent abuse.", 1);
                    return;
                }
                else
                {
                    if (User.DanceId > 0)
                        User.DanceId = 0;

                    if (Session.GetHabbo().Effects().CurrentEffect > 0)
                        Room.SendMessage(new AvatarEffectComposer(User.VirtualId, 0));

                    User.IsAsleep = true;
                    Room.SendMessage(new SleepComposer(User, true));

                    if (!Session.GetRoleplay().IsJailed && !Session.GetRoleplay().IsDead)
                    {
                        Session.GetHabbo().Motto = "[AFK] " + Session.GetRoleplay().Class;
                        Session.GetHabbo().Poof(false);
                    }
                }
            }
            else
            {
                if (User.DanceId > 0)
                    User.DanceId = 0;

                if (Session.GetHabbo().Effects().CurrentEffect > 0)
                    Room.SendMessage(new AvatarEffectComposer(User.VirtualId, 0));

                Room.SendMessage(new ActionComposer(User.VirtualId, Action));
            }
        }
    }
}