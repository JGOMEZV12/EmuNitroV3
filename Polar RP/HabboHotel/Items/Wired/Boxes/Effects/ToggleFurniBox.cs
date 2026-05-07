using Polar.Communication.Packets.Outgoing;
using System;
using System.Linq;
using System.Collections.Concurrent;
using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;

namespace Polar.HabboHotel.Items.Wired.Boxes.Effects
{
    class ToggleFurniBox : IWiredItem, IWiredCycle
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type { get { return WiredBoxType.EffectToggleFurniState; } }
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public int TickCount { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        public int Delay
        {
            get { return _delay; }
            set { _delay = value; this.TickCount = value + 1; }
        }

        private int _delay = 0;
        private long _next = 0;
        private bool _requested = false;

        // ✅ Campos nuevos igual que Java
        private int _toggleType = 0; // TOGGLE_TYPE_NEXT = 0
        private int _furniSource = 0; // SOURCE_TRIGGER   = 0

        private const int TOGGLE_TYPE_NEXT = 0;
        private const int TOGGLE_TYPE_PREVIOUS = 1;

        public ToggleFurniBox(Room instance, Item item)
        {
            this.Instance = instance;
            this.Item = item;
            this.SetItems = new ConcurrentDictionary<int, Item>();
        }

        public void HandleSave(ClientPacket Packet)
        {
            this.SetItems.Clear();

            // ── Mismo patrón que CollisionCaseBox ─────────────────────────────
            // 1. IntCount → consumir ints (aquí vienen toggleType y furniSource)
            int IntCount = Packet.PopInt();
            if (IntCount > 0) _toggleType = Packet.PopInt(); // intParams[0]
            if (IntCount > 1) _furniSource = Packet.PopInt(); // intParams[1]
            // consumir el resto si hubiera más
            for (int i = 2; i < IntCount; i++) Packet.PopInt();

            // 2. StringParam (vacío en este wired)
            string Unknown2 = Packet.PopString();

            // 3. FurniCount → items seleccionados
            int FurniCount = Packet.PopInt();
            for (int i = 0; i < FurniCount; i++)
            {
                Item selected = Instance.GetRoomItemHandler().GetItem(Packet.PopInt());
                if (selected != null && !Instance.GetWired().OtherBoxHasItem(this, selected.Id))
                    SetItems.TryAdd(selected.Id, selected);
            }

            // 4. Delay
            this.Delay = Packet.PopInt();

            // ✅ Si hay furnis seleccionados y furniSource = SOURCE_TRIGGER, cambiar a SOURCE_SELECTED
            if (SetItems.Count > 0 && _furniSource == 0)
                _furniSource = 1; // SOURCE_SELECTED = 1

            // Persistencia
            this.StringData = _toggleType + ";" + _furniSource;
        }

        public void Serialize(ServerPacket Packet)
        {
            Packet.WriteBoolean(false);
            Packet.WriteInteger(100);
            Packet.WriteInteger(SetItems.Count);
            foreach (Item item in SetItems.Values.ToList())
                Packet.WriteInteger(item.Id);

            Packet.WriteInteger(Item.GetBaseItem().SpriteId);
            Packet.WriteInteger(Item.Id);

            // ✅ String vacío igual que Java
            Packet.WriteString("");

            // ✅ 2 intParams: toggleType + furniSource (igual que Java appendInt(2))
            Packet.WriteInteger(2);
            Packet.WriteInteger(_toggleType);
            Packet.WriteInteger(_furniSource);

            // ✅ Orden correcto con delay
            Packet.WriteInteger(0);
            Packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            Packet.WriteInteger(this.Delay);
            Packet.WriteInteger(0);
        }

        private List<Item> _contextItems = new List<Item>();

        public bool Execute(params object[] Params)
        {
            if (this._next == 0 || this._next < PolarEnvironment.Now())
                this._next = PolarEnvironment.Now() + this.Delay;

            _contextItems.Clear();
            WiredContext context = Params.OfType<WiredContext>().FirstOrDefault();
            if (context != null && context.SelectedItems.Any())
            {
                _contextItems.AddRange(context.SelectedItems);
            }

            this._requested = true;
            this.TickCount = Delay;
            return true;
        }

        public bool OnCycle()
        {
            if (!_requested)
                return false;

            long now = PolarEnvironment.Now();
            if (_next < now)
            {
                List<Item> targetItems = new List<Item>();
                if (_contextItems.Any())
                {
                    targetItems.AddRange(_contextItems);
                }
                else
                {
                    targetItems.AddRange(SetItems.Values);
                }

                if (targetItems.Count == 0)
                {
                    _requested = false;
                    return false;
                }

                foreach (Item item in targetItems)
                {
                    if (item == null) continue;

                    if (!Instance.GetRoomItemHandler().GetFloor.Contains(item))
                    {
                        SetItems.TryRemove(item.Id, out _);
                        continue;
                    }

                    // ✅ Toggle correcto igual que Java: calcular nextState según toggleType
                    ToggleItemState(item);
                }

                _requested = false;
                this._next = 0;
                this.TickCount = Delay;
            }

            return true;
        }

        private void ToggleItemState(Item item)
        {
            try
            {
                int stateCount = item.GetBaseItem().Modes;
                if (stateCount <= 1) return;

                int currentState = 0;
                if (!string.IsNullOrEmpty(item.ExtraData))
                {
                    if (!int.TryParse(item.ExtraData, out currentState))
                    {
                        // ExtraData no es numérico — usar interactor directamente
                        item.Interactor.OnWiredTrigger(item);
                        return;
                    }
                }

                int nextState = (_toggleType == TOGGLE_TYPE_PREVIOUS)
                    ? ((currentState - 1 + stateCount) % stateCount)
                    : ((currentState + 1) % stateCount);

                if (currentState == nextState) return;

                item.ExtraData = nextState.ToString();
                item.UpdateNeeded = true;
                Instance.GetRoomItemHandler().UpdateItem(item);
            }
            catch (Exception ex)
            {
                Polar.Core.Logging.LogException("[ToggleFurniBox] " + ex);
            }
        }
    }
}