using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Polar.Communication.Packets.Outgoing.Rooms.Furni.Wired
{
    class WiredMonitorDataComposer : ServerPacket
    {
        public WiredMonitorDataComposer(ConcurrentDictionary<string, string> variables)
            : base(ServerPacketHeader.WiredMonitorDataComposer)
        {
            WriteInteger(variables.Count);
            foreach (var kvp in variables)
            {
                WriteString(kvp.Key);
                WriteString(kvp.Value);
            }
        }
    }
}