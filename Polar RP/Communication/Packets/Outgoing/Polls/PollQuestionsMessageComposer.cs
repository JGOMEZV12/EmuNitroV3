using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.HabboHotel.Polls;
using Polar.HabboHotel.Polls.Enums;
using Polar.HabboHotel.GameClients;

namespace Polar.Communication.Packets.Outgoing.Polls
{
    internal class PollQuestionsMessageComposer : ServerPacket
    {
        public GameClient Session { get; }
        public Poll poll { get; }
        public PollQuestionsMessageComposer(GameClient Session, Poll poll)
            : base(ServerPacketHeader.PollQuestionsMessageComposer)
        {
            this.Session = Session;
            this.poll = poll;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            if (poll.Id == 500000)
            {
                ATMPoll(Session, poll, packet);
                return;
            }

            packet.WriteInteger(poll.Id);
            packet.WriteString(poll.PollName);
            packet.WriteString(poll.Thanks);
            packet.WriteInteger(poll.Questions.Count);

            foreach (PollQuestion current in poll.Questions)
            {
                int QuestionNumber = checked(poll.Questions.IndexOf(current) + 1);

                packet.WriteInteger(current.Index);
                packet.WriteInteger(QuestionNumber);
                packet.WriteInteger((int)current.AType);
                packet.WriteString(current.Question);

                if (current.AType == PollAnswerType.Selection || current.AType == PollAnswerType.RadioSelection)
                {
                    packet.WriteInteger(0);
                    packet.WriteInteger(1);
                    packet.WriteInteger(current.Answers.ToList().Count);
                    
                    int index = 0;
                    foreach (string current2 in current.Answers)
                    {
                        index++;
                        packet.WriteString(index.ToString());
                        packet.WriteString(current2);
                        packet.WriteInteger(0);
                    }
                    packet.WriteInteger(0);
                }
                else
                {
                    packet.WriteInteger(0);
                    packet.WriteInteger(3);
                    packet.WriteInteger(0);
                    packet.WriteInteger(0);
                }
            }
            packet.WriteBoolean(false);
        }

        public void ATMPoll(GameClient Session, Poll poll, ServerPacket packet)
        {
            packet.WriteInteger(500000);
            packet.WriteString(poll.PollName);
            packet.WriteString(poll.Thanks);

            if (Session.GetRoleplay().BankAccount <= 0)
                HasNoBankAccount(Session, packet);
            else if (Session.GetRoleplay().BankAccount == 1)
                OnlyHasChequings(Session, packet);
            else
                HasChequingsAndSavings(Session, packet);
        }

        public void HasNoBankAccount(GameClient Session, ServerPacket packet)
        {
            packet.WriteInteger(1);

            packet.WriteInteger(1);
            packet.WriteInteger(1);
            packet.WriteInteger(1); 
            packet.WriteString("Lo siento, no tiene una cuenta bancaria! ¡Visite el banco para obtener uno!");

            packet.WriteInteger(0);
            packet.WriteInteger(1);
            packet.WriteInteger(1);

            packet.WriteString("1");
            packet.WriteString("Error, ninguna cuenta bancaria detectada.");
            packet.WriteInteger(0);

            packet.WriteInteger(0);

            packet.WriteBoolean(false);
        }

        public void OnlyHasChequings(GameClient Session, ServerPacket packet)
        {
            // Question Count
            packet.WriteInteger(3);

            #region Question 1
            packet.WriteInteger(1);
            packet.WriteInteger(1);
            packet.WriteInteger(1);
            packet.WriteString("¿Qué cuenta le gustaría usar?");

            packet.WriteInteger(0);
            packet.WriteInteger(1);
            packet.WriteInteger(1);

            packet.WriteString("1");
            packet.WriteString("Chequings");
            packet.WriteInteger(0);

            packet.WriteInteger(0);
            #endregion

            #region Question 2
            packet.WriteInteger(2);
            packet.WriteInteger(2);
            packet.WriteInteger(1);
            packet.WriteString("Su saldo es $" + Session.GetRoleplay().BankChequings + ". Que te gustaría hacer?");

            packet.WriteInteger(0);
            packet.WriteInteger(1);
            packet.WriteInteger(2);

            packet.WriteString("1");
            packet.WriteString("Withdraw");
            packet.WriteInteger(0);

            packet.WriteString("2");
            packet.WriteString("Deposit");
            packet.WriteInteger(0);

            packet.WriteInteger(0);
            #endregion

            #region Question 3
            packet.WriteInteger(3);
            packet.WriteInteger(3);
            packet.WriteInteger(3);
            packet.WriteString("Por favor ingrese la cantidad que desea depositar o retirar.");

            packet.WriteInteger(0);
            packet.WriteInteger(3);
            packet.WriteInteger(0);
            packet.WriteInteger(0);
            #endregion

            packet.WriteBoolean(false);
        }

        public void HasChequingsAndSavings(GameClient Session, ServerPacket packet)
        {
            // Question Count
            packet.WriteInteger(3);

            #region Question 1
            packet.WriteInteger(1);
            packet.WriteInteger(1);
            packet.WriteInteger(1);
            packet.WriteString("Which account would you like to use?");

            packet.WriteInteger(0);
            packet.WriteInteger(1);
            packet.WriteInteger(2);

            packet.WriteString("1");
            packet.WriteString("Chequings");
            packet.WriteInteger(0);

            packet.WriteString("2");
            packet.WriteString("Savings");
            packet.WriteInteger(0);

            packet.WriteInteger(0);
            #endregion

            #region Question 2
            packet.WriteInteger(2);
            packet.WriteInteger(2);
            packet.WriteInteger(1);
            packet.WriteString("Your chequings balance is $" + Session.GetRoleplay().BankChequings + " and your savings balance is $" + Session.GetRoleplay().BankSavings + ". What would you like to do?");

            packet.WriteInteger(0);
            packet.WriteInteger(1);
            packet.WriteInteger(2);

            packet.WriteString("1");
            packet.WriteString("Withdraw");
            packet.WriteInteger(0);

            packet.WriteString("2");
            packet.WriteString("Deposit");
            packet.WriteInteger(0);

            packet.WriteInteger(0);
            #endregion

            #region Question 3
            packet.WriteInteger(3);
            packet.WriteInteger(3);
            packet.WriteInteger(3);
            packet.WriteString("Please enter the amount you would like to deposit or withdraw.");

            packet.WriteInteger(0);
            packet.WriteInteger(3);
            packet.WriteInteger(0);
            packet.WriteInteger(0);
            #endregion

            packet.WriteBoolean(false);
        }
    }
}
