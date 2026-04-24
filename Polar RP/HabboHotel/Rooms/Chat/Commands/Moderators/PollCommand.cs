using Polar.HabboHotel.GameClients;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Moderators
{
    internal class PollCommand : IChatCommand
    {
        public async Task Execute(GameClient Session, Room Room, string[] Params)
        {
            if (Params.Length == 0)
            {
                Session.SendWhisper("Por favor introduce la pregunta");
            }
            else
            {

                string quest = CommandManager.MergeParams(Params, 1);
                //int time = Convert.ToInt32(Params[2]);
                if (quest == "end")
                {
                    Room.endQuestion();
                }
                else
                {
                    Room.startQuestion(quest, 50);
                }

            }
        }

        public string Description =>
            "Realizar una encuesta rápida.";

        public string Parameters =>
            "%question% %time%";

        public string PermissionRequired =>
            "command_give_badge";
    }
}