using Polar.Communication.Packets.Outgoing.Inventory.Weapons;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Quests;
using Polar.HabboHotel.Rooms;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.RPRoom;
using System;
using System.Linq;
using System.Threading;

namespace Polar.HabboRoleplay.Events.Methods
{
    /// <summary>
    /// Triggered when the user's health changes
    /// </summary>
    public class OnHealthChange : IEvent
    {
        /// <summary>
        /// Responds to the event
        /// </summary>
        public void Execute(object Source, object[] Params)
        {
            GameClient Client = (GameClient)Source;
            if (Client == null || Client.GetRoleplay() == null || Client.GetHabbo() == null)
                return;

            if (Client.GetRoleplay().CurHealth <= 0 && !Client.GetRoleplay().IsJailed && !Client.GetRoleplay().IsDead)
            {
                Client.GetRoleplay().BeingHealed = false;
                Client.GetRoleplay().CloseInteractingUserDialogues();
                Client.GetRoleplay().ClearWebSocketDialogue(true);

                Client.GetRoleplay().BreakGeneralTimer = true;

                Client.GetRoleplay().IsDead = true;
                Client.GetRoleplay().DeadTimeLeft = RoleplayManager.DeathTime;

                NormalDeath(Client);

            }
            else
                Client.GetRoleplay().UpdateInteractingUserDialogues();

            Client.GetRoleplay().RefreshStatDialogue();

            if (Client.GetRoleplay().BeingHealed || Client.GetRoleplay().CurHealth <= 0 || Client.GetRoleplay().CurHealth >= Client.GetRoleplay().MaxHealth)
                return;

            if (Client.GetRoleplay().Hunger >= 100 && Client.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("hunger"))
            {
                int TimeCount = Client.GetRoleplay().TimerManager.ActiveTimers["hunger"].TimeCount;

                if (TimeCount == 0)
                    Client.SendWhisper("Su salud es ahora [" + Client.GetRoleplay().CurHealth + "/" + Client.GetRoleplay().MaxHealth + "] Será mejor que comas algo antes de morirse de hambre", 1);
               /* else
                    RoleplayManager.Shout(Client, "*[" + Client.GetRoleplay().CurHealth + "/" + Client.GetRoleplay().MaxHealth + "]*", 3);*/
            }
            //else
                //RoleplayManager.Shout(Client, "*[" + Client.GetRoleplay().CurHealth + "/" + Client.GetRoleplay().MaxHealth + "]*", 3);
        }

        /// <summary>
        /// Kills the user normally, sends them to the hospital
        /// </summary>
        /// <param name="Client"></param>
        private void NormalDeath(GameClient Client)
        {
            RoleplayManager.Shout(Client, "*Sus signos vitales ya ni se sienten y es translatado al hospital*", 32);
           
            if (Client.GetRoomUser() != null)
                Client.GetRoomUser().ApplyEffect(0);

            Client.GetRoomUser().Frozen = true;
            Client.GetRoomUser().IsWalking = false;

            if (Client.GetRoleplay().IsWorking)
            {
                WorkManager.RemoveWorkerFromList(Client);
                Client.GetRoleplay().IsWorking = false;
            }

            var User = Client.GetRoomUser();

            #region Lays User Down
            if (!User.Statusses.ContainsKey("lay"))
            {
                if ((User.RotBody % 2) == 0)
                {
                    if (User == null)
                        return;

                    try
                    {
                        User.Statusses.Add("lay", "1.0 null");
                        User.Z -= 0.35;
                        User.isLying = true;
                        User.UpdateNeeded = true;
                    }
                    catch { }
                }
                else
                {
                    User.RotBody--;
                    User.Statusses.Add("lay", "1.0 null");
                    User.Z -= 0.35;
                    User.isLying = true;
                    User.UpdateNeeded = true;
                }
            }
            else
            {
                User.Z += 0.35;
                User.RemoveStatus("lay");
                User.isLying = false;
                User.UpdateNeeded = true;
            }
            #endregion

            //Client.GetRoomUser().Frozen = true;

            if (Client.GetRoleplay() != null && Client.GetRoleplay().TimerManager != null && Client.GetRoleplay().TimerManager.ActiveTimers != null)
            {
                if (Client.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("death"))
                    Client.GetRoleplay().TimerManager.ActiveTimers["death"].EndTimer();
                Client.GetRoleplay().TimerManager.CreateTimer("death", 500, true);
            }

            if (Client.GetRoleplay().EquippedWeapon != null)
            {
                RoleplayManager.UpdateMyWeaponStats(Client, "bullets", Client.GetRoleplay().Bullets, Client.GetRoleplay().EquippedWeapon.Name);
                RoleplayManager.UpdateMyWeaponStats(Client, "life", Client.GetRoleplay().WLife, Client.GetRoleplay().EquippedWeapon.Name);

                if (Client.GetRoomUser().CurrentEffect == Client.GetRoleplay().EquippedWeapon.EffectID)
                    Client.GetRoomUser().ApplyEffect(0);

                Client.GetRoleplay().EquippedWeapon = null;

                Client.SendMessage(new WeaponsComposer(Client));
            }
            PolarEnvironment.GetGame().GetQuestManager().ProgressUserQuest(Client, QuestType.DEATH);

            Room Room = RoleplayManager.GenerateRoom(Client.GetRoomUser().RoomId);
            string MyCity = Room.City;

            Polar.HabboRoleplay.RPRoom.RPRoom Data;
            int ToHosp = PolarEnvironment.GetGame().GetRPRoomManager().TryToGetHospital(MyCity, out Data);

            if (ToHosp > 0)
            {
                Room Room2 = RoleplayManager.GenerateRoom(ToHosp);
                if (Room2 != null)
                {
                    Client.GetRoleplay().IsDead = true;
                    Client.GetRoleplay().DeadTimeLeft = RoleplayManager.DeathTime;

                    Client.GetHabbo().HomeRoom = ToHosp;

                    /*
                    if (base.Client.GetHabbo().CurrentRoomId != ToHosp)
                        RoleplayManager.SendUserTimer(Client, ToHosp, "", "death");
                    else
                        Client.GetPlay().TimerManager.CreateTimer("death", 1000, true);
                    */
                    RoleplayManager.GetLookAndMotto(Client);
                    RoleplayManager.SpawnBeds(Client, "hosptl_bed");
                    RoleplayManager.SendUserTimer(Client, ToHosp, "", "death");
                }
                else
                {
                    Client.SendNotification("[Error][102] -> Lamentablemente ha habido un error. No se encontró ningún Hospital disponible en esta ciudad. Comunícaselo a un Administrador. ¡Gracias!");
                    Client.GetRoleplay().CurHealth = Client.GetRoleplay().MaxHealth;
                    Client.GetRoleplay().RefreshStatDialogue();
                    Client.GetRoomUser().Frozen = false;
                    Client.SendWhisper("Se te ha revivido a causa que no hay ningún hospital en esta Ciudad");
                }
            }
            else
            {
                Client.SendNotification("[Error][103] -> Lamentablemente ha habido un error. No se encontró ningún Hospital disponible en esta ciudad. Comunícaselo a un Administrador. ¡Gracias!");
                Client.GetRoleplay().CurHealth = Client.GetRoleplay().MaxHealth;
                Client.GetRoleplay().RefreshStatDialogue();
                Client.GetRoomUser().Frozen = false;
                Client.SendWhisper("Se te ha revivido a causa que no hay ningún hospital en esta Ciudad");
            }
           

        }

    }
}