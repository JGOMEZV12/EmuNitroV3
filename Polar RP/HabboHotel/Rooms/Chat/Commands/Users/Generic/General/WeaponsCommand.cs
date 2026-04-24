using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.Communication.Packets.Outgoing.Notifications;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Weapons;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.General
{
    class WeaponsCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_general_stats_weapons"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Proporciona una lista de todas las armas que posee."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Session.GetRoleplay().OwnedWeapons.Count <= 0)
            {
                Session.SendWhisper("¡No tienes armas!", 1);
                return;
            }

            StringBuilder Message = new StringBuilder().Append("--- TUS ARMAS ---\n\n");

            lock (Session.GetRoleplay().OwnedWeapons.Values)
            {
                foreach (Weapon Weapon in Session.GetRoleplay().OwnedWeapons.Values)
                {
                    int NeedPieces = Weapon.CostFine / 2;
                    Message.Append(Weapon.PublicName + " (COD: "+Weapon.Name+") ---> Vida: " + Weapon.WLife + ", Alcance: " + Weapon.Range + " y Daño: " + Weapon.MinDamage + " - " + Weapon.MaxDamage + ".\n\n");
                    Message.Append("Balas: " + Weapon.TotalBullets + " en el cargador.\n");
                    Message.Append("Para ser reparada: " + NeedPieces + " piezas.\n\n");
                }
            }
            Session.SendMessage(new MOTDNotificationComposer(Message.ToString()));
        }
    }
}