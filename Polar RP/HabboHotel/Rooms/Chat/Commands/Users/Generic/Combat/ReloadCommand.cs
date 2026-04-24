using System;
using System.Linq;
using System.Text;
using System.Data;
using System.Diagnostics;
using System.Collections.Generic;

using Polar.Communication.Packets.Outgoing.Users;
using Polar.Communication.Packets.Outgoing.Notifications;


using Polar.Communication.Packets.Outgoing.Handshake;
using Polar.Communication.Packets.Outgoing.Quests;
using Polar.HabboHotel.Items;
using Polar.Communication.Packets.Outgoing.Inventory.Furni;
using Polar.Communication.Packets.Outgoing.Catalog;
using Polar.HabboHotel.Quests;
using Polar.HabboHotel.Rooms;
using System.Threading;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Rooms.Avatar;
using Polar.Communication.Packets.Outgoing.Pets;
using Polar.Communication.Packets.Outgoing.Messenger;
using Polar.HabboHotel.Users.Messenger;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using Polar.Communication.Packets.Outgoing.Availability;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Combat
{
    class ReloadGunCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_combat_reload_gun"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Recarga tu arma."; }
        }

        public async Task Execute(GameClient Session, Room Room, string[] Params)
        {
            var Weapon = Session.GetRoleplay().EquippedWeapon;

            if (Weapon == null)
            {
                Session.SendWhisper("No tienes arma equipada", 1);
                return;
            }

            if (Session.GetRoleplay().GunShots <= 0 && Session.GetRoleplay().Bullets > Weapon.ClipSize)
            {
                Session.SendWhisper("¡Tu arma está completamente cargada!", 1);
                return;
            }
            else
            {
                Weapon.Reload(Session);
                Session.GetRoleplay().CooldownManager.CreateCooldown("reload", 1000, Weapon.ReloadTime);
            }

            
        }
    }
}