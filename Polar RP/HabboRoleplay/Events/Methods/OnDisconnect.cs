using System;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;
using Polar.Core;
using Polar.HabboHotel.Guides;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Rooms;
using System.Collections.Generic;
using System.Linq;
using Polar.Communication.Packets.Outgoing.Guides;
using Polar.HabboRoleplay.Gambling;
using Polar.HabboRoleplay.VehicleOwned;
using Polar.HabboRoleplay.Vehicles;
using System.Drawing;
using System.Text;
using Polar.HabboHotel.Users.Effects;
using Polar.Database.Interfaces;
using Polar.HabboRoleplay.RoleplayUsers;

namespace Polar.HabboRoleplay.Events.Methods
{
    /// <summary>
    /// Triggered when the user disconnects
    /// </summary>
    public class OnDisconnect : IEvent
    {
        /// <summary>
        /// Responds to the event
        /// </summary>
        public void Execute(object Source, object[] Params)
        {
            GameClient Client = (GameClient)Source;

            if (Client == null)
                return;

            if (Client.GetRoleplay() == null)
                return;

            if (Client.GetHabbo() == null)
                return;

            if (Client.GetHabbo()._disconnected)
                return;

            if (Client.GetRoleplay().HechizoHealth > 0)
            {

                // Client.GetRoleplay().MaxHealth -= Client.GetRoleplay().HechizoHealth;
                if (Client.GetRoleplay().CurHealth > 200)
                {
                    Client.GetRoleplay().CurHealth -= Client.GetRoleplay().HechizoHealth;
                }
                else if (Client.GetRoleplay().CurHealth < 100)
                {
                    Client.GetRoleplay().CurHealth -= 50;
                }
                Client.GetRoleplay().HechizoHealth = 0;
            }

            Client.GetRoleplay().IsDisconnecting = true;

            RoleplayManager.Shout(Client, "((Se ha desconectado. Desaparecerá en 10 segundos.))", 7);
            Client.GetHabbo().Effects().ApplyEffect(108);

            if (RoleplayManager.InvitedUsersToJuryDuty.Contains(Client))
                RoleplayManager.InvitedUsersToJuryDuty.Remove(Client);


            //Client.GetRoleplay().CloseInteractingUserDialogues();

            var GuideManager = PolarEnvironment.GetGame().GetGuideManager();
            if (GuideManager != null && Client != null)
            {
                if (GuideManager.AllPolice.Contains(Client))
                    GuideManager.AllPolice.Remove(Client);
                if (GuideManager.GuardiansOnDuty.Contains(Client))
                    GuideManager.GuardiansOnDuty.Remove(Client);
                if (GuideManager.GuidesOnDuty.Contains(Client))
                    GuideManager.GuidesOnDuty.Remove(Client);
                if (GuideManager.HelpersOnDuty.Contains(Client))
                    GuideManager.HelpersOnDuty.Remove(Client);

                #region End Existing Calls
                if (Client.GetRoleplay() != null && Client.GetRoleplay().GuideOtherUser != null)
                {
                    Client.GetRoleplay().GuideOtherUser.SendMessage(new OnGuideSessionDetachedComposer(0));
                    Client.GetRoleplay().GuideOtherUser.SendMessage(new OnGuideSessionDetachedComposer(1));
                    if (Client.GetRoleplay().GuideOtherUser.GetRoleplay() != null)
                    {
                        Client.GetRoleplay().GuideOtherUser.GetRoleplay().Sent911Call = false;
                        Client.GetRoleplay().GuideOtherUser.GetRoleplay().GuideOtherUser = null;
                    }

                    Client.GetRoleplay().GuideOtherUser = null;
                    Client.SendMessage(new OnGuideSessionDetachedComposer(0));
                    Client.SendMessage(new OnGuideSessionDetachedComposer(1));
                }
                #endregion
            }

            if (RoleplayData.GetData("farming", "room") != null)
            {
                int RoomId = Convert.ToInt32(RoleplayData.GetData("farming", "room"));
                if (RoleplayManager.GenerateRoom(RoomId, out Room Room) && Room.GetRoomItemHandler() != null && Room.GetRoomItemHandler().GetFloor != null)
                {
                    List<Item> Items = Room.GetRoomItemHandler().GetFloor.Where(x => x.GetBaseItem().InteractionType == InteractionType.FARMING && x.FarmingData != null && x.FarmingData.OwnerId == Client.GetHabbo().Id).ToList();

                    foreach (Item Item in Items)
                    {
                        Room.GetRoomItemHandler().RemoveFurniture(null, Item.Id);
                    }
                }
            }

            //Client.GetRoleplay().EndCycle();

            if (RoleplayManager.WantedList != null)
            {
                if (RoleplayManager.WantedList.ContainsKey(Client.GetHabbo().Id))
                {
                    Wanted Junk;
                    RoleplayManager.WantedList.TryRemove(Client.GetHabbo().Id, out Junk);
                }
            }

            #region CheckIsWorking
            if (Client.GetRoleplay().IsWorking)
                WorkManager.RemoveWorkerFromList(Client);
            #endregion

            #region CheckHouseApart
            CheckHouseApart(Client);
            #endregion

            #region CheckWeapons
            CheckWeapons(Client);
            #endregion

            #region CheckOnGrua
            PolarEnvironment.GetGame().GetVehiclesOwnedManager().UpdateVehicleGruaBug(Client.GetHabbo().Id, out VehiclesOwned VOD);
            #endregion

            #region CheckBasurero
            RoleplayManager.CheckBasurero(Client);
            #endregion

            #region CheckCorpCarp
            RoleplayManager.CheckCorpCarp(Client);
            #endregion

            #region PasajeroCheck
            PasajeroCheck(Client);
            #endregion

            #region CheckDriving 
            CheckDriving(Client);
            #endregion

            #region ChoferCheck
            ChoferCheck(Client);
            #endregion

            #region CheckAntiLaw
            RoleplayManager.CheckAntiLaw(Client);
            #endregion

            if (Client.GetRoleplay().UserDataHandler != null)
            {
                Client.GetRoleplay().UserDataHandler = new UserDataHandler(Client, Client.GetRoleplay());
                Client.GetRoleplay().UserDataHandler.SaveFarmingData();
                Client.GetRoleplay().UserDataHandler.SaveCooldownData();
                Client.GetRoleplay().UserDataHandler.SaveData();
                Client.GetRoleplay().UserDataHandler = null;
            }

            RoleplayManager.UpdateMyProductExtrada(Client, RoleplayManager.BidonID, Client.GetRoleplay().Bidon.ToString());
            RoleplayManager.UpdateMyProductExtrada(Client, RoleplayManager.MecPartsID, Client.GetRoleplay().MecParts.ToString());
            RoleplayManager.UpdateMyProductExtrada(Client, RoleplayManager.ArmMatID, Client.GetRoleplay().ArmMat.ToString());
            RoleplayManager.UpdateMyProductExtrada(Client, RoleplayManager.ArmPiecesID, Client.GetRoleplay().ArmPieces.ToString());

            if (Client.GetRoleplay().BattlePassData != null)
             {
                using (var DB = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    PolarEnvironment.GetGame().GetBattlePassManager().SaveUserData(Client.GetRoleplay().BattlePassData, DB);
                }
              }

            // Repeat this cuz' de Roleplay conditions can modify some vars from GetHabbo().


            // Websockets
            Client.GetRoleplay().CloseInteractingUserDialogues();
            Client.GetRoleplay().EndCycle();
            //PolarEnvironment.GetGame().GetWebEventManager().CloseSocketByGameClient(((Client.GetHabbo() == null) ? 0 : Client.GetHabbo().Id));
            Logging.WriteLine(Client.GetHabbo().Username + " se ha Desconectado.", ConsoleColor.DarkGray);
        }

