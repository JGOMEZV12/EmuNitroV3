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
using Polar.HabboRoleplay.Timers;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users
{
    class FugaCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_fuga"; }
        }

        public string Parameters
        {
            get { return "%user%"; }
        }

        public string Description
        {
            get { return "Ofrece a un usuario encarcelado fugarse de la cárcel."; }
        }

        public async Task Execute(GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            if (Params.Length != 2)
            {
                Session.SendWhisper("Uso correcto: :fuga usuario", 1);
                return;
            }

            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);
            if (TargetClient == null || TargetClient.GetHabbo() == null || TargetClient.GetRoleplay() == null)
            {
                Session.SendWhisper("Se ha producido un error al intentar encontrar a ese usuario, tal vez esté sin conexión.", 1);
                return;
            }

            if (TargetClient.GetHabbo().Id == Session.GetHabbo().Id)
            {
                Session.SendWhisper("¡No puedes ofrecerte una fuga a ti mismo!", 1);
                return;
            }

            if (!TargetClient.GetRoleplay().IsJailed)
            {
                Session.SendWhisper("¡Ese usuario no está encarcelado! Solo puedes ofrecer fugas a personas en la cárcel.", 1);
                return;
            }

            if (TargetClient.GetRoleplay().PassiveMode)
            {
                Session.SendWhisper("¡No puedes ofrecerle una fuga a una persona en modo pasivo!", 1);
                return;
            }

            if (TargetClient.GetRoomUser() != null && TargetClient.GetRoomUser().IsAsleep)
            {
                Session.SendWhisper("¡No puedes ofrecerle una fuga a alguien que no está activo en el juego!", 1);
                return;
            }

            if (TargetClient.GetRoleplay().OfferManager.ActiveOffers.ContainsKey("fuga"))
            {
                Session.SendWhisper("¡Esta persona ya tiene una oferta de fuga pendiente!", 1);
                return;
            }
            #endregion

            #region Execute
            RoleplayManager.Shout(Session, "*Se acerca sigilosamente y le susurra una propuesta de fuga a " + TargetClient.GetHabbo().Username + "*", 5);
            TargetClient.GetRoleplay().OfferManager.CreateOffer("fuga", Session.GetHabbo().Id, 3000);
            TargetClient.SendWhisper(Session.GetHabbo().Username + " te ofrece ayudarte a fugarte de la cárcel. Escribe ':aceptar fuga' para aceptar o ':rechazar fuga' para rechazar.", 1);
            Session.SendWhisper("Le has ofrecido una fuga a " + TargetClient.GetHabbo().Username + ". Esperando su respuesta...", 1);
            #endregion
        }
    }
}