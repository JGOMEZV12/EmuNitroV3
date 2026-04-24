using Polar.Communication.Packets.Outgoing.Inventory.Weapons;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Criminal
{
    class CaramelosCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_criminal_activity_snort"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Come los caramelos que tengas en tus bolsillos para aumentar tu energia."; }
        }

        public async Task Execute(GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            if (Session.GetRoleplay().Caramelos < 1)
            {
                Session.SendWhisper("¡Necesitas al menos un caramelo para comer!", 1);
                return;
            }
            
            if (Session.GetRoleplay().TryGetCooldown("caramelos", false))
            {
                Session.SendWhisper("¡Ya estás harto de caramelos!", 1);
                return;
            }

            if (Session.GetRoleplay().IsDead)
            {
                Session.SendWhisper("¡No puedes completar esta acción mientras estás muerto!", 1);
                return;
            }

            if (Session.GetRoleplay().CurEnergy >= Session.GetRoleplay().MaxEnergy)
            {
                Session.SendWhisper("*No puedes comer más caramelos, ya estás full de energía*", 15);
                return;
            }
            #endregion

            #region Execute
            RoomUser User = Session.GetRoomUser();
            if (User == null)
                return;

            //User.CarryItem(67);

            Session.GetRoleplay().Caramelos -= 1;
            Session.GetRoleplay().CurEnergy = Session.GetRoleplay().MaxEnergy;
            Session.GetRoleplay().CooldownManager.CreateCooldown("caramelos", 1000, 60);
            Session.SendMessage(new WeaponsComposer(Session));
            Session.Shout("*Saca un caramelo de su bolsillo y se lo come [Máximo energía]*", 4);
            return;
            #endregion
        }
    }
}