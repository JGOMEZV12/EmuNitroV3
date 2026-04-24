using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing;

namespace Polar.Communication.Packets.Incoming.Quiz
{
    internal class CheckQuizTypeEvent : IPacketEvent
    {
        public void Parse(GameClient Session, ClientPacket Packet)
        {
            if (Session == null || Session.GetHabbo() == null)
                return;

            /*string HabboType = Packet.PopFixedString();
            if (HabboType == "HabboWay1")
            {
                Session.GetHabbo().HabboQuizQuestions = new List<int>(5);

                var PacketxD = new ServerPacket(ServerPacketHeader.QuizDataMessageComposer);
                PacketxD.WriteString(HabboType);
                PacketxD.WriteInteger(5); // longitud.
                for (int i = 0; i < 5; i++)
                {
                    int rndNumber = new Random().Next(10);
                    if (Session.GetHabbo().HabboQuizQuestions.Contains(rndNumber))
                    {
                        for (int ii = 0; ii < 10; ii++)
                        {
                            if (!Session.GetHabbo().HabboQuizQuestions.Contains(ii))
                            {
                                rndNumber = ii;
                                break;
                            }
                        }
                    }
                    Session.GetHabbo().HabboQuizQuestions.Add(rndNumber);
                    PacketxD.WriteInteger(rndNumber);
                }
                Session.SendMessage(PacketxD);
            }
            else if (HabboType == "SafetyQuiz1")
            {
                PolarEnvironment.GetGame().GetAchievementManager().ProgressAchievement(Session, "ACH_SafetyQuizGraduate", 1, false);
            }*/
        }
    }
}
