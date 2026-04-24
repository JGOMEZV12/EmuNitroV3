using System;
using System.Linq;
using System.Text;
using System.Drawing;
using Polar.Utilities;
using System.Collections.Generic;
using Polar.HabboRoleplay.Timers;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Turfs;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using System.Threading;
using Polar.HabboRoleplay.RPRoom;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Criminal
{
    class RobBankCommand : IChatCommand
    {
        private readonly bool _stopRobCommand;
        public string PermissionRequired
        {
            get { return "command_criminal_activity_rob"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Roba el banco."; }
        }

        public RobBankCommand(bool stopRobCommand = false)
        {
            _stopRobCommand = stopRobCommand;
        }


        public async Task Execute(GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Variables
            
            Group Gang = GroupManager.GetGang(Session.GetRoleplay().GangId);
            Turf Turf = TurfManager.GetTurf(Room.RoomId);
            //bool InsideTurf = false;
            #endregion

            #region Conditions
            if (Session.GetHabbo().CurrentRoomId != 28) {
                Session.SendWhisper("¡Para robar el banco necesitas estar en a sala #28 y entrar en la boveda!", 1);
                return;
            }

            if (_stopRobCommand == true)
            {
                Session.SendWhisper("Has canceado el robo al banco.");
                //Session.GetRoleplay().bankRobTimer.EndTimer();
                Session.GetRoleplay().Robbery = false;
            }


            if (Session.GetRoleplay().PassiveMode)
            {
                Session.SendWhisper("No puedes realizar acciones ilegales en modo pasivo.", 1);
                return;
            }

            if (Session.GetRoleplay().Level <= 5)
            {
                Session.SendWhisper("¡Tienes que ser minimo nivel 5, para robar el banco!", 1);
                return;

            }

            if (Session.GetRoleplay().Robbery == true)

            {
                Session.SendWhisper("¡Ya estás robando el banco!");
                return;
            }

            if (Room.BankCapturing && Room.TurfUserAtackerId != Session.GetHabbo().Id)
            {
                Session.SendWhisper("¡Alguien más ya está robando la bóveda!");
                return;
            }

            if (RoleplayManager.VaultCooldowns.TryGetValue(Room.RoomId, out DateTime cooldownEnd))
            {
                if (DateTime.Now < cooldownEnd)
                {
                    TimeSpan timeLeft = cooldownEnd - DateTime.Now;
                    Session.SendWhisper("La bóveda ha sido robada recientemente. Debes esperar " + (int)timeLeft.TotalHours + " hora(s) y " + timeLeft.Minutes + " minuto(s) más.");
                    return;
                }
            }

            if (Session.GetRoleplay().GangId <= 0)
            {
                Session.SendWhisper("¡No perteneces a ninguna Pandilla para robar el banco!", 1);
                return;
            }

            if (Session.GetRoleplay().IsDead)
            {
                Session.SendWhisper("¡No puedes robar a alguien mientras estás muerto!", 1);
                return;
            }

           

            if (RoleplayManager.PurgeStarted)
            {
                Session.SendWhisper("¡Solo puedes robar la bovéda cuando no esta sellada! pero se ha activado la arma y la policía te busca. HUYE DE INMEDIATO", 1);
                return;
            }

            if (Session.GetRoleplay().IsJailed)
            {
                Session.SendWhisper("¡No puedes robar a alguien mientras estás en la cárcel!", 1);
                return;
            }

            if (Session.GetRoleplay().StaffOnDuty || Session.GetRoleplay().AmbassadorOnDuty)
            {
                Session.SendWhisper("¡No puedes robar a alguien mientras estás de servicio!", 1);
                return;
            }

            Group company = GroupManager.GetJob(9);
            if (company == null)
                return;

            if (company.Balance <= 0)
            {
                Session.SendWhisper("No se puede robar la bóveda como la bóveda fue completamente robada");
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

            if (Session.GetRoleplay().IsNoob)
            {
                Session.SendWhisper("¡No puedes completar esta acción mientras estás bajo la protección de Dios por ser nuevo!", 1);
                return;
            }

           /* if (Session.GetRoleplay().TryGetCooldown("robbery"))
                return;*/


            #endregion

            #region Execute
            
            Session.GetRoleplay().Robbery = true;
            Room.BankCapturing = true;
            Room.TurfUserAtackerId = Session.GetHabbo().Id;
            string RoomId = Session.GetHabbo().CurrentRoomId.ToString() != "0" ? Session.GetHabbo().CurrentRoomId.ToString() : "Unknown";

            Wanted NewWanted = new Wanted(Convert.ToUInt32(Session.GetHabbo().Id), RoomId, 3);
            RoleplayManager.Shout(Session, "*Empezo un robo al banco en la Boveda [-15] Energía*");
            Session.SendWhisper("Tienes 3 minutos Para terminar el robo, No dejes que nadie mas robe. ¡MATA AL QUE ENTRE!");

            Session.GetRoleplay().BankCapturing = true;
            Session.GetRoleplay().LoadingTimeLeft = RoleplayManager.BankCapTime;

            Session.GetRoleplay().WantedLevel = 3;
            Session.GetRoleplay().WantedTimeLeft = 10;
            Session.GetRoleplay().CurEnergy -= 20;

            RoleplayManager.WantedList.TryAdd(Session.GetHabbo().Id, NewWanted);
            Session.GetRoleplay().TimerManager.CreateTimer("general", 1000, true);

            if (Session.GetRoleplay().WebSocketConnection != null)
                PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(Session, "compose_wanted_stars|" + Session.GetRoleplay().WantedLevel);

            PolarEnvironment.GetGame().GetClientManager().JailAlert("[RADIO] Se ha activado la alarma de la boveda, acurdir de inmediato " + Session.GetHabbo().CurrentRoom.RoomData.Name + " posible robo, ir armados");
            
            
            #endregion
            /*
            #region Execute
            CryptoRandom Random = new CryptoRandom();
            Point ClientPos = new Point(RoomUser.X, RoomUser.Y);
            Point TargetClientPos = new Point(TargetUser.X, TargetUser.Y);
            double Distance = RoleplayManager.GetDistanceBetweenPoints2D(ClientPos, TargetClientPos);

            bool Success = false;
            string RobbedItems = "";
            int DrugsChance = Random.Next(1, 101);
            RoleplayManager.Shout(Session, "*Empezo un robo al banco*");
            Session.SendMessage("Uhh, La bobeda esta siendo robada " + Session.GetHabbo().CurrentRoom.RoomData.Name + " Esta ciendo robado! Respondan unidades!", Session, true);
            Success = true;
            if (!Success)
                {
                    Session.SendWhisper("Lo siento, pero esta persona es demasiado pobre para robar", 1);
                    return;
                }

                if (Success)
                {
                    if (!Session.GetRoleplay().WantedFor.Contains("robbing"))
                        Session.GetRoleplay().WantedFor = Session.GetRoleplay().WantedFor + "robbing, ";

                    Session.Shout("*Desliza sus manos hacia abajo de " + TargetClient.GetHabbo().Username + "'s en su patalón y de sus bolsillo roba " + RobbedItems.TrimEnd(',', ' ') + "*", 4);

                    Session.GetRoleplay().CooldownManager.CreateCooldown("robbery", 1000, 300);
                    Session.GetRoleplay().SpecialCooldowns.TryUpdate("robbery", 300, Session.GetRoleplay().SpecialCooldowns["robbery"]);
                }
 
            #endregion*/
        }
    }
}