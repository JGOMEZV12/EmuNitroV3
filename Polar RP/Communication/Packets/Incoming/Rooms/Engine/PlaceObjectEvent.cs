using System;
using System.Linq;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Users;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using Polar.HabboHotel.Items.Data.Moodlight;
using Polar.HabboHotel.Items.Data.Toner;
using Polar.Database.Interfaces;

namespace Polar.Communication.Packets.Incoming.Rooms.Engine
{
    internal class PlaceObjectEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient session, ClientPacket packet)
        {
            if (session == null || session.GetHabbo() == null || !session.GetHabbo().InRoom)
                return;

            Room Room = null;
            if (!PolarEnvironment.GetGame().GetRoomManager().TryGetRoom(session.GetHabbo().CurrentRoomId, out Room))
                return;

            string[] strArray = packet.PopString().Split(' ');

            bool HasRights = false;
            if (Room.CheckRights(session, false, true))
                HasRights = true;

            /*
            var RentableItems = Room.GetRoomItemHandler().GetFloor.Where(x => x.GetBaseItem().InteractionType == InteractionType.RENTABLE_SPACE).ToList();
            Item SpaceItem = null;

            if (RentableItems.Count > 0)
            {
                foreach (var RentableSpace in RentableItems)
                {
                    var Spacedata = RentableSpace.RentableSpaceData;

                    if (Spacedata == null)
                        continue;

                    if (Spacedata.FarmingSpace != null)
                        continue;

                    if (Spacedata.OwnerId != session.GetHabbo().Id)
                        continue;

                    HasRights = true;
                    SpaceItem = RentableSpace;
                    break;
                }
            }*/

            // New Poner items en área Terreno
            bool MyTerrain = false;
            if (Room.CheckTerrain(session, strArray))
                MyTerrain = true;


            if (!HasRights)
            {

                session.SendMessage(new RoomNotificationComposer("furni_placement_error", "message", "${room.error.cant_set_not_owner}"));
                return;
            }

            
            int result1 = 0;
            if (!int.TryParse(strArray[0], out result1) || result1 <= 0)
                return;
            Item Item = session.GetHabbo().GetInventoryComponent().GetItem(result1);
            if (Item == null)
                return;

            if (Item.GetBaseItem().InteractionType == InteractionType.HOUSE_SIGN)
            {
                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("UPDATE `rp_houses` SET `room_id` = '" + Room.Id + "' WHERE `sign_id` = '" + Item.Id + "' LIMIT 1");
                }
                PolarEnvironment.GetGame().GetHouseManager().Init();
            }

            if (Room.GetRoomItemHandler().GetWallAndFloor.Count() > PolarStaticGameSettings.RoomFurnitureLimit)
            {
                session.SendNotification("No puedes tener más de " + PolarStaticGameSettings.RoomFurnitureLimit + " furnis en una sala");
                return;
            }
            else if (Item.GetBaseItem().ItemName.ToLower().Contains("cf") && Room.OwnerId != session.GetHabbo().Id && !session.GetHabbo().GetPermissions().HasRight("room_item_place_exchange_anywhere"))
            {
                session.SendNotification("¡No puedes colocar artículos de cambio en las habitaciones de otras personas!");
                return;
            }

            //TODO: Make neat.
            switch (Item.GetBaseItem().InteractionType)
            {
                #region Interaction Types
                case InteractionType.MOODLIGHT:
                    {
                        MoodlightData moodData = Room.MoodlightData;
                        if (moodData != null && Room.GetRoomItemHandler().GetItem(moodData.ItemId) != null)
                        {
                            session.SendNotification("¡Solo puede tener una luz de fondo de fondo por habitación!");
                            return;
                        }
                        break;
                    }
                case InteractionType.TONER:
                    {
                        TonerData tonerdata = Room.TonerData;
                        if (tonerdata != null && Room.GetRoomItemHandler().GetItem(tonerdata.ItemId) != null)
                        {
                            session.SendNotification("¡Solo puede tener un tóner de fondo por habitación!");
                            return;
                        }
                        break;
                    }
                case InteractionType.HOPPER:
                    {
                        if (Room.GetRoomItemHandler().HopperCount > 0)
                        {
                            session.SendNotification("¡Solo puedes tener una tolva por habitación!");
                            return;
                        }
                        break;
                    }
                case InteractionType.JUKEBOX:
                    {
                        //var trax = Room.GetTraxManager();
                        if (Room.GetRoomItemHandler().JukeboxCount > 0)
                        {
                            session.SendNotification("Solo puedes tener 1 jukebox por sala!");
                            return;
                        }
                        break;
                    }

                case InteractionType.TENT:
                case InteractionType.TENT_SMALL:
                    {
                        Room.AddTent(Item.Id);
                        break;
                    }
                #endregion
            }

            if (!Item.IsWallItem)
            {

                int result2;
                int result3;
                int result4;
                if (strArray.Length < 4 || !int.TryParse(strArray[1], out result2) || !int.TryParse(strArray[2], out result3) || !int.TryParse(strArray[3], out result4))
                    return;

                Item RoomItem2 = new Item(Item.Id, Room.RoomId, Item.BaseItem, Item.ExtraData, result2, result3, 0, result4, session.GetHabbo().Id, Item.GroupId, Item.LimitedNo, Item.LimitedTot, string.Empty, Room);
                if (Room.GetRoomItemHandler().SetFloorItem(session, RoomItem2, result2, result3, result4, true, false, true, false, true))
                {
                    using (IQueryAdapter queryReactor = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                        queryReactor.RunQuery("UPDATE items SET room_id = " + Room.Id.ToString() + ", user_id = " + RoomItem2.UserID.ToString() + " WHERE id = " + result1.ToString());
                    session.GetHabbo().GetInventoryComponent().RemoveItem(result1);

                    if (RoomItem2.IsWired)
                    {
                        try { Room.GetWired().LoadWiredBox(RoomItem2); }
                        catch { Console.WriteLine(Item.GetBaseItem().InteractionType); }
                    }

                    if (!Item.GetBaseItem().IsSeat)
                    {
                        foreach (var square in Item.GetAffectedTiles)
                        {
                            if (Room.GetGameMap().SquareHasUsers(square.X, square.Y))
                            {
                                foreach (var user in Room.GetGameMap().GetRoomUsers(square))
                                {
                                    if (!user.IsWalking)
                                        user.SetPos(user.Coordinate.X, user.Coordinate.Y, Room.GetGameMap().GetHeightForSquare(square));
                                }
                            }
                        }
                    }
                }
                else
                {
                    session.SendMessage(new RoomNotificationComposer("furni_placement_error", "message", "${room.error.cant_set_item}"));
                }
            }
            else
            {
                if (!Item.IsWallItem)
                    return;
                string[] data = new string[strArray.Length - 1];
                for (int index = 1; index < strArray.Length; ++index)
                    data[index - 1] = strArray[index];
                string position = string.Empty;
                if (PlaceObjectEvent.TrySetWallItem2(session.GetHabbo(), Item, data, out position))
                {
                    try
                    {
                        Item RoomItem3 = new Item(Item.Id, Room.RoomId, Item.BaseItem, Item.ExtraData, 0, 0, 0, 0, session.GetHabbo().Id, Item.GroupId, Item.LimitedNo, Item.LimitedTot, position, Room);

                        if (Room.GetRoomItemHandler().SetWallItem(session, RoomItem3))
                        {
                            using (IQueryAdapter queryReactor = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                                queryReactor.RunQuery("UPDATE items SET room_id = " + Room.Id.ToString() + ", user_id = " + RoomItem3.UserID.ToString() + " WHERE id = " + result1.ToString());
                            session.GetHabbo().GetInventoryComponent().RemoveItem(result1);
                            if (session.GetHabbo().Id == Room.OwnerId)
                                PolarEnvironment.GetGame().GetAchievementManager().ProgressAchievement(session, "ACH_RoomDecoFurniCount", 1, false);
                        }
                    }
                    catch
                    {
                        session.SendMessage(new RoomNotificationComposer("furni_placement_error", "message", "${room.error.cant_set_item}"));
                    }
                }
                else
                {
                    session.SendMessage(new RoomNotificationComposer("furni_placement_error", "message", "${room.error.cant_set_item}"));
                }
            }
        }

        private static bool TrySetWallItem(string[] data, out string position)
        {
            if (data.Length != 3 || !data[0].StartsWith(":w=") || !data[1].StartsWith("l=") || data[2] != "r" && data[2] != "l")
            {
                position = (string)null;
                return false;
            }
            string str1 = data[0].Substring(3, data[0].Length - 3);
            string str2 = data[1].Substring(2, data[1].Length - 2);
            if (!str1.Contains(",") || !str2.Contains(","))
            {
                position = (string)null;
                return false;
            }
            int result1 = 0;
            int result2 = 0;
            int result3 = 0;
            int result4 = 0;
            int.TryParse(str1.Split(',')[0], out result1);
            int.TryParse(str1.Split(',')[1], out result2);
            int.TryParse(str2.Split(',')[0], out result3);
            int.TryParse(str2.Split(',')[1], out result4);
            string wallPosition = ":w=" + result1.ToString() + "," + result2.ToString() + " l=" + result3.ToString() + "," + result4.ToString() + " " + data[2];
            position = PlaceObjectEvent.WallPositionCheck(wallPosition);
            return position != null;
        }


        private static bool TrySetWallItem2(
          Habbo Habbo,
          Item item,
          string[] data,
          out string position)
        {
            string str1 = data[0].Substring(3, data[0].Length - 3);
            string str2 = data[1].Substring(2, data[1].Length - 2);
            if (!str1.Contains(",") || !str2.Contains(","))
            {
                position = (string)null;
                return false;
            }
            int result1 = 0;
            int result2 = 0;
            int result3 = 0;
            int result4 = 0;
            int.TryParse(str1.Split(',')[0], out result1);
            int.TryParse(str1.Split(',')[1], out result2);
            int.TryParse(str2.Split(',')[0], out result3);
            int.TryParse(str2.Split(',')[1], out result4);
            string wallPosition = ":w=" + result1.ToString() + "," + result2.ToString() + " l=" + result3.ToString() + "," + result4.ToString() + " " + data[2];
            position = PlaceObjectEvent.WallPositionCheck(wallPosition);
            return position != null;
        }

        public static string WallPositionCheck(string wallPosition)
        {
            try
            {
                if (wallPosition.Contains<char>(Convert.ToChar(13)) || wallPosition.Contains<char>(Convert.ToChar(9)))
                    return (string)null;
                string[] strArray1 = wallPosition.Split(' ');
                if (strArray1[2] != "l" && strArray1[2] != "r")
                    return (string)null;
                string[] strArray2 = strArray1[0].Substring(3).Split(',');
                int num1 = int.Parse(strArray2[0]);
                int num2 = int.Parse(strArray2[1]);
                if (num1 < -1000 || num2 < -1 || num1 > 700 || num2 > 700)
                    return (string)null;
                string[] strArray3 = strArray1[1].Substring(2).Split(',');
                int num3 = int.Parse(strArray3[0]);
                int num4 = int.Parse(strArray3[1]);
                if (num3 < -1 || num4 < -1000 || num3 > 700 || num4 > 700)
                    return (string)null;
                return ":w=" + num1.ToString() + "," + num2.ToString() + " l=" + num3.ToString() + "," + num4.ToString() + " " + strArray1[2];
            }
            catch
            {
                return (string)null;
            }
        }
    }
}