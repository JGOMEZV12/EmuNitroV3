using System;
using System.Linq;
using Polar.Communication.Packets.Incoming;
using Polar.Utilities;
using Polar.HabboHotel.Users;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Catalog;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Catalog;
using Polar.Communication.Packets.Outgoing.Inventory.Purse;
using Polar.Communication.Packets.Outgoing.Inventory.Furni;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Items.Utilities;
using Polar.HabboHotel.Catalog.Utilities;
using Polar.Communication.Packets.Outgoing.Moderation;
using Polar.Communication.Packets.Outgoing.Users;

namespace Polar.Communication.Packets.Incoming.Catalog
{
    public class PurchaseFromCatalogAsGiftEvent : IPacketEvent
    {
        public void Parse(GameClient Session, ClientPacket Packet)
        {
            try
            {
                //Console.WriteLine($"=== PurchaseFromCatalogAsGiftEvent INICIO ===");

                int PageId = Packet.PopInt();
                int ItemId = Packet.PopInt(); // Este es el identificador (puede ser catalog_items.id o offer_id)
                string Data = Packet.PopString();
                string GiftUser = StringCharFilter.Escape(Packet.PopString());
                string GiftMessage = StringCharFilter.Escape(Packet.PopString().Replace(Convert.ToChar(5), ' '));
                int SpriteId = Packet.PopInt();
                int Ribbon = Packet.PopInt();
                int Colour = Packet.PopInt();
                bool dnow = Packet.PopBoolean();

                //Console.WriteLine($"Datos recibidos: PageId={PageId}, ItemId={ItemId}, GiftUser='{GiftUser}', SpriteId={SpriteId}");

                if (PolarEnvironment.GetDBConfig().DBData["gifts_enabled"] != "1")
                {
                    Session.SendNotification("Los gerentes de juego han inhabilitado los regalos");
                    //Console.WriteLine("Regalos deshabilitados");
                    return;
                }

                CatalogItem Item = null;
                CatalogPage Page = null;

                var catalog = PolarEnvironment.GetGame().GetCatalog();

                // CASO 1: PageId = -1 (compra directa por identificador)
                if (PageId == -1)
                {
                    //Console.WriteLine($"Compra regalo directa por identificador: {ItemId}");

                    // PRIMERO: Buscar por OfferId en OfferItems
                    if (catalog.OfferItems.TryGetValue(ItemId, out Item))
                    {
                        //Console.WriteLine($"Item encontrado en OfferItems por OfferId: {Item.Id} - {Item.Name}");

                        // Obtener la página
                        if (catalog.TryGetPage(Item.PageId, out Page))
                        {
                            //Console.WriteLine($"Página encontrada: {Page.Id} - {Page.Caption}");
                            PageId = Item.PageId; // Actualizar PageId al real
                        }
                    }
                    // SEGUNDO: Buscar por CatalogItem.Id (catalog_items.id)
                    else
                    {
                        //Console.WriteLine($"Buscando item en todas las páginas por CatalogItem.Id...");
                        foreach (var catalogPage in catalog.GetPages())
                        {
                            if (catalogPage.Items.TryGetValue(ItemId, out Item))
                            {
                                Page = catalogPage;
                                PageId = Page.Id;
                                //Console.WriteLine($"Item encontrado por CatalogItem.Id en página {Page.Id}: {Item.Id} - {Item.Name}");
                                break;
                            }
                        }
                    }

                    // TERCERO: Buscar por OfferId en las páginas
                    if (Item == null)
                    {
                        //Console.WriteLine($"Buscando por OfferId en páginas...");
                        foreach (var catalogPage in catalog.GetPages())
                        {
                            if (catalogPage.ItemOffers.TryGetValue(ItemId, out Item))
                            {
                                Page = catalogPage;
                                PageId = Page.Id;
                                //Console.WriteLine($"Item encontrado por OfferId en página {Page.Id}: {Item.Id} - {Item.Name}");
                                break;
                            }
                        }
                    }
                }
                // CASO 2: Compra normal por página
                else
                {
                    if (!catalog.TryGetPage(PageId, out Page))
                    {
                        //Console.WriteLine($"Página {PageId} no encontrada");
                        return;
                    }

                    //Console.WriteLine($"Página encontrada: {Page.Id} - {Page.Caption}");

                    if (!Page.Enabled || !Page.Visible || Page.MinimumRank > Session.GetHabbo().Rank ||
                        (Page.MinimumVIP > Session.GetHabbo().VIPRank && Session.GetHabbo().Rank == 1))
                    {
                        //Console.WriteLine("Usuario no tiene acceso a esta página");
                        return;
                    }

                    // Buscar item usando el método mejorado
                    Item = FindCatalogItemInPage(Page, ItemId);
                }

                if (Item == null)
                {
                    //Console.WriteLine($"ERROR: Item no encontrado (PageId={PageId}, ItemId={ItemId})");
                    Session.SendNotification("El artículo no está disponible");
                    return;
                }

                //Console.WriteLine($"Item encontrado: {Item.Id} - {Item.Name}, Tipo: {Item.Data.Type}");

                if (!ItemUtility.CanGiftItem(Item))
                {
                    //Console.WriteLine($"Este item no se puede regalar");
                    Session.SendNotification("Este artículo no se puede regalar");
                    return;
                }

                // Verificar datos del regalo
                if (!PolarEnvironment.GetGame().GetItemManager().GetGift(SpriteId, out ItemData PresentData) ||
                    PresentData.InteractionType != InteractionType.GIFT)
                {
                    //Console.WriteLine($"Datos de regalo inválidos: SpriteId={SpriteId}");
                    Session.SendNotification("Datos de regalo inválidos");
                    return;
                }

                // Validar fondos
                if (Session.GetHabbo().Credits < Item.CostCredits)
                {
                    //Console.WriteLine($"Créditos insuficientes: Necesita {Item.CostCredits}, Tiene {Session.GetHabbo().Credits}");
                    Session.SendMessage(new PresentDeliverErrorMessageComposer(true, false));
                    return;
                }

                if (Session.GetHabbo().Duckets < Item.CostPixels)
                {
                    //Console.WriteLine($"Duckets insuficientes: Necesita {Item.CostPixels}, Tiene {Session.GetHabbo().Duckets}");
                    Session.SendMessage(new PresentDeliverErrorMessageComposer(false, true));
                    return;
                }

                if (Item.CostDiamonds > 0 && Session.GetHabbo().Diamonds < Item.CostDiamonds)
                {
                    //Console.WriteLine($"Diamantes insuficientes: Necesita {Item.CostDiamonds}, Tiene {Session.GetHabbo().Diamonds}");
                    Session.SendNotification("No tienes suficientes diamantes");
                    return;
                }

                // Buscar usuario destinatario
                Habbo Habbo = PolarEnvironment.GetHabboByUsername(GiftUser);
                if (Habbo == null)
                {
                    //Console.WriteLine($"Usuario destinatario no encontrado: {GiftUser}");
                    Session.SendMessage(new GiftWrappingErrorComposer());
                    return;
                }

                //Console.WriteLine($"Destinatario encontrado: {Habbo.Username} (ID: {Habbo.Id})");

                GameClient Receiver = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Habbo.Id);

                if (!Habbo.AllowGifts)
                {
                    //Console.WriteLine($"El usuario {Habbo.Username} no permite regalos");
                    Session.SendNotification("¡Vaya, este usuario no permite regalos para ser enviado a ellos!");
                    return;
                }

                // Validar tiempo entre compras de regalos
                if ((DateTime.Now - Session.GetHabbo().LastGiftPurchaseTime).TotalSeconds <= 15.0)
                {
                    //Console.WriteLine($"Compra de regalos demasiado rápido");
                    Session.SendNotification("¡Estás comprando regalos demasiado rápido! ¡Espere 15 segundos!");

                    Session.GetHabbo().GiftPurchasingWarnings += 1;
                    if (Session.GetHabbo().GiftPurchasingWarnings >= 25)
                        Session.GetHabbo().SessionGiftBlocked = true;
                    return;
                }

                if (Session.GetHabbo().SessionGiftBlocked)
                {
                    //Console.WriteLine($"Usuario bloqueado para comprar regalos");
                    return;
                }

                // Procesar tipos especiales de items
                if (Item.Data.InteractionType == InteractionType.club_1_month ||
                    Item.Data.InteractionType == InteractionType.club_3_month)
                {
                    return; // Club no se puede regalar normalmente
                }

                // Crear datos extra para el regalo
                string ED = $"{GiftUser}{Convert.ToChar(5)}{GiftMessage}{Convert.ToChar(5)}{Session.GetHabbo().Id}{Convert.ToChar(5)}{Item.Data.Id}{Convert.ToChar(5)}{SpriteId}{Convert.ToChar(5)}{Ribbon}{Convert.ToChar(5)}{Colour}";

                string ItemextraData = ProcessItemExtraData(Item, Data, Session);
                if (ItemextraData == null) // null indica error
                {
                    //Console.WriteLine($"Error procesando datos extra del item");
                    return;
                }

                int NewItemId = 0;
                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    // Insertar item temporal para el regalo
                    dbClient.SetQuery($"INSERT INTO `{Polar.Core.DatabaseCompatibility.ItemsTable}` (`{Polar.Core.DatabaseCompatibility.ItemsBaseItemColumn}`,`user_id`,`extra_data`) VALUES (@base_item, @user_id, @extra_data)");
                    dbClient.AddParameter("base_item", PresentData.Id);
                    dbClient.AddParameter("user_id", Habbo.Id);
                    dbClient.AddParameter("extra_data", ED);
                    NewItemId = Convert.ToInt32(dbClient.InsertQuery());

                    // Insertar el presente
                    dbClient.SetQuery("INSERT INTO `user_presents` (`item_id`,`base_id`,`extra_data`) VALUES (@item_id, @base_id, @extra_data)");
                    dbClient.AddParameter("item_id", NewItemId);
                    dbClient.AddParameter("base_id", Item.Data.Id);
                    dbClient.AddParameter("extra_data", string.IsNullOrEmpty(ItemextraData) ? "" : ItemextraData);
                    dbClient.RunQuery();

                    // Eliminar el item temporal (se maneja como presente)
                    dbClient.RunQuery($"DELETE FROM `{Polar.Core.DatabaseCompatibility.ItemsTable}` WHERE `id` = @id LIMIT 1");
                    dbClient.AddParameter("id", NewItemId);
                }

