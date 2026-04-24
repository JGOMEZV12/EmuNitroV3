using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Users;
using Polar.HabboHotel.Users.Messenger;
using Polar.HabboHotel.Users.Relationships;

namespace Polar.Communication.Packets.Outgoing.Messenger
{
    internal class BuddyListComposer : ServerPacket
    {
        public ICollection<MessengerBuddy> Friends { get; }
        public Habbo Player { get; }
        public int pages { get; }
        public int page { get; }
        public BuddyListComposer(ICollection<MessengerBuddy> friends, Habbo player, int pages, int page)
            : base(ServerPacketHeader.BuddyListMessageComposer)
        {
            this.Friends = friends;
            this.Player = player;
            this.pages = pages;
            this.page = page;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(1);// Pages
            packet.WriteInteger(0);// Page

            var friendCount = Friends.Count;

            packet.WriteInteger(friendCount);

            /*foreach (var gp in groups)
            {
                packet.WriteInteger(int.MinValue + gp.Id);
                packet.WriteString(gp.Name);
                packet.WriteInteger(1);//Gender.
                packet.WriteBoolean(true);
                packet.WriteBoolean(false);
                packet.WriteString(gp.Badge);
                packet.WriteInteger(1); // category id
                packet.WriteString(string.Empty);
                packet.WriteString("Chat de Grupo");//Alternative name?
                packet.WriteString(string.Empty);
                packet.WriteBoolean(true);
                packet.WriteBoolean(false);
                packet.WriteBoolean(false);//Pocket Habbo user.
                packet.WriteShort(0);
            }*/

            foreach (MessengerBuddy Friend in Friends.ToList())
            {
                Relationship Relationship = Player.Relationships.FirstOrDefault(x => x.Value.UserId == Convert.ToInt32(Friend.UserId)).Value;

                packet.WriteInteger(Friend.Id);
               packet.WriteString(Friend.mUsername);
                packet.WriteInteger(1);//Gender.
                packet.WriteBoolean(Friend.IsOnline || Friend.isBot);
                packet.WriteBoolean(Friend.IsOnline && Friend.InRoom || Friend.isBot);
               packet.WriteString(Friend.IsOnline || Friend.isBot ? Friend.mLook : string.Empty);
                packet.WriteInteger(0); // category id
               packet.WriteString(Friend.IsOnline  || Friend.isBot ? Friend.mMotto : string.Empty);
               packet.WriteString(string.Empty);//Alternative name?
               packet.WriteString(string.Empty);
                packet.WriteBoolean(true);
                packet.WriteBoolean(false);
                packet.WriteBoolean(false);//Pocket Habbo user.
                packet.WriteShort(Relationship == null ? 0 : Relationship.Type);
            }

        }
    }
}
