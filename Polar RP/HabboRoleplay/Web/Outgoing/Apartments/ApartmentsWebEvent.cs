using ConnectionManager;
﻿using System;
using System.Linq;
using Polar.Net;
using System.Threading.Tasks;
using Polar.HabboHotel.Users;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.GameClients;
using System.IO;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Houses;
using System.Collections.Generic;
using Polar.HabboRoleplay.Apartments;
using Polar.HabboHotel.Rooms;
using Polar.HabboRoleplay.ApartmentsOwned;
using Polar.HabboRoleplay.VehicleOwned;

namespace Polar.HabboHotel.Roleplay.Web.Outgoing.Misc
{
    /// <summary>
    /// ApartmentsWebEvent class.
    /// </summary>
    class ApartmentsWebEvent : IWebEvent
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

            string Action = (Data.Contains(',') ? Data.Split(',')[0] : Data);

            switch (Action)
            {

                #region Open
                case "open":
                    {
                        if (Client.GetRoleplay().TryGetCooldown("viewapart"))
                            return;

                        PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_apart", "welcome");

                        Socket.SendWS( "compose_apart|open|");
                        Client.GetRoleplay().ViewApartments = true;
                        Client.GetRoleplay().CooldownManager.CreateCooldown("viewapart", 1000, 3);
                    }
                    break;
                #endregion

                #region Close
                case "close":
                    {
                        Client.GetRoleplay().ViewApartments = false;
                        Socket.SendWS( "compose_apart|close|");
                        break;
                    }
                #endregion

                #region Close Apartment List
                case "apart_close":
                    {
                        Client.GetRoleplay().ViewApartments = false;
                        Socket.SendWS( "compose_apart|apart_close|");
                        break;
                    }
                #endregion

                #region Welcome
                case "welcome":
                    {
                        Socket.SendWS( "compose_apart|welcome|Bienvenid@ a los Apartamentos");
                    }
                    break;
                #endregion

                #region New Apartment
                case "new_apart":
                    {
                        string html = "";

                        #region Get Apartments Models
                        List<Apartment> AP = ApartmentManager.GetApartments();
                        if (AP != null && AP.Count > 0)
                        {
                            foreach (var apart in AP)
                            {
                                html += "<div class=\"ft1\" style=\"padding: 2px;width:auto!important;\">";
                                html += "<div style=\"background-image:url('" + apart.Image + "');background-position:center;background-repeat:no-repeat;height: 80px;width: 210px;border: solid 1px #161f26;\"></div>";
                                html += "<div class=\"datos2\" style=\"width: 200px;background-color: #1a242c;border-left: solid 1px #161f26;border-right: solid 1px #161f26;border-bottom: #161f26;\">";
                                html += "Modelo: <span>" + apart.ModelName + "</span>";
                                html += "<div class=\"hr2\"></div>";
                                html += "Baldosas: <span>" + apart.Tiles + "</span>";
                                html += "<div class=\"hr2\"></div>";

                                string p = "<font color=\"green\">$ " + String.Format("{0:N0}", apart.Price) + "</font>";
                                if(apart.Price <= 100)
                                    p = "<font color=\"gray\">" + String.Format("{0:N0}", apart.Price) + " RB</font>";

                                html += "Precio: <span style=\"color:#339900\">" + p + "</span>";

                                html += "<div class=\"hr2\"></div>";
                                html += "<button id=\"New_Apart,"+ apart.ID + "\" class=\"dark-button buyapart\" style=\"width: 90%\">Comprar</button>";
                                html += "</div>";
                                html += "</div>";
                            }
                        }
                        #endregion

                        Socket.SendWS( "compose_apart|new_apart|" + html);
                    }
                    break;
                #endregion

                #region Buy New Apartment
                case "buyapart":
                    {
                        #region Conditions
                        if (Client.GetRoleplay().TryGetCooldown("buyapart"))
                            return;

                        string[] ReceivedData = Data.Split(',');
                        int GetApartID;
                        if (!int.TryParse(ReceivedData[2], out GetApartID))
                        {
                            Socket.SendWS( "compose_apart|msg_error|Ha ocurrido un problema al obtener la Información del Apartamento.");
                            return;
                        }

                        if (Client.GetRoleplay().Level < 2)
                        {
                            Socket.SendWS( "compose_apart|msg_error|Debes ser al menos Nivel 2 para comprar un apartamento.");
                            return;
                        }
                        
                        string RoomName = ReceivedData[3];
                        if(string.IsNullOrEmpty(RoomName) || RoomName.Length <= 3)
                        {
                            Socket.SendWS( "compose_apart|msg_error|Debes colocar un nombre mayor a 3 caracteres.");
                            return;
                        }

                        /*string FE = ReceivedData[4];

                        if (string.IsNullOrEmpty(FE) || (FE != "false" && FE != "true"))
                        {
                            Socket.SendWS( "compose_apart|msg_error|No se obtuvo información correctamente sobre el Floor Editor.");
                            return;
                        }

                        bool FloorEditor = (FE == "true");*/

                        Apartment AP = ApartmentManager.GetApartmentById(GetApartID);
                        if (AP == null)
                        {
                            Socket.SendWS( "compose_apart|msg_error|Ha ocurrido un problema al obtener la Información del Apartamento.");
                            return;
                        }

                        if (AP.Price <= 100)
                        {
                            /*if (FloorEditor)
                            {
                                if (Client.GetHabbo().Diamonds < (AP.Price + 25))
                                {
                                    Socket.SendWS( "compose_apart|msg_error|No cuentas con los Rubies suficientes.");
                                    return;
                                }
                            }
                            else
                            {*/
                                if (Client.GetHabbo().Diamonds < AP.Price)
                                {
                                    Socket.SendWS( "compose_apart|msg_error|No cuentas con los Rubies suficientes.");
                                    return;
                                }
                            //}
                        }
                        else
                        {
                            /*
                            if (FloorEditor && Client.GetHabbo().Diamonds < 25)
                            {
                                Socket.SendWS( "compose_apart|msg_error|No cuentas con los Rubies suficientes para el Floor Editor.");
                                return;
                            }*/

                            if (Client.GetHabbo().Credits < AP.Price)
                            {
                                Socket.SendWS( "compose_apart|msg_error|No cuentas con el dinero suficiente.");
                                return;
                            }
                        }

                        int MyAparts = 0;
                        List<ApartmentOwned> VO = PolarEnvironment.GetGame().GetApartmentOwnedManager().GetApartmentsOwned(Client.GetHabbo().Id);
                        if (VO != null)
                            MyAparts = VO.Count;

                        /* if (MyAparts >= 5)
                         {
                             Socket.SendWS( "compose_apart|msg_error|¡Ya tienes " + MyAparts + " apartamentos! No pueden tener más de cinco.");
                             return;
                         }*/

                        #endregion

                        #region Execute
                        Room Room = RoleplayManager.GenerateRoom(Client.GetRoomUser().RoomId);

                        if (!PolarEnvironment.GetGame().GetApartmentOwnedManager().BuyNewApartment(Client, AP.ModelName, RoomName, GetApartID, Room.Id, Room.City, true))
                        {
                            Socket.SendWS( "compose_apart|msg_error|Ocurrió un problema al intentar comprar el apartamento. Contacte con un Administrador.");
                            return;
                        }

                        if(AP.Price <= 100)
                        {
                            Client.GetHabbo().Diamonds -= AP.Price;
                            Client.GetHabbo().UpdateDiamondsBalance();
                        }
                        else
                        {
                            Client.GetHabbo().Credits -= AP.Price;
                            Client.GetHabbo().UpdateCreditsBalance();
                        }

                        /*if (FloorEditor)
                        {
                            Client.GetHabbo().Diamonds -= 25;
                            Client.GetHabbo().UpdateDiamondsBalance();
                        }*/

                        Socket.SendWS( "compose_apart|msg_success|¡Apartamento comprado exitosamente!");
                        RoleplayManager.Shout(Client, "*Ha comprado un nuevo apartamento pagando $ " + String.Format("{0:N0}", AP.Price) + " por el*", 5);
                        Client.SendWhisper("¡Felicidades por tu nuevo apartamento! Ahora dirígete al asensor para poder acceder a el.", 1);
                        #endregion

                        Client.GetRoleplay().CooldownManager.CreateCooldown("buyapart", 1000, 30);
                    }
                    break;
                #endregion

                #region Apart List
                case "apart_list":
                    {
                        List<ApartmentOwned> AO = PolarEnvironment.GetGame().GetApartmentOwnedManager().GetApartmentsOwned();
                        if (AO == null || AO.Count <= 0)
                        {
                            Socket.SendWS("compose_apart|apart_list|<b style='color:red'>Aún no hay apartamentos en uso en este edificio.</b>");
                            return;
                        }
                        else
                        {
                            string html = "";
                            foreach (ApartmentOwned _ao in AO)
                            {
                                if (_ao == null)
                                    continue;

                                if (_ao.LobbyId != Client.GetRoomUser().RoomId)
                                    continue;

                                if (_ao.RoomId <= 0)
                                    continue;

                                RoomData Room = PolarEnvironment.GetGame().GetRoomManager().GenerateRoomData(_ao.RoomId);

                                if (Room == null)
                                    continue;

                                string Class = "bell";
                                if (Room.State == 0)
                                    Class = "open";
                                else if (Room.State == 2)
                                    Class = "lock";

                                string DisplayRoomName = (Room.Name.Length > 10) ? Room.Name.Substring(0, 10) : Room.Name;

                                html += "<tr data-room-id=\""+ Room.Id +"\">";
                                html += "<td><i class=\"fa fa-users\" aria-hidden=\"true\"></i> "+ Room.UsersNow +"</td>";
                                html += "<td>"+ DisplayRoomName + "</td>";
                                html += "<td style=\"text-align: right;\">"+ Room.OwnerName +"</td>";
                                html += "<td style=\"text-align: right;\"><center><div class=\"ap_elev_"+Class+"\"></div></center></td>";
                                html += "</tr>";

                            }
                            Client.GetRoleplay().ViewApartments = true;
                            Socket.SendWS( "compose_apart|apart_list|" + html + "|");
                        }

                    }
                    break;
                #endregion

                #region Enter Apartment
                case "enter_apart":
                    {
                        #region Conditions
                        if (Client == null || Client.GetRoomUser() == null || Client.GetRoomUser().RoomId <= 0)
                            return;

                        Room Room = RoleplayManager.GenerateRoom(Client.GetRoomUser().RoomId);
                        if (Room == null)
                            return;

                        /*#region Comodin Conditions
                        Item BTile = null;
                        BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "comodin_carro" && x.Coordinate == Client.GetRoomUser().Coordinate);
                        if (BTile == null)
                        {
                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_apart", "msg_ele_error," + "Debes acercarte al asensor para entrar a un apartamento.|");
                            return;
                        }
                        #endregion*/

                        string[] ReceivedData = Data.Split(',');
                        int GetRoomID = 0;
                        if (!int.TryParse(ReceivedData[1], out GetRoomID))
                        {
                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_apart", "msg_ele_error," + "Ha ocurrido un problema al obtener la Información del apartamento.|");
                            return;
                        }

                        string Pass = "";
                        // Colocó password para entrar
                        if(ReceivedData.Count() == 3)
                        {
                            Pass = ReceivedData[2];
                        }

                        ApartmentOwned AP = PolarEnvironment.GetGame().GetApartmentOwnedManager().GetApartmentOwnedById(GetRoomID);

                        if(AP == null || AP.LobbyId != Client.GetRoomUser().RoomId)
                        {
                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_apart", "msg_ele_error," + "No existe ningún apartamento con ese número en este edificio.|");
                            return;
                        }
                        #endregion

                        #region Basic Conditions
                        if (Client.GetRoomUser() == null)
                        {
                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_apart", "msg_ele_error," + "Ha ocurrido un problema en obtener la ubicación de tu usuario.|");
                            return;
                        }
                        if (Client.GetRoleplay().Cuffed)
                        {
                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_apart", "msg_ele_error," + "No puedes entrar a ningún apartamento mientras estás esposad@.|");
                            return;
                        }
                        if (!Client.GetRoomUser().CanWalk)
                        {
                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_apart", "msg_ele_error," + "No puedes ni moverte para poder entrar a un apartamento.|");
                            return;
                        }
                        #endregion

                        #region Execute
                        if (!PolarEnvironment.GetGame().GetRoomManager().LoadRoom(AP.RoomId, out Room _room))
                        {
                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_apart", "msg_ele_error," + "Ha ocurrido un problema al intentar entrar al apartamento.|");
                        }
                        else
                        {
                            if (_room.State == 2)
                            {
                                if (Pass.Length <= 0)
                                {
                                    PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_apart", "open_apart_lock," + "<b>"+ _room.Name +"</b><br>Esta Sala está cerrada con clave. Escribe una para intentar entrar.<input type=\"hidden\" id=\"AP_Elevator_Pass_Roomid\" value=\""+_room.Id+"\">");
                                }
                                else
                                {
                                    // Try Pass
                                    if (Client.GetHabbo().PrepareApartment(_room.Id, Pass))
                                        PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_apart", "apart_close");
                                }
                            }
                            else
                            {
                                if (Client.GetHabbo().PrepareApartment(_room.Id, ""))
                                    PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_apart", "apart_close");
                            }
                        }
                        #endregion
                    }
                    break;
                #endregion

