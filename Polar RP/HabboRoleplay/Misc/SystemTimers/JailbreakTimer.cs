using System;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboHotel.Items;
using Polar.Core;
using System.Linq;
using System.Collections.Generic;
using Polar.HabboHotel.Rooms;
using Polar.HabboRoleplay.RPRoom;

namespace Polar.HabboRoleplay.Timers.Types
{
    /// <summary>
    /// Countdown to stop jailbreak
    /// </summary>
    public class JailbreakTimer : SystemRoleplayTimer
    {
        public JailbreakTimer(string Type, int Time, bool Forever, object[] Params) 
            : base(Type, Time, Forever, Params)
        {
            // minutes converted to miliseconds
            int JailbreakTime = Convert.ToInt32(RoleplayData.GetData("jailbreak", "timer"));
            TimeLeft = JailbreakTime * 60000;
            TimeCount = 0;
        }
 
        /// <summary>
        /// Ends the jailbreak
        /// </summary>
        public override void Execute()
        {
            try
            {
                if (!JailbreakManager.JailbreakActivated)
                {
                    base.EndTimer();
                    return;
                }

                string MyCity = "heticosrp";

                HabboRoleplay.RPRoom.RPRoom Data;
                int ToJail = PolarEnvironment.GetGame().GetRPRoomManager().TryToGetJail(MyCity, out Data);
                int ToJailback = PolarEnvironment.GetGame().GetRPRoomManager().TryToGetJailBack(MyCity, out Data);

                List<GameClient> CurrentJailbrokenUsers = PolarEnvironment.GetGame().GetClientManager().GetClients.Where(x => x != null && x.GetHabbo() != null && x.GetRoleplay() != null && x.GetRoleplay().Jailbroken).ToList();
                GameClient UserJailbreaking = JailbreakManager.UserJailbreaking;

                if (CurrentJailbrokenUsers.Count <= 0)
                {
                    JailbreakManager.JailbreakActivated = false;
                    if (JailbreakManager.FenceBroken)
                    {


                        if (RoleplayManager.GenerateRoom(Convert.ToInt32(ToJailback), out Room Room))
                            JailbreakManager.GenerateFence(Room);
                        JailbreakManager.FenceBroken = false;
                    }
                    MessagePoliceOfficers();
                    base.EndTimer();
                    return;
                }

                if (UserJailbreaking != null || UserJailbreaking.GetHabbo().CurrentRoom != null || UserJailbreaking.GetRoomUser() != null)
                {
                    if (UserJailbreaking.GetHabbo().CurrentRoomId != Convert.ToInt32(ToJailback))
                    {
                        JailbreakManager.JailbreakActivated = false;
                        if (JailbreakManager.FenceBroken)
                        {
                            if (RoleplayManager.GenerateRoom(Convert.ToInt32(ToJailback), out Room Room))
                                JailbreakManager.GenerateFence(Room);
                            JailbreakManager.FenceBroken = false;
                        }

                        foreach (GameClient Client in CurrentJailbrokenUsers)
                        {
                            if (Client == null || Client.GetRoleplay() == null || Client.GetHabbo() == null)
                                continue;

                            if (Client.GetRoleplay().Jailbroken && !JailbreakManager.FenceBroken)
                                Client.GetRoleplay().Jailbroken = false;

                            if (Client.GetHabbo().CurrentRoomId == Convert.ToInt32(ToJail))
                            {
                                RoleplayManager.GetLookAndMotto(Client);
                                RoleplayManager.SpawnBeds(Client, "bed_silo_one");
                                Client.SendNotification("¡El proceso de escape se ha detenido, por lo que has vuelto a las camas!");
                            }
                            else
                            {
                                Client.SendNotification("El proceso de jailbreak se ha detenido, por lo que te han enviado de nuevo a la cárcel.!");
                                RoleplayManager.SendUserOld2(Client, Convert.ToInt32(ToJail));
                            }
                        }

                        MessagePoliceOfficers();

                        RoleplayManager.Shout(UserJailbreaking, "*Detiene el escape*", 4);
                        base.EndTimer();
                        return;
                    }
                }

                TimeCount++;
                TimeLeft -= 1000;

                if (TimeLeft > 0)
                {
                    if (TimeCount == 60)
                    {
                        if (UserJailbreaking != null || UserJailbreaking.GetHabbo().CurrentRoom != null || UserJailbreaking.GetRoomUser() != null)
                            RoleplayManager.Shout(UserJailbreaking, "*Se acerca el escape de todos los convictos [" + (TimeLeft / 60000) + " minutos restantes]*", 4);

                        TimeCount = 0;
                    }
                    return;
                }

                foreach (GameClient Client in CurrentJailbrokenUsers)
                {
                    if (Client == null || Client.GetRoleplay() == null || Client.GetHabbo() == null)
                        continue;

                    RoleplayManager.Shout(Client, "*El escape se ha completado*", 4);
                    PolarEnvironment.GetGame().GetAchievementManager().ProgressAchievement(Client, "ACH_Jailbreak", 1);
                    // BattlePass Challenge Integration
                    PolarEnvironment.GetGame().GetBattlePassManager().ProgressChallenge(Client, "jailbreak", 1);
                    Client.GetRoleplay().Jailbroken = false;
                    Client.GetRoleplay().IsWanted = false;
                    Client.GetRoleplay().IsJailed = false;
                    Client.GetHabbo().Poof();
                }

                if (JailbreakManager.FenceBroken)
                {
                    if (RoleplayManager.GenerateRoom(Convert.ToInt32(RoleplayData.GetData("jail", "outsideroomid")), out Room Room))
                        JailbreakManager.GenerateFence(Room);
                    JailbreakManager.FenceBroken = false;
                }
                JailbreakManager.JailbreakActivated = false;
                JailbreakManager.UserJailbreaking = null;
                base.EndTimer();
                return;
            }
            catch (Exception e)
            {
                Logging.LogRPTimersError("Error in Execute() void: " + e);
                base.EndTimer();
            }
        }

        public void MessagePoliceOfficers()
        {
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

                    client.SendWhisper("[RADIO] [ESCAPE] Parece que todos los prisioneros han sido capturados y la cerca ha sido reparada! ¡Buen trabajo a todos!", 30);
                }
            }
        }
    }
}