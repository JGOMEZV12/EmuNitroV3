using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboHotel.Rooms.Chat.Commands.SpecialRights
{
    class UnBanVIPCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_ban_vip_alert_undo"; }
        }

        public string Parameters
        {
            get { return "%user%"; }
        }

        public string Description
        {
            get { return "UnBans un usuario de las alertas vip."; }
        }

        public async Task Execute(GameClients.GameClient Session, Room Room, string[] Params)
        {
            #region Conditions

            if (Params.Length < 2)
            {
                Session.SendWhisper("Incorrect command syntax, :unbanvip [user]!", 1);
                return;
            }

            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);

            if (TargetClient == null)
            {
                Session.SendWhisper("Sorry, but this user could not be found!", 1);
                return;
            }

            if (TargetClient.GetHabbo().VIPRank == 0)
            {
                Session.SendWhisper("Sorry, but this user is not VIP!", 1);
                return;
            }

            if (TargetClient.GetRoleplay().VIPBanned <= 0)
            {
                Session.SendWhisper("This user is not banned from VIP Alerts!", 1);
                return;
            }
            #endregion

            #region Execute

            TargetClient.GetRoleplay().VIPBanned = 0;
            Session.Shout("*Utiliza sus poderes divinos to unban " + TargetClient.GetHabbo().Username + " para enviar alertas VIP!*" , 23);
            TargetClient.SendWhisper("¡Un administrador le ha prohibido el envío de alertas VIP!", 1);

            #region Notify of unban to other VIP users
            lock (PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
            {
                foreach (GameClient client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                {
                    if (client == null || client.GetHabbo() == null)
                        continue;

                    if (client.GetHabbo().VIPRank == 0)
                        continue;

                    if (client.GetRoleplay().DisableVIPA == true)
                        continue;

                    client.SendWhisper("[VIP Alert] *Un administrador ha quitado " + TargetClient.GetHabbo().Username + " para enviar alertas vip!*", 11);
                }
            }
            #endregion

            #endregion
        }
    }
}