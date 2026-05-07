using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;
using Polar.HabboHotel.Users.Effects;

namespace Polar.HabboHotel.Items.Wired.Boxes.Effects
{
    class TeleportUserBox : IWiredItem, IWiredCycle
    {
        // ── Constantes (equivalentes a WiredManager en Java) ─────────────────────
        private const int MAXIMUM_FURNI_SELECTION = 5;
        private const int TELEPORT_DELAY = 500;  // ms — ajustar según config
        private const int SOURCE_TRIGGER = 0;
        private const int SOURCE_SELECTED = 1;

        // ── IWiredItem ───────────────────────────────────────────────────────────
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.EffectTeleportToFurni;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        // ── IWiredCycle ──────────────────────────────────────────────────────────
        public int Delay
        {
            get => _delay;
            set { _delay = value; TickCount = value + 1; }
        }
        public int TickCount { get; set; }

        // ── Campos privados ──────────────────────────────────────────────────────
        private readonly Queue<RoomUser> _queue;
        private int _delay;
        private bool _fastTeleport = false;
        private int _furniSource = SOURCE_TRIGGER;
        private int _userSource = SOURCE_TRIGGER;

        public TeleportUserBox(Room instance, Item item)
        {
            Instance = instance;
            Item = item;
            SetItems = new ConcurrentDictionary<int, Item>();
            _queue = new Queue<RoomUser>();
            TickCount = Delay;
        }

        // ── HandleSave ───────────────────────────────────────────────────────────
        // Java: lee fastTeleport, furniSource, userSource desde intParams
        public void HandleSave(ClientPacket packet)
        {
            int paramsCount = packet.PopInt();   // cantidad de int params
            string strParam = packet.PopString(); // string param (vacío)

            // Leer parámetros int (Java: params[0]=fastTeleport, [1]=furniSource, [2]=userSource)
            bool fastTeleport = false;
            int furniSource = SOURCE_TRIGGER;
            int userSource = SOURCE_TRIGGER;

            if (paramsCount >= 1) fastTeleport = packet.PopInt() == 1;
            if (paramsCount >= 2) furniSource = packet.PopInt();
            if (paramsCount >= 3) userSource = packet.PopInt();

            // Leer furni seleccionados
            SetItems.Clear();
            int furniCount = packet.PopInt();
            for (int i = 0; i < furniCount; i++)
            {
                Item selectedItem = Instance.GetRoomItemHandler().GetItem(packet.PopInt());
                if (selectedItem != null)
                    SetItems.TryAdd(selectedItem.Id, selectedItem);
            }

            // Java: si hay items y furniSource es TRIGGER, cambiar a SELECTED
            if (SetItems.Count > 0 && furniSource == SOURCE_TRIGGER)
                furniSource = SOURCE_SELECTED;

            _fastTeleport = fastTeleport;
            _furniSource = furniSource;
            _userSource = userSource;
            Delay = packet.PopInt();
        }

        // ── Serialize ─────────────────────────────────────────────────────────────
        // Java: bool + maxFurni + itemCount + items[] + spriteId + id + string +
        //       int(3) + fastTeleport + furniSource + userSource + int(0) +
        //       typeCode + delay + invalidTriggers
        public void Serialize(ServerPacket packet)
        {
            var itemsList = SetItems.Values.ToList();

            packet.WriteBoolean(false);
            packet.WriteInteger(MAXIMUM_FURNI_SELECTION);
            packet.WriteInteger(itemsList.Count);
            foreach (Item item in itemsList)
                packet.WriteInteger(item.Id);

            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            packet.WriteString(StringData ?? string.Empty);

            // 3 parámetros int: fastTeleport, furniSource, userSource
            packet.WriteInteger(3);
            packet.WriteInteger(_fastTeleport ? 1 : 0);
            packet.WriteInteger(_furniSource);
            packet.WriteInteger(_userSource);

            packet.WriteInteger(0);
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(Delay);

            // invalidTriggers (si userSource == SOURCE_TRIGGER necesita usuario)
            if (_userSource == SOURCE_TRIGGER)
            {
                // TODO: filtrar triggers que no son triggered by room unit
                // Por ahora enviamos 0 triggers inválidos
                packet.WriteInteger(0);
            }
            else
            {
                packet.WriteInteger(0);
            }
        }

        // ── Execute ───────────────────────────────────────────────────────────────
        public bool Execute(params object[] Params)
        {
            if (Params == null || Params.Length == 0) return false;

            List<Habbo> usersToTeleport = new List<Habbo>();
            WiredContext context = Params.OfType<WiredContext>().FirstOrDefault();

            if (context != null && context.SelectedUsers.Any())
            {
                usersToTeleport.AddRange(context.SelectedUsers);
            }
            else
            {
                Habbo player = Params[0] as Habbo;
                if (player != null) usersToTeleport.Add(player);
            }

            if (usersToTeleport.Count == 0) return false;

            foreach (var player in usersToTeleport)
            {
                RoomUser user = Instance.GetRoomUserManager().GetRoomUserByHabbo(player.Id);
                if (user == null) continue;

                // Efecto visual de teleport (Java: RoomUserEffectComposer con effect 4)
                player.Effects()?.ApplyEffect(EffectsList.Twinkle);

                _queue.Enqueue(user);
            }

            return true;
        }

        // ── OnCycle ───────────────────────────────────────────────────────────────
        public bool OnCycle()
        {
            if (_queue.Count == 0 || SetItems.Count == 0)
            {
                _queue.Clear();
                TickCount = Delay;
                return true;
            }

            while (_queue.Count > 0)
            {
                RoomUser user = _queue.Dequeue();
                if (user == null || user.GetClient()?.GetHabbo()?.CurrentRoom != Instance)
                    continue;

                TeleportUser(user);
            }

            TickCount = Delay;
            return true;
        }

        // ── TeleportUser ──────────────────────────────────────────────────────────
        private void TeleportUser(RoomUser user)
        {
            if (user == null || Instance?.GetGameMap() == null) return;

            List<Item> targetItems = new List<Item>();
            // If we have context in the future for this step, we might need it, but for now
            // IWiredCycle.OnCycle doesn't receive the context.
            // However, we can use the selector's items if we find a way to store them or if we use the items selected in this box.

            // Limpiar items inválidos (Java: removeIf)
            var invalidIds = SetItems
                .Where(kv => !Instance.GetRoomItemHandler().GetFloor.Contains(kv.Value))
                .Select(kv => kv.Key)
                .ToList();
            foreach (int id in invalidIds)
                SetItems.TryRemove(id, out _);

            targetItems.AddRange(SetItems.Values);

            if (targetItems.Count == 0) return;

            // Java: selección aleatoria con nextInt
            var items = targetItems;
            Item target = items[PolarEnvironment.GetRandomNumber(0, items.Count - 1)];
            if (target == null) return;

            // Java: getTile + buscar tile alternativo si está bloqueado
            var tile = new System.Drawing.Point(target.GetX, target.GetY);

            Instance.GetGameMap().TeleportToItem(user, target);
            Instance.GetRoomUserManager().UpdateUserStatusses();

            // Quitar efecto tras teleport
            user.GetClient()?.GetHabbo()?.Effects()?.ApplyEffect(0);
        }
    }
}