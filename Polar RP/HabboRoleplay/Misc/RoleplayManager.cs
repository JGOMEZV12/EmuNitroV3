using System;
using System.Data;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Rooms;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using Polar.Communication.Packets.Outgoing.Notifications;
using Polar.Communication.Packets.Outgoing.Navigator;
using Polar.HabboRoleplay.Weapons;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Pathfinding;
using Polar.HabboRoleplay.Wizards;
using Polar.HabboRoleplay.Timers;
using Polar.HabboHotel.Quests;
using Polar.HabboHotel.Guides;
using Polar.Core;
using Polar.HabboRoleplay.Farming;
using Polar.HabboRoleplay.Gambling;
using System.Text;
using Polar.HabboRoleplay.PhoneAppOwned;
using Polar.HabboRoleplay.PhonesApps;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboRoleplay.Phones;
using Polar.HabboRoleplay.VehicleOwned;
using Polar.HabboRoleplay.VehiclesJobs;
using Polar.HabboRoleplay.Vehicles;
using Polar.Communication.Packets.Outgoing.Rooms.Session;
using Polar.Communication.Packets.Outgoing.Guides;
using Polar.HabboRoleplay.Products;
using log4net;
using Polar.HabboRoleplay.ProductOwned;
using Polar.Communication.Packets.Outgoing.Groups;
using Polar.HabboHotel.Items.Wired;
using Polar.HabboHotel.Users.Effects;
using System.Threading.Tasks;
using Polar.Communication.Packets.Outgoing.Rooms.Permissions;
using Polar.Communication.Packets.Outgoing.Rooms.Settings;
using Polar.HabboHotel.Cache;
using Polar.HabboHotel.Items.Data.RentableSpace;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;

namespace Polar.HabboRoleplay.Misc
{
    public class RoleplayManager
    {
        /// <summary>
        /// Statistic Caps
        /// </summary>
        /// 
        public static int VaultRobbery = 130000;
        public static ConcurrentDictionary<int, DateTime> VaultCooldowns = new ConcurrentDictionary<int, DateTime>();
        public static int VehiclesOwnedID = 10000000;// Para Autos CORP
        public static int ChatsID = 0;
        private static readonly object itemobj = new object();
        public static string APIUrl = "http://" + Convert.ToString(RoleplayData.GetData("server", "apiurl"));
        public static int LevelCap = Convert.ToInt32(RoleplayData.GetData("level", "cap"));
        public static int FarmingLevelCap = Convert.ToInt32(RoleplayData.GetData("farming", "cap"));
        public static int IntelligenceCap = Convert.ToInt32(RoleplayData.GetData("intelligence", "cap"));
        public static int StrengthCap = Convert.ToInt32(RoleplayData.GetData("strength", "cap"));
        public static int StaminaCap = Convert.ToInt32(RoleplayData.GetData("stamina", "cap"));
        public static int DyingTime = Convert.ToInt32(RoleplayData.GetData("timer", "dyingtime"));// min
        public static int DeathTime = Convert.ToInt32(RoleplayData.GetData("hospital", "deathtime"));
        public static int HungerTime = Convert.ToInt32(RoleplayData.GetData("timer", "hungertime"));// segs
        public static int HygieneTime = Convert.ToInt32(RoleplayData.GetData("timer", "hygienetime"));// segs
        public static int StunGunRange = Convert.ToInt32(RoleplayData.GetData("police", "stungunrange"));
        public static int PSVTime = Convert.ToInt32(RoleplayData.GetData("timer", "psvtime"));// segs
        public static bool LevelDifference = Convert.ToBoolean(RoleplayData.GetData("level", "leveldifference"));
        public static int DefaultJailTime = Convert.ToInt32(RoleplayData.GetData("timer", "defaultjailtime"));// min
        public static int StarsJailTime = Convert.ToInt32(RoleplayData.GetData("timer", "starsjailtime"));// min
        public static bool AccurateUserCount = Convert.ToBoolean(RoleplayData.GetData("server", "accurateusercount"));
        public static bool StartWorkInPoliceHQ = Convert.ToBoolean(RoleplayData.GetData("police", "startworkinhq"));
        public static bool DayNightSystem = Convert.ToBoolean(RoleplayData.GetData("server", "daynightsystem"));
        public static bool DayNightTaxiTime = Convert.ToBoolean(RoleplayData.GetData("server", "daynighttaxi"));
        // Hospital
        public static int PayRightJob = Convert.ToInt32(RoleplayData.GetData("hospital", "payrightjob"));
        public static int PayWrongJob = Convert.ToInt32(RoleplayData.GetData("hospital", "paywrongjob"));
        public static int AmbulSave = Convert.ToInt32(RoleplayData.GetData("hospital", "ambulsave"));
        // Server
        public static bool ConfiscateWeapons = Convert.ToBoolean(RoleplayData.GetData("server", "confiscateweapons"));
        public static bool NewVIPAlert = Convert.ToBoolean(RoleplayData.GetData("server", "newvipalerts"));
        public static int VehicleJobTime = Convert.ToInt32(5000);// segs
        public static int VehicleJobPoliTime = Convert.ToInt32(10000);
        public static bool JobCAPTCHABox = Convert.ToBoolean(RoleplayData.GetData("captcha", "job"));
        public static bool WorkoutCAPTCHABox = Convert.ToBoolean(RoleplayData.GetData("captcha", "workout"));
        public static bool FarmingCAPTCHABox = Convert.ToBoolean(RoleplayData.GetData("captcha", "farming"));

        public static int DefaultHitCooldown = Convert.ToInt32(RoleplayData.GetData("combat", "defaulthitcooldown"));
        public static int HitCooldown = Convert.ToInt32(RoleplayData.GetData("combat", "hitcooldown"));
        public static int HitCooldownInEvent = Convert.ToInt32(RoleplayData.GetData("combat", "hitcooldowninevent"));

        public static bool UnloadRoomsAutomatically = Convert.ToBoolean(RoleplayData.GetData("server", "autounloadrooms"));
        public static bool FollowFriends = Convert.ToBoolean(RoleplayData.GetData("server", "followfriends"));
        public static bool PushPullOnArrows = Convert.ToBoolean(RoleplayData.GetData("server", "allowpponarrows"));

        public static int NukeMinutes = Convert.ToInt32(RoleplayData.GetData("npa", "nukeminutes"));
        public static int BreakDownMinutes = Convert.ToInt32(RoleplayData.GetData("npa", "breakdownminutes"));
        public static int NPACoolDown = Convert.ToInt32(RoleplayData.GetData("npa", "cooldown"));

        public static string AVATARIMG = Convert.ToString(RoleplayData.GetData("ws", "avatarimg"));
        public static string HotelUrl = Convert.ToString(RoleplayData.GetData("ws", "hotelurl"));
        // WS
        public static string CDNSWF = "https://" + Convert.ToString(RoleplayData.GetData("ws", "cdnswf"));
        public static string SWFPath = @"C:" + Convert.ToString(RoleplayData.GetData("ws", "swfpath"));
        public static string CMSPath = @"C:" + Convert.ToString(RoleplayData.GetData("ws", "cmspath"));
        // Para imagenes que manda en HTML con WS
        public static string CdnURL = Convert.ToString(RoleplayData.GetData("ws", "cdnurl"));
        public static string CdnURL2 = Convert.ToString(RoleplayData.GetData("ws", "cdnurl2"));
        public static string PhoneAppsPath = @"C:" + Convert.ToString(RoleplayData.GetData("ws", "phoneappspath"));
        public static string PhoneAppsUrl = Convert.ToString(RoleplayData.GetData("ws", "phoneappsurl"));
        public static int ChangeNameCost = Convert.ToInt32(RoleplayData.GetData("server", "changenamecost"));
        // Gangs
        public static int GangsPrice = Convert.ToInt32(RoleplayData.GetData("gangs", "gangsprice"));
        public static int GangsTurfBonif = Convert.ToInt32(RoleplayData.GetData("gangs", "gangsturfbonif"));
        public static int GangsClaimTurfBonif = Convert.ToInt32(RoleplayData.GetData("gangs", "gangsclaimturfbonif"));
        public static int GangsMaxMembers = Convert.ToInt32(RoleplayData.GetData("gangs", "gangsmaxmembers"));
        public static int ATMRobTime = Convert.ToInt32(RoleplayData.GetData("timer", "atmrobtime"));// segs
        public static int TurfCapTime = Convert.ToInt32(RoleplayData.GetData("timer", "turfcaptime"));// segs
        public static int PurgeTime = Convert.ToInt32(RoleplayData.GetData("timer", "purgetime"));// segs
        public static int BankCapTime = Convert.ToInt32(RoleplayData.GetData("timer", "bankcaptime"));// segs
        // Límite de materiales a comprar (Solo hay 1 punto de venta con un pack de 50);
        // De agregarse más, incrementar el contador.
        public static int ArmMatLimit = Convert.ToInt32(RoleplayData.GetData("server", "armmatlimit"));
        public static int ArmMatPrice = Convert.ToInt32(RoleplayData.GetData("server", "armmatprice"));
        // Rango máximo perteneciente a Admin para Trabajos GType 1 NO removibles.
        public static int AdminRankGroupsNoRemov = Convert.ToInt32(RoleplayData.GetData("server", "adminrankgroupsnoremov"));
        public static int LastTutorialStep = Convert.ToInt32(RoleplayData.GetData("server", "lasttutorialstep"));// is the complete tutorial. If add more frank dialogs, increase this.
        public static string DefaultWebPage = Convert.ToString(RoleplayData.GetData("server", "defaultwebpage"));
        public static int PurgeBonif = Convert.ToInt32(RoleplayData.GetData("server", "purgebonif"));
        public static int MinBasuTime = Convert.ToInt32(RoleplayData.GetData("timer", "minbasutime"));// segs
        public static int MaxBasuTime = Convert.ToInt32(RoleplayData.GetData("timer", "maxbasutime"));// segs
        public static int MinMecTime = Convert.ToInt32(RoleplayData.GetData("timer", "minmectime"));// segs
        public static int MaxMecTime = Convert.ToInt32(RoleplayData.GetData("timer", "maxmectime"));// segs
        public static int MinMinerTime = Convert.ToInt32(RoleplayData.GetData("timer", "minminertime"));// segs
        public static int MaxMinerTime = Convert.ToInt32(RoleplayData.GetData("timer", "maxminertime"));// segs
        public static int CamCargTime = Convert.ToInt32(RoleplayData.GetData("timer", "camcargtime"));// segs
        public static int CamDepositTime = Convert.ToInt32(RoleplayData.GetData("timer", "camdeposittime"));// segs
        public static int ProcessCocaineTime = Convert.ToInt32(RoleplayData.GetData("timer", "fcocainetime"));// segs
        public static int ProcessWeedTime = Convert.ToInt32(RoleplayData.GetData("timer", "fweedtime"));// segs
        public static int ProcessHeroineTime = Convert.ToInt32(RoleplayData.GetData("timer", "fheroinetime"));// segs
        public static int FuelPrice = Convert.ToInt32(RoleplayData.GetData("server", "fuelprice"));
        public static int BidonID = 0;
        public static int MecPartsID = 0;
        public static int ArmMatID = 0;
        public static int ArmPiecesID = 0;
        /// <summary>
        /// Global RP System Timer Manager
        /// </summary>
        public static SystemTimerManager TimerManager = new SystemTimerManager();

        /// <summary>
        /// Gun Store deliveries
        /// </summary>
        public static int UserWhoCalledDelivery = 0;
        public static bool CalledDelivery = false;
        public static bool DoubleExp = false;
        public static Weapon DeliveryWeapon = null;

        /// <summary>
        /// Purge
        /// </summary>
        public static bool PurgeStarted = false;

        /// <summary>
        /// Court stuff
        /// </summary>
        public static bool CourtVoteEnabled = false;
        public static int InnocentVotes = 0;
        public static int GuiltyVotes = 0;

        public static int CourtJuryTime = 0;
        public static bool CourtTrialIsStarting = false;
        public static bool CourtTrialStarted = false;
        public static GameClient Defendant = null;
        public static List<GameClient> InvitedUsersToJuryDuty = new List<GameClient>();
        public static List<GameClient> InvitedUsersToRemove = new List<GameClient>();
        public static bool PreLoadedRooms = false;
        /// <summary>
        /// Thread-safe dictionary containing wanted info
        /// </summary>
        public static ConcurrentDictionary<int, Wanted> WantedList = new ConcurrentDictionary<int, Wanted>();

        /// <summary>
        /// Updates the roleplaymanager variables
        /// </summary>
        public static void UpdateRPData()
        {
            APIUrl = "http://" + Convert.ToString(RoleplayData.GetData("server", "apiurl"));
            LevelCap = Convert.ToInt32(RoleplayData.GetData("level", "cap"));
            FarmingLevelCap = Convert.ToInt32(RoleplayData.GetData("farming", "cap"));
            IntelligenceCap = Convert.ToInt32(RoleplayData.GetData("intelligence", "cap"));
            StrengthCap = Convert.ToInt32(RoleplayData.GetData("strength", "cap"));
            StaminaCap = Convert.ToInt32(RoleplayData.GetData("stamina", "cap"));
            DeathTime = Convert.ToInt32(RoleplayData.GetData("hospital", "deathtime"));
            HungerTime = Convert.ToInt32(RoleplayData.GetData("timer", "hungertime"));// segs
            HygieneTime = Convert.ToInt32(RoleplayData.GetData("timer", "hygienetime"));// segs
            StunGunRange = Convert.ToInt32(RoleplayData.GetData("police", "stungunrange"));
            PSVTime = Convert.ToInt32(RoleplayData.GetData("timer", "psvtime"));// segs
            LevelDifference = Convert.ToBoolean(RoleplayData.GetData("level", "leveldifference"));
            DefaultJailTime = Convert.ToInt32(RoleplayData.GetData("timer", "defaultjailtime"));// min
            StarsJailTime = Convert.ToInt32(RoleplayData.GetData("timer", "starsjailtime"));// min
            AccurateUserCount = Convert.ToBoolean(RoleplayData.GetData("server", "accurateusercount"));
            StartWorkInPoliceHQ = Convert.ToBoolean(RoleplayData.GetData("police", "startworkinhq"));
            DayNightSystem = Convert.ToBoolean(RoleplayData.GetData("server", "daynightsystem"));
            DayNightTaxiTime = Convert.ToBoolean(RoleplayData.GetData("server", "daynighttaxi"));
            ConfiscateWeapons = Convert.ToBoolean(RoleplayData.GetData("server", "confiscateweapons"));
            NewVIPAlert = Convert.ToBoolean(RoleplayData.GetData("server", "newvipalerts"));

            JobCAPTCHABox = Convert.ToBoolean(RoleplayData.GetData("captcha", "job"));
            WorkoutCAPTCHABox = Convert.ToBoolean(RoleplayData.GetData("captcha", "workout"));
            FarmingCAPTCHABox = Convert.ToBoolean(RoleplayData.GetData("captcha", "farming"));

            DefaultHitCooldown = Convert.ToInt32(RoleplayData.GetData("combat", "defaulthitcooldown"));
            HitCooldown = Convert.ToInt32(RoleplayData.GetData("combat", "hitcooldown"));
            HitCooldownInEvent = Convert.ToInt32(RoleplayData.GetData("combat", "hitcooldowninevent"));

            UnloadRoomsAutomatically = Convert.ToBoolean(RoleplayData.GetData("server", "autounloadrooms"));
            FollowFriends = Convert.ToBoolean(RoleplayData.GetData("server", "followfriends"));
            PushPullOnArrows = Convert.ToBoolean(RoleplayData.GetData("server", "pponarrows"));

            NukeMinutes = Convert.ToInt32(RoleplayData.GetData("npa", "nukeminutes"));
            BreakDownMinutes = Convert.ToInt32(RoleplayData.GetData("npa", "breakdownminutes"));
            NPACoolDown = Convert.ToInt32(RoleplayData.GetData("npa", "cooldown"));
            // WS
            CDNSWF = "https://" + Convert.ToString(RoleplayData.GetData("ws", "cdnswf"));
            SWFPath = @"C:" + Convert.ToString(RoleplayData.GetData("ws", "swfpath"));
            CMSPath = @"C:" + Convert.ToString(RoleplayData.GetData("ws", "cmspath"));
            // Para imagenes que manda en HTML con WS
            CdnURL = Convert.ToString(RoleplayData.GetData("ws", "cdnurl"));
            CdnURL2 = Convert.ToString(RoleplayData.GetData("ws", "cdnurl2"));
            PhoneAppsPath = @"C:" + Convert.ToString(RoleplayData.GetData("ws", "phoneappspath"));
            PhoneAppsUrl = Convert.ToString(RoleplayData.GetData("ws", "phoneappsurl"));
            ChangeNameCost = Convert.ToInt32(RoleplayData.GetData("server", "changenamecost"));
            // Gangs
            GangsPrice = Convert.ToInt32(RoleplayData.GetData("gangs", "gangsprice"));
            GangsTurfBonif = Convert.ToInt32(RoleplayData.GetData("gangs", "gangsturfbonif"));
            GangsClaimTurfBonif = Convert.ToInt32(RoleplayData.GetData("gangs", "gangsclaimturfbonif"));
            GangsMaxMembers = Convert.ToInt32(RoleplayData.GetData("gangs", "gangsmaxmembers"));
            // Límite de materiales a comprar (Solo hay 1 punto de venta con un pack de 50);
            // De agregarse más, incrementar el contador.
            ArmMatLimit = Convert.ToInt32(RoleplayData.GetData("server", "armmatlimit"));
            ArmMatPrice = Convert.ToInt32(RoleplayData.GetData("server", "armmatprice"));
            // Rango máximo perteneciente a Admin para Trabajos GType 1 NO removibles.
            AdminRankGroupsNoRemov = Convert.ToInt32(RoleplayData.GetData("server", "adminrankgroupsnoremov"));
            LastTutorialStep = Convert.ToInt32(RoleplayData.GetData("server", "lasttutorialstep"));// is the complete tutorial. If add more frank dialogs, increase this.
            DefaultWebPage = Convert.ToString(RoleplayData.GetData("server", "defaultwebpage"));
            MinMinerTime = Convert.ToInt32(RoleplayData.GetData("timer", "minminertime"));// segs
            MaxMinerTime = Convert.ToInt32(RoleplayData.GetData("timer", "maxminertime"));// segs
            MinMecTime = Convert.ToInt32(RoleplayData.GetData("timer", "minmectime"));// segs
            MaxMecTime = Convert.ToInt32(RoleplayData.GetData("timer", "maxmectime"));// segs
            MinBasuTime = Convert.ToInt32(RoleplayData.GetData("timer", "minbasutime"));// segs
            MaxBasuTime = Convert.ToInt32(RoleplayData.GetData("timer", "maxbasutime"));// segs
            CamCargTime = Convert.ToInt32(RoleplayData.GetData("timer", "camcargtime"));// segs
            CamDepositTime = Convert.ToInt32(RoleplayData.GetData("timer", "camdeposittime"));// segs
            ProcessCocaineTime = Convert.ToInt32(RoleplayData.GetData("timer", "fcocainetime"));// segs
            ProcessWeedTime = Convert.ToInt32(RoleplayData.GetData("timer", "fweedetime"));// segs
            ProcessHeroineTime = Convert.ToInt32(RoleplayData.GetData("timer", "fheroinetime"));// segs
            TurfCapTime = Convert.ToInt32(RoleplayData.GetData("timer", "turfcaptime"));// segs
            PurgeTime = Convert.ToInt32(RoleplayData.GetData("timer", "purgetime"));// segs
            BankCapTime = Convert.ToInt32(RoleplayData.GetData("timer", "bankcaptime"));
            ATMRobTime = Convert.ToInt32(RoleplayData.GetData("timer", "atmrobtime"));// segs
            FuelPrice = Convert.ToInt32(RoleplayData.GetData("server", "fuelprice"));


        }

