using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using Polar.HabboRoleplay.Timers;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.Items.Wired;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Quests;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using Polar.HabboHotel.Guides;

using Polar.Communication.Packets.Outgoing.Guides;
using System.Drawing;
using Polar.HabboRoleplay.Farming;
using Polar.HabboRoleplay.Gambling;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using Polar.HabboHotel.Users.Effects;
using Polar.HabboRoleplay.Turfs;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;

namespace Polar.Communication.Packets.Incoming.Rooms.Engine
{
    internal class GetRoomEntryDataEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            if (Session == null || Session.GetHabbo() == null)
                return;

            Room Room = Session.GetHabbo().CurrentRoom;
            if (Room == null)
                return;

            Session.GetHabbo().HomeRoom = Room.Id;

            Session.GetRoleplay().InState = false;

            if (Session.GetRoleplay().PoliceTrial)
                Session.GetRoleplay().PoliceTrial = false;

            #region Police Car Enable Check
            if (Session.GetRoomUser() != null)
            {
                if (Session.GetRoomUser().CurrentEffect == EffectsList.CarPolice)
                    Session.GetRoomUser().ApplyEffect(EffectsList.None);
            }
            #endregion

            #region Spawn/Update Texas Hold 'Em Furni
            if (TexasHoldEmManager.GetGamesByRoomId(Room.RoomId).Count > 0)
            {
                List<TexasHoldEm> Games = TexasHoldEmManager.GetGamesByRoomId(Room.RoomId);

                foreach (TexasHoldEm Game in Games)
                {
                    if (Game != null)
                    {
                        #region PotSquare Check
                        if (Game.PotSquare.Furni != null)
                        {
                            if (Game.PotSquare.Furni.GetX != Game.PotSquare.X && Game.PotSquare.Furni.GetY != Game.PotSquare.Y && Game.PotSquare.Furni.GetZ != Game.PotSquare.Z && Game.PotSquare.Furni.Rotation != Game.PotSquare.Rotation)
                            {
                                if (Room.GetRoomItemHandler().GetFloor.Contains(Game.PotSquare.Furni))
                                    Room.GetRoomItemHandler().RemoveFurniture(null, Game.PotSquare.Furni.Id);
                                Game.PotSquare.SpawnDice();
                            }
                        }
                        else
                            Game.PotSquare.SpawnDice();
                        #endregion

                        #region JoinGate Check
                        if (Game.JoinGate.Furni != null)
                        {
                            if (Game.JoinGate.Furni.GetX != Game.JoinGate.X && Game.JoinGate.Furni.GetY != Game.JoinGate.Y && Game.JoinGate.Furni.GetZ != Game.JoinGate.Z && Game.JoinGate.Furni.Rotation != Game.JoinGate.Rotation)
                            {
                                if (Room.GetRoomItemHandler().GetFloor.Contains(Game.JoinGate.Furni))
                                    Room.GetRoomItemHandler().RemoveFurniture(null, Game.JoinGate.Furni.Id);
                                Game.JoinGate.SpawnDice();
                            }
                        }
                        else
                            Game.JoinGate.SpawnDice();
                        #endregion

                        #region Player1 Check
                        foreach (TexasHoldEmItem Item in Game.Player1.Values)
                        {
                            if (Item.Furni != null)
                            {
                                if (Item.Furni.GetX != Item.X && Item.Furni.GetY != Item.Y && Item.Furni.GetZ != Item.Z && Item.Furni.Rotation != Item.Rotation)
                                {
                                    if (Room.GetRoomItemHandler().GetFloor.Contains(Item.Furni))
                                        Room.GetRoomItemHandler().RemoveFurniture(null, Item.Furni.Id);
                                    Item.SpawnDice();
                                }
                            }
                            else
                                Item.SpawnDice();
                        }
                        #endregion

                        #region Player2 Check
                        foreach (TexasHoldEmItem Item in Game.Player2.Values)
                        {
                            if (Item.Furni != null)
                            {
                                if (Item.Furni.GetX != Item.X && Item.Furni.GetY != Item.Y && Item.Furni.GetZ != Item.Z && Item.Furni.Rotation != Item.Rotation)
                                {
                                    if (Room.GetRoomItemHandler().GetFloor.Contains(Item.Furni))
                                        Room.GetRoomItemHandler().RemoveFurniture(null, Item.Furni.Id);
                                    Item.SpawnDice();
                                }
                            }
                            else
                                Item.SpawnDice();
                        }
                        #endregion

                        #region Player3 Check
                        foreach (TexasHoldEmItem Item in Game.Player3.Values)
                        {
                            if (Item.Furni != null)
                            {
                                if (Item.Furni.GetX != Item.X && Item.Furni.GetY != Item.Y && Item.Furni.GetZ != Item.Z && Item.Furni.Rotation != Item.Rotation)
                                {
                                    if (Room.GetRoomItemHandler().GetFloor.Contains(Item.Furni))
                                        Room.GetRoomItemHandler().RemoveFurniture(null, Item.Furni.Id);
                                    Item.SpawnDice();
                                }
                            }
                            else
                                Item.SpawnDice();
                        }
                        #endregion

                        #region Banker Check
                        foreach (TexasHoldEmItem Item in Game.Banker.Values)
                        {
                            if (Item.Furni != null)
                            {
                                if (Item.Furni.GetX != Item.X && Item.Furni.GetY != Item.Y && Item.Furni.GetZ != Item.Z && Item.Furni.Rotation != Item.Rotation)
                                {
                                    if (Room.GetRoomItemHandler().GetFloor.Contains(Item.Furni))
                                        Room.GetRoomItemHandler().RemoveFurniture(null, Item.Furni.Id);
                                    Item.SpawnDice();
                                }
                            }
                            else
                                Item.SpawnDice();
                        }
                        #endregion
                    }
                }
            }
            #endregion

