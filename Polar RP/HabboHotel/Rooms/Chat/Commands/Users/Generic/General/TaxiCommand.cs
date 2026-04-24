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
using System.Threading.Tasks;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.General
{
    class TaxiCommand : IChatCommand
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
            get { return "Pide un taxi para enviarte a la habitación elegida id."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            // Verificar si Room es null
            if (Room == null)
            {
                Session.SendWhisper("Debes estar en una habitación para usar este comando.", 1);
                return;
            }

            RoomUser User = Session.GetRoomUser();
            if (User == null)
            {
                Session.SendWhisper("Error al obtener información del usuario en la habitación.", 1);
                return;
            }
            Int32 RoomId = 0;
            bool IsVip = Session.GetHabbo().VIPRank > 0 ? true : false;
            int Cost = IsVip ? 0 : 3;
            int Time = IsVip ? 2 : (4 + DayNightManager.GetTaxiTime()); //Vip: 2s & Normal: 4s
            string TaxiText = IsVip ? " ESPECIAL VIP" : "";
            bool OnDuty = false;

            if (Params.Length == 1)
            {
                Session.SendWhisper("¡Ups, se le olvidó ingresar un ID de habitación!", 1);
                return;
            }

            if (!Int32.TryParse(Params[1].ToString(), out RoomId))
            {
                Session.SendWhisper("Por favor ingrese un número valido.", 1);
                return;
            }

            var ApartInside = PolarEnvironment.GetGame().GetApartmentOwnedManager().GetApartmentByInsideRoom(RoomId);

            if (ApartInside != null)
            {
                Session.SendWhisper("¡No puedes ir a un apartamento en taxi!", 1);
                return;
            }
            if (Session.GetRoleplay().IsDead)
            {
                Session.SendWhisper("¡No puedes taxiar mientras estás muerto!", 1);
                return;
            }

            #region Paralizado
            if (Session.GetRoleplay().IsStun)
            {
                Session.SendWhisper("Estás paralizado, no puedes solicitar un taxi..", 1);
                return;
            }
            #endregion

            if (Session.GetRoleplay().IsJailed)
            {
                Session.SendWhisper("¡No puedes taxi mientras estás encarcelado!", 1);
                return;
            }

            if (User.isLying)
            {
                Session.SendWhisper("¡No puedes taxi mientras estás acostado!", 1);
                return;
            }
            
            if (Session.GetRoleplay().DrivingCar)
            {
                Session.SendWhisper("¡No puedes usar taxi mientras estás manejando!", 1);
                return;
            }

            if (Session.GetRoleplay().IsFuelCharging || Session.GetRoleplay().IsCamLoading || Session.GetRoleplay().IsMecLoading || Session.GetRoleplay().TurfCapturing || Session.GetRoleplay().Robbery || Session.GetRoleplay().BankCapturing || Session.GetRoleplay().ATMRobbery || Session.GetRoleplay().Learning)
                Session.GetRoleplay().BreakGeneralTimer = true;

            if (Session.GetRoleplay().CurAlcohol >= Session.GetRoleplay().MaxAlcohol)
            {
                Session.SendWhisper("¡Estas borracho! Tanto que no puedes ni llamar a un taxi porque no sabes donde dejaste el tlf", 1);
                Session.SendWhisper("¡Fuma un cigarrillo, tal vez dos para relajarte y bajar tu estado de ebriedad!", 1);
                return;
            }

            if (Session.GetRoomUser() != null)
            {
                if (Session.GetRoomUser().Frozen)
                {
                    Session.SendWhisper("Usted no puede taxi mientras está congelado o aturdido", 1);
                    return;
                }
            }

            if (Session.GetHabbo().GetPermissions().HasRight("mod_tool") && Session.GetRoleplay().StaffOnDuty)
                OnDuty = true;

            #region Groups
            // WS Groups
            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_group", "close");
            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_group", "open");
            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_gang", "turf_cap_off");
            #endregion

            #region Products
            if (Session.GetRoleplay().ViewProducts)
            {
                // WS Products
                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_products", "close");
                Session.GetRoleplay().ViewProducts = false;
            }
            #endregion

            #region Change Name
            if (Session.GetRoleplay().ViewChangeName)
            {
                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_changename", "close");
                Session.GetRoleplay().ViewChangeName = false;
            }
            #endregion

            #region Car List
            if (Session.GetRoleplay().ViewCarList)
            {
                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_vehicle", "closeshop");
                Session.GetRoleplay().ViewCarList = false;
            }
            #endregion

            #region Weapon List
            if (Session.GetRoleplay().ViewWeaponsList)
            {
                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_shop", "closeshop");
                Session.GetRoleplay().ViewWeaponsList = false;
            }
            #endregion

            #region Apart List
            if (Session.GetRoleplay().ViewApartments)
            {
                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_apart", "apart_close");
                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_apart", "close");
                Session.GetRoleplay().ViewApartments = false;
            }
            #endregion

            if (Session.GetRoomUser() != null)
            {
                if (Session.GetRoomUser().CurrentEffect == 23)
                    Session.GetRoomUser().ApplyEffect(0);
            }

            if (Room != null && !Room.TaxiFromEnabled && !OnDuty)
            {
                Session.SendWhisper("[TAXISTA] Sorry, No podemos usar taxi fuera de esta sala", 1);
                return;
            }

            if (Session.GetRoleplay().Cuffed)
            {
                Session.SendWhisper("¡Los conductores de taxi están demasiado asustados para conducirle alrededor con esas esposas pegadas a usted!", 1);
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
                Session.SendWhisper("[TAXISTA] ¡No tienes suficiente dinero para dar un paseo!", 1);
                return;
            }

            bool PoliceTool = false;
            if (Session.GetRoleplay().GuideOtherUser != null)
            {
                if (HabboHotel.Groups.GroupManager.HasJobCommand(Session, "guide"))
                {
                    if (Session.GetRoleplay().GuideOtherUser.GetHabbo() != null && Session.GetRoleplay().GuideOtherUser.GetRoomUser() != null)
                    {
                        if (RoomId == Session.GetRoleplay().GuideOtherUser.GetRoomUser().RoomId)
                            PoliceTool = true;
                    }
                }
            }

            if (Session.GetRoleplay().InsideTaxi)
            {
                Session.SendWhisper("[TAXISTA] Ya te estoy recogiendo! Tipo ':notaxi' si cambias de opinión!", 1);
                return;
            }

            RoomData roomData = PolarEnvironment.GetGame().GetRoomManager().GenerateRoomData(Convert.ToInt32(RoomId));

            if (roomData == null)
            {
                Session.SendWhisper("[TAXISTA] Sorry, No pudimos encontrar esa habitación", 1);
                return;
            }

            if (!roomData.TaxiToEnabled && !OnDuty && !PoliceTool)
            {
                Session.SendWhisper("[TAXISTA] Lo sentimos, pero no puedo llevarte allí.", 1);
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
                Session.SendWhisper("No se puede taxi en medio de un Texas Hold 'Em juego", 1);
                return;
            }

            if (Session.GetRoleplay().BasuTrashCount > 0)
            {
                Session.SendWhisper("no puedes llevar basura en un taxi", 1);
                return;
            }

            Session.GetRoleplay().InsideTaxi = true;
            bool PoliceTaxi = false;

            if (!OnDuty)
            {
                if (HabboHotel.Groups.GroupManager.HasJobCommand(Session, "guide") && Session.GetRoleplay().IsWorking)
                {
                    Cost = 0;
                    Time = 1;

                    if (Session.GetRoomUser() != null)
                        Session.GetRoomUser().ApplyEffect(19);

                    Session.Shout("*Saque su Radio de Policía y pide una rápida recolección para " + roomData.Name + " [ID: " + RoomId + "]*", 4);
                    PoliceTaxi = true;
                }
                else
                {
                    if (Session.GetRoomUser() != null)
                        Session.GetRoomUser().ApplyEffect(805);

                    Session.Shout("*Llama a un Taxi" + TaxiText + " para ir a " + roomData.Name + " [ID: " + RoomId + "]*", 4);
                }

                Task.Run(async delegate
                {
                    for (int i = 0; i < Time * 10; i++)
                    {
                         if (Session.GetRoleplay() == null)
                             break;

                         if (Session.GetRoleplay().InsideTaxi)
                             await Task.Delay(100);
                         else
                             break;
                     }
                if (Session.GetRoleplay() != null)
                {
                    if (Session.GetRoleplay().InsideTaxi)
                    {
                        if (Cost > 0)
                        {
                            Session.GetHabbo().Credits -= Cost;
                            Session.GetHabbo().UpdateCreditsBalance();
                        }

                        if (PoliceTaxi)
                        {
                            if (Session.GetRoomUser() != null)
                                Session.GetRoomUser().ApplyEffect(19);
                            Session.Shout("*¡Sube al coche de policía de su socio mientras que lo ven tiran para arriba!*", 4);
                        }
                        else
                            Session.Shout("*Sube dentro de su Taxi " + TaxiText + " y se marcha*", 4);

                        RoleplayManager.SendUserOld2(Session, roomData.Id);
                    }
                }
                });
            }
            else
            {
                if (PoliceTool)
                {
                    if (Session.GetRoomUser() != null)
                        Session.GetRoomUser().ApplyEffect(510);
                    Session.Shout("*Salta dentro de su coche de la policía y se va a ayudar a un ciudadano en necesidad*", 1);
                }
                else if (OnDuty)
                {
                    Session.Shout("*Sube dentro de su Staff Mobile y se marcha *", 1);
                }
                RoleplayManager.SendUserOld2(Session, roomData.Id);
            }
        }
    }
}