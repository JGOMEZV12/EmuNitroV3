using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.HabboRoleplay.Houses;

using Polar.HabboHotel.Rooms;
using Polar.HabboRoleplay.Misc;

namespace Polar.Communication.Packets.Incoming.Rooms.Action
{
    internal class KickUserEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            Room Room = Session.GetHabbo().CurrentRoom;
            if (Room == null)
                return;

            if (Session.GetHabbo().Rank == 2 && Session.GetHabbo().GetPermissions().HasRight("ambassador"))
            {
                Session.SendNotification("Esta acción de Embajador está actualmente desactivada.");
                return;
            }

            if (!Room.CheckRights(Session) && Room.WhoCanKick != 2 && Room.Group == null)
                return;

            if (Room.Group != null && !Room.CheckRights(Session, false, true))
                return;


            var House = PolarEnvironment.GetGame().GetHouseManager().GetHouseByInsideRoom(Room.Id);
            var ApartInside = PolarEnvironment.GetGame().GetApartmentOwnedManager().GetApartmentByInsideRoom(Room.Id);
            if (House == null)
            {
                if (ApartInside == null)
                {
                    Session.SendWhisper("¡No estás dentro de ninguna Casa/Apartamento para hacer eso!", 1);
                    return;
                }
            }

            int UserId = Packet.PopInt();
            RoomUser User = Room.GetRoomUserManager().GetRoomUserByHabbo(UserId);
            if (User == null || User.IsBot)
                return;

            //Cannot kick owner or moderators.
            if (Room.CheckRights(User.GetClient(), true) || User.GetClient().GetHabbo().GetPermissions().HasRight("mod_tool"))
                return;

            //Room.GetRoomUserManager().RemoveUserFromRoom(User.GetClient(), true, true);
            //PolarEnvironment.GetGame().GetAchievementManager().ProgressAchievement(Session, "ACH_SelfModKickSeen", 1);


            #region Casa
            if (House != null)
            {
                // New RP
                RoleplayManager.Shout(Session, "*Ha echado a " + User.GetUsername() + " de la Casa*", 5);
                // Enviar a la Sala Exterior y Posición de la Puerta
                User.GetClient().GetRoleplay().ExitingHouse = true;
                User.GetClient().GetRoleplay().HouseX = House.DoorX;
                User.GetClient().GetRoleplay().HouseY = House.DoorY;
                User.GetClient().GetRoleplay().HouseZ = House.DoorZ;
                RoleplayManager.SendUserOld(User.GetClient(), House.RoomId, Session.GetHabbo().Username + " te ha echado de la casa.");
            }
            #endregion

            #region Apartament
            else if (ApartInside != null)
            {
                RoleplayManager.Shout(Session, "*Ha echado a " + User.GetUsername() + " del apartamento*", 5);
                RoleplayManager.SendUserOld2(User.GetClient(), ApartInside.LobbyId, Session.GetHabbo().Username + " te ha echado del apartamento.");
            }
            #endregion


        }
    }
}
