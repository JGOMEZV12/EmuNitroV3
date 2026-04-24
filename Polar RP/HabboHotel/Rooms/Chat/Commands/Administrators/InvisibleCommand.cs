using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using System.Threading.Tasks;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Administrators
{
    class InvisibleCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_invisible"; }
        }

        public string Parameters
        {
            get { return "%username%"; }
        }

        public string Description
        {
            get { return "se invisible"; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {

            if (Session.GetRoleplay().Invisible)
            {
                Session.SendWhisper("Ya eres invisible", 1);
                return;
            }

            foreach (RoomUser roomUser in Session.GetHabbo().CurrentRoom.GetRoomUserManager().GetUserList().ToList())
            {
                if (roomUser == null)
                    continue;
                if (roomUser.GetClient() == null)
                    continue;
                if (roomUser.GetClient().GetHabbo() == null)
                    continue;
                if (roomUser.GetClient().GetRoleplay().Invisible && roomUser.GetClient().GetHabbo().Username != Session.GetHabbo().Username)
                {
                    roomUser.GetClient().SendWhisper(Session.GetHabbo().Username + " ¡Simplemente se hizo invisible, para que puedan verte y también puedes verlos!", 1);
                    continue;
                }
                if (roomUser.GetClient().GetHabbo().Username == Session.GetHabbo().Username)
                {
                    string cansee = "";


                    foreach (RoomUser invisibleuser in Session.GetHabbo().CurrentRoom.GetRoomUserManager().GetUserList().ToList())
                    {

                        if (invisibleuser.IsBot)
                            continue;

                        if (invisibleuser.GetClient().GetHabbo().Username != Session.GetHabbo().Username && invisibleuser.GetClient().GetRoleplay().Invisible)
                        {
                            cansee += invisibleuser.GetClient().GetHabbo().Username + ", ";
                            Session.SendMessage(new UsersComposer(invisibleuser));
                        }
                    }

                    Session.SendWhisper((cansee == "" ? "¡No hay personas invisibles en la habitación!" : "Ahora puedes ver: " + cansee + " ¡ya que son invisibles!"), 1);

                    continue;
                }



                roomUser.GetClient().SendMessage(new UserRemoveComposer(Session.GetRoomUser().VirtualId));
            }

            Session.SendWhisper("Tu ya estas invisible", 1);
            Session.GetRoleplay().Invisible = true;



        }
    }
}