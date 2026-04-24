using System.Collections.Generic;
using System.Collections.Concurrent;
using Polar.HabboHotel.Achievements;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users.Badges;
using Polar.HabboHotel.Users.Messenger;
using Polar.HabboHotel.Users.Relationships;
using Polar.HabboHotel.Subscriptions;

namespace Polar.HabboHotel.Users.UserDataManagement
{
    public class UserData
    {
        public int    userID { get; }
        public Habbo  user   { get; }

        public ConcurrentDictionary<string, UserAchievement> achievements { get; }
        public Dictionary<int, Relationship>                 Relations    { get; }
        public Dictionary<string, Subscription>              subscriptions { get; }
        public Dictionary<int, MessengerBuddy>               friends      { get; }
        public Dictionary<int, MessengerRequest>             requests     { get; }
        public Dictionary<int, int>                          quests       { get; }
        public List<Badge>                                   badges       { get; }
        public List<int>                                     favouritedRooms { get; }
        public List<int>                                     ignores      { get; }
        public List<RoomData>                                rooms        { get; }
        public HashSet<int>                                  SuggestedPolls { get; }

        public UserData(
            int userID,
            ConcurrentDictionary<string, UserAchievement> achievements,
            List<int>                                     favouritedRooms,
            List<int>                                     ignores,
            List<Badge>                                   badges,
            Dictionary<int, MessengerBuddy>               friends,
            Dictionary<int, MessengerRequest>             requests,
            List<RoomData>                                rooms,
            Dictionary<int, int>                          quests,
            Habbo                                         user,
            Dictionary<int, Relationship>                 Relations,
            Dictionary<string, Subscription>              subscriptions)
        {
            this.userID         = userID;
            this.achievements   = achievements;
            this.favouritedRooms = favouritedRooms;
            this.ignores        = ignores;
            this.badges         = badges;
            this.friends        = friends;
            this.requests       = requests;
            this.rooms          = rooms;
            this.quests         = quests;
            this.user           = user;
            this.Relations      = Relations;
            this.subscriptions  = subscriptions;
        }
    }
}
