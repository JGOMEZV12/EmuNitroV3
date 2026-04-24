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
    class AbandonarCommand : IChatCommand
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

            if (Session.GetRoleplay().Hijo == 0)
            {
                Session.SendWhisper("¡No tienes hij@!", 1);
                return;
            }

            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);

            #endregion

            #region Execute
            if (TargetClient == null)
            {
                using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    var Habbo = PolarEnvironment.GetHabboByUsername(Params[1]);

                    if (Habbo == null)
                    {
                        Session.SendWhisper("¡No pudo encontrar este usuario! Tal vez escribió su nombre equivocado", 1);
                        return;
                    }

                    dbClient.SetQuery("SELECT `id` FROM `rp_stats` WHERE `id` = '" + Habbo.Id + "' LIMIT 1");
                    var Row = dbClient.getRow();

                    if (Row == null)
                    {
                        Session.SendWhisper("No pudo encontrar este usuario! Tal vez escribió su nombre equivocado", 1);
                        return;
                    }

                    int TargetHijo = Convert.ToInt32(Row["id"]);

                    if (Session.GetRoleplay().Hijo != TargetHijo)
                    {
                        Session.SendWhisper("Este no es tu hij@", 1);
                        return;
                    }
                    else
                    {
                        dbClient.RunQuery("UPDATE `rp_stats` SET `hijo` = '0' WHERE `id` = '" + Habbo.Id + "'");
                        dbClient.RunQuery("DELETE FROM `user_badges` WHERE `badge_id` = 'DK087' AND `user_id` = '" + Habbo.Id + "'");
                        dbClient.RunQuery("UPDATE `rp_stats` SET `hijo` = '0' WHERE `id` = '" + Session.GetHabbo().Id + "'");

                        Session.GetRoleplay().Hijo = 0;
                        if (Session.GetHabbo().GetBadgeComponent().HasBadge("DK087"))
                            Session.GetHabbo().GetBadgeComponent().RemoveBadge("DK087");

                        if (PolarEnvironment.GetGame().GetCacheManager().ContainsUser(Session.GetHabbo().Id))
                            PolarEnvironment.GetGame().GetCacheManager().TryUpdateUser(Session);

                        UserCache Junk;

                        if (PolarEnvironment.GetGame().GetCacheManager().ContainsUser(Habbo.Id))
                            PolarEnvironment.GetGame().GetCacheManager().TryRemoveUser(Habbo.Id, out Junk);

                        Session.Shout("*Le dice a " + Habbo.Username + " que no es su verdadera madre y lo abandona*", 7);
                    }
                }
            }
            else
            {
                if (TargetClient.GetRoleplay().Hijo != Session.GetHabbo().Id)
                {
                    Session.SendWhisper("¡No es tu hij@!", 1);
                    return;
                }

                TargetClient.GetRoleplay().Hijo = 0;
                Session.GetRoleplay().Hijo = 0;

                if (Session.GetHabbo().GetBadgeComponent().HasBadge("DK087"))
                    Session.GetHabbo().GetBadgeComponent().RemoveBadge("DK087");

                if (TargetClient.GetHabbo().GetBadgeComponent().HasBadge("DK087"))
                    TargetClient.GetHabbo().GetBadgeComponent().RemoveBadge("DK087");

                if (PolarEnvironment.GetGame().GetCacheManager().ContainsUser(Session.GetHabbo().Id))
                    PolarEnvironment.GetGame().GetCacheManager().TryUpdateUser(Session);

                if (PolarEnvironment.GetGame().GetCacheManager().ContainsUser(TargetClient.GetHabbo().Id))
                    PolarEnvironment.GetGame().GetCacheManager().TryUpdateUser(TargetClient);

                Session.Shout("*Saca su anillo de bodas y lo lanza al suelo, anunciando que se divorcian " + TargetClient.GetHabbo().Username + "*", 7);
                TargetClient.SendNotification(Session.GetHabbo().Username + " acaba de abandonarte, ya no es tu madre");
            }
            #endregion
        }
    }
}