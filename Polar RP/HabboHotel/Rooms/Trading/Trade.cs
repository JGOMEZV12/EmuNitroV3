using Polar.Communication.Packets.Outgoing;
using Polar.Communication.Packets.Outgoing.Inventory.Trading;
using Polar.Core;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Items;

namespace Polar.HabboHotel.Rooms
{
    public class Trade
    {
        private readonly TradeUser[] Users;
        private int TradeStage;

        private readonly int RoomId;
        private readonly int oneId;
        private readonly int twoId;

        public bool AllUsersAccepted
        {
            get
            {
                for (int i = 0; i < Users.Length; i++)
                {
                    if (Users[i] == null)
                        continue;
                    if (!Users[i].HasAccepted)
                        return false;
                }

                return true;
            }
        }

        public Trade(int UserOneId, int UserTwoId, int RoomId)
        {
            this.oneId = UserOneId;
            this.twoId = UserTwoId;
            this.Users = new TradeUser[2];
            this.Users[0] = new TradeUser(UserOneId, RoomId);
            this.Users[1] = new TradeUser(UserTwoId, RoomId);
            this.TradeStage = 1;
            this.RoomId = RoomId;
            
            ServerPacket Message = new ServerPacket(ServerPacketHeader.TradingStartMessageComposer);
            Message.WriteInteger(UserOneId);
            Message.WriteInteger(1);
            Message.WriteInteger(UserTwoId);
            Message.WriteInteger(1);
            this.SendMessageToUsers(Message);

            foreach (TradeUser tradeUser in this.Users)
            {
                if (!tradeUser.GetRoomUser().Statusses.ContainsKey("trd"))
                {
                    tradeUser.GetRoomUser().SetStatus("trd");
                    tradeUser.GetRoomUser().IsTrading = true;
                    tradeUser.GetRoomUser().UpdateNeeded = true;
                }
            }
        }

        public bool ContainsUser(int Id)
        {
            for (int index = 0; index < this.Users.Length; ++index)
            {
                if (this.Users[index] != null && this.Users[index].UserId == Id)
                    return true;
            }
            return false;
        }

        public TradeUser GetTradeUser(int Id)
        {
            for (int index = 0; index < this.Users.Length; ++index)
            {
                if (this.Users[index] != null && this.Users[index].UserId == Id)
                    return this.Users[index];
            }
            return (TradeUser)null;
        }

        public void OfferItem(int UserId, Item Item)
        {
            TradeUser tradeUser = this.GetTradeUser(UserId);
            if (tradeUser == null || Item == null || (!Item.GetBaseItem().AllowTrade || tradeUser.HasAccepted) || this.TradeStage != 1)
                return;
            this.ClearAccepted();
            if (!tradeUser.OfferedItems.Contains(Item))
                tradeUser.OfferedItems.Add(Item);
            this.UpdateTradeWindow();
        }

        public void TakeBackItem(int UserId, Item Item)
        {
            TradeUser tradeUser = this.GetTradeUser(UserId);
            if (tradeUser == null || Item == null || (tradeUser.HasAccepted || this.TradeStage != 1))
                return;
            this.ClearAccepted();
            tradeUser.OfferedItems.Remove(Item);
            this.UpdateTradeWindow();
        }

        public void Accept(int UserId)
        {
            TradeUser User = GetTradeUser(UserId);

            if (User == null || TradeStage != 1)
            {
                return;
            }

            User.HasAccepted = true;

            SendMessageToUsers(new TradingAcceptComposer(UserId, true));


            if (AllUsersAccepted)
            {
                TradeStage++;
                ClearAccepted();
                try
                {
                    this.DeliverItems();
                    this.CloseTradeClean();
                }
                catch (Exception ex)
                {
                    Logging.LogThreadException((ex).ToString(), "Trade task");
                }
                
            }
        }

