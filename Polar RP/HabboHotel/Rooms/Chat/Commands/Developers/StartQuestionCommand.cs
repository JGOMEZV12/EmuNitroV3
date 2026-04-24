using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections.Generic;

using Polar.HabboHotel.Users;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.Polls;
using Polar.HabboHotel.Polls.Enums;
using Polar.Communication.Packets.Outgoing.Polls;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Developers
{
    class StartQuestionCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_start_question"; }
        }

        public string Parameters
        {
            get { return "%id%"; }
        }

        public string Description
        {
            get { return "Starts the poll based on id."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length != 4)
            {
                Session.SendWhisper("Please use the command as: ':startquestion (poll) (time to show answer in seconds) (true = room only, false = whole hotel)'", 1);
                return;
            }

            int id = 0;
            int TimeLeft = 0;
            bool RoomOnly = true;

            if (!int.TryParse(Params[1], out id))
            {
                Session.SendWhisper("Please use the command as: ':startquestion (poll) (time to show answer in seconds) (true = room only, false = whole hotel)'", 1);
                return;
            }

            Poll poll = PolarEnvironment.GetGame().GetPollManager().TryGetPollById(id);

            if (poll == null || poll.Type != PollType.Matching)
            {
                Session.SendWhisper("Poll doesn't exist or isn't a matching poll.");
                return;
            }

            if (!int.TryParse(Params[2], out TimeLeft))
            {
                Session.SendWhisper("Please use the command as: ':startquestion (poll) (time to show answer in seconds) (true = room only, false = whole hotel)'", 1);
                return;
            }
            else
            {
                if (TimeLeft < 10)
                {
                    Session.SendWhisper("Please use a time of atleast 10 seconds!", 1);
                    return;
                }
            }

            if (!bool.TryParse(Params[3], out RoomOnly))
            {
                Session.SendWhisper("Please use the command as: ':startquestion (poll) (time to show answer in seconds) (true = room only, false = whole hotel)'", 1);
                return;
            }

            poll.AnswersPositive = 0;
            poll.AnswersNegative = 0;

            MatchingPollAnswer(Session, poll, RoomOnly);

            object[] Objects = { TimeLeft, Session, poll, RoomOnly };
            RoleplayManager.TimerManager.CreateTimer("matchingpoll", 1000, true, Objects);
            return;
        }

        private static void MatchingPollAnswer(GameClients.GameClient Session, Poll poll, bool RoomOnly = true)
        {
            if (poll == null || poll.Type != PollType.Matching)
                return;

            if (RoomOnly)
                Session.GetHabbo().CurrentRoom.SendMessage(new MatchingPollMessageComposer(poll));
            else
            {
                lock (PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                {
                    foreach (GameClients.GameClient client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                    {
                        if (client == null || client.GetHabbo() == null || client.GetRoomUser() == null)
                            continue;

                        client.SendMessage(new MatchingPollMessageComposer(poll));
                    }
                }
            }
        }
    }
}