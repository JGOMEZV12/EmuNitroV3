/*using Polar.Communication.Packets.Outgoing.Navigator;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users.Effects;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing.Navigator;
using Polar.Communication.Packets;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users.Effects;
using Polar.HabboRoleplay.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.Communication.Packets.Incoming.Navigator
{
    class GetGuestRoomEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            #region Original Algorithm (ON)

            int roomID = Packet.PopInt();
            bool RoomLoaded = false;

            RoomData roomData = PolarEnvironment.GetGame().GetRoomManager().GenerateRoomData(roomID);
            if (roomData == null)
                return;

            if (RoomLoaded)
                return;

            /*
            if (Session.GetRoleplay().Chofer || Session.GetRoleplay().EscortingWalk)
            {
                Session.SendNotification("Lo sentimos, pero no se permite hacer uso del navegador mientras llevas pasajeros o escoltados.");
                return;
            }
            if (!Session.GetHabbo().GetPermissions().HasRight("can_use_guest_navigator"))
            {
                Session.SendNotification("Lo sentimos, pero no se permite hacer uso del navegador.");
                return;
            }
            

            Boolean isLoading = Packet.PopInt() == 1;
            Boolean checkEntry = Packet.PopInt() == 1;

           

            #region Clean Websockets (Al cambiar de sala)

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


            #endregion

            #endregion

            Session.SendMessage(new GetGuestRoomResultComposer(Session, roomData, isLoading, false));
        }
    }
}
*/
using Polar.Communication.Packets.Outgoing.Navigator;
using Polar.HabboHotel.Rooms;
using System;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.Users.Effects;
using System.Threading.Tasks;

namespace Polar.Communication.Packets.Incoming.Navigator
{
    internal class GetGuestRoomEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            // Validación inicial crítica
            if (Session == null || Packet == null)
                return;

            // Obtener referencias esenciales con validación
            var habbo = Session.GetHabbo();
            var roleplay = Session.GetRoleplay();
            if (habbo == null || roleplay == null) return;

            int roomID;
            try
            {
                roomID = Packet.PopInt();
            }
            catch
            {
                return; // Paquete malformado
            }

            RoomData roomData = PolarEnvironment.GetGame()?.GetRoomManager()?.GenerateRoomData(roomID);
            if (roomData == null) return;

            // Variables originales con protección null
            bool IsVip = habbo.VIPRank > 0;
            int Cost = IsVip ? 0 : 3;
            int Time = IsVip ? 2 : (4 + DayNightManager.GetTaxiTime());
            string TaxiText = IsVip ? " VIP" : "";
            bool OnDuty = false;

            // Validación original mejorada
            if (habbo.GetPermissions()?.HasRight("mod_tool") == true && roleplay.StaffOnDuty)
                OnDuty = true;

            if (habbo.VIPRank > 1)
                OnDuty = true;

            #region Conditions (validaciones mejoradas)
            if (roleplay.IsJailed || roleplay.IsDead)
                return;

            #region Pasajero
            if (roleplay.Pasajero)
            {
                Session.SendWhisper("Eres un pasajero, baja primero del vehiculo wn", 1);
                return;
            }
            #endregion

            if (roleplay.Cuffed)
            {
                Session.SendWhisper("¡Los taxistas están demasiado asustados para llevarte con esas esposas pegadas a ti!", 1);
                return;
            }

            var roomUser = Session.GetRoomUser();
            if (roomUser?.Frozen == true)
            {
                Session.SendWhisper("¡Usted no puede llamar a un taxi mientras está congelado o aturdido!", 1);
                return;
            }
            #endregion

            Boolean isLoading = Packet.PopInt() == 1;
            Boolean checkEntry = Packet.PopInt() == 1;

            #region Groups (con validación de webEventManager)
            var webEventManager = PolarEnvironment.GetGame()?.GetWebEventManager();
            webEventManager?.ExecuteWebEvent(Session, "event_group", "close");
            webEventManager?.ExecuteWebEvent(Session, "event_group", "open");
            webEventManager?.ExecuteWebEvent(Session, "event_gang", "turf_cap_off");
            #endregion

            #region Products
            if (roleplay.ViewProducts)
            {
                webEventManager?.ExecuteWebEvent(Session, "event_products", "close");
                roleplay.ViewProducts = false;
            }
            #endregion

            #region Products
            if (roleplay.DrivingCar)
            {
                Session.SendWhisper("¡No puedes agarrar un taxi mientras conduces, AntiRolero!", 1);
                return;
            }
            #endregion

            #region Change Name
            if (roleplay.ViewChangeName)
            {
                webEventManager?.ExecuteWebEvent(Session, "event_changename", "close");
                roleplay.ViewChangeName = false;
            }
            #endregion

            #region Car List
            if (roleplay.ViewCarList)
            {
                webEventManager?.ExecuteWebEvent(Session, "event_vehicle", "closeshop");
                roleplay.ViewCarList = false;
            }
            #endregion

            #region Weapon List
            if (roleplay.ViewWeaponsList)
            {
                webEventManager?.ExecuteWebEvent(Session, "event_shop", "closeshop");
                roleplay.ViewWeaponsList = false;
            }
            #endregion

