using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using log4net;
using Polar.Database.Interfaces;

namespace Polar.HabboRoleplay.RPRoom
{
    public class RPRoomManager
    {
        public readonly ILog log = LogManager.GetLogger("Polar.HabboRoleplay.RPRoom");

        // Cada ciudad puede tener varias salas del mismo tipo
        public readonly Dictionary<string, List<RPRoom>> HospitalRooms    = new();
        public readonly Dictionary<string, List<RPRoom>> JailRooms        = new();
        public readonly Dictionary<string, List<RPRoom>> CourtRooms       = new();
        public readonly Dictionary<string, List<RPRoom>> JailBack         = new();
        public readonly Dictionary<string, List<RPRoom>> PolStationRooms  = new();
        public readonly Dictionary<string, List<RPRoom>> CamionerosRooms  = new();
        public readonly Dictionary<string, List<RPRoom>> BasurerosRooms   = new();
        public readonly Dictionary<string, List<RPRoom>> ArmerosRooms     = new();

        private static readonly Random _rng = new Random();

        public RPRoomManager() { }

        // ─── Init ─────────────────────────────────────────────────────────────────

        public void Init()
        {
            HospitalRooms.Clear();
            JailRooms.Clear();
            JailBack.Clear();
            CourtRooms.Clear();
            PolStationRooms.Clear();
            CamionerosRooms.Clear();
            BasurerosRooms.Clear();
            ArmerosRooms.Clear();

            // Una sola query trae todas las salas relevantes de una vez
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery(
                    "SELECT * FROM `rp_rooms` " +
                    "WHERE `is_hospital` = '1' OR `is_prison` = '1' OR `is_prisonback` = '1' " +
                    "   OR `is_court` = '1' OR `is_camionero` = '1' OR `is_basurero` = '1' " +
                    "   OR `is_polstation` = '1' OR `is_armero` = '1'");

                DataTable table = dbClient.getTable();
                if (table == null) return;

                foreach (DataRow row in table.Rows)
                {
                    RPRoom room = BuildRoom(row);
                    string city = Convert.ToString(row["city"]);

                    if (PolarEnvironment.EnumToBool(row["is_hospital"].ToString()))   AddToDict(HospitalRooms,   city, room);
                    if (PolarEnvironment.EnumToBool(row["is_prison"].ToString()))     AddToDict(JailRooms,       city, room);
                    if (PolarEnvironment.EnumToBool(row["is_prisonback"].ToString())) AddToDict(JailBack,        city, room);
                    if (PolarEnvironment.EnumToBool(row["is_court"].ToString()))      AddToDict(CourtRooms,      city, room);
                    if (PolarEnvironment.EnumToBool(row["is_camionero"].ToString()))  AddToDict(CamionerosRooms, city, room);
                    if (PolarEnvironment.EnumToBool(row["is_basurero"].ToString()))   AddToDict(BasurerosRooms,  city, room);
                    if (PolarEnvironment.EnumToBool(row["is_polstation"].ToString())) AddToDict(PolStationRooms, city, room);
                    if (PolarEnvironment.EnumToBool(row["is_armero"].ToString()))     AddToDict(ArmerosRooms,    city, room);
                }
            }

            Out.WriteLine($"{TotalRooms(HospitalRooms)} Hospital(es) -> CARGADO",          "Polar.HabboRoleplay.RPRoom", ConsoleColor.DarkGray);
            Out.WriteLine($"{TotalRooms(JailRooms)} Prision(es) -> CARGADO",               "Polar.HabboRoleplay.RPRoom", ConsoleColor.DarkGray);
            Out.WriteLine($"{TotalRooms(JailBack)} Prisonback(es) -> CARGADO",             "Polar.HabboRoleplay.RPRoom", ConsoleColor.DarkGray);
            Out.WriteLine($"{TotalRooms(PolStationRooms)} Police Station(es) -> CARGADO",  "Polar.HabboRoleplay.RPRoom", ConsoleColor.DarkGray);
            Out.WriteLine($"{TotalRooms(CamionerosRooms)} Camioneros Zone(es) -> CARGADO", "Polar.HabboRoleplay.RPRoom", ConsoleColor.DarkGray);
            Out.WriteLine($"{TotalRooms(BasurerosRooms)} Basureros Zone(es) -> CARGADO",   "Polar.HabboRoleplay.RPRoom", ConsoleColor.DarkGray);
        }

