using System.Collections.Generic;

namespace Polar.Communication.Packets.Outgoing.FurniEditor
{
    public class FurniEditorInteractionsComposer : ServerPacket
    {
        public FurniEditorInteractionsComposer(IReadOnlyList<string> interactions)
            : base(ServerPacketHeader.FurniEditorInteractionsComposer)
        {
            WriteInteger(interactions?.Count ?? 0);

            if (interactions != null)
            {
                foreach (string interaction in interactions)
                    WriteString(interaction ?? string.Empty);
            }
        }
    }
}