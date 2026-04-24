using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.Utilities;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Food;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using Polar.Communication.Packets.Outgoing.Notifications;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboRoleplay.Combat;
using Polar.HabboRoleplay.Vehicles;
using Polar.HabboRoleplay.VehicleOwned;
using System.Collections.Concurrent;
using Polar.HabboRoleplay.Bots.Manager.TimerHandlers;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace Polar.HabboRoleplay.Bots.Types
{
    public class RepairServerBot : RoleplayBotAI
    {
        int VirtualId;
        public bool CheckForOtherWorkers;
        public int OnDutyCheckInterval;
        public int CurOnDutyCheckTime;
        public RepairServerBot(int virtualId)
        {
            this.OnDuty = true;
            this.CheckForOtherWorkers = true;
            this.CurOnDutyCheckTime = 0;
            this.VirtualId = virtualId;
        }

        public override void OnDeployed(GameClient Client)
        {
            this.OnDuty = false;
            this.StartActivities();

            
        }

        public override void OnDeath(GameClient Client)
        {

        }

        public override void OnArrest(GameClient Client)
        {

        }

        public override void OnAttacked(GameClient Client)
        {

        }

        public override void OnUserLeaveRoom(GameClient Client)
        {
            if (!this.OnDuty)
                return;


        }

        public override void OnUserEnterRoom(GameClient Client)
        {/*
            if (!OnDuty)
                return;*/

            /*var UserPoint = new System.Drawing.Point(Client.GetRoomUser().X + 1, Client.GetRoomUser().Y + 2);
            var ServePoint = new System.Drawing.Point(Client.GetRoomUser().SquareBehind.X, Client.GetRoomUser().SquareBehind.Y);

            object[] Params = { Client, ServePoint, UserPoint };


            this.GetRoomUser().MoveTo(UserPoint);*/


            
        }

        public override void OnUserUseTeleport(GameClient Client, object[] Params)
        {
            if (!this.OnDuty)
                return;

            if (Client == null) return;
            if (Client.GetRoomUser() == null) return;

            if (Client == this.GetBotRoleplay().UserFollowing || Client == this.GetBotRoleplay().UserAttacking)
                this.GetBotRoleplay().StartTeleporting(this.GetRoomUser(), GetRoom(), Params);
        }

        public override void OnUserSay(RoomUser User, string Message)
        {
            if (!OnDuty)
                return;

            GameClient Client = User.GetClient();

            if (Client == null)
                return;

            this.HandleRequest(Client, Message);
        }

        public override void OnUserShout(RoomUser User, string Message)
        {
            if (!OnDuty)
                return;

            if (User.GetClient() == null)
                return;

            this.HandleRequest(User.GetClient(), Message);
        }

        public override void OnMessaged(GameClient Client, string Message)
        {
            if (!OnDuty)
                return;
        }


        public override void HandleRequest(GameClient Client, string Message)
        {
            if (!this.OnDuty)
                return;

            if (this.RespondToSpeech(Client, Message))
                return;

            string Name = this.GetBotRoleplay().Name.ToLower();

            string[] keys = new string[] { "aceptar reparacion", "aceptar" };
            string sKeyResult = keys.FirstOrDefault<string>(s => Message.Contains(s));
            Room Room = Client.GetHabbo().CurrentRoom;
            //

            if (Message.ToLower() == Name)
            {
                GetRoomUser().Chat("Hey " + Client.GetHabbo().Username + ", ¿Necesitas ayuda?", true);
            }
            /*else if (Message.ToLower() == "revisar")
            {
                var UserPoint = new System.Drawing.Point(Client.GetRoomUser().X + 1, Client.GetRoomUser().Y + 2);
                var ServePoint = new System.Drawing.Point(Client.GetRoomUser().SquareBehind.X, Client.GetRoomUser().SquareBehind.Y);
                var local = Client.GetRoomUser().SquareInFront;
                object[] Params = { Client, ServePoint, UserPoint };


                this.GetRoomUser().MoveTo(local);

                new Thread(() =>
                {
                    Thread.Sleep(6500);
                    #region Check Vehicle InFront
                    int FuelSizea = 0;
                Vehicle vehiclea = null;
                bool founda = false;
                int itemfurnia = 0, corpa = 0;
                Item BTilea = null;
                string itemnma = null;
                foreach (Vehicle Vehicle in VehicleManager.Vehicles.Values)
                {
                    if (!founda)
                    {
                        BTilea = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == Vehicle.ItemName && x.Coordinate == Client.GetRoomUser().SquareInFront);
                        if (BTilea != null)
                        {
                            vehiclea = Vehicle;
                            itemfurnia = BTilea.Id;
                            itemnma = Vehicle.ItemName;
                            corpa = Convert.ToInt32(Vehicle.CarCorp);
                            founda = true;
                        }
                    }
                }

                /*if (!founda)
                {
                    Client.SendWhisper("¡Debes estar frente al vehículo a revisar!", 1);
                    return;
                }

                #endregion

                #region Select Vehicle State
                int stateas = 0;
                List<VehiclesOwned> VOas = PolarEnvironment.GetGame().GetVehiclesOwnedManager().getVehiclesOwnedByFurniId(itemfurnia);
                if (VOas == null || VOas.Count <= 0)
                {
                    Client.SendWhisper("Este vehículo no necesita ser reparado.", 1);
                    return;
                }
                stateas = VOas[0].State;
                #endregion

                #region Check Vehicle State
                if (stateas != 2 && stateas != 3 && VOas[0].CarLife > 0)
                {
                    Client.SendWhisper("Este vehículo no necesita ser reparado.", 1);
                    return;
                }
                #endregion

                #region Calc Repair Kit Cant
                int MecParts = 0;
                if (FuelSizea <= 90)// Tanque Pequeño
                {
                    MecParts = 3;
                }
                else if (FuelSizea > 90 && FuelSizea <= 100)// Tanque Mediano
                {
                    MecParts = 6;
                }
                else // Tanque Grande
                {
                    MecParts = 9;
                }
                #endregion

                #region Execute
                GetRoomUser().Chat("*Abre el capó del Vehículo y procede a revisarlo*", true);
                Client.SendWhisper("Este vehículo requiere " + MecParts + " repuestos para ser reparado. Escribe 'comprar repuestos' para comprar los repuestos.", 1);
                Client.GetRoleplay().CooldownManager.CreateCooldown("reviewmec", 1000, 3);
                    #endregion

                    Thread.Sleep(2000);
                    //RoleplayManager.CalledDelivery = false;
                }).Start();

            }*/
            else if (Message.ToLower() == "reparar" || Message.ToLower() == "revisar")
            {
               
                if (Message.ToLower() == "revisar")
                {
                    var UserPoint = new System.Drawing.Point(Client.GetRoomUser().X + 1, Client.GetRoomUser().Y + 2);
                    var ServePoint = new System.Drawing.Point(Client.GetRoomUser().SquareBehind.X, Client.GetRoomUser().SquareBehind.Y);
                    var local = Client.GetRoomUser().SquareInFront;
                    object[] Params = { Client, ServePoint, UserPoint };

                    this.GetRoomUser().MoveTo(local);

                    Task.Run(async delegate
                    {
                        await Task.Delay(6500);

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
                                BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == Vehicle.ItemName);
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

                        /*if (!found)
                        {
                            Client.SendWhisper("¡Debes estar frente al vehículo a reparar!", 1);
                            return;
                        }*/

                        #endregion

                        #region Select Vehicle State
                        int state = 0;
                        List<VehiclesOwned> VO = PolarEnvironment.GetGame().GetVehiclesOwnedManager().getVehiclesOwnedByFurniId(itemfurni);
                        if (VO == null || VO.Count <= 0)
                        {
                            RoleplayManager.Shout(Client, "* Una grúa ha pasado a recoger el vehículo que " + Client.GetHabbo().Username + " intentaba reparar.");
                            RoleplayManager.PickItem(Client, itemfurni);
                            
                                await Task.Delay(2000);
                                Room.GetRoomUserManager().RemoveBot(VirtualId, false);
                            
                            return;
                        }
                        state = VO[0].State;
                        #endregion

                        #region Check Vehicle State
                        if (state == 2 || state == 3 || VO[0].CarLife <= 0)
                        {
                            if (state == 3)
                                Client.GetRoleplay().MecNewState = 1;// óptimo y con traba
                            else
                                Client.GetRoleplay().MecNewState = 0;// óptimo y sin traba
                        }
                        else
                        {
                            Client.SendWhisper("Este vehículo no necesita ser reparado.", 1);
                            
                                await Task.Delay(2000);
                                Room.GetRoomUserManager().RemoveBot(VirtualId, false);
                            
                            return;
                        }
                        #endregion

                        #region Calc Repair Kit Cant
                        if (FuelSize <= 90)// Tanque Pequeño
                        {
                            Client.GetRoleplay().MecPartsTo = 3;
                        }
                        else if (FuelSize > 90 && FuelSize <= 100)// Tanque Mediano
                        {
                            Client.GetRoleplay().MecPartsTo = 6;
                        }
                        else // Tanque Grande
                        {
                            Client.GetRoleplay().MecPartsTo = 9;
                        }
                        #endregion

                        #region Execute
                        if (Client.GetRoleplay().MecParts >= Client.GetRoleplay().MecPartsTo)
                        {
                            int Price = 2500;
                            Client.GetRoleplay().MecCarToRepair = itemfurni;
                            Client.GetRoleplay().MecRotPosition = Client.GetRoomUser().RotBody;
                            Client.GetRoleplay().MecCordinates = Client.GetRoomUser().Coordinate;
                            GetRoomUser().Chat("*Ofrece una reparación por $" + String.Format("{0:N0}", Price) + "*", true);
                            //Client.GetRoleplay().OfferManager.CreateOffer("fix_mecanico", Client.GetHabbo().Id, Price);
                            Client.SendWhisper("Te han ofrecido una reparación de Vehículo por $" + String.Format("{0:N0}", Price) + ". Escribe 'aceptar reparacion' para aceptarla ó no.", 1);
                            //Client.GetRoleplay().CooldownManager.CreateCooldown("comprar", 1000, 3);
                        }
                        else
                            Client.SendWhisper("Necesitas " + Client.GetRoleplay().MecPartsTo + " repuestos para reparar este vehículo. Compra diciendo 'comprar repuestos'.", 1);
                        #endregion
                        await Task.Delay(2000);
                        //RoleplayManager.CalledDelivery = false;
                    });
                }

                #region Mecánico
                if (RoleplayManager.PurgeStarted)
                {
                    Client.SendWhisper("¡No puedes trabajar durante la purga!", 1);
                    return;
                }
                #endregion

                #region Conditions Mec
                /*if (!Target.GetRoleplay().PediMec)
                {
                    Client.SendWhisper("Esa persona no ha solicitado los servicios de un Mecánico.", 1);
                    return;
                }*/
                #endregion

                
            }
            else if (Message.ToLower() == "comprar repuestos")
            {
                #region Conditions
                /*int MecID = PolarEnvironment.GetGame().GetRPRoomManager().TryToGetMecanicos(MyCity, out PlayRoom Data);//mecanicos de la cd.
                if (Client.GetHabbo().CurrentRoomId != MecID)
                {
                    Client.SendWhisper("Debes ir al Taller de Mecánicos de la ciudad para comprar repuestos", 1);
                    return;
                }*/
                #endregion


                #region Execute
                int RepairKitPrice = 500;
                int Price = RepairKitPrice * 5;

                if ((Client.GetRoleplay().MecParts + 10) > 100)
                {
                    Client.SendWhisper("¡No puedes llevar más de 100 repuestos en tu inventario!", 1);
                    return;
                }
                if (Client.GetHabbo().Credits < Price)
                {
                    Client.SendWhisper("No cuentas con $" + Price + " para comprar esos repuestos.", 1);
                    return;
                }

                RoleplayManager.Shout(Client, "*Compra " + 10 + " repuestos y paga $" + Price + " por ellos*", 5);
                GetRoomUser().Chat("¡Bien! Escribe 'reparar' para comenzar la reparación del vehiculo.", true);
                Client.SendWhisper("Has comprado " + 10 + " repuestos y pagaste $" + Price, 1);
                Client.GetHabbo().Credits -= Price;
                Client.GetHabbo().UpdateCreditsBalance();
                Client.GetRoleplay().MecParts += 10;
                
                Client.GetRoleplay().CooldownManager.CreateCooldown("comprar", 1000, 3);
                #endregion
            }
            else
            {


                switch (sKeyResult.ToLower())
                {
                    #region Healing
                    case "aceptar reparacion":
                    case "aceptar":
                        {
                            List<VehiclesOwned> VOx = PolarEnvironment.GetGame().GetVehiclesOwnedManager().getVehiclesOwnedByFurniId(Client.GetRoleplay().MecCarToRepair);
                            if (VOx != null && VOx.Count > 0)
                            {
                                int Price = Client.GetRoleplay().MecPriceTo;
                                //RoleplayManager.Shout(Client, "*Repara el vehículo y cierra el capó*", 5);
                                GetRoomUser().Chat("*Repara el vehículo y cierra el capó *", true);
                                //Client.SendWhisper("¡Vehículo reparado! Has ganado $" + Price, 1);

                                //RoleplayManager.JobSkills(Client, Client.GetRoleplay().JobId, Client.GetRoleplay().MecLvl, Client.GetRoleplay().MecXP);

                                Client.GetRoleplay().MecParts -= Client.GetRoleplay().MecPartsTo;
                                RoleplayManager.UpdateVehicleState(Client.GetRoleplay().MecCarToRepair, Client.GetRoleplay().MecNewState);
                                RoleplayManager.UpdateVehicleStat(Client.GetRoleplay().MecCarToRepair, "life", 100);
                                VOx[0].State = Client.GetRoleplay().MecNewState;
                                VOx[0].CarLife = 100;

                                // Cobro $
                                Client.GetHabbo().Credits -= Price;
                                Client.GetHabbo().UpdateCreditsBalance();
                                GetRoomUser().Chat("Espero estes contento con la reparación, me retiro.", true);
                                //RoleplayBotManager.EjectDeployedBot(GetRoomUser(), Room, false);
                                new Thread(() =>
                                {
                                    Thread.Sleep(5000);


                                    Room.GetRoomUserManager().RemoveBot(VirtualId, false);
                                    Thread.Sleep(2000);
                                    //RoleplayManager.CalledDelivery = false;
                                }).Start();
                                // Client.GetHabbo().Credits += Price;
                                //Client.GetRoleplay().MoneyEarned += Price;
                                //Client.GetHabbo().UpdateCreditsBalance();
                            }
                            else
                            {
                                Client.SendWhisper("No se pudo reparar el vehículo. ((Contacta con un administrador))", 1);
                            }
                        }
                        break;
                        #endregion
                }
            }
             


        }
        
       /* public override void HandleRequest(GameClient Client, string Message)
        {
            if (!OnDuty)
                return;

            if (RespondToSpeech(Client, Message))
                return;

            string Name = GetBotRoleplay().Name.ToLower();

            string[] keys = new string[] { "heal", "aid", "heal me", "heal please", "med aid", "curame" };
            string sKeyResult = keys.FirstOrDefault<string>(s => Message.Contains(s));

            if (sKeyResult == null)
                return;

            if (Message.ToLower() == Name)
                GetRoomUser().Chat("Hey " + Client.GetHabbo().Username + ", ¿Necesitas ayuda?", true);
            else
                switch (sKeyResult.ToLower())
                {
                    #region Healing
                    case "heal":
                    case "curame":
                        {
                            if (Client.GetRoleplay().IsDead)
                            {
                                if (Client.GetRoleplay().DeadTimeLeft <= 1 || Client.GetRoleplay().BeingHealed)
                                {
                                    string WhisperMessage = "Ya vas a ser dado de alta pronto! No hay necesidad de mi ayuda.";
                                    Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                                    break;
                                }

                                if (!GetBotRoleplay().WalkingToItem) { }
                                    //InitiateDischarge(Client);
                                else
                                {
                                    string WhisperMessage = "Ya estoy en mi camino para ayudar a alguien! Por favor, espere hasta que esté libre.";
                                    Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                                }
                            }
                            else
                            {
                                if (Client.GetRoleplay().CurHealth >= Client.GetRoleplay().MaxHealth)
                                {
                                    string WhisperMessage = "¡Ya estás completamente curado!";
                                    Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                                    break;
                                }

                                if (Client.GetRoleplay().BeingHealed)
                                {
                                    string WhisperMessage = "¡Ya estás sanado!";
                                    Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                                    break;
                                }

                                GetRoomUser().Chat("Espero que te sientas mejor pronto " + Client.GetHabbo().Username + " también te he Inyectado un complejo de Vitamina B12!¨(-100$)", true);
                                Client.GetRoleplay().BeingHealed = true;
                                Client.GetRoleplay().CurEnergy = Client.GetRoleplay().MaxEnergy;
                                Client.GetHabbo().Credits -= 200;
                                Client.GetHabbo().UpdateCreditsBalance();

                                Client.GetRoleplay().TimerManager.CreateTimer("heal", 1000, false);
                                break;
                            }
                            break;
                        }
                        #endregion
                }
        }
        */


        public override void StartActivities()
        {
            if (OnDuty)
                return;

            if (this.GetBotRoleplay().TimerManager.ActiveTimers.ContainsKey("notrabajar"))
                this.GetBotRoleplay().TimerManager.ActiveTimers["notrabajar"].EndTimer();

            this.GetBotRoleplay().Invisible = false;
            this.GetRoom().SendMessage(new UsersComposer(GetRoomUser()));

            this.GetRoomUser().Chat("Bien, comencemos.. Para revisar el auto escribe 'revisar'.", true);
            this.OnDuty = true;

            
        }

    }
}