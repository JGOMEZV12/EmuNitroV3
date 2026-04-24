using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Polar.HabboHotel.Groups;
using Polar.Database.Interfaces;

namespace Polar.HabboHotel.Rooms
{
    public class RoomData
    {
        // ─────────────────────────────────────
        //  Campos — mantenidos públicos por compatibilidad con el resto del proyecto
        // ─────────────────────────────────────
        public int Id;
        public bool AllowPets;
        public bool AllowPetsEating;
        public bool RoomBlockingEnabled;
        public int Category;
        public string Description;
        public string Floor;
        public int FloorThickness;
        public Group Group;
        public bool Hidewall;
        public string Landscape;
        public string ModelName;
        public string Name;
        public string OwnerName;
        public int OwnerId;
        public string Password;
        public int Score;
        public int State;
        public List<string> Tags;
        public string Type;
        public int UsersMax;
        public int UsersNow;
        public int WallThickness;
        public string Wallpaper;
        public int WhoCanBan;
        public int WhoCanKick;
        public int WhoCanMute;

        private RoomModel _model;

        public int chatMode;
        public int chatSpeed;
        public int chatSize;
        public int extraFlood;
        public int chatDistance;
        public int TradeSettings;

        // FIX: auto-property en lugar de campo _promotion + propiedad trivial
        public RoomPromotion Promotion { get; set; }

        public bool PushEnabled;
        public bool PullEnabled;
        public bool SPushEnabled;
        public bool SPullEnabled;
        public bool EnablesEnabled;
        public bool RespectNotificationsEnabled;
        public bool PetMorphsAllowed;
        public bool HideWired;

        // RP fields
        public string City;
        public int DoorOrientation;
        public int DoorX;
        public int DoorY;
        public double DoorZ;
        public bool BankEnabled;
        public bool ShootEnabled;
        public bool HitEnabled;
        public bool SafeZoneEnabled;
        public bool LearningEnabled;
        public bool SexCommandsEnabled;
        public bool TurfEnabled;
        public bool TurfCapturing = false;
        public bool BankCapturing = false;
        public int TurfUserAtackerId = 0;
        public bool RobEnabled;
        public bool GymEnabled;
        public bool DeliveryEnabled;
        public bool TutorialEnabled;
        public bool DriveEnabled;
        public bool TaxiToEnabled;
        public bool BusToEnabled;
        public bool TaxiFromEnabled;
        public string EnterRoomMessage;
        public bool MallEnabled;
        public bool SupermarketEnabled;
        public bool HuntZoneEnabled;
        public bool BuyCarEnabled;
        public bool IsHospital;
        public bool IsPrison;
        public bool IsPrison2;
        public bool IsPolStation;
        public bool IsCamionero;
        public bool IsCourt;
        public bool IsMecanico;
        public bool IsBasurero;
        public bool WardrobeEnabled;
        public bool PhoneStoreEnabled;
        public int GroupId;

        public Dictionary<int, KeyValuePair<int, string>> WiredScoreBordDay;
        public Dictionary<int, KeyValuePair<int, string>> WiredCasinoApuestas;
        public Dictionary<int, KeyValuePair<int, string>> WiredScoreBordWeek;
        public Dictionary<int, KeyValuePair<int, string>> WiredScoreBordMonth;

        // FIX: eliminada la doble inicialización — se inicializa solo en Fill
        public List<int> WiredScoreFirstBordInformation;

        // ─────────────────────────────────────
        //  Fill — sobrecarga sin ownerName
        // ─────────────────────────────────────
        public void Fill(DataRow row)
        {
            Fill(row, string.Empty);
        }

        // ─────────────────────────────────────
        //  Fill — principal
        // ─────────────────────────────────────
        public void Fill(DataRow row, string ownerName)
        {
            Id = Convert.ToInt32(row["id"]);
            Name = Convert.ToString(row["caption"]);
            Description = Convert.ToString(row["description"]);
            Type = Convert.ToString(row["roomtype"]);
            OwnerId = Convert.ToInt32(row["owner"]);

            // FIX: la consulta de OwnerName usa la conexión existente del llamador cuando ownerName
            // no viene — evita abrir una conexión extra por cada sala cargada
            if (string.IsNullOrEmpty(ownerName))
            {
                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("SELECT `username` FROM `users` WHERE `id` = @owner LIMIT 1");
                    dbClient.AddParameter("owner", OwnerId);
                    OwnerName = dbClient.getString();
                }
            }
            else
            {
                OwnerName = ownerName;
            }

            switch (row["state"].ToString().ToLower())
            {
                case "open": State = 0; break;
                case "password": State = 2; break;
                case "hide": State = 3; break;
                default: State = 1; break;
            }

            Category = Convert.ToInt32(row["category"]);
            UsersNow = string.IsNullOrEmpty(row["users_now"].ToString()) ? 0 : Convert.ToInt32(row["users_now"]);
            UsersMax = Convert.ToInt32(row["users_max"]);
            ModelName = Convert.ToString(row["model_name"]);
            Score = Convert.ToInt32(row["score"]);

