using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.Core;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;

namespace Polar.Communication.Packets.Incoming.Messenger
{
    internal class SendMsgEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            if (Session == null || Session.GetHabbo() == null || Session.GetHabbo().GetMessenger() == null)
                return;

            int userId = Packet.PopInt();
            if (userId == 0 || userId == Session.GetHabbo().Id)
                return;

            string message = Packet.PopString();
            if (string.IsNullOrWhiteSpace(message)) return;
            if (Session.GetHabbo().TimeMuted > 0)
            {
                Session.SendWhisper("Oops, has sido silenciado durante 15 segundos, no podrás enviar mensajes durante este lapso de tiempo.");
                return;
            }

            string word;
            if (!Session.GetHabbo().GetPermissions().HasRight("word_filter_override") &&
                PolarEnvironment.GetGame().GetChatManager().GetFilter().IsUnnaceptableWord(message, out word))
            {
                Session.GetHabbo().BannedPhraseCount++;
                if (Session.GetHabbo().BannedPhraseCount >= 1)
                {
                    Session.GetHabbo().TimeMuted = 25;
                    Session.SendNotification("¡Has sido silenciad@ mientras un moderador revisa tu caso, al parecer nombraste un hotel! <b>Aviso " + Session.GetHabbo().BannedPhraseCount + "/5</b>");
                    PolarEnvironment.GetGame().GetClientManager().StaffAlert(new RoomNotificationComposer("Alerta de publicista:",
                        "Atención, se ha mencionado la palabra <b>" + word.ToUpper() + "</b><br><br><b>Frase:</b><br><i>" + message +
                        "</i>.<br><br><b>Tipo</b><br>Spam por consola.\r\n" + "- Este usuario: <b>" +
                        Session.GetHabbo().Username + "</b>", "filter", "", ""));
                }
                if (Session.GetHabbo().BannedPhraseCount >= 5)
                {
                    PolarEnvironment.GetGame().GetModerationManager().BanUser("Proton", HabboHotel.Moderation.ModerationBanType.USERNAME, Session.GetHabbo().Username, "Baneado por hacer Spam con la Frase (" + message + ")", (PolarEnvironment.GetUnixTimestamp() + 78892200));
                    Session.Disconnect(false);
                    return;
                }
                return;
            }

            Session.GetHabbo().GetMessenger().SendInstantMessage(userId, message);

        }
    }
}