        #region CheckHouseApart
        public void CheckHouseApart(GameClient Client)
        {
            if (Client.GetRoomUser() != null)
            {
                var HouseInside = PolarEnvironment.GetGame().GetHouseManager().GetHouseByInsideRoom(Client.GetRoomUser().RoomId);

                if (HouseInside != null)//Estamos dentro de una casa
                {
                    Client.GetRoleplay().LastCoordinates = HouseInside.DoorX + "," + HouseInside.DoorY + "," + HouseInside.DoorZ + "," + 0;
                    Client.GetHabbo().HomeRoom = HouseInside.RoomId;
                }
                else
                {
                    var ApartInside = PolarEnvironment.GetGame().GetApartmentOwnedManager().GetApartmentByInsideRoom(Client.GetRoomUser().RoomId);

                    if (ApartInside != null)
                    {
                        Client.GetRoleplay().LastCoordinates = 0 + "," + 0 + "," + 0 + "," + 0;
                        Client.GetHabbo().HomeRoom = ApartInside.LobbyId;
                    }
                    else
                    {
                        Client.GetRoleplay().LastCoordinates = Client.GetRoomUser().X + "," + Client.GetRoomUser().Y + "," + Client.GetRoomUser().Z + "," + Client.GetRoomUser().RotBody;
                        Client.GetHabbo().HomeRoom = Client.GetRoomUser().RoomId;
                    }
                }
            }
        }
        #endregion

