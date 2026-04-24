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
using Polar.Communication.Packets.Outgoing.Guides;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Jobs.General
{
    class DemoteCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_corp_demote"; }
        }

        public string Parameters
        {
            get { return "%user%"; }
        }

        public string Description
        {
            get { return "Degrada uno de los trabajadores de su corporación."; }
        }

        public async Task Execute(GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Variables
            int Bubble = 0;
            #endregion

            #region Conditions
            if (Params.Length == 1)
            {
                Session.SendWhisper("Vaya, te olvidaste de introducir un nombre de usuario", 1);
                return;
            }

            if (Session.GetRoleplay().JobId <= 0 && !Session.GetHabbo().GetPermissions().HasRight("corporation_rights"))
            {
                Session.SendWhisper("¡Ni siquiera eres parte de una corporación!", 1);
                return;
            }

            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);
            if (TargetClient == null)
            {
                Session.SendWhisper("Se ha producido un error al intentar encontrar a ese usuario, tal vez estén sin conexión.", 1);
                return;
            }

            int JobRank = TargetClient.GetRoleplay().JobRank;

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

            if (Session.GetHabbo().GetPermissions().HasRight("corporation_rights"))
            {
                if (!GroupManager.HasJobCommand(Session, "demote"))
                {
                    Session.SendWhisper("Usted no es un rango lo suficientemente alto en su corporación para usar este comando", 1);
                    return;
                }

                if (Session.GetRoleplay().JobId != TargetClient.GetRoleplay().JobId)
                {
                    Session.SendWhisper("¡Este ciudadano no trabaja para usted!", 1);
                    return;
                }

                Bubble = 4;
            }

            if (Session.GetHabbo().GetPermissions().HasRight("corporation_rights"))
                Bubble = 23;

            /*if (!GroupManager.JobExists(TargetClient.GetRoleplay().JobId, (JobRank - 1)))
            {
                Session.SendWhisper("¡Ya no puedes degradar a tu trabajador!", 1);
                return;
            }*/
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
                    TargetClient.SendMessage(new HelperToolConfigurationComposer(TargetClient));

                    #region End Existing Calls
                    if (TargetClient.GetRoleplay().GuideOtherUser != null)
                    {
                        TargetClient.GetRoleplay().GuideOtherUser.SendMessage(new OnGuideSessionDetachedComposer(0));
                        TargetClient.GetRoleplay().GuideOtherUser.SendMessage(new OnGuideSessionDetachedComposer(1));
                        if (TargetClient.GetRoleplay().GuideOtherUser.GetRoleplay() != null)
                        {
                            TargetClient.GetRoleplay().GuideOtherUser.GetRoleplay().Sent911Call = false;
                            TargetClient.GetRoleplay().GuideOtherUser.GetRoleplay().GuideOtherUser = null;
                        }

                        TargetClient.GetRoleplay().GuideOtherUser = null;
                        TargetClient.SendMessage(new OnGuideSessionDetachedComposer(0));
                        TargetClient.SendMessage(new OnGuideSessionDetachedComposer(1));
                    }
                    #endregion
                }
            }

            TargetClient.GetRoleplay().JobRank--;

            Group Job = GroupManager.GetJob(TargetClient.GetRoleplay().JobId);
            GroupRank Rank = GroupManager.GetJobRank(TargetClient.GetRoleplay().JobId, TargetClient.GetRoleplay().JobRank);

            Job.UpdateJobMember(TargetClient.GetHabbo().Id);
            Session.Shout("*Degrada a " + TargetClient.GetHabbo().Username + " a " + Job.Name + " " + Rank.Name + "*", Bubble);
            #endregion
        }
    }
}