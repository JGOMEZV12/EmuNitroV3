using System.Collections.Generic;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Wired.Boxes.Add_ons
{
    /// <summary>
    /// Stub de WiredVariableReferenceSupport — implementar según arquitectura del emulador.
    /// </summary>
    static class WiredVariableReferenceSupport
    {
        public const int TARGET_USER = 0;
        public const int TARGET_ROOM = 1;
        public const int SHARED_AVAILABILITY = 11;

        public static SharedDefinitionOption FindSharedDefinition(
            Room room, int sourceRoomId, int sourceVariableItemId, int targetType)
        {
            // TODO: buscar la variable compartida entre salas
            return null;
        }

        public static List<RoomOption> LoadRoomOptions(Room room)
        {
            // TODO: cargar salas con variables compartidas disponibles
            return new List<RoomOption>();
        }

        public static SharedUserAssignment GetSharedUserAssignment(
            AddonVariableReferenceBox reference, int userId)
        {
            // TODO: obtener asignación compartida por usuario
            return null;
        }

        public static bool AssignSharedUserVariable(
            AddonVariableReferenceBox reference, int userId, int? value, bool overrideExisting)
        {
            return false;
        }

        public static bool RemoveSharedUserVariable(
            AddonVariableReferenceBox reference, int userId)
        {
            return false;
        }

        public static void CacheSharedUserAssignment(
            int roomId, int definitionItemId, int userId, int? value, int createdAt, int updatedAt)
        { }

        public static void ClearSharedUserAssignment(
            int roomId, int definitionItemId, int userId)
        { }

        public static void ClearSharedUserDefinition(int roomId, int definitionItemId) { }

        public class SharedDefinitionOption
        {
            public int RoomId { get; set; }
            public string RoomName { get; set; }
            public int ItemId { get; set; }
            public string Name { get; set; }
            public int TargetType { get; set; }
            public bool HasValue { get; set; }
        }

        public class SharedUserAssignment
        {
            public int? Value { get; set; }
            public int CreatedAt { get; set; }
            public int UpdatedAt { get; set; }
        }

        public class RoomOption
        {
            public int RoomId { get; set; }
            public string RoomName { get; set; }
            public List<SharedDefinitionOption> Variables { get; set; } = new();
        }
    }
}