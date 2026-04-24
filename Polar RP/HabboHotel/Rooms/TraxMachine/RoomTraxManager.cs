using Polar.Communication.Packets.Outgoing.Sound;
using Polar.HabboHotel.Items;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Polar.HabboHotel.Rooms.TraxMachine
{
    public class RoomTraxManager
    {
        // ─────────────────────────────────────
        //  Propiedades
        // ─────────────────────────────────────
        public Room Room { get; private set; }
        public List<Item> Playlist { get; private set; }
        public bool IsPlaying { get; private set; }
        public int StartedPlayTimestamp { get; private set; }
        public Item SelectedDiscItem { get; private set; }
        public TraxMusicData AnteriorMusic { get; private set; }
        public Item AnteriorItem { get; private set; }

        // FIX: Capacity como propiedad con setter privado — no exponer mutación libre
        public int Capacity { get; private set; } = 10;

        // ─────────────────────────────────────
        //  Constructor
        // ─────────────────────────────────────
        public RoomTraxManager(Room room)
        {
            Room = room;
            IsPlaying = false;
            StartedPlayTimestamp = 0;
            Playlist = new List<Item>();
            SelectedDiscItem = null;

            // FIX: dataTable ya no se guarda como campo — se consume aquí y se libera
            LoadPlaylistFromDatabase();
        }

        // ─────────────────────────────────────
        //  Carga inicial desde BD
        // ─────────────────────────────────────
        private void LoadPlaylistFromDatabase()
        {
            DataTable dataTable = null;

            // FIX: query parametrizada — antes concatenaba Room.Id directamente (SQL injection)
            using (var adap = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                adap.SetQuery("SELECT * FROM room_jukebox_songs WHERE room_id = @roomId");
                adap.AddParameter("roomId", Room.Id);
                adap.RunQuery();
                dataTable = adap.getTable();
            }

            if (dataTable == null)
                return;

            foreach (DataRow row in dataTable.Rows)
            {
                if (!int.TryParse(row["item_id"].ToString(), out int itemId))
                    continue;

                var item = Room.GetRoomItemHandler().GetItem(itemId);
                if (item == null)
                    continue;

                Playlist.Add(item);
            }
        }

        // ─────────────────────────────────────
        //  Ciclo
        // ─────────────────────────────────────
        public void OnCycle()
        {
            if (!IsPlaying)
                return;

            // FIX: ActualSongData se calculaba DOS veces en el original — ahora una sola vez
            var currentSong = ActualSongData;

            if (currentSong != SelectedDiscItem)
            {
                AnteriorItem = SelectedDiscItem;
                AnteriorMusic = GetMusicByItem(SelectedDiscItem);
                SelectedDiscItem = currentSong;

                if (SelectedDiscItem == null)
                {
                    StopPlayList();
                    return;
                }

                Room.SendMessage(new SetJukeboxNowPlayingComposer(Room));
            }
        }

        // ─────────────────────────────────────
        //  Playlist management
        // ─────────────────────────────────────
        public void ClearPlayList()
        {
            if (IsPlaying)
                StopPlayList();

            Playlist.Clear();
        }

        public void PlayPlaylist()
        {
            if (Playlist.Count == 0)
                return;

            StartedPlayTimestamp = (int)PolarEnvironment.GetUnixTimestamp();
            SelectedDiscItem = null;
            IsPlaying = true;

            Room.SendMessage(new SetJukeboxNowPlayingComposer(Room));
            SetJukeboxesState();
        }

        public void StopPlayList()
        {
            IsPlaying = false;
            StartedPlayTimestamp = 0;
            SelectedDiscItem = null;

            Room.SendMessage(new SetJukeboxNowPlayingComposer(Room));
            SetJukeboxesState();
        }

        public void TriggerPlaylistState()
        {
            if (IsPlaying)
                StopPlayList();
            else
                PlayPlaylist();
        }

        public void SetJukeboxesState()
        {
            foreach (var item in Room.GetRoomItemHandler().GetFloor)
            {
                if (item.GetBaseItem().InteractionType == InteractionType.JUKEBOX)
                {
                    item.ExtraData = IsPlaying ? "1" : "0";
                    item.UpdateState();
                }
            }
        }

        // ─────────────────────────────────────
        //  Añadir / quitar discos
        // ─────────────────────────────────────
        public bool AddDisc(Item item)
        {
            if (item == null)
                return false;

            if (item.GetBaseItem().InteractionType != InteractionType.MUSIC_DISC)
                return false;

            // FIX: usa ExtradataInt consistentemente (antes mezclaba ExtraData string con ExtradataInt)
            var music = TraxSoundManager.GetMusic(item.ExtradataInt);
            if (music == null)
                return false;

            if (Playlist.Contains(item))
                return false;

            if (IsPlaying)
                return false;

            if (Playlist.Count >= Capacity)
                return false;

            using (var adap = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                adap.SetQuery("INSERT INTO room_jukebox_songs (room_id, item_id) VALUES (@room, @item)");
                adap.AddParameter("room", Room.Id);
                adap.AddParameter("item", item.Id);
                adap.RunQuery();
            }

            Playlist.Add(item);
            Room.SendMessage(new SetJukeboxPlayListComposer(Room));
            Room.SendMessage(new LoadJukeboxUserMusicItemsComposer(Room));
            return true;
        }

        public bool RemoveDisc(int id)
        {
            var item = GetDiscItem(id);
            if (item == null)
                return false;

            if (IsPlaying)
                return false;

            // FIX: query parametrizada — antes concatenaba item.Id directamente (SQL injection)
            using (var adap = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                adap.SetQuery("DELETE FROM room_jukebox_songs WHERE item_id = @itemId");
                adap.AddParameter("itemId", item.Id);
                adap.RunQuery();
            }

            Playlist.Remove(item);
            Room.SendMessage(new SetJukeboxPlayListComposer(Room));
            Room.SendMessage(new LoadJukeboxUserMusicItemsComposer(Room));
            return true;
        }

        public bool RemoveDisc(Item item)
        {
            if (item == null)
                return false;

            return RemoveDisc(item.Id);
        }

        // ─────────────────────────────────────
        //  Helpers de consulta
        // ─────────────────────────────────────
        public List<Item> GetAvaliableSongs()
        {
            return Room.GetRoomItemHandler().GetFloor
                .Where(c => c.GetBaseItem().InteractionType == InteractionType.MUSIC_DISC
                         && !Playlist.Contains(c))
                .ToList();
        }

        public Item GetDiscItem(int id)
        {
            foreach (var item in Playlist)
                if (item.Id == id)
                    return item;

            return null;
        }

        public TraxMusicData GetMusicByItem(Item item)
        {
            return item != null ? TraxSoundManager.GetMusic(item.ExtradataInt) : null;
        }

        // FIX: retorna -1 cuando no se encuentra (antes retornaba 0, igual que posición 0)
        public int GetMusicIndex(Item item)
        {
            for (var i = 0; i < Playlist.Count; i++)
            {
                if (Playlist[i] == item)
                    return i;
            }
            return -1;
        }

        // ─────────────────────────────────────
        //  Línea de tiempo de reproducción
        // ─────────────────────────────────────

        // FIX: SortedDictionary garantiza orden por key (tiempo acumulado)
        // El Dictionary original no garantizaba orden en la iteración
        public SortedDictionary<int, Item> GetPlayLine()
        {
            var result = new SortedDictionary<int, Item>();
            int elapsed = 0;

            foreach (var item in Playlist)
            {
                var music = GetMusicByItem(item);
                if (music == null)
                    continue;

                result.Add(elapsed, item);
                elapsed += music.Length;
            }

            return result;
        }

        // ─────────────────────────────────────
        //  Propiedades calculadas de tiempo
        // ─────────────────────────────────────
        public int TimestampSinceStarted
        {
            get { return (int)PolarEnvironment.GetUnixTimestamp() - StartedPlayTimestamp; }
        }

        public int TotalPlayListLength
        {
            get
            {
                int total = 0;
                foreach (var item in Playlist)
                {
                    var music = GetMusicByItem(item);
                    if (music == null)
                        continue;
                    total += music.Length;
                }
                return total;
            }
        }

        public Item ActualSongData
        {
            get
            {
                var now = TimestampSinceStarted;
                if (now > TotalPlayListLength)
                    return null;

                // FIX: SortedDictionary ya viene ordenado — Reverse() es seguro
                var line = GetPlayLine();
                foreach (var entry in line.Reverse())
                {
                    if (entry.Key <= now)
                        return entry.Value;
                }

                return null;
            }
        }

        public int ActualSongTimePassed
        {
            get
            {
                // FIX: calcula ActualSongData una sola vez (antes se recalculaba por propiedad interna)
                var current = ActualSongData;
                if (current == null)
                    return 0;

                var line = GetPlayLine();
                foreach (var entry in line)
                {
                    if (entry.Value == current)
                        return TimestampSinceStarted - entry.Key;
                }

                return 0;
            }
        }
    }
}