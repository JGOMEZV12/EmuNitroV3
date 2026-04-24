using Polar.Communication.Packets.Outgoing.Guides;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.RoleplayUsers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Jobs.General
{
    class StartWorkCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_corp_work_start"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Empieza a trabajar."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            Room = PolarEnvironment.GetGame().GetRoomManager().LoadRoom(Session.GetHabbo().CurrentRoomId);
            #region Conditions
            if (Session.GetRoleplay().IsWorking)
            {
                Session.SendWhisper("Usted ya está trabajando", 1);
                return;
            }

            if (Session.GetRoleplay().IsDead)
            {
                Session.SendWhisper("¡No puedes trabajar mientras estés muerto!", 1);
                return;
            }

            if (Session.GetRoleplay().IsJailed)
            {
                Session.SendWhisper("¡No puedes trabajar mientras estés preso!", 1);
                return;
            }

            if (Session.GetRoleplay().StaffOnDuty)
            {
                Session.SendWhisper("No puedes trabajar si tienes el trabajo staff activo ¡QUITALO!", 1);
                return;
            }

            if (Session.GetRoleplay().JobId == 1)
            {
                Session.SendWhisper("¡No puedes trabajar mientras no tengas empleo! BUSCA UNO VAGO", 1);
                return;
            }


            if (Session.GetRoleplay().TexasHoldEmPlayer > 0)
            {
                Session.SendWhisper("No se puede trabajar en medio de un Texas Hold 'Em juego", 1);
                return;
            }

            if (Session.GetRoleplay().Animo <= 10)
            {
                Session.SendWhisper("No se puede trabajar triste, porfavor juega playstation3 o ve al puticlub", 1);
                return;
            }

            if (Session.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("sendhome"))
            {
                Session.SendWhisper("¡No puedes trabajar mientras te mandan a casa!", 1);
                return;
            }

            if (!GroupManager.JobExists(Session.GetRoleplay().JobId, Session.GetRoleplay().JobRank))
            {
                Session.GetRoleplay().TimeWorked = 0;
                Session.GetRoleplay().JobId = 1;
                Session.GetRoleplay().JobRank = 1;
                Session.GetRoleplay().JobRequest = 0;

                Group NewJob = GroupManager.GetJob(Session.GetRoleplay().JobId);
                NewJob.AddNewMember(Session.GetHabbo().Id);
                NewJob.SendPackets(Session);

                Session.SendWhisper("Lo siento, ¡tu trabajo no existe! Su trabajo se ha eliminado.", 1);
                return;
            }

            Group Job = GroupManager.GetJob(Session.GetRoleplay().JobId);
            GroupRank Rank = GroupManager.GetJobRank(Job.Id, Session.GetRoleplay().JobRank);

            if (!Rank.CanWorkHere(Room.Id))
            {
                Session.SendWhisper("¿Esta no es una de sus salas de trabajo! Sólo puede trabajar en RoomID(s): " + String.Join(",", Rank.WorkRooms) + ".", 1);
                return;
            }

            if (Session.GetRoleplay().CurEnergy <= 0)
            {
                Session.SendWhisper("¡No tienes suficiente energía para trabajar!", 1);
                return;
            }

            if (Session.GetRoleplay().Hygiene <= 10)
            {
                Session.SendWhisper("¡No puedes trabajar sucio y oliendo mal, ve a bañarte!  ASQUEROS@", 1);
                return;
            }

            if (Job.Name.Contains("Policia") && Session.GetRoleplay().PassiveMode)
            {
                Session.SendWhisper("No puedes comenzar a trabajar de policía en modo pasivo.", 1);
                return;
            }
            if (GroupManager.HasJobCommand(Session, "guide") && RoleplayManager.PurgeStarted)
            {
                Session.SendWhisper("¡No puede empezar a trabajar como policía mientras se ha activado una purga!", 1);
                return;
            }

            if (Session.GetRoleplay().StaffOnDuty)
            {
                Session.SendWhisper("No puedes trabajar si tienes el trabajo staff activo ¡QUITALO!", 1);
                return;
            }

            if (Session.GetRoleplay().AmbassadorOnDuty)
            {
                Session.SendWhisper("No puedes trabajar si tienes el trabajo de embajador activo ¡QUITALO!", 1);
                return;
            }

            if (Session.GetRoleplay().TryGetCooldown("startwork", true))
                return;

            #endregion

            #region Execute
          
            #region Farming Level Check
            int JobRank = 1;
            if (GroupManager.HasJobCommand(Session, "farming"))
            {
                if (!Session.GetRoleplay().FarmingStats.HasPlantSatchel && !Session.GetRoleplay().FarmingStats.HasSeedSatchel)
                {
                    Session.SendWhisper("¡Los agricultores necesitan una mochila de plantas y una bolsa de semillas antes de que puedan empezar a trabajar! Coge uno en el supermercado.", 1);
                    return;
                }

                if (Session.GetRoleplay().FarmingStats.Level < 6)
                    JobRank = 1;
                else if (Session.GetRoleplay().FarmingStats.Level >= 6 && Session.GetRoleplay().FarmingStats.Level < 11)
                    JobRank = 2;
                else if (Session.GetRoleplay().FarmingStats.Level >= 11)
                    JobRank = 3;

                if (JobRank != Session.GetRoleplay().JobRank)
                {
                    Session.GetRoleplay().JobRank = JobRank;
                    Job.UpdateJobMember(Session.GetHabbo().Id);
                }
            }
            #endregion
           

                Session.GetRoleplay().IsWorking = true;
            RoleplayManager.GetLookAndMotto(Session);
            WorkManager.AddWorkerToList(Session);
            if (Job.RoomId == Room.Id)
                Session.Shout("*Comienza a trabajar en " + Room.Name + " como " + Rank.Name + "*", 4);
            else
                Session.Shout("*Comienza a trabajar como " + Rank.Name + "*", 4);
            Session.GetRoleplay().TimerManager.CreateTimer("work", 1000, true);
            Session.GetRoleplay().CooldownManager.CreateCooldown("startwork", 1000, 10);
            return;
            #endregion
        }
    }
}