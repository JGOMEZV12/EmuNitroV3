#region

using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using log4net;

#endregion

namespace Polar.Core
{
    class ExtraSettings
    {

        public static bool WELCOME_MESSAGE_ENABLED = true;
        public static bool WELCOME_NEW_MESSAGE_ENABLED = true;
        public static bool TARGETED_OFFERS_ENABLED = true;
        public static string WELCOME_MESSAGE_URL = "habbopages/bienvenidax.txt";
        public static string WEBHOOK_ID_COMBAT = "";
        public static string WEBHOOK_TOKEN_COMBAT = "";
        public static string WEBHOOK_ID = "";
        public static string WEBHOOK_TOKEN = "";
        public static bool DEBUG_ENABLED = true;
        public static string WelcomeMessage = "";
        public static readonly ILog log = LogManager.GetLogger("Polar.Core");

        public static bool RunExtraSettings()
        {
            if (File.Exists("Settings/Welcome/message.txt"))
                WelcomeMessage = File.ReadAllText("Settings/Welcome/message.txt");
            if (!File.Exists("Settings/extra.ini"))
                return false;
            foreach (var @params in from line in File.ReadAllLines("Settings/extra.ini", Encoding.Default) where !String.IsNullOrWhiteSpace(line) && line.Contains("=") select line.Split('='))
            {
                switch (@params[0])
                {
                    case "welcome.message.enabled":
                        WELCOME_MESSAGE_ENABLED = @params[1] == "true";
                        break;
                    case "welcome.new.message.enabled":
                        WELCOME_NEW_MESSAGE_ENABLED = @params[1] == "true";
                        break;
                    case "welcome.message.url":
                        WELCOME_MESSAGE_URL = @params[1];
                        break;
                    case "webhook.id.combat":
                        WEBHOOK_ID_COMBAT = @params[1];
                        break;
                    case "webhook.token.combat":
                        WEBHOOK_TOKEN_COMBAT = @params[1];
                        break;
                    case "webhook.id":
                        WEBHOOK_ID = @params[1];
                        break;
                    case "webhook.token":
                        WEBHOOK_TOKEN = @params[1];
                        break;
                    case "debug.enabled":
                        DEBUG_ENABLED = @params[1] == "true";
                        break;
                    case "targeted.offers.enabled":
                        TARGETED_OFFERS_ENABLED = @params[1] == "true";
                        break;
                }
            }
            //log.Info("» Extra Settings -> CARGADO!");
            Out.WriteLine("Extra Settings -> CARGADO", "Polar.Core", ConsoleColor.DarkGray);
            return true;
        }
    }
}