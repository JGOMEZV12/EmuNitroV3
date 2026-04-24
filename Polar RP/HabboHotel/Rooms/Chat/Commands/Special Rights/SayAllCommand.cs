using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.HabboHotel.Rooms.Chat.Commands.SpecialRights
{
    class SayAllCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_say_all"; }
        }

        public string Parameters
        {
            get { return "%message%"; }
        }

        public string Description
        {
            get { return "Forces all users in the room to say the message."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            RoomUser ThisUser = Session.GetRoomUser();
            if (ThisUser == null)
                return;

            if (Params.Length == 1)
                Session.SendWhisper("You must enter a username and the message you wish to force them to say.", 1);
            else
            {
                string Message = CommandManager.MergeParams(Params, 1);
                
                foreach (var User in Room.GetRoomUserManager().GetRoomUsers())
                {
                    if (User == null)
                        continue;

                    if (User.GetClient() == null)
                        continue;

                    if (User == ThisUser)
                        continue;

                    User.SendNameColourPacket();
                    Room.SendMessage(new ChatComposer(User.VirtualId, Message, 0, User.LastBubble, string.Empty));
                    User.SendNamePacket();
                }
            }
        }
    }
}
