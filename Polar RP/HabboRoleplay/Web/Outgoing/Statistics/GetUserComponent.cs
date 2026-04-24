using ConnectionManager;
using Polar.HabboHotel.Cache;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboRoleplay.Timers.Types;
using Polar.HabboRoleplay.Weapons;
using System.Collections.Concurrent;
using System.Data;

namespace Polar.HabboRoleplay.Web.Outgoing.Statistics
{
    /// <summary>
    /// GetUserComponent class.
    /// </summary>
    public class GetUserComponent
    {
        public static string ReturnUserPurse(GameClient User)
        {
            if (User == null)
                return null;

            int Credits = User.GetHabbo().Credits;
            int Diamonds = User.GetHabbo().Diamonds;
            int Duckets = User.GetHabbo().Duckets;

            string Statistics =
                Credits + "," +
                Duckets + "," +
                Diamonds 
            ;

            return Statistics;
        }
        /// <summary>
        /// Returns the user statistics.
        /// </summary>
        /// <param name="User"></param>
        /// <returns></returns>
        public static string ReturnUserStatistics(GameClient User)
        {
            if (User == null)
                return null;

            int UserID = User.GetHabbo().Id;
            string Figure = User.GetHabbo().Look;
            int CurHealth = User.GetRoleplay().CurHealth;
            int MaxHealth = User.GetRoleplay().MaxHealth;
            int MaxEnergy = User.GetRoleplay().MaxEnergy;
            int CurEnergy = User.GetRoleplay().CurEnergy;
            int CurXP = User.GetRoleplay().LevelEXP;
            int NeedXP = LevelManager.Levels.ContainsKey(User.GetRoleplay().Level + 1) ? LevelManager.Levels[User.GetRoleplay().Level + 1] : 100000;
            int Level = User.GetRoleplay().Level;
            string Username = User.GetHabbo().Username;
            int Hygiene = User.GetRoleplay().Hygiene;
            int Hunger = User.GetRoleplay().Hunger;
            int Caramelos = User.GetRoleplay().Caramelos;
            int Marihuana = User.GetRoleplay().Weed;
            int Cigarros = User.GetRoleplay().Cigarettes;
            int Cocaina = User.GetRoleplay().Cocaine;
            int Balas = User.GetRoleplay().Bullets;
            int Dinamita = User.GetRoleplay().Dynamite;
            int Pildoras = User.GetRoleplay().Pildoras;
            int Chalecos = User.GetRoleplay().Armor;
            int CurArmor = User.GetRoleplay().ChalecoPor;

            // Obtener las armas del usuario directamente desde el Roleplay
            ConcurrentDictionary<string, Weapon> weapons = User.GetRoleplay().OwnedWeapons;
            int weaponsCount = weapons != null ? weapons.Count : 0;

            // DEBUG: Ver qué armas hay realmente
            //Console.WriteLine($"DEBUG - Total armas: {weaponsCount}");
            if (weapons != null)
            {
                foreach (var weapon in weapons)
                {
                    //Console.WriteLine($"DEBUG - Arma: Key={weapon.Key}, Name={weapon.Value?.Name}");
                }
            }

            // Formatear las armas para el string
            string weaponsData = "";
            if (weaponsCount > 0 && weapons != null)
            {
                var weaponsList = new List<string>();
                foreach (Weapon weapon in weapons.Values)
                {
                    if (weapon != null && !string.IsNullOrEmpty(weapon.Name))
                    {
                        // Solo incluir el nombre del arma
                        weaponsList.Add($"{weapon.Name}:{weapon.TotalBullets}");
                        //Console.WriteLine($"DEBUG - Añadiendo arma: {weapon.Name}");
                    }
                }
                weaponsData = string.Join(";", weaponsList);
            }

            //Console.WriteLine($"DEBUG - Resultado final: weaponsCount={weaponsCount}, weaponsData={weaponsData}");

            var Statistics =
                UserID + "," +
                Figure + "," +
                CurHealth + "," +
                MaxHealth + "," +
                CurEnergy + "," +
                MaxEnergy + "," +
                CurXP + "," +
                NeedXP + "," +
                Level + "," +
                Username + "," +
                Hygiene + "," +
                Hunger + "," +
                Caramelos + "," +
                Marihuana + "," +
                Cigarros + "," +
                Cocaina + "," +
                Balas + "," +
                Dinamita + "," +
                Pildoras + "," +
                Chalecos + "," +
                CurArmor + "," +
                weaponsCount + "," +  // Cantidad de armas
                weaponsData            // Nombres de armas formateados
                ;

            //Console.WriteLine($"DEBUG - String completo (últimos campos): ...{weaponsCount},{weaponsData}");

            return Statistics;
        }

