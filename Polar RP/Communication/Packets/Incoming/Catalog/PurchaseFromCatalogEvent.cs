using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing.Catalog;
using Polar.Communication.Packets.Outgoing.Inventory.AvatarEffects;
using Polar.Communication.Packets.Outgoing.Inventory.Bots;
using Polar.Communication.Packets.Outgoing.Inventory.Furni;
using Polar.Communication.Packets.Outgoing.Inventory.Pets;
using Polar.Communication.Packets.Outgoing.Inventory.Purse;
using Polar.Communication.Packets.Outgoing.Moderation;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using Polar.Communication.Packets.Outgoing.Users;
using Polar.Core;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Catalog;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Items.Utilities;
using Polar.HabboHotel.Rooms.AI;
using Polar.HabboHotel.Users.Effects;
using System;
using System.Collections.Generic;

namespace Polar.Communication.Packets.Incoming.Catalog
{
    internal sealed class PurchaseFromCatalogEvent : IPacketEvent
    {
        public void Parse(GameClient session, ClientPacket packet)
        {
            try
            {
                //Console.WriteLine($"=== PurchaseFromCatalogEvent INICIO ===");

                // Validar catálogo habilitado
                if (PolarEnvironment.GetDBConfig().DBData["catalogue_enabled"] != "1")
                {
                    session.SendNotification("Staff han inhabilitado el catálogo");
                    //Console.WriteLine("Catálogo deshabilitado");
                    return;
                }

                // Leer datos del paquete
                int pageId = packet.PopInt();
                int offerId = packet.PopInt();
                string extraData = packet.PopString();
                int amount = packet.PopInt();

               // Console.WriteLine($"Datos recibidos: PageId={pageId}, OfferId={offerId}, ExtraData='{extraData}', Amount={amount}");

                // Validar cantidad
                amount = Math.Clamp(amount, 1, 100);
               // Console.WriteLine($"Cantidad ajustada: {amount}");

                // Obtener el item del catálogo
                CatalogItem catalogItem = null;
                CatalogPage page = null;

                var catalog = PolarEnvironment.GetGame().GetCatalog();

                // DIAGNÓSTICO: Mostrar información sobre la oferta
                if (catalog.OfferItems.ContainsKey(offerId))
                {
                    var debugItem = catalog.OfferItems[offerId];
                    //Console.WriteLine($"DEBUG: OfferId {offerId} existe en OfferItems -> ItemId={debugItem.Id}, PageId={debugItem.PageId}, Name={debugItem.Name}");
                }
                else
                {
                    //Console.WriteLine($"DEBUG: OfferId {offerId} NO existe en OfferItems");
                }

                // ESTRATEGIA MEJORADA: Buscar primero por offerId si pageId es -1
                if (pageId == -1)
                {
                    //Console.WriteLine($"Compra directa por OfferId: {offerId}");

                    // Buscar en todas las páginas por el offerId
                    bool found = false;
                    foreach (var catalogPage in catalog.GetPages())
                    {
                        if (catalogPage.ItemOffers.TryGetValue(offerId, out catalogItem))
                        {
                            page = catalogPage;
                            found = true;
                            //Console.WriteLine($"Item encontrado en página {page.Id}: {catalogItem.Id} - {catalogItem.Name}");
                            break;
                        }
                    }

                    // Si no se encuentra en las páginas, buscar en OfferItems
                    if (!found && catalog.OfferItems.TryGetValue(offerId, out catalogItem))
                    {
                       // Console.WriteLine($"Item encontrado en OfferItems: {catalogItem.Id} - {catalogItem.Name}");

                        // Intentar obtener la página
                        if (catalog.TryGetPage(catalogItem.PageId, out page))
                        {
                           // Console.WriteLine($"Página encontrada: {page.Id} - {page.Caption}");
                        }
                        else
                        {
                           // Console.WriteLine($"WARNING: Página {catalogItem.PageId} no encontrada, continuando sin validación de página");
                        }
                    }

                    if (catalogItem == null)
                    {
                        //Console.WriteLine($"ERROR: No se encontró item para OfferId {offerId}");
                        session.SendNotification("El artículo no está disponible");
                        return;
                    }
                }
                else
                {
                    //Console.WriteLine($"Compra normal por página: {pageId}");

                    // Validar que la página existe
                    if (!catalog.TryGetPage(pageId, out page))
                    {
                        //Console.WriteLine($"ERROR: Página {pageId} no encontrada");
                        session.SendNotification("La página no existe");
                        return;
                    }

                    //Console.WriteLine($"Página encontrada: {page.Id} - {page.Caption}");

                    // Validar permisos de página
                    if (!page.Enabled || !page.Visible)
                    {
                        //Console.WriteLine($"Página no habilitada o visible");
                        session.SendNotification("La página no está disponible");
                        return;
                    }

                    // Validar rango del usuario
                    if (page.MinimumRank > session.GetHabbo().Rank ||
                        (page.MinimumVIP > session.GetHabbo().VIPRank && session.GetHabbo().Rank == 1))
                    {
                        //Console.WriteLine($"Usuario no tiene permisos: Rank={session.GetHabbo().Rank}, VIP={session.GetHabbo().VIPRank}, Requerido: MinRank={page.MinimumRank}, MinVIP={page.MinimumVIP}");
                        session.SendNotification("No tienes permisos para acceder a esta página");
                        return;
                    }

                    // Buscar el item en la página
                    catalogItem = FindCatalogItemInPage(page, offerId);

                    if (catalogItem == null)
                    {
                        //Console.WriteLine($"ERROR: Item no encontrado para OfferId {offerId} en página {pageId}");

                        // Intentar buscar en todas las páginas como fallback
                        //Console.WriteLine($"Buscando como fallback en todas las páginas...");
                        foreach (var catalogPage in catalog.GetPages())
                        {
                            if (catalogPage.ItemOffers.TryGetValue(offerId, out catalogItem))
                            {
                                page = catalogPage;
                                //Console.WriteLine($"Item encontrado en página {page.Id}: {catalogItem.Id} - {catalogItem.Name}");
                                break;
                            }
                        }

                        if (catalogItem == null)
                        {
                            session.SendNotification("El artículo no existe");
                            return;
                        }
                    }
                }

                // Validación final del item
                if (catalogItem == null)
                {
                    Console.WriteLine($"ERROR CRÍTICO: catalogItem es null después de toda la búsqueda");
                    session.SendNotification("Error interno: artículo no encontrado");
                    return;
                }

                //Console.WriteLine($"ITEM FINAL SELECCIONADO: Id={catalogItem.Id}, OfferId={offerId}, Name={catalogItem.Name}, PageId={catalogItem.PageId}");
                //Console.WriteLine($"Datos del item: Type={catalogItem.Data.Type}, Sprite={catalogItem.Data.SpriteId}, Interaction={catalogItem.Data.InteractionType}");
                //Console.WriteLine($"Costos: {catalogItem.CostCredits}c, {catalogItem.CostPixels}d, {catalogItem.CostDiamonds}dm");

                // Procesar la compra
                ProcessPurchase(session, catalogItem, extraData, amount);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR en PurchaseFromCatalogEvent: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                session.SendNotification("Error al procesar la compra");
            }
            finally
            {
                //Console.WriteLine($"=== PurchaseFromCatalogEvent FIN ===\n");
            }
        }

