using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Pathfinding;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using Polar.HabboHotel.Items;

namespace Polar.HabboHotel.Rooms.Chat.Commands.VIP
{
    class PushCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_push"; }
        }

        public string Parameters
        {
            get { return "%target%"; }
        }

        public string Description
        {
            get { return "Empujar a otro usuario."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("Por favor ingrese el nombre de usuario del usuario al que desea enviar.", 1);
                return;
            }

            if (!Room.PushEnabled && !Session.GetHabbo().GetPermissions().HasRight("room_override_custom_config"))
            {
                Session.SendWhisper("¡No puedes empujar en esta habitación!", 1);
                return;
            }

            if (Session.GetRoleplay().StaffOnDuty || Session.GetRoleplay().AmbassadorOnDuty && !Session.GetHabbo().GetPermissions().HasRight("room_override_custom_config"))
            {
                Session.SendWhisper("¡No puedes empujar a alguien mientras estás de servicio!", 1);
                return;
            }

            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);
            if (TargetClient == null)
            {
                Session.SendWhisper("Se produjo un error al encontrar ese usuario, tal vez no están en línea.", 1);
                return;
            }

            RoomUser TargetUser = Room.GetRoomUserManager().GetRoomUserByHabbo(TargetClient.GetHabbo().Username);
            if (TargetUser == null)
            {
                Session.SendWhisper("Se produjo un error al encontrar a ese usuario, tal vez no están en línea o en esta sala.", 1);
                return;
            }

            if (TargetClient.GetHabbo().Username == Session.GetHabbo().Username)
            {
                Session.SendWhisper("¡No puedes forzarte!", 1);
                return;
            }

            if (TargetUser.IsAsleep)
            {
                Session.SendWhisper("¡No puedes empujar a alguien que no está jugando el juego ahora mismo!", 1);
                return;
            }

            if (TargetUser.TeleportEnabled)
            {
                Session.SendWhisper("Vaya, no puedes presionar a un usuario mientras tienen habilitado su modo de teletransportación.", 1);
                return;
            }

            if (TargetClient.GetRoleplay().StaffOnDuty)
            {
                Session.SendWhisper("¡No puedes presionar a un miembro del personal que está de servicio!", 1);
                return;
            }

            if (TargetClient.GetRoleplay().AmbassadorOnDuty)
            {
                Session.SendWhisper("¡No puedes empujar a un embajador que está de servicio!", 1);
                return;
            }

            if (!Session.GetHabbo().GetPermissions().HasRight("mod_tool") && TargetClient.GetRoleplay().PassiveMode)
            {
                Session.SendWhisper("No puedes hacerle eso a una persona en modo pasivo.", 1);
                return;
            }

            if (Session.GetRoleplay().TryGetCooldown("push"))
                return;

          

            RoomUser ThisUser = Session.GetRoomUser();
            if (ThisUser == null)
                return;

            if (!((Math.Abs(TargetUser.X - ThisUser.X) >= 2) || (Math.Abs(TargetUser.Y - ThisUser.Y) >= 2)))
            {
                if (!HabboRoleplay.Misc.RoleplayManager.PushPullOnArrows)
                {
                   
                        List<Item> RoomArrow = Room.GetRoomItemHandler().GetFloor.Where(x => x != null && x.GetBaseItem() != null && x.GetBaseItem().InteractionType == InteractionType.ARROW && (ThisUser.RotBody == 0 ? TargetUser.SetY - 1 == x.GetY : (ThisUser.RotBody == 1 ? TargetUser.SetX + 1 == x.GetX && TargetUser.Y - 1 == x.GetY : (ThisUser.RotBody == 2 ? TargetUser.SetX + 1 == x.GetX : (ThisUser.RotBody == 3 ? TargetUser.X + 1 == x.GetX && TargetUser.Y + 1 == x.GetY : (ThisUser.RotBody == 4 ? TargetUser.SetY + 1 == x.GetY : (ThisUser.RotBody == 5 ? TargetUser.X - 1 == x.GetX && TargetUser.Y + 1 == x.GetY : (ThisUser.RotBody == 6 ? TargetUser.SetX - 1 == x.GetX : (ThisUser.RotBody == 7 ? TargetUser.X - 1 == x.GetX && TargetUser.Y - 1 == x.GetY : false))))))))).ToList();
                        if (RoomArrow.Count > 0)
                        {
                            Session.SendWhisper("¡No puedes expulsar a los usuarios de esta sala!", 1);
                            return;
                        }
                }

                if (TargetUser.RotBody == 4)
                {
                    TargetUser.MoveTo(TargetUser.X, TargetUser.Y + 1);
                }

                if (ThisUser.RotBody == 0)
                {
                    TargetUser.MoveTo(TargetUser.X, TargetUser.Y - 1);
                }

                if (ThisUser.RotBody == 6)
                {
                    TargetUser.MoveTo(TargetUser.X - 1, TargetUser.Y);
                }

                if (ThisUser.RotBody == 2)
                {
                    TargetUser.MoveTo(TargetUser.X + 1, TargetUser.Y);
                }

                if (ThisUser.RotBody == 3)
                {
                    TargetUser.MoveTo(TargetUser.X + 1, TargetUser.Y);
                    TargetUser.MoveTo(TargetUser.X, TargetUser.Y + 1);
                }

                if (ThisUser.RotBody == 1)
                {
                    TargetUser.MoveTo(TargetUser.X + 1, TargetUser.Y);
                    TargetUser.MoveTo(TargetUser.X, TargetUser.Y - 1);
                }

                if (ThisUser.RotBody == 7)
                {
                    TargetUser.MoveTo(TargetUser.X - 1, TargetUser.Y);
                    TargetUser.MoveTo(TargetUser.X, TargetUser.Y - 1);
                }

                if (ThisUser.RotBody == 5)
                {
                    TargetUser.MoveTo(TargetUser.X - 1, TargetUser.Y);
                    TargetUser.MoveTo(TargetUser.X, TargetUser.Y + 1);
                }

                Session.Shout("*Empuja a " + TargetClient.GetHabbo().Username + " por becerr@ ¡Largate!*");
                Session.GetRoleplay().CooldownManager.CreateCooldown("push", 1000, 3);
            }
            else
            {
                Session.SendWhisper("Oops, " + TargetClient.GetHabbo().Username + " no está lo suficientemente cerca!", 1);
            }
        }
    }
}
