using System;
using System.Linq;
using System.Drawing;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.Quests;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Groups;
using Polar.Database.Interfaces;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Polar.HabboRoleplay.Bots;
using Polar.Utilities;

using Polar.Communication.Packets.Outgoing.Inventory.Purse;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboRoleplay.Turfs;
using System.Threading;
using Polar.HabboRoleplay.Bots.Manager.TimerHandlers;
using static Polar.HabboRoleplay.Bots.Manager.TimerHandlers.TimerHandlerManager;

namespace Polar.HabboRoleplay.Combat.Types
{
    public class Fist : ICombat
    {
        /// <summary>
        /// Executes this type of combat
        /// </summary>
        public void Execute(GameClient Client, GameClient TargetClient, bool HitClosest = false)
        {
            RoomUser User = Client.GetRoomUser();
            if (HitClosest)
            {
                if (!this.TryGetClosestTarget(Client, out User))
                    return;

                if (User == null)
                    return;

                if (User.IsBot && User.GetBotRoleplay() != null)
                {
                    this.ExecuteBot(Client, User.GetBotRoleplay());
                    return;
                }
                else
                    TargetClient = User.GetClient();
            }

            if (!this.CanCombat(Client, TargetClient))
                return;

            #region Working Check
            if (!User.GetRoom().TurfEnabled)
            {
                if (TargetClient.GetRoleplay().IsWorking && TargetClient.GetRoleplay().JobId != 3)
                {
                    Client.SendWhisper("¡No puedes hacer esto mientras no trabajas!");
                    return;
                }
            }
            #endregion

            int Damage = this.GetDamage(Client, TargetClient);
            if (!TargetClient.GetRoleplay().DrivingCar && !TargetClient.GetRoleplay().Pasajero)
            {
                // If the user is about to die and the user attacked themself
                if ((TargetClient.GetRoleplay().CurHealth - Damage) <= 0 && TargetClient == Client)
            {
                TargetClient.SendWhisper("¡No puedes matarte!", 1);
                return;
            }


            List<RoomUser> UsersToReturn = Client.GetHabbo().CurrentRoom.GetRoomUserManager().GetRoomUsers().ToList();

            foreach (RoomUser Usera in UsersToReturn)
            {
                if (Usera == null || Usera.GetClient() == null)
                    continue;

                Usera.GetClient().GetRoleplay().UpdateTimerDialogue("Punch-Sound", "add", 70, 100);
                //TargetClient.GetRoleplay().UpdateTimerDialogue("Bala-Sound", Weapon.Name, 70, 100);

            }

            // If about to die
            if (TargetClient.GetRoleplay().CurHealth - Damage <= 0)
            {
                
                    Client.GetRoleplay().ClearWebSocketDialogue();

                    int Amount = this.GetCoins(TargetClient);
                    this.GetRewards(Client, TargetClient, null);

                    Client.GetHabbo().Credits += Amount;
                    Client.GetHabbo().UpdateCreditsBalance();

                    TargetClient.GetHabbo().Credits -= Amount;
                    TargetClient.GetHabbo().UpdateCreditsBalance();

                    if (Amount > 0)
                    {
                        RoleplayManager.ShoutSay(Client, "*Golpea a " + TargetClient.GetHabbo().Username + ", causando " + Damage + " de daño*", 6, "black", true);
                        RoleplayManager.ShoutSay(Client, "*Golpea a " + TargetClient.GetHabbo().Username + ", golpeando y robando $" + String.Format("{0:N0}", Amount) + " de su cartera*", 6, "black", true);

                    }
                    else
                    {
                        RoleplayManager.ShoutSay(Client, "*Golpea a " + TargetClient.GetHabbo().Username + ", causando " + Damage + " daño*", 6, "black", true);
                        RoleplayManager.ShoutSay(Client, "*Golpea a " + TargetClient.GetHabbo().Username + ", noqueandolo*", 6, "black", true);
                    }

                    BountyManager.CheckBounty(Client, TargetClient.GetHabbo().Id);

                    if ((TargetClient.GetRoleplay().CurHealth - Damage) <= 0)
                        TargetClient.GetRoleplay().CurHealth = 0;
            }
            else
            {
                    RoleplayManager.ShoutSay(Client, "*Golpea a " + TargetClient.GetHabbo().Username + ", causando " + Damage + " de daño*", 6, "black", true);
                    Client.GetRoleplay().OpenUsersDialogue(TargetClient);
                TargetClient.GetRoleplay().OpenUsersDialogue(Client);
            }

            if (TargetClient != Client)
            {
                PolarEnvironment.GetGame().GetAchievementManager().ProgressAchievement(Client, "ACH_Punching", 1);
                PolarEnvironment.GetGame().GetQuestManager().ProgressUserQuest(Client, QuestType.PUNCH_USER, 1);
            }


            if (TargetClient.GetRoleplay().CurHealth - Damage <= 0)
                TargetClient.GetRoleplay().CurHealth = 0;
            else
                TargetClient.GetRoleplay().CurHealth -= Damage;

           
            }
            else
            {
                RoleplayManager.ShoutSay(Client, "*Golpea el auto que " + TargetClient.GetHabbo().Username + " concuce causandole " + Damage + " de daño*", 5, "black", true);
                // Stats WebSocket
                Client.GetRoleplay().OpenUsersDialogue(TargetClient);
                TargetClient.GetRoleplay().OpenUsersDialogue(Client);

                // Hace daño al auto que conduce
                TargetClient.GetRoleplay().CarLife -= Damage;
            }

            Client.GetRoleplay().Punches++;

            if (!Client.GetRoleplay().WantedFor.Contains("intento de asalto"))
                Client.GetRoleplay().WantedFor = Client.GetRoleplay().WantedFor + "intento de asalto, ";
            Client.GetRoleplay().CooldownManager.CreateCooldown("fist", 1000, RoleplayManager.DefaultHitCooldown);
        }

