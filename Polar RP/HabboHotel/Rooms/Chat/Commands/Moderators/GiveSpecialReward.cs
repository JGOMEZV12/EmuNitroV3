using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Rooms.Nux;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Moderators
{
    class GiveSpecialReward : IChatCommand
    {
        public string PermissionRequired => "command_give_coins";
        public string Parameters => "%usuario$";
        public string Description => "";

        public async Task Execute(GameClient Session, Room Room, string[] Params)
        {
            if (Params.Length == 0)
            {
                Session.SendWhisper("Por favor introduce un nombre de usuario para premiar.", 34);
                return;
            }

            GameClient Target = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);
            if (Target == null)
            {
                Session.SendWhisper("Oops, No se ha conseguido este usuario!");
                return;
            }

            Target.SendMessage(new NuxItemListComposer());
            Session.SendWhisper("Has activado correctamente el premio especial para " + Target.GetHabbo().Username, 34);
        }
    }
}