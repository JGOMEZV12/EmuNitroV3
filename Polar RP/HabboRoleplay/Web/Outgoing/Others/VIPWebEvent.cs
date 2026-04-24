using ConnectionManager;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.Communication.Packets.Outgoing.Users;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Rooms;
using Polar.HabboRoleplay.Misc;
using Polar.Net;

namespace Polar.HabboHotel.Roleplay.Web.Outgoing.Misc
{
    /// <summary>
    /// WeaponsWebEvent class.
    /// </summary>
    class VIPWebEvent : IWebEvent
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

            string Action = (Data.Contains(',') ? Data.Split(',')[0] : Data);
            string User = (Data.Contains(',') ? Data.Split(',')[1] : Data);
            string Day = (Data.Contains(',') ? Data.Split(',')[2] : Data);


            switch (Action)
            {
                #region VIP Shop
                case "vip1":
                    {
                        int Cost = 300;

                        #region Conditions & Vars
                        if (!RoleplayManager.GenerateRoom(Client.GetRoomUser().RoomId, out Room Room))
                            return;

                        if (Client.GetRoleplay().TryGetCooldown("openvip"))
                            return;

                        if (Cost > Client.GetHabbo().Diamonds)
                        {
                            Socket.SendWS( "compose_vip|msg_error|¡No tienes suficientes rubies!");
                            return;
                        }

                        if (Cost > 0)
                        {
                            Client.GetHabbo().Diamonds -= Cost;
                            //session.SendMessage(new HabboActivityPointNotificationComposer(session.GetHabbo().Diamonds, 0, 5));
                            Client.GetHabbo().UpdateDiamondsBalance();
                        }
                        #endregion

                        int num = 31 * 1;

                        Client.GetHabbo().GetClubManager().AddOrExtendSubscription("habbo_vip", num * 24 * 3600, Client);
                        Client.GetHabbo().GetBadgeComponent().GiveBadge("HC1", true, Client);
                        //Client.SendMessage(new UserNameChangeComposer(Client.GetRoomUser().GetRoom().Id, Client.GetRoomUser().VirtualId, "[VIP] " + Client.GetHabbo().Username));
                        Client.GetHabbo().GetPermissions().Init(Client.GetHabbo());
                        Client.SendMessage(new UserChangeComposer(Client.GetRoomUser(), false));
                        Client.SendMessage(new ScrSendUserInfoComposer(Client.GetHabbo()));
                        Client.GetHabbo().GetClubManager().ReloadSubscription(Client);

                        if (Client.GetHabbo().VIPRank < 1)
                        {
                            Client.GetHabbo().VIPRank = 1;
                            Client.GetHabbo().NameColor = "B53F3F";
                            using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                                dbClient.RunQuery("UPDATE `users` SET `rank_vip` = '1', `colour` = 'B53F3F' WHERE `id` = '" + Client.GetHabbo().Id + "'");

                        }

                        Socket.SendWS( "compose_vip|msg_success|¡Haz comprado el VIP de 1 Mes!");
                        Socket.SendWS( "compose_vip|close");
                        Client.GetRoleplay().CooldownManager.CreateCooldown("openvip", 1000, 1);
                        break;
                    }

                case "vip3":
                    {
                        int Cost = 250;
                        #region Conditions & Vars
                        if (!RoleplayManager.GenerateRoom(Client.GetRoomUser().RoomId, out Room Room))
                            return;

                        if (Client.GetRoleplay().TryGetCooldown("openvip"))
                            return;

                        if (Cost > Client.GetHabbo().Diamonds)
                        {
                            Socket.SendWS( "compose_vip|msg_error|¡No tienes suficientes rubies!");
                            return;
                        }

                        if (Cost > 0)
                        {
                            Client.GetHabbo().Diamonds -= Cost;
                            //session.SendMessage(new HabboActivityPointNotificationComposer(session.GetHabbo().Diamonds, 0, 5));
                            Client.GetHabbo().UpdateDiamondsBalance();
                        }
                        #endregion

                        int num = 31 * 3;

                        Client.GetHabbo().GetClubManager().AddOrExtendSubscription("habbo_vip", num * 24 * 3600, Client);
                        Client.GetHabbo().GetBadgeComponent().GiveBadge("HC1", true, Client);
                        //Client.SendMessage(new UserNameChangeComposer(Client.GetRoomUser().GetRoom().Id, Client.GetRoomUser().VirtualId, "[VIP] " + Client.GetHabbo().Username));
                        Client.GetHabbo().GetPermissions().Init(Client.GetHabbo());
                        Client.SendMessage(new UserChangeComposer(Client.GetRoomUser(), false));
                        Client.SendMessage(new ScrSendUserInfoComposer(Client.GetHabbo()));
                        Client.GetHabbo().GetClubManager().ReloadSubscription(Client);

                        if (Client.GetHabbo().VIPRank < 1)
                        {
                            Client.GetHabbo().VIPRank = 1;
                            Client.GetHabbo().NameColor = "B53F3F";
                            using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                                dbClient.RunQuery("UPDATE `users` SET `rank_vip` = '1', `colour` = 'B53F3F' WHERE `id` = '" + Client.GetHabbo().Id + "'");

                        }

                        Socket.SendWS( "compose_vip|msg_success|¡Haz comprado el VIP de 3 Meses!");
                        Socket.SendWS( "compose_vip|close");
                        Client.GetRoleplay().CooldownManager.CreateCooldown("openvip", 1000, 1);
                        break;
                    }
                    #endregion


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
