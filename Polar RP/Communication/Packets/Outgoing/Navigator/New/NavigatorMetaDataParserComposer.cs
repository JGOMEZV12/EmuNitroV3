using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Navigator;

namespace Polar.Communication.Packets.Outgoing.Navigator
{
    internal class NavigatorMetaDataParserComposer : ServerPacket
    {
        public ICollection<TopLevelItem> TopLevelItems { get; }
        public NavigatorMetaDataParserComposer(ICollection<TopLevelItem> topLevelItems)
            : base(ServerPacketHeader.NavigatorMetaDataParserMessageComposer)
        {
            this.TopLevelItems = topLevelItems;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(TopLevelItems.Count);//Count
            foreach (TopLevelItem TopLevelItem in TopLevelItems.ToList())
            {
                //TopLevelContext
                packet.WriteString(TopLevelItem.SearchCode);//Search code
                packet.WriteInteger(0);//Count of saved searches?
                /*{
                    //SavedSearch
                    packet.WriteInteger(TopLevelItem.Id);//Id
                   packet.WriteString(TopLevelItem.SearchCode);//Search code
                   packet.WriteString(TopLevelItem.Filter);//Filter
                   packet.WriteString(TopLevelItem.Localization);//localization
                }*/
            }
        }
    }
}

