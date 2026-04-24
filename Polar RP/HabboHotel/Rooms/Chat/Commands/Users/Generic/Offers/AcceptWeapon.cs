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
    class AcceptWeaponCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_offers_offer"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Ofrece el tipo deseado al usuario deseado"; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("Diga :aceptar arma", 1);
                return;
            }

            string Type = Params[1];

            Weapon weapon = null;
            if (Type.ToLower() == "weapon" || Type.ToLower() == "arma")
            {
                if (Session.GetRoleplay().OfferManager.ActiveOffers.Values.Where(x => WeaponManager.getWeapon(x.Type.ToLower()) != null).ToList().Count > 0)
                    weapon = WeaponManager.getWeapon(Session.GetRoleplay().OfferManager.ActiveOffers.Values.FirstOrDefault(x => WeaponManager.getWeapon(x.Type.ToLower()) != null).Type.ToLower());
            }

            #region Weapons
            if (Type.ToLower() == "weapon" || Type.ToLower() == "arma" || WeaponManager.Weapons.ContainsKey(Type.ToLower()))
            {
                if (weapon != null)
                {
                    var Offer = Session.GetRoleplay().OfferManager.ActiveOffers[weapon.Name];

                if (Offer.Params != null && Offer.Params.Length > 0)
                {
                    GameClient Offerer = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Offer.OffererId);
                        if (Offerer == null || Offerer.GetRoomUser().RoomId != Session.GetRoomUser().RoomId || Offerer.GetRoomUser() == null)
                        {
                            RoleplayOffer Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove(weapon.Name.ToLower(), out Junk);
                            Session.SendWhisper("Lo sentimos, este usuario se ha desconectado o no está en la misma habitación que tú.", 1);
                            return;
                        }
                        else if (Session.GetHabbo().Credits < Offer.Cost)
                        {
                            RoleplayOffer Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove(weapon.Name.ToLower(), out Junk);
                            Session.SendWhisper("Lo siento, no puedes pagar una " + weapon.PublicName + "", 1);
                            return;
                        }
                        if (Session.GetRoleplay().Level < 5)
                        {
                            Session.SendWhisper("Tu nivel es demasiado bajo para comprar armas a particulares se requiere nivel 5", 1);
                            return;
                        }
                        else
                        {
                            if (!Session.GetRoleplay().OwnedWeapons.ContainsKey(weapon.Name))
                            {
                                Session.Shout("*Acepta la oferta de " + Offerer.GetHabbo().Username + " y compra el " + weapon.PublicName + " por $" + String.Format("{0:N0}", Offer.Cost) + "*", 4);
                                Session.GetHabbo().Credits -= Offer.Cost;
                                Session.GetHabbo().UpdateCreditsBalance();
                                Offerer.GetRoleplay().EquippedWeapon = null;
                                //WeaponManager.Weapons[weapon.Name].Stock--;
                                RoleplayManager.AddWeapon(Session, weapon);
                            }
                            else
                            {
                                Session.Shout("*Acepta la oferta de " + Offerer.GetHabbo().Username + " Y compra " + weapon.PublicName + " por $" + String.Format("{0:N0}", Offer.Cost) + "*", 4);
                                Session.GetHabbo().Credits -= Offer.Cost;
                                Offerer.GetRoleplay().EquippedWeapon = null;
                                Session.GetHabbo().UpdateCreditsBalance();

                                // WeaponManager.Weapons[weapon.Name].Stock--;

                                using (var DB = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                                {
                                    DB.SetQuery("UPDATE `rp_weapons_owned` SET `can_use` = '1' WHERE `user_id` = @userid AND `base_weapon` = @baseweapon LIMIT 1");
                                    DB.AddParameter("userid", Session.GetHabbo().Id);
                                    DB.AddParameter("baseweapon", weapon.Name.ToLower());
                                    DB.RunQuery();
                                }

                                Session.GetRoleplay().OwnedWeapons = null;
                                Session.GetRoleplay().OwnedWeapons = Session.GetRoleplay().LoadAndReturnWeapons();
                            }

                            int Bonus = 200;
                            if (Offer.Cost / 20 > 500)
                                Bonus = 500;
                            else if (Offer.Cost / 20 > 200)
                                Bonus = 200;
                            else
                                Bonus = Offer.Cost / 20;

                            Offerer.GetHabbo().Credits += Offer.Cost;
                            Offerer.GetHabbo().UpdateCreditsBalance();

                            using (var DBs = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                            {
                                DBs.RunQuery("DELETE FROM `rp_weapons_owned` WHERE `user_id` = '" + Offerer.GetHabbo().Id + "' AND `base_weapon` = '" + weapon.Name.ToLower() + "' LIMIT 1");
                            }

                            Session.GetRoleplay().OwnedWeapons = null;
                            Session.GetRoleplay().OwnedWeapons = Session.GetRoleplay().LoadAndReturnWeapons();

                            Offerer.GetRoleplay().OwnedWeapons = null;
                            Offerer.GetRoleplay().OwnedWeapons = Offerer.GetRoleplay().LoadAndReturnWeapons();

                            RoleplayOffer Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove(weapon.Name.ToLower(), out Junk);
                            Offerer.SendWhisper("Recibes $" + String.Format("{0:N0}", Offer.Cost) + " por venta a " + Session.GetHabbo().Username + "  de un " + weapon.PublicName + "!");
                            PolarEnvironment.GetGame().GetAchievementManager().ProgressAchievement(Offerer, "ACH_SellingWeapon", 1);
                            return;
                        }
                      #endregion
                    }
                }
            }
        }
    }
}