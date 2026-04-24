using System;

using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Guides;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.Rooms;
using Polar.Communication.Packets.Outgoing.Guides;

namespace Polar.Communication.Packets.Incoming.Guides
{
    internal class GetHelperToolConfigurationMessageEvent : IPacketEvent
    {
        public void Parse(GameClient Session, ClientPacket Packet)
        {
            RoomUser User = Session.GetRoomUser();
            Group Group = GroupManager.GetJob(Session.GetRoleplay().JobId);

            #region Conditions
            if (User == null)
                return;

            if (Group == null)
            {
                Session.SendNotification("¡Sólo la policía puede utilizar esta herramienta!");
                return;
            }

            if (Group.Id <= 0)
            {
                Session.SendNotification("¡Sólo la policía puede utilizar esta herramienta!");
                return;
            }

            if (!GroupManager.HasJobCommand(Session, "guide"))
            {
                Session.SendNotification("¡Sólo la policía puede utilizar esta herramienta!");
                return;
            }

            if (Session.GetRoleplay().IsDead)
            {
                Session.SendMessage(new HelperToolConfigurationComposer(Session));
                Session.SendWhisper("¡No puedes trabajar mientras estás muerto!", 1);
                return;
            }

            if (Session.GetRoleplay().IsJailed)
            {
                Session.SendMessage(new HelperToolConfigurationComposer(Session));
                Session.SendWhisper("¡No puedes trabajar mientras estás encarcelado!", 1);
                return;
            }

            if (Session.GetRoleplay().IsWorkingOut)
            {
                Session.SendMessage(new HelperToolConfigurationComposer(Session));
                Session.SendWhisper("¡No puedes entrenar mientras estás trabajando!", 1);
                return;
            }

            if (Session.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("sendhome"))
            {
                Session.SendMessage(new HelperToolConfigurationComposer(Session));
                Session.SendWhisper("¡No puedes trabajar mientras te mandan a casa!", 1);
                return;
            }

            if (GroupManager.HasJobCommand(Session, "guide") && RoleplayManager.PurgeStarted)
            {
                Session.SendMessage(new HelperToolConfigurationComposer(Session));
                Session.SendWhisper("¡No puedes empezar a trabajar como policía mientras se ha activado una purga!", 1);
                return;
            }

            if (GroupManager.HasJobCommand(Session, "guide") && Session.GetHabbo().CurrentRoom.RoomData.TurfEnabled && !RoleplayManager.StartWorkInPoliceHQ)
            {
                Session.SendMessage(new HelperToolConfigurationComposer(Session));
                Session.SendWhisper("¡No puedes empezar a trabajar como policía mientras estás dentro de un césped!", 1);
                return;
            }

            if (BlackListManager.BlackList.Contains(Session.GetHabbo().Id))
            {
                Session.SendMessage(new HelperToolConfigurationComposer(Session));
                Session.SendWhisper("¡Lo siento, pero ha sido incluido en la lista negra de la corporación policial!", 1);
                return;
            }
            #endregion

            GuideManager guideManager = PolarEnvironment.GetGame().GetGuideManager();
            bool onDuty = Packet.PopBoolean();

            Session.GetRoleplay().HandlingCalls = Packet.PopBoolean();
            Session.GetRoleplay().HandlingJailbreaks = Packet.PopBoolean();
            Session.GetRoleplay().HandlingHeists = Packet.PopBoolean();

            if (onDuty)
            {
                if (Session.GetRoleplay().TryGetCooldown("startwork", true))
                {
                    Session.SendMessage(new HelperToolConfigurationComposer(Session));
                    return;
                }

                if (Session.GetRoleplay().CurEnergy <= 0)
                {
                    Session.SendMessage(new HelperToolConfigurationComposer(Session));
                    Session.SendWhisper("No tienes energía para trabajar", 1);
                    return;
                }

                if (!Session.GetRoleplay().IsWorking)
                {
                    Session.GetRoleplay().IsWorking = true;
                    guideManager.AddGuide(Session);
                    Session.Shout("*Comienza a trabajar como " + GroupManager.GetJob(Session.GetRoleplay().JobId).Name + " " + GroupManager.GetJobRank(Session.GetRoleplay().JobId, Session.GetRoleplay().JobRank).Name + "*", 4);
                    RoleplayManager.GetLookAndMotto(Session);
                    WorkManager.AddWorkerToList(Session);
                    Session.GetRoleplay().TimerManager.CreateTimer("work", 1000, true);
                    Session.GetRoleplay().CooldownManager.CreateCooldown("startwork", 1000, 10);

                    if (Session.GetHabbo().CurrentRoomId != Convert.ToInt32(RoleplayData.GetData("police", "headquartersroomid")))
                    {
                        if (RoleplayManager.StartWorkInPoliceHQ)
                            RoleplayManager.SendUser(Session, Convert.ToInt32(RoleplayData.GetData("police", "headquartersroomid")));
                    }
                }
            }
            else
            {
                if (Session.GetRoleplay().TryGetCooldown("stopwork", true))
                {
                    Session.SendMessage(new HelperToolConfigurationComposer(Session));
                    return;
                }
                if (Session.GetRoleplay().IsWorking)
                {
                    Session.GetRoleplay().IsWorking = false;
                    guideManager.RemoveGuide(Session);
                    Session.Shout("*Deja de trabajar como " + GroupManager.GetJob(Session.GetRoleplay().JobId).Name + " " + GroupManager.GetJobRank(Session.GetRoleplay().JobId, Session.GetRoleplay().JobRank).Name + "*", 4);
                    WorkManager.RemoveWorkerFromList(Session);
                    Session.GetHabbo().Poof();
                    Session.GetRoleplay().CooldownManager.CreateCooldown("stopwork", 1000, 10);
                }
            }
            Session.SendMessage(new HelperToolConfigurationComposer(Session));
        }
    }
}
