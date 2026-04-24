using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.Communication.Packets.Outgoing.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.HabboHotel.Rooms.Chat.Commands.SpecialRights
{
    class TakeVIPCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_give_vip_undo"; }
        }

        public string Parameters
        {
            get { return "%username%"; }
        }

        public string Description
        {
            get { return "Toma VIP a los usuarios si ya lo tienen."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length != 2)
            {
                Session.SendWhisper("Debes ingresar el nombre de usuario de la persona desde la que deseas recibir VIP.", 1);
                return;
            }

            var TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);

            if (TargetClient == null)
            {
                Session.SendWhisper("¡Este usuario no pudo ser encontrado!Tal vez están fuera de línea.", 1);
                return;
            }

            if (TargetClient.GetHabbo() == null || TargetClient.GetRoomUser() == null)
            {
                Session.SendWhisper("¡Este usuario no pudo ser encontrado!Tal vez están fuera de línea.", 1);
                return;
            }

            if (TargetClient.GetHabbo().VIPRank == 0)
            {
                Session.SendWhisper("¡Lo siento, este usuario no tiene VIP para quitar!", 1);
                return;
            }

            TargetClient.GetHabbo().VIPRank = 0;
            TargetClient.GetHabbo().NameColor = "";
            TargetClient.SendNotification("Tu VIP se ha llevado " + Session.GetHabbo().Username);
            TargetClient.GetHabbo().GetPermissions().Init(TargetClient.GetHabbo());
            TargetClient.SendMessage(new UserNameChangeComposer(TargetClient.GetRoomUser().GetRoom().Id, TargetClient.GetRoomUser().VirtualId, TargetClient.GetHabbo().Username));
            TargetClient.GetRoleplay().MaxHealth = TargetClient.GetRoleplay().MaxHealth - 150;
            TargetClient.GetRoleplay().MaxEnergy = TargetClient.GetRoleplay().MaxEnergy - 150;
            TargetClient.GetHabbo().GetPermissions().Init(TargetClient.GetHabbo());

            Session.SendMessage(new ScrSendUserInfoComposer(Session.GetHabbo()));
            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_purse", "hc");
            using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.RunQuery("UPDATE `users` SET `rank_vip` = '0', `colour` = '' WHERE `id` = '" + TargetClient.GetHabbo().Id + "'");
                dbClient.RunQuery("DELETE FROM `user_subscriptions` WHERE `user_id` = '" + TargetClient.GetHabbo().Id + "'");
            }
            Session.SendWhisper("Has tomado con éxito " + TargetClient.GetHabbo().Username + "'s VIP!", 1);
        }
    }
}
