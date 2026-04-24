using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.Database.Interfaces;

namespace Polar.Communication.Packets.Incoming.Rooms.Action
{
    internal class MuteUserEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            if (!Session.GetHabbo().InRoom)
                return;

            int UserId = Packet.PopInt();
            int RoomId = Packet.PopInt();
            int Time = Packet.PopInt();

            Room Room = Session.GetHabbo().CurrentRoom;
            if (Room == null)
                return;

            if (((Room.WhoCanMute == 0 && !Room.CheckRights(Session, true) && Room.Group == null) || (Room.WhoCanMute == 1 && !Room.CheckRights(Session)) && Room.Group == null && !Session.GetHabbo().GetPermissions().HasRight("ambassador")) || (Room.Group != null && !Room.CheckRights(Session, false, true) && !Session.GetHabbo().GetPermissions().HasRight("ambassador")))
                return;

            RoomUser Target = Room.GetRoomUserManager().GetRoomUserByHabbo(UserId);
            if (Target == null)
                return;
            else if (Target.GetClient().GetHabbo().GetPermissions().HasRight("mod_tool"))
                return;

            if (Session.GetHabbo().GetPermissions().HasRight("ambassador"))
            {
                if (Target.GetClient().GetHabbo().TimeMuted > 0)
                {
                    Session.SendWhisper("Lo sentimos, pero este usuario está silenciado para la próxima " + String.Format("{0:N0}", Math.Floor((Target.GetClient().GetHabbo().TimeMuted / 60))) + " minuto(s) - entonces no puedes silenciarlos de nuevo.", 1);
                    return;
                }

                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.RunQuery("UPDATE `users` SET `time_muted` = '" + (Time * 60) + "' WHERE `id` = '" + Target.GetClient().GetHabbo().Id + "' LIMIT 1");
                }

                Target.GetClient().GetHabbo().TimeMuted = (Time * 60);

                Target.GetClient().SendNotification("Tu fuiste muteado por " + String.Format("{0:N0}", Time) + " minutos por un embajador porque tu comportamiento fue apropiado.");
                Session.SendWhisper("Silenciado correctamente a " + Target.GetClient().GetHabbo().Username + " por " + String.Format("{0:N0}", Time) + " minutos.", 1);

                PolarEnvironment.GetGame().GetChatManager().GetCommands().LogCommand(Session.GetHabbo().Id, "mute " + Target.GetClient().GetHabbo().Username + " " + Time, Session.GetHabbo().MachineId, "ambassador");
                return;
            }
            else
            {
                if (Room.MutedUsers.ContainsKey(UserId))
                {
                    if (Room.MutedUsers[UserId] < PolarEnvironment.GetUnixTimestamp())
                        Room.MutedUsers.Remove(UserId);
                    else
                        return;
                }

                Room.MutedUsers.Add(UserId, (PolarEnvironment.GetUnixTimestamp() + (Time * 60)));

                Target.GetClient().SendWhisper("Fuiste muteado por " + Time + " minutos", 1);
                PolarEnvironment.GetGame().GetAchievementManager().ProgressAchievement(Session, "ACH_SelfModMuteSeen", 1);
            }
        }
    }
}
