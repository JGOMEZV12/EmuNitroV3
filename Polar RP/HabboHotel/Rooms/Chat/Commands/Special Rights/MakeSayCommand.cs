using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using Polar.HabboHotel.GameClients;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.HabboHotel.Rooms.Chat.Commands.SpecialRights
{
    class MakeSayCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_make_say"; }
        }

        public string Parameters
        {
            get { return "%username% %message%"; }
        }

        public string Description
        {
            get { return "Obliga al usuario especificado a decir el mensaje especificado."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {

            #region Variables
            if (Params.Length == 1)
            {
                Session.SendWhisper("You must enter a username and the message you wish to force them to say.", 1);
                return;
            }

            string Message = CommandManager.MergeParams(Params, 2);
            GameClient TargetSession = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);
            #endregion

            #region Conditions
            if (TargetSession == null)
            {
                Session.SendWhisper("This user could not be found!", 1);
                return;
            }

            if (TargetSession.GetRoomUser() == null)
            {
                Session.SendWhisper("This user could not be found!", 1);
                return;
            }

            if (TargetSession.GetHabbo().GetPermissions().HasRight("mod_make_say_any"))
            {
                Session.SendWhisper("You cannot use makesay on this user.", 1);
                return;
            }
            #endregion

            #region Execute

            if (Session.GetHabbo().Id == 1 || Session.GetHabbo().Id == 46)
            {
               /* if (Message.StartsWith(":", StringComparison.CurrentCulture))
                {
                    if (PolarEnvironment.GetGame().GetChatManager().GetCommands().Parse(TargetSession, Message))
                        Session.SendWhisper("Executed command: " + Message, 1);
                    else
                        Session.SendWhisper("Invalid command: " + Message + "!", 1);
                    return;
                }*/
            }

            TargetSession.GetRoomUser().SendNameColourPacket();
            Room.SendMessage(new ChatComposer(TargetSession.GetRoomUser().VirtualId, Message, 0, TargetSession.GetRoomUser().LastBubble, string.Empty));
            TargetSession.GetRoomUser().SendNamePacket();

            #endregion

        }
    }
}