                #region My Apart List
                case "my_aparts":
                    {
                        List<ApartmentOwned> AO = PolarEnvironment.GetGame().GetApartmentOwnedManager().GetApartmentsOwned(Client.GetHabbo().Id);

                        if (AO == null || AO.Count <= 0)
                        {
                            Socket.SendWS( "compose_apart|my_apart_list|<b style='color:red'>No tienes apartamentos en este edificio.</b>|" + Client.GetHabbo().Username + "|");
                            return;
                        }
                        else
                        {
                            string html = "";
                            foreach (ApartmentOwned _ao in AO)
                            {
                                if (_ao.LobbyId != Client.GetRoomUser().RoomId)
                                    continue;

                                Room Room = RoleplayManager.GenerateRoom(_ao.RoomId);

                                if (Room == null)
                                    continue;

                                string Class = "bell";
                                if (Room.State == 0)
                                    Class = "open";
                                else if (Room.State == 2)
                                    Class = "lock";

                                html += "<tr data-room-id=\"" + Room.Id + "\">";
                                html += "<td><i class=\"fa fa-users\" aria-hidden=\"true\"></i> " + Room.UserCount + "</td>";
                                html += "<td>" + Room.Name + "</td>";
                                html += "<td style=\"text-align: right;\">" + Room.OwnerName + "</td>";
                                html += "<td style=\"text-align: right;\"><center><div class=\"ap_elev_" + Class + "\"></div></center></td>";
                                html += "</tr>";
                            }
                             
                            Client.GetRoleplay().ViewApartments = true;
                            Socket.SendWS( "compose_apart|my_apart_list|" + html + "|" + Client.GetHabbo().Username + "|");
                        }

                    }
                    break;
                #endregion

