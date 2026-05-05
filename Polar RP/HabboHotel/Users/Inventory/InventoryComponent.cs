using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Polar.HabboHotel.Rooms.AI;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Items.Crafting;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Users.Inventory.Pets;
using Polar.HabboHotel.Users.Inventory.Bots;
using Polar.Communication.Packets.Outgoing.Inventory.Furni;
using Polar.Database.Interfaces;

namespace Polar.HabboHotel.Users.Inventory
{
    public class InventoryComponent
    {
        private readonly int _userId;
        private GameClient _client;

        public int InventaryUserId { get; set; }

        public ConcurrentDictionary<int, Bot>  _botItems;
        public ConcurrentDictionary<int, Pet>  _petsItems;
        public ConcurrentDictionary<int, Item> _floorItems;
        public ConcurrentDictionary<int, Item> _wallItems;
        public Dictionary<int, Item>           songDisks;
        public PrefixesComponent _prefixesComponent;

        public InventoryComponent(int userId, GameClient client)
        {
            _client        = client;
            _userId        = userId;
            InventaryUserId = 0;

            _floorItems = new ConcurrentDictionary<int, Item>();
            _wallItems  = new ConcurrentDictionary<int, Item>();
            _petsItems  = new ConcurrentDictionary<int, Pet>();
            _botItems   = new ConcurrentDictionary<int, Bot>();
            songDisks   = new Dictionary<int, Item>();
            _prefixesComponent = new(client.GetHabbo());
            Init();
        }

        // ─── Loading ────────────────────────────────────────────────────────────

        public void Init()
        {
            _floorItems.Clear();
            _wallItems.Clear();
            _petsItems.Clear();
            _botItems.Clear();
            songDisks.Clear();

            foreach (Item item in ItemLoader.GetItemsForUser(_userId))
            {
                if (item.GetBaseItem().InteractionType == InteractionType.MUSIC_DISC)
                    songDisks[item.Id] = item;

                if (item.IsFloorItem)
                    _floorItems.TryAdd(item.Id, item);
                else if (item.IsWallItem)
                    _wallItems.TryAdd(item.Id, item);
            }

            foreach (Pet pet in PetLoader.GetPetsForUser(_userId))
            {
                if (!_petsItems.TryAdd(pet.PetId, pet))
                    Console.WriteLine("Error loading pet: " + pet.PetId);
            }

            foreach (Bot bot in BotLoader.GetBotsForUser(_userId))
            {
                if (!_botItems.TryAdd(bot.Id, bot))
                    Console.WriteLine("Error loading bot: " + bot.Id);
            }
        }

        // ─── Items ───────────────────────────────────────────────────────────────

        public Item GetItem(int id)
        {
            if (_floorItems.TryGetValue(id, out Item floor)) return floor;
            if (_wallItems.TryGetValue(id, out Item wall))   return wall;
            return null;
        }

        public IEnumerable<Item> GetItems           => _floorItems.Values.Concat(_wallItems.Values);
        public IEnumerable<Item> GetWallAndFloor    => _floorItems.Values.Concat(_wallItems.Values);
        public IEnumerable<Item> AllItems           => _floorItems.Values.Concat(_wallItems.Values);
        public ICollection<Item> GetFloorItems()    => _floorItems.Values;
        public ICollection<Item> GetWallItems()     => _wallItems.Values;
        public ICollection<Item> GetSongDisks()     => songDisks.Values;

        public void AddItem(Item item)
        {
            AddNewItem(item.Id, item.BaseItem, item.ExtraData, item.GroupId, true, true, item.LimitedNo, item.LimitedTot);
        }

        public Item AddNewItem(int id, int baseItem, string extraData, int group, bool toInsert, bool fromRoom, int limitedNumber, int limitedStack)
        {
            if (toInsert)
            {
                using IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor();

                if (fromRoom)
                {
                    dbClient.RunQuery($"UPDATE `{Polar.Core.DatabaseCompatibility.ItemsTable}` SET `room_id` = '0', `user_id` = '{_userId}' WHERE `id` = '{id}' LIMIT 1");
                }
                else
                {
                    if (id > 0)
                    {
                        dbClient.RunQuery($"INSERT INTO `{Polar.Core.DatabaseCompatibility.ItemsTable}` (`id`,`{Polar.Core.DatabaseCompatibility.ItemsBaseItemColumn}`,`user_id`,`limited_number`,`limited_stack`) VALUES ('{id}','{baseItem}','{_userId}','{limitedNumber}','{limitedStack}')");
                    }
                    else
                    {
                        dbClient.SetQuery($"INSERT INTO `{Polar.Core.DatabaseCompatibility.ItemsTable}` (`{Polar.Core.DatabaseCompatibility.ItemsBaseItemColumn}`,`user_id`,`limited_number`,`limited_stack`) VALUES ('{baseItem}','{_userId}','{limitedNumber}','{limitedStack}')");
                        id = Convert.ToInt32(dbClient.InsertQuery());
                    }

                    SendNewItems(id);

                    if (group > 0)
                        dbClient.RunQuery($"INSERT INTO `items_groups` VALUES ({id}, {group})");

                    if (!string.IsNullOrEmpty(extraData))
                    {
                        dbClient.SetQuery($"UPDATE `{Polar.Core.DatabaseCompatibility.ItemsTable}` SET `extra_data` = @extradata WHERE `id` = '{id}' LIMIT 1");
                        dbClient.AddParameter("extradata", extraData);
                        dbClient.RunQuery();
                    }
                }
            }

            var newItem = new Item(id, 0, baseItem, extraData, 0, 0, 0, 0, _userId, group, limitedNumber, limitedStack, string.Empty);

            RemoveItem(id); // remove duplicate if exists

            if (newItem.GetBaseItem().InteractionType == InteractionType.MUSIC_DISC)
            {
                songDisks[newItem.Id] = newItem;
                UpdateJuke();
            }

            UpdateItems(true);

            if (newItem.IsWallItem)
                _wallItems.TryAdd(newItem.Id, newItem);
            else
                _floorItems.TryAdd(newItem.Id, newItem);

            return newItem;
        }

