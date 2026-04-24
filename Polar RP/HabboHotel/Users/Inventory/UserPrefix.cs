using System;
using System.Data;
using Polar.Core;
using Polar.Database.Interfaces;

namespace Polar.HabboHotel.Users
{
    public class UserPrefix
    {
        private int _id;
        private readonly int _userId;
        private string _text;
        private string _color;
        private string _icon;
        private string _effect;
        private bool _active;
        private bool _needsInsert;
        private bool _needsUpdate;
        private bool _needsDelete;

        // Constructor desde DB
        public UserPrefix(DataRow row)
        {
            _id = Convert.ToInt32(row["id"]);
            _userId = Convert.ToInt32(row["user_id"]);
            _text = row["text"]?.ToString() ?? "";
            _color = row["color"]?.ToString() ?? "";
            _icon = row["icon"]?.ToString() ?? "";
            _effect = row["effect"]?.ToString() ?? "";
            _active = Convert.ToInt32(row["active"]) == 1;
            _needsInsert = false;
            _needsUpdate = false;
            _needsDelete = false;
        }

        // Constructor para nuevo prefix
        public UserPrefix(int userId, string text, string color, string icon, string effect)
        {
            _id = 0;
            _userId = userId;
            _text = text;
            _color = color;
            _icon = icon ?? "";
            _effect = effect ?? "";
            _active = false;
            _needsInsert = true;
            _needsUpdate = false;
            _needsDelete = false;
        }

        public void Save()
        {
            try
            {
                if (_needsInsert)
                {
                    using IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
                    dbClient.SetQuery("INSERT INTO user_prefixes (user_id, text, color, icon, effect, active) VALUES (@userId, @text, @color, @icon, @effect, @active)");
                    dbClient.AddParameter("@userId", _userId);
                    dbClient.AddParameter("@text", _text);
                    dbClient.AddParameter("@color", _color);
                    dbClient.AddParameter("@icon", _icon);
                    dbClient.AddParameter("@effect", _effect);
                    dbClient.AddParameter("@active", _active ? 1 : 0);
                    _id = (int)dbClient.InsertQuery();
                    _needsInsert = false;
                }
                else if (_needsDelete)
                {
                    using IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
                    dbClient.SetQuery("DELETE FROM user_prefixes WHERE id = @id AND user_id = @userId");
                    dbClient.AddParameter("@id", _id);
                    dbClient.AddParameter("@userId", _userId);
                    dbClient.RunQuery();
                    _needsDelete = false;
                }
                else if (_needsUpdate)
                {
                    using IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
                    dbClient.SetQuery("UPDATE user_prefixes SET text = @text, color = @color, icon = @icon, effect = @effect, active = @active WHERE id = @id AND user_id = @userId");
                    dbClient.AddParameter("@text", _text);
                    dbClient.AddParameter("@color", _color);
                    dbClient.AddParameter("@icon", _icon);
                    dbClient.AddParameter("@effect", _effect);
                    dbClient.AddParameter("@active", _active ? 1 : 0);
                    dbClient.AddParameter("@id", _id);
                    dbClient.AddParameter("@userId", _userId);
                    dbClient.RunQuery();
                    _needsUpdate = false;
                }
            }
            catch (Exception ex)
            {
                Logging.LogException($"[UserPrefix] Save: {ex}");
            }
        }

        // ── Getters / Setters ─────────────────────────────────────────────────

        public int GetId() => _id;
        public int GetUserId() => _userId;
        public string GetText() => _text;
        public string GetColor() => _color;
        public string GetIcon() => _icon;
        public string GetEffect() => _effect;
        public bool IsActive() => _active;

        public void SetText(string text) { _text = text; _needsUpdate = true; }
        public void SetColor(string color) { _color = color; _needsUpdate = true; }
        public void SetIcon(string icon) { _icon = icon ?? ""; _needsUpdate = true; }
        public void SetEffect(string effect) { _effect = effect ?? ""; _needsUpdate = true; }

        public void SetActive(bool active) { _active = active; _needsUpdate = true; }
        public void SetNeedsUpdate(bool value) { _needsUpdate = value; }
        public void SetNeedsInsert(bool value) { _needsInsert = value; }
        public void SetNeedsDelete(bool value) { _needsDelete = value; }
    }
}