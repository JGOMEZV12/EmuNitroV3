using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.Communication.Packets.Outgoing.Catalog;
using Polar.Communication.Packets.Outgoing.Inventory.Furni;
using Polar.Communication.Packets.Outgoing.Inventory.Purse;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Instance;
using Polar.HabboHotel.Users;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Polar.HabboHotel.Items.Wired.Boxes.Effects
{
    internal class GiveRewardBox : IWiredItem, IWiredCycle, IWiredCustomData
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.EffectGiveReward;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        public int Delay
        {
            get => _delay;
            set { _delay = value; TickCount = value + 1; }
        }

        public int TickCount { get; set; }

        // ── Campos de recompensa (mirrors WiredEffectGiveReward Java) ──────────
        public int Limit { get; set; } = 0;
        public int Given { get; set; } = 0;
        public int RewardTime { get; set; } = 0;
        public bool UniqueRewards { get; set; } = false;
        public int LimitationInterval { get; set; } = 0;
        public List<WiredRewardItem> RewardItems { get; set; } = new List<WiredRewardItem>();

        private int _delay;
        private int _userSource = WiredSourceUtil.SOURCE_TRIGGER;
        private bool _requested;
        private Habbo _pendingActor;

        // Constantes de intervalo (mirrors Java)
        public const int LIMIT_ONCE = 0;
        public const int LIMIT_N_DAY = 1;
        public const int LIMIT_N_HOURS = 2;
        public const int LIMIT_N_MINUTES = 3;

        public GiveRewardBox(Room instance, Item item)
        {
            Instance = instance;
            Item = item;
            SetItems = new ConcurrentDictionary<int, Item>();
            TickCount = Delay;
            _requested = false;
        }

        // ── IWiredCustomData ───────────────────────────────────────────────────

        public string GetWiredData()
        {
            return JsonConvert.SerializeObject(new JsonData
            {
                limit = Limit,
                given = Given,
                reward_time = RewardTime,
                unique_rewards = UniqueRewards,
                limit_interval = LimitationInterval,
                rewards = RewardItems,
                delay = Delay,
                userSource = _userSource
            });
        }

        public void LoadWiredData(string wiredData)
        {
            Limit = 0;
            Given = 0;
            RewardTime = 0;
            UniqueRewards = false;
            LimitationInterval = 0;
            RewardItems.Clear();
            _userSource = WiredSourceUtil.SOURCE_TRIGGER;
            Delay = 0;

            if (string.IsNullOrEmpty(wiredData)) return;

            if (wiredData.StartsWith("{"))
            {
                var data = JsonConvert.DeserializeObject<JsonData>(wiredData);
                if (data == null) return;

                Limit = data.limit;
                Given = data.given;
                RewardTime = data.reward_time;
                UniqueRewards = data.unique_rewards;
                LimitationInterval = data.limit_interval;
                Delay = data.delay;
                _userSource = data.userSource;

                if (data.rewards != null)
                    RewardItems.AddRange(data.rewards);
            }
            else if (wiredData.Contains(':'))
            {
                // Retrocompatibilidad Java legacy: "limit:given:rewardTime:unique:interval:delay:items"
                var parts = wiredData.Split(':');
                if (parts.Length > 5)
                {
                    int.TryParse(parts[0], out int limit); Limit = limit;
                    int.TryParse(parts[1], out int given); Given = given;
                    int.TryParse(parts[2], out int rtime); RewardTime = rtime;
                    UniqueRewards = parts[3] == "1";
                    int.TryParse(parts[4], out int interval); LimitationInterval = interval;
                    if (int.TryParse(parts[5], out int delay)) Delay = delay;

                    if (parts.Length > 6 && parts[6] != "\t")
                    {
                        foreach (var s in parts[6].Split(';'))
                        {
                            try { RewardItems.Add(new WiredRewardItem(s)); }
                            catch { }
                        }
                    }
                }

                _userSource = WiredSourceUtil.SOURCE_TRIGGER;
            }
            else
            {
                // Retrocompatibilidad C# viejo: "reward-often-limit"
                var parts = wiredData.Split('-');
                if (parts.Length >= 3)
                {
                    int.TryParse(parts[2], out int lim); Limit = lim;
                    int.TryParse(parts[1], out int oft); RewardTime = oft;

                    foreach (var s in parts[0].Split(';'))
                    {
                        try { RewardItems.Add(WiredRewardItem.FromLegacyCSharp(s)); }
                        catch { }
                    }
                }

                UniqueRewards = BoolData;
                _userSource = WiredSourceUtil.SOURCE_TRIGGER;
            }

            // Sincronizar StringData para compatibilidad
            StringData = BuildLegacyStringData();
            TickCount = Delay;

            Console.WriteLine($"[GiveRewardBox LoadWiredData] Limit={Limit}, Items={RewardItems.Count}");
            foreach (var r in RewardItems)
                Console.WriteLine($"  Loaded: IsBadge={r.IsBadge}, Code={r.Code}, Prob={r.Probability}");
        }

        // ── Packet handling ────────────────────────────────────────────────────

        public void HandleSave(ClientPacket packet)
        {
            int intCount = packet.PopInt();
            RewardTime = intCount > 0 ? packet.PopInt() : 0;
            UniqueRewards = intCount > 1 && packet.PopInt() == 1;
            Limit = intCount > 2 ? packet.PopInt() : 0;
            LimitationInterval = intCount > 3 ? packet.PopInt() : 0;
            _userSource = intCount > 4 ? packet.PopInt() : WiredSourceUtil.SOURCE_TRIGGER;
            for (int i = 5; i < intCount; i++) packet.PopInt();

            string rewardsData = packet.PopString(); // s1 = rewards

            int itemCount = packet.PopInt(); // v7 = 0 (sin furnis)
            for (int i = 0; i < itemCount; i++) packet.PopInt();

            Delay = packet.PopInt();    // v8 = delay
            packet.PopInt();            // v9 = stuffTypeSelectionCode, ignorar

            Given = 0;

            RewardItems.Clear();
            int idx = 1;
            foreach (var s in rewardsData.Split(';'))
            {
                if (string.IsNullOrEmpty(s)) continue;
                var d = s.Split(',');
                if (d.Length == 3 && !d[1].Contains(":") && !d[1].Contains(";"))
                    RewardItems.Add(new WiredRewardItem(idx++,
                        d[0].Equals("0", StringComparison.OrdinalIgnoreCase),
                        d[1],
                        int.TryParse(d[2], out int p) ? p : 0));
            }

            StringData = BuildLegacyStringData();
        }
        public void Serialize(ServerPacket packet)
        {
            Console.WriteLine($"[GiveRewardBox Serialize] Limit={Limit}, Given={Given}, RewardTime={RewardTime}, UniqueRewards={UniqueRewards}, Items={RewardItems.Count}");
            foreach (var r in RewardItems)
                Console.WriteLine($"  Reward: IsBadge={r.IsBadge}, Code={r.Code}, Prob={r.Probability}, WiredStr={r.ToWiredString()}");

            var sb = new StringBuilder();
            foreach (var r in RewardItems)
                sb.Append(r.ToWiredString()).Append(';');

            packet.WriteBoolean(false);
            packet.WriteInteger(5);  // MAXIMUM_FURNI_SELECTION
            packet.WriteInteger(0);  // furni count
            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            packet.WriteString(sb.ToString());
            packet.WriteInteger(5);
            packet.WriteInteger(RewardTime);
            packet.WriteInteger(UniqueRewards ? 1 : 0);
            packet.WriteInteger(Limit);
            packet.WriteInteger(LimitationInterval);
            packet.WriteInteger(_userSource);
            packet.WriteInteger(Limit > 0 ? 1 : 0);
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(Delay);
            packet.WriteInteger(0);
        }

        // ── Lógica ─────────────────────────────────────────────────────────────

        public bool Execute(params object[] Params)
        {
            if (RewardItems.Count == 0)
            {
                Console.WriteLine("[GiveRewardBox] Execute — sin reward items");
                return false;
            }

            Habbo owner = PolarEnvironment.GetHabboById(Item.UserID);
            Console.WriteLine($"[GiveRewardBox] Execute — owner={owner?.Username ?? "NULL"}, hasRight={owner?.GetPermissions().HasRight("room_item_wired_rewards")}");

            if (owner == null || !owner.GetPermissions().HasRight("room_item_wired_rewards"))
                return false;

            _pendingActor = Params.Length > 0 ? Params[0] as Habbo : null;
            Console.WriteLine($"[GiveRewardBox] Execute — pendingActor={_pendingActor?.Username ?? "NULL"}");

            if (!_requested)
            {
                TickCount = Delay;
                _requested = true;
            }
            return true;
        }

        public bool OnCycle()
        {
            //Console.WriteLine($"[GiveRewardBox] OnCycle — requested={_requested}, pendingActor={_pendingActor?.Username ?? "NULL"}, RewardItems={RewardItems.Count}");

            if (Instance == null || !_requested) return false;

            _requested = false;

            var targets = ResolveTargets();
            //Console.WriteLine($"[GiveRewardBox] OnCycle — targets={targets.Count}");

            foreach (var habbo in targets)
            {
                Console.WriteLine($"[GiveRewardBox] OnCycle — giving reward to {habbo?.Username}");
                if (habbo?.GetClient() == null) continue;
                GiveRewardToHabbo(habbo);
            }

            _pendingActor = null;
            return true;
        }

        // ── Lógica de premios (port de WiredManager.getReward) ────────────────

        private void GiveRewardToHabbo(Habbo habbo)
        {
            if (habbo?.GetClient() == null) return;

            RoomUser user = Instance.GetRoomUserManager().GetRoomUserByHabbo(habbo.Id);
            if (user == null) return;

            if (Limit > 0 && Given >= Limit)
            {
                habbo.GetClient().SendNotification("Ya no hay más premios, vuelve más tarde.");
                return;
            }

            if (!CheckRewardCooldown(habbo)) return;

            bool premied = false;

            foreach (var reward in RewardItems)
            {
                int roll = PolarEnvironment.GetRandomNumber(0, 100);
                if (!UniqueRewards && reward.Probability < roll) continue;

                premied = true;

                if (reward.IsBadge)
                {
                    if (habbo.GetBadgeComponent().HasBadge(reward.Code))
                        habbo.GetClient().SendMessage(new WhisperComposer(
                            user.VirtualId, "¡Ya tienes esta placa!", 0, user.LastBubble));
                    else
                    {
                        habbo.GetBadgeComponent().GiveBadge(reward.Code, true, habbo.GetClient());
                        habbo.GetClient().SendMessage(new RoomBubbleNotificationComposer(
                            "badge/" + reward.Code, "¡Recibiste una placa!", "/inventory/open/badge"));
                    }
                }
                else if (reward.Code.StartsWith("credits#"))
                {
                    if (int.TryParse(reward.Code.Split('#')[1], out int amount))
                    {
                        habbo.Credits += amount;
                        habbo.UpdateCreditsBalance();
                    }
                }
                else if (reward.Code.StartsWith("pixels#") || reward.Code.StartsWith("duckets#"))
                {
                    if (int.TryParse(reward.Code.Split('#')[1], out int amount))
                    {
                        habbo.Duckets += amount;
                        habbo.UpdateDucketsBalance();
                    }
                }
                else if (reward.Code.StartsWith("diamonds#") || reward.Code.StartsWith("points#"))
                {
                    if (int.TryParse(reward.Code.Split('#')[1], out int amount))
                    {
                        habbo.Diamonds += amount;
                        habbo.UpdateDiamondsBalance();
                    }
                }
                else
                {
                    // Item furni
                    if (!int.TryParse(reward.Code, out int itemId) ||
                        !PolarEnvironment.GetGame().GetItemManager().GetItem(itemId, out ItemData itemData))
                    {
                        habbo.GetClient().SendMessage(new WhisperComposer(
                            user.VirtualId, "No se pudo obtener el premio.", 0, user.LastBubble));
                        continue;
                    }

                    var newItem = ItemFactory.CreateSingleItemNullable(itemData, habbo, "", "", 0, 0, 0);
                    if (newItem != null)
                    {
                        habbo.GetInventoryComponent().TryAddItem(newItem);
                        habbo.GetClient().SendMessage(new FurniListNotificationComposer(newItem.Id, 1));
                        habbo.GetClient().SendMessage(new PurchaseOKComposer());
                        habbo.GetClient().SendMessage(new FurniListAddComposer(newItem));
                        habbo.GetClient().SendMessage(new FurniListUpdateComposer());
                        habbo.GetClient().SendNotification("¡Has recibido un regalo!");
                    }
                }
            }

            if (!premied)
                habbo.GetClient().SendNotification("Suerte la próxima vez :(");
            else
            {
                Given++;
                SetRewardTimestamp(habbo);
                StringData = BuildLegacyStringData();
                Instance.GetWired().SaveBox(this);
            }
        }
        // ── Cooldown por usuario ───────────────────────────────────────────────

        // Clave: userId → timestamp Unix del último premio
        private readonly ConcurrentDictionary<int, long> _lastRewardTime =
            new ConcurrentDictionary<int, long>();

        private bool CheckRewardCooldown(Habbo habbo)
        {
            if (RewardTime == LIMIT_ONCE)
            {
                // Solo una vez por usuario en toda la vida del wired
                return !_lastRewardTime.ContainsKey(habbo.Id);
            }

            if (!_lastRewardTime.TryGetValue(habbo.Id, out long last))
                return true;

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long elapsed = now - last;

            return RewardTime switch
            {
                LIMIT_N_DAY => elapsed >= 86400L * LimitationInterval,
                LIMIT_N_HOURS => elapsed >= 3600L * LimitationInterval,
                LIMIT_N_MINUTES => elapsed >= 60L * LimitationInterval,
                _ => true
            };
        }

        private void SetRewardTimestamp(Habbo habbo)
        {
            _lastRewardTime[habbo.Id] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private List<Habbo> ResolveTargets()
        {
            var result = new List<Habbo>();

            if (_userSource == WiredSourceUtil.SOURCE_TRIGGER ||
                _userSource == WiredSourceUtil.SOURCE_CLICKED_USER)
            {
                if (_pendingActor != null)
                    result.Add(_pendingActor);
            }
            else
            {
                foreach (var ru in Instance.GetRoomUserManager().GetRoomUsers())
                {
                    if (ru == null || ru.IsBot) continue;
                    var h = ru.GetClient()?.GetHabbo();
                    if (h != null) result.Add(h);
                }
            }

            return result;
        }

        private string BuildLegacyStringData()
        {
            var sb = new StringBuilder();
            foreach (var r in RewardItems)
                sb.Append(r.ToLegacyCSharp()).Append(';');

            int limit = Limit > 0 ? Limit : -1;
            return $"{sb}-{RewardTime}-{limit}";
        }

        // ── DTO JSON ───────────────────────────────────────────────────────────

        private class JsonData
        {
            public int limit { get; set; }
            public int given { get; set; }
            public int reward_time { get; set; }
            public bool unique_rewards { get; set; }
            public int limit_interval { get; set; }
            public List<WiredRewardItem> rewards { get; set; }
            public int delay { get; set; }
            public int userSource { get; set; }
        }
    }

    // ── WiredRewardItem (port de WiredGiveRewardItem Java) ─────────────────────

    public class WiredRewardItem
    {
        public int Index { get; set; }
        public bool IsBadge { get; set; }
        public string Code { get; set; }
        public int Probability { get; set; }

        public WiredRewardItem() { }

        public WiredRewardItem(int index, bool isBadge, string code, int probability)
        {
            Index = index;
            IsBadge = isBadge;
            Code = code;
            Probability = probability;
        }

        /// <summary>Carga desde el formato legacy Java: "index,isBadge,code,probability"</summary>
        public WiredRewardItem(string raw)
        {
            var d = raw.Split(',');
            if (d.Length < 4) throw new ArgumentException("Malformed reward string");
            int.TryParse(d[0], out int idx); Index = idx;
            IsBadge = d[1] == "0";
            Code = d[2];
            int.TryParse(d[3], out int prob); Probability = prob;
        }

        /// <summary>Carga desde el formato legacy C#: "isBadge(0/1),code,probability"</summary>
        public static WiredRewardItem FromLegacyCSharp(string raw)
        {
            var d = raw.Split(',');
            if (d.Length < 3) throw new ArgumentException("Malformed reward string");
            return new WiredRewardItem
            {
                IsBadge = d[0] == "0",
                Code = d[1],
                Probability = int.TryParse(d[2], out int p) ? p : 0
            };
        }

        /// <summary>Serializa para el packet del cliente (port de wiredString() Java)</summary>
        public string ToWiredString() => $"{(IsBadge ? "0" : "1")},{Code},{Probability}";

        /// <summary>Serializa al formato C# legacy para compatibilidad con StringData</summary>
        public string ToLegacyCSharp() => $"{(IsBadge ? "0" : "1")},{Code},{Probability}";
    }
}