            #region Taxi Message
            if (Session.GetRoleplay().AntiArrowCheck)
                Session.GetRoleplay().AntiArrowCheck = false;

            if (Session.GetRoleplay().InsideTaxi)
            {
                int Bubble = (Session.GetHabbo().GetPermissions().HasRight("mod_tool") && Session.GetRoleplay().StaffOnDuty) ? 23 : 4;
                Session.GetRoleplay().InsideTaxi = false;

                Task.Run(async delegate
                {
                    await Task.Delay(500);
                    RoleplayManager.Shout(Session, "*¡Hemos llegado a su destino!*", Bubble);
                    Session.GetRoomUser().CanWalk = true;
                    /*Client.GetRoleplay().RoomJoinedInmunity = true;
                    Client.GetRoleplay().IsNoob = true;*/
                    if (Session.GetRoomUser() != null)
                        Session.GetRoomUser().ApplyEffect(0);
                });
            }
            else
                PolarEnvironment.GetGame().GetQuestManager().ProgressUserQuest(Session, QuestType.SOCIAL_VISIT);
            #endregion

            #region Bus Message
            if (Session.GetRoleplay().AntiArrowCheck)
                Session.GetRoleplay().AntiArrowCheck = false;

            if (Session.GetRoleplay().InsideBus)
            {
                int Bubble = (Session.GetHabbo().GetPermissions().HasRight("mod_tool") && Session.GetRoleplay().StaffOnDuty) ? 23 : 4;
                Session.GetRoleplay().InsideBus = false;

                Task.Run(async delegate
                {
                    await Task.Delay(500);
                    RoleplayManager.Shout(Session, "*Tenga señor, su pago ¡Muchas gracias!*", Bubble);
                    /*Client.GetRoleplay().RoomJoinedInmunity = true;
                    Client.GetRoleplay().IsNoob = true;*/
                });
            }
            else
                PolarEnvironment.GetGame().GetQuestManager().ProgressUserQuest(Session, QuestType.SOCIAL_VISIT);
            #endregion

            #region Tutorial Step Check
            if (Session.GetRoleplay().TutorialStep == 13 && Room.WardrobeEnabled && Room.Type.Equals("public"))
            {
                PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(Session, "compose_tutorial|13");
            }
            else if (Session.GetRoleplay().TutorialStep == 18 && Room.PhoneStoreEnabled && Room.Type.Equals("public"))
            {
                PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(Session, "compose_tutorial|18");
            }
            else if (Session.GetRoleplay().TutorialStep == 23 && Room.BuyCarEnabled && Room.Type.Equals("public"))
            {
                PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(Session, "compose_tutorial|24");
            }
            else if (Session.GetRoleplay().TutorialStep == 27 && Room.MallEnabled && Room.Type.Equals("public"))
            {
                PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(Session, "compose_tutorial|28");
            }
            #endregion

