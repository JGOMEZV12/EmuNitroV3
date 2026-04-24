using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms.AI;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;

using Polar.HabboHotel.Users.Inventory.Bots;
using Polar.Communication.Packets.Outgoing.Inventory.Bots;

using Polar.Database.Interfaces;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Developers
{
    class KickBotsCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_kick_bots"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Patear todos los bots de la habitación."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            foreach (RoomUser User in Room.GetRoomUserManager().GetUserList().ToList())
            {
                if (User == null || User.IsPet || !User.IsBot)
                    continue;

                RoomUser BotUser = null;
                if (!Room.GetRoomUserManager().TryGetBot(User.BotData.Id, out BotUser))
                    return;

                /*
                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("UPDATE `bots` SET `room_id` = '0' WHERE `id` = @id LIMIT 1");
                    dbClient.AddParameter("id", User.BotData.Id);
                    dbClient.RunQuery();
                }

                Session.GetHabbo().GetInventoryComponent().TryAddBot(new Bot(Convert.ToInt32(BotUser.BotData.Id), Convert.ToInt32(BotUser.BotData.ownerID), BotUser.BotData.Name, BotUser.BotData.Motto, BotUser.BotData.Look, BotUser.BotData.Gender));
                Session.SendMessage(new BotInventoryComposer(Session.GetHabbo().GetInventoryComponent().GetBots()));
                */

                Room.GetRoomUserManager().RemoveBot(BotUser.VirtualId, false);
            }

            Session.Shout("*Utiliza sus poderes divinos y patea a todos los bots fuera de la habitación*", 23);
            Session.SendWhisper("Éxito, eliminado todos los bots.", 1);
        }
    }
}
