using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.Communication.Packets.Outgoing.Moderation;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboHotel.Rooms.Chat.Commands.VIP
{
    class VIPAlertCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_alert_vip"; }
        }

        public string Parameters
        {
            get { return "%message%"; }
        }

        public string Description
        {
            get { return "Envía un mensaje escrito por usted a todos los usuarios de vip en línea."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("Introduzca un mensaje para enviar.", 1);
                return;
            }

            if (Session.GetRoleplay().DisableVIPA)
            {
                Session.SendWhisper("¡Tienes Alertas VIP deshabilitadas! Escribe ':vipalerta para volver a habilitarlos!", 1);
                return;
            }

            if (Session.GetRoleplay().VIPBanned > 0)
            {
                int TotalSeconds = Session.GetRoleplay().VIPBanned;
                int Minutes = Convert.ToInt32(Math.Floor((double)TotalSeconds / 60));
                int Seconds = TotalSeconds - (Minutes * 60);

                Session.SendWhisper("You have been VIP Alert banned! Your ban expires in: " + Minutes + " minutes and " + Seconds + " seconds!", 1);
                return;
            }

            if (RoleplayManager.NewVIPAlert)
            {
                if (Session.GetRoleplay().BannedFromChatting)
                {
                    Session.SendWhisper("You are banned from using VIP alerts!", 1);
                    return;
                }

                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_chatroom", Newtonsoft.Json.JsonConvert.SerializeObject(new Dictionary<object, object>()
                {
                    { "action", "requestjoin" },
                    { "chatname", "vip-chat" },

                }));
            }
            else
            {
                string Message = CommandManager.MergeParams(Params, 1);
                if (Session.GetHabbo().Translating)
                {
                    string LG1 = Session.GetHabbo().FromLanguage.ToLower();
                    string LG2 = Session.GetHabbo().ToLanguage.ToLower();

                    PolarEnvironment.GetGame().GetClientManager().VIPWhisperAlert(PolarEnvironment.TranslateText(Message, LG1 + "|" + LG2) + " [" + LG1.ToUpper() + " -> " + LG2.ToUpper() + "]", Session);
                }
                else
                    PolarEnvironment.GetGame().GetClientManager().VIPWhisperAlert(Message, Session);
            }
        }
    }
}