            #region StunCheck
            if (Session.GetRoleplay().Paralized == true)
            {

            }
            #endregion

            #region StunCheck
            if (Session.GetRoleplay().IsStun == true)
            {

                if (Session.GetRoleplay().TryGetCooldown("stun"))
                {
                    Session.GetRoleplay().IsStun = true;
                    Session.GetRoleplay().IsJailed = true;


                    string MyCity = Room.City;

                    HabboRoleplay.RPRoom.RPRoom Data;
                    int ToRoomId = PolarEnvironment.GetGame().GetRPRoomManager().TryToGetJail(MyCity, out Data);


                    if (Session.GetHabbo().HomeRoom != ToRoomId)
                        Session.GetHabbo().HomeRoom = ToRoomId;


                    RoleplayManager.SendUserOld2(Session, ToRoomId);

                    if (!Session.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("jail"))
                        Session.GetRoleplay().TimerManager.CreateTimer("jail", 1000, true);
                }
            }
            #endregion

            #region PSVMode
            if (Session.GetRoleplay().PassiveMode)
            {
                Session.SendMessage(new RoomBubbleNotificationComposer("psv-icon", "Modo Pasivo: Activado", ""));
                PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(Session, "compose_psv_mode|active");
                RoleplayManager.Shout(Session, "((Ha entrado en modo pasivo))", 7);
                //
                new Thread(() =>
                {
                    Thread.Sleep(250);
                    if (Session.GetRoomUser() != null)
                        Session.GetRoomUser().ApplyEffect(EffectsList.Passive);
                }).Start();
            }
            #endregion

            #region DeathCheck
            if (Session.GetRoleplay().IsDead)
            {

                string MyCity = Room.City;

                HabboRoleplay.RPRoom.RPRoom Data;
                int HospitalRID = PolarEnvironment.GetGame().GetRPRoomManager().TryToGetHospital(MyCity, out Data);

                if (Room.Id != HospitalRID)
                {
                    RoleplayManager.SendUser(Session, HospitalRID);
                    //Client.SendNotification("¡No puedes dejar el hospital mientras estás muerto!");
                }
                RoleplayManager.GetLookAndMotto(Session);
                RoleplayManager.SpawnBeds(Session, "hosptl_bed");
            }
            #endregion

            #region JailCheck
            if (Session.GetRoleplay().IsJailed)
            {

                if (Session.GetRoleplay().Jailbroken)
                {
                    RoleplayManager.GetLookAndMotto(Session);
                    return;
                }

                string MyCity = Room.City;

                HabboRoleplay.RPRoom.RPRoom Data;
                int ToRoomId = PolarEnvironment.GetGame().GetRPRoomManager().TryToGetJail(MyCity, out Data);
                int CourtRID = PolarEnvironment.GetGame().GetRPRoomManager().TryToGetCourt(MyCity, out Data);

                if (RoleplayManager.Defendant == Session && Room.Id == CourtRID)
                {
                    RoleplayManager.GetLookAndMotto(Session);


                    new Thread(() =>
                    {
                        Thread.Sleep(500);
                        RoleplayManager.SpawnChairs(Session, "uni_lectern", null, Room);
                        if (Session.GetRoomUser() != null)
                            Session.GetRoomUser().Frozen = true;
                    }).Start();
                    return;
                }

                if (Room.Id != ToRoomId)
                {
                    RoleplayManager.SendUserOld2(Session, ToRoomId);
                    Session.SendNotification("¡No puedes salir de la cárcel hasta que tu condena haya expirado!");
                }

                if (Room.Id == ToRoomId)
                {
                    RoleplayManager.GetLookAndMotto(Session);
                    RoleplayManager.SpawnBeds(Session, "bed_silo_one");
                }
            }
            #endregion

            #region JobCheck
            if (Session.GetHabbo().CurrentRoom == null)
                Session.GetRoleplay().IsWorking = false;

