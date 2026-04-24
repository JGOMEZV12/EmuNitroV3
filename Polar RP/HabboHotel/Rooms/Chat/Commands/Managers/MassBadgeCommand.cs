using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.GameClients;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Managers
{
    class MassBadgeCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_give_badge_mass"; }
        }

        public string Parameters
        {
            get { return "%badge%"; }
        }

        public string Description
        {
            get { return "dar placa a todos."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("Por favor, introduzca el código de la insignia que le gustaría dar a toda la comunidad.", 1);
                return;
            }

            Badges.BadgeDefinition BadgeDefinition = null;
            if (!PolarEnvironment.GetGame().GetBadgeManager().TryGetBadge(Params[1].ToUpper(), out BadgeDefinition))
            {
                Session.SendWhisper("¡Las definiciones de la insignia no contienen esta insignia!", 1);
                return;
            }

            foreach (GameClient Client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
            {
                if (Client == null || Client.GetHabbo() == null || Client.GetHabbo().Username == Session.GetHabbo().Username)
                    continue;

                if (!Client.GetHabbo().GetBadgeComponent().HasBadge(Params[1]))
                {
                    Client.GetHabbo().GetBadgeComponent().GiveBadge(Params[1], true, Client);
                    Client.SendNotification("Te han enviado una placa, revisa tu inventario");
                }
                else
                    Client.SendWhisper(Session.GetHabbo().Username + " intentó darte una placa, ¡pero ya la tienes!", 1);
            }

            Session.SendWhisper("Le diste una placa a todos los conectados " + Params[1] + " ¡Enhorabuena!", 1);
        }
    }
}
