using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms;
using Polar.HabboRoleplay.Bots.Types;
using Polar.HabboRoleplay.Bots;
using Polar.HabboHotel.Rooms.AI;
using Polar.HabboHotel.Rooms.AI.Speech;
using Polar.HabboHotel.Items;
using System.Drawing;
using Polar.Communication.Packets.Outgoing.Messenger;
using Polar.Core;
using Polar.HabboHotel.Pathfinding;
using Polar.Utilities;
using Polar.HabboHotel.Users;
using Polar.HabboRoleplay.Bots.Manager;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.Groups;
using Polar.Database.Interfaces;
using System.Collections.Concurrent;


namespace Polar.HabboRoleplay.Bots
{
    public class RoleplayBotResponse
    {
        public string Message;
        public string Response;
        public int Bubble;
        public string Type;

        public RoleplayBotResponse(string Message, string Response, int Bubble, string Type)
        {
            this.Message = Message;
            this.Response = Response;
            this.Bubble = Bubble;
            this.Type = Type;
        }
    }
}