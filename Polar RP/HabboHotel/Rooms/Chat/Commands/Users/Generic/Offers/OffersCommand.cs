using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.Communication.Packets.Outgoing.Notifications;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.RoleplayUsers.Offers;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Weapons;
using Polar.HabboRoleplay.Bots;
using Polar.HabboRoleplay.Farming;
using Polar.HabboHotel.Items;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Offers
{
    class OffersCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_offers_offers_list"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Le informa de cualquier oferta que haya recibido, en su caso."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            StringBuilder Message = new StringBuilder().Append("--- Ofertas actuales activas ---\n\n");

            if (Session.GetRoleplay().OfferManager.ActiveOffers.Count <= 0)
                Message.Append("Actualmente no tienes ofertas activas\n");
            else
                Message.Append("Correcto: ':aceptar tipo' (reemplazar tipo con telefono/casarse/gang/clothing, etc.) para aceptar la oferta\n\n");

            lock (Session.GetRoleplay().OfferManager.ActiveOffers.Values)
            {
                foreach (var Offer in Session.GetRoleplay().OfferManager.ActiveOffers.Values)
                {
                    if (Offer == null)
                        continue;

                    string Name = "";
                    if (Offer.Params != null && Offer.Params.Length > 0)
                    {
                        if (Offer.Type.ToLower() == "seeds" && Offer.Params.ToList().Count == 1)
                        {
                            var OffererCache = PolarEnvironment.GetGame().GetCacheManager().GenerateUser(Offer.OffererId);
                            Name = OffererCache.Username;
                        }
                        else
                        {
                            RoleplayBotAI Bot = (RoleplayBotAI)Offer.Params[0];
                            Name = "[BOT] " + Bot.GetBotRoleplay().Name;
                        }
                    }
                    else
                    {
                        var OffererCache = PolarEnvironment.GetGame().GetCacheManager().GenerateUser(Offer.OffererId);
                        Name = OffererCache.Username;
                    }

                    //string PhoneName = RoleplayManager.GetPhoneName(Session, true);
                    string CarName = RoleplayManager.GetCarName(Session, true);

                    if (Offer.Type.ToLower() == "casarme")
                        Message.Append("Marriage: " + Name + " Les ha ofrecido su mano en matrimonio\n\n");
                    else if (Offer.Type.ToLower() == "fix_mecanico")
                        Message.Append("Reparación Vehículo: Por parte de " + Name + " por $" + Offer.Cost + " \n\n");
                    else if (Offer.Type.ToLower() == "fix_armero")
                        Message.Append("Reparación Arma: Por parte de " + Name + " por $" + Offer.Cost + " \n\n");
                    else if (Offer.Type.ToLower() == "bolsasemillas")
                        Message.Append("Seed Satchel: Una bolsa de semillas por $" + String.Format("{0:N0}", Offer.Cost) + " de " + Name + "!\n\n");
                    else if (Offer.Type.ToLower() == "plantsatchel")
                        Message.Append("Plant Satchel: Una bolsa de plantas por $" + String.Format("{0:N0}", Offer.Cost) + " de " + Name + "!\n\n");
                    else if (Offer.Type.ToLower() == "semillas")
                    {
                        FarmingItem Item = (FarmingItem)Offer.Params[1];

                        ItemData Furni;
                        if (PolarEnvironment.GetGame().GetItemManager().GetItem(Item.BaseItem, out Furni))
                            Message.Append("Seeds: " + Name + " Te ha ofrecido " + Offer.Cost + " " + Furni.PublicName + " semillas por $" + String.Format("{0:N0}", (Offer.Cost * Item.BuyPrice)) + "!\n\n");
                    }
                    else if (Offer.Type.ToLower() == "clothing")
                        Message.Append("Clothing: Descuento del 5% en la próxima compra de ropa de " + Name + "!\n\n");
                    else if (Offer.Type.ToLower() == "telefono")
                        Message.Append("Phone: One Samsung Galaxi S4 por $100 de " + Name + "!\n\n");
                    else if (Offer.Type.ToLower() == "carro")
                        Message.Append("Car: One Toyota Corolla por $1,000 de " + Name + "!\n\n");
                    else if (Offer.Type.ToLower() == "corriente")
                        Message.Append("Chequings: Una cuenta corriente de " + Name + "!\n\n");
                    else if (Offer.Type.ToLower() == "ahorro")
                        Message.Append("Savings: Una cuenta de ahorros por $" + String.Format("{0:N0}", Offer.Cost) + " de " + Name + "!\n\n");
                    /*else if (Offer.Type.ToLower() == "mejorartelefono")
                        Message.Append("Phone Upgrade: Una actualización por " + PhoneName + " por $" + String.Format("{0:N0}", Offer.Cost) + " de " + Name + "!\n\n");
                    */else if (Offer.Type.ToLower() == "mejorarcarro")
                        Message.Append("Car Upgrade: Una mejora de " + CarName + " por $" + String.Format("{0:N0}", Offer.Cost) + " de " + Name + "!\n\n");
                    else if (Offer.Type.ToLower() == "pandilla")
                    {
                        var Gang = Groups.GroupManager.GetGang(Offer.Cost);

                        if (Gang != null)
                            Message.Append("Gang: Invitado a unirse a la pandilla'" + Gang.Name + "' de " + Name + "!\n\n");
                    }
                    else if (Offer.Type.ToLower() == "trabajo")
                    {
                        var Job = Groups.GroupManager.GetJob(Offer.Cost);
                        var JobRank = Groups.GroupManager.GetJobRank(Job.Id, 1);

                        if (Job != null)
                            Message.Append("Job: Invitado a unirse a la '" + Job.Name + "' Empresa como '" + JobRank.Name + "' de " + Name + "!\n\n");
                    }
                    else if (WeaponManager.Weapons.ContainsKey(Offer.Type.ToLower()))
                    {
                        Weapon weapon = WeaponManager.Weapons[Offer.Type.ToLower()];

                        if (weapon != null)
                            Message.Append("Weapon: Una " + weapon.PublicName + " por $" + String.Format("{0:N0}", weapon.Cost) + " de " + Name + "!\n\n");
                    }
                }
            }
            Session.SendMessage(new MOTDNotificationComposer(Message.ToString()));
        }
    }
}