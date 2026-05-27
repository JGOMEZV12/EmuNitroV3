using System;
using System.Threading;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Combat;
using Polar.HabboRoleplay.Weapons;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Bots;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Combat
{
    class HitCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_combat_hit"; }
        }

        public string Parameters
        {
            get { return "%user%"; }
        }

        public string Description
        {
            get { return "Lanza un puñetazo al usuario de destino."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.GetRoleplay().LastCommand = ":golpe";
                CombatManager.GetCombatType("fist").Execute(Session, null, true);
                return;
            }

            // New Target System
            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(((Session.GetRoleplay().Target != "" && Params[1] == "x") || Session.GetRoleplay().TargetLock) ? Session.GetRoleplay().Target : Params[1]);

            if (TargetClient == null)
            {
                RoomUser Bot = Room.GetRoomUserManager().GetBotByName(Params[1]);

                if (Bot != null && Bot.GetBotRoleplay() != null)
                {
                    Session.GetRoleplay().LastCommand = ":golpe " + Params[1];
                    CombatManager.GetCombatType("fist").ExecuteBot(Session, Bot.GetBotRoleplay());
                    return;
                }

                Session.GetRoleplay().LastCommand = ":golpe " + Params[1];
                Session.SendWhisper("Se ha producido un error al intentar encontrar a ese usuario, tal vez estén sin conexión.", 1);
                return;
            }

            if (Session.GetRoleplay().PassiveMode)
            {
                Session.SendWhisper("No puedes agredir en modo pasivo.", 1);
                return;
            }

            if (Session.GetRoleplay().CombatMode)
            {
                Session.SendWhisper("No tienes el modo combate activado", 1);
                return;
            }

            if (TargetClient.GetRoleplay().Cuffed)
            {
                Session.SendWhisper("¡No puedes golpear a una persona esposada!", 1);
                return;
            }
            if (TargetClient.GetRoleplay().IsDead)
            {
                Session.SendWhisper("¡No puedes golpear a una persona muerta!", 1);
                return;
            }
            if (TargetClient.GetRoleplay().IsJailed)
            {
                Session.SendWhisper("¡No puedes golpear a una persona encarcelada!", 1);
                return;
            }

            if (Room == null)
            {
                Session.GetRoleplay().LastCommand = ":golpe " + Params[1];
                Session.SendWhisper("Se ha producido un error al encontrar a ese usuario, tal vez no estén en línea o en esta sala.", 1);
                return;
            }

            if (TargetClient == null)
            {
                Session.GetRoleplay().LastCommand = ":golpe " + Params[1];
                Session.SendWhisper("Se ha producido un error al encontrar a ese usuario, tal vez no estén en línea o en esta sala.", 1);
                return;
            }

            if (TargetClient.GetHabbo() == null)
            {
                Session.GetRoleplay().LastCommand = ":golpe " + Params[1];
                Session.SendWhisper("Se ha producido un error al encontrar a ese usuario, tal vez no estén en línea o en esta sala.", 1);
                return;
            }

            RoomUser TargetUser = Room.GetRoomUserManager().GetRoomUserByHabbo(TargetClient.GetHabbo().Username);
            if (TargetUser == null)
            {
                Session.GetRoleplay().LastCommand = ":golpe " + Params[1];
                Session.SendWhisper("Se ha producido un error al encontrar a ese usuario, tal vez no estén en línea o en esta sala.", 1);
                return;
            }

            if (TargetClient.GetRoomUser().Frozen == true)
            {
                Session.SendWhisper("No se puede golpear a alguien que esta aturdido.", 1);
                return;
            }

            // New Target System
            Session.GetRoleplay().Target = TargetClient.GetHabbo().Username;

            Session.GetRoleplay().LastCommand = ":golpe " + Params[1];
            CombatManager.GetCombatType("fist").Execute(Session, TargetClient);
        }
    }
}