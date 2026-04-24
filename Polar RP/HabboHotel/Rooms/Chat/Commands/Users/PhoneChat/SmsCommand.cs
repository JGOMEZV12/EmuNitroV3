using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Drawing;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using Polar.Communication.Packets.Outgoing.Notifications;
using Polar.HabboRoleplay.RPRoom;
using Polar.HabboRoleplay.PhoneChat;
using System.Text.RegularExpressions;
using Polar.HabboHotel.Users.Messenger;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Jobs.Types.Police
{
    class SmsCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_sms"; }
        }

        public string Parameters
        {
            get { return "%user%"; }
        }

        public string Description
        {
            get { return "Envía un mensaje de texto a una persona."; }
        }

        public Task Execute(GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            if (Session.GetRoleplay().Phone == 0)
            {
                Session.SendWhisper("No tienes ningún teléfono comprado para hacer eso.", 1);
                return Task.CompletedTask;
            }
            if (Params.Length == 1)
            {
                Session.SendWhisper("Debes ingresar un número telefónico. (:sms [número/contacto] [mensaje])", 1);
                return Task.CompletedTask;
            }
            if (Params.Length == 2)
            {
                Session.SendWhisper("Debes ingresar el mensaje a enviar. (:sms [número/contacto] [mensaje])", 1);
                return Task.CompletedTask;
            }
            string Text = CommandManager.MergeParams(Params, 2);
            Text = Regex.Replace(Text, "<(.|\\n)*?>", string.Empty); // Filtramos mensaje
            if (Text.Length <= 0)
            {
                Session.SendWhisper("No puedes enviar mensajes sin texto.", 1);
                return Task.CompletedTask;
            }
            string Target = Params[1];
            Target = Regex.Replace(Target, "<(.|\\n)*?>", string.Empty);
            if (Target.Length <= 0)
            {
                Session.SendWhisper("No puedes enviar mensajes sin destinatario.", 1);
                return Task.CompletedTask;
            }
            if (Session.GetRoleplay().TryGetCooldown("msg", true))
            {
                Session.SendWhisper("Por favor espera un poco para hacer eso nuevamente.", 1);
                return Task.CompletedTask;
            }
            #endregion

            #region Execute
            /*
            * El Target recibido puede ser
            * número telefónico, ya sea con o sin formato
            * ó el nombre de usuario.
            * Si es nombre de usuario éste debe ser "amigo"
            * Nota: Validar cuando el Target esté online o no.
            */

            int Targetid = PolarEnvironment.GetGame().GetPhoneChatManager().GetIDbyContact(Session, Target);

            #region Extra Conditions
            if (Targetid <= 0)
            {
                Session.SendWhisper("Ese número telefónico no existe.", 1);
                return Task.CompletedTask;
            }
            if (Targetid == Session.GetHabbo().Id)
            {
                Session.SendWhisper("No puedes enviarte mensajes a ti mis@.", 1);
                return Task.CompletedTask;
            }

            // Envió a un nombre de contacto
            if (Session.GetRoleplay().SendToName)
            {
                // Verificamos que sean amigos
                List<MessengerBuddy> Friend = (from TG in Session.GetHabbo().GetMessenger().GetFriends().ToList() where TG != null && TG.UserId == Targetid select TG).ToList();

                if (Friend == null || Friend.Count <= 0)
                {
                    Session.SendWhisper("No se encontró ese nombre en tus Contactos. Intenta escribiendo el Número Telefónico.", 1);
                    return Task.CompletedTask;
                }
            }
            #endregion

            #region Execute
            int ID = RoleplayManager.ChatsID += 1;
            DateTime TimeStamp = DateTime.Now;
            string TargetName = PolarEnvironment.GetUserInfoBy("username", "id", Convert.ToString(Targetid));

            // TryAdd al Diccionario de PhoneChat
            PolarEnvironment.GetGame().GetPhoneChatManager().NewPhoneChat(ID, 1, Session.GetHabbo().Id, Session.GetHabbo().Username, Targetid, TargetName, Text, TimeStamp);

            // Comprobamos si destinatario está online
            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Targetid);
            if (TargetClient != null)
            {
                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(TargetClient, "event_phone", "open_chatrooms");
                RoleplayManager.Shout(TargetClient, "*Recibe un Mensaje de Texto*", 5);

                // Verificamos si el último chat del destinatario es el nuestro y actualizamos
                if (TargetClient.GetRoleplay().LastChat == Session.GetHabbo().Username)
                {
                    // Refresh ChatRooms Target
                    TargetClient.GetRoleplay().UpdateChats = true;
                    PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(TargetClient, "event_phone", "open_messages," + Session.GetHabbo().Username);
                    TargetClient.GetRoleplay().UpdateChats = false;

                    // Refresh Msgs Target
                    if (TargetClient.GetRoleplay().LastChat == Session.GetHabbo().Username)
                        PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(TargetClient, "event_phone", "open_chatrooms");
                }
            }

            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_phone", "open_chatrooms");
            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_phone", "open_messages," + TargetName);
            RoleplayManager.Shout(Session, "*Ha enviado un Mensaje de Texto*", 5);
            Session.GetRoleplay().CooldownManager.CreateCooldown("msg", 1000, 3);
            #endregion
            #endregion

            return Task.CompletedTask;
        }

    }
}