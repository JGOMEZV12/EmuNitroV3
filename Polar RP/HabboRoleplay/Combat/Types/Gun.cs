using Nancy.Session;
using Polar.Communication.Packets.Outgoing.Inventory.Weapons;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.Database.Interfaces;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.Quests;
using Polar.HabboHotel.Rooms;
using Polar.HabboRoleplay.Bots;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboRoleplay.Turfs;
using Polar.HabboRoleplay.Weapons;
using Polar.Utilities;
using System;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;

namespace Polar.HabboRoleplay.Combat.Types
{
    public class Gun : ICombat
    {
        /// <summary>
        /// Executes this type of combat
        /// </summary>
        ///
        private static Dictionary<int, DateTime> _lastShotTime = new Dictionary<int, DateTime>();
        private static readonly TimeSpan ShotCooldown = TimeSpan.FromMilliseconds(500); // 500ms entre disparos
        public void Execute(GameClient Client, GameClient TargetClient, bool HitClosest = false)
        {
            if (!CanCombat(Client, TargetClient))
                return;

            RoomUser RoomUser = Client.GetRoomUser();
            RoomUser TargetRoomUser = TargetClient.GetRoomUser();
            int Damage = GetDamage(Client, TargetClient);
            Weapon Weapon = Client.GetRoleplay().EquippedWeapon;
            Point ClientPos = RoomUser.Coordinate;
            Point TargetClientPos = TargetRoomUser.Coordinate;
            double Distance = RoleplayManager.GetDistanceBetweenPoints2D(ClientPos, TargetClientPos);

            CryptoRandom Randomx = new CryptoRandom();
            int Chancex = Randomx.Next(1, 101);

            // Chequeo común para armas y distancia
            if (Weapon.Name == "electrica")
            {
                if (Distance > RoleplayManager.StunGunRange)
                {
                    RoleplayManager.ShoutSay(Client, "*Dispara su pistola electrica hacia " + TargetClient.GetHabbo().Username + ", pero el disparo no lo alcanza*", 37, "black", true);
                    TargetClient.GetRoleplay().SpecialCooldowns.TryUpdate("stun", 1800, TargetClient.GetRoleplay().SpecialCooldowns["stun"]);
                    return;
                }

                if (Chancex <= 8)
                {
                    RoleplayManager.ShoutSay(Client, "*Dispara su pistola electrica hacia " + TargetClient.GetHabbo().Username + ", pero falla*", 37, "black", true);
                    Client.GetRoleplay().CooldownManager.CreateCooldown("stun", 1000, 3);
                }
                else
                {
                    if (!GroupManager.HasJobCommand(Client, "stun"))
                    {
                        Client.SendWhisper("¡No eres un policia para utilizar esta arma!", 1);
                        return;
                    }

                    RoleplayManager.ShoutSay(Client, "*Dispara su pistola electrica hacia " + TargetClient.GetHabbo().Username + " inmovilizándolo inmediatamente*", 37, "black", true);
                    TargetClient.GetRoleplay().TimerManager.CreateTimer("stun", 1000, false);

                    FreezeTarget(TargetClient);
                    Client.GetRoleplay().CooldownManager.CreateCooldown("stun", 1000, 3);
                }
                return;
            }

            // Ammunition Check
            if (Client.GetRoleplay().GunShots >= Weapon.ClipSize)
            {
                Weapon.Reload(Client, TargetClient);
                Client.GetRoleplay().CooldownManager.CreateCooldown("reload", 1000, Weapon.ReloadTime);
                return;
            }

            

            // Verificación de si se está trabajando
            if (!RoomUser.GetRoom().TurfEnabled && !IsPlayerWorking(Client, TargetClient))
                return;

            // Check de inmunidad
            if (HasImmunity(Client, TargetClient))
                return;

            // Verificación de rango de disparo
            #region Distance Check

            if (TargetClient.GetRoleplay().CarEnableId == 817)
            {
                RoleplayManager.ShoutSay(Client, "*Intentó de disparar a " + TargetClient.GetHabbo().Username + ", pero falla*", 4, "black", true);
                Client.GetRoleplay().GunShots++;

                Client.GetRoleplay().Bullets--;
                return;
            }
            else if (TargetClient.GetRoleplay().CarEnableId != 817 && Weapon.Name == "rpg")
            {
                Client.SendWhisper("¡No puedes disparar al usuario con está arma, solo aviones!", 4);
                return;
            }

            if (TargetClient.GetRoleplay().DrivingCarId == 817 && Weapon.Name == "rpg")
            {
                //Retornamos a valores predeterminados
                TargetClient.GetRoleplay().DrivingCar = false;
                TargetClient.GetRoleplay().DrivingInCar = false;

                //Combustible System
                TargetClient.GetRoleplay().CarType = 0;// Define el gasto de combustible
                TargetClient.GetRoleplay().CarFuel = 0;
                TargetClient.GetRoleplay().CarMaxFuel = 0;
                TargetClient.GetRoleplay().CarTimer = 0;
                TargetClient.GetRoleplay().CarLife = 0;

                TargetClient.GetRoleplay().CarEnableId = 0;//Coloca el enable para conducir
                TargetClient.GetRoleplay().CarEffectId = 0;//Guarda el enable del último auto en conducción.
                TargetClient.GetRoomUser().ApplyEffect(0);
                TargetClient.GetRoomUser().FastWalking = false;
                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(TargetClient, "event_vehicle", "close");// WS FUEL
                TargetClient.GetRoleplay().CooldownManager.CreateCooldown("avion", 1000, 60);
                return;
            }

            int Range = Weapon.Range;
            if (Client.GetRoleplay().HechizoRange > 0)
                Range += Client.GetRoleplay().HechizoRange;

            foreach (RoomUser User in Client.GetRoomUser().GetRoom().GetRoomUserManager().GetRoomUsers())
            {
                if (User == null || User.GetClient() == null)
                    continue;

                User.GetClient().GetRoleplay().UpdateTimerDialogue("Bala-Sound", Weapon.Name, TargetClient.GetHabbo().Id, 100);
                //TargetClient.GetRoleplay().UpdateTimerDialogue("Bala-Sound", Weapon.Name, 70, 100);

            }

            if (Distance > Range)
            {
                RoleplayManager.ShoutSay(Client, "*Intentó de disparar a " + TargetClient.GetHabbo().Username + ", pero falla*", 4, "black", true); 
                Client.GetRoleplay().GunShots++;

                Client.GetRoleplay().Bullets--;
                return;
            }
            #endregion
            Random Random = new Random();
            int bulletsPerShot = 1;
            int RandomNumber = Random.Next(1, 4);
            for (int i = 0; i < bulletsPerShot; i++)
            {
                //bbullets++;
            }

            // Procedimiento de daño al objetivo
            if (TargetClient.GetRoleplay().CurHealth - Damage <= 0)
            {
                Client.GetRoleplay().ClearWebSocketDialogue();

                string Text = Weapon.FiringText.Split(':')[1];
                string GunName = Weapon.PublicName;

                RoleplayManager.ShoutSay(Client, FormatFiringText(Text, GunName, TargetClient.GetHabbo().Username, Damage, Weapon.Energy), 6, "black", true);

                PolarEnvironment.GetGame().GetQuestManager().ProgressUserQuest(Client, QuestType.KILL_USER);
                PolarEnvironment.GetGame().GetAchievementManager().ProgressAchievement(Client, "ACH_Kills", 1);
                PolarEnvironment.GetGame().GetAchievementManager().ProgressAchievement(TargetClient, "ACH_Death", 1);

                #region Player Stats
                Client.GetRoleplay().LastKilled = TargetClient.GetHabbo().Id;
                Client.GetRoleplay().Kills += 1;
                Client.GetRoleplay().GunKills += 1;

                string RoomId = Client.GetHabbo().CurrentRoomId.ToString() != "0" ? Client.GetHabbo().CurrentRoomId.ToString() : "Unknown";
                Wanted NewWanted = new Wanted(Convert.ToUInt32(Client.GetHabbo().Id), RoomId, 5);
                if (GroupManager.HasJobCommand(TargetClient, "law") && TargetClient.GetRoleplay().IsWorking)
                    TargetClient.GetRoleplay().CopDeaths += 1;
                else
                    TargetClient.GetRoleplay().Deaths += 1;

                if (!Client.GetRoleplay().WantedFor.Contains("Asesinato"))
                    Client.GetRoleplay().WantedFor = Client.GetRoleplay().WantedFor + "Asesinato, ";
                #endregion

                #region Exp Calculator
                int Multiplier = 2;

                int Chance = Random.Next(1, 101);

                if (Chance <= 16)
                {
                    if (Chance <= 8)
                        Multiplier = 6;
                    else
                        Multiplier = 5;
                }


                #endregion

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
                                    if (RoleplayManager.DoubleExp == true)
                                        LevelManager.AddLevelEXP(Client, GetEXP(Client, TargetClient) * (Multiplier + 2));
                                    else
                                        LevelManager.AddLevelEXP(Client, GetEXP(Client, TargetClient) * Multiplier);

                                    EnemyGang[0].Balance -= Bonif;
                                    EnemyGang[0].SetBussines(EnemyGang[0].Balance, EnemyGang[0].Stock);

                                    MyGang[0].AddLog(Client.GetHabbo().Id, Client.GetHabbo().Username + " mata a " + TargetClient.GetHabbo().Username + ", integrante de la banda " + EnemyGang[0].Name + ", ganando $ " + String.Format("{0:N0}", (Bonif / 2)), (Bonif / 2));

                                    Client.GetHabbo().Credits += (Bonif / 2);
                                    Client.GetHabbo().UpdateCreditsBalance();
                                    Client.SendWhisper("¡Has ganado $ " + String.Format("{0:N0}", (Bonif / 2)) + " y " + GetEXP(Client, TargetClient) + " Experiencia, de bonificación por matar a un miembro de la banda " + EnemyGang[0].Name + "!", 1);

                                    TargetClient.SendWhisper("¡Tu banda pierde $ " + String.Format("{0:N0}", Bonif) + " por haber sido asesinad@ por un miembro de la banda " + MyGang[0].Name + "!", 1);
                                }
                                else
                                    Client.SendWhisper("¡Has matado a un miembro de la banda " + EnemyGang[0].Name + "! Pero no has ganado ninguna bonificación porque su banda está en bancarota.", 1);
                            }
                            else
                            {
                                Client.SendWhisper("¡Has matado a un miembro de la banda " + EnemyGang[0].Name + "! Pero no has ganado ninguna bonificación porque tu banda no tienen ningún barrio controlado.", 1);
                            }

