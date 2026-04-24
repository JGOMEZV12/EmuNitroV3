using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.HabboHotel.Polls;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Polls.Enums;
using Polar.Communication.Packets.Outgoing.Polls;
using Polar.HabboRoleplay.Misc;

namespace Polar.Communication.Packets.Incoming.Polls
{
    internal class AnswerPollQuestionMessageEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            int pollId = Packet.PopInt();
            int questionId = Packet.PopInt();
            int num3 = Packet.PopInt();
            string liststring = "";
            liststring = Packet.PopString();

            if (pollId == 500000)
            {
                HandleATMQuestion(Session, questionId, liststring);
                return;
            }

            List<string> list = new List<string>();

            for (int i = 0; i < num3; i++)
                list.Add(liststring);

            string text = string.Join("\r\n", list);

            if (text == "")
                text = "Did not answer this question.";

            Poll poll = PolarEnvironment.GetGame().GetPollManager().TryGetPollById(pollId);

            if (poll == null)
            {
                Session.SendWhisper("Whoops! Something went wrong. This Poll could not be found!", 1);
                return;
            }

            if (poll.Type == PollType.Matching)
            {
                if (text == "1")
                    poll.AnswersPositive++;
                else
                    poll.AnswersNegative++;

                Session.GetHabbo().AnsweredMatchingPoll = true;
                Session.SendMessage(new MatchingPollAnsweredMessageComposer(Session, text));
                return;
            }

            PollQuestion Question = PolarEnvironment.GetGame().GetPollManager().getPollQuestion(poll, questionId);

            if (Question != null)
            {
                if (text != "Did not answer this question." && (Question.AType == PollAnswerType.RadioSelection || Question.AType == PollAnswerType.RadioSelection))
                {
                    List<string> SelectionAnswers = new List<string>();

                    foreach (string answerid in list)
                    {
                        int index = 0;
                        
                        foreach (string answer in Question.Answers)
                        {
                            index++;

                            if (index != Convert.ToInt32(answerid))
                                continue;

                            SelectionAnswers.Add(answer);
                        }
                    }
                    text = string.Join(",", SelectionAnswers);
                }
            }

            if (questionId == poll.Questions.Count)
            {
                if (!Session.GetHabbo().AnsweredPolls.Contains(poll.Id))
                    Session.GetHabbo().AnsweredPolls.Add(poll.Id);
            }

            var NewQuestion = new PollQuestion(questionId, Question.Question, 1, text, "");
            var AnsweredQuestions = Session.GetRoleplay().AnsweredPollQuestions;
            if (AnsweredQuestions.ContainsKey(poll.Id))
            {
                if (!AnsweredQuestions[poll.Id].Contains(NewQuestion))
                    AnsweredQuestions[poll.Id].Add(NewQuestion);
            }
            else
            {
                List<PollQuestion> List = new List<PollQuestion>();
                List.Add(NewQuestion);
                AnsweredQuestions.TryAdd(poll.Id, List);
            }

