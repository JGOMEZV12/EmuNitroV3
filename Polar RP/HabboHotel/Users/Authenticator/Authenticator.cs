using System;
using System.Data;

namespace Polar.HabboHotel.Users.Authenticator
{
    public static class HabboFactory
    {
        /// <summary>
        /// Genera el objeto Habbo usando DataRows ya precargados por UserDataFactory.
        /// dPoll, dRecipes y dStats pueden ser null (lookup por ID, IsBot, etc.).
        /// Con esto el constructor de Habbo NO hace ninguna query a BD.
        /// </summary>
        public static Habbo GenerateHabbo(DataRow Row, DataRow UserInfo,
            DataRow pollRow, DataTable recipesTable, DataRow statsRow)
        {
            return new Habbo(
                Convert.ToInt32(Row["id"]),
                Convert.ToString(Row["username"]),
                Convert.ToInt32(Row["rank"]),
                Convert.ToString(Row["motto"]),
                Convert.ToString(Row["look"]),
                Convert.ToString(Row["gender"]),
                Convert.ToInt32(Row["credits"]) <= 0 ? 0 : Convert.ToInt32(Row["credits"]),
                Convert.ToInt32(Row["activity_points"]) <= 0 ? 0 : Convert.ToInt32(Row["activity_points"]),
                Convert.ToInt32(Row["home_room"]),
                PolarEnvironment.EnumToBool(Row["block_newfriends"].ToString()),
                Convert.ToInt32(Row["last_online"]),
                PolarEnvironment.EnumToBool(Row["hide_online"].ToString()),
                PolarEnvironment.EnumToBool(Row["hide_inroom"].ToString()),
                Convert.ToDouble(Row["account_created"]),
                Convert.ToInt32(Row["vip_points"]) <= 0 ? 0 : Convert.ToInt32(Row["vip_points"]),
                Convert.ToString(Row["machine_id"]),
                Convert.ToString(Row["volume"]),
                PolarEnvironment.EnumToBool(Row["chat_preference"].ToString()),
                PolarEnvironment.EnumToBool(Row["focus_preference"].ToString()),
                PolarEnvironment.EnumToBool(Row["pets_muted"].ToString()),
                PolarEnvironment.EnumToBool(Row["bots_muted"].ToString()),
                PolarEnvironment.EnumToBool(Row["advertising_report_blocked"].ToString()),
                Convert.ToDouble(Row["last_change"].ToString()),
                Convert.ToInt32(Row["event_points"]),
                PolarEnvironment.EnumToBool(Convert.ToString(Row["ignore_invites"])),
                Convert.ToDouble(Row["time_muted"]),
                Convert.ToDouble(UserInfo["trading_locked"]),
                PolarEnvironment.EnumToBool(Row["allow_gifts"].ToString()),
                Convert.ToInt32(Row["friend_bar_state"]),
                PolarEnvironment.EnumToBool(Row["disable_forced_effects"].ToString()),
                PolarEnvironment.EnumToBool(Row["allow_mimic"].ToString()),
                Convert.ToInt32(Row["rank_vip"]),
                false,
                Convert.ToString(Row.Table.Columns.Contains("colour") ? Row["colour"] : ""),
                Convert.ToString(Row.Table.Columns.Contains("prefix") ? Row["prefix"] : ""),
                Row["talent_status"].ToString(),
                Row["nux_user"].ToString() == "true",
                Convert.ToByte(Row["targeted_buy"]),
                Convert.ToInt32(Row["citizenship_level"]),
                Convert.ToInt32(Row["online"]),
                Convert.ToString(Row["client_pin"]),
                Convert.ToInt32(Row["uniqueToken"]),
                Convert.ToInt32(Row["backgroundId"]),
                Convert.ToInt32(Row["standId"]),
                Convert.ToInt32(Row["overlayId"]),
                // Nuevos parámetros precargados — null si IsBot o lookup por ID
                pollRow,
                recipesTable,
                statsRow);
        }
    }
}