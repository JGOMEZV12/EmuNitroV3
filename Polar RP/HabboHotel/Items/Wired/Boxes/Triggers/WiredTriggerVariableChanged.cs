using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Wired;
using Polar.HabboHotel.Users;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Polar.HabboHotel.Items.Wired.Boxes.Triggers
{
    public class WiredTriggerVariableChanged : IWiredItem, IWiredCustomData
    {
        // ── IWiredItem ────────────────────────────────────────────────────────────
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.TriggerRoomVariableChanged;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        // ── Constants ─────────────────────────────────────────────────────────────
        public const int TARGET_USER = 0;
        public const int TARGET_FURNI = 1;
        public const int TARGET_ROOM = 3;

        private const string CustomTokenPrefix = "custom:";

        // ── Wired state ───────────────────────────────────────────────────────────
        private string _variableToken = string.Empty;
        private int _variableItemId = 0;
        private int _targetType = TARGET_USER;

        private bool _createdEnabled = true;
        private bool _valueChangedEnabled = true;
        private bool _increasedEnabled = true;
        private bool _decreasedEnabled = true;
        private bool _unchangedEnabled = true;
        private bool _deletedEnabled = true;

        // ── Constructor ───────────────────────────────────────────────────────────
        public WiredTriggerVariableChanged(Room instance, Item item)
        {
            Instance = instance;
            Item = item;
            StringData = string.Empty;
            ItemsData = string.Empty;
            SetItems = new ConcurrentDictionary<int, Item>();
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  HandleSave  — void, igual que la firma de IWiredItem
        // ══════════════════════════════════════════════════════════════════════════
        public void HandleSave(ClientPacket packet)
        {
            // Layout del paquete del cliente:
            //   int  paramCount (7)
            //   int  targetType
            //   int  createdEnabled
            //   int  valueChangedEnabled
            //   int  increasedEnabled
            //   int  decreasedEnabled
            //   int  unchangedEnabled
            //   int  deletedEnabled
            //   str  variableToken

            if (packet.RemainingLength < 4) return;

            int paramCount = packet.PopInt();

            int[] p = new int[paramCount];
            for (int i = 0; i < paramCount && packet.RemainingLength >= 4; i++)
                p[i] = packet.PopInt();

            string tokenRaw = packet.RemainingLength >= 2 ? packet.PopString() : string.Empty;

            _targetType = NormalizeTargetType(p.Length > 0 ? p[0] : TARGET_USER);
            _createdEnabled = p.Length <= 1 || p[1] == 1;
            _valueChangedEnabled = p.Length <= 2 || p[2] == 1;
            _increasedEnabled = p.Length <= 3 || p[3] == 1;
            _decreasedEnabled = p.Length <= 4 || p[4] == 1;
            _unchangedEnabled = p.Length <= 5 || p[5] == 1;
            _deletedEnabled = p.Length <= 6 || p[6] == 1;

            SetVariableToken(NormalizeVariableToken(tokenRaw));
            NormalizeOptions();

            // Persistir en StringData para que WiredComponent lo guarde en BD
            StringData = GetWiredData();
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  Execute  — params object[], misma firma que RoomEnterBox
        //
        //  Llamado por WiredComponent.TriggerVariableChanged con:
        //    stuff[0] = targetType        (int)
        //    stuff[1] = definitionItemId  (int)
        //    stuff[2] = created           (bool)
        //    stuff[3] = deleted           (bool)
        //    stuff[4] = changeKind        (int — VariableChangeKind cast)
        //    stuff[5] = Habbo actor       (Habbo, puede ser null)
        // ══════════════════════════════════════════════════════════════════════════
        public bool Execute(params object[] stuff)
        {
            if (!MatchesEvent(stuff)) return false;

            Habbo player = stuff.Length > 5 ? stuff[5] as Habbo : null;

            Instance.GetWired().OnEvent(Item);

            var effects = Instance.GetWired().GetEffects(this);
            var conditions = Instance.GetWired().GetConditions(this);
            var addons = Instance.GetWired().GetTriggers(this)
                                     .Where(x => x.Type.ToString().StartsWith("Addon")).ToList();

            var limitAddon = addons.FirstOrDefault(x => x.Type == WiredBoxType.AddonExecutionLimit);
            if (limitAddon != null && !limitAddon.Execute()) return false;

            var randomAddon = addons.FirstOrDefault(x => x.Type == WiredBoxType.AddonRandom);
            if (randomAddon != null && !randomAddon.Execute()) return false;

            bool hasOrEval = addons.Any(x => x.Type == WiredBoxType.AddonOrEval);
            if (hasOrEval)
            {
                if (conditions.Count > 0 && !conditions.Any(c => c.Execute(player))) return false;
            }
            else
            {
                foreach (var condition in conditions.ToList())
                {
                    if (!condition.Execute(player)) return false;
                    Instance.GetWired().OnEvent(condition.Item);
                }
            }

            bool hasExecuteInOrder = addons.Any(x => x.Type == WiredBoxType.AddonExecuteInOrder);
            bool hasRandomEffect = addons.Any(x => x.Type == WiredBoxType.AddonRandomEffect);
            bool hasUnseen = addons.Any(x => x.Type == WiredBoxType.AddonUnseen);

            if (hasRandomEffect)
            {
                var randomBox = addons.FirstOrDefault(x => x.Type == WiredBoxType.AddonRandomEffect);
                if (randomBox == null || !randomBox.Execute()) return false;

                var selected = Instance.GetWired().GetRandomEffect(effects.ToList());
                if (selected != null && selected.Execute(player))
                    Instance.GetWired().OnEvent(selected.Item);

                Instance.GetWired().OnEvent(randomBox.Item);
            }
            else if (hasUnseen)
            {
                var unseenBox = addons.FirstOrDefault(x => x.Type == WiredBoxType.AddonUnseen);
                if (unseenBox != null && unseenBox.Execute(effects.ToList(), player))
                    Instance.GetWired().OnEvent(unseenBox.Item);
            }
            else if (hasExecuteInOrder)
            {
                foreach (var effect in effects.OrderBy(x => x.Item.GetZ).ToList())
                {
                    if (!effect.Execute(player)) break;
                    Instance.GetWired().OnEvent(effect.Item);
                }
            }
            else
            {
                foreach (var effect in effects.ToList())
                {
                    if (!effect.Execute(player)) continue;
                    Instance.GetWired().OnEvent(effect.Item);
                }
            }

            return true;
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  Serialize
        // ══════════════════════════════════════════════════════════════════════════
        public void Serialize(ServerPacket message)
        {
            message.WriteBoolean(false);
            message.WriteInteger(0);
            message.WriteInteger(0);
            message.WriteInteger(Item.GetBaseItem().SpriteId);
            message.WriteInteger(Item.Id);
            message.WriteString(_variableToken ?? string.Empty);
            message.WriteInteger(7);
            message.WriteInteger(_targetType);
            message.WriteInteger(_createdEnabled ? 1 : 0);
            message.WriteInteger(_valueChangedEnabled ? 1 : 0);
            message.WriteInteger(_increasedEnabled ? 1 : 0);
            message.WriteInteger(_decreasedEnabled ? 1 : 0);
            message.WriteInteger(_unchangedEnabled ? 1 : 0);
            message.WriteInteger(_deletedEnabled ? 1 : 0);
            message.WriteInteger(0);
            message.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            message.WriteInteger(0);
            message.WriteInteger(0);
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  IWiredCustomData
        // ══════════════════════════════════════════════════════════════════════════
        public string GetWiredData()
        {
            return JsonConvert.SerializeObject(new JsonData
            {
                VariableToken = _variableToken,
                VariableItemId = _variableItemId,
                TargetType = _targetType,
                CreatedEnabled = _createdEnabled,
                ValueChangedEnabled = _valueChangedEnabled,
                IncreasedEnabled = _increasedEnabled,
                DecreasedEnabled = _decreasedEnabled,
                UnchangedEnabled = _unchangedEnabled,
                DeletedEnabled = _deletedEnabled
            });
        }

        public void LoadWiredData(string wiredData)
        {
            OnPickUp();

            if (string.IsNullOrWhiteSpace(wiredData)) return;

            // Legacy: plain numeric string
            if (!wiredData.StartsWith("{"))
            {
                SetVariableToken(NormalizeVariableToken(wiredData));
                return;
            }

            JsonData data = null;
            try { data = JsonConvert.DeserializeObject<JsonData>(wiredData); }
            catch { return; }

            if (data == null) return;

            _targetType = NormalizeTargetType(data.TargetType);
            _createdEnabled = data.CreatedEnabled;
            _valueChangedEnabled = data.ValueChangedEnabled;
            _increasedEnabled = data.IncreasedEnabled;
            _decreasedEnabled = data.DecreasedEnabled;
            _unchangedEnabled = data.UnchangedEnabled;
            _deletedEnabled = data.DeletedEnabled;

            string rawToken = !string.IsNullOrEmpty(data.VariableToken)
                ? data.VariableToken
                : (data.VariableItemId > 0 ? data.VariableItemId.ToString() : string.Empty);

            SetVariableToken(NormalizeVariableToken(rawToken));
            NormalizeOptions();
        }

        public void OnPickUp()
        {
            _variableToken = string.Empty;
            _variableItemId = 0;
            _targetType = TARGET_USER;
            _createdEnabled = true;
            _valueChangedEnabled = true;
            _increasedEnabled = true;
            _decreasedEnabled = true;
            _unchangedEnabled = true;
            _deletedEnabled = true;
            StringData = string.Empty;
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  PRIVATE HELPERS
        // ══════════════════════════════════════════════════════════════════════════

        private bool MatchesEvent(object[] stuff)
        {
            if (stuff == null || stuff.Length < 5) return false;

            int eventTargetType = Convert.ToInt32(stuff[0]);
            int eventDefItemId = Convert.ToInt32(stuff[1]);
            bool eventCreated = Convert.ToBoolean(stuff[2]);
            bool eventDeleted = Convert.ToBoolean(stuff[3]);
            var changeKind = (VariableChangeKind)Convert.ToInt32(stuff[4]);

            if (eventTargetType != _targetType || eventDefItemId != _variableItemId)
                return false;

            if (_createdEnabled && eventCreated) return true;
            if (_deletedEnabled && eventDeleted) return true;
            if (!_valueChangedEnabled) return false;

            return changeKind switch
            {
                VariableChangeKind.Increased => _increasedEnabled,
                VariableChangeKind.Decreased => _decreasedEnabled,
                VariableChangeKind.Unchanged => _unchangedEnabled,
                _ => false
            };
        }

        private void SetVariableToken(string token)
        {
            _variableToken = NormalizeVariableToken(token);
            _variableItemId = GetCustomItemId(_variableToken);
        }

        private void NormalizeOptions()
        {
            if (!_valueChangedEnabled)
            {
                _increasedEnabled = false;
                _decreasedEnabled = false;
                _unchangedEnabled = false;
            }

            if (_targetType == TARGET_ROOM)
            {
                _createdEnabled = false;
                _deletedEnabled = false;
            }
        }

        private static int NormalizeTargetType(int value) => value switch
        {
            TARGET_FURNI or TARGET_ROOM => value,
            _ => TARGET_USER
        };

        private static string NormalizeVariableToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return string.Empty;

            string t = token.Trim();
            if (t.StartsWith(CustomTokenPrefix, StringComparison.Ordinal)) return t;

            if (int.TryParse(t, out int id) && id > 0)
                return CustomTokenPrefix + id;

            return string.Empty;
        }

        private static int GetCustomItemId(string token)
        {
            if (string.IsNullOrEmpty(token) ||
                !token.StartsWith(CustomTokenPrefix, StringComparison.Ordinal))
                return 0;

            return int.TryParse(token.Substring(CustomTokenPrefix.Length), out int id) ? id : 0;
        }

        // ── JSON DTO ──────────────────────────────────────────────────────────────
        private class JsonData
        {
            [JsonProperty("variableToken")]
            public string VariableToken { get; set; } = string.Empty;

            [JsonProperty("variableItemId")]
            public int VariableItemId { get; set; }

            [JsonProperty("targetType")]
            public int TargetType { get; set; }

            [JsonProperty("createdEnabled")]
            public bool CreatedEnabled { get; set; } = true;

            [JsonProperty("valueChangedEnabled")]
            public bool ValueChangedEnabled { get; set; } = true;

            [JsonProperty("increasedEnabled")]
            public bool IncreasedEnabled { get; set; } = true;

            [JsonProperty("decreasedEnabled")]
            public bool DecreasedEnabled { get; set; } = true;

            [JsonProperty("unchangedEnabled")]
            public bool UnchangedEnabled { get; set; } = true;

            [JsonProperty("deletedEnabled")]
            public bool DeletedEnabled { get; set; } = true;
        }
    }
}