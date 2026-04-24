using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Rooms.Instance
{
    public class FilterComponent
    {
        private Room _instance;

        public FilterComponent(Room instance)
        {
            // FIX: lanzar excepción en lugar de dejar el objeto en estado inválido
            this._instance = instance ?? throw new ArgumentNullException(nameof(instance));
        }

        public bool AddFilter(string word)
        {
            // FIX: WordFilterList es HashSet → Contains es O(1)
            if (this._instance.WordFilterList.Contains(word))
                return false;

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("INSERT INTO `room_filter` (`room_id`,`word`) VALUES(@rid,@word);");
                dbClient.AddParameter("rid", this._instance.Id);
                dbClient.AddParameter("word", word);
                dbClient.RunQuery();
            }

            this._instance.WordFilterList.Add(word);
            return true;
        }

        public bool RemoveFilter(string word)
        {
            // FIX: HashSet.Remove devuelve bool directamente — sin Contains previo
            if (!this._instance.WordFilterList.Remove(word))
                return false;

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("DELETE FROM `room_filter` WHERE `room_id` = @rid AND `word` = @word;");
                dbClient.AddParameter("rid", this._instance.Id);
                dbClient.AddParameter("word", word);
                dbClient.RunQuery();
            }

            return true;
        }

        public string CheckMessage(string message)
        {
            // FIX: ToLower() una sola vez fuera del loop (antes: N llamadas por mensaje)
            string messageLower = message.ToLower();

            foreach (string filter in this._instance.WordFilterList)
            {
                // FIX: eliminado "|| message == Filter" redundante (subconjunto del Contains)
                // FIX: eliminado "else continue" que no hacía nada
                if (messageLower.Contains(filter))
                    message = Regex.Replace(message, filter, "Bobba", RegexOptions.IgnoreCase);
            }

            return message.TrimEnd(' ');
        }

        public void Cleanup()
        {
            this._instance = null;
        }
    }
}