using Polar.Communication.Packets.Outgoing.Inventory.Weapons;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.RoleplayUsers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Jobs.Types.Hospital
{
    class ComprarChalecoCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_hospital_heal"; }
        }

        public string Parameters
        {
            get { return "%cantidad%"; }
        }

        public string Description
        {
            get { return "Compra un chaleco antibalas en la tienda de armas"; }
        }

        public async Task Execute(GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            RoomUser RoomUser = Session.GetRoomUser();

            // Check if Params has at least 2 elements before accessing Params[1]
            if (Params.Length < 2)
            {
                Session.SendWhisper("Ejecuta bien el comando :comprarchaleco %cantidad%");
                return;
            }

            int Cantidad = 0;

            // Try to parse the quantity, handle invalid input
            if (!int.TryParse(Params[1], out Cantidad))
            {
                Session.SendWhisper("La cantidad debe ser un número válido.");
                return;
            }

            // Rest of your conditions...
            if (Session.GetHabbo().CurrentRoomId != 6)
            {
                Session.SendWhisper("¡El chaleco solo se compra en la tienda de armas Dirección: [BARRIO] Av. Smelly [22]!", 1);
                return;
            }

            if (Cantidad < 1)
            {
                Session.SendWhisper("Debes comprar al menos 1 chaleco.");
                return;
            }

            if (Cantidad > 10)
            {
                Session.SendWhisper("No puedes comprar más de 10 chalecos.");
                return;
            }

            if (Session.GetRoleplay().Armor == 60 || Session.GetRoleplay().Armor > 1)
            {
                Session.SendWhisper("Actualmente tienes " + Session.GetRoleplay().Armor + " chaleco(s). Usalos para poder comprar otros.", 1);
                return;
            }

            if (Session.GetRoleplay().BankChequings < (2000 * Cantidad))
            {
                Session.SendWhisper($"Necesitas tener {2000 * Cantidad}$ en tu cuenta bancaria para poder comprar {Cantidad} chaleco(s).", 1);
                return;
            }

            #endregion

            #region Execute
            int precio = 2000 * Cantidad;
            Session.Shout($"*Compra {Cantidad} chaleco(s) Kevlar por y paga con su tarjeta de débito[-{precio}$]*", 4);
            Session.GetRoleplay().BankChequings -= precio;
            Session.GetRoomUser().ApplyEffect(603);
            Session.GetRoleplay().Armor = Cantidad;
            Session.SendMessage(new WeaponsComposer(Session));
            Session.GetRoleplay().UpdateInteractingUserDialogues();
            Session.GetRoleplay().RefreshStatDialogue();
            Session.GetHabbo().UpdateCreditsBalance();

            #region Bank Company Balance
            RoleplayManager.GiveMoneyToCompany(5, Session, "Ammor", true, precio);
            #endregion
            #endregion
        }
    }
}