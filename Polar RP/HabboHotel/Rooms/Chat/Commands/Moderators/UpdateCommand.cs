using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using Polar.Communication.Packets.Outgoing.Catalog;
using Polar.Core;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Bots.Manager;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Wizards;
using Polar.HabboRoleplay.Weapons;
using Polar.HabboRoleplay.Food;
using Polar.HabboHotel.Items.Crafting;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Farming;
using Polar.HabboRoleplay.Gambling;
using Polar.HabboRoleplay.Comodin;
using Polar.HabboRoleplay.PhonesApps;
using Polar.HabboRoleplay.PlayInternet;
using Polar.HabboRoleplay.Vehicles;
using Polar.HabboRoleplay.VehicleOwned;
using Polar.HabboRoleplay.VehiclesJobs;
using Polar.HabboRoleplay.Skins;
using Polar.HabboRoleplay.Apartments;
using Polar.HabboRoleplay.ApartmentsOwned;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Moderators
{
    class UpdateCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_update"; }
        }

        public string Parameters
        {
            get { return "%variable%"; }
        }

        public string Description
        {
            get { return "refresca una función de la ciudad"; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("You must inculde a thing to update, e.g. :update catalog", 1);
                return;
            }

            string UpdateVariable = Params[1];
            switch (UpdateVariable.ToLower())
            {
                case "apartaments":
                    {
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_update_playphones"))
                        {
                            Session.SendWhisper("No tienes permiso para refrescar eso.", 1);
                            break;
                        }

                        ApartmentManager.Init();
                        PolarEnvironment.GetGame().GetApartmentOwnedManager().Init();
                        Session.SendWhisper("Phone Apps actualizadas con éxito.", 1);
                        break;
                    }
                case "phoneapps":
                    {
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_update_playphones"))
                        {
                            Session.SendWhisper("No tienes permiso para refrescar eso.", 1);
                            break;
                        }

                        PhoneAppManager.Initialize();
                        Session.SendWhisper("Phone Apps actualizadas con éxito.", 1);
                        break;
                    }
                case "internet":
                    {
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_update_playrooms"))
                        {
                            Session.SendWhisper("No tienes permiso para refrescar eso.", 1);
                            break;
                        }

                        PlayInternetManager.Init();
                        Session.SendWhisper("Páginas de internter actualizadas con éxito.", 1);
                        break;
                    }
                case "comodin":
                    {
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_update_comodin"))
                        {
                            Session.SendWhisper("No tienes permiso para refrescar eso.", 1);
                            break;
                        }

                        ComodinManager.Initialize();
                        Session.SendWhisper("Comodines actualizados con éxito.", 1);
                        break;
                    }
                case "cata":
                case "catalog":
                case "catalogue":
                    {
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_update_catalog"))
                        {
                            Session.SendWhisper("Oops, you do not have the 'command_update_catalog' permission.", 1);
                            break;
                        }

                        await PolarEnvironment.GetGame().GetCatalog().InitAsync(PolarEnvironment.GetGame().GetItemManager());
                        PolarEnvironment.GetGame().GetClientManager().SendMessage(new CatalogUpdatedComposer());
                        Session.SendWhisper("Catalogue successfully updated.", 1);
                        break;
                    }

                case "items":
                case "furni":
                case "furniture":
                    {
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_update_furni"))
                        {
                            Session.SendWhisper("Oops, you do not have the 'command_update_furni' permission.", 1);
                            break;
                        }

                        await PolarEnvironment.GetGame().GetItemManager().InitAsync();
                        Session.SendWhisper("Items successfully updated.", 1);
                        break;
                    }

                case "pinatas":
                case "pinata":
                    if (!Session.GetHabbo().GetPermissions().HasCommand("command_update_catalog"))
                    {
                        Session.SendWhisper("Oops, usted no tiene permiso para actualizar los premios de las piñatas.");
                        break;
                    }

                    PolarEnvironment.GetGame().GetPinataManager().Initialize(PolarEnvironment.GetDatabaseManager().GetQueryReactor());
                    Session.SendWhisper("Piñatas Actualizadas");
                    break;

                case "models":
                    {
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_update_models"))
                        {
                            Session.SendWhisper("Oops, you do not have the 'command_update_models' permission.", 1);
                            break;
                        }

                        PolarEnvironment.GetGame().GetRoomManager().LoadModels();
                        Session.SendWhisper("Room models successfully updated.", 1);
                        break;
                    }

                case "promotions":
                    {
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_update_promotions"))
                        {
                            Session.SendWhisper("Oops, you do not have the 'command_update_promotions' permission.", 1);
                            break;
                        }

                        PolarEnvironment.GetGame().GetLandingManager().LoadPromotions();
                        Session.SendWhisper("Landing view promotions successfully updated.", 1);
                        break;
                    }

                case "youtube":
                    {
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_update_youtube"))
                        {
                            Session.SendWhisper("Oops, you do not have the 'command_update_youtube' permission.", 1);
                            break;
                        }

                        PolarEnvironment.GetGame().GetTelevisionManager().Init();
                        Session.SendWhisper("Youtube televisions playlist successfully updated.", 1);
                        break;
                    }

                case "filter":
                    {
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_update_filter"))
                        {
                            Session.SendWhisper("Oops, you do not have the 'command_update_filter' permission.", 1);
                            break;
                        }

                        PolarEnvironment.GetGame().GetChatManager().GetFilter().InitWords();
                        PolarEnvironment.GetGame().GetChatManager().GetFilter().InitCharacters();
                        Session.SendWhisper("Filter definitions successfully updated.", 1);
                        break;
                    }

                case "navigator":
                case "rooms":
                    {
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_update_navigator"))
                        {
                            Session.SendWhisper("Oops, you do not have the 'command_update_navigator' permission.", 1);
                            break;
                        }

                        PolarEnvironment.GetGame().GetNavigator().Init();
                        Session.SendWhisper("Navigator items successfully updated.", 1);
                        break;
                    }

                case "ranks":
                case "rights":
                case "permissions":
                    {
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_update_permissions"))
                        {
                            Session.SendWhisper("Oops, you do not have the 'command_update_rights' permission.", 1);
                            break;
                        }

                        PolarEnvironment.GetGame().GetPermissionManager().Init();

                        foreach (GameClient Client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                        {
                            if (Client == null || Client.GetHabbo() == null || Client.GetHabbo().GetPermissions() == null)
                                continue;

                            Client.GetHabbo().GetPermissions().Init(Client.GetHabbo());
                        }

                        Session.SendWhisper("Rank definitions successfully updated.", 1);
                        break;
                    }

                case "crackable":
                case "ecotron":
                case "piñata":
                    PolarEnvironment.GetGame().GetPinataManager().Initialize(PolarEnvironment.GetDatabaseManager().GetQueryReactor());
                    //PolarEnvironment.GetGame().GetFurniMaticRewardsMnager().Initialize(PolarEnvironment.GetDatabaseManager().GetQueryReactor());
                    PolarEnvironment.GetGame().GetTargetedOffersManager().Initialize(PolarEnvironment.GetDatabaseManager().GetQueryReactor());
                    break;

                case "relampago":
                case "targeted":
                case "targetedoffers":
                    PolarEnvironment.GetGame().GetTargetedOffersManager().Initialize(PolarEnvironment.GetDatabaseManager().GetQueryReactor());
                    break;

                case "config":
                case "settings":
                    {
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_update_configuration"))
                        {
                            Session.SendWhisper("Oops, you do not have the 'command_update_configuration' permission.", 1);
                            break;
                        }
                        ExtraSettings.RunExtraSettings();
                        CatalogSettings.RunCatalogSettings();
                        PolarEnvironment.ConfigData = new ConfigData();
                        Session.SendWhisper("Server configuration successfully updated.", 1);
                        break;
                    }

                case "bans":
                    {
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_update_bans"))
                        {
                            Session.SendWhisper("Oops, you do not have the 'command_update_bans' permission.", 1);
                            break;
                        }

                        PolarEnvironment.GetGame().GetModerationManager().ReCacheBans();
                        Session.SendWhisper("Ban list has successfully updated.", 1);
                        break;
                    }

                case "quests":
                    {
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_update_quests"))
                        {
                            Session.SendWhisper("Oops, you do not have the 'command_update_quests' permission.", 1);
                            break;
                        }

                        PolarEnvironment.GetGame().GetQuestManager().Init();
                        Session.SendWhisper("Quest definitions successfully updated.", 1);
                        break;
                    }

                case "achievements":
                    {
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_update_achievements"))
                        {
                            Session.SendWhisper("Oops, you do not have the 'command_update_achievements' permission.", 1);
                            break;
                        }

                        PolarEnvironment.GetGame().GetAchievementManager().LoadAchievements();
                        Session.SendWhisper("Achievement definitions successfully updated.", 1);
                        break;
                    }

                case "clothing":
                    {
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_update_clothing"))
                        {
                            Session.SendWhisper("Oops, you do not have the 'command_update_clothing' permission.", 1);
                            break;
                        }

                        PolarEnvironment.GetGame().GetCatalog().GetClothingManager().Init();
                        Session.SendWhisper("Clothing furni and prices reloaded.", 1);
                        break;
                    }

                case "moderation":
                    {
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_update_moderation"))
                        {
                            Session.SendWhisper("Oops, you do not have the 'command_update_moderation' permission.", 1);
                            break;
                        }

                        PolarEnvironment.GetGame().GetModerationManager().Init();
                        PolarEnvironment.GetGame().GetClientManager().ModAlert("Moderation presets have been updated. Please reload the client to view the new presets.");

                        Session.SendWhisper("Moderation configuration successfully updated.", 1);
                        break;
                    }

                case "tickets":
                    {
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_update_tickets"))
                        {
                            Session.SendWhisper("Oops, you do not have the 'command_update_tickets' permission.", 1);
                            break;
                        }

                        if (PolarEnvironment.GetGame().GetModerationTool().Tickets.Count > 0)
                            PolarEnvironment.GetGame().GetModerationTool().Tickets.Clear();

                        PolarEnvironment.GetGame().GetClientManager().ModAlert("Tickets have been purged. Please reload the client.");
                        Session.SendWhisper("Tickets successfully purged.", 1);
                        break;
                    }

                case "vouchers":
                    {
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_update_vouchers"))
                        {
                            Session.SendWhisper("Oops, you do not have the 'command_update_vouchers' permission.", 1);
                            break;
                        }

                        PolarEnvironment.GetGame().GetCatalog().GetVoucherManager().Init();
                        Session.SendWhisper("Catalogue vouche cache successfully updated.", 1);
                        break;
                    }

                case "polls":
                    {
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_update_polls"))
                        {
                            Session.SendWhisper("Oops, you do not have the 'command_update_polls' permission.", 1);
                            break;
                        }

                        int PollLoaded;
                        PolarEnvironment.GetGame().GetPollManager().Init(out PollLoaded);
                        Session.SendWhisper("Polls successfully updated.", 1);
                        break;
                    }

                case "gamecenter":
                    {
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_update_game_center"))
                        {
                            Session.SendWhisper("Oops, you do not have the 'command_update_game_center' permission.", 1);
                            break;
                        }

                        PolarEnvironment.GetGame().GetGameDataManager().Init();
                        Session.SendWhisper("Game Center cache successfully updated.", 1);
                        break;
                    }

                case "pet_locale":
                    {
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_update_pet_locale"))
                        {
                            Session.SendWhisper("Oops, you do not have the 'command_update_pet_locale' permission.", 1);
                            break;
                        }

                        PolarEnvironment.GetGame().GetChatManager().GetPetLocale().Init();
                        Session.SendWhisper("Pet locale cache successfully updated.", 1);
                        break;
                    }

                case "locale":
                    {
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_update_locale"))
                        {
                            Session.SendWhisper("Oops, you do not have the 'command_update_locale' permission.", 1);
                            break;
                        }

                        PolarEnvironment.GetGame().GetLanguageLocale().Init();
                        Session.SendWhisper("Locale cache successfully updated.", 1);
                        break;
                    }

                case "mutant":
                    {
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_update_anti_mutant"))
                        {
                            Session.SendWhisper("Oops, you do not have the 'command_update_anti_mutant' permission.", 1);
                            break;
                        }

                        //PolarEnvironment.GetGame().GetAntiMutant().Init();
                        Session.SendWhisper("Anti mutant successfully reloaded.", 1);
                        break;
                    }
                case "botroom":
                case "botsroom":
                    {
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_update_bots"))
                        {
                            Session.SendWhisper("No tienes permiso para refrescar eso.", 1);
                            break;
                        }

                        int redeployed = 0;

                        foreach (RoomUser BotUser in Room.GetRoomUserManager().GetRoleplayBots())
                        {
                            if (BotUser == null || BotUser.GetBotRoleplay() == null) continue;

                            int BotId = BotUser.GetBotRoleplay().Id;

                            if (RoleplayBotManager.CachedRoleplayBots.ContainsKey(BotId))
                            {
                                RoleplayBotManager.EjectDeployedBot(BotUser, Room);
                                RoleplayBotManager.DeployBotByID(BotId, "default");
                                redeployed++;
                            }
                        }

                        Session.SendWhisper("Se recargaron " + redeployed + " bot(s) RP en esta sala.", 1);
                        break;
                    }
                case "bots":
                    {
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_update_bots"))
                        {
                            Session.SendWhisper("Oops, you do not have the 'command_update_bots' permission.", 1);
                            break;
                        }

                        PolarEnvironment.GetGame().GetBotManager().Init();
                        RoleplayBotManager.Initialize(true);
                        Session.SendWhisper("Bots manager successfully reloaded", 1);
                        break;
                    }

                case "bots_speech":
                case "bots speech":
                case "speech":
                case "speeches":
                case "response":
                case "responses":
                    {
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_update_bots"))
                        {
                            Session.SendWhisper("Oops, you do not have the 'command_update_bots' permission.", 1);
                            break;
                        }

                        RoleplayBotManager.FetchCachedSpeeches();
                        Session.SendWhisper("Bots speech and responses successfully reloaded", 1);
                        break;
                    }

                case "rewards":
                    {
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_update_rewards"))
                        {
                            Session.SendWhisper("Oops, you do not have the 'command_update_rewards' permission.", 1);
                            break;
                        }

                        PolarEnvironment.GetGame().GetRewardManager().Reload();
                        Session.SendWhisper("Rewards managaer successfully reloaded.", 1);
                        break;
                    }

                case "chat_styles":
                    {
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_update_chat_styles"))
                        {
                            Session.SendWhisper("Oops, you do not have the 'command_update_chat_styles' permission.", 1);
                            break;
                        }

                        PolarEnvironment.GetGame().GetChatManager().GetChatStyles().Init();
                        Session.SendWhisper("Chat Styles successfully reloaded.", 1);
                        break;
                    }

                case "badges":
                case "badge_definitions":
                    {
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_update_badge_definitions"))
                        {
                            Session.SendWhisper("Oops, you do not have the 'command_update_badge_definitions' permission.", 1);
                            break;
                        }

                        PolarEnvironment.GetGame().GetBadgeManager().Init();
                        Session.SendWhisper("Badge definitions successfully reloaded.", 1);
                        break;
                    }
                case "rprooms":
                    {
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_update_roleplay_data"))
                        {
                            Session.SendWhisper("No tienes permiso para refrescar eso.", 1);
                            break;
                        }

                        PolarEnvironment.GetGame().GetRPRoomManager().Init();
                        Session.SendWhisper("RP Rooms actualizados con éxito.", 1);
                        break;
                    }
                case "rpdata":
                case "roleplaydata":
                    {
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_update_roleplay_data"))
                        {
                            Session.SendWhisper("Oops, you do not have the 'command_update_roleplaydata' permission.", 1);
                            break;
                        }

                        RoleplayData.Initialize();
                        RoleplayManager.UpdateRPData();
                        Session.SendWhisper("Roleplay Data successfully reloaded.", 1);
                        break;
                    }

                case "blacklist":
                    {
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_update_blacklist"))
                        {
                            Session.SendWhisper("Oops, you do not have the 'command_update_blacklist' permission.", 1);
                            break;
                        }

                        BlackListManager.Initialize();
                        Session.SendWhisper("Blacklist successfully reloaded.", 1);
                        break;
                    }

                case "farming":
                    {
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_update_farming"))
                        {
                            Session.SendWhisper("Oops, you do not have the 'command_update_farming' permission.", 1);
                            break;
                        }

                        FarmingManager.Initialize();
                        Session.SendWhisper("Farming items successfully reloaded.", 1);
                        break;
                    }

                case "corps":
                case "jobs":
                case "corporations":
                case "gangs":
                    {
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_update_jobs"))
                        {
                            Session.SendWhisper("Oops, you do not have the 'command_update_jobs' permission.", 1);
                            break;
                        }

                        PolarEnvironment.GetGame().GetGroupManager().Initialize();
                        Session.SendWhisper("Jobs and Gangs successfully reloaded.", 1);
                        break;
                    }

                case "turfs":
                case "turfcaptures":
                case "gangcaptures":
                    {
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_update_turfs"))
                        {
                            Session.SendWhisper("Oops, you do not have the 'command_update_turfs' permission.", 1);
                            break;
                        }

                        PolarEnvironment.GetGame().GetGangTurfsManager().Initialize();
                        Session.SendWhisper("Turfs and Capture Zones successfully reloaded.", 1);
                        break;
                    }


                case "vehicles":
                case "cars":
                case "vehiculo":
                    {
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_update_vehicle"))
                        {
                            Session.SendWhisper("Oops, you do not have the 'command_update_vehicle' permission.", 1);
                            break;
                        }

                        VehicleManager.Initialize();
                        VehicleJobsManager.Initialize();
                        PolarEnvironment.GetGame().GetVehiclesOwnedManager().Init();
                        Session.SendWhisper("Vehicles successfully reloaded.", 1);
                        break;
                    }
                case "wskins":
                case "skins":
                    {
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_update_weapons"))
                        {
                            Session.SendWhisper("Oops, you do not have the 'command_update_weapons' permission.", 1);
                            break;
                        }

                        WSkinManager.Initialize();


                        Session.SendWhisper("Skins de armas recargados correctamente.", 1);
                        break;
                    }
                case "wizards":
                case "hechizos":
                    {
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_update_weapons"))
                        {
                            Session.SendWhisper("Oops, you do not have the 'command_update_weapons' permission.", 1);
                            break;
                        }

                        HechizosManager.Initialize();


                        Session.SendWhisper("Hechizos recargados correctamente.", 1);
                        break;
                    }


                case "weapons":
                case "guns":
                    {
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_update_weapons"))
                        {
                            Session.SendWhisper("Oops, you do not have the 'command_update_weapons' permission.", 1);
                            break;
                        }

                        WeaponManager.Initialize();

                        #region Refresh User Weapons

                        lock (PolarEnvironment.GetGame().GetClientManager().GetClients)
                        {
                            foreach (GameClient Client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                            {
                                if (Client == null || Client.GetHabbo() == null || Client.GetRoleplay() == null)
                                    continue;

                                if (Client.GetRoleplay().EquippedWeapon == null)
                                    continue;

                                Client.GetRoleplay().EquippedWeapon = null;

                                Client.GetRoleplay().OwnedWeapons = null;
                                Client.GetRoleplay().OwnedWeapons = Client.GetRoleplay().LoadAndReturnWeapons();

                                Client.SendWhisper("Un administrador ha actualizado el caché de armas, por lo que tu arma no estaba equipada para que se aplicaran los cambios.", 1);
                            }
                        }

                        #endregion

                        Session.SendWhisper("Weapons successfully reloaded.", 1);
                        break;
                    }

                case "food":
                case "drinks":
                    {
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_update_food"))
                        {
                            Session.SendWhisper("Oops, you do not have the 'command_update_food' permission.", 1);
                            break;
                        }

                        FoodManager.Initialize();
                        Session.SendWhisper("Food and Drinks successfully reloaded.", 1);
                        break;
                    }

                case "houses":
                case "house":
                    {
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_update_houses"))
                        {
                            Session.SendWhisper("Oops, you do not have the 'command_update_houses' permission.", 1);
                            break;
                        }

                        PolarEnvironment.GetGame().GetHouseManager().Init();
                        Session.SendWhisper("Houses successfully reloaded.", 1);
                        break;
                    }

                case "crafting":
                    {
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_update_crafting"))
                        {
                            Session.SendWhisper("Oops, you do not have the 'command_update_crafting' permission.", 1);
                            break;
                        }

                        CraftingManager.Initialize();
                        Session.SendWhisper("Crafting Recipes successfully reloaded.", 1);
                        break;
                    }

                case "lottery":
                    {
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_update_lottery"))
                        {
                            Session.SendWhisper("Oops, you do not have the 'command_update_lottery' permission.", 1);
                            break;
                        }

                        LotteryManager.Initialize();
                        Session.SendWhisper("Lottery successfully updated.", 1);
                        break;
                    }

                case "todo":
                    {
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_update_todo"))
                        {
                            Session.SendWhisper("Oops, you do not have the 'command_update_todo' permission.", 1);
                            break;
                        }

                        ToDoManager.Initialize();
                        Session.SendWhisper("ToDo List successfully updated.", 1);
                        break;
                    }

                case "bounty":
                case "bl":
                case "bounties":
                    {
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_update_bounty"))
                        {
                            Session.SendWhisper("Oops, you do not have the 'command_update_bounty' permission.", 1);
                            break;
                        }

                        BountyManager.Initialize();
                        Session.SendWhisper("Bounty List successfully updated.", 1);
                        break;
                    }

                case "court":
                case "jury":
                    {
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_update_court"))
                        {
                            Session.SendWhisper("Oops, you do not have the 'command_update_court' permission.", 1);
                            break;
                        }

                        RoleplayManager.CourtVoteEnabled = false;
                        RoleplayManager.InnocentVotes = 0;
                        RoleplayManager.GuiltyVotes = 0;

                        RoleplayManager.CourtJuryTime = 0;
                        RoleplayManager.CourtTrialIsStarting = false;
                        RoleplayManager.CourtTrialStarted = false;
                        RoleplayManager.Defendant = null;
                        RoleplayManager.InvitedUsersToJuryDuty.Clear();

                        Session.SendWhisper("Court Misc successfully refreshed.", 1);
                        break;
                    }

                case "chat":
                case "chats":
                case "chatroom":
                case "chatrooms":
                    {
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_update_websocket_chat"))
                        {
                            Session.SendWhisper("Oops, you do not have the 'command_update_websocket_chat' permission.", 1);
                            break;
                        }

                        HabboRoleplay.Web.Util.ChatRoom.WebSocketChatManager.Initialiaze();
                        Session.SendWhisper("Chat rooms successfully updated.", 1);
                        break;
                    }

                case "gambling":
                case "texasholdem":
                    {
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_update_gambling"))
                        {
                            Session.SendWhisper("Oops, you do not have the 'command_update_gambling' permission.", 1);
                            break;
                        }

                        TexasHoldEmManager.Initialize();
                        Session.SendWhisper("Texas Hold 'Em Games successfully updated.", 1);
                        break;
                    }

                default:
                    Session.SendWhisper("'" + UpdateVariable + "' is not a valid thing to reload.", 1);
                    break;
            }
        }
    }
}