        private CatalogItem FindCatalogItemInPage(CatalogPage page, int identifier)
        {
            //Console.WriteLine($"Buscando item en página {page.Id} con identificador: {identifier}");

            // 1. Buscar por CatalogItem.Id (catalog_items.id) - Lo que el cliente envía
            if (page.Items.TryGetValue(identifier, out CatalogItem item))
            {
                //Console.WriteLine($"Encontrado por CatalogItem.Id: {identifier}");
                //Console.WriteLine($"  -> OfferId en BD: {item.OfferId}, FurnitureId: {item.Id}");
                return item;
            }

            // 2. Buscar por OfferId (catalog_items.offer_id)
            if (page.ItemOffers.TryGetValue(identifier, out item))
            {
                //Console.WriteLine($"Encontrado por OfferId: {identifier}");
                //Console.WriteLine($"  -> CatalogItem.Id: {item.Id}, FurnitureId: {item.Id}");
                return item;
            }

            // 3. Buscar por FurnitureId (catalog_items.item_id)
            foreach (var catalogItem in page.Items.Values)
            {
                if (catalogItem.Id == identifier)
                {
                    //Console.WriteLine($"Encontrado por FurnitureId: {identifier}");
                    //Console.WriteLine($"  -> CatalogItem.Id: {catalogItem.Id}, OfferId: {catalogItem.OfferId}");
                    return catalogItem;
                }
            }

            //Console.WriteLine($"Item NO encontrado en página {page.Id}: {identifier}");
            return null;
        }

