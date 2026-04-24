using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

using log4net;

using Polar.Core;
using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Incoming.Quiz;
using Polar.Communication.Packets.Incoming.QuickPolls;
using Polar.Communication.Packets.Incoming.Catalog;
using Polar.Communication.Packets.Incoming.Handshake;
using Polar.Communication.Packets.Incoming.Navigator;
using Polar.Communication.Packets.Incoming.Quests;
using Polar.Communication.Packets.Incoming.Rooms.Avatar;
using Polar.Communication.Packets.Incoming.Rooms.Chat;
using Polar.Communication.Packets.Incoming.Rooms.Connection;
using Polar.Communication.Packets.Incoming.Rooms.Engine;
using Polar.Communication.Packets.Incoming.Rooms.Action;
using Polar.Communication.Packets.Incoming.Users;
using Polar.Communication.Packets.Incoming.Inventory.AvatarEffects;
using Polar.Communication.Packets.Incoming.Inventory.Purse;
using Polar.Communication.Packets.Incoming.Misc;
using Polar.Communication.Packets.Incoming.Inventory.Badges;
using Polar.Communication.Packets.Incoming.Inventory.Weapons;
using Polar.Communication.Packets.Incoming.Avatar;
using Polar.Communication.Packets.Incoming.Inventory.Achievements;
using Polar.Communication.Packets.Incoming.Inventory.Bots;
using Polar.Communication.Packets.Incoming.Inventory.Pets;
using Polar.Communication.Packets.Incoming.LandingView;
using Polar.Communication.Packets.Incoming.Messenger;
using Polar.Communication.Packets.Incoming.Groups;
using Polar.Communication.Packets.Incoming.Rooms.Settings;
using Polar.Communication.Packets.Incoming.Rooms.AI.Pets;
using Polar.Communication.Packets.Incoming.Rooms.AI.Bots;
using Polar.Communication.Packets.Incoming.Rooms.AI.Pets.Horse;
using Polar.Communication.Packets.Incoming.Rooms.Furni;
using Polar.Communication.Packets.Incoming.Rooms.Furni.RentableSpaces;
using Polar.Communication.Packets.Incoming.Rooms.Furni.YouTubeTelevisions;
using Polar.Communication.Packets.Incoming.Rooms.Furni.Crafting;
using Polar.Communication.Packets.Incoming.Rooms.Nux;
using Polar.Communication.Packets.Incoming.Help;
using Polar.Communication.Packets.Incoming.Rooms.FloorPlan;
using Polar.Communication.Packets.Incoming.Rooms.Furni.Wired;
using Polar.Communication.Packets.Incoming.Moderation;
using Polar.Communication.Packets.Incoming.Inventory.Furni;
using Polar.Communication.Packets.Incoming.Rooms.Furni.Stickys;
using Polar.Communication.Packets.Incoming.Rooms.Furni.Moodlight;
using Polar.Communication.Packets.Incoming.Inventory.Trading;
using Polar.Communication.Packets.Incoming.GameCenter;
using Polar.Communication.Packets.Incoming.Marketplace;
using Polar.Communication.Packets.Incoming.Rooms.Furni.LoveLocks;
using Polar.Communication.Packets.Incoming.Talents;
using Polar.Communication.Packets.Incoming.Guides;
using Polar.Communication.Packets.Incoming.Polls;
using Polar.Communication.Packets.Incoming.HabboCamera;
using Akiled.Communication.Packets.Incoming.HabboCamera;
using Polar.Communication.Packets.Incoming.BattlePass;
using Polar.Communication.Packets.Incoming.FurniEditor;
using Polar.Communication.Packets.Incoming.Catalog.CatalogAdmin;
using Polar.Communication.Packets.Incoming.Inventory.Prefixes;

namespace Polar.Communication.Packets
{
    public sealed class PacketManager
    {
        private static readonly ILog log = LogManager.GetLogger("Polar.Communication.Packets");

        // ✅ FIX #1: IgnoreTasks=true hace que ExecutePacketAsync nunca se llame.
        //           Se mantiene la flag pero se documenta claramente su efecto.
        private readonly bool IgnoreTasks = true;

        private readonly int MaximumRunTimeInSec = 300;
        private readonly bool ThrowUserErrors = false;

        // ✅ FIX #2: TaskFactory con LongRunning no es apropiado para paquetes cortos.
        //           PreferFairness está bien para garantizar orden de ejecución justo.
        private readonly TaskFactory _eventDispatcher;

        // ✅ FIX #3: IReadOnlyDictionary después de la inicialización para evitar
        //           modificaciones accidentales en runtime. Se construye con Dictionary
        //           y luego se expone como readonly.
        private readonly Dictionary<int, IPacketEvent> _incomingPackets;
        private readonly Dictionary<int, string> _packetNames;

        private readonly ConcurrentDictionary<int, Task> _runningTasks;

        // ✅ FIX #4: CancellationTokenSource a nivel de PacketManager para poder
        //           cancelar todas las tareas en shutdown, no solo individualmente.
        private readonly CancellationTokenSource _shutdownTokenSource = new();

        public static bool DEBUG_SHOW_PACKETS { get; private set; }

        public PacketManager()
        {
            _incomingPackets = new Dictionary<int, IPacketEvent>();
            _packetNames = new Dictionary<int, string>();

            _eventDispatcher = new TaskFactory(
                TaskCreationOptions.PreferFairness,
                TaskContinuationOptions.None);

            _runningTasks = new ConcurrentDictionary<int, Task>();

            RegisterHandshake();
            RegisterLandingView();
            RegisterCatalog();
            RegisterMarketplace();
            RegisterNewNavigator();
            RegisterRoomAction();
            RegisterQuests();
            RegisterRoomConnection();
            RegisterRoomChat();
            RegisterRoomEngine();
            RegisterFurni();
            RegisterUsers();
            RegisterSound();
            RegisterMisc();
            RegisterInventory();
            RegisterTalents();
            RegisterPolls();
            RegisterPurse();
            RegisterRoomAvatar();
            RegisterAvatar();
            RegisterMessenger();
            RegisterGroups();
            RegisterRoomSettings();
            RegisterPets();
            RegisterBots();
            RegisterHelp();
            FloorPlanEditor();
            RegisterModeration();
            RegisterGameCenter();
            RegisterRoomCamera();
            RegisterNames();
            RegisterNavigator();
            RegisterBattlePass();
        }

        // ── Packet Execution ───────────────────────────────────────────────────

        public void TryExecutePacket(GameClient session, ClientPacket packet)
        {
            // ✅ FIX #5: Validación de sesión antes de buscar el handler.
            //           Evita NullReferenceException si llega un paquete de una
            //           sesión ya desconectada.
            if (session == null || packet == null)
                return;

            if (!_incomingPackets.TryGetValue(packet.Id, out IPacketEvent handler))
            {
                if (ExtraSettings.DEBUG_ENABLED)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"PAQUETE DESCONOCIDO: {packet}");
                    Console.ResetColor();
                }
                return;
            }

