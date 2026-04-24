using System;
using System.Collections.Generic;
using Polar.HabboHotel.Groups;

namespace Polar.Communication.Packets.Outgoing.Groups
{
    internal class GroupForumListingsMessageComposer : ServerPacket
    {
        public int selectType { get; }
        public int qtdForums { get; }
        public int startIndex { get; }
        public List<Group> Groups { get; }
        public GroupForumListingsMessageComposer(int SelectType, int QtdForums, int StartIndex, List<Group> groups)
            : base(ServerPacketHeader.GroupForumListingsMessageComposer)
        {
            this.selectType = SelectType;
            this.qtdForums = QtdForums;
            this.startIndex = StartIndex;
            this.Groups = groups;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(selectType);

            if (selectType == 0 || selectType == 1)
            {
                packet.WriteInteger(qtdForums == 0 ? 1 : qtdForums);
                packet.WriteInteger(startIndex);
                packet.WriteInteger(Groups.Count);

                foreach (Group Group in Groups)
                {
                    packet.WriteInteger(Group.Id);
                    packet.WriteString(Group.Name);
                    packet.WriteString(string.Empty);
                    packet.WriteString(Group.Badge);
                    packet.WriteInteger(0);
                    packet.WriteInteger((int)Math.Round(Group.ForumScore));
                    packet.WriteInteger(Group.ForumMessagesCount);
                    packet.WriteInteger(0);
                    packet.WriteInteger(0);
                    packet.WriteInteger(Group.ForumLastPosterId);
                    packet.WriteString(Group.ForumLastPosterName);
                    packet.WriteInteger(Group.ForumLastPostTime);
                    packet.WriteInteger(0);
                }
            }
            else if (selectType == 2)
            {
                packet.WriteInteger(Groups.Count == 0 ? 1 : Groups.Count);
                packet.WriteInteger(startIndex);
                packet.WriteInteger(Groups.Count);

                foreach (Group Group in Groups)
                {
                    packet.WriteInteger(Group.Id);
                    packet.WriteString(Group.Name);
                    packet.WriteString(string.Empty);
                    packet.WriteString(Group.Badge);
                    packet.WriteInteger(0);
                    packet.WriteInteger((int)Math.Round(Group.ForumScore));
                    packet.WriteInteger(Group.ForumMessagesCount);
                    packet.WriteInteger(0);
                    packet.WriteInteger(0);
                    packet.WriteInteger(Group.ForumLastPosterId);
                    packet.WriteString(Group.ForumLastPosterName);
                    packet.WriteInteger(Group.ForumLastPostTime);
                    packet.WriteInteger(0);
                }
            }
            else
            {
                packet.WriteInteger(1);
                packet.WriteInteger(startIndex);
                packet.WriteInteger(0);
            }
        }
    }
}