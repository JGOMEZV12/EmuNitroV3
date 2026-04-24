using ConnectionManager;
using Polar.Net;
using Polar.HabboHotel.GameClients;
using System.IO;
using Polar.HabboHotel.Cache;
using Polar.HabboRoleplay.Web.Outgoing.Statistics;
using Polar.Utilities;
using Polar.HabboRoleplay.Weapons;

namespace Polar.HabboHotel.Roleplay.Web.Incoming.General
{
    /// <summary>
    /// RetrieveStatsWebEvent class.
    /// </summary>
    class LiveFeedComposer : IWebEvent
    {
        /// <summary>
        /// Executes socket data.
        /// </summary>
        /// <param name="Client"></param>
        /// <param name="Data"></param>
        /// <param name="Socket"></param>
        public void Execute(GameClient Client, string Data, ConnectionInformation Socket)
        {
            if (!PolarEnvironment.GetGame().GetWebEventManager().SocketReady(Client, true) || !PolarEnvironment.GetGame().GetWebEventManager().SocketReady(Socket))
                return;

            string Action = (Data.Contains('|') ? Data.Split('|')[0] : Data);

            #region OLD
            /*
            if (Client != null)
            {
                string CachedDataString = GetUserComponent.ReturnUserStatistics(Client);

                if (String.IsNullOrEmpty(CachedDataString))
                    return;

                Socket.SendWS( "compose_wstest|" + CachedDataString);
            }
            else
            {
                using (UserCache CachedClient = PolarEnvironment.GetGame().GetCacheManager().GenerateUser(Convert.ToInt32(Socket.ConnectionInfo.Path.Trim().Split('/')[1])))
                {
                    if (CachedClient == null)
                        return;

                    string CachedDatString2 = GetUserComponent.ReturnUserStatistics(CachedClient);

                    if (String.IsNullOrEmpty(CachedDatString2))
                        return;

                    Socket.SendWS( "compose_wstest|" + GetUserComponent.ReturnUserStatistics(CachedClient));
                }
            }
            */
            #endregion

            switch (Action)
            {
                case "alert":
                    {
                        string[] ReceivedData = Data.Split('|');

                        if (ReceivedData.Count() == 4)
                        {
                            string User1 = ReceivedData[1];
                            string User2 = ReceivedData[2];
                            string ActionText = ReceivedData[3];

                            string SendData = User1 + "|";
                            SendData += User2 + "|";
                            SendData += ActionText + "|";

                            Socket.SendWS( "compose_combatlog|alert|" + SendData);
                        }
                    }
                break;
                case "sound":
                    {
                        string[] ReceivedData = Data.Split('|');
                        string Sound = "";
                        switch (ReceivedData[1])
                        {
                            case "punch":
                                {
                                    var Random = new CryptoRandom();
                                    int Chance = Random.Next(1, 101);

                                    if(Chance <= 33)
                                        Sound = "/melee/punch_01";
                                    if(Chance > 33 && Chance <= 66)
                                        Sound = "/melee/punch_02";
                                    else
                                        Sound = "/melee/punch_03";
                                }
                                break;
                            case "dead":
                                {
                                    Sound = "/human/dead";
                                }
                                break;
                            case "shoot":
                                {
                                    if (ReceivedData[2] == "9mm")
                                        Sound = "/weapons/shot/shot_weak";
                                    else if (ReceivedData[2] == "9mm-silenciada")
                                        Sound = "/weapons/shot/9mm-s";
                                    else if (ReceivedData[2] == "escopeta")
                                        Sound = "/weapons/shot/shotgun_02";
                                    else if (ReceivedData[2] == "desert-eagle")
                                        Sound = "/weapons/shot/desert-eagle";
                                    else if (ReceivedData[2] == "mp5")
                                        Sound = "/weapons/shot/mp5";
                                    else if (ReceivedData[2] == "ak47")
                                        Sound = "/weapons/shot/ak47";
                                    else if (ReceivedData[2] == "m4")
                                        Sound = "/weapons/shot/shot_burst_01";
                                    else if (ReceivedData[2] == "rifle")
                                    {
                                        var Random = new CryptoRandom();
                                        int Chance = Random.Next(1, 101);
                                        if (Chance <= 50)
                                            Sound = "/weapons/shot/rifle1";
                                        else
                                            Sound = "/weapons/shot/rifle2";
                                    }
                                    else if (ReceivedData[2] == "escopeta-de-combate")
                                        Sound = "/weapons/shot/shotgun_01";
                                    else
                                        Sound = "/weapons/shot/shot_weak";
                                }
                                break;
                            case "reload":
                                {
                                    if (ReceivedData[2] == "9mm")
                                        Sound = "/weapons/reload/9mm";
                                    else if (ReceivedData[2] == "9mm-silenciada")
                                        Sound = "/weapons/reload/9mm";
                                    else if (ReceivedData[2] == "escopeta")
                                        Sound = "/weapons/reload/escopeta";
                                    else if (ReceivedData[2] == "desert-eagle")
                                        Sound = "/weapons/reload/9mm";
                                    else if (ReceivedData[2] == "mp5")
                                        Sound = "/weapons/reload/reload";
                                    else if (ReceivedData[2] == "ak47")
                                        Sound = "/weapons/reload/reload_ak47";
                                    else if (ReceivedData[2] == "m4")
                                        Sound = "/weapons/reload/reload";
                                    else if (ReceivedData[2] == "rifle")
                                        Sound = "/weapons/reload/reload_ak47";
                                    else if (ReceivedData[2] == "escopeta-de-combate")
                                        Sound = "/weapons/reload/reload_gunshot";
                                    else
                                        Sound = "/weapons/reload/reload_short";
                                }
                                break;
                            case "bullet":
                                {
                                    Sound = "/weapons/shot/bullet";
                                }
                                break;
                            case "paralizer":
                                {
                                    Sound = "/weapons/shot/paralizer";
                                }
                                break;
                            case "unequip":
                                {
                                    Sound = "/weapons/unequip";
                                }
                                break;
                            default:
                                break;
                        }

                        Socket.SendWS( "compose_live|sound|" + Sound);
                    }
                break;
                default:
                    break;
            }
        }

        // ── Helper: envía texto como frame WebSocket usando ConnectionInformation
        private static void SendWS(ConnectionInformation socket, string message)
        {
            if (socket == null || string.IsNullOrEmpty(message)) return;
            socket.SendData(System.Text.Encoding.UTF8.GetBytes(message));
        }

    }
}
