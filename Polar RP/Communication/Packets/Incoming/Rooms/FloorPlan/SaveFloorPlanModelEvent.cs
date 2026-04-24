using System;
using System.Linq;
using System.Text;
using System.Data;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboRoleplay.Bots.Manager;
using System.Threading;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Rooms.Session;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Items;
using Polar.Communication.Packets.Outgoing.Notifications;
using Polar.HabboRoleplay.ApartmentsOwned;

namespace Polar.Communication.Packets.Incoming.Rooms.FloorPlan
{
    internal class SaveFloorPlanModelEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            if (!Session.GetHabbo().InRoom)
                return;

            Room Room = Session.GetHabbo().CurrentRoom;
            if (Room == null || Session.GetHabbo().CurrentRoomId != Room.Id || !Room.CheckRights(Session, true))
                return;

            ApartmentOwned AP = PolarEnvironment.GetGame().GetApartmentOwnedManager().GetApartmentOwnedById(Room.Id);
            if (AP != null && !AP.FloorEditor)
            {
                Session.SendNotification("¡Este apartamento no tiene uso de Floor Editor!");
                return;
            }

            char[] validLetters =
            {
                '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f', 'g',
                'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', '\r'
            };

            string Map = Packet.PopString().ToLower().TrimEnd();

            if (Map.Length > 4159) //4096 + New Lines = 4159
            {
                Session.SendMessage(new RoomNotificationComposer("floorplan_editor.error", "errors", "(%%%general%%%): %%%too_large_area%%% (%%%max%%% 2048 %%%tiles%%%)"));
                return;
            }

            if (Map.Any(letter => !validLetters.Contains(letter)) || String.IsNullOrEmpty(Map))
            {
                Session.SendMessage(new RoomNotificationComposer("floorplan_editor.error", "errors", "¡Vaya, parece que ingresaste un mapa de planta no válido!"));
                return;
            }

            //var modelData = Map.Split((char)13);
            var modelData = Map.Split('\r');

            int SizeY = modelData.Length;
            int SizeX = modelData[0].Length;

            if (SizeY > 64 || SizeX > 64)
            {
                Session.SendMessage(new RoomNotificationComposer("floorplan_editor.error", "errors", "¡La altura y el ancho máximos de un modelo son 64x64!"));
                return;
            }

            int lastLineLength = 0;
            bool isValid = true;

            foreach (var data in modelData)
            {
                if (lastLineLength == 0)
                {
                    lastLineLength = data.Length;
                    continue;
                }

                if (lastLineLength != data.Length)
                {
                    isValid = false;
                }
            }

            if (!isValid)
            {
                Session.SendMessage(new RoomNotificationComposer("floorplan_editor.error", "errors", "¡Vaya, parece que ingresaste un mapa de planta no válido!"));
                return;
            }

            int DoorX = Packet.PopInt();
            int DoorY = Packet.PopInt();
            int DoorDirection = Packet.PopInt();
            int WallThick = Packet.PopInt();
            int FloorThick = Packet.PopInt();
            int WallHeight = Packet.PopInt();

            int DoorZ = 0;

            try
            {
                DoorZ = parse(modelData[DoorY][DoorX]);
            }
            catch { }

            if (WallThick > 1)
                WallThick = 1;

            if (WallThick < -2)
                WallThick = -2;

            if (FloorThick > 1)
                FloorThick = 1;

            if (FloorThick < -2)
                WallThick = -2;

            if (WallHeight < 0)
                WallHeight = 0;

            if (WallHeight > 15)
                WallHeight = 15;

            string ModelName = "model_custom";

            Map += '\r' + new string('x', SizeX);

            DataRow Row = null;
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT * FROM `room_models_customs` WHERE `room_id` = @model LIMIT 1");
                dbClient.AddParameter("model", Room.Id);
                Row = dbClient.getRow();