        /// <summary>
        /// Executes this type of combat on a Bot
        /// </summary>
        public void ExecuteBot(GameClient Client, RoleplayBot? Bot = null)
        {
            if (!this.CanCombat(Client, null, Bot))
                return;

            int Damage = this.GetDamage(Client, null, Bot);

            RoomUser BotUser = Client.GetHabbo().CurrentRoom.GetRoomUserManager().GetBotByName(Bot.Name);

            if (BotUser.GetRoom() == null)
            {
                Client.SendWhisper("Este usuario no se encontra en esta sala", 1);
                return;
            }

            if (BotUser == null)
            {
                Client.SendWhisper("Este usuario no se encontra en esta sala", 1);
                return;
            }

            if (BotUser.GetRoom() != Client.GetRoomUser().GetRoom())
            {
                Client.SendWhisper("Este usuario no se encontra en esta sala", 1);
                return;
            }

            if (BotUser.GetRoom().SafeZoneEnabled)
            {
                Client.SendWhisper("ES una zona segura, no puedes disparar a este bot.", 1);
                return;
            }
            // If about to die
            bool Died = false;

            if (Bot.CurHealth - Damage <= 0)
                Died = true;
            else
                RoleplayManager.ShoutSay(Client, "*Golpea a " + Bot.Name + ", causando " + Damage + " de Daño*", 6, "black", true);


           PolarEnvironment.GetGame().GetAchievementManager().ProgressAchievement(Client, "ACH_Punching", 1);
            PolarEnvironment.GetGame().GetQuestManager().ProgressUserQuest(Client, QuestType.PUNCH_USER, 1);

            if ((Client.GetRoleplay().CurEnergy - 2) <= 0)
                Client.GetRoleplay().CurEnergy = 0;
            else
                Client.GetRoleplay().CurEnergy -= 2;

            Client.GetRoleplay().Punches++;

            if (Bot.CurHealth - Damage <= 0)
                Bot.CurHealth = 0;
            else
                Bot.CurHealth -= Damage;

            if (!Died)
            {
                BotUser.Chat("*[" + Bot.CurHealth + "/" + Bot.MaxHealth + "]*", true, 3);
                BotUser.GetBotRoleplayAI().OnAttacked(Client);
            }
            else
                BotUser.GetBotRoleplayAI().OnDeath(Client);

            //Client.GetRoleplay().CooldownManager.CreateCooldown("fist", 1000, RoleplayManager.DefaultHitCooldown);
        }

