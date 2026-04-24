using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.Communication.Packets.Outgoing.Notifications;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.RPRoom;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Police
{
    class SurrenderCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_police_related_surrender"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Te rindes a las autoridades si estás en la lista de deseados."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Session == null || Session.GetHabbo() == null || Session.GetRoleplay() == null || Session.GetRoomUser() == null)
                return;

            if (!Session.GetRoleplay().IsWanted || !RoleplayManager.WantedList.ContainsKey(Session.GetHabbo().Id))
            {
                Session.SendWhisper("No eres buscado!", 1);
                return;
            }

            if (Params.Length < 2)
            {
                Session.SendWhisper("Para confirmar que te rindes escribe :rendicion yes");
                return;
            }

            if (Params[1].ToString().ToLower() == "yes")
            {
                string MyCity = Room.City;
                RPRoom Data;
                int JailRID = PolarEnvironment.GetGame().GetRPRoomManager().TryToGetJail(MyCity, out Data);//prision de la cd.

                Session.Shout("*Se entrega a las autoridades judiciales y es acompañado a prisión.*", 4);

                if (Session.GetHabbo().CurrentRoomId != JailRID)
                    RoleplayManager.SendUserOld2(Session, JailRID, "Te has rendido a las autoridades legales y has sido encarcelado por " + Session.GetRoleplay().WantedLevel * 5 + " minutos!");

                Wanted Junk;
                RoleplayManager.WantedList.TryRemove(Session.GetHabbo().Id, out Junk);

                PolarEnvironment.GetGame().GetClientManager().JailAlert("[RADIO] " + Session.GetHabbo().Username + " se ha entregado a las autoridade!");

                if (Session.GetRoleplay().EquippedWeapon != null)
                    Session.GetRoleplay().EquippedWeapon = null;

                Session.GetRoleplay().IsJailed = true;
                Session.GetRoleplay().JailedTimeLeft = Session.GetRoleplay().WantedLevel * 5;

                Session.GetRoleplay().TimerManager.CreateTimer("jail", 1000, false);
                return;
            }
            else
            {
                Session.SendWhisper("¿Estás seguro de que quieres rendirte? Serás encarcelado por " + Session.GetRoleplay().WantedLevel * 5 + " minutos!", 1);
                Session.SendWhisper("escribe :rendicion yes si realmente quieres renunciar a tu libertad.", 1);
                return;
            }
        }
    }
}