                if (Row == null)//The row is still null, let's insert instead.
                {
                    dbClient.SetQuery("INSERT INTO room_models_customs VALUES (@id, @doorX, @doorY, @doorZ, @doorDir, @heightmap, @murheight)");
                    dbClient.AddParameter("id", Room.Id);
                    dbClient.AddParameter("doorX", DoorX);
                    dbClient.AddParameter("doorY", DoorY);
                    dbClient.AddParameter("doorZ", DoorZ);
                    dbClient.AddParameter("doorDir", DoorDirection);
                    dbClient.AddParameter("heightmap", Map);
                    dbClient.AddParameter("murheight", WallHeight);
                    dbClient.RunQuery();
                }
                else
                {
                    dbClient.SetQuery("UPDATE `room_models_customs` SET `heightmap` = @Map, `door_x` = @DoorX, `door_y` = @DoorY, `door_z` = @DoorZ,  `door_dir` = @DoorDirection, `wall_height` = @WallHeight WHERE `room_id` = @RId LIMIT 1");
                    dbClient.AddParameter("RId", Room.Id);
                    dbClient.AddParameter("Map", Map);
                    dbClient.AddParameter("DoorX", DoorX);
                    dbClient.AddParameter("DoorY", DoorY);
                    dbClient.AddParameter("DoorZ", DoorZ);
                    dbClient.AddParameter("DoorDirection", DoorDirection);
                    dbClient.AddParameter("WallHeight", WallHeight);
                    dbClient.RunQuery();
                }

                dbClient.SetQuery("UPDATE `rooms` SET `model_name` = @ModelName, `wallthick` = @WallThick, `floorthick` = @FloorThick WHERE `id` = '" + Room.Id + "' LIMIT 1");
                dbClient.AddParameter("ModelName", "model_custom");
                dbClient.AddParameter("WallThick", WallThick);
                dbClient.AddParameter("FloorThick", FloorThick);
                dbClient.RunQuery();
            }

            Room.RoomData.ModelName = ModelName;
            Room.RoomData.WallThickness = WallThick;
            Room.RoomData.FloorThickness = FloorThick;

            List<RoomUser> UsersToReturn = Room.GetRoomUserManager().GetRoomUsers().ToList();

            //PolarEnvironment.GetGame().GetRoomManager().ReloadModel(ModelName);
            PolarEnvironment.GetGame().GetRoomManager().UnloadRoom(Room);

            foreach (RoomUser User in UsersToReturn)
            {
                if (User == null || User.GetClient() == null)
                    continue;

                //User.GetClient().SendMessage(new RoomForwardComposer(Room.Id));
                RoleplayManager.SendUserOld2(User.GetClient(), Room.Id, "¡La sala en la que estabas fue vaciada debido a que un constructor de sala editó el modelo de habitación!");
                //RoleplayBotManager.DeployCachedBots(Room);
            }
        }

        public static short parse(char input)
        {

            switch (input)
            {
                default:
                case '0':
                    return 0;
                case '1':
                    return 1;
                case '2':
                    return 2;
                case '3':
                    return 3;
                case '4':
                    return 4;
                case '5':
                    return 5;
                case '6':
                    return 6;
                case '7':
                    return 7;
                case '8':
                    return 8;
                case '9':
                    return 9;
                case 'a':
                    return 10;
                case 'b':
                    return 11;
                case 'c':
                    return 12;
                case 'd':
                    return 13;
                case 'e':
                    return 14;
                case 'f':
                    return 15;
                case 'g':
                    return 16;
                case 'h':
                    return 17;
                case 'i':
                    return 18;
                case 'j':
                    return 19;
                case 'k':
                    return 20;
                case 'l':
                    return 21;
                case 'm':
                    return 22;
                case 'n':
                    return 23;
                case 'o':
                    return 24;
                case 'p':
                    return 25;
                case 'q':
                    return 26;
                case 'r':
                    return 27;
                case 's':
                    return 28;
                case 't':
                    return 29;
                case 'u':
                    return 30;
                case 'v':
                    return 31;
                case 'w':
                    return 32;
            }
        }
    }
}