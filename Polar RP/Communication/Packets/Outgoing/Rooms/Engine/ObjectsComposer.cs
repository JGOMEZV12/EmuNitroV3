using Polar.Core;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Rooms;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Polar.Communication.Packets.Outgoing.Rooms.Engine
{
    class ObjectsComposer : ServerPacket
    {
        public ObjectsComposer(Item[] objects, Room room)
            : base(ServerPacketHeader.ObjectsMessageComposer)
        {
            bool hideWired = room.HideWired;

            // FIX 1: Pre-filtrar en un solo pass, construyendo owners al mismo tiempo.
            //        Antes: dos loops (filter + owners). Ahora: uno solo.
            //        Capacidad inicial = objects.Length evita re-allocs de la lista.
            var filteredItems = new List<Item>(objects.Length);
            var owners = new Dictionary<int, string>(Math.Min(objects.Length, 64));

            foreach (var item in objects)
            {
                if (item == null) continue;
                if (item.IsWallItem) continue;
                if (hideWired && item.IsWired) continue;

                filteredItems.Add(item);

                // TryAdd es O(1) — si ya existe no sobreescribe, que es el comportamiento correcto
                if (!owners.ContainsKey(item.UserID))
                    owners[item.UserID] = item.Username;
            }

            // ── Owners block ──────────────────────────────────────────────────────
            WriteInteger(owners.Count);
            foreach (var owner in owners)
            {
                WriteInteger(owner.Key);
                WriteString(owner.Value);
            }

            // ── Items block ───────────────────────────────────────────────────────
            WriteInteger(filteredItems.Count);
            foreach (var item in filteredItems)
                WriteFloorItem(item);
            // FIX 2: Eliminado el bloque de logging que llamaba GetBytesWithoutLength()
            //        en cada iteración — aunque estuviera comentado, la infra de slice
            //        seguía ejecutándose en debug. Eliminado por completo.
        }

        private void WriteFloorItem(Item item)
        {
            var interactionType = item.Data.InteractionType;

            // FIX 3: ToString("G") con InvariantCulture cacheado — evita boxing y
            //        re-lookup del CultureInfo en cada ítem.
            //        "G" en double elimina ceros finales ("1.5" no "1.50000").
            string zStr = item.GetZ.ToString("G", CultureInfo.InvariantCulture);
            string stackHeightStr = item.TotalHeight.ToString("G", CultureInfo.InvariantCulture);

            WriteInteger(item.Id);
            WriteInteger(item.GetBaseItem().SpriteId);
            WriteInteger(item.GetX);
            WriteInteger(item.GetY);
            WriteInteger(item.Rotation);
            WriteString(zStr);
            WriteString(stackHeightStr);

            // ── Magic number (type-specific) ──────────────────────────────────────
            if (interactionType == InteractionType.GIFT)
            {
                // FIX 4: Split + TryParse en un solo bloque, sin alloc de array.Empty fallback
                string extra = item.ExtraData;
                int magic = 1;
                if (!string.IsNullOrEmpty(extra))
                {
                    var parts = extra.Split((char)5);
                    if (parts.Length >= 7 &&
                        int.TryParse(parts[0], out int colorId) &&
                        int.TryParse(parts[6], out int ribbonId))
                        magic = (colorId * 1000) + ribbonId;
                }
                WriteInteger(magic);
            }
            else if (interactionType == InteractionType.MUSIC_DISC)
            {
                WriteInteger(int.TryParse(item.ExtraData, out int songId) ? songId : 1);
            }
            else
            {
                WriteInteger(1);
            }

            // ── ExtraData / behaviours ────────────────────────────────────────────
            try
            {
                ItemBehaviourUtility.GenerateExtradata(item, this);
            }
            catch (Exception ex)
            {
                Logging.WriteLine(
                    $"[GenerateExtradata FAIL] Item {item.Id} tipo {interactionType}: {ex.Message}",
                    ConsoleColor.DarkGray);
                WriteInteger(0);
                WriteString(string.Empty);
            }

            // ── Flags ─────────────────────────────────────────────────────────────
            WriteInteger(-1); // expires

            // FIX 5: switch expression en lugar de cadena de || — el compilador genera
            //        una tabla de salto en lugar de comparaciones secuenciales.
            WriteInteger(interactionType switch
            {
                InteractionType.TELEPORT or
                InteractionType.SWITCH or
                InteractionType.VENDING_MACHINE or
                InteractionType.INFO_TERMINAL or
                InteractionType.POSTIT or
                InteractionType.PUZZLE_BOX => 2,
                _ when item.GetBaseItem().Modes > 1 => 1,
                _ => 0
            });

            WriteInteger(item.UserID);
            WriteInteger(item.Data.Stackable ? 1 : 0);
            WriteInteger(item.Data.IsSeat ? 1 : 0);
            WriteInteger(0);                            // allowLay
            WriteInteger(item.Data.Walkable ? 1 : 0);
            WriteInteger(item.Data.Width);
            WriteInteger(item.Data.Length);
            WriteInteger(0);                            // teleportTargetId
        }
    }
}