        /// <summary>
        /// Checks if a client can complete this action
        /// </summary>
        public bool CanCombat(GameClient Client, GameClient TargetClient, RoleplayBot? Bot = null)
        {
            RoomUser RoomUser = Client.GetRoomUser();
            RoomUser TargetRoomUser = Bot == null ? TargetClient.GetHabbo().CurrentRoom.GetRoomUserManager().GetRoomUserByHabbo(TargetClient.GetHabbo().Username) : Client.GetHabbo().CurrentRoom.GetRoomUserManager().GetBotByName(Bot.Name);

            if (RoomUser == null || TargetRoomUser == null)
                return false;

            if (Bot != null)
            {
                if (!Bot.CanBeAttacked)
                {
                    Client.SendWhisper("Lo siento, pero este bot no puede ser dañado", 1);
                    return false;
                }
            }

            Point ClientPos = RoomUser.Coordinate;
            Point TargetClientPos = TargetRoomUser.Coordinate;

            #region Main Conditions
            double Distance = RoleplayManager.GetDistanceBetweenPoints2D(ClientPos, TargetClientPos);

            Room Room = null;

            if (Client.GetHabbo().CurrentRoomId > 0)
                Room = Client.GetHabbo().CurrentRoom;

            if (Room != null)
            {
                if (Room.SafeZoneEnabled)
                {
                    Client.SendWhisper("¡No puedes disparar en esta habitación!", 1);
                    return false;
                }

                if (!RoleplayManager.PurgeStarted)
                {
                    if (!Room.ShootEnabled)
                    {
                        Client.SendWhisper("¡No puedes disparar en esta habitación!", 1);
                        return false;
                    }
                }

                if (Room.IsHospital || (Room.Group != null && Room.Group.GType == 2 && Room.Group.Name.Contains("Hospital")))
                {
                    Client.SendWhisper("No se puede disparar dentro del hospital.", 1);
                    return false;
                }
            }

            if (TargetRoomUser == null)
            {
                Client.SendWhisper("¡Esta persona no está en la misma habitación que tú!", 1);
                return false;
            }

            if (RoleplayManager.LevelDifference)
            {
                if (!Room.TurfEnabled)
                {
                    int TargetLevel = TargetClient.GetRoleplay().Level;
                    int LevelDifference = Math.Abs(Client.GetRoleplay().Level - TargetLevel);

                    if (LevelDifference > 8)
                    {
                        Client.SendWhisper("((No puedes golpear a una persona con 8 niveles de diferencia tuya.))", 1);
                        return false;
                    }

                }
            }


                if (Client.GetRoleplay().IsNoob)
                {
                    if (!Client.GetRoleplay().NoobWarned)
                    {
                        Client.SendWhisper("Actualmente estás bajo la protección de Dios, si procedes a hacer esto de nuevo perderás tu protección. (Advertencias: 1/2)", 1);
                        Client.GetRoleplay().NoobWarned = true;
                        return false;
                    }
                    else if (!Client.GetRoleplay().NoobWarned2)
                    {
                        Client.SendWhisper("Actualmente estás bajo la protección de Dios, si procedes a hacer esto de nuevo perderás tu protección. (Advertencias: 2/2)", 1);
                        Client.GetRoleplay().NoobWarned2 = true;
                        return false;
                    }
                    else
                    {
                        Client.SendWhisper("Has perdido tu protección de Dios, estás por tu cuenta ahora.", 1);

                        if (Client.GetRoleplay().TimerManager != null && Client.GetRoleplay().TimerManager.ActiveTimers != null)
                        {
                            if (Client.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("noob"))
                                Client.GetRoleplay().TimerManager.ActiveTimers["noob"].EndTimer();
                        }

                        Client.GetRoleplay().IsNoob = false;
                        Client.GetRoleplay().NoobTimeLeft = 0;
                        return true;
                    }
                }

            if (Client.GetRoleplay().TryGetCooldown("fist"))
                return false;

            #endregion

            #region Status Conditions
            if (RoomUser.Frozen)
            {
                Client.SendWhisper("¡No puedes hacer esto mientras estás aturdido!", 1);
                return false;
            }

            if (RoomUser.IsAsleep)
            {
                Client.SendWhisper("¡No puedes hacer esto mientras estás AFK!", 1);
                return false;
            }

            if (Client.GetRoleplay().IsDead)
            {
                Client.SendWhisper("¡No puedes hacer esto mientras estás muerto!", 1);
                return false;
            }

            if (Client.GetRoleplay().IsJailed)
            {
                Client.SendWhisper("¡No puedes hacer esto mientras estás preso!", 1);
                return false;
            }

            if (Client.GetRoleplay().IsWorking)
            {
                Client.SendWhisper("¡No puedes hacer esto mientras estás trabajando!", 1);
                return false;
            }
            if (Client.GetRoleplay().PassiveMode)
            {
                Client.SendWhisper("No puedes agredir en modo pasivo.", 1);
                return false;
            }

            if (Client.GetRoleplay().StaffOnDuty || Client.GetRoleplay().AmbassadorOnDuty)
            {
                Client.SendWhisper("¡No puedes golpear a alguien mientras estás de servicio!", 1);
                return false;
            }

            if (Bot != null)
            {
                if (Bot.Dead)
                {
                    Client.SendWhisper("¡No puedes golpear a alguien que está muerto!", 1);
                    return false;
                }

                if (Bot.Jailed)
                {
                    Client.SendWhisper("¡No puedes golpear a alguien que está preso!", 1);
                    return false;
                }
            }
            if (Client.GetRoleplay().DrivingCar || Client.GetRoleplay().DrivingInCar)
            {
                Client.SendWhisper("No puedes agredir mientras vas dentro de un vehículo.", 1);
                return false;
            }
            if (TargetClient != null)
            {
                if(TargetClient.GetRoleplay().IsDead)
                {
                    Client.SendWhisper("¡No puedes golpear a alguien que está muerto!", 1);
                    return false;
                }

                if (TargetClient.GetRoleplay().IsJailed)
                {
                    Client.SendWhisper("¡No puedes golpear a alguien que está preso!", 1);
                    return false;
                }

                if (TargetClient.GetRoleplay().StaffOnDuty)
                {
                    Client.SendWhisper("¡No se puede golpear a un miembro del personal que está de servicio!", 1);
                    return false;
                }

                if (TargetClient.GetRoleplay().PassiveMode)
                {
                    Client.SendWhisper("((Esta persona se encuentra en modo pasivo.))", 1);
                    return false;
                }

                if (TargetClient.GetRoleplay().AmbassadorOnDuty)
                {
                    Client.SendWhisper("¡No puedes golpear a un embajador que está de servicio!", 1);
                    return false;
                }

                if (TargetClient == Client)
                {
                    Client.SendWhisper("¡No puedes golpearte!", 1);
                    return false;
                }

                /*if (TargetClient.MachineId == Client.MachineId)
                {
                    Client.SendWhisper("¡No puedes golpear otra de tus cuentas!", 1);
                    return false;
                }*/

                if (TargetClient.LoggingOut || TargetClient.GetRoleplay().IsDisconnecting)
                {
                    Client.SendWhisper("((Esta persona se encuentra desconectándose.))", 1);
                    return false;
                }


                if (TargetClient.GetRoleplay().IsNoob)
                {
                    Client.SendWhisper("*Este usuario se encuentra bajo inmunidad*", 1);
                    return false;
                }
            }

            if (Client.GetRoleplay().CurEnergy <= 0)
            {
                Client.SendWhisper("¡Te has quedado sin energía para golpear a alguien!", 1);
                return false;
            }

           
            if (Client.GetRoleplay().Cuffed)
            {
                Client.SendWhisper("¡No puedes golpear a un ciudadano mientras estás esposado!", 1);
                return false;
            }

            if (Client.GetRoleplay().DrivingCar)
            {
                Client.SendWhisper("¡Deje de conducir su vehículo para pelear con los puños!", 1);
                return false;
            }

            if (TargetRoomUser.IsAsleep)
            {
                Client.SendWhisper("¡No se puede golpear a alguien que no está jugando el juego ahora mismo!", 1);
                return false;
            }
            #endregion

            #region Distance

            if (Distance > 1)
            {
                RoleplayManager.Shout(Client, "*Golpea a " + (Bot == null ? TargetClient.GetHabbo().Username : Bot.Name) + ", pero falla*", 4);
                Client.GetRoleplay().CooldownManager.CreateCooldown("fist", 1000, RoleplayManager.DefaultHitCooldown);
                return false;
            }

            #endregion

            return true;
        }