                #region Search Apart List
                case "search_apart":
                    {
                        string[] ReceivedData = Data.Split(',');
                        string Search = ReceivedData[1];

                        Habbo Habbo = PolarEnvironment.GetHabboByUsername(Search);
                        if(Habbo == null)
                        {
                            Socket.SendWS( "compose_apart|apart_list|<b style='color:red'>No se encontraron apartamentos con ese dueño en este edificio.</b>");
                            return;
                        }

                        List<ApartmentOwned> AO = PolarEnvironment.GetGame().GetApartmentOwnedManager().GetApartmentsOwned(Habbo.Id);

                        if (AO == null || AO.Count <= 0)
                        {
                            Socket.SendWS( "compose_apart|apart_list|<b style='color:red'>No se encontraron apartamentos con ese dueño en este edificio.</b>");
                            return;
                        }
                        else
                        {
                            string html = "";
                            foreach (ApartmentOwned _ao in AO)
                            {
                                if (_ao.LobbyId != Client.GetRoomUser().RoomId)
                                    continue;

                                Room Room = RoleplayManager.GenerateRoom(_ao.RoomId);

                                if (Room == null)
                                    continue;

                                string Class = "bell";
                                if (Room.State == 0)
                                    Class = "open";
                                else if (Room.State == 2)
                                    Class = "lock";

                                html += "<tr data-room-id=\"" + Room.Id + "\">";
                                html += "<td><i class=\"fa fa-users\" aria-hidden=\"true\"></i> " + Room.UserCount + "</td>";
                                html += "<td>" + Room.Name + "</td>";
                                html += "<td style=\"text-align: right;\">" + Room.OwnerName + "</td>";
                                html += "<td style=\"text-align: right;\"><center><div class=\"ap_elev_" + Class + "\"></div></center></td>";
                                html += "</tr>";
                            }

                            Client.GetRoleplay().ViewApartments = true;
                            Socket.SendWS( "compose_apart|apart_list|" + html + "|" + Client.GetHabbo().Username + "|");
                        }

                    }
                    break;
                #endregion

