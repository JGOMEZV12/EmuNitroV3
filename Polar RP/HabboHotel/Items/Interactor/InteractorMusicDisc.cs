using Polar.Communication.Packets.Outgoing.Sound;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms;
using System;
using System.Collections.Generic;

namespace Polar.HabboHotel.Items.Interactor
{
    internal class InteractorMusicDisc : IFurniInteractor
    {
        public void OnRemove(GameClient Session, Item Item)
        {
            var room = Item.GetRoom();
            var cd = room.GetTraxManager().GetDiscItem(Item.Id);

            if (cd != null)
            {
                room.GetTraxManager().StopPlayList();
                room.GetTraxManager().RemoveDisc(Item);
            }

            // Siempre actualizar la lista visual
            var Items = room.GetTraxManager().GetAvaliableSongs();
            Items.Remove(Item);
            room.SendMessage(new LoadJukeboxUserMusicItemsComposer(Items));
        }
        public void OnPlace(GameClient Session, Item Item)
        {
            Room room = Item.GetRoom();
            List<Item> avaliableSongs = room.GetTraxManager().GetAvaliableSongs();
            bool flag = !avaliableSongs.Contains(Item) && !room.GetTraxManager().Playlist.Contains(Item);
            if (flag)
            {
                avaliableSongs.Add(Item);
            }
            room.SendMessage(new LoadJukeboxUserMusicItemsComposer(avaliableSongs));
        }

        public void OnTrigger(GameClient Session, Item item, int Request, bool HasRights)
        {
        }
        public void OnWiredTrigger(Item Item)
        {
        }
    }
}
