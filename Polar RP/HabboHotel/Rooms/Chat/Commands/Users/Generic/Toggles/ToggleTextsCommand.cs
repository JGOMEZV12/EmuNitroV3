using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Toggles
{
    class ToggleTextsCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_toggle_texts"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Le permite elegir la opción para activar o desactivar mensajes de texto."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            Session.GetHabbo().AllowConsoleMessages = !Session.GetHabbo().AllowConsoleMessages;
            Session.SendWhisper("Usted " + (Session.GetHabbo().AllowConsoleMessages ? "ahora acepta" : "no acepta") + " mensajes de la consola.", 1);
            Session.Shout("*Télefono: " + (Session.GetHabbo().AllowConsoleMessages ? "ENCENDIDO" : "APAGADO") + " y " + (Session.GetHabbo().AllowConsoleMessages ? "activa" : "apaga") + " su whatsapp*", 4);
        }
    }
}