        #region CheckWeapons
        public void CheckWeapons(GameClient Client)
        {
            if (Client.GetRoleplay().EquippedWeapon != null)
            {
                RoleplayManager.UpdateMyWeaponStats(Client, "bullets", Client.GetRoleplay().Bullets, Client.GetRoleplay().EquippedWeapon.Name);
                RoleplayManager.UpdateMyWeaponStats(Client, "life", Client.GetRoleplay().WLife, Client.GetRoleplay().EquippedWeapon.Name);
            }
        }
        #endregion

        #region PasajeroCheck
        public void PasajeroCheck(HabboHotel.GameClients.GameClient Client)
        {
            if (!Client.GetRoleplay().Pasajero)
                return;

            GameClient Chofer = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Client.GetRoleplay().ChoferName);
            if (Chofer != null)
            {
                // Descontamos Pasajero
                Chofer.GetRoleplay().PasajerosCount--;
                Chofer.GetRoleplay().Pasajeros.Replace(Client.GetHabbo().Username + ";", "");

                // CHOFER 
                Chofer.GetRoleplay().Chofer = (Client.GetRoleplay().PasajerosCount <= 0) ? false : true;
                Chofer.GetRoomUser().AllowOverride = (Client.GetRoleplay().PasajerosCount <= 0) ? false : true;
            }
        }
        #endregion

        #region ChoferCheck
        public void ChoferCheck(HabboHotel.GameClients.GameClient Client)
        {
            if (!Client.GetRoleplay().Chofer)
                return;

            if (Client.GetRoleplay().IsBasuChofer)
                Client.GetRoleplay().IsBasuChofer = false;

            //Si lleva pasajeros
            #region Pasajeros
            //Vars
            string Pasajeros = Client.GetRoleplay().Pasajeros;
            string[] stringSeparators = new string[] { ";" };
            string[] result;
            result = Pasajeros.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);

            foreach (string psjs in result)
            {
                HabboHotel.GameClients.GameClient PJ = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(psjs);
                if (PJ != null)
                {
                    if (PJ.GetRoleplay().ChoferName == Client.GetHabbo().Username)
                    {
                        HabboRoleplay.Misc.RoleplayManager.Shout(PJ, "*Baja del vehículo de " + Client.GetHabbo().Username + "*", 5);
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
                    Client.GetRoleplay().PasajerosCount--;
                    Client.GetRoleplay().Pasajeros.Replace(PJ.GetHabbo().Username + ";", "");

                    // CHOFER 
                    Client.GetRoleplay().Chofer = (Client.GetRoleplay().PasajerosCount <= 0) ? false : true;
                    Client.GetRoomUser().AllowOverride = (Client.GetRoleplay().PasajerosCount <= 0) ? false : true;

                    // SI EL PASAJERO ES COMPAÑERO DE BASURERO
                    if (PJ.GetRoleplay().IsBasuPasaj)
                        PJ.GetRoleplay().IsBasuPasaj = false;

                    PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(PJ, "event_vehicle", "close");// WS FUEL
                }
            }
            #endregion
        }
        #endregion