        /// <summary>
        /// Selects the closest person to the client
        /// </summary>
        public bool TryGetClosestTarget(GameClient Client, out RoomUser Target)
        {
            Target = null;

            if (Client.GetRoomUser() == null)
                return false;

            if (Client.GetRoomUser().RoomId <= 0)
                return false;

            if (!RoleplayManager.GenerateRoom(Client.GetRoomUser().RoomId, out Room Room))
                return false;

            if (Room == null)
                return false;

            if (Room.GetRoomUserManager().GetRoomUsers().Count <= 1)
            {
                Client.SendWhisper("¡No hay nadie cerca de ti para apuntar automáticamente!", 1);
                return false;
            }

            var Point = new Point(Client.GetRoomUser().Coordinate.X, Client.GetRoomUser().Coordinate.Y);

            ConcurrentDictionary<RoomUser, double> PossibleUsers = new ConcurrentDictionary<RoomUser, double>();
            lock (Room.GetRoomUserManager().GetRoomUsers())
            {
                foreach (var User in Room.GetRoomUserManager().GetRoomUsers())
                {
                    if (User.IsBot)
                    {
                        if (User.GetBotRoleplay() == null)
                            continue;

                        if (!User.GetBotRoleplay().CanBeAttacked)
                            continue;
                    }

                    if (User == Client.GetRoomUser())
                        continue;

                    Point TargetPoint = new Point(User.Coordinate.X, User.Coordinate.Y);
                    double Distance = RoleplayManager.GetDistanceBetweenPoints2D(Point, TargetPoint);

                    if (!PossibleUsers.ContainsKey(User))
                        PossibleUsers.TryAdd(User, Distance);
                }
            }

            var OrderedUsers = PossibleUsers.OrderBy(x => x.Value);

            if (OrderedUsers.ToList().Count < 1)
                return false;

            Target = OrderedUsers.FirstOrDefault().Key;

            if (Target != null)
                return true;

            return false;
        }

