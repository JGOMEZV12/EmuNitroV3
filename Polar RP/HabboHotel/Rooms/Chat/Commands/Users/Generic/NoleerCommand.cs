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
    class NoleerCommand : IChatCommand
    {
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

        public async Task Execute(GameClient Session, Rooms.Room Room, string[] Params)
        {

            #region Conditions


            if (Session.GetHabbo().CurrentRoomId != 9)
            {
                Session.SendWhisper("¡No estas en la biblioteca y tampoco estas leyendo #9!", 1);
                return;
            }
            #endregion

            #region Execute
            RoomUser User = Session.GetRoomUser();
            if (User == null)
                return;

            User.CarryItem(0);
            Session.GetRoleplay().Learning = false;
            RoleplayManager.Shout(Session, "* Termina de leer, ve por una cerveza o algo de beber, ya que estas cansado *");
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