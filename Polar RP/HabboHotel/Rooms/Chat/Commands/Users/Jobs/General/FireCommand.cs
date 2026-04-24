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
using System.Data;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Jobs.General
{
    class FireCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_corp_fire"; }
        }

        public string Parameters
        {
            get { return "%user%"; }
        }

        public string Description
        {
            get { return "Fires uno de los trabajadores de su corporación."; }
        }

        public async Task Execute(GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Variables
            int Bubble = 0;
            #endregion

            #region Conditions
            if (Params.Length == 1)
            {
                Session.SendWhisper("Vaya, se le olvidó ingresar un nombre de usuario", 1);
                return;
            }

            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);
            if (TargetClient == null)
            {
                if (!GroupManager.HasJobCommand(Session, "fire"))
                {
                    Session.SendWhisper("Usted no es un rango lo suficientemente alto en su corporación para usar este comando", 1);
                    return;
                }

                int Id = PolarEnvironment.GetGame().GetClientManager().GetIdByName(Params[1]);
                if (Id != 0)
                {
                    using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                    {
                        dbClient.SetQuery("UPDATE `rp_stats` SET `job_id` = '1', `job_rank` = '1', `time_worked` = 0 WHERE `id` = '" + Id + "'");
                        dbClient.RunQuery();
                    }

                    Session.Shout("*Despide " + Params[1] + " de la empresa*", 4);
                    PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_group", "open");
                }
                else
                {
                    Session.SendWhisper("¡No existe ese usuario!", 1);
                    return;
                }
            }
            else
            {

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

                if (!Session.GetHabbo().GetPermissions().HasRight("corporation_rights"))
                {
                    if (!GroupManager.HasJobCommand(Session, "fire"))
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

                if (TargetClient == Session)
                {
                    Session.SendWhisper("¡No puedes despedirte!", 1);
                    return;
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

                Group OldJob = GroupManager.GetJob(TargetClient.GetRoleplay().JobId);

                TargetClient.GetRoleplay().TimeWorked = 0;
                TargetClient.GetRoleplay().JobId = 1;
                TargetClient.GetRoleplay().JobRank = 1;
                TargetClient.GetRoleplay().JobRequest = 0;

                Group Job = GroupManager.GetJob(TargetClient.GetRoleplay().JobId);
                Job.AddNewMember(TargetClient.GetHabbo().Id);
                Job.SendPackets(TargetClient);
                Session.SendMessage(new GroupInfoComposer(Job, Session));

                Session.Shout("*Despide " + TargetClient.GetHabbo().Username + " de la " + OldJob.Name + " empresa*", Bubble);
                TargetClient.SendWhisper("Usted ha sido despedido de la " + OldJob.Name + " empresa a: " + Session.GetHabbo().Username + "!", 1);
                #endregion
            }
        }
    }
}