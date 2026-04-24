using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.Items;
using Polar.HabboRoleplay.Farming;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Phones;
using Polar.HabboRoleplay.Turfs;
using Polar.HabboRoleplay.VehicleOwned;
using Polar.HabboRoleplay.Vehicles;
using Polar.HabboRoleplay.Weapons;
using System.Data;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Offers
{
    class SellCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_offers_sell"; }
        }
        // :reparar [x] [precio]
        // :vender [x] [objeto] [precio]
        // :vender [x] [objeto] [cantidad] [precio] (consumibles)
        public string Parameters
        {
            get { return "%user% %obj% %cant% %price%"; }
        }

        public string Description
        {
            get { return "Vende algo a otra persona."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Vars
            int Dir = 0;
            string Params2 = "";
            string command = Params[0];
            //int WantedLevel = 1;
            //int NewWantedLevel = 1;
            bool hasWanted = false;
            string RoomId = Session.GetHabbo().CurrentRoomId.ToString() != "0" ? Session.GetHabbo().CurrentRoomId.ToString() : "Unknown";
            Wanted NewWanted = new Wanted(Convert.ToUInt32(Session.GetHabbo().Id), RoomId, 5);
            #endregion

            #region Conditions

            #region Params Conditions
            // Primero verificar que Params no sea null
            if (Params == null || Params.Length < 1)
            {
                Session.SendWhisper("Comando inválido.", 1);
                return;
            }

            if (command == "mamada" && Params.Length >= 3)
            {
                Dir = 1;
                Params2 = "mamada";
            }
            else if (Params.Length >= 3 && (command == "reparar" || command == "vender"))
            {
                Dir = 1;
                Params2 = "reparacion";
            }
            else if (Params.Length >= 4)
            {
                Dir = 1;
                Params2 = Params[2];
            }
            else if (Params.Length >= 5)
            {
                Dir = 2;
                Params2 = Params[2];
            }
            else if (Params.Length >= 2) // Cambiado de 1 a 2 porque necesitamos al menos 2 parámetros
            {
                Dir = 3;
                Params2 = Params[1];

                /*
                if(Params2.ToLower() != "objeto")
                {
                    Session.SendWhisper("Comando inválido, escribe ':vender objeto'", 1);
                    return;
                }
                */
            }
            else
            {
                if (command == "reparar")
                    Session.SendWhisper("Comando inválido, escribe :reparar [cliente] [precio]", 1);
                else if (command == "mamada")
                    Session.SendWhisper("Comando inválido, escribe :mamada [cliente] [precio]", 1);
                else
                    Session.SendWhisper("Comando inválido, escribe :vender [usuario] [objeto] [precio] o :vender [usuario] [objeto] [cantidad] [precio]", 1);
                return;
            }
            #endregion

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

            if (Session.GetRoleplay().DrivingCar)
            {
                Session.SendWhisper("¡No puedes hacer eso mientras conduces!", 1);
                return;
            }
            #endregion

            #region GetTarget (Si no es :vender objeto => Targer es el Params[1])
            GameClient Target = null;
            RoomUser TargetUser = null;
            if (Dir != 3)
            {
                // Verificar que Params[1] existe
                if (Params.Length < 2)
                {
                    Session.SendWhisper("Debes especificar un usuario.", 1);
                    return;
                }

                Target = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);
                if (Target == null)
                {
                    Session.SendWhisper("No se ha podido encontrar al usuario.", 1);
                    return;
                }

                TargetUser = Room.GetRoomUserManager().GetRoomUserByHabbo(Target.GetHabbo().Username);
                if (TargetUser == null)
                {
                    Session.SendWhisper("Ha ocurrido un error en encontrar al usuario, probablemente esté desconectado o no está en la Zona.", 1);
                    return;
                }

                if (TargetUser.GetClient() == Session)
                {
                    Session.SendWhisper("¡No puedes venderte cosas a ti mism@!", 1);
                    return;
                }

                /*if (TargetUser.GetClient().GetConnection().getIp() == Session.GetConnection().getIp())
                {
                    Session.SendWhisper("¡No puedes vender/ofrecer servicios entre tus cuentas!", 1);
                    return;
                }*/
            }
            #endregion

            if (Session.GetRoleplay().TryGetCooldown("sell", true))
                return;
            #endregion

            if (Dir == 1)
            {
                #region :vender [x] [objeto] [precio]
                string Type = Params2;
                int Price = 0;

                #region Weapon Check
                Weapon weapon = null;
                foreach (Weapon Weapon in WeaponManager.Weapons.Values)
                {
                    if (Type.ToLower() == Weapon.Name.ToLower())
                    {
                        Type = "weapon";
                        weapon = Weapon;
                    }
                }
                #endregion

                switch (Type.ToLower())
                {
                    #region Weapon
                    case "weapon":
                    case "arma":
                        {
                            if (weapon == null)
                            {
                                Session.SendWhisper("'" + Type + "' no es un arma válida.");
                                break;
                            }
                            /*if (Session.GetRoleplay().EquippedWeapon == null || Session.GetRoleplay().EquippedWeapon.Name.ToLower() != weapon.Name.ToLower())
                            {
                                Session.SendWhisper("Debes tener equipada el arma a vender.", 1);
                                break;
                            }*/
                            if (Session.GetHabbo().Rank < 2)
                            {
                                if (!GroupManager.HasJobCommand(Session, "weapon") && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                                {
                                    Session.SendWhisper("Lo siento, no trabajas en la tienda de armas", 1);
                                    break;
                                }
                            }

                            if (Session.GetRoleplay().PassiveMode)
                            {
                                Session.SendWhisper("No puedes hacer eso mientras estás en modo pasivo.", 1);
                                break;
                            }
                            if (Target.GetRoleplay().PassiveMode)
                            {
                                Session.SendWhisper("No puedes venderle eso a una persona en modo pasivo.", 1);
                                break;
                            }
                            if (Target.GetRoleplay().OwnedWeapons.ContainsKey(weapon.Name))
                            {
                                Session.SendWhisper("Esta persona ya tiene un/a " + weapon.PublicName, 1);
                                break;
                            }
                            else
                            {
                                bool HasOffer = false;

                                if (Session.GetHabbo().Rank < 8)
                                {
                                    Price = (!Target.GetRoleplay().OwnedWeapons.ContainsKey(weapon.Name) ? weapon.Cost : weapon.CostFine);

                                    #region Conditions Price
                                    if (Price < 0 || Price == 0 || Price <= 0)
                                    {
                                        Session.SendWhisper("El precio no puede ser negativo o igual a cero.", 1);
                                        return;
                                    }
                                    #endregion

                                    if (Target.GetHabbo().Credits >= Convert.ToInt32(Price.ToString().Replace("-", "")))
                                    {
                                        foreach (var Offer in Target.GetRoleplay().OfferManager.ActiveOffers.Values)
                                        {
                                            if (WeaponManager.Weapons.ContainsKey(Offer.Type.ToLower()))
                                                HasOffer = true;
                                        }
                                        if (!HasOffer)
                                        {
                                            RoleplayManager.Shout(Session, "*Ofrece un/a " + weapon.PublicName + " a " + Target.GetHabbo().Username + " por $" + String.Format("{0:N0}", Price) + "*", 5);
                                            Target.GetRoleplay().OfferManager.CreateOffer(weapon.Name.ToLower(), Session.GetHabbo().Id, Price);
                                            Target.SendWhisper("Te han ofrecido un/a " + weapon.PublicName + " por $" + String.Format("{0:N0}", Price) + " Escribe ':aceptar arma' para comprarla ó en su defecto ':rechazar arma'.", 1);
                                            //Target.SendWhisper("Detalles del Arma: Uso: " + Session.GetRoleplay().WLife + " / " + weapon.WLife, 1);
                                            break;
                                        }
                                        else
                                        {
                                            Session.SendWhisper("¡Esta persona tiene una oferta de Arma pendiente!", 1);
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        Session.SendWhisper("Esta persona no tiene dinero suficiente para comprar tu " + weapon.PublicName, 1);
                                        break;
                                    }
                                }
                                else
                                {
                                    // Verificar que Params[3] existe
                                    if (Params.Length < 4)
                                    {
                                        Session.SendWhisper("Debes especificar un precio.", 1);
                                        return;
                                    }

                                    if (int.TryParse(Params[3], out Price))
                                    {
                                        foreach (var Offer in Target.GetRoleplay().OfferManager.ActiveOffers.Values)
                                        {
                                            if (WeaponManager.Weapons.ContainsKey(Offer.Type.ToLower()))
                                                HasOffer = true;
                                        }
                                        if (!HasOffer)
                                        {
                                            RoleplayManager.Shout(Session, "*Ofrece un/a " + weapon.PublicName + " a " + Target.GetHabbo().Username + " por $" + String.Format("{0:N0}", Price) + "*", 5);
                                            Target.GetRoleplay().OfferManager.CreateOffer(weapon.Name.ToLower(), Session.GetHabbo().Id, Price);
                                            Target.SendWhisper("Te han ofrecido un/a " + weapon.PublicName + " por $" + String.Format("{0:N0}", Price) + " Escribe ':aceptar arma' para comprarla ó en su defecto ':rechazar arma'.", 1);
                                            //Target.SendWhisper("Detalles del Arma: Uso: " + Session.GetRoleplay().WLife + " / " + weapon.WLife, 1);
                                            break;
                                        }
                                        else
                                        {
                                            Session.SendWhisper("¡Esta persona tiene una oferta de Arma pendiente!", 1);
                                            break;
                                        }

                                    }
                                    else
                                    {
                                        Session.SendWhisper("El precio es inválido", 1);
                                        return;
                                    }
                                }
                                /*if (int.TryParse(Params[3], out Price))
                            { */


                                /*}
                                else
                                {
                                    Session.SendWhisper("El precio es inválido", 1);
                                    return;
                                }*/

                            }
                        }
                    #endregion

                    #region Reparación
                    case "reparacion":
                        {
                            string Job = "";
                            if (command != "reparar")
                            {
                                Session.SendWhisper("Para ofrecer reparaciones de vehículos o armas (depende tu trabajo), usa: ':reparar [cliente] [precio]'", 1);
                                return;
                            }

                            // Verificar que Params[2] existe
                            if (Params.Length < 3)
                            {
                                Session.SendWhisper("Debes especificar un precio.", 1);
                                return;
                            }

                            if (int.TryParse(Params[2], out Price))
                            {
                                #region Conditions Price
                                if (Price < 0 || Price == 0 || Price <= 0)
                                {
                                    Session.SendWhisper("El precio no puede ser negativo.", 1);
                                    return;
                                }
                                #endregion

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

                                if (!Session.GetRoleplay().IsWorking)
                                {
                                    Session.SendWhisper("Tu trabajo actual no te permite hacer uso de ese comando. ¡Solo Mecánicos o Armeros!", 1);
                                    return;
                                }

                                if (GroupManager.HasJobCommand(Session, "armero"))
                                {
                                    Job = "armero";
                                }
                                else if (GroupManager.HasJobCommand(Session, "reparar"))
                                {
                                    Job = "mecanico";
                                }



                                #endregion

                                if (Job != "armero")
                                {
                                    #region Mecánico
                                    if (RoleplayManager.PurgeStarted)
                                    {
                                        Session.SendWhisper("¡No puedes trabajar durante la purga!", 1);
                                        return;
                                    }

                                    #region Conditions Mec
                                    foreach (var Offer in Target.GetRoleplay().OfferManager.ActiveOffers.Values)
                                    {
                                        if (Offer.Type.ToLower() == "fix_mecanico")
                                        {
                                            Session.SendWhisper("Esa persona ya tiene una reparación de vehículo pendiente.", 1);
                                            return;
                                        }
                                    }
                                    if (!Target.GetRoleplay().PediMec)
                                    {
                                        Session.SendWhisper("Esa persona no ha solicitado los servicios de un Mecánico.", 1);
                                        return;
                                    }

                                    #region Check Vehicle InFront
                                    int FuelSize = 0;
                                    Vehicle vehicle = null;
                                    bool found = false;
                                    int itemfurni = 0, corp = 0;
                                    Item BTile = null;
                                    string itemnm = null;
                                    foreach (Vehicle Vehicle in VehicleManager.Vehicles.Values)
                                    {
                                        if (!found)
                                        {
                                            BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == Vehicle.ItemName && x.Coordinate == Session.GetRoomUser().SquareInFront);
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

                                    if (!found)
                                    {
                                        Session.SendWhisper("¡Debes estar frente al vehículo a reparar!", 1);
                                        return;
                                    }

                                    #endregion

                                    #region Select Vehicle State
                                    int state = 0;
                                    List<VehiclesOwned> VO = PolarEnvironment.GetGame().GetVehiclesOwnedManager().getVehiclesOwnedByFurniId(itemfurni);
                                    if (VO == null || VO.Count <= 0)
                                    {
                                        RoleplayManager.Shout(Session, "* Una grúa ha pasado a recoger el vehículo que " + Session.GetHabbo().Username + " intentaba reparar.");
                                        RoleplayManager.PickItem(Session, itemfurni);
                                        return;
                                    }
                                    state = VO[0].State;
                                    #endregion

                                    #region Check Vehicle State
                                    if (state == 2 || state == 3 || VO[0].CarLife <= 0)
                                    {
                                        if (state == 3)
                                            Session.GetRoleplay().MecNewState = 1;// óptimo y con traba
                                        else
                                            Session.GetRoleplay().MecNewState = 0;// óptimo y sin traba
                                    }
                                    else
                                    {
                                        Session.SendWhisper("Este vehículo no necesita ser reparado.", 1);
                                        return;
                                    }
                                    #endregion

                                    #region Calc Repair Kit Cant
                                    if (FuelSize <= 90)// Tanque Pequeño
                                    {
                                        Session.GetRoleplay().MecPartsTo = 3;
                                    }
                                    else if (FuelSize > 90 && FuelSize <= 100)// Tanque Mediano
                                    {
                                        Session.GetRoleplay().MecPartsTo = 6;
                                    }
                                    else // Tanque Grande
                                    {
                                        Session.GetRoleplay().MecPartsTo = 9;
                                    }
                                    #endregion
                                    #endregion

                                    #region Execute
                                    if (Session.GetRoleplay().MecParts >= Session.GetRoleplay().MecPartsTo)
                                    {
                                        Session.GetRoleplay().MecCarToRepair = itemfurni;
                                        Session.GetRoleplay().MecRotPosition = Session.GetRoomUser().RotBody;
                                        Session.GetRoleplay().MecCordinates = Session.GetRoomUser().Coordinate;
                                        RoleplayManager.Shout(Session, "*Ofrece una reparación a " + Target.GetHabbo().Username + " por $" + String.Format("{0:N0}", Price) + "*", 5);
                                        Target.GetRoleplay().OfferManager.CreateOffer("fix_mecanico", Session.GetHabbo().Id, Price);
                                        Target.SendWhisper("Te han ofrecido una reparación de Vehículo por $" + String.Format("{0:N0}", Price) + ". Escribe ':aceptar reparacion' para aceptarla ó en su defecto ':rechazar reparacion'.", 1);
                                        Session.GetRoleplay().CooldownManager.CreateCooldown("comprar", 1000, 3);
                                    }
                                    else
                                        Session.SendWhisper("Necesitas " + Session.GetRoleplay().MecPartsTo + " repuestos para reparar este vehículo.", 1);
                                    #endregion
                                    #endregion
                                }
                                else
                                {
                                    #region Armero
                                    #region Conditions Arm
                                    /*if (!Target.GetRoleplay().PediArm)
                                    {
                                        Session.SendWhisper("Esa persona no ha solicitado los servicios de un Armero.", 1);
                                        return;
                                    }*/
                                    foreach (var Offer in Target.GetRoleplay().OfferManager.ActiveOffers.Values)
                                    {
                                        if (Offer.Type.ToLower() == "fix_armero")
                                        {
                                            Session.SendWhisper("Esa persona ya tiene una reparación de arma pendiente.", 1);
                                            return;
                                        }
                                    }
                                    if (Target.GetRoleplay().EquippedWeapon == null)
                                    {
                                        Session.SendWhisper("Esa persona no lleva ningún arma Equipada a ser reparada.", 1);
                                        return;
                                    }
                                    if (Target.GetRoleplay().WLife > 0)
                                    {
                                        Session.SendWhisper("Esa arma no necesita una reparación.", 1);
                                        return;
                                    }
                                    if (Session.GetRoleplay().ArmPiecesTo <= 0 || Session.GetRoleplay().ArmUserTo != Target.GetHabbo().Id)
                                    {
                                        Session.SendWhisper("Primero debes :revisar el arma de " + Target.GetHabbo().Username, 1);
                                        return;
                                    }
                                    if (Session.GetRoleplay().ArmPieces < Session.GetRoleplay().ArmPiecesTo)
                                    {
                                        Session.SendWhisper("Necesitas " + Session.GetRoleplay().ArmPiecesTo + " pieza(s) para reparar esa arma.", 1);
                                        return;
                                    }
                                    #endregion

                                    #region Execute
                                    RoleplayManager.Shout(Session, "*Ofrece una reparación de Arma a " + Target.GetHabbo().Username + " por $" + String.Format("{0:N0}", Price) + "*", 5);
                                    Target.GetRoleplay().OfferManager.CreateOffer("fix_armero", Session.GetHabbo().Id, Price);
                                    Target.SendWhisper("Te han ofrecido una reparación de Arma por $" + String.Format("{0:N0}", Price) + ". Escribe ':aceptar reparacion' para aceptarla o en su defecto ':rechazar reparacion'.", 1);
                                    Session.GetRoleplay().CooldownManager.CreateCooldown("comprar", 1000, 3);
                                    #endregion
                                    #endregion
                                }
                            }
                            else
                                Session.SendWhisper("El precio es inválido", 1);
                        }
                        break;
                    #endregion

                    #region Mamada
                    case "mamada":
                        {
                            // Verificar que Params[2] existe
                            if (Params.Length < 3)
                            {
                                Session.SendWhisper("Debes especificar un precio.", 1);
                                return;
                            }

                            if (int.TryParse(Params[2], out Price))
                            {
                                #region Conditions Price
                                if (Price < 0 || Price == 0 || Price <= 0)
                                {
                                    Session.SendWhisper("El precio no puede ser negativo.", 1);
                                    return;
                                }
                                #endregion

                                if (Session.GetHabbo().CurrentRoomId != 16)
                                {
                                    Session.SendWhisper("¡No estas en la sala del prostibulo!", 6);
                                    return;
                                }

                                /* if (Session.GetRoleplay().JobId != 15)
                                 {
                                     Session.SendWhisper("¡No Perteneces al trabajo del prostibulo!", 6);
                                     return;
                                 }
                                 */
                                if (!GroupManager.HasJobCommand(Session, "mamada") && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                                {
                                    Session.SendWhisper("Usted no trabaja de prostitut@", 1);
                                    break;
                                }

                                if (!Session.GetRoleplay().IsWorking && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                                {
                                    Session.SendWhisper("¡Usted debe estar trabajando para ofrecer a alguien una mamada!", 1);
                                    break;
                                }
                                if (Target == Session)
                                {
                                    Session.SendWhisper("¡No puedes pincharte a ti mismo, pidele apoyo a un compañero de trabajo!", 1);
                                    return;
                                }

                                else
                                {
                                    bool HasOffer = false;
                                    if (Target.GetHabbo().Credits >= Convert.ToInt32(Price.ToString().Replace("-", "")))
                                    {
                                        foreach (var Offer in Target.GetRoleplay().OfferManager.ActiveOffers.Values)
                                        {
                                            if (Offer.Type.ToLower() == Type.ToLower())
                                            {
                                                HasOffer = true;
                                            }
                                        }
                                        if (!HasOffer)
                                        {
                                            Session.Shout("*Ofrece una rica mamada a " + Target.GetHabbo().Username + " por $" + String.Format("{0:N0}", Convert.ToInt32(Price.ToString().Replace("-", ""))) + "*", 4);
                                            Target.GetRoleplay().OfferManager.CreateOffer("mamada", Session.GetHabbo().Id, Convert.ToInt32(Price.ToString().Replace("-", "")));
                                            Target.SendWhisper("Recuerda que te aumenta [50+ FELICIDAD] $" + String.Format("{0:N0}", Convert.ToInt32(Price.ToString().Replace("-", ""))) + " Diga ':aceptar mamada' para que te la chupen!", 1);
                                            break;
                                        }
                                        else
                                        {
                                            Session.SendWhisper("A este usuario ya se le ha ofrecido una mamada", 1);
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        Session.SendWhisper("Este ciudadano no puede pagar una mamada", 1);
                                        break;
                                    }
                                }

                            }
                            else
                                Session.SendWhisper("El precio es inválido", 1);
                        }
                        break;
                    #endregion

                    
                   #region Phone
                    case "telefono":
                    {

                            if (!GroupManager.HasJobCommand(Session, "telefono") && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                            {
                                Session.SendWhisper("Usted no trabaja en el teléfono corporation!", 1);
                                break;
                            }

                            if (Target.GetRoleplay().Phone > 0)
                            {
                                string WhisperMessage = "Usted ya tiene teléfono";
                                Session.SendWhisper("Este ciudadano ya tiene un teléfono", 1);
                                break;
                            }

                            if (!Session.GetRoleplay().IsWorking && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                            {
                                Session.SendWhisper("Usted debe estar trabajando para ofrecer a alguien a teléfono", 1);
                                break;
                            }

                            else
                            {
                                Phone phone = PhoneManager.getPhone("iphone");
                                Params[2] = Convert.ToString(phone.Price);
                                if (int.TryParse(Params[2], out Price))
                                {
                                    #region Conditions Price
                                    if (Price < 0 || Price == 0 || Price <= 0)
                                    {
                                        Session.SendWhisper("El precio no puede ser negativo.", 1);
                                        return;
                                    }
                                    #endregion
                                bool HasOffer = false;
                                if (Target.GetHabbo().Credits >= Convert.ToInt32(Price.ToString().Replace("-", "")))
                                {
                                    foreach (var Offer in Target.GetRoleplay().OfferManager.ActiveOffers.Values)
                                    {
                                        if (Offer.Type.ToLower() == Type.ToLower())
                                        {
                                            HasOffer = true;
                                        }
                                    }
                                    if (!HasOffer)
                                    {
                                        Session.Shout("*Ofrece un iPhone a " + Target.GetHabbo().Username + " por $"+ Price +"!*", 4);
                                        Target.GetRoleplay().OfferManager.CreateOffer("telefono", Session.GetHabbo().Id, Convert.ToInt32(Price.ToString().Replace("-", "")));
                                        Target.SendWhisper("¡Recién te han ofrecido un iPhone por $"+ Price +"! Diga ':aceptar telefono' para comprarlo!", 1);
                                        break;
                                    }
                                    else
                                    {
                                        Session.SendWhisper("A este usuario ya se le ha ofrecido un teléfono", 1);
                                        break;
                                    }
                                }
                                else
                                {
                                    Session.SendWhisper("Este ciudadano no puede teléfono", 1);
                                    break;
                                }
                                }
                                else
                                    Session.SendWhisper("El precio es inválido", 1);
                            }
                            break;
                        }
                    #endregion

                    #region Default
                    default:
                        {
                            Session.SendWhisper("'" + Type + "' no es una oferta válida.", 1);
                            break;
                        }
                        #endregion
                }
                #endregion
            }
            else if (Dir == 2)
            {
                #region Consumibles :vender [x] [objetos] [cantidad] [precio]
                // Verificar que tenemos todos los parámetros necesarios
                if (Params.Length < 5)
                {
                    Session.SendWhisper("Comando inválido, escribe :vender [usuario] [objeto] [cantidad] [precio]", 1);
                    return;
                }

                string Type = Params2;
                int Cant = 0;
                int Cost = 0;

                #region Conditions
                if (!int.TryParse(Params[3], out Cant))
                {
                    Session.SendWhisper("La cantidad no es válida.", 1);
                    return;
                }
                if (!int.TryParse(Params[4], out Cost))
                {
                    Session.SendWhisper("El precio no es válido.", 1);
                    return;
                }
                if (Cant <= 0)
                {
                    Session.SendWhisper("La cantidad debe ser mayor a 0.", 1);
                    return;
                }
                if (Cost < 0)
                {
                    Session.SendWhisper("El precio no puede ser negativo.", 1);
                    return;
                }
                #endregion

                switch (Type.ToLower())
                {
                    #region Medicamentos
                    case "medicamentos":
                    case "medicamento":
                        {
                            if (Session.GetRoleplay().Medicina < Cant)
                            {
                                Session.SendWhisper("No tienes " + Cant + " medicamento(s) para vender.", 1);
                                return;
                            }

                            /*if (Session.GetRoleplay().PassiveMode)
                            {
                                Session.SendWhisper("No puedes hacer eso mientras estás en modo pasivo.", 1);
                                return;
                            }
                            if (Target.GetRoleplay().PassiveMode)
                            {
                                Session.SendWhisper("No puedes venderle eso a una persona en modo pasivo.", 1);
                                return;
                            }*/

                            #region Offer Conditions
                            foreach (var Offer in Target.GetRoleplay().OfferManager.ActiveOffers.Values)
                            {
                                if (Offer.Type.ToLower() == "medicamentos")
                                {
                                    Session.SendWhisper("Esa persona ya tiene una oferta de medicamentos pendiente.", 1);
                                    return;
                                }
                            }
                            #endregion

                            #region Execute
                            RoleplayManager.Shout(Session, "*Ofrece " + Cant + " medicamento(s) a " + Target.GetHabbo().Username + " por $" + String.Format("{0:N0}", Cost) + "*", 5);
                            Target.GetRoleplay().OfferManager.CreateOffer("medicamentos", Session.GetHabbo().Id, Cost, Cant);
                            Target.SendWhisper("Te han ofrecido " + Cant + " medicamento(s) por $" + String.Format("{0:N0}", Cost) + ". Escribe ':aceptar medicamentos' para aceptarlos en su defecto ':rechazar medicamentos'.", 1);
                            Session.GetRoleplay().CooldownManager.CreateCooldown("comprar", 1000, 3);
                            #endregion
                        }
                        break;
                    #endregion

                    #region Crack
                    case "crackx":
                        {
                            Group Gang = GroupManager.GetGang(Session.GetRoleplay().GangId);
                            Group Gang2 = GroupManager.GetGang(Room.Group.Id);
                            if (Gang == null)
                            {
                                Session.SendWhisper("¡Tienes que pertenecer a una pandilla para vender crack!.", 1);
                                return;
                            }
                            List<Turf> TF = PolarEnvironment.GetGame().GetGangTurfsManager().getTurfsbyGang(Gang.Id);
                            if (TF == null)
                            {
                                Session.SendWhisper("¡Debes tener un territorio capturado para vender crack!.", 1);
                                return;
                            }
                            if (Session.GetRoleplay().Cocaine < Cant)
                            {
                                Session.SendWhisper("No tienes " + Cant + " g. de crack para vender.", 1);
                                return;
                            }

                            /*if (Session.GetRoleplay().PassiveMode)
                            {
                                Session.SendWhisper("No puedes hacer eso mientras estás en modo pasivo.", 1);
                                return;
                            }
                            if (Target.GetRoleplay().PassiveMode)
                            {
                                Session.SendWhisper("No puedes venderle eso a una persona en modo pasivo.", 1);
                                return;
                            }*/

                            #region Offer Conditions
                            foreach (var Offer in Target.GetRoleplay().OfferManager.ActiveOffers.Values)
                            {
                                if (Offer.Type.ToLower() == "crack")
                                {
                                    Session.SendWhisper("Esa persona ya tiene una oferta de crack pendiente.", 1);
                                    return;
                                }
                            }
                            #endregion

                            #region Execute
                            RoleplayManager.Shout(Session, "*Ofrece " + Cant + " g. de crack a " + Target.GetHabbo().Username + " por $" + String.Format("{0:N0}", Cost) + "*", 5);
                            Target.GetRoleplay().OfferManager.CreateOffer("crack", Session.GetHabbo().Id, Cost, Cant);
                            Target.SendWhisper("Te han ofrecido " + Cant + " g. de crack por $" + String.Format("{0:N0}", Cost) + ". Escribe ':aceptar crack' para aceptarlos en su defecto ':rechazar crack'.", 1);
                            Session.GetRoleplay().CooldownManager.CreateCooldown("comprar", 1000, 3);

                            // Alertamos a los integrantes de la banda atacada
                            foreach (GameClient client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                            {
                                if (client == null || client.GetHabbo() == null)
                                    continue;

                                Group thegroup = GroupManager.GetGang(Room.Group.Id);

                                if (thegroup == null)
                                    continue;

                                if (thegroup != Room.Group)
                                    continue;

                                if (client.GetRoleplay().DisableRadio == true)
                                    continue;

                                client.SendWhisper("[ATENCIÓN] ¡Están vendiendo drogas en " + Room.Name + "! ¡Vamos a detener esa venta!", 30);
                            }
                            #endregion
                        }
                        break;
                    #endregion

                    #region Bullets

                    case "bullets":
                    case "ammo":
                    case "balas":
                        {
                            if (!GroupManager.HasJobCommand(Session, "weapon") && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                            {
                                Session.SendWhisper("Usted no trabaja en la tienda de armas", 1);
                                break;
                            }

                            if (Params.Length == 3)
                            {
                                Session.SendWhisper("Por favor ingrese la cantidad de puntos que le gustaría ofrecer al ciudadano!", 1);
                                return;
                            }

                            int Amount;
                            if (!int.TryParse(Params[3], out Amount))
                            {
                                Session.SendWhisper("¡Ingrese una cantidad válida de balas que le gustaría ofrecer al ciudadano!", 1);
                                break;
                            }

                            if (Amount < 10)
                            {
                                Session.SendWhisper("Usted necesita ofrecer al ciudadano al menos 10 balas!", 1);
                                break;
                            }

                            if (!Session.GetRoleplay().IsWorking && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                            {
                                Session.SendWhisper("Usted debe estar trabajando para ofrecer a alguien balas!", 1);
                                break;
                            }

                            else
                            {
                                Cost = Convert.ToInt32(Math.Floor((double)Amount / 2));
                                bool HasOffer = false;

                                if (Target.GetHabbo().Credits >= Convert.ToInt32(Cost.ToString().Replace("-", "")))
                                {
                                    foreach (var Offer in Target.GetRoleplay().OfferManager.ActiveOffers.Values)
                                    {
                                        if (Offer.Type.ToLower() == "balas")
                                        {
                                            HasOffer = true;
                                        }
                                    }
                                    if (!HasOffer)
                                    {
                                        Session.Shout("*Ofrece " + String.Format("{0:N0}", Amount) + " balas a " + Target.GetHabbo().Username + " por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + "*", 4);
                                        Target.GetRoleplay().OfferManager.CreateOffer("balas", Session.GetHabbo().Id, Amount);
                                        Target.SendWhisper("Acaba de ofrecerte " + String.Format("{0:N0}", Amount) + " balas por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + "! Diga ':aceptar balas' para comprarlo!", 1);
                                        break;
                                    }
                                    else
                                    {
                                        Session.SendWhisper("Este usuario ya se le ha ofrecido balas", 1);
                                        break;
                                    }
                                }
                                else
                                {
                                    Session.SendWhisper("No puede pagar por las balas", 1);
                                    break;
                                }
                            }
                        }

                    #endregion

                    #region Seeds
                    case "seed":
                    case "seeds":
                    case "semillas":
                        {
                            if (!GroupManager.HasJobCommand(Session, "supermarket") && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                            {
                                Session.SendWhisper("Usted no trabaja en el supermarket corporation!", 1);
                                break;
                            }

                            if (Params.Length < 5)
                            {
                                Session.SendWhisper("El comando es: 'ofrecer <user> semillas <id> <amount>'!", 1);
                                return;
                            }

                            int Id;
                            if (!int.TryParse(Params[3], out Id))
                            {
                                Session.SendWhisper("Por favor, ingrese un ID de semilla válido para ofrecer al ciudadano!", 1);
                                return;
                            }

                            FarmingItem Item = FarmingManager.GetFarmingItem(Id);

                            if (Item == null)
                            {
                                Session.SendWhisper("Lo sentimos, pero no se pudo encontrar este ID de semilla!", 1);
                                return;
                            }

                            ItemData Furni;
                            if (!PolarEnvironment.GetGame().GetItemManager().GetItem(Item.BaseItem, out Furni))
                            {
                                Session.SendWhisper("Lo sentimos, pero esta semilla no se pudo encontrar!", 1);
                                return;
                            }

                            int Amount;
                            if (!int.TryParse(Params[4], out Amount))
                            {
                                Session.SendWhisper("Ingrese una cantidad válida de seeds you would like offer the citizen!", 1);
                                break;
                            }

                            if (!Target.GetRoleplay().FarmingStats.HasSeedSatchel)
                            {
                                Session.SendWhisper("Lo siento, este ciudadano no tiene semillas!", 1);
                                break;
                            }

                            Cost = (Amount * Item.BuyPrice);

                            if (Item.LevelRequired > Target.GetRoleplay().FarmingStats.Level)
                            {
                                Session.SendWhisper("Lo sentimos, pero este ciudadano no tiene un nivel de cultivo lo suficientemente alto para esta semilla!", 1);
                                return;
                            }

                            if (!Session.GetRoleplay().IsWorking && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                            {
                                Session.SendWhisper("Usted debe estar trabajando para ofrecer a alguien semillas", 1);
                                break;
                            }
                            else
                            {
                                if (Target.GetHabbo().Credits >= Convert.ToInt32(Cost.ToString().Replace("-", "")))
                                {
                                    if (Target.GetRoleplay().OfferManager.ActiveOffers.Values.Where(x => x.Type.ToLower() == "semillas").ToList().Count <= 0)
                                    {
                                        Session.Shout("*Ofrece " + String.Format("{0:N0}", Amount) + " " + Furni.PublicName + " semillas a " + Target.GetHabbo().Username + " por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + "*", 4);
                                        Target.GetRoleplay().OfferManager.CreateOffer("semillas", Session.GetHabbo().Id, Amount, Item);
                                        Target.SendWhisper("Acaba de ofrecerte " + String.Format("{0:N0}", Amount) + " " + Furni.PublicName + " semillas por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + "! Diga ':aceptar semillas' para comprarlo!", 1);
                                        break;
                                    }
                                    else
                                    {
                                        Session.SendWhisper("Este usuario ya se ha ofrecido algunas semillas!", 1);
                                        break;
                                    }
                                }
                                else
                                {
                                    Session.SendWhisper("No puede pagar estas semillas", 1);
                                    break;
                                }
                            }
                        }

                    #endregion

                    #region Seed Satchel
                    case "seedsatchel":
                    case "bolsasemillas":
                        {
                            if (!GroupManager.HasJobCommand(Session, "supermarket") && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                            {
                                Session.SendWhisper("Usted no trabaja en el supermarket corporation!", 1);
                                break;
                            }

                            if (Target.GetRoleplay().FarmingStats.HasSeedSatchel)
                            {
                                Session.SendWhisper("Este ciudadano ya tiene una bolsa de semillas", 1);
                                break;
                            }

                            if (!Session.GetRoleplay().IsWorking && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                            {
                                Session.SendWhisper("Usted debe estar trabajando para ofrecer a alguien una bolsa de semillas", 1);
                                break;
                            }
                            else
                            {
                                Cost = Convert.ToInt32(RoleplayData.GetData("farming", "seedsatchelcost"));

                                bool HasOffer = false;
                                if (Target.GetHabbo().Credits >= Convert.ToInt32(Cost.ToString().Replace("-", "")))
                                {
                                    foreach (var Offer in Target.GetRoleplay().OfferManager.ActiveOffers.Values)
                                    {
                                        if (Offer.Type.ToLower() == Type.ToLower())
                                        {
                                            HasOffer = true;
                                        }
                                    }
                                    if (!HasOffer)
                                    {
                                        Session.Shout("*Ofrece una bolsa de semillas " + Target.GetHabbo().Username + " por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + "*", 4);
                                        Target.GetRoleplay().OfferManager.CreateOffer("bolsasemillas", Session.GetHabbo().Id, Convert.ToInt32(Cost.ToString().Replace("-", "")));
                                        Target.SendWhisper("Acaba de ofrecerte una bolsa de semillas por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + "! diga ':aceptar bolsasemillas' para comprarlo!", 1);
                                        break;
                                    }
                                    else
                                    {
                                        Session.SendWhisper("A este usuario ya se le ha ofrecido una bolsa de semillas!", 1);
                                        break;
                                    }
                                }
                                else
                                {
                                    Session.SendWhisper("Este ciudadano no puede pagar una bolsa de semillas!", 1);
                                    break;
                                }
                            }
                        }
                    #endregion

                    #region Plant Satchel
                    case "plantsatchel":
                    case "bolsoavegetal":
                        {
                            if (!GroupManager.HasJobCommand(Session, "supermarket") && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                            {
                                Session.SendWhisper("Usted no trabaja en el supermarket corporation!", 1);
                                break;
                            }

                            if (Target.GetRoleplay().FarmingStats.HasPlantSatchel)
                            {
                                Session.SendWhisper("Este ciudadano ya tiene un bolso vegetal!", 1);
                                break;
                            }

                            if (!Session.GetRoleplay().IsWorking && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                            {
                                Session.SendWhisper("Usted debe estar trabajando para ofrecer a alguien un bolso vegetal!", 1);
                                break;
                            }
                            else
                            {
                                Cost = Convert.ToInt32(RoleplayData.GetData("farming", "plantsatchelcost"));

                                bool HasOffer = false;
                                if (Target.GetHabbo().Credits >= Convert.ToInt32(Cost.ToString().Replace("-", "")))
                                {
                                    foreach (var Offer in Target.GetRoleplay().OfferManager.ActiveOffers.Values)
                                    {
                                        if (Offer.Type.ToLower() == Type.ToLower())
                                        {
                                            HasOffer = true;
                                        }
                                    }
                                    if (!HasOffer)
                                    {
                                        Session.Shout("*Ofrece un bolso vegetal a " + Target.GetHabbo().Username + " por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + "*", 4);
                                        Target.GetRoleplay().OfferManager.CreateOffer("plantsatchel", Session.GetHabbo().Id, Convert.ToInt32(Cost.ToString().Replace("-", "")));
                                        Target.SendWhisper("Acaba de ofrecerte un bolso vegetal por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + "! Diga ':aceptar bolsavegetal' para comprarlo!", 1);
                                        break;
                                    }
                                    else
                                    {
                                        Session.SendWhisper("A este usuario ya se le ha ofrecido un Plant Satchel!", 1);
                                        break;
                                    }
                                }
                                else
                                {
                                    Session.SendWhisper("Este ciudadano no puede Plant Satchel!", 1);
                                    break;
                                }
                            }
                        }
                    #endregion

                    #region Cigarrettes
                    case "cigarrillos":
                    case "cigarros":
                        {
                            if (!GroupManager.HasJobCommand(Session, "cigarrillos") && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                            {
                                Session.SendWhisper("Usted no trabaja en la farmacia", 1);
                                break;
                            }

                            if (!Session.GetRoleplay().IsWorking && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                            {
                                Session.SendWhisper("Usted debe estar trabajando para ofrecer a alguien un cigarrillo!", 1);
                                break;
                            }

                            else
                            {
                                Cost = 350;
                                bool HasOffer = false;
                                if (Target.GetHabbo().Credits >= Convert.ToInt32(Cost.ToString().Replace("-", "")))
                                {
                                    foreach (var Offer in Target.GetRoleplay().OfferManager.ActiveOffers.Values)
                                    {
                                        if (Offer.Type.ToLower() == Type.ToLower())
                                        {
                                            HasOffer = true;
                                        }
                                    }
                                    if (!HasOffer)
                                    {
                                        Session.Shout("*Ofrece una caja de cigarrillos a " + Target.GetHabbo().Username + " por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + "*", 4);
                                        Target.GetRoleplay().OfferManager.CreateOffer("cigarrillos", Session.GetHabbo().Id, Convert.ToInt32(Cost.ToString().Replace("-", "")));
                                        Target.SendWhisper("Acaba de ofrecerte una caja de cigarrillos por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + " Diga ':aceptar cigarrillos' para comprarlo!", 1);
                                        break;
                                    }
                                    else
                                    {
                                        Session.SendWhisper("A este usuario ya se le ha ofrecido un cigarrilos", 1);
                                        break;
                                    }
                                }
                                else
                                {
                                    Session.SendWhisper("Este ciudadano no puede pagar un caja de cigarrillos", 1);
                                    break;
                                }
                            }
                        }
                    #endregion

                    #region Cocaina
                    case "cocaina":
                    case "crack":
                        {
                            Group Gang = GroupManager.GetGang(Session.GetRoleplay().GangId);
                            Group Job = GroupManager.GetJob(Room.GroupId);
                            if (Gang == null)
                            {
                                Session.SendWhisper("¡Tienes que pertenecer a una pandilla para vender cocaina!.", 1);
                                return;
                            }
                            List<Turf> TF = PolarEnvironment.GetGame().GetGangTurfsManager().getTurfsbyGang(Gang.Id);
                            if (TF == null)
                            {
                                Session.SendWhisper("¡Debes tener un territorio capturado para vender cocaina!.", 1);
                                return;
                            }
                            if (Session.GetRoleplay().Cocaine < 10)
                            {
                                Session.SendWhisper("Necesitas tener por lo menos 10g de cocaina para vender", 1);
                                return;
                            }
                            if (Session.GetRoleplay().PassiveMode)
                            {
                                Session.SendWhisper("No puedes hacer eso mientras estás en modo pasivo.", 1);
                            }
                            if (Target.GetRoleplay().PassiveMode)
                            {
                                Session.SendWhisper("No puedes venderle eso a una persona en modo pasivo.", 1);
                            }
                            /*
                            else
                            {*/
                            //Cost = Convert.ToInt32(Params[4]);
                            bool HasOffer = false;
                            if (Target.GetHabbo().Credits >= Convert.ToInt32(Cost.ToString().Replace("-", "")))
                            {
                                foreach (var Offer in Target.GetRoleplay().OfferManager.ActiveOffers.Values)
                                {
                                    if (Offer.Type.ToLower() == Type.ToLower())
                                    {
                                        HasOffer = true;
                                    }
                                }
                                if (!HasOffer)
                                {
                                    Session.Shout("*Ofrece " + Cant + " gramos de cocaina a " + Target.GetHabbo().Username + " por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + "*", 4);
                                    Target.GetRoleplay().OfferManager.CreateOffer("cocaina", Session.GetHabbo().Id, Convert.ToInt32(Cost.ToString().Replace("-", "")), Cant);
                                    Target.SendWhisper("Acaba de ofrecerte " + Cant + "g de cocaina por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + " Diga ':aceptar cocaina' para comprarlo!", 1);

                                    if (Room.GroupId == 0 && Room.OwnerId == 1)
                                        hasWanted = false;
                                    else if (Room.GroupId < 1000 && Room.OwnerId == 1)
                                        hasWanted = true;

                                    if (hasWanted)
                                    {
                                        if (RoleplayManager.WantedList.ContainsKey(Session.GetHabbo().Id))
                                        {

                                            Session.GetRoleplay().IsWanted = true;
                                            Session.GetRoleplay().WantedLevel = 5;
                                            Session.GetRoleplay().WantedTimeLeft = 20;

                                            Session.GetRoleplay().TimerManager.CreateTimer("wanted", 1000, false);
                                            RoleplayManager.WantedList.TryUpdate(Session.GetHabbo().Id, NewWanted, RoleplayManager.WantedList[Session.GetHabbo().Id]);
                                            Session.SendWhisper("¡Actualizaciones de " + Session.GetHabbo().Username + "'s Nivel de búsqueda 5 estrella(s)!", 34);
                                            //PolarEnvironment.GetGame().GetClientManager().JailAlert("[RADIO Alert] " + TargetClient.GetHabbo().Username + "'s Wanted Level se ha actualizado desde " + CurrentWantedLevel + " Star(s) to " + WantedLevel + " Star(s)!");
                                            // WS Wanted Stars
                                            if (Session.GetRoleplay().WebSocketConnection != null)
                                                PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(Session, "compose_wanted_stars|" + Session.GetRoleplay().WantedLevel);
                                            return;

                                        }
                                        else
                                        {
                                            Session.GetRoleplay().IsWanted = true;
                                            Session.GetRoleplay().WantedLevel = 5;
                                            Session.GetRoleplay().WantedTimeLeft = 20;

                                            Session.GetRoleplay().TimerManager.CreateTimer("wanted", 1000, false);
                                            RoleplayManager.WantedList.TryAdd(Session.GetHabbo().Id, NewWanted);
                                            Session.SendWhisper("¡Se ha agregado a " + Session.GetHabbo().Username + " a la lista de Buscados con un Nivel de 5 estrellas(s)!", 34);
                                            //PolarEnvironment.GetGame().GetClientManager().JailAlert("[RADIO Alert] " + TargetClient.GetHabbo().Username + "'s Wanted Level se ha actualizado desde " + CurrentWantedLevel + " Star(s) to " + WantedLevel + " Star(s)!");
                                            // WS Wanted Stars
                                            if (Session.GetRoleplay().WebSocketConnection != null)
                                                PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(Session, "compose_wanted_stars|" + Session.GetRoleplay().WantedLevel);
                                            return;
                                        }
                                    }

                                    if (Gang != null)
                                    {
                                        // Alertamos a los integrantes de la banda atacada
                                        lock (PolarEnvironment.GetGame().GetClientManager().GetClients)
                                        {
                                            foreach (GameClient client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                                            {
                                                if (client == null || client.GetHabbo() == null)
                                                    continue;

                                                List<Groups.Group> thegroup = PolarEnvironment.GetGame().GetGroupManager().GetGangsForUser(client.GetHabbo().Id);

                                                if (thegroup == null || thegroup.Count <= 0)
                                                    continue;

                                                if (thegroup[0] != Room.Group)
                                                    continue;

                                                if (client.GetRoleplay().DisableRadio == true)
                                                    continue;


                                                client.SendWhisper("[ATENCIÓN] ¡Están vendiendo drogas en " + Room.Name + "! ¡Vamos a detener esa venta!", 30);
                                            }
                                        }
                                    }
                                    break;
                                }
                                else
                                {
                                    Session.SendWhisper("A este usuario ya se le ha ofrecido un cocaina", 1);
                                    break;
                                }
                            }
                            else
                            {
                                Session.SendWhisper("Este ciudadano no puede pagar la cocaina", 1);
                                break;
                            }
                            // }
                        }
                    #endregion

                    #region Caramelos
                    case "caramelos":
                        {
                            if (Session.GetRoleplay().Caramelos < 5)
                            {
                                Session.SendWhisper("Necesitas tener 5 caramelos disponibles para vender", 1);
                                return;
                            }
                            if (!GroupManager.HasGangCommand(Session, "caramelos") && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                            {
                                Session.SendWhisper("Necesitas ser vendedor de Caramelos", 1);
                                return;
                            }

                            else
                            {
                                //Cost = 2000;
                                bool HasOffer = false;
                                if (Target.GetHabbo().Credits >= Convert.ToInt32(Cost.ToString().Replace("-", "")))
                                {
                                    foreach (var Offer in Target.GetRoleplay().OfferManager.ActiveOffers.Values)
                                    {
                                        if (Offer.Type.ToLower() == Type.ToLower())
                                        {
                                            HasOffer = true;
                                        }
                                    }
                                    if (!HasOffer)
                                    {
                                        Session.Shout("*Ofrece " + Cant + " unidades de caramelos a " + Target.GetHabbo().Username + " por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + "*", 4);
                                        Target.GetRoleplay().OfferManager.CreateOffer("caramelos", Session.GetHabbo().Id, Convert.ToInt32(Cost.ToString().Replace("-", "")), Cant);
                                        Target.SendWhisper("Acaba de ofrecerte 5 caramelos por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + " Diga ':aceptar caramelos' para comprarlo!", 1);
                                        break;
                                    }
                                    else
                                    {
                                        Session.SendWhisper("A este usuario ya se le ha ofrecido un caramelos", 1);
                                        break;
                                    }
                                }
                                else
                                {
                                    Session.SendWhisper("Este ciudadano no puede pagar un caja de caramelos", 1);
                                    break;
                                }
                            }
                        }
                    #endregion

                    #region Medicina
                    case "medicina":
                        {
                            if (!GroupManager.HasJobCommand(Session, "medicina"))
                            {
                                Session.SendWhisper("Necesitas ser farmaceutico para vender medicinas", 1);
                                return;
                            }
                            else
                            {
                                Cost = 10000;
                                bool HasOffer = false;
                                if (Target.GetHabbo().Credits >= Convert.ToInt32(Cost.ToString().Replace("-", "")))
                                {
                                    foreach (var Offer in Target.GetRoleplay().OfferManager.ActiveOffers.Values)
                                    {
                                        if (Offer.Type.ToLower() == Type.ToLower())
                                        {
                                            HasOffer = true;
                                        }
                                    }
                                    if (!HasOffer)
                                    {
                                        Session.Shout("*Ofrece 50 Cc de medicinas a " + Target.GetHabbo().Username + " por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + "*", 4);
                                        Target.GetRoleplay().OfferManager.CreateOffer("medicina", Session.GetHabbo().Id, Convert.ToInt32(Cost.ToString().Replace("-", "")));
                                        Target.SendWhisper("Acaba de ofrecerte 50Cc de medicina por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + " Diga ':aceptar medicina' para comprarlo!", 1);
                                        break;
                                    }
                                    else
                                    {
                                        Session.SendWhisper("A este usuario ya se le ha ofrecido una medicina", 1);
                                        break;
                                    }
                                }
                                else
                                {
                                    Session.SendWhisper("Este ciudadano no puede pagar un caja de medicina", 1);
                                    break;
                                }
                            }
                        }
                    #endregion

                    #region Marihuana
                    case "marihuana":
                        {
                            if (Session.GetRoleplay().PassiveMode)
                            {
                                Session.SendWhisper("No puedes hacer eso mientras estás en modo pasivo.", 1);
                            }
                            if (Target.GetRoleplay().PassiveMode)
                            {
                                Session.SendWhisper("No puedes venderle eso a una persona en modo pasivo.", 1);
                            }

                            Group Gang = GroupManager.GetGang(Session.GetRoleplay().GangId);
                            Group Job = GroupManager.GetJob(Room.GroupId);
                            if (Gang == null)
                            {
                                Session.SendWhisper("¡Tienes que pertenecer a una pandilla para vender marihuana!.", 1);
                                return;
                            }
                            List<Turf> TF = PolarEnvironment.GetGame().GetGangTurfsManager().getTurfsbyGang(Gang.Id);
                            if (TF == null)
                            {
                                Session.SendWhisper("¡Debes tener un territorio capturado para vender marihuana!.", 1);
                                return;
                            }
                            /*if (!GroupManager.HasGangCommand(Session, "marihuana"))
                            {
                                Session.SendWhisper("Necesitas ser traficante de drogas de una pandilla para venderla", 1);
                                return;
                            }*/

                            if (Session.GetRoleplay().Weed < 10)
                            {
                                Session.SendWhisper("¡Necesitas tener por lo menos 10 porros para vender!");
                                return;
                            }

                            else
                            {
                                bool HasOffer = false;
                                if (Target.GetHabbo().Credits >= Convert.ToInt32(Cost.ToString().Replace("-", "")))
                                {
                                    foreach (var Offer in Target.GetRoleplay().OfferManager.ActiveOffers.Values)
                                    {
                                        if (Offer.Type.ToLower() == Type.ToLower())
                                        {
                                            HasOffer = true;
                                        }
                                    }
                                    if (!HasOffer)
                                    {
                                        Session.Shout("*Ofrece " + Cant + " porros de marihuana a " + Target.GetHabbo().Username + " por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + "*", 4);
                                        Target.GetRoleplay().OfferManager.CreateOffer("marihuana", Session.GetHabbo().Id, Convert.ToInt32(Cost.ToString().Replace("-", "")), Cant);
                                        Target.SendWhisper("Acaba de ofrecerte " + Cant + " porros de marihuana por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + " Diga ':aceptar marihuana' para comprarlo!", 1);

                                        if (Room.GroupId == 0 && Room.OwnerId == 1)
                                            hasWanted = false;
                                        else if (Room.GroupId < 1000 && Room.OwnerId == 1)
                                            hasWanted = true;

                                        if (hasWanted)
                                        {
                                            if (RoleplayManager.WantedList.ContainsKey(Session.GetHabbo().Id))
                                            {

                                                Session.GetRoleplay().IsWanted = true;
                                                Session.GetRoleplay().WantedLevel = 5;
                                                Session.GetRoleplay().WantedTimeLeft = 20;

                                                Session.GetRoleplay().TimerManager.CreateTimer("wanted", 1000, false);
                                                RoleplayManager.WantedList.TryUpdate(Session.GetHabbo().Id, NewWanted, RoleplayManager.WantedList[Session.GetHabbo().Id]);
                                                Session.SendWhisper("¡Actualizaciones de " + Session.GetHabbo().Username + "'s Nivel de búsqueda 5 estrella(s)!", 34);
                                                //PolarEnvironment.GetGame().GetClientManager().JailAlert("[RADIO Alert] " + TargetClient.GetHabbo().Username + "'s Wanted Level se ha actualizado desde " + CurrentWantedLevel + " Star(s) to " + WantedLevel + " Star(s)!");
                                                // WS Wanted Stars
                                                if (Session.GetRoleplay().WebSocketConnection != null)
                                                    PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(Session, "compose_wanted_stars|" + Session.GetRoleplay().WantedLevel);
                                                return;

                                            }
                                            else
                                            {
                                                Session.GetRoleplay().IsWanted = true;
                                                Session.GetRoleplay().WantedLevel = 5;
                                                Session.GetRoleplay().WantedTimeLeft = 20;

                                                Session.GetRoleplay().TimerManager.CreateTimer("wanted", 1000, false);
                                                RoleplayManager.WantedList.TryAdd(Session.GetHabbo().Id, NewWanted);
                                                Session.SendWhisper("¡Se ha agregado a " + Session.GetHabbo().Username + " a la lista de Buscados con un Nivel de 5 estrellas(s)!", 34);
                                                //PolarEnvironment.GetGame().GetClientManager().JailAlert("[RADIO Alert] " + TargetClient.GetHabbo().Username + "'s Wanted Level se ha actualizado desde " + CurrentWantedLevel + " Star(s) to " + WantedLevel + " Star(s)!");
                                                // WS Wanted Stars
                                                if (Session.GetRoleplay().WebSocketConnection != null)
                                                    PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(Session, "compose_wanted_stars|" + Session.GetRoleplay().WantedLevel);
                                                return;
                                            }
                                        }

                                        if (Gang != null)
                                        {
                                            // Alertamos a los integrantes de la banda atacada
                                            foreach (GameClient client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                                            {
                                                if (client == null || client.GetHabbo() == null)
                                                    continue;

                                                Group thegroup = GroupManager.GetGang(Room.Group.Id);

                                                if (thegroup == null)
                                                    continue;

                                                if (thegroup != Room.Group)
                                                    continue;

                                                if (client.GetRoleplay().DisableRadio == true)
                                                    continue;

                                                client.SendWhisper("[ATENCIÓN] ¡Están vendiendo drogas en " + Room.Name + "! ¡Vamos a detener esa venta!", 30);
                                            }
                                        }
                                        break;
                                    }
                                    else
                                    {
                                        Session.SendWhisper("A este usuario ya se le ha ofrecido un Marihuana", 1);
                                        break;
                                    }
                                }
                                else
                                {
                                    Session.SendWhisper("Este ciudadano no puede pagar porros de Marihuana", 1);
                                    break;
                                }
                            }
                        }
                    #endregion


                    #region Heroina
                    case "heroina":
                        {

                            if (Session.GetRoleplay().PassiveMode)
                            {
                                Session.SendWhisper("No puedes hacer eso mientras estás en modo pasivo.", 1);
                            }
                            if (Target.GetRoleplay().PassiveMode)
                            {
                                Session.SendWhisper("No puedes venderle eso a una persona en modo pasivo.", 1);
                            }

                            if (Session.GetRoleplay().Heroina < 10)
                            {
                                Session.SendWhisper("¡Necesitas tener por lo menos 10cc para vender!");
                                return;
                            }

                            else
                            {
                                bool HasOffer = false;
                                if (Target.GetHabbo().Credits >= Convert.ToInt32(Cost.ToString().Replace("-", "")))
                                {
                                    foreach (var Offer in Target.GetRoleplay().OfferManager.ActiveOffers.Values)
                                    {
                                        if (Offer.Type.ToLower() == Type.ToLower())
                                        {
                                            HasOffer = true;
                                        }
                                    }
                                    if (!HasOffer)
                                    {
                                        Session.Shout("*Ofrece " + Cant + "cc de Heroina a " + Target.GetHabbo().Username + " por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + "*", 4);
                                        Target.GetRoleplay().OfferManager.CreateOffer("heroina", Session.GetHabbo().Id, Convert.ToInt32(Cost.ToString().Replace("-", "")), Cant);
                                        Target.SendWhisper("Acaba de ofrecerte " + Cant + "cc de Heroina por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + " Diga ':aceptar heroina' para comprarlo!", 1);
                                        break;
                                    }
                                    else
                                    {
                                        Session.SendWhisper("A este usuario ya se le ha ofrecido un Heroina", 1);
                                        break;
                                    }
                                }
                                else
                                {
                                    Session.SendWhisper("Este ciudadano no puede pagar la Heroina", 1);
                                    break;
                                }
                            }
                        }
                    #endregion

                    #region Fuel
                    case "gasolina":
                        {
                            if (!GroupManager.HasJobCommand(Session, "carro") && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                            {
                                Session.SendWhisper("Usted no trabaja en el car store corporation!", 1);
                                break;
                            }

                            if (Params.Length == 3)
                            {
                                Session.SendWhisper("Introduzca la cantidad de combustible que le gustaría ofrecer al ciudadano!", 1);
                                return;
                            }

                            int Amount;
                            if (!int.TryParse(Params[3], out Amount))
                            {
                                Session.SendWhisper("Ingrese una cantidad válida de combustible que le gustaría ofrecer al ciudadano!", 1);
                                break;
                            }

                            if (Amount < 10)
                            {
                                Session.SendWhisper("Usted necesita ofrecer al ciudadano menos 10 galones de combustible a la vez!", 1);
                                break;
                            }

                            if (Target.GetRoleplay().CarType < 1)
                            {
                                Session.SendWhisper("Lo siento, este ciudadano no tiene carro!", 1);
                                break;
                            }

                            if (!Session.GetRoleplay().IsWorking && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                            {
                                Session.SendWhisper("Usted debe estar trabajando para ofrecer a alguien gasolina!", 1);
                                break;
                            }

                            else
                            {
                                Cost = Convert.ToInt32(Math.Floor((double)(Amount * 2) / 3));
                                bool HasOffer = false;

                                if (Target.GetHabbo().Credits >= Convert.ToInt32(Cost.ToString().Replace("-", "")))
                                {
                                    foreach (var Offer in Target.GetRoleplay().OfferManager.ActiveOffers.Values)
                                    {
                                        if (Offer.Type.ToLower() == "gasolina")
                                        {
                                            HasOffer = true;
                                        }
                                    }
                                    if (!HasOffer)
                                    {
                                        Session.Shout("*Ofrece " + String.Format("{0:N0}", Amount) + " galones de gasolina a " + Target.GetHabbo().Username + " por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + "*", 4);
                                        Target.GetRoleplay().OfferManager.CreateOffer("gasolina", Session.GetHabbo().Id, Amount);
                                        Target.SendWhisper("Acaba de ofrecerte " + String.Format("{0:N0}", Amount) + " galones de gasolina por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + "! Diga ':aceptar gasolina' para comprarlo!", 1);
                                        break;
                                    }
                                    else
                                    {
                                        Session.SendWhisper("A este usuario ya se le ha ofrecido combustible", 1);
                                        break;
                                    }
                                }
                                else
                                {
                                    Session.SendWhisper("no puede pagar gasolina", 1);
                                    break;
                                }
                            }
                        }

                    #endregion

                    #region Pildoras
                    case "pildoras":
                        {
                            if (!GroupManager.HasJobCommand(Session, "pildoras") && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                            {
                                Session.SendWhisper("Usted no trabaja en la farmacia", 1);
                                break;
                            }

                            if (!Session.GetRoleplay().IsWorking && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                            {
                                Session.SendWhisper("Usted debe estar trabajando para ofrecer a alguien esteroides!", 1);
                                break;
                            }

                            else
                            {
                                Cost = 10000;
                                bool HasOffer = false;
                                if (Target.GetHabbo().Credits >= Convert.ToInt32(Cost.ToString().Replace("-", "")))
                                {
                                    foreach (var Offer in Target.GetRoleplay().OfferManager.ActiveOffers.Values)
                                    {
                                        if (Offer.Type.ToLower() == Type.ToLower())
                                        {
                                            HasOffer = true;
                                        }
                                    }
                                    if (!HasOffer)
                                    {
                                        Session.Shout("*Ofrece unas pildoras de fuerza a " + Target.GetHabbo().Username + " por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + "*", 4);
                                        Target.GetRoleplay().OfferManager.CreateOffer("pildoras", Session.GetHabbo().Id, Convert.ToInt32(Cost.ToString().Replace("-", "")));
                                        Target.SendWhisper("Recuerda que te aumenta [50+ EXPERIENCIA EN FUERZA] al consumirlo por  $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + " Diga ':aceptar pildoras' para comprarlo!", 1);
                                        break;
                                    }
                                    else
                                    {
                                        Session.SendWhisper("A este usuario ya se le ha ofrecido un pildoras", 1);
                                        break;
                                    }
                                }
                                else
                                {
                                    Session.SendWhisper("Este ciudadano no puede pagar un caja de pildoras", 1);
                                    break;
                                }
                            }
                        }
                    #endregion

                    #region Mamada
                    case "mamada":
                        {
                            if (!GroupManager.HasJobCommand(Session, "mamada") && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                            {
                                Session.SendWhisper("Usted no trabaja de prostitut@", 1);
                                break;
                            }

                            if (!Session.GetRoleplay().IsWorking && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                            {
                                Session.SendWhisper("¡Usted debe estar trabajando para ofrecer a alguien una mamada!", 1);
                                break;
                            }
                            if (Target == Session)
                            {
                                Session.SendWhisper("¡No puedes pincharte a ti mismo, pidele apoyo a un compañero de trabajo!", 1);
                                return;
                            }

                            else
                            {
                                bool HasOffer = false;
                                if (Target.GetHabbo().Credits >= Convert.ToInt32(Cost.ToString().Replace("-", "")))
                                {
                                    foreach (var Offer in Target.GetRoleplay().OfferManager.ActiveOffers.Values)
                                    {
                                        if (Offer.Type.ToLower() == Type.ToLower())
                                        {
                                            HasOffer = true;
                                        }
                                    }
                                    if (!HasOffer)
                                    {
                                        Session.Shout("*Ofrece una rica mamada a " + Target.GetHabbo().Username + " por $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + "*", 4);
                                        Target.GetRoleplay().OfferManager.CreateOffer("mamada", Session.GetHabbo().Id, Convert.ToInt32(Cost.ToString().Replace("-", "")));
                                        Target.SendWhisper("Recuerda que te aumenta [50+ FELICIDAD] $" + String.Format("{0:N0}", Convert.ToInt32(Cost.ToString().Replace("-", ""))) + " Diga ':aceptar mamada' para que te la chupen!", 1);
                                        break;
                                    }
                                    else
                                    {
                                        Session.SendWhisper("A este usuario ya se le ha ofrecido una mamada", 1);
                                        break;
                                    }
                                }
                                else
                                {
                                    Session.SendWhisper("Este ciudadano no puede pagar una mamada", 1);
                                    break;
                                }
                            }
                        }
                    #endregion

                    #region Piezas
                    case "piezas":
                    case "pieza":
                        {
                            /*if (Session.GetRoleplay().ArmPieces < Cant)
                            {
                                Session.SendWhisper("No tienes " + Cant + " piezas de armas para vender.", 1);
                                return;
                            }

                            if (Session.GetRoleplay().PassiveMode)
                            {
                                Session.SendWhisper("No puedes hacer eso mientras estás en modo pasivo.", 1);
                                return;
                            }
                            if (Target.GetRoleplay().PassiveMode)
                            {
                                Session.SendWhisper("No puedes venderle eso a una persona en modo pasivo.", 1);
                                return;
                            }
                            */
                            #region Offer Conditions
                            foreach (var Offer in Target.GetRoleplay().OfferManager.ActiveOffers.Values)
                            {
                                if (Offer.Type.ToLower() == "piezas")
                                {
                                    Session.SendWhisper("Esa persona ya tiene una oferta de piezas de armas pendiente.", 1);
                                    return;
                                }
                            }
                            #endregion

                            #region Execute
                            RoleplayManager.Shout(Session, "*Ofrece " + Cant + " piezas de armas a " + Target.GetHabbo().Username + " por $" + String.Format("{0:N0}", Cost) + "*", 5);
                            Target.GetRoleplay().OfferManager.CreateOffer("piezas", Session.GetHabbo().Id, Cost, Cant);
                            Target.SendWhisper("Te han ofrecido " + Cant + " piezas de armas por $" + String.Format("{0:N0}", Cost) + ". Escribe ':aceptar piezas' para aceptarlos en su defecto ':rechazar piezas'.", 1);
                            Session.GetRoleplay().CooldownManager.CreateCooldown("comprar", 1000, 3);
                            #endregion
                        }
                        break;
                    #endregion

                    #region Platinos
                    /*case "rubies":
                        {
                            if (Session.GetHabbo().Diamonds < Cant)
                            {
                                Session.SendWhisper("No tienes " + Cant + " rubie(s) para vender.", 1);
                                return;
                            }

                            #region Offer Conditions
                            foreach (var Offer in Target.GetRoleplay().OfferManager.ActiveOffers.Values)
                            {
                                if (Offer.Type.ToLower() == "rubies")
                                {
                                    Session.SendWhisper("Esa persona ya tiene una oferta de rubies pendiente.", 1);
                                    return;
                                }
                            }
                            #endregion

                            #region Execute
                            RoleplayManager.Shout(Session, "*Ofrece " + Cant + " rubie(s) a " + Target.GetHabbo().Username + " por $" + String.Format("{0:N0}", Cost) + "*", 5);
                            Target.GetRoleplay().OfferManager.CreateOffer("rubies", Session.GetHabbo().Id, Cost, Cant);
                            Target.SendWhisper("Te han ofrecido " + Cant + " rubie(s) por $" + String.Format("{0:N0}", Cost) + ". Escribe ':aceptar rubies' para aceptarlos o en su defecto ':rechazar rubies'.", 1);
                            Session.GetRoleplay().CooldownManager.CreateCooldown("comprar", 1000, 3);
                            #endregion
                        }
                        break;*/
                    #endregion

                    #region Default
                    default:
                        {
                            Session.SendWhisper("'" + Type + "' no es una oferta válida.", 1);
                            break;
                        }
                        #endregion
                }
                #endregion
            }
            else if (Dir == 3)
            {
                // Verificar que tenemos al menos 2 parámetros
                if (Params.Length < 2)
                {
                    Session.SendWhisper("Comando inválido, escribe ':vender objeto' o ':vender vehículo'", 1);
                    return;
                }

                string Type = Params2;
                switch (Type.ToLower())
                {
                    #region :vender objeto
                    case "objeto":
                        {
                            /*if (!Room.RobStoreEnabled)
                            {
                                Session.SendWhisper("Debes estar en una Tienda de Venta de Objetos Robados para hacer eso.", 1);
                                return;
                            }*

                            #region Comodin Conditions
                            Item BTile = null;
                            BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "actionpoint01" && x.Coordinate == Session.GetRoomUser().Coordinate);
                            if (BTile == null)
                            {
                                Session.SendWhisper("Debes estar cerca del Mostrador para vender objetos.", 1);
                                return;
                            }
                            #endregion

                            if (Session.GetRoleplay().Object.Length <= 0)
                            {
                                Session.SendWhisper("No tienes ningún objeto en el Inventario para vender.", 1);
                                return;
                            }

                            if (Session.GetRoleplay().PassiveMode)
                            {
                                Session.SendWhisper("No puedes hacer eso mientras estás en modo pasivo.", 1);
                                return;
                            }

                            RoleplayManager.Shout(Session, "*Vende un/a " + Session.GetRoleplay().Object + "*", 5);
                            Session.SendWhisper("Has vendido un/a " + Session.GetRoleplay().Object + " y recibes $ " + String.Format("{0:N0}", Session.GetRoleplay().ObjectPrice), 1);
                            Session.GetHabbo().Credits += Session.GetRoleplay().ObjectPrice;
                            Session.GetRoleplay().MoneyEarned += Session.GetRoleplay().ObjectPrice;
                            Session.GetHabbo().UpdateCreditsBalance();
                            Session.GetRoleplay().Object = "";
                            Session.GetRoleplay().ObjectPrice = 0;*/
                        }
                        break;
                    #endregion

                    #region :vender vehículo
                    case "vehiculo":
                    case "carro":
                    case "car":
                        {
                            /*if (!Room.MunicipalidadEnabled)
                            {
                                Session.SendWhisper("Debes estar en la Municipalidad de la ciudad para hacer eso.", 1);
                                return;
                            }*/
                            if (Session.GetRoleplay().DrivingInCar)
                            {
                                Session.SendWhisper("¡No puedes hacer eso mientras tengas un vehículo en marcha afuera!", 1);
                                return;
                            }

                            #region Action Point Conditions
                            /*Item BTile = null;
                            BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "actionpoint01" && x.Coordinate == Session.GetRoomUser().Coordinate);
                            if (BTile == null)
                            {
                                Session.SendWhisper("Debes acercarte al mostrador para vender vehículos.", 1);
                                return;
                            }*/
                            #endregion

                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_products", "close");
                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_products", "open_sellcar");
                        }
                        break;
                    #endregion

                    #region Default
                    default:
                        {
                            Session.SendWhisper("'" + Type + "' no es un elemento válido a vender.", 1);
                        }
                        break;
                        #endregion
                }
            }
        }
    }
}