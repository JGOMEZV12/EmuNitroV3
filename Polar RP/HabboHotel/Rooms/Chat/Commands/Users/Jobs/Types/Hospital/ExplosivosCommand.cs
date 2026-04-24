using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Jobs.Types.Hospital
{
    class ExplosivosCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_hospital_heal"; }
        }

        public string Parameters
        {
            get { return "%user%"; }
        }

        public string Description
        {
            get { return "Compra pack de explosivos"; }
        }

        public async Task Execute(GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            RoomUser RoomUser = Session.GetRoomUser();
            if (Session.GetHabbo().CurrentRoomId != 6)
            {
                Session.SendWhisper("¡Los explosivos solo se compra en la tienda de armas ID: [6]!", 1);
                return;
            }

            if (Session.GetRoleplay().BankChequings < 10000)
            {
                Session.SendWhisper("Necesitas tener 10000$ en tu cuenta bancaria para poder comprar dinamitas ¡Trabaja!", 1);
                return;
            }

            #endregion

            #region Execute
            Session.Shout("*Compra una caja [+10 Explosivos] de explosivos y paga con su tarjeta de débito [-10000$]*", 4);
            Session.GetRoleplay().BankChequings -= 10000;
            Session.GetRoomUser().ApplyEffect(603);
            Session.GetHabbo().UpdateCreditsBalance();
            Session.GetRoleplay().Dynamite += 10;
            #endregion
        }
    }
}