            if (Session.GetRoleplay().JobId > 1 && Session.GetRoleplay().IsWorking)
            {

                int JobId = Session.GetRoleplay().JobId;
                int JobRank = Session.GetRoleplay().JobRank;

                if (!GroupManager.GetJobRank(JobId, JobRank).CanWorkHere(Room.Id))
                {
                    if (GroupManager.HasJobCommand(Session, "guide"))
                    {
                        GuideManager guideManager = PolarEnvironment.GetGame().GetGuideManager();
                        guideManager.RemoveGuide(Session);

                        #region End Existing Calls

                        if (Session.GetRoleplay().GuideOtherUser != null)
                        {
                            Session.GetRoleplay().GuideOtherUser.SendMessage(new OnGuideSessionDetachedComposer(0));
                            Session.GetRoleplay().GuideOtherUser.SendMessage(new OnGuideSessionDetachedComposer(1));
                            if (Session.GetRoleplay().GuideOtherUser.GetRoleplay() != null)
                            {
                                Session.GetRoleplay().GuideOtherUser.GetRoleplay().Sent911Call = false;
                                Session.GetRoleplay().GuideOtherUser.GetRoleplay().GuideOtherUser = null;
                            }

                            Session.GetRoleplay().GuideOtherUser = null;
                            Session.SendMessage(new OnGuideSessionDetachedComposer(0));
                            Session.SendMessage(new OnGuideSessionDetachedComposer(1));
                        }
                        #endregion
                        else
                            Session.SendMessage(new HelperToolConfigurationComposer(Session));
                    }
                    WorkManager.RemoveWorkerFromList(Session);
                    Session.GetRoleplay().IsWorking = false;
                    Session.GetHabbo().Poof();
                }
            }

            #endregion

            #region ProbationCheck
            if (!Session.GetRoleplay().OnProbation)
            {

                if (!Session.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("probation"))
                    Session.GetRoleplay().TimerManager.CreateTimer("probation", 1000, false);
            }
            #endregion

            #region SendHomeCheck
            if (Session.GetRoleplay().SendHomeTimeLeft <= 0)
            {
               // return;
            }
            else { 
                if (Session.GetRoleplay().SendHomeTimeLeft > 30)
                    Session.GetRoleplay().SendHomeTimeLeft = 30;

                if (!Session.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("sendhome"))
                    Session.GetRoleplay().TimerManager.CreateTimer("sendhome", 1000, false);
            }
            #endregion

            #region BotInteractionCheck
            List<RoomUser> Bots = Room.GetRoomUserManager().GetBotList().ToList();

            foreach (RoomUser Bot in Bots)
            {
                if (Bot == null)
                    continue;

                if (!Bot.IsBot)
                    continue;

                if (!Bot.IsRoleplayBot)
                    continue;

                if (!Bot.GetBotRoleplay().Deployed)
                    continue;

                Bot.GetBotRoleplayAI().OnUserEnterRoom(Session);
            }
            #endregion

            #region WebSocket Dialogue Check
            Session.GetRoleplay().ClearWebSocketDialogue();
            #endregion

            #region Police Car Enable Check
            if (Session.GetRoomUser() != null)
            {
                if (Session.GetRoomUser().CurrentEffect == EffectsList.CarPolice)
                    Session.GetRoomUser().ApplyEffect(EffectsList.None);
            }
            #endregion

