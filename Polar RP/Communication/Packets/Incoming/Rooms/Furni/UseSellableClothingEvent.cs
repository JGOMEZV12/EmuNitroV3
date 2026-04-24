using System;
using System.Linq;
using System.Text;
using System.Threading;

using Polar.HabboHotel.Items;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Catalog.Clothing;

using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using Polar.Communication.Packets.Outgoing.Inventory.AvatarEffects;
using Polar.Database.Interfaces;
using Polar.HabboRoleplay.RoleplayUsers.Offers;

namespace Polar.Communication.Packets.Incoming.Rooms.Furni
{
    internal class UseSellableClothingEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            if (Session == null || Session.GetHabbo() == null || Session.GetRoleplay() == null|| !Session.GetHabbo().InRoom)
                return;

            Room Room = Session.GetHabbo().CurrentRoom;

            if (Room == null)
                return;

            int ItemId = Packet.PopInt();

            Item Item = Room.GetRoomItemHandler().GetItem(ItemId);

            if (Item == null)
            {
                Session.GetRoleplay().PurchasingClothing = true;
                return;
            }

            if (Item.Data == null)
            {
                Session.GetRoleplay().PurchasingClothing = true;
                return;
            }

            if (Item.Data.InteractionType != InteractionType.PURCHASABLE_CLOTHING)
            {
                Session.SendNotification("¡Vaya, este artículo no se fija como artículo vendible de la ropa!");
                Session.GetRoleplay().PurchasingClothing = true;
                return;
            }

            if (Item.Data.BehaviourData == 0)
            {
                Session.SendNotification("Vaya, este artículo no tiene una configuración de ropa vinculada, ¡Ya se te ha colocado!");
                return;
            }

            ClothingItem clothing;
            if (!PolarEnvironment.GetGame().GetCatalog().GetClothingManager().TryGetClothing(Item.Data.BehaviourData, out clothing))
            {
                Session.SendNotification("¡Vaya, no pudimos encontrar esta pieza de ropa!");
                return;
            }

            if (Item.Data.ClothingId == 0)
            {
                Session.SendNotification("¡Vaya, este artículo no tiene una configuración de ropa de enlace, por favor reportalo a los staff!");
                Session.GetRoleplay().PurchasingClothing = true;
                return;
            }

            ClothingItem Clothing = null;
            if (!PolarEnvironment.GetGame().GetCatalog().GetClothingManager().TryGetClothing(Item.Data.ClothingId, out Clothing))
            {
                Session.SendNotification("¡Vaya, no pudimos encontrar esta pieza de ropa!");
                Session.GetRoleplay().PurchasingClothing = true;
                return;
            }

            if (Session.GetRoleplay().PurchasingClothing)
                return;

            if (Session.GetRoleplay().Clothing != Clothing)
            {
                string Discount = "";
                if (Session.GetRoleplay().OfferManager.ActiveOffers.ContainsKey("clothing"))
                {
                    int Bonus = Convert.ToInt32((double)Clothing.Cost * 0.05);
                    int NewCost = Clothing.Cost - Bonus;

                    Discount = " ($" + String.Format("{0:N0}", NewCost) + " Debido al 5% de descuento en la ropa)";
                }

                Session.SendWhisper("Esta ropa te costará $" + String.Format("{0:N0}", Clothing.Cost) + "" + (Discount == null ? "" : Discount) + " ¡Haga clic nuevamente en la prenda, si realmente quieres comprarla!", 1);
                Session.GetRoleplay().Clothing = Clothing;
                Session.GetRoleplay().PurchasingClothing = true;
                return;
            }

            if (Session.GetHabbo().Credits < Clothing.Cost)
            {
                Session.SendWhisper("¡Lo sentimos, no tienes suficiente dinero para comprar esta ropa!", 1);
                Session.GetRoleplay().PurchasingClothing = true;
                return;
            }

            Session.GetRoleplay().Clothing = null;
            Session.GetHabbo().GetClothing().AddClothing(Clothing.ClothingName, Clothing.PartIds);
            Session.SendMessage(new FigureSetIdsComposer(Session.GetHabbo().GetClothing().GetClothingAllParts));
            Session.Shout("*Ha Comprado " + Item.GetBaseItem().PublicName + " por $" + String.Format("{0:N0}", Clothing.Cost) + "*", 4);
            //Session.SendMessage(new RoomNotificationComposer("figureset.redeemed.success"));
            Session.SendMessage(new RoomNotificationComposer("purchased_clothing", "message", "¡Hurra! Has comprado correctamente el '" + Item.GetBaseItem().PublicName + "' por $" + String.Format("{0:N0}", Clothing.Cost) + "!"));
            Session.SendWhisper("Si por alguna razón no puede ver su nueva ropa, vuelva a recargar la comunidad", 1);

            #region Clothing Discount Check
            if (Session.GetRoleplay().OfferManager.ActiveOffers.ContainsKey("clothing"))
            {
                if (Clothing.Cost > 0)
                {
                    RoleplayOffer Offer = Session.GetRoleplay().OfferManager.ActiveOffers["clothing"];
                    int Bonus = Convert.ToInt32((double)Clothing.Cost * 0.05);

                    if (Offer.Params == null)
                    {
                        var Offerer = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Offer.OffererId);

                        if (Offerer != null && Offerer.GetHabbo() != null)
                        {
                            Offerer.GetHabbo().Credits += Bonus;
                            Offerer.GetHabbo().UpdateCreditsBalance();
                            Offerer.SendWhisper("Has recibido un bono de $" + String.Format("{0:N0}", Bonus) + " por que " + Session.GetHabbo().Username + " comprado " + Item.GetBaseItem().PublicName + "!", 1);
                            PolarEnvironment.GetGame().GetAchievementManager().ProgressAchievement(Offerer, "ACH_Discounting", 1);
                        }
                    }

                    RoleplayOffer Junk;
                    Session.GetRoleplay().OfferManager.ActiveOffers.TryRemove("clothing", out Junk);

                    int NewCost = Clothing.Cost - Bonus;

                    if (NewCost > 0)
                    {
                        Session.GetHabbo().Credits -= NewCost;
                        Session.GetHabbo().UpdateCreditsBalance();
                    }
                    return;
                }
                else
                {
                    Session.SendWhisper("Usted todavía tiene el descuento para la próxima vez que compre un artículo de ropa", 1);
                    return;
                }
            }
            else
            {
                if (Clothing.Cost > 0)
                {
                    Session.GetHabbo().Credits -= Clothing.Cost;
                    Session.GetHabbo().UpdateCreditsBalance();
                }
            }
            #endregion

            return;
        }
    }
}