        private void ProcessPurchase(GameClient session, CatalogItem item, string extraData, int amount)
        {
           //Console.WriteLine($"=== Procesando compra de '{item.Name}' ===");


            // Calcular costos totales
            int totalCredits = item.CostCredits * amount;
            int totalPixels = item.CostPixels * amount;
            int totalDiamonds = item.CostDiamonds * amount;

            //Console.WriteLine($"Costos totales para {amount} unidad(es):");
            //Console.WriteLine($"  Créditos: {totalCredits} (usuario tiene: {session.GetHabbo().Credits})");
            //Console.WriteLine($"  Pixels: {totalPixels} (usuario tiene: {session.GetHabbo().Duckets})");
            //Console.WriteLine($"  Diamantes: {totalDiamonds} (usuario tiene: {session.GetHabbo().Diamonds})");

            // Validar fondos
            if (session.GetHabbo().Credits < totalCredits)
            {
                //Console.WriteLine($"ERROR: Créditos insuficientes (necesita {totalCredits}, tiene {session.GetHabbo().Credits})");
                session.SendNotification($"Necesitas {totalCredits} créditos");
                return;
            }

            if (session.GetHabbo().Duckets < totalPixels)
            {
                //Console.WriteLine($"ERROR: Pixels insuficientes (necesita {totalPixels}, tiene {session.GetHabbo().Duckets})");
                session.SendNotification($"Necesitas {totalPixels} pixels");
                return;
            }

            if (session.GetHabbo().Diamonds < totalDiamonds)
            {
                //Console.WriteLine($"ERROR: Diamantes insuficientes (necesita {totalDiamonds}, tiene {session.GetHabbo().Diamonds})");
                session.SendNotification($"Necesitas {totalDiamonds} diamantes");
                return;
            }

            // Procesar item limitado
            if (item.IsLimited)
            {
                //Console.WriteLine($"Procesando item limitado: Stack={item.LimitedEditionStack}, Sells={item.LimitedEditionSells}");
                if (!ProcessLimitedItem(item, session))
                    return;
            }

            #region Create the extraData
            switch (item.Data.InteractionType)
            {
                case InteractionType.NONE:
                    extraData = "";
                    break;

                case InteractionType.WHISPER_TILE:
                case InteractionType.SLIDING_DOORS:
                    extraData = "0";
                    break;
                case InteractionType.Cocaina:
                case InteractionType.HEROINA:
                case InteractionType.WEEDMATERIA:
                case InteractionType.WEEDPORRO:
                    extraData = "0";
                    break;

                case InteractionType.GUILD_ITEM:
                case InteractionType.GUILD_GATE:
                case InteractionType.GUILD_FORUM:
                    break;

                case InteractionType.PINATA:
                case InteractionType.PINATATRIGGERED:
                case InteractionType.MAGICEGG:
                case InteractionType.MAGICCHEST:
                    extraData = "0";
                    break;
                case InteractionType.PET:
                    try
                    {

                        string[] Bits = extraData.Split('\n');
                        string PetName = Bits[0];
                        string Race = Bits[1];
                        string Color = Bits[2];

                        int.Parse(Race); // to trigger any possible errors

                        if (!PetUtility.CheckPetName(PetName))
                            return;

                        if (Race.Length > 2)
                            return;

                        if (Color.Length != 6)
                            return;

                        PolarEnvironment.GetGame().GetAchievementManager().ProgressAchievement(session, "ACH_PetLover", 1);
                    }
                    catch (Exception e)
                    {
                        Logging.LogException(e.ToString());
                        return;
                    }

                    break;

                case InteractionType.FLOOR:
                case InteractionType.WALLPAPER:
                case InteractionType.LANDSCAPE:

                    Double Number = 0;

                    try
                    {
                        if (string.IsNullOrEmpty(extraData))
                            Number = 0;
                        else
                            Number = Double.Parse(extraData, PolarEnvironment.CultureInfo);
                    }
                    catch (Exception e)
                    {
                        Logging.HandleException(e, "Catalog.HandlePurchase: " + extraData);
                    }

                    extraData = Number.ToString().Replace(',', '.');
                    break; // maintain extra data // todo: validate

                case InteractionType.POSTIT:
                    extraData = "FFFF33";
                    break;

                case InteractionType.MOODLIGHT:
                    extraData = "1,1,1,#000000,255";
                    break;

                case InteractionType.TROPHY:
                    extraData = session.GetHabbo().Username + Convert.ToChar(9) + DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year + Convert.ToChar(9) + extraData;
                    break;

                case InteractionType.MANNEQUIN:
                    extraData = "m" + Convert.ToChar(5) + ".ch-210-1321.lg-285-92" + Convert.ToChar(5) + "Default Mannequin";
                    break;

                case InteractionType.BADGE_DISPLAY:
                    if (!session.GetHabbo().GetBadgeComponent().HasBadge(extraData))
                    {
                        session.SendMessage(new BroadcastMessageAlertComposer("¡Vaya !, parece que usted no es dueño de esta insignia."));
                        return;
                    }

                    extraData = extraData + Convert.ToChar(9) + session.GetHabbo().Username + Convert.ToChar(9) + DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year;
                    break;

                case InteractionType.BADGE:
                    {
                        if (session.GetHabbo().GetBadgeComponent().HasBadge(item.Data.ItemName))
                        {
                            session.SendMessage(new PurchaseErrorComposer(1));
                            return;
                        }
                        break;
                    }
                default:
                    extraData = "";
                    break;
            }
            #endregion

            // Deductir monedas
            //Console.WriteLine("Deductiendo monedas...");
            DeductCurrency(session, totalCredits, totalPixels, totalDiamonds);

            // Crear y entregar item
            //Console.WriteLine("Entregando item(s)...");
            DeliverItem(session, item, extraData, amount);

            // Enviar confirmación
            //Console.WriteLine("Enviando confirmación de compra...");
            session.SendMessage(new PurchaseOKComposer(item, item.Data));

            // Notificación
            SendPurchaseNotification(session, item, amount);

            // Actualizar inventario
            session.SendMessage(new FurniListUpdateComposer());

            // Registrar en log si es necesario
            //LogPurchase(session, item, amount, totalCredits, totalPixels, totalDiamonds);

            //Console.WriteLine($"=== Compra completada exitosamente ===\n");
        }

