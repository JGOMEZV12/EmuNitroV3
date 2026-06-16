using Polar.Core;
using Polar.Database.Interfaces;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.FurniEditor;

namespace Polar.Communication.Packets.Incoming.FurniEditor
{
    public class FurniEditorBySpriteEvent : IPacketEvent
    {
        public void Parse(GameClient session, ClientPacket packet)
        {
            if (!session.GetHabbo().GetPermissions().HasRight("acc_catalogfurni"))
            {
                session.SendMessage(new FurniEditorResultComposer(false, "No permission"));
                return;
            }

            int spriteId = packet.PopInt();
            if (spriteId <= 0)
            {
                session.SendMessage(new FurniEditorResultComposer(false, "Invalid sprite ID"));
                return;
            }

            int itemId = -1;

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery($"SELECT `id` FROM `{DatabaseCompatibility.FurnitureTable}` WHERE `{DatabaseCompatibility.FurniSpriteIdColumn}` = @spriteId LIMIT 1");
                dbClient.AddParameter("spriteId", spriteId);
                itemId = dbClient.getInteger();
            }

            if (itemId <= 0)
            {
                session.SendMessage(new FurniEditorResultComposer(false, $"No item found with sprite_id: {spriteId}"));
                return;
            }

            FurniEditorDetailEvent.SendDetailResponse(session, itemId);
        }
    }
}