using Polar.Communication.Packets.Outgoing.Catalog;
using Polar.Communication.Packets.Outgoing.Groups;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.Communication.Packets.Outgoing.Users;
using Polar.Database.Interfaces;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Users.Inventory.Bots;
using Polar.HabboRoleplay.Bots;
using Polar.HabboRoleplay.Farming;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.PhoneOwned;
using Polar.HabboRoleplay.Phones;
using Polar.HabboRoleplay.RoleplayUsers.Offers;
using Polar.HabboRoleplay.VehicleOwned;
using Polar.HabboRoleplay.Vehicles;
using Polar.HabboRoleplay.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Offers
{
    class AcceptCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_offers_accept"; }
        }

        public string Parameters
        {
            get { return "%type%"; }
        }

        public string Description
        {
            get { return "Acepta la oferta según el tipo deseado."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("Introduzca un tipo de oferta! Compruebe: ofertas para ver sus ofertas actuales!", 1);
                return;
            }

            if (Session.GetRoleplay().OfferManager.ActiveOffers.Count <= 0)
            {
                Session.SendWhisper("¡No tienes ofertas activas que aceptar!", 1);
                return;
            }

            string Type = Params[1];

            #region Basic Conditions
            if (Session.GetRoleplay().Cuffed)
            {
                Session.SendWhisper("No puedes hacer eso mientras estás esposad@", 1);
                return;
            }
            if (!Session.GetRoomUser().CanWalk)
            {
                Session.SendWhisper("Al parecer no puedes ni moverte para hacer eso.", 1);
                return;
            }
            if (Session.GetRoleplay().Pasajero)
            {
                Session.SendWhisper("¡No puedes hacer eso mientras vas de Pasajer@!", 1);
                return;
            }
            if (Session.GetRoleplay().IsDead)
            {
                Session.SendWhisper("¡No puedes hacer esto mientras estás muert@!", 1);
                return;
            }
            if (Session.GetRoleplay().DrivingCar)
            {
                Session.SendWhisper("¡No puedes hacer eso mientras conduces!", 1);
                return;
            }
            #endregion

            if (CommandManager.MergeParams(Params, 1).ToLower() == "seed satchel")
                Type = "bolsasemillas";
            else if (CommandManager.MergeParams(Params, 1).ToLower() == "bolsa semillas")
                Type = "bolsasemillas";
            else if (CommandManager.MergeParams(Params, 1).ToLower() == "bolsa vegetal")
                Type = "bolsavegetal";
            else if (CommandManager.MergeParams(Params, 1).ToLower() == "bolsa vegetal")
                Type = "bolsavegetal";

            Weapon? weapon = null;
            if (Type.ToLower() == "weapon" || Type.ToLower() == "arma")
            {
                if (Session.GetRoleplay().OfferManager.ActiveOffers.Values.Where(x => WeaponManager.getWeapon(x.Type.ToLower()) != null).ToList().Count > 0)
                    weapon = WeaponManager.getWeapon(Session.GetRoleplay().OfferManager.ActiveOffers.Values.FirstOrDefault(x => WeaponManager.getWeapon(x.Type.ToLower()) != null).Type.ToLower());
            }

            if (Session.GetRoleplay().OfferManager.ActiveOffers.ContainsKey(Type.ToLower()) || Type.ToLower() == "weapon" || Type.ToLower() == "arma" || Type.ToLower() == "checkings" || Type.ToLower() == "reparacion")
            {

                /* Old
                #region Weapons
                if (Type.ToLower() == "weapon" || Type.ToLower() == "arma" || WeaponManager.Weapons.ContainsKey(Type.ToLower()))
                {
                    if (weapon != null)
                    {
                        var Offer = Session.GetRoleplay().OfferManager.ActiveOffers[weapon.Name.ToLower()];

                        if (Offer.Params != null && Offer.Params.Length > 0)
                        {
                            RoleplayBotAI Bot = (RoleplayBotAI)Offer.Params[0];
                            if (Bot.GetRoomUser().RoomId != Room.Id)
                            {
                                RoleplayOffer? Junk;
                                Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove(weapon.Name.ToLower(), out Junk);
                                Session.SendWhisper("Lo siento, no estás en la misma habitación que " + Bot.GetBotRoleplay().Name + " para aceptar la oferta del arma!", 1);
                                return;
                            }
                            else if (weapon.Stock < 1)
                            {
                                Session.SendWhisper("Lo sentimos, pero esta arma se ha agotado", 1);
                                return;
                            }
                            else if (Session.GetRoleplay().BankTarget < 1)
                            {
                                Session.SendWhisper("Lo sentimos, pero aquí solo aceptamos débito y usted no tiene tarjeta, vaya al banco y solicite", 1);
                                return;
                            }

                            else if (Session.GetRoleplay().PassiveMode)
                            {
                                Session.SendWhisper("No puedes hacer eso mientras estás en modo pasivo.", 1);
                                return;
                            }

                            else if (Session.GetRoleplay().BankChequings < Offer.Cost)
                            {
                                RoleplayOffer? Junk;
                                Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove(weapon.Name.ToLower(), out Junk);
                                Session.SendWhisper("¡Lo siento, no puedes pagar una " + weapon.PublicName + " no tiene dinero en su cuenta bancaria!", 1);
                                return;
                            }
                            else
                            {
                                if (!Session.GetRoleplay().OwnedWeapons.ContainsKey(weapon.Name))
                                {
                                    Session.Shout("*Acepta la oferta de " + Bot.GetBotRoleplay().Name + " Y compra la " + weapon.PublicName + " por $" + String.Format("{0:N0}", Offer.Cost) + "*", 4);
                                    Session.GetRoleplay().BankChequings -= Offer.Cost;
                                    Session.GetHabbo().UpdateCreditsBalance();
                                    Session.GetRoomUser().ApplyEffect(603);

                                    WeaponManager.Weapons[weapon.Name].Stock--;
                                    RoleplayManager.AddWeapon(Session, weapon);
                                }
                                else
                                {
                                    Session.Shout("*Acepta la oferta de " + Bot.GetBotRoleplay().Name + " Y compra " + weapon.PublicName + " por $" + String.Format("{0:N0}", Offer.Cost) + "*", 4);
                                    Session.GetRoleplay().BankChequings -= Offer.Cost;
                                    Session.GetHabbo().UpdateCreditsBalance();
                                    Session.GetRoomUser().ApplyEffect(603);

                                    WeaponManager.Weapons[weapon.Name].Stock--;

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

                                RoleplayOffer? Junk;
                                Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove(weapon.Name.ToLower(), out Junk);
                                Bot.GetRoomUser().Chat("Gracias por comprar un " + weapon.PublicName + " " + Session.GetHabbo().Username + "!", true);
                                int CAmount = (Offer.Cost * 2) / 100;
                                RoleplayManager.GiveMoneyToCompany(5, Session, "ammunation", true, Offer.Cost);
                                Session.GetRoleplay().UpdateInteractingUserDialogues();
                                Session.GetRoleplay().RefreshStatDialogue();
                            }
                            return;
                        }
                        else
                        {
                            GameClient Offerer = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Offer.OffererId);
                            if (Offerer == null)
                            {
                                RoleplayOffer? Junk;
                                Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove(weapon.Name.ToLower(), out Junk);
                                Session.SendWhisper("Al parecer el Ofertante se ha ido. Oferta cancelada.", 1);
                                return;
                            }
                            if (Offerer.GetRoomUser().RoomId != Room.Id)
                            {
                                RoleplayOffer? Junk;
                                Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove(weapon.Name.ToLower(), out Junk);
                                Session.SendWhisper("Lo sentimos, no te encuentras en el mismo lugar que " + Offerer.GetHabbo().Username + " para aceptar la oferta del arma.", 1);
                                return;
                            }
                            if (Session.GetRoleplay().PassiveMode)
                            {
                                Session.SendWhisper("No puedes hacer eso mientras estás en modo pasivo.", 1);
                                return;
                            }
                            if (Offerer.GetRoleplay().PassiveMode)
                            {
                                Session.SendWhisper("No puedes aceptarle eso a una persona en modo pasivo.", 1);
                                return;
                            }
                            if (Session.GetRoleplay().Level < 2)
                            {
                                RoleplayOffer? Junk;
                                Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove(weapon.Name.ToLower(), out Junk);
                                Session.SendWhisper("((Debes ser al menos Nivel 2 para poder portar armas))", 1);
                                return;
                            }


                            if (Session.GetRoleplay().OwnedWeapons.ContainsKey(weapon.Name))
                            {
                                RoleplayOffer? Junk;
                                Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove(weapon.Name.ToLower(), out Junk);
                                Session.SendWhisper("Ya tienes esa arma en tu inventario.", 1);
                                return;
                            }
                            if (Session.GetRoleplay().BankChequings < Offer.Cost)
                            {
                                RoleplayOffer? Junk;
                                Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove(weapon.Name.ToLower(), out Junk);
                                Session.SendWhisper("No tienes $" + Offer.Cost + " para aceptar la oferta", 1);
                                return;
                            }
                            if (Session.GetRoleplay().EquippedWeapon != null)
                            {
                                Session.SendWhisper("No debes tener ningún arma equipada mientras aceptas la oferta.", 1);
                                return;
                            }
                            if (!Offerer.GetRoleplay().OwnedWeapons.ContainsKey(weapon.Name))
                            {
                                Session.SendWhisper("Esta persona ya no tiene un/a " + weapon.PublicName, 1);
                                RoleplayOffer? Junk;
                                Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove(weapon.Name.ToLower(), out Junk);
                                return;
                            }
                            if (Offerer.GetRoleplay().EquippedWeapon == null || Offerer.GetRoleplay().EquippedWeapon.Name.ToLower() != weapon.Name.ToLower())
                            {
                                Session.SendWhisper("El vendedor no tiene el arma equipada.", 1);
                                return;
                            }
                            else
                            {
                                RoleplayManager.Shout(Session, "*Acepta la oferta de " + Offerer.GetHabbo().Username + " y compra su " + weapon.PublicName + " por $" + String.Format("{0:N0}", Offer.Cost) + "*", 5);
                                Session.GetRoleplay().BankChequings -= Offer.Cost;
                                Session.GetHabbo().UpdateCreditsBalance();

                                Offerer.GetRoleplay().BankChequings += Offer.Cost;
                                //Offerer.GetRoleplay().MoneyEarned += Offer.Cost;
                                //Offerer.GetHabbo().UpdateCreditsBalance();

                                // Traspaso del dato WLife & Bullets
                                Session.GetRoleplay().WLife = Offerer.GetRoleplay().WLife;
                                Session.GetRoleplay().Bullets = Offerer.GetRoleplay().Bullets;

                                #region Desequipar al Ofertante
                                if (Offerer.GetRoleplay().EquippedWeapon != null)
                                {
                                    if (Offerer.GetRoleplay().EquippedWeapon.Name == weapon.Name)
                                    {
                                        string UnEquipMessage = Offerer.GetRoleplay().EquippedWeapon.UnEquipText;
                                        UnEquipMessage = UnEquipMessage.Replace("[NAME]", Offerer.GetRoleplay().EquippedWeapon.PublicName);

                                        RoleplayManager.Shout(Offerer, UnEquipMessage, 5);

                                        if (Offerer.GetRoomUser().CurrentEffect == Offerer.GetRoleplay().EquippedWeapon.EffectID)
                                            Offerer.GetRoomUser().ApplyEffect(0);

                                        if (Offerer.GetRoomUser().CarryItemID == Offerer.GetRoleplay().EquippedWeapon.HandItem)
                                            Offerer.GetRoomUser().CarryItem(0);

                                        Offerer.GetRoleplay().CooldownManager.CreateCooldown("unequip", 1000, 3);
                                        Offerer.GetRoleplay().EquippedWeapon = null;

                                        Offerer.GetRoleplay().WLife = 0;
                                        Offerer.GetRoleplay().Bullets = 0;
                                    }
                                }
                                #endregion

                                // Cambio
                                RoleplayManager.AddWeapon(Session, weapon);
                                Session.GetRoleplay().EquippedWeapon = null;
                                Session.GetRoleplay().OwnedWeapons = null;
                                Session.GetRoleplay().OwnedWeapons = Session.GetRoleplay().LoadAndReturnWeapons();

                                RoleplayManager.DropMyWeapon(Offerer, weapon.Name);
                                Offerer.GetRoleplay().EquippedWeapon = null;
                                Offerer.GetRoleplay().OwnedWeapons = null;
                                Offerer.GetRoleplay().OwnedWeapons = Offerer.GetRoleplay().LoadAndReturnWeapons();

                                #region Equipar al comprador
                                string GunName = weapon.Name;
                                Weapon BaseWeapon = WeaponManager.getWeapon(GunName);
                                if (!Session.GetRoleplay().OwnedWeapons.ContainsKey(GunName))
                                {
                                    Session.SendWhisper("Algo ha pasado y no has recibido el arma. ((Contacta con un Administrador))", 1);
                                    return;
                                }

                                RoleplayManager.UpdateMyWeaponStats(Session, "bullets", Session.GetRoleplay().Bullets, GunName);
                                RoleplayManager.UpdateMyWeaponStats(Session, "life", Session.GetRoleplay().WLife, GunName);
                                Session.GetRoleplay().OwnedWeapons = null;
                                Session.GetRoleplay().OwnedWeapons = Session.GetRoleplay().LoadAndReturnWeapons();

                                var Weapon = Session.GetRoleplay().OwnedWeapons[GunName];

                                string EquipMessage = Weapon.EquipText;
                                EquipMessage = EquipMessage.Replace("[NAME]", Weapon.PublicName);

                                RoleplayManager.Shout(Session, EquipMessage, 5);
                                Session.SendWhisper("Has recibido un/a " + Weapon.PublicName + " con " + Session.GetRoleplay().Bullets + "/" + Weapon.ClipSize + " balas y un estado de " + Session.GetRoleplay().WLife + "/100", 1);

                                Session.GetRoleplay().EquippedWeapon = Weapon;
                                Session.GetRoleplay().Bullets = Weapon.TotalBullets;
                                Session.GetRoleplay().WLife = Weapon.WLife;

                                Session.GetRoleplay().CooldownManager.CreateCooldown("equip", 1000, 3);

                                if (Session.GetRoomUser().CurrentEffect != Weapon.EffectID)
                                    Session.GetRoomUser().ApplyEffect(Weapon.EffectID);

                                if (Session.GetRoomUser().CarryItemID != Weapon.HandItem)
                                    Session.GetRoomUser().CarryItem(Weapon.HandItem);
                                #endregion

                                Session.GetRoleplay().UpdateInteractingUserDialogues();
                                Session.GetRoleplay().RefreshStatDialogue();
                                RoleplayOffer? Junk;
                                Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove(weapon.Name.ToLower(), out Junk);
                                Offerer.SendWhisper(Session.GetHabbo().Username + " acepta tu " + weapon.PublicName + " y recibes $" + Offer.Cost, 1);
                                return;
                            }
                        }
                    }
                    else
                    {
                        Session.SendWhisper("¡No tienes una oferta de armas!", 1);
                        return;
                    }
                }
                #endregion
                */

                #region Weapons
                if (Type.ToLower() == "weapon" || Type.ToLower() == "arma" || WeaponManager.Weapons.ContainsKey(Type.ToLower()))
                {
                    if (weapon != null)
                    {
                        var Offer = Session.GetRoleplay().OfferManager.ActiveOffers[weapon.Name.ToLower()];

                        if (Offer.Params != null && Offer.Params.Length > 0)
                        {
                            RoleplayBotAI Bot = (RoleplayBotAI)Offer.Params[0];
                            if (Bot.GetRoomUser().RoomId != Room.Id)
                            {
                                RoleplayOffer Junk;
                                Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove(weapon.Name.ToLower(), out Junk);
                                Session.SendWhisper("Lo siento, no estás en la misma habitación que " + Bot.GetBotRoleplay().Name + " para aceptar la oferta del arma!", 1);
                                return;
                            }
                            else if (weapon.Stock < 1)
                            {
                                Session.SendWhisper("Lo sentimos, pero esta arma se ha agotado", 1);
                                return;
                            }
                            else if (Session.GetRoleplay().BankTarget < 1)
                            {
                                Session.SendWhisper("Lo sentimos, pero aquí solo aceptamos débito y usted no tiene tarjeta, vaya al banco y solicite", 1);
                                return;
                            }

                            else if (Session.GetRoleplay().BankChequings < Offer.Cost)
                            {
                                RoleplayOffer Junk;
                                Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove(weapon.Name.ToLower(), out Junk);
                                Session.SendWhisper("¡Lo siento, no puedes pagar una " + weapon.PublicName + " no tiene dinero en su cuenta bancaria!", 1);
                                return;
                            }
                            else
                            {
                                if (!Session.GetRoleplay().OwnedWeapons.ContainsKey(weapon.Name))
                                {
                                    Session.Shout("*Acepta la oferta de " + Bot.GetBotRoleplay().Name + " Y compra la " + weapon.PublicName + " por $" + String.Format("{0:N0}", Offer.Cost) + "*", 4);
                                    Session.GetRoleplay().BankChequings -= Offer.Cost;
                                    Session.GetHabbo().UpdateCreditsBalance();
                                    Session.GetRoomUser().ApplyEffect(603);

                                    WeaponManager.Weapons[weapon.Name].Stock--;
                                    RoleplayManager.AddWeapon(Session, weapon);
                                }
                                else
                                {
                                    Session.Shout("*Acepta la oferta de " + Bot.GetBotRoleplay().Name + " Y compra " + weapon.PublicName + " por $" + String.Format("{0:N0}", Offer.Cost) + "*", 4);
                                    Session.GetRoleplay().BankChequings -= Offer.Cost;
                                    Session.GetHabbo().UpdateCreditsBalance();
                                    Session.GetRoomUser().ApplyEffect(603);

                                    WeaponManager.Weapons[weapon.Name].Stock--;

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

                                RoleplayOffer Junk;
                                Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove(weapon.Name.ToLower(), out Junk);
                                Bot.GetRoomUser().Chat("Gracias por comprar un " + weapon.PublicName + " " + Session.GetHabbo().Username + "!", true);
                                int CAmount = (Offer.Cost * 2) / 100;
                                RoleplayManager.GiveMoneyToCompany(5, Session, "ammunation", true, CAmount);
                            }
                            return;
                        }
                        else
                        {
                            GameClient Offerer = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Offer.OffererId);
                            if (Offerer == null || Offerer.GetRoomUser().RoomId != Session.GetRoomUser().RoomId || Offerer.GetRoomUser() == null)
                            {
                                RoleplayOffer Junk;
                                Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove(weapon.Name.ToLower(), out Junk);
                                Session.SendWhisper("Lo sentimos, este usuario se ha desconectado o no está en la misma habitación que tú.", 1);
                                return;
                            }
                            else if (weapon.Stock < 1)
                            {
                                Session.SendWhisper("Lo sentimos, pero esta arma se ha agotado", 1);
                                return;
                            }
                            else if (Session.GetHabbo().Credits < Offer.Cost)
                            {
                                RoleplayOffer Junk;
                                Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove(weapon.Name.ToLower(), out Junk);
                                Session.SendWhisper("Lo siento, no puedes pagar una " + weapon.PublicName + "", 1);
                                return;
                            }
                            else
                            {
                                if (!Session.GetRoleplay().OwnedWeapons.ContainsKey(weapon.Name))
                                {
                                    Session.Shout("*Acepta la oferta de " + Offerer.GetHabbo().Username + " y compra el " + weapon.PublicName + " por $" + String.Format("{0:N0}", Offer.Cost) + "*", 4);
                                    Session.GetHabbo().Credits -= Offer.Cost;
                                    Session.GetHabbo().UpdateCreditsBalance();

                                    //WeaponManager.Weapons[weapon.Name].Stock--;
                                    RoleplayManager.AddWeapon(Session, weapon);

                                    #region Equipar al comprador
                                    string GunName = weapon.Name;
                                    Weapon BaseWeapon = WeaponManager.getWeapon(GunName);
                                    if (!Session.GetRoleplay().OwnedWeapons.ContainsKey(GunName))
                                    {
                                        Session.SendWhisper("Algo ha pasado y no has recibido el arma. ((Contacta con un Administrador))", 1);
                                        return;
                                    }

                                    RoleplayManager.UpdateMyWeaponStats(Session, "bullets", Session.GetRoleplay().Bullets, GunName);
                                    RoleplayManager.UpdateMyWeaponStats(Session, "life", 100, GunName);
                                    Session.GetRoleplay().OwnedWeapons = null;
                                    Session.GetRoleplay().OwnedWeapons = Session.GetRoleplay().LoadAndReturnWeapons();

                                    var Weapon = Session.GetRoleplay().OwnedWeapons[GunName];

                                    string EquipMessage = Weapon.EquipText;
                                    EquipMessage = EquipMessage.Replace("[NAME]", Weapon.PublicName);

                                    RoleplayManager.Shout(Session, EquipMessage, 5);
                                    Session.SendWhisper("Has recibido un/a " + Weapon.PublicName + " con " + Session.GetRoleplay().Bullets + "/" + Weapon.ClipSize + " balas y un estado de " + Session.GetRoleplay().WLife + "/100", 1);

                                    Session.GetRoleplay().EquippedWeapon = Weapon;
                                    Session.GetRoleplay().Bullets = Weapon.TotalBullets;
                                    Session.GetRoleplay().WLife = Weapon.WLife;

                                    Session.GetRoleplay().CooldownManager.CreateCooldown("equip", 1000, 3);

                                    if (Session.GetRoomUser().CurrentEffect != Weapon.EffectID)
                                        Session.GetRoomUser().ApplyEffect(Weapon.EffectID);

                                    if (Session.GetRoomUser().CarryItemID != Weapon.HandItem)
                                        Session.GetRoomUser().CarryItem(Weapon.HandItem);
                                    #endregion
                                }
                                else
                                {
                                    Session.Shout("*Acepta la oferta de " + Offerer.GetHabbo().Username + " Y compra " + weapon.PublicName + " por $" + String.Format("{0:N0}", Offer.Cost) + "*", 4);
                                    Session.GetHabbo().Credits -= Offer.Cost;
                                    Session.GetHabbo().UpdateCreditsBalance();

                                    // WeaponManager.Weapons[weapon.Name].Stock--;

                                    using (var DB = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                                    {
                                        DB.SetQuery("UPDATE `rp_weapons_owned` SET `can_use` = '1' WHERE `user_id` = @userid AND `base_weapon` = @baseweapon LIMIT 1");
                                        DB.AddParameter("userid", Session.GetHabbo().Id);
                                        DB.AddParameter("baseweapon", weapon.Name.ToLower());
                                        DB.RunQuery();
                                    }

                                    using (var DBs = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                                    {
                                        DBs.SetQuery("DELETE FROM `rp_weapons_owned` WHERE `user_id` = @userids AND `base_weapon` = @baseweapons LIMIT 1");
                                        DBs.AddParameter("userids", Offerer.GetHabbo().Id);
                                        DBs.AddParameter("baseweapons", weapon.Name.ToLower());
                                        DBs.RunQuery();
                                    }

                                    Session.GetRoleplay().EquippedWeapon = null;
                                    Session.GetRoleplay().OwnedWeapons = null;
                                    Session.GetRoleplay().OwnedWeapons = Session.GetRoleplay().LoadAndReturnWeapons();


                                    #region Equipar al comprador
                                    string GunName = weapon.Name;
                                    Weapon BaseWeapon = WeaponManager.getWeapon(GunName);
                                    if (!Session.GetRoleplay().OwnedWeapons.ContainsKey(GunName))
                                    {
                                        Session.SendWhisper("Algo ha pasado y no has recibido el arma. ((Contacta con un Administrador))", 1);
                                        return;
                                    }

                                    RoleplayManager.UpdateMyWeaponStats(Session, "bullets", Session.GetRoleplay().Bullets, GunName);
                                    RoleplayManager.UpdateMyWeaponStats(Session, "life", Session.GetRoleplay().WLife, GunName);
                                    Session.GetRoleplay().OwnedWeapons = null;
                                    Session.GetRoleplay().OwnedWeapons = Session.GetRoleplay().LoadAndReturnWeapons();

                                    var Weapon = Session.GetRoleplay().OwnedWeapons[GunName];

                                    string EquipMessage = Weapon.EquipText;
                                    EquipMessage = EquipMessage.Replace("[NAME]", Weapon.PublicName);

                                    RoleplayManager.Shout(Session, EquipMessage, 5);
                                    Session.SendWhisper("Has recibido un/a " + Weapon.PublicName + " con " + Session.GetRoleplay().Bullets + "/" + Weapon.ClipSize + " balas y un estado de " + Session.GetRoleplay().WLife + "/100", 1);

                                    Session.GetRoleplay().EquippedWeapon = Weapon;
                                    Session.GetRoleplay().Bullets = Weapon.TotalBullets;
                                    Session.GetRoleplay().WLife = Weapon.WLife;

                                    Session.GetRoleplay().CooldownManager.CreateCooldown("equip", 1000, 3);

                                    if (Session.GetRoomUser().CurrentEffect != Weapon.EffectID)
                                        Session.GetRoomUser().ApplyEffect(Weapon.EffectID);

                                    if (Session.GetRoomUser().CarryItemID != Weapon.HandItem)
                                        Session.GetRoomUser().CarryItem(Weapon.HandItem);
                                    #endregion
                                }

                                int Bonus = 200;
                                if (Offer.Cost / 20 > 500)
                                    Bonus = 500;
                                else if (Offer.Cost / 20 > 200)
                                    Bonus = 200;
                                else
                                    Bonus = Offer.Cost / 20;

                                Offerer.GetHabbo().Credits += Bonus;
                                Offerer.GetHabbo().UpdateCreditsBalance();

                                RoleplayOffer Junk;
                                Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove(weapon.Name.ToLower(), out Junk);
                                Offerer.SendWhisper("Recibes un recorte de $" + String.Format("{0:N0}", Bonus) + " por vender " + Session.GetHabbo().Username + " un " + weapon.PublicName + "!");
                                PolarEnvironment.GetGame().GetAchievementManager().ProgressAchievement(Offerer, "ACH_SellingWeapon", 1);
                                return;
                            }
                        }
                    }
                    else
                    {
                        Session.SendWhisper("¡No tienes una oferta de armas!", 1);
                        return;
                    }
                }
                #endregion

                #region Fuga
                else if (Type.ToLower() == "fuga")
                {

                    var Offer = Session.GetRoleplay().OfferManager.ActiveOffers["fuga"];
                    GameClient Offerer = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Offer.OffererId);

                    if (Offerer == null)
                    {
                        RoleplayOffer? Junk;
                        Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("fuga", out Junk);
                        Session.SendWhisper("Al parecer el cómplice se ha ido. Oferta cancelada.", 1);
                        return;
                    }

                    int Amount = Offer.Cost;

                    // Solo se puede aceptar si el usuario está encarcelado
                    if (!Session.GetRoleplay().IsJailed)
                    {
                        Session.SendWhisper("¡No estás encarcelado!", 1);
                        return;
                    }

                    if (Offerer == null || Offerer.GetRoomUser().RoomId != Session.GetRoomUser().RoomId || Offerer.GetRoomUser() == null)
                    {
                        RoleplayOffer? Junk;
                        Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("fuga", out Junk);
                        Session.SendWhisper("Lo sentimos, este usuario se ha desconectado o no está en la misma habitación que tú.", 1);
                        return;
                    }
                    else if (Session.GetHabbo().Credits < Amount)
                    {
                        RoleplayOffer? Junk;
                        Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("fuga", out Junk);
                        Session.SendWhisper("Lo siento, no puedes escapar sin pagar " + String.Format("{0:N0}", Amount) + "$", 1);
                        return;
                    }
                    else
                    {

                        // ---- Sacar de la cárcel ----
                        Session.GetRoleplay().IsJailed = false;
                        Session.GetRoleplay().JailedTimeLeft = 0;

                        if (Session.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("jail"))
                            Session.GetRoleplay().TimerManager.ActiveTimers["jail"].EndTimer();

                        // ---- Añadir a la lista de buscados (nivel 3) ----
                        int WantedLevel = 3;
                        string RoomId = Session.GetHabbo().CurrentRoomId.ToString() != "0"
                                        ? Session.GetHabbo().CurrentRoomId.ToString()
                                        : "Unknown";

                        if (Session.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("wanted"))
                            Session.GetRoleplay().TimerManager.ActiveTimers["wanted"].EndTimer();

                        if (Session.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("probation"))
                            Session.GetRoleplay().TimerManager.ActiveTimers["probation"].EndTimer();

                        Session.GetRoleplay().OnProbation = false;
                        Session.GetRoleplay().ProbationTimeLeft = 0;
                        Session.GetRoleplay().IsWanted = true;
                        Session.GetRoleplay().WantedLevel = WantedLevel;
                        Session.GetRoleplay().WantedTimeLeft = 6; // nivel 3 = 6 minutos

                        Wanted NewWanted = new Wanted(Convert.ToUInt32(Session.GetHabbo().Id), RoomId, WantedLevel);

                        if (RoleplayManager.WantedList.ContainsKey(Session.GetHabbo().Id))
                        {
                            int CurrentLevel = RoleplayManager.WantedList[Session.GetHabbo().Id].WantedLevel;
                            if (WantedLevel > CurrentLevel)
                                RoleplayManager.WantedList.TryUpdate(Session.GetHabbo().Id, NewWanted, RoleplayManager.WantedList[Session.GetHabbo().Id]);
                            else
                                Session.GetRoleplay().WantedLevel = CurrentLevel; // respetar el nivel más alto
                        }
                        else
                        {
                            RoleplayManager.WantedList.TryAdd(Session.GetHabbo().Id, NewWanted);
                        }

                        Wanted NewWanted2 = new Wanted(Convert.ToUInt32(Offerer.GetHabbo().Id), RoomId, WantedLevel);

                        if (RoleplayManager.WantedList.ContainsKey(Offerer.GetHabbo().Id))
                        {
                            int CurrentLevel = RoleplayManager.WantedList[Offerer.GetHabbo().Id].WantedLevel;
                            if (WantedLevel > CurrentLevel)
                                RoleplayManager.WantedList.TryUpdate(Offerer.GetHabbo().Id, NewWanted2, RoleplayManager.WantedList[Session.GetHabbo().Id]);
                            else
                                Offerer.GetRoleplay().WantedLevel = CurrentLevel; // respetar el nivel más alto
                        }
                        else
                        {
                            RoleplayManager.WantedList.TryAdd(Offerer.GetHabbo().Id, NewWanted2);
                        }

                        if (!Session.GetRoleplay().WantedFor.Contains("fuga"))
                            Session.GetRoleplay().WantedFor = Offerer.GetRoleplay().WantedFor + ", Por fuga, ";

                        if (!Offerer.GetRoleplay().WantedFor.Contains("fugarse"))
                            Offerer.GetRoleplay().WantedFor = Offerer.GetRoleplay().WantedFor +", Apoyo a " + Session.GetHabbo().Username + " fugarse, ";

                        Session.GetRoleplay().TimerManager.CreateTimer("wanted", 1000, false);
                        Offerer.GetRoleplay().TimerManager.CreateTimer("wanted", 1000, false);

                        // WebSocket: actualizar estrellas
                        if (Session.GetRoleplay().WebSocketConnection != null)
                            PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(Session, "compose_wanted_stars|" + Session.GetRoleplay().WantedLevel);

                        if (Offerer.GetRoleplay().WebSocketConnection != null)
                            PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(Offerer, "compose_wanted_stars|" + Offerer.GetRoleplay().WantedLevel);

                        // Shout roleplay
                        RoleplayManager.Shout(Session, "*Se fuga de la cárcel con la ayuda de " + Offerer.GetHabbo().Username + "*", 37);

                        // Notificar a ambas partes
                        Session.SendWhisper("¡Te has fugado de la cárcel! Ahora eres buscado con " + Session.GetRoleplay().WantedLevel + " estrella(s). ¡Cuidado con la policía!", 1);
                        Offerer.SendWhisper("¡" + Session.GetHabbo().Username + " ha aceptado la fuga y escapó de la cárcel! Ahora es buscado con " + Session.GetRoleplay().WantedLevel + " estrella(s).", 1);

                        // Notificación global
                        lock (PolarEnvironment.GetGame().GetClientManager().GetClients)
                        {
                            foreach (var client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                            {
                                if (client == null || client.GetHabbo() == null)
                                    continue;

                                client.SendWhisper("[NOTIFICACIÓN IMPORTANTE] ¡" + Session.GetHabbo().Username + " se ha fugado de la cárcel con ayuda de " + Offerer.GetHabbo().Username + "! ¡La policía los está buscando!", 33);
                            }
                        }

                        RoleplayOffer? FugaJunk;
                        Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("fuga", out FugaJunk);
                        return;
                    }
                }
                #endregion

                #region Reparación
                else if (Type.ToLower() == "reparacion")
                {
                    #region FixArma
                    if (Session.GetRoleplay().OfferManager.ActiveOffers.ContainsKey("fix_armero"))
                    {
                        var Offer = Session.GetRoleplay().OfferManager.ActiveOffers["fix_armero"];
                        if (Offer.Params != null && Offer.Params.Length > 0)
                        {
                            RoleplayBotAI Bot = (RoleplayBotAI)Offer.Params[0];
                            if (Bot.GetRoomUser().RoomId != Room.Id)
                            {
                                RoleplayOffer? Junk;
                                Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("fix_armero", out Junk);
                                Session.SendWhisper("Lo siento, no estás en la misma habitación que " + Bot.GetBotRoleplay().Name + " para comprar la reparacion", 1);
                                return;
                            }
                            else if (Session.GetRoleplay().BankChequings < Convert.ToInt32(Math.Floor((double)Offer.Cost / 2)))
                            {
                                RoleplayOffer? Junk;
                                Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("fix_armero", out Junk);
                                Session.SendWhisper("Lo siento, ¡no puedes permitirte aceptar la reparación!", 1);
                                return;
                            }
                            else if (Session.GetRoleplay().PassiveMode)
                            {
                                Session.SendWhisper("No puedes hacer eso mientras estás en modo pasivo.", 1);
                                return;
                            }
                            if ((Session.GetRoleplay().EquippedWeapon.CostFine / 2) != Session.GetRoleplay().ArmPiecesTo)
                            {
                                RoleplayOffer? Junk;
                                Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("fix_armero", out Junk);
                                Session.SendWhisper("Esta no es la misma arma que el Armero examinó.", 1);
                                return;
                            }
                            else
                            {
                                RoleplayOffer? Junk;
                                
                                Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("fix_armero", out Junk);
                                Bot.GetRoomUser().Chat("*Usa su destornillador para reparar el arma de " + Session.GetHabbo().Username + "*", true);

                                Session.SendWhisper("Has pagado $" + Offer.Cost + " a " + Bot.GetBotRoleplay().Name + " por reparar tu arma.", 1);
                                
                                Session.GetHabbo().Credits -= Offer.Cost;
                                Session.GetHabbo().UpdateCreditsBalance();
                                RoleplayManager.UpdateMyWeaponStats(Session, "life", 100, Session.GetRoleplay().EquippedWeapon.Name);
                                Session.GetRoleplay().WLife = 100;
                                RoleplayManager.UpdateMyWeaponStats(Session, "life", Session.GetRoleplay().WLife, Session.GetRoleplay().EquippedWeapon.Name);
                                Session.GetRoleplay().OwnedWeapons = null;
                                Session.GetRoleplay().OwnedWeapons = Session.GetRoleplay().LoadAndReturnWeapons();
                                int CAmount = (Offer.Cost * 2) / 100;
                                RoleplayManager.GiveMoneyToCompany(5, Session, "ammunation", true, CAmount);
                                return;
                            }
                        }
                        else { 
                            GameClient Offerer = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Offer.OffererId);
                            if (Offerer == null || Offerer.GetRoomUser().RoomId != Session.GetRoomUser().RoomId || Offerer.GetRoomUser() == null)
                            {
                                RoleplayOffer? Junk;
                                Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("fix_armero", out Junk);
                                Session.SendWhisper("Lo sentimos, el armero no está en la misma zona que tú.", 1);
                                return;
                            }
                            else if (Offerer.GetRoleplay().ArmUserTo != Session.GetHabbo().Id)
                            {
                                RoleplayOffer? Junk;
                                Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("fix_armero", out Junk);
                                Session.SendWhisper("El Armero ya no recuerda cuantas piezas usar. Dile que vuelva a revisar.", 1);
                                return;
                            }
                            else if (Offerer.GetRoleplay().ArmPieces < Offerer.GetRoleplay().ArmPiecesTo)
                            {
                                RoleplayOffer? Junk;
                                Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("fix_armero", out Junk);
                                Session.SendWhisper("El Armero ya no tiene las piezas suficientes para la reparación.", 1);
                                return;
                            }
                            else if (Session.GetHabbo().Credits < Offer.Cost)
                            {
                                RoleplayOffer? Junk;
                                Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("fix_armero", out Junk);
                                Session.SendWhisper("¡Necesitas $" + Offer.Cost + " para aceptar la reparación!", 1);
                                return;
                            }
                            else if (Session.GetRoleplay().PassiveMode)
                            {
                                Session.SendWhisper("No puedes hacer eso mientras estás en modo pasivo.", 1);
                                return;
                            }
                            else if (Offerer.GetRoleplay().PassiveMode)
                            {
                                Session.SendWhisper("No puedes aceptarle eso a una persona en modo pasivo.", 1);
                                return;
                            }
                            if (Session.GetRoleplay().EquippedWeapon == null)
                            {
                                Session.SendWhisper("Debes tener Equipada el arma a ser reparada.", 1);
                                return;
                            }
                            if (Session.GetRoleplay().WLife > 0)
                            {
                                Session.SendWhisper("Esa arma no necesita una reparación.", 1);
                                return;
                            }
                            if ((Session.GetRoleplay().EquippedWeapon.CostFine / 2) != Offerer.GetRoleplay().ArmPiecesTo)
                            {
                                RoleplayOffer? Junk;
                                Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("fix_armero", out Junk);
                                Session.SendWhisper("Esta no es la misma arma que el Armero examinó.", 1);
                                return;
                            }
                            else
                            {
                                RoleplayManager.Shout(Session, "*Acepta la oferta de Reparación de arma a " + Offerer.GetHabbo().Username + " por $" + String.Format("{0:N0}", Offer.Cost) + "*", 5);
                                RoleplayOffer? Junk;
                                Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("fix_armero", out Junk);

                                RoleplayManager.Shout(Offerer, "*Usa su destornillador para reparar el arma de " + Session.GetHabbo().Username + "*", 5);
                                Offerer.SendWhisper("¡Buen trabajo! Has usado " + Offerer.GetRoleplay().ArmPiecesTo + " pieza(s) por reparar el arma y tus ganancias son: $" + Offer.Cost, 1);
                                Session.SendWhisper("Has pagado $" + Offer.Cost + " a " + Offerer.GetHabbo().Username + " por reparar tu arma.", 1);
                                Offerer.GetRoleplay().ArmPieces -= Offerer.GetRoleplay().ArmPiecesTo;
                                RoleplayManager.UpdateMyWeaponStats(Session, "life", 100, Session.GetRoleplay().EquippedWeapon.Name);
                                Session.GetRoleplay().WLife = 100;
                                RoleplayManager.UpdateMyWeaponStats(Session, "life", Session.GetRoleplay().WLife, Session.GetRoleplay().EquippedWeapon.Name);
                                Session.GetRoleplay().OwnedWeapons = null;
                                Session.GetRoleplay().OwnedWeapons = Session.GetRoleplay().LoadAndReturnWeapons();
                                Session.GetHabbo().Credits -= Offer.Cost;
                                Session.GetHabbo().UpdateCreditsBalance();
                                Offerer.GetHabbo().Credits += Offer.Cost;
                                Offerer.GetHabbo().UpdateCreditsBalance();
                            }
                        }
                    }
                    #endregion

                    #region FixCar
                    else if (Session.GetRoleplay().OfferManager.ActiveOffers.ContainsKey("fix_mecanico"))
                    {
                        var Offer2 = Session.GetRoleplay().OfferManager.ActiveOffers["fix_mecanico"];
                        if (Offer2 != null)
                        {
                            GameClient Offerer = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Offer2.OffererId);
                            if (Offerer == null || Offerer.GetRoomUser().RoomId != Session.GetRoomUser().RoomId || Offerer.GetRoomUser() == null)
                            {
                                RoleplayOffer? Junk;
                                Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("fix_mecanico", out Junk);
                                Session.SendWhisper("Lo sentimos, el ofertante no está en la misma zona que tú.", 1);
                                return;
                            }
                            else if (Session.GetRoleplay().Level < 2)
                            {
                                RoleplayOffer? Junk;
                                Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("fix_mecanico", out Junk);
                                Session.SendWhisper("((Lo sentimos, no puedes aceptar los servicios de un mecánico hasta el nivel 2.))", 1);
                                return;
                            }
                            else if (Session.GetHabbo().Credits < Offer2.Cost)
                            {
                                RoleplayOffer? Junk;
                                Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("fix_mecanico", out Junk);
                                Session.SendWhisper("¡Necesitas $" + Offer2.Cost + " para aceptar la reparación!", 1);
                                return;
                            }
                            else if (Offerer.GetRoomUser().Coordinate != Offerer.GetRoleplay().MecCordinates)
                            {
                                Session.SendWhisper("¡El mecánico no se encuentra frente a tu Vehículo! Pídele que vuelva a la posición donde te ofreció sus servicios para poder aceptar o usa ':rechazar reparacion'", 1);
                                return;
                            }
                            else
                            {
                                #region Check Vehicle InFront
                                int FuelSize = 0;
                                Vehicle? vehicle;
                                bool found = false;
                                int itemfurni = 0, corp = 0;
                                Item? BTile;
                                string? itemnm;
                                foreach (Vehicle Vehicle in VehicleManager.Vehicles.Values)
                                {
                                    if (!found)
                                    {
                                        BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == Vehicle.ItemName && x.Coordinate == Offerer.GetRoomUser().SquareInFront);
                                        if (BTile != null)
                                        {
                                            vehicle = Vehicle;
                                            itemfurni = BTile.Id;
                                            itemnm = Vehicle.ItemName;
                                            corp = Convert.ToInt32(Vehicle.CarCorp);
                                            found = true;
                                        }
                                    }
                                }

                                if (!found)
                                {
                                    RoleplayOffer? Junk2;
                                    Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("fix_mecanico", out Junk2);
                                    Session.SendWhisper("¡El Mecánico debe estar frente al vehículo a reparar!", 1);
                                    return;
                                }

                                #endregion

                                #region Select Vehicle State
                                int state = 0;
                                List<VehiclesOwned> VO = PolarEnvironment.GetGame().GetVehiclesOwnedManager().getVehiclesOwnedByFurniId(itemfurni);
                                if (VO == null || VO.Count <= 0)
                                {
                                    RoleplayOffer? Junk3;
                                    Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("fix_mecanico", out Junk3);
                                    Session.SendWhisper("¡El Mecánico debe estar frente a un vehículo que requiera reparacion!", 1);
                                    return;
                                }
                                state = VO[0].State;
                                #endregion

                                #region Check Vehicle State
                                if (state == 2 || state == 3 || VO[0].CarLife <= 0)
                                {
                                    if (state == 2)
                                        Offerer.GetRoleplay().MecNewState = 0;// óptimo y sin traba
                                    else
                                        Offerer.GetRoleplay().MecNewState = 1;// óptimo y con traba
                                }
                                else
                                {
                                    RoleplayOffer? Junk4;
                                    Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("fix_mecanico", out Junk4);
                                    Session.SendWhisper("Este vehículo no necesita ser reparado.", 1);
                                    return;
                                }
                                #endregion

                                #region Calc Repair Kit Cant
                                if (FuelSize <= 90)// Tanque Pequeño
                                {
                                    Offerer.GetRoleplay().MecPartsTo = 3;
                                }
                                else if (FuelSize > 90 && FuelSize <= 100)// Tanque Mediano
                                {
                                    Offerer.GetRoleplay().MecPartsTo = 6;
                                }
                                else // Tanque Grande
                                {
                                    Offerer.GetRoleplay().MecPartsTo = 9;
                                }
                                #endregion

                                if (Offerer.GetRoleplay().MecParts < Offerer.GetRoleplay().MecPartsTo)
                                {
                                    RoleplayOffer? Junk4;
                                    Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("fix_mecanico", out Junk4);
                                    Session.SendWhisper("El mecánico ya no cuenta con los repuestos suficientes.", 1);
                                    return;
                                }

                                RoleplayManager.Shout(Session, "*Acepta la oferta de Reparación de " + Offerer.GetHabbo().Username + " por $" + String.Format("{0:N0}", Offer2.Cost) + "*", 5);
                                RoleplayOffer? Junk;
                                Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("fix_mecanico", out Junk);

                                // Iniciar Timer al Mecánico
                                Offerer.GetRoleplay().MecUserToRepair = Session.GetHabbo().Id;
                                Offerer.GetRoleplay().MecPriceTo = Offer2.Cost;
                                Offerer.GetRoleplay().IsMecLoading = true;
                                Offerer.GetRoleplay().LoadingTimeLeft = RoleplayManager.GetTimerByMyJob(Offerer, "mecanico"); // Depende nivel del Mecánico

                                #region In State Mecánico
                                if (!Offerer.GetRoomUser().isSitting)
                                {
                                    Offerer.GetRoomUser().SetRot(Offerer.GetRoleplay().MecRotPosition, false);
                                    #region Sit
                                    if (!Offerer.GetRoomUser().Statusses.ContainsKey("sit"))
                                    {
                                        if ((Offerer.GetRoomUser().RotBody % 2) == 0)
                                        {
                                            try
                                            {
                                                Offerer.GetRoomUser().Statusses.Add("sit", "1.0");
                                                Offerer.GetRoomUser().Z -= 0.35;
                                                Offerer.GetRoomUser().isSitting = true;
                                                Offerer.GetRoomUser().UpdateNeeded = true;
                                            }
                                            catch { }
                                        }
                                        else
                                        {
                                            Offerer.GetRoomUser().RotBody--;
                                            Offerer.GetRoomUser().Statusses.Add("sit", "1.0");
                                            Offerer.GetRoomUser().Z -= 0.35;
                                            Offerer.GetRoomUser().isSitting = true;
                                            Offerer.GetRoomUser().UpdateNeeded = true;
                                        }
                                    }
                                    else if (Offerer.GetRoomUser().isSitting == true)
                                    {
                                        Offerer.GetRoomUser().Z += 0.35;
                                        Offerer.GetRoomUser().Statusses.Remove("sit");
                                        Offerer.GetRoomUser().Statusses.Remove("1.0");
                                        Offerer.GetRoomUser().isSitting = false;
                                        Offerer.GetRoomUser().UpdateNeeded = true;
                                    }
                                    #endregion
                                }
                                #endregion

                                RoleplayManager.Shout(Offerer, "*Saca sus herramientas y comienza a reparar el vehículo de " + Session.GetHabbo().Username + "*", 5);
                                Offerer.SendWhisper("Debes esperar " + Offerer.GetRoleplay().LoadingTimeLeft + " segundo(s)...", 1);
                                Offerer.GetRoleplay().TimerManager.CreateTimer("general", 1000, true);
                                return;
                            }
                        }
                    }
                    #endregion

                    else
                        Session.SendWhisper("No tienes ninguna oferta de reparación pendiente.", 1);
                    return;
                }
                #endregion

                #region Bullets
                else if (Type.ToLower() == "balas" || Type.ToLower() == "bullets")
                {
                    var Offer = Session.GetRoleplay().OfferManager.ActiveOffers[Type.ToLower()];
                    if (Offer.Params != null && Offer.Params.Length > 0)
                    {
                        RoleplayBotAI Bot = (RoleplayBotAI)Offer.Params[0];
                        if (Bot.GetRoomUser().RoomId != Room.Id)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("balas", out Junk);
                            Session.SendWhisper("Lo siento, no estás en la misma habitación que " + Bot.GetBotRoleplay().Name + " para comprar las balas", 1);
                            return;
                        }
                        else if (Session.GetRoleplay().BankChequings < Convert.ToInt32(Math.Floor((double)Offer.Cost / 2)))
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("balas", out Junk);
                            Session.SendWhisper("Lo siento, ¡no puedes permitirte balas!", 1);
                            return;
                        }
                        else
                        {
                            Session.Shout("*Acepta la oferta de " + Bot.GetBotRoleplay().Name + " y compra " + String.Format("{0:N0}", Offer.Cost) + " balas por $" + String.Format("{0:N0}", Convert.ToInt32(Math.Floor((double)Offer.Cost / 2))) + " con su tarjeta de débito*", 4);
                            Session.GetRoleplay().BankChequings -= Convert.ToInt32(Math.Floor((double)Offer.Cost / 2));
                            Session.GetHabbo().UpdateCreditsBalance();
                            Session.GetRoomUser().ApplyEffect(603);
                            Session.GetRoleplay().Bullets += Offer.Cost;

                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("balas", out Junk);
                            Bot.GetRoomUser().Chat("Gracias por comprar " + String.Format("{0:N0}", Offer.Cost) + " balas " + Session.GetHabbo().Username + "!", true);
                            //RoleplayManager.GiveMoneyFromCompanyNoRight(5, Offer.Cost, Session);
                            //RoleplayManager.UpdateMyWeaponStats(Session, "bullets", Session.GetRoleplay().Bullets, Session.GetRoleplay().EquippedWeapon.Name);
                            Session.GetRoleplay().OwnedWeapons = null;
                            Session.GetRoleplay().OwnedWeapons = Session.GetRoleplay().LoadAndReturnWeapons();
                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_charweapons", "");
                            Session.GetRoleplay().UpdateInteractingUserDialogues();
                            Session.GetRoleplay().RefreshStatDialogue();
                            int CAmount = (Offer.Cost * 2) / 100;
                            RoleplayManager.GiveMoneyToCompany(5, Session, "ammunation", true, CAmount);
                            return;
                        }
                    }
                    else
                    {
                        GameClient Offerer = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Offer.OffererId);
                        if (Offerer == null || Offerer.GetRoomUser().RoomId != Session.GetRoomUser().RoomId || Offerer.GetRoomUser() == null)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("balas", out Junk);
                            Session.SendWhisper("Lo sentimos, este usuario se ha desconectado o no está en la misma habitación que tú.", 1);
                            return;
                        }
                        else if (Session.GetHabbo().Credits < Convert.ToInt32(Math.Floor((double)Offer.Cost / 2)))
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("balas", out Junk);
                            Session.SendWhisper("Usted no puede pagar balas", 1);
                            return;
                        }
                        else
                        {
                            Session.Shout("*Acepta la oferta de " + Offerer.GetHabbo().Username + " y compra " + String.Format("{0:N0}", Offer.Cost) + " balas por $" + String.Format("{0:N0}", Convert.ToInt32(Math.Floor((double)Offer.Cost / 2))) + " con su tarjeta de débito*", 4);
                            Session.GetRoleplay().BankChequings -= Convert.ToInt32(Math.Floor((double)Offer.Cost / 2));
                            Session.GetHabbo().UpdateCreditsBalance();
                            Session.GetRoomUser().ApplyEffect(603);
                            Session.GetRoleplay().Bullets += Offer.Cost;
                            //RoleplayManager.UpdateMyWeaponStats(Session, "bullets", Session.GetRoleplay().Bullets, Session.GetRoleplay().EquippedWeapon.Name);
                            Session.GetRoleplay().OwnedWeapons = null;
                            Session.GetRoleplay().OwnedWeapons = Session.GetRoleplay().LoadAndReturnWeapons();
                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_charweapons", "");
                            Session.GetRoleplay().UpdateInteractingUserDialogues();
                            Session.GetRoleplay().RefreshStatDialogue();
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("balas", out Junk);
                            return;
                        }
                    }
                }
                #endregion

                #region Seeds
                else if (Type.ToLower() == "semillas" || Type.ToLower() == "seeds")
                {
                    var Offer = Session.GetRoleplay().OfferManager.ActiveOffers[Type.ToLower()];
                    if (Offer.Params.ToList().Count > 1)
                    {
                        RoleplayBotAI Bot = (RoleplayBotAI)Offer.Params[0];
                        FarmingItem Item = (FarmingItem)Offer.Params[1];

                        ItemData Furni;
                        if (!PolarEnvironment.GetGame().GetItemManager().GetItem(Item.BaseItem, out Furni))
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("semillas", out Junk);
                            Session.SendWhisper("Lo sentimos, este artículo no existe", 1);
                            return;
                        }

                        int Amount = Offer.Cost;
                        int Cost = (Amount * Item.BuyPrice);

                        if (Bot.GetRoomUser().RoomId != Room.Id)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("semillas", out Junk);
                            Session.SendWhisper("Lo siento, no estás en la misma habitación que " + Bot.GetBotRoleplay().Name + " para comprar semillas", 1);
                            return;
                        }
                        else if (Session.GetHabbo().Credits < Cost)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("semillas", out Junk);
                            Session.SendWhisper("Lo siento, no te puedes permitir " + String.Format("{0:N0}", Amount) + " " + Furni.PublicName + " semillas", 1);
                            return;
                        }
                        else
                        {
                            Session.Shout("*Acepta la oferta de " + Bot.GetBotRoleplay().Name + " y compra " + String.Format("{0:N0}", Amount) + " " + Furni.PublicName + " semillas*", 4);
                            Session.GetHabbo().Credits -= Cost;
                            Session.GetHabbo().UpdateCreditsBalance();
                            FarmingManager.IncreaseSatchelCount(Session, Item, Amount, false);

                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("semillas", out Junk);
                            Bot.GetRoomUser().Chat("Gracias por su compra  " + Session.GetHabbo().Username + "!", true);
                            RoleplayManager.GiveMoneyFromCompanyNoRight(5, Offer.Cost, Session);
                            return;
                        }
                    }
                    else
                    {
                        FarmingItem Item = (FarmingItem)Offer.Params[0];
                        GameClient Offerer = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Offer.OffererId);

                        ItemData Furni;
                        if (!PolarEnvironment.GetGame().GetItemManager().GetItem(Item.BaseItem, out Furni))
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("semillas", out Junk);
                            Session.SendWhisper("Lo sentimos, este artículo no existe", 1);
                            return;
                        }

                        int Amount = Offer.Cost;
                        int Cost = (Amount * Item.BuyPrice);

                        if (Offerer == null || Offerer.GetRoomUser().RoomId != Session.GetRoomUser().RoomId || Offerer.GetRoomUser() == null)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("semillas", out Junk);
                            Session.SendWhisper("Lo sentimos, este usuario se ha desconectado o no está en la misma habitación que tú.", 1);
                            return;
                        }
                        else if (Session.GetHabbo().Credits < Cost)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("semillas", out Junk);
                            Session.SendWhisper("Lo siento, no puedes permitirte " + Amount + " " + Furni.PublicName + " semillas", 1);
                            return;
                        }
                        else
                        {
                            Session.Shout("*Acepta la oferta de " + Offerer.GetHabbo().Username + " y compra " + String.Format("{0:N0}", Amount) + " " + Furni.PublicName + " semillas*", 4);
                            Session.GetHabbo().Credits -= Cost;
                            Session.GetHabbo().UpdateCreditsBalance();
                            FarmingManager.IncreaseSatchelCount(Session, Item, Amount, false);

                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("semillas", out Junk);
                            return;
                        }
                    }
                }
                #endregion

                #region Seed Satchel
                else if (Type.ToLower() == "bolsasemillas" || Type.ToLower() == "seedsatchel")
                {
                    var Offer = Session.GetRoleplay().OfferManager.ActiveOffers[Type.ToLower()];
                    if (Offer.Params != null && Offer.Params.Length > 0)
                    {
                        RoleplayBotAI Bot = (RoleplayBotAI)Offer.Params[0];
                        if (Session.GetRoleplay().FarmingStats.HasSeedSatchel)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("bolsasemillas", out Junk);
                            Session.SendWhisper("Ya tienes la bolsa de semillas", 1);
                            return;
                        }
                        else if (Bot.GetRoomUser().RoomId != Room.Id)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("bolsasemillas", out Junk);
                            Session.SendWhisper("Lo siento, no estás en la misma habitación que " + Bot.GetBotRoleplay().Name + " para aceptar la oferta de la bolsa de semillas!", 1);
                            return;
                        }
                        else if (Session.GetHabbo().Credits < Offer.Cost)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("bolsasemillas", out Junk);
                            Session.SendWhisper("Lo sentimos, no puede permitirse una bolsa de semillas", 1);
                            return;
                        }
                        else
                        {
                            Session.Shout("*Acepta la oferta de " + Bot.GetBotRoleplay().Name + " y compra una bolsa de semillas por $" + String.Format("{0:N0}", Offer.Cost) + "*", 4);
                            Session.GetHabbo().Credits -= Offer.Cost;
                            Session.GetHabbo().UpdateCreditsBalance();
                            Session.GetRoleplay().FarmingStats.HasSeedSatchel = true;

                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("bolsasemillas", out Junk);
                            Bot.GetRoomUser().Chat("Gracias por comprar una bolsa de semillas, " + Session.GetHabbo().Username + "!", true);
                            RoleplayManager.GiveMoneyFromCompanyNoRight(12, Offer.Cost, Session);
                            return;
                        }
                    }
                    else
                    {
                        GameClient Offerer = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Offer.OffererId);
                        if (Session.GetRoleplay().FarmingStats.HasSeedSatchel)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("bolsasemillas", out Junk);
                            Session.SendWhisper("Ya tienes una", 1);
                            return;
                        }
                        else if (Offerer == null || Offerer.GetRoomUser().RoomId != Session.GetRoomUser().RoomId || Offerer.GetRoomUser() == null)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("bolsasemillas", out Junk);
                            Session.SendWhisper("Lo sentimos, este usuario se ha desconectado o no está en la misma habitación que tú.", 1);
                            return;
                        }
                        else if (Session.GetHabbo().Credits < Offer.Cost)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("bolsasemillas", out Junk);
                            Session.SendWhisper("Lo siento, no puede pagar una bolsa de semillas!", 1);
                            return;
                        }
                        else
                        {
                            Session.Shout("*Acepta la oferta de " + Offerer.GetHabbo().Username + " y compra una bolsa de semillas por $" + String.Format("{0:N0}", Offer.Cost) + "*", 4);
                            Session.GetHabbo().Credits -= Offer.Cost;
                            Session.GetHabbo().UpdateCreditsBalance();
                            Session.GetRoleplay().FarmingStats.HasSeedSatchel = true;

                            Offerer.GetHabbo().Credits += Offer.Cost / 10;
                            Offerer.GetHabbo().UpdateCreditsBalance();

                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("bolsasemillas", out Junk);
                            Offerer.SendWhisper("Recibes un recorte de $" + String.Format("{0:N0}", (Offer.Cost / 10)) + " por venderle a " + Session.GetHabbo().Username + " una bolsa de semillas!");
                            PolarEnvironment.GetGame().GetAchievementManager().ProgressAchievement(Offerer, "ACH_SellingSatchel", 1);
                            return;
                        }
                    }
                }
                #endregion

                #region Plant Satchel
                else if (Type.ToLower() == "bolsavegetal" || Type.ToLower() == "plantsatchel")
                {
                    var Offer = Session.GetRoleplay().OfferManager.ActiveOffers[Type.ToLower()];
                    if (Offer.Params != null && Offer.Params.Length > 0)
                    {
                        RoleplayBotAI Bot = (RoleplayBotAI)Offer.Params[0];
                        if (Session.GetRoleplay().FarmingStats.HasPlantSatchel)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("bolsavegetal", out Junk);
                            Session.SendWhisper("Ya tienes bolsa vegetal", 1);
                            return;
                        }
                        else if (Bot.GetRoomUser().RoomId != Room.Id)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("bolsavegetal", out Junk);
                            Session.SendWhisper("Lo siento, no estás en la misma habitación que " + Bot.GetBotRoleplay().Name + " para aceptar la oferta de la bolsa vegetal!", 1);
                            return;
                        }
                        else if (Session.GetHabbo().Credits < Offer.Cost)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("bolsavegetal", out Junk);
                            Session.SendWhisper("¡Lo siento, no puedes pagarte una Plant Satchel!", 1);
                            return;
                        }
                        else
                        {
                            Session.Shout("*Acepta la oferta de " + Bot.GetBotRoleplay().Name + " y compra a Plant Satchel por $" + String.Format("{0:N0}", Offer.Cost) + "*", 4);
                            Session.GetHabbo().Credits -= Offer.Cost;
                            Session.GetHabbo().UpdateCreditsBalance();
                            Session.GetRoleplay().FarmingStats.HasPlantSatchel = true;

                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("bolsavegetal", out Junk);
                            Bot.GetRoomUser().Chat("Gracias por comprar una bolsa vegetal " + Session.GetHabbo().Username + "!", true);
                            RoleplayManager.GiveMoneyFromCompanyNoRight(12, Offer.Cost, Session);
                            return;
                        }
                    }
                    else
                    {
                        GameClient Offerer = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Offer.OffererId);
                        if (Session.GetRoleplay().FarmingStats.HasPlantSatchel)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("bolsavegetal", out Junk);
                            Session.SendWhisper("Ya tienes una bolsa vegetal", 1);
                            return;
                        }
                        else if (Offerer == null || Offerer.GetRoomUser().RoomId != Session.GetRoomUser().RoomId || Offerer.GetRoomUser() == null)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("bolsavegetal", out Junk);
                            Session.SendWhisper("Lo sentimos, este usuario se ha desconectado o no está en la misma habitación que tú.", 1);
                            return;
                        }
                        else if (Session.GetHabbo().Credits < Offer.Cost)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("bolsavegetal", out Junk);
                            Session.SendWhisper("Lo siento, no puedes pagar una bolsa vegetal!", 1);
                            return;
                        }
                        else
                        {
                            Session.Shout("*Acepta la oferta de " + Offerer.GetHabbo().Username + " y compra a bolsa vegetal por $" + String.Format("{0:N0}", Offer.Cost) + "*", 4);
                            Session.GetHabbo().Credits -= Offer.Cost;
                            Session.GetHabbo().UpdateCreditsBalance();
                            Session.GetRoleplay().FarmingStats.HasPlantSatchel = true;

                            Offerer.GetHabbo().Credits += Offer.Cost / 10;
                            Offerer.GetHabbo().UpdateCreditsBalance();

                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("bolsavegetal", out Junk);
                            Offerer.SendWhisper("Recibes un recorte de $" + String.Format("{0:N0}", (Offer.Cost / 10)) + " por venderle a " + Session.GetHabbo().Username + " a Plant Satchel!");
                            PolarEnvironment.GetGame().GetAchievementManager().ProgressAchievement(Offerer, "ACH_SellingSatchel", 1);
                            return;
                        }
                    }
                }
                #endregion

                #region Cigarrettes
                else if (Type.ToLower() == "cigarrillos")
                {
                    var Offer = Session.GetRoleplay().OfferManager.ActiveOffers[Type.ToLower()];
                    if (Offer.Params != null && Offer.Params.Length > 0)
                    {
                        RoleplayBotAI Bot = (RoleplayBotAI)Offer.Params[0];
                        if (Bot.GetRoomUser().RoomId != Room.Id)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("cigarrillos", out Junk);
                            Session.SendWhisper("Lo siento, no estás en la misma habitación que " + Bot.GetBotRoleplay().Name + " para aceptar la oferta de los cigarros!", 1);
                            return;
                        }
                        else if (Session.GetHabbo().Credits < Offer.Cost)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("cigarrillos", out Junk);
                            Session.SendWhisper("Lo siento, no puedes pagar una caja de cigarrillos", 1);
                            return;
                        }
                        else
                        {
                            Session.Shout("*Acepta la oferta de " + Bot.GetBotRoleplay().Name + " Y una caja de cigarrillos por $" + String.Format("{0:N0}", Offer.Cost) + "*", 4);
                            Session.GetHabbo().Credits -= Offer.Cost;
                            Session.GetHabbo().UpdateCreditsBalance();
                            Session.GetRoleplay().Cigarettes += 10;
                            return;
                        }
                    }
                    else
                    {
                        GameClient Offerer = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Offer.OffererId);
                        if (Offerer == null || Offerer.GetRoomUser().RoomId != Session.GetRoomUser().RoomId || Offerer.GetRoomUser() == null)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("cigarrillos", out Junk);
                            Session.SendWhisper("Lo sentimos, este usuario se ha desconectado o no está en la misma habitación que tú.", 1);
                            return;
                        }
                        else if (Session.GetHabbo().Credits < Offer.Cost)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("cigarrillos", out Junk);
                            Session.SendWhisper("Lo siento, no puedes pagar una caja de cigarrillos", 1);
                            return;
                        }
                        else
                        {
                            Session.Shout("*Acepta la oferta de " + Offerer.GetHabbo().Username + " Y compra una caja de cigarrillos $" + String.Format("{0:N0}", Offer.Cost) + "*", 4);
                            Session.GetHabbo().Credits -= Offer.Cost;
                            Session.GetHabbo().UpdateCreditsBalance();
                            Session.GetRoleplay().Cigarettes += 10;

                            Offerer.GetHabbo().Credits += Offer.Cost / 20;
                            Offerer.GetHabbo().UpdateCreditsBalance();

                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("cigarrillos", out Junk);
                            Offerer.SendWhisper("Recibes un recorte de $" + String.Format("{0:N0}", (Offer.Cost / 20)) + " por venderle a " + Session.GetHabbo().Username + " unos " + Offer.Type + "!");
                            PolarEnvironment.GetGame().GetAchievementManager().ProgressAchievement(Offerer, "ACH_SellingCar", 1);
                            return;
                        }
                    }
                }
                #endregion

                #region Pildoras
                else if (Type.ToLower() == "pildoras")
                {
                    var Offer = Session.GetRoleplay().OfferManager.ActiveOffers[Type.ToLower()];
                    if (Offer.Params != null && Offer.Params.Length > 0)
                    {
                        RoleplayBotAI Bot = (RoleplayBotAI)Offer.Params[0];
                        if (Bot.GetRoomUser().RoomId != Room.Id)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("pildoras", out Junk);
                            Session.SendWhisper("Lo siento, no estás en la misma habitación que " + Bot.GetBotRoleplay().Name + " para aceptar la oferta de pildoras!", 1);
                            return;
                        }
                        else if (Session.GetHabbo().Credits < Offer.Cost)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("pildoras", out Junk);
                            Session.SendWhisper("Lo siento, no puedes pagar una pildora", 1);
                            return;
                        }
                        else
                        {
                            Session.Shout("*Acepta la oferta de " + Bot.GetBotRoleplay().Name + " y le da una pildora por $" + String.Format("{0:N0}", Offer.Cost) + "*", 4);
                            Session.GetHabbo().Credits -= Offer.Cost;
                            Session.GetHabbo().UpdateCreditsBalance();
                            Session.GetRoleplay().Pildoras += 1;
                            return;
                        }
                    }
                    else
                    {
                        GameClient Offerer = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Offer.OffererId);
                        if (Offerer == null || Offerer.GetRoomUser().RoomId != Session.GetRoomUser().RoomId || Offerer.GetRoomUser() == null)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("pildoras", out Junk);
                            Session.SendWhisper("Lo sentimos, este usuario se ha desconectado o no está en la misma habitación que tú.", 1);
                            return;
                        }
                        else if (Session.GetHabbo().Credits < Offer.Cost)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("pildoras", out Junk);
                            Session.SendWhisper("Lo siento, no puedes pagar una pildora", 1);
                            return;
                        }
                        else
                        {
                            Session.Shout("*Acepta la oferta de " + Offerer.GetHabbo().Username + " Y compra una pildora de fuerza $" + String.Format("{0:N0}", Offer.Cost) + "*", 4);
                            Session.GetHabbo().Credits -= Offer.Cost;
                            Session.GetHabbo().UpdateCreditsBalance();
                            Session.GetRoleplay().Pildoras += 1;

                            Offerer.GetHabbo().Credits += Offer.Cost / 300;
                            Offerer.GetHabbo().UpdateCreditsBalance();

                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("pildoras", out Junk);
                            Offerer.SendWhisper("Recibes un recorte de $" + String.Format("{0:N0}", (Offer.Cost / 20)) + " por venderle a " + Session.GetHabbo().Username + " unos " + Offer.Type + "!");
                            PolarEnvironment.GetGame().GetAchievementManager().ProgressAchievement(Offerer, "ACH_SellingCar", 1);
                            return;
                        }
                    }
                }
                #endregion

                #region Cocaina
                else if (Type.ToLower() == "cocaina")
                {
                    var Offer = Session.GetRoleplay().OfferManager.ActiveOffers[Type.ToLower()];
                    int Cant = Convert.ToInt32(Offer.Params[0]);
                    if (Offer.Params != null && Offer.Params.Length > 0 && Cant < 0)
                    {
                        RoleplayBotAI Bot = (RoleplayBotAI)Offer.Params[0];
                        if (Bot.GetRoomUser().RoomId != Room.Id)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("cocaina", out Junk);
                            Session.SendWhisper("Lo siento, no estás en la misma habitación que " + Bot.GetBotRoleplay().Name + " para aceptar la oferta de la cocaina!", 1);
                            return;
                        }
                        else if (Session.GetHabbo().Credits < Offer.Cost)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("cocaina", out Junk);
                            Session.SendWhisper("Lo siento, no puedes pagar 5g de cocaina", 1);
                            return;
                        }
                        else
                        {
                            Session.Shout("*Acepta la oferta de " + Bot.GetBotRoleplay().Name + " 5g de cocaina por $" + String.Format("{0:N0}", Offer.Cost) + "*", 4);
                            Session.GetHabbo().Credits -= Offer.Cost;
                            Session.GetHabbo().UpdateCreditsBalance();
                            Session.GetRoleplay().Cocaine += 5;
                            return;
                        }
                    }
                    else
                    {
                        GameClient Offerer = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Offer.OffererId);
                        if (Offerer == null || Offerer.GetRoomUser().RoomId != Session.GetRoomUser().RoomId || Offerer.GetRoomUser() == null)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("cocaina", out Junk);
                            Session.SendWhisper("Lo sentimos, este usuario se ha desconectado o no está en la misma habitación que tú.", 1);
                            return;
                        }
                        else if (Session.GetHabbo().Credits < Offer.Cost)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("cocaina", out Junk);
                            Session.SendWhisper("Lo siento, no puedes pagar "+Cant+"g de cocaina", 1);
                            return;
                        }
                        else
                        {
                            Session.Shout("*Acepta la oferta de " + Offerer.GetHabbo().Username + " Y compra "+Cant+"g de cocaina $" + String.Format("{0:N0}", Offer.Cost) + "*", 4);
                            Session.GetHabbo().Credits -= Offer.Cost;
                            Session.GetHabbo().UpdateCreditsBalance();
                            Session.GetRoleplay().Cocaine += Cant;
                            Offerer.GetRoleplay().Cocaine -= Cant;

                            Offerer.GetHabbo().Credits += Offer.Cost;
                            Offerer.GetHabbo().UpdateCreditsBalance();

                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("cocaina", out Junk);
                            Offerer.SendWhisper("Recibes $" + String.Format("{0:N0}", (Offer.Cost)) + " por venderle a " + Session.GetHabbo().Username + " " + Offer.Type + "!");
                            return;
                        }
                    }
                }
                #endregion

                #region Caramelos
                else if (Type.ToLower() == "caramelos")
                {
                    var Offer = Session.GetRoleplay().OfferManager.ActiveOffers[Type.ToLower()];
                    int Cant = Convert.ToInt32(Offer.Params[0]);
                    if (Offer.Params != null && Offer.Params.Length > 0 && Cant < 0)
                    {
                        RoleplayBotAI Bot = (RoleplayBotAI)Offer.Params[0];
                        if (Bot.GetRoomUser().RoomId != Room.Id)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("caramelos", out Junk);
                            Session.SendWhisper("Lo siento, no estás en la misma habitación que " + Bot.GetBotRoleplay().Name + " para aceptar la oferta de caramelos!", 1);
                            return;
                        }
                        else if (Session.GetHabbo().Credits < Offer.Cost)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("caramelos", out Junk);
                            Session.SendWhisper("Lo siento, no puedes pagar 5 unidades de caramelos", 1);
                            return;
                        }
                        else
                        {
                            Session.Shout("*Acepta la oferta de " + Bot.GetBotRoleplay().Name + " 5 unidades de caramelos por $" + String.Format("{0:N0}", Offer.Cost) + "*", 4);
                            Session.GetHabbo().Credits -= Offer.Cost;
                            Session.GetHabbo().UpdateCreditsBalance();
                            Session.GetRoleplay().Caramelos += 5;
                            return;
                        }
                    }
                    else
                    {
                        GameClient Offerer = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Offer.OffererId);
                        if (Offerer == null || Offerer.GetRoomUser().RoomId != Session.GetRoomUser().RoomId || Offerer.GetRoomUser() == null)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("caramelos", out Junk);
                            Session.SendWhisper("Lo sentimos, este usuario se ha desconectado o no está en la misma habitación que tú.", 1);
                            return;
                        }
                        else if (Session.GetHabbo().Credits < Offer.Cost)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("caramelos", out Junk);
                            Session.SendWhisper("Lo siento, no puedes pagar 5 unidades de caramelos", 1);
                            return;
                        }
                        else
                        {
                            Session.Shout("*Acepta la oferta de " + Offerer.GetHabbo().Username + " Y compra "+Cant+" unidades de caramelos por $" + String.Format("{0:N0}", Offer.Cost) + "*", 4);
                            Session.GetHabbo().Credits -= Offer.Cost;
                            Session.GetHabbo().UpdateCreditsBalance();
                            Session.GetRoleplay().Caramelos += Cant;

                            Offerer.GetHabbo().Credits += Offer.Cost / 20;
                            Offerer.GetHabbo().UpdateCreditsBalance();

                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("caramelos", out Junk);
                            Offerer.SendWhisper("Recibes un recorte de $" + String.Format("{0:N0}", (Offer.Cost / 20)) + " por venderle a " + Session.GetHabbo().Username + " unos " + Offer.Type + "!");
                            return;
                        }
                    }
                }
                #endregion

                #region Medicina
                else if (Type.ToLower() == "medicina")
                {
                    var Offer = Session.GetRoleplay().OfferManager.ActiveOffers[Type.ToLower()];
                    if (Offer.Params != null && Offer.Params.Length > 0)
                    {
                        RoleplayBotAI Bot = (RoleplayBotAI)Offer.Params[0];
                        if (Bot.GetRoomUser().RoomId != Room.Id)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("medicina", out Junk);
                            Session.SendWhisper("Lo siento, no estás en la misma habitación que " + Bot.GetBotRoleplay().Name + " para aceptar la oferta de la medicina!", 1);
                            return;
                        }
                        else if (Session.GetHabbo().Credits < Offer.Cost)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("medicina", out Junk);
                            Session.SendWhisper("Lo siento, no puedes pagar 50Cc de medicinas", 1);
                            return;
                        }
                        else
                        {
                            Session.Shout("*Acepta la oferta de " + Bot.GetBotRoleplay().Name + " 50Cc de Medicinas por $" + String.Format("{0:N0}", Offer.Cost) + "*", 4);
                            Session.GetHabbo().Credits -= Offer.Cost;
                            Session.GetHabbo().UpdateCreditsBalance();
                            Session.GetRoleplay().Medicina += 50;
                            return;
                        }
                    }
                    else
                    {
                        GameClient Offerer = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Offer.OffererId);
                        if (Offerer == null || Offerer.GetRoomUser().RoomId != Session.GetRoomUser().RoomId || Offerer.GetRoomUser() == null)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("medicina", out Junk);
                            Session.SendWhisper("Lo sentimos, este usuario se ha desconectado o no está en la misma habitación que tú.", 1);
                            return;
                        }
                        else if (Session.GetHabbo().Credits < Offer.Cost)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("medicina", out Junk);
                            Session.SendWhisper("Lo siento, no puedes pagar 50Cc de Medicina", 1);
                            return;
                        }
                        else
                        {
                            Session.Shout("*Acepta la oferta de " + Offerer.GetHabbo().Username + " Y compra 50Cc de medicina $" + String.Format("{0:N0}", Offer.Cost) + "*", 4);
                            Session.GetHabbo().Credits -= Offer.Cost;
                            Session.GetHabbo().UpdateCreditsBalance();
                            Session.GetRoleplay().Medicina += 50;

                            Offerer.GetHabbo().Credits += Offer.Cost / 20;
                            Offerer.GetHabbo().UpdateCreditsBalance();

                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("medicina", out Junk);
                            Offerer.SendWhisper("Recibes un recorte de $" + String.Format("{0:N0}", (Offer.Cost / 20)) + " por venderle a " + Session.GetHabbo().Username + " unos " + Offer.Type + "!");
                            return;
                        }
                    }
                }
                #endregion

                #region Marihuana
                else if (Type.ToLower() == "marihuana")
                {
                    var Offer = Session.GetRoleplay().OfferManager.ActiveOffers[Type.ToLower()];
                    int Cant = Convert.ToInt32(Offer.Params[0]);
                    if (Offer.Params != null && Offer.Params.Length > 0 && Cant < 0)
                    {
                        RoleplayBotAI Bot = (RoleplayBotAI)Offer.Params[0];
                        if (Bot.GetRoomUser().RoomId != Room.Id)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("marihuana", out Junk);
                            Session.SendWhisper("Lo siento, no estás en la misma habitación que " + Bot.GetBotRoleplay().Name + " para aceptar la oferta de la marihuana!", 1);
                            return;
                        }
                        else if (Session.GetHabbo().Credits < Offer.Cost)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("marihuana", out Junk);
                            Session.SendWhisper("Lo siento, no puedes pagar 10 porros de marihuana", 1);
                            return;
                        }
                        else
                        {
                            Session.Shout("*Acepta la oferta de " + Bot.GetBotRoleplay().Name + " 10 porros de marihuana por $" + String.Format("{0:N0}", Offer.Cost) + "*", 4);
                            Session.GetHabbo().Credits -= Offer.Cost;
                            Session.GetHabbo().UpdateCreditsBalance();
                            Session.GetRoleplay().Weed += 10;
                            return;
                        }
                    }
                    else
                    {
                        GameClient Offerer = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Offer.OffererId);
                        if (Offerer == null || Offerer.GetRoomUser().RoomId != Session.GetRoomUser().RoomId || Offerer.GetRoomUser() == null)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("marihuana", out Junk);
                            Session.SendWhisper("Lo sentimos, este usuario se ha desconectado o no está en la misma habitación que tú.", 1);
                            return;
                        }
                        else if (Session.GetHabbo().Credits < Offer.Cost)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("marihuana", out Junk);
                            Session.SendWhisper("Lo siento, no puedes pagar "+Cant+" porro de marihuana", 1);
                            return;
                        }
                        else
                        {
                            Session.Shout("*Acepta la oferta de " + Offerer.GetHabbo().Username + " y compra "+Cant+" porros de marihuana $" + String.Format("{0:N0}", Offer.Cost) + "*", 4);
                            Session.GetHabbo().Credits -= Offer.Cost;
                            Session.GetHabbo().UpdateCreditsBalance();
                            Session.GetRoleplay().Weed += Cant;
                            Offerer.GetRoleplay().Weed -= 10;

                            Offerer.GetHabbo().Credits += Offer.Cost;
                            Offerer.GetHabbo().UpdateCreditsBalance();

                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("marihuana", out Junk);
                            Offerer.SendWhisper("Recibes $" + String.Format("{0:N0}", (Offer.Cost)) + " por venderle a " + Session.GetHabbo().Username + " " + Offer.Type + "!");
                            return;
                        }
                    }
                }
                #endregion

                #region Heroina
                else if (Type.ToLower() == "heroina")
                {
                    var Offer = Session.GetRoleplay().OfferManager.ActiveOffers[Type.ToLower()];
                    int Cant = Convert.ToInt32(Offer.Params[0]);
                    if (Offer.Params != null && Offer.Params.Length > 0 && Cant < 0)
                    {
                        RoleplayBotAI Bot = (RoleplayBotAI)Offer.Params[0];
                        if (Bot.GetRoomUser().RoomId != Room.Id)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("heroina", out Junk);
                            Session.SendWhisper("Lo siento, no estás en la misma habitación que " + Bot.GetBotRoleplay().Name + " para aceptar la oferta de la marihuana!", 1);
                            return;
                        }
                        else if (Session.GetHabbo().Credits < Offer.Cost)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("heroina", out Junk);
                            Session.SendWhisper("Lo siento, no puedes pagar 10cc de heroina", 1);
                            return;
                        }
                        else
                        {
                            Session.Shout("*Acepta la oferta de " + Bot.GetBotRoleplay().Name + " 10cc de heroina por $" + String.Format("{0:N0}", Offer.Cost) + "*", 4);
                            Session.GetHabbo().Credits -= Offer.Cost;
                            Session.GetHabbo().UpdateCreditsBalance();
                            Session.GetRoleplay().Heroina += 10;
                            return;
                        }
                    }
                    else
                    {
                        
                        GameClient Offerer = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Offer.OffererId);
                        if (Offerer == null || Offerer.GetRoomUser().RoomId != Session.GetRoomUser().RoomId || Offerer.GetRoomUser() == null)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("heroina", out Junk);
                            Session.SendWhisper("Lo sentimos, este usuario se ha desconectado o no está en la misma habitación que tú.", 1);
                            return;
                        }
                        else if (Session.GetHabbo().Credits < Offer.Cost)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("heroina", out Junk);
                            Session.SendWhisper("Lo siento, no puedes pagar "+ Cant+"cc heroina", 1);
                            return;
                        }
                        else
                        {
                            Session.Shout("*Acepta la oferta de " + Offerer.GetHabbo().Username + " y compra "+ Cant+"cc de heroina $" + String.Format("{0:N0}", Offer.Cost) + "*", 4);
                            Session.GetHabbo().Credits -= Offer.Cost;
                            Session.GetHabbo().UpdateCreditsBalance();
                            Session.GetRoleplay().Heroina += Cant;
                            Offerer.GetRoleplay().Heroina -= Cant;

                            Offerer.GetHabbo().Credits += Offer.Cost;
                            Offerer.GetHabbo().UpdateCreditsBalance();

                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("heroina", out Junk);
                            Offerer.SendWhisper("Recibes $" + String.Format("{0:N0}", (Offer.Cost)) + " por venderle a " + Session.GetHabbo().Username + " " + Offer.Type + "!");
                            return;
                        }
                    }
                }
                #endregion

                #region Fuel
                else if (Type.ToLower() == "gasolina")
                {
                    var Offer = Session.GetRoleplay().OfferManager.ActiveOffers[Type.ToLower()];
                    if (Offer.Params != null && Offer.Params.Length > 0)
                    {
                        RoleplayBotAI Bot = (RoleplayBotAI)Offer.Params[0];
                        if (Bot.GetRoomUser().RoomId != Room.Id)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("gasolina", out Junk);
                            Session.SendWhisper("Lo siento, no estás en la misma habitación que " + Bot.GetBotRoleplay().Name + " para aceptar la gasolina!", 1);
                            return;
                        }
                        else if (Session.GetHabbo().Credits < Convert.ToInt32(Math.Floor((double)(Offer.Cost * 2) / 3)))
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("gasolina", out Junk);
                            Session.SendWhisper("Lo siento, no puedes permitirte fuel!", 1);
                            return;
                        }
                        else
                        {
                            Session.Shout("*Acepta la oferta de " + Bot.GetBotRoleplay().Name + " y compra " + String.Format("{0:N0}", Offer.Cost) + " Galones de combustible por $" + String.Format("{0:N0}", Convert.ToInt32(Math.Floor((double)(Offer.Cost * 2) / 3))) + "*", 4);
                            Session.GetHabbo().Credits -= Convert.ToInt32(Math.Floor((double)(Offer.Cost * 2) / 3));
                            Session.GetHabbo().UpdateCreditsBalance();
                            Session.GetRoleplay().CarFuel += Offer.Cost;

                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("gasolina", out Junk);
                            Bot.GetRoomUser().Chat("Gracias por comprar " + String.Format("{0:N0}", Offer.Cost) + " Galones de combustible " + Session.GetHabbo().Username + "!", true);
                            //RoleplayManager.GiveMoneyFromCompanyNoRight(10, Offer.Cost, Session);
                            double dub = Offer.Cost * 2;
                            double Conv = Math.Round(dub, 0);
                            int Pay = Convert.ToInt32(Conv);
                            int CAmount = (Pay * 3) / 100;
                            RoleplayManager.GiveMoneyToCompany(10, Session, "cars", true, CAmount);
                            return;
                        }
                    }
                    else
                    {
                        GameClient Offerer = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Offer.OffererId);
                        if (Offerer == null || Offerer.GetRoomUser().RoomId != Session.GetRoomUser().RoomId || Offerer.GetRoomUser() == null)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("gasolina", out Junk);
                            Session.SendWhisper("Lo sentimos, este usuario se ha desconectado o no está en la misma habitación que tú.", 1);
                            return;
                        }
                        else if (Session.GetHabbo().Credits < Convert.ToInt32(Math.Floor((double)(Offer.Cost * 2) / 3)))
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("gasolina", out Junk);
                            Session.SendWhisper("Usted no puedo comprar combustible", 1);
                            return;
                        }
                        else
                        {
                            Session.Shout("*Acepta la oferta de " + Offerer.GetHabbo().Username + " y compra " + String.Format("{0:N0}", Offer.Cost) + " Galones de combustible por $" + String.Format("{0:N0}", Convert.ToInt32(Math.Floor((double)(Offer.Cost * 2) / 3))) + "*", 4);
                            Session.GetHabbo().Credits -= Convert.ToInt32(Math.Floor((double)(Offer.Cost * 2) / 3));
                            Session.GetHabbo().UpdateCreditsBalance();
                            Session.GetRoleplay().CarFuel += Offer.Cost;

                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("gasolina", out Junk);
                            return;
                        }
                    }
                }
                #endregion

                #region Permiso
                if (Type.ToLower() == "permiso")
                {
                    var Offer = Session.GetRoleplay().OfferManager.ActiveOffers[Type.ToLower()];
                    GameClient Offerer = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Offer.OffererId);
                    if (Offerer == null || Offerer.GetRoomUser().RoomId != Session.GetRoomUser().RoomId || Offerer.GetRoomUser() == null)
                    {
                        RoleplayOffer? Junk;
                        Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("permiso", out Junk);
                        Session.SendWhisper("Lo sentimos, este usuario se ha desconectado o no está en la misma habitación que tú.", 1);
                        return;
                    }
                    else
                    {
                        if (Offerer.GetRoleplay().OfferManager.ActiveOffers.ContainsKey("permiso"))
                        {
                            RoleplayOffer OffererJunk;
                            Offerer.GetRoleplay().OfferManager.ActiveOffers.TryRemove("permiso", out OffererJunk);
                        }

                        if (!Session.GetHabbo().GetBadgeComponent().HasBadge("PERMISO"))
                            Session.GetHabbo().GetBadgeComponent().GiveBadge("PERMISO", true, Session);

                        if (PolarEnvironment.GetGame().GetCacheManager().ContainsUser(Session.GetHabbo().Id))
                            PolarEnvironment.GetGame().GetCacheManager().TryUpdateUser(Session);

                        RoleplayOffer? Junk;
                        Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("permiso", out Junk);
                        Session.Shout("*Acepta la oferta de " + Offerer.GetHabbo().Username + " y compra un porte legal de armas por 50.000$ ¡Enhorabuena!*", 4);
                        Session.GetHabbo().Credits -= 50000;
                        Session.GetHabbo().UpdateCreditsBalance();
                        Offerer.GetHabbo().Credits += 2000;
                        Offerer.GetHabbo().UpdateCreditsBalance();

                    }
                }
                #endregion

                #region Permisoweed
                if (Type.ToLower() == "permisoweed")
                {
                    var Offer = Session.GetRoleplay().OfferManager.ActiveOffers[Type.ToLower()];
                    GameClient Offerer = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Offer.OffererId);
                    if (Offerer == null || Offerer.GetRoomUser().RoomId != Session.GetRoomUser().RoomId || Offerer.GetRoomUser() == null)
                    {
                        RoleplayOffer? Junk;
                        Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("permisoweed", out Junk);
                        Session.SendWhisper("Lo sentimos, este usuario se ha desconectado o no está en la misma habitación que tú.", 1);
                        return;
                    }
                    else
                    {
                        if (Offerer.GetRoleplay().OfferManager.ActiveOffers.ContainsKey("permisoweed"))
                        {
                            RoleplayOffer OffererJunk;
                            Offerer.GetRoleplay().OfferManager.ActiveOffers.TryRemove("permisoweed", out OffererJunk);
                        }

                        if (!Session.GetHabbo().GetBadgeComponent().HasBadge("MEDICINAL"))
                            Session.GetHabbo().GetBadgeComponent().GiveBadge("MEDICINAL", true, Session);

                        if (PolarEnvironment.GetGame().GetCacheManager().ContainsUser(Session.GetHabbo().Id))
                            PolarEnvironment.GetGame().GetCacheManager().TryUpdateUser(Session);

                        RoleplayOffer? Junk;
                        Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("permisoweed", out Junk);
                        Session.Shout("*Acepta la oferta de " + Offerer.GetHabbo().Username + " y compra un permiso legal marihuana medicinal por 200.000$ ¡Enhorabuena!*", 4);
                        Offerer.SendWhisper("El ciudadano aceptó la oferta del permiso y usted obtiene 2.000$ de comisión");
                        Session.GetHabbo().Credits -= 200000;
                        Offerer.GetHabbo().Credits += 2000;
                        Offerer.GetHabbo().UpdateCreditsBalance();
                        Session.GetHabbo().UpdateCreditsBalance();

                    }
                }
                #endregion

                #region Job
                if (Type.ToLower() == "trabajo")
                {
                    var Offer = Session.GetRoleplay().OfferManager.ActiveOffers[Type.ToLower()];
                    if (Offer.Params != null && Offer.Params.Length > 0)
                    {
                        RoleplayBotAI Bot = (RoleplayBotAI)Offer.Params[0];
                        if (Bot.GetRoomUser().RoomId != Room.Id)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("trabajo", out Junk);
                            Session.SendWhisper("Lo siento, no estás en la misma habitación que " + Bot.GetBotRoleplay().Name + " para aceptar este trabajo", 1);
                            return;
                        }
                        else
                        {
                            var Job = GroupManager.GetJob(Offer.Cost);
                            var JobRank = GroupManager.GetJobRank(Offer.Cost, 1);

                            if (Job != null)
                            {
                                if (Job.Members.Count < JobRank.Limit || JobRank.Limit <= 0)
                                {
                                    Session.Shout("*Acepta la oferta de " + Bot.GetBotRoleplay().Name + " Y se une a la '" + Job.Name + "' Empresa como '" + JobRank.Name + "'*", 4);
                                    Bot.GetRoomUser().Chat("Bienvenido a " + Job.Name + " y esperamos mucho de ti " + Session.GetHabbo().Username + "!", true);

                                    Session.GetRoleplay().TimeWorked = 0;
                                    Session.GetRoleplay().JobId = Job.Id;
                                    Session.GetRoleplay().JobRank = 1;
                                    Session.GetRoleplay().JobRequest = 0;

                                    Job.AddNewMember(Session.GetHabbo().Id);
                                    Job.SendPackets(Session);
                                }
                                else
                                    Session.SendWhisper("Lo siento, pero esta empresa de trabajo está lleno", 1);
                            }
                            else
                                Session.SendWhisper("¡Por alguna extraña razón, este trabajo no pudo ser encontrado!", 1);

                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("trabajo", out Junk);
                            return;
                        }
                    }
                    else
                    {
                        GameClient Offerer = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Offer.OffererId);
                        if (Offerer == null || Offerer.GetRoomUser() == null || Session.GetRoomUser() == null || Offerer.GetRoomUser().RoomId != Session.GetRoomUser().RoomId)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("trabajo", out Junk);
                            Session.SendWhisper("Lo sentimos, este usuario se ha desconectado o no está en la misma habitación que tú.", 1);
                            return;
                        }
                        else
                        {
                            var Job = GroupManager.GetJob(Offer.Cost);
                            var JobRank = GroupManager.GetJobRank(Offer.Cost, 1);

                            if (Job != null)
                            {
                                Session.Shout("*Acepta la oferta de " + Offerer.GetHabbo().Username + " Y se une a la '" + Job.Name + "' Corporación como '" + JobRank.Name + "'*", 4);
                                Offerer.SendWhisper(Session.GetHabbo().Username + " Acaba de unirse a su corporación!", 1);

                                Session.GetRoleplay().TimeWorked = 0;
                                Session.GetRoleplay().JobId = Job.Id;
                                Session.GetRoleplay().JobRank = 1;
                                Session.GetRoleplay().JobRequest = 0;

                                Job.AddNewMember(Session.GetHabbo().Id);
                                Job.SendPackets(Session);
                            }
                            else
                                Session.SendWhisper("¡Por alguna extraña razón, este trabajo no pudo ser encontrado!", 1);

                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("trabajo", out Junk);
                            return;
                        }
                    }
                }
                #endregion

                #region Gang
                if (Type.ToLower() == "pandilla")
                {
                    var Offer = Session.GetRoleplay().OfferManager.ActiveOffers[Type.ToLower()];
                    GameClient Offerer = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Offer.OffererId);
                    if (Offerer == null || Offerer.GetRoomUser() == null || Session.GetRoomUser() == null || Offerer.GetRoomUser().RoomId != Session.GetRoomUser().RoomId)
                    {
                        RoleplayOffer? Junk;
                        Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("pandilla", out Junk);
                        Session.SendWhisper("Lo sentimos, este usuario se ha desconectado o no está en la misma habitación que tú.", 1);
                        return;
                    }
                    else
                    {
                        if (GroupManager.Jobs.Values.Where(x => x.CreatorId == Session.GetHabbo().Id).ToList().Count > 0)
                        {
                            Session.SendWhisper("Por favor borre a su pandilla antes de intentar unirse a otra", 1);
                            return;
                        }

                        if (Session.GetHabbo().Credits < 2000)
                        {
                            Session.SendWhisper("¡No tiene 2.000$ para unirse a la pandilla!", 1);
                            return;
                        }

                        var Gang = GroupManager.GetGang(Offer.Cost);
                        var GangRank = GroupManager.GetGangRank(Offer.Cost, 1);

                        if (Gang != null)
                        {
                            Session.GetRoleplay().GangId = Gang.Id;
                            Session.GetRoleplay().GangRank = 1;
                            Session.GetRoleplay().GangRequest = 0;

                            Gang.AddNewMember(Session.GetHabbo().Id);
                            Gang.SendPackets(Session);
                        }
                        else
                            Session.SendWhisper("Por alguna extraña razón, esta pandilla no se pudo encontrar, podría haber sido eliminado después de invitarlo a él", 1);


                        Gang.MediPacks += 100;

                        using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                            dbClient.RunQuery("UPDATE `rp_gangs` SET `medipacks` = '" + Gang.MediPacks + "' WHERE `id` = '" + Gang.Id + "'");

                        Gang.Balance += 1000;

                        using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                            dbClient.RunQuery("UPDATE `rp_gangs` SET `bank_balance` = '" + Gang.Balance + "' WHERE `id` = '" + Gang.Id + "'");

                        RoleplayOffer? Junk;
                        Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("pandilla", out Junk);
                        Session.GetHabbo().Credits -= 2000;
                        Session.GetHabbo().UpdateCreditsBalance();
                        Session.Shout("*Acepta la oferta de pandillas de " + Offerer.GetHabbo().Username + " Y se une a la " + Gang.Name + " pandilla*", 4);
                        Session.SendWhisper("*Pagaste 2.000$ por unirte a la pandilla, esto servirá para gastos médicos en guerras*");
                        return;
                    }
                }
                #endregion

                #region Marriage
                if (Type.ToLower() == "casarme")
                {
                    var Offer = Session.GetRoleplay().OfferManager.ActiveOffers[Type.ToLower()];
                    GameClient Offerer = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Offer.OffererId);
                    if (Offerer == null || Offerer.GetRoomUser().RoomId != Session.GetRoomUser().RoomId || Offerer.GetRoomUser() == null)
                    {
                        RoleplayOffer? Junk;
                        Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("casarme", out Junk);
                        Session.SendWhisper("Lo sentimos, este usuario se ha desconectado o no está en la misma habitación que tú.", 1);
                        return;
                    }
                    else if (Session.GetRoleplay().MarriedTo > 0)
                    {
                        RoleplayOffer? Junk;
                        Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("casarme", out Junk);
                        Session.SendWhisper("¡Lo siento ya está casado!", 1);
                        return;
                    }
                    else if (Offerer.GetRoleplay().MarriedTo > 0)
                    {
                        RoleplayOffer? Junk;
                        Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("casarme", out Junk);
                        Session.SendWhisper("Lo sentimos este usuario ya está casado", 1);
                        return;
                    }
                    else
                    {
                        if (Offerer.GetRoleplay().OfferManager.ActiveOffers.ContainsKey("casarme"))
                        {
                            RoleplayOffer OffererJunk;
                            Offerer.GetRoleplay().OfferManager.ActiveOffers.TryRemove("casarme", out OffererJunk);
                        }

                        Session.GetRoleplay().MarriedTo = Offerer.GetHabbo().Id;
                        Offerer.GetRoleplay().MarriedTo = Session.GetHabbo().Id;

                        if (!Session.GetHabbo().GetBadgeComponent().HasBadge("WD0"))
                            Session.GetHabbo().GetBadgeComponent().GiveBadge("WD0", true, Session);

                        if (!Offerer.GetHabbo().GetBadgeComponent().HasBadge("WD0"))
                            Offerer.GetHabbo().GetBadgeComponent().GiveBadge("WD0", true, Offerer);

                        if (PolarEnvironment.GetGame().GetCacheManager().ContainsUser(Session.GetHabbo().Id))
                            PolarEnvironment.GetGame().GetCacheManager().TryUpdateUser(Session);

                        if (PolarEnvironment.GetGame().GetCacheManager().ContainsUser(Offerer.GetHabbo().Id))
                            PolarEnvironment.GetGame().GetCacheManager().TryUpdateUser(Offerer);

                        using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                        {
                            dbClient.RunQuery("UPDATE `rp_stats` SET `married_to` = '" + Session.GetHabbo().Id + "' WHERE `id` = '" + Offerer.GetHabbo().Id + "'");
                            dbClient.RunQuery("UPDATE `rp_stats` SET `married_to` = '" + Offerer.GetHabbo().Id + "' WHERE `id` = '" + Session.GetHabbo().Id + "'");
                        }

                        RoleplayOffer? Junk;
                        Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("casarme", out Junk);
                        Session.Shout("*Acepta casarse con " + Offerer.GetHabbo().Username + "'s ¡Enhorabuena!*", 16);
                        Session.GetRoomUser().ApplyEffect(908);
                        Offerer.GetRoomUser().ApplyEffect(908);

                    }
                }
                #endregion

                #region Hijo
                if (Type.ToLower() == "mama")
                {
                    var Offer = Session.GetRoleplay().OfferManager.ActiveOffers[Type.ToLower()];
                    GameClient Offerer = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Offer.OffererId);
                    if (Offerer == null || Offerer.GetRoomUser().RoomId != Session.GetRoomUser().RoomId || Offerer.GetRoomUser() == null)
                    {
                        RoleplayOffer? Junk;
                        Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("mama", out Junk);
                        Session.SendWhisper("Lo sentimos, este usuario se ha desconectado o no está en la misma habitación que tú.", 1);
                        return;
                    }
                    else if (Session.GetRoleplay().Hijo > 0)
                    {
                        RoleplayOffer? Junk;
                        Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("mama", out Junk);
                        Session.SendWhisper("¡Lo siento ya tiene hij@!", 1);
                        return;
                    }
                    else if (Offerer.GetRoleplay().Hijo > 0)
                    {
                        RoleplayOffer? Junk;
                        Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("mama", out Junk);
                        Session.SendWhisper("Lo sentimos este usuario ya tiene hij@", 1);
                        return;
                    }
                    else
                    {
                        if (Offerer.GetRoleplay().OfferManager.ActiveOffers.ContainsKey("mama"))
                        {
                            RoleplayOffer OffererJunk;
                            Offerer.GetRoleplay().OfferManager.ActiveOffers.TryRemove("mama", out OffererJunk);
                        }

                        Session.GetRoleplay().Hijo = Offerer.GetHabbo().Id;
                        Offerer.GetRoleplay().Hijo = Session.GetHabbo().Id;

                        if (!Offerer.GetHabbo().GetBadgeComponent().HasBadge("DK087"))
                            Offerer.GetHabbo().GetBadgeComponent().GiveBadge("DK087", true, Offerer);

                        if (PolarEnvironment.GetGame().GetCacheManager().ContainsUser(Session.GetHabbo().Id))
                            PolarEnvironment.GetGame().GetCacheManager().TryUpdateUser(Session);

                        if (PolarEnvironment.GetGame().GetCacheManager().ContainsUser(Offerer.GetHabbo().Id))
                            PolarEnvironment.GetGame().GetCacheManager().TryUpdateUser(Offerer);

                        using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                        {
                            dbClient.RunQuery("UPDATE `rp_stats` SET `hijo` = '" + Session.GetHabbo().Id + "' WHERE `id` = '" + Offerer.GetHabbo().Id + "'");
                            dbClient.RunQuery("UPDATE `rp_stats` SET `hijo` = '" + Offerer.GetHabbo().Id + "' WHERE `id` = '" + Session.GetHabbo().Id + "'");
                        }

                        RoleplayOffer? Junk;
                        Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("mama", out Junk);
                        Session.Shout("*¡Ha nacido un bebé su mamá es: " + Offerer.GetHabbo().Username + "'s ¡Enhorabuena!*", 16);
                        Session.GetRoomUser().ApplyEffect(908);
                        Offerer.GetRoomUser().ApplyEffect(908);
                        Offerer.GetRoleplay().Embarazo = 0;
                        Offerer.Shout("¡Aaaaaah, aaaaaahhh, uuuuuuuuuuuuuh!");
                        HabboRoleplay.Misc.RoleplayManager.GetLookAndMotto(Offerer, "poof");
                        Offerer.SendWhisper("¡Se ha actualizado tu look!", 1);

                    }
                }
                #endregion

                #region CorrienteAccount
                if (Type.ToLower() == "corriente" || Type.ToLower() == "checkings")
                {
                    Type = "corriente";
                    var Offer = Session.GetRoleplay().OfferManager.ActiveOffers[Type.ToLower()];

                    if (Offer != null)
                    {
                        if (Offer.Params != null && Offer.Params.Length > 0)
                        {
                            RoleplayBotAI Bot = (RoleplayBotAI)Offer.Params[0];
                            if (Bot.GetRoomUser().RoomId != Room.Id)
                            {
                                RoleplayOffer? Junk;
                                Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("corriente", out Junk);
                                Session.SendWhisper("Lo siento, no estás en la misma habitación que " + Bot.GetBotRoleplay().Name + " para aceptar una cuenta corriente", 1);
                                return;
                            }
                            else
                            {
                                Session.Shout("*Acepta la oferta de " + Bot.GetBotRoleplay().Name + " Y obtiene una Cuenta Corriente y tarjeta de débito*", 4);
                                Session.GetRoleplay().BankAccount = 1;
                                Session.GetRoleplay().BankTarget = 1;
                                Session.GetRoomUser().ApplyEffect(603);

                                RoleplayOffer? Junk;
                                Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("corriente", out Junk);
                                Bot.GetRoomUser().Chat("Gracias por abrir una cuenta de Corriente " + Session.GetHabbo().Username + "!", true);
                                return;
                            }
                        }
                        else
                        {
                            GameClient Offerer = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Offer.OffererId);
                            if (Offerer == null || Offerer.GetRoomUser().RoomId != Session.GetRoomUser().RoomId || Offerer.GetRoomUser() == null)
                            {
                                RoleplayOffer? Junk;
                                Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("corriente", out Junk);
                                Session.SendWhisper("Lo sentimos, este usuario se ha desconectado o no está en la misma habitación que tú.", 1);
                                return;
                            }
                            else
                            {
                                Session.Shout("*Acepta la oferta de " + Offerer.GetHabbo().Username + " Y obtiene una Cuenta Corriente y tarjeta de débito*", 4);
                                Session.GetRoleplay().BankAccount = 1;
                                Session.GetRoleplay().BankTarget = 1;
                                Session.GetRoomUser().ApplyEffect(603);

                                Offerer.GetHabbo().Credits += 100;
                                Offerer.GetHabbo().UpdateCreditsBalance();

                                RoleplayOffer? Junk;
                                Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("corriente", out Junk);
                                Offerer.SendWhisper("Recibes un recorte de $100 por ofrecer cuenta y tarjeta  " + Session.GetHabbo().Username + "!");
                                PolarEnvironment.GetGame().GetAchievementManager().ProgressAchievement(Offerer, "ACH_SellingBankAccount", 1);
                                return;
                            }
                        }
                    }
                    else
                    {
                        Session.SendWhisper("Usted no tiene una oferta de cuenta corriente", 1);
                        return;
                    }
                }
                #endregion

                #region Savings Account
                if (Type.ToLower() == "ahorro")
                {
                    var Offer = Session.GetRoleplay().OfferManager.ActiveOffers[Type.ToLower()];
                    if (Offer.Params != null && Offer.Params.Length > 0)
                    {
                        RoleplayBotAI Bot = (RoleplayBotAI)Offer.Params[0];
                        if (Bot.GetRoomUser().RoomId != Room.Id)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("ahorro", out Junk);
                            Session.SendWhisper("Lo siento, no estás en la misma habitación que " + Bot.GetBotRoleplay().Name + "Para aceptar la cuenta de ahorros!", 1);
                            return;
                        }
                        else if (Session.GetHabbo().Credits < Offer.Cost)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("ahorro", out Junk);
                            Session.SendWhisper("¡Lo siento, no puede permitirse el lujo de abrir una cuenta de ahorros!", 1);
                            return;
                        }
                        else
                        {
                            Session.Shout("*Acepta la oferta de " + Bot.GetBotRoleplay().Name + " Y se abre una cuenta de ahorros*", 4);
                            Session.GetHabbo().Credits -= Offer.Cost;
                            Session.GetHabbo().UpdateCreditsBalance();

                            Session.GetRoleplay().BankAccount = 2;
                            Session.GetRoleplay().BankTarget = 1;

                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("ahorro", out Junk);
                            Bot.GetRoomUser().Chat("*Gracias por abrir una cuenta de ahorros " + Session.GetHabbo().Username + "*", true);
                        }
                    }
                    else
                    {
                        GameClient Offerer = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Offer.OffererId);
                        if (Offerer == null || Offerer.GetRoomUser().RoomId != Session.GetRoomUser().RoomId || Offerer.GetRoomUser() == null)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("ahorro", out Junk);
                            Session.SendWhisper("Lo sentimos, este usuario se ha desconectado o no está en la misma habitación que tú.", 1);
                            return;
                        }
                        else if (Session.GetHabbo().Credits < Offer.Cost)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("ahorro", out Junk);
                            Session.SendWhisper("¡Lo siento, no puede permitirse el lujo de abrir una cuenta de ahorros!", 1);
                            return;
                        }
                        else
                        {
                            Session.Shout("*Acepta la oferta de " + Offerer.GetHabbo().Username + " Y se abre una cuenta de ahorros*", 4);
                            Session.GetHabbo().Credits -= Offer.Cost;
                            Session.GetHabbo().UpdateCreditsBalance();

                            Session.GetRoleplay().BankAccount = 2;
                            Session.GetRoleplay().BankTarget = 1;

                            Offerer.GetHabbo().Credits += Offer.Cost / 25;
                            Offerer.GetHabbo().UpdateCreditsBalance();

                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("ahorro", out Junk);
                            Offerer.SendWhisper("Recibes un recorte de $" + String.Format("{0:N0}", (Offer.Cost / 25)) + " por la venta a " + Session.GetHabbo().Username + " de un " + Offer.Type + "!");
                            PolarEnvironment.GetGame().GetAchievementManager().ProgressAchievement(Offerer, "ACH_SellingBankAccount", 1);
                            return;
                        }
                    }
                }
                #endregion

                #region Mamada
                else if (Type.ToLower() == "mamada")
                {
                    var Offer = Session.GetRoleplay().OfferManager.ActiveOffers[Type.ToLower()];
                    if (Offer.Params != null && Offer.Params.Length > 0)
                    {
                        RoleplayBotAI Bot = (RoleplayBotAI)Offer.Params[0];
                        if (Bot.GetRoomUser().RoomId != Room.Id)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("mamada", out Junk);
                            Session.SendWhisper("Lo siento, no estás en la misma habitación que " + Bot.GetBotRoleplay().Name + " para aceptar la oferta de la mamada!", 1);
                            return;
                        }
                        else if (Session.GetHabbo().Credits < Offer.Cost)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("mamada", out Junk);
                            Session.SendWhisper("Lo siento, no puedes pagar una mamada", 1);
                            return;
                        }
                    }
                    else
                    {
                        GameClient Offerer = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Offer.OffererId);
                        if (Offerer == null || Offerer.GetRoomUser().RoomId != Session.GetRoomUser().RoomId || Offerer.GetRoomUser() == null)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("mamada", out Junk);
                            Session.SendWhisper("Lo sentimos, este usuario se ha desconectado o no está en la misma habitación que tú.", 1);
                            return;
                        }
                        else if (Session.GetHabbo().Credits < Offer.Cost)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("mamada", out Junk);
                            Session.SendWhisper("Lo siento, no puedes pagar una mamada", 1);
                            return;
                        }
                        else
                        {
                            Session.Shout("*Acepta la oferta de " + Offerer.GetHabbo().Username + " por una mamada de $" + String.Format("{0:N0}", Offer.Cost) + "*", 4);
                            Session.GetHabbo().Credits -= Offer.Cost;
                            Session.GetHabbo().UpdateCreditsBalance();
                            Session.GetRoleplay().Animo = 100;
                            Session.GetRoleplay().CurHealth = Session.GetRoleplay().MaxHealth;
                            Session.GetRoleplay().CurEnergy = Session.GetRoleplay().MaxEnergy;
                            Session.GetRoleplay().Sida += 10;
                            Session.GetRoomUser().ApplyEffect(507);
                            Session.GetRoleplay().SexTimer = 15;
                            Offerer.GetHabbo().Credits += 300;
                            Offerer.GetHabbo().UpdateCreditsBalance();

                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("mamada", out Junk);
                            Offerer.SendWhisper("Recibes $300 por darle a " + Session.GetHabbo().Username + " una " + Offer.Type + " y pagas comisión de 200$ al local");
                            return;
                        }
                    }
                }


                #endregion

                #region FixCar
                else if (Session.GetRoleplay().OfferManager.ActiveOffers.ContainsKey("fix_mecanico"))
                {
                    var Offer2 = Session.GetRoleplay().OfferManager.ActiveOffers["fix_mecanico"];
                    if (Offer2 != null)
                    {
                        GameClient Offerer = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Offer2.OffererId);
                        if (Offerer == null || Offerer.GetRoomUser().RoomId != Session.GetRoomUser().RoomId || Offerer.GetRoomUser() == null)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("fix_mecanico", out Junk);
                            Session.SendWhisper("Lo sentimos, el ofertante no está en la misma zona que tú.", 1);
                            return;
                        }
                        else if (Session.GetRoleplay().Level < 2)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("fix_mecanico", out Junk);
                            Session.SendWhisper("((Lo sentimos, no puedes aceptar los servicios de un mecánico hasta el nivel 2.))", 1);
                            return;
                        }
                        else if (Session.GetHabbo().Credits < Offer2.Cost)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("fix_mecanico", out Junk);
                            Session.SendWhisper("¡Necesitas $" + Offer2.Cost + " para aceptar la reparación!", 1);
                            return;
                        }
                        else if (Offerer.GetRoomUser().Coordinate != Offerer.GetRoleplay().MecCordinates)
                        {
                            Session.SendWhisper("¡El mecánico no se encuentra frente a tu Vehículo! Pídele que vuelva a la posición donde te ofreció sus servicios para poder aceptar o usa ':rechazar reparacion'", 1);
                            return;
                        }
                        else
                        {
                            #region Check Vehicle InFront
                            int FuelSize = 0;
                            Vehicle? vehicle;
                            bool found = false;
                            int itemfurni = 0, corp = 0;
                            Item? BTile;
                            string? itemnm;
                            foreach (Vehicle Vehicle in VehicleManager.Vehicles.Values)
                            {
                                if (!found)
                                {
                                    BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == Vehicle.ItemName && x.Coordinate == Offerer.GetRoomUser().SquareInFront);
                                    if (BTile != null)
                                    {
                                        vehicle = Vehicle;
                                        itemfurni = BTile.Id;
                                        itemnm = Vehicle.ItemName;
                                        corp = Convert.ToInt32(Vehicle.CarCorp);
                                        found = true;
                                    }
                                }
                            }

                            if (!found)
                            {
                                RoleplayOffer? Junk2;
                                Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("fix_mecanico", out Junk2);
                                Session.SendWhisper("¡El Mecánico debe estar frente al vehículo a reparar!", 1);
                                return;
                            }

                            #endregion

                            #region Select Vehicle State
                            int state = 0;
                            List<VehiclesOwned> VO = PolarEnvironment.GetGame().GetVehiclesOwnedManager().getVehiclesOwnedByFurniId(itemfurni);
                            if (VO == null || VO.Count <= 0)
                            {
                                RoleplayOffer? Junk3;
                                Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("fix_mecanico", out Junk3);
                                Session.SendWhisper("¡El Mecánico debe estar frente a un vehículo que requiera reparacion!", 1);
                                return;
                            }
                            state = VO[0].State;
                            #endregion

                            #region Check Vehicle State
                            if (state == 2 || state == 3 || VO[0].CarLife <= 0)
                            {
                                if (state == 2)
                                    Offerer.GetRoleplay().MecNewState = 0;// óptimo y sin traba
                                else
                                    Offerer.GetRoleplay().MecNewState = 1;// óptimo y con traba
                            }
                            else
                            {
                                RoleplayOffer? Junk4;
                                Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("fix_mecanico", out Junk4);
                                Session.SendWhisper("Este vehículo no necesita ser reparado.", 1);
                                return;
                            }
                            #endregion

                            #region Calc Repair Kit Cant
                            if (FuelSize <= 90)// Tanque Pequeño
                            {
                                Offerer.GetRoleplay().MecPartsTo = 3;
                            }
                            else if (FuelSize > 90 && FuelSize <= 100)// Tanque Mediano
                            {
                                Offerer.GetRoleplay().MecPartsTo = 6;
                            }
                            else // Tanque Grande
                            {
                                Offerer.GetRoleplay().MecPartsTo = 9;
                            }
                            #endregion

                            if (Offerer.GetRoleplay().MecParts < Offerer.GetRoleplay().MecPartsTo)
                            {
                                RoleplayOffer? Junk4;
                                Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("fix_mecanico", out Junk4);
                                Session.SendWhisper("El mecánico ya no cuenta con los repuestos suficientes.", 1);
                                return;
                            }

                            RoleplayManager.Shout(Session, "*Acepta la oferta de Reparación de " + Offerer.GetHabbo().Username + " por $" + String.Format("{0:N0}", Offer2.Cost) + "*", 5);
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("fix_mecanico", out Junk);

                            // Iniciar Timer al Mecánico
                            Offerer.GetRoleplay().MecUserToRepair = Session.GetHabbo().Id;
                            Offerer.GetRoleplay().MecPriceTo = Offer2.Cost;
                            Offerer.GetRoleplay().IsMecLoading = true;
                            Offerer.GetRoleplay().LoadingTimeLeft = RoleplayManager.GetTimerByMyJob(Offerer, "mecanico"); // Depende nivel del Mecánico

                            #region In State Mecánico
                            if (!Offerer.GetRoomUser().isSitting)
                            {
                                Offerer.GetRoomUser().SetRot(Offerer.GetRoleplay().MecRotPosition, false);
                                #region Sit
                                if (!Offerer.GetRoomUser().Statusses.ContainsKey("sit"))
                                {
                                    if ((Offerer.GetRoomUser().RotBody % 2) == 0)
                                    {
                                        try
                                        {
                                            Offerer.GetRoomUser().Statusses.Add("sit", "1.0");
                                            Offerer.GetRoomUser().Z -= 0.35;
                                            Offerer.GetRoomUser().isSitting = true;
                                            Offerer.GetRoomUser().UpdateNeeded = true;
                                        }
                                        catch { }
                                    }
                                    else
                                    {
                                        Offerer.GetRoomUser().RotBody--;
                                        Offerer.GetRoomUser().Statusses.Add("sit", "1.0");
                                        Offerer.GetRoomUser().Z -= 0.35;
                                        Offerer.GetRoomUser().isSitting = true;
                                        Offerer.GetRoomUser().UpdateNeeded = true;
                                    }
                                }
                                else if (Offerer.GetRoomUser().isSitting == true)
                                {
                                    Offerer.GetRoomUser().Z += 0.35;
                                    Offerer.GetRoomUser().Statusses.Remove("sit");
                                    Offerer.GetRoomUser().Statusses.Remove("1.0");
                                    Offerer.GetRoomUser().isSitting = false;
                                    Offerer.GetRoomUser().UpdateNeeded = true;
                                }
                                #endregion
                            }
                            #endregion

                            RoleplayManager.Shout(Offerer, "*Saca sus herramientas y comienza a reparar el vehículo de " + Session.GetHabbo().Username + "*", 5);
                            Offerer.SendWhisper("Debes esperar " + Offerer.GetRoleplay().LoadingTimeLeft + " segundo(s)...", 1);
                            Offerer.GetRoleplay().TimerManager.CreateTimer("general", 1000, true);
                            return;
                        }
                    }
                }
                #endregion

                #region Telefono
                else if (Session.GetRoleplay().OfferManager.ActiveOffers.ContainsKey("telefono"))
                {
                    var Offer2 = Session.GetRoleplay().OfferManager.ActiveOffers["telefono"];
                    if (Offer2 != null)
                    {
                        GameClient Offerer = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Offer2.OffererId);
                        Phone phone = PhoneManager.getPhone("iphone");
                        if (Offerer == null || Offerer.GetRoomUser().RoomId != Session.GetRoomUser().RoomId || Offerer.GetRoomUser() == null)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("telefono", out Junk);
                            Session.SendWhisper("Lo sentimos, el ofertante no está en la misma zona que tú.", 1);
                            return;
                        }
                        else if (Session.GetHabbo().Credits < phone.Price)
                        {
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("telefono", out Junk);
                            Session.SendWhisper("¡Necesitas $" + Offer2.Cost + " para aceptar el teléfono!", 1);
                            return;
                        }
                        
                        else if (phone == null)
                        {
                            Session.SendWhisper("Ha ocurrido un problema al obtener la Información del Teléfono.", 1);
                            return;
                        }
                        else
                        {


                            RoleplayManager.Shout(Session, "*Acepta la oferta de teléfono de " + Offerer.GetHabbo().Username + " por $" + String.Format("{0:N0}", Offer2.Cost) + "*", 5);
                            RoleplayOffer? Junk;
                            Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("telefono", out Junk);

                            #region Execute
                            String NewNumber = Session.GetRoleplay().PhoneNumber;
                            string NumberInfo = "Tu número para tu nuevo teléfono sigue siendo el mismo: " + NewNumber;
                            if (NewNumber.Length <= 0)
                            {
                                // Obtenemos Numero Random con Formato (xxx)-xxx-xxxx
                                NewNumber = RoleplayManager.GeneratePhoneNumber(Session.GetHabbo().Id);
                                NumberInfo = "Tu número es: " + NewNumber + ". ((Para volverlo a consultar usa :minumero))";

                                PhonesOwned nPO;
                                if (!PolarEnvironment.GetGame().GetPhonesOwnedManager().TryCreatePhoneOwned(Session, phone.ID, Session.GetHabbo().Id, NewNumber, out nPO))
                                {
                                    Session.SendWhisper("No se pudo autorizar el registro de papeles para tu nuevo teléfono. Inténtalo de nuevo.", 1);
                                    return;
                                }

                                PolarEnvironment.GetGame().GetClientManager().RegisterClientPhone(Session.GetRoomUser().GetClient(), Session.GetHabbo().Id, NewNumber);

                                RoleplayManager.SetDefaultApps(Session);
                            }
                            else
                            {
                                PhonesOwned nPO;
                                if (!PolarEnvironment.GetGame().GetPhonesOwnedManager().UpdatePhoneOwner(Session, phone.ID, true, out nPO))
                                {
                                    Session.SendWhisper("No se pudo autorizar el registro de papeles para tu nuevo teléfono. Inténtalo de nuevo.", 1);
                                    return;
                                }
                            }

                            Session.GetHabbo().Credits -= phone.Price;
                            Session.GetHabbo().UpdateCreditsBalance();
                            RoleplayManager.Shout(Session, "*Compra un " + phone.DisplayName + " nuevo y paga $" + phone.Price + " por él*", 5);
                            Session.SendWhisper("Has comprado un " + phone.DisplayName + " y pagaste $" + phone.Price, 1);
                            Session.SendWhisper("Ahora podrás agregar contactos, enviar mensajes y realizar llamadas.", 1);
                            Session.SendWhisper(NumberInfo, 1);
                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_phone", "load_apps");
                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_phone", "show_button");

                            #endregion
                            return;
                        }
                    }
                }
                #endregion



            }
            else
            {
                Session.SendWhisper("No tienes un " + Type + " de oferta", 1);
                return;
            }
        }
    }
}