        // ─── Lookups (eligen una sala aleatoria de la ciudad) ─────────────────────

        public int TryToGetHospital(string city, out RPRoom room)   => TryGetRandom(HospitalRooms,   city, out room);
        public int TryToGetJail(string city, out RPRoom room)       => TryGetRandom(JailRooms,       city, out room);
        public int TryToGetJailBack(string city, out RPRoom room)   => TryGetRandom(JailBack,        city, out room);
        public int TryToGetCourt(string city, out RPRoom room)      => TryGetRandom(CourtRooms,      city, out room);
        public int TryToGetCamioneros(string city, out RPRoom room) => TryGetRandom(CamionerosRooms, city, out room);
        public int TryToGetPolStation(string city, out RPRoom room) => TryGetRandom(PolStationRooms, city, out room);
        public int TryToGetBasureros(string city, out RPRoom room)  => TryGetRandom(BasurerosRooms,  city, out room);
        public int TryToGetArmeros(string city, out RPRoom room)    => TryGetRandom(ArmerosRooms,    city, out room);

        // ─── Helpers privados ─────────────────────────────────────────────────────

        /// <summary>
        /// Devuelve el Id de una sala aleatoria entre las disponibles para esa ciudad.
        /// Devuelve 0 si no hay ninguna.
        /// </summary>
        private static int TryGetRandom(Dictionary<string, List<RPRoom>> dict, string city, out RPRoom room)
        {
            if (dict.TryGetValue(city, out List<RPRoom> rooms) && rooms.Count > 0)
            {
                room = rooms[_rng.Next(rooms.Count)];
                return room.Id;
            }

            room = null;
            return 0;
        }

        /// <summary>
        /// Agrega una sala a la lista correspondiente a la ciudad,
        /// creando la entrada si todavía no existe.
        /// </summary>
        private static void AddToDict(Dictionary<string, List<RPRoom>> dict, string city, RPRoom room)
        {
            if (!dict.TryGetValue(city, out List<RPRoom> list))
            {
                list = new List<RPRoom>();
                dict[city] = list;
            }
            list.Add(room);
        }

        /// <summary>
        /// Construye un RPRoom a partir de un DataRow.
        /// </summary>
        private static RPRoom BuildRoom(DataRow row)
        {
            return new RPRoom(
                Convert.ToInt32(row["id"]),
                Convert.ToString(row["city"]),
                PolarEnvironment.EnumToBool(row["is_court"].ToString()),
                PolarEnvironment.EnumToBool(row["is_hospital"].ToString()),
                PolarEnvironment.EnumToBool(row["is_prison"].ToString()),
                PolarEnvironment.EnumToBool(row["is_prisonback"].ToString()),
                PolarEnvironment.EnumToBool(row["is_camionero"].ToString()),
                PolarEnvironment.EnumToBool(row["is_mecanico"].ToString()),
                PolarEnvironment.EnumToBool(row["is_basurero"].ToString()),
                PolarEnvironment.EnumToBool(row["is_armero"].ToString()),
                PolarEnvironment.EnumToBool(row["is_polstation"].ToString()));
        }

        /// <summary>
        /// Cuenta el total de salas en un diccionario (sumando todas las ciudades).
        /// </summary>
        private static int TotalRooms(Dictionary<string, List<RPRoom>> dict)
            => dict.Values.Sum(list => list.Count);
    }
}