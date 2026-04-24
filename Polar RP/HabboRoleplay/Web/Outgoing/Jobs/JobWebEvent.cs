using ConnectionManager;
using Polar.Net;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms;
using System.IO;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Incoming.Groups;
using Polar.Communication.Packets.Outgoing;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing.Groups;
using Polar.Communication.Packets.Outgoing.Catalog;
using Polar.Communication.Packets.Outgoing.Messenger;
using System.Collections.Generic;
using Polar.HabboHotel.Groups;
using System.Text.RegularExpressions;

namespace Polar.HabboHotel.Roleplay.Web.Outgoing.Misc
{
    /// <summary>
    /// JobWebEvent class.
    /// </summary>
    class JobWebEvent : IWebEvent
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
            /*
            if (!Client.GetRoleplay().UsingAtm)
            {
                Client.SendNotification("Buen intento, tratando de injectar el systema, ve a un ATM!");
                return;
            }
            */

            string Action = (Data.Contains(',') ? Data.Split(',')[0] : Data);

            //Generamos la Sala
            if (!RoleplayManager.GenerateRoom(Client.GetRoomUser().RoomId, out Room Room))
                return;

            if(Room.Group == null)
            {
                Client.SendNotification("Esta zona no es ningún punto de solicitud de Trabajo.");
                return;
            }
            List<GroupMember> Administrators = Room.Group.Members.Values.Where(x => x.IsAdmin).OrderBy(x => x.UserId).ToList();
            switch (Action)
            {

                #region Open
                case "open":
                    {                     
                        string Founder = "Ninguno";
                       
                        if (Administrators.Count > 0)
                            Founder = PolarEnvironment.GetUsernameById(Administrators[0].UserId);
                        
                        int Pay = GroupManager.GetGroupRank(Room.Group.Id, 1).Pay;

                        string SendData = "";
                        SendData += Room.Group.Name + ",";
                        SendData += Room.Group.Description + ",";
                        SendData += "$" + Pay.ToString() + " cada 10 minutos,";
                        SendData += Room.Group.Badge + ",";
                        SendData += Founder;
                        Socket.SendWS( "compose_job:open:" + SendData);
                    }
                    break;
                #endregion

                #region Close
                case "close":
                    {
                        Client.GetRoleplay().JobRequest = 0;
                        break;
                    }
                #endregion

                #region Send
                case "send":
                    {
                        #region Vars
                        string[] ReceivedData = Data.Split(',');
                        int time;
                        string textwork = Convert.ToString(ReceivedData[1]);
                        string zone = Convert.ToString(ReceivedData[3]);

                        // FILTER
                        textwork = Regex.Replace(textwork, "<(.|\\n)*?>", string.Empty);
                        zone = Regex.Replace(zone, "<(.|\\n)*?>", string.Empty);
                        #endregion

                        #region Conditions
                        if (!int.TryParse(ReceivedData[2], out time))
                        {
                            Socket.SendWS( "compose_job:error:Debes ingresar un número (entero) de tus horas libres.");
                            return;
                        }

                        if (time < 0)
                        {
                            Socket.SendWS( "compose_job:error:Debes ingresar un número (entero y positivo) de tus horas libres.");
                            return;
                        }

                        if (textwork.Length < 10)
                        {
                            Socket.SendWS( "compose_job:error:Debes ingresar un mínimo de 10 caracteres en el campo de texto.");
                            return;
                        }

                        if (zone == "" || zone == null)
                        {
                            Socket.SendWS( "compose_job:error:Debes seleccionar tu País de residencia.");
                            return;
                        }

                        if (Client.GetRoleplay().TryGetCooldown("jobrequest"))
                            return;
                        #endregion

                        #region Execute

                        RoleplayManager.Shout(Client, "*Envía una solicitud de empleo con el mensaje '" + textwork + "' es de " + zone + " y tiene " + time + " horas libres al día*", 5);
                        Client.GetRoleplay().JobRequest = 1;
                        Socket.SendWS( "compose_job:close");
                        Client.GetRoleplay().CooldownManager.CreateCooldown("jobrequest", 1000, 30);

                        #region Execute Packet
                        Room.Group.AddNewMember(Client.GetHabbo().Id);

                        if (Room.Group.GroupType == GroupType.LOCKED)
                        {
                            List<GameClient> GroupAdmins = (from Session in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList() where Client != null && Client.GetHabbo() != null && Room.Group.IsAdmin(Client.GetHabbo().Id) select Client).ToList();
                            foreach (GameClient Session in GroupAdmins)
                            {
                                Session.SendMessage(new GroupMembershipRequestedComposer(Room.Group.Id, Session.GetHabbo(), 3));
                            }

                            Client.SendMessage(new GroupInfoComposer(Room.Group, Client));
                        }
                        else
                        {
                            Client.SendMessage(new GroupFurniConfigComposer(PolarEnvironment.GetGame().GetGroupManager().GetGroupsForUser(Client.GetHabbo().Id)));
                            Client.SendMessage(new GroupInfoComposer(Room.Group, Client));

                            if (Client.GetHabbo().CurrentRoom != null)
                                Client.GetHabbo().CurrentRoom.SendMessage(new RefreshFavouriteGroupComposer(Client.GetHabbo().Id));
                            else
                                Client.SendMessage(new RefreshFavouriteGroupComposer(Client.GetHabbo().Id));

                            if (Room.Group.HasChat)
                            {
                                var Clientx = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Client.GetHabbo().Id);
                                if (Clientx != null)
                                {
                                    //MessengerBuddy newgroup = new MessengerBuddy(-Room.Group.Id, Room.Group.Name, Room.Group.Badge, string.Empty, 0, false, true, false);
                                    Clientx.SendMessage(new FriendListUpdateComposer(Room.Group, 0));
                                }
                            }
                        }
                        Client.GetRoleplay().JobRequest = 0;
                        #endregion

                        #endregion
                    }
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
