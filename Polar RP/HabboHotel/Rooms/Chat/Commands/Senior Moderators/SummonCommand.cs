using System;
using System.Linq;
using System.Threading;
using System.Text;
using System.Collections.Generic;

using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Rooms.Session;
using Polar.Communication.Packets.Outgoing.Navigator;
using Polar.Communication.Packets.Outgoing.Notifications;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Moderators.Seniors
{
    class SummonCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_summon"; }
        }

        public string Parameters
        {
            get { return "%username%"; }
        }

        public string Description
        {
            get { return "trae a un usuario."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("coloque el nombre del usuario", 1);
                return;
            }

            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);
            if (TargetClient == null || TargetClient.GetHabbo() == null)
            {
                Session.SendWhisper("Se ha producido un error al encontrar a ese usuario, tal vez no están en línea.", 1);
                return;
            }

            if (TargetClient.GetRoleplay().IsDead)
            {
                TargetClient.GetRoleplay().IsDead = false;
                TargetClient.GetRoleplay().ReplenishStats(true);
                TargetClient.GetHabbo().Poof();
            }

            if (TargetClient.GetRoleplay().IsJailed)
            {
                TargetClient.GetRoleplay().IsJailed = false;
                TargetClient.GetRoleplay().JailedTimeLeft = 0;
                TargetClient.GetHabbo().Poof();
            }

            if (TargetClient.GetRoleplay().TurfCapturing)
            {
                Session.SendWhisper("Esa persona se encuentra capturando un barrio. Espera a que termine o síguela.", 1);
                return;
            }

            Session.Shout("*Usa sus poderes para trae a " + TargetClient.GetHabbo().Username + " a esta sala*", 23);
            TargetClient.GetHabbo().RoomAuthOk = true;
            TargetClient.GetHabbo().PrepareApartment(Room.Id, "");
            TargetClient.SendNotification("Has sido atraíd@ por " + Session.GetHabbo().Username);
            //RoleplayManager.SendUser(TargetClient, Room.Id, "Te trajo a esta sala: " + Session.GetHabbo().Username + "!");
        }
    }
}