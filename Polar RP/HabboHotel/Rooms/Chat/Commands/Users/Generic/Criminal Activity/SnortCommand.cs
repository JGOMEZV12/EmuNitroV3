using System;
using System.Linq;
using System.Text;
using System.Drawing;
using Polar.Utilities;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using System.Threading;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Criminal
{
    class SnortCommand : IChatCommand
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
            get { return "Rinde un poco de cocaína para una rápida alta."; }
        }

        public async Task Execute(GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            if (Session.GetRoleplay().Cocaine < 5)
            {
                Session.SendWhisper("¡Necesitas al menos 5 gramos de cocaína para consumir!", 1);
                return;
            }

            if (Session.GetRoleplay().TryGetCooldown("cocaine", false))
            {
                Session.SendWhisper("Ya estás alto de cocaína!", 1);
                return;
            }

            if (Session.GetRoleplay().IsDead)
            {
                Session.SendWhisper("¡No puedes completar esta acción mientras estás muerto!", 1);
                return;
            }
            RoomUser User = Session.GetRoomUser();
            if (User == null)
                return;
            #endregion

            #region Execute

            Session.GetRoleplay().Cocaine -= 6;
            Session.GetRoleplay().CurEnergy += 5;
            Session.GetRoleplay().CurHealth += 5;
            Session.GetRoleplay().HighOffCocaine = true;
            Session.GetRoleplay().CooldownManager.CreateCooldown("cocaine", 30000, 10);
            Session.Shout("*Extrae 6g de cocaína, alineándolo, dando [+5 Energia / +5 Vida] y luego resoplando todo con un billete de 100$*", 4);
            User.SuperFastWalking = !User.SuperFastWalking;

            if (User.FastWalking)
                User.FastWalking = false;
            if (!Session.GetRoleplay().WantedFor.Contains("Consumiendo sustacias prohibidas "))
                Session.GetRoleplay().WantedFor = Session.GetRoleplay().WantedFor + " Consumiendo sustacias prohibidas , ";
            return;
            #endregion
        }
    }
}