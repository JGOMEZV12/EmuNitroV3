using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Jobs.Types.Hospital
{
    class ChalecopoliciaCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_hospital_heal"; }
        }

        public string Parameters
        {
            get { return "%user%"; }
        }

        public string Description
        {
            get { return "Colocate un chaleco ANTIBALAS ESPECIAL DE POLICIA"; }
        }

        public async Task Execute(GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            RoomUser RoomUser = Session.GetRoomUser();
            if (Session.GetHabbo().CurrentRoomId != 98)
            {
                Session.SendWhisper("¡Si eres policía debes buscarlo en la central de policías [4]!", 1);
                return;
            }

            if (!GroupManager.HasJobCommand(Session, "stun") && !Session.GetRoleplay().PoliceTrial)
            {
                Session.SendWhisper("Sólo un oficial de policía puede utilizar este comando", 1);
                return;
            }

            if (!Session.GetRoleplay().IsWorking && !Session.GetRoleplay().PoliceTrial)
            {
                Session.SendWhisper("¡Debes estar trabajando para usar este comando!", 1);
                return;
            }

            if (Session.GetRoleplay().CurHealth > 300)
            {
                Session.SendWhisper("¡No puedes ponerte un chaleco sobre otro! ¿Que te pasa? antirolero!", 1);
                return;
            }

            if (Session.GetRoleplay().CurHealth < 100)
            {
                Session.SendWhisper("Debes estar saludable para cargar el peso del Kevlar, ve al hospital o consume algo", 1);
                return;
            }

            if (Session.GetRoleplay().BankChequings < 500)
            {
                Session.SendWhisper("Necesitas tener 500$ en tu cuenta bancaria para poder pedir un chaleco ¡Trabaja!", 1);
                return;
            }

            #endregion

            #region Execute
            Session.Shout("*Agarra un chaleco del departamento de policía y se lo coloca*", 4);
            Session.SendWhisper("¡Se te ha colocado el equipo Kevlar patrocinado por la policía!", 1);
            Session.GetRoleplay().BankChequings-= 500;
            Session.GetRoomUser().ApplyEffect(603);
            Session.GetHabbo().UpdateCreditsBalance();
            Session.GetRoleplay().CurHealth = 320;
            Session.GetRoleplay().Armor = 100;
            HabboRoleplay.Misc.RoleplayManager.GetLookAndMotto(Session, "poof");
            #endregion
        }
    }
}