            if (AnsweredQuestions[poll.Id].Count == poll.Questions.Count)
            {
                string Answers = "";

                var OrderedAnswers = AnsweredQuestions[poll.Id].OrderBy(x => x.Index).ToList();

                int count = 0;
                foreach (string Answer in OrderedAnswers.Select(x => x.Answers[0]))
                {
                    count++;
                    if (count < OrderedAnswers.Count)
                        Answers += Answer + "Ø";
                    else
                        Answers += Answer;
                }

                using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("INSERT INTO `user_polls` VALUES (@userid ,@pollid ,@answers)");
                    dbClient.AddParameter("userid", Session.GetHabbo().Id);
                    dbClient.AddParameter("pollid", poll.Id);
                    dbClient.AddParameter("answers", Answers);
                    dbClient.RunQuery();
                }
            }
        }

        public void HandleATMQuestion(HabboHotel.GameClients.GameClient Session, int questionId, string amount)
        {
            if (Session.GetRoleplay().BankAccount <= 0)
            {
                if (Session.GetRoomUser() != null)
                {
                    if (!Session.GetRoomUser().CanWalk)
                        Session.GetRoomUser().CanWalk = true;
                }
            }

            if (questionId == 1)
            {
                if (!Session.GetRoleplay().ATMFailed)
                {
                    if (amount == "1")
                        Session.GetRoleplay().ATMAccount = "Chequings";
                    else if (amount == "2")
                        Session.GetRoleplay().ATMAccount = "Savings";
                    else
                        Session.GetRoleplay().ATMFailed = true;
                }
            }
            else if (questionId == 2)
            {
                if (!Session.GetRoleplay().ATMFailed)
                {
                    if (amount == "1")
                        Session.GetRoleplay().ATMAction = "Withdraw";
                    else if (amount == "2")
                        Session.GetRoleplay().ATMAction = "Deposit";
                    else
                        Session.GetRoleplay().ATMFailed = true;
                }
            }
            else
            {
                int Amount;
                if (int.TryParse(amount, out Amount))
                {
                    if (Amount <= 0)
                        Session.GetRoleplay().ATMFailed = true;

                    if (!Session.GetRoleplay().ATMFailed)
                    {
                        if (Session.GetRoleplay().ATMAccount.ToLower() == "chequings")
                        {
                            if (Session.GetRoleplay().ATMAction.ToLower() == "withdraw")
                            {
                                if (Session.GetRoleplay().BankChequings < Amount)
                                    Session.GetRoleplay().ATMFailed = true;
                                else
                                {
                                    Session.GetRoleplay().BankChequings -= Amount;
                                    Session.GetHabbo().Credits += Amount;
                                    Session.GetHabbo().UpdateCreditsBalance();

                                    Session.Shout("*Uses the ATM to withdraw $" + Amount + " from their Chequings Account*", 5);
                                }
                            }
                            else if (Session.GetRoleplay().ATMAction.ToLower() == "deposit")
                            {
                                if (Amount > Session.GetHabbo().Credits)
                                    Session.GetRoleplay().ATMFailed = true;
                                else
                                {
                                    Session.GetRoleplay().BankChequings += Amount;
                                    Session.GetHabbo().Credits -= Amount;
                                    Session.GetHabbo().UpdateCreditsBalance();

                                    Session.Shout("*Uses the ATM to deposit $" + Amount + " into their Chequings Account*", 5);
                                }
                            }
                            else
                                Session.GetRoleplay().ATMFailed = true;
                        }
                        else
                        {
                            if (Session.GetRoleplay().ATMAction.ToLower() == "withdraw")
                            {
                                if (Amount > 20)
                                {
                                    if (Session.GetRoleplay().BankSavings < Amount)
                                        Session.GetRoleplay().ATMFailed = true;
                                    else
                                    {
                                        int TaxAmount = Convert.ToInt32((double)Amount * 0.05);

                                        Session.GetHabbo().Credits += (Amount - TaxAmount);
                                        Session.GetHabbo().UpdateCreditsBalance();

                                        Session.GetRoleplay().BankSavings -= Amount;
                                        Session.Shout("*Takes out $" + Amount + " from their Chequings Account and places it in their pockets*", 5);
                                        Session.SendWhisper("You paid a tax of $" + TaxAmount + " in order to withdraw " + Amount, 1);
                                    }
                                }
                                else
                                    Session.GetRoleplay().ATMFailed = true;
                            }
                            else if (Session.GetRoleplay().ATMAction.ToLower() == "deposit")
                            {
                                if (Amount > Session.GetHabbo().Credits)
                                    Session.GetRoleplay().ATMFailed = true;
                                else
                                {
                                    Session.GetRoleplay().BankSavings += Amount;
                                    Session.GetHabbo().Credits -= Amount;
                                    Session.GetHabbo().UpdateCreditsBalance();

                                    Session.Shout("*Uses the ATM to deposit $" + Amount + " into their Savings Account*", 5);
                                }
                            }
                            else
                                Session.GetRoleplay().ATMFailed = true;
                        }
                    }
                    else
                        Session.GetRoleplay().ATMFailed = true;

                    if (Session.GetRoleplay().ATMFailed == true)
                        Session.SendWhisper("Error... ATM Failed!", 1);
                }
                else
                    Session.SendWhisper("Error... ATM Failed!", 1);

                Session.GetRoleplay().ATMAccount = "";
                Session.GetRoleplay().ATMAction = "";
                Session.GetRoleplay().ATMFailed = false;
            }
        }
    }
}