        public void Unaccept(int UserId)
        {
            TradeUser tradeUser = this.GetTradeUser(UserId);
            if (tradeUser == null || this.TradeStage != 1 || this.AllUsersAccepted)
                return;
            tradeUser.HasAccepted = false;
            ServerPacket Message = new ServerPacket(ServerPacketHeader.TradingAcceptMessageComposer);
            Message.WriteInteger(UserId);
            Message.WriteInteger(0);
            this.SendMessageToUsers(Message);
        }

        public void CompleteTrade(int UserId)
        {
            TradeUser User = GetTradeUser(UserId);

            if (User == null || TradeStage != 2)
            {
                return;
            }

            User.HasAccepted = true;
            SendMessageToUsers(new TradingConfirmedComposer(UserId, true));
            if (AllUsersAccepted)
            {
                TradeStage = 999;
                Finnito();
            }
        }

        private void Finnito()
        {
            try
            {
                this.DeliverItems();
                this.CloseTradeClean();
            }
            catch (Exception ex)
            {
                Logging.LogThreadException((ex).ToString(), "Trade task");
            }
        }

        public void ClearAccepted()
        {
            foreach (TradeUser tradeUser in this.Users)
                tradeUser.HasAccepted = false;
        }

        public void UpdateTradeWindow()
        {
            ServerPacket Message = new ServerPacket(ServerPacketHeader.TradingUpdateMessageComposer);
            for (int index = 0; index < this.Users.Length; ++index)
            {
                TradeUser tradeUser = this.Users[index];
                if (tradeUser != null)
                {
                    Message.WriteInteger(tradeUser.UserId);
                    Message.WriteInteger(tradeUser.OfferedItems.Count);
                    foreach (Item userItem in tradeUser.OfferedItems)
                    {
                        Message.WriteInteger(userItem.Id);
                        Message.WriteString(userItem.GetBaseItem().Type.ToString().ToLower());
                        Message.WriteInteger(userItem.Id);
                        Message.WriteInteger(userItem.GetBaseItem().SpriteId);
                        Message.WriteInteger(0);
                        if (userItem.LimitedNo > 0)
                        {
                            Message.WriteBoolean(false);
                            Message.WriteInteger(256);
                            Message.WriteString("");
                            Message.WriteInteger(userItem.LimitedNo);
                            Message.WriteInteger(userItem.LimitedTot);
                        }
                        else if (userItem.GetBaseItem().InteractionType == InteractionType.BADGE_DISPLAY)
                        {
                            Message.WriteBoolean(false);
                            Message.WriteInteger(2);
                            Message.WriteInteger(4);

                            if (userItem.ExtraData.Contains(Convert.ToChar(9).ToString()))
                            {
                                string[] BadgeData = userItem.ExtraData.Split(Convert.ToChar(9));

                                Message.WriteString("0");//No idea
                                Message.WriteString(BadgeData[0]);//Badge name
                                Message.WriteString(BadgeData[1]);//Owner
                                Message.WriteString(BadgeData[2]);//Date
                            }
                            else
                            {
                                Message.WriteString("0");//No idea
                                Message.WriteString(userItem.ExtraData);//Badge name
                                Message.WriteString("");//Owner
                                Message.WriteString("");//Date
                            }
                        }
                        else
                        {
                            Message.WriteBoolean(true);
                            Message.WriteInteger(0);
                            Message.WriteString("");
                        }
                        Message.WriteInteger(0);
                        Message.WriteInteger(0);
                        Message.WriteInteger(0);
                        if (userItem.GetBaseItem().Type == 's')
                            Message.WriteInteger(0);
                    }
                    Message.WriteInteger(tradeUser.OfferedItems.Count);
                    Message.WriteInteger(0);

                }
            }
            this.SendMessageToUsers(Message);
        }

