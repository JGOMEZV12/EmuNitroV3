using Polar.HabboHotel.GameClients;

namespace Polar.HabboHotel.Rooms.Chat.Commands.User
{
    class MapCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_map"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Abre el mapa de la ciudad."; }
        }

        public async Task Execute(GameClient Session, Room Room, string[] Params)
        {
            // Enviamos WS de ventana del mapa
            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_mapa", "");
        }
    }
}
