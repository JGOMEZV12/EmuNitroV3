using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Wired;

namespace Polar.Communication.Packets.Outgoing.Rooms.Furni.Wired
{
    public class WiredUserVariablesDataComposer : ServerPacket
    {
        public WiredUserVariablesDataComposer(
            RoomUserVariableManager.Snapshot userSnapshot,
            RoomFurniVariableManager.Snapshot furniSnapshot,
            RoomVariableManager.Snapshot roomSnapshot)
            : base(ServerPacketHeader.WiredUserVariablesDataComposer)
        {
            // ── Room ID ───────────────────────────────────────────────────────────
            int roomId = 0;
            if (userSnapshot != null) roomId = userSnapshot.RoomId;
            else if (furniSnapshot != null) roomId = furniSnapshot.RoomId;
            else if (roomSnapshot != null) roomId = roomSnapshot.RoomId;

            base.WriteInteger(roomId);

            // ══════════════════════════════════════════════════════════════════════
            //  USER VARIABLES
            // ══════════════════════════════════════════════════════════════════════

            // Definitions block — nuestra versión C# no tiene DefinitionEntry en el
            // snapshot (no tenemos WiredVariableDefinitionInfo en el port), así que
            // escribimos 0 para que el cliente sepa que no hay definiciones extra.
            base.WriteInteger(0); // userSnapshot.getDefinitions().size()

            // User assignments
            int userCount = userSnapshot?.Users?.Count ?? 0;
            base.WriteInteger(userCount);

            if (userSnapshot != null)
            {
                foreach (var user in userSnapshot.Users)
                {
                    base.WriteInteger(user.UserId);
                    base.WriteInteger(user.Assignments.Count);

                    foreach (var a in user.Assignments)
                    {
                        base.WriteInteger(a.VariableItemId);
                        base.WriteBoolean(a.HasValue);
                        base.WriteInteger(a.Value ?? 0);
                        base.WriteInteger(a.CreatedAt);
                        base.WriteInteger(a.UpdatedAt);
                    }
                }
            }

            // ══════════════════════════════════════════════════════════════════════
            //  FURNI VARIABLES
            // ══════════════════════════════════════════════════════════════════════

            base.WriteInteger(0); // furniSnapshot.getDefinitions().size()

            int furniCount = furniSnapshot?.Furnis?.Count ?? 0;
            base.WriteInteger(furniCount);

            if (furniSnapshot != null)
            {
                foreach (var furni in furniSnapshot.Furnis)
                {
                    base.WriteInteger(furni.FurniId);
                    base.WriteInteger(furni.Assignments.Count);

                    foreach (var a in furni.Assignments)
                    {
                        base.WriteInteger(a.VariableItemId);
                        base.WriteBoolean(a.HasValue);
                        base.WriteInteger(a.Value ?? 0);
                        base.WriteInteger(a.CreatedAt);
                        base.WriteInteger(a.UpdatedAt);
                    }
                }
            }

            // ══════════════════════════════════════════════════════════════════════
            //  ROOM VARIABLES
            // ══════════════════════════════════════════════════════════════════════

            base.WriteInteger(0); // roomSnapshot.getDefinitions().size()

            int roomAssignCount = roomSnapshot?.Assignments?.Count ?? 0;
            base.WriteInteger(roomAssignCount);

            if (roomSnapshot != null)
            {
                foreach (var a in roomSnapshot.Assignments)
                {
                    base.WriteInteger(a.VariableItemId);
                    base.WriteBoolean(true);  // HasValue — RoomVariableManager siempre tiene valor (int, no null)
                    base.WriteInteger(a.Value);
                    base.WriteInteger(a.CreatedAt);
                    base.WriteInteger(a.UpdatedAt);
                }
            }

            // ══════════════════════════════════════════════════════════════════════
            //  CONTEXT DEFINITIONS  (WiredContextVariableSupport en Java)
            //  Sin equivalente en este port — escribir 0.
            // ══════════════════════════════════════════════════════════════════════
            base.WriteInteger(0);
        }
    }
}