using System;
using System.Linq;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;

namespace Polar.HabboHotel.Items.Wired
{
    public static class WiredVariableResolver
    {
        public static int? ResolveValue(Room instance, string token, Habbo user, Item item, WiredContext context)
        {
            if (string.IsNullOrEmpty(token)) return null;

            if (token.StartsWith("custom:"))
            {
                string varName = token.Substring(7);
                if (user != null)
                {
                    var userVar = instance.GetRoomUserVariableManager()?.GetAssignment(user.Id, varName);
                    if (userVar != null) return userVar.Value;
                }

                var roomVar = instance.GetRoomVariableManager()?.GetAssignment(varName);
                if (roomVar != null) return roomVar;

                if (item != null)
                {
                    var furniVar = instance.GetRoomFurniVariableManager()?.GetAssignment(item.Id, varName);
                    if (furniVar != null) return furniVar;
                }
            }
            else if (token.StartsWith("internal:"))
            {
                string property = token.Substring(9).ToLower();
                switch (property)
                {
                    case "avatar_id": return user?.Id;
                    case "item_id": return item?.Id;
                    case "room_id": return instance?.Id;
                    case "user_count": return instance?.UserCount;
                }
            }
            else if (int.TryParse(token, out int constant))
            {
                return constant;
            }

            return null;
        }

        public static string ResolveText(Room instance, string token, Habbo user, Item item, WiredContext context)
        {
            if (string.IsNullOrEmpty(token)) return "";

            if (token.StartsWith("custom:"))
            {
                string varName = token.Substring(7);
                if (user != null)
                {
                    var userVar = instance.GetRoomUserVariableManager()?.GetAssignment(user.Id, varName);
                    if (userVar != null) return userVar.Value.ToString();
                }

                var roomVar = instance.GetRoomVariableManager()?.GetAssignment(varName);
                if (roomVar != null) return roomVar.Value.ToString();

                if (item != null)
                {
                    var furniVar = instance.GetRoomFurniVariableManager()?.GetAssignment(item.Id, varName);
                    if (furniVar != null) return furniVar.Value.ToString();
                }
            }
            else if (token.StartsWith("internal:"))
            {
                string property = token.Substring(9).ToLower();
                switch (property)
                {
                    case "username": return user?.Username ?? "";
                    case "motto": return user?.Motto ?? "";
                    case "furni_name": return item?.GetBaseItem()?.PublicName ?? "";
                }
            }

            return token;
        }
    }
}
