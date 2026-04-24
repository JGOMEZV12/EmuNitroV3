using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Quests;
using Polar.HabboHotel.GameClients;

namespace Polar.Communication.Packets.Outgoing.Quests
{
    internal class QuestCompletedComposer : ServerPacket
    {
        public GameClient Session { get; }
        public Quest Quest { get; }

        public QuestCompletedComposer(GameClient session, Quest Quest)
            : base(ServerPacketHeader.QuestCompletedMessageComposer)
        {
            this.Session = session;
            this.Quest = Quest;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            int AmountInCat = PolarEnvironment.GetGame().GetQuestManager().GetAmountOfQuestsInCategory(Quest.Category);
            int Number = Quest == null ? AmountInCat : Quest.Number;
            int UserProgress = Quest == null ? 0 : Session.GetHabbo().GetQuestProgress(Quest.Id);

           packet.WriteString(Quest.Category);
            packet.WriteInteger(Number); // Quest progress in this cat
            packet.WriteInteger((Quest.Name.Contains("xmas2012")) ? 1 : AmountInCat); // Total quests in this cat
            packet.WriteInteger(Quest == null ? 3 : Quest.RewardType); // Reward type (1 = Snowflakes, 2 = Love hearts, 3 = Pixels, 4 = Seashells, everything else is pixels
            packet.WriteInteger(Quest == null ? 0 : Quest.Id); // Quest id
            packet.WriteBoolean(Quest == null ? false : Session.GetHabbo().GetStats().QuestID == Quest.Id); // Quest started
           packet.WriteString(Quest == null ? string.Empty : Quest.ActionName);
           packet.WriteString(Quest == null ? string.Empty : Quest.DataBit);
            packet.WriteInteger(Quest == null ? 0 : Quest.Reward);
           packet.WriteString(Quest == null ? string.Empty : Quest.Name);
            packet.WriteInteger(UserProgress); // Current progress
            packet.WriteInteger(Quest == null ? 0 : Quest.GoalData); // Target progress
            packet.WriteInteger(Quest == null ? 0 : Quest.TimeUnlock); // "Next quest available countdown" in seconds
           packet.WriteString("");
           packet.WriteString("");
            packet.WriteBoolean(true); // ?
            packet.WriteBoolean(true); // Activate next quest..
        }
    }
}
