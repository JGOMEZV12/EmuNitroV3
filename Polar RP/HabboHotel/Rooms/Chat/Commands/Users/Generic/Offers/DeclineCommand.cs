using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Groups;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.RoleplayUsers.Offers;
using Polar.HabboRoleplay.Bots;
using Polar.HabboRoleplay.Weapons;
using Polar.HabboRoleplay.Farming;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Offers
{
    class DeclineCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_offers_decline"; }
        }

        public string Parameters
        {
            get { return "%type%"; }
        }

        public string Description
        {
            get { return "Rechaza cualquier oferta."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("escribe, ':recharzar (offer)'. mirar :ofertas allí sabrás que ofertas tienes", 1);
                return;
            }

            string Type = Params[1];

            if (Session.GetRoleplay().OfferManager.ActiveOffers.Count <= 0)
            {
                Session.SendWhisper("¡No tienes ofertas que rechazar!", 1);
                return;
            }

            Weapon weapon = null;
            if (Type.ToLower() == "arma")
            {
                if (Session.GetRoleplay().OfferManager.ActiveOffers.Values.Where(x => WeaponManager.getWeapon(x.Type.ToLower()) != null).ToList().Count > 0)
                    weapon = WeaponManager.getWeapon(Session.GetRoleplay().OfferManager.ActiveOffers.Values.FirstOrDefault(x => WeaponManager.getWeapon(x.Type.ToLower()) != null).Type.ToLower());
            }

            if (Type.ToLower() == "corriente")
                Type = "corriente";

            if (Session.GetRoleplay().OfferManager.ActiveOffers.ContainsKey(Type.ToLower()) || weapon != null)
            {
                RoleplayOffer Offer;
                if (weapon == null)
                    Offer = Session.GetRoleplay().OfferManager.ActiveOffers[Type.ToLower()];
                else
                    Offer = Session.GetRoleplay().OfferManager.ActiveOffers[weapon.Name.ToLower()];

                if (Offer.Params != null && Offer.Params.Length > 0)
                {
                    if (Offer.Type.ToLower() == "semillas")
                    {
                        if (Offer.Params.Length > 1)
                        {
                            RoleplayBotAI Bot = (RoleplayBotAI)Offer.Params[0];
                            Session.Shout("*Rechaza su " + (Type.Substring(0, 1).ToUpper() + Type.Substring(1)) + " oferta de " + Bot.GetBotRoleplay().Name + "*", 4);
                        }
                        else
                            Session.Shout("*Rechaza su " + (Type.Substring(0, 1).ToUpper() + Type.Substring(1)) + " oferta de " + PolarEnvironment.GetHabboById(Offer.OffererId).Username + "*", 4);
                    }
                    else
                    {
                        RoleplayBotAI Bot = (RoleplayBotAI)Offer.Params[0];
                        Session.Shout("*Rechaza su " + (Type.Substring(0, 1).ToUpper() + Type.Substring(1)) + " oferta de " + Bot.GetBotRoleplay().Name + "*", 4);
                    }
                }
                else
                    Session.Shout("*Rechaza su " + (Type.Substring(0, 1).ToUpper() + Type.Substring(1)) + " oferta de " + PolarEnvironment.GetHabboById(Offer.OffererId).Username + "*", 4);

                RoleplayOffer Junk;
                Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove(Offer.Type.ToLower(), out Junk);
            }
            else
            {
                Session.SendWhisper("No tienes oferta de " + (Type.Substring(0, 1).ToUpper() + Type.Substring(1)) + " para rechazar ':ofertas' para ver todas", 1);
                return;
            }
        }
    }
}