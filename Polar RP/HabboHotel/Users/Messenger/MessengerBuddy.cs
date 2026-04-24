using System;
using System.Linq;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users.Relationships;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Groups;

namespace Polar.HabboHotel.Users.Messenger
{
    public class MessengerBuddy
    {
        #region Fields

        public int UserId;
        public bool mAppearOffline;
        public bool mHideInroom;
        public int mLastOnline;
        public string mLook;
        public string mMotto;
        public bool isBot;

        public GameClient client;
        private Room currentRoom;
        public string mUsername;
        public int groupID;

        #endregion

        #region Return values

        public int Id
        {
            get { return UserId; }
        }

        public bool IsOnline
        {
            get
            {
                return (client != null && client.GetHabbo() != null && client.GetHabbo().GetMessenger() != null &&
                        !client.GetHabbo().GetMessenger().AppearOffline || isBot);
            }
        }

        private GameClient Client
        {
            get { return client; }
            set { client = value; }
        }

        public bool InRoom
        {
            get { return (currentRoom != null); }
        }

        public Room CurrentRoom
        {
            get { return currentRoom; }
            set { currentRoom = value; }
        }

        #endregion

        #region Constructor

        public MessengerBuddy(int UserId, string pUsername, string pLook, string pMotto, int pLastOnline,
                                bool pAppearOffline, bool pHideInroom, bool isBot)
        {
            this.UserId = UserId;
            mUsername = pUsername;
            mLook = pLook;
            mMotto = pMotto;
            mLastOnline = pLastOnline;
            mAppearOffline = pAppearOffline;
            mHideInroom = pHideInroom;
            this.isBot = isBot;
        }

        public MessengerBuddy(int groupID)
        {
            Group group = null;
            if (!PolarEnvironment.GetGame().GetGroupManager().TryGetGroup(groupID, out group))
                return;
            this.UserId = int.MinValue + groupID;
            mUsername = group.Name;

            mMotto = "";
            mLastOnline = 0;
            mAppearOffline = false;
            mHideInroom = true;
            mLook = group.Badge;
            this.groupID = groupID;
        }

        #endregion

        #region Methods
        public void UpdateUser(GameClient client)
        {
            this.client = client;
            if (client != null && client.GetHabbo() != null)
                currentRoom = client.GetHabbo().CurrentRoom;

            try
            {
                GameClient newClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(UserId);
                if (newClient != null && newClient.GetHabbo() != null)
                {
                    mUsername = newClient.GetHabbo().Username;
                    mLook = newClient.GetHabbo().Look;
                    mMotto = newClient.GetHabbo().Motto;
                }
            } catch { }
        }

        public void Serialize(ServerPacket Message, GameClient Session)
        {
            Relationship Relationship = null;

            if (Session != null && Session.GetHabbo() != null && Session.GetHabbo().Relationships != null)
                Relationship = Session.GetHabbo().Relationships.FirstOrDefault(x => x.Value.UserId == Convert.ToInt32(UserId)).Value;

            int y = Relationship == null ? 0 : Relationship.Type;

            Message.WriteInteger(UserId);
            Message.WriteString(mUsername);
            Message.WriteInteger(1);
            Message.WriteBoolean(!mAppearOffline || Session.GetHabbo().GetPermissions().HasRight("mod_tool") ? IsOnline : false);
            Message.WriteBoolean(!mHideInroom || Session.GetHabbo().GetPermissions().HasRight("mod_tool") ? InRoom : false);
            Message.WriteString(IsOnline || isBot ? mLook : "");
            Message.WriteInteger(0); // categoryid
            Message.WriteString(mMotto);
            Message.WriteString(string.Empty); // Facebook username
            Message.WriteString(string.Empty);
            Message.WriteBoolean(true); // Allows offline messaging
            Message.WriteBoolean(false); // ?
            Message.WriteBoolean(false); // Uses phone
            Message.WriteShort(y);
        }

        public void SerializeGroupchat(ServerPacket Message, GameClient Session)
        {

            int y = 0;

            Message.WriteInteger(UserId);
            Message.WriteString(mUsername);
            Message.WriteInteger(0);
            Message.WriteBoolean(false);
            Message.WriteBoolean(true);
            Message.WriteString(mLook);
            Message.WriteInteger(1); // categoryid
            Message.WriteString(mMotto);
            Message.WriteString(string.Empty); // Facebook username
            Message.WriteString(string.Empty);
            Message.WriteBoolean(false); // Allows offline messaging
            Message.WriteBoolean(false); // ?
            Message.WriteBoolean(false); // Uses phone
            Message.WriteShort(y);
        }
        #endregion
    }
}