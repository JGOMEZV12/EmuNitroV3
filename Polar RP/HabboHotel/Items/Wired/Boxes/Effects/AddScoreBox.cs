using Polar.HabboHotel.Items.Wired;
using Polar.Communication.Packets.Outgoing;
using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using System.Globalization;

namespace Polar.HabboHotel.Items.Wired.Boxes.Effects
{
    class AddScoreBox : IWiredItem, IWiredCycle
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type { get { return WiredBoxType.EffectAddScore; } }
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public int Delay { get { return this._delay; } set { this._delay = value; this.TickCount = value + 1; } }
        public int TickCount { get; set; }
        public string ItemsData { get; set; }

        private Queue _queue;
        private int _delay = 0;

        public AddScoreBox(Room Instance, Item Item)
        {
            this.Instance = Instance;
            this.Item = Item;
            this.SetItems = new ConcurrentDictionary<int, Item>();
            this._queue = new Queue();
            this.TickCount = Delay;
        }

                public void HandleSave(ClientPacket packet)
        {
            int paramsCount = packet.PopInt();
            for (int i = 0; i < paramsCount; i++) packet.PopInt();

            this.StringData = packet.PopString();

            if (this.SetItems != null) this.SetItems.Clear();
            int itemsCount = packet.PopInt();
            for (int i = 0; i < itemsCount; i++)
            {
                Item item = Instance.GetRoomItemHandler().GetItem(packet.PopInt());
                if (item != null) this.SetItems.TryAdd(item.Id, item);
            }

            int delay = packet.PopInt();
            if (this is IWiredCycle cycle) cycle.Delay = delay;
        }

        public bool OnCycle()
        {
            if (_queue.Count == 0)
            {
                this._queue.Clear();
                this.TickCount = Delay;
                return true;
            }

            while (_queue.Count > 0)
            {
                Habbo Player = (Habbo)_queue.Dequeue();
                if (Player == null || Player.CurrentRoom != Instance)
                    continue;

                this.TeleportUser(Player);
            }

            this.TickCount = Delay;
            return true;
        }


                                public void Serialize(ServerPacket packet)
        {
            packet.WriteBoolean(false);
            packet.WriteInteger(100);
            packet.WriteInteger(SetItems?.Count ?? 0);
            foreach (var item in SetItems?.Values.ToList() ?? new List<Item>()) packet.WriteInteger(item.Id);
            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            packet.WriteString(StringData ?? "");
            packet.WriteInteger(0); // Params count
            packet.WriteInteger(0); // Categorical
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(this is IWiredCycle cycle ? cycle.Delay : 0);
        }
        public bool Execute(params object[] Params)
        {
            if (Params == null || Params.Length == 0)
                return false;

            Habbo Player = (Habbo)Params[0];
            if (Player == null)
                return false;

            this._queue.Enqueue(Player);
            return true;
        }

