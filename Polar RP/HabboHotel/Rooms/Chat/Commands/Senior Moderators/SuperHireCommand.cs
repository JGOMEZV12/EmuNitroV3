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
using Polar.Communication.Packets.Outgoing.Groups;
using Polar.Communication.Packets.Outgoing.Users;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.Communication.Packets.Outgoing.Guides;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Moderators.Seniors
{
    class SuperHireCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_superhire"; }
        }

        public string Parameters
        {
            get { return "%user% %jobid% %jobrank%"; }
        }

        public string Description
        {
            get { return "Darle trabajo a un usuario con mis superpoderes"; }
        }

        public async Task Execute(GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            if (Params.Length < 4)
            {
                Session.SendWhisper("el comando es ':dartrabajo (user) (job) (rank)'!", 1);
                return;
            }

            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);
            if (TargetClient == null)
            {
                Session.SendWhisper("Se ha producido un error al intentar encontrar a ese usuario, tal vez estén sin conexión.", 1);
                return;
            }

            int jobId;
            if (!int.TryParse(Params[2], out jobId))
            {
                Session.SendWhisper("coloque un numero", 1);
                return;
            }

            int jobRank;
            if (!int.TryParse(Params[3], out jobRank))
            {
                Session.SendWhisper("coloque en rank del trabajo", 1);
                return;
            }

            if (!GroupManager.JobExists(jobId, jobRank))
            {
                Session.SendWhisper("¡Este no es un trabajo válido!", 1);
                return;
            }

            if (TargetClient.GetRoleplay().JobId == jobId && TargetClient.GetRoleplay().JobRank == jobRank)
            {
                Session.SendWhisper("Este ciudadano ya tiene este id de trabajo y rango de trabajo!", 1);
                return;
            }

            var Job = GroupManager.GetJob(jobId);
            var JobRank = GroupManager.GetJobRank(jobId, jobRank);

            if (JobRank.HasCommand("guide"))
            {
                if (BlackListManager.BlackList.Contains(TargetClient.GetHabbo().Id))
                {
                    Session.SendWhisper("Lo sentimos, pero este usuario ha sido puesto en la lista negra de unirse a la corporación de la policía!", 1);
                    return;
                }
            }
            #endregion

            #region Execute

            if (TargetClient.GetRoleplay().IsWorking)
            {
                WorkManager.RemoveWorkerFromList(TargetClient);
                TargetClient.GetRoleplay().IsWorking = false;
                TargetClient.GetHabbo().Poof();

                if (GroupManager.HasJobCommand(TargetClient, "guide"))
                {
                    PolarEnvironment.GetGame().GetGuideManager().RemoveGuide(TargetClient);
                    //TargetClient.SendMessage(new HelperToolConfigurationComposer(TargetClient));

                    #region End Existing Calls
                    if (TargetClient.GetRoleplay().GuideOtherUser != null)
                    {
                        //TargetClient.GetRoleplay().GuideOtherUser.SendMessage(new OnGuideSessionDetachedComposer(0));
                        //TargetClient.GetRoleplay().GuideOtherUser.SendMessage(new OnGuideSessionDetachedComposer(1));
                        if (TargetClient.GetRoleplay().GuideOtherUser.GetRoleplay() != null)
                        {
                            TargetClient.GetRoleplay().GuideOtherUser.GetRoleplay().Sent911Call = false;
                            TargetClient.GetRoleplay().GuideOtherUser.GetRoleplay().GuideOtherUser = null;
                        }

                        TargetClient.GetRoleplay().GuideOtherUser = null;
                        //TargetClient.SendMessage(new OnGuideSessionDetachedComposer(0));
                        //TargetClient.SendMessage(new OnGuideSessionDetachedComposer(1));
                    }
                    #endregion

                }
            }

            int OriginalJob = TargetClient.GetRoleplay().JobId;
            var OldJob = GroupManager.GetJob(OriginalJob);

            TargetClient.GetRoleplay().TimeWorked = 0;
            TargetClient.GetRoleplay().JobId = jobId;
            TargetClient.GetRoleplay().JobRank = jobRank;
            TargetClient.GetRoleplay().JobRequest = 0;

            if (Job.Id == OriginalJob)
                Job.UpdateJobMember(TargetClient.GetHabbo().Id);
            else
                Job.AddNewMember(TargetClient.GetHabbo().Id, jobRank);

            //Job.SendPackets(TargetClient);
            //Session.SendMessage(new GroupInfoComposer(OldJob, Session));

            Session.Shout("*Utiliza sus poderes de dios para dar trabajo a " + TargetClient.GetHabbo().Username + " en '" + Job.Name + "' como '" + JobRank.Name + "'*", 23);
            TargetClient.SendWhisper("Te han dado un trabajo " + Session.GetHabbo().Username + " en '" + Job.Name + "' como '" + JobRank.Name + "'!", 1);
            return;
            #endregion
        }
    }
}