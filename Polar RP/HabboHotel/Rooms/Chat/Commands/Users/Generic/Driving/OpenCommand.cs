using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Groups;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Weapons;
using Polar.HabboHotel.Items;
using Polar.HabboRoleplay.Vehicles;
using System.Data;
using System.Text.RegularExpressions;
using Polar.HabboRoleplay.VehicleOwned;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Offers
{
    class OpenCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_open"; }
        }
        // :abrir (auto)
        // :abrir [casa]
        
        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Interactúa con cosas que puedas abrir o cerrar. (EJ: Autos, Casas, etc)."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            int Dir = 0;
            string toDo = "";

            #region Conditions
            if (Params.Length == 2)
            {
                Dir = 1;
                toDo = Params[1].ToLower();// casa,...
            }
            else if (Params.Length == 1)
            {
                Dir = 2;// :abrir
            }
            else
            {
                Session.SendWhisper("Comando inválido, usa ':ayuda' para ver más información acerca de los comandos.", 1);
                return;
            }

            #region Basic Conditions
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
                Session.SendWhisper("¡No puedes hacer esto mientras estás muert@!", 1);
                return;
            }
            if (Session.GetRoleplay().IsJailed)
            {
                Session.SendWhisper("¡No puedes hacer eso mientras estás muert@!", 1);
                return;
            }
            #endregion

            if (Session.GetRoleplay().TryGetCooldown("open", true))
            {
                Session.SendWhisper("Por favor espera un poco para hacer eso nuevamente.", 1);
                return;
            }
            #endregion

            #region Execute
            if(Dir == 1)// Casa,...
            {
                switch (toDo)
                {
                    #region Casa
                    case "casa":
                        {
                            break;
                        }
                    #endregion

                    default:
                        Session.SendWhisper("Comando inválido. Usa ':ayuda' para recibir más información.", 1);
                        break;
                }
            }
            else if(Dir == 2)// Abrir/Cerrar
            {
                List<VehiclesOwned> VO = null;

                // Si está conduciendo, obtiene info del auto desde su variable DrivingCarId (Diccionario)
                if (Session.GetRoleplay().DrivingCar)
                {
                    VO = PolarEnvironment.GetGame().GetVehiclesOwnedManager().getVehiclesOwnedList(Session.GetRoleplay().DrivingCarId);
                }
                // Si no está conduciendo, obtiene info del auto desde el furni encima y consulta el itemid al diccionario
                else
                {
                    #region Get Veihcle Info (play_vehicles)  
                    Vehicle vehicle = null;
                    bool found = false;
                    int itemfurni = 0, corp = 0;
                    Item BTile = null;
                    string itemnm = null;
                    foreach (Vehicle Vehicle in VehicleManager.Vehicles.Values)
                    {
                        if (!found)
                        {
                            BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == Vehicle.ItemName && x.Coordinate == Session.GetRoomUser().Coordinate);
                            if (BTile != null)
                            {
                                vehicle = Vehicle;
                                itemfurni = BTile.Id;
                                itemnm = Vehicle.ItemName;
                                corp = Convert.ToInt32(Vehicle.CarCorp);
                                found = true;
                            }
                        }
                    }
                    //Al examinar todos los autos ninguno conincide con el item donde está parado el user...
                    if (!found)
                    {
                        Session.SendWhisper("¡Debes estar sobre un vehículo para hacer eso!", 1);
                        return;
                    }
                    #endregion

                    VO = PolarEnvironment.GetGame().GetVehiclesOwnedManager().getVehiclesOwnedByFurniId(itemfurni);                    
                }
                if (VO == null || VO.Count <= 0)
                {
                    Session.SendWhisper("((No se encontró información del vehículo))", 1);
                    return;
                }
                if (VO[0].OwnerId != Session.GetHabbo().Id)
                {
                    Session.SendWhisper("Este vehículo no te pertenece.", 1);
                    return;
                }
                if (!VO[0].Traba)
                {
                    Session.SendWhisper("Este vehículo no tiene traba de seguridad. Compra una en un 24/7.", 1);
                    return;
                }

                int State = VO[0].State;
                //state = 0 -> Normal
                //state = 1 -> Bloqueado con traba
                //state = 2 -> No traba y Averiado
                //state = 3 -> Con traba y Averiado
                //state = 4 -> Grua
                if (State == 0 || State == 2)
                {
                    Session.SendWhisper("El vehículo ya se encuentra abierto.", 1);
                    return;
                }
                int newState = (State == 1) ? 0 : 2;

                VO[0].State = newState;// Actualizamos State en Diccionario
                RoleplayManager.UpdateVehicleState(VO[0].FurniId, newState);// Actualizamos en DB
                RoleplayManager.Shout(Session, "*Abre las puertas de su Vehículo*", 5);
                Session.GetRoleplay().CooldownManager.CreateCooldown("open", 1000, 5);
            }
            #endregion
        }
    }
}