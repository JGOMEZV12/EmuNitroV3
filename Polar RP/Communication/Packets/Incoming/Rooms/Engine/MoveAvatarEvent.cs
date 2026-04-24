using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.Communication.Packets.Outgoing.Rooms.Nux;
using Polar.HabboRoleplay.Timers;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Items;
using System.Drawing;
using Polar.HabboRoleplay.RoleplayUsers;

namespace Polar.Communication.Packets.Incoming.Rooms.Engine
{
    class MoveAvatarEvent : IPacketEvent
    {
        public async void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            Room Room = Session.GetHabbo().CurrentRoom;
            if (Room == null)
                return;

            RoomUser User = Room.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);

            if (User == null || (!User.CanWalk && !User.TeleportEnabled))
                return;


            int MoveX = Packet.PopInt();
            int MoveY = Packet.PopInt();

            if (MoveX == User.X && MoveY == User.Y)
                return;

            #region Camionero
            if (Session.GetRoleplay().ViewCamCargas)
            {
                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_camionero", "close");
                Session.SendWhisper("Has dejado de mirar los cargamentos.", 1);
            }
            #endregion

            #region Products
            if (Session.GetRoleplay().ViewProducts)
            {
                // WS Products
                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_products", "close");
                Session.GetRoleplay().ViewProducts = false;
            }
            #endregion

            #region Armero
            if (Session.GetRoleplay().ViewArmeroPieces)
            {
                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_armero", "close_pieces");
                Session.SendWhisper("Has dejado de mirar el creador de piezas.", 1);
            }
            if (Session.GetRoleplay().ViewArmeroWeapons)
            {
                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_armero", "close_weapons");
                Session.SendWhisper("Has dejado de mirar el creador de armas.", 1);
            }
            #endregion

            #region Hospital
            if (Session.GetRoleplay().ViewHospBotiq)
            {
                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_hospital", "close_botiq");
                Session.SendWhisper("Has dejado de mirar el botiquín", 1);
            }
            #endregion

            #region Phone Shop
            if (Session.GetRoleplay().ViewShopPhones)
            {
                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_phone", "close_shop");
                Session.GetRoleplay().ViewShopPhones = false;
            }
            #endregion

            #region Change Name
            if (Session.GetRoleplay().ViewChangeName)
            {
                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_changename", "close");
                Session.GetRoleplay().ViewChangeName = false;
            }
            #endregion

            if (User.RidingHorse)
            {
                RoomUser Horse = Room.GetRoomUserManager().GetRoomUserByVirtualId(User.HorseID);
                if (Horse != null)
                    Horse.MoveTo(MoveX, MoveY);
            }


            if (Session.GetRoleplay().IsFuelCharging || Session.GetRoleplay().IsCamLoading || Session.GetRoleplay().IsMecLoading || Session.GetRoleplay().TurfCapturing || /*Session.GetRoleplay().Robbery || Session.GetRoleplay().BankCapturing || */Session.GetRoleplay().ATMRobbery)
                Session.GetRoleplay().BreakGeneralTimer = true;

            if (Session.GetRoleplay().TogglingPSV)
            {
                Session.SendWhisper("Cambio del Modo pasivo cancelado.", 1);
            }

            if (User.GetClient().GetRoleplay().Chofer)
                User.MoveDriving(MoveX, MoveY, User);

            if (!User.GetClient().GetRoleplay().DrivingCar)
                PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(User.GetClient(), "compose_carnew|close");// Sum 6 para 90
            else
                PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(User.GetClient(), "compose_carnew|stop");// Sum 6 para 90

            if (User.ReverseWalk)
            {
                MoveX = User.SetX + (User.SetX - MoveX);
                MoveY = User.SetY + (User.SetY - MoveY);
            }

            User.MoveTo(MoveX, MoveY);
        }
    }
}