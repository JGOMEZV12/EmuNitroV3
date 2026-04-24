using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Groups;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Weapons;
using System.Drawing;
using Polar.HabboHotel.Users.Effects;
using Polar.Utilities;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Interactions.Basic
{
    class RapeCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_interactions_rape"; }
        }

        public string Parameters
        {
            get { return "%username%"; }
        }

        public string Description
        {
            get { return "Violar a un usuario."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            if (Params.Length == 1)
            {
                Session.SendWhisper("Vaya, se le olvidó ingresar un nombre de usuario", 1);
                return;
            }

            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);
            if (TargetClient == null)
            {
                Session.SendWhisper("Se ha producido un error al intentar encontrar a ese usuario, tal vez estén sin conexión.", 1);
                return;
            }

            RoomUser RoomUser = Session.GetRoomUser();
            RoomUser TargetUser = Room.GetRoomUserManager().GetRoomUserByHabbo(TargetClient.GetHabbo().Username);
            if (TargetUser == null)
            {
                Session.SendWhisper("Se ha producido un error al encontrar a ese usuario, tal vez no estén en línea o en esta sala.", 1);
                return;
            }

            if (Session.GetRoleplay().TryGetCooldown("violar"))
                return;

            if (Session.GetRoleplay().IsDead)
            {
                Session.SendWhisper("¡No puedes violar a alguien mientras estás muerto! ¡MALDITO NECROFILO!", 1);
                return;
            }

            if (Session.GetRoleplay().IsWorking)
            {
                Session.SendWhisper("No puedes violar a nadie en pleno trabajo ¡Sinverguenza!", 1);
                return;
            }

            if (Session.GetRoleplay().StaffOnDuty)
            {
                Session.SendWhisper("No puedes violar a un staff ¿Estas loco?", 1);
                return;
            }
            if (Session.GetRoleplay().PassiveMode)
            {
                Session.SendWhisper("No puedes realizar acciones ilegales en modo pasivo.", 1);
                return;
            }

            if (TargetClient.GetRoleplay().PassiveMode)
            {
                Session.SendWhisper("¡Este usuario tiene el modo pasivo activo!.", 1);
                return;
            }

            if (Session.GetRoleplay().CurEnergy <= 0)
            {
                Session.SendWhisper("¡No tienes suficiente energía para tener sexo, ve a tomar una cerveza!", 1);
                return;
            }

            if (TargetUser.IsAsleep)
            {
                Session.SendWhisper("No puedes hacerle esto, esta ausente", 1);
                return;
            }

            if (TargetClient == Session)
            {
                Session.SendWhisper("No puedes hacer esto", 1);
                return;
            }

            if (TargetClient.GetRoleplay().Sida > 100)
            {
                Session.SendWhisper("¡Esta persona tiene sida, no puedes hacer eso!", 10);
                return;
            }

            if (Session.GetRoomUser().Frozen)
            {
                Session.SendWhisper("No puedes violar mientras estás aturdido", 1);
                return;
            }

            if (Session.GetRoleplay().Sida > 100)
            {
                Session.SendWhisper("¡Tienes SIDA! no esta permitido hacer esto hasta que te cures.", 10);
                return;
            }

            #endregion

            #region Execute
            Point ClientPos = new Point(RoomUser.X, RoomUser.Y);
            var Random = new CryptoRandom();
            int Chance = Random.Next(1, 101);
            Point TargetClientPos = new Point(TargetUser.X, TargetUser.Y);
            double Distance = RoleplayManager.GetDistanceBetweenPoints2D(ClientPos, TargetClientPos);

            if (Distance <= 1)
            {
                if (!Session.GetRoleplay().WantedFor.Contains("sexual harassment"))
                    Session.GetRoleplay().WantedFor = Session.GetRoleplay().WantedFor + "Acoso sexual, ";

                Session.Shout("* Agarra a " + TargetClient.GetHabbo().Username + " Le arranca la ropa y golpea [-80] Energía, [-35] Salud [20% probabilidad de sida] (Se lastimó los genitales) *", 32);
                RoleplayManager.Shout(TargetClient, "* AUXILIO ME VIOLÓ " + Session.GetHabbo().Username + " llamen a la policía *", 32);
                Session.GetRoleplay().CooldownManager.CreateCooldown("violar", 1000, ((Session.GetRoleplay().IsJailed) ? 20 : 8));
                Session.GetRoleplay().CurEnergy -= 80;
                Session.GetRoleplay().CurHealth -= 35;
                Session.GetRoleplay().IsWanted = true;
                Session.GetRoleplay().Sida += 5;
                TargetClient.GetRoleplay().Sida += 5;
                RoomUser.ApplyEffect(507);
                TargetUser.ApplyEffect(507);
                Session.GetRoleplay().SexTimer = 15;
                TargetClient.GetRoleplay().SexTimer = 15;
                RoleplayManager.GetLookAndMotto(Session);
                RoleplayManager.GetLookAndMotto(TargetClient);
                return;


            }
            else
            {
                Session.SendWhisper("¡Debes acercarte a este ciudadano para violarlos!", 1);
                return;
            }
            #endregion
        }
    }
}