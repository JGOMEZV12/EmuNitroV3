using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using Polar.HabboHotel.GameClients;
using System;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Managers
{
    internal class StartEventCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get
            {
                return "command_start_event";
            }
        }
        public string Parameters
        {
            get
            {
                return "%name%";
            }
        }
        public string Description
        {
            get
            {
                return "iniciar un evento";
            }
        }
        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("porfavor coloca un tipo de evento", 1);
                return;
            }

            string Message = Params[1].ToString().ToLower();

            switch (Message)
            {
                #region Purga
                case "purga":
                    {
                        if (RoleplayManager.PurgeStarted)
                        { 
                            return;
                        }   
                        else
                        {
                            RoleplayManager.PurgeStarted = true;
                            Session.SendMessage(new RoomNotificationComposer("Nuevo evento de la ciudad",
                 "<font color=\"#8904B1\"><b>¡La purga ha comenzado!</b></font>\n\n" +
                 "<font size=\"11\" color=\"#1C1C1C\">La purga es un estado de la ciudad " + PolarEnvironment.GetConfig().data["hotel.name"] + ", Donde no se controla ningún crimen pues los policías se les ha otorgado unas vacaciones.\n\n</font>" +
                 "<font size=\"11\" color=\"#1C1C1C\">Ahora puedes hacer lo que quieras, tranquil@ la policía no te lo impedirá.</font>\n\n <b>Recomendaciones:</b> \n - ir a la Av Venezuela [1] y acabar con todos \n - Robar la bovéda \n - Conquista barrios para tu pandilla", "", ""));
                        }
                        break;
                    }
                #endregion

                default:
                    {
                        Session.SendWhisper("¡Este tipo de evento no existe o está deshabilitado!", 1);
                        break;
                    }
            }
        }
    }
}