        public void DeliverItems()
        {
            // List items
            List<Item> ItemsOne = GetTradeUser(oneId).OfferedItems;
            List<Item> ItemsTwo = GetTradeUser(twoId).OfferedItems;

            string User1 = "";
            string User2 = "";

            // Verify they are still in user inventory
            foreach (Item I in ItemsOne.ToList())
            {
                if (I == null)
                    continue;

                if (GetTradeUser(oneId).GetClient().GetHabbo().GetInventoryComponent().GetItem(I.Id) == null)
                {
                    GetTradeUser(oneId).GetClient().SendNotification(PolarEnvironment.GetGame().GetLanguageLocale().TryGetValue("trade_failed"));
                    GetTradeUser(twoId).GetClient().SendNotification(PolarEnvironment.GetGame().GetLanguageLocale().TryGetValue("trade_failed"));
                    return;
                }
                User1 += I.Id + ";";
                //Logging.LogException("Aqui bien bro 0-1");
            }

            foreach (Item I in ItemsTwo.ToList())
            {
                if (I == null)
                    continue;

                if (GetTradeUser(twoId).GetClient().GetHabbo().GetInventoryComponent().GetItem(I.Id) == null)
                {
                    GetTradeUser(oneId).GetClient().SendNotification(PolarEnvironment.GetGame().GetLanguageLocale().TryGetValue("trade_failed"));
                    GetTradeUser(twoId).GetClient().SendNotification(PolarEnvironment.GetGame().GetLanguageLocale().TryGetValue("trade_failed"));

                    return;
                }
                User2 += I.Id + ";";
                //Logging.LogException("Aqui bien bro 0-2");
            }


            // Deliver them
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                foreach (Item I in ItemsOne.ToList())
                {
                    if (I == null)
                        continue;

                    GetTradeUser(oneId).GetClient().GetHabbo().GetInventoryComponent().RemoveItem(I.Id);
                   // Logging.LogException("Aqui bien bro 1-1");

                    dbClient.SetQuery("UPDATE `items` SET `user_id` = @user WHERE `id` = @id LIMIT 1");
                    dbClient.AddParameter("user", twoId);
                    dbClient.AddParameter("id", I.Id);
                    dbClient.RunQuery();

                    GetTradeUser(twoId).GetClient().GetHabbo().GetInventoryComponent().AddNewItem(I.Id, I.BaseItem, I.ExtraData, I.GroupId, false, false, I.LimitedNo, I.LimitedTot);
                   // Logging.LogException("Aqui bien bro 1-2");
                }

                foreach (Item I in ItemsTwo.ToList())
                {
                    if (I == null)
                        continue;

                    GetTradeUser(twoId).GetClient().GetHabbo().GetInventoryComponent().RemoveItem(I.Id);
                   // Logging.LogException("Aqui bien bro 2-1");

                    dbClient.SetQuery("UPDATE `items` SET `user_id` = @user WHERE `id` = @id LIMIT 1");
                    dbClient.AddParameter("user", oneId);
                    dbClient.AddParameter("id", I.Id);
                    dbClient.RunQuery();

                    GetTradeUser(oneId).GetClient().GetHabbo().GetInventoryComponent().AddNewItem(I.Id, I.BaseItem, I.ExtraData, I.GroupId, false, false, I.LimitedNo, I.LimitedTot);
                   // Logging.LogException("Aqui bien bro 2-2");
                }
            }

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("INSERT INTO `logs_client_trade` VALUES(null, @1id, @2id, @1items, @2items, UNIX_TIMESTAMP())");
                dbClient.AddParameter("1id", oneId);
                dbClient.AddParameter("2id", twoId);
                dbClient.AddParameter("1items", User1);
                dbClient.AddParameter("2items", User2);
                dbClient.RunQuery();
            }


