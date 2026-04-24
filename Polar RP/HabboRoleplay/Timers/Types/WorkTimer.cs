using System;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.Guides;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.Communication.Packets.Outgoing.Guides;
using Polar.HabboHotel.Quests;
using Polar.Core;

namespace Polar.HabboRoleplay.Timers.Types
{
    /// <summary>
    /// Work timer
    /// </summary>
    public class WorkTimer : RoleplayTimer
    {
        public WorkTimer(string Type, GameClient Client, int Time, bool Forever, object[] Params)
            : base(Type, Client, Time, Forever, Params)
        {
            TimeCount = 0;
            TimeCount2 = 0;

            // Convert to milliseconds
            double TimeDivisible = Math.Floor((double)base.Client.GetRoleplay().TimeWorked / 5);
            int TimeRemaining = base.Client.GetRoleplay().TimeWorked - Convert.ToInt32(TimeDivisible) * 5;
            TimeLeft = (5 - TimeRemaining) * 60000;
            OriginalTime = 5;

            Client.GetRoleplay().UpdateTimerDialogue("Work-Timer", "add", (TimeLeft / 60000), OriginalTime);

            base.Client.SendWhisper("Tienes " + (TimeLeft / 60000) + " Minuto(s) restante hasta que reciba su cheque de pago", 1);
        }

