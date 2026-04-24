using Polar.Communication.Packets.Outgoing.Inventory.Weapons;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.Combat;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboRoleplay.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Combat
{
    class EquipCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_combat_equip"; }
        }

        public string Parameters
        {
            get { return "%weapon%"; }
        }

        public string Description
        {
            get { return "Sacar el arma que quiera"; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            // ✅ FIX: GetRoomUser() puede ser null si el usuario se desconectó o salió de la sala
            if (Session.GetRoomUser() == null)
                return;

            if (Params.Length == 1)
            {
                Session.SendWhisper("¡Vaya, olvidó ingresar un nombre de arma!", 1);
                return;
            }

            string GunName = Params[1].ToLower();
            Weapon BaseWeapon = WeaponManager.getWeapon(GunName);

            if (BaseWeapon == null)
            {
                Session.SendWhisper("¡Esta arma no existe!", 1);
                return;
            }

            if (Session.GetRoomUser().Frozen)
                return;

            if (Session.GetRoleplay().PassiveMode)
            {
                Session.SendWhisper("No puedes equipar armas en modo pasivo.", 1);
                return;
            }

            if (Session.GetRoleplay().EquippedWeapon != null)
            {
                if (Session.GetRoleplay().EquippedWeapon.Name == BaseWeapon.Name)
                {
                    Session.SendWhisper("Ya tienes esta arma equipada", 1);
                    return;
                }
            }

            if (Session.GetRoleplay().AmbassadorOnDuty)
            {
                Session.SendWhisper("No puedes sacar un arma si estas como embajador, trampos@", 1);
                return;
            }

            if (!Session.GetRoleplay().OwnedWeapons.ContainsKey(GunName))
            {
                Session.SendWhisper("¡No eres el dueño de esta arma!", 1);
                return;
            }

            if (Session.GetRoomUser().RidingHorse == true)
            {
                Session.SendWhisper("¡No puede hacer esto si eres un caballo!", 1);
                return;
            }

            if (Session.GetRoleplay().Learning)
            {
                Session.SendWhisper("Deja de leer para poder sacar tu arma, recuerda respetar el espacio libre de armas");
                return;
            }

            if (!Session.GetRoleplay().OwnedWeapons[GunName].CanUse)
            {
                Session.SendWhisper("No puedes usar esta arma hasta que pagues una multa de $" + String.Format("{0:N0}", Session.GetRoleplay().OwnedWeapons[GunName].CostFine) + "!", 1);
                return;
            }

            if (Session.GetRoleplay().IsDead)
            {
                Session.SendWhisper("¡No puedes sacar un arma mientras estás muerto!", 1);
                return;
            }

            if (Session.GetRoleplay().IsJailed)
            {
                Session.SendWhisper("¡No puedes equipar un arma mientras estás encarcelado!", 1);
                return;
            }

            if (Session.GetRoleplay().Cuffed)
            {
                Session.SendWhisper("No puedes sacar un " + GunName + ", con las manos esposadas", 1);
                return;
            }
            
            if (BaseWeapon.LevelRequirement > Session.GetRoleplay().Level)
            {
                Session.SendWhisper("Lo siento, el requisito de nivel de armas es: Level " + BaseWeapon.LevelRequirement + ".", 1);
                return;
            }

            //if (Session.GetRoleplay().TryGetCooldown("equip", true))
            //    return;
            #endregion

            #region Execute

            if (Session.GetRoleplay().EquippedWeapon != null)
            {
                RoleplayManager.UpdateMyWeaponStats(Session, "bullets", Session.GetRoleplay().Bullets, Session.GetRoleplay().EquippedWeapon.Name);
                RoleplayManager.UpdateMyWeaponStats(Session, "life", Session.GetRoleplay().WLife, Session.GetRoleplay().EquippedWeapon.Name);
            }

            var Weapon = Session.GetRoleplay().OwnedWeapons[GunName];

            string EquipMessage = Weapon.EquipText;
            EquipMessage = EquipMessage.Replace("[NAME]", Weapon.PublicName);

            Session.SendWhisper(EquipMessage, 1);
            //Session.SendWhisper("Your " + Weapon.PublicName + " has " + Weapon.Clip + "/" + Weapon.ClipSize + " bullets in the magazine.", 1);
            Session.GetRoleplay().IsNoob = false;
            Session.GetRoleplay().EquippedWeapon = Weapon;
            Session.GetRoleplay().Bullets = Weapon.TotalBullets;
            Session.GetRoleplay().WLife = Weapon.WLife;
            //Session.GetRoleplay().CooldownManager.CreateCooldown("equip", 1000, 3);

            if (Session.GetRoomUser().CurrentEffect != Weapon.EffectID)
                Session.GetRoomUser().ApplyEffect(Weapon.EffectID);

            if (Session.GetRoomUser().CarryItemID != Weapon.HandItem)
                Session.GetRoomUser().CarryItem(Weapon.HandItem);

            #region Sound System
            foreach (RoomUser RoomUsers in Session.GetRoomUser().GetRoom().GetRoomUserManager().GetRoomUsers())
            {
                if (RoomUsers == null || RoomUsers.GetClient() == null)
                    continue;

                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(RoomUsers.GetClient(), "event_feedcomposer", "sound|reload|" + Session.GetRoleplay().EquippedWeapon.Name);
            }
            #endregion
            Session.SendMessage(new WeaponsComposer(Session));
            Session.GetRoleplay().UpdateInteractingUserDialogues();
            Session.GetRoleplay().RefreshStatDialogue();
            return;
            #endregion
        }
    }
}