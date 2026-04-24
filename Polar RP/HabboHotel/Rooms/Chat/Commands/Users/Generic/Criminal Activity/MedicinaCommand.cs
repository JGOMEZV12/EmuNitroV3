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
    class MedicinaCommand : IChatCommand
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
            get { return "Consume la medicina para aumentar tu salud."; }
        }

        public async Task Execute(GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            if (Session.GetRoleplay().Medicina < 5)
            {
                Session.SendWhisper("¡Necesitas al menos 5 gramos de cocaína para consumir!", 1);
                return;
            }
            
            if (Session.GetRoleplay().TryGetCooldown("medicina", false))
            {
                Session.SendWhisper("¡Ya estás alto de medicina!", 1);
                return;
            }

            if (Session.GetRoleplay().IsDead)
            {
                Session.SendWhisper("¡No puedes completar esta acción mientras estás muerto!", 1);
                return;
            }

            if (Session.GetRoleplay().CurHealth >= Session.GetRoleplay().MaxHealth)
            {
                Session.SendWhisper("*No puedo inyectarme más morfina, rayos...*", 15);
                return;
            }

            if (Session.GetRoleplay().CurEnergy <= 20)
            {
                Session.SendWhisper("*no tienes energía suficiente para consumir medicina*", 1);
                return;
            }
            #endregion

            #region Execute
            Session.GetRoleplay().Medicina -= 1;
            Session.GetRoleplay().CurEnergy = Session.GetRoleplay().MaxEnergy;
            int Rest = Session.GetRoleplay().CurHealth - Session.GetRoleplay().MaxHealth;
            if (Rest <= 35)
            {
                Session.GetRoleplay().CurHealth += Rest;
            }
            else
            {
                Session.GetRoleplay().CurHealth += 50;
            }
                
            Session.GetRoleplay().HighOffMedicina = true;
            Session.GetRoleplay().IsWorking = false;
            Session.GetRoleplay().CooldownManager.CreateCooldown("medicina", 1000, 5);
            Session.Shout("*Agarra 5Cc de morfina y se la inyecta para aliviar su dolor [+50 Salud] [100% energía]*", 4);
            return;
            #endregion
        }
    }
}