            if (ExtraSettings.DEBUG_ENABLED)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                string name = _packetNames.TryGetValue(packet.Id, out string n) ? n : "UnnamedPacketEvent";
                Console.WriteLine($"PAQUETE RECIBIDO: [{packet.Id}] {name}");
                Console.ResetColor();
            }

            if (!IgnoreTasks)
                ExecutePacketAsync(session, packet, handler);
            else
                // ✅ FIX #6: Envolver en try/catch para que una excepción en un
                //           handler no tire abajo el thread de red completo.
                SafeExecute(session, packet, handler);
        }

        private void SafeExecute(GameClient session, ClientPacket packet, IPacketEvent handler)
        {
            try
            {
                handler.Parse(session, packet);
            }
            catch (Exception ex)
            {
                string packetName = _packetNames.TryGetValue(packet.Id, out string n) ? n : "Unknown";
                string user = session?.GetHabbo()?.Username ?? "sin_sesion";

                log.Error($"[PacketManager] Error en paquete [{packet.Id}] {packetName} " +
                          $"usuario={user}: {ex.Message}\n{ex.StackTrace}", ex);

                if (ThrowUserErrors)
                    throw;

                // Solo desconectar si es un error de sesión/estado corrupto,
                // NO por bugs internos como IndexOutOfRange en rollers
                if (ex is NullReferenceException || ex is ObjectDisposedException)
                {
                    try { session?.Disconnect(true); } catch { }
                }
                // Para cualquier otro error (IndexOutOfRange, etc.) el usuario
                // sigue conectado — solo ese paquete falló
            }
        
        }

        private void ExecutePacketAsync(GameClient session, ClientPacket packet, IPacketEvent handler)
        {
            // ✅ FIX #7: Pasar el token de shutdown del manager, no crear uno nuevo
            //           por paquete, para poder cancelar todo en Dispose/shutdown.
            CancellationToken shutdownToken = _shutdownTokenSource.Token;
            var packetCancelSource = CancellationTokenSource.CreateLinkedTokenSource(shutdownToken);
            CancellationToken token = packetCancelSource.Token;

            Task t = _eventDispatcher.StartNew(() =>
            {
                token.ThrowIfCancellationRequested();
                handler.Parse(session, packet);
            }, token);

            _runningTasks.TryAdd(t.Id, t);

            try
            {
                if (!t.Wait(MaximumRunTimeInSec * 1000, token))
                {
                    packetCancelSource.Cancel();
                    log.Warn($"[PacketManager] Packet [{packet.Id}] timed out after {MaximumRunTimeInSec}s");
                }
            }
            catch (AggregateException ex)
            {
                foreach (Exception e in ex.Flatten().InnerExceptions)
                {
                    if (ThrowUserErrors) throw e;

                    log.Error($"[PacketManager] Async error in packet [{packet.Id}]: {e.Message}", e);
                    try { session.Disconnect(true); } catch { }
                }
            }
            catch (OperationCanceledException)
            {
                try { session.Disconnect(true); } catch { }
            }
            finally
            {
                _runningTasks.TryRemove(t.Id, out _);
                packetCancelSource.Dispose();
            }
        }

        // ── Lifecycle ──────────────────────────────────────────────────────────

        public void WaitForAllToComplete()
        {
            try
            {
                Task.WaitAll(_runningTasks.Values.ToArray());
            }
            catch { /* tareas ya canceladas */ }
        }

        public void Shutdown()
        {
            // ✅ FIX #8: Método de shutdown explícito que cancela todas las tareas
            //           activas antes de limpiar los handlers.
            _shutdownTokenSource.Cancel();
            WaitForAllToComplete();
            UnregisterAll();
        }

        public void UnregisterAll()
        {
            _incomingPackets.Clear();
        }

        // ── Register Methods ───────────────────────────────────────────────────
        // ✅ FIX #9: Método helper para registrar y detectar duplicados en tiempo
        //           de arranque, en lugar de lanzar excepción en runtime silenciosa.
        private void Register(int headerId, IPacketEvent handler)
        {
            if (_incomingPackets.ContainsKey(headerId))
            {
                log.Warn($"[PacketManager] Duplicate handler registration for header [{headerId}] " +
                         $"({_incomingPackets[headerId].GetType().Name} → {handler.GetType().Name}). Overwriting.");
            }
            _incomingPackets[headerId] = handler;
        }

        private void RegisterHandshake()
        {
            Register(ClientPacketHeader.GetClientVersionMessageEvent, new GetClientVersionEvent());
            Register(ClientPacketHeader.InitCryptoMessageEvent, new InitCryptoEvent());
            Register(ClientPacketHeader.GenerateSecretKeyMessageEvent, new GenerateSecretKeyEvent());
            Register(ClientPacketHeader.UniqueIDMessageEvent, new UniqueIDEvent());
            Register(ClientPacketHeader.SSOTicketMessageEvent, new SSOTicketEvent());
            Register(ClientPacketHeader.InfoRetrieveMessageEvent, new InfoRetrieveEvent());
            Register(ClientPacketHeader.PingMessageEvent, new PingEvent());
        }

        private void RegisterLandingView()
        {
            Register(ClientPacketHeader.RefreshCampaignMessageEvent, new RefreshCampaignEvent());
            Register(ClientPacketHeader.GetPromoArticlesMessageEvent, new GetPromoArticlesEvent());
            Register(ClientPacketHeader.CommunityGoalHallOfFame, new GetCommunityGoalHallOfFameEvent());
        }

        private void RegisterRoomCamera()
        {
            Register(ClientPacketHeader.HabboCameraPictureDataMessageEvent, new HabboCameraPictureDataEvent());
            Register(ClientPacketHeader.PurchaseCameraPictureMessageEvent, new PurchaseCameraPictureEvent());
            Register(ClientPacketHeader.SetRoomThumbnailMessageEvent, new SetRoomThumbnailEvent());
            Register(ClientPacketHeader.PublishCameraPictureMessageEvent, new PublishCameraPictureEvent());
            Register(ClientPacketHeader.ParticipatePictureCameraCompetitionMessageEvent, new ParticipatePictureCameraCompetitionEvent());
        }

        private void RegisterCatalog()
        {
            Register(ClientPacketHeader.GetCatalogIndexMessageEvent, new GetCatalogIndexEvent());
            Register(ClientPacketHeader.BuyTargettedOfferMessageEvent, new BuyTargettedOfferMessage());
            Register(ClientPacketHeader.GetCatalogPageMessageEvent, new GetCatalogPageEvent());
            Register(ClientPacketHeader.GetCatalogOfferMessageEvent, new GetCatalogOfferEvent());
            Register(ClientPacketHeader.PurchaseFromCatalogMessageEvent, new PurchaseFromCatalogEvent());
            Register(ClientPacketHeader.PurchaseFromCatalogAsGiftMessageEvent, new PurchaseFromCatalogAsGiftEvent());
            Register(ClientPacketHeader.PurchaseRoomPromotionMessageEvent, new PurchaseRoomPromotionEvent());
            Register(ClientPacketHeader.GetGiftWrappingConfigurationMessageEvent, new GetGiftWrappingConfigurationEvent());
            Register(ClientPacketHeader.GetMarketplaceConfigurationMessageEvent, new GetMarketplaceConfigurationEvent());
            Register(ClientPacketHeader.CheckPetNameMessageEvent, new CheckPetNameEvent());
            Register(ClientPacketHeader.RedeemVoucherMessageEvent, new RedeemVoucherEvent());
            Register(ClientPacketHeader.GetSellablePetBreedsMessageEvent, new GetSellablePetBreedsEvent());
            Register(ClientPacketHeader.GetPromotableRoomsMessageEvent, new GetPromotableRoomsEvent());
            Register(ClientPacketHeader.GetNuxPresentEvent, new GetNuxPresentEvent());
            Register(ClientPacketHeader.GetGroupFurniConfigMessageEvent, new GetGroupFurniConfigEvent());
            Register(ClientPacketHeader.CheckGnomeNameMessageEvent, new CheckGnomeNameEvent());
            Register(ClientPacketHeader.GetClubGiftsMessageEvent, new GetClubGiftsEvent());
            Register(ClientPacketHeader.RequestCameraConfigurationMessageEvent, new RequestCameraConfigurationEvent());

            Register(ClientPacketHeader.CatalogAdminCreateOfferEvent, new CatalogAdminCreateOfferEvent());
            Register(ClientPacketHeader.CatalogAdminCreatePageEvent, new CatalogAdminCreatePageEvent());
            Register(ClientPacketHeader.CatalogAdminDeleteOfferEvent, new CatalogAdminDeleteOfferEvent());
            Register(ClientPacketHeader.CatalogAdminDeletePageEvent, new CatalogAdminDeletePageEvent());
            Register(ClientPacketHeader.CatalogAdminMoveOfferEvent, new CatalogAdminMoveOfferEvent());
            Register(ClientPacketHeader.CatalogAdminMovePageEvent, new CatalogAdminMovePageEvent());
            Register(ClientPacketHeader.CatalogAdminPublishEvent, new CatalogAdminPublishEvent());
            Register(ClientPacketHeader.CatalogAdminSaveOfferEvent, new CatalogAdminSaveOfferEvent());
            Register(ClientPacketHeader.CatalogAdminSavePageEvent, new CatalogAdminSavePageEvent());
            Register(ClientPacketHeader.CatalogAdminSavePageIconEvent, new CatalogAdminSavePageIconEvent());
        }

        private void RegisterMarketplace()
        {
            Register(ClientPacketHeader.GetOffersMessageEvent, new GetOffersEvent());
            Register(ClientPacketHeader.GetOwnOffersMessageEvent, new GetOwnOffersEvent());
            Register(ClientPacketHeader.GetMarketplaceCanMakeOfferMessageEvent, new GetMarketplaceCanMakeOfferEvent());
            Register(ClientPacketHeader.GetMarketplaceItemStatsMessageEvent, new GetMarketplaceItemStatsEvent());
            Register(ClientPacketHeader.MakeOfferMessageEvent, new MakeOfferEvent());
            Register(ClientPacketHeader.CancelOfferMessageEvent, new CancelOfferEvent());
            Register(ClientPacketHeader.BuyOfferMessageEvent, new BuyOfferEvent());
            Register(ClientPacketHeader.RedeemOfferCreditsMessageEvent, new RedeemOfferCreditsEvent());
        }

        private void RegisterNavigator()
        {
            Register(ClientPacketHeader.AddFavouriteRoomMessageEvent, new AddFavouriteRoomEvent());
            Register(ClientPacketHeader.GetUserFlatCatsMessageEvent, new GetUserFlatCatsEvent());
            Register(ClientPacketHeader.DeleteFavouriteRoomMessageEvent, new RemoveFavouriteRoomEvent());
            Register(ClientPacketHeader.GoToHotelViewMessageEvent, new GoToHotelViewEvent());
            Register(ClientPacketHeader.UpdateNavigatorSettingsMessageEvent, new UpdateNavigatorSettingsEvent());
            Register(ClientPacketHeader.CanCreateRoomMessageEvent, new CanCreateRoomEvent());
            Register(ClientPacketHeader.CreateFlatMessageEvent, new CreateFlatEvent());
            Register(ClientPacketHeader.GetGuestRoomMessageEvent, new GetGuestRoomEvent());
            Register(ClientPacketHeader.EditRoomPromotionMessageEvent, new EditRoomEventEvent());
            Register(ClientPacketHeader.GetEventCategoriesMessageEvent, new GetNavigatorFlatsEvent());
        }

        public void RegisterNewNavigator()
        {
            Register(ClientPacketHeader.InitializeNewNavigatorMessageEvent, new InitializeNewNavigatorEvent());
            Register(ClientPacketHeader.NewNavigatorSearchMessageEvent, new NewNavigatorSearchEvent());
            Register(ClientPacketHeader.FindRandomFriendingRoomMessageEvent, new FindRandomFriendingRoomEvent());
            Register(ClientPacketHeader.NavigatorSavedSearchMessageEvent, new NavigatorSavedSearchEvent());
            Register(ClientPacketHeader.DeleteNavigatorSavedSearchMessageEvent, new DeleteNavigatorSavedSearchEvent());
        }

        private void RegisterQuests()
        {
            Register(ClientPacketHeader.GetQuestListMessageEvent, new GetQuestListEvent());
            Register(ClientPacketHeader.StartQuestMessageEvent, new StartQuestEvent());
            Register(ClientPacketHeader.CancelQuestMessageEvent, new CancelQuestEvent());
            Register(ClientPacketHeader.GetCurrentQuestMessageEvent, new GetCurrentQuestEvent());
            Register(ClientPacketHeader.GetDailyQuestMessageEvent, new GetDailyQuestEvent());
        }

        private void RegisterHelp()
        {
            Register(ClientPacketHeader.OnBullyClickMessageEvent, new OnBullyClickEvent());
            Register(ClientPacketHeader.SendBullyReportMessageEvent, new SendBullyReportEvent());
            Register(ClientPacketHeader.SubmitBullyReportMessageEvent, new SubmitBullyReportEvent());
            Register(ClientPacketHeader.GetSanctionStatusMessageEvent, new GetSanctionStatusEvent());
        }

        private void RegisterRoomAction()
        {
            Register(ClientPacketHeader.LetUserInMessageEvent, new LetUserInEvent());
            Register(ClientPacketHeader.BanUserMessageEvent, new BanUserEvent());
            Register(ClientPacketHeader.KickUserMessageEvent, new KickUserEvent());
            Register(ClientPacketHeader.AssignRightsMessageEvent, new AssignRightsEvent());
            Register(ClientPacketHeader.RemoveRightsMessageEvent, new RemoveRightsEvent());
            Register(ClientPacketHeader.RemoveAllRightsMessageEvent, new RemoveAllRightsEvent());
            Register(ClientPacketHeader.MuteUserMessageEvent, new MuteUserEvent());
            Register(ClientPacketHeader.GiveHandItemMessageEvent, new GiveHandItemEvent());
            Register(ClientPacketHeader.RemoveMyRightsMessageEvent, new RemoveMyRightsEvent());
        }

        private void RegisterAvatar()
        {
            Register(ClientPacketHeader.GetWardrobeMessageEvent, new GetWardrobeEvent());
            Register(ClientPacketHeader.SaveWardrobeOutfitMessageEvent, new SaveWardrobeOutfitEvent());
        }

        private void RegisterRoomAvatar()
        {
            Register(ClientPacketHeader.ActionMessageEvent, new ActionEvent());
            Register(ClientPacketHeader.ApplySignMessageEvent, new ApplySignEvent());
            Register(ClientPacketHeader.DanceMessageEvent, new DanceEvent());
            Register(ClientPacketHeader.SitMessageEvent, new SitEvent());
            Register(ClientPacketHeader.ChangeMottoMessageEvent, new ChangeMottoEvent());
            Register(ClientPacketHeader.LookToMessageEvent, new LookToEvent());
            Register(ClientPacketHeader.DropHandItemMessageEvent, new DropHandItemEvent());
            Register(ClientPacketHeader.GiveRoomScoreMessageEvent, new GiveRoomScoreEvent());
            Register(ClientPacketHeader.IgnoreUserMessageEvent, new IgnoreUserEvent());
            Register(ClientPacketHeader.UnIgnoreUserMessageEvent, new UnIgnoreUserEvent());
        }

        private void RegisterRoomConnection()
        {
            Register(ClientPacketHeader.OpenFlatConnectionMessageEvent, new OpenFlatConnectionEvent());
            Register(ClientPacketHeader.GoToFlatMessageEvent, new GoToFlatEvent());
        }

        private void RegisterRoomChat()
        {
            Register(ClientPacketHeader.ChatMessageEvent, new ChatEvent());
            Register(ClientPacketHeader.ShoutMessageEvent, new ShoutEvent());
            Register(ClientPacketHeader.WhisperMessageEvent, new WhisperEvent());
            Register(ClientPacketHeader.StartTypingMessageEvent, new StartTypingEvent());
            Register(ClientPacketHeader.CancelTypingMessageEvent, new CancelTypingEvent());
        }

        private void RegisterRoomEngine()
        {
            Register(ClientPacketHeader.GetRoomEntryDataMessageEvent, new GetRoomEntryDataEvent());
            Register(ClientPacketHeader.GetFurnitureAliasesMessageEvent, new GetFurnitureAliasesEvent());
            Register(ClientPacketHeader.MoveAvatarMessageEvent, new MoveAvatarEvent());
            Register(ClientPacketHeader.MoveObjectMessageEvent, new MoveObjectEvent());
            Register(ClientPacketHeader.UpdateFurniturePositionEvent, new UpdateFurniturePositionEvent());
            Register(ClientPacketHeader.PickupObjectMessageEvent, new PickupObjectEvent());
            Register(ClientPacketHeader.MoveWallItemMessageEvent, new MoveWallItemEvent());
            Register(ClientPacketHeader.ApplyDecorationMessageEvent, new ApplyDecorationEvent());
            Register(ClientPacketHeader.PlaceObjectMessageEvent, new PlaceObjectEvent());
            Register(ClientPacketHeader.UseFurnitureMessageEvent, new UseFurnitureEvent());
            Register(ClientPacketHeader.UseWallItemMessageEvent, new UseWallItemEvent());
        }

        private void RegisterInventory()
        {
            Register(ClientPacketHeader.InitTradeMessageEvent, new InitTradeEvent());
            Register(ClientPacketHeader.TradingOfferItemMessageEvent, new TradingOfferItemEvent());
            Register(ClientPacketHeader.TradingOfferItemsMessageEvent, new TradingOfferItemsEvent());
            Register(ClientPacketHeader.TradingRemoveItemMessageEvent, new TradingRemoveItemEvent());
            Register(ClientPacketHeader.TradingAcceptMessageEvent, new TradingAcceptEvent());
            Register(ClientPacketHeader.TradingCancelMessageEvent, new TradingCancelEvent());
            Register(ClientPacketHeader.TradingConfirmMessageEvent, new TradingConfirmEvent());
            Register(ClientPacketHeader.TradingModifyMessageEvent, new TradingModifyEvent());
            Register(ClientPacketHeader.TradingCancelConfirmMessageEvent, new TradingCancelConfirmEvent());
            Register(ClientPacketHeader.RequestFurniInventoryMessageEvent, new RequestFurniInventoryEvent());
            Register(ClientPacketHeader.GetBadgesMessageEvent, new GetBadgesEvent());
            Register(ClientPacketHeader.RequestInventoryRP, new GetWeaponsEvent());
            Register(ClientPacketHeader.EquipRPInventory, new EquipRPItemEvent());
            Register(ClientPacketHeader.DesequipRPInventory, new UnEquipRPItemEvent());
            Register(ClientPacketHeader.GetAchievementsMessageEvent, new GetAchievementsEvent());
            Register(ClientPacketHeader.SetActivatedBadgesMessageEvent, new SetActivatedBadgesEvent());
            Register(ClientPacketHeader.SetActivatedWeaponsMessageEvent, new SetActivatedWeaponsEvent());
            Register(ClientPacketHeader.GetBotInventoryMessageEvent, new GetBotInventoryEvent());
            Register(ClientPacketHeader.GetPetInventoryMessageEvent, new GetPetInventoryEvent());
            Register(ClientPacketHeader.AvatarEffectActivatedMessageEvent, new AvatarEffectActivatedEvent());
            Register(ClientPacketHeader.AvatarEffectSelectedMessageEvent, new AvatarEffectSelectedEvent());
            Register(ClientPacketHeader.RequestFurniDeleteItem, new RequestFurniDeleteItems());
            //Prefix
            Register(ClientPacketHeader.DeletePrefixEvent, new DeletePrefixEvent());
            Register(ClientPacketHeader.PurchasePrefixEvent, new PurchasePrefixEvent());
            Register(ClientPacketHeader.RequestUserPrefixesEvent, new RequestUserPrefixesEvent());
            Register(ClientPacketHeader.SetActivePrefixEvent, new SetActivePrefixEvent());
        }

        private void RegisterTalents()
        {
            Register(ClientPacketHeader.GetTalentTrackMessageEvent, new GetTalentTrackEvent());
            Register(ClientPacketHeader.RetrieveCitizenshipStatus, new RetrieveCitizenshipStatus());
            Register(ClientPacketHeader.CheckQuizTypeEvent, new CheckQuizTypeEvent());
            Register(ClientPacketHeader.PostQuizAnswersMessageEvent, new PostQuizAnswersMessageEvent());
        }

        private void RegisterPolls()
        {
            Register(ClientPacketHeader.SubmitPollAnswerMessageEvent, new SubmitPollAnswerMessageEvent());
        }

        private void RegisterPurse()
        {
            Register(ClientPacketHeader.GetCreditsInfoMessageEvent, new GetCreditsInfoEvent());
            Register(ClientPacketHeader.GetHabboClubWindowMessageEvent, new GetHabboClubWindowEvent());
            Register(ClientPacketHeader.GetHabboClubCenterInfoMessageEvent, new GetHabboClubCenterInfoMessageEvent());
        }

        private void RegisterUsers()
        {
            Register(ClientPacketHeader.ScrGetUserInfoMessageEvent, new ScrGetUserInfoEvent());
            Register(ClientPacketHeader.SetChatPreferenceMessageEvent, new SetChatPreferenceEvent());
            Register(ClientPacketHeader.SetUserFocusPreferenceEvent, new SetUserFocusPreferenceEvent());
            Register(ClientPacketHeader.SetMessengerInviteStatusMessageEvent, new SetMessengerInviteStatusEvent());
            Register(ClientPacketHeader.RespectUserMessageEvent, new RespectUserEvent());
            Register(ClientPacketHeader.UpdateFigureDataMessageEvent, new UpdateFigureDataEvent());
            Register(ClientPacketHeader.UpdateBackgroundDataMessageEvent, new UpdateBackgroundEvent());
            Register(ClientPacketHeader.OpenPlayerProfileMessageEvent, new OpenPlayerProfileEvent());
            Register(ClientPacketHeader.GetSelectedBadgesMessageEvent, new GetSelectedBadgesEvent());
            Register(ClientPacketHeader.GetRelationshipsMessageEvent, new GetRelationshipsEvent());
            Register(ClientPacketHeader.SetRelationshipMessageEvent, new SetRelationshipEvent());
            Register(ClientPacketHeader.CheckValidNameMessageEvent, new CheckValidNameEvent());
            Register(ClientPacketHeader.ChangeNameMessageEvent, new ChangeNameEvent());
            Register(ClientPacketHeader.SetUsernameMessageEvent, new SetUsernameEvent());
            Register(ClientPacketHeader.GetHabboGroupBadgesMessageEvent, new GetHabboGroupBadgesEvent());
            Register(ClientPacketHeader.GetUserTagsMessageEvent, new GetUserTagsEvent());
        }

        private void RegisterSound() { }

        private void RegisterBattlePass()
        {
            Register(ClientPacketHeader.GetBattlePassMessageEvent, new GetBattlePassEvent());
            Register(ClientPacketHeader.ClaimBattlePassRewardMessageEvent, new ClaimBattlePassRewardEvent());
        }

        private void RegisterMisc()
        {
            Register(ClientPacketHeader.EventTrackerMessageEvent, new EventTrackerEvent());
            Register(ClientPacketHeader.ClientVariablesMessageEvent, new ClientVariablesEvent());
            Register(ClientPacketHeader.DisconnectionMessageEvent, new DisconnectEvent());
            Register(ClientPacketHeader.LatencyTestMessageEvent, new LatencyTestEvent());
            Register(ClientPacketHeader.MemoryPerformanceMessageEvent, new MemoryPerformanceEvent());
            Register(ClientPacketHeader.SetFriendBarStateMessageEvent, new SetFriendBarStateEvent());
            Register(ClientPacketHeader.GetCraftingListMessageEvent, new GetCraftingListMessageEvent());
            Register(ClientPacketHeader.GetCraftingRecipesAvailableMessageEvent, new GetCraftingRecipesAvailableMessageEvent());
            Register(ClientPacketHeader.CraftSecretMessageEvent, new CraftSecretMessageEvent());
            Register(ClientPacketHeader.GetRecipeConfigMessageEvent, new GetRecipeConfigMessageEvent());
            Register(ClientPacketHeader.CraftedRecipeExecutedMessageEvent, new CraftedRecipeExecutedMessageEvent());
        }

        private void RegisterMessenger()
        {
            Register(ClientPacketHeader.MessengerInitMessageEvent, new MessengerInitEvent());
            Register(ClientPacketHeader.GetBuddyRequestsMessageEvent, new GetBuddyRequestsEvent());
            Register(ClientPacketHeader.FollowFriendMessageEvent, new FollowFriendEvent());
            Register(ClientPacketHeader.FindNewFriendsMessageEvent, new FindNewFriendsEvent());
            Register(ClientPacketHeader.FriendListUpdateMessageEvent, new FriendListUpdateEvent());
            Register(ClientPacketHeader.RemoveBuddyMessageEvent, new RemoveBuddyEvent());
            Register(ClientPacketHeader.RequestBuddyMessageEvent, new RequestBuddyEvent());
            Register(ClientPacketHeader.SendMsgMessageEvent, new SendMsgEvent());
            Register(ClientPacketHeader.SendRoomInviteMessageEvent, new SendRoomInviteEvent());
            Register(ClientPacketHeader.HabboSearchMessageEvent, new HabboSearchEvent());
            Register(ClientPacketHeader.AcceptBuddyMessageEvent, new AcceptBuddyEvent());
            Register(ClientPacketHeader.DeclineBuddyMessageEvent, new DeclineBuddyEvent());
        }

        private void RegisterGroups()
        {
            Register(ClientPacketHeader.JoinGroupMessageEvent, new JoinGroupEvent());
            Register(ClientPacketHeader.RemoveGroupFavouriteMessageEvent, new RemoveGroupFavouriteEvent());
            Register(ClientPacketHeader.SetGroupFavouriteMessageEvent, new SetGroupFavouriteEvent());
            Register(ClientPacketHeader.GetGroupInfoMessageEvent, new GetGroupInfoEvent());
            Register(ClientPacketHeader.GetGroupMembersMessageEvent, new GetGroupMembersEvent());
            Register(ClientPacketHeader.GetGroupCreationWindowMessageEvent, new GetGroupCreationWindowEvent());
            Register(ClientPacketHeader.GetBadgeEditorPartsMessageEvent, new GetBadgeEditorPartsEvent());
            Register(ClientPacketHeader.PurchaseGroupMessageEvent, new PurchaseGroupEvent());
            Register(ClientPacketHeader.UpdateGroupIdentityMessageEvent, new UpdateGroupIdentityEvent());
            Register(ClientPacketHeader.UpdateGroupBadgeMessageEvent, new UpdateGroupBadgeEvent());
            Register(ClientPacketHeader.UpdateGroupColoursMessageEvent, new UpdateGroupColoursEvent());
            Register(ClientPacketHeader.UpdateGroupSettingsMessageEvent, new UpdateGroupSettingsEvent());
            Register(ClientPacketHeader.ManageGroupMessageEvent, new ManageGroupEvent());
            Register(ClientPacketHeader.GiveAdminRightsMessageEvent, new GiveAdminRightsEvent());
            Register(ClientPacketHeader.TakeAdminRightsMessageEvent, new TakeAdminRightsEvent());
            Register(ClientPacketHeader.RemoveGroupMemberMessageEvent, new RemoveGroupMemberEvent());
            Register(ClientPacketHeader.AcceptGroupMembershipMessageEvent, new AcceptGroupMembershipEvent());
            Register(ClientPacketHeader.DeclineGroupMembershipMessageEvent, new DeclineGroupMembershipEvent());
            Register(ClientPacketHeader.DeleteGroupMessageEvent, new DeleteGroupEvent());
            Register(ClientPacketHeader.GetGroupForumsMessageEvent, new GetGroupForumsMessageEvent());
            Register(ClientPacketHeader.GetGroupForumDataMessageEvent, new GetGroupForumDataMessageEvent());
            Register(ClientPacketHeader.GetGroupForumThreadRootMessageEvent, new GetGroupForumThreadRootMessageEvent());
            Register(ClientPacketHeader.UpdateThreadMessageEvent, new UpdateThreadMessageEvent());
            Register(ClientPacketHeader.UpdateForumSettingsMessageEvent, new UpdateForumSettingsMessageEvent());
            Register(ClientPacketHeader.AlterForumThreadStateMessageEvent, new AlterForumThreadStateMessageEvent());
            Register(ClientPacketHeader.PublishForumThreadMessageEvent, new PublishForumThreadMessageEvent());
            Register(ClientPacketHeader.ReadForumThreadMessageEvent, new ReadForumThreadMessageEvent());
            Register(ClientPacketHeader.DeleteGroupPostMessageEvent, new DeleteGroupPostMessageEvent());
        }

        private void RegisterRoomSettings()
        {
            Register(ClientPacketHeader.GetRoomSettingsMessageEvent, new GetRoomSettingsEvent());
            Register(ClientPacketHeader.SaveRoomSettingsMessageEvent, new SaveRoomSettingsEvent());
            Register(ClientPacketHeader.DeleteRoomMessageEvent, new DeleteRoomEvent());
            Register(ClientPacketHeader.ToggleMuteToolMessageEvent, new ToggleMuteToolEvent());
            Register(ClientPacketHeader.GetRoomFilterListMessageEvent, new GetRoomFilterListEvent());
            Register(ClientPacketHeader.ModifyRoomFilterListMessageEvent, new ModifyRoomFilterListEvent());
            Register(ClientPacketHeader.GetRoomRightsMessageEvent, new GetRoomRightsEvent());
            Register(ClientPacketHeader.GetRoomBannedUsersMessageEvent, new GetRoomBannedUsersEvent());
            Register(ClientPacketHeader.UnbanUserFromRoomMessageEvent, new UnbanUserFromRoomEvent());
            Register(ClientPacketHeader.SaveEnforcedCategorySettingsMessageEvent, new SaveEnforcedCategorySettingsEvent());
            Register(ClientPacketHeader.AcceptPollMessageEvent, new AcceptPollMessageEvent());
            Register(ClientPacketHeader.RefusePollMessageEvent, new RefusePollMessageEvent());
        }

        private void RegisterPets()
        {
            Register(ClientPacketHeader.RespectPetMessageEvent, new RespectPetEvent());
            Register(ClientPacketHeader.GetPetInformationMessageEvent, new GetPetInformationEvent());
            Register(ClientPacketHeader.PickUpPetMessageEvent, new PickUpPetEvent());
            Register(ClientPacketHeader.PlacePetMessageEvent, new PlacePetEvent());
            Register(ClientPacketHeader.RideHorseMessageEvent, new RideHorseEvent());
            Register(ClientPacketHeader.ApplyHorseEffectMessageEvent, new ApplyHorseEffectEvent());
            Register(ClientPacketHeader.RemoveSaddleFromHorseMessageEvent, new RemoveSaddleFromHorseEvent());
            Register(ClientPacketHeader.ModifyWhoCanRideHorseMessageEvent, new ModifyWhoCanRideHorseEvent());
            Register(ClientPacketHeader.GetPetTrainingPanelMessageEvent, new GetPetTrainingPanelEvent());
        }

        private void RegisterBots()
        {
            Register(ClientPacketHeader.PlaceBotMessageEvent, new PlaceBotEvent());
            Register(ClientPacketHeader.PickUpBotMessageEvent, new PickUpBotEvent());
            Register(ClientPacketHeader.OpenBotActionMessageEvent, new OpenBotActionEvent());
            Register(ClientPacketHeader.SaveBotActionMessageEvent, new SaveBotActionEvent());
        }

        private void RegisterFurni()
        {
            Register(ClientPacketHeader.UpdateMagicTileMessageEvent, new UpdateMagicTileEvent2());
            Register(ClientPacketHeader.GetYouTubeTelevisionMessageEvent, new GetYouTubeTelevisionEvent());
            Register(ClientPacketHeader.GetRentableSpaceMessageEvent, new GetRentableSpaceEvent());
            Register(ClientPacketHeader.PurchaseRentableSpaceMessageEvent, new PurchaseRentableSpaceEvent());
            Register(ClientPacketHeader.CancelRentableSpaceMessageEvent, new CancelRentableSpaceEvent());
            Register(ClientPacketHeader.ToggleYouTubeVideoMessageEvent, new ToggleYouTubeVideoEvent());
            Register(ClientPacketHeader.YouTubeVideoInformationMessageEvent, new YouTubeVideoInformationEvent());
            Register(ClientPacketHeader.YouTubeGetNextVideo, new YouTubeGetNextVideo());
            Register(ClientPacketHeader.SaveWiredTriggerConfigMessageEvent, new SaveWiredConfigEvent());
            Register(ClientPacketHeader.SaveWiredEffectConfigMessageEvent, new SaveWiredConfigEvent());
            Register(ClientPacketHeader.SaveWiredConditionConfigMessageEvent, new SaveWiredConfigEvent());
            Register(ClientPacketHeader.SaveBrandingItemMessageEvent, new SaveBrandingItemEvent());
            Register(ClientPacketHeader.SetTonerMessageEvent, new SetTonerEvent());
            Register(ClientPacketHeader.DiceOffMessageEvent, new DiceOffEvent());
            Register(ClientPacketHeader.ThrowDiceMessageEvent, new ThrowDiceEvent());
            Register(ClientPacketHeader.SetMannequinNameMessageEvent, new SetMannequinNameEvent());
            Register(ClientPacketHeader.SetMannequinFigureMessageEvent, new SetMannequinFigureEvent());
            Register(ClientPacketHeader.CreditFurniRedeemMessageEvent, new CreditFurniRedeemEvent());
            Register(ClientPacketHeader.GetStickyNoteMessageEvent, new GetStickyNoteEvent());
            Register(ClientPacketHeader.AddStickyNoteMessageEvent, new AddStickyNoteEvent());
            Register(ClientPacketHeader.UpdateStickyNoteMessageEvent, new UpdateStickyNoteEvent());
            Register(ClientPacketHeader.DeleteStickyNoteMessageEvent, new DeleteStickyNoteEvent());
            Register(ClientPacketHeader.GetMoodlightConfigMessageEvent, new GetMoodlightConfigEvent());
            Register(ClientPacketHeader.MoodlightUpdateMessageEvent, new MoodlightUpdateEvent());
            Register(ClientPacketHeader.ToggleMoodlightMessageEvent, new ToggleMoodlightEvent());
            Register(ClientPacketHeader.UseOneWayGateMessageEvent, new UseFurnitureEvent());
            Register(ClientPacketHeader.UseHabboWheelMessageEvent, new UseFurnitureEvent());
            Register(ClientPacketHeader.OpenGiftMessageEvent, new OpenGiftEvent());
            Register(ClientPacketHeader.GetGroupFurniSettingsMessageEvent, new GetGroupFurniSettingsEvent());
            Register(ClientPacketHeader.UseSellableClothingMessageEvent, new UseSellableClothingEvent());
            Register(ClientPacketHeader.ConfirmLoveLockMessageEvent, new ConfirmLoveLockEvent());
            Register(ClientPacketHeader.FurniEditorBySpriteEvent, new FurniEditorBySpriteEvent());
            Register(ClientPacketHeader.FurniEditorDeleteEvent, new FurniEditorDeleteEvent());
            Register(ClientPacketHeader.FurniEditorDetailEvent, new FurniEditorDetailEvent());
            Register(ClientPacketHeader.FurniEditorInteractionsEvent, new FurniEditorInteractionsEvent());
            Register(ClientPacketHeader.FurniEditorSearchEvent, new FurniEditorSearchEvent());
            Register(ClientPacketHeader.FurniEditorUpdateEvent, new FurniEditorUpdateEvent());
        }

        private void FloorPlanEditor()
        {
            Register(ClientPacketHeader.SaveFloorPlanModelMessageEvent, new SaveFloorPlanModelEvent());
            Register(ClientPacketHeader.InitializeFloorPlanSessionMessageEvent, new InitializeFloorPlanSessionEvent());
            Register(ClientPacketHeader.FloorPlanEditorRoomPropertiesMessageEvent, new FloorPlanEditorRoomPropertiesEvent());
        }

        private void RegisterModeration()
        {
            Register(ClientPacketHeader.SendHelpTicketMessageEvent, new SendHelpTicketEvent());
            Register(ClientPacketHeader.OpenHelpToolMessageEvent, new OpenHelpToolEvent());
            Register(ClientPacketHeader.GetModeratorRoomInfoMessageEvent, new GetModeratorRoomInfoEvent());
            Register(ClientPacketHeader.GetModeratorUserInfoMessageEvent, new GetModeratorUserInfoEvent());
            Register(ClientPacketHeader.GetModeratorUserRoomVisitsMessageEvent, new GetModeratorUserRoomVisitsEvent());
            Register(ClientPacketHeader.ModerateRoomMessageEvent, new ModerateRoomEvent());
            Register(ClientPacketHeader.ModeratorActionMessageEvent, new ModeratorActionEvent());
            Register(ClientPacketHeader.SubmitNewTicketMessageEvent, new SubmitNewTicketEvent());
            Register(ClientPacketHeader.GetModeratorRoomChatlogMessageEvent, new GetModeratorRoomChatlogEvent());
            Register(ClientPacketHeader.GetModeratorUserChatlogMessageEvent, new GetModeratorUserChatlogEvent());
            Register(ClientPacketHeader.GetModeratorTicketChatlogsMessageEvent, new GetModeratorTicketChatlogsEvent());
            Register(ClientPacketHeader.PickTicketMessageEvent, new PickTicketEvent());
            Register(ClientPacketHeader.ReleaseTicketMessageEvent, new ReleaseTicketEvent());
            Register(ClientPacketHeader.CloseTicketMesageEvent, new CloseTicketEvent());
            Register(ClientPacketHeader.ModerationMuteMessageEvent, new ModerationMuteEvent());
            Register(ClientPacketHeader.ModerationKickMessageEvent, new ModerationKickEvent());
            Register(ClientPacketHeader.ModerationBanMessageEvent, new ModerationBanEvent());
            Register(ClientPacketHeader.ModerationMsgMessageEvent, new ModerationMsgEvent());
            Register(ClientPacketHeader.ModerationCautionMessageEvent, new ModerationCautionEvent());
            Register(ClientPacketHeader.ModerationTradeLockMessageEvent, new ModerationTradeLockEvent());
            Register(ClientPacketHeader.GetHelperToolConfigurationMessageEvent, new GetHelperToolConfigurationMessageEvent());
            Register(ClientPacketHeader.OnGuideSessionDetachedMessageEvent, new OnGuideSessionDetachedMessageEvent());
            Register(ClientPacketHeader.GuideToolMessageNew, new GuideToolMessageNew());
            Register(ClientPacketHeader.GuideInviteToRoom, new GuideInviteToRoom());
            Register(ClientPacketHeader.VisitRoomGuides, new VisitRoomGuides());
            Register(ClientPacketHeader.GuideEndSession, new GuideEndSession());
            Register(ClientPacketHeader.OnGuideSessionTyping, new OnGuideSessionTyping());
            Register(ClientPacketHeader.OnGuideMessageEvent, new OnGuideMessageEvent());
            Register(ClientPacketHeader.OnGuideFeedbackMessageEvent, new OnGuideFeedbackMessageEvent());
            Register(ClientPacketHeader.AmbassadorWarningMessageEvent, new AmbassadorWarningEvent());
        }

        public void RegisterGameCenter()
        {
            Register(ClientPacketHeader.GetGameListingMessageEvent, new GetGameListingEvent());
            Register(ClientPacketHeader.InitializeGameCenterMessageEvent, new InitializeGameCenterEvent());
            Register(ClientPacketHeader.GetPlayableGamesMessageEvent, new GetPlayableGamesEvent());
            Register(ClientPacketHeader.JoinPlayerQueueMessageEvent, new JoinPlayerQueueEvent());
            Register(ClientPacketHeader.Game2GetWeeklyLeaderboardMessageEvent, new Game2GetWeeklyLeaderboardEvent());
        }

        public void RegisterNames()
        {
            // ✅ FIX #10: Usar un helper para nombres también, evita duplicados silenciosos
            void AddName(int id, string name)
            {
                if (!_packetNames.ContainsKey(id))
                    _packetNames[id] = name;
            }

            AddName(ClientPacketHeader.GetClientVersionMessageEvent, "GetClientVersionEvent");
            AddName(ClientPacketHeader.InitCryptoMessageEvent, "InitCryptoEvent");
            AddName(ClientPacketHeader.GenerateSecretKeyMessageEvent, "GenerateSecretKeyEvent");
            AddName(ClientPacketHeader.UniqueIDMessageEvent, "UniqueIDEvent");
            AddName(ClientPacketHeader.SSOTicketMessageEvent, "SSOTicketEvent");
            AddName(ClientPacketHeader.InfoRetrieveMessageEvent, "InfoRetrieveEvent");
            AddName(ClientPacketHeader.PingMessageEvent, "PingEvent");
            AddName(ClientPacketHeader.RefreshCampaignMessageEvent, "RefreshCampaignEvent");
            AddName(ClientPacketHeader.GetPromoArticlesMessageEvent, "RefreshPromoEvent");
            AddName(ClientPacketHeader.GetCatalogModeMessageEvent, "GetCatalogModeEvent");
            AddName(ClientPacketHeader.GetCatalogIndexMessageEvent, "GetCatalogIndexEvent");
            AddName(ClientPacketHeader.GetCatalogPageMessageEvent, "GetCatalogPageEvent");
            AddName(ClientPacketHeader.GetCatalogOfferMessageEvent, "GetCatalogOfferEvent");
            AddName(ClientPacketHeader.PurchaseFromCatalogMessageEvent, "PurchaseFromCatalogEvent");
            AddName(ClientPacketHeader.PurchaseFromCatalogAsGiftMessageEvent, "PurchaseFromCatalogAsGiftEvent");
            AddName(ClientPacketHeader.PurchaseRoomPromotionMessageEvent, "PurchaseRoomPromotionEvent");
            AddName(ClientPacketHeader.GetGiftWrappingConfigurationMessageEvent, "GetGiftWrappingConfigurationEvent");
            AddName(ClientPacketHeader.GetMarketplaceConfigurationMessageEvent, "GetMarketplaceConfigurationEvent");
            AddName(ClientPacketHeader.CheckPetNameMessageEvent, "CheckPetNameEvent");
            AddName(ClientPacketHeader.RedeemVoucherMessageEvent, "RedeemVoucherEvent");
            AddName(ClientPacketHeader.GetPromotableRoomsMessageEvent, "GetPromotableRoomsEvent");
            AddName(ClientPacketHeader.GetCatalogRoomPromotionMessageEvent, "GetCatalogRoomPromotionEvent");
            AddName(ClientPacketHeader.GetGroupFurniConfigMessageEvent, "GetGroupFurniConfigEvent");
            AddName(ClientPacketHeader.CheckGnomeNameMessageEvent, "CheckGnomeNameEvent");
            AddName(ClientPacketHeader.GetOffersMessageEvent, "GetOffersEvent");
            AddName(ClientPacketHeader.GetOwnOffersMessageEvent, "GetOwnOffersEvent");
            AddName(ClientPacketHeader.GetMarketplaceCanMakeOfferMessageEvent, "GetMarketplaceCanMakeOfferEvent");
            AddName(ClientPacketHeader.GetMarketplaceItemStatsMessageEvent, "GetMarketplaceItemStatsEvent");
            AddName(ClientPacketHeader.MakeOfferMessageEvent, "MakeOfferEvent");
            AddName(ClientPacketHeader.CancelOfferMessageEvent, "CancelOfferEvent");
            AddName(ClientPacketHeader.BuyOfferMessageEvent, "BuyOfferEvent");
            AddName(ClientPacketHeader.RedeemOfferCreditsMessageEvent, "RedeemOfferCreditsEvent");
            AddName(ClientPacketHeader.AddFavouriteRoomMessageEvent, "AddFavouriteRoomEvent");
            AddName(ClientPacketHeader.GetUserFlatCatsMessageEvent, "GetUserFlatCatsEvent");
            AddName(ClientPacketHeader.DeleteFavouriteRoomMessageEvent, "RemoveFavouriteRoomEvent");
            AddName(ClientPacketHeader.GoToHotelViewMessageEvent, "GoToHotelViewEvent");
            AddName(ClientPacketHeader.UpdateNavigatorSettingsMessageEvent, "UpdateNavigatorSettingsEvent");
            AddName(ClientPacketHeader.CanCreateRoomMessageEvent, "CanCreateRoomEvent");
            AddName(ClientPacketHeader.CreateFlatMessageEvent, "CreateFlatEvent");
            AddName(ClientPacketHeader.GetGuestRoomMessageEvent, "GetGuestRoomEvent");
            AddName(ClientPacketHeader.EditRoomPromotionMessageEvent, "EditRoomEventEvent");
            AddName(ClientPacketHeader.GetEventCategoriesMessageEvent, "GetNavigatorFlatsEvent");
            AddName(ClientPacketHeader.InitializeNewNavigatorMessageEvent, "InitializeNewNavigatorEvent");
            AddName(ClientPacketHeader.NewNavigatorSearchMessageEvent, "NewNavigatorSearchEvent");
            AddName(ClientPacketHeader.FindRandomFriendingRoomMessageEvent, "FindRandomFriendingRoomEvent");
            AddName(ClientPacketHeader.GetQuestListMessageEvent, "GetQuestListEvent");
            AddName(ClientPacketHeader.StartQuestMessageEvent, "StartQuestEvent");
            AddName(ClientPacketHeader.CancelQuestMessageEvent, "CancelQuestEvent");
            AddName(ClientPacketHeader.GetCurrentQuestMessageEvent, "GetCurrentQuestEvent");
            AddName(ClientPacketHeader.OnBullyClickMessageEvent, "OnBullyClickEvent");
            AddName(ClientPacketHeader.SendBullyReportMessageEvent, "SendBullyReportEvent");
            AddName(ClientPacketHeader.SubmitBullyReportMessageEvent, "SubmitBullyReportEvent");
            AddName(ClientPacketHeader.LetUserInMessageEvent, "LetUserInEvent");
            AddName(ClientPacketHeader.BanUserMessageEvent, "BanUserEvent");
            AddName(ClientPacketHeader.KickUserMessageEvent, "KickUserEvent");
            AddName(ClientPacketHeader.AssignRightsMessageEvent, "AssignRightsEvent");
            AddName(ClientPacketHeader.RemoveRightsMessageEvent, "RemoveRightsEvent");
            AddName(ClientPacketHeader.RemoveAllRightsMessageEvent, "RemoveAllRightsEvent");
            AddName(ClientPacketHeader.MuteUserMessageEvent, "MuteUserEvent");
            AddName(ClientPacketHeader.GiveHandItemMessageEvent, "GiveHandItemEvent");
            AddName(ClientPacketHeader.GetWardrobeMessageEvent, "GetWardrobeEvent");
            AddName(ClientPacketHeader.SaveWardrobeOutfitMessageEvent, "SaveWardrobeOutfitEvent");
            AddName(ClientPacketHeader.ActionMessageEvent, "ActionEvent");
            AddName(ClientPacketHeader.ApplySignMessageEvent, "ApplySignEvent");
            AddName(ClientPacketHeader.DanceMessageEvent, "DanceEvent");
            AddName(ClientPacketHeader.SitMessageEvent, "SitEvent");
            AddName(ClientPacketHeader.ChangeMottoMessageEvent, "ChangeMottoEvent");
            AddName(ClientPacketHeader.LookToMessageEvent, "LookToEvent");
            AddName(ClientPacketHeader.DropHandItemMessageEvent, "DropHandItemEvent");
            AddName(ClientPacketHeader.GiveRoomScoreMessageEvent, "GiveRoomScoreEvent");
            AddName(ClientPacketHeader.IgnoreUserMessageEvent, "IgnoreUserEvent");
            AddName(ClientPacketHeader.UnIgnoreUserMessageEvent, "UnIgnoreUserEvent");
            AddName(ClientPacketHeader.OpenFlatConnectionMessageEvent, "OpenFlatConnectionEvent");
            AddName(ClientPacketHeader.GoToFlatMessageEvent, "GoToFlatEvent");
            AddName(ClientPacketHeader.ChatMessageEvent, "ChatEvent");
            AddName(ClientPacketHeader.ShoutMessageEvent, "ShoutEvent");
            AddName(ClientPacketHeader.WhisperMessageEvent, "WhisperEvent");
            AddName(ClientPacketHeader.StartTypingMessageEvent, "StartTypingEvent");
            AddName(ClientPacketHeader.CancelTypingMessageEvent, "CancelTypingEvent");
            AddName(ClientPacketHeader.GetRoomEntryDataMessageEvent, "GetRoomEntryDataEvent");
            AddName(ClientPacketHeader.GetFurnitureAliasesMessageEvent, "GetFurnitureAliasesEvent");
            AddName(ClientPacketHeader.MoveAvatarMessageEvent, "MoveAvatarEvent");
            AddName(ClientPacketHeader.MoveObjectMessageEvent, "MoveObjectEvent");
            AddName(ClientPacketHeader.UpdateFurniturePositionEvent, "UpdateFurniturePositionEvent");
            AddName(ClientPacketHeader.PickupObjectMessageEvent, "PickupObjectEvent");
            AddName(ClientPacketHeader.MoveWallItemMessageEvent, "MoveWallItemEvent");
            AddName(ClientPacketHeader.ApplyDecorationMessageEvent, "ApplyDecorationEvent");
            AddName(ClientPacketHeader.PlaceObjectMessageEvent, "PlaceObjectEvent");
            AddName(ClientPacketHeader.UseFurnitureMessageEvent, "UseFurnitureEvent");
            AddName(ClientPacketHeader.UseWallItemMessageEvent, "UseWallItemEvent");
            AddName(ClientPacketHeader.InitTradeMessageEvent, "InitTradeEvent");
            AddName(ClientPacketHeader.TradingOfferItemMessageEvent, "TradingOfferItemEvent");
            AddName(ClientPacketHeader.TradingRemoveItemMessageEvent, "TradingRemoveItemEvent");
            AddName(ClientPacketHeader.TradingAcceptMessageEvent, "TradingAcceptEvent");
            AddName(ClientPacketHeader.TradingCancelMessageEvent, "TradingCancelEvent");
            AddName(ClientPacketHeader.TradingConfirmMessageEvent, "TradingConfirmEvent");
            AddName(ClientPacketHeader.TradingModifyMessageEvent, "TradingModifyEvent");
            AddName(ClientPacketHeader.TradingCancelConfirmMessageEvent, "TradingCancelConfirmEvent");
            AddName(ClientPacketHeader.RequestFurniInventoryMessageEvent, "RequestFurniInventoryEvent");
            AddName(ClientPacketHeader.GetBadgesMessageEvent, "GetBadgesEvent");
            AddName(ClientPacketHeader.GetAchievementsMessageEvent, "GetAchievementsEvent");
            AddName(ClientPacketHeader.SetActivatedBadgesMessageEvent, "SetActivatedBadgesEvent");
            AddName(ClientPacketHeader.GetBotInventoryMessageEvent, "GetBotInventoryEvent");
            AddName(ClientPacketHeader.GetPetInventoryMessageEvent, "GetPetInventoryEvent");
            AddName(ClientPacketHeader.AvatarEffectActivatedMessageEvent, "AvatarEffectActivatedEvent");
            AddName(ClientPacketHeader.AvatarEffectSelectedMessageEvent, "AvatarEffectSelectedEvent");
            AddName(ClientPacketHeader.GetTalentTrackMessageEvent, "GetTalentTrackEvent");
            AddName(ClientPacketHeader.GetCreditsInfoMessageEvent, "GetCreditsInfoEvent");
            AddName(ClientPacketHeader.GetHabboClubWindowMessageEvent, "GetHabboClubWindowEvent");
            AddName(ClientPacketHeader.GetHabboClubCenterInfoMessageEvent, "GetHabboClubCenterInfoMessageEvent");
            AddName(ClientPacketHeader.ScrGetUserInfoMessageEvent, "ScrGetUserInfoEvent");
            AddName(ClientPacketHeader.SetChatPreferenceMessageEvent, "SetChatPreferenceEvent");
            AddName(ClientPacketHeader.SetUserFocusPreferenceEvent, "SetUserFocusPreferenceEvent");
            AddName(ClientPacketHeader.SetMessengerInviteStatusMessageEvent, "SetMessengerInviteStatusEvent");
            AddName(ClientPacketHeader.RespectUserMessageEvent, "RespectUserEvent");
            AddName(ClientPacketHeader.UpdateFigureDataMessageEvent, "UpdateFigureDataEvent");
            AddName(ClientPacketHeader.OpenPlayerProfileMessageEvent, "OpenPlayerProfileEvent");
            AddName(ClientPacketHeader.GetSelectedBadgesMessageEvent, "GetSelectedBadgesEvent");
            AddName(ClientPacketHeader.GetRelationshipsMessageEvent, "GetRelationshipsEvent");
            AddName(ClientPacketHeader.SetRelationshipMessageEvent, "SetRelationshipEvent");
            AddName(ClientPacketHeader.CheckValidNameMessageEvent, "CheckValidNameEvent");
            AddName(ClientPacketHeader.ChangeNameMessageEvent, "ChangeNameEvent");
            AddName(ClientPacketHeader.SetUsernameMessageEvent, "SetUsernameEvent");
            AddName(ClientPacketHeader.GetHabboGroupBadgesMessageEvent, "GetHabboGroupBadgesEvent");
            AddName(ClientPacketHeader.GetUserTagsMessageEvent, "GetUserTagsEvent");
            AddName(ClientPacketHeader.EventTrackerMessageEvent, "EventTrackerEvent");
            AddName(ClientPacketHeader.ClientVariablesMessageEvent, "ClientVariablesEvent");
            AddName(ClientPacketHeader.DisconnectionMessageEvent, "DisconnectEvent");
            AddName(ClientPacketHeader.LatencyTestMessageEvent, "LatencyTestEvent");
            AddName(ClientPacketHeader.MemoryPerformanceMessageEvent, "MemoryPerformanceEvent");
            AddName(ClientPacketHeader.SetFriendBarStateMessageEvent, "SetFriendBarStateEvent");
            AddName(ClientPacketHeader.MessengerInitMessageEvent, "MessengerInitEvent");
            AddName(ClientPacketHeader.GetBuddyRequestsMessageEvent, "GetBuddyRequestsEvent");
            AddName(ClientPacketHeader.FollowFriendMessageEvent, "FollowFriendEvent");
            AddName(ClientPacketHeader.FindNewFriendsMessageEvent, "FindNewFriendsEvent");
            AddName(ClientPacketHeader.FriendListUpdateMessageEvent, "FriendListUpdateEvent");
            AddName(ClientPacketHeader.RemoveBuddyMessageEvent, "RemoveBuddyEvent");
            AddName(ClientPacketHeader.RequestBuddyMessageEvent, "RequestBuddyEvent");
            AddName(ClientPacketHeader.SendMsgMessageEvent, "SendMsgEvent");
            AddName(ClientPacketHeader.SendRoomInviteMessageEvent, "SendRoomInviteEvent");
            AddName(ClientPacketHeader.HabboSearchMessageEvent, "HabboSearchEvent");
            AddName(ClientPacketHeader.AcceptBuddyMessageEvent, "AcceptBuddyEvent");
            AddName(ClientPacketHeader.DeclineBuddyMessageEvent, "DeclineBuddyEvent");
            AddName(ClientPacketHeader.JoinGroupMessageEvent, "JoinGroupEvent");
            AddName(ClientPacketHeader.RemoveGroupFavouriteMessageEvent, "RemoveGroupFavouriteEvent");
            AddName(ClientPacketHeader.SetGroupFavouriteMessageEvent, "SetGroupFavouriteEvent");
            AddName(ClientPacketHeader.GetGroupInfoMessageEvent, "GetGroupInfoEvent");
            AddName(ClientPacketHeader.GetGroupMembersMessageEvent, "GetGroupMembersEvent");
            AddName(ClientPacketHeader.GetGroupCreationWindowMessageEvent, "GetGroupCreationWindowEvent");
            AddName(ClientPacketHeader.GetBadgeEditorPartsMessageEvent, "GetBadgeEditorPartsEvent");
            AddName(ClientPacketHeader.PurchaseGroupMessageEvent, "PurchaseGroupEvent");
            AddName(ClientPacketHeader.UpdateGroupIdentityMessageEvent, "UpdateGroupIdentityEvent");
            AddName(ClientPacketHeader.UpdateGroupBadgeMessageEvent, "UpdateGroupBadgeEvent");
            AddName(ClientPacketHeader.UpdateGroupColoursMessageEvent, "UpdateGroupColoursEvent");
            AddName(ClientPacketHeader.UpdateGroupSettingsMessageEvent, "UpdateGroupSettingsEvent");
            AddName(ClientPacketHeader.ManageGroupMessageEvent, "ManageGroupEvent");
            AddName(ClientPacketHeader.GiveAdminRightsMessageEvent, "GiveAdminRightsEvent");
            AddName(ClientPacketHeader.TakeAdminRightsMessageEvent, "TakeAdminRightsEvent");
            AddName(ClientPacketHeader.RemoveGroupMemberMessageEvent, "RemoveGroupMemberEvent");
            AddName(ClientPacketHeader.AcceptGroupMembershipMessageEvent, "AcceptGroupMembershipEvent");
            AddName(ClientPacketHeader.DeclineGroupMembershipMessageEvent, "DeclineGroupMembershipEvent");
            AddName(ClientPacketHeader.DeleteGroupMessageEvent, "DeleteGroupEvent");
            AddName(ClientPacketHeader.GetGroupForumsMessageEvent, "GetGroupForumsMessageEvent");
            AddName(ClientPacketHeader.GetGroupForumDataMessageEvent, "GetGroupForumDataMessageEvent");
            AddName(ClientPacketHeader.GetGroupForumThreadRootMessageEvent, "GetGroupForumThreadRootMessageEvent");
            AddName(ClientPacketHeader.UpdateThreadMessageEvent, "UpdateThreadMessageEvent");
            AddName(ClientPacketHeader.UpdateForumSettingsMessageEvent, "UpdateForumSettingsMessageEvent");
            AddName(ClientPacketHeader.AlterForumThreadStateMessageEvent, "AlterForumThreadStateMessageEvent");
            AddName(ClientPacketHeader.PublishForumThreadMessageEvent, "PublishForumThreadMessageEvent");
            AddName(ClientPacketHeader.ReadForumThreadMessageEvent, "ReadForumThreadMessageEvent");
            AddName(ClientPacketHeader.DeleteGroupPostMessageEvent, "DeleteGroupPostMessageEvent");
            AddName(ClientPacketHeader.GetRoomSettingsMessageEvent, "GetRoomSettingsEvent");
            AddName(ClientPacketHeader.SaveRoomSettingsMessageEvent, "SaveRoomSettingsEvent");
            AddName(ClientPacketHeader.DeleteRoomMessageEvent, "DeleteRoomEvent");
            AddName(ClientPacketHeader.ToggleMuteToolMessageEvent, "ToggleMuteToolEvent");
            AddName(ClientPacketHeader.GetRoomFilterListMessageEvent, "GetRoomFilterListEvent");
            AddName(ClientPacketHeader.ModifyRoomFilterListMessageEvent, "ModifyRoomFilterListEvent");
            AddName(ClientPacketHeader.GetRoomRightsMessageEvent, "GetRoomRightsEvent");
            AddName(ClientPacketHeader.GetRoomBannedUsersMessageEvent, "GetRoomBannedUsersEvent");
            AddName(ClientPacketHeader.UnbanUserFromRoomMessageEvent, "UnbanUserFromRoomEvent");
            AddName(ClientPacketHeader.SaveEnforcedCategorySettingsMessageEvent, "SaveEnforcedCategorySettingsEvent");
            AddName(ClientPacketHeader.RespectPetMessageEvent, "RespectPetEvent");
            AddName(ClientPacketHeader.GetPetInformationMessageEvent, "GetPetInformationEvent");
            AddName(ClientPacketHeader.PickUpPetMessageEvent, "PickUpPetEvent");
            AddName(ClientPacketHeader.PlacePetMessageEvent, "PlacePetEvent");
            AddName(ClientPacketHeader.RideHorseMessageEvent, "RideHorseEvent");
            AddName(ClientPacketHeader.ApplyHorseEffectMessageEvent, "ApplyHorseEffectEvent");
            AddName(ClientPacketHeader.RemoveSaddleFromHorseMessageEvent, "RemoveSaddleFromHorseEvent");
            AddName(ClientPacketHeader.ModifyWhoCanRideHorseMessageEvent, "ModifyWhoCanRideHorseEvent");
            AddName(ClientPacketHeader.GetPetTrainingPanelMessageEvent, "GetPetTrainingPanelEvent");
            AddName(ClientPacketHeader.PlaceBotMessageEvent, "PlaceBotEvent");
            AddName(ClientPacketHeader.PickUpBotMessageEvent, "PickUpBotEvent");
            AddName(ClientPacketHeader.OpenBotActionMessageEvent, "OpenBotActionEvent");
            AddName(ClientPacketHeader.SaveBotActionMessageEvent, "SaveBotActionEvent");
            AddName(ClientPacketHeader.UpdateMagicTileMessageEvent, "UpdateMagicTileEvent2");
            AddName(ClientPacketHeader.GetYouTubeTelevisionMessageEvent, "GetYouTubeTelevisionEvent");
            AddName(ClientPacketHeader.GetRentableSpaceMessageEvent, "GetRentableSpaceEvent");
            AddName(ClientPacketHeader.PurchaseRentableSpaceMessageEvent, "PurchaseRentableSpaceEvent");
            AddName(ClientPacketHeader.CancelRentableSpaceMessageEvent, "CancelRentableSpaceEvent");
            AddName(ClientPacketHeader.ToggleYouTubeVideoMessageEvent, "ToggleYouTubeVideoEvent");
            AddName(ClientPacketHeader.YouTubeVideoInformationMessageEvent, "YouTubeVideoInformationEvent");
            AddName(ClientPacketHeader.YouTubeGetNextVideo, "YouTubeGetNextVideo");
            AddName(ClientPacketHeader.SaveWiredTriggerConfigMessageEvent, "SaveWiredConfigEvent");
            AddName(ClientPacketHeader.SaveWiredEffectConfigMessageEvent, "SaveWiredConfigEvent");
            AddName(ClientPacketHeader.SaveWiredConditionConfigMessageEvent, "SaveWiredConfigEvent");
            AddName(ClientPacketHeader.SaveBrandingItemMessageEvent, "SaveBrandingItemEvent");
            AddName(ClientPacketHeader.SetTonerMessageEvent, "SetTonerEvent");
            AddName(ClientPacketHeader.DiceOffMessageEvent, "DiceOffEvent");
            AddName(ClientPacketHeader.ThrowDiceMessageEvent, "ThrowDiceEvent");
            AddName(ClientPacketHeader.SetMannequinNameMessageEvent, "SetMannequinNameEvent");
            AddName(ClientPacketHeader.SetMannequinFigureMessageEvent, "SetMannequinFigureEvent");
            AddName(ClientPacketHeader.CreditFurniRedeemMessageEvent, "CreditFurniRedeemEvent");
            AddName(ClientPacketHeader.GetStickyNoteMessageEvent, "GetStickyNoteEvent");
            AddName(ClientPacketHeader.AddStickyNoteMessageEvent, "AddStickyNoteEvent");
            AddName(ClientPacketHeader.UpdateStickyNoteMessageEvent, "UpdateStickyNoteEvent");
            AddName(ClientPacketHeader.DeleteStickyNoteMessageEvent, "DeleteStickyNoteEvent");
            AddName(ClientPacketHeader.GetMoodlightConfigMessageEvent, "GetMoodlightConfigEvent");
            AddName(ClientPacketHeader.MoodlightUpdateMessageEvent, "MoodlightUpdateEvent");
            AddName(ClientPacketHeader.ToggleMoodlightMessageEvent, "ToggleMoodlightEvent");
            AddName(ClientPacketHeader.UseOneWayGateMessageEvent, "UseFurnitureEvent");
            AddName(ClientPacketHeader.UseHabboWheelMessageEvent, "UseFurnitureEvent");
            AddName(ClientPacketHeader.OpenGiftMessageEvent, "OpenGiftEvent");
            AddName(ClientPacketHeader.GetGroupFurniSettingsMessageEvent, "GetGroupFurniSettingsEvent");
            AddName(ClientPacketHeader.UseSellableClothingMessageEvent, "UseSellableClothingEvent");
            AddName(ClientPacketHeader.ConfirmLoveLockMessageEvent, "ConfirmLoveLockEvent");
            AddName(ClientPacketHeader.SaveFloorPlanModelMessageEvent, "SaveFloorPlanModelEvent");
            AddName(ClientPacketHeader.InitializeFloorPlanSessionMessageEvent, "InitializeFloorPlanSessionEvent");
            AddName(ClientPacketHeader.FloorPlanEditorRoomPropertiesMessageEvent, "FloorPlanEditorRoomPropertiesEvent");
            AddName(ClientPacketHeader.OpenHelpToolMessageEvent, "OpenHelpToolEvent");
            AddName(ClientPacketHeader.GetModeratorRoomInfoMessageEvent, "GetModeratorRoomInfoEvent");
            AddName(ClientPacketHeader.GetModeratorUserInfoMessageEvent, "GetModeratorUserInfoEvent");
            AddName(ClientPacketHeader.GetModeratorUserRoomVisitsMessageEvent, "GetModeratorUserRoomVisitsEvent");
            AddName(ClientPacketHeader.ModerateRoomMessageEvent, "ModerateRoomEvent");
            AddName(ClientPacketHeader.ModeratorActionMessageEvent, "ModeratorActionEvent");
            AddName(ClientPacketHeader.SubmitNewTicketMessageEvent, "SubmitNewTicketEvent");
            AddName(ClientPacketHeader.GetModeratorRoomChatlogMessageEvent, "GetModeratorRoomChatlogEvent");
            AddName(ClientPacketHeader.GetModeratorUserChatlogMessageEvent, "GetModeratorUserChatlogEvent");
            AddName(ClientPacketHeader.GetModeratorTicketChatlogsMessageEvent, "GetModeratorTicketChatlogsEvent");
            AddName(ClientPacketHeader.PickTicketMessageEvent, "PickTicketEvent");
            AddName(ClientPacketHeader.ReleaseTicketMessageEvent, "ReleaseTicketEvent");
            AddName(ClientPacketHeader.CloseTicketMesageEvent, "CloseTicketEvent");
            AddName(ClientPacketHeader.ModerationMuteMessageEvent, "ModerationMuteEvent");
            AddName(ClientPacketHeader.ModerationKickMessageEvent, "ModerationKickEvent");
            AddName(ClientPacketHeader.ModerationBanMessageEvent, "ModerationBanEvent");
            AddName(ClientPacketHeader.ModerationMsgMessageEvent, "ModerationMsgEvent");
            AddName(ClientPacketHeader.ModerationCautionMessageEvent, "ModerationCautionEvent");
            AddName(ClientPacketHeader.ModerationTradeLockMessageEvent, "ModerationTradeLockEvent");
            AddName(ClientPacketHeader.GetGameListingMessageEvent, "GetGameListingEvent");
            AddName(ClientPacketHeader.InitializeGameCenterMessageEvent, "InitializeGameCenterEvent");
            AddName(ClientPacketHeader.GetPlayableGamesMessageEvent, "GetPlayableGamesEvent");
            AddName(ClientPacketHeader.JoinPlayerQueueMessageEvent, "JoinPlayerQueueEvent");
            AddName(ClientPacketHeader.Game2GetWeeklyLeaderboardMessageEvent, "Game2GetWeeklyLeaderboardEvent");
            AddName(ClientPacketHeader.GetClubGiftsMessageEvent, "GetClubGiftsEvent");
            AddName(ClientPacketHeader.GetHelperToolConfigurationMessageEvent, "GetHelperToolConfigurationEvent");
            AddName(ClientPacketHeader.OnGuideSessionDetachedMessageEvent, "OnGuideSessionDetachedEvent");
            AddName(ClientPacketHeader.GuideToolMessageNew, "GuideToolMessageNewEvent");
            AddName(ClientPacketHeader.GuideInviteToRoom, "GuideInviteToRoomEvent");
            AddName(ClientPacketHeader.VisitRoomGuides, "VisitRoomGuidesEvent");
            AddName(ClientPacketHeader.GuideEndSession, "GuideEndSessionEvent");
            AddName(ClientPacketHeader.OnGuideSessionTyping, "OnGuideSessionTypingEvent");
            AddName(ClientPacketHeader.CheckQuizTypeEvent, "CheckQuizType");
            AddName(ClientPacketHeader.PostQuizAnswersMessageEvent, "PostQuizAnswersMessageEvent");
            AddName(ClientPacketHeader.OnGuideMessageEvent, "OnGuideMessageEvent");
            AddName(ClientPacketHeader.OnGuideFeedbackMessageEvent, "OnGuideFeedbackEvent");
            AddName(ClientPacketHeader.AcceptPollMessageEvent, "AcceptPollEvent");
            AddName(ClientPacketHeader.RefusePollMessageEvent, "RefusePollEvent");
            AddName(ClientPacketHeader.RequestCameraConfigurationMessageEvent, "RequestCameraConfigurationEvent");
            AddName(ClientPacketHeader.HabboCameraPictureDataMessageEvent, "HabboCameraPictureDataEvent");
            AddName(ClientPacketHeader.PurchaseCameraPictureMessageEvent, "PurchaseCameraPictureEvent");
            AddName(ClientPacketHeader.SetRoomThumbnailMessageEvent, "SetRoomThumbnailEvent");
            AddName(ClientPacketHeader.PublishCameraPictureMessageEvent, "PublishCameraPictureEvent");
            AddName(ClientPacketHeader.ParticipatePictureCameraCompetitionMessageEvent, "ParticipatePictureCameraCompetitionEvent");
            AddName(ClientPacketHeader.GetCraftingListMessageEvent, "GetCraftingListEvent");
            AddName(ClientPacketHeader.GetCraftingRecipesAvailableMessageEvent, "GetCraftingRecipesAvailableEvent");
            AddName(ClientPacketHeader.CraftSecretMessageEvent, "CraftSecretEvent");
            AddName(ClientPacketHeader.GetRecipeConfigMessageEvent, "GetRecipeConfigEvent");
            AddName(ClientPacketHeader.CraftedRecipeExecutedMessageEvent, "CraftedRecipeExecutedEvent");
            AddName(ClientPacketHeader.UpdateBackgroundDataMessageEvent, "BackgroundEvent");
            AddName(ClientPacketHeader.GetSanctionStatusMessageEvent, "GetSanctionStatusEvent");
            AddName(ClientPacketHeader.SendHelpTicketMessageEvent, "SendHelpTicketEvent");
            AddName(ClientPacketHeader.RequestInventoryRP, "GetWeaponsEvent");
            AddName(ClientPacketHeader.EquipRPInventory, "EquipRPItemEvent");
            AddName(ClientPacketHeader.DesequipRPInventory, "UnEquipRPItemEvent");
            AddName(ClientPacketHeader.AmbassadorWarningMessageEvent, "AmbassadorWarningEvent");
            AddName(ClientPacketHeader.NuxAcceptGiftsMessageEvent, "NuxAcceptGiftsMessageEvent");
            AddName(ClientPacketHeader.RoomNuxAlert, "RoomNuxAlert");
            AddName(ClientPacketHeader.CommunityGoalHallOfFame, "HallOfFame");
            AddName(ClientPacketHeader.GetJukeboxPlaylistMessageEvent, "GetJukeboxPlayListEvent");
            AddName(ClientPacketHeader.LoadJukeboxDiscsMessageEvent, "LoadJukeboxDiscsEvent");
            AddName(ClientPacketHeader.GetJukeboxDiscsDataMessageEvent, "GetJukeboxDiscsDataEvent");
            AddName(ClientPacketHeader.AddDiscToPlayListMessageEvent, "AddDiscToPlayListEvent");
            AddName(ClientPacketHeader.RemoveDiscFromPlayListMessageEvent, "RemoveDiscFromPlayListEvent");
            AddName(ClientPacketHeader.GetSellablePetBreedsMessageEvent, "selleable");
            AddName(ClientPacketHeader.BuyTargettedOfferMessageEvent, "BuyTargettedOfferEvent");
            AddName(ClientPacketHeader.FurniMaticPageEvent, "FurniMaticPageEvent");
            AddName(ClientPacketHeader.FurniMaticRecycleEvent, "FurniMaticRecycleEvent");
            AddName(ClientPacketHeader.FurniMaticRewardsEvent, "FurniMaticRewardsEvent");
            AddName(ClientPacketHeader.RequestFurniDeleteItem, "RequestFurniDeleteItems");
            AddName(ClientPacketHeader.NavigatorSavedSearchMessageEvent, "NavigatorSavedSearchEvent");
            AddName(ClientPacketHeader.DeleteNavigatorSavedSearchMessageEvent, "DeleteNavigatorSavedSearchEvent");
            AddName(ClientPacketHeader.SetSoundSettingsMessageEvent, "SetSoundSettingsEvent");
            AddName(ClientPacketHeader.GetSongInfoMessageEvent, "GetSongInfoEvent");
            AddName(ClientPacketHeader.GetBattlePassMessageEvent, "GetBattlePassEvent");
            AddName(ClientPacketHeader.ClaimBattlePassRewardMessageEvent, "ClaimBattlePassRewardEvent");
           // Add
            AddName(ClientPacketHeader.FurniEditorBySpriteEvent, "FurniEditorBySpriteEvent");
            AddName(ClientPacketHeader.FurniEditorDeleteEvent, "FurniEditorDeleteEvent");
            AddName(ClientPacketHeader.FurniEditorDetailEvent, "FurniEditorDetailEvent");
            AddName(ClientPacketHeader.FurniEditorInteractionsEvent, "FurniEditorInteractionsEvent");
            AddName(ClientPacketHeader.FurniEditorSearchEvent, "FurniEditorSearchEvent");
            AddName(ClientPacketHeader.FurniEditorUpdateEvent, "FurniEditorUpdateEvent");
            AddName(ClientPacketHeader.CatalogAdminCreateOfferEvent, "CatalogAdminCreateOfferEvent");
            AddName(ClientPacketHeader.CatalogAdminCreatePageEvent, "CatalogAdminCreatePageEvent");
            AddName(ClientPacketHeader.CatalogAdminDeleteOfferEvent, "CatalogAdminDeleteOfferEvent");
            AddName(ClientPacketHeader.CatalogAdminDeletePageEvent, "CatalogAdminDeletePageEvent");
            AddName(ClientPacketHeader.CatalogAdminMoveOfferEvent, "CatalogAdminMoveOfferEvent");
            AddName(ClientPacketHeader.CatalogAdminMovePageEvent, "CatalogAdminMovePageEvent");
            AddName(ClientPacketHeader.CatalogAdminPublishEvent, "CatalogAdminPublishEvent");
            AddName(ClientPacketHeader.CatalogAdminSaveOfferEvent, "CatalogAdminSaveOfferEvent");
            AddName(ClientPacketHeader.CatalogAdminSavePageEvent, "CatalogAdminSavePageEvent");
            AddName(ClientPacketHeader.CatalogAdminSavePageIconEvent, "CatalogAdminSavePageIconEvent");
            AddName(ClientPacketHeader.DeletePrefixEvent, "DeletePrefixEvent");
            AddName(ClientPacketHeader.PurchasePrefixEvent, "PurchasePrefixEvent");
            AddName(ClientPacketHeader.RequestUserPrefixesEvent, "RequestUserPrefixesEvent");
            AddName(ClientPacketHeader.SetActivePrefixEvent, "SetActivePrefixEvent");
        }
    }
}