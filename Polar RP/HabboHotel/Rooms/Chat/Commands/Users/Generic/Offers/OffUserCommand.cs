using System;
using System.Linq;
using System.Data;
using System.Text;
using System.Collections.Concurrent;
using Polar.Communication.Packets.Outgoing.Notifications;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Weapons;
using Polar.HabboRoleplay.Farming;
using Polar.HabboHotel.Items;
using Polar.HabboRoleplay.RoleplayUsers.Offers;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Offers
{
    class OffUserCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_offers_offer"; }
        }

        public string Parameters
        {
            get { return "%user% %arma% %precio%"; }
        }

        public string Description
        {
            get { return "Ofrece el tipo deseado al usuario deseado"; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("olvidaste el nombre de la persona", 1);
                return;
            }

            string Type = Params[2];

            GameClient Target = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);
            if (Target == null)
            {
                Session.SendWhisper("¡Uy, no pudo encontrar ese usuario!", 1);
                return;
            }

            RoomUser TargetUser = Room.GetRoomUserManager().GetRoomUserByHabbo(Target.GetHabbo().Username);
            if (TargetUser == null)
            {
                Session.SendWhisper("Se ha producido un error al encontrar a ese usuario, tal vez no estén en línea o en esta sala.", 1);
                return;
            }

            #region Weapon Check
            Weapon weapon = null;
            foreach (Weapon Weapon in WeaponManager.Weapons.Values)
            {
                if (Type.ToLower() == Weapon.Name.ToLower())
                {
                    weapon = Weapon;
                }
            }
            #endregion

            if (!Session.GetRoleplay().OwnedWeapons.ContainsKey(Params[2]))
            {
                Session.SendWhisper("No tienes un/una " + weapon.PublicName + " para ofrecerle a " + Target.GetHabbo().Username + "!", 1);
                return;
            }

            if (Target.GetRoleplay().OwnedWeapons.ContainsKey(Params[2]) && Target.GetRoleplay().OwnedWeapons[Params[2]].CanUse)
            {
                Session.SendWhisper("Este ciudadano ya tiene un " + weapon.PublicName + "!", 1);
                return;
            }

            int Cost = Convert.ToInt32(Params[3]);
            bool HasOffer = false;
            if (Target.GetHabbo().Credits >= Cost)
            {
                foreach (var Offer in Target.GetRoleplay().OfferManager.ActiveOffers.Values)
                {
                    if (Target.GetRoleplay().OwnedWeapons.ContainsKey(Offer.Type.ToLower()))
                        HasOffer = true;
                }
                if (!HasOffer)
                {
                    Session.Shout("*Ofrece un " + weapon.PublicName + " a " + Target.GetHabbo().Username + " por $" + String.Format("{0:N0}", Cost) + "*", 4);
                    Target.GetRoleplay().OfferManager.CreateOffer(weapon.Name.ToLower(), Session.GetHabbo().Id, Cost, this);
                    Target.SendWhisper("Acaba de ofrecerte a " + weapon.PublicName + " por $" + String.Format("{0:N0}", Cost) + "! Diga ':acc weapon' para comprarlo!", 1);
                    return;
                }
                else
                {
                    Session.SendWhisper("A este usuario ya se le ha ofrecido un arma", 1);
                    return;
                }
            }
            else
            {
                Session.SendWhisper("Este ciudadano no puede " + weapon.PublicName + "!", 1);
                return;
            }
        }
    }
}