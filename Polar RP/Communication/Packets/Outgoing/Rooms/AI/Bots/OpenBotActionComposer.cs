using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;

namespace Polar.Communication.Packets.Outgoing.Rooms.AI.Bots
{
    internal class OpenBotActionComposer : ServerPacket
    {
        public int BotId { get; }
        public string BotName { get; }
        public int ActionId { get; }
        public string BotSpeech { get; }

        public OpenBotActionComposer(RoomUser BotUser, int ActionId, string BotSpeech)
            : base(ServerPacketHeader.OpenBotActionMessageComposer)
        {
            this.BotId = BotUser.BotData.Id;
            this.BotName = BotUser.BotData.Name;
            this.ActionId = ActionId;
            this.BotSpeech = BotSpeech;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(BotId);
            packet.WriteInteger(ActionId);
            if (ActionId == 2)
                packet.WriteString(BotSpeech);
            else if (ActionId == 5)
                packet.WriteString(BotName);
        }
    }
}
