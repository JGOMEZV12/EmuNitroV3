using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Developers
{
    class SetWhisperTileCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_set_whisper_tile"; }
        }

        public string Parameters
        {
            get { return "%message%"; }
        }

        public string Description
        {
            get { return "Establece el mensaje de susurro en el que está parado."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("Se te olvidó introducir un mensaje para cambiar el susurro a!", 1);
                return;
            }

            RoomUser User = Session.GetRoomUser();
            if (User == null)
                return;

            string Message = CommandManager.MergeParams(Params, 1);

            var Items = Room.GetGameMap().GetAllRoomItemForSquare(User.Coordinate.X, User.Coordinate.Y);
            bool HasWhisperTile = Items.Where(x => x.GetBaseItem().InteractionType == HabboHotel.Items.InteractionType.WHISPER_TILE).ToList().Count > 0;

            if (HasWhisperTile)
            {
                var Item = Items.FirstOrDefault(x => x.GetBaseItem().InteractionType == HabboHotel.Items.InteractionType.WHISPER_TILE);

                if (Item == null)
                {
                    Session.SendWhisper("Por alguna extraña razón, el furni no podría ser encontrado!", 1);
                    return;
                }

                if (Item.WhisperTileData == null)
                    Item.WhisperTileData = new Items.Data.WhisperTile.WhisperTileData(Item.Id);

                using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("UPDATE `room_items_whisper_tile` SET `message` = @message WHERE `item_id` = @itemid LIMIT 1");
                    dbClient.AddParameter("itemid", Item.Id);
                    dbClient.AddParameter("message", Message);
                    dbClient.RunQuery();
                }

                Item.WhisperTileData.Message = Message;
                Session.SendWhisper("¡Usted ha actualizado con éxito este mensaje del susurro de los azulejos!", 1);
                return;
            }
            else
            {
                Session.SendWhisper("¡No estás parado en un susurro!", 1);
                return;
            }
        }
    }
}
