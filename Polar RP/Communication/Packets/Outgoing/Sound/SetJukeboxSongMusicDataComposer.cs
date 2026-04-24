using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.TraxMachine;

namespace Polar.Communication.Packets.Outgoing.Sound
{
    class SetJukeboxSongMusicDataComposer : ServerPacket
    {
        public SetJukeboxSongMusicDataComposer(ICollection<TraxMusicData> Songs)
            : base(ServerPacketHeader.SetJukeboxSongMusicDataMessageComposer)
        {
            base.WriteInteger(Songs.Count);//while

            foreach (var item in Songs)
            {
                base.WriteInteger(item.Id);// Song id
                base.WriteString(item.CodeName); // Song code name
                base.WriteString(item.Name);
                base.WriteString(item.Data);
                base.WriteInteger((int)(item.Length * 1000.0)); // Music Length - Duration
                base.WriteString(item.Artist);
            }
        }
    }
}