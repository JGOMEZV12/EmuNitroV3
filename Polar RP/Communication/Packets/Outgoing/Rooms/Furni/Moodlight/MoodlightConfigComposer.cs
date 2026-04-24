using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Items.Data.Moodlight;

namespace Polar.Communication.Packets.Outgoing.Rooms.Furni.Moodlight
{
    internal class MoodlightConfigComposer : ServerPacket
    {
        public MoodlightData Data { get; }

        public MoodlightConfigComposer(MoodlightData MoodlightData)
            : base(ServerPacketHeader.MoodlightConfigMessageComposer)
        {
            this.Data = MoodlightData;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(Data.Presets.Count);
            packet.WriteInteger(Data.CurrentPreset);

            int i = 1;
            foreach (MoodlightPreset Preset in Data.Presets)
            {
                packet.WriteInteger(i);
                packet.WriteInteger(Preset.BackgroundOnly ? 2 : 1);
                packet.WriteString(Preset.ColorCode);
                packet.WriteInteger(Preset.ColorIntensity);
                i++;
            }
        }
    }
}