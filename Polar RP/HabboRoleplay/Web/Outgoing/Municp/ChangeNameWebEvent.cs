using ConnectionManager;
using Polar.Net;
using Polar.HabboHotel.GameClients;
using System.IO;
using Polar.HabboHotel.Roleplay.Web;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.Users.Effects;
using System.Text.RegularExpressions;
using Polar.HabboHotel.Rooms;
using Polar.Database.Interfaces;
using Polar.Communication.Packets.Outgoing.Users;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using System.Collections.Generic;
using Polar.Communication.Packets.Outgoing.Navigator;
using Polar.Communication.Packets.Outgoing.Rooms.Session;

namespace Polar.HabboRoleplay.Web.Outgoing.Statistics
{
    /// <summary>
    /// TargetWebEvent class.
    /// </summary>
    class ChangeNameWebEvent : IWebEvent
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

            switch (Action)
            {
                #region Open
                case "open":
                    {
                        Client.GetRoleplay().ViewChangeName = true;

                        string SendData = (Client.GetRoleplay().ChangeNameCount <= 0) ? "Cambiar nombre (GRATIS)" : "Cambiar nombre ("+RoleplayManager.ChangeNameCost+" RB)";

                        Socket.SendWS( "compose_changename|open|" + SendData);
                    }
                    break;
                #endregion

                #region Close
                case "close":
                    {
                        Client.GetRoleplay().ViewChangeName = false;
                        Socket.SendWS( "compose_changename|close");
                    }
                    break;
                #endregion

                #region Change Name
                case "changename":
                    {
                        #region Conditions
                        if (Client == null || Client.GetHabbo() == null)
                            return;

                        Room Room = Client.GetHabbo().CurrentRoom;
                        if (Room == null)
                            return;

                        RoomUser User = Room.GetRoomUserManager().GetRoomUserByHabbo(Client.GetHabbo().Username);
                        if (User == null)
                            return;

                        if (Client.GetRoleplay().TryGetCooldown("changename"))
                            return;

                        if (!Client.GetRoleplay().ViewChangeName)
                            return;

                        string[] ReceivedData = Data.Split(',');

                        string NewName = Regex.Replace(ReceivedData[1], "<(.|\\n)*?>", string.Empty); ;

                        if(string.IsNullOrEmpty(NewName) || NewName.Length < 3 || NewName.Length > 18)
                        {
                            Socket.SendWS( "compose_changename|chnamemsg|Tu nuevo nombre debe tener entre 3 y 18 caracteres.");
                            return;
                        }

                        if(!Regex.IsMatch(NewName, @"^[a-zA-Z0-9]+$"))
                        {
                            Socket.SendWS( "compose_changename|chnamemsg|Tu nuevo nombre no puede contener caracteres especiales ni espacios.");
                            return;
                        }

                        if(Client.GetHabbo().Username == NewName)
                        {
                            Socket.SendWS( "compose_changename|chnamemsg|Tu nuevo nombre no puede ser igual al actual.");
                            return;
                        }

                        bool InUse = false;
                        using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                        {
                            dbClient.SetQuery("SELECT COUNT(0) FROM `users` WHERE `username` = @name LIMIT 1");
                            dbClient.AddParameter("name", NewName);
                            InUse = dbClient.getInteger() == 1;
                        }

                        if(InUse)
                        {
                            Socket.SendWS( "compose_changename|chnamemsg|¡Ese nombre ya está en uso!");
                            return;
                        }

                        if (Client.GetRoleplay().ChangeNameCount > 0)
                        {
                            if(Client.GetHabbo().Diamonds < RoleplayManager.ChangeNameCost)
                            {
                                Socket.SendWS( "compose_changename|chnamemsg|No cuentas con los rubies suficientes.");
                                return;
                            }
                        }
                        #endregion

                        #region Execute
                        RoleplayManager.Shout(Client, "*Ha solicitado un cambio de nombre a '" + NewName + "'*", 5);
                        Socket.SendWS( "compose_changename|chnamemsg_green|Solicitando cambio de nombre. Por favor espera...");

                        // Reformateamos nombre a primera letra mayúscula
                        string OldName = Client.GetHabbo().Username;
                        //NewName = char.ToUpper(NewName[0]) + NewName.Substring(1).ToLower();                        

                        if (!PolarEnvironment.GetGame().GetClientManager().UpdateClientUsername(Client, OldName, NewName))
                        {
                            Client.SendNotification("¡Ocurrió un problema mientras se actualizaba tu nombre! Contacta a un Administrador.");
                            return;
                        }

                        Client.GetHabbo().ChangingName = false;

                        Room.GetRoomUserManager().RemoveUserFromRoom(Client, true, false);

                        Client.GetHabbo().ChangeName(NewName);
                        Client.GetHabbo().GetMessenger().OnStatusChanged(true);

                        Client.SendMessage(new UpdateUsernameComposer(NewName));
                        Room.SendMessage(new UserNameChangeComposer(Room.Id, User.VirtualId, NewName));

                        using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                        {
                            dbClient.SetQuery("INSERT INTO `logs_client_namechange` (`user_id`,`new_name`,`old_name`,`timestamp`) VALUES ('" + Client.GetHabbo().Id + "', @name, '" + OldName + "', '" + PolarEnvironment.GetUnixTimestamp() + "')");
                            dbClient.AddParameter("name", NewName);
                            dbClient.RunQuery();
                        }

                        ICollection<RoomData> Rooms = Client.GetHabbo().UsersRooms;
                        foreach (RoomData _Data in Rooms)
                        {
                            if (_Data == null)
                                continue;

                            _Data.OwnerName = NewName;
                        }

                        foreach (Room UserRoom in PolarEnvironment.GetGame().GetRoomManager().GetRooms().ToList())
                        {
                            if (UserRoom == null || UserRoom.RoomData.OwnerName != NewName)
                                continue;

                            UserRoom.OwnerName = NewName;
                            UserRoom.RoomData.OwnerName = NewName;

                            UserRoom.SendMessage(new RoomInfoUpdatedComposer(UserRoom.RoomId));
                        }

                        PolarEnvironment.GetGame().GetAchievementManager().ProgressAchievement(Client, "ACH_Name", 1);

                        Client.SendMessage(new RoomForwardComposer(Room.Id));

                        // RP
                        if (Client.GetRoleplay().ChangeNameCount > 0)
                        {
                            Client.GetHabbo().Diamonds -= RoleplayManager.ChangeNameCost;
                            Client.GetHabbo().UpdateDiamondsBalance();
                        }

                        Client.GetRoleplay().ChangeNameCount++;
                        Client.SendWhisper("¡Tu nombre ha sido actualizado correctamente!");

                        Client.GetRoleplay().UpdateInteractingUserDialogues();
                        Client.GetRoleplay().RefreshStatDialogue();
                        #endregion

                        Client.GetRoleplay().CooldownManager.CreateCooldown("changename", 1000, 15);
                    }
                    break;
                #endregion

                #region Default
                default:
                    break;
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
