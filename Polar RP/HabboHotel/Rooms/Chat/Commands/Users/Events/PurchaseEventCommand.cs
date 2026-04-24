using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Polar.HabboRoleplay.Turfs;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Pathfinding;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.Groups;
using Polar.Communication.Packets.Outgoing.Handshake;
using Polar.Communication.Packets.Outgoing.Notifications;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Events
{
    class PurchaseEventCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_events_purchase"; }
        }

        public string Parameters
        {
            get { return "%type%"; }
        }

        public string Description
        {
            get { return "Le permite comprar productos con puntos de evento."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            int EventsLobby = Convert.ToInt32(RoleplayData.GetData("eventslobby", "roomid"));

            if (Room.Id != EventsLobby)
            {
                Session.SendWhisper("¡Debe estar dentro del Lobby de Eventos para ver la tienda de Puntos de Evento!", 1);
                return;
            }

            if (Params.Length == 1)
            {
                #region Store List
                StringBuilder Message = new StringBuilder().Append("----------- Comprar con puntos VIP -----------\n");
                Message.Append("To purchase any of the following, please type ':comprar (numero)', eg. ':comprar 1'\n\n");
                Message.Append(" 1   ---->   5 Puntos VIP por $80\n\n");
                Message.Append(" 2   ---->   5 Puntos VIP por 200 balas\n\n");
                Message.Append(" 3   ---->   5 Puntos VIP por 300 saldo\n\n");
                Message.Append(" 4   ---->   10 Puntos VIP por 250 galones de gasolina\n\n");
                Message.Append(" 5   ---->   25 Puntos VIP por cambiar un teléfono (para iPhone 4s)\n\n");
                Message.Append(" 6   ---->   35 Puntos VIP por cambiar un teléfono (to iPhone 7)\n\n");
                Message.Append(" 7   ---->   50 Puntos VIP por $1000\n\n");
                Message.Append(" 8   ---->   50 Puntos VIP por 2500 balas\n\n");
                Message.Append(" 9   ---->   50 Puntos VIP por 3750 saldo\n\n");
                Message.Append(" 10   ---->   50 Puntos VIP por cambiar de carro (a Honda Accord)\n\n");
                Message.Append(" 11   ---->   60 Puntos VIP por por 'flagme' (No necesitas VIP)\n\n");
                Message.Append(" 12   ---->   80 Puntos VIP por cambiar de carro (un Nissan GTR)\n\n");
                Message.Append(" 13   ---->   100 Puntos VIP por 3250 litros de gasolina\n\n");
                Message.Append(" 14   ---->   200 Puntos VIP por cambiar clase\n\n");
                Message.Append(" 15   ---->   1000 Puntos VIP por Arma premium (un arma premium, puede pagar 0,99 $ por ella)\n\n");
                Session.SendMessage(new MOTDNotificationComposer(Message.ToString()));
                #endregion
            }
            else
            {
                switch (Params[1].ToLower())
                {
                    #region Type 1
                    case "1":
                        {
                            if (Session.GetHabbo().EventPoints < 5)
                            {
                                Session.SendWhisper("No tienes suficientes puntos VIP para comprar: 1", 1);
                                break;
                            }

                            Session.GetHabbo().EventPoints -= 5;
                            Session.GetHabbo().UpdateEventPointsBalance();

                            Session.GetHabbo().Credits += 80;
                            Session.GetHabbo().UpdateCreditsBalance();
                            Session.Shout("*Reclama un producto a cambio de puntos vip ¡Enhorabuena!*", 4);
                            break;
                        }
                    #endregion

                    #region Type 2
                    case "2":
                        {
                            if (Session.GetHabbo().EventPoints < 5)
                            {
                                Session.SendWhisper("No tienes suficientes puntos VIP para comprar: 2", 1);
                                break;
                            }

                            Session.GetHabbo().EventPoints -= 5;
                            Session.GetHabbo().UpdateEventPointsBalance();

                            Session.GetRoleplay().Bullets += 200;
                            Session.Shout("*Reclama un producto a cambio de puntos vip ¡Enhorabuena!*", 4);
                            break;
                        }
                    #endregion

                    #region Type 3
                    case "3":
                        {
                            if (Session.GetHabbo().EventPoints < 5)
                            {
                                Session.SendWhisper("No tienes suficientes puntos VIP para comprar: 3", 1);
                                break;
                            }

                            Session.GetHabbo().EventPoints -= 5;
                            Session.GetHabbo().UpdateEventPointsBalance();

                            Session.GetHabbo().Duckets += 300;
                            Session.GetHabbo().UpdateDucketsBalance();
                            Session.Shout("*Reclama un producto a cambio de puntos vip ¡Enhorabuena!*", 4);
                            break;
                        }
                    #endregion

                    #region Type 4
                    case "4":
                        {
                            if (Session.GetHabbo().EventPoints < 10)
                            {
                                Session.SendWhisper("No tienes suficientes puntos VIP para comprar: 4", 1);
                                break;
                            }

                            Session.GetHabbo().EventPoints -= 10;
                            Session.GetHabbo().UpdateEventPointsBalance();

                            Session.GetRoleplay().CarFuel += 250;
                            Session.Shout("*Reclama un producto a cambio de puntos vip ¡Enhorabuena!*", 4);
                            break;
                        }
                    #endregion

                    #region Type 5
                    case "5":
                        {
                            if (Session.GetHabbo().EventPoints < 25)
                            {
                                Session.SendWhisper("No tienes suficientes puntos VIP para comprar: 5", 1);
                                break;
                            }

                            /*if (Session.GetRoleplay().PhoneType <= 0)
                            {
                                Session.SendWhisper("You must have a phone in order to purchase an upgrade!", 1);
                                break;
                            }

                            if (Session.GetRoleplay().PhoneType > 2)
                            {
                                Session.SendWhisper("You already have a better phone than the iPhone 4s!", 1);
                                break;
                            }*/

                            Session.GetHabbo().EventPoints -= 25;
                            Session.GetHabbo().UpdateEventPointsBalance();

                            //Session.GetRoleplay().PhoneType = 2;
                            Session.Shout("*Reclama un producto a cambio de puntos vip ¡Enhorabuena!*", 4);
                            break;
                        }
                    #endregion

                    #region Type 6
                    case "6":
                        {
                            /*if (Session.GetHabbo().EventPoints < 35)
                            {
                                Session.SendWhisper("No tienes suficientes puntos VIP para comprar: 6", 1);
                                break;
                            }

                            if (Session.GetRoleplay().PhoneType <= 1)
                            {
                                Session.SendWhisper("No tienes un iPhone 4s para actualizar a la iPhone 7!", 1);
                                break;
                            }*/

                            Session.GetHabbo().EventPoints -= 35;
                            Session.GetHabbo().UpdateEventPointsBalance();

                            //Session.GetRoleplay().PhoneType = 3;
                            Session.Shout("*Reclama un producto a cambio de puntos vip ¡Enhorabuena!*", 4);
                            break;
                        }
                    #endregion

                    #region Type 7
                    case "7":
                        {
                            if (Session.GetHabbo().EventPoints < 50)
                            {
                                Session.SendWhisper("No tienes suficientes puntos VIP para comprar: 7", 1);
                                break;
                            }

                            Session.GetHabbo().EventPoints -= 50;
                            Session.GetHabbo().UpdateEventPointsBalance();

                            Session.GetHabbo().Credits += 1000;
                            Session.GetHabbo().UpdateCreditsBalance();
                            Session.Shout("*Reclama un producto a cambio de puntos vip ¡Enhorabuena!*", 4);
                            break;
                        }
                    #endregion

                    #region Type 8
                    case "8":
                        {
                            if (Session.GetHabbo().EventPoints < 50)
                            {
                                Session.SendWhisper("No tienes suficientes puntos VIP para comprar: 2", 8);
                                break;
                            }

                            Session.GetHabbo().EventPoints -= 50;
                            Session.GetHabbo().UpdateEventPointsBalance();

                            Session.GetRoleplay().Bullets += 2500;
                            Session.Shout("*Reclama un producto a cambio de puntos vip ¡Enhorabuena!*", 4);
                            break;
                        }
                    #endregion

                    #region Type 9
                    case "9":
                        {
                            if (Session.GetHabbo().EventPoints < 50)
                            {
                                Session.SendWhisper("No tienes suficientes puntos VIP para comprar: 9", 1);
                                break;
                            }

                            Session.GetHabbo().EventPoints -= 50;
                            Session.GetHabbo().UpdateEventPointsBalance();

                            Session.GetHabbo().Duckets += 3750;
                            Session.GetHabbo().UpdateDucketsBalance();
                            Session.Shout("*Reclama un producto a cambio de puntos vip ¡Enhorabuena!*", 4);
                            break;
                        }
                    #endregion

                    #region Type 10
                    case "10":
                        {
                            if (Session.GetHabbo().EventPoints < 50)
                            {
                                Session.SendWhisper("No tienes suficientes puntos VIP para comprar: 10", 1);
                                break;
                            }

                            if (Session.GetRoleplay().CarType <= 0)
                            {
                                Session.SendWhisper("Usted debe tener un coche para comprar una actualización!", 1);
                                break;
                            }

                            if (Session.GetRoleplay().CarType > 2)
                            {
                                Session.SendWhisper("Ya tienes un coche mejor que el Honda Accord!", 1);
                                break;
                            }

                            Session.GetHabbo().EventPoints -= 50;
                            Session.GetHabbo().UpdateEventPointsBalance();

                            Session.GetRoleplay().CarType = 2;
                            Session.Shout("*Reclama un producto a cambio de puntos vip ¡Enhorabuena!*", 4);
                            break;
                        }
                    #endregion

                    #region Type 11
                    case "11":
                        {
                            if (Session.GetHabbo().EventPoints < 60)
                            {
                                Session.SendWhisper("No tienes suficientes puntos VIP para comprar: 11", 1);
                                break;
                            }

                            Session.GetHabbo().EventPoints -= 60;
                            Session.GetHabbo().UpdateEventPointsBalance();

                            Session.GetHabbo().LastNameChange = 0;
                            Session.GetHabbo().ChangingName = true;
                            Session.GetRoleplay().FreeNameChange = true;
                            Session.SendNotification("Tenga en cuenta que si su nombre de usuario es considerado inapropiado, será ban sin duda.\r\rTambién tenga en cuenta que el personal NO le permitirá cambiar su nombre de usuario de nuevo si tiene un problema con lo que ha elegido.\r\r¡Cierra esta ventana y haz clic para empezar a elegir un nuevo nombre de usuario!");
                            Session.SendMessage(new UserObjectComposer(Session.GetHabbo()));
                            break;
                        }
                    #endregion

                    #region Type 12
                    case "12":
                        {
                            if (Session.GetHabbo().EventPoints < 80)
                            {
                                Session.SendWhisper("No tienes suficientes puntos VIP para comprar: 12", 1);
                                break;
                            }

                            if (Session.GetRoleplay().CarType <= 1)
                            {
                                Session.SendWhisper("Usted no tiene un Honda Accord para actualizar a la Nissan GTR!", 1);
                                break;
                            }

                            Session.GetHabbo().EventPoints -= 80;
                            Session.GetHabbo().UpdateEventPointsBalance();

                            Session.GetRoleplay().CarType = 3;
                            Session.Shout("*Reclama un producto a cambio de puntos vip ¡Enhorabuena!*", 4);
                            break;
                        }
                    #endregion

                    #region Type 13
                    case "13":
                        {
                            if (Session.GetHabbo().EventPoints < 100)
                            {
                                Session.SendWhisper("No tienes suficientes puntos VIP para comprar: 13", 1);
                                break;
                            }

                            Session.GetHabbo().EventPoints -= 100;
                            Session.GetHabbo().UpdateEventPointsBalance();

                            Session.GetRoleplay().CarFuel += 3250;
                            Session.Shout("*Reclama un producto a cambio de puntos vip ¡Enhorabuena!*", 4);
                            break;
                        }
                    #endregion

                    #region Type 14
                    case "14":
                        {
                            if (Session.GetHabbo().EventPoints < 200)
                            {
                                Session.SendWhisper("No tienes suficientes puntos VIP para comprar: 14", 1);
                                break;
                            }

                            if (Params.Length < 3)
                            {
                                Session.SendWhisper("Por favor escribe ':comprar 14 lista' Para ver sus opciones, para elegir una opción, eg. ':comprar 14 peleador'", 1);
                                break;
                            }
                            else
                            {
                                bool RunQuery = false;
                                if (Params[2].ToLower() == "list")
                                {
                                    StringBuilder Message = new StringBuilder().Append("----- Clases " + PolarEnvironment.GetConfig().data["hotel.name"] + " -----\n\n");
                                    Message.Append("¡Los civiles reciben más dinero cuando completan un ciclo de trabajo!\n\n");
                                    Message.Append("Los combatientes causan un poco más de daño con sus puños (:golpear comando)!\n\n");
                                    Message.Append("Los artilleros causan un poco más de daño con armas de fuego (:disparar comando)!\n\n");
                                    Message.Append("Elija sabiamente, ya que le costará 200 puntos vip");
                                    Session.SendNotification(Message.ToString());
                                }
                                else if (Params[2].ToLower() == "peleador")
                                {
                                    Session.GetRoleplay().Class = "peleador";
                                    Session.GetHabbo().Motto = "peleador";
                                    Session.GetHabbo().Poof(true);
                                    RunQuery = true;
                                }
                                else if (Params[2].ToLower() == "peleador")
                                {
                                    Session.GetRoleplay().Class = "peleador";
                                    Session.GetHabbo().Motto = "peleador";
                                    Session.GetHabbo().Poof(true);
                                    RunQuery = true;
                                }
                                else if (Params[2].ToLower() == "ciudadano")
                                {
                                    Session.GetRoleplay().Class = "ciudadano";
                                    Session.GetHabbo().Motto = "ciudadano";
                                    Session.GetHabbo().Poof(true);
                                    RunQuery = true;
                                }
                                else
                                    Session.SendWhisper("¡No has escogido una clase válida! Por favor, elija 'armero', 'peleador', 'ciudadano' o 'list' si no está seguro!", 1);

                                if (RunQuery)
                                {
                                    using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                                    {
                                        dbClient.SetQuery("UPDATE `users` set `motto` = @class WHERE `id` = @userid LIMIT 1");
                                        dbClient.AddParameter("class", Session.GetRoleplay().Class);
                                        dbClient.AddParameter("userid", Session.GetHabbo().Id);
                                        dbClient.RunQuery();

                                        dbClient.SetQuery("UPDATE `rp_stats` set `class` = @class WHERE `id` = @userid LIMIT 1");
                                        dbClient.AddParameter("class", Session.GetRoleplay().Class);
                                        dbClient.AddParameter("userid", Session.GetHabbo().Id);
                                        dbClient.RunQuery();
                                    }

                                    Session.GetHabbo().EventPoints -= 200;
                                    Session.GetHabbo().UpdateEventPointsBalance();
                                    Session.Shout("*Reclama un producto a cambio de puntos vip ¡Enhorabuena!*", 4);
                                    Session.SendNotification("cambiaste tu clase a " + Session.GetRoleplay().Class + "!");
                                }
                            }
                            break;
                        }
                    #endregion

                    #region Type 15
                    case "15":
                        {
                            StringBuilder Message = new StringBuilder().Append("---------- Armas personalizadas ----------\n\n");
                            Message.Append("¡Primero off, usted DEBE poseer el arma que usted quiere modificar para requisitos particulares! Ejemplo: Glock, MP5, cuchillo etc...n\n");
                            Message.Append("La pistola de encargo se verá la misma como el arma regular, pero tienen 'oro de adorno' para diferenciarla.\n\n");
                            Message.Append("El arma personalizada también vendrá con su propio ':sacar texto' y ':disparar texto'.\n\n");
                            Message.Append("Tenga en cuenta que la pistola personalizada hará el mismo daño, etc. como su versión no personalizada.\n\n");
                            Message.Append("¡El arma de encargo es puramente para los propósitos estéticos, para 'mostrar apagado' sus logros y para mirar!\n\n");
                            Message.Append("¡Si usted quisiera comprar la arma de encargo, notifique por favor a Hefesto si usted tiene los 1000 puntos del acontecimiento!");
                            Session.SendMessage(new MOTDNotificationComposer(Message.ToString()));
                            break;
                        }
                    #endregion

                    #region Default
                    default:
                        {
                            Session.SendWhisper("Que no es uno de los tipos de opción, por favor escriba ':comprar' para ver todas las opciones!", 1);
                            break;
                        }
                        #endregion
                }
            }
        }
    }
}