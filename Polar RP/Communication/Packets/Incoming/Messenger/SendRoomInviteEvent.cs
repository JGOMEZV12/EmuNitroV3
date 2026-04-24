using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.Core;
using Polar.Utilities;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Messenger;
using Polar.Database.Interfaces;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;

namespace Polar.Communication.Packets.Incoming.Messenger
{
    internal class SendRoomInviteEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            if (Session.GetHabbo().TimeMuted > 0)
            {
                Session.SendNotification("Oops, estas muteado - No puedes enviar invitaciones a salas");
                return;
            }

            int Amount = Packet.PopInt();
            if (Amount > 500)
                return; // don't send at all

            List<int> Targets = new List<int>();
            for (int i = 0; i < Amount; i++)
            {
                int uid = Packet.PopInt();
                if (i < 100) // limit to 100 people, keep looping until we fulfil the request though
                {
                    Targets.Add(uid);
                }
            }

            string Message = StringCharFilter.Escape(Packet.PopString());
            if (Message.Length > 121)
                Message = Message.Substring(0, 121);

            string word;
            if (!Session.GetHabbo().GetPermissions().HasRight("word_filter_override") &&
                PolarEnvironment.GetGame().GetChatManager().GetFilter().IsUnnaceptableWord(Message, out word))
            {
                Session.GetHabbo().BannedPhraseCount++;
                if (Session.GetHabbo().BannedPhraseCount >= 1)
                {
                    Session.GetHabbo().TimeMuted = 25;
                    Session.SendNotification("¡Has sido silenciad@ mientras un moderador revisa tu caso, al parecer nombraste un hotel! Aviso " + Session.GetHabbo().BannedPhraseCount + "/3");
                    PolarEnvironment.GetGame().GetClientManager().StaffAlert(new RoomNotificationComposer("Alerta de publicista:",
                        "Atención, se ha mencionado la palabra <b>" + word.ToUpper() + "</b><br><br><b>Frase:</b><br><i>" + Message +
                        "</i>.<br><br><b>Tipo</b><br>Spam por invitación en consola.\r\n" + "- Este usuario: <b>" +
                        Session.GetHabbo().Username + "</b>", "filter", "", ""));
                }
                if (Session.GetHabbo().BannedPhraseCount >= 3)
                {
                    PolarEnvironment.GetGame().GetModerationManager().BanUser("System", HabboHotel.Moderation.ModerationBanType.USERNAME, Session.GetHabbo().Username, "Baneado por hacer Spam con la Frase (" + Message + ")", (PolarEnvironment.GetUnixTimestamp() + 78892200));
                    Session.Disconnect(true);
                    return;
                }
                return;
            }

            foreach (int UserId in Targets)
            {
                if (!Session.GetHabbo().GetMessenger().FriendshipExists(UserId))
                    continue;

                GameClient Client = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(UserId);
                if (Client == null || Client.GetHabbo() == null || Client.GetHabbo().AllowMessengerInvites == true || Client.GetHabbo().AllowConsoleMessages == false)
                    continue;

                Client.SendMessage(new RoomInviteComposer(Session.GetHabbo().Id, Message));
                Client.SendMessage(new RoomBubbleNotificationComposer("eventoxx", "" + Session.GetHabbo().Username + " te invita a un nuevo evento en su sala. Su mensaje es " + Message + ".", "event:navigator/goto/" + Session.GetHabbo().CurrentRoomId));

            }

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("INSERT INTO `chatlogs_console_invitations` (`user_id`,`message`,`timestamp`) VALUES ('" + Session.GetHabbo().Id + "', @message, UNIX_TIMESTAMP())");
                dbClient.AddParameter("message", Message);
                dbClient.RunQuery();
            }
        }
    }
}