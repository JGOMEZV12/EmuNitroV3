using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Groups;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Weapons;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Jobs.Types.Clothing
{
    class DiscountCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_jobs_discount"; }
        }

        public string Parameters
        {
            get { return "%user%"; }
        }

        public string Description
        {
            get { return "Proporciona un descuento a un comprador en la tienda de ropa, al comprar ropa."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            if (Params.Length == 1)
            {
                Session.SendWhisper("Por favor ingrese el nombre de usuario del comprador");
                return;
            }

            GameClient Target = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);
            if (Target == null)
            {
                Session.SendWhisper("¡Uy, no pudo encontrar ese usuario!");
                return;
            }

            RoomUser TargetUser = Room.GetRoomUserManager().GetRoomUserByHabbo(Target.GetHabbo().Username);
            if (TargetUser == null)
            {
                Session.SendWhisper("Se ha producido un error al encontrar a ese usuario, tal vez no estén en línea o en esta sala.", 1);
                return;
            }

            if (!GroupManager.HasJobCommand(Session, "discount") && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
            {
                Session.SendWhisper("Lo siento, ¡no trabajas en la corporación de tiendas de ropa!", 1);
                return;
            }

            if (!Session.GetRoleplay().IsWorking && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
            {
                Session.SendWhisper("Usted debe estar trabajando para ofrecer a alguien un descuento de ropa", 1);
                return;
            }

            if (Target.GetRoleplay().OfferManager.ActiveOffers.ContainsKey("clothing"))
            {
                Session.SendWhisper("Este usuario ya se ha ofrecido un descuento en la tienda de ropa", 1);
                return;
            }
            #endregion

            #region Execute
            Session.Shout("*Ofrece un descuento a " + Target.GetHabbo().Username + " Del 5% al comprar un artículo de ropa en la tienda*", 4);
            Target.GetRoleplay().OfferManager.CreateOffer("clothing", Session.GetHabbo().Id, 0);
            return;
            #endregion
        }
    }
}