using System;
using System.Data;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboRoleplay.Misc
{
    public class JailbreakManager
    {
        /// <summary>
        /// Is the fence broken
        /// </summary>
        public static bool FenceBroken = false;

        /// <summary>
        /// Has a jailbreak been initiated
        /// </summary>
        public static bool JailbreakActivated = false;

        /// <summary>
        /// User trying to free all convicts
        /// </summary>
        public static GameClient UserJailbreaking = null;

        /// <summary>
        /// Begin jailbreak sequence
        /// </summary>
        public static void InitiateJailbreak(GameClient UserJailbreaking)
        {
            #region Message Police Officers
            lock (PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
            {
                foreach (GameClient client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                {
                    if (client == null || client.GetHabbo() == null || client.GetRoleplay() == null)
                        continue;

                    if (!GroupManager.HasJobCommand(client, "radio") && !client.GetHabbo().GetPermissions().HasRight("corporation_rights"))
                        continue;

                    if (GroupManager.HasJobCommand(client, "radio"))
                    {
                        if (!client.GetRoleplay().IsWorking)
                            continue;

                        if (!client.GetRoleplay().HandlingJailbreaks)
                            continue;

                        if (client.GetRoleplay().DisableRadio)
                            continue;
                    }

                    client.SendWhisper("[Alerta RADIO] [JAILBREAK] ¡Advertencia! Ha habido una fuerte explosión procedente de la prisión!", 30);
                }
            }
            #endregion

            List<GameClient> CurrentJailedUsers = PolarEnvironment.GetGame().GetClientManager().GetClients.Where(x => x != null && x.GetHabbo() != null && x.GetRoleplay() != null && x.GetRoleplay().IsJailed && x != RoleplayManager.Defendant).ToList();

            foreach (GameClient Client in CurrentJailedUsers)
            {
                Client.GetRoleplay().Jailbroken = true;
                RoleplayManager.GetLookAndMotto(Client);
            }

            int X = Convert.ToInt32(RoleplayData.GetData("jailbreak", "fencex"));
            int Y = Convert.ToInt32(RoleplayData.GetData("jailbreak", "fencey"));
            string MyCity = "heticosrp";
            HabboRoleplay.RPRoom.RPRoom Data;
            int RoomId = PolarEnvironment.GetGame().GetRPRoomManager().TryToGetJailBack(MyCity, out Data);

            // Old int RoomId = Convert.ToInt32(RoleplayData.GetData("jail", "outsideroomid"));
            if (RoleplayManager.GenerateRoom(RoomId, out Room Room) && Room.GetRoomItemHandler() != null && Room.GetRoomItemHandler().GetFloor != null)
            {
                List<Item> ItemsToRemove = Room.GetRoomItemHandler().GetFloor.Where(x => x.BaseItem == 3011 || x.BaseItem == 6088).ToList();

                if (ItemsToRemove != null && ItemsToRemove.Count > 0)
                {
                    foreach (Item Remove in ItemsToRemove)
                        Room.GetRoomItemHandler().RemoveFurniture(null, Remove.Id);
                }

                Item FenceToRemove = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.BaseItem == 8049 && x.GetX == X && x.GetY == Y);

                if (FenceToRemove != null)
                {
                    Room.GetRoomItemHandler().RemoveFurniture(null, FenceToRemove.Id);
                    JailbreakManager.FenceBroken = true;
                }
            }

            JailbreakManager.UserJailbreaking = UserJailbreaking;
            RoleplayManager.TimerManager.CreateTimer("jailbreak", 1000, false);
        }

        /// <summary>
        /// Generates the fence for jailbreak
        /// </summary>
        /// <param name="Room"></param>
        public static void GenerateFence(Room Room)
        {
            int X = Convert.ToInt32(RoleplayData.GetData("jailbreak", "fencex"));
            int Y = Convert.ToInt32(RoleplayData.GetData("jailbreak", "fencey"));
            int Rot = Convert.ToInt32(RoleplayData.GetData("jailbreak", "fencerotation"));

            if (Room.GetRoomItemHandler().GetFloor.Where(x => x.BaseItem == 8049 && x.GetX == X && x.GetY == Y).ToList().Count <= 0)
            {
                double MaxHeight = 0.0;
                Item ItemInFront;
                if (Room.GetGameMap().GetHighestItemForSquare(new Point(X, Y), out ItemInFront))
                {
                    if (ItemInFront != null)
                        MaxHeight = ItemInFront.TotalHeight;
                }

                RoleplayManager.PlaceItemToRoom(null, 8049, 0, X, Y, MaxHeight, Rot, false, Room.RoomId, false);
            }
        }
    }
}