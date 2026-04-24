using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.Communication.Packets.Outgoing.Notifications;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Weapons;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.General
{
    class TranslateCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_translate"; }
        }

        public string Parameters
        {
            get { return "%fromlang% %tolang%"; }
        }

        public string Description
        {
            get { return "Traduce un mensaje de un idioma específico a un idioma específico."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length < 3)
            {
                Session.SendWhisper("Sintaxis de comandos no válido! :translate <languagefrom> <languageto>. Ejemplo ':translate en fr'", 1);
                return;
            }

            string Language1 = Params[1].ToLower();
            string Language2 = Params[2].ToLower();

            #region Available Languages
            List<string> AvailableLanguages = new List<string>();

            AvailableLanguages.Add("af"); // Afrikaans
            AvailableLanguages.Add("sq"); // Albanian 
            AvailableLanguages.Add("az"); // Azerbaijani
            AvailableLanguages.Add("eu"); // Basque
            AvailableLanguages.Add("be"); // Belarusian
            AvailableLanguages.Add("bg"); // Bulgarian
            AvailableLanguages.Add("ca"); // Catalan
            AvailableLanguages.Add("hr"); // Croatian
            AvailableLanguages.Add("cs"); // Czech
            AvailableLanguages.Add("da"); // Danish
            AvailableLanguages.Add("nl"); // Dutch
            AvailableLanguages.Add("en"); // English
            AvailableLanguages.Add("eo"); // Esperanto
            AvailableLanguages.Add("et"); // Estonian
            AvailableLanguages.Add("tl"); // Filipino
            AvailableLanguages.Add("fi"); // Finnish
            AvailableLanguages.Add("fr"); // French
            AvailableLanguages.Add("gl"); // Galician
            AvailableLanguages.Add("de"); // German
            AvailableLanguages.Add("el"); // Greek
            AvailableLanguages.Add("ht"); // Haitian Creole
            AvailableLanguages.Add("hu"); // Hungarian
            AvailableLanguages.Add("is"); // Icelandic
            AvailableLanguages.Add("id"); // Indonesian
            AvailableLanguages.Add("ga"); // Irish
            AvailableLanguages.Add("it"); // Italian
            AvailableLanguages.Add("la"); // Latin
            AvailableLanguages.Add("lv"); // Latvian
            AvailableLanguages.Add("lt"); // Lithuanian
            AvailableLanguages.Add("mk"); // Macedonian
            AvailableLanguages.Add("ms"); // Malay
            AvailableLanguages.Add("mt"); // Maltese
            AvailableLanguages.Add("no"); // Norwegian
            AvailableLanguages.Add("pl"); // Polish
            AvailableLanguages.Add("pt"); // Portuguese
            AvailableLanguages.Add("ro"); // Romanian
            AvailableLanguages.Add("sk"); // Slovak
            AvailableLanguages.Add("sl"); // Slovenian
            AvailableLanguages.Add("es"); // Spanish
            AvailableLanguages.Add("sw"); // Swahili
            AvailableLanguages.Add("sv"); // Swedish
            AvailableLanguages.Add("tr"); // Turkish
            AvailableLanguages.Add("vi"); // Vietnamese
            #endregion

            if (Session.GetHabbo().FromLanguage == Language1 && Session.GetHabbo().ToLanguage == Language2)
            {
                Session.SendWhisper("*Ya estás traduciendo de " + Language1.ToUpper() + " a " + Language2.ToUpper() + "*", 1);
                return;
            }

            if (!AvailableLanguages.Contains(Language1) || !AvailableLanguages.Contains(Language2))
            {
                Session.SendWhisper("¡Uno de los idiomas que ha seleccionado no está disponible! Aquí está una lista de idiomas disponibles: " + String.Join(", ", AvailableLanguages.ToArray()), 1);
                return;
            }

            Session.GetHabbo().Translating = true;
            Session.GetHabbo().FromLanguage = Language1;
            Session.GetHabbo().ToLanguage = Language2;

            Session.SendWhisper("*Ahora estás traduciendo de " + Language1.ToUpper() + " para " + Language2.ToUpper() + "*", 1);
        }
    }
}