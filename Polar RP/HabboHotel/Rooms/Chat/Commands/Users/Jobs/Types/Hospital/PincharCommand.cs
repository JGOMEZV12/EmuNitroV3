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
    class PincharCommand : IChatCommand
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
            get { return "Comienza a inyectar los esteroides"; }
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

            if (TargetClient == Session)
            {
                Session.SendWhisper("¡No puedes pincharte a ti mismo, pidele apoyo a un compañero de trabajo!", 1);
                return;
            }

            if (Session.GetRoleplay().EquippedWeapon != null)
            {
                Session.SendWhisper("¿Como piensas pincharte con las manos ocupadas? GUARDA EL ARMA", 1);
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
                Session.SendWhisper("Sólo un trabajador del gym puede utilizar este comando", 1);
                return;
            }

            if (!Session.GetRoleplay().IsWorking)
            {
                Session.SendWhisper("¡Debes estar trabajando para usar este comando!", 1);
                return;
            }

            if (TargetClient.GetRoleplay().IsDead)
            {
                Session.SendWhisper("¡No puedes pinchar a alguien que está muerto!", 1);
                return;
            }

            if (TargetClient.GetRoleplay().CurEnergy >= TargetClient.GetRoleplay().MaxEnergy)
            {
                Session.SendWhisper("* " + TargetClient.GetHabbo().Username + " ¡Oh, Lo siento. no puedo inyectarle más Vitamina B-12!", 15);
                return;
            }

            if (TargetUser.IsAsleep)
            {
                Session.SendWhisper("¡No puedes hidratar a alguien que no está jugando ahora mismo!", 1);
                return;
            }

            if (TargetClient.GetHabbo().Credits < 100)
            {
                Session.SendWhisper("¡No tiene 150$ para pincharlo!", 1);
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

                Session.Shout("* Inyecta a " + TargetClient.GetHabbo().Username + " Poniendole 3Cc complejo vitaminico B-12 *", 15);
                TargetClient.Shout("*" + TargetClient.GetHabbo().Username + " se siente más enérgico y fuerte pero pierde [-100$]*", 5);
                TargetClient.GetHabbo().Credits -= 100;
                TargetClient.GetHabbo().UpdateCreditsBalance();
                TargetClient.GetRoleplay().CurEnergy = TargetClient.GetRoleplay().MaxEnergy;
                TargetClient.GetRoleplay().TimerManager.CreateTimer("pinchar", 4000, false);
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
                        Session.SendWhisper("Has ganado un extra $" + Amount + " por pinchar a " + TargetClient.GetHabbo().Username + "!", 1);
                    }
                }

                return;
            }
            else
            {
                Session.SendWhisper("¡Debes acercarte a este ciudadano para colocarle una inyectadora con esteroides!", 1);
                return;
            }
            #endregion
        }
    }
}