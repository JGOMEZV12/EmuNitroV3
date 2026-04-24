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
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic
{
    class LearningCommand : IChatCommand
    {
        private readonly bool _stopLearningCommand;
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
            get { return "Leer."; }
        }

        public LearningCommand(bool stopLearningCommand = false)
        {
            _stopLearningCommand = stopLearningCommand;
        }

        public async Task Execute(GameClient Session, Rooms.Room Room, string[] Params)
        {

            #region Conditions


            if (!Session.GetRoomUser().GetRoom().RoomData.LearningEnabled)
            {
                Session.SendWhisper("¡En está sala no se permite leer!..", 1);
                return;
            }

            if (Session.GetRoleplay().IsDead)
            {
                Session.SendWhisper("¡No puedes leer  mientras estás muerto!", 1);
                return;
            }

            if (Session.GetRoleplay().EquippedWeapon != null)
            {
                Session.SendWhisper("Esto es un territorio libre de armas, guardar tu arma para poder leer por favor.", 1);
                return;
            }

            if (Session.GetRoleplay().IsJailed)
            {
                Session.SendWhisper("¡No puedes leer mientras estás en la cárcel!", 1);
                return;
            }

            if (Session.GetRoleplay().BankChequings < 150)
            {
                Session.SendWhisper("¡No puedes leer porque no tienes 150$ en tu cuenta corriente", 1);
                return;
            }

            if (Session.GetRoleplay().Learning)
            {
                Session.SendWhisper("Ya está aprendiendo!");
                return;
            }

            if (Session.GetRoleplay().CurEnergy < 20)
            {
                Session.SendWhisper("No puedes estudiar porque no tienes energía suficiente, ve por algo de beber o consume algo que la suba");
                return;
            }

            if (Session.GetRoleplay().DrivingCar)
            {
                Session.SendWhisper("¡No puedes leer mientras conduces un vehículo!", 1);
                return;
            }

            if (Session.GetRoleplay().Intelligence >= RoleplayManager.IntelligenceCap)
            {
                Session.SendWhisper("Ya eres uno de los más inteligentes de la ciudad: " + RoleplayManager.IntelligenceCap + "!", 1);
                return;
            }


            if (Session.GetRoleplay().TryGetCooldown("learning"))
                return;

            if (!Session.GetRoleplay().MultiCoolDown.ContainsKey("learning"))
            {
                Session.GetRoleplay().MultiCoolDown.Add("learning", 0);
            }
            if (Session.GetRoleplay().MultiCoolDown["learning"] > 0)
            {
                Session.SendWhisper("Debe esperar hasta que pueda comenzar a aprender de nuevo! [" + Session.GetRoleplay().MultiCoolDown["learning"] + "/1]");
                return;
            }
            #endregion

            #region Execute
            RoomUser User = Session.GetRoomUser();
            if (User == null)
                return;

            User.CarryItem(1070);
            Session.GetRoleplay().Learning = true;
            Session.GetRoleplay().CurEnergy -= 20;
            Session.GetRoleplay().BankChequings -= 150;
            Session.GetRoleplay().LoadingTimeLeft = 30;
            Session.GetHabbo().UpdateCreditsBalance();
            Session.GetRoleplay().TimerManager.CreateTimer("general", 1000, false);
            RoleplayManager.Shout(Session, "* Empezo a leer, ya que necesita aumentar su capacidad intelectual [-20] Energía [-150$]  *");
            //Session.SendWhisper("Tienes " + Session.GetRoleplay().learningTimer.getTime() + " minutos para aprender mas.");
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