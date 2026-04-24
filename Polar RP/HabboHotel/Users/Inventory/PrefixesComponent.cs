using System;
using System.Collections.Generic;
using System.Data;
using Polar.Core;
using Polar.HabboHotel.Users;
using Polar.Database.Interfaces;

namespace Polar.HabboHotel.Users.Inventory
{
    public class PrefixesComponent
    {
        private readonly List<UserPrefix> _prefixes = new List<UserPrefix>();
        private readonly Habbo _habbo;

        public PrefixesComponent(Habbo habbo)
        {
            _habbo = habbo;
            LoadPrefixes();
        }

        private void LoadPrefixes()
        {
            try
            {
                using IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
                dbClient.SetQuery("SELECT * FROM user_prefixes WHERE user_id = @userId");
                dbClient.AddParameter("@userId", _habbo.Id);

                DataTable dt = dbClient.getTable();
                if (dt == null) return;

                lock (_prefixes)
                {
                    foreach (DataRow row in dt.Rows)
                        _prefixes.Add(new UserPrefix(row));
                }
            }
            catch (Exception ex)
            {
                Logging.LogException($"[PrefixesComponent] LoadPrefixes: {ex}");
            }
        }

        public List<UserPrefix> GetPrefixes()
        {
            lock (_prefixes)
                return new List<UserPrefix>(_prefixes);
        }

        public UserPrefix GetActivePrefix()
        {
            lock (_prefixes)
            {
                foreach (UserPrefix prefix in _prefixes)
                    if (prefix.IsActive()) return prefix;
            }
            return null;
        }

        public UserPrefix GetPrefix(int id)
        {
            lock (_prefixes)
            {
                foreach (UserPrefix prefix in _prefixes)
                    if (prefix.GetId() == id) return prefix;
            }
            return null;
        }

        public void AddPrefix(UserPrefix prefix)
        {
            lock (_prefixes)
                _prefixes.Add(prefix);
        }

        public void RemovePrefix(UserPrefix prefix)
        {
            lock (_prefixes)
                _prefixes.Remove(prefix);
        }

        public void SetActive(int prefixId)
        {
            lock (_prefixes)
            {
                foreach (UserPrefix prefix in _prefixes)
                {
                    bool shouldBeActive = prefix.GetId() == prefixId;
                    if (prefix.IsActive() != shouldBeActive)
                    {
                        prefix.SetActive(shouldBeActive);
                        prefix.Save();
                    }
                }
            }
        }

        public void DeactivateAll()
        {
            lock (_prefixes)
            {
                foreach (UserPrefix prefix in _prefixes)
                {
                    if (prefix.IsActive())
                    {
                        prefix.SetActive(false);
                        prefix.Save();
                    }
                }
            }
        }

        public void Dispose()
        {
            lock (_prefixes)
                _prefixes.Clear();
        }
    }
}