                #region My Offer Apart List
                case "my_offer_aparts":
                    {
                        List<ApartmentOwned> AO = PolarEnvironment.GetGame().GetApartmentOwnedManager().GetApartmentsOwned(Client.GetHabbo().Id);

                        if (AO == null || AO.Count <= 0)
                        {
                            Socket.SendWS( "compose_apart|my_offer_aparts|<b style='color:red'>No tienes apartamentos en este edificio.</b>");
                            return;
                        }
                        else
                        {
                            string html = "";
                            foreach (ApartmentOwned _ao in AO)
                            {
                                if (_ao.LobbyId != Client.GetRoomUser().RoomId)
                                    continue;

                                Room Room = RoleplayManager.GenerateRoom(_ao.RoomId);

                                if (Room == null)
                                    continue;

                                Apartment AP = ApartmentManager.GetApartmentById(_ao.ApartId);

                                if (AP == null)
                                    continue;

                                string BtnText = "Vender";
                                string Disabled = "";

                                if (_ao.ForSale)
                                {
                                    BtnText = "No vender";
                                    Disabled = "disabled";
                                }

                                string Din = "";
                                string Plat = "";

                                if (_ao.PaymentType == "Dinero")
                                    Din = "selected";
                                else
                                    Plat = "selected";

                                string DisplayFE = (_ao.FloorEditor) ? "Sí" : "No";

                                html += "<div class=\"ft1\" style=\"padding: 2px;width:auto!important;\">";
                                html += "<div style=\"background-image:url('"+AP.Image+"');background-position:center;background-repeat:no-repeat;height: 80px;width: 210px;border: solid 1px #161f26;\"></div>";
                                html += "<div class=\"datos2\" style=\"width: 200px;background-color: #1a242c;border-left: solid 1px #161f26;border-right: solid 1px #161f26;border-bottom: #161f26;\">";
                                html += "Nombre: <span>"+ Room.Name +"</span>";
                                html += "<div class=\"hr2\"></div>";
                                html += "Modelo: <span>" + AP.ModelName + "</span>";
                                html += "<div class=\"hr2\"></div>";
                                html += "Baldosas: <span>" + AP.Tiles + "</span>";
                                html += "<div class=\"hr2\"></div>";
                                html += "Floor Editor: <span>" + DisplayFE + "</span>";
                                html += "<div class=\"hr2\"></div>";
                                html += "Precio: <input type=\"number\" class=\"dark-button\" value=\""+_ao.Price+"\" autocomplete=\"false\" style=\"width: 134px;text-align: right;\" "+Disabled+ " onkeyup=\"ModifPrice($(this))\">";
                                html += "<div class=\"hr2\"></div>";
                                html += "Moneda:";
                                html += "<select class=\"dark-button\" style=\"width: 132px;\" "+Disabled+ " onchange=\"ModifMoneda($(this))\">";
                                html += "<option value=\"Dinero\" "+Din+" style=\"color:black\">Dinero</option>";
                                html += "<option value=\"Rubies\" " + Plat+ " style=\"color:black\">Rubies</option>";
                                html += "</select>";
                                html += "<div class=\"hr2\"></div>";
                                html += "Vender a:";
                                html += "<select class=\"dark-button\" style=\"width: 127px;\" " + Disabled + " onchange=\"ModifMerca($(this))\">";
                                html += "<option value=\"Mercadillo\" style=\"color:black\">Mercadillo</option>";
                                html += "<option value=\"Gobierno\" style=\"color:black\">Gobierno</option>";
                                html += "</select>";
                                html += "<div class=\"hr2\"></div>";
                                html += "<button value=\"" + _ao.RoomId + "," + _ao.Price + "," + _ao.PaymentType + ",Mercadillo\" class=\"dark-button btn_my_offer_apart\" style=\"width: 90%\">" + BtnText +"</button>";
                                html += "</div>";
                                html += "</div>";
                            }

                            Socket.SendWS( "compose_apart|my_offer_aparts|" + html);
                        }

                    }
                    break;
                #endregion

