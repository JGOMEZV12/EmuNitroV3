using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboRoleplay.Turfs;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Pathfinding;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.Groups;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Gangs
{
    class GangInviteCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_gang_invite"; }
        }

        public string Parameters
        {
            get { return "%username%"; }
        }

        public string Description
        {
            get { return "Invita a un ciudadano a tu pandilla."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Variables
            Group Gang = GroupManager.GetGang(Session.GetRoleplay().GangId);
            #endregion

            #region Conditions
            if (Params.Length == 1)
            {
                Session.SendWhisper("¡Ingrese el nombre de usuario del usuario que desea invitar a su pandilla!", 1);
                return;
            }

            if (Gang == null)
            {
                Session.SendWhisper("¡No tienes una pandilla para invitar a alguien!", 1);
                return;
            }

            if (Gang.Id <= 1000)
            {
                Session.SendWhisper("¡No tienes una pandilla para invitar a alguien!", 1);
                return;
            }

            if (!GroupManager.HasGangCommand(Session, "ginvite"))
            {
                Session.SendWhisper("¡No eres lo suficientemente alto para usar este comando!", 1);
                return;
            }

            if (Session.GetRoleplay().IsDead)
            {
                Session.SendWhisper("¡No puedes invitar a alguien a tu pandilla mientras estás muerto!", 1);
                return;
            }

            if (Session.GetRoleplay().IsJailed)
            {
                Session.SendWhisper("¡No puedes invitar a alguien a tu pandilla mientras estás en la cárcel!", 1);
                return;
            }

            GameClients.GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);

            if (TargetClient == null)
            {
                Session.SendWhisper("Se produjo un error al intentar encontrar a ese usuario, tal vez están fuera de línea.", 1);
                return;
            }

            if (Session == TargetClient)
            {
                Session.SendWhisper("¡No puedes invitarte a tu propia pandilla!", 1);
                return;
            }

            RoomUser TargetUser = Room.GetRoomUserManager().GetRoomUserByHabbo(TargetClient.GetHabbo().Username);
            if (TargetUser == null)
            {
                Session.SendWhisper("Se produjo un error al encontrar a ese usuario, tal vez no están en línea o en esta sala.", 1);
                return;
            }

            if (TargetClient.GetRoleplay().GangId > 1000)
            {
                Session.SendWhisper("Lo sentimos, ¡este usuario ya es parte de una pandilla!", 1);
                return;
            }

            if (TargetClient.GetRoleplay().OfferManager.ActiveOffers.ContainsKey("pandilla"))
            {
                var Invite = TargetClient.GetRoleplay().OfferManager.ActiveOffers["pandilla"];
                var InviteGang = GroupManager.GetGang(Invite.Cost);

                if (InviteGang != Gang)
                    Session.SendWhisper("Lo sentimos, este usuario ya ha sido invitado a '" + InviteGang.Name + "'! ¡Dales la oportunidad de responder!", 1);
                else
                    Session.SendWhisper("¡Ya has invitado a este usuario a tu pandilla! ¡Dales la oportunidad de responder!", 1);
                return;
            }

            if (Session.GetRoleplay().TryGetCooldown("ginvite"))
                return;
            #endregion

            #region Execute
            Session.GetRoleplay().CooldownManager.CreateCooldown("ginvite", 1000, 5);
            Session.Shout("*Invita a " + TargetClient.GetHabbo().Username + " unirse a la pandilla: '" + Gang.Name + "'*", 4);
            TargetClient.SendWhisper("Para unirte a la pandilla: '" + Gang.Name + "' escribe ':aceptar pandilla' para unirte debes aportar 2.000$ para gastos", 34);
            TargetClient.GetRoleplay().OfferManager.CreateOffer("pandilla", Session.GetHabbo().Id, Gang.Id);
            #endregion
        }
    }
}