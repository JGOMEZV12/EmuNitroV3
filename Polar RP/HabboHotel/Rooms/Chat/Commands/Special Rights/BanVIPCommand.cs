using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboHotel.Rooms.Chat.Commands.SpecialRights
{
    class BanVIPCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_ban_vip_alert"; }
        }

        public string Parameters
        {
            get { return "%user% %time%"; }
        }

        public string Description
        {
            get { return "Prohibe a un usuario de las alertas vip."; }
        }

        public async Task Execute(GameClients.GameClient Session, Room Room, string[] Params)
        {
            #region Conditions

            if (Params.Length < 3)
            {
                Session.SendWhisper("escriba, :banvip [user] [time]!", 1);
                return;
            }

            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);

            if (TargetClient == null)
            {
                Session.SendWhisper("Lo sentimos, pero este usuario no se pudo encontrar", 1);
                return;
            }

            if (TargetClient.GetHabbo().VIPRank == 0)
            {
                Session.SendWhisper("Lo sentimos, pero este usuario no es VIP", 1);
                return;
            }

            #endregion

            #region Execute
            int Time;
            if (int.TryParse(Params[2], out Time))
            {
                if (Time < TargetClient.GetRoleplay().VIPBanned)
                {
                    Session.SendWhisper("Este usuario ya está prohibido de las alertas VIP para " + String.Format("{0:N0}", TargetClient.GetRoleplay().VIPBanned) + " more seconds!", 1);
                    return;
                }

                int Minutes = Convert.ToInt32(Math.Floor((double)Time / 60));
                int Seconds = Time - (Minutes * 60);

                TargetClient.GetRoleplay().VIPBanned = Time;
                if (Minutes > 0)
                    Session.Shout("*Utiliza sus poderes divinos para prohibir " + TargetClient.GetHabbo().Username + " from sending VIP alerts for " + String.Format("{0:N0}", Minutes) + " minutes and " + String.Format("{0:N0}", Seconds) + " seconds!*", 23);
                else
                    Session.Shout("*Utiliza sus poderes divinos para prohibir " + TargetClient.GetHabbo().Username + " from sending VIP alerts for " + String.Format("{0:N0}", Seconds) + " seconds!*", 23);
                TargetClient.SendWhisper("¡Un administrador te ha prohibido enviar alertas VIP!", 1);

                #region Notify of ban to other VIP users
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

                        client.SendWhisper("[VIP Alert] *Un administrador ha prohibido " + TargetClient.GetHabbo().Username + " De enviar alertas VIP*", 11);
                    }
                }
                #endregion
                return;
            }
            else
            {
                Session.SendWhisper("Incorrect command syntax, :banvip [user] [time]!", 1);
                return;
            }
            #endregion
        }
    }
}