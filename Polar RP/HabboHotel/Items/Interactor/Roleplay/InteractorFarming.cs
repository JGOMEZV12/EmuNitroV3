using System;
using System.Linq;
using Polar.HabboHotel.GameClients;
using System.Collections.Generic;
using Polar.Communication.Packets.Outgoing.Inventory.Furni;
using Polar.Communication.Packets.Outgoing.Rooms.Furni.Crafting;
using Polar.HabboHotel.Rooms;
using System.Threading;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Farming;
using Polar.Utilities;
using System.Drawing;

namespace Polar.HabboHotel.Items.Interactor
{
    internal class InteractorFarming : IFurniInteractor
    {
        public void OnPlace(GameClient Session, Item Item)
        {

        }

        public void OnRemove(GameClient Session, Item Item)
        {

        }

        public void OnTrigger(GameClient Session, Item Item, int Request, bool HasRights)
        {
            RoomUser roomUser = null;

            if (Session != null && Session.GetRoleplay() != null)
                roomUser = Item.GetRoom().GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);

            if (roomUser == null)
                return;

            if (Item.FarmingData == null)
            {
                Session.SendWhisper("Lo sentimos, este artículo no tiene datos de cultivo!", 1);
                return;
            }

            FarmingItem FarmingItem = FarmingManager.GetFarmingItem(Item.GetBaseItem().ItemName);

            if (FarmingItem == null)
            {
                Session.SendWhisper("Lo siento, esto no es un elemento agrícola!", 1);
                return;
            }

            if (Item.FarmingData.OwnerId != 0 && Item.FarmingData.OwnerId != Session.GetHabbo().Id)
            {
                Session.SendWhisper("Alguien más posee esta planta!", 1);
                return;
            }

            if (Session.GetRoleplay().TryGetCooldown("farming", false))
                return;

            /*#region Check Rentable Space
            if (Item.GetRoom().GetRoomItemHandler() != null && Item.GetRoom().GetRoomItemHandler().GetFloor != null)
            {
                lock (Item.GetRoom().GetRoomItemHandler().GetFloor)
                {
                    List<Item> OwnedRentableSpaces = Item.GetRoom().GetRoomItemHandler().GetFloor.Where(x => x.GetBaseItem().InteractionType == InteractionType.RENTABLE_SPACE && x.RentableSpaceData != null && x.RentableSpaceData.FarmingSpace != null && x.RentableSpaceData.FarmingSpace.OwnerId == Session.GetHabbo().Id).ToList();
                    if (OwnedRentableSpaces.Count <= 0)
                    {
                        Session.SendWhisper("You do not own this plot of land to farm on!", 1);
                        Session.GetRoleplay().CooldownManager.CreateCooldown("farming", 500);
                        return;
                    }

                    Item SpaceItem = OwnedRentableSpaces.FirstOrDefault();
                    List<Point> SpacePoints = SpaceItem.GetAffectedTiles;

                    if (!SpacePoints.Contains(Item.Coordinate))
                    {
                        Session.SendWhisper("You do not own this plot of land to farm on!", 1);
                        Session.GetRoleplay().CooldownManager.CreateCooldown("farming", 500);
                        return;
                    }
                }
            }
            #endregion*/


            if (Item.FarmingData.BeingFarmed)
            {
                if (Item.ExtraData != "0")
                    Session.SendWhisper("¡Esta planta fue recientemente regada!", 1);
                else
                    Session.SendWhisper("¡Esta semilla fue plantada!", 1);
                return;
            }

            if (!Session.GetRoleplay().WateringCan && Item.ExtraData != "8")
            {
                Session.SendWhisper("Usted no tiene un riego en su mano!", 1);
                Session.GetRoleplay().CooldownManager.CreateCooldown("farming", 500);
                return;
            }

            if (RoleplayManager.FarmingCAPTCHABox)
            {
                if (Session.GetRoleplay().CaptchaSent)
                {
                    Session.SendWhisper("¡Debe ingresar el código en el código AFK para seguir cultivando!", 1);
                    Session.GetRoleplay().CooldownManager.CreateCooldown("farming", 500);
                    return;
                }
            }

            CryptoRandom Random = new CryptoRandom();

            if (Gamemap.TilesTouching(Item.GetX, Item.GetY, roomUser.X, roomUser.Y))
            {
                if (Item.ExtraData == "4")
                {
                    if (!Session.GetRoleplay().FarmingStats.HasPlantSatchel)
                    {
                        Session.SendWhisper("¡Necesitas una mochila vegetal para almacenar esta planta!", 1);
                        Session.GetRoleplay().CooldownManager.CreateCooldown("farming", 500);
                        return;
                    }

                    if (Item.GetRoom() != null)
                    {
                        Item.GetRoom().GetRoomItemHandler().RemoveFurniture(null, Item.Id);
                        FarmingManager.AddEXP(Session, Random.Next(FarmingItem.MaxExp, (FarmingItem.MaxExp + 4)));
                        FarmingManager.IncreaseSatchelCount(Session, FarmingItem, 1, true);
                        // BattlePass Challenge Integration
                        PolarEnvironment.GetGame().GetBattlePassManager().ProgressChallenge(Session, "harvest_plant", 1);
                        Session.Shout("*Cosecha el " + Item.GetBaseItem().PublicName + " y lo coloca en su mochila*", 4);
                        PolarEnvironment.GetGame().GetAchievementManager().ProgressAchievement(Session, "ACH_Farming", 1);

                        Session.GetRoleplay().CooldownManager.CreateCooldown("farming", 500);
                        return;
                    }
                }
                else
                {
                    var cracks = 0;
                    int.TryParse(Item.ExtraData, out cracks);
                    cracks++;

                    Item.FarmingData.BeingFarmed = true;
                    Session.Shout("*Pura un poco de agua en el " + Item.GetBaseItem().PublicName + " y espera a que crezca*", 4);
                    FarmingManager.AddEXP(Session, Random.Next(FarmingItem.MinExp, (FarmingItem.MaxExp + 1)));
                    // BattlePass Challenge Integration
                    PolarEnvironment.GetGame().GetBattlePassManager().ProgressChallenge(Session, "water_plant", 1);
                    Session.GetRoleplay().CooldownManager.CreateCooldown("farming", 500);

                    #region Timer
                    new Thread(() =>
                    {
                        int count = 0;
                        while (Session != null && Item != null && Item.FarmingData != null)
                        {
                            if (Session == null || Item == null || Item.FarmingData == null)
                                break;

                            count++;
                            Thread.Sleep(1000);

                            if (count >= 10)
                                break;
                        }

                        if (count >= 10 && Session != null && Item != null && Item.FarmingData != null)
                        {
                            Item.ExtraData = Convert.ToString(cracks);
                            Item.UpdateState(false, true);

                            if (Item.ExtraData != "4")
                                Session.SendWhisper("Uno de los " + Item.GetBaseItem().PublicName + " que regaron ha madurado un poco!", 1);
                            else
                                Session.SendWhisper("Uno de los " + Item.GetBaseItem().PublicName + " que regaron ha madurado!", 1);
                        }

                        if (Item != null && Item.FarmingData != null)
                            Item.FarmingData.BeingFarmed = false;
                    }).Start();
                    #endregion
                }

                Session.GetRoleplay().CooldownManager.CreateCooldown("farming", 500);
            }
        }

        public void OnWiredTrigger(Item Item)
        {

        }
    }
}