        /// <summary>
        /// Returns the user statistics via database.
        /// </summary>
        /// <param name="dRow"></param>
        /// <param name="dRowRP"></param>
        /// <returns></returns>
        public static string ReturnUserStatistics(DataRow dRow, DataRow dRowRP)
        {
            int UserID = Convert.ToInt32(dRowRP["id"]);
            string Figure = Convert.ToString(dRow["look"]);
            int CurHealth = Convert.ToInt32(dRowRP["curhealth"]);
            int MaxHealth = Convert.ToInt32(dRowRP["maxhealth"]);
            int MaxEnergy = Convert.ToInt32(dRowRP["maxenergy"]);
            int CurEnergy = Convert.ToInt32(dRowRP["curenergy"]);
            int CurXP = Convert.ToInt32(dRowRP["level_exp"]);
            int NeedXP = LevelManager.Levels.ContainsKey(Convert.ToInt32(dRowRP["level"]) + 1) ? LevelManager.Levels[Convert.ToInt32(dRowRP["level"]) + 1] : 100000;
            int Level = Convert.ToInt32(dRowRP["level"]);
            string Username = Convert.ToString(dRow["username"]);
            int Hygiene = Convert.ToInt32(dRowRP["hygiene"]);
            int Hunger = Convert.ToInt32(dRowRP["hunger"]);
            int Caramelos = Convert.ToInt32(dRowRP["caramelos"]);
            int Marihuana = Convert.ToInt32(dRowRP["weed"]);
            int Cigarros = Convert.ToInt32(dRowRP["cigarette"]);
            int Cocaina = Convert.ToInt32(dRowRP["cocaine"]);
            int Balas = Convert.ToInt32(dRowRP["bullets"]);
            int Dinamita = Convert.ToInt32(dRowRP["dynamite"]);
            int Pildoras = Convert.ToInt32(dRowRP["pildora"]);
            int Chalecos = Convert.ToInt32(dRowRP["kevlar"]);
            int CurArmor = 0;


            string Statistics =
                UserID + "," +
                Figure + "," +
                CurHealth + "," +
                MaxHealth + "," +
                CurEnergy + "," +
                MaxEnergy + "," +
                CurXP + "," +
                NeedXP + "," +
                Level + "," +
                Username + "," +
                Hygiene + "," +
                Hunger + "," +
                Caramelos + "," +
                Marihuana + "," +
                Cigarros + "," +
                Cocaina + "," +
                Balas + "," +
                Dinamita + "," +
                Pildoras + "," +
                Chalecos + "," +
                CurArmor
            ;

            return Statistics;
        }

        public static string ReturnUserWeapons(GameClient Session)
        {
            if (Session.GetRoleplay().OwnedWeapons.Count > 0)
            {

                string Message = "";
                    foreach (Weapon Weapon in Session.GetRoleplay().OwnedWeapons.Values)
                    {
                        Message += "name:" + Weapon.Name + ";bullets:" + Weapon.TotalBullets + ";";
                    }

                string Statistics = Message;
                return Statistics;
            }
            return null;
        }

