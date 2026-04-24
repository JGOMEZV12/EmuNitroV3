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
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using Polar.Communication.Packets.Outgoing.Notifications;
using Polar.Communication.Packets.Outgoing.Navigator;
using Polar.HabboRoleplay.Weapons;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Pathfinding;
using Polar.HabboRoleplay.Houses;
using Polar.HabboRoleplay.Timers;

namespace Polar.HabboRoleplay.Misc
{
    public class WorkManager
    {
        /// <summary>
        /// Thread-safe dictionary containing users who are working in the specific corp
        /// </summary>
        public static ConcurrentDictionary<int, List<int>> WorkingUsersList = new ConcurrentDictionary<int, List<int>>();

        /// <summary>
        /// Add new user to the working users list
        /// </summary>
        public static void AddWorkerToList(GameClient Session)
        {
            if (Session.GetRoleplay().JobId <= 1)
                return;

            Group Job = GroupManager.GetJob(Session.GetRoleplay().JobId);

            if (Job == null)
                return;

            if (WorkingUsersList.ContainsKey(Job.Id))
            {
                List<int> Workers = WorkingUsersList[Job.Id];

                if (Workers.Contains(Session.GetHabbo().Id))
                    return;

                Workers.Add(Session.GetHabbo().Id);
                WorkingUsersList.TryUpdate(Job.Id, Workers, WorkingUsersList[Job.Id]);
            }
            else
            {
                List<int> Workers = new List<int>();

                Workers.Add(Session.GetHabbo().Id);
                WorkingUsersList.TryAdd(Job.Id, Workers);
            }

            var CurrentWorkers = WorkingUsersList[Job.Id];
            if (CurrentWorkers != null)
            {
                if (CurrentWorkers.Count > 0)
                {
                    foreach (var Bot in Bots.Manager.RoleplayBotManager.DeployedRoleplayBots.Values)
                    {
                        if (Bot.GetBotRoleplay().AIType == Bots.RoleplayBotAIType.DELIVERY)
                            continue;

                        if (Bot.GetBotRoleplay().RoamBot)
                            continue;

                        if (Bot.GetBotRoleplay().Corporation == Job.Id && Bot.GetBotRoleplayAI().OnDuty)
                            Bot.GetBotRoleplayAI().StopActivities();
                    }
                }
            }
        }

        /// <summary>
        /// Removes a user from the working users list
        /// </summary>
        public static void RemoveWorkerFromList(GameClient Session)
        {
            if (Session.GetRoleplay().JobId <= 1)
                return;

            if (Session.GetRoleplay().WateringCan)
            {
                Session.GetRoleplay().WateringCan = false;
                if (Session.GetRoomUser() != null && Session.GetRoomUser().CurrentEffect == 192)
                    Session.GetRoomUser().ApplyEffect(0);
            }

            Group Job = GroupManager.GetJob(Session.GetRoleplay().JobId);

            if (Job == null)
                return;

            if (!WorkingUsersList.ContainsKey(Job.Id))
                return;

            // Ponemos un cooldown para médicos que quieran abusar de su inmunidad para agredir
            if (Job.Name.Contains("Hospital"))
            {
                Session.GetRoleplay().CooldownManager.CreateCooldown("fist", 1000, 120);
                Session.GetRoleplay().CooldownManager.CreateCooldown("gun", 1000, 120);
            }

            List<int> Workers = WorkingUsersList[Job.Id];

            if (!Workers.Contains(Session.GetHabbo().Id))
                return;

            Workers.Remove(Session.GetHabbo().Id);
            WorkingUsersList.TryUpdate(Job.Id, Workers, WorkingUsersList[Job.Id]);

            var CurrentWorkers = WorkingUsersList[Job.Id];
            if (CurrentWorkers != null)
            {
                if (CurrentWorkers.Count == 0)
                {
                    foreach (var Bot in Bots.Manager.RoleplayBotManager.DeployedRoleplayBots.Values)
                    {
                        if (Bot.GetBotRoleplay().AIType == Bots.RoleplayBotAIType.DELIVERY)
                            continue;

                        if (Bot.GetBotRoleplay().RoamBot)
                            continue;

                        if (Bot.GetBotRoleplay().Corporation == Job.Id && !Bot.GetBotRoleplayAI().OnDuty)
                            Bot.GetBotRoleplayAI().StartActivities();
                    }
                }
            }
        }
    }
}