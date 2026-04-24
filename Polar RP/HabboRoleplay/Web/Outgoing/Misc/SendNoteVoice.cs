using ConnectionManager;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using Polar.HabboHotel.GameClients;
using System.IO;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using Polar.Communication.Packets.Outgoing.Rooms.Avatar;
using Polar.HabboHotel.Rooms.Chat.Logs;
using Polar.Utilities;
using Polar.HabboHotel.Rooms;
using Polar.Communication.Packets.Outgoing.Moderation;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using System.Collections.Generic;
using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.Users;
using Polar.HabboRoleplay.Combat;
using Polar.HabboHotel.Groups;
using System.Text.RegularExpressions;
using System.Collections;
using System.Net.Sockets;
using System.Security.Policy;

namespace Polar.HabboHotel.Roleplay.Web.Outgoing.Default
{
	/// <summary>
	/// SendNoteVoice class.
	/// </summary>
	class SendNoteVoice : IWebEvent
	{
		/// <summary>
		/// Executes socket data.
		/// </summary>
		/// <param name="Client"></param>
		/// <param name="Data"></param>
		/// <param name="Socket"></param>
		/// 
		public void Execute(GameClient Client, string Data, ConnectionInformation Socket)
		{
			if (!PolarEnvironment.GetGame().GetWebEventManager().SocketReady(Socket))
				return;

			string parameters = Data;
            string Action = (Data.Contains('|') ? Data.Split('|')[0] : Data);
            string Data2 = (Data.Contains('|') ? Data.Split('|')[1] : Data);
            //Console.WriteLine(Action + Data2);
            if (parameters.Length < 1)
                return;

            int Colour = 0;

            if (Colour != 0 && !Client.GetHabbo().GetPermissions().HasRight("use_any_bubble"))
                Colour = 0;

            ChatStyle Style = null;
            if (!PolarEnvironment.GetGame().GetChatManager().GetChatStyles().TryGetStyle(Colour, out Style) || (Style.RequiredRight.Length > 0 && !Client.GetHabbo().GetPermissions().HasRight(Style.RequiredRight)))
                Colour = 0;

            Room Room = Client.GetHabbo().CurrentRoom;
			if (Room == null)
				return;
			RoomUser user = Room.GetRoomUserManager().GetRoomUserByHabbo(Client.GetHabbo().Id);
			if (user == null)
				return;

			if (!Client.GetHabbo().GetPermissions().HasRight("mod_tool") && Room.CheckMute(Client))
			{
				Client.SendWhisper("Vaya, actualmente estás silenciado.", 1);
				return;
			}

			if (PolarEnvironment.GetUnixTimestamp() < Client.GetHabbo().FloodTime && Client.GetHabbo().FloodTime != 0)
				return;


			string Frase = StringCharFilter.Escape(parameters);
			if (Frase == "x")
            {
				if (Client.GetRoleplay().LastCommand != "")
					Frase = Client.GetRoleplay().LastCommand.ToString();
			}

            if (Frase.Length > 150)
				Frase = Frase.Substring(0, 150);


			switch (Action)
			{

                #region Open
                case "audio":
					
					{
                        Frase = Data2;
                        Room.SendMessage(new ChatComposer(Client.GetRoomUser().VirtualId, Frase, 0, 2, string.Empty), false);
                    }
					break;
                    #endregion
            }

            if (Client.GetHabbo().TimeMuted > 0)
			{
				Client.SendMessage(new MutedComposer(Client.GetHabbo().TimeMuted));
				return;
			}

			user.LastBubble = Client.GetHabbo().CustomBubbleId == 0 ? Colour : Client.GetHabbo().CustomBubbleId;


			if (parameters.ToLower().Equals("o/"))
			{
				Client.SendMessage(new ActionComposer(Client.GetRoomUser().VirtualId, 1));
				return;
			}


			if (parameters.ToLower().Equals("_b"))
			{
				Client.SendMessage(new ActionComposer(Client.GetRoomUser().VirtualId, 7));
				return;
			}
			Client.GetRoomUser().UnIdle();
        }
	}
}
