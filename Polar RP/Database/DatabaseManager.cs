using MySqlConnector;
using System;
using Polar.Core;
using Polar.Database.Interfaces;

namespace Polar.Database
{
    public sealed class DatabaseManager
    {
        private readonly string _connectionStr;

        public DatabaseManager(uint DbPoolMax, uint DbPoolMin, string DbHostname, uint DbPort, string DbUsername, string DbPassword, string DbName)
        {
            MySqlConnectionStringBuilder connectionString = new MySqlConnectionStringBuilder
            {
                ConnectionTimeout = 240,
                Database = DbName,
                DefaultCommandTimeout = 240,
                AllowPublicKeyRetrieval = true,
                //Logging = false,
                MaximumPoolSize = DbPoolMax,
                MinimumPoolSize = DbPoolMin,
                Password = DbPassword,
                Pooling = true,
                Port = DbPort,
                Server = DbHostname,
                UserID = DbUsername,
                AllowZeroDateTime = true,
                ConvertZeroDateTime = true,
                SslMode = MySqlSslMode.None
            };
            this._connectionStr = connectionString.ToString();
        }

        public bool IsConnected()
        {
            try
            {

                MySqlConnection con = new MySqlConnection(this._connectionStr);
                con.Open();
                MySqlCommand CMD = con.CreateCommand();
                CMD.CommandText = "SELECT 1+1";
                CMD.ExecuteNonQuery();

                CMD.Dispose();
                con.Close();
            }
            catch (MySqlException)
            {
                return false;
            }

            return true;
        }

        public IQueryAdapter GetQueryReactor()
        {
            try
            {
                IDatabaseClient DbConnection = new DatabaseConnection(this._connectionStr);

                DbConnection.connect();

                return DbConnection.GetQueryReactor();
            }
            catch (Exception e)
            {
                Logging.LogException(e.ToString());
                return null;
            }
        }
    }
}