                // Crear el item de regalo
                Item GiveItem = ItemFactory.CreateGiftItem(PresentData, Habbo, ED, ED, NewItemId, 0, 0);
                if (GiveItem != null)
                {
                    if (Receiver != null)
                    {
                        Receiver.GetHabbo().GetInventoryComponent().TryAddItem(GiveItem);
                        Receiver.SendMessage(new FurniListNotificationComposer(GiveItem.Id, 1));
                        Receiver.SendMessage(new PurchaseOKComposer());
                        Receiver.SendMessage(new FurniListAddComposer(GiveItem));
                        Receiver.SendMessage(new FurniListUpdateComposer());
                        //Console.WriteLine($"Regalo entregado a {Receiver.GetHabbo().Username}");
                    }

                    if (Habbo.Id != Session.GetHabbo().Id && !string.IsNullOrWhiteSpace(GiftMessage))
                    {
                        PolarEnvironment.GetGame().GetAchievementManager().ProgressAchievement(Session, "ACH_GiftGiver", 1);
                        //Console.WriteLine($"Logro ACH_GiftGiver progresado");
                    }
                }

                // Debitar monedas
                if (Item.CostCredits > 0)
                {
                    Session.GetHabbo().Credits -= Item.CostCredits;
                    Session.SendMessage(new CreditBalanceComposer(Session.GetHabbo().Credits));
                    //Console.WriteLine($"Créditos debitados: {Item.CostCredits}");
                }

