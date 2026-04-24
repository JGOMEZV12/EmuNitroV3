using System;
using System.Linq;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboRoleplay.Misc;
using Polar.Core;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Items;
using Polar.HabboRoleplay.Timers;
using System.Threading;

namespace Polar.HabboRoleplay.Timers.Types
{
    /// <summary>
    /// Waits specified time then releases user from hospital
    /// </summary>
    public class DeathTimer : RoleplayTimer
    {

        public DeathTimer(string Type, GameClient Client, int Time, bool Forever, object[] Params)
            : base(Type, Client, Time, Forever, Params)
        {
            // Convert to milliseconds
            OriginalTime = RoleplayManager.DeathTime;
            TimeLeft = Client.GetRoleplay().DeadTimeLeft * 1000;
            Client.GetRoleplay().UpdateTimerDialogue("Dead-Timer", "add", Client.GetRoleplay().DeadTimeLeft, OriginalTime);
        }

        /// <summary>
        /// Decrease our users timer
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

                if (base.Client.GetRoomUser() == null)
                    return;

                // Asignamos una herida si no la tiene.
                if (base.Client.GetRoleplay().HeridaName == null || base.Client.GetRoleplay().HeridaName == "")
                {
                    #region Heridas
                    Random rnds = new Random();
                    int heri = rnds.Next(1, 11);
                    if (base.Client.GetRoleplay().HeridaPaci <= 0)
                    {
                        if (heri == 1)
                        {
                            base.Client.GetRoleplay().HeridaName = "Herida de bala";
                            base.Client.GetRoleplay().HeridaPaci = 1;
                        }
                        else if (heri == 2)
                        {
                            base.Client.GetRoleplay().HeridaName = "Múltiples heridas de bala";
                            base.Client.GetRoleplay().HeridaPaci = 2;
                        }
                        else if (heri == 3)
                        {
                            base.Client.GetRoleplay().HeridaName = "Fracturas Graves";
                            base.Client.GetRoleplay().HeridaPaci = 3;
                        }
                        else if (heri == 4)
                        {
                            base.Client.GetRoleplay().HeridaName = "Fracturas leves";
                            base.Client.GetRoleplay().HeridaPaci = 4;
                        }
                        else if (heri == 5)
                        {
                            base.Client.GetRoleplay().HeridaName = "Herida abierta de sangre";
                            base.Client.GetRoleplay().HeridaPaci = 5;
                        }
                        else if (heri == 6)
                        {
                            base.Client.GetRoleplay().HeridaName = "Hematomas";
                            base.Client.GetRoleplay().HeridaPaci = 6;
                        }
                        else if (heri == 7)
                        {
                            base.Client.GetRoleplay().HeridaName = "Hematomas y huesos fracturados";
                            base.Client.GetRoleplay().HeridaPaci = 7;
                        }
                        else if (heri == 8)
                        {
                            base.Client.GetRoleplay().HeridaName = "Hemorragia cerebral";
                            base.Client.GetRoleplay().HeridaPaci = 8;
                        }
                        else if (heri == 9)
                        {
                            base.Client.GetRoleplay().HeridaName = "Quemaduras";
                            base.Client.GetRoleplay().HeridaPaci = 9;
                        }
                        else if (heri == 10)
                        {
                            base.Client.GetRoleplay().HeridaName = "Daños severos";
                            base.Client.GetRoleplay().HeridaPaci = 10;
                        }
                        else
                        {
                            base.Client.GetRoleplay().HeridaName = "Herida de bala";
                            base.Client.GetRoleplay().HeridaPaci = 1;
                        }
                    }
                    #endregion
                }

                #region Revivido #1 (endtimer)
                if (!base.Client.GetRoleplay().IsDead)
                {
                    Client.GetRoleplay().UpdateTimerDialogue("Dead-Timer", "remove", Client.GetRoleplay().DeadTimeLeft, OriginalTime);

                    if (base.Client.GetRoomUser().Frozen)
                        base.Client.GetRoomUser().Frozen = false;

                    //if (base.Client.GetHabbo().CurrentRoomId == Convert.ToInt32(RoleplayData.GetData("hospital", "roomid2")))
                    //RoleplayManager.SpawnChairs(base.Client, "val14_wchair");

                    #region IN CHAIR By Jeihden
                    Room Room2 = base.Client.GetHabbo().CurrentRoom;

                    Item BTile2 = Room2.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "val14_wchair" && x.Coordinate == base.Client.GetRoomUser().Coordinate);
                    if (BTile2 == null)
                    {
                        new Thread(() =>
                        {
                            Thread.Sleep(500);
                            RoleplayManager.SpawnChairs(Client, "val14_wchair", null, Room2);
                        }).Start();
                    }
                    #endregion

