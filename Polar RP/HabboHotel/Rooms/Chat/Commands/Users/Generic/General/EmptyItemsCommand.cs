using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.Communication.Packets.Outgoing.Inventory.Furni;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.General
{
    class EmptyItemsCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_general_empty_items"; }
        }

        public string Parameters
        {
            get { return "%yes%"; }
        }

        public string Description
        {
            get { return "¿Está lleno el inventario? Puede eliminar todos los elementos escribiendo este comando."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendNotification("¿Está seguro de que desea borrar su inventario? Usted perderá todos los furnis\n" +
                 "Para confirmar, escriba \":emptyitems yes \". \n \nCuando hagas esto, no hay vuelta atrás! \n (¡Si no quieres vaciarlo, solo ignora este mensaje!)\n\n" +
                 "¡TENGA EN CUENTA! Si tiene más de 3000 artículos, Los elementos ocultos también serán eliminados.");
                return;
            }
            else
            {
                if (Params.Length == 2 && Params[1].ToString() == "yes")
                {
                    Session.GetHabbo().GetInventoryComponent().ClearItems();
                    Session.SendWhisper("Su inventario ha sido borrado", 1);   
                    return;
                }
                else if (Params.Length == 2 && Params[1].ToString() != "yes")
                {
                    Session.SendWhisper("Para confirmar, debe escribir :emptyitems yes", 1);
                    return;
                }
            }
        }
    }
}
