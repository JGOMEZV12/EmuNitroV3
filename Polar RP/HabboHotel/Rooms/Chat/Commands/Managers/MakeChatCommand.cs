using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Fleck;
using Polar.Communication.Packets.Outgoing.Notifications;
using Polar.HabboRoleplay.Web.Util.ChatRoom;
using Polar.HabboRoleplay.Misc;
using System.Text.RegularExpressions;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.General
{ 
    class MakeChatCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_websocket_chat_create"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Crear un grupo de whatsapp"; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {

            #region Params
            if (Params.Length < 2)
            {
                Session.SendWhisper("¡Error! es de esta manera: :creargrupo <nombre> (Para entrar en el grupo debes poner :iniciarchat <nombre>)", 1);
                return;
            }

            string NewChatName = Convert.ToString(Params[1]);
            #endregion

            #region Conditions

            if (Session.GetRoleplay().BannedFromMakingChat)
            {
                Session.SendWhisper("Usted está permanentemente expulsado del grupo de whatsapp", 1);
                return;
            }
            /*if (!Session.GetRoleplay().PhoneApps.Contains("whatsapp"))
            {
                Session.SendWhisper("¡Necesitas la aplicación para iPhone de whatsapp para hacer esto!", 1);
                return;
            }*/
            
            if (WebSocketChatManager.RunningChatRooms.ContainsKey(NewChatName.ToLower()))
            {
                Session.SendWhisper("Este chat (" + NewChatName.ToLower() + ") ¡ya existe! ¡Por favor, elija un nuevo nombre!", 1);
                return;
            }

            if ((WebSocketChatManager.RunningChatRooms.Values.Where(Runningchat => Runningchat != null).Where(Runningchat => Runningchat.ChatOwner == Session.GetHabbo().Id).ToList().Count > 0) && Session.GetHabbo().VIPRank < 2)
            {
                Session.SendWhisper("¡Solo puedes crear una charla a la vez!", 1);
                return;
            }

            Regex regexItem = new Regex("^[a-zA-Z0-9 ]*$");
            if (!regexItem.IsMatch(NewChatName))
            {
                Session.SendWhisper("¡Nombre de chat no válido! ¡Elimine los caracteres especiales!", 1);
                return;
            }
            #endregion

            #region Execute
            WebSocketChatRoom NewChatRoom = new WebSocketChatRoom(NewChatName, Session.GetHabbo().Id, new Dictionary<object, object>() { { "password", "" }, {"gang", 0}, {"locked", false} }, new List<int>() { }, false);
            if (NewChatRoom.OnUserJoin(Session))
            {
                NewChatRoom.BeginChatJoin(Session);
               // Session.Shout("*Crea una nueva charla de grupo en WhatsApp con su " + RoleplayManager.GetPhoneName(Session) + "*", 4);
            }
            #endregion

        }

    }
}