        /*private void LogPurchase(GameClient session, CatalogItem item, int amount, int credits, int pixels, int diamonds)
        {
            try
            {
                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("INSERT INTO `catalog_purchases_log` (user_id, item_id, offer_id, item_name, amount, cost_credits, cost_pixels, cost_diamonds, timestamp) VALUES (@uid, @iid, @oid, @name, @amount, @credits, @pixels, @diamonds, UNIX_TIMESTAMP())");
                    dbClient.AddParameter("uid", session.GetHabbo().Id);
                    dbClient.AddParameter("iid", item.Id);
                    dbClient.AddParameter("oid", item.OfferId);
                    dbClient.AddParameter("name", item.Name);
                    dbClient.AddParameter("amount", amount);
                    dbClient.AddParameter("credits", credits);
                    dbClient.AddParameter("pixels", pixels);
                    dbClient.AddParameter("diamonds", diamonds);
                    dbClient.RunQuery();
                }
                Console.WriteLine($"Compra registrada en log: {item.Name} x{amount}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al registrar compra en log: {ex.Message}");
            }
        }*/

        private CatalogItem GetCatalogItem(CatalogPage page, int identifier)
        {
            // Primero buscar por OfferId en ItemOffers
            if (page.ItemOffers.TryGetValue(identifier, out CatalogItem item))
            {
                //Console.WriteLine($"Item encontrado por OfferId: {identifier}");
                return item;
            }

            // Luego buscar por ItemId en Items
            if (page.Items.TryGetValue(identifier, out item))
            {
                //Console.WriteLine($"Item encontrado por ItemId: {identifier}");
                return item;
            }

            //Console.WriteLine($"Item NO encontrado: {identifier}");
            return null;
        }

       
        private bool ProcessLimitedItem(CatalogItem item, GameClient session)
        {
            if (item.LimitedEditionStack <= item.LimitedEditionSells)
            {
                //Console.WriteLine($"Item limitado agotado: Stack={item.LimitedEditionStack}, Sells={item.LimitedEditionSells}");
                session.SendNotification("¡Este artículo se ha agotado!");
                session.SendMessage(new CatalogUpdatedComposer());
                session.SendMessage(new PurchaseOKComposer());
                return false;
            }

            item.LimitedEditionSells++;
            //Console.WriteLine($"Actualizando item limitado: Nuevo sells={item.LimitedEditionSells}");

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                    dbClient.SetQuery($"UPDATE `catalog_items` SET `limited_sells` = @sells WHERE `{Polar.Core.DatabaseCompatibility.CatalogItemIdColumn}` = @id");
                dbClient.AddParameter("sells", item.LimitedEditionSells);
                dbClient.AddParameter("id", item.Id);
                dbClient.RunQuery();
            }

