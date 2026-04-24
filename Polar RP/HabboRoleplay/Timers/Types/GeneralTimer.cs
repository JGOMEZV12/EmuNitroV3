using System;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.Core;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.Rooms;
using Polar.HabboRoleplay.VehicleOwned;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.Items.Crafting;
using Polar.Utilities;

namespace Polar.HabboRoleplay.Timers.Types
{
    /// <summary>
    /// Makes the citizen get hungry over time
    /// </summary>
    public class GeneralTimer : RoleplayTimer
    {
        public GeneralTimer(string Type, GameClient Client, int Time, bool Forever, object[] Params) 
            : base(Type, Client, Time, Forever, Params)
        {
            // Convert to milliseconds
            TimeLeft = base.Client.GetRoleplay().LoadingTimeLeft * 1000;
        }
 
        /// <summary>
        /// Increases the users hunger
        /// </summary>
        public override void Execute()
        {
            try
            {
                // ✅ FIX #1: La condición guard verificaba base.Client == null pero luego dentro del bloque
                //   accedía a base.Client.GetRoleplay().TogglingPSV sin re-verificar.
                //   Si Client==null o GetRoleplay()==null, esa línea interna explota con NullReferenceException.
                //   Solución: guardar el estado que necesitamos ANTES de entrar al bloque, usando null-conditional.
                bool shouldBreak = base.Client == null
                    || base.Client.GetHabbo() == null
                    || base.Client.GetRoleplay() == null
                    || base.Client.GetRoleplay().IsDead
                    || base.Client.GetRoleplay().IsJailed
                    || base.Client.GetRoomUser() == null
                    || base.Client.GetRoleplay().BreakGeneralTimer;

                if (shouldBreak)
                {
                    // Ahora es seguro acceder — sabemos que Client y GetRoleplay() no son null
                    // solo si llegamos aquí por IsDead/IsJailed/BreakGeneralTimer/GetRoomUser==null.
                    // Si Client o GetRoleplay() eran null, no tocamos nada más y solo terminamos el timer.
                    if (base.Client == null || base.Client.GetRoleplay() == null)
                    {
                        base.EndTimer();
                        return;
                    }

                    if (!base.Client.GetRoleplay().TogglingPSV)
                        RoleplayManager.Shout(base.Client, "*Ha dejado de realizar la acción en la que estaba*", 5);

                    base.EndTimer();
                    base.Client.GetRoleplay().BreakGeneralTimer = false;

                    #region Retornamos Variables de Timers

                    #region Combustible
                    if (base.Client.GetRoleplay().IsFuelCharging)
                    {
                        base.Client.GetRoleplay().IsFuelCharging = false;
                        base.Client.GetRoleplay().FuelChargingCant = 0;
                        base.Client.SendWhisper("No debes moverte ni apagar tu vehículo hasta que se termine de llenar el tanque.", 1);
                    }
                    #endregion

                    #region Camionero Cargando
                    if (base.Client.GetRoleplay().IsCamLoading)
                    {
                        base.Client.GetRoleplay().IsCamLoading = false;
                        base.Client.GetRoleplay().CamCargId = 0;
                        base.Client.SendWhisper("No debes moverte ni apagar tu Camión hasta que se termine de cargar.", 1);
                    }
                    #endregion

                    #region Camionero Descargando
                    if (base.Client.GetRoleplay().IsCamUnLoading)
                    {
                        base.Client.GetRoleplay().IsCamUnLoading = false;
                        base.Client.SendWhisper("No debes moverte ni apagar tu Camión hasta que se termine de descargar.", 1);
                    }
                    #endregion

                    #region Robar Banco
                    if (base.Client.GetRoleplay().Robbery)
                    {
                        List<RoomUser> UsersToReturn = base.Client.GetRoomUser().GetRoom().GetRoomUserManager().GetRoomUsers().ToList();
                        foreach (RoomUser User in UsersToReturn)
                        {
                            if (User == null || User.GetClient() == null)
                                continue;

                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(User.GetClient(), "event_gang", "bank_cap_off");
                        }

                        base.Client.SendWhisper("Oops! Has dejado de robar.");
                        base.Client.GetRoomUser().GetRoom().BankCapturing = false;
                        base.Client.GetRoleplay().BankCapturing = false;
                        base.Client.GetRoleplay().Robbery = false;
                    }
                    #endregion

                    #region Leer
                    if (base.Client.GetRoleplay().Learning)
                    {
                        base.Client.SendWhisper("Oops! Dejaste de leer.");
                        base.Client.GetRoleplay().Learning = false;
                    }
                    #endregion

                    #region Toggling PSV Mode
                    if (base.Client.GetRoleplay().TogglingPSV)
                    {
                        base.Client.GetRoleplay().TogglingPSV = false;
                        base.Client.GetRoleplay().SpecialCooldowns.TryUpdate("psvmode", 0, base.Client.GetRoleplay().SpecialCooldowns["psvmode"]);
                    }
                    #endregion

                    #endregion

                    return;
                }

                if (base.Client.GetRoomUser() == null)
                    return;

                if (base.Client.GetRoomUser().IsAsleep)
                    return;

                #region Specific Special Conditions
                if ((base.Client.GetRoleplay().IsFuelCharging && !base.Client.GetRoleplay().DrivingCar) || (base.Client.GetRoleplay().IsCamLoading && !base.Client.GetRoleplay().DrivingCar) || (base.Client.GetRoleplay().IsCamUnLoading && !base.Client.GetRoleplay().DrivingCar))
                {
                    base.Client.GetRoleplay().BreakGeneralTimer = true;
                    return;
                }
                #endregion

                TimeCount++;
                
                TimeLeft -= 1000;

                base.Client.GetRoleplay().LoadingTimeLeft--;

                if (TimeLeft > 0)
                {
                    if (TimeCount == 2)
                    {
                        if (!base.Client.GetRoleplay().TurfCapturing && !base.Client.GetRoleplay().BankCapturing && !base.Client.GetRoleplay().Learning 
                            && !base.Client.GetRoleplay().ProcessCocaine 
                            && !base.Client.GetRoleplay().ProcessHeroine 
                            && !base.Client.GetRoleplay().ProcessWeed)
                            base.Client.SendWhisper("Debes esperar " + base.Client.GetRoleplay().LoadingTimeLeft + " segundo(s)...", 1);
                        TimeCount = 0;
                    }
                    else if (TimeCount == 60)
                    {
                        if (base.Client.GetRoleplay().BankCapturing)
                            RoleplayManager.Shout(base.Client, "*Se acerca a robar el banco [" + (TimeLeft / 60000) + " Minutos restantes]*", 4);

                        if (base.Client.GetRoleplay().ProcessCocaine)
                            base.Client.SendWhisper("¡Casi termina la fabricación de la Cocaina!", 1);
                        if (base.Client.GetRoleplay().ProcessHeroine)
                            base.Client.SendWhisper("¡Casi termina la fabricación de la Heroina!", 1);
                        if (base.Client.GetRoleplay().ProcessWeed)
                            base.Client.SendWhisper("¡Casi termina la fabricación de la Marihuana!", 1);

                        TimeCount = 0;
                    }
                    else if (TimeLeft == 5000)
                    {
                        if (base.Client.GetRoleplay().IsCamLoading)
                            base.Client.SendWhisper("Los Cargadores están terminando de subir la última carga y cerrando tu compuerta.", 1);
                        if (base.Client.GetRoleplay().IsCamUnLoading)
                            base.Client.SendWhisper("Los Cargadores están terminando de bajar la última carga y cerrando tu compuerta.", 1);
                        if (base.Client.GetRoleplay().IsMecLoading)
                            RoleplayManager.Shout(base.Client, "*Hace las últimas pruebas al vehículo comprobando que todo funcione correctamente*", 5);
                    }
                    return;
                }

                #region Cumple el Timer

                #region Fuel Charging
                if (base.Client.GetRoleplay().IsFuelCharging)
                {
                    int Cant = base.Client.GetRoleplay().FuelChargingCant;
                    int Price = Cant * RoleplayManager.FuelPrice;
                    base.Client.SendWhisper("¡" + Cant + " L. de Combustible Cargado(s)! Gracias por su compra. (-$" + Price + ")", 1);

                    base.Client.GetHabbo().Credits -= Price;
                    base.Client.GetHabbo().UpdateCreditsBalance();

                    base.Client.GetRoleplay().IsFuelCharging = false;
                    base.Client.GetRoleplay().FuelChargingCant = 0;

                    List<VehiclesOwned> VO = PolarEnvironment.GetGame().GetVehiclesOwnedManager().getVehiclesOwnedList(base.Client.GetRoleplay().DrivingCarId);
                    if (VO != null && VO.Count > 0)
                    {
                        VO[0].Fuel += Cant;
                        RoleplayManager.UpdateVehicleStat(VO[0].Id, "fuel", VO[0].Fuel);
                        base.Client.GetRoleplay().CarFuel = VO[0].Fuel;
                    }

                    PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(base.Client, "event_vehicle", "open");
                    base.Client.GetRoleplay().LoadingTimeLeft = 0;
                }
                #endregion

                #region Camionero
                if (base.Client.GetRoleplay().IsCamLoading)
                {
                    List<VehiclesOwned> VO = PolarEnvironment.GetGame().GetVehiclesOwnedManager().getVehiclesOwnedList(base.Client.GetRoleplay().DrivingCarId);
                    if (VO != null && VO.Count > 0)
                    {
                        if (!RoleplayManager.GenerateRoom(VO[0].CamDest, out Room Room))
                        {
                            base.Client.SendWhisper("Al parecer no se encontró el destino generado para esta ciudad. ((Contacta con un administrador))");

                            base.Client.GetRoleplay().IsCamLoading = false;
                            base.Client.GetRoleplay().LoadingTimeLeft = 0;
                            base.EndTimer();
                        }
                        else
                        {
                            VO[0].CamCargId = base.Client.GetRoleplay().CamCargId;
                            VO[0].CamState = 1;
                            VO[0].CamOwnId = base.Client.GetHabbo().Id;
                            base.Client.SendWhisper("¡Camión Cargado! Ahora dirígete a " + Room.Name + " y usa ':depositarcarga'", 1);
                            base.Client.GetRoleplay().IsCamLoading = false;

                            PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(base.Client, "compose_camionero|showinfo|depositar|" + RoleplayManager.getCamCargName(VO[0].CamCargId) + "|" + base.Client.GetHabbo().Username + "|" + Room.Name);
                        }
                    }
                }

                if (base.Client.GetRoleplay().IsCamUnLoading)
                {
                    List<VehiclesOwned> VO = PolarEnvironment.GetGame().GetVehiclesOwnedManager().getVehiclesOwnedList(base.Client.GetRoleplay().DrivingCarId);
                    if (VO != null && VO.Count > 0)
                    {
                        if (!RoleplayManager.GenerateRoom(Client.GetRoomUser().RoomId, out Room Room))
                            return;
                        string MyCity = Room.City;

                        HabboRoleplay.RPRoom.RPRoom Data;
                        int Camioneros = PolarEnvironment.GetGame().GetRPRoomManager().TryToGetCamioneros(MyCity, out Data);
                        if (Camioneros < 1)
                        {
                            base.Client.SendWhisper("Al parecer no se encontró la Zona de Camioneros en la Ciudad. ((Contacta con un administrador))");
                            base.Client.GetRoleplay().IsCamUnLoading = false;
                            base.Client.GetRoleplay().LoadingTimeLeft = 0;
                            base.EndTimer();
                        }
                        else
                        {
                            base.Client.SendWhisper("¡Carga entregada! Ahora regresa tu camión y usa ':entregarcamion' para recibir tu pago.", 1);
                            VO[0].CamDest = Camioneros;
                            VO[0].CamState = 2;
                            base.Client.GetRoleplay().IsCamUnLoading = false;

                            if (!RoleplayManager.GenerateRoom(Camioneros, out Room DestRoom))
                                return;

                            PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(base.Client, "compose_camionero|showinfo|entregar|Ninguno|" + base.Client.GetHabbo().Username + "|" + DestRoom.Name);
                        }
                    }
                }
                #endregion

                #region Mecánico
                if (base.Client.GetRoleplay().IsMecLoading)
                {
                    GameClient Cliente = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(base.Client.GetRoleplay().MecUserToRepair);
                    if (Cliente != null)
                    {
                        List<VehiclesOwned> VO = PolarEnvironment.GetGame().GetVehiclesOwnedManager().getVehiclesOwnedByFurniId(base.Client.GetRoleplay().MecCarToRepair);
                        if (VO != null && VO.Count > 0)
                        {
                            int Price = base.Client.GetRoleplay().MecPriceTo;
                            RoleplayManager.Shout(base.Client, "*Repara el vehículo y cierra el capó*", 5);
                            base.Client.SendWhisper("¡Vehículo reparado! Has ganado $" + Price, 1);

                            RoleplayManager.JobSkills(base.Client, base.Client.GetRoleplay().JobId, base.Client.GetRoleplay().MecLvl, base.Client.GetRoleplay().MecXP);

                            base.Client.GetRoleplay().MecParts -= base.Client.GetRoleplay().MecPartsTo;
                            RoleplayManager.UpdateVehicleState(base.Client.GetRoleplay().MecCarToRepair, base.Client.GetRoleplay().MecNewState);
                            RoleplayManager.UpdateVehicleStat(base.Client.GetRoleplay().MecCarToRepair, "life", 100);
                            VO[0].State = base.Client.GetRoleplay().MecNewState;
                            VO[0].CarLife = 100;

                            Cliente.GetHabbo().Credits -= Price;
                            Cliente.GetHabbo().UpdateCreditsBalance();
                            base.Client.GetHabbo().Credits += Price;
                            base.Client.GetHabbo().UpdateCreditsBalance();
                        }
                        else
                        {
                            base.Client.SendWhisper("No se pudo reparar el vehículo. ((Contacta con un administrador))", 1);
                        }
                        base.Client.GetRoleplay().MecCarToRepair = 0;
                        base.Client.GetRoleplay().MecPartsTo = 0;
                        base.Client.GetRoleplay().MecPriceTo = 0;
                        base.Client.GetRoleplay().MecUserToRepair = 0;
                        base.Client.GetRoleplay().MecNewState = 0;
                        base.Client.GetRoleplay().MecRotPosition = 0;
                        base.Client.GetRoleplay().IsMecLoading = false;
                    }
                    else
                    {
                        base.Client.GetRoleplay().MecCarToRepair = 0;
                        base.Client.GetRoleplay().MecPartsTo = 0;
                        base.Client.GetRoleplay().MecPriceTo = 0;
                        base.Client.GetRoleplay().MecUserToRepair = 0;
                        base.Client.GetRoleplay().MecNewState = 0;
                        base.Client.GetRoleplay().MecRotPosition = 0;
                        base.Client.GetRoleplay().IsMecLoading = false;
                        base.Client.SendWhisper("¡Oh oh! Al parecer la persona con quien negociabas se ha ido. No se te pagará nada pero tampoco le dejarás su Vehículo Reparado.", 1);
                    }
                }
                #endregion

                #region Toggling PSV Mode
                if (base.Client.GetRoleplay().TogglingPSV)
                {
                    RoleplayManager.TogglePassiveMode(base.Client);
                    base.Client.GetRoleplay().TogglingPSV = false;
                }
                #endregion

                #region Robar banco
                if (base.Client.GetRoleplay().Robbery)
                {
                    Group company = GroupManager.GetJob(9);
                    if (company == null)
                    {
                        base.EndTimer();
                        return;
                    }

                    if (company.Balance <= 50000)
                    {
                        base.Client.SendWhisper("Oops! La boveda actualmente no cuenta con fondos minimos para el robo.");
                        base.EndTimer();
                        return;
                    }

                    Random rnd = new Random();
                    int maxrob = (company.Balance < 30000) ? (company.Balance / 2) : 30000;
                    int money = rnd.Next(5000, maxrob);

                    company.Balance -= money;

                    if (company.Balance <= 0)
                    {
                        base.Client.SendWhisper("La boveda tiene $0, Significa que ya fue completamente robada! Eso dice que no ganaste nada robando el banco!");
                        base.Client.GetRoleplay().Robbery = false;
                        base.EndTimer();
                        return;
                    }

                    RoleplayManager.Shout(base.Client, "*Termino su robo al banco [+$" + money + "]*");
                    RoleplayManager.GiveMoney(base.Client, money);

                    #region Bank Company Balance
                    RoleplayManager.TakeMoneyFromCompany(9, money);
                    #endregion

                    base.Client.SendWhisper("Dinero restante de boveda: $" + company.Balance + "!");
                    base.Client.GetRoleplay().Robbery = false;
                    base.Client.GetRoleplay().BankCapturing = false;
                    base.Client.GetRoomUser().GetRoom().BankCapturing = false;

                    var roomUser = base.Client.GetRoomUser();
                    if (roomUser != null)
                    {
                        var roomUsers = roomUser.GetRoom()?.GetRoomUserManager()?.GetRoomUsers()?.ToList();
                        if (roomUsers != null)
                        {
                            foreach (var user in roomUsers)
                            {
                                if (user?.GetClient() != null)
                                {
                                    PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(
                                        user.GetClient(), "event_gang", "bank_cap_off");
                                }
                            }
                        }
                    }
                    // Set 5 hour cooldown for the vault
                    RoleplayManager.VaultCooldowns[base.Client.GetRoomUser().RoomId] = DateTime.Now.AddHours(5);
                }
                #endregion

                #region Leer
                if (base.Client.GetRoleplay().Learning)
                {
                    LevelManager.AddIntelligenceEXP(base.Client, 1);
                    RoleplayManager.Shout(base.Client, "*Termino de leer [+1] Inteligencia ¡Wooow!*");
                    Client.SendWhisper("*Tu inteligencia es ahora: " + base.Client.GetRoleplay().Intelligence + "*");
                    base.Client.GetRoleplay().Learning = false;
                }
                #endregion

                #region ProcessCocaina
                if (base.Client.GetRoleplay().ProcessCocaine)
                {
                    var random = new CryptoRandom();
                    int totalCraftingItems = CraftingManager.CraftableItems.Count;
                    int chance = random.Next(1, 5);
                    int secondChance = random.Next(1, 5);

                    if (secondChance < 4 && chance > totalCraftingItems)
                        chance = random.Next(1, totalCraftingItems + 1);

                    if (chance > 1 && chance <= 6)
                    {
                        int amount = random.Next(10, 15);
                        base.Client.GetRoleplay().Cocaine += amount;
                        base.Client.Shout($"*¡Felicidades fabricaste! {amount}g de cocaína*", 7);
                        base.Client.SendWhisper("*¡Ve a tu casa y guárdala en el baúl! [TODO ESTO ES ILEGAL]*", 1);
                        base.Client.GetRoleplay().RefreshStatDialogue();
                    }
                    // FIX #5: Null check en HRidItem antes de acceder a sus propiedades
                    if (base.Client.GetRoleplay().HRidItem != null)
                    {
                        base.Client.GetRoleplay().HRidItem.ExtraData = "0";
                        base.Client.GetRoleplay().HRidItem.UpdateState(false, true);
                    }
                    base.Client.GetRoomUser().CanWalk = true;
                    base.Client.GetRoleplay().ProcessCocaine = false;
                }
                #endregion

                #region ProcessHeroina
                if (base.Client.GetRoleplay().ProcessHeroine)
                {
                    if (base.Client.GetRoomUser().CurrentEffect != 546 && base.Client.GetRoleplay().EquippedWeapon == null)
                        base.Client.GetRoomUser().ApplyEffect(0);

                    var random = new CryptoRandom();
                    int totalCraftingItems = CraftingManager.CraftableItems.Count;
                    int chance = random.Next(1, 5);
                    int secondChance = random.Next(1, 5);

                    if (secondChance < 4 && chance > totalCraftingItems)
                        chance = random.Next(1, totalCraftingItems + 1);

                    if (chance > 1 && chance <= 6)
                    {
                        int amount = random.Next(10, 15);
                        base.Client.GetRoleplay().Heroina += amount;
                        base.Client.Shout("*¡Felicidades fabricaste! " + amount + "cc de Heroína*", 7);
                        base.Client.SendWhisper("*¡Ve a tu casa y guárdala en el baúl! [TODO ESTO ES ILEGAL]*", 1);
                        base.Client.GetRoleplay().RefreshStatDialogue();
                    }

                    if (base.Client.GetRoleplay().HRidItem != null)
                    {
                        base.Client.GetRoleplay().HRidItem.ExtraData = "0";
                        base.Client.GetRoleplay().HRidItem.UpdateState(false, true);
                    }
                    base.Client.GetRoomUser().CanWalk = true;
                    base.Client.GetRoleplay().ProcessHeroine = false;
                }
                #endregion

                #region ProcessWeed
                if (base.Client.GetRoleplay().ProcessWeed)
                {
                    if (base.Client.GetRoomUser().CurrentEffect != 595 && base.Client.GetRoleplay().EquippedWeapon == null)
                        base.Client.GetRoomUser().ApplyEffect(0);

                    // ✅ FIX #2: Se usaba "Random.Next" (tipo System.Random como nombre de clase/campo estático)
                    //   en lugar de la instancia local "random". Reemplazado con instancia local correcta.
                    var random = new CryptoRandom();
                    int totalCraftingItems = CraftingManager.CraftableItems.Count;
                    int chance = random.Next(1, 5);
                    int secondChance = random.Next(1, 5);

                    if (secondChance < 4 && chance > totalCraftingItems)
                        chance = random.Next(1, totalCraftingItems + 1);

                    // ✅ FIX #3: "else if" huérfano sin "if" previo en el mismo bloque.
                    //   La estructura correcta es if/else if/else, igual que ProcessCocaine y ProcessHeroine.
                    if (chance > 1 && chance <= 6)
                    {
                        int Amount = random.Next(1, 10);
                        base.Client.GetRoleplay().Weedmateria += Amount;
                        base.Client.Shout("*¡Felicidades cosechaste! " + Amount + " semillas de marihuana *", 6);
                        base.Client.SendWhisper("*¡utiliza la mesa para preparar los porros! [TODO ESTO ES ILEGAL]*", 1);
                    }
                    else
                    {
                        base.Client.SendWhisper("*¡Esta planta no tiene semillas que ofrecer! intentalo más tarde*", 1);
                    }

                    base.Client.GetRoleplay().HRidItem.ExtraData = "0";
                    base.Client.GetRoleplay().HRidItem.UpdateState(false, true);
                    base.Client.GetRoomUser().CanWalk = true;
                    base.Client.GetRoleplay().ProcessWeed = false;
                }
                #endregion

                base.Client.GetRoleplay().LoadingTimeLeft = 0;
                base.EndTimer();
                #endregion

            }
            catch (Exception e)
            {
                Logging.LogRPTimersError("Error in Execute() void: " + e);
                base.EndTimer();
            }
        }
    }
}