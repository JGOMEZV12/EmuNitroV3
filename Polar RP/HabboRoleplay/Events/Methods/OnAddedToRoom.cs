using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Turfs;
using Polar.HabboHotel.Quests;
using Polar.HabboHotel.Guides;
using Polar.Communication.Packets.Outgoing.Guides;
using Polar.HabboRoleplay.Gambling;
using Polar.HabboHotel.Users.Effects;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;


namespace Polar.HabboRoleplay.Events.Methods
{
    /// <summary>
    /// Triggered when the user is added to the room
    /// </summary>
    public class OnAddedToRoom : IEvent
    {
        #region Execute Event
        /// <summary>
        /// Responds to the event
        /// </summary>
        public void Execute(object Source, object[] Params)
        {
            GameClient Client = (GameClient)Source;
            if (Client == null || Client.GetRoleplay() == null || Client.GetHabbo() == null)
                return;

            Room Room = (Room)Params[0];

            //Tutorial(Client, Params);

            if (Client.GetRoleplay().PoliceTrial)
                Client.GetRoleplay().PoliceTrial = false;

           

            #region WebSocket Dialogue Check
            Client.GetRoleplay().ClearWebSocketDialogue();
            #endregion

            #region Police Car Enable Check
            if (Client.GetRoomUser() != null)
            {
                if (Client.GetRoomUser().CurrentEffect == EffectsList.CarPolice)
                    Client.GetRoomUser().ApplyEffect(EffectsList.None);
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

            #region Spawn Jailbreak Fence
            /*if (Room.RoomId == Convert.ToInt32(RoleplayData.GetData("jail", "outsideroomid")) && !JailbreakManager.JailbreakActivated)
            {
                int X = Convert.ToInt32(RoleplayData.GetData("jailbreak", "fencex"));
                int Y = Convert.ToInt32(RoleplayData.GetData("jailbreak", "fencey"));
                int Rot = Convert.ToInt32(RoleplayData.GetData("jailbreak", "fencerotation"));

                if (Room.GetRoomItemHandler().GetFloor.Where(x => x.BaseItem == 8049 && x.GetX == X && x.GetY == Y).ToList().Count <= 0)
                {
                    double MaxHeight = 0.0;
                    Item ItemInFront;
                    if (Room.GetGameMap().GetHighestItemForSquare(new Point(X, Y), out ItemInFront))
                    {
                        if (ItemInFront != null)
                            MaxHeight = ItemInFront.TotalHeight;
                    }

                    RoleplayManager.PlaceItemToRoom(null, 8049, 0, X, Y, MaxHeight, Rot, false, Room.RoomId, false);
                }
            }*/
            #endregion

            //Inmunity(Client, Params);

            #region Taxi Message
            if (Client.GetRoleplay().AntiArrowCheck)
                Client.GetRoleplay().AntiArrowCheck = false;

            if (Client.GetRoleplay().InsideTaxi)
            {
                int Bubble = (Client.GetHabbo().GetPermissions().HasRight("mod_tool") && Client.GetRoleplay().StaffOnDuty) ? 23 : 4;
                Client.GetRoleplay().InsideTaxi = false;

                Task.Run(async delegate
                {
                    await Task.Delay(500);
                    RoleplayManager.Shout(Client, "*¡Hemos llegado a su destino!*", Bubble);
                    /*Client.GetRoleplay().RoomJoinedInmunity = true;
                    Client.GetRoleplay().IsNoob = true;*/
                    if(Client.GetRoomUser() != null)
                    Client.GetRoomUser().ApplyEffect(0);
                });
            }
            else
                PolarEnvironment.GetGame().GetQuestManager().ProgressUserQuest(Client, QuestType.SOCIAL_VISIT);
            #endregion

            #region Bus Message
            if (Client.GetRoleplay().AntiArrowCheck)
                Client.GetRoleplay().AntiArrowCheck = false;

            if (Client.GetRoleplay().InsideBus)
            {
                int Bubble = (Client.GetHabbo().GetPermissions().HasRight("mod_tool") && Client.GetRoleplay().StaffOnDuty) ? 23 : 4;
                Client.GetRoleplay().InsideBus = false;

                Task.Run(async delegate
                 {
                     await Task.Delay(500);
                     RoleplayManager.Shout(Client, "*Tenga señor, su pago ¡Muchas gracias!*", Bubble);
                     /*Client.GetRoleplay().RoomJoinedInmunity = true;
                     Client.GetRoleplay().IsNoob = true;*/
                 });
            }
            else
                PolarEnvironment.GetGame().GetQuestManager().ProgressUserQuest(Client, QuestType.SOCIAL_VISIT);
            #endregion

            #region Room Entrance Message
            /*if (Room.EnterRoomMessage != "none")
            {
                new Thread(() =>
                {
                    Thread.Sleep(500);
                    Client.SendWhisper(Room.EnterRoomMessage, 34);
                }).Start();
            }*/
            #endregion

            #region Main checks

                BotInteractionCheck(Client, Params);


                #region disabled temporary
                HomeRoomCheck(Client, Params);
                #endregion

                JobCheck(Client, Params);
                SendhomeCheck(Client, Params);
                DeathCheck(Client, Params);
                JailCheck(Client, Params);
                WantedCheck(Client, Params);
                ProbationCheck(Client, Params);
                StunCheck(Client, Params);
                PSVModeCheck(Client, Params);

            #region AFK check

            if (Client.GetRoomUser() != null)
                    Client.GetHabbo().Poof(true);

                #endregion
            #endregion

                
        }
        #endregion

        #region InmunityCheck
        public void Inmunity(GameClient Session, object[] Params)
        {


            /*if (Room.SafeZoneEnabled)
            {*/
            if (Session.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("inmunity"))
                Session.GetRoleplay().TimerManager.ActiveTimers["inmunity"].EndTimer();


            Session.GetRoleplay().TimerManager.CreateTimer("inmunity", 1000, false);
            /*}
            else
            {
                if (Session.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("inmunity"))
                    Session.GetRoleplay().TimerManager.ActiveTimers["inmunity"].EndTimer();

                Session.GetRoleplay().IsNoob = false;
            }*/

        }
        #endregion

        #region Give Tutorial Badge
        public void Tutorial(GameClient Session, object[] Params)
        {
            Room Room = (Room)Params[0];

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
        }
        #endregion

        #region HomeRoomCheck
        /// <summary>
        /// Checks if the users homeroom is the correct one
        /// </summary>
        private void HomeRoomCheck(GameClient Client, object[] Params)
        {
            Room Room = (Room)Params[0];
            if (Room.Id == 0)
                return;

            if (Client.GetHabbo().HomeRoom != Room.Id)
                Client.GetHabbo().HomeRoom = Room.Id;
        }
        #endregion

        #region JobCheck
        /// <summary>
        /// Checks if the user is in the correct working room
        /// </summary>
        private void JobCheck(GameClient Client, object[] Params)
        {
            Room Room = (Room)Params[0];
            if (Client.GetHabbo().CurrentRoom == null)
                Client.GetRoleplay().IsWorking = false;

            if (Client.GetRoleplay().JobId > 1 && Client.GetRoleplay().IsWorking)
            {

                int JobId = Client.GetRoleplay().JobId;
                int JobRank = Client.GetRoleplay().JobRank;

                if (!GroupManager.GetJobRank(JobId, JobRank).CanWorkHere(Room.Id))
                {
                    if (GroupManager.HasJobCommand(Client, "guide"))
                    {
                        GuideManager guideManager = PolarEnvironment.GetGame().GetGuideManager();
                        guideManager.RemoveGuide(Client);

                        #region End Existing Calls

                        if (Client.GetRoleplay().GuideOtherUser != null)
                        {
                            Client.GetRoleplay().GuideOtherUser.SendMessage(new OnGuideSessionDetachedComposer(0));
                            Client.GetRoleplay().GuideOtherUser.SendMessage(new OnGuideSessionDetachedComposer(1));
                            if (Client.GetRoleplay().GuideOtherUser.GetRoleplay() != null)
                            {
                                Client.GetRoleplay().GuideOtherUser.GetRoleplay().Sent911Call = false;
                                Client.GetRoleplay().GuideOtherUser.GetRoleplay().GuideOtherUser = null;
                            }

                            Client.GetRoleplay().GuideOtherUser = null;
                            Client.SendMessage(new OnGuideSessionDetachedComposer(0));
                            Client.SendMessage(new OnGuideSessionDetachedComposer(1));
                        }
                        #endregion
                        else
                            Client.SendMessage(new HelperToolConfigurationComposer(Client));
                    }
                    WorkManager.RemoveWorkerFromList(Client);
                    Client.GetRoleplay().IsWorking = false;
                    Client.GetHabbo().Poof();
                }
            }
            else
                return;
        }
        #endregion

        #region Sendhome Check
        /// <summary>
        /// Checks if the user has been senthome
        /// </summary>
        /// <param name="Client"></param>
        public void SendhomeCheck(GameClient Client, object[] Params)
        {
            if (Client.GetRoleplay().SendHomeTimeLeft <= 0)
                return;

            if (Client.GetRoleplay().SendHomeTimeLeft > 30)
                Client.GetRoleplay().SendHomeTimeLeft = 30;

            if (!Client.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("sendhome"))
                Client.GetRoleplay().TimerManager.CreateTimer("sendhome", 1000, false);
            else
                return;
        }
        #endregion

        #region DeathCheck
        /// <summary>
        /// Checks to see if the client is dead, if true send back to hospital if not already in one
        /// </summary>
        private void DeathCheck(GameClient Client, object[] Params)
        {
            Room Room = (Room)Params[0];
            if (Client.GetRoleplay().IsDead)
            {

                string MyCity = Room.City;

                HabboRoleplay.RPRoom.RPRoom Data;
                int HospitalRID = PolarEnvironment.GetGame().GetRPRoomManager().TryToGetHospital(MyCity, out Data);

                if (Room.Id != HospitalRID)
                {
                    RoleplayManager.SendUser(Client, HospitalRID);
                    //Client.SendNotification("¡No puedes dejar el hospital mientras estás muerto!");
                }
                RoleplayManager.GetLookAndMotto(Client);
                RoleplayManager.SpawnBeds(Client, "hosptl_bed");
            }
        }

        #region StuNCheck
        /// <summary>
        /// Checks if the client is jailed, if so send the user to jail
        /// </summary>
        public void StunCheck(GameClient Client, object[] Params)
        {
            Room Room = (Room)Params[0];
            if (Client.GetRoleplay().IsStun == false)
                return;

            if (Client.GetRoleplay().TryGetCooldown("stun"))
            {
                Client.GetRoleplay().IsStun = true;
                Client.GetRoleplay().IsJailed = true;


                string MyCity = Room.City;

                HabboRoleplay.RPRoom.RPRoom Data;
                int ToRoomId = PolarEnvironment.GetGame().GetRPRoomManager().TryToGetJail(MyCity, out Data);


                if (Client.GetHabbo().HomeRoom != ToRoomId)
                    Client.GetHabbo().HomeRoom = ToRoomId;


                RoleplayManager.SendUserOld2(Client, ToRoomId);

                if (!Client.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("jail"))
                    Client.GetRoleplay().TimerManager.CreateTimer("jail", 1000, true);
            }

        }
        #endregion

        #region PSVModeCheck
        /// <summary>
        /// Checks if the client is jailed, if so send the user to jail
        /// </summary>
        public void PSVModeCheck(GameClient Client, object[] Params)
        {
            if (Client.GetRoleplay().PassiveMode)
            {
                Client.SendMessage(new RoomBubbleNotificationComposer("psv-icon", "Modo Pasivo: Activado", ""));
                PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(Client, "compose_psv_mode|active");
                RoleplayManager.Shout(Client, "((Ha entrado en modo pasivo))", 7);
                //
                new Thread(() =>
                {
                    Thread.Sleep(250);
                    if (Client.GetRoomUser() != null)
                        Client.GetRoomUser().ApplyEffect(EffectsList.Passive);
                }).Start();
            }

        }
        #endregion

        #endregion

        #region JailCheck
        /// <summary>
        /// Checks to see if the client is jailed, if true send back to jail if not already in one
        /// </summary>
        private void JailCheck(GameClient Client, object[] Params)
        {
            Room Room = (Room)Params[0];
            if (Client.GetRoleplay().IsJailed)
            {

                if (Client.GetRoleplay().Jailbroken)
                {
                    RoleplayManager.GetLookAndMotto(Client);
                    return;
                }

                string MyCity = Room.City;

                HabboRoleplay.RPRoom.RPRoom Data;
                int ToRoomId = PolarEnvironment.GetGame().GetRPRoomManager().TryToGetJail(MyCity, out Data);
                int CourtRID = PolarEnvironment.GetGame().GetRPRoomManager().TryToGetCourt(MyCity, out Data);

                if (RoleplayManager.Defendant == Client && Room.Id == CourtRID)
                {
                    RoleplayManager.GetLookAndMotto(Client);


                    new Thread(() =>
                    {
                        Thread.Sleep(500);
                        RoleplayManager.SpawnChairs(Client, "uni_lectern", null, Room);
                        if (Client.GetRoomUser() != null)
                            Client.GetRoomUser().Frozen = true;
                    }).Start();
                    return;
                }

                if (Room.Id != ToRoomId)
                {
                    RoleplayManager.SendUserOld2(Client, ToRoomId);
                    Client.SendNotification("¡No puedes salir de la cárcel hasta que tu condena haya expirado!");
                }

                if (Room.Id == ToRoomId)
                {
                    RoleplayManager.GetLookAndMotto(Client);
                    RoleplayManager.SpawnBeds(Client, "bed_silo_one");
                }

                /* int JailRID = Convert.ToInt32(RoleplayData.GetData("jail", "insideroomid"));
                 int JailRID2 = Convert.ToInt32(RoleplayData.GetData("jail", "outsideroomid"));
                 int CourtRID = Convert.ToInt32(RoleplayData.GetData("court", "roomid"));
                

                 if (RoleplayManager.Defendant == Client && Room.Id == CourtRID)
                 {
                     RoleplayManager.GetLookAndMotto(Client);
                     RoleplayManager.SpawnChairs(Client, "uni_lectern");

                     new Thread(() =>
                     {
                         Thread.Sleep(500);
                         if (Client.GetRoomUser() != null)
                             Client.GetRoomUser().Frozen = true;
                     }).Start();
                     return;
                 }

                 if (Room.Id != JailRID && Room.Id != JailRID2)
                 {
                     RoleplayManager.SendUser(Client, JailRID);
                     Client.SendNotification("¡No puedes salir de la cárcel hasta que tu condena haya expirado!");
                 }

                 if (Room.Id == JailRID)
                 {
                     RoleplayManager.GetLookAndMotto(Client);
                     RoleplayManager.SpawnBeds(Client, "bed_silo_one");
                 }*/
            }
        }
        #endregion

        #region SecuestroCheck
        /// <summary>
        /// Checks to see if the client is jailed, if true send back to jail if not already in one
        /// </summary>
        private void SecuestroCheck(GameClient Client, object[] Params)
        {
            Room Room = (Room)Params[0];
            if (Client.GetRoleplay().IsJailed)
            {
                if (Client.GetRoleplay().Jailbroken)
                {
                    RoleplayManager.GetLookAndMotto(Client);
                    return;
                }

                int SecuestroRID = Convert.ToInt32(RoleplayData.GetData("secuestro", "insideroomid"));
                int JailRID2 = Convert.ToInt32(RoleplayData.GetData("secuestro", "outsideroomid"));


                if (Room.Id != SecuestroRID)
                {
                    RoleplayManager.SendUser(Client, SecuestroRID);
                    Client.SendNotification("¡No puedes salir de la cárcel hasta que tu condena haya expirado!");
                }

                if (Room.Id == SecuestroRID)
                {
                    RoleplayManager.GetLookAndMotto(Client);
                    RoleplayManager.SpawnBeds(Client, "grunge_mattress");
                }
            }
            else
                return;
        }
        #endregion

        #region Wanted Check
        /// <summary>
        /// Checks if the user is wanted
        /// </summary>
        /// <param name="Client"></param>
        public void WantedCheck(GameClient Client, object[] Params)
        {
            Room Room = (Room)Params[0];
            if (!Client.GetRoleplay().IsWanted)
                return;

            if (RoleplayManager.WantedList.ContainsKey(Client.GetHabbo().Id))
                return;


            string RoomId = Room.Id.ToString() != "0" ? Room.Id.ToString() : "Unknown";

            if (!RoleplayManager.WantedList.ContainsKey(Client.GetHabbo().Id))
            {
                Wanted Wanted = new Wanted(Convert.ToUInt32(Client.GetHabbo().Id), RoomId, Client.GetRoleplay().WantedLevel);
                RoleplayManager.WantedList.TryAdd(Client.GetHabbo().Id, Wanted);
            }

            if (!Client.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("wanted"))
                Client.GetRoleplay().TimerManager.CreateTimer("wanted", 1000, false);
        }
        #endregion

        #region Probation Check
        /// <summary>
        /// Checks if the user is on probation
        /// </summary>
        /// <param name="Client"></param>
        public void ProbationCheck(GameClient Client, object[] Params)
        {
            Room Room = (Room)Params[0];
            if (!Client.GetRoleplay().OnProbation)
                return;

            if (!Client.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("probation"))
                Client.GetRoleplay().TimerManager.CreateTimer("probation", 1000, false);
            else
                return;
        }
        #endregion

        #region Bot Interaction Check
        /// <summary>
        /// Checks for any possible interactions with bots in room
        /// </summary>
        /// <param name="Client"></param>
        /// <param name="Params"></param>
        public void BotInteractionCheck(GameClient Client, object[] Params)
        {
            Room Room = (Room)Params[0];
            if (Room == null) return;

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

                Bot.GetBotRoleplayAI().OnUserEnterRoom(Client);
            }
        }
        #endregion

    }
}