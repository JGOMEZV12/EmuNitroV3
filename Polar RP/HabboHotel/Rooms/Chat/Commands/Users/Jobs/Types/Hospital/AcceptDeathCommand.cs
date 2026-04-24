using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Drawing;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using Polar.Communication.Packets.Outgoing.Notifications;
using Polar.HabboRoleplay.RPRoom;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Jobs.Types.Police
{
    class AcceptDeathCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_accept_death"; }
        }

        public string Parameters
        {
            get { return "%user%"; }
        }

        public string Description
        {
            get { return "Acepta tu muerte para reaparecer en el Hospital."; }
        }

        public async Task Execute(GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions           

            if (Session.GetRoleplay().Cuffed)
            {
                Session.SendWhisper("No puedes hacer eso mientras estás esposad@", 1);
                return;
            }
            if (Session.GetRoleplay().Pasajero)
            {
                Session.SendWhisper("Ya te encuentras siendo transportado al Hospital. Por favor espera.", 1);
                return;
            }
            if (Session.GetRoleplay().TryGetCooldown("acceptdeath", true))
            {
                Session.SendWhisper("Por favor espera un poco para hacer eso nuevamente.", 1);
                return;
            }
            #endregion

            #region Execute
            RoleplayManager.Shout(Session, "*Pierde el conocimiento y es trasladad@ al hospital*", 5);
            
            string MyCity = Room.City;

            RPRoom Data;
            int ToHosp = PolarEnvironment.GetGame().GetRPRoomManager().TryToGetHospital(MyCity, out Data);

            if (ToHosp > 0)
            {
                if (RoleplayManager.GenerateRoom(ToHosp, out Room Room2))
                {
                    Session.GetRoleplay().IsDead = true;
                    Session.GetRoleplay().DeadTimeLeft = RoleplayManager.DeathTime;

                    Session.GetHabbo().HomeRoom = ToHosp;
                    /*
                    if (Session.GetHabbo().CurrentRoomId != ToHosp)
                        RoleplayManager.SendUserTimer(Client, ToHosp, "", "death");
                    else
                        Client.GetRoleplay().TimerManager.CreateTimer("death", 1000, true);
                    */
                    RoleplayManager.SendUserTimer(Session, ToHosp, "", "death");
                }
                else
                {
                    Session.SendNotification("[Error][102] -> Lamentablemente ha habido un error. No se encontró ningún Hospital disponible en esta ciudad. Comunícaselo a un Administrador. ¡Gracias!");
                    Session.GetRoleplay().CurHealth = Session.GetRoleplay().MaxHealth;
                    Session.GetRoleplay().RefreshStatDialogue();
                    Session.GetRoomUser().Frozen = false;
                    Session.SendWhisper("Se te ha revivido a causa de que no hay ningún hospital en esta Ciudad", 1);
                }
            }
            else
            {
                Session.SendNotification("[Error][103] -> Lamentablemente ha habido un error. No se encontró ningún Hospital disponible en esta ciudad. Comunícaselo a un Administrador. ¡Gracias!");
                Session.GetRoleplay().CurHealth = Session.GetRoleplay().MaxHealth;
                Session.GetRoleplay().RefreshStatDialogue();
                Session.GetRoomUser().Frozen = false;
                Session.SendWhisper("Se te ha revivido a causa que no hay ningún hospital en esta Ciudad");
            }
            #endregion
        }
        
    }
}