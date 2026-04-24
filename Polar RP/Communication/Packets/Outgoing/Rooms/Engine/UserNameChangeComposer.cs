using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Polar.Communication.Packets.Outgoing.Rooms.Engine
{
    internal class UserNameChangeComposer : ServerPacket
    {
        public static string PREFIX_FORMAT = "[<font color=\"%color%\">%prefix%</font>] ";
        public UserNameChangeComposer(Habbo habbo, bool includePrefix)
            : base(ServerPacketHeader.UserNameChangeMessageComposer)
        {
            base.WriteInteger(habbo.CurrentRoomId);
            base.WriteInteger(habbo.Id);
            base.WriteString((includePrefix ? PREFIX_FORMAT.Replace("%color%", PolarEnvironment.GetGame().GetPermissionManager().GetPrefixColorForPlayer(habbo)).Replace("%prefix%", PolarEnvironment.GetGame().GetPermissionManager().GetPrefixForPlayer(habbo)) : "") + habbo.Username);
        }

        public UserNameChangeComposer(int RoomId, int VirtualId, string Username)
            : base(ServerPacketHeader.UserNameChangeMessageComposer)
        {
            base.WriteInteger(RoomId);
            base.WriteInteger(VirtualId);
            base.WriteString(Username);
        }

    }
}