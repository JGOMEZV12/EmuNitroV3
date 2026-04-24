using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.HabboHotel.Subscriptions;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.HabboHotel.Items;
using Newtonsoft.Json;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Owners
{
    class MPUCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_mpu"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Te da la habilidad de correr."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            ICollection<Item> Items = Session.GetHabbo().CurrentRoom.GetRoomItemHandler().GetFloor;
            // Item Item = Room.GetRoomItemHandler().GetItem(Id);
            var FloorItems = Room.GetRoomItemHandler().GetFloor.Where(x => x.GetBaseItem().InteractionType == InteractionType.BACKGROUND).ToList();
            //Item[] FloorItems = Session.GetHabbo().CurrentRoom.GetRoomItemHandler().GetFloor.Where(x => x.GetBaseItem().InteractionType == InteractionType.BACKGROUND).ToArray();
            int id = 0;
            var pares = new List<int>();
            List<int> Lista = new List<int>();
            int[] data = { };
            foreach (Item item in FloorItems)
                {
                    Lista.Add(item.Id);


                }
            foreach (var aPart in Lista)
            {
                pares.Add(aPart);
                //co
            }

            List<string> strings = Lista.ConvertAll<string>(x => x.ToString());
            //Console.WriteLine(String.Join(",", strings));
            PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(Session, "compose_mpu|"+ String.Join(",", strings) + "|vacio");
                return;

        }
    }
}
