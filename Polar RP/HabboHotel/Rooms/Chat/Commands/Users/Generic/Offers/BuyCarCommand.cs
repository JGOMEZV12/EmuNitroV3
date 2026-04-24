using System;
using System.Linq;
using System.Data;
using System.Text;
using System.Collections.Concurrent;
using Polar.Communication.Packets.Outgoing.Notifications;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Weapons;
using Polar.HabboRoleplay.Farming;
using Polar.HabboHotel.Items;
using Polar.HabboRoleplay.RoleplayUsers.Offers;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Offers
{
    class BuyCarCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_offers_offer"; }
        }

        public string Parameters
        {
            get { return "%user% %arma% %precio%"; }
        }

        public string Description
        {
            get { return "Ofrece el tipo deseado al usuario deseado"; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Execute
            /*

                        if (Params.Length == 0 || Params[1].ToLower() == "info" || Params[1].ToLower() == "list")
                        {
                            StringBuilder Message = new StringBuilder().Append("----- Lista de carros que puedes comprar -----\n\n");
                            Message.Append("¡Bienvenido a la tienda de autos de " + PolarEnvironment.GetConfig().data["hotel.name"] + "!\n Aquí podrás encontrar el auto de tus sueños a un precio accesible, porfavor escribe  <b>:comprarcarro nombre</b> para comprar el que desees\n\n");
                            Message.Append("<b>:comprarcarro skyblue</b> - Precio: $3.000\n");
                            Message.Append("<b>:comprarcarro fireball</b> - Precio: $4.000\n");
                            Message.Append("<b>:comprarcarro doggi</b> - Precio: $5.000\n");
                            Message.Append("<b>:comprarcarro bunni</b> - Precio: $5.000\n");
                            Message.Append("<b>:comprarcarro beetle</b> - Precio: $6.000\n");
                            Message.Append("<b>:comprarcarro aveo</b> - Precio: $9.000\n");
                            Message.Append("<b>:comprarcarro corolla</b> - Precio: $11.000\n");
                            Message.Append("<b>:comprarcarro mustanggt</b> - Precio: $30.000\n");
                            Message.Append("<b>:comprarcarro nascar4</b> - Precio: $40.000\n");
                            Message.Append("<b>:comprarcarro nascar</b> - Precio: $100.000\n");
                            Message.Append("<b>:comprarcarro JetPack</b> - Precio: $250.000\n\n");
                            Message.Append("Hey, si en algún momento deseas cambiar de carro, vendrán con un nuevo tanque de gasolina el cual debes llenar\n");
                            Session.SendNotification(Message.ToString());
                        }

                        if (Params[1].ToLower().Equals("skyblue"))
                        {
                            if (Session.GetHabbo().Credits < 3000)
                            {
                                Session.SendWhisper("Usted no puede permitirse este coche todavía , escriba :comprarcarro list para una lista de modelos disponibles.");
                                return;
                            }
                            RoleplayManager.Shout(Session, "*Compra un DatCarr celeste [-$3.000]");
                            RoleplayManager.GiveMoney(Session, -3000);
                            Session.GetRoleplay().CarType = 4;
                            Session.GetRoleplay().CarFuel = 300;
                            return;
                        }
                        if (Params[1].ToLower().Equals("fireball"))
                        {
                            if (Session.GetHabbo().Credits < 4000)
                            {
                                Session.SendWhisper("Usted no puede permitirse este coche todavía , escriba :comprarcarro list para una lista de modelos disponibles.");
                                return;
                            }
                            RoleplayManager.Shout(Session, "*Compra un DatCarr Bola de Fuego [-$4.000]");
                            RoleplayManager.GiveMoney(Session, -4000);
                            Session.GetRoleplay().CarType = 5;
                            Session.GetRoleplay().CarFuel = 300;
                            return;
                        }
                        if (Params[1].ToLower().Equals("doggi"))
                        {
                            if (Session.GetHabbo().Credits < 5000)
                            {
                                Session.SendWhisper("Usted no puede permitirse este coche todavía , escriba :comprarcarro list para una lista de modelos disponibles.");
                                return;
                            }
                            RoleplayManager.Shout(Session, "*Compra un DatCarr Doggi [-$5.000]");
                            RoleplayManager.GiveMoney(Session, -5000);
                            Session.GetRoleplay().CarType = 6;
                            Session.GetRoleplay().CarFuel = 300;
                        }
                        if (Params[1].ToLower().Equals("bunni"))
                        {
                            if (Session.GetHabbo().Credits < 5000)
                            {
                                Session.SendWhisper("Usted no puede permitirse este coche todavía , escriba :comprarcarro list para una lista de modelos disponibles.");
                                return;
                            }
                            RoleplayManager.Shout(Session, "*comprar a DatCarr Bunni [-$5.000]");
                            RoleplayManager.GiveMoney(Session, -5000);
                            Session.GetRoleplay().CarType = 7;
                            Session.GetRoleplay().CarFuel = 300;
                            return;
                        }
                        if (Params[1].ToLower().Equals("carstaff"))
                        {
                            if (Session.GetHabbo().Credits < 85000)
                            {
                                Session.SendWhisper("Usted no puede permitirse este coche todavía , escriba :comprarcarro list para una lista de modelos disponibles.");
                                return;
                            }
                            RoleplayManager.Shout(Session, "*comprar a DatCarr Carstaff [-$85.000]");
                            RoleplayManager.GiveMoney(Session, -85000);
                            Session.GetRoleplay().CarType = 8;
                            Session.GetRoleplay().CarFuel = 300;
                            return;
                        }
                        if (Params[1].ToLower().Equals("jetpack"))
                        {
                            if (Session.GetHabbo().Credits < 250000)
                            {
                                Session.SendWhisper("Usted no puede permitirse este coche todavía , escriba :comprarcarro list para una lista de modelos disponibles.");
                                return;
                            }
                            RoleplayManager.Shout(Session, "*comprar un JetPack [-$250.000]");
                            RoleplayManager.GiveMoney(Session, -250000);
                            Session.GetRoleplay().CarType = 9;
                            Session.GetRoleplay().CarFuel = 1000;
                            return;
                        }
                        if (Params[1].ToLower().Equals("nascar"))
                        {
                            if (Session.GetHabbo().Credits < 100000)
                            {
                                Session.SendWhisper("Usted no puede permitirse este coche todavía , escriba :comprarcarro list para una lista de modelos disponibles.");
                                return;
                            }
                            RoleplayManager.Shout(Session, "*comprar un nascar [-$750.000]");
                            RoleplayManager.GiveMoney(Session, -100000);
                            Session.GetRoleplay().CarType = 10;
                            Session.GetRoleplay().CarFuel = 300;
                            return;
                        }
                        if (Params[1].ToLower().Equals("nascar4"))
                        {
                            if (Session.GetHabbo().Credits < 40000)
                            {
                                Session.SendWhisper("Usted no puede permitirse este coche todavía , escriba :comprarcarro list para una lista de modelos disponibles.");
                                return;
                            }
                            RoleplayManager.Shout(Session, "*comprar un Nascar4 [-$40.000]");
                            RoleplayManager.GiveMoney(Session, -40000);
                            Session.GetRoleplay().CarType = 11;
                            Session.GetRoleplay().CarFuel = 300;
                            return;
                        }
                        if (Params[1].ToLower().Equals("aveo"))
                        {
                            if (Session.GetHabbo().Credits < 9000)
                            {
                                Session.SendWhisper("Usted no puede permitirse este coche todavía , escriba :comprarcarro list para una lista de modelos disponibles.");
                                return;
                            }
                            RoleplayManager.Shout(Session, "*comprar un Aveo [-$9.000]");
                            RoleplayManager.GiveMoney(Session, -9000);
                            Session.GetRoleplay().CarType = 12;
                            Session.GetRoleplay().CarFuel = 300;
                            return;
                        }
                        if (Params[1].ToLower().Equals("corolla"))
                        {
                            if (Session.GetHabbo().Credits < 11000)
                            {
                                Session.SendWhisper("Usted no puede permitirse este coche todavía , escriba :comprarcarro list para una lista de modelos disponibles.");
                                return;
                            }
                            RoleplayManager.Shout(Session, "*comprar un Toyota Corolla [-$11.000]");
                            RoleplayManager.GiveMoney(Session, -11000);
                            Session.GetRoleplay().CarType = 13;
                            Session.GetRoleplay().CarFuel = 300;
                            return;
                        }
                        if (Params[1].ToLower().Equals("mustanggt"))
                        {
                            if (Session.GetHabbo().Credits < 30000)
                            {
                                Session.SendWhisper("Usted no puede permitirse este coche todavía , escriba :comprarcarro list para una lista de modelos disponibles.");
                                return;
                            }
                            RoleplayManager.Shout(Session, "*comprar un Ford Mustang GT [-$30.000]");
                            RoleplayManager.GiveMoney(Session, -30000);
                            Session.GetRoleplay().CarType = 14;
                            Session.GetRoleplay().CarFuel = 300;
                            return;
                        }
                        if (Params[1].ToLower().Equals("beetle"))
                        {
                            if (Session.GetHabbo().Credits < 6000)
                            {
                                Session.SendWhisper("Usted no puede permitirse este coche todavía , escriba :comprarcarro list para una lista de modelos disponibles.");
                                return;
                            }
                            RoleplayManager.Shout(Session, "*Comprar un Beetle [-$6.000]");
                            RoleplayManager.GiveMoney(Session, -6000);
                            Session.GetRoleplay().CarType = 15;
                            Session.GetRoleplay().CarFuel = 300;
                            return;
                        }
                        Session.SendWhisper("Usted no puede permitirse este coche todavía , escriba :comprarcarro list para una lista de modelos disponibles.");
                        return;
            */
            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_vehicle", "openshop");
            #endregion
        }
    }
}