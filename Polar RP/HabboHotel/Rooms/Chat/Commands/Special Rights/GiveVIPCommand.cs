using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polar.Communication.Packets.Outgoing.Users;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;

namespace Polar.HabboHotel.Rooms.Chat.Commands.SpecialRights
{
    class GiveVIPCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_give_vip"; }
        }

        public string Parameters
        {
            get { return "%username%"; }
        }

        public string Description
        {
            get { return "Dar vip a un usuario."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length != 3)
            {
                Session.SendWhisper("Debe ingresar el nombre de usuario de la persona y los dias a la que desea otorgar VIP.", 1);
                return;
            }

            var TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);

            if (TargetClient == null)
            {
                Session.SendWhisper("¡Este usuario no pudo ser encontrado! Tal vez están fuera de línea.", 1);
                return;
            }

            if (TargetClient.GetHabbo() == null || TargetClient.GetRoomUser() == null)
            {
                Session.SendWhisper("¡Este usuario no pudo ser encontrado!Tal vez están fuera de línea.", 1);
                return;
            }

            if (TargetClient.GetHabbo().VIPRank > 0)
            {
                Session.SendWhisper("¡Lo siento, este usuario ya tiene VIP!", 1);
                return;
            }

            if (Convert.ToInt32(Params[2]) <= 0)
            {
                Session.SendWhisper("El monto no puede ser 0 o menos que eso.", 1);
                return;
            }
            int cAmount = 500;
            int dAmount = 25;
            int num = Convert.ToInt32(Params[2]) * 1;


            TargetClient.GetHabbo().Credits += cAmount;
            TargetClient.GetHabbo().UpdateCreditsBalance();

            TargetClient.GetHabbo().Diamonds += dAmount;
            TargetClient.GetHabbo().UpdateDiamondsBalance(dAmount);

            TargetClient.SendNotification("Te acaban de dar VIP");
            TargetClient.GetHabbo().GetClubManager().AddOrExtendSubscription("habbo_vip", num * 24 * 3600, TargetClient);
            TargetClient.GetHabbo().GetBadgeComponent().GiveBadge("HC1", true, TargetClient);
            //TargetClient.SendMessage(new UserNameChangeComposer(TargetClient.GetRoomUser().GetRoom().Id, TargetClient.GetRoomUser().VirtualId, "[VIP] " + TargetClient.GetHabbo().Username));
            TargetClient.GetHabbo().GetPermissions().Init(TargetClient.GetHabbo());
            TargetClient.SendMessage(new ScrSendUserInfoComposer(TargetClient.GetHabbo()));
            TargetClient.GetHabbo().GetClubManager().ReloadSubscription(TargetClient);

            if (TargetClient.GetHabbo().VIPRank < 1)
            {
                TargetClient.GetHabbo().VIPRank = 1;
                TargetClient.GetHabbo().NameColor = "B53F3F";
                using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                    dbClient.RunQuery("UPDATE `users` SET `rank_vip` = '1', `colour` = 'B53F3F' WHERE `id` = '" + TargetClient.GetHabbo().Id + "'");

            }

            Session.SendWhisper("Diste correctaente el VIP " + TargetClient.GetHabbo().Username + " ¡Enhorabuena!", 1);
        }
    }
}