        /// <summary>
        /// Gets the damage
        /// </summary>
        private int GetDamage(GameClient Client, GameClient TargetClient, RoleplayBot? Bot = null)
        {
            CryptoRandom Randomizer = new CryptoRandom();

            int Strength = Client.GetRoleplay().Strength;
            int MinDamage = (Strength - 6) <= 0 ? 5 : (Strength - 6);
            int MaxDamage = Strength + 6;

            // Lucky shot?
            if (Randomizer.Next(0, 100) < 12)
            {
                MinDamage = Strength + 12;
                MaxDamage = MinDamage + 3;
            }

            int Damage = Randomizer.Next(MinDamage, MaxDamage);

            if (Client.GetRoleplay().Class.ToLower() == "peleador")
                Damage += Randomizer.Next(2, 4);

            if (Client.GetRoleplay().GangId > 1000 && Bot == null)
            {
                if (GroupManager.HasGangCommand(Client, "peleador"))
                {
                    RoleplayManager.GenerateRoom(Client.GetHabbo().CurrentRoomId, out Room Room, false);
                    if (Room.TurfEnabled || GroupManager.HasJobCommand(TargetClient, "guide"))
                        Damage += Randomizer.Next(1, 3);
                }
            }

            if (Client.GetRoleplay().HighOffWeed)
                Damage += Randomizer.Next(1, 3);

            return Damage;
        }

        /// <summary>
        /// Gets the coins from the users dead body
        /// </summary>
        public int GetCoins(GameClient TargetClient, RoleplayBot? Bot = null)
        {
            if (TargetClient != null && TargetClient.GetHabbo() != null)
            {
                if (TargetClient.GetHabbo().VIPRank > 1)
                    return 0;

                if (TargetClient.GetHabbo().Credits < 3)
                    return 0;
            }

            if (Bot != null)
            {
                int MinMoney = Convert.ToInt32(RoleplayData.GetData("bots", "minmoney"));
                int MaxMoney = Convert.ToInt32(RoleplayData.GetData("bots", "maxmoney"));

                if (MaxMoney == 0)
                    return 0;

                CryptoRandom Random = new CryptoRandom();
                return Random.Next(MinMoney, (MaxMoney + 1));
            }

            return TargetClient.GetHabbo().Credits / 3;
        }