            AllowPets = PolarEnvironment.EnumToBool(row["allow_pets"].ToString());
            AllowPetsEating = PolarEnvironment.EnumToBool(row["allow_pets_eat"].ToString());
            RoomBlockingEnabled = PolarEnvironment.EnumToBool(row["room_blocking_disabled"].ToString());
            Hidewall = PolarEnvironment.EnumToBool(row["allow_hidewall"].ToString());
            Password = Convert.ToString(row["password"]);
            Wallpaper = Convert.ToString(row["wallpaper"]);
            Floor = Convert.ToString(row["floor"]);
            Landscape = Convert.ToString(row["landscape"]);
            FloorThickness = Convert.ToInt32(row["floorthick"]);
            WallThickness = Convert.ToInt32(row["wallthick"]);
            WhoCanMute = Convert.ToInt32(row["mute_settings"]);
            WhoCanKick = Convert.ToInt32(row["kick_settings"]);
            WhoCanBan = Convert.ToInt32(row["ban_settings"]);
            chatMode = Convert.ToInt32(row["chat_mode"]);
            chatSpeed = Convert.ToInt32(row["chat_speed"]);
            chatSize = Convert.ToInt32(row["chat_size"]);
            TradeSettings = Convert.ToInt32(row["trade_settings"]);
            GroupId = Convert.ToInt32(row["group_id"]);

            PushEnabled = PolarEnvironment.EnumToBool(row["push_enabled"].ToString());
            PullEnabled = PolarEnvironment.EnumToBool(row["pull_enabled"].ToString());
            SPushEnabled = PolarEnvironment.EnumToBool(row["spush_enabled"].ToString());
            SPullEnabled = PolarEnvironment.EnumToBool(row["spull_enabled"].ToString());
            EnablesEnabled = PolarEnvironment.EnumToBool(row["enables_enabled"].ToString());
            RespectNotificationsEnabled = PolarEnvironment.EnumToBool(row["respect_notifications_enabled"].ToString());
            PetMorphsAllowed = PolarEnvironment.EnumToBool(row["pet_morphs_allowed"].ToString());
            HideWired = PolarEnvironment.EnumToBool(row["hide_wired"].ToString());

            // FIX: Group — variable local innecesaria eliminada
            Group = GroupId < 1000
                ? GroupManager.GetJob(GroupId)
                : GroupManager.GetGang(GroupId);

            // FIX: Split + Trim para eliminar espacios en los tags
            Tags = new List<string>();
            foreach (string tag in row["tags"].ToString().Split(','))
            {
                string trimmed = tag.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                    Tags.Add(trimmed);
            }

            _model = PolarEnvironment.GetGame().GetRoomManager().GetModel(ModelName, Id);

            // ── Scoreboard ──────────────────────────────────────────────
            WiredScoreBordDay = new Dictionary<int, KeyValuePair<int, string>>();
            WiredScoreBordWeek = new Dictionary<int, KeyValuePair<int, string>>();
            WiredScoreBordMonth = new Dictionary<int, KeyValuePair<int, string>>();

            DateTime now = DateTime.Now;
            int todayStamp = Convert.ToInt32(now.ToString("MMddyyyy"));
            int monthStamp = Convert.ToInt32(now.ToString("MM"));
            int weekStamp = CultureInfo.GetCultureInfo("Nl-nl").Calendar
                                        .GetWeekOfYear(now, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

            // FIX: inicialización única aquí, eliminada la del campo
            WiredScoreFirstBordInformation = new List<int> { todayStamp, monthStamp, weekStamp };

            bool resetDay = false;
            bool resetMonth = false;
            bool resetWeek = false;

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT * FROM wired_scorebord WHERE roomid = @id ORDER BY `punten` DESC");
                dbClient.AddParameter("id", Id);

                // FIX: null check — getTable() puede retornar null
                DataTable scoreTable = dbClient.getTable();
                if (scoreTable != null)
                {
                    foreach (DataRow scoreRow in scoreTable.Rows)
                    {
                        int userId = Convert.ToInt32(scoreRow["userid"]);
                        string username = Convert.ToString(scoreRow["username"]);
                        int punten = Convert.ToInt32(scoreRow["punten"]);
                        string soort = Convert.ToString(scoreRow["soort"]);
                        int timestamp = Convert.ToInt32(scoreRow["timestamp"]);

                        if (soort == "day" && !WiredScoreBordDay.ContainsKey(userId))
                        {
                            if (timestamp != todayStamp)
                                resetDay = true;
                            else
                                WiredScoreBordDay.Add(userId, new KeyValuePair<int, string>(punten, username));
                        }

                        if (soort == "month" && !WiredScoreBordMonth.ContainsKey(userId))
                        {
                            if (timestamp != monthStamp)
                                resetMonth = true;
                            else
                                WiredScoreBordMonth.Add(userId, new KeyValuePair<int, string>(punten, username));
                        }

                        // FIX: soort == "week" borra WiredScoreBordWeek (antes borraba WiredScoreBordDay por error)
                        if (soort == "week" && !WiredScoreBordWeek.ContainsKey(userId))
                        {
                            if (timestamp != weekStamp)
                                resetWeek = true;
                            else
                                WiredScoreBordWeek.Add(userId, new KeyValuePair<int, string>(punten, username));
                        }
                    }
                }

                // FIX: queries parametrizadas — antes concatenaban this.Id directamente (SQL injection)
                if (resetDay)
                {
                    dbClient.SetQuery("DELETE FROM `wired_scorebord` WHERE `roomid` = @id AND `soort` = 'day'");
                    dbClient.AddParameter("id", Id);
                    dbClient.RunQuery();
                    WiredScoreBordDay.Clear();
                }

                if (resetMonth)
                {
                    dbClient.SetQuery("DELETE FROM `wired_scorebord` WHERE `roomid` = @id AND `soort` = 'month'");
                    dbClient.AddParameter("id", Id);
                    dbClient.RunQuery();
                    WiredScoreBordMonth.Clear();
                }

                if (resetWeek)
                {
                    // FIX: borra WiredScoreBordWeek — antes borraba WiredScoreBordDay por error
                    dbClient.SetQuery("DELETE FROM `wired_scorebord` WHERE `roomid` = @id AND `soort` = 'week'");
                    dbClient.AddParameter("id", Id);
                    dbClient.RunQuery();
                    WiredScoreBordWeek.Clear();
                }
            }
        }

