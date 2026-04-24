using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.Communication.Packets.Outgoing.Navigator;
using Polar.Communication.Packets.Outgoing.Moderation;
using Polar.HabboHotel.Navigator;

namespace Polar.Communication.Packets.Incoming.Navigator
{
    internal class CreateFlatEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            if (Session == null || Session.GetHabbo() == null)
                return;
            
            if (!Session.GetHabbo().GetPermissions().HasRight("can_create_room"))
            {
                Session.SendNotification("No puedes crear salas, busca alguien que te venda un apartamento o una casa");
                return;
            }

            if (Session.GetRoleplay().BankChequings <= 75000)
            {
                Session.SendNotification("¡Debes tener 75.000 dólares en tu cuenta bancaria para poder comprar una casa!");
                return;
            }

            if (Session.GetHabbo().UsersRooms.Count >= 500)
            {
                Session.SendMessage(new CanCreateRoomComposer(true, 500));
                return;
            }

            string word;
            string Name = Packet.PopString();
            Name = PolarEnvironment.GetGame().GetChatManager().GetFilter().IsUnnaceptableWord(Name, out word) ? "Spam" : Name;
            string Description = Packet.PopString();
            Description = PolarEnvironment.GetGame().GetChatManager().GetFilter().IsUnnaceptableWord(Description, out word) ? "Spam" : Description;
            string ModelName = Packet.PopString();

            int Category = Packet.PopInt();
            int MaxVisitors = Packet.PopInt();//10 = min, 25 = max.
            int TradeSettings = Packet.PopInt();//2 = All can trade, 1 = owner only, 0 = no trading.

            if (Name.Length < 3)
                return;

            if (Name.Length > 25)
                return;

            RoomModel RoomModel = null;
            if (!PolarEnvironment.GetGame().GetRoomManager().TryGetModel(ModelName, out RoomModel))
                return;

            SearchResultList SearchResultList = null;
            if (!PolarEnvironment.GetGame().GetNavigator().TryGetSearchResultList(Category, out SearchResultList))
                Category = 9;
            
            if (SearchResultList.CategoryType != NavigatorCategoryType.CATEGORY || SearchResultList.RequiredRank > Session.GetHabbo().Rank)
                Category = 9;

            if (MaxVisitors < 10 || MaxVisitors > 25)
                MaxVisitors = 10;

            if (TradeSettings < 0 || TradeSettings > 2)
                TradeSettings = 0;

            string City = SearchResultList.CategoryIdentifier.ToString();
            RoomData NewRoom = PolarEnvironment.GetGame().GetRoomManager().CreateRoom(Session, Name, Description, ModelName, Category, City, MaxVisitors, TradeSettings);
            if (NewRoom != null)
            Session.GetRoleplay().BankChequings -= 75000;
            Session.GetHabbo().UpdateCreditsBalance();
            Session.SendMessage(new FlatCreatedComposer(NewRoom.Id, Name));
        }
    }
}