                #region Toggle Offer Apartment
                case "toggle_offer_apart":
                    {
                        if (Client.GetRoleplay().TryGetCooldown("toggleapart"))
                            return;

                        string[] ReceivedData = Data.Split(',');
                        int GetAPId, GetAPPrice;
                        string GetAPMoneda = ReceivedData[3];
                        string GetMerca = ReceivedData[4];
                        if (!int.TryParse(ReceivedData[1], out GetAPId))
                        {
                            Socket.SendWS( "compose_apart|msg_error|Ha ocurrido un problema al obtener la Información del Apartamento.");
                            return;
                        }
                        if (!int.TryParse(ReceivedData[2], out GetAPPrice))
                        {
                            Socket.SendWS( "compose_apart|msg_error|Ha ocurrido un problema al obtener la Información del Precio.");
                            return;
                        }

                        ApartmentOwned AP = PolarEnvironment.GetGame().GetApartmentOwnedManager().GetApartmentOwnedById(GetAPId);

                        if (AP == null || AP.LobbyId != Client.GetRoomUser().RoomId)
                        {
                            Socket.SendWS( "compose_apart|msg_error|No existe ningún apartamento con ese número en este edificio.|");
                            return;
                        }

                        Client.GetRoleplay().CooldownManager.CreateCooldown("toggleapart", 1000, 30);

                        if (GetMerca != "Gobierno")
                        {
                            if (PolarEnvironment.GetGame().GetApartmentOwnedManager().ToggleOfferApartment(AP, GetAPPrice, GetAPMoneda))
                            {
                                Socket.SendWS( "compose_apart|msg_success|¡Apartamento colocado en venta exitosamente! Toma en cuenta que ahora cualquier persona podrá entrar a verlo.|");
                                RoleplayManager.Shout(Client, "*Coloca en venta un apartamento*", 5);
                            }
                            else
                            {
                                Socket.SendWS( "compose_apart|msg_success|¡Apartamento quitado de la venta exitosamente!|");
                                RoleplayManager.Shout(Client, "*Quita un apartamento de la lista de ventas*", 5);
                            }
                        }
                        else
                        {
                            Apartment APModel = ApartmentManager.GetApartmentById(AP.ApartId);

                            double calc = APModel.Price - (APModel.Price * 0.25);
                            int Pay = (int)calc;

                            if (PolarEnvironment.GetGame().GetApartmentOwnedManager().SellApartmentGouv(Client, AP))
                            {
                                Socket.SendWS( "compose_apart|msg_success|¡Apartamento vendido al Gobierno exitosamente!|");
                                RoleplayManager.Shout(Client, "*Vende un apartamento al Gobierno*", 5);

                                if (APModel.Price <= 100)
                                {
                                    Client.SendWhisper("El gobierno ha reclamado tu apartamento y te ha dado " + Pay + " PL");
                                    Client.GetHabbo().Diamonds += Pay;
                                    //Client.GetRoleplay().PLEarned += Pay;
                                    Client.GetHabbo().UpdateDiamondsBalance();
                                }
                                else
                                {
                                    Client.SendWhisper("El gobierno ha reclamado tu apartamento y te ha dado $ " + String.Format("{0:N0}", Pay));
                                    Client.GetHabbo().Credits += Pay;
                                    //Client.GetRoleplay().MoneyEarned += Pay;
                                    Client.GetHabbo().UpdateCreditsBalance();
                                }
                            }
							else
							{
                                Client.SendWhisper("No pudimos encontrar tu apartamento. Por favor, ingresa primero a él, y después vuelve aquí a intentar venderlo.", Pay);
                            }
                        }

                        // Refrescamos lista
                        PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_apart", "my_offer_aparts");
                    }
                    break;
                #endregion

