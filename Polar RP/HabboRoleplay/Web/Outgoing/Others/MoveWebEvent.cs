using ConnectionManager;
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Polar.Core;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms;
using Polar.HabboRoleplay.RoleplayUsers;
using System.Net.Sockets;


namespace Polar.HabboHotel.Roleplay.Web.Incoming.Others
{
    /// <summary>
    /// ATMWebEvent class.
    /// </summary>
    class MoveWebEvent : IWebEvent
    {
        /// <summary>
        /// Executes socket data.
        /// </summary>
        /// <param name="Client"></param>
        /// <param name="Data"></param>
        /// <param name="Socket"></param>
        public void Execute(GameClient Session, string Data, ConnectionInformation Socket)
        {
            if (!PolarEnvironment.GetGame().GetWebEventManager().SocketReady(Session, true) || !PolarEnvironment.GetGame().GetWebEventManager().SocketReady(Socket))
                return;

            string Action = (Data.Contains(',') ? Data.Split(',')[0] : Data);
            string Action2 = (Data.Contains(',') ? Data.Split(',')[1] : Data);
            int ReceivedData = Convert.ToInt32(Action);
            int ReceivedData2 = Convert.ToInt32(Action2);

            if (Action.Contains('.') || Action2.Contains(".")) {
                ReceivedData = Convert.ToInt32(Action.Contains('.') ? Action.Split('.')[0] : Data);
                ReceivedData2 = Convert.ToInt32(Action2.Contains('.') ? Action2.Split('.')[0] : Data);
            }

            
            Console.WriteLine(ReceivedData + "," + ReceivedData2);
             Room Room = Session.GetHabbo().CurrentRoom;
             if (Room == null)
                 return;

             RoomUser User = Room.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);

             int MoveX = Convert.ToInt32(ReceivedData);
             int MoveY = Convert.ToInt32(ReceivedData2);
          

             User.MoveTo(MoveX, MoveY);
            
        }
    }
}