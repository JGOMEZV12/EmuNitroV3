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
using Polar.Communication.Packets.Outgoing.Rooms.Avatar;
using Polar.Communication.Packets.Outgoing.Rooms.Session;

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

            #region 1. Basic State and Room Switching
            Session.GetHabbo().HomeRoom = Room.Id;
            Session.GetRoleplay().InState = false;

            if (Session.GetHabbo().InRoom)
            {
                if (PolarEnvironment.GetGame().GetRoomManager().TryGetRoom(Session.GetHabbo().CurrentRoomId, out Room OldRoom))
                {
                    if (OldRoom.Id != Room.Id && OldRoom.GetRoomUserManager() != null)
                        OldRoom.GetRoomUserManager().RemoveUserFromRoom(Session, false, false);
                }
            }
            #endregion

            #region 2. Room Entry Handshake (Camera focus order)
            if (!Room.GetRoomUserManager().AddAvatarToRoom(Session))
            {
                Room.GetRoomUserManager().RemoveUserFromRoom(Session, false, false);
                return;
            }

            // Sends maps, furniture, users and metadata in the correct order for localization
            Room.SendObjects(Session);

            Session.SendMessage(new RoomEventComposer(Room.RoomData, Room.RoomData.Promotion));

            if (Room.HideWired && Room.CheckRights(Session, true, false))
                Session.SendMessage(new RoomNotificationComposer("furni_placement_error", "message", "Los Wired estan escondidos en esta habitación."));

            RoomUser ThisUser = Room.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);
            if (ThisUser != null && Session.GetHabbo().PetId == 0)
                Room.SendMessage(new UserChangeComposer(ThisUser, false));
            #endregion

            #region 3. RP Logic & Status Checks (Texas, Taxi, Bus, Jobs, Jail, etc.)

            // Texas Hold 'Em
            ProcessTexasHoldEm(Room);

            // Stun / Jail / Death Checks
            if (Session.GetRoleplay().IsStun) HandleStun(Session, Room);
            if (Session.GetRoleplay().IsDead) HandleDeath(Session, Room);
            if (Session.GetRoleplay().IsJailed) HandleJail(Session, Room);

            // Taxi / Bus
            HandleTaxiBus(Session);

            // Job & Probation
            HandleJobs(Session, Room);
            if (!Session.GetRoleplay().OnProbation)
            {
                if (!Session.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("probation"))
                    Session.GetRoleplay().TimerManager.CreateTimer("probation", 1000, false);
            }

            // SendHome
            if (Session.GetRoleplay().SendHomeTimeLeft > 0)
            {
                if (Session.GetRoleplay().SendHomeTimeLeft > 30)
                    Session.GetRoleplay().SendHomeTimeLeft = 30;

                if (!Session.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("sendhome"))
                    Session.GetRoleplay().TimerManager.CreateTimer("sendhome", 1000, false);
            }

            // Cancel active RP processes
            CancelActiveProcesses(Session);

            // PSV Mode
            if (Session.GetRoleplay().PassiveMode)
            {
                Session.SendMessage(new RoomBubbleNotificationComposer("psv-icon", "Modo Pasivo: Activado", ""));
                PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(Session, "compose_psv_mode|active");
                RoleplayManager.Shout(Session, "((Ha entrado en modo pasivo))", 7);

                Task.Run(async delegate {
                    await Task.Delay(250);
                    Session.GetRoomUser()?.ApplyEffect(EffectsList.Passive);
                });
            }

            // Bot Interaction
            foreach (RoomUser Bot in Room.GetRoomUserManager().GetBotList().ToList())
            {
                if (Bot?.BotAI != null && Bot.IsRoleplayBot && Bot.GetBotRoleplay().Deployed)
                    Bot.GetBotRoleplayAI().OnUserEnterRoom(Session);
            }

            Session.GetRoleplay().ClearWebSocketDialogue();
            #endregion

            #region 4. Triggers & Finalization
            if (Room.GetWired() != null)
                Room.GetWired().TriggerEvent(WiredBoxType.TriggerRoomEnter, Session.GetHabbo());

            if (Session.GetHabbo().GetStats().QuestID > 0)
                PolarEnvironment.GetGame().GetQuestManager().QuestReminder(Session, Session.GetHabbo().GetStats().QuestID);

            if (PolarEnvironment.GetUnixTimestamp() < Session.GetHabbo().FloodTime && Session.GetHabbo().FloodTime != 0)
                Session.SendMessage(new FloodControlComposer((int)Session.GetHabbo().FloodTime - (int)PolarEnvironment.GetUnixTimestamp()));

            try
            {
                Session.GetHabbo().GetMessenger()?.OnStatusChanged(true);
            }
            catch { }
            #endregion
        }

        private void ProcessTexasHoldEm(Room Room)
        {
            var games = TexasHoldEmManager.GetGamesByRoomId(Room.RoomId);
            if (games.Count == 0) return;

            foreach (TexasHoldEm Game in games)
            {
                if (Game == null) continue;

                CheckTexasItem(Room, Game.PotSquare);
                CheckTexasItem(Room, Game.JoinGate);
                foreach (var item in Game.Player1.Values) CheckTexasItem(Room, item);
                foreach (var item in Game.Player2.Values) CheckTexasItem(Room, item);
                foreach (var item in Game.Player3.Values) CheckTexasItem(Room, item);
                foreach (var item in Game.Banker.Values) CheckTexasItem(Room, item);
            }
        }

        private void CheckTexasItem(Room room, TexasHoldEmItem item)
        {
            if (item.Furni != null)
            {
                if (item.Furni.GetX != item.X || item.Furni.GetY != item.Y || item.Furni.GetZ != item.Z || item.Furni.Rotation != item.Rotation)
                {
                    if (room.GetRoomItemHandler().GetFloor.Contains(item.Furni))
                        room.GetRoomItemHandler().RemoveFurniture(null, item.Furni.Id);
                    item.SpawnDice();
                }
            }
            else item.SpawnDice();
        }

        private void HandleStun(GameClient Session, Room Room)
        {
            if (Session.GetRoleplay().TryGetCooldown("stun"))
            {
                Session.GetRoleplay().IsStun = true;
                Session.GetRoleplay().IsJailed = true;
                string MyCity = Room.City;
                int ToRoomId = PolarEnvironment.GetGame().GetRPRoomManager().TryToGetJail(MyCity, out _);

                if (Session.GetHabbo().HomeRoom != ToRoomId)
                    Session.GetHabbo().HomeRoom = ToRoomId;

                RoleplayManager.SendUserOld2(Session, ToRoomId);

                if (!Session.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("jail"))
                    Session.GetRoleplay().TimerManager.CreateTimer("jail", 1000, true);
            }
        }

        private void HandleDeath(GameClient Session, Room Room)
        {
            string MyCity = Room.City;
            int HospitalRID = PolarEnvironment.GetGame().GetRPRoomManager().TryToGetHospital(MyCity, out _);

            if (Room.Id != HospitalRID)
                RoleplayManager.SendUser(Session, HospitalRID);

            RoleplayManager.GetLookAndMotto(Session);
            RoleplayManager.SpawnBeds(Session, "hosptl_bed");
        }

        private void HandleJail(GameClient Session, Room Room)
        {
            if (Session.GetRoleplay().Jailbroken)
            {
                RoleplayManager.GetLookAndMotto(Session);
                return;
            }

            string MyCity = Room.City;
            int ToRoomId = PolarEnvironment.GetGame().GetRPRoomManager().TryToGetJail(MyCity, out _);
            int CourtRID = PolarEnvironment.GetGame().GetRPRoomManager().TryToGetCourt(MyCity, out _);

            if (RoleplayManager.Defendant == Session && Room.Id == CourtRID)
            {
                RoleplayManager.GetLookAndMotto(Session);
                Task.Run(async delegate {
                    await Task.Delay(500);
                    RoleplayManager.SpawnChairs(Session, "uni_lectern", null, Room);
                    if (Session.GetRoomUser() != null)
                        Session.GetRoomUser().Frozen = true;
                });
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

        private void HandleTaxiBus(GameClient Session)
        {
            if (Session.GetRoleplay().AntiArrowCheck)
                Session.GetRoleplay().AntiArrowCheck = false;

            if (Session.GetRoleplay().InsideTaxi)
            {
                int Bubble = (Session.GetHabbo().GetPermissions().HasRight("mod_tool") && Session.GetRoleplay().StaffOnDuty) ? 23 : 4;
                Session.GetRoleplay().InsideTaxi = false;
                Task.Run(async delegate {
                    await Task.Delay(500);
                    RoleplayManager.Shout(Session, "*¡Hemos llegado a su destino!*", Bubble);
                    if (Session.GetRoomUser() != null)
                    {
                        Session.GetRoomUser().CanWalk = true;
                        Session.GetRoomUser().ApplyEffect(0);
                    }
                });
            }
            else if (Session.GetRoleplay().InsideBus)
            {
                int Bubble = (Session.GetHabbo().GetPermissions().HasRight("mod_tool") && Session.GetRoleplay().StaffOnDuty) ? 23 : 4;
                Session.GetRoleplay().InsideBus = false;
                Task.Run(async delegate {
                    await Task.Delay(500);
                    RoleplayManager.Shout(Session, "*Tenga señor, su pago ¡Muchas gracias!*", Bubble);
                });
            }
            else
                PolarEnvironment.GetGame().GetQuestManager().ProgressUserQuest(Session, QuestType.SOCIAL_VISIT);
        }

        private void HandleJobs(GameClient Session, Room Room)
        {
            if (Session.GetRoleplay().JobId > 1 && Session.GetRoleplay().IsWorking)
            {
                if (!GroupManager.GetJobRank(Session.GetRoleplay().JobId, Session.GetRoleplay().JobRank).CanWorkHere(Room.Id))
                {
                    if (GroupManager.HasJobCommand(Session, "guide"))
                    {
                        PolarEnvironment.GetGame().GetGuideManager().RemoveGuide(Session);
                        if (Session.GetRoleplay().GuideOtherUser != null)
                        {
                            var other = Session.GetRoleplay().GuideOtherUser;
                            other.SendMessage(new OnGuideSessionDetachedComposer(0));
                            other.SendMessage(new OnGuideSessionDetachedComposer(1));
                            if (other.GetRoleplay() != null)
                            {
                                other.GetRoleplay().Sent911Call = false;
                                other.GetRoleplay().GuideOtherUser = null;
                            }
                            Session.GetRoleplay().GuideOtherUser = null;
                            Session.SendMessage(new OnGuideSessionDetachedComposer(0));
                            Session.SendMessage(new OnGuideSessionDetachedComposer(1));
                        }
                        else Session.SendMessage(new HelperToolConfigurationComposer(Session));
                    }
                    WorkManager.RemoveWorkerFromList(Session);
                    Session.GetRoleplay().IsWorking = false;
                    Session.GetHabbo().Poof();
                }
            }
        }

        private void CancelActiveProcesses(GameClient Session)
        {
            if (Session.GetRoleplay().ATMRobbery)
            {
                Session.SendWhisper("Has salido de sala, por tal motivo el robo fue cancelado.");
                Session.GetRoleplay().BreakGeneralTimer = true;
                Session.GetRoleplay().ATMRobbery = false;
            }
            if (Session.GetRoleplay().RobartiendaRobbery)
            {
                Session.SendWhisper("Has salido de sala, por tal motivo el robo de la tienda fue cancelado.");
                Session.GetRoleplay().BreakGeneralTimer = true;
                Session.GetRoleplay().RobartiendaRobbery = false;
            }
            if (Session.GetRoleplay().Learning)
            {
                Session.SendWhisper("Has salido de sala, por tal motivo la lectura ha sido cancelada.");
                Session.GetRoleplay().BreakGeneralTimer = true;
                Session.GetRoleplay().Learning = false;
            }
            if (Session.GetRoleplay().ProcessCocaine)
            {
                Session.SendWhisper("Has salido de sala, por tal motivo la fabricación de la cocaina se cancelo.");
                if (Session.GetRoleplay().HRidItem != null) { Session.GetRoleplay().HRidItem.ExtraData = "0"; Session.GetRoleplay().HRidItem.UpdateState(false, true); }
                Session.GetRoleplay().BreakGeneralTimer = true;
                Session.GetRoleplay().ProcessCocaine = false;
            }
            if (Session.GetRoleplay().ProcessHeroine)
            {
                Session.SendWhisper("Has salido de sala, por tal motivo la fabricación de la heroina se cancelo.");
                if (Session.GetRoleplay().HRidItem != null) { Session.GetRoleplay().HRidItem.ExtraData = "0"; Session.GetRoleplay().HRidItem.UpdateState(false, true); }
                Session.GetRoleplay().BreakGeneralTimer = true;
                Session.GetRoleplay().ProcessHeroine = false;
            }
            if (Session.GetRoleplay().ProcessWeed)
            {
                Session.SendWhisper("Has salido de sala, por tal motivo la fabricación de la marihuana se cancelo.");
                if (Session.GetRoleplay().HRidItem != null) { Session.GetRoleplay().HRidItem.ExtraData = "0"; Session.GetRoleplay().HRidItem.UpdateState(false, true); }
                Session.GetRoleplay().BreakGeneralTimer = true;
                Session.GetRoleplay().ProcessWeed = false;
            }
            if (Session.GetRoleplay().ViewProducts)
            {
                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_products", "close");
                Session.GetRoleplay().ViewProducts = false;
            }
        }
    }
}
