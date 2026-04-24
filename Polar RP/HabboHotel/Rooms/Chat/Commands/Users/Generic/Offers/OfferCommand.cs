using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Groups;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Turfs;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Weapons;
using Polar.HabboRoleplay.Farming;
using Polar.HabboHotel.Items;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Offers
{
    class OfferCommand : IChatCommand
    {


        public string PermissionRequired
        {
            get { return "command_offers_offer"; }
        }

        public string Parameters
        {
            get { return "%usuarii% %tipo%"; }
        }

        public string Description
        {
            get { return "Ofrece el tipo deseado al usuario deseado"; }
        }

        public GameClient? TargetClient { get; private set; }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {

            #region Variables
            Group Gang = GroupManager.GetGang(Session.GetRoleplay().GangId);
            Turf Turf = TurfManager.GetTurf(Room.RoomId);
            //bool InsideTurf = false;
            #endregion


            if (Params.Length < 3)
            {
                Session.SendWhisper("Invalid command syntax: ':ofrecer usuario item costo'.", 1);
                return;
            }

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

            string Type = Params[2];

            if (CommandManager.MergeParams(Params, 2) == "seed satchel")
                Type = "bolsasemillas";
           else  if (CommandManager.MergeParams(Params, 2) == "bolsa semillas")
                Type = "bolsasemillas";
            else if (CommandManager.MergeParams(Params, 2) == "plant satchel")
                Type = "bolsavegetal";
            else if (CommandManager.MergeParams(Params, 2) == "bolsa vegetal")
                Type = "bolsavegetal";

            #region Weapon Check
            Weapon weapon = null;
            foreach (Weapon Weapon in WeaponManager.Weapons.Values)
            {
                if (Type.ToLower() == Weapon.Name.ToLower())
                {
                    Type = "weapon";
                    weapon = Weapon;
                }
            }
            #endregion

            #region Car/Phone Upgrade Check
            if (Type.ToLower() == "mejorar")
            {
                lock (GroupManager.Jobs)
                {
                    Group CarJob = GroupManager.Jobs.Values.FirstOrDefault(x => x.Ranks.Count > 0 && x.Ranks.Values.FirstOrDefault().HasCommand("car"));
                    Group PhoneJob = GroupManager.Jobs.Values.FirstOrDefault(x => x.Ranks.Count > 0 && x.Ranks.Values.FirstOrDefault().HasCommand("phone"));

                    if (CarJob != null && CarJob.Ranks.Values.FirstOrDefault().CanWorkHere(Room.Id))
                        Type = "mejorarcarro";

                    if (PhoneJob != null && PhoneJob.Ranks.Values.FirstOrDefault().CanWorkHere(Room.Id))
                        Type = "iphone";
                }
            }
            #endregion

            switch (Type.ToLower())
            {
                #region Weapon
                case "weapon":
                case "arma":
                    {
                        if (weapon == null)
                        {
                            Session.SendWhisper("'" + Type + "' No es un tipo de oferta válido");
                            break;
                        }

                        if (weapon.Stock < 1)
                        {
                            Session.SendWhisper("No existen " + weapon.PublicName + " Restante en inventario, Por favor, use el ':calldelivery' Comando para conseguir algunos entregados", 1);
                            return;
                        }

                        if (!GroupManager.HasJobCommand(Session, "weapon") && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                        {
                            Session.SendWhisper("Lo siento, no trabajas en la tienda de armas", 1);
                            break;
                        }

                        if (Target.GetRoleplay().OwnedWeapons.ContainsKey(weapon.Name) && Target.GetRoleplay().OwnedWeapons[weapon.Name].CanUse)
                        {
                            Session.SendWhisper("Este ciudadano ya tiene un " + weapon.PublicName + "!", 1);
                            break;
                        }

                        if (!Session.GetRoleplay().IsWorking && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                        {
                            Session.SendWhisper("Usted debe estar trabajando para ofrecer a alguien una " + weapon.PublicName + "!", 1);
                            break;
                        }

                        else
                        {
                            int Cost = (!Target.GetRoleplay().OwnedWeapons.ContainsKey(weapon.Name) ? weapon.Cost : weapon.CostFine);
                            bool HasOffer = false;
                            if (Target.GetHabbo().Credits >= Convert.ToInt32(Cost.ToString().Replace("-", "")))
                            {
                                foreach (var Offer in Target.GetRoleplay().OfferManager.ActiveOffers.Values)
                                {
                                    if (WeaponManager.Weapons.ContainsKey(Offer.Type.ToLower()))
                                        HasOffer = true;
                                }
                                if (!HasOffer)
                                {
                                    Session.Shout("*Ofrece un " + weapon.PublicName + " a " + Target.GetHabbo().Username + " por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + "*", 4);
                                    Target.GetRoleplay().OfferManager.CreateOffer(weapon.Name.ToLower(), Session.GetHabbo().Id, Convert.ToInt32(Cost.ToString().Replace("-", "")));
                                    Target.SendWhisper("Acaba de ofrecerte a " + weapon.PublicName + " por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + "! Diga ':aceptar arma' para comprarlo!", 1);
                                    break;
                                }
                                else
                                {
                                    Session.SendWhisper("A este usuario ya se le ha ofrecido un arma", 1);
                                    break;
                                }
                            }
                            else
                            {
                                Session.SendWhisper("Este ciudadano no puede " + weapon.PublicName + "!", 1);
                                break;
                            }
                        }
                    }
                #endregion

               /* #region Phone
                case "telefono":
                    {
                        if (!GroupManager.HasJobCommand(Session, "telefono") && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                        {
                            Session.SendWhisper("Usted no trabaja en el teléfono corporation!", 1);
                            break;
                        }

                        if (Target.GetRoleplay().PhoneType > 0)
                        {
                            Session.SendWhisper("Este ciudadano ya tiene un teléfono", 1);
                            break;
                        }

                        if (!Session.GetRoleplay().IsWorking && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                        {
                            Session.SendWhisper("Usted debe estar trabajando para ofrecer a alguien a teléfono", 1);
                            break;
                        }

                        else
                        {
                            int Cost = 120;
                            bool HasOffer = false;
                            if (Target.GetHabbo().Credits >= Convert.ToInt32(Cost.ToString().Replace("-", "")))
                            {
                                foreach (var Offer in Target.GetRoleplay().OfferManager.ActiveOffers.Values)
                                {
                                    if (Offer.Type.ToLower() == Type.ToLower())
                                    {
                                        HasOffer = true;
                                    }
                                }
                                if (!HasOffer)
                                {
                                    Session.Shout("*Ofrece Samsung Galaxy S4 a " + Target.GetHabbo().Username + " por $120*", 4);
                                    Target.GetRoleplay().OfferManager.CreateOffer("telefono", Session.GetHabbo().Id, Convert.ToInt32(Cost.ToString().Replace("-", "")));
                                    Target.SendWhisper("¡Recién te han ofrecido un Samsung Galaxy S4 por $100! Diga ':aceptar telefono' para comprarlo!", 1);
                                    break;
                                }
                                else
                                {
                                    Session.SendWhisper("A este usuario ya se le ha ofrecido un teléfono", 1);
                                    break;
                                }
                            }
                            else
                            {
                                Session.SendWhisper("Este ciudadano no puede teléfono", 1);
                                break;
                            }
                        }
                    }
                #endregion

                #region Phone Upgrade
                case "iphone":
                    {
                        if (!GroupManager.HasJobCommand(Session, "telefono") && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                        {
                            Session.SendWhisper("Usted no trabaja en el teléfono corporation!", 1);
                            break;
                        }

                        if (Target.GetRoleplay().PhoneType < 1)
                        {
                            Session.SendWhisper("Este ciudadano no tiene teléfono para mejorar!", 1);
                            break;
                        }

                        if (Target.GetRoleplay().PhoneType > 2)
                        {
                            Session.SendWhisper("Lo sentimos, este ciudadano ya tiene el teléfono más alto que pueden conseguir!", 1);
                            break;
                        }

                        if (!Session.GetRoleplay().IsWorking && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                        {
                            Session.SendWhisper("Usted debe estar trabajando para ofrecer a alguien un teléfono", 1);
                            break;
                        }

                        else
                        {
                            int Cost = Target.GetRoleplay().PhoneType == 1 ? 750 : 2000;
                            bool HasOffer = false;
                            string PhoneName = RoleplayManager.GetPhoneName(Target, true);

                            if (Target.GetHabbo().Credits >= Convert.ToInt32(Cost.ToString().Replace("-", "")))
                            {
                                foreach (var Offer in Target.GetRoleplay().OfferManager.ActiveOffers.Values)
                                {
                                    if (Offer.Type.ToLower() == "iphone")
                                    {
                                        HasOffer = true;
                                    }
                                }
                                if (!HasOffer)
                                {
                                    Session.Shout("*Ofrece una mejora para el " + PhoneName + " de " + Target.GetHabbo().Username + " por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + "*", 4);
                                    Target.GetRoleplay().OfferManager.CreateOffer("iphone", Session.GetHabbo().Id, Convert.ToInt32(Cost.ToString().Replace("-", "")));
                                    Target.SendWhisper("¡Recién te han ofrecido una mejora para el " + PhoneName + " teléfono por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + "! Diga ':aceptar iphone' para comprarlo!", 1);
                                    break;
                                }
                                else
                                {
                                    Session.SendWhisper("A este usuario ya se le ha ofrecido una actualización de su teléfono!", 1);
                                    break;
                                }
                            }
                            else
                            {
                                Session.SendWhisper("Este ciudadano no puede actulizar su teléfon!", 1);
                                break;
                            }
                        }
                    }
                #endregion

                #region Phone Credit
                case "credit":
                case "credits":
                case "phonecredit":
                case "comprarsaldo":
                    {
                        if (!GroupManager.HasJobCommand(Session, "telefono") && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                        {
                            Session.SendWhisper("Usted no trabaja en el teléfono corporation!", 1);
                            break;
                        }

                        if (Params.Length == 3)
                        {
                            Session.SendWhisper("Por favor ingrese la cantidad de crédito que le gustaría ofrecer al ciudadano", 1);
                            return;
                        }

                        int Amount;
                        if (!int.TryParse(Params[3], out Amount))
                        {
                            Session.SendWhisper("Ingrese una cantidad válida de teléfono Crédito que le gustaría ofrecer al ciudadano!", 1);
                            break;
                        }

                        if (Amount < 10)
                        {
                            Session.SendWhisper("Usted necesita ofrecer al ciudadano menos 10 créditos de teléfono a la vez!", 1);
                            break;
                        }

                        if (Target.GetRoleplay().PhoneType < 1)
                        {
                            Session.SendWhisper("Lo siento, este ciudadano no tiene teléfono", 1);
                            break;
                        }

                        if (!Session.GetRoleplay().IsWorking && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                        {
                            Session.SendWhisper("Usted debe estar trabajando para ofrecer a alguien teléfono credits!", 1);
                            break;
                        }

                        else
                        {
                            int Cost = Convert.ToInt32(Math.Floor((double)Amount / 2));
                            bool HasOffer = false;

                            if (Target.GetHabbo().Credits >= Convert.ToInt32(Cost.ToString().Replace("-", "")))
                            {
                                foreach (var Offer in Target.GetRoleplay().OfferManager.ActiveOffers.Values)
                                {
                                    if (Offer.Type.ToLower() == "comprarsaldo")
                                    {
                                        HasOffer = true;
                                    }
                                }
                                if (!HasOffer)
                                {
                                    Session.Shout("*Ofrece " + String.Format("{0:N0}", Amount) + " créditos para el teléfono a " + Target.GetHabbo().Username + " por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + "*", 4);
                                    Target.GetRoleplay().OfferManager.CreateOffer("comprarsaldo", Session.GetHabbo().Id, Amount);
                                    Target.SendWhisper("Acaba de ofrecerte " + String.Format("{0:N0}", Amount) + " créditos para el teléfono por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + "! Diga ':aceptar comprarsaldo' para comprarlo!", 1);
                                    break;
                                }
                                else
                                {
                                    Session.SendWhisper("A este usuario ya se le han ofrecido créditos por teléfono!", 1);
                                    break;
                                }
                            }
                            else
                            {
                                Session.SendWhisper("¡Este ciudadano no puede pagar los créditos por teléfono!", 1);
                                break;
                            }
                        }
                    }

                #endregion
                    */

                #region Bullets

                case "bullets":
                case "ammo":
                case "balas":
                    {
                        if (!GroupManager.HasJobCommand(Session, "weapon") && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                        {
                            Session.SendWhisper("Usted no trabaja en la tienda de armas", 1);
                            break;
                        }

                        if (Params.Length == 3)
                        {
                            Session.SendWhisper("Por favor ingrese la cantidad de puntos que le gustaría ofrecer al ciudadano!", 1);
                            return;
                        }

                        int Amount;
                        if (!int.TryParse(Params[3], out Amount))
                        {
                            Session.SendWhisper("¡Ingrese una cantidad válida de balas que le gustaría ofrecer al ciudadano!", 1);
                            break;
                        }

                        if (Amount < 10)
                        {
                            Session.SendWhisper("Usted necesita ofrecer al ciudadano al menos 10 balas!", 1);
                            break;
                        }

                        if (!Session.GetRoleplay().IsWorking && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                        {
                            Session.SendWhisper("Usted debe estar trabajando para ofrecer a alguien balas!", 1);
                            break;
                        }

                        else
                        {
                            int Cost = Convert.ToInt32(Math.Floor((double)Amount / 2));
                            bool HasOffer = false;

                            if (Target.GetHabbo().Credits >= Convert.ToInt32(Cost.ToString().Replace("-", "")))
                            {
                                foreach (var Offer in Target.GetRoleplay().OfferManager.ActiveOffers.Values)
                                {
                                    if (Offer.Type.ToLower() == "balas")
                                    {
                                        HasOffer = true;
                                    }
                                }
                                if (!HasOffer)
                                {
                                    Session.Shout("*Ofrece " + String.Format("{0:N0}", Amount) + " balas a " + Target.GetHabbo().Username + " por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + "*", 4);
                                    Target.GetRoleplay().OfferManager.CreateOffer("balas", Session.GetHabbo().Id, Amount);
                                    Target.SendWhisper("Acaba de ofrecerte " + String.Format("{0:N0}", Amount) + " balas por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + "! Diga ':aceptar balas' para comprarlo!", 1);
                                    break;
                                }
                                else
                                {
                                    Session.SendWhisper("Este usuario ya se le ha ofrecido balas", 1);
                                    break;
                                }
                            }
                            else
                            {
                                Session.SendWhisper("No puede pagar por las balas", 1);
                                break;
                            }
                        }
                    }

                #endregion

                #region Seeds
                case "seed":
                case "seeds":
                case "semillas":
                    {
                        if (!GroupManager.HasJobCommand(Session, "supermarket") && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                        {
                            Session.SendWhisper("Usted no trabaja en el supermarket corporation!", 1);
                            break;
                        }

                        if (Params.Length < 5)
                        {
                            Session.SendWhisper("El comando es: 'ofrecer <user> semillas <id> <amount>'!", 1);
                            return;
                        }

                        int Id;
                        if (!int.TryParse(Params[3], out Id))
                        {
                            Session.SendWhisper("Por favor, ingrese un ID de semilla válido para ofrecer al ciudadano!", 1);
                            return;
                        }

                        FarmingItem Item = FarmingManager.GetFarmingItem(Id);

                        if (Item == null)
                        {
                            Session.SendWhisper("Lo sentimos, pero no se pudo encontrar este ID de semilla!", 1);
                            return;
                        }

                        ItemData Furni;
                        if (!PolarEnvironment.GetGame().GetItemManager().GetItem(Item.BaseItem, out Furni))
                        {
                            Session.SendWhisper("Lo sentimos, pero esta semilla no se pudo encontrar!", 1);
                            return;
                        }

                        int Amount;
                        if (!int.TryParse(Params[4], out Amount))
                        {
                            Session.SendWhisper("Ingrese una cantidad válida de seeds you would like offer the citizen!", 1);
                            break;
                        }

                        if (!Target.GetRoleplay().FarmingStats.HasSeedSatchel)
                        {
                            Session.SendWhisper("Lo siento, este ciudadano no tiene semillas!", 1);
                            break;
                        }

                        int Cost = (Amount * Item.BuyPrice);

                        if (Item.LevelRequired > Target.GetRoleplay().FarmingStats.Level)
                        {
                            Session.SendWhisper("Lo sentimos, pero este ciudadano no tiene un nivel de cultivo lo suficientemente alto para esta semilla!", 1);
                            return;
                        }

                        if (!Session.GetRoleplay().IsWorking && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                        {
                            Session.SendWhisper("Usted debe estar trabajando para ofrecer a alguien semillas", 1);
                            break;
                        }
                        else
                        {
                            if (Target.GetHabbo().Credits >= Convert.ToInt32(Cost.ToString().Replace("-", "")))
                            {
                                if (Target.GetRoleplay().OfferManager.ActiveOffers.Values.Where(x => x.Type.ToLower() == "semillas").ToList().Count <= 0)
                                {
                                    Session.Shout("*Ofrece " + String.Format("{0:N0}", Amount) + " " + Furni.PublicName + " semillas a " + Target.GetHabbo().Username + " por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + "*", 4);
                                    Target.GetRoleplay().OfferManager.CreateOffer("semillas", Session.GetHabbo().Id, Amount, Item);
                                    Target.SendWhisper("Acaba de ofrecerte " + String.Format("{0:N0}", Amount) + " " + Furni.PublicName + " semillas por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + "! Diga ':aceptar semillas' para comprarlo!", 1);
                                    break;
                                }
                                else
                                {
                                    Session.SendWhisper("Este usuario ya se ha ofrecido algunas semillas!", 1);
                                    break;
                                }
                            }
                            else
                            {
                                Session.SendWhisper("No puede pagar estas semillas", 1);
                                break;
                            }
                        }
                    }

                #endregion

                #region Seed Satchel
                case "seedsatchel":
                case "bolsasemillas":
                    {
                        if (!GroupManager.HasJobCommand(Session, "supermarket") && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                        {
                            Session.SendWhisper("Usted no trabaja en el supermarket corporation!", 1);
                            break;
                        }

                        if (Target.GetRoleplay().FarmingStats.HasSeedSatchel)
                        {
                            Session.SendWhisper("Este ciudadano ya tiene una bolsa de semillas", 1);
                            break;
                        }

                        if (!Session.GetRoleplay().IsWorking && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                        {
                            Session.SendWhisper("Usted debe estar trabajando para ofrecer a alguien una bolsa de semillas", 1);
                            break;
                        }
                        else
                        {
                            int Cost = Convert.ToInt32(RoleplayData.GetData("farming", "seedsatchelcost"));

                            bool HasOffer = false;
                            if (Target.GetHabbo().Credits >= Convert.ToInt32(Cost.ToString().Replace("-", "")))
                            {
                                foreach (var Offer in Target.GetRoleplay().OfferManager.ActiveOffers.Values)
                                {
                                    if (Offer.Type.ToLower() == Type.ToLower())
                                    {
                                        HasOffer = true;
                                    }
                                }
                                if (!HasOffer)
                                {
                                    Session.Shout("*Ofrece una bolsa de semillas " + Target.GetHabbo().Username + " por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + "*", 4);
                                    Target.GetRoleplay().OfferManager.CreateOffer("bolsasemillas", Session.GetHabbo().Id, Convert.ToInt32(Cost.ToString().Replace("-", "")));
                                    Target.SendWhisper("Acaba de ofrecerte una bolsa de semillas por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + "! diga ':aceptar bolsasemillas' para comprarlo!", 1);
                                    break;
                                }
                                else
                                {
                                    Session.SendWhisper("A este usuario ya se le ha ofrecido una bolsa de semillas!", 1);
                                    break;
                                }
                            }
                            else
                            {
                                Session.SendWhisper("Este ciudadano no puede pagar una bolsa de semillas!", 1);
                                break;
                            }
                        }
                    }
                #endregion

                #region Plant Satchel
                case "plantsatchel":
                case "bolsoavegetal":
                    {
                        if (!GroupManager.HasJobCommand(Session, "supermarket") && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                        {
                            Session.SendWhisper("Usted no trabaja en el supermarket corporation!", 1);
                            break;
                        }

                        if (Target.GetRoleplay().FarmingStats.HasPlantSatchel)
                        {
                            Session.SendWhisper("Este ciudadano ya tiene un bolso vegetal!", 1);
                            break;
                        }

                        if (!Session.GetRoleplay().IsWorking && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                        {
                            Session.SendWhisper("Usted debe estar trabajando para ofrecer a alguien un bolso vegetal!", 1);
                            break;
                        }
                        else
                        {
                            int Cost = Convert.ToInt32(RoleplayData.GetData("farming", "plantsatchelcost"));

                            bool HasOffer = false;
                            if (Target.GetHabbo().Credits >= Convert.ToInt32(Cost.ToString().Replace("-", "")))
                            {
                                foreach (var Offer in Target.GetRoleplay().OfferManager.ActiveOffers.Values)
                                {
                                    if (Offer.Type.ToLower() == Type.ToLower())
                                    {
                                        HasOffer = true;
                                    }
                                }
                                if (!HasOffer)
                                {
                                    Session.Shout("*Ofrece un bolso vegetal a " + Target.GetHabbo().Username + " por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + "*", 4);
                                    Target.GetRoleplay().OfferManager.CreateOffer("plantsatchel", Session.GetHabbo().Id, Convert.ToInt32(Cost.ToString().Replace("-", "")));
                                    Target.SendWhisper("Acaba de ofrecerte un bolso vegetal por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + "! Diga ':aceptar bolsavegetal' para comprarlo!", 1);
                                    break;
                                }
                                else
                                {
                                    Session.SendWhisper("A este usuario ya se le ha ofrecido un Plant Satchel!", 1);
                                    break;
                                }
                            }
                            else
                            {
                                Session.SendWhisper("Este ciudadano no puede Plant Satchel!", 1);
                                break;
                            }
                        }
                    }
                #endregion

                #region Car
                case "carro":
                case "car":
                case "auto":
                    {
                        if (!GroupManager.HasJobCommand(Session, "carro") && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                        {
                            Session.SendWhisper("Usted no trabaja en el car corporation!", 1);
                            break;
                        }

                        if (Target.GetRoleplay().CarType > 0)
                        {
                            Session.SendWhisper("Este ciudadano ya tiene un carro!", 1);
                            break;
                        }

                        if (!Session.GetRoleplay().IsWorking && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                        {
                            Session.SendWhisper("Usted debe estar trabajando para ofrecer a alguien un carro!", 1);
                            break;
                        }

                        else
                        {
                            int Cost = 1000;
                            bool HasOffer = false;
                            if (Target.GetHabbo().Credits >= Convert.ToInt32(Cost.ToString().Replace("-", "")))
                            {
                                foreach (var Offer in Target.GetRoleplay().OfferManager.ActiveOffers.Values)
                                {
                                    if (Offer.Type.ToLower() == Type.ToLower())
                                    {
                                        HasOffer = true;
                                    }
                                }
                                if (!HasOffer)
                                {
                                    Session.Shout("*Ofrece un Toyota Corolla a " + Target.GetHabbo().Username + " por $1,000*", 4);
                                    Target.GetRoleplay().OfferManager.CreateOffer("carro", Session.GetHabbo().Id, Convert.ToInt32(Cost.ToString().Replace("-", "")));
                                    Target.SendWhisper("Acaba de ofrecerte un Toyota Corolla por $1,000! Diga ':aceptar carro' para comprarlo!", 1);
                                    break;
                                }
                                else
                                {
                                    Session.SendWhisper("A este usuario ya se le ha ofrecido un carro!", 1);
                                    break;
                                }
                            }
                            else
                            {
                                Session.SendWhisper("Este ciudadano no puede pagar un carro!", 1);
                                break;
                            }
                        }
                    }
                #endregion

                #region Cigarrettes
                case "cigarrillos":
                case "cigarros":
                    {
                        if (!GroupManager.HasJobCommand(Session, "cigarrillos") && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                        {
                            Session.SendWhisper("Usted no trabaja en la farmacia", 1);
                            break;
                        }

                        if (!Session.GetRoleplay().IsWorking && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                        {
                            Session.SendWhisper("Usted debe estar trabajando para ofrecer a alguien un cigarrillo!", 1);
                            break;
                        }

                        else
                        {
                            int Cost = 350;
                            bool HasOffer = false;
                            if (Target.GetHabbo().Credits >= Convert.ToInt32(Cost.ToString().Replace("-", "")))
                            {
                                foreach (var Offer in Target.GetRoleplay().OfferManager.ActiveOffers.Values)
                                {
                                    if (Offer.Type.ToLower() == Type.ToLower())
                                    {
                                        HasOffer = true;
                                    }
                                }
                                if (!HasOffer)
                                {
                                    Session.Shout("*Ofrece una caja de cigarrillos a " + Target.GetHabbo().Username + " por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + "*", 4);
                                    Target.GetRoleplay().OfferManager.CreateOffer("cigarrillos", Session.GetHabbo().Id, Convert.ToInt32(Cost.ToString().Replace("-", "")));
                                    Target.SendWhisper("Acaba de ofrecerte una caja de cigarrillos por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + " Diga ':aceptar cigarrillos' para comprarlo!", 1);
                                    break;
                                }
                                else
                                {
                                    Session.SendWhisper("A este usuario ya se le ha ofrecido un cigarrilos", 1);
                                    break;
                                }
                            }
                            else
                            {
                                Session.SendWhisper("Este ciudadano no puede pagar un caja de cigarrillos", 1);
                                break;
                            }
                        }
                    }
                #endregion

                #region Cocaina
                case "cocaina":
                    {
                        /*if (!GroupManager.HasGangCommand(Session, "cocaina"))
                        {
                            Session.SendWhisper("Necesitas ser traficante de drogas de una pandilla para venderla", 1);
                            return;
                        }

                        else
                        {*/
                            int Cost = Convert.ToInt32(Params[3]);
                            bool HasOffer = false;
                            if (Target.GetHabbo().Credits >= Convert.ToInt32(Cost.ToString().Replace("-", "")))
                            {
                                foreach (var Offer in Target.GetRoleplay().OfferManager.ActiveOffers.Values)
                                {
                                    if (Offer.Type.ToLower() == Type.ToLower())
                                    {
                                        HasOffer = true;
                                    }
                                }
                                if (!HasOffer)
                                {
                                    Session.Shout("*Ofrece 5 gramos de cocaina a " + Target.GetHabbo().Username + " por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + "*", 4);
                                    Target.GetRoleplay().OfferManager.CreateOffer("cocaina", Session.GetHabbo().Id, Convert.ToInt32(Cost.ToString().Replace("-", "")));
                                    Target.SendWhisper("Acaba de ofrecerte 5g de cocaina por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + " Diga ':aceptar cocaina' para comprarlo!", 1);
                                    break;
                                }
                                else
                                {
                                    Session.SendWhisper("A este usuario ya se le ha ofrecido un cocaina", 1);
                                    break;
                                }
                            }
                            else
                            {
                                Session.SendWhisper("Este ciudadano no puede pagar la cocaina", 1);
                                break;
                            }
                       // }
                    }
                #endregion

                #region Caramelos
                case "caramelos":
                    {
                        if (!GroupManager.HasGangCommand(Session, "caramelos") && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                        {
                            Session.SendWhisper("Necesitas ser vendedor de Caramelos", 1);
                            return;
                        }

                        else
                        {
                            int Cost = 2000;
                            bool HasOffer = false;
                            if (Target.GetHabbo().Credits >= Convert.ToInt32(Cost.ToString().Replace("-", "")))
                            {
                                foreach (var Offer in Target.GetRoleplay().OfferManager.ActiveOffers.Values)
                                {
                                    if (Offer.Type.ToLower() == Type.ToLower())
                                    {
                                        HasOffer = true;
                                    }
                                }
                                if (!HasOffer)
                                {
                                    Session.Shout("*Ofrece 5 unidades de caramelos a " + Target.GetHabbo().Username + " por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + "*", 4);
                                    Target.GetRoleplay().OfferManager.CreateOffer("caramelos", Session.GetHabbo().Id, Convert.ToInt32(Cost.ToString().Replace("-", "")));
                                    Target.SendWhisper("Acaba de ofrecerte 5 caramelos por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + " Diga ':aceptar caramelos' para comprarlo!", 1);
                                    break;
                                }
                                else
                                {
                                    Session.SendWhisper("A este usuario ya se le ha ofrecido un caramelos", 1);
                                    break;
                                }
                            }
                            else
                            {
                                Session.SendWhisper("Este ciudadano no puede pagar un caja de caramelos", 1);
                                break;
                            }
                        }
                    }
                #endregion

                #region Medicina
                case "medicina":
                    {
                        if (!GroupManager.HasJobCommand(Session, "medicina"))
                        {
                            Session.SendWhisper("Necesitas ser farmaceutico para vender medicinas", 1);
                            return;
                        }

                        else
                        {
                            int Cost = 10000;
                            bool HasOffer = false;
                            if (Target.GetHabbo().Credits >= Convert.ToInt32(Cost.ToString().Replace("-", "")))
                            {
                                foreach (var Offer in Target.GetRoleplay().OfferManager.ActiveOffers.Values)
                                {
                                    if (Offer.Type.ToLower() == Type.ToLower())
                                    {
                                        HasOffer = true;
                                    }
                                }
                                if (!HasOffer)
                                {
                                    Session.Shout("*Ofrece 50 Cc de medicinas a " + Target.GetHabbo().Username + " por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + "*", 4);
                                    Target.GetRoleplay().OfferManager.CreateOffer("medicina", Session.GetHabbo().Id, Convert.ToInt32(Cost.ToString().Replace("-", "")));
                                    Target.SendWhisper("Acaba de ofrecerte 50Cc de medicina por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + " Diga ':aceptar medicina' para comprarlo!", 1);
                                    break;
                                }
                                else
                                {
                                    Session.SendWhisper("A este usuario ya se le ha ofrecido una medicina", 1);
                                    break;
                                }
                            }
                            else
                            {
                                Session.SendWhisper("Este ciudadano no puede pagar un caja de medicina", 1);
                                break;
                            }
                        }
                    }
                #endregion

                #region Marihuana
                case "marihuana":
                    {

                        /*if (!GroupManager.HasGangCommand(Session, "marihuana"))
                        {
                            Session.SendWhisper("Necesitas ser traficante de drogas de una pandilla para venderla", 1);
                            return;
                        }*/

                        if (Session.GetRoleplay().Weed < 10)
                        {
                            Session.SendWhisper("¡Necesitas tener por lo menos 10 porros para vender!");
                            return;
                        }

                        else
                        {
                            int Cost = Convert.ToInt32(Params[3]);
                            bool HasOffer = false;
                            if (Target.GetHabbo().Credits >= Convert.ToInt32(Cost.ToString().Replace("-", "")))
                            {
                                foreach (var Offer in Target.GetRoleplay().OfferManager.ActiveOffers.Values)
                                {
                                    if (Offer.Type.ToLower() == Type.ToLower())
                                    {
                                        HasOffer = true;
                                    }
                                }
                                if (!HasOffer)
                                {
                                    Session.Shout("*Ofrece 10 porros de marihuana a " + Target.GetHabbo().Username + " por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + "*", 4);
                                    Target.GetRoleplay().OfferManager.CreateOffer("marihuana", Session.GetHabbo().Id, Convert.ToInt32(Cost.ToString().Replace("-", "")));
                                    Target.SendWhisper("Acaba de ofrecerte 10 porros de marihuana por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + " Diga ':aceptar marihuana' para comprarlo!", 1);
                                    break;
                                }
                                else
                                {
                                    Session.SendWhisper("A este usuario ya se le ha ofrecido un Marihuana", 1);
                                    break;
                                }
                            }
                            else
                            {
                                Session.SendWhisper("Este ciudadano no puede pagar porros de Marihuana", 1);
                                break;
                            }
                        }
                    }
                #endregion

                #region Heroina
                case "heroina":
                    {

                        /*if (!GroupManager.HasGangCommand(Session, "marihuana"))
                        {
                            Session.SendWhisper("Necesitas ser traficante de drogas de una pandilla para venderla", 1);
                            return;
                        }*/

                        if (Session.GetRoleplay().Heroina < 10)
                        {
                            Session.SendWhisper("¡Necesitas tener por lo menos 10cc para vender!");
                            return;
                        }

                        else
                        {
                            int Cost = Convert.ToInt32(Params[3]);
                            bool HasOffer = false;
                            if (Target.GetHabbo().Credits >= Convert.ToInt32(Cost.ToString().Replace("-", "")))
                            {
                                foreach (var Offer in Target.GetRoleplay().OfferManager.ActiveOffers.Values)
                                {
                                    if (Offer.Type.ToLower() == Type.ToLower())
                                    {
                                        HasOffer = true;
                                    }
                                }
                                if (!HasOffer)
                                {
                                    Session.Shout("*Ofrece 10cc de Heroina a " + Target.GetHabbo().Username + " por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + "*", 4);
                                    Target.GetRoleplay().OfferManager.CreateOffer("heroina", Session.GetHabbo().Id, Convert.ToInt32(Cost.ToString().Replace("-", "")));
                                    Target.SendWhisper("Acaba de ofrecerte 10c de Heroina por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + " Diga ':aceptar heroina' para comprarlo!", 1);
                                    break;
                                }
                                else
                                {
                                    Session.SendWhisper("A este usuario ya se le ha ofrecido un Heroina", 1);
                                    break;
                                }
                            }
                            else
                            {
                                Session.SendWhisper("Este ciudadano no puede pagar la Heroina", 1);
                                break;
                            }
                        }
                    }
                #endregion


                #region Car Upgrade
                case "mejorarcarro":
                    {
                        if (!GroupManager.HasJobCommand(Session, "carro") && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                        {
                            Session.SendWhisper("Usted no trabaja en el car corporation!", 1);
                            break;
                        }

                        if (Target.GetRoleplay().CarType < 1)
                        {
                            Session.SendWhisper("Este ciudadano no tiene carro para mejorar!", 1);
                            break;
                        }

                        if (Target.GetRoleplay().CarType > 2)
                        {
                            Session.SendWhisper("Lo siento, este ciudadano ya tiene el coche más alto que puede obtener!", 1);
                            break;
                        }

                        if (!Session.GetRoleplay().IsWorking && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                        {
                            Session.SendWhisper("Usted debe estar trabajando para ofrecer a alguien un carro!", 1);
                            break;
                        }

                        else
                        {
                            int Cost = Target.GetRoleplay().CarType == 1 ? 2500 : 5000;
                            bool HasOffer = false;
                            string CarName = RoleplayManager.GetCarName(Target, true);

                            if (Target.GetHabbo().Credits >= Convert.ToInt32(Cost.ToString().Replace("-", "")))
                            {
                                foreach (var Offer in Target.GetRoleplay().OfferManager.ActiveOffers.Values)
                                {
                                    if (Offer.Type.ToLower() == "mejorarcarro")
                                    {
                                        HasOffer = true;
                                    }
                                }
                                if (!HasOffer)
                                {
                                    Session.Shout("*Ofrece una mejora para el " + CarName + " de " + Target.GetHabbo().Username + " por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + "*", 4);
                                    Target.GetRoleplay().OfferManager.CreateOffer("carupgrade", Session.GetHabbo().Id, Convert.ToInt32(Cost.ToString().Replace("-", "")));
                                    Target.SendWhisper("¡Recién te han ofrecido una mejora para el " + CarName + " por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + "! Diga ':aceptar mejorarcarro' para comprarlo!", 1);
                                    break;
                                }
                                else
                                {
                                    Session.SendWhisper("A este usuario ya se le ha ofrecido una mejora a su carro!", 1);
                                    break;
                                }
                            }
                            else
                            {
                                Session.SendWhisper("Este ciudadano no puede pagar esto!", 1);
                                break;
                            }
                        }
                    }
                #endregion

                #region Fuel
                case "gasolina":
                    {
                        if (!GroupManager.HasJobCommand(Session, "carro") && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                        {
                            Session.SendWhisper("Usted no trabaja en el car store corporation!", 1);
                            break;
                        }

                        if (Params.Length == 3)
                        {
                            Session.SendWhisper("Introduzca la cantidad de combustible que le gustaría ofrecer al ciudadano!", 1);
                            return;
                        }

                        int Amount;
                        if (!int.TryParse(Params[3], out Amount))
                        {
                            Session.SendWhisper("Ingrese una cantidad válida de combustible que le gustaría ofrecer al ciudadano!", 1);
                            break;
                        }

                        if (Amount < 10)
                        {
                            Session.SendWhisper("Usted necesita ofrecer al ciudadano menos 10 galones de combustible a la vez!", 1);
                            break;
                        }

                        if (Target.GetRoleplay().CarType < 1)
                        {
                            Session.SendWhisper("Lo siento, este ciudadano no tiene carro!", 1);
                            break;
                        }

                        if (!Session.GetRoleplay().IsWorking && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                        {
                            Session.SendWhisper("Usted debe estar trabajando para ofrecer a alguien gasolina!", 1);
                            break;
                        }

                        else
                        {
                            int Cost = Convert.ToInt32(Math.Floor((double)(Amount * 2) / 3));
                            bool HasOffer = false;

                            if (Target.GetHabbo().Credits >= Convert.ToInt32(Cost.ToString().Replace("-", "")))
                            {
                                foreach (var Offer in Target.GetRoleplay().OfferManager.ActiveOffers.Values)
                                {
                                    if (Offer.Type.ToLower() == "gasolina")
                                    {
                                        HasOffer = true;
                                    }
                                }
                                if (!HasOffer)
                                {
                                    Session.Shout("*Ofrece " + String.Format("{0:N0}", Amount) + " galones de gasolina a " + Target.GetHabbo().Username + " por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + "*", 4);
                                    Target.GetRoleplay().OfferManager.CreateOffer("gasolina", Session.GetHabbo().Id, Amount);
                                    Target.SendWhisper("Acaba de ofrecerte " + String.Format("{0:N0}", Amount) + " galones de gasolina por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + "! Diga ':aceptar gasolina' para comprarlo!", 1);
                                    break;
                                }
                                else
                                {
                                    Session.SendWhisper("A este usuario ya se le ha ofrecido combustible", 1);
                                    break;
                                }
                            }
                            else
                            {
                                Session.SendWhisper("no puede pagar gasolina", 1);
                                break;
                            }
                        }
                    }

                #endregion

                #region Pildoras
                case "pildoras":
                    {
                        if (!GroupManager.HasJobCommand(Session, "pildoras") && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                        {
                            Session.SendWhisper("Usted no trabaja en la farmacia", 1);
                            break;
                        }

                        if (!Session.GetRoleplay().IsWorking && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                        {
                            Session.SendWhisper("Usted debe estar trabajando para ofrecer a alguien esteroides!", 1);
                            break;
                        }

                        else
                        {
                            int Cost = 10000;
                            bool HasOffer = false;
                            if (Target.GetHabbo().Credits >= Convert.ToInt32(Cost.ToString().Replace("-", "")))
                            {
                                foreach (var Offer in Target.GetRoleplay().OfferManager.ActiveOffers.Values)
                                {
                                    if (Offer.Type.ToLower() == Type.ToLower())
                                    {
                                        HasOffer = true;
                                    }
                                }
                                if (!HasOffer)
                                {
                                    Session.Shout("*Ofrece unas pildoras de fuerza a " + Target.GetHabbo().Username + " por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + "*", 4);
                                    Target.GetRoleplay().OfferManager.CreateOffer("pildoras", Session.GetHabbo().Id, Convert.ToInt32(Cost.ToString().Replace("-", "")));
                                    Target.SendWhisper("Recuerda que te aumenta [50+ EXPERIENCIA EN FUERZA] al consumirlo por  $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + " Diga ':aceptar pildoras' para comprarlo!", 1);
                                    break;
                                }
                                else
                                {
                                    Session.SendWhisper("A este usuario ya se le ha ofrecido un pildoras", 1);
                                    break;
                                }
                            }
                            else
                            {
                                Session.SendWhisper("Este ciudadano no puede pagar un caja de pildoras", 1);
                                break;
                            }
                        }
                    }
                #endregion

                #region Mamada
                case "mamada":
                    {
                        if (!GroupManager.HasJobCommand(Session, "mamada") && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                        {
                            Session.SendWhisper("Usted no trabaja de prostitut@", 1);
                            break;
                        }

                        if (!Session.GetRoleplay().IsWorking && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                        {
                            Session.SendWhisper("¡Usted debe estar trabajando para ofrecer a alguien una mamada!", 1);
                            break;
                        }
                        if (TargetClient == Session)
                        {
                            Session.SendWhisper("¡No puedes pincharte a ti mismo, pidele apoyo a un compañero de trabajo!", 1);
                            return;
                        }

                        else
                        {
                            int Cost = Convert.ToInt32(Params[3]);
                            bool HasOffer = false;
                            if (Target.GetHabbo().Credits >= Convert.ToInt32(Cost.ToString().Replace("-", "")))
                            {
                                foreach (var Offer in Target.GetRoleplay().OfferManager.ActiveOffers.Values)
                                {
                                    if (Offer.Type.ToLower() == Type.ToLower())
                                    {
                                        HasOffer = true;
                                    }
                                }
                                if (!HasOffer)
                                {
                                    Session.Shout("*Ofrece una rica mamada a " + Target.GetHabbo().Username + " por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + "*", 4);
                                    Target.GetRoleplay().OfferManager.CreateOffer("mamada", Session.GetHabbo().Id, Convert.ToInt32(Cost.ToString().Replace("-", "")));
                                    Target.SendWhisper("Recuerda que te aumenta [50+ FELICIDAD] $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + " Diga ':aceptar mamada' para que te la chupen!", 1);
                                    break;
                                }
                                else
                                {
                                    Session.SendWhisper("A este usuario ya se le ha ofrecido una mamada", 1);
                                    break;
                                }
                            }
                            else
                            {
                                Session.SendWhisper("Este ciudadano no puede pagar una mamada", 1);
                                break;
                            }
                        }
                    }
                #endregion

                #region Default
                default:
                    {
                        Session.SendWhisper("'" + Type + "'No es una oferta valida", 1);
                        break;
                    }
                    #endregion
            }
        }
    }
}