                if (Item.CostPixels > 0)
                {
                    Session.GetHabbo().Duckets -= Item.CostPixels;
                    Session.SendMessage(new HabboActivityPointNotificationComposer(Session.GetHabbo().Duckets, Session.GetHabbo().Duckets));
                    //Console.WriteLine($"Duckets debitados: {Item.CostPixels}");
                }

                if (Item.CostDiamonds > 0)
                {
                    Session.GetHabbo().Diamonds -= Item.CostDiamonds;
                    Session.SendMessage(new HabboActivityPointNotificationComposer(Session.GetHabbo().Diamonds, 0, 5));
                    //Console.WriteLine($"Diamantes debitados: {Item.CostDiamonds}");
                }

                // Enviar confirmación
                Session.SendMessage(new PurchaseOKComposer(Item, PresentData));
                //Console.WriteLine($"Confirmación de compra enviada");

                // Actualizar tiempo de última compra de regalo
                Session.GetHabbo().LastGiftPurchaseTime = DateTime.Now;

                //Console.WriteLine($"=== Regalo enviado exitosamente ===");
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"ERROR en PurchaseFromCatalogAsGiftEvent: {ex.Message}");
                //Console.WriteLine($"Stack trace: {ex.StackTrace}");
                Session.SendNotification("Error al procesar el regalo");
            }
        }

        private CatalogItem FindCatalogItemInPage(CatalogPage page, int identifier)
        {
            //Console.WriteLine($"Buscando item en página {page.Id} con identificador: {identifier}");

            // 1. Buscar por CatalogItem.Id (catalog_items.id) - Lo que el cliente probablemente envía
            if (page.Items.TryGetValue(identifier, out CatalogItem item))
            {
                //Console.WriteLine($"Encontrado por CatalogItem.Id: {identifier}");
                return item;
            }

            // 2. Buscar por OfferId (catalog_items.offer_id)
            if (page.ItemOffers.TryGetValue(identifier, out item))
            {
                //Console.WriteLine($"Encontrado por OfferId: {identifier}");
                return item;
            }

            // 3. Buscar por FurnitureId (catalog_items.item_id)
            foreach (var catalogItem in page.Items.Values)
            {
                if (catalogItem.Id == identifier)
                {
                    //Console.WriteLine($"Encontrado por FurnitureId: {identifier}");
                    return catalogItem;
                }
            }

            //Console.WriteLine($"Item NO encontrado en página {page.Id}: {identifier}");
            return null;
        }

        private string ProcessItemExtraData(CatalogItem item, string data, GameClient session)
        {
            try
            {
                //Console.WriteLine($"Procesando extraData para item tipo: {item.Data.InteractionType}");

                switch (item.Data.InteractionType)
                {
                    case InteractionType.NONE:
                        return "";

                    #region Pet handling
                    case var petType when petType.ToString().StartsWith("pet"):
                        try
                        {
                            string[] Bits = data.Split('\n');
                            if (Bits.Length < 3)
                            {
                                //Console.WriteLine($"Datos de mascota insuficientes: {Bits.Length} partes");
                                return null;
                            }

                            string PetName = Bits[0];
                            string Race = Bits[1];
                            string Color = Bits[2];

                            // Validar nombre
                            if (!PetUtility.CheckPetName(PetName))
                            {
                                //Console.WriteLine($"Nombre de mascota inválido: {PetName}");
                                return null;
                            }

                            // Validar raza
                            if (!int.TryParse(Race, out _) || Race.Length > 2)
                            {
                                //Console.WriteLine($"Raza de mascota inválida: {Race}");
                                return null;
                            }

                            // Validar color
                            if (Color.Length != 6)
                            {
                                //Console.WriteLine($"Color de mascota inválido: {Color} (debe ser 6 caracteres)");
                                return null;
                            }

                            PolarEnvironment.GetGame().GetAchievementManager().ProgressAchievement(session, "ACH_PetLover", 1);
                            //Console.WriteLine($"Logro ACH_PetLover progresado");

                            return data; // Mantener datos originales
                        }
                        catch (Exception ex)
                        {
                            //Console.WriteLine($"Error procesando datos de mascota: {ex.Message}");
                            return null;
                        }
                    #endregion

                    case InteractionType.FLOOR:
                    case InteractionType.WALLPAPER:
                    case InteractionType.LANDSCAPE:
                        double Number = 0;
                        try
                        {
                            if (!string.IsNullOrEmpty(data))
                                Number = double.Parse(data, PolarEnvironment.CultureInfo);
                        }
                        catch
                        {
                            // Si hay error, usar 0
                        }
                        return Number.ToString().Replace(',', '.');

                    case InteractionType.POSTIT:
                        return "FFFF33";

                    case InteractionType.MOODLIGHT:
                        return "1,1,1,#000000,255";

                    case InteractionType.TROPHY:
                        return $"{session.GetHabbo().Username}{Convert.ToChar(9)}{DateTime.Now:dd-MM-yyyy}{Convert.ToChar(9)}{data}";

                    case InteractionType.MANNEQUIN:
                        return "m" + Convert.ToChar(5) + ".ch-210-1321.lg-285-92" + Convert.ToChar(5) + "Default Mannequin";

                    case InteractionType.BADGE_DISPLAY:
                        if (!session.GetHabbo().GetBadgeComponent().HasBadge(data))
                        {
                            session.SendMessage(new BroadcastMessageAlertComposer("¡Vaya!, parece que usted no es dueño de esta insignia."));
                            return null;
                        }
                        return $"{data}{Convert.ToChar(9)}{session.GetHabbo().Username}{Convert.ToChar(9)}{DateTime.Now:dd-MM-yyyy}";

                    default:
                        return data ?? "";
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error en ProcessItemExtraData: {ex.Message}");
                return null;
            }
        }
    }
}