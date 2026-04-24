using log4net;
using Polar.Core;
using System.Collections.Generic;
using System.Data;

namespace Polar.HabboHotel.Rooms.TraxMachine
{
    public class TraxSoundManager
    {
        // FIX: Dictionary para búsqueda O(1) por Id — antes era List con foreach O(n)
        private static Dictionary<int, TraxMusicData> _songs = new Dictionary<int, TraxMusicData>();

        // FIX: readonly — este campo nunca se reasigna
        private static readonly ILog Log = LogManager.GetLogger("Polar.HabboHotel.Rooms.TraxMachine");

        // Exposición de solo lectura para los casos que necesiten iterar la colección
        public static IReadOnlyCollection<TraxMusicData> Songs => _songs.Values;

        public static void Init()
        {
            _songs.Clear();

            DataTable table;
            using (var adap = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                adap.RunQuery("SELECT * FROM jukebox_songs_data");
                table = adap.getTable();
            }

            // FIX: null check — getTable() puede retornar null y reventar el foreach
            if (table == null)
            {
                Log.Warn("jukebox_songs_data returned a null DataTable — no songs loaded.");
                return;
            }

            foreach (DataRow row in table.Rows)
            {
                var music = TraxMusicData.Parse(row);
                if (music == null)
                    continue;

                // FIX: evitar duplicados por Id
                if (!_songs.ContainsKey(music.Id))
                    _songs.Add(music.Id, music);
                else
                    Log.Warn("Duplicate jukebox song Id: " + music.Id + " — skipped.");
            }

            Log.Info("Loaded " + _songs.Count + " Jukebox Songs.");
        }

        // FIX: O(1) con Dictionary en lugar de O(n) con foreach sobre List
        public static TraxMusicData GetMusic(int id)
        {
            _songs.TryGetValue(id, out TraxMusicData music);
            return music;
        }
    }
}