using System;
using System.Linq;
using System.Text;
using System.Data;
using System.Collections.Generic;
using Polar.HabboHotel.Users;
using Polar.HabboHotel.GameClients;
using Polar.Database.Interfaces;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Moderators
{
    class UserInfoCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_user_info"; }
        }

        public string Parameters
        {
            get { return "%username%"; }
        }

        public string Description
        {
            get { return "Ver información de un ciudadano."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("Por favor, escribe el nombre de usuario que deseas revisar.", 1);
                return;
            }

            DataRow UserData = null;
            DataRow UserInfo = null;
            string Username = Params[1];

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT `id`,`username`,`mail`,`rank`,`motto`,`credits`,`activity_points`,`vip_points`,`event_points`,`online`,`rank_vip`,`ip_last`,`ip_reg`,`birth` FROM users WHERE `username` = @Username LIMIT 1");
                dbClient.AddParameter("Username", Username);
                UserData = dbClient.getRow();
            }

            if (UserData == null)
            {
                Session.SendWhisper("Oops, este usuario no se encuentra en la base de datos (" + Username + ")!", 1);
                return;
            }

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT * FROM `user_info` WHERE `user_id` = '" + Convert.ToInt32(UserData["id"]) + "' LIMIT 1");
                UserInfo = dbClient.getRow();
                if (UserInfo == null)
                {
                    dbClient.RunQuery("INSERT INTO `user_info` (`user_id`) VALUES ('" + Convert.ToInt32(UserData["id"]) + "')");

                    dbClient.SetQuery("SELECT * FROM `user_info` WHERE `user_id` = '" + Convert.ToInt32(UserData["id"]) + "' LIMIT 1");
                    UserInfo = dbClient.getRow();
                }
            }

            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Username);

            StringBuilder HabboInfo = new StringBuilder();
            HabboInfo.Append(Convert.ToString(UserData["username"]) + " datos:\r\r");
            HabboInfo.Append("Información General:\r");
            HabboInfo.Append("ID: " + String.Format("{0:N0}", Convert.ToInt32(UserData["id"])) + "\r");
            HabboInfo.Append("Rank: " + Convert.ToInt32(UserData["rank"]) + "\r");
            HabboInfo.Append("VIP Rank: " + Convert.ToInt32(UserData["rank_vip"]) + "\r");
            HabboInfo.Append("Email: " + Convert.ToString(UserData["mail"]) + "\r");
            HabboInfo.Append("última IP: " + Convert.ToString(UserData["ip_last"]) + "\r");
            HabboInfo.Append("Registro IP: " + Convert.ToString(UserData["ip_reg"]) + "\r");
            HabboInfo.Append("Cumpleaños: " + Convert.ToString(UserData["birth"]) + "\r");
            HabboInfo.Append("Estado: " + (TargetClient != null ? "conectado" : "desconectado") + "\r\r");

            HabboInfo.Append("Información financiera:\r");
            HabboInfo.Append("Créditos: " + String.Format("{0:N0}", Convert.ToInt32(UserData["credits"])) + "\r");
            HabboInfo.Append("Saldo de tlf: " + String.Format("{0:N0}", Convert.ToInt32(UserData["activity_points"])) + "\r");
            HabboInfo.Append("Rubies: " + String.Format("{0:N0}", Convert.ToInt32(UserData["vip_points"])) + "\r");
            HabboInfo.Append("Puntos de eventos: " + String.Format("{0:N0}", Convert.ToInt32(UserData["event_points"])) + "\r\r");

            HabboInfo.Append("Información de moderación:\r");
            HabboInfo.Append("Baneos: " + String.Format("{0:N0}", Convert.ToInt32(UserInfo["bans"])) + "\r");
            HabboInfo.Append("CFHs Sent: " + String.Format("{0:N0}", Convert.ToInt32(UserInfo["cfhs"])) + "\r");
            HabboInfo.Append("Abusivo CFHs: " + String.Format("{0:N0}", Convert.ToInt32(UserInfo["cfhs_abusive"])) + "\r");

            if (TargetClient != null)
            {
                HabboInfo.Append("Localización:\r");
                if (!TargetClient.GetHabbo().InRoom)
                    HabboInfo.Append("No está en ninguna sala.\r");
                else
                {
                    HabboInfo.Append("Sala: " + TargetClient.GetHabbo().CurrentRoom.Name + " (" + TargetClient.GetHabbo().CurrentRoom.RoomId + ")\r");
                    HabboInfo.Append("Dueño de sala: " + TargetClient.GetHabbo().CurrentRoom.OwnerName + "\r");
                    HabboInfo.Append("Usuarios en sala: " + TargetClient.GetHabbo().CurrentRoom.UserCount + "/" + TargetClient.GetHabbo().CurrentRoom.UsersMax + "\r");
                    HabboInfo.Append("Invisible: " + (TargetClient.GetRoleplay().Invisible ? "Yes" : "No") + "\r");
                }
            }
            Session.SendNotification(HabboInfo.ToString());
        }
    }
}
