using System;
using System.Linq;
using System.Text;
using System.Drawing;
using Polar.Utilities;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Criminal
{
    class RobCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_criminal_activity_rob"; }
        }

        public string Parameters
        {
            get { return "%user%"; }
        }

        public string Description
        {
            get { return "Roba la cartera de usuarios si llevan dinero, hierba, cocaína o cigarrillos."; }
        }

        public Wanted NewWanted { get; private set; }

        public async Task Execute(GameClient Session, Rooms.Room Room, string[] Params)
        {

            #region Conditions

            if (Params.Length == 1)
            {
                Session.SendWhisper("Vaya, se le olvidó ingresar un nombre de usuario!", 1);
                return;
            }

            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);

            if (TargetClient == null || TargetClient.GetRoleplay() == null)
            {
                Session.SendWhisper("Se ha producido un error al intentar encontrar a ese usuario, tal vez estén sin conexión.", 1);
                return;
            }

            int LevelDifference = Math.Abs(Session.GetRoleplay().Level - TargetClient.GetRoleplay().Level);

            RoomUser RoomUser = Session.GetRoomUser();
            RoomUser TargetUser = Room.GetRoomUserManager().GetRoomUserByHabbo(TargetClient.GetHabbo().Username);

            if (TargetUser == null)
            {
                Session.SendWhisper("Se ha producido un error al encontrar a ese usuario, tal vez no estén en línea o en esta sala.", 1);
                return;
            }

            if (Session.GetRoleplay().IsDead)
            {
                Session.SendWhisper("¡No puedes robar a alguien mientras estás muerto!", 1);
                return;
            }

            if (Session.GetRoleplay().PassiveMode)
            {
                Session.SendWhisper("No puedes realizar acciones ilegales en modo pasivo.", 1);
                return;
            }

            if (Session.GetRoleplay().IsJailed)
            {
                Session.SendWhisper("¡No puedes robar a alguien mientras estás en la cárcel!", 1);
                return;
            }

            if (Session.GetRoleplay().IsWorking)
            {
                Session.SendWhisper("No puedes robar, estas trabajando...", 1);
                return;
            }

            if (Session.GetRoleplay().StaffOnDuty || Session.GetRoleplay().AmbassadorOnDuty)
            {
                Session.SendWhisper("¡No puedes robar a alguien mientras estás de servicio!", 1);
                return;
            }

            if (TargetClient.GetRoleplay().IsDead)
            {
                Session.SendWhisper("¡No puedes robar a alguien que está muerto!", 1);
                return;
            }

            if (TargetClient.GetRoleplay().IsJailed)
            {
                Session.SendWhisper("¡No puedes robar a alguien que está en la cárcel!", 1);
                return;
            }

            if (TargetClient.GetRoleplay().StaffOnDuty)
            {
                Session.SendWhisper("Usted no puede robar a un miembro del personal que está de servicio", 1);
                return;
            }

            if (TargetClient.GetRoleplay().Level < 8)
            {
                Session.SendWhisper("Necesita ser minimo nivel 8 para poder robar", 1);
                return;
            }

            if (TargetClient.GetRoleplay().AmbassadorOnDuty)
            {
                Session.SendWhisper("¡No puedes robar a un embajador que está de servicio!", 1);
                return;
            }

            if (TargetClient.GetHabbo().VIPRank < 1)
            {
                Session.SendWhisper("No se puede robar a este miembro del personal específico", 1);
                return;
            }

            if (TargetUser.IsAsleep)
            {
                Session.SendWhisper("¡No puedes robar a alguien que no esté jugando el juego ahora mismo!", 1);
                return;
            }

            if (!Room.RobEnabled && !RoleplayManager.PurgeStarted)
            {
                Session.SendWhisper("No se puede robar en esta habitación", 1);
                return;
            }

            if (Session.GetRoleplay().DrivingCar)
            {
                Session.SendWhisper("¡No puedes robar a alguien mientras conduces un vehículo!", 1);
                return;
            }

            if (Session.GetRoleplay().Pasajero)
            {
                Session.SendWhisper("¡No puedes hacer eso mientras vas de Pasajer@!", 1);
                return;
            }
            if (Session.GetRoleplay().IsNoob)
            {
                Session.SendWhisper("¡No puedes completar esta acción mientras estás bajo la protección de Dios por ser nuevo!", 1);
                return;
            }

            if (TargetClient == Session)
            {
                Session.SendWhisper("No se puede robar", 1);
                return;
            }

            /*if (TargetClient.MachineId == Session.MachineId)
            {
                Session.SendWhisper("¡No puedes robar otra de tus cuentas!", 1);
                return;
            }*/

            if (TargetClient.GetRoleplay().IsNoob)
            {
                Session.SendWhisper("*Este usuario se encuentra bajo inmunidad*", 1);
                return;
            }

            if (Session.GetRoleplay().TryGetCooldown("robbery"))
                return;

            if (LevelDifference > 8)
            {
                Session.SendWhisper("¡No puedes robar a este usuario ya que tu diferencia de nivel es mayor de 5!", 1);
                return;
            }

            
            if (Session.GetRoleplay().SpecialCooldowns["robbery"] > 0)
            {
                Session.SendWhisper("Debe esperar hasta que pueda robar al usuario otra vez!");
                return;
            }

            #endregion

            #region Execute
            CryptoRandom Random = new CryptoRandom();
            Point ClientPos = new Point(RoomUser.X, RoomUser.Y);
            Point TargetClientPos = new Point(TargetUser.X, TargetUser.Y);
            double Distance = RoleplayManager.GetDistanceBetweenPoints2D(ClientPos, TargetClientPos);

            bool Success = false;
            string RobbedItems = "";
            int DrugsChance = Random.Next(1, 101);

            if (Distance <= 1)
            {
                if (TargetClient.GetHabbo().Credits > 30)
                {
                    int AmountToRob;
                    if (TargetClient.GetHabbo().Credits > 500)
                        AmountToRob = 500;
                    else
                        AmountToRob = TargetClient.GetHabbo().Credits;

                    int MaxAmount = Convert.ToInt32(Math.Floor((Double)AmountToRob / 5));
                    int MinAmount = Convert.ToInt32(Math.Floor((double)AmountToRob / 40));

                    int Amount = Random.Next(MinAmount, MaxAmount + 1);

                    Session.GetHabbo().Credits += Amount;
                    Session.GetHabbo().UpdateCreditsBalance();

                    TargetClient.GetHabbo().Credits -= Amount;
                    TargetClient.GetHabbo().UpdateCreditsBalance();

                    Success = true;
                    RobbedItems += "$" + String.Format("{0:N0}", Amount) + ", ";
                }

                if (DrugsChance <= 15)
                {
                    if (TargetClient.GetRoleplay().Weed > 30)
                    {
                        int AmountToRob;
                        if (TargetClient.GetRoleplay().Weed > 100)
                            AmountToRob = 100;
                        else
                            AmountToRob = TargetClient.GetRoleplay().Weed;

                        int MaxAmount = Convert.ToInt32(Math.Floor((Double)AmountToRob / 5));
                        int MinAmount = Convert.ToInt32(Math.Floor((double)AmountToRob / 40));

                        int Amount = Random.Next(MinAmount, MaxAmount + 1);

                        Session.GetRoleplay().Weed += Amount;
                        TargetClient.GetRoleplay().Weed -= Amount;

                        Success = true;
                        RobbedItems += "Una pequeña bolsa que contiene " + String.Format("{0:N0}", Amount) + "g de marihuana, ";
                    }

                    if (TargetClient.GetRoleplay().Heroina > 30)
                    {
                        int AmountToRob;
                        if (TargetClient.GetRoleplay().Heroina > 100)
                            AmountToRob = 100;
                        else
                            AmountToRob = TargetClient.GetRoleplay().Heroina;

                        int MaxAmount = Convert.ToInt32(Math.Floor((Double)AmountToRob / 5));
                        int MinAmount = Convert.ToInt32(Math.Floor((double)AmountToRob / 40));

                        int Amount = Random.Next(MinAmount, MaxAmount + 1);

                        Session.GetRoleplay().Heroina += Amount;
                        TargetClient.GetRoleplay().Heroina -= Amount;

                        Success = true;
                        RobbedItems += "Una pequeña caja que contiene " + String.Format("{0:N0}", Amount) + "cc de heroina, ";
                    }

                    if (TargetClient.GetRoleplay().Cocaine > 30)
                    {
                        int AmountToRob;
                        if (TargetClient.GetRoleplay().Cocaine > 100)
                            AmountToRob = 100;
                        else
                            AmountToRob = TargetClient.GetRoleplay().Cocaine;

                        int MaxAmount = Convert.ToInt32(Math.Floor((Double)AmountToRob / 5));
                        int MinAmount = Convert.ToInt32(Math.Floor((double)AmountToRob / 40));

                        int Amount = Random.Next(MinAmount, MaxAmount + 1);

                        Session.GetRoleplay().Cocaine += Amount;
                        TargetClient.GetRoleplay().Cocaine -= Amount;

                        Success = true;
                        RobbedItems += "Una pequeña bolsa que contiene " + String.Format("{0:N0}", Amount) + "g de cocaina, ";
                    }

                    if (TargetClient.GetRoleplay().Cigarettes > 30)
                    {
                        int AmountToRob;
                        if (TargetClient.GetRoleplay().Cigarettes > 100)
                            AmountToRob = 100;
                        else
                            AmountToRob = TargetClient.GetRoleplay().Cigarettes;

                        int MaxAmount = Convert.ToInt32(Math.Floor((Double)AmountToRob / 5));
                        int MinAmount = Convert.ToInt32(Math.Floor((double)AmountToRob / 40));

                        int Amount = Random.Next(MinAmount, MaxAmount + 1);

                        Session.GetRoleplay().Cigarettes += Amount;
                        TargetClient.GetRoleplay().Cigarettes -= Amount;

                        Success = true;
                        RobbedItems += " Una pequeña bolsa que contiene " + String.Format("{0:N0}", Amount) + "g de cigarrillos, ";
                    }

                    if (TargetClient.GetRoleplay().Medicina > 30)
                    {
                        int AmountToRob;
                        if (TargetClient.GetRoleplay().Cigarettes > 5)
                            AmountToRob = 5;
                        else
                            AmountToRob = TargetClient.GetRoleplay().Cigarettes;

                        int MaxAmount = Convert.ToInt32(Math.Floor((Double)AmountToRob / 5));
                        int MinAmount = Convert.ToInt32(Math.Floor((double)AmountToRob / 40));

                        int Amount = Random.Next(MinAmount, MaxAmount + 1);

                        Session.GetRoleplay().Medicina += Amount;
                        TargetClient.GetRoleplay().Medicina -= Amount;

                        Success = true;
                        RobbedItems += " Una pequeña bolsa que contiene " + String.Format("{0:N0}", Amount) + "g de Medicinas, ";
                    }
                }
                
                if (!Success)
                {
                    Session.SendWhisper("Lo siento, pero esta persona es demasiado pobre para robar", 1);
                    return;
                }

                if (Success)
                {

                    Session.GetRoleplay().TimerManager.CreateTimer("wanted", 1000, false);
                    if (!Session.GetRoleplay().WantedFor.Contains("robbing"))
                    Session.GetRoleplay().WantedFor = Session.GetRoleplay().WantedFor + "robbing, ";
                    Session.GetRoleplay().LevelEXP -= 20;
                    Session.GetRoleplay().CurHealth -= 20;
                    Session.GetRoleplay().IsWanted = true;
                    Session.GetRoleplay().WantedLevel = 2;
                    Session.GetRoleplay().WantedTimeLeft = 7;
                    Session.Shout("*Agarra a  " + TargetClient.GetHabbo().Username + " por el cuello y lo apunta para robarle " + RobbedItems.TrimEnd(',', ' ') + " pero pierde [-20 Exp] y por un golpe [-20 Salud]*", 4);
                    TargetClient.SendWhisper("Te robaron " + RobbedItems.TrimEnd(',', ' ') + "  fue: " + Session.GetHabbo().Username + "!", 1);

                    Session.GetRoleplay().CooldownManager.CreateCooldown("robbery", 1000, 300);
                    Session.GetRoleplay().SpecialCooldowns.TryUpdate("robbery", 300, Session.GetRoleplay().SpecialCooldowns["robbery"]);
                }
            }
            else
            {
                Session.SendWhisper("Usted necesita acercarse a " + TargetClient.GetHabbo().Username + " Con el fin de robarlo", 1);
                return;
            }
            #endregion
        }
    }
}