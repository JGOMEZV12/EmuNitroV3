using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Handshake;
using Polar.Communication.Packets.Outgoing.Users;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.Communication.Packets.Outgoing.Navigator;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Managers
{
    class FlagOtherCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_flag_other"; }
        }

        public string Parameters
        {
            get { return "%username%"; }
        }

        public string Description
        {
            get { return "Obliga al usuario especificado a cambiar su nombre."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("Introduzca el nombre de usuario de la persona que desea marcar.", 1);
                return;
            }

            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);
            if (TargetClient == null)
            {
                Session.SendWhisper("Se ha producido un error al encontrar a ese usuario, tal vez no están en línea.", 1);
                return;
            }

            if (Params.Length == 2)
            {
                Session.SendWhisper("¡Has marcado con éxito a este usuario!", 1);
                TargetClient.GetHabbo().LastNameChange = 0;
                TargetClient.GetHabbo().ChangingName = true;
                TargetClient.GetRoleplay().FreeNameChange = true;
                TargetClient.SendNotification("Tenga en cuenta que si su nombre de usuario es considerado inapropiado, será expulsado sin duda.\r\rTambién tenga en cuenta que el personal NO le permitirá cambiar su nombre de usuario de nuevo si tiene un problema con lo que ha elegido. \r \rCierre esta ventana y haga clic en usted mismo para comenzar a elegir un nuevo nombre de usuario! \r\rUse nombres sencillos como por ejemplo: <b>Juan, Hugo, Hefesto, Bryan,etc... \r \r");
                TargetClient.SendMessage(new UserObjectComposer(TargetClient.GetHabbo()));
            }
            else if (Params.Length > 2)
            {
                var User = TargetClient.GetRoomUser();

                if (User == null)
                {
                    Session.SendWhisper("Lo sentimos, pero esta persona no está en una habitación!", 1);
                    return;
                }

                if (User.RoomId != Room.Id)
                {
                    Session.SendWhisper("Por favor, asegúrese de que el objetivo está en la misma habitación que usted con el fin de marcarlos!", 1);
                    return;
                }

                string NewName = Params[2];
                string OldName = TargetClient.GetHabbo().Username;

                bool InUse = false;
                using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("SELECT COUNT(0) FROM `users` WHERE `username` = @name LIMIT 1");
                    dbClient.AddParameter("name", NewName);
                    InUse = dbClient.getInteger() == 1;
                }

                if (InUse)
                {
                    Session.SendWhisper("Lo sentimos, pero el nombre que eligió para esta persona ya está tomado!", 1);
                    return;
                }

                if (!PolarEnvironment.GetGame().GetClientManager().UpdateClientUsername(TargetClient, OldName, NewName))
                {
                    Session.SendWhisper("¡Vaya! Se ha producido un problema al actualizar su nombre de usuario. Por favor, inténtelo de nuevo.", 1);
                    return;
                }

                Room TargetRoom = User.GetRoom();

                TargetClient.GetHabbo().ChangingName = false;
                if (TargetRoom != null)
                    TargetRoom.GetRoomUserManager().RemoveUserFromRoom(TargetClient, true, false);

                TargetClient.GetHabbo().ChangeName(NewName);
                TargetClient.GetHabbo().GetMessenger().OnStatusChanged(true);

                TargetClient.SendMessage(new UpdateUsernameComposer(NewName));
                if (TargetRoom != null)
                    TargetRoom.SendMessage(new UserNameChangeComposer(Room.Id, User.VirtualId, NewName));

                using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("INSERT INTO `logs_client_namechange` (`user_id`,`new_name`,`old_name`,`timestamp`) VALUES ('" + TargetClient.GetHabbo().Id + "', @name, '" + OldName + "', '" + PolarEnvironment.GetUnixTimestamp() + "')");
                    dbClient.AddParameter("name", NewName);
                    dbClient.RunQuery();
                }

                ICollection<RoomData> Rooms = TargetClient.GetHabbo().UsersRooms;
                foreach (RoomData Data in Rooms)
                {
                    if (Data == null)
                        continue;

                    Data.OwnerName = NewName;
                }

                foreach (Room UserRoom in PolarEnvironment.GetGame().GetRoomManager().GetRooms().ToList())
                {
                    if (UserRoom == null || UserRoom.RoomData.OwnerName != NewName)
                        continue;

                    UserRoom.OwnerName = NewName;
                    UserRoom.RoomData.OwnerName = NewName;

                    UserRoom.SendMessage(new RoomInfoUpdatedComposer(UserRoom.RoomId));
                }
                RoleplayManager.SendUserOld2(TargetClient, Room.Id, "");
                Session.Shout("*Utiliza sus poderes divinos y cambia el nombre de " + OldName + " a " + NewName + "*", 23);
                return;
            }
        }
    }
}
