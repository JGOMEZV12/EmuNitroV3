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
    class PullCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_pull"; }
        }

        public string Parameters
        {
            get { return "%target%"; }
        }

        public string Description
        {
            get { return "Empuja a otro."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("Por favor ingrese el nombre de usuario del usuario que desea extraer.", 1);
                return;
            }

            if (!Room.PullEnabled && !Session.GetHabbo().GetPermissions().HasRight("room_override_custom_config"))
            {
                Session.SendWhisper("¡No puedes entrar a esta habitación!", 1);
                return;
            }

            if (Session.GetRoleplay().StaffOnDuty || Session.GetRoleplay().AmbassadorOnDuty && !Session.GetHabbo().GetPermissions().HasRight("room_override_custom_config"))
            {
                Session.SendWhisper("¡No puedes sacar a alguien mientras estás de servicio!", 1);
                return;
            }

            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);
            if (TargetClient == null)
            {
                Session.SendWhisper("Se produjo un error al encontrar a ese usuario, tal vez no están en línea.", 1);
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
                Session.SendWhisper("¡No puedes jalarte!", 1);
                return;
            }

            if (TargetUser.IsAsleep)
            {
                Session.SendWhisper("¡No puedes sacar a alguien que no está jugando el juego ahora mismo!", 1);
                return;
            }

            if (TargetUser.TeleportEnabled)
            {
                Session.SendWhisper("Vaya, no puedes llamar a un usuario mientras tienen habilitado su modo de teletransportación.", 1);
                return;
            }

            if (!Session.GetHabbo().GetPermissions().HasRight("mod_tool") && TargetClient.GetRoleplay().PassiveMode)
            {
                Session.SendWhisper("No puedes hacerle eso a una persona en modo pasivo.", 1);
                return;
            }

            if (TargetClient.GetRoleplay().StaffOnDuty)
            {
                Session.SendWhisper("¡No puedes sacar a un miembro del personal que está de servicio!", 1);
                return;
            }

            if (TargetClient.GetRoleplay().AmbassadorOnDuty)
            {
                Session.SendWhisper("¡No puedes sacar a un embajador que está de servicio!", 1);
                return;
            }

            if (Session.GetRoleplay().TryGetCooldown("pull"))
                return;

            RoomUser ThisUser = Session.GetRoomUser();
            if (ThisUser == null)
                return;

            if (!HabboRoleplay.Misc.RoleplayManager.PushPullOnArrows)
            {

                    List<Item> RoomArrow = Room.GetRoomItemHandler().GetFloor.Where(x => x != null && x.GetBaseItem() != null && x.GetBaseItem().InteractionType == InteractionType.ARROW && (ThisUser.RotBody == 0 ? TargetUser.SetY - 1 == x.GetY : (ThisUser.RotBody == 1 ? TargetUser.SetX + 1 == x.GetX && TargetUser.Y - 1 == x.GetY : (ThisUser.RotBody == 2 ? TargetUser.SetX + 1 == x.GetX : (ThisUser.RotBody == 3 ? TargetUser.X + 1 == x.GetX && TargetUser.Y + 1 == x.GetY : (ThisUser.RotBody == 4 ? TargetUser.SetY + 1 == x.GetY : (ThisUser.RotBody == 5 ? TargetUser.X - 1 == x.GetX && TargetUser.Y + 1 == x.GetY : (ThisUser.RotBody == 6 ? TargetUser.SetX - 1 == x.GetX : (ThisUser.RotBody == 7 ? TargetUser.X - 1 == x.GetX && TargetUser.Y - 1 == x.GetY : false))))))))).ToList();
                    if (RoomArrow.Count > 0)
                    {
                        Session.SendWhisper("¡No puedes sacar a los usuarios de esta sala!", 1);
                        return;
                    }
                
            }

            string PushDirection = "down";
            if (TargetClient.GetHabbo().CurrentRoomId == Session.GetHabbo().CurrentRoomId && (Math.Abs(ThisUser.X - TargetUser.X) < 3 && Math.Abs(ThisUser.Y - TargetUser.Y) < 3))
            {
                if (ThisUser.RotBody == 0)
                    PushDirection = "up";
                if (ThisUser.RotBody == 2)
                    PushDirection = "right";
                if (ThisUser.RotBody == 4)
                    PushDirection = "down";
                if (ThisUser.RotBody == 6)
                    PushDirection = "left";

                if (PushDirection == "up")
                    TargetUser.MoveTo(ThisUser.X, ThisUser.Y - 1);

                if (PushDirection == "right")
                    TargetUser.MoveTo(ThisUser.X + 1, ThisUser.Y);

                if (PushDirection == "down")
                    TargetUser.MoveTo(ThisUser.X, ThisUser.Y + 1);

                if (PushDirection == "left")
                    TargetUser.MoveTo(ThisUser.X - 1, ThisUser.Y);

                Session.Shout("*Atrae a " + TargetClient.GetHabbo().Username + " con el olor de un rico perfume Hacia el/ella*", 4);
                Session.GetRoleplay().CooldownManager.CreateCooldown("pull", 1000, 3);
                return;
            }
            else
            {
                Session.SendWhisper("Ese usuario no está lo suficientemente cerca de usted para que lo jalen, intente acercarse.", 1);
                return;
            }
        }
    }
}
