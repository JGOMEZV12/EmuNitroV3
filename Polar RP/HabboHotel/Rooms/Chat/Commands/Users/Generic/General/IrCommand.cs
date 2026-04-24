using System;
using System.Threading;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.Communication.Packets.Outgoing.Navigator;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.Users.Effects;
using Polar.HabboRoleplay.Vehicles;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.VehicleOwned;
using Polar.HabboRoleplay.VehiclesJobs;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.General
{
    class IrCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_general_taxi"; }
        }

        public string Parameters
        {
            get { return "%room_id%"; }
        }

        public string Description
        {
            get { return "Ve con tu carro  a la habitación elegida id."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            UInt32 RoomId = 0;
            bool IsVip = Session.GetHabbo().VIPRank < 1 ? false : true;
            int Cost = IsVip ? 0 : 2;
            bool OnDuty = false;

            if (Params.Length == 1)
            {
                Session.SendWhisper("¡Ups, se le olvidó ingresar un ID de habitación!", 1);
                return;
            }

            if (Session.GetHabbo().VIPRank < 1)
            {
                Session.SendWhisper("No eres vip para utilizar este comando.", 1);
                return;
            }

            if (!UInt32.TryParse(Params[1].ToString(), out RoomId))
            {
                Session.SendWhisper("Por favor ingrese un número valido.", 1);
                return;
            }
            /*if (Session.GetRoleplay().DrivingCar)
            {
                Session.SendWhisper("No puedes hacer eso mientras conduces.", 1);
                return;
            }*/
            if (Session.GetRoleplay().Cuffed)
            {
                Session.SendWhisper("No puedes hacer eso mientras estás esposad@", 1);
                return;
            }
            if (!Session.GetRoomUser().CanWalk)
            {
                Session.SendWhisper("Al parecer no puedes ni moverte para hacer eso.", 1);
                return;
            }
            if (Session.GetRoleplay().Pasajero)
            {
                Session.SendWhisper("¡No puedes hacer eso mientras vas de Pasajer@!", 1);
                return;
            }

            if (Session.GetRoleplay().IsDead)
            {
                Session.SendWhisper("¡No puedes conducir mientras estás muerto!", 1);
                return;
            }

            if (Session.GetRoleplay().IsJailed)
            {
                Session.SendWhisper("¡No puedes ir en carro mientras estás encarcelado!", 1);
                return;
            }

            if (Session.GetRoomUser() != null)
            {
                if (Session.GetRoomUser().Frozen)
                {
                    Session.SendWhisper("Usted no puede manejar mientras está congelado o aturdido", 1);
                    return;
                }
            }

            if (!Room.TaxiFromEnabled && !OnDuty)
            {
                Session.SendWhisper("No puedes llegar en carro a esta sala", 1);
                return;
            }

            if (RoomId == Session.GetHabbo().CurrentRoomId)
            {
                Session.SendWhisper("¡Ya estás en esa habitación!", 1);
                return;
            }

            RoomData roomData = PolarEnvironment.GetGame().GetRoomManager().GenerateRoomData(Convert.ToInt32(RoomId));

            if (roomData == null)
            {
                Session.SendWhisper("El GPS de tu carro no encuentra ese lugar", 1);
                return;
            }

            if (!roomData.TaxiToEnabled && !OnDuty)
            {
                Session.SendWhisper("Tu carro no tiene permitido llegar hasta este sitio de la ciudad", 1);
                return;
            }

            if (roomData.TutorialEnabled && !OnDuty)
            {
                Session.SendWhisper("No se puede ir en carro a esta sala tutorial", 1);
                return;
            }

            if (Session.GetHabbo().CurrentRoom != null)
            {
                if (Session.GetHabbo().CurrentRoom.TutorialEnabled && !OnDuty)
                {
                    Session.SendWhisper("¡No puedes salir de una sala de tutorial! Termine el tutorial en su lugar.", 1);
                    return;
                }
            }


            if (Session.GetRoleplay().TexasHoldEmPlayer > 0)
            {
                Session.SendWhisper("No puede irse en el carro en medio de un Texas Hold 'Em juego", 1);
                return;
            }


            if (Session.GetRoleplay().BasuTrashCount > 0)
            {
                Session.SendWhisper("No puedes llevar basura en tu carro", 1);
                return;
            }

            if (Session.GetRoleplay().TryGetCooldown("ir"))
                return;


            if (!OnDuty)
            {

                if(Session.GetRoleplay().DrivingCar)
                {
                    //RoleplayManager.Shout(Session, "*Encendió el motor de su vehículo*", 5);
                    RoleplayManager.SendUserOld2(Session, roomData.Id);
                    Session.GetRoleplay().CooldownManager.CreateCooldown("ir", 1000, 10);
                    PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_vehicle", "open");// WS FUEL

                }

            }

        }
    }
}