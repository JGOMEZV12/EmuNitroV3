using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Groups;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.Cache;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Interactions.Marriage
{
    class DivorceCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_interactions_divorce"; }
        }

        public string Parameters
        {
            get { return "%username%"; }
        }

        public string Description
        {
            get { return "divorciarse de su pareja"; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            if (Params.Length == 1)
            {
                Session.SendWhisper("comando es así: ':divorciarme nombre'.", 1);
                return;
            }

            if (Session.GetRoleplay().MarriedTo == 0)
            {
                Session.SendWhisper("¡No estás casado con nadie!", 1);
                return;
            }

            if (Session.GetHabbo().Credits < 50000)
            {
                Session.SendWhisper("¡Necesitas más de 50 mil dolares para divorciarte!", 1);
                return;
            }

            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);

            #endregion

            string Username = Params[1];
            #region Execute
            if (TargetClient == null)
            {
                /*Session.SendWhisper("¡Esta persona no se encuentra en linea o no existe!", 1);
                return;*/
                using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                 {

                     dbClient.SetQuery("SELECT * FROM `users` WHERE `username` = @Username LIMIT 1");
                     dbClient.AddParameter("Username", Username);
                     var UserData = dbClient.getRow();

                     if (UserData == null)
                     {
                         Session.SendWhisper("El usuario no existe.", 1);
                         return;
                     }

                     dbClient.SetQuery("SELECT `id` FROM `rp_stats` WHERE `id` = '" + UserData["id"] + "' LIMIT 1");
                     var Row = dbClient.getRow();

                     if (Row == null)
                     {
                         Session.SendWhisper("No pudo encontrar este usuario! Tal vez escribió su nombre equivocado", 1);
                         return;
                     }

                     int TargetMarriedTo = Convert.ToInt32(Row["id"]);

                     if (Session.GetRoleplay().MarriedTo != TargetMarriedTo)
                     {
                         Session.SendWhisper("Tu no estas casado con esta persona", 1);
                         return;
                     }
                     else
                     {
                         dbClient.RunQuery("UPDATE `rp_stats` SET `married_to` = '0' WHERE `id` = '" + UserData["id"] + "'");
                         dbClient.RunQuery("DELETE FROM `user_badges` WHERE `badge_id` = 'WD0' AND `user_id` = '" + UserData["id"] + "'");
                         dbClient.RunQuery("UPDATE `rp_stats` SET `married_to` = '0' WHERE `id` = '" + Session.GetHabbo().Id + "'");

                         Session.GetRoleplay().MarriedTo = 0;
                         if (Session.GetHabbo().GetBadgeComponent().HasBadge("WD0"))
                             Session.GetHabbo().GetBadgeComponent().RemoveBadge("WD0");

                         if (PolarEnvironment.GetGame().GetCacheManager().ContainsUser(Session.GetHabbo().Id))
                             PolarEnvironment.GetGame().GetCacheManager().TryUpdateUser(Session);

                         UserCache Junk;

                         if (PolarEnvironment.GetGame().GetCacheManager().ContainsUser(Convert.ToInt32(UserData["id"])))
                             PolarEnvironment.GetGame().GetCacheManager().TryRemoveUser(Convert.ToInt32(UserData["id"]), out Junk);
                         Session.GetHabbo().Credits -= 50000;
                         Session.Shout("*Saca su anillo de bodas y lo lanza al suelo, anunciando que se divorcian " + UserData["username"] + "*", 7);
                     }
                 }
            }
            else
            {
                if (TargetClient.GetRoleplay().MarriedTo != Session.GetHabbo().Id)
                {
                    Session.SendWhisper("¡No estás casado con esta persona!", 1);
                    return;
                }

                TargetClient.GetRoleplay().MarriedTo = 0;
                Session.GetRoleplay().MarriedTo = 0;

                if (Session.GetHabbo().GetBadgeComponent().HasBadge("WD0"))
                    Session.GetHabbo().GetBadgeComponent().RemoveBadge("WD0");

                if (TargetClient.GetHabbo().GetBadgeComponent().HasBadge("WD0"))
                    TargetClient.GetHabbo().GetBadgeComponent().RemoveBadge("WD0");

                if (PolarEnvironment.GetGame().GetCacheManager().ContainsUser(Session.GetHabbo().Id))
                    PolarEnvironment.GetGame().GetCacheManager().TryUpdateUser(Session);

                if (PolarEnvironment.GetGame().GetCacheManager().ContainsUser(TargetClient.GetHabbo().Id))
                    PolarEnvironment.GetGame().GetCacheManager().TryUpdateUser(TargetClient);

                Session.Shout("*Saca su anillo de bodas y lo lanza al suelo, anunciando que se divorcian " + TargetClient.GetHabbo().Username + "*", 7);
                TargetClient.SendNotification(Session.GetHabbo().Username + " Acaba de divorciarse de usted");
                Session.GetHabbo().Credits -= 50000;
            }
            #endregion
        }
    }
}