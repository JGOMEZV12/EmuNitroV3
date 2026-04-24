using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.Communication.Packets.Outgoing.Notifications;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Police
{
    class BailCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_police_related_bail"; }
        }

        public string Parameters
        {
            get { return "%user%"; }
        }

        public string Description
        {
            get { return "Le permite liberar a otro ciudadano que está actualmente en la cárcel."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Variables
            int BailCost = 0;
            #endregion

            #region Conditions
            if (Params.Length == 1)
            {
                Session.SendWhisper("Vaya, se le olvidó ingresar un nombre de usuario!", 1);
                return;
            }

            GameClients.GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);
            if (TargetClient == null)
            {
                Session.SendWhisper("Se ha producido un error al intentar encontrar a ese usuario, tal vez están sin conexión.", 1);
                return;
            }

            if(TargetClient.GetHabbo().Username == Session.GetHabbo().Username)
            {
                Session.SendWhisper("¿No puedes pagarte la fianza tu mismo!", 1);
                return;
            }

            RoomUser RoomUser = Session.GetRoomUser();
            RoomUser TargetUser = Room.GetRoomUserManager().GetRoomUserByHabbo(TargetClient.GetHabbo().Username);
            if (TargetUser == null)
            {
                Session.SendWhisper("Se ha producido un error al encontrar a ese usuario, tal vez no estén en línea o en esta sala.", 1);
                return;
            }

            if (!TargetClient.GetRoleplay().IsJailed)
            {
                Session.SendWhisper("¡No puedes liberar a alguien que no está en la cárcel!", 1);
                return;
            }

            if (TargetUser.IsAsleep)
            {
                Session.SendWhisper("No puedes hacer esto dormido o alguien que este dormir", 1);
                return;
            }

            if (TargetClient == RoleplayManager.Defendant)
            {
                Session.SendWhisper("¡No puedes liberar a alguien que esté solicitando un juicio en la corte!", 1);
                return;
            }

            BailCost = 50000;

            if (Session.GetHabbo().Credits < BailCost)
            {
                Session.SendWhisper("Usted no puede permitirse el lujo de fianza " + TargetClient.GetHabbo().Username + " ¡fuera de la carcel! Te costará $" + String.Format("{0:N0}", BailCost) + "!", 1);
                return;
            }

            if (TargetClient.GetRoomUser().RoomId != Session.GetRoomUser().RoomId)
            {
                Session.SendWhisper(TargetClient.GetHabbo().Username + " ¡Ni siquiera está en la misma habitación que tú!", 1);
                return;
            }

            /*if (!GroupManager.GetJob(Session.GetRoleplay().JobId).Name.Contains("Justicia"))
            {
                Session.SendWhisper("¡No perteneces al Ministerio de juticia!.", 1);
                return;
            }*/

            /*if (!GroupManager.HasJobCommand(Session, "fianza"))
            {
                Session.SendWhisper("¡Tienes que ser abogado o juez para poder permitir la fianza!", 1);
                return;
            }*/

            #endregion

            #region Execute
            if (Params.Length >= 3)
            {
                if (Params[2].ToString().ToLower() == "yes")
                {
                    Session.Shout("*Paga por " + TargetClient.GetHabbo().Username + "'s para liberarlo unos $" + String.Format("{0:N0}", BailCost) + ", Liberándolo de la cárcel en libertad condicional*", 4);

                    Session.GetHabbo().Credits -= BailCost;
                    Session.GetHabbo().UpdateCreditsBalance();

                    TargetClient.GetRoleplay().IsJailed = false;
                    TargetClient.GetRoleplay().JailedTimeLeft = 0;
                   
                }
                else
                {
                    Session.SendWhisper("Si realmente quieres pagar $" + String.Format("{0:N0}", BailCost) + " para liberar a " + TargetClient.GetHabbo().Username + ", di :fianza " + TargetClient.GetHabbo().Username + " yes.", 1);
                    return;
                }
            }
            else
            {
                Session.SendWhisper("Si realmente quieres pagar $" + String.Format("{0:N0}", BailCost) + " to bail " + TargetClient.GetHabbo().Username + ", escribe :fianza " + TargetClient.GetHabbo().Username + " yes.", 1);
                return;
            }
            #endregion
        }
    }
}