using System;
using System.Text;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.Database.Interfaces;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Developers
{
    class RoomCommand : IChatCommand
    {
        public string PermissionRequired => "command_room";
        public string Parameters         => "%type%";
        public string Description        => "Gives you the ability to enable or disable basic room commands.";

        // ─── Entry point ─────────────────────────────────────────────────────────

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("Escribe :room list para ver los comandos disponibles.", 1);
                return;
            }

            switch (Params[1].ToLower())
            {
                // ── Info / List ──────────────────────────────────────────────────

                case "list":
                    ShowCommandList(Session);
                    break;

                case "info":
                    ShowRoomInfo(Session, Room);
                    break;

                // ── Room Settings ────────────────────────────────────────────────

                case "push":
                    ToggleRoom(Session, Room, "rooms", "push_enabled", ref Room.PushEnabled,
                        v => { Room.PushEnabled = v; Room.RoomData.PushEnabled = v; },
                        v => "Push mode is now " + OnOff(v));
                    break;

                case "spush":
                    ToggleRoom(Session, Room, "rooms", "spush_enabled", ref Room.SPushEnabled,
                        v => { Room.SPushEnabled = v; Room.RoomData.SPushEnabled = v; },
                        v => "Super Push mode is now " + OnOff(v));
                    break;

                case "pull":
                    ToggleRoom(Session, Room, "rooms", "pull_enabled", ref Room.PullEnabled,
                        v => { Room.PullEnabled = v; Room.RoomData.PullEnabled = v; },
                        v => "Pull mode is now " + OnOff(v));
                    break;

                case "spull":
                    ToggleRoom(Session, Room, "rooms", "spull_enabled", ref Room.SPullEnabled,
                        v => { Room.SPullEnabled = v; Room.RoomData.SPullEnabled = v; },
                        v => "Super Pull mode is now " + OnOff(v));
                    break;

                case "enable":
                case "enables":
                    ToggleRoom(Session, Room, "rooms", "enables_enabled", ref Room.EnablesEnabled,
                        v => { Room.EnablesEnabled = v; Room.RoomData.EnablesEnabled = v; },
                        v => "Enables mode set to " + OnOff(v));
                    break;

                case "respect":
                    ToggleRoom(Session, Room, "rooms", "respect_notifications_enabled", ref Room.RespectNotificationsEnabled,
                        v => { Room.RespectNotificationsEnabled = v; Room.RoomData.RespectNotificationsEnabled = v; },
                        v => "Respect notifications set to " + OnOff(v));
                    break;

                case "pets":
                case "morphs":
                    TogglePetMorphs(Session, Room);
                    break;

                // ── Roleplay Settings ────────────────────────────────────────────

                case "bank":
                    ToggleRoom(Session, Room, "rp_rooms", "bank_enabled", ref Room.BankEnabled,
                        v => { Room.BankEnabled = v; Room.RoomData.BankEnabled = v; },
                        v => "El modo 'Banco' está ahora " + OnOff(v));
                    break;

                case "shoot":
                    ToggleRoom(Session, Room, "rp_rooms", "shoot_enabled", ref Room.ShootEnabled,
                        v => { Room.ShootEnabled = v; Room.RoomData.ShootEnabled = v; },
                        v => "El modo 'Disparar' está ahora " + OnOff(v));
                    break;

                case "hit":
                    ToggleRoom(Session, Room, "rp_rooms", "hit_enabled", ref Room.HitEnabled,
                        v => { Room.HitEnabled = v; Room.RoomData.HitEnabled = v; },
                        v => "El modo 'Golpes' está ahora " + OnOff(v));
                    break;

                case "safezone":
                    ToggleRoom(Session, Room, "rp_rooms", "safezone_enabled", ref Room.SafeZoneEnabled,
                        v => { Room.SafeZoneEnabled = v; Room.RoomData.SafeZoneEnabled = v; },
                        v => "El modo 'Zona segura' está ahora " + OnOff(v));
                    break;

                case "learning":
                    ToggleRoom(Session, Room, "rp_rooms", "learning_enabled", ref Room.LearningEnabled,
                        v => { Room.LearningEnabled = v; Room.RoomData.LearningEnabled = v; },
                        v => "El modo 'Leer' está ahora " + OnOff(v));
                    break;

                case "rob":
                    ToggleRoom(Session, Room, "rp_rooms", "rob_enabled", ref Room.RobEnabled,
                        v => { Room.RobEnabled = v; Room.RoomData.RobEnabled = v; },
                        v => "El modo 'Robos' está ahora " + OnOff(v));
                    break;

                case "sex":
                case "sexcommands":
                    ToggleRoom(Session, Room, "rp_rooms", "sexcommands_enabled", ref Room.SexCommandsEnabled,
                        v => { Room.SexCommandsEnabled = v; Room.RoomData.SexCommandsEnabled = v; },
                        v => "El modo 'Comandos sexuales' está ahora " + OnOff(v));
                    break;

                case "turf":
                    ToggleRoom(Session, Room, "rp_rooms", "turf_enabled", ref Room.TurfEnabled,
                        v => { Room.TurfEnabled = v; Room.RoomData.TurfEnabled = v; },
                        v => "Turf settings set to " + OnOff(v));
                    break;

                case "gym":
                    ToggleRoom(Session, Room, "rp_rooms", "gym_enabled", ref Room.GymEnabled,
                        v => { Room.GymEnabled = v; Room.RoomData.GymEnabled = v; },
                        v => "Gym settings set to " + OnOff(v));
                    break;

                case "delivery":
                    ToggleRoom(Session, Room, "rp_rooms", "delivery_enabled", ref Room.DeliveryEnabled,
                        v => { Room.DeliveryEnabled = v; Room.RoomData.DeliveryEnabled = v; },
                        v => "Delivery settings set to " + OnOff(v));
                    break;

                case "tutorial":
                    ToggleRoom(Session, Room, "rp_rooms", "tutorial_enabled", ref Room.TutorialEnabled,
                        v => { Room.TutorialEnabled = v; Room.RoomData.TutorialEnabled = v; },
                        v => "Tutorial settings set to " + OnOff(v));
                    break;

                case "drive":
                case "car":
                    ToggleRoom(Session, Room, "rp_rooms", "drive_enabled", ref Room.DriveEnabled,
                        v => { Room.DriveEnabled = v; Room.RoomData.DriveEnabled = v; },
                        v => "El modo 'Condur' está ahora " + OnOff(v));
                    break;

                case "taxito":
                    ToggleRoom(Session, Room, "rp_rooms", "taxi_to_enabled", ref Room.TaxiToEnabled,
                        v => { Room.TaxiToEnabled = v; Room.RoomData.TaxiToEnabled = v; },
                        v => "El modo 'Taxi a' está ahora " + OnOff(v));
                    break;

                case "busto":
                    ToggleRoom(Session, Room, "rp_rooms", "bus_to_enabled", ref Room.BusToEnabled,
                        v => { Room.BusToEnabled = v; Room.RoomData.BusToEnabled = v; },
                        v => "El modo 'Bus a' está ahora " + OnOff(v));
                    break;

                case "taxifrom":
                    ToggleRoom(Session, Room, "rp_rooms", "taxi_from_enabled", ref Room.TaxiFromEnabled,
                        v => { Room.TaxiFromEnabled = v; Room.RoomData.TaxiFromEnabled = v; },
                        v => "El modo 'Taxi de' está ahora " + OnOff(v));
                    break;

                case "buycar":
                    ToggleRoom(Session, Room, "rp_rooms", "buycar_enabled", ref Room.BuyCarEnabled,
                        v => { Room.BuyCarEnabled = v; Room.RoomData.BuyCarEnabled = v; },
                        v => "El modo 'Concesionario' está ahora " + ActDes(v));
                    break;

                case "message":
                    SetEntranceMessage(Session, Room, Params);
                    break;

                // ── Zone Settings (exclusivos entre sí) ──────────────────────────

                case "hospital":
                    ToggleZone(Session, Room, "is_hospital", ref Room.IsHospital,
                        v => { Room.IsHospital = v; Room.RoomData.IsHospital = v; },
                        v => "El modo 'Hospital' está ahora " + ActDes(v), reinit: true);
                    break;

                case "prision":
                    ToggleZone(Session, Room, "is_prison", ref Room.IsPrison,
                        v => { Room.IsPrison = v; Room.RoomData.IsPrison = v; },
                        v => "El modo 'Prisión' está ahora " + ActDes(v), reinit: true);
                    break;

                case "prisionback":
                    ToggleZone(Session, Room, "is_prisonback", ref Room.IsPrison2,
                        v => { Room.IsPrison2 = v; Room.RoomData.IsPrison2 = v; },
                        v => "El modo 'Jailbreak' está ahora " + ActDes(v), reinit: true);
                    break;

                case "comisaria":
                case "polstation":
                    ToggleZone(Session, Room, "is_polstation", ref Room.IsPolStation,
                        v => { Room.IsPolStation = v; Room.RoomData.IsPolStation = v; },
                        v => "El modo 'Estación de Policía' está ahora " + ActDes(v), reinit: false);
                    break;

                case "camionero":
                    ToggleZone(Session, Room, "is_camionero", ref Room.IsCamionero,
                        v => { Room.IsCamionero = v; Room.RoomData.IsCamionero = v; },
                        v => "El modo 'Camionero' está ahora " + ActDes(v), reinit: false);
                    break;

                case "basurero":
                    ToggleZone(Session, Room, "is_basurero", ref Room.IsBasurero,
                        v => { Room.IsBasurero = v; Room.RoomData.IsBasurero = v; },
                        v => "El modo 'Basurero' está ahora " + ActDes(v), reinit: false);
                    break;

                case "huntzone":
                    ToggleZone(Session, Room, "huntzone_enabled", ref Room.HuntZoneEnabled,
                        v => { Room.HuntZoneEnabled = v; Room.RoomData.HuntZoneEnabled = v; },
                        v => "El modo 'Caza' está ahora " + ActDes(v), reinit: false);
                    break;

                // ── Comando desconocido ──────────────────────────────────────────

                default:
                    Session.SendWhisper($"Opción '{Params[1]}' no reconocida. Usa :room list para ver los comandos disponibles.", 1);
                    break;
            }
        }

        // ─── Helpers de toggle ────────────────────────────────────────────────────

        /// <summary>
        /// Invierte un booleano, actualiza Room + RoomData, guarda en DB y avisa al usuario.
        /// </summary>
        private void ToggleRoom(
            GameClients.GameClient session,
            Rooms.Room room,
            string table,
            string column,
            ref bool field,
            Action<bool> applyValue,
            Func<bool, string> getMessage)
        {
            bool newValue = !field;
            applyValue(newValue);
            PolarEnvironment.GetGame().GetRoomManager().UpdateRoom(room);

            using (IQueryAdapter db = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                db.SetQuery($"UPDATE `{table}` SET `{column}` = @val WHERE `id` = '{room.Id}' LIMIT 1");
                db.AddParameter("val", PolarEnvironment.BoolToEnum(newValue));
                db.RunQuery();
            }

            session.SendWhisper(getMessage(newValue), 1);
        }

        /// <summary>
        /// Como ToggleRoom pero además limpia las demás zonas exclusivas cuando se activa.
        /// </summary>
        private void ToggleZone(
            GameClients.GameClient session,
            Rooms.Room room,
            string column,
            ref bool field,
            Action<bool> applyValue,
            Func<bool, string> getMessage,
            bool reinit)
        {
            bool newValue = !field;
            applyValue(newValue);
            PolarEnvironment.GetGame().GetRoomManager().UpdateRoom(room);

            using (IQueryAdapter db = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                db.SetQuery($"UPDATE `rp_rooms` SET `{column}` = @val WHERE `id` = '{room.Id}' LIMIT 1");
                db.AddParameter("val", PolarEnvironment.BoolToEnum(newValue));
                db.RunQuery();
            }

            if (newValue)
                ClearOtherZones(room, column);

            if (reinit)
                PolarEnvironment.GetGame()._rproomManager.Init();

            session.SendWhisper(getMessage(newValue), 1);
        }

        /// <summary>
        /// Desactiva todas las zonas exclusivas excepto la que se acaba de activar.
        /// </summary>
        private void ClearOtherZones(Rooms.Room room, string exceptColumn)
        {
            if (exceptColumn != "is_hospital")  { room.IsHospital   = false; room.RoomData.IsHospital   = false; }
            if (exceptColumn != "is_prison")     { room.IsPrison     = false; room.RoomData.IsPrison     = false; }
            if (exceptColumn != "is_prisonback") { room.IsPrison2    = false; room.RoomData.IsPrison2    = false; }
            if (exceptColumn != "is_polstation") { room.IsPolStation = false; room.RoomData.IsPolStation = false; }
            if (exceptColumn != "is_camionero")  { room.IsCamionero  = false; room.RoomData.IsCamionero  = false; }
            if (exceptColumn != "is_basurero")   { room.IsBasurero   = false; room.RoomData.IsBasurero   = false; }
            if (exceptColumn != "is_mecanico")   { room.IsMecanico   = false; }
        }

        // ─── Casos especiales ─────────────────────────────────────────────────────

        private void TogglePetMorphs(GameClients.GameClient session, Rooms.Room room)
        {
            room.PetMorphsAllowed = !room.PetMorphsAllowed;
            room.RoomData.PetMorphsAllowed = room.PetMorphsAllowed;
            PolarEnvironment.GetGame().GetRoomManager().UpdateRoom(room);

            using (IQueryAdapter db = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                db.SetQuery($"UPDATE `rooms` SET `pet_morphs_allowed` = @val WHERE `id` = '{room.Id}' LIMIT 1");
                db.AddParameter("val", PolarEnvironment.BoolToEnum(room.PetMorphsAllowed));
                db.RunQuery();
            }

            session.SendWhisper("Human pet morphs set to " + OnOff(room.PetMorphsAllowed), 1);

            if (!room.PetMorphsAllowed)
            {
                foreach (RoomUser user in room.GetRoomUserManager().GetRoomUsers())
                {
                    var habbo = user?.GetClient()?.GetHabbo();
                    if (habbo == null) continue;

                    user.GetClient().SendWhisper("The room owner has disabled the ability to use a pet morph in this room.", 1);

                    if (habbo.PetId > 0)
                    {
                        user.GetClient().SendWhisper("Oops, the room owner has just disabled pet-morphs, un-morphing you.", 1);
                        habbo.PetId = 0;
                        room.SendMessage(new UserRemoveComposer(user.VirtualId));
                        room.SendMessage(new UsersComposer(user));
                    }
                }
            }
        }

        private void SetEntranceMessage(GameClients.GameClient session, Rooms.Room room, string[] Params)
        {
            if (Params.Length < 3)
            {
                session.SendWhisper("You need to type a room entrance message!", 1);
                return;
            }

            string message = CommandManager.MergeParams(Params, 2);
            room.EnterRoomMessage      = message;
            room.RoomData.EnterRoomMessage = message;
            PolarEnvironment.GetGame().GetRoomManager().UpdateRoom(room);

            using (IQueryAdapter db = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                db.SetQuery($"UPDATE `rp_rooms` SET `enter_message` = @msg WHERE `id` = '{room.Id}' LIMIT 1");
                db.AddParameter("msg", message);
                db.RunQuery();
            }

            session.SendWhisper("The room's enter message is now: " + message, 1);
        }

        // ─── Notificaciones ───────────────────────────────────────────────────────

        private void ShowCommandList(GameClients.GameClient session)
        {
            var sb = new StringBuilder();
            sb.Append("---------- Room Settings ----------\n\n");
            sb.Append("  push         · Push\n");
            sb.Append("  spush        · Super Push\n");
            sb.Append("  pull         · Pull\n");
            sb.Append("  spull        · Super Pull\n");
            sb.Append("  enable       · Enables\n");
            sb.Append("  respect      · Notificaciones de respetos\n");
            sb.Append("  pets/morphs  · Pet Morphs\n\n");
            sb.Append("---------- Roleplay Settings ----------\n\n");
            sb.Append("  bank         · Bank\n");
            sb.Append("  shoot        · Disparar\n");
            sb.Append("  hit          · Golpear\n");
            sb.Append("  safezone     · Zona segura\n");
            sb.Append("  rob          · Robos\n");
            sb.Append("  sex          · Comandos sexuales\n");
            sb.Append("  turf         · Turf\n");
            sb.Append("  gym          · Gym\n");
            sb.Append("  delivery     · Delivery\n");
            sb.Append("  tutorial     · Tutorial\n");
            sb.Append("  learning     · Leer\n");
            sb.Append("  drive/car    · Conducir\n");
            sb.Append("  taxito       · Taxi a\n");
            sb.Append("  busto        · Bus To\n");
            sb.Append("  taxifrom     · Taxi de\n");
            sb.Append("  buycar       · Concesionario\n");
            sb.Append("  message      · Mensaje de entrada\n\n");
            sb.Append("---------- Zone Settings ----------\n\n");
            sb.Append("  hospital     · Zona Hospital\n");
            sb.Append("  prision      · Zona Prisión\n");
            sb.Append("  prisionback  · Zona Jailbreak\n");
            sb.Append("  comisaria    · Zona Comisaría\n");
            sb.Append("  camionero    · Zona Camioneros\n");
            sb.Append("  basurero     · Zona Basureros\n\n");
            sb.Append("Usa :room info para ver el estado actual.");
            session.SendNotification(sb.ToString());
        }

        private void ShowRoomInfo(GameClients.GameClient session, Rooms.Room room)
        {
            var sb = new StringBuilder();
            sb.Append("---------- Room Settings ----------\n\n");
            sb.Append("Pet Morphs: "  + ED(room.PetMorphsAllowed)            + "\n");
            sb.Append("Pull: "         + ED(room.PullEnabled)                 + "\n");
            sb.Append("Push: "         + ED(room.PushEnabled)                 + "\n");
            sb.Append("Super Pull: "   + ED(room.SPullEnabled)                + "\n");
            sb.Append("Super Push: "   + ED(room.SPushEnabled)                + "\n");
            sb.Append("Notificaciones de respeto: "      + ED(room.RespectNotificationsEnabled) + "\n");
            sb.Append("Enables: "      + ED(room.EnablesEnabled)              + "\n\n");
            sb.Append("---------- Roleplay Settings ----------\n\n");
            sb.Append("Banco: "         + ED(room.BankEnabled)        + "\n");
            sb.Append("Disparar: "     + ED(room.ShootEnabled)       + "\n");
            sb.Append("Golpear: "      + ED(room.HitEnabled)         + "\n");
            sb.Append("Zona segura: "     + ED(room.SafeZoneEnabled)    + "\n");
            sb.Append("Robos: "      + ED(room.RobEnabled)         + "\n");
            sb.Append("Comandos sexuales: " + ED(room.SexCommandsEnabled) + "\n");
            sb.Append("Turf: "         + ED(room.TurfEnabled)        + "\n");
            sb.Append("Gym: "          + ED(room.GymEnabled)         + "\n");
            sb.Append("Delivery: "     + ED(room.DeliveryEnabled)    + "\n");
            sb.Append("Tutorial: "     + ED(room.TutorialEnabled)    + "\n");
            sb.Append("Leer: " + ED(room.LearningEnabled) + "\n");
            sb.Append("Conducir: "        + ED(room.DriveEnabled)       + "\n");
            sb.Append("Taxi a: "      + ED(room.TaxiToEnabled)      + "\n");
            sb.Append("Bus a: "       + ED(room.BusToEnabled)       + "\n");
            sb.Append("Taxi de: "    + ED(room.TaxiFromEnabled)    + "\n");
            sb.Append("Mensaje de entrada: " + room.EnterRoomMessage   + "\n\n");
            sb.Append("---------- Zone Settings ----------\n\n");
            sb.Append("Hospital: "   + AD(room.IsHospital)   + "\n");
            sb.Append("Prisión: "    + AD(room.IsPrison)     + "\n");
            sb.Append("Jailbreak: "  + AD(room.IsPrison2)    + "\n");
            sb.Append("Comisaría: "  + AD(room.IsPolStation) + "\n");
            sb.Append("Camioneros: " + AD(room.IsCamionero)  + "\n");
            sb.Append("Basureros: "  + AD(room.IsBasurero)   + "\n");
            session.SendNotification(sb.ToString());
        }

        // ─── Utilidades de texto ──────────────────────────────────────────────────

        private static string OnOff(bool v)  => v ? "activado!"  : "desactivado!";
        private static string ED(bool v)     => v ? "activado" : "desactivado";
        private static string AD(bool v)     => v ? "activado"  : "desactivado";
        private static string ActDes(bool v) => v ? "activado." : "desactivado.";
    }
}