                            MyGang[0].GangKills += 1;
                            MyGang[0].UpdateStat(MyGang[0].Id, "gang_kills", MyGang[0].GangKills);
                        }

                        if (EnemyGang == null || EnemyGang.Count <= 0)
                        {
                            MyGang[0].GangKills += 1;
                            MyGang[0].UpdateStat(MyGang[0].Id, "gang_kills", MyGang[0].GangKills);
                        }

                        if (GroupManager.HasJobCommand(TargetClient, "law"))
                        {
                            MyGang[0].GangCopKills += 1;
                            MyGang[0].UpdateStat(MyGang[0].Id, "gang_cop_kills", MyGang[0].GangCopKills);
                        }
                    }
                }
                else
                {
                    if (RoleplayManager.DoubleExp == true)
                        LevelManager.AddLevelEXP(Client, GetEXP(Client, TargetClient) * (Multiplier + 2));
                    else
                        LevelManager.AddLevelEXP(Client, GetEXP(Client, TargetClient) * Multiplier);
                }

                if (EnemyGang != null && EnemyGang.Count > 0)
                {
                    EnemyGang[0].GangDeaths += 1;
                    EnemyGang[0].UpdateStat(EnemyGang[0].Id, "gang_deaths", EnemyGang[0].GangDeaths);
                }
                #endregion

                #region Gang Stats 
                Group Gang = GroupManager.GetGang(Client.GetRoleplay().GangId);
                Group TarGetGang = GroupManager.GetGang(TargetClient.GetRoleplay().GangId);

                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    if (Gang != null)
                    {
                        if (Gang.Id > 1000)
                        {
                            int ScoreIncrease = Random.Next(1, 11);

                            Gang.GangKills += 1;
                            Gang.GangScore += ScoreIncrease;

                            dbClient.RunQuery("UPDATE `rp_gangs` SET `gang_kills` = '" + Gang.GangKills + "', `gang_score` = '" + Gang.GangScore + "' WHERE `id` = '" + Gang.Id + "'");
                        }
                    }
                    if (TarGetGang != null)
                    {
                        if (TarGetGang.Id > 1000)
                        {
                            TarGetGang.GangDeaths += 1;

                            dbClient.RunQuery("UPDATE `rp_gangs` SET `gang_deaths` = '" + TarGetGang.GangDeaths + "' WHERE `id` = '" + TarGetGang.Id + "'");
                        }
                    }
                }
                #endregion

                //PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(Client, "compose_discord|" + Client.GetHabbo().Username + "|Asesinó a|" + TargetClient.GetHabbo().Username);

                BountyManager.CheckBounty(Client, TargetClient.GetHabbo().Id);
                RoleplayManager.ShoutSay(TargetClient, "*" + Client.GetHabbo().Username + " me ha asesinado!*", 32, "black", true);
                TargetClient.GetRoleplay().CurHealth = 0;
                TargetClient.GetRoleplay().IsDead = true;
                TargetClient.GetRoleplay().DeadTimeLeft = RoleplayManager.DeathTime;


                #region Live Feed
                foreach (GameClient client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                {
                    if (client == null)
                        continue;

                    PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(client, "event_feedcomposer", "alert|" + Client.GetHabbo().Username + "|" + TargetClient.GetHabbo().Username + "|" + "Asesinó a");
                }
                #endregion
            }
            else
            {
                // Diccionario estático para rastrear el último disparo por usuario


        // Dentro de tu método de disparo:
        int userId = Client.GetHabbo().Id;
        DateTime now = DateTime.Now;

// Verificar cooldown
if (_lastShotTime.ContainsKey(userId) && (now - _lastShotTime[userId]) < ShotCooldown)
{
    return; // Ignorar el disparo si está en cooldown
}

// Actualizar tiempo del último disparo
_lastShotTime[userId] = now;

string Text = Weapon.FiringText.Split(':')[0];
        string GunName = Weapon.PublicName;
        string modifiedText;

if (RandomNumber > 1)
{
    modifiedText = "*Dispara (" + RandomNumber + ") balas con su " + GunName + 
                   " contra " + TargetClient.GetHabbo().Username + 
                   ", causando " + Damage + " de daño*";
}
else
{
    modifiedText = "*Dispara (" + RandomNumber + ") bala con su " + GunName + 
                   " contra " + TargetClient.GetHabbo().Username + 
                   ", causando " + Damage + " de daño*";
}

                RoleplayManager.ShoutSay(Client, modifiedText, 6, "black", true);

                Client.GetRoleplay().OpenUsersDialogue(TargetClient);
                TargetClient.GetRoleplay().OpenUsersDialogue(Client);

                PolarEnvironment.GetGame().GetQuestManager().ProgressUserQuest(Client, QuestType.SHOOT_USER);
            }

            if (TargetClient.GetRoleplay().ChalecoPor > 0)
            {
                if (TargetClient.GetRoleplay().ChalecoPor - Damage <= 0)

                {
                    TargetClient.SendMessage(new WeaponsComposer(TargetClient));
                    TargetClient.GetRoleplay().ChalecoPor = 0;
                    RoleplayManager.GetLookAndMotto(TargetClient, "poof");
                    TargetClient.SendMessage(new UserChangeComposer(TargetClient.GetRoomUser(), true));
                    TargetClient.GetHabbo().CurrentRoom.SendMessage(new UserChangeComposer(TargetClient.GetRoomUser(), false));
                }
                else
                {
                    TargetClient.GetRoleplay().ChalecoPor -= Damage;
                    TargetClient.SendMessage(new UserChangeComposer(TargetClient.GetRoomUser(), true));
                    TargetClient.GetHabbo().CurrentRoom.SendMessage(new UserChangeComposer(TargetClient.GetRoomUser(), false));
                }

            }
            else if (TargetClient.GetRoleplay().ChalecoPor <= 0)
            {
                if (TargetClient.GetRoleplay().CurHealth - Damage <= 0)
                    TargetClient.GetRoleplay().CurHealth = 0;
                else
                    TargetClient.GetRoleplay().CurHealth -= Damage;
            }
            else if (TargetClient.GetRoleplay().HechizoShield > 0)
            {
                if (TargetClient.GetRoleplay().HechizoShield - Damage <= 0)
                    TargetClient.GetRoleplay().HechizoShield = 0;
                else
                    TargetClient.GetRoleplay().HechizoShield -= Damage;
            }

            Client.GetRoleplay().Bullets--;

            Client.GetRoleplay().CurEnergy -= Weapon.Energy;

            Client.GetRoleplay().GunShots++;


            if (!Client.GetRoleplay().WantedFor.Contains("intento de asalto"))
                Client.GetRoleplay().WantedFor = Client.GetRoleplay().WantedFor + "intento de asalto, ";


        }

        private bool IsPlayerWorking(GameClient Client, GameClient TargetClient)
        {
            if (!Client.GetRoleplay().IsWorking && Client.GetRoleplay().JobId == 3)
            {
                Client.SendWhisper("¡No puedes hacer esto mientras no trabajas!");
                return false;
            }

            if (TargetClient.GetRoleplay().IsWorking && TargetClient.GetRoleplay().JobId == 2 && TargetClient.GetRoleplay().JobId > 4)
            {
                Client.SendWhisper("¡No puedes hacer esto mientras él trabaja!");
                return false;
            }
            return true;
        }

        private bool HasImmunity(GameClient Client, GameClient TargetClient)
        {
            if (Client.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("inmunity") || Client.GetRoleplay().IsNoob)
            {
                Client.SendWhisper("¡No puedes hacer esto porque tienes inmunidad!");
                return true;
            }

            if (TargetClient.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("inmunity") || TargetClient.GetRoleplay().IsNoob)
            {
                Client.SendWhisper("¡No puedes hacer esto porque tiene inmunidad!");
                return true;
            }

            return false;
        }

        private void FreezeTarget(GameClient TargetClient)
        {
            TargetClient.GetRoomUser().Frozen = true;
            TargetClient.GetRoomUser().CanWalk = false;
            TargetClient.GetRoomUser().ClearMovement(true);
        }


        private void HandleTargetDamage(GameClient Client, GameClient TargetClient, int Damage, Weapon Weapon, int bulletsPerShot)
        {
            
        }

        private void KillTarget(GameClient Client, GameClient TargetClient, int Damage, Weapon Weapon)
        {
           
        }

        public void Executex(GameClient Client, GameClient TargetClient, bool HitClosest = false)
        {
            if (!CanCombat(Client, TargetClient))
                return;

            #region Variables

            RoomUser RoomUser = Client.GetRoomUser();
            RoomUser TargetRoomUser = TargetClient.GetRoomUser();
            int Damage = GetDamage(Client, TargetClient);
            Weapon Weapon = Client.GetRoleplay().EquippedWeapon;
            Point ClientPos = RoomUser.Coordinate;
            Point TargetClientPos = TargetRoomUser.Coordinate;
            double Distance = RoleplayManager.GetDistanceBetweenPoints2D(ClientPos, TargetClientPos);
            // Paralizar
            CryptoRandom Randomx = new CryptoRandom();
            int Chancex = Randomx.Next(1, 101);
            #endregion

            #region Pistola electrica
            if (Weapon.Name == "electrica")
            {
                if (Distance <= RoleplayManager.StunGunRange)
                {
                    if (Chancex <= 8)
                    {
                        RoleplayManager.Shout(Client, "*Dispara su pistola electrica hacia " + TargetClient.GetHabbo().Username + ", pero falla*", 37);

                        Client.GetRoleplay().CooldownManager.CreateCooldown("stun", 1000, 3);
                    }
                    else
                    {

                if (!GroupManager.HasJobCommand(Client, "stun"))
                {
                    Client.SendWhisper("¡No eres un policia para utilizar esta arma!", 1);
                    return;
                }
                RoleplayManager.Shout(Client, "*Dispara su pistola electrica hacia " + TargetClient.GetHabbo().Username + " inmovilizándolo inmediatamente*", 37);
                TargetClient.GetRoleplay().TimerManager.CreateTimer("stun", 1000, false);
                //TargetClient.SendMessage(new FloodControlComposer(15));

                if (TargetClient.GetRoleplay().InsideTaxi)
                    TargetClient.GetRoleplay().InsideTaxi = false;

                TargetClient.GetRoomUser().Frozen = true;
                TargetClient.GetRoomUser().CanWalk = false;
                TargetClient.GetRoomUser().ClearMovement(true);

                #region Desequipar al Convicto
                if (TargetClient.GetRoleplay().EquippedWeapon != null)
                {
                    string UnEquipMessage = TargetClient.GetRoleplay().EquippedWeapon.UnEquipText;
                    UnEquipMessage = UnEquipMessage.Replace("[NAME]", TargetClient.GetRoleplay().EquippedWeapon.PublicName);

                    RoleplayManager.Shout(TargetClient, UnEquipMessage, 5);

                    if (TargetClient.GetRoomUser().CurrentEffect == TargetClient.GetRoleplay().EquippedWeapon.EffectID)
                        TargetClient.GetRoomUser().ApplyEffect(0);

                    if (TargetClient.GetRoomUser().CarryItemID == TargetClient.GetRoleplay().EquippedWeapon.HandItem)
                        TargetClient.GetRoomUser().CarryItem(0);

                    TargetClient.GetRoleplay().CooldownManager.CreateCooldown("unequip", 1000, 3);
                    TargetClient.GetRoleplay().EquippedWeapon = null;

                    TargetClient.GetRoleplay().WLife = 0;
                    TargetClient.GetRoleplay().Bullets = 0;
                }
                #endregion

                Client.GetRoleplay().CooldownManager.CreateCooldown("stun", 1000, 3);
                    }
                }
                else
                {
                    RoleplayManager.Shout(Client, "*Dispara su pistola electrica hacia " + TargetClient.GetHabbo().Username + ", pero el disparo no lo alcanza*", 37);
                    TargetClient.GetRoleplay().SpecialCooldowns.TryUpdate("stun", 1800, TargetClient.GetRoleplay().SpecialCooldowns["stun"]);
                }
            }
            #endregion

            #region Ammo Check
            if (Client.GetRoleplay().GunShots >= Weapon.ClipSize)
            {
                Weapon.Reload(Client, TargetClient);
                Client.GetRoleplay().CooldownManager.CreateCooldown("reload", 1000, Weapon.ReloadTime);
                return;
            }
            #endregion

            #region Working Check
            if (!RoomUser.GetRoom().TurfEnabled)
            {
                if (Client.GetRoleplay().IsWorking == false && Client.GetRoleplay().JobId == 3)
                {
                    Client.SendWhisper("¡No puedes hacer esto mientras no trabajas!");
                    return;
                }
            }

            if (TargetClient.GetRoleplay().IsWorking == true && TargetClient.GetRoleplay().JobId == 2 && TargetClient.GetRoleplay().JobId > 4)
            {
                Client.SendWhisper("¡No puedes hacer esto mientras el trabaja!");
                return;
            }
            if (Client.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("inmunity") || Client.GetRoleplay().IsNoob == true)
            {
                Client.SendWhisper("¡No puedes hacer esto porque tienes inmunidad!");
                return;
            }
            if (TargetClient.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("inmunity") || TargetClient.GetRoleplay().IsNoob == true)
            {
                Client.SendWhisper("¡No puedes hacer esto porque tiene inmunidad!");
                return;
            }
            #endregion

            #region Distance Check

            if(TargetClient.GetRoleplay().CarEnableId == 817)
            {
                RoleplayManager.Shout(Client, "*Intentó de disparar a " + TargetClient.GetHabbo().Username + ", pero falla*", 4);
                Client.GetRoleplay().GunShots++;

                Client.GetRoleplay().Bullets--;
                return;
            }
            else if (TargetClient.GetRoleplay().CarEnableId != 817 && Weapon.Name == "rpg")
            {
                Client.SendWhisper("¡No puedes disparar al usuario con está arma, solo aviones!", 4);
                return;
            }

            if (TargetClient.GetRoleplay().DrivingCarId == 817 && Weapon.Name == "rpg")
            {
                //Retornamos a valores predeterminados
                TargetClient.GetRoleplay().DrivingCar = false;
                TargetClient.GetRoleplay().DrivingInCar = false;

                //Combustible System
                TargetClient.GetRoleplay().CarType = 0;// Define el gasto de combustible
                TargetClient.GetRoleplay().CarFuel = 0;
                TargetClient.GetRoleplay().CarMaxFuel = 0;
                TargetClient.GetRoleplay().CarTimer = 0;
                TargetClient.GetRoleplay().CarLife = 0;

                TargetClient.GetRoleplay().CarEnableId = 0;//Coloca el enable para conducir
                TargetClient.GetRoleplay().CarEffectId = 0;//Guarda el enable del último auto en conducción.
                TargetClient.GetRoomUser().ApplyEffect(0);
                TargetClient.GetRoomUser().FastWalking = false;
                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(TargetClient, "event_vehicle", "close");// WS FUEL
                TargetClient.GetRoleplay().CooldownManager.CreateCooldown("avion", 1000, 60);
                return;
            }

            int Range = Weapon.Range;
            if (Client.GetRoleplay().HechizoRange > 0)
                Range += Client.GetRoleplay().HechizoRange;

            if (Distance > Range)
            {
                RoleplayManager.Shout(Client, "*Intentó de disparar a " + TargetClient.GetHabbo().Username + ", pero falla*", 4);
                Client.GetRoleplay().GunShots++;

                Client.GetRoleplay().Bullets--;
                return;
            }
            #endregion

           /* if (!TargetClient.GetRoleplay().DrivingCar)
            {*/
                #region Target Death Procedure
                if (TargetClient.GetRoleplay().CurHealth - Damage <= 0)
                {
                    Client.GetRoleplay().ClearWebSocketDialogue();

                    string Text = Weapon.FiringText.Split(':')[1];
                    string GunName = Weapon.PublicName;

                    //RoleplayManager.Shout(Client, FormatFiringText(Text, GunName, TargetClient.GetHabbo().Username, Damage, Weapon.Energy), 6);


                    PolarEnvironment.GetGame().GetQuestManager().ProgressUserQuest(Client, QuestType.KILL_USER);
                    PolarEnvironment.GetGame().GetAchievementManager().ProgressAchievement(Client, "ACH_Kills", 1);
                    PolarEnvironment.GetGame().GetAchievementManager().ProgressAchievement(TargetClient, "ACH_Death", 1);

                    #region Player Stats
                    Client.GetRoleplay().LastKilled = TargetClient.GetHabbo().Id;
                    Client.GetRoleplay().Kills += 1;
                    Client.GetRoleplay().GunKills += 1;

                    string RoomId = Client.GetHabbo().CurrentRoomId.ToString() != "0" ? Client.GetHabbo().CurrentRoomId.ToString() : "Unknown";
                    Wanted NewWanted = new Wanted(Convert.ToUInt32(Client.GetHabbo().Id), RoomId, 5);
                    if (GroupManager.HasJobCommand(TargetClient, "law") && TargetClient.GetRoleplay().IsWorking)
                        TargetClient.GetRoleplay().CopDeaths += 1;
                    else
                        TargetClient.GetRoleplay().Deaths += 1;

                    if (!Client.GetRoleplay().WantedFor.Contains("Asesinato"))
                        Client.GetRoleplay().WantedFor = Client.GetRoleplay().WantedFor + "Asesinato, ";
                    #endregion

                    #region Exp Calculator
                    CryptoRandom Random = new CryptoRandom();
                    int Multiplier = 2;

                    int Chance = Random.Next(1, 101);

                    if (Chance <= 16)
                    {
                        if (Chance <= 8)
                            Multiplier = 6;
                        else
                            Multiplier = 5;
                    }


                    #endregion

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
                                        if (RoleplayManager.DoubleExp == true)
                                            LevelManager.AddLevelEXP(Client, GetEXP(Client, TargetClient) * (Multiplier + 2));
                                        else
                                            LevelManager.AddLevelEXP(Client, GetEXP(Client, TargetClient) * Multiplier);

                                        EnemyGang[0].Balance -= Bonif;
                                        EnemyGang[0].SetBussines(EnemyGang[0].Balance, EnemyGang[0].Stock);

                                        MyGang[0].AddLog(Client.GetHabbo().Id, Client.GetHabbo().Username + " mata a " + TargetClient.GetHabbo().Username + ", integrante de la banda " + EnemyGang[0].Name + ", ganando $ " + String.Format("{0:N0}", (Bonif / 2)), (Bonif / 2));

                                        Client.GetHabbo().Credits += (Bonif / 2);
                                        Client.GetHabbo().UpdateCreditsBalance();
                                        Client.SendWhisper("¡Has ganado $ " + String.Format("{0:N0}", (Bonif / 2)) + " y " + GetEXP(Client, TargetClient) + " Experiencia, de bonificación por matar a un miembro de la banda " + EnemyGang[0].Name + "!", 1);

                                        TargetClient.SendWhisper("¡Tu banda pierde $ " + String.Format("{0:N0}", Bonif) + " por haber sido asesinad@ por un miembro de la banda " + MyGang[0].Name + "!", 1);
                                    }
                                    else
                                        Client.SendWhisper("¡Has matado a un miembro de la banda " + EnemyGang[0].Name + "! Pero no has ganado ninguna bonificación porque su banda está en bancarota.", 1);
                                }
                                else
                                {
                                    Client.SendWhisper("¡Has matado a un miembro de la banda " + EnemyGang[0].Name + "! Pero no has ganado ninguna bonificación porque tu banda no tienen ningún barrio controlado.", 1);
                                }

                                MyGang[0].GangKills += 1;
                                MyGang[0].UpdateStat(MyGang[0].Id, "gang_kills", MyGang[0].GangKills);
                            }

                            if (EnemyGang == null || EnemyGang.Count <= 0)
                            {
                                MyGang[0].GangKills += 1;
                                MyGang[0].UpdateStat(MyGang[0].Id, "gang_kills", MyGang[0].GangKills);
                            }

                            if (GroupManager.HasJobCommand(TargetClient, "law"))
                            {
                                MyGang[0].GangCopKills += 1;
                                MyGang[0].UpdateStat(MyGang[0].Id, "gang_cop_kills", MyGang[0].GangCopKills);
                            }
                        }
                    }
                    else
                    {
                        if (RoleplayManager.DoubleExp == true)
                            LevelManager.AddLevelEXP(Client, GetEXP(Client, TargetClient) * (Multiplier + 2));
                        else
                            LevelManager.AddLevelEXP(Client, GetEXP(Client, TargetClient) * Multiplier);
                    }

                    if (EnemyGang != null && EnemyGang.Count > 0)
                    {
                        EnemyGang[0].GangDeaths += 1;
                        EnemyGang[0].UpdateStat(EnemyGang[0].Id, "gang_deaths", EnemyGang[0].GangDeaths);
                    }
                    #endregion

                    #region Gang Stats 
                    Group Gang = GroupManager.GetGang(Client.GetRoleplay().GangId);
                    Group TarGetGang = GroupManager.GetGang(TargetClient.GetRoleplay().GangId);

                    using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                    {
                        if (Gang != null)
                        {
                            if (Gang.Id > 1000)
                            {
                                int ScoreIncrease = Random.Next(1, 11);

                                Gang.GangKills += 1;
                                Gang.GangScore += ScoreIncrease;

                                dbClient.RunQuery("UPDATE `rp_gangs` SET `gang_kills` = '" + Gang.GangKills + "', `gang_score` = '" + Gang.GangScore + "' WHERE `id` = '" + Gang.Id + "'");
                            }
                        }
                        if (TarGetGang != null)
                        {
                            if (TarGetGang.Id > 1000)
                            {
                                TarGetGang.GangDeaths += 1;

                                dbClient.RunQuery("UPDATE `rp_gangs` SET `gang_deaths` = '" + TarGetGang.GangDeaths + "' WHERE `id` = '" + TarGetGang.Id + "'");
                            }
                        }
                    }
                    #endregion
                    //PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(Client, "compose_discord|" + Client.GetHabbo().Username + "|Asesinó a|" + TargetClient.GetHabbo().Username);
                    
                    BountyManager.CheckBounty(Client, TargetClient.GetHabbo().Id);
                    RoleplayManager.Shout(TargetClient, "*"+Client.GetHabbo().Username + " me ha asesinado!*", 32);
                    TargetClient.GetRoleplay().CurHealth = 0;
                    TargetClient.GetRoleplay().IsDead = true;
                    TargetClient.GetRoleplay().DeadTimeLeft = RoleplayManager.DeathTime;

                    #region Live Feed
                    foreach (GameClient client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                    {
                        if (client == null)
                            continue;

                        PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(client, "event_feedcomposer", "alert|" + Client.GetHabbo().Username + "|" + TargetClient.GetHabbo().Username + "|" + "Asesinó a");
                    }
                    #endregion
                }
                #endregion

                #region Target Damage Procedure (Did not die)
                else
                {
                    string Text = Weapon.FiringText.Split(':')[0];
                    string GunName = Weapon.PublicName;
                    Client.GetRoleplay().OpenUsersDialogue(TargetClient);
                    TargetClient.GetRoleplay().OpenUsersDialogue(Client);

                    PolarEnvironment.GetGame().GetQuestManager().ProgressUserQuest(Client, QuestType.SHOOT_USER);
                    //RoleplayManager.Shout(Client, FormatFiringText(Text, GunName, TargetClient.GetHabbo().Username, Damage, Weapon.Energy), 6);
                }
            #endregion

            
            //TargetClient.GetPlay().CurHealth -= Damage;
            if (TargetClient.GetRoleplay().ChalecoPor <= 0)
                {
                    if (TargetClient.GetRoleplay().CurHealth - Damage <= 0)
                        TargetClient.GetRoleplay().CurHealth = 0;
                    else
                        TargetClient.GetRoleplay().CurHealth -= Damage;
                }
            else if (TargetClient.GetRoleplay().ChalecoPor > 0)
            {
                    if (TargetClient.GetRoleplay().ChalecoPor - Damage <= 0)
                        TargetClient.GetRoleplay().ChalecoPor = 0;
                    else
                        TargetClient.GetRoleplay().ChalecoPor -= Damage;
            }
            else if(TargetClient.GetRoleplay().HechizoShield > 0)
            {
                if (TargetClient.GetRoleplay().HechizoShield - Damage <= 0)
                    TargetClient.GetRoleplay().HechizoShield = 0;
                else
                    TargetClient.GetRoleplay().HechizoShield -= Damage;
            }

            /*}
            else
            {
                if (TargetClient.GetRoleplay().ChalecoPor <= 0)
                {
                    if (TargetClient.GetRoleplay().CurHealth - Damage <= 0)
                        TargetClient.GetRoleplay().CurHealth = 0;
                    else
                        TargetClient.GetRoleplay().CurHealth -= Damage;
                }
                else // Si tiene chaleco...
                {
                    if (TargetClient.GetRoleplay().ChalecoPor - Damage <= 0)
                        TargetClient.GetRoleplay().ChalecoPor = 0;
                    else
                        TargetClient.GetRoleplay().ChalecoPor -= Damage;
                }

                Client.GetRoleplay().OpenUsersDialogue(TargetClient);
                TargetClient.GetRoleplay().OpenUsersDialogue(Client);

                //RoleplayManager.Shout(Client, "*Dispara con su " + Weapon.PublicName + " al auto que " + TargetClient.GetHabbo().Username + " concuce causandole " + Damage + " de daño*", 5);

                // Hace daño al auto que conduce
                //TargetClient.GetRoleplay().CarLife -= Damage;
            }*/

            Client.GetRoleplay().Bullets--;

            Client.GetRoleplay().CurEnergy -= Weapon.Energy;

            Client.GetRoleplay().GunShots++;

            foreach (RoomUser User in Client.GetRoomUser().GetRoom().GetRoomUserManager().GetRoomUsers())
            {
                if (User == null || User.GetClient() == null)
                    continue;

                User.GetClient().GetRoleplay().UpdateTimerDialogue("Bala-Sound", Weapon.Name, TargetClient.GetHabbo().Id, 100);
                //TargetClient.GetRoleplay().UpdateTimerDialogue("Bala-Sound", Weapon.Name, 70, 100);

            }
            if (!Client.GetRoleplay().WantedFor.Contains("intento de asalto"))
                Client.GetRoleplay().WantedFor = Client.GetRoleplay().WantedFor + "intento de asalto, ";
        }

        /// <summary>
        /// Executes this type of combat on a Bot
        /// </summary>
        public void ExecuteBot(GameClient Client, RoleplayBot? Bot = null)
        {
            CryptoRandom Randomizer = new CryptoRandom();
            int Damage = Randomizer.Next(1, 36);

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

            if (BotUser.GetBotRoleplay().Dead)
            {
                Client.SendWhisper("Este bot se encuentra muerto, espera que se regenere.", 1);
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
                RoleplayManager.Shout(Client, "*Dispara a " + Bot.Name + ", causando " + Damage + " de Daño*", 6);

            if ((Client.GetRoleplay().CurEnergy - 2) <= 0)
                Client.GetRoleplay().CurEnergy = 0;
            else
                Client.GetRoleplay().CurEnergy -= 2;

            Client.GetRoleplay().GunShots++;
            Client.GetRoleplay().Bullets--;

            if (Bot.CurHealth - Damage <= 0)
                Bot.CurHealth = 0;
            else
                Bot.CurHealth -= Damage;

            if (!Died)
            {
                BotUser.Chat("*[" + Bot.CurHealth + "/" + Bot.MaxHealth + "]*", true, 3);
                BotUser.GetBotRoleplay().UserAttacking = Client;
                BotUser.GetBotRoleplay().Roaming = false;
                BotUser.GetBotRoleplay().Attacking = true;
                BotUser.GetBotRoleplayAI().OnAttacked(Client);
            }
            else
                BotUser.GetBotRoleplayAI().OnDeath(Client);

            Client.GetRoleplay().CooldownManager.CreateCooldown("gun", 1000, RoleplayManager.DefaultHitCooldown);
        }

        /// <summary>
        /// Checks if a client can complete this action
        /// </summary>
        public bool CanCombat(GameClient Client, GameClient TargetClient, RoleplayBot? Bot = null)
        {
            #region Variables
            RoomUser RoomUser = Client.GetRoomUser();
            RoomUser TargetRoomUser = TargetClient.GetHabbo().CurrentRoom.GetRoomUserManager().GetRoomUserByHabbo(TargetClient.GetHabbo().Username);
            #endregion

            #region Cooldown Conditions
            if (Client.GetRoleplay().TryGetCooldown("reload", false))
                return false;

            if (Client.GetRoleplay().TryGetCooldown("gun", false))
                return false;
            #endregion

            #region Main Conditions
            Weapon Weapon = Client.GetRoleplay().EquippedWeapon;
            Room Room = null;

            if (Weapon == null)
                return false;

            if (Client.GetHabbo().CurrentRoomId > 0)
                Room = Client.GetHabbo().CurrentRoom;

            if (Room != null)
            {
                if(Weapon.Name != "electrica"){
                    if (Room.SafeZoneEnabled)
                    {
                        Client.SendWhisper("¡No puedes disparar en esta habitación!", 1);
                        return false;
                    }
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
                    int LevelDifference = Math.Abs(Client.GetRoleplay().Level - TargetClient.GetRoleplay().Level);

                    if (LevelDifference > 8)
                    {
                        Client.SendWhisper("((¡No puedes dispararle a alguien con 8 niveles de diferencia mayor a ti!))", 1);
                        return false;
                    }
                }
            }

            if (Weapon == null)
            {
                Client.SendWhisper("Actualmente no tienes armas equipadas", 1);
                return false;
            }


            #endregion

            #region User Conditions

            if (RoomUser == null)
                return false;

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

            if (Client.GetRoleplay().StaffOnDuty || Client.GetRoleplay().AmbassadorOnDuty)
            {
                Client.SendWhisper("¡No puedes disparar a alguien mientras estás de servicio!", 1);
                return false;
            }

            if (Client.GetRoleplay().Cuffed)
            {
                Client.SendWhisper("¡No puedes disparar a un ciudadano mientras estás esposado!", 1);
                return false;
            }


            if (Client.GetRoleplay().IsJailed)
            {
                Client.SendWhisper("¡No puedes completar esta acción mientras estás en prisión!", 1);
                return false;
            }

            /*if (Client.GetRoleplay().DrivingCar)
            {
                Client.SendWhisper("No puedes disparar mientras vas dentro de un vehículo.", 1);
                return false;
            }*/

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
                            if (Client.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("inmunity"))
                                Client.GetRoleplay().TimerManager.ActiveTimers["inmunity"].EndTimer();
                        }

                        Client.GetRoleplay().IsNoob = false;
                        Client.GetRoleplay().NoobTimeLeft = 0;
                        return true;
                    }
            }

            if (Client.GetRoleplay().Bullets <= 0 && Weapon.Name != "electrica")
            {
                Client.SendWhisper("¡No puedes completar esta acción cuando te quedas sin balas!", 1);
                return false;
            }

            if (Client.GetRoleplay().CurEnergy <= 0)
            {
                Client.SendWhisper("No puede completar esta acción ya que se quedó sin energía", 1);
                return false;
            }
            #endregion

            #region Target Conditions
            if (TargetClient == Client)
            {
                Client.SendWhisper("¡No puedes dispararte!", 1);
                return false;
            }

            if (TargetClient.GetRoleplay().IsDead)
            {
                Client.SendWhisper("¡No puedes disparar a alguien que está muerto!", 1);
                return false;
            }

            if (TargetClient.GetRoleplay().IsJailed)
            {
                Client.SendWhisper("¡No puedes disparar a alguien que está en prisión!", 1);
                return false;
            }

            if (TargetClient.GetRoleplay().PassiveMode)
            {
                Client.SendWhisper("((Esta persona se encuentra en modo pasivo.))", 1);
                return false;
            }

            if (TargetClient.GetRoleplay().StaffOnDuty)
            {
                Client.SendWhisper("No se puede disparar a un miembro del personal que está de servicio", 1);
                return false;
            }

            if (TargetClient.GetRoleplay().AmbassadorOnDuty)
            {
                Client.SendWhisper("¡No puedes disparar contra un embajador que está de servicio!", 1);
                return false;
            }

            if (TargetClient.GetRoomUser().IsAsleep)
            {
                Client.SendWhisper("¡No puedes disparar a alguien que no está jugando el juego ahora mismo!", 1);
                return false;
            }

            if (TargetClient.GetRoleplay().IsNoob == true)
            {
                Client.SendWhisper("*Este usuario se encuentra bajo inmunidad*", 1);
                return false;
            }

            if (TargetClient.LoggingOut || TargetClient.GetRoleplay().IsDisconnecting)
            {
                Client.SendWhisper("((Esta persona se encuentra desconectándose.))", 1);
                return false;
            }

            #endregion

            return true;
        }

        /// <summary>
        /// Gets the damage
        /// </summary>
        private int GetDamage(GameClient Client, GameClient TargetClient, RoleplayBot? Bot = null)
        {
            CryptoRandom Randomizer = new CryptoRandom();
            Weapon Weapon = Client.GetRoleplay().EquippedWeapon;

            
            int MinDamage = Weapon.MinDamage;
            int MaxDamage = Weapon.MaxDamage;

            if(Client.GetRoleplay().HechizoDamage > 0)
            {
                MaxDamage += Client.GetRoleplay().HechizoDamage;
            }

            int Damage = Randomizer.Next(MinDamage, MaxDamage);

            if (Client.GetRoleplay().Class.ToLower() == "gunner")
                Damage += Randomizer.Next(1, 3);

            if (Client.GetRoleplay().GangId > 1000 && Bot == null)
            {
                if (GroupManager.HasGangCommand(Client, "gunner"))
                {
                    if (RoleplayManager.GenerateRoom(Client.GetHabbo().CurrentRoomId, out Room room, false) && room.TurfEnabled || GroupManager.HasJobCommand(TargetClient, "guide"))
                        Damage += Randomizer.Next(0, 6);
                }
            }


            return Damage;
        }

        /// <summary>
        /// Formats the string
        /// </summary>
        private string FormatFiringText(string Text, string GunName, string TargetName, int Damage, int Energy)
        {
            Text = Text.Replace("[NAME]", GunName);
            Text = Text.Replace("[TARGET]", TargetName);
            Text = Text.Replace("[DAMAGE]", Convert.ToString(Damage));
            Text = Text.Replace("[ENERGY]", Convert.ToString(Energy));
            return Text;
        }

        /// <summary>
        /// calculates the amount of exp to give to the client
        /// </summary>
        public int GetEXP(GameClient Client, GameClient TargetClient, RoleplayBot? Bot = null)
        {
            CryptoRandom Random = new CryptoRandom();
            int LevelDifference = Math.Abs(Client.GetRoleplay().Level - TargetClient.GetRoleplay().Level);
            int Amount;
            int Bonus;

            if (LevelDifference > 8)
            {
                Amount = 0;
                Bonus = 0;
            }
            else
            {
                if (TargetClient.GetRoleplay().Level > Client.GetRoleplay().Level)
                    Bonus = (10 * (LevelDifference + 1)) + LevelDifference * 2;
                else if (TargetClient.GetRoleplay().Level == Client.GetRoleplay().Level)
                    Bonus = (10 * 2) + 3;
                else if (TargetClient.GetRoleplay().Level < Client.GetRoleplay().Level)
                    Bonus = 10;
                else
                    Bonus = 2 * LevelDifference;

                Amount = Random.Next(10, 10 + (LevelDifference + 5));
            }

            return (Amount + Bonus + 15);
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

                if (TargetClient.GetHabbo().Credits < 5)
                    return 0;
            }

            if (Bot != null)
            {
                int MinMoney = Convert.ToInt32(RoleplayData.GetData("bots", "minmoney"));
                int MaxMoney = Convert.ToInt32(RoleplayData.GetData("bots", "maxmoney"));

                if (MaxMoney == 0)
                    return 0;

                CryptoRandom Random = new CryptoRandom();
                return Random.Next(MinMoney, (MaxMoney + 5));
            }

            return TargetClient.GetHabbo().Credits / 5;
        }

        /// <summary>
        /// Gets the rewards from the dead body
        /// </summary>
        /// <param name="Client"></param>
        /// <param name="TargetClient"></param>
        /// <param name="Bot"></param>
        public void GetRewards(GameClient Client, GameClient TargetClient, RoleplayBot? Bot = null)
        {
            int Multiplier = 1;
            PolarEnvironment.GetGame().GetQuestManager().ProgressUserQuest(Client, QuestType.KILL_USER);
            PolarEnvironment.GetGame().GetAchievementManager().ProgressAchievement(Client, "ACH_Kills", 1);

            Client.GetRoleplay().Kills++;
            Client.GetRoleplay().HitKills++;

            // BattlePass Challenge Integration
            PolarEnvironment.GetGame().GetBattlePassManager().ProgressChallenge(Client, "kill_user", 1);

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