            #region Apart List
            if (roleplay.ViewApartments)
            {
                webEventManager?.ExecuteWebEvent(Session, "event_apart", "apart_close");
                webEventManager?.ExecuteWebEvent(Session, "event_apart", "close");
                roleplay.ViewApartments = false;
            }
            #endregion

            if (roomUser != null && roomUser.CurrentEffect == 23)
                roomUser.ApplyEffect(0);

            if (roomUser?.GetRoom() != null)
            {
                if (!roomUser.GetRoom().TaxiFromEnabled && !OnDuty)
                {
                    Session.SendWhisper("[TAXISTA] Lo siento, no podemos sacarte de esta habitación!", 1);
                    return;
                }
            }

            if (roomID != habbo.CurrentRoomId)
            {
                bool PoliceCost = false;
                if (HabboHotel.Groups.GroupManager.HasJobCommand(Session, "guide") && roleplay.IsWorking)
                    PoliceCost = true;

                if (habbo.Credits < Cost && Cost > 0 && !OnDuty && !PoliceCost)
                {
                    Session.SendWhisper("[TAXISTA] ¡No tienes suficiente dinero para dar un paseo!", 1);
                    return;
                }

                if (roleplay.InsideTaxi)
                {
                    Session.SendWhisper("[TAXISTA] Ya te estoy recogiendo! Tipo ':notaxi' si cambias de opinión!", 1);
                    return;
                }

                if (roleplay.InsideBus)
                {
                    Session.SendWhisper("[PARADA DE BUS] Ya un BUS viene en camino, porfavor espere...!", 1);
                    return;
                }

                bool PoliceTool = false;
                if (roleplay.GuideOtherUser != null)
                {
                    if (HabboHotel.Groups.GroupManager.HasJobCommand(Session, "guide"))
                    {
                        var guideUser = roleplay.GuideOtherUser;
                        if (guideUser?.GetHabbo() != null && guideUser.GetRoomUser() != null)
                        {
                            if (roomID == guideUser.GetRoomUser().RoomId)
                                PoliceTool = true;
                        }
                    }
                }

                if (!roomData.TaxiToEnabled && !OnDuty && !PoliceTool)
                {
                    Session.SendWhisper("[TAXISTA] ¡Lo siento, no podemos taxi a esta habitación!", 1);
                    return;
                }

                #region disabled temporary
                roleplay.InsideTaxi = true;
                roleplay.InsideBus = true;
                #endregion

                bool PoliceTaxi = false;

                if (!OnDuty && !PoliceTool && habbo.CurrentRoomId > 0)
                {
                    #region disabled temporary
                    if (HabboHotel.Groups.GroupManager.HasJobCommand(Session, "guide") && roleplay.IsWorking)
                    {
                        Cost = 0;
                        Time = 5;

                        if (roomUser != null)
                            roomUser.ApplyEffect(EffectsList.PoliceTaxi);

                        Session.Shout($"*Saca su radio de la policía y pide una recogida rápida para {roomData.Name} [ID: {roomID}]*", 37);
                        PoliceTaxi = true;
                    }
                    else
                    {
                        if (roomUser != null)
                        {
                            roomUser.ApplyEffect(805);
                            roomUser.CanWalk = false;
                        }
                        Session.Shout($"*Llama a un Taxi{TaxiText} para ir a {roomData.Name} [ID: {roomID}]*", 4);
                    }

                    Task.Run(async delegate
                    {
                        for (int i = 0; i < Time * 10; i++)
                        {
                            if (roleplay == null || !roleplay.InsideTaxi)
                                break;

                            await Task.Delay(100);
                        }

                        if (roleplay != null && roleplay.InsideTaxi)
                        {
                            if (Cost > 0)
                            {
                                habbo.Credits -= Cost;
                                habbo.UpdateCreditsBalance();
                            }

                            if (PoliceTaxi && roomUser != null)
                            {
                                roomUser.ApplyEffect(EffectsList.CarPolice);
                                Session.Shout("*¡Sube al coche de policía de su socio mientras que lo ven tiran para arriba!*", 37);
                            }
                            else
                            {
                                Session.Shout($"*Salta dentro de su Taxi{TaxiText} como lo ven tire hacia arriba*", 4);
                            }
                            RoleplayManager.SendUserOld2(Session, roomData.Id);
                        }
                    });
                    #endregion
                }
                else
                {
                    if (PoliceTool && roomUser != null)
                    {
                        roomUser.ApplyEffect(EffectsList.CarPolice);
                        Session.Shout("*Salta dentro de su coche de la policía y se va a ayudar a un ciudadano en necesidad*", 4);
                    }
                    else if (OnDuty)
                    {
                        Session.Shout("*Sube dentro de su Staff Mobile y se marcha *", 23);
                    }

                    RoleplayManager.SendUserOld2(Session, roomData.Id);
                }
            }
            else
            {
                Session.SendMessage(new GetGuestRoomResultComposer(Session, roomData, isLoading, checkEntry));
            }
        }
    }
}
