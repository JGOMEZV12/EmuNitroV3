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
    class TimeLeftCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_timers_timeleft"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Indica los temporizadores que tiene en ejecución, si los hay."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            StringBuilder Message = new StringBuilder().Append("--- Temporizadores Activos Actuales ---\n\n");

            var RealTimers = Session.GetRoleplay().TimerManager.ActiveTimers.Values.Where(x => x.TimeLeft > 0).ToList();

            if (RealTimers.Count == 0)
                Message.Append("Actualmente no tienes temporizadores en ejecución.\n");
            else
            { 
                foreach (var Timer in RealTimers)
                {
                    if (Timer == null)
                        continue;

                    int TotalSeconds = Timer.TimeLeft / 1000;
                    int Minutes = Convert.ToInt32(Math.Floor((double)TotalSeconds / 60));
                    int Seconds = TotalSeconds - (Minutes * 60);

                    // Capital at the start of type
                    string TypeOfTimer = Timer.Type.Substring(0, 1).ToUpper() + Timer.Type.Substring(1);

                    if (TypeOfTimer == "Turfcapture")
                        TypeOfTimer = "Turf Capture";

                    if (TypeOfTimer != "Death")
                        Message.Append(TypeOfTimer + " Timer: " + Minutes + " minute(s) and " + Seconds + " second(s) left!\n");
                    else
                        Message.Append(TypeOfTimer + " Timer: En algún lugar menos de 2 minutos\n");
                    Message.Append("----------\n");
                }
                Message.Append("\nUtilice el comando :cooldowns para verificar los cooldowns remanentes, si los hubiera.");
            }
            Session.SendMessage(new MOTDNotificationComposer(Message.ToString()));
        }
    }
}