            #region Spawn/Update Texas Hold 'Em Furni
            if (TexasHoldEmManager.GetGamesByRoomId(Room.RoomId).Count > 0)
            {
                List<TexasHoldEm> Games = TexasHoldEmManager.GetGamesByRoomId(Room.RoomId);

                foreach (TexasHoldEm Game in Games)
                {
                    if (Game != null)
                    {
                        #region PotSquare Check
                        if (Game.PotSquare.Furni != null)
                        {
                            if (Game.PotSquare.Furni.GetX != Game.PotSquare.X && Game.PotSquare.Furni.GetY != Game.PotSquare.Y && Game.PotSquare.Furni.GetZ != Game.PotSquare.Z && Game.PotSquare.Furni.Rotation != Game.PotSquare.Rotation)
                            {
                                if (Room.GetRoomItemHandler().GetFloor.Contains(Game.PotSquare.Furni))
                                    Room.GetRoomItemHandler().RemoveFurniture(null, Game.PotSquare.Furni.Id);
                                Game.PotSquare.SpawnDice();
                            }
                        }
                        else
                            Game.PotSquare.SpawnDice();
                        #endregion

                        #region JoinGate Check
                        if (Game.JoinGate.Furni != null)
                        {
                            if (Game.JoinGate.Furni.GetX != Game.JoinGate.X && Game.JoinGate.Furni.GetY != Game.JoinGate.Y && Game.JoinGate.Furni.GetZ != Game.JoinGate.Z && Game.JoinGate.Furni.Rotation != Game.JoinGate.Rotation)
                            {
                                if (Room.GetRoomItemHandler().GetFloor.Contains(Game.JoinGate.Furni))
                                    Room.GetRoomItemHandler().RemoveFurniture(null, Game.JoinGate.Furni.Id);
                                Game.JoinGate.SpawnDice();
                            }
                        }
                        else
                            Game.JoinGate.SpawnDice();
                        #endregion

                        #region Player1 Check
                        foreach (TexasHoldEmItem Item in Game.Player1.Values)
                        {
                            if (Item.Furni != null)
                            {
                                if (Item.Furni.GetX != Item.X && Item.Furni.GetY != Item.Y && Item.Furni.GetZ != Item.Z && Item.Furni.Rotation != Item.Rotation)
                                {
                                    if (Room.GetRoomItemHandler().GetFloor.Contains(Item.Furni))
                                        Room.GetRoomItemHandler().RemoveFurniture(null, Item.Furni.Id);
                                    Item.SpawnDice();
                                }
                            }
                            else
                                Item.SpawnDice();
                        }
                        #endregion

                        #region Player2 Check
                        foreach (TexasHoldEmItem Item in Game.Player2.Values)
                        {
                            if (Item.Furni != null)
                            {
                                if (Item.Furni.GetX != Item.X && Item.Furni.GetY != Item.Y && Item.Furni.GetZ != Item.Z && Item.Furni.Rotation != Item.Rotation)
                                {
                                    if (Room.GetRoomItemHandler().GetFloor.Contains(Item.Furni))
                                        Room.GetRoomItemHandler().RemoveFurniture(null, Item.Furni.Id);
                                    Item.SpawnDice();
                                }
                            }
                            else
                                Item.SpawnDice();
                        }
                        #endregion

                        #region Player3 Check
                        foreach (TexasHoldEmItem Item in Game.Player3.Values)
                        {
                            if (Item.Furni != null)
                            {
                                if (Item.Furni.GetX != Item.X && Item.Furni.GetY != Item.Y && Item.Furni.GetZ != Item.Z && Item.Furni.Rotation != Item.Rotation)
                                {
                                    if (Room.GetRoomItemHandler().GetFloor.Contains(Item.Furni))
                                        Room.GetRoomItemHandler().RemoveFurniture(null, Item.Furni.Id);
                                    Item.SpawnDice();
                                }
                            }
                            else
                                Item.SpawnDice();
                        }
                        #endregion

                        #region Banker Check
                        foreach (TexasHoldEmItem Item in Game.Banker.Values)
                        {
                            if (Item.Furni != null)
                            {
                                if (Item.Furni.GetX != Item.X && Item.Furni.GetY != Item.Y && Item.Furni.GetZ != Item.Z && Item.Furni.Rotation != Item.Rotation)
                                {
                                    if (Room.GetRoomItemHandler().GetFloor.Contains(Item.Furni))
                                        Room.GetRoomItemHandler().RemoveFurniture(null, Item.Furni.Id);
                                    Item.SpawnDice();
                                }
                            }
                            else
                                Item.SpawnDice();
                        }
                        #endregion
                    }
                }
            }
            #endregion


            if (Session.GetHabbo().InRoom)
            {
                Room OldRoom;

                if (!PolarEnvironment.GetGame().GetRoomManager().TryGetRoom(Session.GetHabbo().CurrentRoomId, out OldRoom))
                    return;

                if (OldRoom.GetRoomUserManager() != null)
                    OldRoom.GetRoomUserManager().RemoveUserFromRoom(Session, false, false);
            }

            if (!Room.GetRoomUserManager().AddAvatarToRoom(Session))
            {
                Room.GetRoomUserManager().RemoveUserFromRoom(Session, false, false);
                return;//TODO: Remove?
            }

            Room.SendObjects(Session);

            if (Room.HideWired && Room.CheckRights(Session, true, false))
                Session.SendMessage(new RoomNotificationComposer("furni_placement_error", "message", "Los Wired estan escondidos en esta habitación."));
            //Status updating for messenger, do later as buggy.

