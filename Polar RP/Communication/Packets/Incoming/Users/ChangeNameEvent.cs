using System;
using System.Linq;
using System.Data;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;
using Polar.Communication.Packets.Outgoing.Users;
using Polar.Communication.Packets.Outgoing.Navigator;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;

using Polar.Database.Interfaces;
using Polar.Communication.Packets.Outgoing.Rooms.Session;
using Polar.Communication.Packets.Outgoing.Handshake;
using Polar.Communication.Packets.Outgoing.Inventory.Purse;

namespace Polar.Communication.Packets.Incoming.Users
{
    internal class ChangeNameEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            if (Session == null || Session.GetHabbo() == null)
                return;

            Room Room = Session.GetHabbo().CurrentRoom;
            if (Room == null)
                return;

            RoomUser User = Room.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);
            if (User == null)
                return;

            string NewName = Packet.PopString();
            string OldName = Session.GetHabbo().Username;

            if (NewName == OldName)
            {
                Session.GetHabbo().ChangeName(OldName);
                Session.SendMessage(new UpdateUsernameComposer(NewName));
                return;
            }

            if (!CanChangeName(Session.GetHabbo()))
            {
                Session.SendMessage(new UserObjectComposer(Session.GetHabbo()));
                Session.SendNotification("Vaya, parece que actualmente no puedes cambiar tu nombre de usuario.");
                return;
            }

            if (!Session.GetRoleplay().FreeNameChange)
            {
                if (Session.GetHabbo().Diamonds < 1)
                {
                    Session.SendMessage(new UserObjectComposer(Session.GetHabbo()));
                    Session.SendNotification("¡No tienes suficientes diamantes para un cambio de nombre!");
                    return;
                }
            }

            bool InUse = false;
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT COUNT(0) FROM `users` WHERE `username` = @name LIMIT 1");
                dbClient.AddParameter("name", NewName);
                InUse = dbClient.getInteger() == 1;
            }

            char[] Letters = NewName.ToLower().ToCharArray();
            string AllowedCharacters = "abcdefghijklmnopqrstuvwxyz-";

            foreach (char Chr in Letters)
            {
                if (!AllowedCharacters.Contains(Chr))
                {
                    return;
                }
            }

            List<string> BlacklistedWords = new List<string>();
            using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT * FROM `cms_blacklisted_words`");
                DataTable Table = dbClient.getTable();

                if (Table != null)
                {
                    foreach (DataRow Row in Table.Rows)
                    {
                        string Word = Row["word"].ToString();

                        if (Word.Length > 0 && Word.ToLower() != "")
                        {
                            if (!BlacklistedWords.Contains(Word.ToLower()))
                                BlacklistedWords.Add(Word.ToLower());
                        }
                    }
                }
            }

            if (NewName.ToLower().Contains("mod") || NewName.ToLower().Contains("adm") || NewName.ToLower().Contains("admin") || NewName.ToLower().Contains("m0d"))
            {
                Session.SendMessage(new UserObjectComposer(Session.GetHabbo()));
                Session.SendNotification("¡No puedes hacer que este sea tu nombre! Por favor escribe ':flagme' otra vez");
                return;
            }
            else if (!NewName.Contains('-'))
            {
                Session.SendMessage(new UserObjectComposer(Session.GetHabbo()));
                Session.SendNotification("¡No puedes hacer que este sea tu nombre!Por favor escribe ':flagme' again!");
                return;
            }
            else if (NewName.Split('-')[0].Length < 3 || NewName.Split('-')[1].Length < 1)
            {
                Session.SendMessage(new UserObjectComposer(Session.GetHabbo()));
                Session.SendNotification("¡No puedes hacer que este sea tu nombre! Por favor escribe ':flagme' again!");
                return;
            }
            else if (NewName.Length > 15)
            {
                Session.SendMessage(new UserObjectComposer(Session.GetHabbo()));
                Session.SendNotification("¡No puedes hacer que este sea tu nombre! Por favor escribe ':flagme' again!");
                return;
            }
            else if (NewName.Length < 3)
            {
                Session.SendMessage(new UserObjectComposer(Session.GetHabbo()));
                Session.SendNotification("¡No puedes hacer que este sea tu nombre! Por favor escribe ':flagme' again!");
                return;
            }
            else if (InUse)
            {
                Session.SendMessage(new UserObjectComposer(Session.GetHabbo()));
                Session.SendNotification("¡No puedes hacer que este sea tu nombre! Por favor escribe ':flagme' again!");
                return;
            }
            else
            {
                string FirstName = NewName.Split('-')[0].ToLower();
                string SecondName = NewName.Split('-')[1].ToLower();

                if (BlacklistedWords.Contains(FirstName) || BlacklistedWords.Contains(SecondName))
                {
                    if (BlacklistedWords.Contains(FirstName))
                        Session.SendNotification("Lo siento, pero ese primer nombre no está permitido!");
                    else
                        Session.SendNotification("Lo siento, pero ese segundo nombre no está permitido!");
                    Session.SendMessage(new UserObjectComposer(Session.GetHabbo()));
                    return;
                }

                if (!PolarEnvironment.GetGame().GetClientManager().UpdateClientUsername(Session, OldName, NewName))
                {
                    Session.SendMessage(new UserObjectComposer(Session.GetHabbo()));
                    Session.SendNotification("Oops!Se ha producido un problema al actualizar su nombre de usuario.Por favor escribe ':flagme' again.");
                    return;
                }

                Session.GetHabbo().ChangingName = false;
                Room.GetRoomUserManager().RemoveUserFromRoom(Session, true, false);

                Session.GetHabbo().ChangeName(NewName);
                Session.GetHabbo().GetMessenger().OnStatusChanged(true);

                if (!Session.GetRoleplay().FreeNameChange)
                {
                    Session.GetHabbo().Diamonds--;
                    Session.SendMessage(new ActivityPointsComposer(Session.GetHabbo().Duckets, Session.GetHabbo().Diamonds, Session.GetHabbo().EventPoints));
                }

                Session.SendMessage(new UpdateUsernameComposer(NewName));
                Room.SendMessage(new UserNameChangeComposer(Room.Id, User.VirtualId, NewName));

                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("INSERT INTO `logs_client_namechange` (`user_id`,`new_name`,`old_name`,`timestamp`) VALUES ('" + Session.GetHabbo().Id + "', @name, '" + OldName + "', '" + PolarEnvironment.GetUnixTimestamp() + "')");
                    dbClient.AddParameter("name", NewName);
                    dbClient.RunQuery();
                }

                ICollection<RoomData> Rooms = Session.GetHabbo().UsersRooms;
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

                 Session.SendMessage(new RoomForwardComposer(Room.Id));
            }
        }

        private static bool CanChangeName(Habbo Habbo)
        {

            if (Habbo.Rank == 1 && Habbo.VIPRank == 0 && Habbo.LastNameChange == 0)
                return true;
            else if (Habbo.Rank == 1 && Habbo.VIPRank == 1 && (Habbo.LastNameChange == 0 || (PolarEnvironment.GetUnixTimestamp() + 604800) > Habbo.LastNameChange))
                return true;
            else if (Habbo.Rank == 1 && Habbo.VIPRank == 2 && (Habbo.LastNameChange == 0 || (PolarEnvironment.GetUnixTimestamp() + 86400) > Habbo.LastNameChange))
                return true;
            else if (Habbo.GetPermissions().HasRight("mod_tool"))
                return true;

            return false;
        }
    }
}