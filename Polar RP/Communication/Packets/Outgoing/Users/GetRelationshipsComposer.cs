using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboRoleplay.Bots;
using Polar.HabboRoleplay.Bots.Manager;
using Polar.HabboHotel.Users;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.Cache;

namespace Polar.Communication.Packets.Outgoing.Users
{
    internal class GetRelationshipsComposer : ServerPacket
    {
        public UserCache Habbo { get; }
        public RoleplayBot Bot { get; }
        public GetRelationshipsComposer(UserCache Habbo, RoleplayBot Bot = null)
            : base(ServerPacketHeader.GetRelationshipsMessageComposer)
        {
            this.Habbo = Habbo;
            this.Bot = Bot;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            if (Bot != null)
            {
                WriteBotData(Bot, packet);
                return;
            }

            if (Habbo == null)
            {
                WriteNullData(packet);
                return;
            }
            else
            {
                WriteHabboData(Habbo, packet);
                return;
            }
        }

        public void WriteBotData(RoleplayBot Bot, ServerPacket packet)
        {
            int FakeBotId = Bot.Id + 1000000;

            packet.WriteInteger(FakeBotId);
            packet.WriteInteger(3);

            packet.WriteInteger(1);
            packet.WriteInteger(1);
            packet.WriteInteger(FakeBotId);
            packet.WriteString("Matrimonio con: Nadie");
            packet.WriteString(Bot.Figure);

            packet.WriteInteger(2);
            packet.WriteInteger(1);
            packet.WriteInteger(FakeBotId);
            packet.WriteString("Nivel: " + Bot.Level + "/" + RoleplayManager.LevelCap);
            packet.WriteString(Bot.Figure);

            packet.WriteInteger(3);
            packet.WriteInteger(1);
            packet.WriteInteger(FakeBotId);
            packet.WriteString("Fuerza: " + Bot.Strength + "/" + (Bot.Strength > 20 ? Bot.Strength : 20));
            packet.WriteString(Bot.Figure);
        }

        public void WriteNullData(ServerPacket packet)
        {
            packet.WriteInteger(0);
            packet.WriteInteger(0);
        }

        public void WriteHabboData(UserCache Habbo, ServerPacket packet)
        {
            #region Default
            packet.WriteInteger(Habbo.Id);
            packet.WriteInteger(3);
            #endregion

            #region Marriage
            packet.WriteInteger(1);
            packet.WriteInteger(1);

            if (Habbo.GetRoleplay() != null && Habbo.GetRoleplay().MarriedTo > 0)
            {
                UserCache Married = PolarEnvironment.GetGame().GetCacheManager().GenerateUser(Habbo.GetRoleplay().MarriedTo);

                if (Married == null)
                {
                    Habbo.GetRoleplay().MarriedTo = 0;
                    packet.WriteInteger(Habbo.Id);
                    packet.WriteString("Matrimonio con: Nadie");
                    packet.WriteString(Habbo.Look);
                }
                else
                {
                    packet.WriteInteger(Married.Id);
                    packet.WriteString("Matrimonio con: " + Married.Username);
                    packet.WriteString(Married.Look);
                }
            }
            else if (Habbo.MarriedId > 0)
            {
                UserCache Married = PolarEnvironment.GetGame().GetCacheManager().GenerateUser(Habbo.MarriedId);

                if (Married == null)
                {
                    packet.WriteInteger(Habbo.Id);
                    packet.WriteString("Matrimonio con: Nadie");
                    packet.WriteString(Habbo.Look);
                }
                else
                {
                    packet.WriteInteger(Married.Id);
                    packet.WriteString("Matrimonio con: " + Married.Username);
                    packet.WriteString(Married.Look);
                }
            }
            else
            {
                packet.WriteInteger(Habbo.Id);
                packet.WriteString("Matrimonio con: Nadie");
                packet.WriteString(Habbo.Look);
            }
            #endregion

            #region Hunger
            packet.WriteInteger(2);
            packet.WriteInteger(1);
            packet.WriteInteger(Habbo.Id);

            int hunger = Habbo.GetRoleplay()?.Hunger ?? 0;
            packet.WriteString("Hambre: " + hunger + "/100");
            packet.WriteString(Habbo.Look);
            #endregion

            #region Hygiene
            packet.WriteInteger(3);
            packet.WriteInteger(1);
            packet.WriteInteger(Habbo.Id);

            int hygiene = Habbo.GetRoleplay()?.Hygiene ?? 0;
            packet.WriteString("Higiene: " + hygiene + "/100");
            packet.WriteString(Habbo.Look);
            #endregion
        }
    }
}