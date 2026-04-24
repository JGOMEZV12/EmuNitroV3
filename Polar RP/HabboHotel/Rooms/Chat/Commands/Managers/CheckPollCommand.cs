using System;
using System.Data;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Users;
using Polar.Database.Interfaces;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Notifications;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Managers
{
    class CheckPollCommand :IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_check_poll"; }
        }

        public string Parameters
        {
            get { return "%username% %poll% or %poll%"; }
        }

        public string Description
        {
            get { return "Checks the users poll answers for the desired poll, or provides a list of all users who answered the poll."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            StringBuilder Message = new StringBuilder();

            if (Params.Length == 1 || Params.Length > 3)
            {
                Session.SendWhisper("Please use the command as: ':checkpoll (user) (poll)' or ':checkpoll (poll)'.", 1);
                return;
            }

            using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                if (Params.Length == 2)
                {
                    int PollId = 0;

                    if (!int.TryParse(Params[1], out PollId))
                    {
                        Session.SendWhisper("Please use the command as: ':checkpoll (user) (poll)' or ':checkpoll (poll)'.", 1);
                        return;
                    }

                    var Poll = PolarEnvironment.GetGame().GetPollManager().TryGetPollById(PollId);

                    if (Poll == null)
                    {
                        Session.SendWhisper("The Poll ID you entered does not correspond to any polls!", 1);
                        return;
                    }

                    Message.Append("----- [" + Poll.Id + "] " + Poll.PollName + " -----\n\n");
                    Message.Append("Poll Title: " + Poll.PollInvitation + "\n");
                    Message.Append("Consists of: " + Poll.Questions.Count + " Total Questions\n\n");
                    Message.Append("The following users have answered this poll:\n");

                    dbClient.SetQuery("SELECT * FROM `user_polls` WHERE `poll_id` = '" + PollId + "'");
                    DataTable Table = dbClient.getTable();

                    if (Table == null)
                    {
                        Session.SendMessage(new MOTDNotificationComposer("Nobody has answered this poll yet!"));
                        return;
                    }
                    else
                    {
                        List<string> UserNames = new List<string>();

                        foreach (DataRow Row in Table.Rows)
                        {
                            int UserId = Convert.ToInt32(Row["user_id"]);

                            var User = PolarEnvironment.GetGame().GetCacheManager().GenerateUser(UserId);

                            if (!UserNames.Contains(User.Username))
                                UserNames.Add(User.Username);
                        }

                        foreach (string user in UserNames)
                            Message.Append(user + "\n");

                        Session.SendMessage(new MOTDNotificationComposer(Message.ToString()));
                    }

                }
                else
                {
                    Habbo Target = PolarEnvironment.GetHabboByUsername(Params[1]);

                    if (Target == null)
                    {
                        Session.SendWhisper("Sorry, but this user does not exist!", 1);
                        return;
                    }

                    int PollId = 0;

                    if (!int.TryParse(Params[2], out PollId))
                    {
                        Session.SendWhisper("Please use the command as: ':checkpoll (user) (poll)' or ':checkpoll (poll)'.", 1);
                        return;
                    }

                    var Poll = PolarEnvironment.GetGame().GetPollManager().TryGetPollById(PollId);

                    if (Poll == null)
                    {
                        Session.SendWhisper("The Poll ID you entered does not correspond to any polls!", 1);
                        return;
                    }

                    dbClient.SetQuery("SELECT * FROM `user_polls` where `user_id` = '" + Target.Id + "' AND `poll_id` = '" + Poll.Id + "' AND `accepted` = '1'");
                    DataTable Table = dbClient.getTable();

                    Dictionary<int, string> AnswerList = new Dictionary<int, string>();

                    foreach (DataRow row in Table.Rows)
                    {
                        int QuestionId = Convert.ToInt32(row["question_id"]);
                        string Answer = row["answer"].ToString();

                        if (!AnswerList.ContainsKey(QuestionId))
                            AnswerList.Add(QuestionId, Answer);
                    }

                    if (Table == null)
                    {
                        Session.SendWhisper("This user has not answered this poll yet!", 1);
                        return;
                    }

                    Message.Append("----- [" + Poll.Id + "] " + Poll.PollName + " -----\n\n");
                    Message.Append("Poll Title: " + Poll.PollInvitation + "\n");
                    Message.Append("Consists of: " + Poll.Questions.Count + " Total Questions\n\n");

                    foreach (var question in Poll.Questions)
                    {
                        Message.Append("Question Number: " + question.Index + "\n");
                        Message.Append("Question: " + question.Question + "\n");

                        if (question.AType == Polls.Enums.PollAnswerType.Text)
                        {
                            Message.Append("Answer Type: Text\n");
                            if (AnswerList.ContainsKey(question.Index))
                                Message.Append("User Answered with: " + AnswerList[question.Index] + "\n\n");
                            else
                                Message.Append("User did NOT answer this question!\n\n");
                        }
                        else
                        {
                            Message.Append("Answer Type: Selection\n");
                            Message.Append("Possible Answers: " + string.Join(",", question.Answers.ToList()) + "\n");
                            Message.Append("Correct Answer(s): " + string.Join(",", question.CorrectAnswers.ToList()) + "\n");
                            if (AnswerList.ContainsKey(question.Index))
                                Message.Append("User Answered: " + AnswerList[question.Index] + "\n\n");
                            else
                                Message.Append("User did NOT answer this question!\n\n");
                        }
                    }
                    Session.SendMessage(new MOTDNotificationComposer(Message.ToString()));
                }
            }
        }
    }
}
