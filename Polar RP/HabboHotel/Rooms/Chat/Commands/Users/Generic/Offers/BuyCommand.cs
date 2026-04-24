using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Pathfinding;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.Food;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.Quests;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Interactions.Items
{
    class BuyCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_offers_buy"; }
        }

        public string Parameters
        {
            get { return "%objeto% %cant%"; }
        }

        public string Description
        {
            get { return "Permite comprar objetos en su sitio respectivo: EJ: (repuestos,palanca,balde,martillo,materiales,semillas,etc)"; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Variables
            int Cant;
            bool OverrideConditions = false;
            #endregion

            #region Conditions
            if (Params.Length == 2)//:comprar [objeto] (Con cantidad predefinida)
            {
                if (Params[1].ToLower() == "materiales")
                    Cant = Convert.ToInt32(Params[2]);
                else if (Params[1].ToLower() == "bidon")
                    Cant = 1;
                else if (Params[1].ToLower() == "vehiculo")
                    Cant = 1;
                else if (Params[1].ToLower() == "productos")// 24/7
                    Cant = 1;
                else if (Params[1].ToLower() == "herramientas")// Ferretería
                    Cant = 1;
                else if (Params[1].ToLower() == "semillas")// Granja
                    Cant = 1;
                else if (Params[1].ToLower() == "nivel")
                {
                    Cant = 1;
                    OverrideConditions = true;
                }
                else if (Params[1].ToLower() == "telefono" || Params[1].ToLower() == "celular")
                    Cant = 1;
                else if(Params[1].ToLower() == "repuestos")
                {
                    Session.SendWhisper("Debes ingresar la cantidad a comprar. ((:comprar [objeto] [cantidad]))", 1);
                    return;
                }
                else
                {
                    Session.SendWhisper("'" + Params[1].ToLower() + "' no es un objeto válido a comprar.", 1);
                    return;
                }
            }
            else
            {
                if (Params.Length != 3)
                {
                    Session.SendWhisper("Debes ingresar el nombre del objeto y cantidad. :comprar [objeto] [cantidad]", 1);
                    return;
                }
                if (!int.TryParse(Params[2], out Cant))
                {
                    Session.SendWhisper("¡Cantidad Inválida!", 1);
                    return;
                }
                else if (Cant < 1)
                {
                    Session.SendWhisper("Al menos debes de comprar un Objeto.", 1);
                    return;
                }

            }

            #region Basic Conditions
            if (!OverrideConditions)
            {
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
                if (Session.GetRoleplay().DrivingCar)
                {
                    Session.SendWhisper("¡No puedes hacer eso mientras conduces!", 1);
                    return;
                }
            }
            #endregion
            
            if (Session.GetRoleplay().TryGetCooldown("comprar", true))
            {
                Session.SendWhisper("Por favor espera un poco para hacer eso nuevamente.", 1);
                return;
            }
            #endregion

            #region Execute
            string Type = Params[1].ToLower();
            switch (Type)
            {

                #region Repuestos
                case "repuestos":
                    {
                        #region Conditions
                        /*int MecID = PolarEnvironment.GetGame().GetRPRoomManager().TryToGetMecanicos(MyCity, out PlayRoom Data);//mecanicos de la cd.
                        if (Session.GetHabbo().CurrentRoomId != MecID)
                        {
                            Session.SendWhisper("Debes ir al Taller de Mecánicos de la ciudad para comprar repuestos", 1);
                            return;
                        }*/
                        #endregion

                        #region Comodin Conditions
                        Item BTile = null;
                        BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "actionpoint01" && x.Coordinate == Session.GetRoomUser().Coordinate);
                        if (BTile == null)
                        {
                            Session.SendWhisper("Debes acercarte al punto de venta de repuestos en la tienda de autos en el TALLER.", 1);
                            return;
                        }
                        #endregion


                        #region Execute
                        int RepairKitPrice = 1000;
                        int Price = RepairKitPrice * Cant;

                        if ((Session.GetRoleplay().MecParts + Cant) > 100)
                        {
                            Session.SendWhisper("¡No puedes llevar más de 100 repuestos en tu inventario!", 1);
                            return;
                        }
                        if (Session.GetHabbo().Credits < Price)
                        {
                            Session.SendWhisper("No cuentas con $" + Price + " para comprar esos repuestos.", 1);
                            return;
                        }

                        RoleplayManager.Shout(Session, "*Compra " + Cant + " repuestos y paga $" + Price + " por ellos*", 5);
                        Session.SendWhisper("Has comprado " + Cant + " repuestos y pagaste $" + Price, 1);
                        Session.GetHabbo().Credits -= Price;
                        Session.GetHabbo().UpdateCreditsBalance();
                        Session.GetRoleplay().MecParts += Cant;
                        Session.GetRoleplay().CooldownManager.CreateCooldown("comprar", 1000, 3);
                        #endregion
                    }
                    break;
                #endregion

                #region Vehicles
                case "vehiculo":
                    {
                        #region Conditions
                        if (!Room.BuyCarEnabled)
                        {
                            Session.SendWhisper("Debes ir a un concesionario para comprar vehículos.", 1);
                            return;
                        }
                        #endregion

                        #region Comodin Conditions
                        Item BTile = null;
                        BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "comodin_carro" && x.Coordinate == Session.GetRoomUser().Coordinate);
                        if (BTile == null)
                        {
                            Session.SendWhisper("Debes acercarte a un despacho para comprar un vehículo.", 1);
                            return;
                        }
                        #endregion

                        #region Execute
                        PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_vehicle", "openshop");
                        break;
                        #endregion
                    }
                #endregion

                #region Productos
                case "productos":
                    {
                        #region Conditions
                       /* if (!Room.SupermarketEnabled)
                        {
                            Session.SendWhisper("Debes ir a un supermercado para comprar productos.", 1);
                            return;
                        }*/
                        #endregion

                        #region Comodin Conditions
                        Item BTile = null;
                        BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "comodin_carro" && x.Coordinate == Session.GetRoomUser().Coordinate);
                        if (BTile == null)
                        {
                            Session.SendWhisper("Debes acercarte a la caja para comprar productos.", 1);
                            return;
                        }
                        #endregion

                        #region Execute
                        PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_products", "open_mall");
                        break;
                        #endregion
                    }
                #endregion

                #region Bidon
                case "bidon":
                    {
                        #region Conditions
                       /* if (!Room.GasEnabled)
                        {
                            Session.SendWhisper("Debes ir a una Gasolinera para comprar Bidones.", 1);
                            return;
                        }*/
                        #endregion

                        #region Comodin Conditions
                        Item BTile = null;
                        BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "comodin_carro" && x.Coordinate == Session.GetRoomUser().Coordinate);
                        if (BTile == null)
                        {
                            Session.SendWhisper("Debes acercarte al la despachadora de combustible.", 1);
                            return;
                        }
                        #endregion

                        #region Execute
                        int L = 5;
                        int Price = (L * RoleplayManager.FuelPrice) + 10;// $20
                        
                        if (Session.GetHabbo().Credits < Price)
                        {
                            Session.SendWhisper("No cuentas con $" + Price + " para comprar un Bidón.", 1);
                            return;
                        }

                        RoleplayManager.Shout(Session, "*Compra un Bidón de 5 L de Combustible y paga $" + Price + " por él*", 5);
                        Session.SendWhisper("Has comprado un Bidón de 5 L y pagaste $" + Price, 1);
                        Session.GetHabbo().Credits -= Price;
                        Session.GetHabbo().UpdateCreditsBalance();
                        Session.GetRoleplay().Bidon += Cant;
                        Session.GetRoleplay().CooldownManager.CreateCooldown("comprar", 1000, 3);
                        #endregion
                    }
                    break;
                #endregion

                #region Teléfono
                case "telefono":
                case "celular":
                    {
                        #region V1 OFF
                        /*
                        #region Conditions
                        if (Session.GetRoleplay().Phone > 0)
                        {
                            Session.SendWhisper("Ya cuentas con un teléfono móvil.", 1);
                            return;
                        }
                        if (!Room.PhoneStoreEnabled)
                        {
                            Session.SendWhisper("Debes ir a una Tienda de Teléfonos.", 1);
                            return;
                        }
                        #endregion

                        #region Comodin Conditions
                        Item BTile = null;
                        BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "comodin_carro" && x.Coordinate == Session.GetRoomUser().Coordinate);
                        if (BTile == null)
                        {
                            Session.SendWhisper("Debes acercarte al mostrador para comprar un teléfono.", 1);
                            return;
                        }
                        #endregion

                        #region Execute
                        int Price = 1500;

                        if (Session.GetHabbo().Credits < Price)
                        {
                            Session.SendWhisper("No cuentas con $" + Price + " para comprar un Teléfono.", 1);
                            return;
                        }

                        RoleplayManager.Shout(Session, "*Compra un Teléfono nuevo y paga $" + Price + " por él*", 5);
                        Session.SendWhisper("Has comprado un Teléfono y pagaste $" + Price, 1);
                        Session.SendWhisper("Ahora podrás agregar contactos, enviar mensajes y realizar llamadas.", 1);
                        Session.GetHabbo().Credits -= Price;
                        Session.GetHabbo().UpdateCreditsBalance();

                        // Obtenemos Numero Random con Formato (xxx)-xxx-xxxx
                        String NewNumber = RoleplayManager.GeneratePhoneNumber(Session.GetHabbo().Id);
                        PlusEnvironment.GetGame().GetClientManager().RegisterClientPhone(Session.GetRoomUser().GetClient(), Session.GetHabbo().Id, NewNumber);
                        Session.SendWhisper("Tu número es: " + NewNumber + ". ((Para volverlo a consultar usa :minumero))", 1);
                        // Actualizamos información del teléfono.
                        Session.GetRoleplay().Phone = 1;
                        Session.GetRoleplay().PhoneNumber = NewNumber;
                        PlusEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_phone", "show_button");
                        Session.GetRoleplay().CooldownManager.CreateCooldown("comprar", 1000, 3);
                        #endregion
                        */
                        #endregion

                        #region Comodin Conditions
                        Item BTile = null;
                        BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "comodin_carro" && x.Coordinate == Session.GetRoomUser().Coordinate);
                        if (BTile == null)
                        {
                            Session.SendWhisper("Debes acercarte al despacho para comprar un teléfono.", 1);
                            return;
                        }
                        #endregion

                        PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_phone", "open_shop_phone");
                    }
                    break;
                #endregion

                #region Materiales (only Gunners)
                case "materiales":
                    {

                            int Amount;
                        if (!int.TryParse(Cant.ToString(), out Amount))
                        {
                            Session.SendWhisper("¡Debes ingresar una cantidad!", 1);
                            return;
                        }

                        if (int.TryParse(Params[2], out Amount))
                        {
                            Cant = Amount;
                        
                        #region Group Conditions
                        List<Group> Groups = PolarEnvironment.GetGame().GetGroupManager().GetJobsForUser(Session.GetHabbo().Id);

                        if (Groups.Count <= 0)
                        {
                            Session.SendWhisper("No tienes ningún trabajo para hacer eso.", 1);
                            return;
                        }

                        int GroupNumber = -1;

                        if (Groups[0].GType != 2)
                        {
                            if (Groups.Count > 1)
                            {
                                if (Groups[1].GType != 2)
                                {
                                    Session.SendWhisper("((No perteneces a ningún trabajo usar ese comando))", 1);
                                    return;
                                }
                                GroupNumber = 1; // Segundo indicie de variable
                            }
                            else
                            {
                                Session.SendWhisper("((No perteneces a ningún trabajo para usar ese comando))", 1);
                                return;
                            }
                        }
                        else
                        {
                            GroupNumber = 0; // Primer indice de Variable Group
                        }

                        Session.GetRoleplay().JobId = Groups[GroupNumber].Id;
                        Session.GetRoleplay().JobRank = Groups[GroupNumber].Members[Session.GetHabbo().Id].UserRank;
                        #endregion

                        #region Extra Conditions            
                        // Existe el trabajo?
                        if (!GroupManager.JobExists(Session.GetRoleplay().JobId, Session.GetRoleplay().JobRank))
                        {
                            Session.GetRoleplay().TimeWorked = 0;
                            Session.GetRoleplay().JobId = 1; // Desempleado
                            Session.GetRoleplay().JobRank = 1;

                            //Room.Group.DeleteMember(Session.GetHabbo().Id);// OJO ACÁ

                            Session.SendWhisper("Lo sentimos, ese trabajo no existe. Te hemos removido ese trabajo.", 1);
                            return;
                        }

                        if (!GroupManager.HasJobCommand(Session, "armero"))
                        {
                            Session.SendWhisper("Debes tener el trabajo de Armero para usar ese comando.", 1);
                            return;
                        }
                        // Puede trabajar aquí?
                        Group Job = GroupManager.GetJob(Session.GetRoleplay().JobId);
                        GroupRank Rank = GroupManager.GetJobRank(Job.Id, Session.GetRoleplay().JobRank);
                        if (!Rank.CanWorkHere(Room.Id))
                        {
                            //String.Join(",", Rank.WorkRooms)
                            Session.SendWhisper("¡Debes buscar el Punto de Venta de materiales! Podrías buscar cerca de los Barrios de la Ciudad.", 1);
                            return;
                        }
                        #endregion

                        #region Comodin Conditions
                        Item BTile = null;
                        BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "actionpoint01" && x.Coordinate == Session.GetRoomUser().Coordinate);
                        if (BTile == null)
                        {
                            Session.SendWhisper("Debes acercarte al punto de Venta de Materiales.", 1);
                            return;
                        }
                        #endregion

                        #region Execute
                        int Price = Cant * RoleplayManager.ArmMatPrice;
                        if ((Session.GetRoleplay().ArmMat + Cant) > RoleplayManager.ArmMatLimit)
                        {
                            Session.SendWhisper("Solo puedes llevar " + RoleplayManager.ArmMatLimit + " materiales en tu inventario.", 1);
                            return;
                        }
                        // Does user has more credits than Price
                        if (Session.GetHabbo().Credits < Price)
                        {
                            Session.SendWhisper("Necesitas al menos $" + Price + " para comprar 50 materiales.", 1);
                            return;
                        }

                        RoleplayManager.Shout(Session, "*Compra "+ Cant + " materiales y paga $" + Price + " por ellos*", 5);
                        Session.SendWhisper("Ahora dirígete a la Fábrica para preparar las piezas usando ':crear priezas'.", 1);
                        Session.GetHabbo().Credits -= Price;
                        Session.GetHabbo().UpdateCreditsBalance();
                        Session.GetRoleplay().ArmMat += Cant;
                        Session.GetRoleplay().CooldownManager.CreateCooldown("comprar", 1000, 3);
                            #endregion
                        }
                        else
                        {
                            Session.SendWhisper("Ingresa la cantidad correctamente", 1);
                            return;
                        }
                    }
                    break;
                #endregion


                #region Default
                default:
                    {
                        Session.SendWhisper("'" + Type + "' no es un objeto válido a comprar.", 1);
                        break;
                    }
                    #endregion
            }
            #endregion
        }
    }
}