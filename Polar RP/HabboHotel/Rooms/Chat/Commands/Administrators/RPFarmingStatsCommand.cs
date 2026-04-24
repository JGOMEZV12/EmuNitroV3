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
using Polar.HabboRoleplay.Farming;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.General
{
    class RPFarmingStatsCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_rpfarmingstats"; }
        }

        public string Parameters
        {
            get { return "%user%"; }
        }

        public string Description
        {
            get { return "Provides you a list of the targets farming statistics."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("You forgot to enter a username of a person you want to check!", 1);
                return;
            }

            #region Variables
            int Level;
            int Exp;

            bool HasSeedSatchel;
            bool HasPlantSatchel;

            int BlueStarflowerSeeds;
            int YellowStarflowerSeeds;
            int PinkDahliaSeeds;
            int YellowPlumeriaSeeds;
            int PinkPrimroseSeeds;
            int BluePrimroseSeeds;
            int YellowPrimroseSeeds;
            int YellowDahliaSeeds;
            int BluePlumeriaSeeds;
            int PinkPlumeriaSeeds;
            int RedStarflowerSeeds;
            int BlueDahliaSeeds;

            int BlueStarflowers;
            int YellowStarflowers;
            int PinkDahlias;
            int YellowPlumerias;
            int PinkPrimroses;
            int BluePrimroses;
            int YellowPrimroses;
            int YellowDahlias;
            int BluePlumerias;
            int PinkPlumerias;
            int RedStarflowers;
            int BlueDahlias;

            string Username = Params[1];
            GameClients.GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Username);
            #endregion

            #region Variables Client Check & Set
            if (TargetClient == null)
            {
                using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("SELECT `id` FROM `users` where `username` = '" + Username + "' LIMIT 1");
                    var Row = dbClient.getRow();

                    if (Row == null)
                    {
                        Session.SendWhisper("Sorry! This person does not exist!", 1);
                        return;
                    }

                    int UserId = Convert.ToInt32(Row["id"]);

                    dbClient.SetQuery("SELECT * FROM `rp_stats_farming` where `id` = '" + UserId + "' LIMIT 1");
                    var Stats = dbClient.getRow();

                    if (Stats == null)
                    {
                        Session.SendWhisper("Sorry! This person does not exist!", 1);
                        return;
                    }

                    Level = Convert.ToInt32(Stats["level"]);
                    Exp = Convert.ToInt32(Stats["level_exp"]);

                    HasSeedSatchel = PolarEnvironment.EnumToBool(Row["has_seed_satchel"].ToString());
                    HasPlantSatchel = PolarEnvironment.EnumToBool(Row["has_plant_satchel"].ToString());

                    BlueStarflowerSeeds = Convert.ToInt32(Row["blue_starflower"].ToString().Split(':')[0]);
                    YellowStarflowerSeeds = Convert.ToInt32(Row["yellow_starflower"].ToString().Split(':')[0]);
                    PinkDahliaSeeds = Convert.ToInt32(Row["pink_dahlia"].ToString().Split(':')[0]);
                    YellowPlumeriaSeeds = Convert.ToInt32(Row["yellow_plumeria"].ToString().Split(':')[0]);
                    PinkPrimroseSeeds = Convert.ToInt32(Row["pink_primrose"].ToString().Split(':')[0]);
                    BluePrimroseSeeds = Convert.ToInt32(Row["blue_primrose"].ToString().Split(':')[0]);
                    YellowPrimroseSeeds = Convert.ToInt32(Row["yellow_primrose"].ToString().Split(':')[0]);
                    YellowDahliaSeeds = Convert.ToInt32(Row["yellow_dahlia"].ToString().Split(':')[0]);
                    BluePlumeriaSeeds = Convert.ToInt32(Row["blue_plumeria"].ToString().Split(':')[0]);
                    PinkPlumeriaSeeds = Convert.ToInt32(Row["pink_plumeria"].ToString().Split(':')[0]);
                    RedStarflowerSeeds = Convert.ToInt32(Row["red_starflower"].ToString().Split(':')[0]);
                    BlueDahliaSeeds = Convert.ToInt32(Row["blue_dahlia"].ToString().Split(':')[0]);

                    BlueStarflowers = Convert.ToInt32(Row["blue_starflower"].ToString().Split(':')[1]);
                    YellowStarflowers = Convert.ToInt32(Row["yellow_starflower"].ToString().Split(':')[1]);
                    PinkDahlias = Convert.ToInt32(Row["pink_dahlia"].ToString().Split(':')[1]);
                    YellowPlumerias = Convert.ToInt32(Row["yellow_plumeria"].ToString().Split(':')[1]);
                    PinkPrimroses = Convert.ToInt32(Row["pink_primrose"].ToString().Split(':')[1]);
                    BluePrimroses = Convert.ToInt32(Row["blue_primrose"].ToString().Split(':')[1]);
                    YellowPrimroses = Convert.ToInt32(Row["yellow_primrose"].ToString().Split(':')[1]);
                    YellowDahlias = Convert.ToInt32(Row["yellow_dahlia"].ToString().Split(':')[1]);
                    BluePlumerias = Convert.ToInt32(Row["blue_plumeria"].ToString().Split(':')[1]);
                    PinkPlumerias = Convert.ToInt32(Row["pink_plumeria"].ToString().Split(':')[1]);
                    RedStarflowers = Convert.ToInt32(Row["red_starflower"].ToString().Split(':')[1]);
                    BlueDahlias = Convert.ToInt32(Row["blue_dahlia"].ToString().Split(':')[1]);
                }
            }
            else
            {
                Level = TargetClient.GetRoleplay().FarmingStats.Level;
                Exp = TargetClient.GetRoleplay().FarmingStats.Exp;

                HasSeedSatchel = TargetClient.GetRoleplay().FarmingStats.HasSeedSatchel;
                HasPlantSatchel = TargetClient.GetRoleplay().FarmingStats.HasPlantSatchel;

                BlueStarflowerSeeds = TargetClient.GetRoleplay().FarmingStats.SeedSatchel.BlueStarflowerSeeds;
                YellowStarflowerSeeds = TargetClient.GetRoleplay().FarmingStats.SeedSatchel.YellowStarflowerSeeds;
                PinkDahliaSeeds = TargetClient.GetRoleplay().FarmingStats.SeedSatchel.PinkDahliaSeeds;
                YellowPlumeriaSeeds = TargetClient.GetRoleplay().FarmingStats.SeedSatchel.YellowPlumeriaSeeds;
                PinkPrimroseSeeds = TargetClient.GetRoleplay().FarmingStats.SeedSatchel.PinkPrimroseSeeds;
                BluePrimroseSeeds = TargetClient.GetRoleplay().FarmingStats.SeedSatchel.BluePrimroseSeeds;
                YellowPrimroseSeeds = TargetClient.GetRoleplay().FarmingStats.SeedSatchel.YellowPrimroseSeeds;
                YellowDahliaSeeds = TargetClient.GetRoleplay().FarmingStats.SeedSatchel.YellowDahliaSeeds;
                BluePlumeriaSeeds = TargetClient.GetRoleplay().FarmingStats.SeedSatchel.BluePlumeriaSeeds;
                PinkPlumeriaSeeds = TargetClient.GetRoleplay().FarmingStats.SeedSatchel.PinkPlumeriaSeeds;
                RedStarflowerSeeds = TargetClient.GetRoleplay().FarmingStats.SeedSatchel.RedStarflowerSeeds;
                BlueDahliaSeeds = TargetClient.GetRoleplay().FarmingStats.SeedSatchel.BlueDahliaSeeds;

                BlueStarflowers = TargetClient.GetRoleplay().FarmingStats.PlantSatchel.BlueStarflowers;
                YellowStarflowers = TargetClient.GetRoleplay().FarmingStats.PlantSatchel.YellowStarflowers;
                PinkDahlias = TargetClient.GetRoleplay().FarmingStats.PlantSatchel.PinkDahlias;
                YellowPlumerias = TargetClient.GetRoleplay().FarmingStats.PlantSatchel.YellowPlumerias;
                PinkPrimroses = TargetClient.GetRoleplay().FarmingStats.PlantSatchel.PinkPrimroses;
                BluePrimroses = TargetClient.GetRoleplay().FarmingStats.PlantSatchel.BluePrimroses;
                YellowPrimroses = TargetClient.GetRoleplay().FarmingStats.PlantSatchel.YellowPrimroses;
                YellowDahlias = TargetClient.GetRoleplay().FarmingStats.PlantSatchel.YellowDahlias;
                BluePlumerias = TargetClient.GetRoleplay().FarmingStats.PlantSatchel.BluePlumerias;
                PinkPlumerias = TargetClient.GetRoleplay().FarmingStats.PlantSatchel.PinkPlumerias;
                RedStarflowers = TargetClient.GetRoleplay().FarmingStats.PlantSatchel.RedStarflowers;
                BlueDahlias = TargetClient.GetRoleplay().FarmingStats.PlantSatchel.BlueDahlias;
            }
            #endregion

            StringBuilder MessageToSend = new StringBuilder().Append(
                                   "-------- " + Username + "'s tus estadisticas en agricultura --------\n\n" +

                                   "--- Basic Info ---\n" +
                                   "Level: " + Level + "/" + RoleplayManager.FarmingLevelCap + "\n" +
                                   "Level EXP: " + Exp + "/" + (!FarmingManager.levels.ContainsKey(Level + 1) ? 100000 : FarmingManager.levels[Level + 1]) + "\n\n" +

                                   "--- Seed Satchel ---\n" +
                                   (HasSeedSatchel ? ("Blue Starflower: " + BlueStarflowerSeeds + "\n" +
                                   "Yellow Starflower: " + YellowStarflowerSeeds + "\n" +
                                   "Red Starflower: " + RedStarflowerSeeds + "\n" +
                                   "Blue Dahlia: " + BlueDahliaSeeds + "\n" +
                                   "Yellow Dahlia: " + YellowDahliaSeeds + "\n" +
                                   "Pink Dahlia: " + PinkDahliaSeeds + "\n" +
                                   "Blue Primrose " + BluePrimroseSeeds + "\n" +
                                   "Yellow Primrose: " + YellowPrimroseSeeds + "\n" +
                                   "Pink Primrose: " + PinkPrimroseSeeds + "\n" +
                                   "Blue Plumeria: " + BluePlumeriaSeeds + "\n" +
                                   "Yellow Plumeria: " + YellowPlumeriaSeeds + "\n" +
                                   "Pink Plumeria: " + PinkPlumeriaSeeds + "\n\n") : "This user does not own a Seed Satchel!\n\n") +

                                   "--- Plant Satchel ---\n" +
                                   (HasPlantSatchel ? ("Blue Starflower: " + BlueStarflowers + "\n" +
                                   "Yellow Starflower: " + YellowStarflowers + "\n" +
                                   "Red Starflower: " + RedStarflowers + "\n" +
                                   "Blue Dahlia: " + BlueDahlias + "\n" +
                                   "Yellow Dahlia: " + YellowDahlias + "\n" +
                                   "Pink Dahlia: " + PinkDahlias + "\n" +
                                   "Blue Primrose " + BluePrimroses + "\n" +
                                   "Yellow Primrose: " + YellowPrimroses + "\n" +
                                   "Pink Primrose: " + PinkPrimroses + "\n" +
                                   "Blue Plumeria: " + BluePlumerias + "\n" +
                                   "Yellow Plumeria: " + YellowPlumerias + "\n" +
                                   "Pink Plumeria: " + PinkPlumerias + "\n\n") : "This user does not own a Plant Satchel!\n\n"));

            Session.SendMessage(new MOTDNotificationComposer(MessageToSend.ToString()));
        }
    }
}