using Polar.HabboHotel.Rooms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace Polar.Communication.Packets.Outgoing.Rooms.Settings
{
    internal class RoomSettingsDataComposer : ServerPacket
    {
        public RoomSettingsDataComposer(Room Room)
            : base(ServerPacketHeader.RoomSettingsDataMessageComposer)
        {
            base.WriteInteger(Room.RoomId);
            base.WriteString(Room.Name);
            base.WriteString(Room.Description);
            base.WriteInteger(Room.State);
            base.WriteInteger(Room.Category);
            base.WriteInteger(Room.UsersMax);
            //base.WriteInteger(((Room.RoomData.Model.MapSizeX * Room.RoomData.Model.MapSizeY) > 100) ? 50 : 25);
            base.WriteInteger(Room.UsersMax);
            if (Room.Tags.Count > 0)
            {
                base.WriteInteger(Room.Tags.Count);
                foreach (string tag in Room.Tags)
                {
                    base.WriteString(tag);
                }
            }
            else
            {
                base.WriteInteger(0);
            }

            base.WriteInteger(Room.TradeSettings); //Trade
            base.WriteInteger(Room.AllowPets ? 1 : 0); // allows pets in room - pet system lacking, so always off
            base.WriteInteger(Room.AllowPetsEating ? 1 : 0);// allows pets to eat your food - pet system lacking, so always off
            base.WriteInteger(Room.RoomBlockingEnabled ? 1 : 0);
            base.WriteInteger(Room.Hidewall ? 1 : 0);
            base.WriteInteger(Room.WallThickness);
            base.WriteInteger(Room.FloorThickness);

            base.WriteInteger(Room.chatMode);//Chat mode
            base.WriteInteger(Room.chatSize);//Chat size
            base.WriteInteger(Room.chatSpeed);//Chat speed
            base.WriteInteger(Room.chatDistance);//Hearing Distance
            base.WriteInteger(Room.extraFlood);//Additional Flood

            base.WriteBoolean(false);

            base.WriteInteger(Room.WhoCanMute); // who can mute
            base.WriteInteger(Room.WhoCanKick); // who can kick
            base.WriteInteger(Room.WhoCanBan); // who can ban
        }
    }
}
