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
    class PonerchalecoCommand : IChatCommand
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
            get { return "Poner la cantidad de chaleco"; }
        }

        public async Task Execute(GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            RoomUser RoomUser = Session.GetRoomUser();
            int Cantidad = Convert.ToInt32(Params[1]);
            if (Session.GetRoleplay().IsDead)
            {
                Session.SendWhisper("¡No puedes usar esto estando muerto!", 1);
                return;
            }

            if (Session.GetRoleplay().IsJailed)
            {
                Session.SendWhisper("No puedes usar esto en la carcel", 1);
                return;
            }

            if (Params.Length < 1)
            {
                Session.SendWhisper("Ejecuta bien el comando :ponerchaleco %cantidad%");
                return;
            }

            if (Session.GetRoleplay().ChalecoPor > 0)
            {
                Session.SendWhisper("¡No puedes ponerte otro chaleco! ¿Que te pasa? antirolero! Te queda del chaleco: [" + Session.GetRoleplay().Armor + "]", 1);
                return;
            }

            if (Cantidad > 1)
            {
                Session.SendWhisper("No puedes ponerte más de 1 chaleco.");
                return;
            }

            if (Session.GetRoleplay().Armor < 0 || Session.GetRoleplay().Armor == 0)
            {
                Session.SendWhisper("No tienes más chalecos.");
                return;
            }



            #endregion

            #region Execute
            //Session.Shout("*Compra un chaleco Kevlar por y paga con su tarjeta de débito[-2000$]*", 4);
            Session.GetRoleplay().Armor -= 1;
            Session.SendWhisper("¡Se te ha colocado el equipo Kevlar! Te quedan: "+ Session.GetRoleplay().Armor+" chaleco(s)", 1);
            //Session.GetRoleplay().BankChequings-= 2000;
            //Session.GetRoomUser().ApplyEffect(603);
            Session.GetRoleplay().ChalecoPor = 100;
            Session.GetRoleplay().UpdateInteractingUserDialogues();
            Session.GetRoleplay().RefreshStatDialogue();
            //Session.GetHabbo().UpdateCreditsBalance();
            //Session.GetRoleplay().CurHealth = 300 * Cantidad;
            //Session.SendMessage(new WeaponsComposer(Session));
            HabboRoleplay.Misc.RoleplayManager.GetLookAndMotto(Session);
            #endregion
        }
    }
}