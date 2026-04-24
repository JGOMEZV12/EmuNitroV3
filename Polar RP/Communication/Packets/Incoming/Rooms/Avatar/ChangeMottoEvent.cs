using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Polar.Utilities;
using Polar.Core;
using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Quests;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;

using Polar.Database.Interfaces;


namespace Polar.Communication.Packets.Incoming.Rooms.Avatar
{
    internal class ChangeMottoEvent : IPacketEvent
    {
        public void Parse(GameClient Session, ClientPacket Packet)
        {
            /*if (!Session.GetHabbo().GetPermissions().HasRight("change_motto"))
                return;*/
			
			if (Session.GetHabbo().TimeMuted > 0)
            {
                Session.SendNotification("Vaya, actualmente estás muteado: no puedes cambiar tu lmisión.");
                return;
            }

            if ((DateTime.Now - Session.GetHabbo().LastMottoUpdateTime).TotalSeconds <= 2.0)
            {
                Session.GetHabbo().MottoUpdateWarnings += 1;
                if (Session.GetHabbo().MottoUpdateWarnings >= 25)
                    Session.GetHabbo().SessionMottoBlocked = false;
                return;
            }

            if (Session.GetHabbo().SessionMottoBlocked)
                return;

            Session.GetHabbo().LastMottoUpdateTime = DateTime.Now;

            string newMotto = StringCharFilter.Escape(Packet.PopString().Trim());

            if (newMotto.Length > 38)
                newMotto = newMotto.Substring(0, 38);

            if (newMotto == Session.GetHabbo().Motto)
                return;

            string word;
            if (!Session.GetHabbo().GetPermissions().HasRight("word_filter_override"))
                newMotto = PolarEnvironment.GetGame().GetChatManager().GetFilter().IsUnnaceptableWord(newMotto, out word) ? "Spam" : newMotto;

            Session.GetHabbo().Motto = newMotto;

           /* if (newMotto.StartsWith("#"))
            {
                int delimiterIndex = newMotto.IndexOf('#');
                if (delimiterIndex != -1)
                {
                    string key = newMotto.Substring(0, delimiterIndex);
                }
            }*/

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("UPDATE `users` SET `motto` = @motto WHERE `id` = '" + Session.GetHabbo().Id + "' LIMIT 1");
                dbClient.AddParameter("motto", newMotto);
                dbClient.RunQuery();
            }

			PolarEnvironment.GetGame().GetQuestManager().ProgressUserQuest(Session, QuestType.PROFILE_CHANGE_MOTTO);
            PolarEnvironment.GetGame().GetAchievementManager().ProgressAchievement(Session, "ACH_Motto", 1);

            if (Session.GetHabbo().InRoom)
            {
                Room Room = Session.GetHabbo().CurrentRoom;
                if (Room == null)
                    return;

                RoomUser User = Room.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);
                if (User == null || User.GetClient() == null)
                    return;

                Room.SendMessage(new UserChangeComposer(User, false));
            }
        }
    }
}