                #region Offer Apart List
                case "offer_aparts":
                    {
                        List<ApartmentOwned> AO = PolarEnvironment.GetGame().GetApartmentOwnedManager().GetOfferApartmentsByLobby(Client.GetRoomUser().RoomId);

                        if (AO == null || AO.Count <= 0)
                        {
                            Socket.SendWS( "compose_apart|offer_aparts|<b style='color:red'>No hay apartamentos usados en venta en este edificio.</b>");
                            return;
                        }
                        else
                        {
                            string html = "";
                            foreach (ApartmentOwned _ao in AO)
                            {
                                Room Room = RoleplayManager.GenerateRoom(_ao.RoomId);

                                if (Room == null)
                                    continue;

                                Apartment AP = ApartmentManager.GetApartmentById(_ao.ApartId);

                                if (AP == null)
                                    continue;

                                string DisplayPrice = "<b style='color:green;float:right'>$ " + String.Format("{0:N0}", _ao.Price) + "</b>";

                                if (_ao.PaymentType == "Rubies")
                                    DisplayPrice = "<b style='color:orange;float:right'>" + String.Format("{0:N0}", _ao.Price) + " RB</b>";

                                string DisplayButton = "Comprar";

                                if (_ao.Owner == Client.GetHabbo().Id)
                                    DisplayButton = "No vender";

                                string DisplayFE = (_ao.FloorEditor) ? "Sí" : "No";

                                html += "<div class=\"ft1\" style=\"padding: 2px;width:auto!important;\">";
                                html += "<div style=\"background-image:url('" + AP.Image + "');background-position:center;background-repeat:no-repeat;height: 80px;width: 210px;border: solid 1px #161f26;\"></div>";
                                html += "<div class=\"datos2\" style=\"width: 200px;background-color: #1a242c;border-left: solid 1px #161f26;border-right: solid 1px #161f26;border-bottom: #161f26;\">";
                                html += "Nombre: <span>" + Room.Name + "</span>";
                                html += "<div class=\"hr2\"></div>";
                                html += "Modelo: <span>" + AP.ModelName + "</span>";
                                html += "<div class=\"hr2\"></div>";
                                html += "Baldosas: <span>" + AP.Tiles + "</span>";
                                html += "<div class=\"hr2\"></div>";
                                html += "Floor Editor: <span>" + DisplayFE + "</span>";
                                html += "<div class=\"hr2\"></div>";
                                html += "Dueñ@: <span>" + PolarEnvironment.GetUsernameById(_ao.Owner) + "</span>";
                                html += "<div class=\"hr2\"></div>";
                                html += "Precio: " + DisplayPrice;
                                html += "<div class=\"hr2\"></div>";
                                html += "<button value=\"" + _ao.RoomId + "\" class=\"dark-button btn_offer_apart\" style=\"width: 90%\">" + DisplayButton + "</button>";
                                html += "</div>";
                                html += "</div>";
                            }

                            Socket.SendWS( "compose_apart|offer_aparts|" + html);
                        }

                    }
                    break;
                #endregion

