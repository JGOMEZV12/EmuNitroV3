namespace Polar.Communication.Packets.Outgoing.FurniEditor
{
    public class FurniEditorResultComposer : ServerPacket
    {
        public FurniEditorResultComposer(bool success, string message)
            : this(success, message, -1) { }

        public FurniEditorResultComposer(bool success, string message, int id)
            : base(ServerPacketHeader.FurniEditorResultComposer)
        {
            WriteBoolean(success);
            WriteString(message ?? string.Empty);
            WriteInteger(id);
        }
    }
}