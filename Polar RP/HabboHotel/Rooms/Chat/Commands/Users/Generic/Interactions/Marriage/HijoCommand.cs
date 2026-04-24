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
    class HijoCommand : IChatCommand
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
            get { return "Adoptar como hijo a otro usuario."; }
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
                Session.SendWhisper("¡No puedes ser hijo de ti mismo!", 1);
                return;
            }

            if (!Session.GetRoleplay().NearItem("hosptl_bed", 1))
            {
                Session.SendWhisper("¡Para dar a luz necesitas ir al hospital y acostarte en la camilla!");
                return;
            }

            if (Session.GetHabbo().CurrentRoomId != 2)
            {
                Session.SendWhisper("¡Para dar a luz debes ir al hospital!", 1);
                return;
            }

            RoomUser TargetUser = Room.GetRoomUserManager().GetRoomUserByHabbo(TargetClient.GetHabbo().Username);
            if (TargetUser == null)
            {
                Session.SendWhisper("Se ha producido un error al encontrar a ese usuario, tal vez no estén en línea o en esta sala.", 1);
                return;
            }

            if (Session.GetRoleplay().Hijo > 0)
            {
                Session.SendWhisper("¡Ya tienes un hijo! Debes abandonarlo para tener uno nuevo ¡MALA MADRE!", 1);
                return;
            }

            if (Session.GetRoleplay().Embarazo < 1)
            {
                Session.SendWhisper("¡Para dar a luz necesitas estar embarazada!", 1);
                return;
            }

            if (TargetClient.GetRoleplay().Hijo > 0)
            {
                Session.SendWhisper("Lo sentimos, pero este usuario ya tiene mamá", 1);
                return;
            }

            if (Session.GetHabbo().Credits < 10000)
            {
                Session.SendWhisper("Dar luz tiene un gasto de 10.0000$", 1);
                return;
            }

            if (TargetClient.GetRoleplay().OfferManager.ActiveOffers.ContainsKey("mama"))
            {
                Session.SendWhisper("¡Alguien debe haber ofrecido recientemente como hijo antes que usted! ¡Mejor suerte la próxima vez!", 1);
                return;
            }
            #endregion

            #region Execute
            TargetClient.GetRoleplay().OfferManager.CreateOffer("mama", Session.GetHabbo().Id, 0);
            TargetClient.SendWhisper(Session.GetHabbo().Username + " Acaba de ofrecerte que seas su hij@! ESCRIBE ':aceptar mama' para tener una mamá!", 34);
            Session.SendWhisper("¡Acabas de gastar 10.000$ en un anillo de bodas!", 1);
            Session.GetHabbo().Credits -= 10000;
            Session.GetHabbo().UpdateCreditsBalance();
            Session.Shout("*Comienza los dólores y contracciones para que salga el bebé: " + TargetClient.GetHabbo().Username + " ¡¡aaaaaaahh!!*", 16);
            #endregion
        }
    }
}