                #region Buy Offer Apartment
                case "buy_offer_apart":
                    {
                        #region Conditions
                        if (Client.GetRoleplay().TryGetCooldown("buyapart"))
                            return;

                        string[] ReceivedData = Data.Split(',');
                        int GetApartID;
                        if (!int.TryParse(ReceivedData[1], out GetApartID))
                        {
                            Socket.SendWS( "compose_apart|msg_error|Ha ocurrido un problema al obtener la Información del Apartamento.");
                            return;
                        }

                        if (Client.GetRoleplay().Level < 2)
                        {
                            Socket.SendWS( "compose_apart|msg_error|Debes ser al menos Nivel 2 para comprar un apartamento.");
                            return;
                        }

                        ApartmentOwned AP = PolarEnvironment.GetGame().GetApartmentOwnedManager().GetApartmentByInsideRoom(GetApartID);
                        if (AP == null)
                        {
                            Socket.SendWS( "compose_apart|msg_error|Ha ocurrido un problema al obtener la Información del Apartamento.");
                            return;
                        }
                        #endregion

                        #region Execute
                        Client.GetRoleplay().CooldownManager.CreateCooldown("buyapart", 1000, 30);

                        // No Vender
                        if (AP.Owner == Client.GetHabbo().Id)
                        {
                            // Hacemos toggle
                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_apart", "toggle_offer_apart," + AP.RoomId + "," + AP.Price + "," + AP.PaymentType);
                        }
                        // Comprar
                        else
                        {
                            string DisplayPrice = "$ " + String.Format("{0:N0}", AP.Price);
                            if (AP.PaymentType == "Rubies")
                            {
                                if (Client.GetHabbo().Diamonds < AP.Price)
                                {
                                    Socket.SendWS( "compose_apart|msg_error|No cuentas con los Rubies suficientes.");
                                    return;
                                }

                                DisplayPrice = AP.Price + " RB";

                                Client.GetHabbo().Diamonds -= AP.Price;
                                Client.GetHabbo().UpdateDiamondsBalance();
                            }
                            else
                            {
                                if (Client.GetHabbo().Credits < AP.Price)
                                {
                                    Socket.SendWS( "compose_apart|msg_error|No cuentas con el dinero suficiente.");
                                    return;
                                }

                                Client.GetHabbo().Credits -= AP.Price;
                                Client.GetHabbo().UpdateCreditsBalance();
                            }

                            if (!PolarEnvironment.GetGame().GetApartmentOwnedManager().BuyOfferApartment(Client, AP))
                            {
                                Socket.SendWS( "compose_apart|msg_error|Ha ocurrido un error al intentar comprar el apartamento.");
                                return;
                            }

                            Socket.SendWS( "compose_apart|msg_success|¡Apartamento comprado exitosamente!");
                            RoleplayManager.Shout(Client, "*Ha comprado un apartamento pagando " + DisplayPrice + " por el*", 5);
                            Client.SendWhisper("¡Felicidades por tu apartamento! Ahora dirígete al asensor para poder acceder a el.", 1);
                        }
                        // Refrescamos lista
                        PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_apart", "offer_aparts");
                        #endregion
                    }
                    break;
                #endregion

                #region Elevator Error
                case "msg_ele_error":
                    {
                        string[] PData = Data.Split(',');
                        string[] ReceivedData = PData[1].Split('|');
                        string Msg = ReceivedData[0];

                        Socket.SendWS( "compose_apart|msg_ele_error|" + Msg);
                    }
                    break;
                #endregion

                #region Apartment Lock
                case "open_apart_lock":
                    {
                        string[] PData = Data.Split(',');
                        string ReceivedData = PData[1];

                        Socket.SendWS( "compose_apart|open_apart_lock|" + ReceivedData);
                    }
                    break;
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
