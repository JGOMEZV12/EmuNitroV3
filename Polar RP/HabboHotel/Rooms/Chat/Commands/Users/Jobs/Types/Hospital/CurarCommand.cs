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

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Jobs.Types.Hospital
{
    class CurarCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_hospital_heal"; }
        }

        public string Parameters
        {
            get { return "%user%"; }
        }

        public string Description
        {
            get { return "Comienza a sanar al ciudadano seleccionado"; }
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
                Session.SendWhisper("Se ha producido un error al intentar encontrar a ese usuario, tal vez estén sin conexión.", 1);
                return;
            }

            if (TargetClient.GetRoleplay() == null)
            {
                Session.SendWhisper("Se ha producido un error al intentar encontrar a ese usuario, tal vez estén sin conexión.", 1);
                return;
            }

            if (TargetClient.GetRoleplay().TimerManager == null)
            {
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

            if (!GroupManager.HasJobCommand(Session, "heal"))
            {
                Session.SendWhisper("Sólo un trabajador del hospital puede utilizar este comando", 1);
                return;
            }

            if (!Session.GetRoleplay().IsWorking)
            {
                Session.SendWhisper("¡Debes estar trabajando para usar este comando!", 1);
                return;
            }

            if (TargetClient.GetRoleplay().IsDead)
            {
                Session.SendWhisper("¡No puedes curar a alguien que está muerto!", 1);
                return;
            }

            if (TargetClient.GetRoleplay().CurHealth >= TargetClient.GetRoleplay().MaxHealth)
            {
                Session.Shout("* ¡Hey " + TargetClient.GetHabbo().Username + " Usted goza de buena salud! *", 1);
                return;
            }

            if (Session.GetRoleplay().EquippedWeapon != null)
            {
                Session.SendWhisper("¿Como piensas curar con las manos ocupadas? GUARDA EL ARMA", 1);
                return;
            }

            if (TargetUser.IsAsleep)
            {
                Session.SendWhisper("¡No puedes curar a alguien que no está jugando ahora mismo!", 1);
                return;
            }

            if (TargetClient.GetHabbo().Credits < 100)
            {
                Session.SendWhisper("* Hey, " + TargetClient.GetHabbo().Username + " ¡No tienes 100$ para curarte debes ser transladado al hospital!", 1);
                return;
            }

            if (TargetClient == Session)
            {
                Session.SendWhisper("¡No puedes curarte a ti mismo, pidele apoyo a un compañero de trabajo!", 1);
                return;
            }

            if (TargetClient.GetRoleplay().CurHealth >= TargetClient.GetRoleplay().MaxHealth)
            {
                RoomUser User = Session.GetRoomUser();
                if (User == null)
                    return;

                User.CarryItem(1014);
                Session.SendWhisper("¡No puedes curar a alguien que tiene buena salud!", 1);
                return;
            }
            #endregion

            #region Execute
            Point ClientPos = new Point(RoomUser.X, RoomUser.Y);
            Point TargetClientPos = new Point(TargetUser.X, TargetUser.Y);
            double Distance = RoleplayManager.GetDistanceBetweenPoints2D(ClientPos, TargetClientPos);

            if (Distance <= 2)
            {
                RoomUser User = Session.GetRoomUser();
                if (User == null)
                    return;

                User.CarryItem(1014);

                Session.Shout("*Inyecta a " + TargetClient.GetHabbo().Username + " medicamentos para estabilizarlo y sanarlo [-5$]*", 4);
                Session.SendWhisper("Aplique varias dosis si es necesario, debe estar al 100 % o lo que el paciente le pida", 1);
                TargetClient.GetRoomUser().ApplyEffect(0);
                TargetClient.GetHabbo().Credits -= 100;
                Session.GetHabbo().Credits += 1;
                TargetClient.GetHabbo().UpdateCreditsBalance();
                Session.GetHabbo().UpdateCreditsBalance();
                TargetClient.GetRoleplay().CurHealth = TargetClient.GetRoleplay().MaxHealth;
                TargetClient.GetRoleplay().TimerManager.CreateTimer("curar", 4000, false);
                #region Bank Company Balance
                RoleplayManager.GiveMoneyToCompany(2, Session, "hospital", true, 100);
                #endregion Bank Company Balance

                if (Session.GetRoleplay().LastKilled != TargetClient.GetHabbo().Id && TargetClient.GetHabbo().Id != Session.GetHabbo().Id)
                {
                    int Amount = 0;

                    if (Session.GetRoleplay().Level <= 10)
                        Amount = 1;
                    else
                        Amount = 2;

                    PolarEnvironment.GetGame().GetAchievementManager().ProgressAchievement(Session, "ACH_Healing", 1);

                    Session.GetRoleplay().LastKilled = TargetClient.GetHabbo().Id;

                    if (!Room.HitEnabled)
                    {
                        Session.GetHabbo().Credits += Amount;
                        Session.GetHabbo().UpdateCreditsBalance();
                        Session.SendWhisper("Has ganado un extra $" + Amount + " por asistir a " + TargetClient.GetHabbo().Username + "!", 1);
                    }
                }

                return;
            }
            else
            {
                Session.SendWhisper("¡Debes acercarte a este ciudadano para colocarle una intravesosa!", 1);
                return;
            }
            #endregion
        }
    }
}