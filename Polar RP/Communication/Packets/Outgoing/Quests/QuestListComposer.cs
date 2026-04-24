using System;
using System.Collections.Generic;

using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.Quests;
using Polar.HabboHotel.GameClients;

namespace Polar.Communication.Packets.Outgoing.Quests
{
    public class QuestListComposer : ServerPacket
    {
        public GameClient Session { get; }
        public bool Send { get; }
        public Dictionary<string, Quest> UserQuests { get; }
        public Dictionary<string, int> UserQuestGoals { get; }
        public QuestListComposer(GameClient session, List<Quest> Quests, bool send, Dictionary<string, int> userQuestGoals, Dictionary<string, Quest> userQuests)
            : base(ServerPacketHeader.QuestListMessageComposer)
        {
            this.Session = session;
            this.Send = send;
            this.UserQuests = userQuests;
            this.UserQuestGoals = userQuestGoals;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(UserQuests.Count);

            // Active ones first
            foreach (var UserQuest in UserQuests)
            {
                if (UserQuest.Value == null)
                    continue;

                SerializeQuest(packet, Session, UserQuest.Value, UserQuest.Key);
            }

            // Dead ones last
            foreach (var UserQuest in UserQuests)
            {
                if (UserQuest.Value != null)
                    continue;

                SerializeQuest(packet, Session, UserQuest.Value, UserQuest.Key);
            }

            packet.WriteBoolean(Send);
        }

        private void SerializeQuest(ServerPacket Message, GameClient Session, Quest Quest, string Category)
        {
            if (Message == null || Session == null)
                return;

            int AmountInCat = PolarEnvironment.GetGame().GetQuestManager().GetAmountOfQuestsInCategory(Category);
            int Number = Quest == null ? AmountInCat : Quest.Number - 1;
            int UserProgress = Quest == null ? 0 : Session.GetHabbo().GetQuestProgress(Quest.Id);

            if (Quest != null && Quest.IsCompleted(UserProgress))
                Number++;

            Message.WriteString(Category);
            Message.WriteInteger(Quest == null ? 0 : ((Quest.Category.Contains("xmas2012")) ? 0 : Number));  // Quest progress in this cat
            Message.WriteInteger(Quest == null ? 0 : (Quest.Category.Contains("xmas2012")) ? 0 : AmountInCat); // Total quests in this cat
            Message.WriteInteger(Quest == null ? 3 : Quest.RewardType);// Reward type (1 = Snowflakes, 2 = Love hearts, 3 = Pixels, 4 = Seashells, everything else is pixels
            Message.WriteInteger(Quest == null ? 0 : Quest.Id); // Quest id
            Message.WriteBoolean(Quest == null ? false : Session.GetHabbo().GetStats().QuestID == Quest.Id);  // Quest started
            Message.WriteString(Quest == null ? string.Empty : Quest.ActionName);
            Message.WriteString(Quest == null ? string.Empty : Quest.DataBit);
            Message.WriteInteger(Quest == null ? 0 : Quest.Reward);
            Message.WriteString(Quest == null ? string.Empty : Quest.Name);
            Message.WriteInteger(UserProgress); // Current progress
            Message.WriteInteger(Quest == null ? 0 : Quest.GoalData); // Target progress
            Message.WriteInteger(Quest == null ? 0 : Quest.TimeUnlock); // "Next quest available countdown" in seconds
            Message.WriteString("");
            Message.WriteString("");
            Message.WriteBoolean(true);
        }
    }
}