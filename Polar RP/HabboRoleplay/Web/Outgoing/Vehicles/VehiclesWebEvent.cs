using ConnectionManager;
using Polar.Net;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms;
using System.IO;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Incoming.Groups;
using Polar.Communication.Packets.Outgoing;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing.Groups;
using Polar.Communication.Packets.Outgoing.Catalog;
using Polar.Communication.Packets.Outgoing.Messenger;
using System.Collections.Generic;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.Cache;
using Polar.Communication.Packets.Outgoing.Rooms.Permissions;
using Polar.Database.Interfaces;
using System.Text.RegularExpressions;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using Polar.HabboRoleplay.Vehicles;
using System.Data;
using Polar.HabboRoleplay.VehicleOwned;

namespace Polar.HabboHotel.Roleplay.Web.Outgoing.Misc
{
    /// <summary>
    /// VehiclesWebEvent class.
    /// </summary>
    class VehiclesWebEvent : IWebEvent
    {
        /// <summary>
        /// Executes socket data.
        /// </summary>
        /// <param name="Client"></param>
        /// <param name="Data"></param>
        /// <param name="Socket"></param>
        public void Execute(GameClient Client, string Data, ConnectionInformation Socket)
        {

            if (!PolarEnvironment.GetGame().GetWebEventManager().SocketReady(Client, true) || !PolarEnvironment.GetGame().GetWebEventManager().SocketReady(Socket))
                return;
            /*
            if (!Client.GetRoleplay().UsingAtm)
            {
                Client.SendNotification("Buen intento, tratando de injectar el systema, ve a un ATM!");
                return;
            }
            */

            string Action = (Data.Contains(',') ? Data.Split(',')[0] : Data);


            switch (Action)
            {

                #region Open
                case "open":
                    {
                        int CarFuel = Client.GetRoleplay().CarFuel;
                        int CarMaxFuel = Client.GetRoleplay().CarMaxFuel;

                        if (Client.GetRoleplay().Pasajero)
                        {
                            if (Client.GetRoleplay().ChoferClient != null)
                            {
                                CarFuel = Client.GetRoleplay().ChoferClient.GetRoleplay().CarFuel;
                                CarMaxFuel = Client.GetRoleplay().ChoferClient.GetRoleplay().CarMaxFuel;
                            }
                        }
                        else
                        {
                            #region Pasajeros (Algoritmo replicado en ConditionCheckTimer por seguridad)
                            //Vars
                            string Pasajeros = Client.GetRoleplay().Pasajeros;
                            string[] stringSeparators = new string[] { ";" };
                            string[] result;
                            result = Pasajeros.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);

                            foreach (string psjs in result)
                            {
                                GameClient PJ = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(psjs);
                                if (PJ != null)
                                {
                                    PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(PJ, "event_vehicle", "open");// WS FUEL
                                }
                            }
                            #endregion
                        }

                        string SendData = "";
                        SendData += CarFuel + ";";
                        SendData += CarMaxFuel + ";";
                        Socket.SendWS( "compose_fuel|open|" + SendData);
                    }
                    break;
                #endregion

                #region Close
                case "close":
                    {
                        Socket.SendWS( "compose_fuel|close|");
                        break;
                    }
                #endregion

                #region Baul
                case "baul":
                    {
                        Client.GetRoleplay().ViewBaul = true;
                        string SendData = "";
                        SendData += Client.GetRoleplay().CarWSBaul + ";";
                        Socket.SendWS( "compose_vehicle|baul|" + SendData);
                        break;
                    }
                #endregion

                #region Close Baul
                case "closebaul":
                    {
                        Client.GetRoleplay().ViewBaul = false;
                        Client.GetRoleplay().CarWSBaul = "";
                        Socket.SendWS( "compose_vehicle|closebaul|");
                        break;
                    }
                #endregion

                #region Open Shop
                case "openshop":
                    {
                        #region Conditions & Vars
                        if (!RoleplayManager.GenerateRoom(Client.GetRoomUser().RoomId, out Room Room))
                            return;

                        if (Client.GetRoleplay().TryGetCooldown("openshopcar"))
                            return;

                        #region Conditions
                        if (!Room.BuyCarEnabled)
                        {
                            Client.SendWhisper("Debes ir a un concesionario para comprar vehículos.", 1);
                            return;
                        }
                        #endregion

                       /* #region Comodin Conditions
                        Item BTile = null;
                        BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "comodin_carro" && x.Coordinate == Client.GetRoomUser().Coordinate);
                        if (BTile == null)
                        {
                            Client.SendWhisper("Debes acercarte a un despacho para comprar un vehículo.", 1);
                            return;
                        }
                        #endregion*/
                        #endregion

                        Client.GetRoleplay().ViewCarList = true;

                        #region HTML
                        string html = "";
                        List<Vehicle> PO = VehicleManager.getAllVehicles();
                        if (PO != null && PO.Count > 0)
                        {
                            foreach (var car in PO)
                            {
                                if (car.CarCorp >= 1)
                                    continue;

                                string Price = (car.Price > 100) ? "$ " + String.Format("{0:N0}", car.Price) : car.Price + " RB";

                                html += "<div class=\"ft1\">";
                                html += "<div style=\"background:url('" + RoleplayManager.AVATARIMG + Client.GetHabbo().Look + "&gesture=sml&head_direction=3&effect=" + car.EffectID + "') no-repeat;height: 100px;width: 100%;background-position: center -18px;background-size: auto;\"></div>";
                                html += "<div class=\"datos2\"><span>" + car.DisplayName + "</span>";
                                html += "<div class=\"hr2\"></div>";
                                html += "Litros: <span>" + car.MaxFuel + "</span>";
                                /*html += "<div class=\"hr2\"></div>";
                                html += "Espacio en el baúl: <span>"+ car.MaxTrunks +" lugares</span>";*/
                                html += "<div class=\"hr2\"></div>";
                                html += "Pasajeros: <span>" + car.MaxDoors + "</span>";
                                html += "<div class=\"hr2\"></div>";
                                html += "<span style=\"color:#339900\"><font color=\"orange\">" + Price + "</font></span><br><div id=\"" + car.EffectID + "," + car.DisplayName + "\" class=\"shopcar\"></div>";
                                html += "</div>";
                                html += "</div>";
                            }
                        }
                        #endregion

                        string SendData = "";
                        SendData += html;
                        Socket.SendWS( "compose_vehicle|openshop|" + SendData);
                        Client.GetRoleplay().CooldownManager.CreateCooldown("openshopcar", 1000, 1);
                        break;
                    }
                #endregion

                #region Close Shop
                case "closeshop":
                    {
                        Client.GetRoleplay().ViewCarList = false;
                        Socket.SendWS( "compose_vehicle|closeshop|");
                        break;
                    }
                #endregion

                #region Buy Car
                case "shop":
                    {
                        if (!RoleplayManager.GenerateRoom(Client.GetRoomUser().RoomId, out Room Room))
                            return;

                        #region Conditions & Vars
                        if (Client.GetRoleplay().TryGetCooldown("buy"))
                            return;

                        #region Conditions
                        if (!Room.BuyCarEnabled)
                        {
                            Client.SendWhisper("Debes ir a un concesionario para comprar vehículos.", 1);
                            return;
                        }
                        #endregion

                        /*#region Comodin Conditions
                        Item BTile = null;
                        BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "comodin_carro" && x.Coordinate == Client.GetRoomUser().Coordinate);
                        if (BTile == null)
                        {
                            Client.SendWhisper("Debes acercarte a un despacho para comprar un vehículo.", 1);
                            return;
                        }
                        #endregion*/

                        if (Client.GetRoleplay().DrivingInCar)
                        {
                            Socket.SendWS( "compose_vehicle|shopmsg|Primero debes detener el vehículo que tienes afuera.");
                            return;
                        }
                        if (Client.GetRoleplay().InTutorial && Client.GetRoleplay().TutorialStep < 23)
                        {
                            Client.SendWhisper("¡Hey, no tan rápido! Ve siguiendo el Tutorial paso a paso para guiarte de la mejor manera.", 1);
                            return;
                        }

                        string[] ReceivedData = Data.Split(',');
                        int GetEffect;
                        string GetCarModel = ReceivedData[2];
                        if (!int.TryParse(ReceivedData[1], out GetEffect))
                        {
                            Socket.SendWS( "compose_vehicle|shopmsg|Ha ocurrido un problema al obtener la Información del Vehículo.");
                            return;
                        }
                        GetCarModel = Regex.Replace(GetCarModel, "<(.|\\n)*?>", string.Empty);

                        Vehicle vehicle = VehicleManager.getVehicle(GetCarModel);
                        if (vehicle == null)
                        {
                            Socket.SendWS( "compose_vehicle|shopmsg|Ha ocurrido un problema al obtener la Información del Vehículo. [2]");
                            return;
                        }
                        if (vehicle.Price > 100)
                        {
                            if (Client.GetHabbo().Credits < vehicle.Price)
                            {
                                Socket.SendWS( "compose_vehicle|shopmsg|No tienes dinero suficiente para comprar ese vehículo.");
                                return;
                            }
                        }
                        else
                        {
                            if (Client.GetHabbo().Diamonds < vehicle.Price)
                            {
                                Socket.SendWS( "compose_vehicle|shopmsg|No tienes los Rubies suficientes para comprar ese vehículo.");
                                return;
                            }
                        }
                        #endregion

                        #region Check Car Limit
                        int MyCars = 0;
                        List<VehiclesOwned> VO = PolarEnvironment.GetGame().GetVehiclesOwnedManager().getMyVehiclesOwned(Client.GetHabbo().Id);
                        if (VO != null)
                            MyCars = VO.Count;

                        if (MyCars >= 2)
                        {
                            Socket.SendWS( "compose_vehicle|shopmsg|¡Ya tienes " + MyCars + " Vehículos! Usuarios VIP pueden tener más de dos.");
                            #region Tutorial Step Check
                            if (Client.GetRoleplay().TutorialStep == 23 && Room.BuyCarEnabled && Room.Type.Equals("public"))
                            {
                                Client.GetRoleplay().TutorialStep = 27;
                                PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(Client, "compose_tutorial|26");
                            }
                            #endregion
                            return;
                        }
                        /*else if (MyCars >= 3 && Client.GetHabbo().VIPRank != 1)
                        {
                            Socket.SendWS( "compose_vehicle|shopmsg|¡Ya tienes "+MyCars+" Vehículos! Usuarios VIP pueden tener hasta 4 autos.");
                            #region Tutorial Step Check
                            if (Client.GetRoleplay().TutorialStep == 23 && Room.BuyCarEnabled && Room.Type.Equals("public"))
                            {
                                Client.GetRoleplay().TutorialStep = 27;
                                PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(Client, "compose_tutorial|26");
                            }
                            #endregion
                            return;
                        }
                        else if (MyCars >= 4)
                        {
                            Socket.SendWS( "compose_vehicle|shopmsg|¡Ya tienes " + MyCars + " Vehículos! Solo es posible tener hasta 4 autos.");
                            #region Tutorial Step Check
                            if (Client.GetRoleplay().TutorialStep == 23 && Room.BuyCarEnabled && Room.Type.Equals("public"))
                            {
                                Client.GetRoleplay().TutorialStep = 27;
                                PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(Client, "compose_tutorial|26");
                            }
                            #endregion
                            return;
                        }*/
                        #endregion

                        #region Execute
                        VehiclesOwned nVO = null;
                        if (!PolarEnvironment.GetGame().GetVehiclesOwnedManager().TryCreateVehicleOwned(Client, 0, vehicle.ItemID, Client.GetHabbo().Id, Client.GetHabbo().Id, vehicle.Model, vehicle.MaxFuel, 0, 0, false, false, Client.GetRoomUser().RoomId, 0, 0, 0, string.Empty.ToString().Split(';'), false, out nVO))
                        {
                            Socket.SendWS( "compose_vehicle|shopmsg|No se pudo autorizar el registro de papeles para tu nuevo vehículo. Inténtalo de nuevo.");
                            return;
                        }

                        #region Online DriveVars
                        //Lo conduce
                        Client.GetRoleplay().DrivingCar = false;
                        Client.GetRoleplay().DrivingInCar = true;
                        Client.GetRoleplay().DrivingCarId = nVO.Id;// - Ya lo establece al Crear el Vehicle en Diccionario y DB.
                        Client.GetRoleplay().DrivingCarItem = nVO.FurniId;

                        //Combustible System
                        Client.GetRoleplay().CarType = vehicle.CarType;// Define el gasto de combustible
                        Client.GetRoleplay().CarFuel = vehicle.MaxFuel;
                        Client.GetRoleplay().CarMaxFuel = vehicle.MaxFuel;
                        Client.GetRoleplay().CarTimer = 0;
                        Client.GetRoleplay().CarLife = 100;

                        Client.GetRoleplay().CarEnableId = vehicle.EffectID;//Coloca el enable para conducir
                        Client.GetRoleplay().CarEffectId = vehicle.EffectID;//Guarda el enable del último auto en conducción.
                        //Session.GetRoomUser().ApplyEffect(vehicle.EffectID); - No Pone efecto con DrivingIncar = True
                        //Session.GetRoomUser().FastWalking = true; - No FastWalking sin efecto
                        #endregion

                        if (vehicle.Price > 100)
                        {
                            Client.GetHabbo().Credits -= vehicle.Price;
                            Client.GetHabbo().UpdateCreditsBalance();
                            RoleplayManager.Shout(Client, "*Compra un " + vehicle.Model + " y paga $ " + String.Format("{0:N0}", vehicle.Price) + "*", 5);
                        }
                        else
                        {
                            Client.GetHabbo().Diamonds -= vehicle.Price;
                            Client.GetHabbo().UpdateDiamondsBalance();
                            RoleplayManager.Shout(Client, "*Compra un " + vehicle.Model + " y paga " + String.Format("{0:N0}", vehicle.Price) + " RB*", 5);
                        }

                        Client.SendWhisper("¡Tu vehículo se encuentra estacionado afuera! Sal del Establecimiento para conducirlo.", 1);
                        Socket.SendWS( "compose_vehicle|shopmsg_green|¡Felicitaciones! Tu nuevo vehículo se encuentra afuera.");
                        Client.GetRoleplay().CooldownManager.CreateCooldown("buy", 1000, 15);
                        #region Tutorial Step Check
                        if (Client.GetRoleplay().TutorialStep == 23 && Room.BuyCarEnabled && Room.Type.Equals("public"))
                        {
                            Client.GetRoleplay().TutorialStep = 27;
                            PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(Client, "compose_tutorial|26");
                        }
                        #endregion
                        break;
                        #endregion
                    }
                    #endregion

            }
        }

        // ── Helper: envía texto como frame WebSocket usando ConnectionInformation
        private static void SendWS(ConnectionInformation socket, string message)
        {
            if (socket == null || string.IsNullOrEmpty(message)) return;
            socket.SendData(System.Text.Encoding.UTF8.GetBytes(message));
        }

    }
}
