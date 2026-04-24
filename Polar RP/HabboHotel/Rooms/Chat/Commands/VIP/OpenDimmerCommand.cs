using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboRoleplay.Houses;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Items.Data.Moodlight;
using Polar.Communication.Packets.Outgoing.Rooms.Furni.Moodlight;

namespace Polar.HabboHotel.Rooms.Chat.Commands.VIP
{
    class OpenDimmerCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_open_dimmer"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Establezca una altura para que los muebles sean apilados."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Room.MoodlightData == null)
            {
                foreach (Item Item in Room.GetRoomItemHandler().GetWall.ToList())
                {
                    if (Item.GetBaseItem().InteractionType == InteractionType.MOODLIGHT)
                        Room.MoodlightData = new MoodlightData(Item.Id);
                }
            }

            if (Room.MoodlightData == null)
            {
                Session.SendWhisper("Vaya, parece ser que no hay moodlights en esta habitación", 1);
                return;
            }

            Session.SendMessage(new MoodlightConfigComposer(Room.MoodlightData));
        }
    }
}