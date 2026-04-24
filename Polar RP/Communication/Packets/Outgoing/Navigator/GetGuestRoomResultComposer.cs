using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Polar.HabboHotel.Users;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;

namespace Polar.Communication.Packets.Outgoing.Navigator
{
    internal class GetGuestRoomResultComposer : ServerPacket
    {
      
        public GetGuestRoomResultComposer(GameClient session, RoomData data, bool isLoading, bool checkEntry)
            : base(ServerPacketHeader.GetGuestRoomResultMessageComposer)
        {
        
            base.WriteBoolean(isLoading);
            base.WriteInteger(data.Id);
            base.WriteString(data.Name);
            base.WriteInteger(data.OwnerId);
            base.WriteString(data.OwnerName);
            base.WriteInteger(data.State);
            base.WriteInteger(data.UsersNow);
            base.WriteInteger(data.UsersMax);
            base.WriteString(data.Description);
            base.WriteInteger(data.TradeSettings);
            base.WriteInteger(data.Score);
            base.WriteInteger(0);//Top rated room rank.
            base.WriteInteger(data.Category);

            base.WriteInteger(data.Tags.Count);
            foreach (string Tag in data.Tags)
            {
                base.WriteString(Tag);
            }

            if (data.Group != null && data.Promotion != null)
            {
                base.WriteInteger(62);//What?

                base.WriteInteger(data.Group == null ? 0 : data.Group.Id);
                base.WriteString(data.Group == null ? "" : data.Group.Name);
                base.WriteString(data.Group == null ? "" : data.Group.Badge);

                base.WriteString(data.Promotion != null ? data.Promotion.Name : "");
                base.WriteString(data.Promotion != null ? data.Promotion.Description : "");
                base.WriteInteger(data.Promotion != null ? data.Promotion.MinutesLeft : 0);
            }
            else if (data.Group != null && data.Promotion == null)
            {
                base.WriteInteger(58);//What?
                base.WriteInteger(data.Group == null ? 0 : data.Group.Id);
                base.WriteString(data.Group == null ? "" : data.Group.Name);
                base.WriteString(data.Group == null ? "" : data.Group.Badge);
            }
            else if (data.Group == null && data.Promotion != null)
            {
                base.WriteInteger(60);//What?
                base.WriteString(data.Promotion != null ? data.Promotion.Name : "");
                base.WriteString(data.Promotion != null ? data.Promotion.Description : "");
                base.WriteInteger(data.Promotion != null ? data.Promotion.MinutesLeft : 0);
            }
            else
            {
                base.WriteInteger(56);//What?
            }


            base.WriteBoolean(checkEntry);
            base.WriteBoolean(false);
            base.WriteBoolean(false);
            base.WriteBoolean(false);

            base.WriteInteger(data.WhoCanMute);
            base.WriteInteger(data.WhoCanKick);
            base.WriteInteger(data.WhoCanBan);

            bool HasRights = false;
            if (session != null && session.GetHabbo() != null && session.GetHabbo().GetPermissions() != null)
            {
                if (session.GetHabbo().GetPermissions().HasRight("mod_tool"))
                    HasRights = true;

                if (data.OwnerId == session.GetHabbo().Id)
                    HasRights = true;
            }

            base.WriteBoolean(HasRights);
            base.WriteInteger(data.chatMode);
            base.WriteInteger(data.chatSize);
            base.WriteInteger(data.chatSpeed);
            base.WriteInteger(data.extraFlood);//Flood!!
            base.WriteInteger(data.chatDistance);//Hearing distance
        }
    }
}