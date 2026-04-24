using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using Polar.HabboRoleplay.Bots.Manager;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Jobs.Types.Hospital
{
    class DischargeCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_hospital_discharge"; }
        }

        public string Parameters
        {
            get { return "%user%"; }
        }

        public string Description
        {
            get { return "Revivir al ciudadano del hospital si está muerto."; }
        }

        public async Task Execute(GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            if (Params.Length == 1)
            {
                Session.SendWhisper("Vaya, se le olvidó ingresar un nombre de usuario", 1);
                return;
            }

            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);
            if (TargetClient == null)
            {
                RoomUser Bot = Room.GetRoomUserManager().GetBotByName(Params[1]);

                if (Bot != null && Bot.GetBotRoleplay() != null)
                {
                    ExecuteBot(Session, Bot, Room);
                    return;
                }

                Session.SendWhisper("Se ha producido un error al intentar encontrar a ese usuario, tal vez estén sin conexión.", 1);
                return;
            }

            RoomUser RoomUser = Session.GetRoomUser();
            RoomUser TargetUser = Room.GetRoomUserManager().GetRoomUserByHabbo(TargetClient.GetHabbo().Username);
            if (TargetUser == null)
            {
                Session.SendWhisper("Se ha producido un error al encontrar a ese usuario, tal vez no estén en línea o en esta sala.", 1);
                return;
            }

            if (!GroupManager.HasJobCommand(Session, "discharge"))
            {
                Session.SendWhisper("Sólo un trabajador del hospital puede utilizar este comando", 1);
                return;
            }

            if (!Session.GetRoleplay().IsWorking)
            {
                Session.SendWhisper("¡Debes estar trabajando para usar este comando!", 1);
                return;
            }

            if (Session.GetHabbo().Credits < 10)
            {
                Session.Shout("* Paciente " + TargetClient.GetHabbo().Username + " No puede pagar gastos médicos, debe esperar que sane por si solo!", 15);
                return;
            }

            if (!TargetClient.GetRoleplay().IsDead)
            {
                Session.SendWhisper("¡No puedes revivir a alguien que no está muerto!", 1);
                return;
            }
            #endregion

            #region Execute
            double Distance = RoleplayManager.GetDistanceBetweenPoints2D(Session.GetRoomUser().Coordinate, TargetClient.GetRoomUser().Coordinate);

            if (Distance <= 5)
            {
                Session.Shout("* Revive a " + TargetClient.GetHabbo().Username + " y le da de alta, ya se puede ir*", 15);
                TargetClient.SendWhisper("¡Pagaste 10$ por gastos médicos! ya puedes irte del hospital o escribe 'salir'", 1);
                TargetClient.GetRoleplay().IsDead = false;
				TargetClient.GetRoleplay().InState = false;
                TargetClient.GetRoleplay().DeadTimeLeft = 0;
                TargetClient.GetRoomUser().ApplyEffect(0);
                Session.GetHabbo().Credits += 10;
                Session.GetHabbo().UpdateCreditsBalance();
                TargetClient.GetHabbo().Credits -= 10;
                TargetClient.GetHabbo().UpdateCreditsBalance();
                Session.SendWhisper("¡Ganas 10$ por revivir y dar de alta a este paciente!", 1);
                return;
            }
            else
            {
                Session.SendWhisper("¡Debes acercarte a este ciudadano para descargarlos!", 1);
                return;
            }
            #endregion
        }

        public void ExecuteBot(GameClient Session, RoomUser Bot, Room Room)
        {
            if (!Bot.GetBotRoleplay().Dead)
            {
                Session.SendWhisper("Perdón pero " + Bot.GetBotRoleplay().Name + " ¡no está muerto!", 1);
                return;
            }

            double Distance = RoleplayManager.GetDistanceBetweenPoints2D(Session.GetRoomUser().Coordinate, Bot.Coordinate);

            if (Distance <= 5)
            {
                if (Bot.GetBotRoleplay().TimerManager.ActiveTimers.ContainsKey("botdeath"))
                    Bot.GetBotRoleplay().TimerManager.ActiveTimers["botdeath"].EndTimer();

                if (Bot.Frozen)
                    Bot.Frozen = false;

                Session.Shout("*Revive a " + Bot.GetBotRoleplay().Name + " en su cama de hospital*", 4);
                RoleplayManager.SpawnChairs(null, "val14_wchair", Bot);

                Bot.GetBotRoleplay().Dead = false;
                Room.SendMessage(new UsersComposer(Bot));

                if (Bot.GetBotRoleplay().RoamBot)
                    Bot.GetBotRoleplay().MoveRandomly();

                if (Session.GetRoleplay().LastKilled != (RoleplayBotManager.BotFriendMultiplyer + Bot.GetBotRoleplay().Id))
                {
                    int Amount = 0;

                    if (Session.GetRoleplay().Level <= 5)
                        Amount = 1;
                    else if (Session.GetRoleplay().Level > 5 && Session.GetRoleplay().Level <= 10)
                        Amount = 2;
                    else if (Session.GetRoleplay().Level > 10 && Session.GetRoleplay().Level <= 15)
                        Amount = 3;
                    else
                        Amount = 4;

                    PolarEnvironment.GetGame().GetAchievementManager().ProgressAchievement(Session, "ACH_Discharging", 1);

                    Session.GetRoleplay().LastKilled = (RoleplayBotManager.BotFriendMultiplyer + Bot.GetBotRoleplay().Id);

                    if (!Room.HitEnabled && !Room.ShootEnabled)
                    {
                        Session.GetHabbo().Credits += Amount;
                        Session.GetHabbo().UpdateCreditsBalance();
                        Session.SendWhisper("Has ganado un extra $" + Amount + " por revivir a " + Bot.GetBotRoleplay().Name + "!", 1);
                    }
                }
                return;
            }
            else
            {
                Session.SendWhisper("Debes acercarte a " + Bot.GetBotRoleplay().Name + " para poder revivirlo", 1);
                return;
            }
        }
    }
}