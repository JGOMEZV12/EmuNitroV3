using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Quests;
using Polar.Communication.Packets.Outgoing.Users;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Rooms;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Weapons;


namespace Polar.Communication.Packets.Incoming.Inventory.Weapons
{
    internal class SetActivatedWeaponsEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {

            int Slot = Packet.PopInt();
            string Badge = Packet.PopString();

            string GunName = Badge.ToLower();
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

            if (Session.GetRoleplay().TryGetCooldown("equip", true))
                return;

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
            Session.GetRoleplay().CooldownManager.CreateCooldown("equip", 1000, 3);

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

            Session.GetRoleplay().UpdateInteractingUserDialogues();
            Session.GetRoleplay().RefreshStatDialogue();
            return;
            #endregion
        }
    }
}
