using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.Cache;
using Polar.Communication.Packets.Outgoing.Guides;
using Polar.Communication.Packets.Outgoing.Groups;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Jobs.General
{
    class RenunciarCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_corp_fire"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Renuncia a tu trabajo."; }
        }

        public async Task Execute(GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Variables
            int Bubble = 0;
            #endregion

            #region Conditions


            int JobRank = Session.GetRoleplay().JobRank;
            if (Session.GetRoleplay().JobId == 1)
            {
                Session.SendWhisper("¡No perteneces a ningun grupo de trabajo!", 1);
                return;
            }

            if (Session.GetRoleplay().IsDead)
            {
                Session.SendWhisper("¡No puedes hacer esto mientras estés muerto!", 1);
                return;
            }

            if (Session.GetRoleplay().IsJailed)
            {
                Session.SendWhisper("¡No puedes hacer esto mientras estás encarcelado!", 1);
                return;
            }
            #endregion

            #region Execute
            if (Session.GetRoleplay().IsWorking)
            {
                WorkManager.RemoveWorkerFromList(Session);
                Session.GetRoleplay().IsWorking = false;
                Session.GetHabbo().Poof();

                if (GroupManager.HasJobCommand(Session, "guide"))
                {
                    PolarEnvironment.GetGame().GetGuideManager().RemoveGuide(Session);
                    Session.SendMessage(new HelperToolConfigurationComposer(Session));

                    #region End Existing Calls
                    if (Session.GetRoleplay().GuideOtherUser != null)
                    {
                        Session.GetRoleplay().GuideOtherUser.SendMessage(new OnGuideSessionDetachedComposer(0));
                        Session.GetRoleplay().GuideOtherUser.SendMessage(new OnGuideSessionDetachedComposer(1));
                        if (Session.GetRoleplay().GuideOtherUser.GetRoleplay() != null)
                        {
                            Session.GetRoleplay().GuideOtherUser.GetRoleplay().Sent911Call = false;
                            Session.GetRoleplay().GuideOtherUser.GetRoleplay().GuideOtherUser = null;
                        }

                        Session.GetRoleplay().GuideOtherUser = null;
                        Session.SendMessage(new OnGuideSessionDetachedComposer(0));
                        Session.SendMessage(new OnGuideSessionDetachedComposer(1));
                    }
                    #endregion
                }
            }

            Group OldJob = GroupManager.GetJob(Session.GetRoleplay().JobId);

            Session.GetRoleplay().TimeWorked = 0;
            Session.GetRoleplay().JobId = 1;
            Session.GetRoleplay().JobRank = 1;
            Session.GetRoleplay().JobRequest = 0;

            Group Job = GroupManager.GetJob(Session.GetRoleplay().JobId);
            Job.AddNewMember(Session.GetHabbo().Id);
            Job.SendPackets(Session);
            Session.SendMessage(new GroupInfoComposer(Job, Session));

            Session.Shout("*Has renunciado a la empresa " + OldJob.Name + "*", Bubble);
            #endregion
        }
    }
}