        #region CheckDriving
        public void CheckDriving(GameClient Client)
        {
            if (Client.GetRoleplay().DrivingCar)
            {
                #region Vehicle Check
                Vehicle vehicle = null;
                int corp = 0;
                bool ToDB = true;
                foreach (Vehicle Vehicle in VehicleManager.Vehicles.Values)
                {
                    if (Client.GetRoleplay().CarEnableId == Convert.ToInt32(Vehicle.EffectID))
                    {
                        vehicle = Vehicle;
                        if (vehicle.CarCorp > 0)
                        {
                            corp = vehicle.CarCorp;
                            ToDB = false;
                        }
                    }
                }
                #endregion

                int ItemPlaceId = 0;
                int roomid = Client.GetHabbo().HomeRoom;
                if (!RoleplayManager.GenerateRoom(roomid, out Room Room, false))
                    return;

                object[] Coords = Client.GetRoleplay().LastCoordinates.Split(',');
                Point SCoords = new Point(Convert.ToInt32(Coords[0]), Convert.ToInt32(Coords[1]));

                if (Client.GetRoleplay().DrivingInCar || !RoleplayManager.isValidPark(Room, SCoords))
                {
                    RoleplayManager.Shout(Client, "* Una Grúa se ha llevado el vehículo que " + Client.GetHabbo().Username + " conducía por encontrarse mal estacionado.", 4);
                    // Actualizamos datos del auto en el diccionario y DB
                    VehiclesOwned VOD;
                    PolarEnvironment.GetGame().GetVehiclesOwnedManager().UpdateVehicleOwnerDisc(Client, 0, ToDB, roomid, Convert.ToInt32(Coords[0]), Convert.ToInt32(Coords[1]), Convert.ToInt32(Coords[3]), out VOD);
                    if (VOD != null)
                        ItemPlaceId = VOD.Id;

                    if (corp > 0)
                    {
                        PolarEnvironment.GetGame().GetVehiclesOwnedManager().DeleteVehicleOwned(Client.GetRoleplay().DrivingCarId);
                        RoleplayManager.CheckCorpCarp(Client);
                    }

                }
                else if (Client.GetRoleplay().DrivingCar)
                {
                    if (corp > 0)
                    {
                        RoleplayManager.Shout(Client, "* Una Grúa se ha llevado el vehículo que " + Client.GetHabbo().Username + " conducía por encontrarse mal estacionado.", 4);
                        PolarEnvironment.GetGame().GetVehiclesOwnedManager().DeleteVehicleOwned(Client.GetRoleplay().DrivingCarId);
                        RoleplayManager.CheckCorpCarp(Client);
                    }
                    else
                    {
                        // Colocamos Furni en Sala
                        Client.GetRoleplay().isParking = true;
                        Item ItemPlace = RoleplayManager.PutItemToRoom(Client, Client.GetRoleplay().DrivingCarItem, roomid, vehicle.ItemID, Convert.ToInt32(Coords[0]), Convert.ToInt32(Coords[1]), Convert.ToInt32(Coords[3]), ToDB);
                        //Item ItemPlace = RoleplayManager.PlaceItemToRoom(Client, vehicle.ItemID, 0, Convert.ToInt32(Coords[0]), Convert.ToInt32(Coords[1]), Convert.ToInt32(Coords[2]), Convert.ToInt32(Coords[3]), false, Room.Id, ToDB, "");
                        Client.GetRoleplay().isParking = false;
                        if (ItemPlace != null)
                            ItemPlaceId = ItemPlace.Id;
                        else
                            ItemPlaceId = 0;
                        // Actualizamos datos del auto en el diccionario y DB
                        VehiclesOwned VOD;
                        PolarEnvironment.GetGame().GetVehiclesOwnedManager().UpdateVehicleOwnerDisc(Client, ItemPlaceId, ToDB, roomid, Convert.ToInt32(Coords[0]), Convert.ToInt32(Coords[1]), Convert.ToInt32(Coords[3]), out VOD);
                    }
                }

                #region Extra Conditions & Checks
                #region CorpCar Respawn
                if (corp > 0)
                {
                    Client.GetRoleplay().CarJobLastItemId = ItemPlaceId;
                }
                #endregion

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
                        if (PJ.GetRoleplay().ChoferName == Client.GetHabbo().Username)
                        {
                            RoleplayManager.Shout(PJ, "*Baja del vehículo de " + Client.GetHabbo().Username + "*", 5);
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
                        Client.GetRoleplay().PasajerosCount--;
                        StringBuilder builder = new StringBuilder(Client.GetRoleplay().Pasajeros);
                        builder.Replace(PJ.GetHabbo().Username + ";", "");
                        Client.GetRoleplay().Pasajeros = builder.ToString();

                        // CHOFER 
                        Client.GetRoleplay().Chofer = (Client.GetRoleplay().PasajerosCount <= 0) ? false : true;
                        //Client.GetRoomUser().AllowOverride = (Client.GetRoleplay().PasajerosCount <= 0) ? false : true;

                    }
                }
                #endregion

                #region Check Jobs

                #region Basurero
                if (Client.GetRoleplay().BasuTeamId <= 0)
                    Client.GetRoleplay().IsBasuChofer = false;
                #endregion

                #endregion

                #endregion

                #region Online ParkVars
                //Retornamos a valores predeterminados
                Client.GetRoleplay().DrivingCar = false;
                Client.GetRoleplay().DrivingInCar = false;
                Client.GetRoleplay().DrivingCarId = 0;// Id de VehiclesOwned;

                //Combustible System
                Client.GetRoleplay().CarType = 0;// Define el gasto de combustible
                Client.GetRoleplay().CarFuel = 0;
                Client.GetRoleplay().CarMaxFuel = 0;
                Client.GetRoleplay().CarTimer = 0;
                Client.GetRoleplay().CarLife = 0;

                Client.GetRoleplay().CarEnableId = 0;//Coloca el enable para conducir
                Client.GetRoleplay().CarEffectId = 0;//Guarda el enable del último auto en conducción.
                //Client.GetRoomUser().ApplyEffect(0);
                //Client.GetRoomUser().FastWalking = false;
                #endregion
            }
        }
        #endregion

    }
}