using Polar;
using Polar.Core;
using Polar.Net;
using System;

namespace ConnectionManager
{
    public class ConnectionHandling
    {
        public GameSocketManager manager;

        public ConnectionHandling(int port, int maxConnections, int connectionsPerIP)
        {
            this.manager = new GameSocketManager();
            this.manager.init(port, maxConnections, new InitialPacketParser());

            this.manager.connectionEvent += new GameSocketManager.ConnectionEvent(this.ConnectionEvent);
        }

        private void ConnectionEvent(ConnectionInformation connection)
        {
            connection.connectionClose += new ConnectionInformation.ConnectionChange(this.ConnectionChanged);

            PolarEnvironment.GetGame().GetClientManager().CreateAndStartClient(connection.getConnectionID(), connection);
        }

        private void ConnectionChanged(ConnectionInformation information)
        {
            this.CloseConnection(information);

            information.connectionClose -= new ConnectionInformation.ConnectionChange(this.ConnectionChanged);
        }

        public void CloseConnection(ConnectionInformation Connection)
        {
            try
            {
                PolarEnvironment.GetGame().GetClientManager().DisposeConnection(Connection.getConnectionID());

                Connection.Dispose();
            }
            catch (Exception ex)
            {
                Logging.LogException((ex).ToString());
            }
        }


        public void destroy()
        {
            this.manager.Destroy();
        }
    }
}
