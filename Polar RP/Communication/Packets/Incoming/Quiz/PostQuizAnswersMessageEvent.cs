using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Users.HabboQuiz;

namespace Polar.Communication.Packets.Incoming.Quiz
{
    internal class PostQuizAnswersMessageEvent : IPacketEvent
    {
        public void Parse(GameClient Session, ClientPacket Packet)
        {
            if (Session == null || Session.GetHabbo() == null)
                return;

            /*string HabboType = Packet.PopFixedString();
            if (HabboType != "HabboWay1")
                return;

            int HabboQuestions = Packet.PopInt();
            List<int> errors = new List<int>(5);

            var PacketxD = new ServerPacket(ServerPacketHeader.PostQuizAnswersMessageComposer);
            PacketxD.WriteString(HabboType);
            for (int i = 0; i < HabboQuestions; i++)
            {
                int QuestionId = Session.GetHabbo().HabboQuizQuestions[i];
                int respuesta = Packet.PopInt();
                if (!HabboQuizxD.CorrectAnswer(QuestionId, respuesta))
                {
                    errors.Add(QuestionId);
                }
            }
            PacketxD.WriteInteger(errors.Count);
            foreach (int error in errors)
            {
                PacketxD.WriteInteger(error);
            }
            Session.SendMessage(PacketxD);

            if (errors.Count == 0)
            {
                PolarEnvironment.GetGame().GetAchievementManager().ProgressAchievement(Session, "ACH_HabboWayGraduate", 1, false);
                
            }*/
        }
    }
}
