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

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.General
{
    class BusCommand : IChatCommand
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
            get { return "Pide un Bus para enviarte a la habitación elegida id."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            UInt32 RoomId = 0;
            bool IsVip = Session.GetHabbo().VIPRank > 0 ? true : false;
            int Cost = IsVip ? 0 : 4;
            int Time = IsVip ? (3 + DayNightManager.GetTaxiTime()) : (8 + DayNightManager.GetTaxiTime()); //Vip: 2s & Normal: 4s
            string BusText = IsVip ? " VIP" : "";
            bool OnDuty = false;

            if (Params.Length == 1)
            {
                Session.SendWhisper("¡Ups, se le olvidó ingresar un ID de habitación!", 1);
                return;
            }

            if (!UInt32.TryParse(Params[1].ToString(), out RoomId))
            {
                Session.SendWhisper("Por favor ingrese un número valido.", 1);
                return;
            }

            if (!Session.GetRoleplay().NearItem("holorp_busstop", 1))
            {
                Session.SendWhisper("Ve a la parada y usa nuevamente el comando para ir en bus");
                return;
            }

            if (Session.GetRoleplay().IsDead)
            {
                Session.SendWhisper("¡No puedes Busar mientras estás muerto!", 1);
                return;
            }

            if (Session.GetRoleplay().IsJailed)
            {
                Session.SendWhisper("¡No puedes Bus mientras estás encarcelado!", 1);
                return;
            }

            if (Session.GetRoleplay().CurAlcohol >= Session.GetRoleplay().MaxAlcohol)
            {
                Session.SendWhisper("¡Estas borracho! Tanto que no puedes ni llamar a un Bus porque no sabes donde dejaste el tlf", 1);
                Session.SendWhisper("¡Fuma un cigarrillo, tal vez dos para relajarte y bajar tu estado de ebriedad!", 1);
                return;
            }

            if (Session.GetRoomUser() != null)
            {
                if (Session.GetRoomUser().Frozen)
                {
                    Session.SendWhisper("Usted no puede Bus mientras está congelado o aturdido", 1);
                    return;
                }
            }

            if (Session.GetHabbo().GetPermissions().HasRight("mod_tool") && Session.GetRoleplay().StaffOnDuty)
                OnDuty = true;
            if (Session.GetHabbo().VIPRank > 0)
                OnDuty = true;

            if (!Room.TaxiFromEnabled && !OnDuty)
            {
                Session.SendWhisper("[Conductor de BUS] Sorry, No podemos usar Bus fuera de esta sala", 1);
                return;
            }

            if (Session.GetRoleplay().Cuffed)
            {
                Session.SendWhisper("¡Los conductores de Bus están demasiado asustados para conducirle alrededor con esas esposas pegadas a usted!", 1);
                return;
            }

            if (RoomId == Session.GetHabbo().CurrentRoomId)
            {
                Session.SendWhisper("¡Ya estás en esa habitación!", 1);
                return;
            }

            bool PoliceCost = false;
            if (HabboHotel.Groups.GroupManager.HasJobCommand(Session, "guide") && Session.GetRoleplay().IsWorking)
                PoliceCost = true;

            if (Session.GetHabbo().Credits < Cost && Cost > 0 && !OnDuty && !PoliceCost)
            {
                Session.SendWhisper("[Conductor de BUS] ¡No tienes suficiente dinero para dar un paseo!", 1);
                return;
            }

            if (Session.GetRoleplay().InsideBus)
            {
                Session.SendWhisper("[Conductor de BUS] Ya te estoy recogiendo! Tipo ':noBus' si cambias de opinión!", 1);
                return;
            }

            RoomData roomData = PolarEnvironment.GetGame().GetRoomManager().GenerateRoomData(Convert.ToInt32(RoomId));

            if (roomData == null)
            {
                Session.SendWhisper("[Conductor de BUS] Sorry, No pudimos encontrar esa habitación", 1);
                return;
            }

            if (!roomData.BusToEnabled && !OnDuty)
            {
                Session.SendWhisper("[Conductor de BUS] Lo sentimos, pero no tenemos ese recorrido usa :businfo para ver nuestras rutas", 1);
                return;
            }

            if (roomData.TutorialEnabled && !OnDuty)
            {
                Session.SendWhisper("No se puede Bus a una sala de tutorial, lo siento", 1);
                return;
            }

            if (Session.GetHabbo().CurrentRoom != null)
            {
                if (Session.GetHabbo().CurrentRoom.TutorialEnabled && !OnDuty)
                {
                    Session.SendWhisper("¡No puedes salir de una sala de tutorías! Termine el tutorial en su lugar.", 1);
                    return;
                }
            }

            if (Session.GetRoleplay().TexasHoldEmPlayer > 0)
            {
                Session.SendWhisper("No se puede Bus en medio de un Texas Hold 'Em juego", 1);
                return;
            }

            Session.GetRoleplay().InsideBus = true;
            bool PoliceBus = false;

            if (!OnDuty)
            {
                if (HabboHotel.Groups.GroupManager.HasJobCommand(Session, "guide") && Session.GetRoleplay().IsWorking)
                {
                    Cost = 10;
                    Time = 6;

                    if (Session.GetRoomUser() != null)
                        Session.GetRoomUser().ApplyEffect(598);

                    Session.Shout("*Se monta en el bus y pide la cola a " + roomData.Name + " [ID: " + RoomId + "]*", 37);
                    PoliceBus = true;
                }
                else
                {
                    if (Session.GetRoomUser() != null)
                        Session.GetRoomUser().ApplyEffect(598);

                    Session.Shout("*Espera un bus " + BusText + " para ir a " + roomData.Name + " [ID: " + RoomId + "]*", 4);
                }

                new Thread(() =>
                {
                    for (int i = 0; i < (Time + 5) * 10; i++)
                    {
                        if (Session.GetRoleplay() == null)
                            break;

                        if (Session.GetRoleplay().InsideBus)
                            Thread.Sleep(100);
                        else
                            break;
                    }
                    if (Session.GetRoleplay() != null)
                    {
                        if (Session.GetRoleplay().InsideBus)
                        {
                            if (Cost > 0)
                            {
                                Session.GetHabbo().Credits -= Cost;
                                Session.GetHabbo().UpdateCreditsBalance();
                            }

                            if (PoliceBus)
                            {
                                if (Session.GetRoomUser() != null)
                                    Session.GetRoomUser().ApplyEffect(598);
                                Session.Shout("*¡Sube al bus de la policía!*", 37);
                            }
                            else
                                Session.Shout("*Sube al Bus" + BusText + " *", 4);
                            RoleplayManager.SendUser(Session, roomData.Id);
                        }
                    }
                }).Start();
            }
            else
            {
                Session.Shout("*Sube dentro de su carro staff y se marcha a " + roomData.Name + " [ID: " + RoomId + "]*", 23);
                Session.GetRoomUser().ApplyEffect(598);
                RoleplayManager.SendUser(Session, roomData.Id);
            }
        }
    }
}