        /// <summary>
        /// calculates the amount of exp to give to the client
        /// </summary>
        public int GetEXP(GameClient Client, GameClient TargetClient, RoleplayBot? Bot = null)
        {
            int TargetLevel = TargetClient == null ? Bot.Level : TargetClient.GetRoleplay().Level;

            CryptoRandom Random = new CryptoRandom();
            int LevelDifference = Math.Abs(Client.GetRoleplay().Level - TargetLevel);
            int Amount;
            int Bonus;

            if (LevelDifference > 8)
            {
                Amount = 0;
                Bonus = 0;
            }
            else
            {
                if (TargetLevel > Client.GetRoleplay().Level)
                    Bonus = (10 * (LevelDifference + 1)) + LevelDifference * 2 + 5;
                else if (TargetLevel == Client.GetRoleplay().Level)
                    Bonus = (10 * 2) + 3 + 5;
                else if (TargetLevel < Client.GetRoleplay().Level)
                    Bonus = 10 + 5;
                else
                    Bonus = 2 * LevelDifference + 5;

                Amount = Random.Next(20, 20 + (LevelDifference + 9));
            }

            return (Amount + Bonus + 18);
        }

        /// <summary>
        /// Gets the rewards from the dead body
        /// </summary>
        /// <param name="Client"></param>
        /// <param name="TargetClient"></param>
        /// <param name="Bot"></param>
        public void GetRewards(GameClient Client, GameClient TargetClient, RoleplayBot? Bot = null)
        {
            if (Bot == null)
            {
                if (Client.GetRoleplay().LastKilled != TargetClient.GetHabbo().Id)
                {
                    PolarEnvironment.GetGame().GetQuestManager().ProgressUserQuest(Client, QuestType.KILL_USER);
                    PolarEnvironment.GetGame().GetAchievementManager().ProgressAchievement(Client, "ACH_Kills", 1);
                    PolarEnvironment.GetGame().GetAchievementManager().ProgressAchievement(TargetClient, "ACH_Death", 1);

                    Client.GetRoleplay().LastKilled = TargetClient.GetHabbo().Id;
                    Client.GetRoleplay().Kills++;
                    Client.GetRoleplay().HitKills++;

                    // BattlePass Challenge Integration
                    PolarEnvironment.GetGame().GetBattlePassManager().ProgressChallenge(Client, "kill_user", 1);

                    if (GroupManager.HasJobCommand(TargetClient, "guide") && TargetClient.GetRoleplay().IsWorking)
                        TargetClient.GetRoleplay().CopDeaths++;
                    else
                        TargetClient.GetRoleplay().Deaths++;

                    if (!Client.GetRoleplay().WantedFor.Contains("murder"))
                        Client.GetRoleplay().WantedFor = Client.GetRoleplay().WantedFor + "murder, ";

                    CryptoRandom Random = new CryptoRandom();
                    int Multiplier = 1;

                    int Chance = Random.Next(1, 101);

                    if (Chance <= 16)
                    {
                        if (Chance <= 8)
                            Multiplier = 3;
                        else
                            Multiplier = 2;
                    }

                    if (RoleplayManager.DoubleExp == true)
                        LevelManager.AddLevelEXP(Client, GetEXP(Client, TargetClient) * (Multiplier + 2));
                    else
                        LevelManager.AddLevelEXP(Client, GetEXP(Client, TargetClient) * Multiplier);

                    #region Check Gangs
                    List<Group> MyGang = PolarEnvironment.GetGame().GetGroupManager().GetGangsForUser(Client.GetHabbo().Id);
                    List<Group> EnemyGang = PolarEnvironment.GetGame().GetGroupManager().GetGangsForUser(TargetClient.GetHabbo().Id);

                    if (MyGang != null && MyGang.Count > 0)
                    {
                        if (MyGang[0].BankRuptcy)
                        {
                            Client.SendWhisper("Tu banda está en bancarota y no podrás gozar de los beneficios de ella.", 1);
                        }
                        else
                        {
                            if (EnemyGang != null && EnemyGang.Count > 0 && EnemyGang[0] != MyGang[0])
                            {
                                int NewTurfsCount = 0;
                                List<Turf> TF = PolarEnvironment.GetGame().GetGangTurfsManager().getTurfsbyGang(MyGang[0].Id);
                                if (TF != null && TF.Count > 0)
                                    NewTurfsCount = TF.Count;

                                int Bonif = NewTurfsCount * RoleplayManager.GangsTurfBonif;

                                if (Bonif > 0)
                                {
                                    if (!EnemyGang[0].BankRuptcy && EnemyGang[0].Balance > 0)
                                    {
                                        MyGang[0].Balance += (Bonif / 2);
                                        MyGang[0].SetBussines(MyGang[0].Balance, MyGang[0].Stock);
                                        EnemyGang[0].Balance -= Bonif;
                                        EnemyGang[0].SetBussines(EnemyGang[0].Balance, EnemyGang[0].Stock);

                                        MyGang[0].AddLog(Client.GetHabbo().Id, Client.GetHabbo().Username + " mata a " + TargetClient.GetHabbo().Username + ", integrante de la banda " + EnemyGang[0].Name + ", ganando $ " + String.Format("{0:N0}", (Bonif / 2)), (Bonif / 2));

                                        Client.GetHabbo().Credits += (Bonif / 2);
                                        Client.GetHabbo().UpdateCreditsBalance();
                                        Client.SendWhisper("¡Has ganado $ " + String.Format("{0:N0}", (Bonif / 2)) + " de bonificación por matar a un miembro de la banda " + EnemyGang[0].Name + "!", 1);

                                        TargetClient.SendWhisper("¡Tu banda pierde $ " + String.Format("{0:N0}", Bonif) + " por haber sido asesinad@ por un miembro de la banda " + MyGang[0].Name + "!", 1);
                                    }
                                    else
                                        Client.SendWhisper("¡Has matado a un miembro de la banda " + EnemyGang[0].Name + "! Pero no has ganado ninguna bonificación porque su banda está en bancarota.", 1);
                                }
                                else
                                {
                                    Client.SendWhisper("¡Has matado a un miembro de la banda " + EnemyGang[0].Name + "! Pero no has ganado ninguna bonificación porque tu banda no tienen ningún barrio controlado.", 1);
                                }

                                MyGang[0].GangKills++;
                                MyGang[0].UpdateStat(MyGang[0].Id, "gang_kills", MyGang[0].GangKills);
                            }

                            if (EnemyGang == null || EnemyGang.Count <= 0)
                            {
                                MyGang[0].GangKills++;
                                MyGang[0].UpdateStat(MyGang[0].Id, "gang_kills", MyGang[0].GangKills);
                            }

                            if (GroupManager.HasJobCommand(TargetClient, "law"))
                            {
                                MyGang[0].GangCopKills++;
                                MyGang[0].UpdateStat(MyGang[0].Id, "gang_cop_kills", MyGang[0].GangCopKills);
                            }
                        }
                    }

                    if (EnemyGang != null && EnemyGang.Count > 0)
                    {
                        EnemyGang[0].GangDeaths++;
                        EnemyGang[0].UpdateStat(EnemyGang[0].Id, "gang_deaths", EnemyGang[0].GangDeaths);
                    }
                    #endregion

                }
            }
            else
            {
                int Multiplier = 1;
                PolarEnvironment.GetGame().GetQuestManager().ProgressUserQuest(Client, QuestType.KILL_USER);
                PolarEnvironment.GetGame().GetAchievementManager().ProgressAchievement(Client, "ACH_Kills", 1);

                Client.GetRoleplay().Kills++;
                Client.GetRoleplay().HitKills++;

                CryptoRandom Random = new CryptoRandom();


                int Chance = Random.Next(1, 101);

                if (Chance <= 16)
                {
                    if (Chance <= 8)
                        Multiplier = 5;
                    else
                        Multiplier = 4;
                }

                LevelManager.AddLevelEXP(Client, CombatManager.GetCombatType("fist").GetEXP(Client, null, Bot) * Multiplier);
            }
        }
    }
}
