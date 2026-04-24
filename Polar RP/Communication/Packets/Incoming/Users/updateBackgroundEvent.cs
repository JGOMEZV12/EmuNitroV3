using Polar.Communication.Packets.Outgoing.Moderation;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Global;
using Polar.HabboHotel.Quests;
using Polar.HabboHotel.Rooms;
using Polar.HabboRoleplay.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Polar.Communication.Packets.Incoming.Users
{
    internal class UpdateBackgroundEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            // Validación inicial reforzada
            if (Session?.GetHabbo() == null || Packet == null)
                return;

            int backgroundId = Packet.PopInt();
            int standId = Packet.PopInt();
            int overlayId = Packet.PopInt();
            // Obtener RoomUser de forma segura
            var roomUser = Session.GetRoomUser();
            if (roomUser == null)
                return;

            // Validar estado en sala
            if (!Session.GetHabbo().InRoom || Session.GetHabbo().CurrentRoom == null)
                return;

            // Actualizar base de datos con seguridad
            var dbManager = PolarEnvironment.GetDatabaseManager();
            if (dbManager != null)
            {
                using (IQueryAdapter dbClient = dbManager.GetQueryReactor())
                {
                    if (dbClient != null)
                    {
                        dbClient.SetQuery("UPDATE users SET backgroundId = @bgId, standId = @sId, overlayId = @overId WHERE id = @id");
                        dbClient.AddParameter("bgId", backgroundId);
                        dbClient.AddParameter("sId", standId);
                        dbClient.AddParameter("overId", overlayId);
                        dbClient.AddParameter("id", Session.GetHabbo().Id);
                        dbClient.RunQuery();
                    }
                }
            }

            // Actualizar apariencia
            Session.GetHabbo().BackgroundId = backgroundId;
            Session.GetHabbo().StandId = standId;
            Session.GetHabbo().OverlayId = overlayId;

            // Notificar sala
            if (Session.GetHabbo().CurrentRoom != null)
            {
                var currentRoomUser = Session.GetHabbo().CurrentRoom.GetRoomUserManager()?.GetRoomUserByHabbo(Session.GetHabbo().Id);
                if (currentRoomUser != null)
                {
                    Session.SendMessage(new UserChangeComposer(currentRoomUser, true));
                    Session.GetHabbo().CurrentRoom.SendMessage(new UserChangeComposer(currentRoomUser, false));
                }
            }
        }
    }
}