        public static void AddProduct(GameClient Client, Product Product, string Extradata = "")
        {
            if (!Client.GetRoleplay().OwnedProducts.ContainsKey(Product.ID))
            {
                using (var DB = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    DB.SetQuery("INSERT INTO `rp_products_owned` (`product_id`, `user_id`, `extradata`) VALUES (@productid, @owner, @extra)");
                    DB.AddParameter("productid", Product.ID);
                    DB.AddParameter("owner", Client.GetHabbo().Id);
                    DB.AddParameter("extra", Extradata);
                    DB.RunQuery();

                    ProductsOwned PO = new ProductsOwned((Client.GetRoleplay().OwnedProducts.Count + 1), Product.ID, Client.GetHabbo().Id, Extradata);

                    Client.GetRoleplay().OwnedProducts.TryAdd(Product.ID, PO);
                }
            }
            else
            {
                // Ya tiene el producto, revisamos el max_cant y sumamos en extradata
                int getCant;
                if (!int.TryParse(Client.GetRoleplay().OwnedProducts[Product.ID].Extradata, out getCant))
                    getCant = Client.GetRoleplay().OwnedProducts.Where(x => x.Value.ProductId == Product.ID).Count();

                if (getCant >= Product.MaxCant && Product.MaxCant != -1)
                    Client.SendWhisper("¡Ya cuentas con " + Product.MaxCant + " " + Product.DisplayName + " en tu inventario! No es posible tener más a la vez.", 1);
                else
                {
                    int newCant = Convert.ToInt32(Client.GetRoleplay().OwnedProducts[Product.ID].Extradata) + 1;
                    Client.GetRoleplay().OwnedProducts[Product.ID].Extradata = newCant.ToString();
                    UpdateMyProductExtrada(Client, Product.ID, newCant.ToString());
                }
            }
        }

        public static void Say(GameClient Session, string Speech, int Bubble = 0, bool isSystemMessage = false)
        {
            if (Session?.GetRoomUser() == null) return;
            Session.GetRoomUser().OnChat(Bubble, Speech, false, string.Empty, isSystemMessage);
        }

        public static void AddPhoneAppOwned(GameClient Client, int AppId, string Extradata = "")
        {
            if (Client.GetRoleplay().Phone > 0)
            {
                if (!Client.GetRoleplay().OwnedPhonesApps.ContainsKey(AppId))
                {
                    int id = 0;
                    using (var DB = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                    {
                        List<Phone> mPH = PhoneManager.getPhoneById(Client.GetRoleplay().PhoneModelId);
                        if (mPH != null)
                        {
                            int MaxApps = mPH[0].ScreenSlots - mPH[0].DockSlots;
                            int LastSlot = 0;
                            int LastScreen = 0;

                            #region Get the LastScreen & LastSlot
                            foreach (KeyValuePair<int, PhonesAppsOwned> App in Client.GetRoleplay().OwnedPhonesApps.ToList().OrderByDescending(S => S.Value.ScreenId))
                            {
                                LastScreen = App.Value.ScreenId;
                                break;
                            }

                            foreach (KeyValuePair<int, PhonesAppsOwned> App in Client.GetRoleplay().OwnedPhonesApps.ToList().Where(x => x.Value.ScreenId == LastScreen).OrderByDescending(S => S.Value.SlotId))
                            {
                                LastSlot = App.Value.SlotId + 1;
                                break;
                            }

                            if (LastSlot > MaxApps)
                            {
                                LastSlot = 0;
                                LastScreen++;
                            }
                            #endregion


                            DB.SetQuery("INSERT INTO `rp_phones_apps_owned` (`phone_id`, `app_id`, `screen_id`, `slot_id`, `extradata`) VALUES (@phoneid, @appid, @screenid @slotid, @extradata)");
                            DB.AddParameter("phoneid", Client.GetRoleplay().Phone);
                            DB.AddParameter("appid", AppId);
                            DB.AddParameter("screenid", LastScreen);
                            DB.AddParameter("slotid", LastSlot);
                            DB.AddParameter("extradata", Extradata);
                            DB.RunQuery();

                            PhonesAppsOwned PO = new PhonesAppsOwned(id, Client.GetRoleplay().Phone, AppId, LastScreen, LastSlot, Extradata);
                            Client.GetRoleplay().OwnedPhonesApps.TryAdd(AppId, PO);
                        }
                    }
                }
            }
        }

        public static void ShoutSay(GameClient Session, string Speech, int Bubble = 0, string Colour = "black", bool shout = true, bool isSystemMessage = false)
        {
            if (Session?.GetRoomUser() == null) return;

            // Bypass flood para mensajes de sistema
            if (isSystemMessage)
            {
                Session.GetRoomUser().ChatSpamCount = 0;
                Session.GetHabbo().FloodTime = 0;
            }

            Session.GetRoomUser().OnChat(Bubble, Speech, shout, Colour, isSystemMessage);
        }


        public static void SetDefaultApps(GameClient Client)
        {
            if (Client.GetRoleplay().Phone > 0)
            {
                int id = 0;
                using (var DB = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    List<Phone> mPH = PhoneManager.getPhoneById(Client.GetRoleplay().PhoneModelId);
                    if (mPH != null)
                    {
                        // MYSQL PROCEDIMIENTO
                        DB.RunQuery("CALL `SetDefaultApps`(" + Client.GetRoleplay().Phone + ");");

                        int LastScreen = 1;
                        int LastSlot = 1;
                        for (int AppId = 1; AppId <= 18; AppId++)
                        {
                            id++;

                            PhonesAppsOwned PO = new PhonesAppsOwned(id, Client.GetRoleplay().Phone, AppId, LastScreen, LastSlot, "");
                            Client.GetRoleplay().OwnedPhonesApps.TryAdd(AppId, PO);

                            LastSlot++;

                            if (AppId == 14)
                            {
                                LastScreen = 0;
                                LastSlot = 1;
                            }
                        }
                    }
                }
            }
        }

        public static String GetLada()
        {
            return "555";
        }

        public static String GeneratePhoneNumber(int userid)
        {
            bool repetido = true;
            Random rnd = new Random();
            String Num = "";

            while (repetido)
            {
                Num = PolarEnvironment.GetGame().GetClientManager().NumberFormatRP(GetLada() + rnd.Next(1000000, 9999999));

                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("SELECT phone_number FROM rp_phones_owned WHERE phone_number = '" + Num + "'");
                    DataRow GetNums = dbClient.getRow();

                    if (GetNums == null)
                        repetido = false;
                }

            }

            return Num;
        }

        public static void AssingInventoryProducts()
        {
            Product PO = ProductsManager.getProduct("bidon");
            if (PO != null)
                BidonID = PO.ID;

            PO = ProductsManager.getProduct("MecParts");
            if (PO != null)
                MecPartsID = PO.ID;

            PO = ProductsManager.getProduct("ArmMat");
            if (PO != null)
                ArmMatID = PO.ID;

            PO = ProductsManager.getProduct("ArmPieces");
            if (PO != null)
                ArmPiecesID = PO.ID;

            //log.Info("Assignaments of Inventory Products successfully");
        }