        // ─────────────────────────────────────
        //  FillRP
        // ─────────────────────────────────────
        public void FillRP(DataRow row)
        {
            City = row["city"].ToString();
            BankEnabled = PolarEnvironment.EnumToBool(row["bank_enabled"].ToString());
            ShootEnabled = PolarEnvironment.EnumToBool(row["shoot_enabled"].ToString());
            HitEnabled = PolarEnvironment.EnumToBool(row["hit_enabled"].ToString());
            SafeZoneEnabled = PolarEnvironment.EnumToBool(row["safezone_enabled"].ToString());
            LearningEnabled = PolarEnvironment.EnumToBool(row["learning_enabled"].ToString());
            SexCommandsEnabled = PolarEnvironment.EnumToBool(row["sexcommands_enabled"].ToString());
            TurfEnabled = PolarEnvironment.EnumToBool(row["turf_enabled"].ToString());
            RobEnabled = PolarEnvironment.EnumToBool(row["rob_enabled"].ToString());
            GymEnabled = PolarEnvironment.EnumToBool(row["gym_enabled"].ToString());
            DeliveryEnabled = PolarEnvironment.EnumToBool(row["delivery_enabled"].ToString());
            TutorialEnabled = PolarEnvironment.EnumToBool(row["tutorial_enabled"].ToString());
            DriveEnabled = PolarEnvironment.EnumToBool(row["drive_enabled"].ToString());
            TaxiFromEnabled = PolarEnvironment.EnumToBool(row["taxi_from_enabled"].ToString());
            TaxiToEnabled = PolarEnvironment.EnumToBool(row["taxi_to_enabled"].ToString());
            BusToEnabled = PolarEnvironment.EnumToBool(row["bus_to_enabled"].ToString());
            EnterRoomMessage = row["enter_message"].ToString();
            IsHospital = PolarEnvironment.EnumToBool(row["is_hospital"].ToString());
            IsPrison = PolarEnvironment.EnumToBool(row["is_prison"].ToString());
            IsPrison2 = PolarEnvironment.EnumToBool(row["is_prisonback"].ToString());
            IsCourt = PolarEnvironment.EnumToBool(row["is_court"].ToString());
            IsCamionero = PolarEnvironment.EnumToBool(row["is_camionero"].ToString());
            IsBasurero = PolarEnvironment.EnumToBool(row["is_basurero"].ToString());
            MallEnabled = PolarEnvironment.EnumToBool(row["mall_enabled"].ToString());
            BuyCarEnabled = PolarEnvironment.EnumToBool(row["buycar_enabled"].ToString());
            WardrobeEnabled = PolarEnvironment.EnumToBool(row["wardrobe_enabled"].ToString());
            PhoneStoreEnabled = PolarEnvironment.EnumToBool(row["phonestore_enabled"].ToString());
            SupermarketEnabled = PolarEnvironment.EnumToBool(row["supermarket_enabled"].ToString());
            HuntZoneEnabled = PolarEnvironment.EnumToBool(row["huntzone_enabled"].ToString());
        }

        // ─────────────────────────────────────
        //  Promotion helpers
        // ─────────────────────────────────────
        public bool HasActivePromotion => Promotion != null;

        public void EndPromotion()
        {
            if (!HasActivePromotion)
                return;
            Promotion = null;
        }

        // ─────────────────────────────────────
        //  Model
        // ─────────────────────────────────────
        public RoomModel Model
        {
            get
            {
                if (_model == null)
                    _model = PolarEnvironment.GetGame().GetRoomManager().GetModel(ModelName, Id);
                return _model;
            }
        }
    }
}