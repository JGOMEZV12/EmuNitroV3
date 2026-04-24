using System;
using System.Collections.Generic;
using Polar.Communication.Packets.Outgoing.Rooms.Furni.Wired;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Items.Wired;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Interactor
{
    public class InteractorWiredEffect : IFurniInteractor
    {
        public void OnPlace(GameClient Session, Item Item) { }

        public void OnRemove(GameClient Session, Item Item) { }

        public void OnTrigger(GameClient Session, Item Item, int Request, bool HasRights)
        {
            if (Session == null || Item == null || !HasRights) return;

            if (!Item.GetRoom().GetWired().TryGet(Item.Id, out IWiredItem? Box)) return;

            Item.ExtraData = "1";
            Item.UpdateState(false, true);
            Item.RequestUpdate(2, true);

            List<int> BlockedItems = WiredBoxTypeUtility.ContainsBlockedTrigger(Box, Item.GetRoom().GetWired().GetTriggers(Box));
            Session.SendMessage(new WiredEffectConfigComposer(Box, BlockedItems));
        }

        public void OnWiredTrigger(Item Item) { }
    }
}