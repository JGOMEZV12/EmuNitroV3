using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.Database.Interfaces;

namespace Polar.HabboHotel.Users.Navigator.SavedSearches
{
    public class SavedSearch
    {
        private int _id;
        private string _filter;
        private string _search;

        public SavedSearch(int Id, string Filter, string Search)
        {
            this._id = Id;
            this._filter = Filter;
            this._search = Search;
        }

        public int Id
        {
            get { return this._id; }
            set { this._id = value; }
        }

        public string Filter
        {
            get { return this._filter; }
            set { this._filter = value; }
        }

        public string Search
        {
            get { return this._search; }
            set { this._search = value; }
        }

        public static bool OnSave(GameClients.GameClient Session, string Filter, string SearchCode, bool asSave)
        {
            using (IQueryAdapter db = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                db.SetQuery("INSERT INTO `user_saved_searches` (`id`,`user_id`,`filter`,`search_code`) VALUES (@id, @habboid, @filter, @Code)");
                db.AddParameter("id", null);
                db.AddParameter("habboid", Session.GetHabbo().Id);
                db.AddParameter("filter", Filter);
                db.AddParameter("code", SearchCode);
                db.RunQuery();
            }
            return asSave;
        }

        public static bool OnDelete(GameClients.GameClient Session, bool asDelete, int SearchId)
        {
            using (IQueryAdapter db = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                db.SetQuery("DELETE FROM `user_saved_searches` WHERE `id` = @id LIMIT 1;");
                db.AddParameter("id", SearchId);
                db.RunQuery();
            }
            return asDelete;
        }
    }
}