        public static string ReturnWebUserStatistics(GameClient Session)
        {

            int UserID = Session.GetHabbo().Id;

            string Motto = Session.GetHabbo().Motto;
            string Figure = Session.GetHabbo().Look;
            int Level = Convert.ToInt32(Session.GetRoleplay().Level);
            DateTime AccountCreated = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(Convert.ToInt32(Session.GetHabbo().AccountCreated));
            string Username = Convert.ToString(Session.GetHabbo().Username);
            int GangId = Convert.ToInt32(Session.GetRoleplay().GangId);
            int GangRank = Convert.ToInt32(Session.GetRoleplay().GangRank);
            int JobId = Convert.ToInt32(Session.GetRoleplay().JobId);
            int JobRank = Convert.ToInt32(Session.GetRoleplay().JobRank);
            int Kills = Convert.ToInt32(Session.GetRoleplay().Kills);
            int HitKills = Convert.ToInt32(Session.GetRoleplay().HitKills);
            int Deaths = Convert.ToInt32(Session.GetRoleplay().Deaths);
            int Arrested = Convert.ToInt32(Session.GetRoleplay().Arrested);
            int Intelligence = Convert.ToInt32(Session.GetRoleplay().Intelligence);
            int Strength = Convert.ToInt32(Session.GetRoleplay().Strength);
            bool Online = (PolarEnvironment.EnumToBool(Convert.ToString(Session.GetHabbo().Online)) ||
                              PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Session.GetHabbo().Id) != null);
            DateTime LastOn = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(Convert.ToInt32(Session.GetHabbo().LastOnline));
            int IsGang = 0;
            string GangName = "nulo";
            string GangRankName = "nulo";
            string GangBadge = "nulo";
            string MarriedTo = "Nadie";
            UserCache Married = PolarEnvironment.GetGame().GetCacheManager().GenerateUser(Convert.ToInt32(Session.GetRoleplay().MarriedTo));
            HabboHotel.Groups.Group Job = GroupManager.GetJob(JobId);
            HabboHotel.Groups.GroupRank JobRank2 = GroupManager.GetJobRank(JobId, JobRank);
            HabboHotel.Groups.Group Gang = GroupManager.GetGang(GangId);
            HabboHotel.Groups.GroupRank GangRank2 = GroupManager.GetGangRank(GangId, GangRank);
            if (Gang != null)
            {
                IsGang = 1;
                GangName = Gang.Name;
                GangRankName = GangRank2.Name;
                GangBadge = Gang.Badge;
            }

            if (Married != null)
                MarriedTo = Married.Username;

            int Health = Convert.ToInt32(Session.GetRoleplay().CurHealth);
            int MaxHealth = Convert.ToInt32(Session.GetRoleplay().MaxHealth);
            int Hygiene = Convert.ToInt32(Session.GetRoleplay().Hygiene);
            int Hunger = Convert.ToInt32(Session.GetRoleplay().Hunger);

            string Statistics =
                UserID + "," +
                Username + "," +
                Figure + "," +
                Motto + "," +
                LastOn.ToString("dd/MM/yyyy hh:mm tt") + "," +
                AccountCreated.ToString("dd/MM/yyyy") + "," +
                Strength + "," +
                Intelligence + "," +
                Job.Name + "," +
                JobRank2.Name + "," +
                Job.Badge + "," +
                IsGang + "," +
                GangName + "," +
                GangRankName + "," +
                GangBadge + "," +
                Kills + "," +
                Arrested + "," +
                Deaths + "," +
                HitKills + "," +
                Online + "," +
                MarriedTo + "," +
                Health + "," +
                MaxHealth + "," +
                Hygiene + "," +
                Hunger;
            return Statistics;
        }

        public static string ReturnWebUserStatistics(DataRow dRow, DataRow dRowRP)
        {
            
            int UserID = Convert.ToInt32(dRow["id"]);
            
            string Motto = Convert.ToString(dRow["motto"]);
            string Figure = Convert.ToString(dRow["look"]);
            int Level = Convert.ToInt32(dRowRP["level"]);
            DateTime AccountCreated = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(Convert.ToInt32(dRow["account_created"]));
            string Username = Convert.ToString(dRow["username"]);
            int GangId = Convert.ToInt32(dRowRP["gang_id"]);
            int GangRank = Convert.ToInt32(dRowRP["gang_rank"]);
            int JobId = Convert.ToInt32(dRowRP["job_id"]);
            int JobRank = Convert.ToInt32(dRowRP["job_rank"]);
            int Kills = Convert.ToInt32(dRowRP["kills"]);
            int HitKills = Convert.ToInt32(dRowRP["hit_kills"]);
            int Deaths = Convert.ToInt32(dRowRP["deaths"]);
            int Arrested = Convert.ToInt32(dRowRP["arrested"]);
            int Intelligence = Convert.ToInt32(dRowRP["intelligence"]);
            int Strength = Convert.ToInt32(dRowRP["strength"]);
            string Online = dRow["online"].ToString();
            DateTime LastOn = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(Convert.ToInt32(dRow["last_online"]));
            int IsGang = 0;
            string GangName = "nulo";
            string GangRankName = "nulo";
            string GangBadge = "nulo";
            string MarriedTo = "Nadie";
            UserCache Married = PolarEnvironment.GetGame().GetCacheManager().GenerateUser(Convert.ToInt32(dRowRP["married_to"]));
            HabboHotel.Groups.Group Job = GroupManager.GetJob(JobId);
            HabboHotel.Groups.GroupRank JobRank2 = GroupManager.GetJobRank(JobId, JobRank);
            HabboHotel.Groups.Group Gang = GroupManager.GetGang(GangId);
            HabboHotel.Groups.GroupRank GangRank2 = GroupManager.GetGangRank(GangId, GangRank);
            if (Gang != null)
            {
                IsGang = 1;
                GangName = Gang.Name;
                GangRankName = GangRank2.Name;
                GangBadge = Gang.Badge;
            }

            if (Married != null)
                MarriedTo = Married.Username;

            int Health = Convert.ToInt32(dRowRP["curhealth"]);
            int MaxHealth = Convert.ToInt32(dRowRP["maxhealth"]);
            int Hygiene = Convert.ToInt32(dRowRP["hygiene"]);
            int Hunger = Convert.ToInt32(dRowRP["hunger"]);

            string Statistics =
                UserID + "," +
                Username + "," +
                Figure + "," +
                Motto + "," +
                LastOn.ToString("dd/MM/yyyy hh:mm tt") + "," +
                AccountCreated.ToString("dd/MM/yyyy") + "," +
                Strength + "," +
                Intelligence + "," +
                Job.Name + "," +
                JobRank2.Name + "," +
                Job.Badge + "," +
                IsGang + "," +
                GangName + "," +
                GangRankName + "," +
                GangBadge + "," +
                Kills + "," +
                Arrested + "," +
                Deaths + "," +
                HitKills + "," +
                PolarEnvironment.EnumToBool(Online) + "," +
                MarriedTo + "," +
                Health + "," +
                MaxHealth + "," +
                Hygiene + "," +
                Hunger;
            return Statistics;
        }