        /// <summary>
        /// Pays user after shift
        /// </summary>
        public override void Execute()
        {
            try
            {
                if (base.Client == null || base.Client.GetHabbo() == null || base.Client.GetRoleplay() == null)
                {
                    base.EndTimer();
                    return;
                }

                GuideManager guideManager = PolarEnvironment.GetGame().GetGuideManager();
                GroupRank JobRank = GroupManager.GetJobRank(base.Client.GetRoleplay().JobId, base.Client.GetRoleplay().JobRank);

                if (!base.Client.GetRoleplay().IsWorking)
                {
                    Client.GetRoleplay().UpdateTimerDialogue("Work-Timer", "remove", (TimeLeft / 60000), OriginalTime);

                    WorkManager.RemoveWorkerFromList(base.Client);
                    base.Client.GetRoleplay().IsWorking = false;
                    base.Client.GetHabbo().Poof();

                    if (GroupManager.HasJobCommand(base.Client, "guide"))
                    {
                        guideManager.RemoveGuide(base.Client);
                        base.Client.SendMessage(new HelperToolConfigurationComposer(base.Client));

                        #region End Existing Calls
                        if (base.Client.GetRoleplay().GuideOtherUser != null)
                        {
                            base.Client.GetRoleplay().GuideOtherUser.SendMessage(new OnGuideSessionDetachedComposer(0));
                            base.Client.GetRoleplay().GuideOtherUser.SendMessage(new OnGuideSessionDetachedComposer(1));
                            if (base.Client.GetRoleplay().GuideOtherUser.GetRoleplay() != null)
                            {
                                base.Client.GetRoleplay().GuideOtherUser.GetRoleplay().Sent911Call = false;
                                base.Client.GetRoleplay().GuideOtherUser.GetRoleplay().GuideOtherUser = null;
                            }

                            base.Client.GetRoleplay().GuideOtherUser = null;
                            base.Client.SendMessage(new OnGuideSessionDetachedComposer(0));
                            base.Client.SendMessage(new OnGuideSessionDetachedComposer(1));
                        }
                        #endregion
                    }
                    base.EndTimer();
                    return;
                }

                if (base.Client.GetRoleplay().CurEnergy <= 0)
                {
                    Client.GetRoleplay().UpdateTimerDialogue("Work-Timer", "remove", (TimeLeft / 120000), OriginalTime);

                    RoleplayManager.Shout(base.Client, "*Deja de funcionar ya que se han quedado sin energía, ve a tomar un café o pop*", 4);

                    WorkManager.RemoveWorkerFromList(base.Client);
                    base.Client.GetRoleplay().IsWorking = false;
                    base.Client.GetHabbo().Poof();

                    if (GroupManager.HasJobCommand(base.Client, "guide"))
                    {
                        guideManager.RemoveGuide(base.Client);
                        base.Client.SendMessage(new HelperToolConfigurationComposer(base.Client));

                        #region End Existing Calls
                        if (base.Client.GetRoleplay().GuideOtherUser != null)
                        {
                            base.Client.GetRoleplay().GuideOtherUser.SendMessage(new OnGuideSessionDetachedComposer(0));
                            base.Client.GetRoleplay().GuideOtherUser.SendMessage(new OnGuideSessionDetachedComposer(1));
                            if (base.Client.GetRoleplay().GuideOtherUser.GetRoleplay() != null)
                            {
                                base.Client.GetRoleplay().GuideOtherUser.GetRoleplay().Sent911Call = false;
                                base.Client.GetRoleplay().GuideOtherUser.GetRoleplay().GuideOtherUser = null;
                            }

                            base.Client.GetRoleplay().GuideOtherUser = null;
                            base.Client.SendMessage(new OnGuideSessionDetachedComposer(0));
                            base.Client.SendMessage(new OnGuideSessionDetachedComposer(1));
                        }
                        #endregion
                    }

                    base.EndTimer();
                    return;
                }



                if (RoleplayManager.PurgeStarted)
                {
                    Client.GetRoleplay().UpdateTimerDialogue("Work-Timer", "remove", (TimeLeft / 120000), OriginalTime);

                    RoleplayManager.Shout(base.Client, "*Deja de trabajar, ya que ha empezado el modo purga*", 4);

                    WorkManager.RemoveWorkerFromList(base.Client);
                    base.Client.GetRoleplay().IsWorking = false;
                    base.Client.GetHabbo().Poof();

                    if (GroupManager.HasJobCommand(base.Client, "guide"))
                    {
                        guideManager.RemoveGuide(base.Client);
                        base.Client.SendMessage(new HelperToolConfigurationComposer(base.Client));

                        #region End Existing Calls
                        if (base.Client.GetRoleplay().GuideOtherUser != null)
                        {
                            base.Client.GetRoleplay().GuideOtherUser.SendMessage(new OnGuideSessionDetachedComposer(0));
                            base.Client.GetRoleplay().GuideOtherUser.SendMessage(new OnGuideSessionDetachedComposer(1));
                            if (base.Client.GetRoleplay().GuideOtherUser.GetRoleplay() != null)
                            {
                                base.Client.GetRoleplay().GuideOtherUser.GetRoleplay().Sent911Call = false;
                                base.Client.GetRoleplay().GuideOtherUser.GetRoleplay().GuideOtherUser = null;
                            }

                            base.Client.GetRoleplay().GuideOtherUser = null;
                            base.Client.SendMessage(new OnGuideSessionDetachedComposer(0));
                            base.Client.SendMessage(new OnGuideSessionDetachedComposer(1));
                        }
                        #endregion
                    }

                    base.EndTimer();
                    return;
                }

                if (base.Client.GetRoomUser() != null)
                {
                    if (base.Client.GetRoomUser().IsAsleep)
                    {
                        Client.GetRoleplay().UpdateTimerDialogue("Work-Timer", "remove", (TimeLeft / 220000), OriginalTime);
                        RoleplayManager.Shout(base.Client, "*Deja de trabajar mientras se han quedado dormido*", 4);

                        WorkManager.RemoveWorkerFromList(base.Client);
                        base.Client.GetRoleplay().IsWorking = false;
                        base.Client.GetHabbo().Poof();

                        RoleplayManager.CheckCorpCarp(base.Client);
                        base.Client.GetRoleplay().CamCargId = 0;
                        if (base.Client.GetRoleplay().TimerManager != null && Client.GetRoleplay().TimerManager.ActiveTimers != null)
                        {
                            if (Client.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("vehiclejob"))
                            {
                                base.Client.SendWhisper("¡Tu Vehículo de trabajo ha sido regresado a su sitio por haberlo abandonado mucho tiempo!", 1);
                                Client.GetRoleplay().TimerManager.ActiveTimers["vehiclejob"].EndTimer();
                            }
                        }

                        if (GroupManager.HasJobCommand(base.Client, "guide"))
                        {
                            guideManager.RemoveGuide(base.Client);
                            base.Client.SendMessage(new HelperToolConfigurationComposer(base.Client));

                            #region End Existing Calls
                            if (base.Client.GetRoleplay().GuideOtherUser != null)
                            {
                                base.Client.GetRoleplay().GuideOtherUser.SendMessage(new OnGuideSessionDetachedComposer(0));
                                base.Client.GetRoleplay().GuideOtherUser.SendMessage(new OnGuideSessionDetachedComposer(1));
                                if (base.Client.GetRoleplay().GuideOtherUser.GetRoleplay() != null)
                                {
                                    base.Client.GetRoleplay().GuideOtherUser.GetRoleplay().Sent911Call = false;
                                    base.Client.GetRoleplay().GuideOtherUser.GetRoleplay().GuideOtherUser = null;
                                }

                                base.Client.GetRoleplay().GuideOtherUser = null;
                                base.Client.SendMessage(new OnGuideSessionDetachedComposer(0));
                                base.Client.SendMessage(new OnGuideSessionDetachedComposer(1));
                            }
                            #endregion
                        }

                        base.EndTimer();
                        return;
                    }

                    /* if (base.Client.GetHabbo().CurrentRoom != null)
                     {
                         if (base.Client.GetHabbo().CurrentRoom.TurfEnabled)
                         {
                             if (GroupManager.HasJobCommand(base.Client, "guide"))
                             {
                                 Client.GetRoleplay().UpdateTimerDialogue("Work-Timer", "remove", (TimeLeft / 220000), OriginalTime);

                                 WorkManager.RemoveWorkerFromList(base.Client);
                                 base.Client.GetRoleplay().IsWorking = false;
                                 base.Client.GetHabbo().Poof();

                                 guideManager.RemoveGuide(base.Client);
                                 base.Client.SendMessage(new HelperToolConfigurationComposer(base.Client));

                                 #region End Existing Calls
                                 if (base.Client.GetRoleplay().GuideOtherUser != null)
                                 {
                                     base.Client.GetRoleplay().GuideOtherUser.SendMessage(new OnGuideSessionDetachedComposer(0));
                                     base.Client.GetRoleplay().GuideOtherUser.SendMessage(new OnGuideSessionDetachedComposer(1));
                                     if (base.Client.GetRoleplay().GuideOtherUser.GetRoleplay() != null)
                                     {
                                         base.Client.GetRoleplay().GuideOtherUser.GetRoleplay().Sent911Call = false;
                                         base.Client.GetRoleplay().GuideOtherUser.GetRoleplay().GuideOtherUser = null;
                                     }

                                     base.Client.GetRoleplay().GuideOtherUser = null;
                                     base.Client.SendMessage(new OnGuideSessionDetachedComposer(0));
                                     base.Client.SendMessage(new OnGuideSessionDetachedComposer(1));
                                 }
                                 #endregion

                                 base.EndTimer();
                                 return;
                             }
                         
                }
                    }*/
                }

                if (base.Client.GetRoomUser() != null)
                    base.Client.GetRoomUser().IdleTime += 3;

                if (RoleplayManager.JobCAPTCHABox)
                {
                    if (base.Client.GetRoleplay().CaptchaSent)
                        return;

                    if (!base.Client.GetRoleplay().CaptchaSent && base.Client.GetRoleplay().CaptchaTime >= Convert.ToInt32(RoleplayData.GetData("captcha", "jobinterval")))
                    {
                        base.Client.GetRoleplay().CreateCaptcha("¡Introduzca el código en la casilla para continuar recogiendo cheques de pago!");
                        return;
                    }
                }

                TimeCount++;
                TimeCount2++;
                TimeLeft -= 1000;

                if (TimeCount == 30 || TimeCount == 60)
                {
                    var Timers = base.Client.GetRoleplay().TimerManager;

                    if (Timers != null)
                    {
                        if (Timers.ActiveTimers != null)
                        {
                            if (Timers.ActiveTimers.ContainsKey("hunger"))
                            {
                                int hungercount = Random.Next(20, 46);
                                Timers.ActiveTimers["hunger"].TimeCount += hungercount;
                            }
                            if (Timers.ActiveTimers.ContainsKey("hygiene"))
                            {
                                int hygienecount = Random.Next(20, 46);
                                Timers.ActiveTimers["hygiene"].TimeCount += hygienecount;
                            }
                            if (Timers.ActiveTimers.ContainsKey("felicidad"))
                            {
                                int felicidadcount = Random.Next(20, 46);
                                Timers.ActiveTimers["felicidad"].TimeCount += felicidadcount;
                            }
                        }
                    }
                }

                if (TimeCount2 == 60)
                {
                    base.Client.GetRoleplay().TimeWorked++;
                    TimeCount2 = 0;
                }

                if (TimeLeft > 0)
                {
                    if (TimeCount == 60)
                    {
                        int EnergyLoss = Random.Next(2, 6);

                        if (base.Client.GetRoleplay().CurEnergy - EnergyLoss <= 0)
                            base.Client.GetRoleplay().CurEnergy = 0;
                        else
                            base.Client.GetRoleplay().CurEnergy -= EnergyLoss;
                        Client.GetRoleplay().UpdateTimerDialogue("Work-Timer", "decrement", (TimeLeft / 60000), OriginalTime);

                        base.Client.SendWhisper("Tienes " + (TimeLeft / 60000) + " Minuto(s) restante hasta que reciba su cheque de pago! Usted también ha perdido " + EnergyLoss + " energía por el trabajo ¡AGOTADOR!", 1);
                        TimeCount = 0;
                    }
                    return;
                }

                if (JobRank == null)
                    return;

                if (base.Client.GetRoleplay().JobId > 1)
                {
                    int Pay;

                    if (base.Client.GetHabbo().VIPRank > 0 || base.Client.GetHabbo().GetClubManager().HasSubscription("habbo_club") || base.Client.GetHabbo().GetClubManager().HasSubscription("habbo_vip"))
                    {
                        base.Client.SendWhisper("¡Por ser VIP se te duplica el sueldo!", 1);
                        if (RoleplayManager.DoubleExp == true)
                            Pay = JobRank.Pay * 4;
                        else
                            Pay = JobRank.Pay * 2;
                    }
                    else
                    {
                        if (RoleplayManager.DoubleExp == true)
                            Pay = JobRank.Pay * 2;
                        else
                            Pay = JobRank.Pay;
                    }

                    if (base.Client.GetRoleplay().Class.ToLower() == "civilian" || base.Client.GetRoleplay().Class.ToLower() == "ciudadano")
                    {
                        Random Random = new Random();
                        int ExtraPay = Random.Next(1, 6);

                        Pay += ExtraPay;
                    }

                    RoleplayManager.Shout(base.Client, "*Recibe su cheque de pago para completar su turno*", 4);
                    //WorkManager.RemoveWorkerFromList(base.Client);
                    base.Client.GetHabbo().Poof();

                    base.Client.SendWhisper("Usted ha ganado $" + Pay + ", sigue trabajando duro." + (base.Client.GetRoleplay().BankAccount > 0 ? " ¡Se ha depositado automáticamente en su cuenta bancaria!" : ""), 1);

                    if (base.Client.GetRoleplay().BankAccount > 0)
                        base.Client.GetRoleplay().BankChequings += Pay;
                    else
                    {
                        base.Client.GetHabbo().Credits += Pay;
                        base.Client.GetHabbo().UpdateCreditsBalance();

                    }
                    if (base.Client.GetHabbo().VIPRank > 0 || base.Client.GetHabbo().GetClubManager().HasSubscription("habbo_club") || base.Client.GetHabbo().GetClubManager().HasSubscription("habbo_vip"))
                    {
                        base.Client.SendWhisper("¡Por ser VIP se te duplica la Experiencia!", 1);
                        if (RoleplayManager.DoubleExp == true)
                            LevelManager.AddLevelEXP(base.Client, GetExp() * 4);
                        else
                            LevelManager.AddLevelEXP(base.Client, GetExp() * 2);
                    }
                    else {
                        if (RoleplayManager.DoubleExp == true)
                            LevelManager.AddLevelEXP(base.Client, GetExp() * 2);
                        else
                            LevelManager.AddLevelEXP(base.Client, GetExp());
                    }
                    
                    PolarEnvironment.GetGame().GetQuestManager().ProgressUserQuest(Client, QuestType.WORK_CYCLE);

                    // BattlePass Challenge Integration
                    PolarEnvironment.GetGame().GetBattlePassManager().ProgressChallenge(base.Client, "work_cycle", 1);

                    #region Timer Restart Calculation

                    double TimeDivisible2 = Math.Floor(Convert.ToDouble(base.Client.GetRoleplay().TimeWorked) / 5);
                    int TimeRemaining2 = base.Client.GetRoleplay().TimeWorked - Convert.ToInt32(TimeDivisible2) * 5;
                    TimeLeft = (5 - TimeRemaining2) * 60000;
                    TimeCount = 0;
                    TimeCount2 = 0;

                    #endregion
                }
                return;
            }
            catch (Exception e)
            {
                Logging.LogRPTimersError("Error in Execute() void: " + e);
                base.EndTimer();
            }
        }

        /// <summary>
        /// Calculates the exp earned from finishing a shift
        /// </summary>
        public int GetExp()
        {
            int Multiplier = 1;

            int Chance = Random.Next(1, 101);

            if (Chance <= 42)
            {
                if (Chance <= 8)
                    Multiplier = 5;
                else if (Chance <= 16)
                    Multiplier = 4;
                else if (Chance <= 32)
                    Multiplier = 3;
                else
                    Multiplier = 2;
            }

            int Amount = Random.Next(15, 51) * Multiplier;

            return Amount;
        }
    }
}