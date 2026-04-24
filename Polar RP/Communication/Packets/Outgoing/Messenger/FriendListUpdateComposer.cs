using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Users;
using Polar.HabboHotel.Users.Relationships;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.Users.Messenger;

namespace Polar.Communication.Packets.Outgoing.Messenger
{
    internal class FriendListUpdateComposer : ServerPacket
    {
        public int FriendId { get; }
        public Group Group { get; }
        public int State;
        public MessengerBuddy Buddy { get; }
        public GameClient Session { get; }
        public FriendListUpdateComposer(int FriendId)
            : base(ServerPacketHeader.FriendListUpdateMessageComposer)
        {
            this.FriendId = FriendId;
            Compose(this);

        }

        public FriendListUpdateComposer(Group group, int state)
    : base(ServerPacketHeader.FriendListUpdateMessageComposer)
        {
            this.Group = group;
            this.State = state;

            if (Group == null)
            {
                // Manejar el caso null aquí
                // Por ejemplo, podrías lanzar una excepción más específica
                // o inicializar con valores por defecto
                throw new ArgumentNullException(nameof(Group), "Group cannot be null");
            }

            Compose(this);
        }


        public FriendListUpdateComposer(GameClient habbo, MessengerBuddy Buddy)
            : base(ServerPacketHeader.FriendListUpdateMessageComposer)
        {
            this.Session = habbo;
            this.Buddy = Buddy;
            Compose(this);

        }
        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(1);//Category Count
            packet.WriteInteger(1);//category ID
            packet.WriteString("Grupos");
            // Si Group es null, no escribir ningún grupo
            if (Group == null)
            {
                packet.WriteInteger(0); // Updates Count = 0
                return;
            }
            packet.WriteInteger(0);//Update
            if (Buddy.UserId > 0)
            {
                
                Relationship Relationship = Session.GetHabbo().Relationships.FirstOrDefault(x => x.Value.UserId == Convert.ToInt32(Buddy.UserId)).Value;
                int y = Relationship == null ? 0 : Relationship.Type;

                packet.WriteInteger(Buddy.UserId);
                packet.WriteString(Buddy.mUsername);
                packet.WriteInteger(1);
                if (!Buddy.mAppearOffline || Session.GetHabbo().GetPermissions().HasRight("mod_tool"))
                    packet.WriteBoolean(Buddy.IsOnline || Buddy.isBot);
                else
                    packet.WriteBoolean(false);

                if (!Buddy.mHideInroom || Session.GetHabbo().GetPermissions().HasRight("mod_tool"))
                    packet.WriteBoolean(Buddy.InRoom);
                else
                    packet.WriteBoolean(false);

                packet.WriteString(Buddy.mLook);//Habbo.IsOnline ? Habbo.Look : "");
                packet.WriteInteger(0); // categoryid
                packet.WriteString(Buddy.mMotto);
                packet.WriteString(string.Empty); // Facebook username
                packet.WriteString(string.Empty);
                packet.WriteBoolean(true); // Allows offline messaging
                packet.WriteBoolean(false); // ?
                packet.WriteBoolean(false); // Uses phone
                packet.WriteShort(y);
            }
        }
    }
}
