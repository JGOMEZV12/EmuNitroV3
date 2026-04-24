using System;
using static Polar.HabboRoleplay.Bots.Manager.TimerHandlers.TimerHandlerManager;
using Polar.HabboHotel.Items;
using Polar.HabboRoleplay.Bots.Manager.TimerHandlers;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Managers
{
    class MakeBotActionCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_wonline"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Usuarios en línea con websocket de trabajo"; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {

            if (Params.Length < 3)
            {
                Session.SendWhisper("Invalid syntax! :makebotaction <bot> <action>");
                return;
            }

            string BotName = Convert.ToString(Params[1]);
            string BotAction = Convert.ToString(Params[2]);

            RoomUser Bot = Room.GetRoomUserManager().GetBotByName(BotName);

            if (Bot == null)
            {
                Session.SendWhisper("This bot is null", 1);
                return;
            }

            IBotHandler Handler = null;

            switch (BotAction.ToLower())
            {
                case "teleport":
                    object[] Parameters = { Bot.GetBotRoleplay().GetRandomTeleport() };
                    Bot.GetBotRoleplay().StartHandler(Handlers.TELEPORT, out Handler, Parameters);
                    break;
                default:
                    Session.SendWhisper("Invalid action!", 1);
                    break;
            }

            return;
        }
    }
}
