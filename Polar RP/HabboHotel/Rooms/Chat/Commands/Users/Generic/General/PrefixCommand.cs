using System;
using System.Linq;
using System.Threading.Tasks;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.General
{
    class PrefixCommand : IChatCommand
    {
        public string PermissionRequired => "command_prefix";
        public string Parameters => "[id]";
        public string Description => "Cambia tu prefijo activo.";

        public async Task Execute(GameClient Session, Room Room, string[] Params)
        {
            var prefixes = Session.GetHabbo().GetPrefixesComponent();
            if (prefixes == null)
            {
                Session.SendWhisper("Sistema de prefijos no disponible.");
                return;
            }

            if (Params.Length < 2)
            {
                var list = prefixes.GetPrefixes();
                if (list.Count == 0)
                {
                    Session.SendWhisper("No tienes prefijos disponibles.");
                    return;
                }

                Session.SendWhisper("Tus prefijos disponibles: " + string.Join(", ", list.Select(p => $"[{p.GetId()}] {p.GetText()}")));
                Session.SendWhisper("Usa :prefix [id] para activar uno, o :prefix off para desactivar.");
                return;
            }

            if (Params[1].ToLower() == "off")
            {
                prefixes.DeactivateAll();
                Session.SendWhisper("Has desactivado tu prefijo.");
                return;
            }

            if (int.TryParse(Params[1], out int id))
            {
                var p = prefixes.GetPrefix(id);
                if (p == null)
                {
                    Session.SendWhisper("No posees ese prefijo.");
                    return;
                }

                prefixes.SetActive(id);
                Session.SendWhisper("Has activado el prefijo: " + p.GetText());
            }
            else
            {
                Session.SendWhisper("ID de prefijo inválido.");
            }
        }
    }
}
