using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Items;
using Polar.Communication.Packets.Outgoing.Inventory.Furni;
using Polar.Database.Interfaces;
using Polar.HabboRoleplay.Houses;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Apartment
{
    class PickAllCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_house_pick_all"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Recoge todos los items de tu casa."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Room.OwnerId != Session.GetHabbo().Id)
            {
                House House;
                if (Room.TryGetHouse(out House))
                {
                    if (House.OwnerId != Session.GetHabbo().Id)
                    {
                        Session.SendWhisper("Solo puedes hacer eso en tu casa/apartamento.", 1);
                        return;
                    }
                }
                else
                {
                    Session.SendWhisper("Esta zona no es tu casa/apartamento.", 1);
                    return;
                }
            }
            else
            {
                Room.GetRoomItemHandler().RemoveItems(Session);

                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("UPDATE `items` SET `room_id` = '0', `user_id` = @UserId WHERE `room_id` = @RoomId");
                    dbClient.AddParameter("RoomId", Room.Id);
                    dbClient.AddParameter("UserId", Session.GetHabbo().Id);
                    dbClient.RunQuery();
                }

                List<Item> Items = Room.GetRoomItemHandler().GetWallAndFloor.ToList();
                if (Items.Count > 0)
                    Session.SendWhisper("¡Todavía quedaron furnis en la sala! Recogelos manualmente.", 1);

                Session.SendMessage(new FurniListUpdateComposer());
            }
        }
    }
}