            // Update inventories
            GetTradeUser(oneId).GetClient().GetHabbo().GetInventoryComponent().UpdateItems(false);
            GetTradeUser(twoId).GetClient().GetHabbo().GetInventoryComponent().UpdateItems(false);

        }

        /*public void DeliverItemsAsync()
        {
            List<Item> list1 = this.GetTradeUser(this.oneId).OfferedItems;
            List<Item> list2 = this.GetTradeUser(this.twoId).OfferedItems;
            string items1 = "";
            string items2 = "";
            string itemsname1 = ":";
            string itemsname2 = ":";
            foreach (Item userItem in list1.ToList())
            {
                if (this.GetTradeUser(this.oneId).GetClient().GetHabbo().GetInventoryComponent().GetItem(userItem.Id) == null)
                {
                    GetTradeUser(this.oneId).GetClient().SendNotification(PolarEnvironment.GetGame().GetLanguageLocale().TryGetValue("trade_failed"));
                    GetTradeUser(this.twoId).GetClient().SendNotification(PolarEnvironment.GetGame().GetLanguageLocale().TryGetValue("trade_failed"));
                    return;
                }
                items1 = items1 + userItem.Id.ToString() + ";";
                itemsname1 = itemsname1 + userItem.GetBaseItem().ItemName.ToString() + ";";
                Logging.WriteLine("Aqui bien bro 0-1");
            }
            foreach (Item userItem in list2.ToList())
            {
                if (this.GetTradeUser(this.twoId).GetClient().GetHabbo().GetInventoryComponent().GetItem(userItem.Id) == null)
                {
                    GetTradeUser(this.oneId).GetClient().SendNotification(PolarEnvironment.GetGame().GetLanguageLocale().TryGetValue("trade_failed"));
                    GetTradeUser(this.twoId).GetClient().SendNotification(PolarEnvironment.GetGame().GetLanguageLocale().TryGetValue("trade_failed"));
                    return;
                }
                items2 = items2 + userItem.Id.ToString() + ";";
                itemsname2 = itemsname2 + userItem.GetBaseItem().ItemName.ToString() + ";";
                Logging.WriteLine("Aqui bien bro 0-2");
            }
            foreach (Item userItem in list1.ToList())
            {
                GetTradeUser(oneId).GetClient().GetHabbo().GetInventoryComponent().RemoveItem(userItem.Id);
                Logging.WriteLine("Aqui bien bro 1-1");
                //this.GetTradeUser(this.twoId).GetClient().GetHabbo().GetInventoryComponent().TryAddItem(userItem);
                //this.GetTradeUser(this.twoId).GetClient().GetHabbo().GetInventoryComponent().AddItem(userItem);

                // this.GetTradeUser(this.twoId).GetClient().SendMessage(new FurniListAddComposer(userItem));
                // this.GetTradeUser(this.twoId).GetClient().SendMessage(new FurniListNotificationComposer(userItem.Id, 1));

                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                    {
                        dbClient.SetQuery("UPDATE `items` SET `user_id` = @user WHERE `id` = @id LIMIT 1");
                        dbClient.AddParameter("user", twoId);
                        dbClient.AddParameter("id", userItem.Id);
                        dbClient.RunQuery();
                    }

                GetTradeUser(twoId).GetClient().GetHabbo().GetInventoryComponent().AddNewItem(userItem.Id, userItem.BaseItem, userItem.ExtraData, userItem.GroupId, false, false, userItem.LimitedNo, userItem.LimitedTot);
                Logging.WriteLine("Aqui bien bro 1-2");
            }
            foreach (Item userItem in list2.ToList())
            {
                this.GetTradeUser(twoId).GetClient().GetHabbo().GetInventoryComponent().RemoveItem(userItem.Id);
                Logging.WriteLine("Aqui bien bro 2-1");
                // this.GetTradeUser(this.oneId).GetClient().GetHabbo().GetInventoryComponent().TryAddItem(userItem);
                // this.GetTradeUser(this.twoId).GetClient().GetHabbo().GetInventoryComponent().AddItem(userItem);

                //this.GetTradeUser(this.oneId).GetClient().SendMessage(new FurniListAddComposer(userItem));
                //  this.GetTradeUser(this.oneId).GetClient().SendMessage(new FurniListNotificationComposer(userItem.Id, 1));

                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                    {
                        dbClient.SetQuery("UPDATE `items` SET `user_id` = @user WHERE `id` = @id LIMIT 1");
                        dbClient.AddParameter("user", oneId);
                        dbClient.AddParameter("id", userItem.Id);
                        dbClient.RunQuery();
                    }

                GetTradeUser(oneId).GetClient().GetHabbo().GetInventoryComponent().AddNewItem(userItem.Id, userItem.BaseItem, userItem.ExtraData, userItem.GroupId, false, false, userItem.LimitedNo, userItem.LimitedTot);
                Logging.WriteLine("Aqui bien bro 2-2");
            }
            using (IQueryAdapter queryReactor = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                queryReactor.SetQuery("INSERT INTO `logs_client_trade` VALUES(null, @1id, @2id, @1items, @2items, UNIX_TIMESTAMP())");
                queryReactor.AddParameter("1id", this.oneId);
                queryReactor.AddParameter("2id", this.twoId);
                queryReactor.AddParameter("1items", items1);
                queryReactor.AddParameter("2items", items2);
                queryReactor.RunQuery();
            }

            // Update inventories
            GetTradeUser(oneId).GetClient().GetHabbo().GetInventoryComponent().UpdateItems(false);
            GetTradeUser(twoId).GetClient().GetHabbo().GetInventoryComponent().UpdateItems(false);

        }
        */
        public void CloseTradeClean()
        {
            foreach (TradeUser User in this.Users.ToList())
            {
                if (User == null || User.GetRoomUser() == null)
                    continue;

                if (User.GetRoomUser().Statusses.ContainsKey("trd"))
                {
                    User.GetRoomUser().RemoveStatus("trd");
                    User.GetRoomUser().UpdateNeeded = true;
                    User.GetRoomUser().IsTrading = false;
                }
            }

            SendMessageToUsers(new TradingFinishComposer());
            GetRoom().ActiveTrades.Remove(this);
            /*for (int index = 0; index < this.Users.Length; ++index)
            {
                TradeUser tradeUser = this.Users[index];
                if (tradeUser != null && tradeUser.GetRoomUser() != null)
                {
                    tradeUser.GetRoomUser().RemoveStatus("trd");
                    tradeUser.GetRoomUser().UpdateNeeded = true;
                    tradeUser.GetRoomUser().IsTrading = false;
                }
            }
            this.SendMessageToUsers(new ServerPacket(ServerPacketHeader.TradingFinishMessageComposer));
            this.GetRoom().ActiveTrades.Remove(this);*/
        }

        public void CloseTrade(int UserId)
        {
            for (int index = 0; index < this.Users.Length; ++index)
            {
                TradeUser tradeUser = this.Users[index];
                if (tradeUser != null && tradeUser.GetRoomUser() != null)
                {
                    tradeUser.GetRoomUser().RemoveStatus("trd");
                    tradeUser.GetRoomUser().UpdateNeeded = true;
                }
            }
            ServerPacket Message = new ServerPacket(ServerPacketHeader.TradingClosedMessageComposer);
            Message.WriteInteger(UserId);
            Message.WriteInteger(2);
            this.SendMessageToUsers(Message);
        }

        public void SendMessageToUsers(ServerPacket Message)
        {
            if (this.Users == null)
                return;
            for (int index = 0; index < this.Users.Length; ++index)
            {
                TradeUser tradeUser = this.Users[index];
                if (tradeUser != null && tradeUser != null && tradeUser.GetClient() != null)
                    tradeUser.GetClient().SendMessage(Message);
            }
        }

        private Room GetRoom()
        {
            return PolarEnvironment.GetGame().GetRoomManager().GetRoom(this.RoomId);
        }
    }
}