        public void RemoveItem(int id)
        {
            var client = GetClient();
            if (client?.GetHabbo()?.GetInventoryComponent() == null)
            {
                client?.Disconnect(true);
                return;
            }

            _floorItems.TryRemove(id, out _);
            _wallItems.TryRemove(id, out _);
            songDisks.Remove(id);

            client.SendMessage(new FurniListRemoveComposer(id));
        }

        private bool UserHoldsItem(int itemId)
            => _floorItems.ContainsKey(itemId) || _wallItems.ContainsKey(itemId) || songDisks.ContainsKey(itemId);

        public bool TryAddItem(Item item)
        {
            string type = item.Data.Type.ToString().ToLower();

            if (type == "s") return _floorItems.TryAdd(item.Id, item);
            if (type == "i") return _wallItems.TryAdd(item.Id, item);

            throw new InvalidOperationException("Item did not match neither floor or wall item");
        }

        // ─── Sync / Update ───────────────────────────────────────────────────────

        public void UpdateItems(bool fromDatabase)
        {
            if (fromDatabase) Init();
            _client?.SendMessage(new FurniListUpdateComposer());
        }

        public void UpdateJuke() { /* stub */ }

        public void SendNewItems(int id)
        {
            _client?.SendMessage(new FurniListNotificationComposer(id, 1));
        }
        public PrefixesComponent GetPrefixesComponent()
        {
            return _prefixesComponent;
        }
        public void ClearItems()
        {
            // NO llamar UpdateItems(true)/Init() aquí — es innecesario recargar
            // desde DB items que vamos a borrar inmediatamente después

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                string table = Polar.Core.DatabaseCompatibility.ItemsTable;
                dbClient.runFastQuery(
                    $"DELETE i, user_presents, room_items_moodlight, room_items_tele_links, room_items_toner, items_groups FROM `{table}` i " +
                    "LEFT JOIN user_presents            ON (user_presents.item_id = i.id) " +
                    "LEFT JOIN room_items_moodlight     ON (room_items_moodlight.item_id = i.id) " +
                    "LEFT JOIN room_items_tele_links    ON (room_items_tele_links.tele_one_id = i.id OR room_items_tele_links.tele_two_id = i.id) " +
                    "LEFT JOIN room_items_toner         ON (room_items_toner.id = i.id) " +
                    "LEFT JOIN items_groups             ON (items_groups.id = i.id) " +
                    $"WHERE i.room_id = '0' AND i.user_id = '{_userId}'");
            }

            _floorItems?.Clear();
            _wallItems?.Clear();
            songDisks?.Clear();

            // Enviar update solo si el cliente sigue conectado
            _client?.SendMessage(new FurniListUpdateComposer());
        }

        public void SetIdleState()
        {
            _botItems?.Clear();
            _petsItems?.Clear();
            _floorItems?.Clear();
            _wallItems?.Clear();
            songDisks?.Clear();

            _botItems   = null;
            _petsItems  = null;
            _floorItems = null;
            _wallItems  = null;
            songDisks   = null;
            _client     = null;
        }

        public void LoadUserInventory(int inventoryId)
        {
            InventaryUserId = inventoryId;
            UpdateItems(true);
        }

        // ─── Pets ────────────────────────────────────────────────────────────────

        public ICollection<Pet> GetPets() => _petsItems.Values;

        public bool TryAddPet(Pet pet)
        {
            pet.RoomId      = 0;
            pet.PlacedInRoom = false;
            return _petsItems.TryAdd(pet.PetId, pet);
        }

        public bool TryRemovePet(int petId, out Pet petItem)
        {
            if (_petsItems.ContainsKey(petId)) return _petsItems.TryRemove(petId, out petItem);
            petItem = null;
            return false;
        }

        public bool TryGetPet(int petId, out Pet pet)
        {
            if (_petsItems.ContainsKey(petId)) return _petsItems.TryGetValue(petId, out pet);
            pet = null;
            return false;
        }

        // ─── Bots ────────────────────────────────────────────────────────────────

        public ICollection<Bot> GetBots() => _botItems.Values;

        public bool TryAddBot(Bot bot) => _botItems.TryAdd(bot.Id, bot);

        public bool TryRemoveBot(int botId, out Bot bot)
        {
            if (_botItems.ContainsKey(botId)) return _botItems.TryRemove(botId, out bot);
            bot = null;
            return false;
        }

        public bool TryGetBot(int botId, out Bot bot)
        {
            if (_botItems.ContainsKey(botId)) return _botItems.TryGetValue(botId, out bot);
            bot = null;
            return false;
        }

        // ─── Helpers ─────────────────────────────────────────────────────────────

        private GameClient GetClient()
            => PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(_userId);

        internal Item GetFirstItemByBaseId(int baseId)
            => _floorItems.Values.FirstOrDefault(i => i?.GetBaseItem()?.Id == baseId);

        private int GetUserInventaryId()
            => InventaryUserId > 0 ? InventaryUserId : _userId;
    }
}
