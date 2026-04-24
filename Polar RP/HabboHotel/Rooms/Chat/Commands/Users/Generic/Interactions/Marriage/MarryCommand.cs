using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Groups;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Weapons;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Interactions.Marriage
{
    class MarryCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_interactions_marry"; }
        }

        public string Parameters
        {
            get { return "%username%"; }
        }

        public string Description
        {
            get { return "Ofrece casarse con el usuario."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            if (Params.Length == 1)
            {
                Session.SendWhisper("Se ha producido un error al intentar encontrar a ese usuario, tal vez estén sin conexión.", 1);
                return;
            }

            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);
            if (TargetClient == null)
            {
                Session.SendWhisper("¡Uy, no pudo encontrar ese usuario!", 1);
                return;
            }

            if (Session == TargetClient)
            {
                Session.SendWhisper("¡No puedes casarte!", 1);
                return;
            }

            if (!Session.GetRoleplay().NearItem("val14_rosebook", 1))
            {
                Session.SendWhisper("¡Debes estar frente al libro del amor y en la iglesia SALAID: 8!");
                return;
            }

            if (!TargetClient.GetRoleplay().NearItem("val14_rosebook", 1))
            {
                Session.SendWhisper("¡Tu pareja debe estar sentad@ frente al libro del amor!");
                return;
            }

            if (Session.GetHabbo().CurrentRoomId != 8)
            {
                Session.SendWhisper("¡Para casarse deben estar en la Iglesia de " + PolarEnvironment.GetConfig().data["hotel.name"] + " (8) y sentarse en los sillones de novios!", 1);
                return;
            }

            RoomUser TargetUser = Room.GetRoomUserManager().GetRoomUserByHabbo(TargetClient.GetHabbo().Username);
            if (TargetUser == null)
            {
                Session.SendWhisper("Se ha producido un error al encontrar a ese usuario, tal vez no estén en línea o en esta sala.", 1);
                return;
            }

            if (Session.GetRoleplay().MarriedTo > 0)
            {
                Session.SendWhisper("¡Ya estás casado con alguien! ¡Divórtelos primero!", 1);
                return;
            }

            if (TargetClient.GetRoleplay().MarriedTo > 0)
            {
                Session.SendWhisper("Lo sentimos, pero este usuario ya está casado con otra persona", 1);
                return;
            }

            if (Session.GetHabbo().Credits < 10000)
            {
                Session.SendWhisper("No tienes 10.000$ para comprar un Anillo", 1);
                return;
            }

            if (TargetClient.GetRoleplay().OfferManager.ActiveOffers.ContainsKey("casarme"))
            {
                Session.SendWhisper("¡Alguien debe haber ofrecido recientemente casarlos antes de usted! ¡Mejor suerte la próxima vez!", 1);
                return;
            }
            #endregion

            #region Execute
            TargetClient.GetRoleplay().OfferManager.CreateOffer("casarme", Session.GetHabbo().Id, 0);
            TargetClient.SendWhisper(Session.GetHabbo().Username + " Acaba de ofrecer a casarse contigo! Tipo ':aceptar casarme' para casarse con el/ella!", 1);
            Session.SendWhisper("¡Acabas de gastar 10.000$ en un anillo de bodas!", 1);
            Session.GetHabbo().Credits -= 10000;
            Session.GetHabbo().UpdateCreditsBalance();
            Session.Shout("*Se pone de rodillas y pregunta " + TargetClient.GetHabbo().Username + " ¿Te casas conmigo?", 16);
            #endregion
        }
    }
}