            return true;
        }

        private void DeductCurrency(GameClient session, int credits, int pixels, int diamonds)
        {
            if (credits > 0)
            {
                session.GetHabbo().Credits -= credits;
                session.SendMessage(new CreditBalanceComposer(session.GetHabbo().Credits));
                //Console.WriteLine($"Créditos deducidos: {credits}, Nuevo saldo: {session.GetHabbo().Credits}");
            }

            if (pixels > 0)
            {
                session.GetHabbo().Duckets -= pixels;
                session.SendMessage(new HabboActivityPointNotificationComposer(
                    session.GetHabbo().Duckets, session.GetHabbo().Duckets));
                //Console.WriteLine($"Duckets deducidos: {pixels}, Nuevo saldo: {session.GetHabbo().Duckets}");
            }

            if (diamonds > 0)
            {
                session.GetHabbo().Diamonds -= diamonds;
                session.SendMessage(new HabboActivityPointNotificationComposer(
                    session.GetHabbo().Diamonds, 0, 5));
                //Console.WriteLine($"Diamantes deducidos: {diamonds}, Nuevo saldo: {session.GetHabbo().Diamonds}");
            }
        }

        private void DeliverItem(GameClient session, CatalogItem catalogItem, string extraData, int amount)
        {
            //Console.WriteLine($"Entregando item tipo: {catalogItem.Data.Type}, Interaction: {catalogItem.Data.InteractionType}");

            switch (catalogItem.Data.Type.ToString().ToLower())
            {
                case "s": // Furniture
                    DeliverFurniture(session, catalogItem, extraData, amount);
                    break;

                case "e": // Effect
                    DeliverEffect(session, catalogItem);
                    break;

                case "r": // Bot
                    DeliverBot(session, catalogItem);
                    break;

                case "b": // Badge
                    DeliverBadge(session, catalogItem);
                    break;

                case "p": // Pet
                    {
                        string[] petData = extraData.Split('\n');
                        if (petData.Length >= 3)
                        {
                            int.TryParse(catalogItem.Data.ItemName.Replace("a0 pet", ""), out int pType);
                            Pet GeneratedPet = PetUtility.CreatePet(session.GetHabbo().Id, petData[0], pType, petData[1], petData[2]);
                            session.GetHabbo().GetInventoryComponent().TryAddPet(GeneratedPet);
                        }
                    }

                    session.SendMessage(new FurniListNotificationComposer(0, 3));
                    session.SendMessage(new PetInventoryComposer(session.GetHabbo().GetInventoryComponent().GetPets()));

                    ItemData PetFood = null;
                    if (PolarEnvironment.GetGame().GetItemManager().GetItem(320, out PetFood))
                    {
                        Item Food = ItemFactory.CreateSingleItemNullable(PetFood, session.GetHabbo(), "", "");
                        if (Food != null)
                        {
                            session.GetHabbo().GetInventoryComponent().TryAddItem(Food);
                            session.SendMessage(new FurniListNotificationComposer(Food.Id, 1));
                        }
                    }
                    break;

                default:
                    //Console.WriteLine($"Tipo desconocido: {catalogItem.Data.Type}, tratando como furniture");
                    if (catalogItem.Data.InteractionType == InteractionType.NAME_COLOR)
                    {
                        session.GetHabbo().NameColor = catalogItem.Name;
                        session.GetHabbo().SaveKey("name_color", catalogItem.Name);
                        session.SendWhisper($"Has cambiado tu color de nombre a: {catalogItem.Name}", 1);

                        // Update in room
                        if (session.GetHabbo().InRoom)
                        {
                            var user = session.GetHabbo().CurrentRoom.GetRoomUserManager().GetRoomUserByHabbo(session.GetHabbo().Id);
                            if (user != null)
                                session.GetHabbo().CurrentRoom.SendMessage(new UsersComposer(user));
                        }
                    }
                    else if (catalogItem.Data.InteractionType == InteractionType.NAME_PREFIX)
                    {
                        session.GetHabbo().NamePrefix = catalogItem.Name;
                        session.GetHabbo().SaveKey("prefix", catalogItem.Name);
                        session.SendWhisper($"Has cambiado tu prefijo de nombre a: {catalogItem.Name}", 1);

                        // Update in room
                        if (session.GetHabbo().InRoom)
                        {
                            var user = session.GetHabbo().CurrentRoom.GetRoomUserManager().GetRoomUserByHabbo(session.GetHabbo().Id);
                            if (user != null)
                                session.GetHabbo().CurrentRoom.SendMessage(new UsersComposer(user));
                        }
                    }
                    else
                    {
                        DeliverFurniture(session, catalogItem, extraData, amount);
                    }
                    break;
            }

            // Badge adicional si existe
            if (!string.IsNullOrEmpty(catalogItem.Badge))
            {
                //Console.WriteLine($"Entregando badge adicional: {catalogItem.Badge}");
                session.GetHabbo().GetBadgeComponent().GiveBadge(catalogItem.Badge, true, session);
            }
        }

