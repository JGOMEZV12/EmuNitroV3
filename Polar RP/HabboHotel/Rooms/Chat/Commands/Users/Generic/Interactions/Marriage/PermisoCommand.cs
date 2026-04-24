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
    class PermisoCommand : IChatCommand
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
            get { return "Ofrece al comprador porte de armas."; }
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

            if (!GroupManager.HasJobCommand(Session, "permiso"))
            {
                Session.SendWhisper("Necesitas ser policía o vendedor en la tienda de armas", 1);
                return;
            }

            if (!Session.GetRoleplay().NearItem("computer_flatscreen", 1))
            {
                Session.SendWhisper("¡Debes estar frente a una PC para poder tramitar el permiso!");
                return;
            }

            if (Session.GetHabbo().CurrentRoomId != 4 && Session.GetHabbo().CurrentRoomId != 6)
            {
                Session.SendWhisper("¡Para ofrecer un porte de armas debes estar en la 4 [Departamento de policia] o 6 [Tienda de armas]!", 1);
                return;
            }

            RoomUser TargetUser = Room.GetRoomUserManager().GetRoomUserByHabbo(TargetClient.GetHabbo().Username);
            if (TargetUser == null)
            {
                Session.SendWhisper("Se ha producido un error al encontrar a ese usuario, tal vez no estén en línea o en esta sala.", 1);
                return;
            }

            if (TargetClient.GetHabbo().Credits < 50000)
            {
                Session.SendWhisper("Este usuario no tiene 50.000$ para pagar permiso de armas", 1);
                return;
            }

            if (TargetClient.GetRoleplay().OfferManager.ActiveOffers.ContainsKey("permiso"))
            {
                Session.SendWhisper("¡Alguien debe haber ofrecido recientemente un permiso antes de usted! ¡Mejor suerte la próxima vez!", 1);
                return;
            }
            #endregion

            #region Execute
            TargetClient.GetRoleplay().OfferManager.CreateOffer("permiso", Session.GetHabbo().Id, 0);
            TargetClient.SendWhisper(Session.GetHabbo().Username + " Acaba de ofrecer un permiso para portar armas escriba ':aceptar permiso' para comprarlo!", 4);
            Session.Shout("*Sr/Sra " + TargetClient.GetHabbo().Username + " firme aquí para que obtenga su permiso legal para portar armas ¡Tenga cuidado de su uso!", 4);
            #endregion
        }
    }
}