            try
            {
                if (Session.GetHabbo().GetMessenger() != null)
                    Session.GetHabbo().GetMessenger().OnStatusChanged(true);
            }
            catch { }

            if (Session.GetHabbo().GetStats().QuestID > 0)
                PolarEnvironment.GetGame().GetQuestManager().QuestReminder(Session, Session.GetHabbo().GetStats().QuestID);

            Session.SendMessage(new RoomEntryInfoComposer(Room.RoomId, Room.CheckRights(Session, true)));
            Session.SendMessage(new RoomVisualizationSettingsComposer(Room.WallThickness, Room.FloorThickness, Room.Hidewall));

            RoomUser ThisUser = Room.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);

            if (ThisUser != null && Session.GetHabbo().PetId == 0)
                Room.SendMessage(new UserChangeComposer(ThisUser, false));

            Session.SendMessage(new RoomEventComposer(Room.RoomData, Room.RoomData.Promotion));


            if (Room.GetWired() != null)
                Room.GetWired().TriggerEvent(WiredBoxType.TriggerRoomEnter, Session.GetHabbo());

            if (PolarEnvironment.GetUnixTimestamp() < Session.GetHabbo().FloodTime && Session.GetHabbo().FloodTime != 0)
                Session.SendMessage(new FloodControlComposer((int)Session.GetHabbo().FloodTime - (int)PolarEnvironment.GetUnixTimestamp()));

            if (Session.GetRoleplay().ATMRobbery == true)
            {
                Session.SendWhisper("Has salido de sala, por tal motivo el robo fue cancelado.");
                Session.GetRoleplay().BreakGeneralTimer = true;
                Session.GetRoleplay().ATMRobbery = false;
            }

            if (Session.GetRoleplay().RobartiendaRobbery == true)
            {
                Session.SendWhisper("Has salido de sala, por tal motivo el robo de la tienda fue cancelado.");
                Session.GetRoleplay().BreakGeneralTimer = true;
                Session.GetRoleplay().RobartiendaRobbery = false;
            }

            if (Session.GetRoleplay().Learning == true)
            {
                Session.SendWhisper("Has salido de sala, por tal motivo la lectura ha sido cancelada.");
                Session.GetRoleplay().BreakGeneralTimer = true;
                Session.GetRoleplay().Learning = false;
            }

            if (Session.GetRoleplay().ProcessCocaine == true)
            {
                Session.SendWhisper("Has salido de sala, por tal motivo la fabricación de la cocaina se cancelo.");
                Session.GetRoleplay().HRidItem.ExtraData = "0";
                Session.GetRoleplay().HRidItem.UpdateState(false, true);
                Session.GetRoleplay().BreakGeneralTimer = true;
                Session.GetRoleplay().ProcessCocaine = false;
            }
            if (Session.GetRoleplay().ProcessHeroine == true)
            {
                Session.SendWhisper("Has salido de sala, por tal motivo la fabricación de la heroina se cancelo.");
                Session.GetRoleplay().HRidItem.ExtraData = "0";
                Session.GetRoleplay().HRidItem.UpdateState(false, true);
                Session.GetRoleplay().BreakGeneralTimer = true;
                Session.GetRoleplay().ProcessHeroine = false;
            }
            if (Session.GetRoleplay().ProcessWeed == true)
            {
                Session.SendWhisper("Has salido de sala, por tal motivo la fabricación de la marihuana se cancelo.");
                Session.GetRoleplay().HRidItem.ExtraData = "0";
                Session.GetRoleplay().HRidItem.UpdateState(false, true);
                Session.GetRoleplay().BreakGeneralTimer = true;
                Session.GetRoleplay().ProcessWeed = false;
            }

            #region Products
            if (Session.GetRoleplay().ViewProducts)
            {
                // WS Products
                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_products", "close");
                Session.GetRoleplay().ViewProducts = false;
            }
            #endregion

            if (Session.GetHabbo().GetClubManager().HasSubscription("habbo_vip"))
            {
                //Session.GetHabbo().GetClubManager().TimeExpired("habbo_vip", Session.GetHabbo().GetClubManager().GetSubscription("habbo_vip").ExpireTime, Session);
                //Session.SendMessage(new UserNameChangeComposer(Session.GetRoomUser().GetRoom().Id, Session.GetRoomUser().VirtualId, "[VIP] " + Session.GetHabbo().Username));
            }
        }
    }
}