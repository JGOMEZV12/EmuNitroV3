using Polar.Communication.Packets.Outgoing.HabboCamera;
using Polar.HabboHotel.GameClients;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.Communication.Packets.Incoming.HabboCamera
{
    internal class PublishCameraPictureEvent : IPacketEvent
    {
        public void Parse(GameClient Session, ClientPacket Packet)
        {
           /* var pic = HabboCameraManager.GetUserPurchasePic(Session);
            if (pic == null)
                return;

            int InsetId;
            using (var Adap = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                Adap.SetQuery("INSERT INTO server_pictures_publish (picture_id) VALUES (@pic)");
                Adap.AddParameter("pic", pic.pictureid);
                InsetId = (int)Adap.InsertQuery();
            }

            Session.SendMessage(new CameraFinishPublishComposer(InsetId));*/

        }
    }
}