        private void TeleportUser(Habbo Player)
        {
            RoomUser User = Player.CurrentRoom.GetRoomUserManager().GetRoomUserByHabbo(Player.Id);
            if (User == null)
                return;

            Room Instance = Player.CurrentRoom;
            string[] parts = StringData.Split(';');
            int score = int.Parse(parts[0]);
            int operation = parts.Length > 1 ? int.Parse(parts[1]) : 0;
            int mScore = operation == 1 ? -score : score; // 1 = remove = negativo
            int currentscore = 0;
            KeyValuePair<int, string> newkey;
            KeyValuePair<int, string> item;
            DateTime now = DateTime.Now;
            int getdaytoday = Convert.ToInt32(now.ToString("MMddyyyy"));
            int getmonthtoday = Convert.ToInt32(now.ToString("MM"));
            int getweektoday = CultureInfo.GetCultureInfo("Nl-nl").Calendar.GetWeekOfYear(DateTime.Now, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

            // FIX: Condición simplificada — la original era innecesariamente compleja
            if (Instance != null && User != null && !User.IsBot)
            {
                Instance.GetRoomItemHandler().usedwiredscorebord = true;

                if (Instance.WiredScoreFirstBordInformation.Count == 3)
                    Instance.GetRoomItemHandler().ScorebordChangeCheck();

                if (Instance.WiredScoreBordDay != null && Instance.WiredScoreBordMonth != null && Instance.WiredScoreBordWeek != null)
                {
                    string username = User.GetClient().GetHabbo().Username;

                    bool hasDay = false, hasWeek = false, hasMonth = false;

                    foreach (var roomItem in Instance.GetRoomItemHandler().GetFloor)
                    {
                        string itemName = roomItem.GetBaseItem().ItemName.ToLower();
                        if (itemName == "highscore_classic*2" || itemName == "highscore_mostwin*2" || itemName == "highscore_perteamn*2")
                            hasDay = true;
                        else if (itemName == "highscore_classic*3" || itemName == "highscore_mostwin*3" || itemName == "highscore_perteamn*3")
                            hasWeek = true;
                        else if (itemName == "highscore_classic*4" || itemName == "highscore_mostwin*4" || itemName == "highscore_perteamn*4")
                            hasMonth = true;
                    }

                    if (hasDay)
                    {
                        lock (Instance.WiredScoreBordDay)
                        {
                            if (!Instance.WiredScoreBordDay.ContainsKey(User.UserId))
                                Instance.WiredScoreBordDay.Add(User.UserId, new KeyValuePair<int, string>(mScore, username));
                            else
                            {
                                item = Instance.WiredScoreBordDay[User.UserId];
                                currentscore = item.Key + mScore;
                                Instance.WiredScoreBordDay[User.UserId] = new KeyValuePair<int, string>(currentscore, username);
                            }

                            using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                            {
                                dbClient.SetQuery("SELECT COUNT(*) FROM `wired_scorebord` WHERE `roomid` = '" + Item.Id + "' AND `userid` = '" + User.UserId + "' AND `soort` = 'day'");
                                int exists = dbClient.getInteger();

                                if (exists == 0)
                                {
                                    dbClient.SetQuery("INSERT INTO `wired_scorebord` (`roomid`, `userid`, `username`, `punten`, `soort`, `timestamp`, `item_id`) VALUES ('" + Item.Id + "', '" + User.UserId + "', @dusername, '" + mScore + "', 'day', '" + getdaytoday + "', '" + Item.Id + "')");
                                    dbClient.AddParameter("dusername", username);
                                    dbClient.RunQuery();
                                }
                                else
                                {
                                    dbClient.SetQuery("UPDATE `wired_scorebord` SET `punten` = `punten` + '" + mScore + "', `username` = @dusername, `timestamp` = '" + getdaytoday + "' WHERE `roomid` = '" + Item.Id + "' AND `userid` = '" + User.UserId + "' AND `soort` = 'day'");
                                    dbClient.AddParameter("dusername", username);
                                    dbClient.RunQuery();
                                }
                            }
                        }
                    }

                    if (hasWeek)
                    {
                        lock (Instance.WiredScoreBordWeek)
                        {
                            if (!Instance.WiredScoreBordWeek.ContainsKey(User.UserId))
                                Instance.WiredScoreBordWeek.Add(User.UserId, new KeyValuePair<int, string>(mScore, username));
                            else
                            {
                                item = Instance.WiredScoreBordWeek[User.UserId];
                                currentscore = item.Key + mScore;
                                Instance.WiredScoreBordWeek[User.UserId] = new KeyValuePair<int, string>(currentscore, username);
                            }

                            using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                            {
                                dbClient.SetQuery("SELECT COUNT(*) FROM `wired_scorebord` WHERE `roomid` = '" + Item.Id + "' AND `userid` = '" + User.UserId + "' AND `soort` = 'week'");
                                int exists = dbClient.getInteger();

                                if (exists == 0)
                                {
                                    dbClient.SetQuery("INSERT INTO `wired_scorebord` (`roomid`, `userid`, `username`, `punten`, `soort`, `timestamp`, `item_id`) VALUES ('" + Item.Id + "', '" + User.UserId + "', @wusername, '" + mScore + "', 'week', '" + getweektoday + "', '" + Item.Id + "')");
                                    dbClient.AddParameter("wusername", username);
                                    dbClient.RunQuery();
                                }
                                else
                                {
                                    dbClient.SetQuery("UPDATE `wired_scorebord` SET `punten` = `punten` + '" + mScore + "', `username` = @wusername, `timestamp` = '" + getweektoday + "' WHERE `roomid` = '" + Item.Id + "' AND `userid` = '" + User.UserId + "' AND `soort` = 'week'");
                                    dbClient.AddParameter("wusername", username);
                                    dbClient.RunQuery();
                                }
                            }
                        }
                    }

                    if (hasMonth)
                    {
                        lock (Instance.WiredScoreBordMonth)
                        {
                            if (!Instance.WiredScoreBordMonth.ContainsKey(User.UserId))
                                Instance.WiredScoreBordMonth.Add(User.UserId, new KeyValuePair<int, string>(mScore, username));
                            else
                            {
                                item = Instance.WiredScoreBordMonth[User.UserId];
                                currentscore = item.Key + mScore;
                                Instance.WiredScoreBordMonth[User.UserId] = new KeyValuePair<int, string>(currentscore, username);
                            }

                            using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                            {
                                dbClient.SetQuery("SELECT COUNT(*) FROM `wired_scorebord` WHERE `roomid` = '" + Item.Id + "' AND `userid` = '" + User.UserId + "' AND `soort` = 'month'");
                                int exists = dbClient.getInteger();

                                if (exists == 0)
                                {
                                    dbClient.SetQuery("INSERT INTO `wired_scorebord` (`roomid`, `userid`, `username`, `punten`, `soort`, `timestamp`, `item_id`) VALUES ('" + Item.Id + "', '" + User.UserId + "', @musername, '" + mScore + "', 'month', '" + getmonthtoday + "', '" + Item.Id + "')");
                                    dbClient.AddParameter("musername", username);
                                    dbClient.RunQuery();
                                }
                                else
                                {
                                    dbClient.SetQuery("UPDATE `wired_scorebord` SET `punten` = `punten` + '" + mScore + "', `username` = @musername, `timestamp` = '" + getmonthtoday + "' WHERE `roomid` = '" + Item.Id + "' AND `userid` = '" + User.UserId + "' AND `soort` = 'month'");
                                    dbClient.AddParameter("musername", username);
                                    dbClient.RunQuery();
                                }
                            }
                        }
                    }
                }
                Instance.GetRoomItemHandler().UpdateWiredScoreBord();
                User.GetClient().SendMessage(new RoomBubbleNotificationComposer("award", "Has ganado " + mScore + " puntos en la clasificación. ¡Enhorabuena!", ""));

                if (Player.Effects() != null)
                    Player.Effects().ApplyEffect(0);
            }
        }
    }
}
