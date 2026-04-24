using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.Communication.Packets.Outgoing.Notifications;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Weapons;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Administrators
{
    class RPStatsCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_rpstats"; }
        }

        public string Parameters
        {
            get { return "%username%"; }
        }

        public string Description
        {
            get { return "Estadisticas del usuario."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("You forgot to enter a username of a person you want to check!", 1);
                return;
            }

            #region Variables
            int JobId = 1;
            int JobRank = 1;
            int MarriedTo;
            bool IsJailed = false;
            int JailTimeLeft;
            bool IsDead = false;
            int DeadTimeLeft;
            bool IsWanted = false;
            int WantedTimeLeft;
            bool OnProbation = false;
            int ProbationTimeLeft;
            int SendHomeTimeLeft;
            int Phone;
            int GangId = 1000;
            int GangRankId = 1;
            int Car;
            int Fuel;

            int Level;
            int LevelEXP;
            int BankSavings;
            int BankChequings;
            int Arrests;
            int Arrested;
            int Evasions;
            int Punches;
            int Kills;
            int HitKills;
            int GunKills;
            int Deaths;
            int CopDeaths;
            int Hunger;
            int Hygiene;
            int Poop;
            int CurHealth;
            int MaxHealth;
            int CurEnergy;
            int MaxEnergy;
            int CurAlcohol;
            int MaxAlcohol;
            int Intelligence;
            int Strength;
            int Stamina;
            int Bullets;
            int Dynamites;
            int Weed;
            int Cocaine;
            int Heroine;
            int Medicina;
            int Caramelos;
            int Cigarettes;
            int IntelligenceEXP;
            int StrengthEXP;
            int StaminaEXP;
            int TimeWorked;
            string Class;

            string Username = Params[1];
            GameClients.GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Username);
            #endregion

            #region Variables Client Check & Set
            if (TargetClient == null)
            {
                using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("SELECT `id` FROM `users` WHERE `username` = '" + Username + "' LIMIT 1");
                    var Row = dbClient.getRow();

                    if (Row == null)
                    {
                        Session.SendWhisper("Sorry! This person does not exist!", 1);
                        return;
                    }

                    int UserId = Convert.ToInt32(Row["id"]);

                    dbClient.SetQuery("SELECT * FROM `rp_stats` WHERE `id` = '" + UserId + "' LIMIT 1");
                    var Stats = dbClient.getRow();

                    if (Stats == null)
                    {
                        Session.SendWhisper("Sorry! This person does not exist!", 1);
                        return;
                    }

                    JobId = Convert.ToInt32(Stats["job_id"]);
                    JobRank = Convert.ToInt32(Stats["job_rank"]);

                    MarriedTo = Convert.ToInt32(Stats["married_to"]);
                    IsJailed = PolarEnvironment.EnumToBool(Stats["is_jailed"].ToString());
                    JailTimeLeft = Convert.ToInt32(Stats["jailed_time_left"]);
                    IsDead = PolarEnvironment.EnumToBool(Stats["is_dead"].ToString());
                    DeadTimeLeft = Convert.ToInt32(Stats["dead_time_left"]);
                    IsWanted = PolarEnvironment.EnumToBool(Stats["is_wanted"].ToString());
                    WantedTimeLeft = Convert.ToInt32(Stats["wanted_time_left"]);
                    OnProbation = PolarEnvironment.EnumToBool(Stats["on_probation"].ToString());
                    ProbationTimeLeft = Convert.ToInt32(Stats["probation_time_left"]);
                    SendHomeTimeLeft = Convert.ToInt32(Stats["sendhome_time_left"]);
                    Phone = Convert.ToInt32(Stats["phone"]);
                    GangId = Convert.ToInt32(Stats["gang_id"]);
                    GangRankId = Convert.ToInt32(Stats["gang_rank"]);
                    Car = Convert.ToInt32(Stats["car"]);
                    Fuel = Convert.ToInt32(Stats["car_fuel"]);

                    Level = Convert.ToInt32(Stats["level"]);
                    LevelEXP = Convert.ToInt32(Stats["level_exp"]);
                    BankSavings = Convert.ToInt32(Stats["bank_savings"]);
                    BankChequings = Convert.ToInt32(Stats["bank_chequings"]);
                    Arrests = Convert.ToInt32(Stats["arrests"]);
                    Arrested = Convert.ToInt32(Stats["arrested"]);
                    Evasions = Convert.ToInt32(Stats["evasions"]);
                    Punches = Convert.ToInt32(Stats["punches"]);
                    Kills = Convert.ToInt32(Stats["kills"]);
                    HitKills = Convert.ToInt32(Stats["hit_kills"]);
                    GunKills = Convert.ToInt32(Stats["gun_kills"]);
                    Deaths = Convert.ToInt32(Stats["deaths"]);
                    CopDeaths = Convert.ToInt32(Stats["cop_deaths"]);
                    Hunger = Convert.ToInt32(Stats["hunger"]);
                    Hygiene = Convert.ToInt32(Stats["hygiene"]);
                    Poop = Convert.ToInt32(Stats["poop"]);
                    CurHealth = Convert.ToInt32(Stats["curhealth"]);
                    MaxHealth = Convert.ToInt32(Stats["maxhealth"]);
                    CurEnergy = Convert.ToInt32(Stats["curenergy"]);
                    MaxEnergy = Convert.ToInt32(Stats["maxenergy"]);
                    CurAlcohol = Convert.ToInt32(Stats["curalcohol"]);
                    MaxAlcohol = Convert.ToInt32(Stats["maxalcohol"]);
                    Intelligence = Convert.ToInt32(Stats["intelligence"]);
                    Strength = Convert.ToInt32(Stats["strength"]);
                    Stamina = Convert.ToInt32(Stats["stamina"]);
                    Bullets = Convert.ToInt32(Stats["bullets"]);
                    Dynamites = Convert.ToInt32(Stats["dynamite"]);
                    Weed = Convert.ToInt32(Stats["weed"]);
                    Cocaine = Convert.ToInt32(Stats["cocaine"]);
                    Heroine = Convert.ToInt32(Stats["heroina"]);
                    Medicina = Convert.ToInt32(Stats["medicina"]);
                    Caramelos = Convert.ToInt32(Stats["caramelos"]);
                    Cigarettes = Convert.ToInt32(Stats["cigarette"]);
                    IntelligenceEXP = Convert.ToInt32(Stats["intelligence_exp"]);
                    StrengthEXP = Convert.ToInt32(Stats["strength_exp"]);
                    StaminaEXP = Convert.ToInt32(Stats["stamina_exp"]);
                    TimeWorked = Convert.ToInt32(Stats["time_worked"]);
                    Class = Stats["class"].ToString();
                }
            }
            else
            {
                JobId = TargetClient.GetRoleplay().JobId;
                JobRank = TargetClient.GetRoleplay().JobRank;
                MarriedTo = TargetClient.GetRoleplay().MarriedTo;
                IsJailed = TargetClient.GetRoleplay().IsJailed;
                JailTimeLeft = TargetClient.GetRoleplay().JailedTimeLeft;
                IsDead = TargetClient.GetRoleplay().IsDead;
                DeadTimeLeft = TargetClient.GetRoleplay().DeadTimeLeft;
                IsWanted = TargetClient.GetRoleplay().IsWanted;
                WantedTimeLeft = TargetClient.GetRoleplay().WantedTimeLeft;
                OnProbation = TargetClient.GetRoleplay().OnProbation;
                ProbationTimeLeft = TargetClient.GetRoleplay().ProbationTimeLeft;
                SendHomeTimeLeft = TargetClient.GetRoleplay().SendHomeTimeLeft;
                //Phone = TargetClient.GetRoleplay().PhoneType;
                GangId = TargetClient.GetRoleplay().GangId;
                GangRankId = TargetClient.GetRoleplay().GangRank;
                Car = TargetClient.GetRoleplay().CarType;
                Fuel = TargetClient.GetRoleplay().CarFuel;

                Level = TargetClient.GetRoleplay().Level;
                LevelEXP = TargetClient.GetRoleplay().LevelEXP;
                BankSavings = TargetClient.GetRoleplay().BankSavings;
                BankChequings = TargetClient.GetRoleplay().BankChequings;
                Arrests = TargetClient.GetRoleplay().Arrests;
                Arrested = TargetClient.GetRoleplay().Arrested;
                Evasions = TargetClient.GetRoleplay().Evasions;
                Punches = TargetClient.GetRoleplay().Punches;
                Kills = TargetClient.GetRoleplay().Kills;
                HitKills = TargetClient.GetRoleplay().HitKills;
                GunKills = TargetClient.GetRoleplay().GunKills;
                Deaths = TargetClient.GetRoleplay().Deaths;
                CopDeaths = TargetClient.GetRoleplay().CopDeaths;
                Hunger = TargetClient.GetRoleplay().Hunger;
                Hygiene = TargetClient.GetRoleplay().Hygiene;
                Poop = TargetClient.GetRoleplay().Poop;
                CurHealth = TargetClient.GetRoleplay().CurHealth;
                MaxHealth = TargetClient.GetRoleplay().MaxHealth;
                CurEnergy = TargetClient.GetRoleplay().CurEnergy;
                MaxEnergy = TargetClient.GetRoleplay().MaxEnergy;
                CurAlcohol = TargetClient.GetRoleplay().CurAlcohol;
                MaxAlcohol = TargetClient.GetRoleplay().MaxAlcohol;
                Intelligence = TargetClient.GetRoleplay().Intelligence;
                Strength = TargetClient.GetRoleplay().Strength;
                Stamina = TargetClient.GetRoleplay().Stamina;
                Bullets = TargetClient.GetRoleplay().Bullets;
                Dynamites = TargetClient.GetRoleplay().Dynamite;
                Weed = TargetClient.GetRoleplay().Weed;
                Heroine = TargetClient.GetRoleplay().Heroina;
                Cocaine = TargetClient.GetRoleplay().Cocaine;
                Medicina = TargetClient.GetRoleplay().Medicina;
                Caramelos = TargetClient.GetRoleplay().Caramelos;
                Cigarettes = TargetClient.GetRoleplay().Cigarettes;
                IntelligenceEXP = TargetClient.GetRoleplay().IntelligenceEXP;
                StrengthEXP = TargetClient.GetRoleplay().StrengthEXP;
                StaminaEXP = TargetClient.GetRoleplay().StaminaEXP;
                TimeWorked = TargetClient.GetRoleplay().TimeWorked;
                Class = TargetClient.GetRoleplay().Class;
            }
            #endregion

            Group job = GroupManager.GetJob(JobId);
            GroupRank rank = GroupManager.GetJobRank(JobId, JobRank);

            StringBuilder MarriedMesssage = new StringBuilder();
            if (MarriedTo != 0) MarriedMesssage.Append(PolarEnvironment.GetGame().GetCacheManager().GenerateUser(MarriedTo).Username);
            else MarriedMesssage.Append("Nobody!");

            StringBuilder JailMessage = new StringBuilder();
            if (IsJailed) JailMessage.Append("They are Jailed for another " + JailTimeLeft + " minutes");
            else JailMessage.Append("They are not jailed");

            StringBuilder DeadMessage = new StringBuilder();
            if (IsDead) DeadMessage.Append("They are dead for another " + DeadTimeLeft + " minutes");
            else DeadMessage.Append("They are not dead");

            StringBuilder WantedMessage = new StringBuilder();
            if (IsWanted) WantedMessage.Append("They are wanted for another " + WantedTimeLeft + " minutes");
            else WantedMessage.Append("They are not wanted");

            StringBuilder ProbationMessage = new StringBuilder();
            if (OnProbation) ProbationMessage.Append("They are on probation for another " + ProbationTimeLeft + " minutes");
            else ProbationMessage.Append("They are not on probation");

            StringBuilder SendhomeMessage = new StringBuilder();
            if (SendHomeTimeLeft > 0) SendhomeMessage.Append("They are sent home for another " + SendHomeTimeLeft + " minutes");
            else SendhomeMessage.Append("They are not sent home from work");

            /*StringBuilder PhoneType = new StringBuilder();
            if (Phone == 0) PhoneType.Append("They do not have a phone");
            if (Phone == 1) PhoneType.Append("They have an LG Gossip. Texts cost 3 Duckets each");
            if (Phone == 2) PhoneType.Append("They have an iPhone 4s. Texts cost 2 Duckets each");
            if (Phone == 3) PhoneType.Append("They have the latest iPhone 7. Texts cost 1 Duckets each");*/

            Group Gang = GroupManager.GetGang(GangId);
            GroupRank GangRank = GroupManager.GetGangRank(GangId, GangRankId);

            string grank = "\n";
            if (GangId > 1000)
            {
                if (GangRank != null)
                    grank = "Gang Rank : " + GangRank.Name + "\n\n";
            }

            StringBuilder CarType = new StringBuilder();
            if (Car == 0)
                CarType.Append("They do not have a car");
            else if (Car == 1)
                CarType.Append("They have a Toyota Corolla. They use 3 fuel for every 10 seconds");
            else if (Car == 2)
                CarType.Append("They have a Honda Accord. They use 2 fuel for every 10 seconds");
            else
                CarType.Append("They have the fanciest Nissan GTR. They use 1 fuel for every 10 seconds");

            StringBuilder CarFuel = new StringBuilder();
            if (Car == 0)
                CarFuel.Append("");
            else
            {
                if (Fuel > 0)
                    CarFuel.Append("Fuel: They have " + String.Format("{0:N0}", Fuel) + " gallons remaining!\n");
                else
                    CarFuel.Append("Fuel: They have no fuel remaining!\n");
            }

            StringBuilder MessageToSend = new StringBuilder().Append(
                                   "-------- Tus estadísticas " + Username + " --------\n\n" +

                                   "--- Información básica ---\n" +
                                   "Level: " + Level + "/" + RoleplayManager.LevelCap + "\n" +
                                   "Level EXP: " + String.Format("{0:N0}", LevelEXP) + "/" + String.Format("{0:N0}", String.Format("{0:N0}", (!LevelManager.Levels.ContainsKey(Level + 1) ? 100000 : LevelManager.Levels[Level + 1]))) + "\n" +
                                   "Clase: " + Class + "\n\n" +

                                   "--- Relacionado al trabajo ---\n" +
                                   "Trabajo: " + job.Name + " " + rank.Name + "\n" +
                                   "Pago: $" + String.Format("{0:N0}", rank.Pay) + " por 10 minutos\n" +
                                   "Enviado a casa: " + SendhomeMessage + "\n" +
                                   "Minutos trabajados: " + String.Format("{0:N0}", TimeWorked) + "\n\n" +

                                   "--- Necesidades humanas ---\n" +
                                   "Vida: " + String.Format("{0:N0}", CurHealth) + "/" + MaxHealth + "\n" +
                                   "Energía: " + CurEnergy + "/" + MaxEnergy + "\n" +
                                   "Alcohol: " + CurAlcohol + "/" + MaxAlcohol + "\n" +
                                   "Hambre: " + Hunger + "/100\n" +
                                   "Higiene: " + Hygiene + "/100\n" +
                                   "Ganas de ir al baño: " + Poop + "/100\n\n" +

                                   "--- Estadísticas nivelables ---\n" +
                                   "Inteligencia: " + Intelligence + "/" + RoleplayManager.IntelligenceCap + " --- EXP: " + String.Format("{0:N0}", IntelligenceEXP) + " / " + String.Format("{0:N0}", (!LevelManager.IntelligenceLevels.ContainsKey(Intelligence + 1) ? 100000 : LevelManager.IntelligenceLevels[Intelligence + 1])) + "\n" +
                                   "Fuerza: " + Strength + "/" + RoleplayManager.StrengthCap + " --- EXP: " + String.Format("{0:N0}", StrengthEXP) + " / " + String.Format("{0:N0}", (!LevelManager.StrengthLevels.ContainsKey(Strength + 1) ? 100000 : LevelManager.StrengthLevels[Strength + 1])) + "\n" +
                                   "Stamina: " + Stamina + "/" + RoleplayManager.StaminaCap + " --- EXP: " + String.Format("{0:N0}", StaminaEXP) + " / " + String.Format("{0:N0}", (!LevelManager.StaminaLevels.ContainsKey(Stamina + 1) ? 100000 : LevelManager.StaminaLevels[Stamina + 1])) + "\n\n" +

                                   "--- Encarcelado - Muerto - Se busca - Libertad condicional ---\n" +
                                   "Encarcelado: " + JailMessage + "\n" +
                                   "Muerto: " + DeadMessage + "\n" +
                                   "Buscado: " + WantedMessage + "\n" +
                                   "Libertad condicional: " + ProbationMessage + "\n\n" +

                                   "--- Afiliaciones ---\n" +
                                   "Casado(a): " + MarriedMesssage + "\n" +
                                   "Pandilla: " + (Gang == null ? "No Gang" : Gang.Name) + "\n" +
                                   grank +

                                   "--- Otras estadísticas ---\n" +
                                   "Punzones: " + String.Format("{0:N0}", Punches) + "\n" +
                                   "Asesinatos: " + String.Format("{0:N0}", Kills) + "\n" +
                                   "Muertes c/ Golpes: " + String.Format("{0:N0}", HitKills) + "\n" +
                                   "Muertes c/ Arma: " + String.Format("{0:N0}", GunKills) + "\n" +
                                   "Muertes: " + String.Format("{0:N0}", Deaths) + "\n" +
                                   "Policias Muertos: " + String.Format("{0:N0}", CopDeaths) + "\n" +
                                   "Arrestos: " + String.Format("{0:N0}", Arrests) + "\n" +
                                   "Detenido: " + String.Format("{0:N0}", Arrested) + "\n" +
                                   "Evasiones: " + String.Format("{0:N0}", Evasions) + "\n\n" +

                                   "--- Banca ---\n" +
                                   "Corriente: $" + String.Format("{0:N0}", BankChequings) + "\n" +
                                   "Ahorros: $" + String.Format("{0:N0}", BankSavings) + "\n\n" +

                                   "--- Inventario ---\n" +
                                   /*"Teléfono: " + PhoneType + "\n" +*/
                                   "Carro: " + CarType + "\n" +
                                   CarFuel +
                                   "Balas: " + String.Format("{0:N0}", Bullets) + "\n" +
                                   "Dinamitas: " + String.Format("{0:N0}", Dynamites) + "\n" +
                                   "Cigarros: " + String.Format("{0:N0}", Cigarettes) + "\n" +
                                   "Hierba: " + String.Format("{0:N0}", Weed) + " gramos\n" +
                                   "Heroina: " + String.Format("{0:N0}", Heroine) + " gramos\n\n" +
                                   "Cocaina: " + String.Format("{0:N0}", Cocaine) + " gramos\n\n" +
                                   "Medicinas: " + String.Format("{0:N0}", Medicina) + " gramos\n\n" +
                                   "Caramelos: " + String.Format("{0:N0}", Caramelos) + " unidades\n\n" +

                                   "--- Armas propias ---\n" +
                                   "Usa el comando :rpweapons para ver las armas de tu propiedad!\n");

            Session.SendMessage(new MOTDNotificationComposer(MessageToSend.ToString()));
        }
    }
}