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
    class FarmingStatsCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_general_farming_stats"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Muestra tus estadisticas de agricultura."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            StringBuilder MessageToSend = new StringBuilder().Append(
                                   "-------- Tus estadisticas de agricultura --------\n\n" +

                                   "--- Información basica ---\n" +
                                   "Level: " + Session.GetRoleplay().FarmingStats.Level + "/" + RoleplayManager.FarmingLevelCap + "\n" +
                                   "Level EXP: " + Session.GetRoleplay().FarmingStats.Exp + "/" + (!FarmingManager.levels.ContainsKey(Session.GetRoleplay().FarmingStats.Level + 1) ? 100000 : FarmingManager.levels[Session.GetRoleplay().FarmingStats.Level + 1]) + "\n\n" +

                                   "--- Bolso de semillas ---\n" +
                                   (Session.GetRoleplay().FarmingStats.HasSeedSatchel ? ("[1] Yellow Plumeria: " + Session.GetRoleplay().FarmingStats.SeedSatchel.YellowPlumeriaSeeds + "\n" +
                                   "[2] Blue Plumeria: " + Session.GetRoleplay().FarmingStats.SeedSatchel.BluePlumeriaSeeds + "\n" +
                                   "[3] Pink Plumeria: " + Session.GetRoleplay().FarmingStats.SeedSatchel.PinkPlumeriaSeeds + "\n" +
                                   "[4] Yellow Primrose: " + Session.GetRoleplay().FarmingStats.SeedSatchel.YellowPrimroseSeeds + "\n" +
                                   "[5] Blue Primrose " + Session.GetRoleplay().FarmingStats.SeedSatchel.BluePrimroseSeeds + "\n" +
                                   "[6] Pink Primrose: " + Session.GetRoleplay().FarmingStats.SeedSatchel.PinkPrimroseSeeds + "\n" +
                                   "[7] Yellow Dahlia: " + Session.GetRoleplay().FarmingStats.SeedSatchel.YellowDahliaSeeds + "\n" +
                                   "[8] Blue Dahlia: " + Session.GetRoleplay().FarmingStats.SeedSatchel.BlueDahliaSeeds + "\n" +
                                   "[9] Pink Dahlia: " + Session.GetRoleplay().FarmingStats.SeedSatchel.PinkDahliaSeeds + "\n" +
                                   "[10] Yellow Starflower: " + Session.GetRoleplay().FarmingStats.SeedSatchel.YellowStarflowerSeeds + "\n" +
                                   "[11] Blue Starflower: " + Session.GetRoleplay().FarmingStats.SeedSatchel.BlueStarflowerSeeds + "\n" +
                                   "[12] Red Starflower: " + Session.GetRoleplay().FarmingStats.SeedSatchel.RedStarflowerSeeds + "\n\n") : "Usted no tiene bolso de semillas\n\n") +

                                   "--- Bolsa de plantas ---\n" +
                                   (Session.GetRoleplay().FarmingStats.HasPlantSatchel ? ("[1] Yellow Plumeria: " + Session.GetRoleplay().FarmingStats.PlantSatchel.YellowPlumerias + "\n" +
                                   "[2] Blue Plumeria: " + Session.GetRoleplay().FarmingStats.PlantSatchel.BluePlumerias + "\n" +
                                   "[3] Pink Plumeria: " + Session.GetRoleplay().FarmingStats.PlantSatchel.PinkPlumerias + "\n" +
                                   "[4] Yellow Primrose: " + Session.GetRoleplay().FarmingStats.PlantSatchel.YellowPrimroses + "\n" +
                                   "[5] Blue Primrose " + Session.GetRoleplay().FarmingStats.PlantSatchel.BluePrimroses + "\n" +
                                   "[6] Pink Primrose: " + Session.GetRoleplay().FarmingStats.PlantSatchel.PinkPrimroses + "\n" +
                                   "[7] Yellow Dahlia: " + Session.GetRoleplay().FarmingStats.PlantSatchel.YellowDahlias + "\n" +
                                   "[8] Blue Dahlia: " + Session.GetRoleplay().FarmingStats.PlantSatchel.BlueDahlias + "\n" +
                                   "[9] Pink Dahlia: " + Session.GetRoleplay().FarmingStats.PlantSatchel.PinkDahlias + "\n" +
                                   "[10] Yellow Starflower: " + Session.GetRoleplay().FarmingStats.PlantSatchel.YellowStarflowers + "\n" +
                                   "[11] Blue Starflower: " + Session.GetRoleplay().FarmingStats.PlantSatchel.BlueStarflowers + "\n" +
                                   "[12] Red Starflower: " + Session.GetRoleplay().FarmingStats.PlantSatchel.RedStarflowers + "\n") : "Usted no es dueño de un bolso de plantas"));

            Session.SendMessage(new MOTDNotificationComposer(MessageToSend.ToString()));
        }
    }
}