        public static void UpdateMyProductExtrada(GameClient Session, int ProductId, string extradata)
        {
            if (Session == null || Session.GetHabbo() == null)
                return;

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.RunQuery("UPDATE `rp_products_owned` SET `extradata` = '" + extradata + "' WHERE `rp_products_owned`.`product_id` = '" + ProductId + "' AND `rp_products_owned`.`user_id` = " + Session.GetHabbo().Id + ";");
            }
        }
        public static List<Room> GetRoomByDesc(string name)
        {
            List<Room> rooms = new List<Room>();
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT `id` FROM `rooms` WHERE `description` LIKE '" + name + "%'");
                DataTable GetRooms = dbClient.getTable();
                if (GetRooms != null)
                {
                    foreach (DataRow Row in GetRooms.Rows)
                    {
                        if (GenerateRoom(Convert.ToInt32(Row["id"]), out Room Room))
                            rooms.Add(Room);
                    }
                }
            }
            return rooms;
        }

        public static int getCamCargDest(Room Room, int CargaId)
        {
            List<Room> Rooms = null;
            string MyCity = Room.Name;
            int Destino = -1;
            switch (CargaId)
            {
                case 1:
                    Rooms = GetRoomByDesc("24/7");
                    break;
                case 2:
                    Rooms = GetRoomByDesc("Tienda de Ropa");
                    break;
                case 3:
                    Rooms = GetRoomByDesc("Drogas");
                    break;
                case 4:
                    Rooms = GetRoomByDesc("Ammunation");
                    break;
                default:
                    break;
            }
            if (Rooms.Count > 0)
            {
                foreach (Room CurRoom in Rooms)
                {
                    if (GenerateRoom(CurRoom.RoomId, out Room RoomInfo))
                    {
                        string JobCity = RoomInfo.Name;
                        if (MyCity == JobCity)
                            Destino = CurRoom.RoomId;
                    }
                }

            }
            return Destino;
        }
        public static string getCamCargName(int CargaId)
        {
            string CargName = "Ninguno";
            switch (CargaId)
            {
                case 1:
                    CargName = "Productos de 24/7";
                    break;
                case 2:
                    CargName = "Ropa";
                    break;
                case 3:
                    CargName = "Drogas";
                    break;
                case 4:
                    CargName = "Armas";
                    break;
                default:
                    break;
            }
            return CargName;
        }


        public static void ClaimTurf(GameClient Session, Room Turf, Group Gang)
        {
            if (Turf.Group != null)
            {
                // Alertamos que han perdido banda
                foreach (GameClient client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                {
                    if (client == null || client.GetHabbo() == null)
                        continue;

                    List<Group> thegroup = PolarEnvironment.GetGame().GetGroupManager().GetGangsForUser(client.GetHabbo().Id);

                    if (thegroup == null || thegroup.Count <= 0)
                        continue;

                    if (thegroup[0] != Turf.Group)
                        continue;

                    if (client.GetRoleplay().DisableRadio == true)
                        continue;

                    client.SendWhisper("[RADIO] ¡Hemos perdido el " + Turf.Name + "!", 30);
                }

                if (!Turf.Group.BankRuptcy)
                {
                    Turf.Group.Balance -= GangsClaimTurfBonif;
                    Turf.Group.SetBussines(Turf.Group.Balance, Turf.Group.Stock);
                    // Alertar miembros de banda que han perdido su territorio

                    Gang.Balance += GangsClaimTurfBonif;
                    Gang.SetBussines(Gang.Balance, Gang.Stock);
                    Session.SendWhisper("¡Tu banda ha saqueado $ " + String.Format("{0:N0}", GangsClaimTurfBonif) + " de las riquezas de la banda " + Turf.Group.Name + "!", 1);
                    Gang.AddLog(Session.GetHabbo().Id, Session.GetHabbo().Username + " ha capturado el " + Turf.Name + " ganando $ " + String.Format("{0:N0}", GangsClaimTurfBonif) + " para la banda.", GangsClaimTurfBonif);
                }
                else { }
                    Gang.AddLog(Session.GetHabbo().Id, Session.GetHabbo().Username + " ha capturado el " + Turf.Name, 0);
            }
            else
                Gang.AddLog(Session.GetHabbo().Id, Session.GetHabbo().Username + " ha capturado el " + Turf.Name, 0);

            if (Gang == null)
                return;

            // Stats Ganga
            Gang.GangTurfsTaken++;
            Gang.UpdateStat(Gang.Id, "gang_turfs_taken", Gang.GangTurfsTaken);

            // Actualizamos caché del grupo de la sala
            Turf.Group = Gang;
            Turf.RoomData.Group = Gang;
            Session.SendMessage(new NewGroupInfoComposer(Turf.RoomId, Gang.Id));

            Gang.ClaimTurf(Turf.RoomId, Session.GetRoleplay().TurfFlagId);

            // Actualizamos colores de la bandera en caché
            Turf.GetRoomItemHandler().LoadFurniture(true);
            Turf.GetGameMap().GenerateMaps();
            Item GroupFlag = Turf.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "army_c15_groupflag");
            if (GroupFlag != null)
            {
                PickItem(Session, GroupFlag.Id);
                Session.GetRoleplay().isParking = true;
                Turf.GetRoomItemHandler().SetFloorItem(Session, GroupFlag, GroupFlag.GetX, GroupFlag.GetY, GroupFlag.Rotation, true, false, true);
                Session.GetRoleplay().isParking = false;
            }

            // Mandamos WS del Grupo de la Sala a los users de la sala.
            foreach (RoomUser roomUser in Turf.GetRoomUserManager().GetRoomUsers())
            {
                if (roomUser == null)
                    continue;

                if (roomUser.GetClient() == null || roomUser.GetClient().GetConnection() == null)
                    continue;

                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(roomUser.GetClient(), "event_group", "open");
                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(roomUser.GetClient(), "event_gang", "turf_cap_off");
            }
        }

        /// <summary>
        /// Gets all vital parts of a users figure
        /// </summary>
        /// 
        public static void CheckBasurero(GameClient Client)
        {
            if (Client.GetRoleplay().BasuTeamId > 0)
            {
                GameClient TeamPasaj = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Client.GetRoleplay().BasuTeamId);
                if (TeamPasaj != null)
                {
                    TeamPasaj.SendWhisper("¡Tu compañero se ha ido! Han fracasado el recorrido.", 1);
                    TeamPasaj.GetRoleplay().BasuTeamId = 0;
                    TeamPasaj.GetRoleplay().BasuTeamName = "";
                    TeamPasaj.GetRoleplay().BasuTrashCount = 0;
                    TeamPasaj.GetRoleplay().IsBasuPasaj = false;
                    if (!TeamPasaj.GetRoleplay().DrivingCar)
                        TeamPasaj.GetRoleplay().IsBasuChofer = false;
                    else
                    {
                        PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(TeamPasaj,
                            "compose_basurero|" +
                            "showinfo|" +
                            TeamPasaj.GetHabbo().Username + "|" + // Chofer
                            "Ninguno|" + // Recolector
                            TeamPasaj.GetRoleplay().BasuTrashCount + "/15|" +
                            TeamPasaj.GetRoleplay().IsBasuChofer);
                    }
                }
            }
        }


        public static void CheckAntiLaw(GameClient Client)
        {
            if (Client.GetRoleplay().Cuffed)
                ForceAntirolJail(Client);

            if (Client.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("stun"))
                ForceAntirolSanc(Client, 15);

        }

        public static void ForceAntirolJail(GameClient Client)
        {
            Wanted Wanted = RoleplayManager.WantedList.ContainsKey(Client.GetHabbo().Id) ? RoleplayManager.WantedList[Client.GetHabbo().Id] : null;
            int WantedTime = Wanted == null ? 10 : (Wanted.WantedLevel + 1) * 5;

            Client.GetRoleplay().IsJailed = true;
            Client.GetRoleplay().JailedTimeLeft = WantedTime;
            Client.GetRoleplay().TimerManager.CreateTimer("jail", 1000, false);
            Client.GetRoleplay().Arrested++;
            SaveQuickStat(Client, "is_jailed", "1");
            SaveQuickStat(Client, "jailed_time_left", "" + WantedTime);
            SaveQuickStat(Client, "is_wanted", "0");
            SaveQuickStat(Client, "wanted_level", "0");
            SaveQuickStat(Client, "wanted_time_left", "0");
            SaveQuickStat(Client, "is_cuffed", "0");
            SaveQuickStat(Client, "cuffed_time_left", "0");
            SaveQuickStat(Client, "arrested", Client.GetRoleplay().Arrested);
        }

        public static void ForceAntirolJail(int Id)
        {
            Wanted Wanted = RoleplayManager.WantedList.ContainsKey(Id) ? RoleplayManager.WantedList[Id] : null;
            int WantedTime = Wanted == null ? 10 : (Wanted.WantedLevel + 1) * 5;

            SaveQuickStat(Id, "is_jailed", "1");
            SaveQuickStat(Id, "jailed_time_left", "" + WantedTime);
            SaveQuickStat(Id, "is_wanted", "0");
            SaveQuickStat(Id, "wanted_level", "0");
            SaveQuickStat(Id, "wanted_time_left", "0");
            SaveQuickStat(Id, "is_cuffed", "0");
            SaveQuickStat(Id, "cuffed_time_left", "0");
            SaveQuickStat(Id, "arrested", "arrested + 1", true);
        }

        public static void ForceAntirolSanc(GameClient Client, int timeSanc)
        {
            #region Check Workers
            if (Client.GetRoleplay().IsWorking)
            {
                WorkManager.RemoveWorkerFromList(Client);
                Client.GetRoleplay().IsWorking = false;
                Client.GetHabbo().Poof();
                //RoleplayManager.CheckCorpCarp(client);
                RoleplayManager.Shout(Client, "*Ha dejado de trabajar*", 5);

                #region Check Police Car
                if (Client.GetRoleplay().DrivingCar && Client.GetRoleplay().CarEnableId == EffectsList.CarPolice)
                {
                    #region Park
                    int ItemPlaceId = 0;
                    int roomid = Client.GetRoomUser().RoomId;
                    if (!RoleplayManager.GenerateRoom(roomid, out Room Room, false))
                        return;
                    VehiclesOwned VOD = null;
                    if (Client.GetRoleplay().DrivingInCar)
                    {
                        RoleplayManager.Shout(Client, "* Una Grúa se ha llevado el vehículo que " + Client.GetHabbo().Username + " conducía.", 4);
                        // Actualizamos datos del auto en el diccionario y DB
                        PolarEnvironment.GetGame().GetVehiclesOwnedManager().UpdateVehicleOwner(Client, 0, false, out VOD);
                        ItemPlaceId = VOD.Id;
                        PolarEnvironment.GetGame().GetVehiclesOwnedManager().DeleteVehicleOwned(Client.GetRoleplay().DrivingCarId);
                        RoleplayManager.CheckCorpCarp(Client);
                    }

                    #region Extra Conditions & Checks
                    #region CorpCar Respawn
                    Client.GetRoleplay().CarJobLastItemId = ItemPlaceId;
                    #endregion

                    #region Pasajeros (Algoritmo replicado en ConditionCheckTimer por seguridad)
                    //Vars
                    string Pasajeros = Client.GetRoleplay().Pasajeros;
                    string[] stringSeparators = new string[] { ";" };
                    string[] result;
                    result = Pasajeros.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string psjs in result)
                    {
                        GameClient PJ = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(psjs);
                        if (PJ != null)
                        {
                            if (PJ.GetRoleplay().ChoferName == Client.GetHabbo().Username)
                            {
                                RoleplayManager.Shout(PJ, "*Baja del vehículo de " + Client.GetHabbo().Username + "*", 5);
                            }
                            // PASAJERO
                            PJ.GetRoleplay().Pasajero = false;
                            PJ.GetRoleplay().ChoferName = "";
                            PJ.GetRoleplay().ChoferID = 0;
                            PJ.GetRoomUser().CanWalk = true;
                            PJ.GetRoomUser().FastWalking = false;
                            PJ.GetRoomUser().TeleportEnabled = false;
                            PJ.GetRoomUser().AllowOverride = false;

                            // Descontamos Pasajero
                            Client.GetRoleplay().PasajerosCount--;
                            StringBuilder builder = new StringBuilder(Client.GetRoleplay().Pasajeros);
                            builder.Replace(PJ.GetHabbo().Username + ";", "");
                            Client.GetRoleplay().Pasajeros = builder.ToString();

                            // CHOFER 
                            Client.GetRoleplay().Chofer = (Client.GetRoleplay().PasajerosCount <= 0) ? false : true;
                            Client.GetRoomUser().AllowOverride = (Client.GetRoleplay().PasajerosCount <= 0) ? false : true;

                            // SI EL PASAJERO ES COMPAÑERO DE BASURERO
                            if (PJ.GetRoleplay().IsBasuPasaj)
                                PJ.GetRoleplay().IsBasuPasaj = false;

                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(PJ, "event_vehicle", "close");// WS FUEL
                        }
                    }
                    #endregion

                    #region Check Jobs

                    #endregion
                    #endregion

                    #region Online ParkVars
                    //Retornamos a valores predeterminados
                    Client.GetRoleplay().DrivingCar = false;
                    Client.GetRoleplay().DrivingInCar = false;
                    Client.GetRoleplay().DrivingCarId = 0;// Id de VehiclesOwned;

                    //Combustible System
                    Client.GetRoleplay().CarType = 0;// Define el gasto de combustible
                    Client.GetRoleplay().CarFuel = 0;
                    Client.GetRoleplay().CarMaxFuel = 0;
                    Client.GetRoleplay().CarTimer = 0;
                    Client.GetRoleplay().CarLife = 0;

                    Client.GetRoleplay().CarEnableId = 0;//Coloca el enable para conducir
                    Client.GetRoleplay().CarEffectId = 0;//Guarda el enable del último auto en conducción.
                    Client.GetRoomUser().ApplyEffect(0);
                    Client.GetRoomUser().FastWalking = false;
                    #endregion

                    PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_vehicle", "close");
                    #endregion

                    RoleplayManager.CheckCorpCarp(Client);
                }
                #endregion
            }
            #endregion

            #region Check Dying or Dead
            if (Client.GetRoleplay().IsDead)
            {
                Client.GetRoleplay().IsDead = false;

                if (Client.GetRoomUser() != null)
                {
                    Client.GetRoomUser().ApplyEffect(0);
                    Client.GetRoomUser().CanWalk = true;
                    Client.GetRoomUser().Frozen = false;
                }

                Client.GetRoleplay().DeadTimeLeft = 0;
                Client.GetRoleplay().CurHealth = Client.GetRoleplay().MaxHealth;

                // Refrescamos WS
                Client.GetRoleplay().UpdateInteractingUserDialogues();
                Client.GetRoleplay().RefreshStatDialogue();
            }
            #endregion

            #region Check Cuffed
            if (Client.GetRoleplay().Cuffed)
                Client.GetRoleplay().Cuffed = false;
            #endregion

            #region Check Jailed
            Client.GetRoleplay().IsJailed = false;
            Client.GetRoleplay().JailedTimeLeft = 0;
            Client.GetHabbo().Poof(true);
            #endregion

            #region Desequipar
            if (Client.GetRoleplay().EquippedWeapon != null)
            {
                string UnEquipMessage = Client.GetRoleplay().EquippedWeapon.UnEquipText;
                UnEquipMessage = UnEquipMessage.Replace("[NAME]", Client.GetRoleplay().EquippedWeapon.PublicName);

                RoleplayManager.Shout(Client, UnEquipMessage, 5);

                if (Client.GetRoomUser().CurrentEffect == Client.GetRoleplay().EquippedWeapon.EffectID)
                    Client.GetRoomUser().ApplyEffect(0);

                if (Client.GetRoomUser().CarryItemID == Client.GetRoleplay().EquippedWeapon.HandItem)
                    Client.GetRoomUser().CarryItem(0);

                Client.GetRoleplay().CooldownManager.CreateCooldown("unequip", 1000, 3);
                Client.GetRoleplay().EquippedWeapon = null;

                Client.GetRoleplay().WLife = 0;
                Client.GetRoleplay().Bullets = 0;
            }
            #endregion

            Client.SendMessage(new RoomNotificationComposer("room_sanc", "message", "Has sido sancionad@ por Antirol durante " + timeSanc + " minuto(s)"));

            SaveQuickStat(Client, "is_jailed", "0");
            SaveQuickStat(Client, "jailed_time_left", "0");
            SaveQuickStat(Client, "is_cuffed", "0");
            SaveQuickStat(Client, "cuffed_time_left", "0");
            /*SaveQuickStat(Client, "is_sanc", "1");
            SaveQuickStat(Client, "sanc_time_left", timeSanc);*/
        }

        public static void ForceAntirolSanc(int Id, int timeSanc)
        {
            SaveQuickStat(Id, "is_jailed", "0");
            SaveQuickStat(Id, "jailed_time_left", "0");
            SaveQuickStat(Id, "is_cuffed", "0");
            SaveQuickStat(Id, "cuffed_time_left", "0");
            /*S//SaveQuickStat(Id, "is_sanc", "1");
           // SaveQuickStat(Id, "sanc_time_left", timeSanc);
            //SaveQuickStat(Id, "sancs", "sancs + 1", true);*/
        }
        // Retorna en número entero en segundos
        public static int GetTimerByMyJob(GameClient Session, string Job)
        {
            int Timer = 0;

            #region GetByCases
            switch (Job.ToLower())
            {
                #region Basurero
                case "basurero":
                    {
                        Timer = MaxBasuTime - (Session.GetRoleplay().BasuLvl - 1);

                        if (Timer < MinBasuTime)
                            Timer = MinBasuTime;
                    }
                    break;
                #endregion

                #region Mecánico
                case "mecanico":
                    {
                        Timer = MaxMecTime - ((Session.GetRoleplay().MecLvl - 1) * 5);

                        if (Timer < MinMecTime)
                            Timer = MinMecTime;
                    }
                    break;
                #endregion

                #region default
                default:
                    break;
                    #endregion
            }
            #endregion

            if (Timer < 0)
                Timer = 0;
            return Timer;
        }

        public static void GiveMoneyToCompany(int JobID, GameClient Session, string Right, bool CustomAmount = false, int CAmount = 0)
        {

            if (Session == null)
                return;

            Group Group = PolarEnvironment.GetGame().GetGroupManager().GetJobByID(JobID);

            if (Group == null)
                return;

            int Amount = 0;
            if (CustomAmount == false)
            {

                if (!GroupManager.JobExists(Session.GetRoleplay().JobId, Session.GetRoleplay().JobRank))
                    return;
            }
            else
            {
                Amount = CAmount;
            }
            /*using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.RunQuery("UPDATE `rp_jobs` SET bank_balance = (bank_balance + " + Amount + ") WHERE id = " + JobID + " LIMIT 1");
            }*/


            Group.Balance += Amount;
            Group.SetBussines(Group.Balance);

            if (CustomAmount == false)
                Session.SendWhisper("*La empresa ha ganado (" + Amount + "$), Gracias a ti, Sigue trabajando!*", 0);
            
            Session.GetRoleplay().ClearWebSocketDialogue();
            Session.GetRoleplay().RefreshStatDialogue();
        }

        public static void SetJobCar(GameClient Session, int VehicleJobID)
        {
            VehicleJobs VehicleJob = VehicleJobsManager.getVehicleJob(VehicleJobID);
            if (VehicleJob != null)
            {
                if (GenerateRoom(VehicleJob.RoomID, out Room Room))
                {
                    foreach (Item item in Room.GetRoomItemHandler().GetFurniObjects(VehicleJob.X, VehicleJob.Y).ToList())
                    {
                        //if (item.GetBaseItem().ItemName.StartsWith("carro_rp_"))
                        if (item.GetBaseItem().Id >= 8000002)//Cualquier vehículo (rp_vehicles)
                        {
                            Room.GetRoomItemHandler().RemoveRoomItem(item);
                            PickItem(Session, item.Id);
                            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                            {
                                dbClient.RunQuery("DELETE FROM items WHERE id = " + item.Id);
                            }
                            //Session.SendWhisper("Se ha quitado un vehiculo detectado en esa posición", 1);
                        }
                    }

                    lock (itemobj)
                    {
                        Session.GetRoleplay().isParking = true;

                        //Console.WriteLine("SessionID: " + Session.GetHabbo().Id + "\nVehicleJob.BaseItem: " + VehicleJob.BaseItem + "\nVehicleJobX/Y/Z" + VehicleJob.X + " " + VehicleJob.Y + " " + VehicleJob.Z + "VehicleJob.Rot: " + VehicleJob.Rot + "Room.Id" + Room.Id);

                        var Obj = PlaceItemToRoom(Session, VehicleJob.BaseItem, 0, VehicleJob.X, VehicleJob.Y, VehicleJob.Z, VehicleJob.Rot, false, Room.Id, true, "", false);

                        //Console.WriteLine(Obj);

                        Session.GetRoleplay().isParking = false;
                    }
                    //Session.SendWhisper("Auto colocado", 1);
                }
            }
        }
        public static void CheckCorpCarp(GameClient Session)
        {
            // Si conduce un auto de trabajo, pero ya había conducido uno
            // Verificamos si es el mismo.
            // Si es otro, recogemos el anterior (de existir) y spawn.
            if (Session.GetRoleplay().CarJobId > 0)
            {
                /*VERFICAR SI LO TRAE CONDUCIENDO ALGUIEN PARA EVITAR DUPLICAR*/
                #region Remove Vehicle From Target
                List<GameClient> TargetDriver = (from TG in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList() where TG != null && TG.GetHabbo() != null && TG.GetRoleplay() != null && TG.GetRoleplay().DrivingCarItem == Session.GetRoleplay().CarJobLastItemId && TG.GetHabbo().Id != Session.GetHabbo().Id select TG).ToList();
                foreach (GameClient Target in TargetDriver)
                {
                    #region Extra Conditions & Checks

                    #region Pasajeros (Algoritmo replicado en ConditionCheckTimer por seguridad)
                    //Vars
                    string Pasajeros = Target.GetRoleplay().Pasajeros;
                    string[] stringSeparators = new string[] { ";" };
                    string[] result;
                    result = Pasajeros.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string psjs in result)
                    {
                        GameClient PJ = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(psjs);
                        if (PJ != null)
                        {
                            if (PJ.GetRoleplay().ChoferName == Target.GetHabbo().Username)
                            {
                                RoleplayManager.Shout(PJ, "*Baja del vehículo de " + Target.GetHabbo().Username + "*", 5);
                            }
                            // PASAJERO
                            PJ.GetRoleplay().Pasajero = false;
                            PJ.GetRoleplay().ChoferName = "";
                            PJ.GetRoleplay().ChoferID = 0;
                            PJ.GetRoomUser().CanWalk = true;
                            PJ.GetRoomUser().FastWalking = false;
                            PJ.GetRoomUser().TeleportEnabled = false;
                            PJ.GetRoomUser().AllowOverride = false;

                            // Descontamos Pasajero
                            Target.GetRoleplay().PasajerosCount--;
                            StringBuilder builder = new StringBuilder(Target.GetRoleplay().Pasajeros);
                            builder.Replace(PJ.GetHabbo().Username + ";", "");
                            Target.GetRoleplay().Pasajeros = builder.ToString();

                            // CHOFER 
                            Target.GetRoleplay().Chofer = (Target.GetRoleplay().PasajerosCount <= 0) ? false : true;
                            Target.GetRoomUser().AllowOverride = (Target.GetRoleplay().PasajerosCount <= 0) ? false : true;

                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(PJ, "event_vehicle", "close");// WS FUEL
                        }
                    }
                    #endregion

                    #region Online ParkVars
                    //Retornamos a valores predeterminados
                    Target.GetRoleplay().DrivingCar = false;
                    Target.GetRoleplay().DrivingInCar = false;
                    Target.GetRoleplay().DrivingCarId = 0;// Id de VehiclesOwned;

                    //Combustible System
                    Target.GetRoleplay().CarType = 0;// Define el gasto de combustible
                    Target.GetRoleplay().CarFuel = 0;
                    Target.GetRoleplay().CarMaxFuel = 0;
                    Target.GetRoleplay().CarTimer = 0;
                    Target.GetRoleplay().CarLife = 0;

                    Target.GetRoleplay().CarEnableId = 0;//Coloca el enable para conducir
                    Target.GetRoleplay().CarEffectId = 0;//Guarda el enable del último auto en conducción.
                    if (Target.GetRoomUser() != null)
                    {
                        Target.GetRoomUser().ApplyEffect(0);
                        Target.GetRoomUser().FastWalking = false;
                    }
                    PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Target, "event_vehicle", "close");// WS FUEL

                    Target.GetRoleplay().CarJobId = 0;
                    Target.GetRoleplay().CarJobLastItemId = 0;
                    #endregion

                    Shout(Target, " el vehículo que " + Target.GetHabbo().Username + " conducía fue decomisado por una grúa.", 4);
                    Target.SendWhisper("Al parecer el propietario original del vehículo ha reclamado su uso.", 1);
                    #endregion
                }
                #endregion

                //Recoge el item
                PickItem(Session, Session.GetRoleplay().CarJobLastItemId);
                // Quitar del diccionario
                PolarEnvironment.GetGame().GetVehiclesOwnedManager().DeleteVehicleOwnedByFurniId(Session.GetRoleplay().CarJobLastItemId);
                //ClearCorpCar(Session, Session.GetRoleplay().CarJobLastItemId); <= NO se guardan en db, no necesario.
                //Respawneamos nuevo auto
                SetJobCar(Session, Session.GetRoleplay().CarJobId);
            }

            // Retornamos
            Session.GetRoleplay().CarJobId = 0;
            Session.GetRoleplay().CarJobLastItemId = 0;

            if (Session.GetRoleplay().TimerManager != null && Session.GetRoleplay().TimerManager.ActiveTimers != null)
            {
                if (Session.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("vehiclejob"))
                {
                    Session.SendWhisper("¡Tu Vehículo de trabajo ha sido regresado a su sitio por haberlo abandonado mucho tiempo!", 1);
                    Session.GetRoleplay().TimerManager.ActiveTimers["vehiclejob"].EndTimer();
                }
            }
        }

        public static void PickItem(GameClient Client, int furni_id, int newroom = 0)
        {
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
               dbClient.RunQuery($"UPDATE `{Polar.Core.DatabaseCompatibility.ItemsTable}` SET `room_id` = '0' WHERE `id` = '" + furni_id + "' LIMIT 1");
            }

            if (Client != null && Client.GetRoomUser() != null && newroom <= 0)
            {
                if (GenerateRoom(Client.GetRoomUser().RoomId, out Room Room))
                    Room.GetRoomItemHandler().RemoveFurniture(null, furni_id);
            }
            else if (newroom > 0)
            {
                if (GenerateRoom(newroom, out Room Room))
                    Room.GetRoomItemHandler().RemoveFurniture(null, furni_id);
            }
        }
        public static void GiveMoneyFromCompanyNoRight(int JobID, int Amount, GameClient Session = null, bool isATM = false)
        {
            if (!GroupManager.validJustJob(JobID))
                return;

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {

                dbClient.SetQuery("SELECT * FROM rp_jobs WHERE id = " + JobID + " LIMIT 1");

                DataTable Jobs = dbClient.getTable();
                if (Jobs == null || Jobs.Rows.Count == 0)
                    return;
                int GainAmount = 0;
                foreach (DataRow Job in Jobs.Rows)
                {
                    GainAmount = Convert.ToInt32(Job["bank_balance"]);
                }

                int Calc = GainAmount + Amount;

                dbClient.RunQuery("UPDATE `rp_jobs` SET bank_balance = " + Calc + " WHERE id = " + JobID + " LIMIT 1");

            }
            if (isATM == true)
            {
                if (Session != null)
                    Session.SendWhisper("*Has depositado con exito (" + Amount + "$), el banco se encargara de guardar tu dinero!*", 0);
            }
            else
            {
                if (Session != null)
                    Session.SendWhisper("*La compañia ha ganado (" + Amount + "$), Gracias a ti, Sigue trabajando!*", 0);
            }

        }

        public static bool isValidPark(Room Room, Point Coords)
        {
            if (Room == null)
                return false;

            Item BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "arow" && x.Coordinate == Coords);
            Item BTile2 = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "room_wl15_teleblock" && x.Coordinate == Coords);
            Item BTile3 = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "comodin_carro" && x.Coordinate == Coords);
            Item BTile4 = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "comodin_carr2" && x.Coordinate == Coords);
            Item BTile5 = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "actionpoint01" && x.Coordinate == Coords);
            Item BTile6 = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "actionpoint02" && x.Coordinate == Coords);
            Item BTile7 = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "room_wl15_telearrow" && x.Coordinate == Coords);

            if (BTile != null || BTile2 != null || BTile3 != null || BTile4 != null || BTile5 != null || BTile6 != null || BTile7 != null)
                return false;

            return true;
        }

        public static void CheckOnCar(GameClient Client)
        {
            #region Is Driving?
            if (Client.GetRoleplay().DrivingCar)
            {
                #region Vehicle Check
                Vehicle vehicle = null;
                int corp = 0;
                bool ToDB = true;
                foreach (Vehicle Vehicle in VehicleManager.Vehicles.Values)
                {
                    if (Client.GetRoleplay().CarEnableId == Convert.ToInt32(Vehicle.EffectID))
                    {
                        vehicle = Vehicle;
                        if (vehicle.CarCorp > 0)
                        {
                            corp = vehicle.CarCorp;
                            ToDB = false;
                        }
                    }
                }
                #endregion

                #region Park
                int ItemPlaceId = 0;
                int roomid = Client.GetRoomUser().RoomId;
                if (!RoleplayManager.GenerateRoom(roomid, out Room RoomDriving, false))
                    return;

                object[] Coords = { Client.GetRoomUser().X, Client.GetRoomUser().Y, Client.GetRoomUser().Z, Client.GetRoomUser().RotBody };
                if (Client.GetRoleplay().DrivingInCar || !RoleplayManager.isValidPark(RoomDriving, Client.GetRoomUser().Coordinate))
                {
                    RoleplayManager.Shout(Client, "* Una Grúa se ha llevado el vehículo que " + Client.GetHabbo().Username + " conducía por encontrarse mal estacionado y sin combustible.", 4);
                    // Actualizamos datos del auto en el diccionario y DB
                    VehiclesOwned VOD;
                    PolarEnvironment.GetGame().GetVehiclesOwnedManager().UpdateVehicleOwner(Client, 0, ToDB, out VOD);
                    ItemPlaceId = VOD.Id;
                    if (corp > 0)
                    {
                        PolarEnvironment.GetGame().GetVehiclesOwnedManager().DeleteVehicleOwned(Client.GetRoleplay().DrivingCarId);
                        RoleplayManager.CheckCorpCarp(Client);
                    }
                }
                else if (Client.GetRoleplay().DrivingCar)
                {
                    if (corp > 0)
                    {
                        RoleplayManager.Shout(Client, "* Una Grúa se ha llevado el vehículo que " + Client.GetHabbo().Username + " conducía por encontrarse mal estacionado y sin combustible.", 4);
                        PolarEnvironment.GetGame().GetVehiclesOwnedManager().DeleteVehicleOwned(Client.GetRoleplay().DrivingCarId);
                        RoleplayManager.CheckCorpCarp(Client);
                    }
                    else
                    {
                        // Colocamos Furni en Sala
                        Client.GetRoleplay().isParking = true;
                        Item ItemPlace = RoleplayManager.PutItemToRoom(Client, Client.GetRoleplay().DrivingCarItem, Client.GetRoomUser().RoomId, vehicle.ItemID, Convert.ToInt32(Coords[0]), Convert.ToInt32(Coords[1]), Convert.ToInt32(Coords[3]), ToDB);
                        //Item ItemPlace = RoleplayManager.PlaceItemToRoom(Client, vehicle.ItemID, 0, Client.GetRoomUser().X, Client.GetRoomUser().Y, Client.GetRoomUser().Z, Client.GetRoomUser().RotBody, false, RoomDriving.Id, ToDB, "");
                        Client.GetRoleplay().isParking = false;
                        ItemPlaceId = ItemPlace.Id;
                        // Actualizamos datos del auto en el diccionario y DB
                        VehiclesOwned VOD;
                        PolarEnvironment.GetGame().GetVehiclesOwnedManager().UpdateVehicleOwner(Client, ItemPlaceId, ToDB, out VOD);
                    }
                }

                #region Extra Conditions & Checks
                #region CorpCar Respawn
                if (corp > 0)
                {
                    Client.GetRoleplay().CarJobLastItemId = ItemPlaceId;
                }
                #endregion

                #region Pasajeros (Algoritmo replicado en ConditionCheckTimer por seguridad)
                //Vars
                string Pasajeros = Client.GetRoleplay().Pasajeros;
                string[] stringSeparators = new string[] { ";" };
                string[] result;
                result = Pasajeros.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);

                foreach (string psjs in result)
                {
                    GameClient PJ = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(psjs);
                    if (PJ != null)
                    {
                        if (PJ.GetRoleplay().ChoferName == Client.GetHabbo().Username)
                        {
                            RoleplayManager.Shout(PJ, "*Baja del vehículo de " + Client.GetHabbo().Username + "*", 5);
                        }
                        // PASAJERO
                        PJ.GetRoleplay().Pasajero = false;
                        PJ.GetRoleplay().ChoferName = "";
                        PJ.GetRoleplay().ChoferID = 0;
                        PJ.GetRoomUser().CanWalk = true;
                        PJ.GetRoomUser().FastWalking = false;
                        PJ.GetRoomUser().TeleportEnabled = false;
                        PJ.GetRoomUser().AllowOverride = false;

                        // Descontamos Pasajero
                        Client.GetRoleplay().PasajerosCount--;
                        StringBuilder builder = new StringBuilder(Client.GetRoleplay().Pasajeros);
                        builder.Replace(PJ.GetHabbo().Username + ";", "");
                        Client.GetRoleplay().Pasajeros = builder.ToString();

                        // CHOFER 
                        Client.GetRoleplay().Chofer = (Client.GetRoleplay().PasajerosCount <= 0) ? false : true;
                        Client.GetRoomUser().AllowOverride = (Client.GetRoleplay().PasajerosCount <= 0) ? false : true;

                        
                        // SI EL PASAJERO ES COMPAÑERO DE BASURERO
                        if (PJ.GetRoleplay().IsBasuPasaj)
                            PJ.GetRoleplay().IsBasuPasaj = false;

                        PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(PJ, "event_vehicle", "close");// WS FUEL
                    }
                }
                #endregion

                #endregion

                #region Online ParkVars
                //Retornamos a valores predeterminados
                Client.GetRoleplay().DrivingCar = false;
                Client.GetRoleplay().DrivingInCar = false;
                Client.GetRoleplay().DrivingCarId = 0;// Id de VehiclesOwned;

                //Combustible System
                Client.GetRoleplay().CarType = 0;// Define el gasto de combustible
                Client.GetRoleplay().CarFuel = 0;
                Client.GetRoleplay().CarMaxFuel = 0;
                Client.GetRoleplay().CarTimer = 0;
                Client.GetRoleplay().CarLife = 0;

                Client.GetRoleplay().CarEnableId = 0;//Coloca el enable para conducir
                Client.GetRoleplay().CarEffectId = 0;//Guarda el enable del último auto en conducción.
                Client.GetRoomUser().ApplyEffect(0);
                Client.GetRoomUser().FastWalking = false;
                #endregion

                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_vehicle", "close");
                #endregion
            }
            #endregion

            #region Is Pasajero?
            if (Client.GetRoleplay().Pasajero)
            {
                // PASAJERO
                Client.GetRoleplay().Pasajero = false;
                Client.GetRoleplay().ChoferName = "";
                Client.GetRoleplay().ChoferID = 0;
                Client.GetRoomUser().CanWalk = true;
                Client.GetRoomUser().FastWalking = false;
                Client.GetRoomUser().TeleportEnabled = false;
                Client.GetRoomUser().AllowOverride = false;

                GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Client.GetRoleplay().ChoferName);

                if (TargetClient != null)
                {
                    // Descontamos Pasajero
                    TargetClient.GetRoleplay().PasajerosCount--;
                    if (TargetClient.GetRoleplay().PasajerosCount <= 0)
                        TargetClient.GetRoleplay().Pasajeros = "";
                    else
                        TargetClient.GetRoleplay().Pasajeros.Replace(Client.GetHabbo().Username + ";", "");

                    // CHOFER 
                    TargetClient.GetRoleplay().Chofer = (TargetClient.GetRoleplay().PasajerosCount <= 0) ? false : true;
                    TargetClient.GetRoomUser().AllowOverride = (TargetClient.GetRoleplay().PasajerosCount <= 0) ? false : true;
                }
                RoleplayManager.Shout(Client, "*Baja del vehículo*", 5);
            }
            #endregion
        }


        public static void AssingRights(GameClient Session, int RoomId, bool ChangeOwner = false)
        {
            if (Session == null || Session.GetHabbo() == null)
                return;

            int UserId = Session.GetHabbo().Id;

            if (!GenerateRoom(RoomId, out Room Room))
                return;

            // Limpiamos Permisos de Caché
            if (Room.UsersWithRights.Count > 0)
                Room.UsersWithRights.Clear();

            // Limpiamos Expulsiones de Caché
            foreach (int Id in Room.BannedUsers().ToList())
            {
                Room.Unban(Id);
            }

            Room.UsersWithRights.Add(UserId);

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.RunQuery("DELETE FROM `room_rights` WHERE `room_id` = '" + Room.RoomId + "'");
                dbClient.RunQuery("DELETE FROM `room_bans` WHERE `room_id` = '" + Room.RoomId + "'");
                dbClient.RunQuery("UPDATE items SET user_id = '" + Session.GetHabbo().Id + "' WHERE room_id = '" + Room.RoomId + "'");

                if (ChangeOwner)
                {
                    Room.OwnerId = Session.GetHabbo().Id;
                    Room.OwnerName = Session.GetHabbo().Username;
                    dbClient.RunQuery("UPDATE rooms SET owner = '" + Session.GetHabbo().Id + "' WHERE id = '" + Room.RoomId + "'");
                }

                dbClient.RunQuery("INSERT INTO `room_rights` (`room_id`,`user_id`) VALUES ('" + Room.RoomId + "','" + UserId + "')");
            }

            RoomUser RoomUser = Room.GetRoomUserManager().GetRoomUserByHabbo(UserId);
            if (RoomUser != null && !RoomUser.IsBot)
            {
                RoomUser.SetStatus("flatctrl 1", "");
                RoomUser.UpdateNeeded = true;
                if (RoomUser.GetClient() != null)
                    RoomUser.GetClient().SendMessage(new YouAreControllerComposer(1));

                Session.SendMessage(new FlatControllerAddedComposer(Room.RoomId, RoomUser.GetClient().GetHabbo().Id, RoomUser.GetClient().GetHabbo().Username));
            }
            else
            {
                UserCache User = PolarEnvironment.GetGame().GetCacheManager().GenerateUser(UserId);
                if (User != null)
                    Session.SendMessage(new FlatControllerAddedComposer(Room.RoomId, User.Id, User.Username));
            }
            List<RoomUser> UsersToReturn = Room.GetRoomUserManager().GetRoomUsers().ToList();
            PolarEnvironment.GetGame().GetRoomManager().UnloadRoom(Room, true);

            foreach (RoomUser User in UsersToReturn)
            {
                if (User == null || User.GetClient() == null)
                    continue;

                //User.GetClient().SendMessage(new RoomForwardComposer(Room.Id));
                RoleplayManager.SendUser(User.GetClient(), Room.Id, "¡La sala ha sido comprada!");
            }
        }

        public static bool DeleteRoomRP(GameClient Session, int RoomId)
        {
            if (Session == null || Session.GetHabbo() == null || Session.GetHabbo().UsersRooms == null)
                return false;

            if (RoomId == 0)
                return false;

            Room Room;

            if (!PolarEnvironment.GetGame().GetRoomManager().TryGetRoom(RoomId, out Room))
                return false;

            RoomData data = Room.RoomData;
            if (data == null)
                return false;

            if (Room.OwnerId != Session.GetHabbo().Id && !Session.GetHabbo().GetPermissions().HasRight("room_delete_any"))
                return false;

            foreach (RoomUser RoomUser in Room.GetRoomUserManager().GetRoomUsers())
            {
                if (RoomUser == null || RoomUser.GetClient() == null)
                    continue;

                SendUser(RoomUser.GetClient(), 61, "");
            }

            List<Item> ItemsToRemove = new List<Item>();
            foreach (Item Item in Room.GetRoomItemHandler().GetWallAndFloor.ToList())
            {
                if (Item == null)
                    continue;

                if (Item.GetBaseItem().InteractionType == InteractionType.MOODLIGHT)
                {
                    using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                    {
                        dbClient.RunQuery("DELETE FROM `room_items_moodlight` WHERE `item_id` = '" + Item.Id + "' LIMIT 1");
                    }
                }

                ItemsToRemove.Add(Item);
            }

            foreach (Item Item in ItemsToRemove)
            {
                GameClient targetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Item.UserID);
                if (targetClient != null && targetClient.GetHabbo() != null)//Again, do we have an active client?
                {
                    Room.GetRoomItemHandler().RemoveFurniture(targetClient, Item.Id);
                    //targetClient.GetHabbo().GetInventoryComponent().AddNewItem(Item.Id, Item.BaseItem, Item.ExtraData, Item.GroupId, true, true, Item.LimitedNo, Item.LimitedTot);
                    //targetClient.GetHabbo().GetInventoryComponent().UpdateItems(false);
                }
                else//No, query time.
                {
                    Room.GetRoomItemHandler().RemoveFurniture(null, Item.Id);
                    using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                    {
                        dbClient.RunQuery("DELETE FROM `items` WHERE `id` = '" + Item.Id + "' LIMIT 1");
                    }
                }
            }

            PolarEnvironment.GetGame().GetRoomManager().UnloadRoom(Room, true);

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.RunQuery("DELETE FROM `user_roomvisits` WHERE `room_id` = '" + RoomId + "'");
                dbClient.RunQuery("DELETE FROM `rooms` WHERE `id` = '" + RoomId + "' LIMIT 1");
                dbClient.RunQuery("DELETE FROM `user_favorites` WHERE `room_id` = '" + RoomId + "'");
                dbClient.RunQuery("DELETE FROM `items` WHERE `room_id` = '" + RoomId + "'");
                dbClient.RunQuery("DELETE FROM `room_rights` WHERE `room_id` = '" + RoomId + "'");
                dbClient.RunQuery("DELETE FROM `rp_rooms` WHERE `id` = '" + RoomId + "' LIMIT 1");
                dbClient.RunQuery("UPDATE `users` SET `home_room` = '1' WHERE `home_room` = '" + RoomId + "'");
                dbClient.RunQuery("DELETE FROM `rp_apartments_owned` WHERE `room_id` = '" + RoomId + "' LIMIT 1");
            }

            RoomData removedRoom = (from p in Session.GetHabbo().UsersRooms where p.Id == RoomId select p).SingleOrDefault();
            if (removedRoom != null)
                Session.GetHabbo().UsersRooms.Remove(removedRoom);

            return true;
        }

        public static bool CheckHaveProduct(GameClient Session, string productname)
        {
            Product PO = ProductsManager.getProduct(productname);
            if (PO != null)
            {
                return Session.GetRoleplay().OwnedProducts.ContainsKey(PO.ID);
            }

            return false;
        }
        public static Item PutItemToRoom(GameClient Session, int ItemId, int roomid, int BaseItem, int X, int Y, int Rotation, bool ToDB = false)
        {
            if (!GenerateRoom(roomid, out Room Room, false))
                return null;

            Item RoomItem = new Item(ItemId, Room.RoomId, BaseItem, "", X, Y, 0, Rotation, /*Session.GetHabbo().Id*/0, 0, 0, 0, string.Empty, Room);
            if (ToDB)
            {
                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery($"SELECT id FROM `{Polar.Core.DatabaseCompatibility.ItemsTable}` WHERE id = '" + ItemId + "'");
                    if (dbClient.getInteger() <= 0)
                    {
                         dbClient.SetQuery($"INSERT INTO `{Polar.Core.DatabaseCompatibility.ItemsTable}` (id,user_id,room_id,{Polar.Core.DatabaseCompatibility.ItemsBaseItemColumn}) VALUES (" + ItemId + ", 0, " + roomid + ", " + BaseItem + ")");
                        dbClient.RunQuery();
                    }
                }
            }
            if (Room.GetRoomItemHandler().SetFloorItem(Session, RoomItem, X, Y, Rotation, true, false, true))
                return RoomItem;
            else
            {
                return null;
            }
        }

        public static void UpdateMyWeaponStats(GameClient Session, string stat, string newsat, string weaponname)
        {
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.RunQuery("UPDATE `rp_weapons_owned` SET `" + stat + "` = '" + newsat + "' WHERE `rp_weapons_owned`.`base_weapon` = '" + weaponname + "' AND `rp_weapons_owned`.`user_id` = " + Session.GetHabbo().Id + ";");
            }
        }
        public static void UpdateMyWeaponStats(GameClient Session, string stat, int newsat, string weaponname)
        {
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.RunQuery("UPDATE `rp_weapons_owned` SET `" + stat + "` = '" + newsat + "' WHERE `rp_weapons_owned`.`base_weapon` = '" + weaponname + "' AND `rp_weapons_owned`.`user_id` = " + Session.GetHabbo().Id + ";");
            }
        }

        public static void UpdateToWeaponBaul(GameClient Session, int Owner, int BaulID, string weaponname)
        {
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.RunQuery("UPDATE `rp_weapons_owned` SET `user_id` = '" + Session.GetHabbo().Id + "', `baul_car_id` = '0' WHERE `rp_weapons_owned`.`base_weapon` = '" + weaponname + "' AND `rp_weapons_owned`.`baul_car_id` = " + BaulID + " LIMIT 1;");
                //dbClient.RunQuery("UPDATE `play_weapons_owned` SET `user_id` = '" + Session.GetHabbo().Id + "', `baul_car_id` = '0' WHERE `play_weapons_owned`.`base_weapon` = '" + weaponname + "' AND `play_weapons_owned`.`baul_car_id` = " + BaulID + " AND `play_weapons_owned`.`user_id` = " + Owner + " LIMIT 1;");
            }
        }
        public static void UpdateToBaulWeaponBaul(GameClient Session, int BaulID, string weaponname)
        {
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.RunQuery("UPDATE `rp_weapons_owned` SET `baul_car_id` = '" + BaulID + "' WHERE `rp_weapons_owned`.`base_weapon` = '" + weaponname + "' AND `rp_weapons_owned`.`baul_car_id` = '0' AND `rp_weapons_owned`.`user_id` = " + Session.GetHabbo().Id + " LIMIT 1;");
            }
        }

        public static void DropMyWeapon(GameClient Session, string weaponname)
        {
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.RunQuery("DELETE FROM `rp_weapons_owned` WHERE `rp_weapons_owned`.`base_weapon` = '" + weaponname + "' AND `rp_weapons_owned`.`user_id` = " + Session.GetHabbo().Id + ";");
            }
        }
        public static void DropAllMyWeapon(GameClient Session)
        {
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.RunQuery("DELETE FROM `rp_weapons_owned` WHERE `rp_weapons_owned`.`user_id` = " + Session.GetHabbo().Id + ";");
            }
        }

        public static bool IsRobObject(string Obj)
        {
            bool rob = false;
            switch (Obj.ToLower())
            {
                case "reloj de estuche":
                case "maletín":
                case "guitarra":
                case "televisor moderno":
                case "bajo":
                case "televisor antiguo":
                case "nintendo 64":
                case "extintor":
                case "radio":
                case "tabla de surf":
                case "videocasetera":
                    rob = true;
                    break;

                default:
                    break;
            }

            return rob;
        }

        public static void SetPriceObject(GameClient Session, string Obj)
        {
            switch (Obj)
            {
                case "reloj de estuche":
                    Session.GetRoleplay().ObjectPrice = 150;
                    break;
                case "maletín":
                    Session.GetRoleplay().ObjectPrice = 125;
                    break;
                case "guitarra":
                    Session.GetRoleplay().ObjectPrice = 50;
                    break;
                case "televisor moderno":
                    Session.GetRoleplay().ObjectPrice = 25;
                    break;
                case "bajo":
                    Session.GetRoleplay().ObjectPrice = 38;
                    break;
                case "televisor antiguo":
                    Session.GetRoleplay().ObjectPrice = 20;
                    break;
                case "nintendo 64":
                    Session.GetRoleplay().ObjectPrice = 30;
                    break;
                case "extintor":
                    Session.GetRoleplay().ObjectPrice = 18;
                    break;
                case "radio":
                    Session.GetRoleplay().ObjectPrice = 15;
                    break;
                case "tabla de surf":
                    Session.GetRoleplay().ObjectPrice = 13;
                    break;
                case "videocasetera":
                    Session.GetRoleplay().ObjectPrice = 6;
                    break;
                default:
                    Session.GetRoleplay().ObjectPrice = 0;
                    break;
            }
        }
        public static void TakeMoneyFromCompany(int JobID, int Amount, GameClient Session = null)
        {
            if (!GroupManager.validJustJob(JobID))
                return;

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {

                dbClient.SetQuery("SELECT * FROM rp_jobs WHERE id = " + JobID + " LIMIT 1");

                DataTable Jobs = dbClient.getTable();
                if (Jobs == null || Jobs.Rows.Count == 0)
                    return;
                int LostAmount = 0;
                foreach (DataRow Job in Jobs.Rows)
                {
                    LostAmount = Convert.ToInt32(Job["bank_balance"]);
                }

                int Calc = LostAmount - Amount;
                if (Calc <= 0)
                {
                    dbClient.RunQuery("UPDATE `rp_jobs` SET bank_balance = 0 WHERE id = " + JobID + " LIMIT 1");
                }
                else
                {
                    dbClient.RunQuery("UPDATE `rp_jobs` SET bank_balance = " + Calc + " WHERE id = " + JobID + " LIMIT 1");
                }
            }
            if (Session != null)
                Session.SendWhisper("*La compañia ha gastado (" + Amount + "$), en tu nombre!*", 0);

        }

        public static void GiveMoney(GameClient TargetClient, int credits)
        {
            if (TargetClient != null)
            {
                TargetClient.GetHabbo().Credits = TargetClient.GetHabbo().Credits + credits;

                TargetClient.GetHabbo().UpdateCreditsBalance();

            }
        }

        public static void TakeMoney(GameClient TargetClient, int credits)
        {
            if (TargetClient != null)
            {
                TargetClient.GetHabbo().Credits = TargetClient.GetHabbo().Credits - credits;

                TargetClient.GetHabbo().UpdateCreditsBalance();

            }
        }

        public static void TakeMoneyCredits(GameClient TargetClient, int Duckets)
        {
            if (TargetClient != null)
            {
                TargetClient.GetHabbo().Duckets = TargetClient.GetHabbo().Duckets - Duckets;

                TargetClient.GetHabbo().UpdateDucketsBalance();

            }
        }

        public static void SaveQuickStat(GameClient Session, string Stat, string Newval)
        {
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.RunQuery("UPDATE rp_stats SET " + Stat + " = '" + Newval + "' WHERE id = " + Session.GetHabbo().Id + "");
            }
        }
        public static void SaveQuickStat(GameClient Session, string Stat, int Newval)
        {
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.RunQuery("UPDATE rp_stats SET " + Stat + " = '" + Newval + "' WHERE id = " + Session.GetHabbo().Id + "");
            }
        }
        public static void SaveQuickStat(int UserId, string Stat, string Newval, bool Query = false)
        {
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                if (!Query)
                    dbClient.RunQuery("UPDATE rp_stats SET " + Stat + " = '" + Newval + "' WHERE id = " + UserId + "");
                else
                    dbClient.RunQuery("UPDATE rp_stats SET " + Stat + " = " + Newval + " WHERE id = " + UserId + "");
            }
        }

        public static void SaveQuickStat(int UserId, string Stat, int Newval)
        {
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.RunQuery("UPDATE rp_stats SET " + Stat + " = '" + Newval + "' WHERE id = " + UserId + "");
            }
        }

        public static Room GenerateRoom(int RoomId)
        {
            if (PolarEnvironment.GetGame() == null || PolarEnvironment.GetGame().GetRoomManager() == null)
                return null;

            Room Room = PolarEnvironment.GetGame().GetRoomManager().LoadRoom(RoomId);
            return Room;
        }
        /// <summary>
        /// Generates room based on roomid
        /// </summary>
        /// <param name="RoomId"></param>
        /// <returns></returns>
        public static bool GenerateRoom(int RoomId, out Room Room)
        {
            Room = null;
            if (PolarEnvironment.GetGame() == null || PolarEnvironment.GetGame().GetRoomManager() == null)
                return false;

            return PolarEnvironment.GetGame().GetRoomManager().LoadRoom(RoomId, out Room);
        }

        public static string SplitFigure(string Look, string Outfit)
        {
            // Validar que Look no sea nulo o vacío
            if (string.IsNullOrEmpty(Look))
                Look = "hd-180-1.ch-210-62.lg-280-82.sh-290-62.cc-0-0";

            // Validar que Outfit no sea nulo
            if (string.IsNullOrEmpty(Outfit))
                Outfit = "";

            ConcurrentDictionary<string, string> NewFigure = new ConcurrentDictionary<string, string>();
            string[] Splitted = Look.Split('.');
            string[] Splitted2 = Outfit.Split('.');

            // Procesar Look
            for (int i = 0; i < Splitted.Length; i++)
            {
                // Saltar elementos vacíos
                if (string.IsNullOrEmpty(Splitted[i]))
                    continue;

                string[] SplittedPart = Splitted[i].Split('-');

                // Validar que SplittedPart tenga al menos 2 elementos
                if (SplittedPart == null || SplittedPart.Length < 2)
                    continue;

                string BodyPart = SplittedPart[0];
                string Type = SplittedPart[1];
                string Colour;

                if (SplittedPart.Length >= 3)
                    Colour = SplittedPart[2];
                else
                    Colour = "110";

                string SpecificPart = "-" + Type + "-" + Colour;

                if (!NewFigure.ContainsKey(BodyPart))
                    NewFigure.TryAdd(BodyPart, SpecificPart);
                else
                    NewFigure.TryUpdate(BodyPart, SpecificPart, NewFigure[BodyPart]);
            }

            // Procesar Outfit
            for (int i = 0; i < Splitted2.Length; i++)
            {
                // Saltar elementos vacíos
                if (string.IsNullOrEmpty(Splitted2[i]))
                    continue;

                string[] SplittedPart2 = Splitted2[i].Split('-');

                // Validar que SplittedPart2 tenga al menos 2 elementos
                if (SplittedPart2 == null || SplittedPart2.Length < 2)
                    continue;

                string BodyPart2 = SplittedPart2[0];
                string Type2 = SplittedPart2[1];
                string Colour2;

                if (SplittedPart2.Length >= 3)
                    Colour2 = SplittedPart2[2];
                else
                    Colour2 = "110";

                string SpecificPart2 = "-" + Type2 + "-" + Colour2;

                if (!NewFigure.ContainsKey(BodyPart2))
                    NewFigure.TryAdd(BodyPart2, SpecificPart2);
                else
                    NewFigure.TryUpdate(BodyPart2, SpecificPart2, NewFigure[BodyPart2]);
            }

            // Si no hay ninguna figura, devolver una por defecto
            if (NewFigure.Count == 0)
                return "hd-180-1.ch-210-62.lg-280-82.sh-290-62.cc-0-0";

            string ReturnFigure = "";
            int count = 0;
            foreach (var Row in NewFigure)
            {
                count++;

                if (NewFigure.Count == count)
                    ReturnFigure += Row.Key + Row.Value;
                else
                    ReturnFigure += Row.Key + Row.Value + ".";
            }

            return ReturnFigure;
        }
        public static int Distance(Vector2D Pos1, Vector2D Pos2)
        {
            return Math.Abs(Pos1.X - Pos2.X) + Math.Abs(Pos1.Y - Pos2.Y);
        }

        /// <summary>
        /// Gets the distance between 2 points
        /// </summary>
        public static double GetDistanceBetweenPoints2D(Point From, Point To)
        {
            Vector2D Pos1 = new Vector2D(From.X, From.Y);
            Vector2D Pos2 = new Vector2D(To.X, To.Y);

            double XDistance = Math.Abs(Pos1.X - Pos2.X);
            double YDistance = Math.Abs(Pos1.Y - Pos2.Y);

            if (XDistance == 0 && YDistance == 0)
                return 0;

            if (XDistance == 0)
                return YDistance;

            if (YDistance == 0)
                return XDistance;

            double DiagonalDistance = Math.Sqrt(XDistance * XDistance + YDistance * YDistance);

            return DiagonalDistance;
        }

        /// <summary>
        /// Generates a shout message based on paramter session
        /// </summary>
        /// <param name="Session"></param>
        /// <param name="Speech"></param>
        /// <param name="Bubble"></param>
        public static void Chat(GameClient Session, string Speech, int Bubble = 0)
        {
            Room Room = null;
            RoomUser User = null;

            if (Session == null || Session.GetHabbo() == null || Session.GetRoleplay() == null || Session.GetRoomUser() == null)
                return;

            Room = Session.GetHabbo().CurrentRoom;
            User = Session.GetRoomUser();

            if (User != null)
            {
                if (User.GetClient() != null && User.GetClient().GetHabbo() != null)
                {
                    if (Room != null)
                    {
                        if (!Room.TutorialEnabled)
                        {
                            foreach (RoomUser roomUser in Room.GetRoomUserManager().GetRoomUsers())
                            {
                                if (roomUser == null || roomUser.IsBot)
                                    continue;

                                if (roomUser.GetClient() == null)
                                    continue;

                                if (User.GetClient().GetRoleplay().Invisible)
                                    if (User.GetClient().GetHabbo().Username != roomUser.GetClient().GetHabbo().Username && !roomUser.GetClient().GetRoleplay().Invisible)
                                        continue;

                                roomUser.GetClient().SendMessage(new ChatComposer(User.VirtualId, Speech, 0, Bubble, string.Empty));
                            }
                        }
                        else
                            Session.SendMessage(new ChatComposer(User.VirtualId, Speech, 0, Bubble, string.Empty));
                    }
                }
            }
        }

        public static void UpdateVehicleState(int furni_id, int newstate)
        {
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.RunQuery("UPDATE rp_vehicles_owned SET state = '" + newstate + "' WHERE furni_id = '" + furni_id + "'");
            }
        }

        public static void UpdateVehicleBaul(int furni_id, string value, int newvalue)
        {
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.RunQuery("UPDATE rp_vehicles_owned SET " + value + " = '" + newvalue + "' WHERE furni_id = '" + furni_id + "'");
            }
        }

        public static void UpdateVehicleBaul(int furni_id, string value, string newvalue)
        {
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.RunQuery("UPDATE rp_vehicles_owned SET " + value + " = '" + newvalue + "' WHERE furni_id = '" + furni_id + "'");
            }
        }
        // Update a String
        public static void UpdateVehicleStat(int carid, string val, string newval)
        {
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.RunQuery("UPDATE rp_vehicles_owned SET " + val + " = '" + newval + "' WHERE id = '" + carid + "'");
            }
        }
        // Update a INT
        public static void UpdateVehicleStat(int carid, string val, int newval)
        {
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.RunQuery("UPDATE rp_vehicles_owned SET " + val + " = " + newval + " WHERE id = '" + carid + "'");
            }
        }
        public static bool IsMyVehicle(GameClient Session, int carid)
        {
            VehiclesOwned VO = PolarEnvironment.GetGame().GetVehiclesOwnedManager().getVehiclesOwned(carid);

            if (VO == null)
                return false;

            return (VO.OwnerId == Session.GetHabbo().Id) ? true : false;
            /*
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT `id` FROM `rp_vehicles_owned` WHERE `id` = '" + carid + "' AND `owner` = '"+ Session.GetHabbo().Id +"' LIMIT 1");
                DataRow Check = dbClient.getRow();
                if (Check == null)
                    return false;
                else
                    return true;
            }
            */
        }
        /// <summary>
        /// Generates a shout message based on paramter session
        /// </summary>
        /// <param name="Session"></param>
        /// <param name="Speech"></param>
        /// <param name="Bubble"></param>
        public static void Shout(GameClient Session, string Speech, int Bubble = 0, string Colour = "black")
        {
            Room Room = null;
            RoomUser User = null;

            if (Speech.StartsWith(""))
                Speech = "" + Char.ToLowerInvariant(Speech[1]) + Speech.Substring(2);

            if (Session == null || Session.GetHabbo() == null || Session.GetRoleplay() == null || Session.GetRoomUser() == null)
                return;

            Room = Session.GetHabbo().CurrentRoom;
            User = Session.GetRoomUser();

            if (User != null)
            {
                if (User.GetClient() != null && User.GetClient().GetHabbo() != null)
                {
                    if (Room != null)
                    {
                        if (!Room.TutorialEnabled)
                        {
                            //User.SendNameColourPacket();
                            //User.SendMeCommandPacket();

                            foreach (RoomUser roomUser in Room.GetRoomUserManager().GetRoomUsers())
                            {
                                if (roomUser == null || roomUser.IsBot)
                                    continue;

                                if (roomUser.GetClient() == null)
                                    continue;

                                if (User.GetClient().GetRoleplay().Invisible)
                                    if (User.GetClient().GetHabbo().Username != roomUser.GetClient().GetHabbo().Username && !roomUser.GetClient().GetRoleplay().Invisible)
                                        continue;

                                roomUser.GetClient().SendMessage(new ShoutComposer(User.VirtualId, Speech, 0, Bubble, Colour));
                            }
                        }
                        else
                        {
                            //User.SendNameColourPacket();
                           // User.SendMeCommandPacket();

                            Session.SendMessage(new ShoutComposer(User.VirtualId, Speech, 0, Bubble, Colour));
                        }
                    }
                }
                //User.SendNamePacket();
            }
        }


        /// <summary>
        /// Generates room based on roomid
        /// </summary>
        /// <param name="RoomId"></param>
        /// <returns></returns>
        public static bool GenerateRoom(int RoomId, out Room Room, bool BotCheck = false)
        {
            Room = null;
            if (PolarEnvironment.GetGame() == null || PolarEnvironment.GetGame().GetRoomManager() == null)
                return false;

            return PolarEnvironment.GetGame().GetRoomManager().LoadRoom(RoomId, out Room);
        }

        /// <summary>
        /// Gets the look of the user
        /// </summary>
        /// <param name="Client"></param>
        /// <returns></returns>
        public static void GetLookAndMotto(GameClient Client, string Type = "")
        {
            try
            {
                // Validar que Client y Habbo existan
                if (Client == null || Client.GetHabbo() == null)
                    return;

                // Validar que Look no esté vacío
                string Look = Client.GetHabbo().Look;
                if (string.IsNullOrEmpty(Look))
                {
                    Look = "hd-180-1.ch-210-62.lg-280-82.sh-290-62.cc-0-0";
                    Client.GetHabbo().Look = Look;
                }

                string WorkLook = "";
                string Motto = Client.GetHabbo().Motto;

                if (Client.GetHabbo().Gender == null || Client.GetHabbo().Gender == "")
                    Client.GetHabbo().Gender = "m";
                string Gender = Client.GetHabbo().Gender;

                if (Type.ToLower() == "poof")
            {
                Look = Client.GetRoleplay().OriginalOutfit;
                Motto = Client.GetRoleplay().OriginalMotto;
            }

            int JobId = Client.GetRoleplay().JobId;
            int JobRank = Client.GetRoleplay().JobRank;

            Group Group = GroupManager.GetJob(JobId);
            GroupRank GroupRank = GroupManager.GetJobRank(JobId, JobRank);

            if (Client.GetRoleplay().IsDead)
            {
                if (Gender.ToLower() == "m")
                    Look = SplitFigure(Look, "lg-280-83.ch-215-83");

                if (Gender.ToLower() == "f")
                    Look = SplitFigure(Look, "lg-710-83.ch-635-83");

                Motto = "[EN COMA] " + Client.GetRoleplay().Class;
            }

            if (Client.GetRoleplay().IsJailed)
            {
                Random Random = new Random();
                int PrisonNumber = Random.Next(11111, 100000);

                if (Gender.ToLower() == "m")
                    Look = SplitFigure(Look, "ch-3688-94.sh-300-1.hd-180-1.lg-9587209-94-64");

                if (Gender.ToLower() == "f")
                    Look = SplitFigure(Look, "ch-3689-94.lg-9587209-94-64.sh-735-92.hd-600-1");

                /*if (Client.GetRoleplay().Jailbroken)
                    Motto = "[ESCAPADO] Prisionero [#" + PrisonNumber + "]";
                else
                    */Motto = "[PRISIONERO] Prisionero [#" + PrisonNumber + "]";
            }

            if (Client.GetRoleplay().IsWorking)
            {
                if (Client.GetRoleplay().JobId != 15)
                {
                    if (Gender.ToLower() == "m" && GroupRank.MaleFigure != "")
                        WorkLook = GroupRank.MaleFigure;

                    if (Gender.ToLower() == "f" && GroupRank.FemaleFigure != "")
                        WorkLook = GroupRank.FemaleFigure;

                    Look = SplitFigure(Look, WorkLook);
                }
                Motto = "[TRABAJANDO] " + Group.Name + " " + GroupRank.Name;
            }

            if (Client.GetRoleplay().SexTimer > 0)
            {
                if (Gender.ToLower() == "m")  
                    Look = SplitFigure(Look, "lg-7218322-79.ch-3203-153638.-180-7");

                if (Gender.ToLower() == "f")
                    Look = SplitFigure(Look, "ch-3135-1320.lg-7218322-66.-600-1");
            }

            if (Client.GetRoleplay().ChalecoPor > 0)
            {
                if (Gender.ToLower() == "m")
                    Look = SplitFigure(Look, "cc-3420-1408");

                if (Gender.ToLower() == "f")
                    Look = SplitFigure(Look, "cc-3420-1408");
            }

            if (Client.GetRoleplay().Embarazo > 0)
            {
                if (Gender.ToLower() == "f")
                    Look = SplitFigure(Look, "ch-6050578-73.sh-730-92.hd-3096-1.lg-827-92");
                Motto = "[EMBARAZADA]";
            }

            Client.SendMessage(new AvatarAspectUpdateComposer(Look, Gender));

            var RoomUser = Client.GetRoomUser();
            if (RoomUser != null)
            {
                Client.GetHabbo().Look = Look;
                if (RoomUser.IsAsleep)
                    Client.GetHabbo().Motto = "[DORMIDO] " + Motto;
                else
                    Client.GetHabbo().Motto = Motto;

                Client.SendMessage(new UserChangeComposer(RoomUser, true));

                if (Client.GetHabbo().CurrentRoom != null)
                    Client.GetHabbo().CurrentRoom.SendMessage(new UserChangeComposer(RoomUser, false));
            }
            }
            catch (Exception ex)
            {
                Logging.LogException("GetLookAndMotto error: " + ex.ToString());
            }
        }
        /// <summary>
        /// Sends the user to desired bed in the room
        /// </summary>
        /// <param name="Client"></param>
        public static void SpawnBeds(GameClient Client, string BedName, RoomUser Bot = null)
        {
            RoomUser RoomUser;

            if (Bot != null)
                RoomUser = Bot;
            else
                RoomUser = Client.GetRoomUser();

            List<Item> Beds = new List<Item>();

            if (RoomUser == null)
                return;

            if (RoomUser.isSitting || RoomUser.Statusses.ContainsKey("sit"))
            {
                if (RoomUser.Statusses.ContainsKey("sit"))
                    RoomUser.RemoveStatus("sit");
                RoomUser.isSitting = false;
                RoomUser.UpdateNeeded = true;
            }

            if (RoomUser.isLying || RoomUser.Statusses.ContainsKey("lay"))
            {
                if (RoomUser.Statusses.ContainsKey("lay"))
                    RoomUser.RemoveStatus("lay");
                RoomUser.isLying = false;
                RoomUser.UpdateNeeded = true;
            }

            if (RoomUser != null)
                RoomUser.ClearMovement(true);

            lock (RoomUser.GetRoom().GetRoomItemHandler().GetFloor)
            {
                foreach (Item item in RoomUser.GetRoom().GetRoomItemHandler().GetFloor)
                {
                    if (item.GetBaseItem().ItemName == BedName)
                    {
                        if (!Beds.Contains(item))
                            Beds.Add(item);
                    }
                }

                var Beds2 = new List<Item>();
                foreach (var bed in Beds)
                {
                    if (!bed.GetRoom().GetGameMap().SquareHasUsers(bed.GetX, bed.GetY))
                    {
                        if (!Beds2.Contains(bed))
                            Beds2.Add(bed);
                    }
                }
                Item LandItem = null;
                Random Random = new Random();
                if (Beds2.Count >= 1)
                {
                    if (Beds2.Count == 1)
                        LandItem = Beds2[0];
                    else
                        LandItem = Beds2[Random.Next(0, Beds2.Count)];
                }
                else if (Beds.Count >= 1)
                {
                    if (Beds.Count == 1)
                        LandItem = Beds[0];
                    else
                        LandItem = Beds[Random.Next(0, Beds.Count)];
                }

                if (LandItem != null)
                {
                    if (RoomUser.Statusses.ContainsKey("sit"))
                        RoomUser.RemoveStatus("sit");
                    if (RoomUser.Statusses.ContainsKey("lay"))
                        RoomUser.RemoveStatus("lay");
                    RoomUser.Statusses.Add("lay", Utilities.TextHandling.GetString(LandItem.GetBaseItem().Height) + " null");

                    Point OldCoord = new Point(RoomUser.X, RoomUser.Y);
                    Point NewCoord = new Point(LandItem.GetX, LandItem.GetY);


                    RoomUser.X = LandItem.GetX;
                    RoomUser.Y = LandItem.GetY;
                    RoomUser.Z = LandItem.GetZ;
                    RoomUser.RotHead = LandItem.Rotation;
                    RoomUser.RotBody = LandItem.Rotation;

                    RoomUser.UpdateNeeded = true;
                    RoomUser.GetRoom().GetGameMap().UpdateUserMovement(OldCoord, NewCoord, RoomUser);
                }
            }
        }

        /// <summary>
        /// Sends the user to desired chair in the room
        /// </summary>
        /// <param name="Client"></param>
        public static void SpawnChairs(GameClient Client, string ChairName, RoomUser Bot = null, Room room = null)
        {
            try
            {
                RoomUser RoomUser;

                if (Client != null)
                {
                    string MyCity = room.City;

                    HabboRoleplay.RPRoom.RPRoom Data;
                    int CourtRID = PolarEnvironment.GetGame().GetRPRoomManager().TryToGetCourt(MyCity, out Data);

                    if (Client.GetHabbo().CurrentRoomId != CourtRID)
                    {
                        Client.GetHabbo().Look = Client.GetRoleplay().OriginalOutfit;
                        //Client.GetHabbo().Motto = Client.GetRoleplay().Class;
                        Client.GetHabbo().Poof(false);
                    }
                }

                if (Bot != null)
                    RoomUser = Bot;
                else
                    RoomUser = Client.GetRoomUser();

                List<Item> Chairs = new List<Item>();

                if (RoomUser == null)
                    return;

                if (RoomUser.isSitting || RoomUser.Statusses.ContainsKey("sit"))
                {
                    if (RoomUser.Statusses.ContainsKey("sit"))
                        RoomUser.RemoveStatus("sit");
                    RoomUser.isSitting = false;
                    RoomUser.UpdateNeeded = true;
                }

                if (RoomUser.isLying || RoomUser.Statusses.ContainsKey("lay"))
                {
                    if (RoomUser.Statusses.ContainsKey("lay"))
                        RoomUser.RemoveStatus("lay");
                    RoomUser.isLying = false;
                    RoomUser.UpdateNeeded = true;
                }

                if (RoomUser != null)
                    RoomUser.ClearMovement(true);

                lock (RoomUser.GetRoom().GetRoomItemHandler().GetFloor)
                {
                    foreach (Item item in RoomUser.GetRoom().GetRoomItemHandler().GetFloor)
                    {
                        if (item.GetBaseItem().ItemName == ChairName)
                        {
                            if (!Chairs.Contains(item))
                                Chairs.Add(item);
                        }
                    }

                    var Chairs2 = new List<Item>();
                    foreach (var bed in Chairs)
                    {
                        if (!bed.GetRoom().GetGameMap().SquareHasUsers(bed.GetX, bed.GetY))
                        {
                            if (!Chairs2.Contains(bed))
                                Chairs2.Add(bed);
                        }
                    }

                    Item LandItem = null;
                    Random Random = new Random();
                    if (Chairs2.Count >= 1)
                    {
                        if (Chairs2.Count == 1)
                            LandItem = Chairs2[0];
                        else
                            LandItem = Chairs2[Random.Next(0, Chairs2.Count)];
                    }
                    else if (Chairs.Count >= 1)
                    {
                        if (Chairs.Count == 1)
                            LandItem = Chairs[0];
                        else
                            LandItem = Chairs[Random.Next(0, Chairs.Count)];
                    }

                    if (LandItem != null)
                    {
                        if (RoomUser.Statusses.ContainsKey("sit"))
                            RoomUser.RemoveStatus("sit");
                        if (RoomUser.Statusses.ContainsKey("lay"))
                            RoomUser.RemoveStatus("lay");
                        RoomUser.Statusses.Add("sit", Utilities.TextHandling.GetString(LandItem.GetBaseItem().Height) + " null");

                        Point OldCoord = new Point(RoomUser.X, RoomUser.Y);
                        Point NewCoord = new Point(LandItem.GetX, LandItem.GetY);

                        RoomUser.X = LandItem.GetX;
                        RoomUser.Y = LandItem.GetY;
                        RoomUser.Z = LandItem.GetZ;
                        RoomUser.RotHead = LandItem.Rotation;
                        RoomUser.RotBody = LandItem.Rotation;

                        RoomUser.GetRoom().GetGameMap().UpdateUserMovement(OldCoord, NewCoord, RoomUser);
                    }
                    //RoomUser.CanWalk = true;
                    //RoomUser.UpdateNeeded = true;
                }
            }
            catch { }
        }

        public static void UpdateWeapon(GameClient Client, string Weapon, int newstate)
        {
            if (!Client.GetRoleplay().OwnedWeapons.ContainsKey(Weapon.ToLower()))
            {
                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.RunQuery("UPDATE rp_weapons_owned SET effectid = '" + newstate + "' WHERE base_weapon = '" + Weapon + "' AND user_id  = '" + Client.GetHabbo().Id + "'");
                }
            }
        }

        /// <summary>
        /// Adds a weapon to the users owned weapons
        /// </summary>
        /// <returns></returns>
        public static void AddWeapon(GameClient Client, Weapon Weapon)
        {
            if (!Client.GetRoleplay().OwnedWeapons.ContainsKey(Weapon.Name.ToLower()))
            {
                using (var DB = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    DB.SetQuery("INSERT INTO `rp_weapons_owned` (`user_id`,`base_weapon`,`name`,`min_damage`,`max_damage`,`range`,`clip`,`effectid`) VALUES (@userid,@baseweapon,@name,@mindamage,@maxdamage,@range,@clip,@effectId)");
                    DB.AddParameter("userid", Client.GetHabbo().Id);
                    DB.AddParameter("baseweapon", Weapon.Name.ToLower());
                    DB.AddParameter("name", Weapon.PublicName);
                    DB.AddParameter("mindamage", Weapon.MinDamage);
                    DB.AddParameter("maxdamage", Weapon.MaxDamage);
                    DB.AddParameter("range", Weapon.Range);
                    DB.AddParameter("clip", Weapon.ClipSize);
                    DB.AddParameter("effectId", 0);
                    DB.RunQuery();

                    Client.GetRoleplay().OwnedWeapons.TryAdd(Weapon.Name.ToLower(), Weapon);
                }
            }
            else
            {
                Client.SendWhisper("Ya tienes un " + Weapon.PublicName + "", 1);
            }
            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_item", "update");
        }

        public static void AddWizard(GameClient Client, Hechizos Wizard)
        {

                using (var DB = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    DB.SetQuery("INSERT INTO `rp_hechizos_owned` (`user_id`,`base_wizard`,`name`,`power`,`firingrange`,`shields`,`firingdamage`,`health`) VALUES (@userid,@baseweapon,@name,@power,@frange,@shields,@fdamage,@health)");
                    DB.AddParameter("userid", Client.GetHabbo().Id);
                    DB.AddParameter("baseweapon", Wizard.Name.ToLower());
                    DB.AddParameter("name", Wizard.PublicName);
                    DB.AddParameter("power", Wizard.Power);
                    DB.AddParameter("frange", Wizard.FiringRange);
                    DB.AddParameter("shields", Wizard.Shields);
                    DB.AddParameter("fdamage", Wizard.FiringDamage);
                    DB.AddParameter("health", Wizard.Health);
                    DB.RunQuery();

                    Client.GetRoleplay().OwnedHechizos.TryAdd(Wizard.Name.ToLower(), Wizard);
                }
  
            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_item", "update");
        }

        public static void SendUserTimer(GameClient Client, int RID, string Message = "", string Timer = "")
        {
            RoomData roomData = PolarEnvironment.GetGame().GetRoomManager().GenerateRoomData(RID);
            if (Client != null && roomData != null)
            {
                
                if (Client.GetHabbo().InRoom)
                {
                    Room OldRoom = null;
                    if (!PolarEnvironment.GetGame().GetRoomManager().TryGetRoom(Client.GetHabbo().CurrentRoomId, out OldRoom))
                        return;

                    if (OldRoom.GetRoomUserManager() != null)
                        OldRoom.GetRoomUserManager().RemoveUserFromRoom(Client, false, false);
                }

                Client.SendMessage(new GetGuestRoomResultComposer(Client, roomData, false, true));

                if (Message != "")
                    Client.SendNotification(Message);

                if (Timer != "" && Timer != null)
                {
                    Client.GetRoleplay().TimerManager.CreateTimer(Timer, 1000, true);
                }
            }
            else
            {
                Client.SendNotification("[Error][101] -> Lamentablemente ha habido un error al mandarte a la zona solicitada, porfavor comunicate con el administrador/dueño del servidor explicando con detalles de lo ocurrido. ¡Gracias!");
                return;
            }
        }
        public static void JobSkills(GameClient Session, int JobId, int JobLvl, int JobXp)
        {
            if (!GroupManager.JobExists(JobId, 1))
                return;

            Group Job = null;

            PolarEnvironment.GetGame().GetGroupManager().TryGetGroup(JobId, out Job);

            if (Job == null)
                return;

            if (Job.GType != 2)
                return;

            string Type = "";

            #region Get Job Info
            if (Job.Name.Contains("Camionero"))
            {
                Type = "Camionero";
            }
            else if (Job.Name.Contains("Fábrica de Armas"))
            {
                Type = "Armero";
            }
            else if (Job.Name.Contains("Mecánico"))
            {
                Type = "Mecánico";
            }
            else if (Job.Name.Contains("Basurero"))
            {
                Type = "Basurero";
            }
            else if (Job.Name.Contains("Ladrón"))
            {
                Type = "Ladrón";
            }
            else if (Job.Name.Contains("Minero"))
            {
                Type = "Minero";
            }
            #endregion

            switch (Type)
            {
                #region Camionero
                case "Camionero":
                    if (JobXp < 50)
                    {
                        if (Session.GetHabbo().VIPRank > 0)
                        {
                            Session.GetRoleplay().CamXP += 1;
                            Session.SendWhisper("Has ganado +1 de Habilidad de Camionero", 1);
                        }
                        else
                        {
                            Random rnd = new Random();
                            int Prob = rnd.Next(0, 101);
                            if (Prob <= 50)
                            {
                                Session.GetRoleplay().CamXP += 1;
                                Session.SendWhisper("Has ganado +1 de Habilidad de Camionero", 1);
                            }
                        }
                    }
                    else
                    {
                        Session.GetRoleplay().CamXP = 0;
                        Session.GetRoleplay().CamLvl += 1;
                        Session.SendWhisper("¡Enhorabuena! Has subido a nivel " + Session.GetRoleplay().CamLvl + " de Camionero. ¡Ahora se te pagará más! ((Usa ':habilidades' para más información))", 1);
                    }
                    break;
                #endregion

                #region Armero
                case "Armero":
                    if (JobXp < 50)
                    {
                        // Caso especial temporal
                        if (Session.GetHabbo().Id == 0)// Nunca entrará
                        {
                            Session.GetRoleplay().ArmXP += 5;
                            Session.SendWhisper("Has ganado +5 de Habilidad de Armero bebesini. Lov U |", 1);
                        }
                        else
                        {
                            if (Session.GetHabbo().VIPRank > 0)
                            {
                                Session.GetRoleplay().ArmXP += 1;
                                Session.SendWhisper("Has ganado +1 de Habilidad de Armero", 1);
                            }
                            else
                            {
                                Random rnd = new Random();
                                int Prob = rnd.Next(0, 101);

                                if (Prob <= 50)
                                {
                                    Session.GetRoleplay().ArmXP += 1;
                                    Session.SendWhisper("Has ganado +1 de Habilidad de Armero", 1);
                                }
                            }
                        }
                    }
                    else
                    {
                        Session.GetRoleplay().ArmXP = 0;
                        Session.GetRoleplay().ArmLvl += 1;
                        Session.SendWhisper("¡Enhorabuena! Has subido a nivel " + Session.GetRoleplay().ArmLvl + " de Armero. ¡Ahora podrás Fabricar más tipos de Armas! ((Usa ':habilidades' para más información))", 1);
                    }
                    break;
                #endregion
               
                #region Mecánico
                case "Mecanico":
                    if (JobXp < 50)
                    {
                        if (Session.GetHabbo().VIPRank > 0)
                        {
                            Session.GetRoleplay().MecXP += 1;
                            Session.SendWhisper("Has ganado +1 de Habilidad de Mecánico", 1);
                        }
                        else
                        {
                            Random rnd = new Random();
                            int Prob = rnd.Next(0, 101);
                            if (Prob <= 50)
                            {
                                Session.GetRoleplay().MecXP += 1;
                                Session.SendWhisper("Has ganado +1 de Habilidad de Mecánico", 1);
                            }
                        }
                    }
                    else
                    {
                        Session.GetRoleplay().MecXP = 0;
                        Session.GetRoleplay().MecLvl += 1;
                        Session.SendWhisper("¡Enhorabuena! Has subido a nivel " + Session.GetRoleplay().MecLvl + " de Mecánico. ¡Ahora repararás más rápido! ((Usa ':habilidades' para más información))", 1);
                    }
                    break;
                #endregion

                #region Basurero
                case "Basurero":
                    if (JobXp < 50)
                    {
                        if (Session.GetHabbo().VIPRank > 0)
                        {
                            Session.GetRoleplay().BasuXP += 1;
                            Session.SendWhisper("Has ganado +1 de Habilidad de Basurero", 1);
                        }
                        else
                        {
                            Random rnd = new Random();
                            int Prob = rnd.Next(0, 101);
                            if (Prob <= 50)
                            {
                                Session.GetRoleplay().BasuXP += 1;
                                Session.SendWhisper("Has ganado +1 de Habilidad de Basurero", 1);
                            }
                        }
                    }
                    else
                    {
                        Session.GetRoleplay().BasuXP = 0;
                        Session.GetRoleplay().BasuLvl += 1;
                        Session.SendWhisper("¡Enhorabuena! Has subido a nivel " + Session.GetRoleplay().BasuLvl + " de Basurero. ¡Ahora se te pagará más y recolectarás más rápido! ((Usa ':habilidades' para más información))", 1);
                    }
                    break;
                #endregion

     

                #region Default
                default:
                    break;
                    #endregion
            }
        }

        public static void NoJobSkills(GameClient Session, string Job, int JobLvl, int JobXp)
        {
            switch (Job.ToLower())
            {
                #region Ladrón
                case "ladron":
                    
                    break;
                #endregion

                #region Default
                default:
                    break;
                    #endregion
            }
        }

        public static void SendUserOld(GameClient Client, int RID, string Message = "")
        {
            RoomData roomData = PolarEnvironment.GetGame().GetRoomManager().GenerateRoomData(RID);
            if (Client != null && roomData != null)
            {
                Client.GetRoleplay().AntiArrowCheck = true;

                if (Client.GetHabbo().InRoom)
                {
                    Room OldRoom = null;
                    if (!PolarEnvironment.GetGame().GetRoomManager().TryGetRoom(Client.GetHabbo().CurrentRoomId, out OldRoom))
                        return;

                    if (OldRoom.GetRoomUserManager() != null)
                        OldRoom.GetRoomUserManager().RemoveUserFromRoom(Client, false, false);

                }

                Client.SendMessage(new GetGuestRoomResultComposer(Client, roomData, false, true));
                Client.SendMessage(new GetGuestRoomResultComposer(Client, roomData, true, false));

                if (!string.IsNullOrEmpty(Message))
                    Client.SendNotification(Message);
            }
            else
            {
                Client.SendNotification("[Error][100] -> Lamentablemente ha habido un error al mandarte a la zona solicitada, porfavor comunicate con el administrador/dueño del servidor explicando con detalles de lo ocurrido. ¡Gracias!");
                return;
            }
        }

        public static void SendUserOld2(GameClient Client, int RID, string Message = "")
        {
            RoomData roomData = PolarEnvironment.GetGame().GetRoomManager().GenerateRoomData(RID);

            if (Client != null && roomData != null)
            {
                Client.GetRoleplay().AntiArrowCheck = true;

                if (Client.GetHabbo().InRoom)
                {
                    Room OldRoom = null;
                    if (!PolarEnvironment.GetGame().GetRoomManager().TryGetRoom(Client.GetHabbo().CurrentRoomId, out OldRoom))
                        return;

                    if (OldRoom.GetRoomUserManager() != null)
                        OldRoom.GetRoomUserManager().RemoveUserFromRoom(Client, false, false);
                }

                Client.SendMessage(new GetGuestRoomResultComposer(Client, roomData, false, true));

                if (Message != "")
                    Client.SendMessage(new MOTDNotificationComposer(Message));
            }
            else
                return;
        }
        public static void SendUserNew2(GameClient Client, int RID, string Message = "")
        {
            RoomData roomData = PolarEnvironment.GetGame().GetRoomManager().GenerateRoomData(RID);

            if (Client != null && roomData != null)
            {
                Client.GetRoleplay().AntiArrowCheck = true;

                if (Client.GetHabbo().InRoom)
                {
                    Room OldRoom = null;
                    if (!PolarEnvironment.GetGame().GetRoomManager().TryGetRoom(Client.GetHabbo().CurrentRoomId, out OldRoom))
                        return;

                    if (OldRoom.GetRoomUserManager() != null)
                        OldRoom.GetRoomUserManager().RemoveUserFromRoom(Client, false, false);
                }

                Client.SendMessage(new GetGuestRoomResultComposer(Client, roomData, false, true));
                Client.SendMessage(new GetGuestRoomResultComposer(Client, roomData, true, false));

                if (Message != "")
                    Client.SendMessage(new MOTDNotificationComposer(Message));
            }
            else
                return;
        }
        public static void SendUserNew(GameClient Client, int RID, string Message = "")
{
            RoomData roomData = PolarEnvironment.GetGame().GetRoomManager().GenerateRoomData(RID);
            if (Client == null || roomData == null)
            {
                Client?.SendNotification("[Error][100] -> Lamentablemente ha habido un error...");
                return;
            }

            Client.GetRoleplay().AntiArrowCheck = true;

            // --- SALIDA DE LA SALA ACTUAL ---
            if (Client.GetHabbo().InRoom)
            {
                if (PolarEnvironment.GetGame().GetRoomManager().TryGetRoom(Client.GetHabbo().CurrentRoomId, out Room oldRoom))
                {
                    oldRoom.GetRoomUserManager().RemoveUserFromRoom(Client, false, false);
                }
            }

            // --- ENTRADA A NUEVA SALA ---
            Client.SendMessage(new GetGuestRoomResultComposer(Client, roomData, false, true));
            Client.SendMessage(new GetGuestRoomResultComposer(Client, roomData, true, false));

            if (!PolarEnvironment.GetGame().GetRoomManager().LoadRoom(RID, out Room newRoom))
            {
                Client.SendMessage(new CloseConnectionComposer());
                return;
            }

            if (newRoom.isCrashed)
            {
                Client.SendNotification("Esta sala está corrompida :(");
                Client.SendMessage(new CloseConnectionComposer());
                return;
            }

            if (!newRoom.GetRoomUserManager().AddAvatarToRoom(Client))
            {
                newRoom.GetRoomUserManager().RemoveUserFromRoom(Client, false, false);
                return;
            }

            Client.GetHabbo().CurrentRoomId = newRoom.RoomId;
            Client.GetHabbo().HomeRoom = newRoom.Id;
            Client.GetRoleplay().InState = false;
            Client.GetRoleplay().RoomEntryHandled = true;

            // --- PROPIEDADES VISUALES ---
            if (newRoom.Wallpaper != "0.0")
                Client.SendMessage(new RoomPropertyComposer("wallpaper", newRoom.Wallpaper));
            if (newRoom.Floor != "0.0")
                Client.SendMessage(new RoomPropertyComposer("floor", newRoom.Floor));
            Client.SendMessage(new RoomPropertyComposer("landscape", newRoom.Landscape));

            if (newRoom.OwnerId != Client.GetHabbo().Id)
                Client.GetHabbo().GetStats().RoomVisits++;

            // ✅ FIX: EL MAPA YA DEBE ESTAR GENERADO DESDE QUE SE CARGÓ LA SALA
            // No llamar a GenerateMaps() aquí (ya se generó en LoadRoom o en cambios de items)
            // Simplemente enviamos los objetos al cliente
            newRoom.SendObjects(Client);

            #region Tutorial Step Check
            if (Client.GetRoleplay().TutorialStep == 13 && newRoom.WardrobeEnabled && newRoom.Type.Equals("public"))
            PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(Client, "compose_tutorial|13");
        else if (Client.GetRoleplay().TutorialStep == 18 && newRoom.PhoneStoreEnabled && newRoom.Type.Equals("public"))
            PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(Client, "compose_tutorial|18");
        else if (Client.GetRoleplay().TutorialStep == 23 && newRoom.BuyCarEnabled && newRoom.Type.Equals("public"))
            PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(Client, "compose_tutorial|24");
        else if (Client.GetRoleplay().TutorialStep == 27 && newRoom.MallEnabled && newRoom.Type.Equals("public"))
            PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(Client, "compose_tutorial|28");
        #endregion

        Client.SendMessage(new RoomVisualizationSettingsComposer(newRoom.WallThickness, newRoom.FloorThickness, newRoom.Hidewall));

        RoomUser ThisUser = null;
        if (Client.GetHabbo() != null)
            ThisUser = newRoom.GetRoomUserManager().GetRoomUserByHabbo(Client.GetHabbo().Username);

        if (ThisUser != null && Client.GetHabbo().PetId == 0)
                newRoom.SendMessage(new UserChangeComposer(ThisUser, false));

        if (newRoom.GetWired() != null)
                newRoom.GetWired().TriggerEvent(WiredBoxType.TriggerRoomEnter, Client.GetHabbo());

        foreach (RoomUser Bot in newRoom.GetRoomUserManager().GetBots().ToList())
        {
            if (Bot.IsBot || Bot.IsPet)
                Bot.BotAI.OnUserEnterRoom(ThisUser);
        }

        if (PolarEnvironment.GetUnixTimestamp() < Client.GetHabbo().FloodTime && Client.GetHabbo().FloodTime != 0)
            Client.SendMessage(new FloodControlComposer(
                (int)Client.GetHabbo().FloodTime - (int)PolarEnvironment.GetUnixTimestamp()));

        if (Client.GetHabbo().CurrentRoom == null)
            Client.GetRoleplay().IsWorking = false;

        if (Client.GetRoleplay().IsWorking)
        {
            int JobId = Client.GetRoleplay().JobId;
            int JobRank = Client.GetRoleplay().JobRank;

            if (!GroupManager.GetJobRank(JobId, JobRank).CanWorkHere(newRoom.Id))
            {
                if (GroupManager.HasJobCommand(Client, "guide"))
                {
                    var guideManager = PolarEnvironment.GetGame().GetGuideManager();
                    guideManager.RemoveGuide(Client);

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
                    else
                        Client.SendMessage(new HelperToolConfigurationComposer(Client));
                }
                WorkManager.RemoveWorkerFromList(Client);
                Client.GetRoleplay().IsWorking = false;
                Client.GetHabbo().Poof();
            }
        }

        PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_group", "close");
        PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_group", "open");
        PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_gang", "turf_cap_off");

        if (Client.GetRoleplay().ViewProducts)
        {
            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_products", "close");
            Client.GetRoleplay().ViewProducts = false;
        }

        if (Client.GetRoleplay().ViewChangeName)
        {
            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_changename", "close");
            Client.GetRoleplay().ViewChangeName = false;
        }

        if (Client.GetRoleplay().ViewCarList)
        {
            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_vehicle", "closeshop");
            Client.GetRoleplay().ViewCarList = false;
        }

        if (Client.GetRoleplay().ViewWeaponsList)
        {
            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_shop", "closeshop");
            Client.GetRoleplay().ViewWeaponsList = false;
        }

        if (Client.GetRoleplay().ViewApartments)
        {
            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_apart", "apart_close");
            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_apart", "close");
            Client.GetRoleplay().ViewApartments = false;
        }

        if (Client.GetRoleplay().Pasajero == true)
        {
            GameClient Chofer = PolarEnvironment.GetGame().GetClientManager()
                .GetClientByUsername(Client.GetRoleplay().ChoferName);
            if (Chofer != null && Client.GetRoomUser() != null)
            {
                Chofer.SendMessage(new UserRemoveComposer(Client.GetRoomUser().VirtualId));
                Client.SendMessage(new UserRemoveComposer(Client.GetRoomUser().VirtualId));
            }
        }

        if (Client.GetRoomUser() != null)
        {
            if (Client.GetRoomUser().CurrentEffect == 23)
                Client.GetRoomUser().ApplyEffect(0);
        }


        if (!string.IsNullOrEmpty(Message))
            Client.SendMessage(new MOTDNotificationComposer(Message));
}
        public static void SendUser(GameClient Client, int RID, string Message = "")
        {
            RoomData roomData = PolarEnvironment.GetGame().GetRoomManager().GenerateRoomData(RID);

            if (Client != null && roomData != null)
            {
                Client.GetRoleplay().AntiArrowCheck = true;

                if (Client.GetHabbo().InRoom)
                {
                    Room OldRoom = null;
                    if (!PolarEnvironment.GetGame().GetRoomManager().TryGetRoom(Client.GetHabbo().CurrentRoomId, out OldRoom))
                        return;

                    if (OldRoom.GetRoomUserManager() != null)
                        OldRoom.GetRoomUserManager().RemoveUserFromRoom(Client, false, false);

                    // New JDN
                    OldRoom.GetRoomItemHandler().ClearItems(Client);
                    OldRoom.GetRoomUserManager().ClearUsers(Client);
                }

                Client.SendMessage(new GetGuestRoomResultComposer(Client, roomData, true, false));

                // New JDN

                #region Habbo=>PrepareRoom
            Room Room;
            if (!PolarEnvironment.GetGame().GetRoomManager().LoadRoom(RID, out Room))
                {
                    Client.SendMessage(new CloseConnectionComposer());
                    return;
                }

                if (Room.isCrashed)
                {
                    Client.SendNotification("Esta sala está corrompida :(");
                    Client.SendMessage(new CloseConnectionComposer());
                    return;
                }

                if (!Room.GetRoomUserManager().AddAvatarToRoom(Client))
                {
                    Room.GetRoomUserManager().RemoveUserFromRoom(Client, false, false);
                    return;
                }
                Client.GetHabbo().CurrentRoomId = Room.RoomId;
                #endregion
                
                Client.GetHabbo().HomeRoom = Room.Id;
                Client.GetRoleplay().InState = false;

                #region Habbo=>EntrerRoom
                if (Room.Wallpaper != "0.0")
                    Client.SendMessage(new RoomPropertyComposer("wallpaper", Room.Wallpaper));
                if (Room.Floor != "0.0")
                    Client.SendMessage(new RoomPropertyComposer("floor", Room.Floor));

                Client.SendMessage(new RoomPropertyComposer("landscape", Room.Landscape));


                if (Room.OwnerId != Client.GetHabbo().Id)
                    Client.GetHabbo().GetStats().RoomVisits += 1;

                #endregion

                Room.GetGameMap().GenerateMaps();
                Room.SendObjects(Client);

                #region GetRoomEntryDataEvent

                Client.SendMessage(new RoomVisualizationSettingsComposer(Room.WallThickness, Room.FloorThickness, Room.Hidewall));

                RoomUser ThisUser = null;

                if (Client.GetHabbo() != null)
                    ThisUser = Room.GetRoomUserManager().GetRoomUserByHabbo(Client.GetHabbo().Username);

                if (ThisUser != null && Client.GetHabbo().PetId == 0)
                    Room.SendMessage(new UserChangeComposer(ThisUser, false));


                if (Room.GetWired() != null)
                    Room.GetWired().TriggerEvent(WiredBoxType.TriggerRoomEnter, Client.GetHabbo());

                foreach (RoomUser Bot in Room.GetRoomUserManager().GetBots().ToList())
                {
                    if (Bot.IsBot || Bot.IsPet)
                        Bot.BotAI.OnUserEnterRoom(ThisUser);
                }

                if (PolarEnvironment.GetUnixTimestamp() < Client.GetHabbo().FloodTime && Client.GetHabbo().FloodTime != 0)
                    Client.SendMessage(new FloodControlComposer((int)Client.GetHabbo().FloodTime - (int)PolarEnvironment.GetUnixTimestamp()));
                #endregion

                #region Tutorial Step Check
                if (Client.GetRoleplay().TutorialStep == 13 && Client.GetHabbo().CurrentRoom.WardrobeEnabled && Client.GetHabbo().CurrentRoom.Type.Equals("public"))
                {
                    PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(Client, "compose_tutorial|13");
                }
                else if (Client.GetRoleplay().TutorialStep == 18 && Client.GetHabbo().CurrentRoom.PhoneStoreEnabled && Client.GetHabbo().CurrentRoom.Type.Equals("public"))
                {
                    PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(Client, "compose_tutorial|18");
                }
                else if (Client.GetRoleplay().TutorialStep == 23 && Client.GetHabbo().CurrentRoom.BuyCarEnabled && Client.GetHabbo().CurrentRoom.Type.Equals("public"))
                {
                    PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(Client, "compose_tutorial|24");
                }
                else if (Client.GetRoleplay().TutorialStep == 27 && Client.GetHabbo().CurrentRoom.MallEnabled && Client.GetHabbo().CurrentRoom.Type.Equals("public"))
                {
                    PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(Client, "compose_tutorial|28");
                }
                #endregion
               
                #region Clean Websockets (Al cambiar de sala)

                #region Groups
                // WS Groups
                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_group", "close");
                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_group", "open");
                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_gang", "turf_cap_off");
                #endregion

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
                        Client.GetRoomUser().CanWalk = true;
                        /*Client.GetRoleplay().RoomJoinedInmunity = true;
                        Client.GetRoleplay().IsNoob = true;*/
                        if (Client.GetRoomUser() != null)
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

                #region StunCheck
                if (Client.GetRoleplay().IsStun == true)
                {

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

                #region PSVMode
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
                #endregion

                #region DeathCheck
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
                #endregion

                #region JailCheck
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
                }
                #endregion

                #region JobCheck
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

                #endregion

                #region ProbationCheck
                if (!Client.GetRoleplay().OnProbation)
                {

                    if (!Client.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("probation"))
                        Client.GetRoleplay().TimerManager.CreateTimer("probation", 1000, false);
                }
                #endregion

                #region SendHomeCheck
                if (Client.GetRoleplay().SendHomeTimeLeft <= 0)
                {
                    // return;
                }
                else
                {
                    if (Client.GetRoleplay().SendHomeTimeLeft > 30)
                        Client.GetRoleplay().SendHomeTimeLeft = 30;

                    if (!Client.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("sendhome"))
                        Client.GetRoleplay().TimerManager.CreateTimer("sendhome", 1000, false);
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

                    Bot.GetBotRoleplayAI().OnUserEnterRoom(Client);
                }
                #endregion

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

                if (Message != "")
                    Client.SendMessage(new MOTDNotificationComposer(Message));

                Room.GetGameMap().GenerateMaps();

            }
            else
                return;
        }

        public static void TogglePassiveMode(GameClient Client)
        {
            #region Conditions

            #region Basic Conditions
            if (Client.GetRoleplay().Cuffed)
            {
                Client.SendWhisper("No puedes hacer eso mientras estás esposad@", 1);
                return;
            }
            if (!Client.GetRoomUser().CanWalk)
            {
                Client.SendWhisper("Al parecer no puedes ni moverte para hacer eso.", 1);
                return;
            }
            if (Client.GetRoleplay().IsDead)
            {
                Client.SendWhisper("¡No puedes hacer esto mientras estás muert@!", 1);
                return;
            }
            if (Client.GetRoleplay().IsJailed)
            {
                Client.SendWhisper("¡No puedes hacer eso mientras estás muert@!", 1);
                return;
            }
            #endregion

            #region Conditions Checks
            if (!Client.GetRoomUser().GetRoom().RoomData.SafeZoneEnabled)
            {
                Client.SendWhisper("Debes estar en una zona segura para hacer eso.", 1);
                return;
            }
            if (RoleplayManager.PurgeStarted)
            {
                Client.SendWhisper("¡No puedes hacer eso durante la purga!", 1);
                return;
            }
            if (!Client.GetRoleplay().PassiveMode)
            {
                if (Client.GetRoleplay().CurHealth < Client.GetRoleplay().MaxHealth)
                {
                    Client.SendWhisper("Debes tener el 100% de vida para hacer eso.", 1);
                    return;
                }
            }
            if (Client.GetRoleplay().IsWanted)
            {
                Client.SendWhisper("No puedes hacer eso mientras estás en la lista de buscados.", 1);
                return;
            }
            if (Client.GetRoleplay().IsWorking && GroupManager.HasJobCommand(Client, "law"))
            {
                Client.SendWhisper("No puedes hacer eso mientras estás trabajando de policía", 1);
                return;
            }
            if (Client.GetRoleplay().CamCargId == 3 || Client.GetRoleplay().CamCargId == 4)
            {
                Client.SendWhisper("No puedes hacer eso mientras transportas cargamentos ilegales.", 1);
                return;
            }
            if (Client.GetRoleplay().EquippedWeapon != null)
            {
                Client.SendWhisper("No puedes hacer eso mientras lleves un arma equipada.", 1);
                return;
            }
            #endregion

            #endregion

            Client.GetRoleplay().PassiveMode = !Client.GetRoleplay().PassiveMode;

            if (Client.GetRoleplay().PassiveMode)
            {
                Client.SendMessage(new RoomBubbleNotificationComposer("psv-icon", "Modo Pasivo: Activado", ""));
                PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(Client, "compose_psv_mode|active");
                RoleplayManager.Shout(Client, "((Ha entrado en modo pasivo))", 7);
                Client.GetRoomUser().ApplyEffect(EffectsList.Passive);
            }
            else
            {
                Client.SendMessage(new RoomBubbleNotificationComposer("psv-icon", "Modo Pasivo: Desactivado", ""));
                PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(Client, "compose_psv_mode|desactive");
                RoleplayManager.Shout(Client, "((Ha salido del modo pasivo))", 7);
                Client.GetRoomUser().ApplyEffect(EffectsList.None);
            }
        }
        /// <summary>
        /// Lets you place a furni in the desired location
        /// </summary>
        public static Item PlaceItemToRoom(GameClient Session, int BaseId, int GroupId, int X, int Y, double Z, int Rot, bool FromInventory, int roomid, bool ToDB = true, string ExtraData = "", bool IsFood = false, string deliverytype = "", RentableSpaceData House = null, FarmingSpace FarmingSpace = null, TexasHoldEmItem TexasHoldEmData = null)
        {
            try
            {
                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    // Generate the room and ensure it is not null
                    if (!GenerateRoom(roomid, out Room Room, false))
                    {
                        Logging.LogRPGamesError("Failed to generate room with ID: " + roomid);
                        return null;
                    }

                    int ItDemId = 0;
                    int ItemId = 10000000; // Start at 1 bill to prevent item glitches

                    if (House != null)
                        ItemId = PolarEnvironment.GetGame().GetHouseManager().SignMultiplier + House.RoomId;
                    else if (FarmingSpace != null)
                        ItemId = FarmingManager.SignMultiplier + FarmingSpace.Id;
                    else if (ToDB)
                    {
                        try
                        {
                            dbClient.SetQuery($"INSERT INTO `{Polar.Core.DatabaseCompatibility.ItemsTable}` (user_id, {Polar.Core.DatabaseCompatibility.ItemsBaseItemColumn}, room_id) VALUES (1, " + BaseId + ", " + roomid + ")");
                            dbClient.RunQuery();
                             dbClient.SetQuery($"SELECT id FROM `{Polar.Core.DatabaseCompatibility.ItemsTable}` WHERE user_id = '1' AND room_id = '" + roomid + "' AND {Polar.Core.DatabaseCompatibility.ItemsBaseItemColumn} = '" + BaseId + "' ORDER BY id DESC LIMIT 1");
							 ItDemId = dbClient.getInteger();
                            ItemId = ItDemId;
                        }
                        catch (Exception ex)
                        {
                            Logging.LogRPGamesError("Database error in PlaceItemToRoom: " + ex);
                            return null;
                        }
                    }
                    else
                    {
                        while (Room.GetRoomItemHandler().GetFloor.Where(x => x.Id == ItemId).ToList().Count > 0)
                            ItemId++;
                    }

                    // Create the new item and ensure it is not null
                    Item NewItem = new Item(ItemId, Room.RoomId, BaseId, ExtraData, X, Y, Z, Rot, 0, GroupId, 0, 0, "", null, House, FarmingSpace, TexasHoldEmData);
                    if (NewItem == null)
                    {
                        Logging.LogRPGamesError("Failed to create item with BaseId: " + BaseId);
                        return null;
                    }

                    NewItem.DeliveryType = deliverytype;

                    // Handle farming logic if applicable
                    if (NewItem.FarmingData != null && NewItem.GetBaseItem().InteractionType == InteractionType.FARMING && Session != null && Session.GetHabbo() != null)
                    {
                        NewItem.FarmingData.OwnerId = Session.GetHabbo().Id;

                        Task.Run(async delegate
                        {
                            try
                            {
                                if (NewItem != null && NewItem.FarmingData != null)
                                    NewItem.FarmingData.BeingFarmed = true;

                                await Task.Delay(3000);

                                if (Session != null && NewItem != null && NewItem.FarmingData != null)
                                {
                                    Session.SendWhisper("La " + NewItem.GetBaseItem().PublicName + " Que acaba de plantar está listo para ser regado!", 1);
                                    NewItem.FarmingData.BeingFarmed = false;
                                }
                            }
                            catch (Exception ex)
                            {
                                Logging.LogRPGamesError("Error in farming task: " + ex);
                            }
                        });
                    }

                    // Handle food logic if applicable
                    if (IsFood && Session != null && Session.GetHabbo() != null)
                    {
                        NewItem.InteractingUser = Session.GetHabbo().Id;
                        // Session = null; ← ELIMINA ESTA LÍNEA
                    }

                    // Place the item in the room
                    if (NewItem != null)
                        Room.GetRoomItemHandler().SetFloorItem(Session, NewItem, X, Y, Rot, true, false, true, false, false, null, true);

                    return NewItem;
                }
            }
            catch (Exception ex)
            {
                Logging.LogRPGamesError("Error in PlaceItemToRoom: " + ex);
                return null;
            }
        }

        /// <summary>
        /// Gets the car name based on cartype
        /// </summary>
        public static string GetCarName(GameClient Client, bool Upgrade = false)
        {
            if (Client == null || Client.GetHabbo() == null || Client.GetRoleplay() == null)
                return "Toyota Corolla";

            int Car = Client.GetRoleplay().CarType;

            if (Upgrade)
                Car++;

            if (Car == 0)
                return "No Car";
            else if (Car == 1 || Car == 13)
                return "Toyota Corolla";
            else if (Car == 2)
                return "Honda Accord";
            else if (Car == 3)
                return "Nissan GTR";
            else if (Car == 4)
                return "DatCarr celeste";
            else if (Car == 5)
                return "DatCarr Bola de Fuego";
            else if (Car == 6)
                return "DatCarr Doggi";
            else if (Car == 7)
                return "DatCarr Bunni";
            else if (Car == 8)
                return "DatCarr Carstaff";
            else if (Car == 9)
                return "Jetpack";
            else if (Car == 10)
                return "Nascar";
            else if (Car == 11)
                return "Nascar 4";
            else if (Car == 12)
                return "Chevrolet Aveo";
            else if (Car == 14)
                return "Ford Mustang GT";
            else if (Car == 15)
                return "Beetle";
            else
                return "Nissan GTR";
        }

        /// <summary>
        /// Gets the phone name based on phonetype
        /// </summary>
        /*public static string GetPhoneName(GameClient Client, bool Upgrade = false)
        {
            if (Client == null || Client.GetHabbo() == null || Client.GetRoleplay() == null)
                return "LG Gossip";

            var Phone = Client.GetRoleplay().PhoneType;

            if (Upgrade)
                Phone++;

            if (Phone == 0)
                return "No Phone";
            else if (Phone == 1)
                return "LG Gossip";
            else if (Phone == 2)
                return "iPhone 4s";
            else if (Phone == 3)
                return "iPhone 7";
            else
                return "iPhone 7";
        }*/

        /// <summary>
        /// Generates a list of coordinates in the room based on a starting and ending coordinate
        /// </summary>
        public static List<ThreeDCoord> GenerateMap(int BeginX, int BeginY, int EndX, int EndY)
        {
            List<ThreeDCoord> Squares = new List<ThreeDCoord>();

            int Length = Math.Abs(BeginX - EndX);
            int Width = Math.Abs(BeginY - EndY);

            for (int i = 0; i < Length; i++)
            {
                Squares.Add(new ThreeDCoord(BeginX + i, BeginY, 0));

                for (int j = 0; j < Width; j++)
                {
                    Squares.Add(new ThreeDCoord(BeginX + i, BeginY + j, 0));
                }
            }

            return Squares;
        }

        public static Point GetDirectionDeviation(RoomUser User)
        {
            if (User == null)
                return new Point(0, 0);

            if (User.GetClient() == null)
                return new Point(0, 0);

            WalkDirections Direction = User.GetClient().GetRoleplay().WalkDirection;
            Point Deviation = new Point(User.Coordinate.X, User.Coordinate.Y);

            if (Direction == WalkDirections.Up)
            {
                Deviation = new Point(User.Coordinate.X - 2, User.Coordinate.Y);

                if (!User.GetRoom().GetGameMap().IsValidStep(User, new Vector2D(User.X, User.Y), new Vector2D(Deviation.X, Deviation.Y),
                   (User.GoalX == Deviation.X && User.GoalY == User.SetY), User.AllowOverride))
                {
                    Deviation = new Point(User.Coordinate.X - 1, User.Coordinate.Y);
                }

            }

            else if (Direction == WalkDirections.Down)
            {
                Deviation = new Point(User.Coordinate.X + 2, User.Coordinate.Y);
                if (!User.GetRoom().GetGameMap().IsValidStep(User, new Vector2D(User.X, User.Y), new Vector2D(Deviation.X, Deviation.Y),
                     (User.GoalX == Deviation.X && User.GoalY == User.SetY), User.AllowOverride))
                {
                    Deviation = new Point(User.Coordinate.X + 1, User.Coordinate.Y);
                }
            }

            else if (Direction == WalkDirections.Right)
            {
                Deviation = new Point(User.Coordinate.X, User.Coordinate.Y - 2);
                if (!User.GetRoom().GetGameMap().IsValidStep(User, new Vector2D(User.X, User.Y), new Vector2D(Deviation.X, Deviation.Y),
                     (User.GoalX == Deviation.X && User.GoalY == User.SetY), User.AllowOverride))
                {
                    Deviation = new Point(User.Coordinate.X, User.Coordinate.Y - 1);
                }
            }
            else if (Direction == WalkDirections.Left)
            {
                Deviation = new Point(User.Coordinate.X, User.Coordinate.Y + 2);
                if (!User.GetRoom().GetGameMap().IsValidStep(User, new Vector2D(User.X, User.Y), new Vector2D(Deviation.X, Deviation.Y),
                     (User.GoalX == Deviation.X && User.GoalY == User.SetY), User.AllowOverride))
                {
                    Deviation = new Point(User.Coordinate.X, User.Coordinate.Y + 1);
                }
            }
            return Deviation;
        }


        /// <summary>
        /// Sends a delayed whisper alert to a targetted client
        /// </summary>
        /// 

        /// <param name="Client">Target user</param>
        /// <param name="Msg">Desired message</param>
        /// <param name="Bubble">Desired speech bubble</param>
        /// <param name="Time">Desired Delay (In Seconds)</param>
        public static void SendDelayedWhisper(GameClient Client, string Msg, int Bubble = 1, int Time = 3)
        {
            Task.Run(async delegate 
            {
                await Task.Delay(Time * 1000);
                if (Client == null) return;
                Client.SendWhisper(Msg, Bubble);
            });
        }

        public static void RideHorseUser(RoomUser User)
        {

        }

        /// <summary>
        /// Checks if item is near the user.
        /// </summary>
        public static Item GetNearItem(string Item, Room MRoom)
        {
            Item Inter = null;
            lock (MRoom.GetRoomItemHandler().GetFloor)
            {
                foreach (Item item in MRoom.GetRoomItemHandler().GetFloor)
                {
                    if (item == null)
                        continue;

                    if (item.GetBaseItem() == null)
                        continue;

                    if (!item.GetBaseItem().ItemName.Contains(Item))
                        continue;

                    Inter = item;
                }
            }

            return Inter;
        }

        /// <summary>
        /// Turns the user into a pet
        /// </summary>
        /// <param name="TargetClient"></param>
        /// <param name="Pet"></param>
        public static void MakePet(GameClient TargetClient, string Pet)
        {

            int TargetPetId = RoleplayManager.GetPetIdByString(Pet);

            if (TargetClient == null)
                return;
            if (TargetClient.GetHabbo() == null)
                return;
            if (TargetClient.GetRoomUser() == null)
                return;

            //Change the users Pet Id.         
            TargetClient.GetHabbo().PetId = (TargetPetId == -1 ? 0 : TargetPetId);

            //Quickly remove the old user instance.
            TargetClient.GetRoomUser().GetRoom().SendMessage(new UserRemoveComposer(TargetClient.GetRoomUser().VirtualId));

            //Add the new one, they won't even notice a thing!!11 8-)
            TargetClient.GetRoomUser().GetRoom().SendMessage(new UsersComposer(TargetClient.GetRoomUser()));
        }

        public static int GetPetIdByString(string Pet)
        {
            switch (Pet.ToLower())
            {
                default:
                    return 0;
                case "habbo":
                    return -1;
                case "dog":
                    return 60;//This should be 0.
                case "cat":
                    return 1;
                case "terrier":
                    return 2;
                case "croc":
                case "croco":
                    return 3;
                case "bear":
                    return 4;
                case "pig":
                    return 5;
                case "lion":
                    return 6;
                case "rhino":
                    return 7;
                case "spider":
                    return 8;
                case "turtle":
                    return 9;
                case "chick":
                case "chicken":
                    return 10;
                case "frog":
                    return 11;
                case "drag":
                case "dragon":
                    return 12;
                case "monkey":
                    return 14;
                case "horse":
                    return 15;
                case "plant":
                    return 16;
                case "bunny":
                    return 17;
                case "evilbunny":
                    return 18;
                case "brownbunny":
                    return 19;
                case "pinkbunny":
                    return 20;
                case "whitepigeon":
                case "whitechick":
                    return 21;
                case "blackpigeon":
                case "blackchick":
                    return 22;
                case "demon":
                case "evilmonkey":
                case "demonmonkey":
                    return 23;
                case "bbear":
                case "babybear":
                    return 24;
                case "bterrier":
                case "babyterrier":
                    return 25;
                case "gnome":
                    return 26;
                case "kitty":
                case "kitten":
                    return 28;
                case "puppy":
                case "doggy":
                    return 29;
                case "bpig":
                case "piggy":
                case "babypig":
                    return 30;
                case "oompa":
                case "oompaloompa":
                    return 31;
                case "rock":
                    return 32;
                case "ptera":
                case "pteradactyl":
                    return 33;
                case "trex":
                case "dino":
                    return 34;
            }
        }
    }
}