        private void DeliverFurniture(GameClient session, CatalogItem item, string extraData, int amount)
        {
            //Console.WriteLine($"Creando furniture: Amount={amount}");

            List<Item> items = new List<Item>();
            Item NewItem = null;
            switch (item.Data.InteractionType)
            {
                default:
                    if (amount > 1)
                    {
                        items = ItemFactory.CreateMultipleItems(item.Data, session.GetHabbo(), extraData, amount);
                    }
                    else
                    {
                        NewItem = ItemFactory.CreateSingleItemNullable(item.Data, session.GetHabbo(), extraData, extraData, 0, item.LimitedEditionSells, item.LimitedEditionStack);

                        if (NewItem != null)
                        {
                            items.Add(NewItem);
                        }
                    }
                    break;

                case InteractionType.GUILD_GATE:
                case InteractionType.GUILD_ITEM:
                case InteractionType.GUILD_FORUM:
                    if (amount > 1)
                    {
                        items = ItemFactory.CreateMultipleItems(item.Data, session.GetHabbo(), extraData, amount);
                    }
                    else
                    {
                        // FIX: extraData puede llegar vacío, usar TryParse para evitar FormatException
                        int guildId = 0;
                        if (!string.IsNullOrEmpty(extraData))
                            int.TryParse(extraData, out guildId);

                        NewItem = ItemFactory.CreateSingleItemNullable(item.Data, session.GetHabbo(), extraData, extraData, guildId);

                        if (NewItem != null)
                        {
                            items.Add(NewItem);
                        }
                    }
                    break;

                case InteractionType.MUSIC_DISC:
                    string flags = Convert.ToString(item.ExtradataInt);
                    if (amount > 1)
                    {
                        items = ItemFactory.CreateMultipleItems(item.Data, session.GetHabbo(), extraData, amount);
                    }
                    else
                    {
                        NewItem = ItemFactory.CreateSingleItemNullable(item.Data, session.GetHabbo(), flags, flags);

                        if (NewItem != null)
                        {
                            items.Add(NewItem);
                        }
                    }
                    break;
               
                case InteractionType.ARROW:
                case InteractionType.ARROW2:
                case InteractionType.TELEPORT:
                    for (int i = 0; i < amount; i++)
                    {
                        List<Item> TeleItems = ItemFactory.CreateTeleporterItems(item.Data, session.GetHabbo());

                        if (TeleItems != null)
                        {
                            items.AddRange(TeleItems);
                        }
                    }
                    break;

                case InteractionType.MOODLIGHT:
                    {
                        if (amount > 1)
                        {
                            items = ItemFactory.CreateMultipleItems(item.Data, session.GetHabbo(), extraData, amount);
                        }
                        else
                        {
                            NewItem = ItemFactory.CreateSingleItemNullable(item.Data, session.GetHabbo(), extraData, extraData);

                            if (NewItem != null)
                            {
                                items.Add(NewItem);
                                ItemFactory.CreateMoodlightData(NewItem);
                            }
                        }
                    }
                    break;

                case InteractionType.TONER:
                    {
                        if (amount > 1)
                        {
                            items = ItemFactory.CreateMultipleItems(item.Data, session.GetHabbo(), extraData, amount);
                        }
                        else
                        {
                            NewItem = ItemFactory.CreateSingleItemNullable(item.Data, session.GetHabbo(), extraData, extraData);

                            if (NewItem != null)
                            {
                                items.Add(NewItem);
                                ItemFactory.CreateTonerData(NewItem);
                            }
                        }
                    }
                    break;

            }
           /* if (amount > 1)
            {
                items = ItemFactory.CreateMultipleItems(item.Data, session.GetHabbo(), extraData, amount);
                //Console.WriteLine($"Creados {items?.Count ?? 0} items múltiples");
            }
            else
            {
                var newItem = ItemFactory.CreateSingleItemNullable(item.Data, session.GetHabbo(), extraData, extraData);
                if (newItem != null)
                {
                    items.Add(newItem);
                    //Console.WriteLine($"Creado 1 item: {newItem.Id}");
                }
            }*/

            if (items != null)
            {
                foreach (var furniItem in items)
                {
                    if (session.GetHabbo().GetInventoryComponent().TryAddItem(furniItem))
                    {
                        session.SendMessage(new FurniListNotificationComposer(furniItem.Id, 1));
                        //Console.WriteLine($"Item agregado al inventario: {furniItem.Id}");
                    }
                    else
                    {
                        //Console.WriteLine($"No se pudo agregar item al inventario: {furniItem.Id}");
                    }
                }
            }
        }

