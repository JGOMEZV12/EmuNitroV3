using Polar.Communication.Packets.Outgoing.Inventory.Weapons;
using Polar.Core;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users.Effects;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.RPRoom;
using Polar.HabboRoleplay.VehicleOwned;
using Polar.HabboRoleplay.Vehicles;
using Polar.HabboRoleplay.Weapons;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace Polar.HabboRoleplay.Timers.Types
{
    /// <summary>
    /// Checks the users roleplay conditions
    /// </summary>
    public class ConditionCheckTimer : RoleplayTimer
    {
        public ConditionCheckTimer(string Type, GameClient Client, int Time, bool Forever, object[] Params)
            : base(Type, Client, Time, Forever, Params)
        {
            TimeCount = 0;
        }

        /// <summary>
        /// Checks the users roleplay conditions
        /// </summary>
        public override void Execute()
        {
            try
            {
                #region Base Conditions
                if (base.Client == null || base.Client.GetHabbo() == null || base.Client.GetRoleplay() == null)
                {
                    base.EndTimer();
                    return;
                }

                if (base.Client.GetRoomUser() == null)
                    return;

                if (base.Client.GetHabbo().CurrentRoom == null)
                    return;
                #endregion

                #region Variables
                bool Equipped = base.Client.GetRoleplay().EquippedWeapon == null ? false : true;
                bool Hygiene = base.Client.GetRoleplay().Hygiene == 0 ? true : false;
                bool Poop = base.Client.GetRoleplay().Poop == 0 ? true : false;
                bool Healing = base.Client.GetRoleplay().BeingHealed;
                bool Farming = base.Client.GetRoleplay().WateringCan;
                int Effect = base.Client.GetRoomUser().CurrentEffect;
                int Item = base.Client.GetRoomUser().CarryItemID;
                #endregion

                #region Random Checks
                if (base.Client.GetRoleplay().IsWorking || base.Client.GetRoleplay().IsWorkingOut || base.Client.GetRoleplay().BankAccount <= 2 && !base.Client.GetRoomUser().IsAsleep)
                {
                    if (!base.Client.GetRoleplay().CaptchaSent)
                        base.Client.GetRoleplay().CaptchaTime++;
                }

                if (base.Client.GetRoomUser().IsAsleep)
                {
                    if (Equipped)
                    {
                        base.Client.GetRoleplay().EquippedWeapon = null;

                        if (base.Client.GetRoomUser().CurrentEffect != EffectsList.None)
                            base.Client.GetRoomUser().ApplyEffect(EffectsList.None);
                        if (base.Client.GetRoomUser().CarryItemID != 0)
                            base.Client.GetRoomUser().CarryItem(0);

                        base.Client.SendMessage(new WeaponsComposer(base.Client));
                    }
                }
                if (!Equipped)
                {
                    if (Effect > 0 && WeaponManager.Weapons.Values.Where(x => x.EffectID == Effect).ToList().Count > 0)
                        base.Client.GetRoomUser().ApplyEffect(EffectsList.None);

                    if (Item > 0 && WeaponManager.Weapons.Values.Where(x => x.HandItem == Item).ToList().Count > 0)
                        base.Client.GetRoomUser().CarryItem(EffectsList.None);
                }
                // Al salir a la Calle, recolocar el auto si entró antes sin estacionar.
                if (base.Client.GetHabbo().CurrentRoom.DriveEnabled && !base.Client.GetRoleplay().DrivingCar)
                {
                    if (base.Client.GetRoleplay().DrivingInCar)
                    {
                        RoleplayManager.Shout(base.Client, "*Sale del establecimiento y arranca su vehículo*", 5);
                        base.Client.GetRoleplay().DrivingInCar = false;
                        base.Client.GetRoleplay().DrivingCar = true;
                        base.Client.GetRoleplay().CarEnableId = base.Client.GetRoleplay().CarEffectId;
                        // Abrimos ventana de combustible
                        PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(base.Client, "event_vehicle", "open");
                        PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(base.Client, "compose_carnew|open");
                    }
                }
                #endregion

                #region Anti-Enable Checks
                // Repairing Fence & Event Capturing Check
                if (Effect == EffectsList.SunnyD && (!base.Client.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("repair") || !base.Client.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("capture")))
                    base.Client.GetRoomUser().ApplyEffect(EffectsList.None);
                // Working Out
                if ((Effect == EffectsList.Treadmill || Effect == EffectsList.CrossTrainer) && !base.Client.GetRoleplay().IsWorkingOut)
                    base.Client.GetRoomUser().ApplyEffect(EffectsList.None);
                // Farming Check
                if (Effect == EffectsList.WateringCan && !Farming)
                    base.Client.GetRoomUser().ApplyEffect(EffectsList.None);
                // Hygiene Check
                if (Effect == EffectsList.Flies && !Hygiene)
                    base.Client.GetRoomUser().ApplyEffect(EffectsList.None);
                // Cagar Check
                if (Effect == EffectsList.Flies && !Poop)
                    base.Client.GetRoomUser().ApplyEffect(EffectsList.None);
                // Stun & Frozen Check
                if ((Effect == EffectsList.Dizzy || Effect == EffectsList.Ice) && !base.Client.GetRoomUser().Frozen)
                    base.Client.GetRoomUser().ApplyEffect(EffectsList.None);
                // Riding Horse Check
                if (Effect == EffectsList.HorseRiding && !base.Client.GetRoomUser().RidingHorse)
                    base.Client.GetRoomUser().ApplyEffect(EffectsList.None);
                // Healing check
                if (Effect == EffectsList.GreenGlow && !Healing)
                    base.Client.GetRoomUser().ApplyEffect(EffectsList.None);
                // Police On Duty Check
                if (Effect == EffectsList.HoloRPPolice && (!base.Client.GetRoleplay().IsWorking || !GroupManager.HasJobCommand(base.Client, "guide")))
                    base.Client.GetRoomUser().ApplyEffect(EffectsList.None);
                // Hospital On Duty Check
                if (Effect == EffectsList.Medic && (!base.Client.GetRoleplay().IsWorking || !GroupManager.HasJobCommand(base.Client, "heal")))
                    base.Client.GetRoomUser().ApplyEffect(EffectsList.None);
                // Car Driving Check
                if ((Effect == EffectsList.CarStaff || Effect == EffectsList.CarAmbulance || Effect == EffectsList.CarJuice || Effect == EffectsList.CarBunny || Effect == EffectsList.CarDog || Effect == EffectsList.CarPolice || Effect == EffectsList.CarDollar || Effect == EffectsList.CarTopFuel || Effect == EffectsList.CarMini || Effect == EffectsList.HoverboardYellow) && !base.Client.GetRoleplay().DrivingCar)
                    base.Client.GetRoomUser().ApplyEffect(EffectsList.None);
                // Cuffed Check
                if (Effect == EffectsList.Cuffed && !base.Client.GetRoleplay().Cuffed)
                    base.Client.GetRoomUser().ApplyEffect(EffectsList.None);
                // Taxi Check (Police)
                if (Effect == EffectsList.PoliceTaxi && !base.Client.GetRoleplay().InsideTaxi)
                    base.Client.GetRoomUser().ApplyEffect(EffectsList.None);
                // Passive Check
                if (Effect == EffectsList.Passive && !base.Client.GetRoleplay().PassiveMode)
                    base.Client.GetRoomUser().ApplyEffect(EffectsList.None);
                // Passive Check
                if (Effect == EffectsList.Twinkle)
                    base.Client.GetRoomUser().ApplyEffect(EffectsList.None);
                #endregion

                #region Main Checks

                if (base.Client.GetRoleplay().DrivingCar)
                {
                    // Si God == true detenemos inmunidad 
                    if (base.Client.GetRoleplay().SexTimer > 0)
                    {
                        base.Client.GetRoleplay().SexTimer = 0;
                        base.Client.GetHabbo().Poof(true);
                    }

                    if (base.Client.GetRoleplay().ChalecoPor > 0)
                    {
                        base.Client.GetRoleplay().ChalecoPor = 0;
                        base.Client.GetHabbo().Poof(true);
                    }

                    // No consume combustible mientras esté llenando
                    if (!base.Client.GetRoleplay().IsFuelCharging)
                        base.Client.GetRoleplay().CarTimer++;

                    if (!base.Client.GetRoomUser().FastWalking)
                        base.Client.GetRoomUser().FastWalking = true;

                    if (base.Client.GetRoleplay().Chofer && !base.Client.GetRoomUser().AllowOverride)
                        base.Client.GetRoomUser().AllowOverride = true;

                    if (base.Client.GetHabbo().CurrentRoom == null)
                    {
                        base.Client.GetRoleplay().DrivingCar = false;
                        base.Client.GetRoleplay().CarEnableId = 0;
                    }
                    else
                    {
                        if (base.Client.GetRoleplay().CarEnableId == EffectsList.CarPolice)
                        {
                            if (!GroupManager.HasJobCommand(base.Client, "arrest") || !base.Client.GetRoleplay().IsWorking)
                            {
                                base.Client.GetRoleplay().DrivingCar = false;
                                base.Client.GetRoleplay().CarEnableId = EffectsList.None;
                                RoleplayManager.Shout(base.Client, "*Abre la puerta y sale de su coche de policía*", 4);
                            }
                        }

                        // Si conduce en un lugar donde no es DRIVING
                        if (!base.Client.GetHabbo().CurrentRoom.DriveEnabled && base.Client.GetRoleplay().DrivingCar)
                        {
                            base.Client.GetRoleplay().DrivingInCar = true; // "Guardar auto para colocarlo al salir"
                            base.Client.GetRoleplay().DrivingCar = false;
                            base.Client.GetRoleplay().CarEnableId = EffectsList.None;
                            RoleplayManager.Shout(base.Client, "*Estaciona su vehículo afuera y entra al lugar*", 5);
                            base.Client.GetRoomUser().FastWalking = false;

                            // Si lleva pasajeros
                            #region Pasajeros
                            string Pasajeros = base.Client.GetRoleplay().Pasajeros;
                            string[] stringSeparators = new string[] { ";" };
                            string[] result;
                            result = Pasajeros.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);

                            foreach (string psjs in result)
                            {
                                GameClient PJ = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(psjs);
                                if (PJ != null)
                                {
                                    if (PJ.GetRoleplay().ChoferName == base.Client.GetHabbo().Username)
                                    {
                                        RoleplayManager.Shout(PJ, "*Baja del vehículo de " + base.Client.GetHabbo().Username + "*", 5);

                                        // FIX #1: Verificar GetRoomUser() del pasajero antes de acceder a X/Y
                                        if (PJ.GetRoomUser() != null)
                                        {
                                            if (!RoleplayManager.GenerateRoom(base.Client.GetRoomUser().RoomId, out Room Room))
                                                return;
                                            int NewX = PJ.GetRoomUser().X;
                                            int NewY = PJ.GetRoomUser().Y;

                                            // FIX #2: Verificar GetRoomItemHandler() y GetGameMap() antes de usarlos
                                            if (Room != null && Room.GetRoomItemHandler() != null && Room.GetGameMap() != null)
                                            {
                                                Room.SendMessage(Room.GetRoomItemHandler().UpdateUserOnRoller(
                                                    base.Client.GetRoomUser(),
                                                    new Point(NewX, NewY),
                                                    0,
                                                    Room.GetGameMap().SqAbsoluteHeight(NewX, NewY)));
                                            }
                                        }
                                    }

                                    // PASAJERO
                                    PJ.GetRoleplay().Pasajero = false;
                                    PJ.GetRoleplay().ChoferName = "";
                                    PJ.GetRoleplay().ChoferID = 0;
                                    PJ.GetRoomUser().CanWalk = true;
                                    PJ.GetRoomUser().FastWalking = false;
                                    PJ.GetRoomUser().TeleportEnabled = false;
                                    PJ.GetRoomUser().AllowOverride = false;

                                    // Descontamos Pasajero
                                    base.Client.GetRoleplay().PasajerosCount--;
                                    StringBuilder builder = new StringBuilder(base.Client.GetRoleplay().Pasajeros);
                                    builder.Replace(PJ.GetHabbo().Username + ";", "");
                                    base.Client.GetRoleplay().Pasajeros = builder.ToString();

                                    // CHOFER 
                                    base.Client.GetRoleplay().Chofer = (base.Client.GetRoleplay().PasajerosCount <= 0) ? false : true;
                                    base.Client.GetRoomUser().AllowOverride = (base.Client.GetRoleplay().PasajerosCount <= 0) ? false : true;

                                    // SI EL PASAJERO ES COMPAÑERO DE BASURERO
                                    if (PJ.GetRoleplay().IsBasuPasaj)
                                        PJ.GetRoleplay().IsBasuPasaj = false;

                                    PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(PJ, "event_vehicle", "close");
                                }
                            }
                            #endregion

                            // Cerramos ventana de combustible
                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(base.Client, "event_vehicle", "close");
                        }
                    }

                    if (base.Client.GetRoleplay().DrivingCar && base.Client.GetRoleplay().CarEnableId != EffectsList.HoverBoardWhite && !base.Client.GetRoleplay().IsFuelCharging)
                    {
                        int MaxGas = 0;

                        #region Vehicle Check
                        Vehicle vehicle = null;
                        int corp = 0;
                        bool ToDB = true;
                        foreach (Vehicle Vehicle in VehicleManager.Vehicles.Values)
                        {
                            if (base.Client.GetRoleplay().CarEnableId == Convert.ToInt32(Vehicle.EffectID))
                            {
                                vehicle = Vehicle;
                                MaxGas = vehicle.MaxFuel;
                                if (vehicle.CarCorp > 0)
                                {
                                    corp = vehicle.CarCorp;
                                    ToDB = false;
                                }
                            }
                        }
                        #endregion

                        // 60 segs. (30 mins aprox con vehículo encendido)
                        if (base.Client.GetRoleplay().CarTimer >= 60)
                        {
                            if (base.Client.GetRoleplay().CarType == 3)
                                base.Client.GetRoleplay().CarFuel -= 3;
                            else if (base.Client.GetRoleplay().CarType == 2)
                                base.Client.GetRoleplay().CarFuel -= 2;
                            else
                                base.Client.GetRoleplay().CarFuel -= 1;

                            base.Client.GetRoleplay().CarTimer = 0;
                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(base.Client, "event_vehicle", "open");
                        }

                        if (base.Client.GetRoleplay().CarFuel <= 0 || base.Client.GetRoleplay().CarLife <= 0)
                        {
                            if (base.Client.GetRoleplay().CarFuel < 0)
                                base.Client.GetRoleplay().CarFuel = 0;

                            if (base.Client.GetRoleplay().CarLife < 0)
                                base.Client.GetRoleplay().CarLife = 0;

                            #region Park
                            int ItemPlaceId = 0;
                            int roomid = base.Client.GetRoomUser().RoomId;
                            if (!RoleplayManager.GenerateRoom(roomid, out Room Room, false))
                                return;
                            VehiclesOwned VOD = null;
                            if (base.Client.GetRoleplay().DrivingInCar || !RoleplayManager.isValidPark(Room, base.Client.GetRoomUser().Coordinate))
                            {
                                if (base.Client.GetRoleplay().CarFuel <= 0)
                                    RoleplayManager.Shout(base.Client, "* Una Grúa se ha llevado el vehículo que " + base.Client.GetHabbo().Username + " conducía por encontrarse mal estacionado y sin combustible.", 4);
                                else
                                    RoleplayManager.Shout(base.Client, "* Una Grúa se ha llevado el vehículo que " + base.Client.GetHabbo().Username + " conducía por encontrarse mal estacionado y averiado", 4);

                                // Actualizamos datos del auto en el diccionario y DB
                                PolarEnvironment.GetGame().GetVehiclesOwnedManager().UpdateVehicleOwner(base.Client, 0, ToDB, out VOD);
                                ItemPlaceId = VOD != null ? VOD.Id : 0;
                                if (corp > 0)
                                {
                                    PolarEnvironment.GetGame().GetVehiclesOwnedManager().DeleteVehicleOwned(base.Client.GetRoleplay().DrivingCarId);
                                    RoleplayManager.CheckCorpCarp(base.Client);
                                }
                            }
                            else if (base.Client.GetRoleplay().DrivingCar)
                            {
                                // FIX #3: Verificar que PutItemToRoom no retorne null antes de acceder a .Id
                                base.Client.GetRoleplay().isParking = true;
                                HabboHotel.Items.Item ItemPlace = RoleplayManager.PutItemToRoom(
                                    base.Client,
                                    base.Client.GetRoleplay().DrivingCarItem,
                                    base.Client.GetRoomUser().RoomId,
                                    vehicle != null ? vehicle.ItemID : 0,
                                    base.Client.GetRoomUser().X,
                                    base.Client.GetRoomUser().Y,
                                    base.Client.GetRoomUser().RotBody,
                                    ToDB);
                                base.Client.GetRoleplay().isParking = false;

                                if (ItemPlace != null)
                                {
                                    ItemPlaceId = ItemPlace.Id;
                                    // Actualizamos datos del auto en el diccionario y DB
                                    PolarEnvironment.GetGame().GetVehiclesOwnedManager().UpdateVehicleOwner(base.Client, ItemPlaceId, ToDB, out VOD);
                                }
                                else
                                {
                                    // Si no se pudo colocar el item, actualizamos igualmente con ID 0
                                    PolarEnvironment.GetGame().GetVehiclesOwnedManager().UpdateVehicleOwner(base.Client, 0, ToDB, out VOD);
                                }
                            }

                            #region Extra Conditions & Checks
                            #region CorpCar Respawn
                            if (corp > 0)
                            {
                                base.Client.GetRoleplay().CarJobLastItemId = ItemPlaceId;
                            }
                            #endregion

                            #region Pasajeros (Algoritmo replicado en ConditionCheckTimer por seguridad)
                            string Pasajeros = base.Client.GetRoleplay().Pasajeros;
                            string[] stringSeparators = new string[] { ";" };
                            string[] result;
                            result = Pasajeros.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);

                            foreach (string psjs in result)
                            {
                                GameClient PJ = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(psjs);
                                if (PJ != null)
                                {
                                    if (PJ.GetRoleplay().ChoferName == base.Client.GetHabbo().Username)
                                    {
                                        RoleplayManager.Shout(PJ, "*Baja del vehículo de " + base.Client.GetHabbo().Username + "*", 5);
                                    }

                                    // PASAJERO
                                    PJ.GetRoleplay().Pasajero = false;
                                    PJ.GetRoleplay().ChoferName = "";
                                    PJ.GetRoleplay().ChoferID = 0;

                                    // FIX #4: Verificar GetRoomUser() del pasajero antes de acceder a sus propiedades
                                    if (PJ.GetRoomUser() != null)
                                    {
                                        PJ.GetRoomUser().CanWalk = true;
                                        PJ.GetRoomUser().FastWalking = false;
                                        PJ.GetRoomUser().TeleportEnabled = false;
                                        PJ.GetRoomUser().AllowOverride = false;
                                    }

                                    // Descontamos Pasajero
                                    base.Client.GetRoleplay().PasajerosCount--;
                                    StringBuilder builder = new StringBuilder(base.Client.GetRoleplay().Pasajeros);
                                    builder.Replace(PJ.GetHabbo().Username + ";", "");
                                    base.Client.GetRoleplay().Pasajeros = builder.ToString();

                                    // CHOFER 
                                    base.Client.GetRoleplay().Chofer = (base.Client.GetRoleplay().PasajerosCount <= 0) ? false : true;
                                    base.Client.GetRoomUser().AllowOverride = (base.Client.GetRoleplay().PasajerosCount <= 0) ? false : true;

                                    // SI EL PASAJERO ES COMPAÑERO DE BASURERO
                                    if (PJ.GetRoleplay().IsBasuPasaj)
                                        PJ.GetRoleplay().IsBasuPasaj = false;

                                    PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(PJ, "event_vehicle", "close");
                                }
                            }
                            #endregion
                            #endregion

                            #region Online ParkVars
                            // Retornamos a valores predeterminados
                            base.Client.GetRoleplay().DrivingCar = false;
                            base.Client.GetRoleplay().DrivingInCar = false;
                            base.Client.GetRoleplay().DrivingCarId = 0;

                            // Combustible System
                            base.Client.GetRoleplay().CarType = 0;
                            base.Client.GetRoleplay().CarFuel = 0;
                            base.Client.GetRoleplay().CarMaxFuel = 0;
                            base.Client.GetRoleplay().CarTimer = 0;
                            base.Client.GetRoleplay().CarLife = 0;

                            base.Client.GetRoleplay().CarEnableId = 0;
                            base.Client.GetRoleplay().CarEffectId = 0;
                            base.Client.GetRoomUser().ApplyEffect(0);
                            base.Client.GetRoomUser().FastWalking = false;
                            #endregion

                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(base.Client, "event_vehicle", "close");
                            #endregion
                        }
                    }

                    if (base.Client.GetRoleplay().DrivingCar && Effect != base.Client.GetRoleplay().CarEnableId)
                        base.Client.GetRoomUser().ApplyEffect(base.Client.GetRoleplay().CarEnableId);

                    if (!base.Client.GetRoleplay().DrivingCar)
                    {
                        base.Client.GetRoleplay().CarTimer = 0;

                        if (Effect != EffectsList.None)
                            base.Client.GetRoomUser().ApplyEffect(EffectsList.None);

                        if (base.Client.GetRoomUser().FastWalking)
                            base.Client.GetRoomUser().FastWalking = false;

                        if (base.Client.GetRoomUser().AllowOverride)
                            base.Client.GetRoomUser().AllowOverride = false;

                        if (base.Client.GetRoleplay().CooldownManager.ActiveCooldowns.ContainsKey("car"))
                            base.Client.GetRoleplay().CooldownManager.ActiveCooldowns["car"].Amount = 90;
                        else
                            base.Client.GetRoleplay().CooldownManager.CreateCooldown("car", 1000, 90);
                    }
                }
                else if (base.Client.GetRoleplay().Pasajero)
                {
                    if (base.Client.GetRoomUser() != null)
                    {
                        if (base.Client.GetRoomUser().CanWalk)
                            base.Client.GetRoomUser().CanWalk = false;
                        if (!base.Client.GetRoomUser().FastWalking)
                            base.Client.GetRoomUser().FastWalking = true;
                        if (!base.Client.GetRoomUser().TeleportEnabled)
                            base.Client.GetRoomUser().TeleportEnabled = true;
                        if (!base.Client.GetRoomUser().AllowOverride)
                            base.Client.GetRoomUser().AllowOverride = true;
                    }
                }
                else if (base.Client.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("repair") || base.Client.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("capture"))
                {
                    base.Client.GetRoomUser().ApplyEffect(EffectsList.SunnyD);
                    return;
                }
                else if (Healing)
                {
                    if (base.Client.GetRoleplay().EquippedWeapon != null)
                    {
                        if (Equipped && Item != base.Client.GetRoleplay().EquippedWeapon.HandItem)
                            base.Client.GetRoomUser().CarryItem(0);
                    }
                    if (Effect != EffectsList.GreenGlow)
                        base.Client.GetRoomUser().ApplyEffect(EffectsList.GreenGlow);
                    return;
                }
                else if (base.Client.GetRoleplay().IsWorkingOut)
                    return;
                else if (base.Client.GetRoleplay().TextTimer > 0 || base.Client.GetRoleplay().UsingPhone)
                {
                    if (Effect == EffectsList.CellPhone)
                    {
                        if (base.Client.GetRoleplay().TextTimer == 1)
                        {
                            base.Client.GetRoleplay().TextTimer = 0;

                            if (base.Client.GetRoomUser() != null)
                                base.Client.GetRoomUser().ApplyEffect(EffectsList.None);
                        }
                        else
                            base.Client.GetRoleplay().TextTimer--;
                    }
                    else
                        base.Client.GetRoleplay().TextTimer = 0;
                }
                else if (base.Client.GetRoleplay().SexTimer > 0)
                {
                    if (Effect == EffectsList.RunningMan)
                    {
                        if (base.Client.GetRoleplay().SexTimer == 1)
                        {
                            base.Client.GetRoleplay().SexTimer = 0;

                            if (base.Client.GetRoomUser() != null)
                                base.Client.GetRoomUser().ApplyEffect(EffectsList.None);

                            base.Client.GetHabbo().Poof(true);
                        }
                        else
                            base.Client.GetRoleplay().SexTimer--;
                    }
                    else
                    {
                        base.Client.GetRoleplay().SexTimer = 0;
                        base.Client.GetHabbo().Poof(true);
                    }
                }
                else if (base.Client.GetRoleplay().RapeTimer > 0)
                {
                    if (Effect == EffectsList.Twinkle)
                    {
                        if (base.Client.GetRoleplay().RapeTimer == 1)
                        {
                            base.Client.GetRoleplay().RapeTimer = 0;

                            if (base.Client.GetRoomUser() != null)
                                base.Client.GetRoomUser().ApplyEffect(EffectsList.None);
                        }
                        else
                            base.Client.GetRoleplay().RapeTimer--;
                    }
                    else
                        base.Client.GetRoleplay().RapeTimer = 0;
                }
                else if (base.Client.GetRoleplay().KissTimer > 0)
                {
                    if (Effect == EffectsList.Love)
                    {
                        if (base.Client.GetRoleplay().KissTimer == 1)
                        {
                            base.Client.GetRoleplay().KissTimer = 0;

                            if (base.Client.GetRoomUser() != null)
                                base.Client.GetRoomUser().ApplyEffect(EffectsList.None);
                        }
                        else
                            base.Client.GetRoleplay().KissTimer--;
                    }
                    else
                        base.Client.GetRoleplay().KissTimer = 0;
                }
                else if (base.Client.GetRoleplay().HugTimer > 0)
                {
                    if (Effect == EffectsList.Love)
                    {
                        if (base.Client.GetRoleplay().HugTimer == 1)
                        {
                            base.Client.GetRoleplay().HugTimer = 0;

                            if (base.Client.GetRoomUser() != null)
                                base.Client.GetRoomUser().ApplyEffect(EffectsList.None);
                        }
                        else
                            base.Client.GetRoleplay().HugTimer--;
                    }
                    else
                        base.Client.GetRoleplay().HugTimer = 0;
                }
                else if (Effect == EffectsList.HorseRiding && base.Client.GetRoomUser().RidingHorse)
                    return;
                else if ((Effect == EffectsList.Dizzy || Effect == EffectsList.Ice) && base.Client.GetRoomUser().Frozen)
                    return;
                else if (base.Client.GetRoleplay().InsideTaxi)
                {
                    if (GroupManager.HasJobCommand(base.Client, "guide") && base.Client.GetRoleplay().IsWorking)
                    {
                        if (Effect != EffectsList.PoliceTaxi)
                            base.Client.GetRoomUser().ApplyEffect(EffectsList.PoliceTaxi);
                    }
                    return;
                }
                else if (base.Client.GetRoleplay().Cuffed)
                {
                    if (Effect != EffectsList.Cuffed)
                        base.Client.GetRoomUser().ApplyEffect(EffectsList.Cuffed);
                    return;
                }
                else if (Equipped)
                {
                    if (base.Client.GetRoleplay() != null && base.Client.GetRoleplay().EquippedWeapon != null && base.Client.GetRoomUser() != null)
                    {
                        if (base.Client.GetRoleplay().EquippedWeapon.EffectID > 0 && base.Client.GetRoomUser().CurrentEffect != base.Client.GetRoleplay().EquippedWeapon.EffectID)
                            base.Client.GetRoomUser().ApplyEffect(base.Client.GetRoleplay().EquippedWeapon.EffectID);
                    }
                    if (base.Client.GetRoleplay() != null && base.Client.GetRoleplay().EquippedWeapon != null && base.Client.GetRoomUser() != null)
                    {
                        if (base.Client.GetRoleplay().EquippedWeapon.HandItem > 0 && base.Client.GetRoomUser().CarryItemID != base.Client.GetRoleplay().EquippedWeapon.HandItem)
                            base.Client.GetRoomUser().CarryItem(base.Client.GetRoleplay().EquippedWeapon.HandItem);
                    }
                    return;
                }
                else if (Farming)
                {
                    if (Effect != EffectsList.WateringCan)
                        base.Client.GetRoomUser().ApplyEffect(EffectsList.WateringCan);
                    return;
                }
                else if (Hygiene)
                {
                    if (Effect != EffectsList.Flies)
                        base.Client.GetRoomUser().ApplyEffect(EffectsList.Flies);
                    return;
                }
                else if (Poop)
                {
                    if (Effect != EffectsList.Flies)
                        base.Client.GetRoomUser().ApplyEffect(EffectsList.Flies);
                    return;
                }
                else if (base.Client.GetRoleplay().IsWorking && GroupManager.HasJobCommand(base.Client, "guide"))
                {
                    if (Effect != EffectsList.HoloRPPolice)
                        base.Client.GetRoomUser().ApplyEffect(EffectsList.HoloRPPolice);
                    return;
                }
                else if (base.Client.GetRoleplay().IsWorking && GroupManager.HasJobCommand(base.Client, "heal"))
                {
                    if (Effect != EffectsList.Medic)
                        base.Client.GetRoomUser().ApplyEffect(EffectsList.Medic);
                    return;
                }
                #endregion
            }
            catch (Exception e)
            {
                Logging.LogRPTimersError("Error in Execute() void: " + e);
            }
        }
    }
}