        /// <summary>
        /// Return user statistics via cache.
        /// </summary>
        /// <param name="CachedUser"></param>
        /// <returns></returns>
        public static string ReturnUserStatistics(UserCache CachedUser)
        {
            string[] SocketParts = CachedUser.SocketStatistics.Split(',');

            int UserID = Convert.ToInt32(SocketParts[0]);
           
            string Figure = Convert.ToString(SocketParts[1]);
            int CurHealth = Convert.ToInt32(SocketParts[2]);
            int MaxHealth = Convert.ToInt32(SocketParts[3]);
            int MaxEnergy = Convert.ToInt32(SocketParts[4]);
            int CurEnergy = Convert.ToInt32(SocketParts[5]);
            int CurXP = Convert.ToInt32(SocketParts[6]);
            int NeedXP = Convert.ToInt32(SocketParts[7]);
            int Level = Convert.ToInt32(SocketParts[8]);
            string Username = Convert.ToString(SocketParts[9]);
            int Hygiene = Convert.ToInt32(SocketParts[10]);
            int Hunger = Convert.ToInt32(SocketParts[11]);
            int Caramelos = Convert.ToInt32(SocketParts[12]);
            int Marihuana = Convert.ToInt32(SocketParts[13]);
            int Cigarros = Convert.ToInt32(SocketParts[14]);
            int Cocaina = Convert.ToInt32(SocketParts[15]);
            int Balas = Convert.ToInt32(SocketParts[16]);
            int Dinamita = Convert.ToInt32(SocketParts[17]);
            int Pildoras = Convert.ToInt32(SocketParts[18]);
            int Chalecos = Convert.ToInt32(SocketParts[19]);
            int CurArmor = Convert.ToInt32(SocketParts[20]);

            string Statistics =
                UserID + "," +
                Figure + "," +
                CurHealth + "," +
                MaxHealth + "," +
                CurEnergy + "," +
                MaxEnergy + "," + 
                CurXP + "," +
                NeedXP + "," + 
                Level + "," +
                Username + "," +
                Hygiene + "," +
                Hunger + "," +
                Caramelos + "," +
                Marihuana + "," +
                Cigarros + "," +
                Cocaina + "," +
                Balas + "," +
                Dinamita + "," +
                Pildoras + "," +
                Chalecos + "," +
                CurArmor
            ;

            return Statistics;
        }


            /// <summary>
            /// Clears the statistic dialogue.
            /// </summary>
            /// <param name="User"></param>
            public static void ClearStatisticsDialogue(GameClient User)
        {
            if (User.GetRoleplay().WebSocketConnection != null)
                PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(User, "compose_clear_characterbar|true");
        }
    }
}
