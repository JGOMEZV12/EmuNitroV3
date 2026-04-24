using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Quests;

namespace Polar.Communication.Packets.Incoming.Messenger
{
    internal class RequestBuddyEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            if (Session == null || Session.GetHabbo() == null || Session.GetHabbo().GetMessenger() == null)
                return;

            if (Session.GetRoleplay().Phone <= 0)
            {
                Session.SendNotification("No tienes ningún teléfono para agregar contactos.");
                return;
            }

            if (Session.GetHabbo().GetMessenger().RequestBuddy(Packet.PopString()))
                PolarEnvironment.GetGame().GetQuestManager().ProgressUserQuest(Session, QuestType.SOCIAL_FRIEND);
        }
    }
}