        private void DeliverEffect(GameClient session, CatalogItem item)
        {
            //Console.WriteLine($"Entregando effect: {item.Data.SpriteId}");

            var effect = session.GetHabbo().Effects().GetEffectNullable(item.Data.SpriteId);

            if (effect != null)
            {
                effect.AddToQuantity();
                //Console.WriteLine($"Effect existente incrementado");
            }
            else
            {
                var newEffect = AvatarEffectFactory.CreateNullable(session.GetHabbo(), item.Data.SpriteId, 3600);
                if (newEffect != null)
                {
                    session.SendMessage(new AvatarEffectAddedComposer(item.Data.SpriteId, 3600));
                    //Console.WriteLine($"Nuevo effect creado");
                }
            }
        }

        private void DeliverBot(GameClient session, CatalogItem item)
        {
            if (item?.Data == null) return;

            try
            {
                // item.Id es el id de catalog_items, que coincide con catalog_bots.item_id
                var bot = BotUtility.CreateBot(item.Data, session.GetHabbo().Id, item.Id);

                if (bot == null)
                {
                    Console.WriteLine($"[DeliverBot] CreateBot devolvió null para catalog item id={item.Id}");
                    session.SendNotification("No se pudo crear el bot.");
                    return;
                }

                session.GetHabbo().GetInventoryComponent().TryAddBot(bot);
                session.SendMessage(new BotInventoryComposer(session.GetHabbo().GetInventoryComponent().GetBots()));
                session.SendMessage(new FurniListNotificationComposer(bot.Id, 5));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DeliverBot] Excepción: {ex.Message}");
            }
        }

        private void DeliverBadge(GameClient session, CatalogItem item)
        {
            //Console.WriteLine($"Entregando badge: {item.Data.ItemName}");

            session.GetHabbo().GetBadgeComponent().GiveBadge(item.Data.ItemName, true, session);
            session.SendMessage(new FurniListNotificationComposer(0, 4));
        }

        private void DeliverPet(GameClient session, CatalogItem item, string extraData)
        {
            //Console.WriteLine($"Entregando pet, extraData: {extraData}");

            try
            {
                string[] petData = extraData.Split('\n');
                if (petData.Length >= 3)
                {
                    string petName = petData[0];
                    string race = petData[1];
                    string color = petData[2];

                    Console.WriteLine($"Pet data: Name='{petName}', Race='{race}', Color='{color}'");


                        var pet = PetUtility.CreatePet(session.GetHabbo().Id, petName, Convert.ToInt32(race), race, color);
                        if (pet != null)
                        {
                            session.GetHabbo().GetInventoryComponent().TryAddPet(pet);
                            session.SendMessage(new FurniListNotificationComposer(0, 3));
                            session.SendMessage(new PetInventoryComposer(session.GetHabbo().GetInventoryComponent().GetPets()));
                            //Console.WriteLine($"Pet creado: {pet.PetId}, Type: {pet.Type}");
                        }
                    
                }
                else
                {
                    //Console.WriteLine($"Pet data inválida, longitud: {petData.Length}");
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error al crear mascota: {ex.Message}");
            }
        }

        private void SendPurchaseNotification(GameClient session, CatalogItem item, int amount)
        {
            string itemName = item.Data.ItemName.Replace("*", "_");
            string displayName = string.IsNullOrEmpty(item.Data.PublicName)
                ? item.Data.ItemName
                : item.Data.PublicName;

            string message = amount > 1
                ? $"Acabas de comprar unos/unas {displayName}"
                : $"Acabas de comprar un/una {displayName}";

            //Console.WriteLine($"Enviando notificación: {message}");

            session.SendMessage(new RoomBubbleNotificationComposer($"icon/{itemName}_icon", message));
        }
    }
}