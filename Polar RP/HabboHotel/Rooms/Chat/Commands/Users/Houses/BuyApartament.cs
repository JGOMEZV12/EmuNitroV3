
using Polar.HabboRoleplay.Houses;


namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Apartment
{
    class BuyApartmentCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_apartment_buy"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Compra el apartamento en el que se encuentra actualmente."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            House Apartment;
            if (!Room.TryGetHouse(out Apartment))
            {
                Session.SendWhisper("¡Ni siquiera estás dentro de un apartamento!", 34);
                return;
            }

            if (!Apartment.ForSale)
            {
                Session.SendWhisper("Lo sentimos pero este apartamento no está a la venta", 34);
                return;
            }

            /* if (Params.Length == 1)
             {
                 Session.SendWhisper("¿Estás seguro de que quieres comprar este apartamento por $" + Apartment.Cost + "? si es así, escriba ':comprarcasa yes' para comprarlo", 34);
                 return;
             }
             else
             {
                 if (Params[1].ToLower() != "yes")
                 {
                     Session.SendWhisper("¿Estás seguro de que quieres comprar este apartamento por $" + Apartment.Cost + "? Si es así, escriba ':comprarcasa yes' Para comprarlo", 34);
                     return;
                 }
                 else
                 {
                     if (Session.GetHabbo().Credits < Apartment.Cost)
                     {
                         Session.SendWhisper("No tienes $" + Apartment.Cost + " para comprar este apartamento o casa", 34);
                         return;
                     }
                     else
                     {
                         RoleplayManager.Shout(Session, "*Compra este apartamento por $" + Apartment.Cost + "*", 6);
                         Session.GetHabbo().Credits -= Apartment.Cost;
                         Session.GetHabbo().UpdateCreditsBalance();

                         var Client = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Apartment.OwnerId);

                         if (Client == null)
                         {
                             using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                             {
                                 dbClient.SetQuery("UPDATE `users` SET `credits` = `credits` + @cost WHERE `id` = @userid");
                                 dbClient.AddParameter("cost", Apartment.Cost);
                                 dbClient.AddParameter("userid", Apartment.OwnerId);
                                 dbClient.RunQuery();
                             }
                         }
                         else
                         {
                             Client.SendNotification("Su apartamento en [RoomID: " + Apartment.RoomId + "] Acaba de ser vendido a " + Session.GetHabbo().Id + " por $" + Apartment.Cost + "!\n\n¡Felicidades!.");
                             Client.GetHabbo().Credits += Apartment.Cost;
                             Client.GetHabbo().UpdateCreditsBalance();
                         }

                         Apartment.BuyHouse(Session);

                         using (var Adapter = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                         {
                             Adapter.SetQuery("UPDATE rooms SET owner = @new WHERE id = @id");
                             Adapter.AddParameter("new", Session.GetHabbo().Id);
                             Adapter.AddParameter("id", Apartment.RoomId);
                             Adapter.RunQuery();

                             Adapter.SetQuery("UPDATE items SET user_id = @new WHERE room_id = @id");
                             Adapter.AddParameter("new", Session.GetHabbo().Id);
                             Adapter.AddParameter("id", Apartment.RoomId);
                             Adapter.RunQuery();

                             Adapter.SetQuery("DELETE FROM room_rights WHERE room_id = @id");
                             Adapter.AddParameter("id", Apartment.RoomId);
                         }

                         Room.RoomData.OwnerId = Session.GetHabbo().Id;
                         Room.RoomData.OwnerName = Session.GetHabbo().Username;

                         foreach (Item Item in Room.GetRoomItemHandler().GetWallAndFloor)
                         {
                             if (Item == null)
                                 continue;

                             Item.UserID = Session.GetHabbo().Id;
                             Item.Username = Session.GetHabbo().Username;
                         }

                         Apartment.ForSale = false;
                         Apartment.UpdateCost(0);

                         Session.GetHabbo().UsersRooms.Add(Room);
                         Session.GetHabbo().UsersRooms.Remove(Room);

                         List<RoomUser> UsersToReturn = new List<RoomUser>(Session.GetHabbo().CurrentRoom.GetRoomUserManager().GetRoomUsers().ToList());
                         RoomData Data = PolarEnvironment.GetGame().GetRoomManager().GenerateRoomData(Room.RoomId);
                         Session.GetHabbo().PrepareRoom(Session.GetHabbo().CurrentRoom.RoomId, "");
                         Room room;
                         PolarEnvironment.GetGame().GetRoomManager().LoadRoom(Room.RoomId, out room);
                         foreach (RoomUser User in UsersToReturn)
                         {
                             if (User == null || User.GetClient() == null)
                                 continue;

                             User.GetClient().SendMessage(new RoomForwardComposer(Room.RoomId));
                             User.GetClient().SendNotification("¡El apartamento en la que te encontrabas acaba de ser comprado por " + Session.GetHabbo().Username + " ¡Enhorabuena!");
                         }
                         return;
                     }
                 }
             }*/
            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_apart", "open");
        }
    }
}
