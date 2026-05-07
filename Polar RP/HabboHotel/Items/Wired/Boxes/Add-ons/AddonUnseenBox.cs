using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;

namespace Polar.HabboHotel.Items.Wired.Boxes.Add_ons
{
    class AddonUnseenBox : IWiredItem
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.AddonUnseen;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        public const int CODE = 62;
        private const int MAX_SEEN_LIST_SIZE = 1000;

        private readonly object _lock = new object();

        /// <summary>
        /// LinkedList usado como LinkedHashSet: mantiene orden de inserción
        /// y permite evicción LRU del elemento más antiguo.
        /// </summary>
        private readonly LinkedList<int> _seenList = new LinkedList<int>();
        private readonly HashSet<int> _seenSet = new HashSet<int>();

        public AddonUnseenBox(Room instance, Item item)
        {
            this.Instance = instance;
            this.Item = item;
            this.SetItems = new ConcurrentDictionary<int, Item>();
            this.StringData = "";
        }

        public void HandleSave(ClientPacket packet)
        {
            packet.PopInt();    // intCount = 0
            packet.PopString(); // string vacío
        }

        public void Serialize(ServerPacket packet)
        {
            packet.WriteBoolean(false);
            packet.WriteInteger(0); // sin furnis
            packet.WriteInteger(0);
            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            packet.WriteString("");
            packet.WriteInteger(0); // sin intParams
            packet.WriteInteger(0);
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type)); // 62
            packet.WriteInteger(0);
            packet.WriteInteger(0);
        }

        public bool Execute(params object[] @params)
        {
            return false;
        }

        /// <summary>
        /// Devuelve el primer efecto no visto. Si todos fueron vistos, reinicia la lista
        /// y devuelve el primero de la lista completa. Equivale a getUnseenEffect() de Java.
        /// </summary>
        public T GetUnseenEffect<T>(List<T> effects, Func<T, int> idResolver) where T : class
        {
            lock (_lock)
            {
                if (effects == null || effects.Count == 0)
                    return null;

                // Primer pase: buscar un efecto no visto
                T selected = null;
                foreach (var effect in effects)
                {
                    if (!_seenSet.Contains(idResolver(effect)))
                    {
                        selected = effect;
                        break;
                    }
                }

                // Si todos fueron vistos, reiniciar y tomar el primero
                if (selected == null)
                {
                    ClearSeenListInternal();
                    selected = effects[0];
                }

                // Registrar como visto con evicción LRU
                AddToSeenList(idResolver(selected));
                return selected;
            }
        }

        /// <summary>
        /// Versión de lista: devuelve lista de un solo elemento no visto.
        /// Equivale a selectWiredEffects() de Java.
        /// </summary>
        public List<T> SelectUnseenEffects<T>(List<T> effects, Func<T, int> idResolver) where T : class
        {
            var result = GetUnseenEffect(effects, idResolver);
            return result != null ? new List<T> { result } : new List<T>();
        }

        public int GetSeenListSize()
        {
            lock (_lock)
            {
                return _seenSet.Count;
            }
        }

        public void ClearSeenList()
        {
            lock (_lock)
            {
                ClearSeenListInternal();
            }
        }

        // Llamar solo dentro de lock
        private void ClearSeenListInternal()
        {
            _seenList.Clear();
            _seenSet.Clear();
        }

        // Llamar solo dentro de lock
        private void AddToSeenList(int id)
        {
            if (_seenSet.Contains(id))
                return;

            // Evicción LRU si se alcanza el límite
            if (_seenSet.Count >= MAX_SEEN_LIST_SIZE)
            {
                int oldest = _seenList.First.Value;
                _seenList.RemoveFirst();
                _seenSet.Remove(oldest);
            }

            _seenList.AddLast(id);
            _seenSet.Add(id);
        }
    }
}