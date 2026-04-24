using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.Communication.Packets.Outgoing.Notifications;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Timers
{
    class CooldownsCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_timers_cooldowns"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Te dice de cualquier tiempo de reutilización que tengas."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            StringBuilder Message = new StringBuilder().Append("--- activos Cooldowns ---\n\n");

            if (Session.GetRoleplay().CooldownManager.ActiveCooldowns.Count <= 0)
                Message.Append("Actualmente no tienes Cooldowns funcionando\n");

            lock (Session.GetRoleplay().CooldownManager.ActiveCooldowns.Values)
            {
                foreach (var Cooldown in Session.GetRoleplay().CooldownManager.ActiveCooldowns.Values)
                {
                    if (Cooldown == null)
                        continue;

                    // Don't show combat timers
                    if (Cooldown.Type.ToLower() == "gun" || Cooldown.Type.ToLower() == "fist" || Cooldown.Type.ToLower() == "reload")
                        continue;

                    // Capital at the start of type
                    int TotalSeconds = Cooldown.TimeLeft / 1000;
                    int Minutes = Convert.ToInt32(Math.Floor((double)TotalSeconds / 60));
                    int Seconds = TotalSeconds - (Minutes * 60);

                    string TypeOfCooldown = Cooldown.Type.Substring(0, 1).ToUpper() + Cooldown.Type.Substring(1);
                    Message.Append(TypeOfCooldown + " Cooldown: " + Minutes + " minute(s) and " + Seconds + " Segundo (s)\n");
                    Message.Append("----------\n");
                }
                Message.Append("\nUtilice el comando :timeleft para comprobar los temporizadores restantes, si los hubiere.");
            }

            Session.SendMessage(new MOTDNotificationComposer(Message.ToString()));
        }
    }
}