using Polar.Core;
using Polar.HabboHotel.Items;
using Polar.Utilities;
using System;

namespace Polar.Communication.Packets.Outgoing.Rooms.Engine
{
    internal class ObjectUpdateComposer : ServerPacket
    {
        public Item Item { get; }
        public int UserId { get; }

        public ObjectUpdateComposer(Item item, string itemOwnerName)
            : base(ServerPacketHeader.ObjectUpdateMessageComposer)
        {
            Item = item;
            UserId = item.UserID;

            itemOwnerName ??= string.Empty;

            var interactionType = item.Data.InteractionType;
            string zStr = item.GetZ.ToString("G", System.Globalization.CultureInfo.InvariantCulture);
            string stackHeightStr = item.TotalHeight.ToString("G", System.Globalization.CultureInfo.InvariantCulture);

            // ── serializeFloorData ────────────────────────────────────────────
            WriteInteger(item.Id);
            WriteInteger(item.GetBaseItem().SpriteId);
            WriteInteger(item.GetX);
            WriteInteger(item.GetY);
            WriteInteger(item.Rotation);
            WriteString(zStr);

            WriteString(item.GetZ.ToString("G", System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty); // FIX: era "0.0"

            // ── _extra ────────────────────────────────────────────────────────
            // Java: Gift=colorId*1000+ribbonId | MusicDisc=songId | StackWalkHelper=2147483001 | default=1
            if (interactionType == InteractionType.GIFT)
            {
                string[] parts = item.ExtraData?.Split((char)5) ?? Array.Empty<string>();
                if (parts.Length >= 7 &&
                    int.TryParse(parts[0], out int colorId) &&
                    int.TryParse(parts[6], out int ribbonId))
                    WriteInteger((colorId * 1000) + ribbonId);
                else
                    WriteInteger(1);
            }
            else if (interactionType == InteractionType.MUSIC_DISC)
            {
                WriteInteger(int.TryParse(item.ExtraData, out int songId) ? songId : 1);
            }
            else
            {
                WriteInteger(1); // FIX: era 0
            }

            // ── _data — GenerateExtradata maneja LimitedNo internamente ───────
            try
            {
                ItemBehaviourUtility.GenerateExtradata(item, this);
            }
            catch (Exception ex)
            {
                Logging.WriteLine(
                    $"[GenerateExtradata FAIL] Item {item.Id} tipo {interactionType} ExtraData='{item.ExtraData}': {ex.Message}",
                    ConsoleColor.DarkGray);
                WriteInteger(0);
                WriteString(string.Empty);
            }

            // ── _expires ──────────────────────────────────────────────────────
            WriteInteger(-1);

            // ── _usagePolicy ──────────────────────────────────────────────────
            // FIX: no siempre 0 — misma lógica que ObjectsComposer/ObjectAddComposer
           
            WriteInteger(0);

            WriteInteger(item.UserID);
            WriteInteger(item.Data.Stackable ? 1 : 0);
            WriteInteger(item.Data.IsSeat ? 1 : 0);
            WriteInteger(0); // FIX: era 0
            WriteInteger(item.Data.Walkable ? 1 : 0);
            WriteInteger(item.Data.Width);
            WriteInteger(item.Data.Length);
            WriteInteger(0);
            WriteString(Convert.ToString(itemOwnerName));          // FIX: parámetro, no lookup

            if (item.GetBaseItem().SpriteId < 0)
                WriteString(item.GetBaseItem().ItemName);
        }

        // Sobrecarga para compatibilidad con código que no pasa el nombre
        public ObjectUpdateComposer(Item item, int userId)
            : this(item, PolarEnvironment.GetUserInfoBy("username", "id", item.UserID.ToString()))
        { }
    }
}