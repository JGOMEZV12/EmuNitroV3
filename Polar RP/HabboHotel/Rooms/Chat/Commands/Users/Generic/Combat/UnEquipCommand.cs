using Polar.Communication.Packets.Outgoing.Inventory.Weapons;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.Combat;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboRoleplay.Weapons;
using Polar.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Combat
{
    class UnEquipCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_combat_equip_undo"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Unequips any weapon you have equipped."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions

            // ✅ FIX: GetRoomUser() puede ser null si el usuario se desconectó o salió de la sala
            if (Session.GetRoomUser() == null)
                return;

            if (Session.GetRoomUser().Frozen)
                return;

            if (Session.GetRoleplay().EquippedWeapon == null)
            {
                Session.SendWhisper("¡No tienes un arma equipada!", 1);
                return;
            }

            //if (Session.GetRoleplay().TryGetCooldown("unequip", true))
               // return;

            #endregion

            #region Execute

            CryptoRandom Random = new CryptoRandom();
            int Chance = Random.Next(1, 101);

            if (Chance <= 8)
            {
                Session.Shout("*Intenta deslizar su " + Session.GetRoleplay().EquippedWeapon.PublicName + " de nuevo en su funda, pero falla*", 4);
                return;
            }
            else
            {
                RoleplayManager.UpdateMyWeaponStats(Session, "bullets", Session.GetRoleplay().Bullets, Session.GetRoleplay().EquippedWeapon.Name);
                RoleplayManager.UpdateMyWeaponStats(Session, "life", Session.GetRoleplay().WLife, Session.GetRoleplay().EquippedWeapon.Name);

                string UnEquipMessage = Session.GetRoleplay().EquippedWeapon.UnEquipText;
                UnEquipMessage = UnEquipMessage.Replace("[NAME]", Session.GetRoleplay().EquippedWeapon.PublicName);

                Session.Shout(UnEquipMessage, 4);

                if (Session.GetRoomUser().CurrentEffect == Session.GetRoleplay().EquippedWeapon.EffectID)
                    Session.GetRoomUser().ApplyEffect(0);

                if (Session.GetRoomUser().CarryItemID == Session.GetRoleplay().EquippedWeapon.HandItem)
                    Session.GetRoomUser().CarryItem(0);

                //Session.GetRoleplay().CooldownManager.CreateCooldown("unequip", 1000, 3);
                
                Session.GetRoleplay().EquippedWeapon = null;
                Session.GetRoleplay().Bullets = 0;
                Session.SendMessage(new WeaponsComposer(Session));
                Session.GetRoleplay().UpdateInteractingUserDialogues();
                Session.GetRoleplay().RefreshStatDialogue();
                return;
            }

            #endregion
        }
    }
}