                    base.Client.GetRoleplay().BeingHealed = false;
                    base.Client.GetRoleplay().HeridaName = "";
                    base.Client.GetRoleplay().HeridaPaci = 0;
                    base.Client.GetRoleplay().IsDead = false;
                    base.Client.GetRoomUser().ApplyEffect(0);
                    base.Client.GetRoomUser().Frozen = false;
                    base.Client.GetRoleplay().DeadTimeLeft = 0;
                    base.Client.GetRoleplay().InState = false;
                    base.Client.GetHabbo().Poof(true);
                    base.Client.GetRoleplay().ReplenishStats(true);
                    base.EndTimer();
                    return;
                }
                #endregion

                if (!base.Client.GetRoomUser().Frozen)
                    base.Client.GetRoomUser().Frozen = true;

                if (base.Client.GetRoleplay().BeingHealed)
                  return;

                TimeCount++;
                TimeLeft -= 1000;

                #region IN STATE CONDITIONS By Jeihden
                // Verificar si está en una camilla.
                if (!base.Client.GetRoleplay().InState)
                {
                    Room Room3 = base.Client.GetHabbo().CurrentRoom;

                    Item BTile3 = Room3.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "hosptl_bed" && x.Coordinate == base.Client.GetRoomUser().Coordinate);
                    if (BTile3 == null)
                    {
                        RoleplayManager.GetLookAndMotto(Client);
                        RoleplayManager.SpawnBeds(Client, "hosptl_bed");
                    }
                    else
                        base.Client.GetRoleplay().InState = true;
                }
                #endregion

                // Animación de curación.
                if (TimeCount % 9 == 0 && base.Client.GetRoleplay().CurHealth + 3 < base.Client.GetRoleplay().MaxHealth)
                {
                    base.Client.GetRoleplay().CurHealth += 3;
                    base.Client.GetRoleplay().RefreshStatDialogue();

                }

                // Cada minuto.
                if (TimeCount == 60)
                    base.Client.GetRoleplay().DeadTimeLeft--;

                // Mensaje al Usuario cada minuto.
                if (TimeLeft > 0)
                {
                    if (TimeCount == 60)
                    {
                        Client.GetRoleplay().UpdateTimerDialogue("Dead-Timer", "decrement", Client.GetRoleplay().DeadTimeLeft, OriginalTime);
                        base.Client.SendWhisper("Resta(n) " + base.Client.GetRoleplay().DeadTimeLeft + " minuto(s) para que seas dad@ de alta del Hospital.", 1);
                        TimeCount = 0;
                    }
                    return;
                }

                #region Revivido #2 (endtimer)
                if (base.Client.GetRoomUser().Frozen)
                    base.Client.GetRoomUser().Frozen = false;

                //if (base.Client.GetRoomUser().RoomId == Convert.ToInt32(RoleplayData.GetData("hospital", "roomid2")))
                //RoleplayManager.SpawnChairs(base.Client, "val14_wchair");

                #region IN CHAIR By JD
                Room Room = base.Client.GetHabbo().CurrentRoom;

                Item BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "val14_wchair" && x.Coordinate == base.Client.GetRoomUser().Coordinate);
                if (BTile == null)
                {
                    new Thread(() =>
                    {
                        Thread.Sleep(500);
                        RoleplayManager.SpawnChairs(Client, "val14_wchair", null, Room);
                    }).Start();
                }
                #endregion

                Client.GetRoleplay().UpdateTimerDialogue("Dead-Timer", "remove", Client.GetRoleplay().DeadTimeLeft, OriginalTime);

                RoleplayManager.Shout(base.Client, "*Recupera la conciencia [-1000$] por gastos médicos*", 4);

                base.Client.GetRoleplay().BeingHealed = false;
                base.Client.GetRoleplay().HeridaName = "";
                base.Client.GetRoomUser().ApplyEffect(0);
                base.Client.GetRoleplay().HeridaPaci = 0;
                base.Client.GetRoleplay().IsDead = false;
                base.Client.GetRoleplay().DeadTimeLeft = 0;
                base.Client.GetRoleplay().InState = false;
                base.Client.GetRoleplay().Sida = 0;
                base.Client.GetRoleplay().Embarazo = 0;
                base.Client.GetHabbo().Credits -= 1000;
                #region Bank Company Balance
                RoleplayManager.GiveMoneyToCompany(2, base.Client, "hospital", true, 1000);
                #endregion Bank Company Balance
                base.Client.GetHabbo().UpdateCreditsBalance();
                base.Client.GetHabbo().Poof(true);
                base.Client.GetRoleplay().ReplenishStats(true);
                base.Client.GetRoleplay().RefreshStatDialogue();
                base.EndTimer();
                #endregion
            }
            catch (Exception e)
            {
                Logging.LogRPTimersError("Error in Execute() void: " + e);
                base.EndTimer();
            }
        }
    }
}