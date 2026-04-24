using System;
using System.Linq;
using System.Text;
using System.Threading;
using Polar.Utilities;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Weapons;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using Polar.Communication.Packets.Outgoing.Notifications;

namespace Polar.HabboRoleplay.Bots.Types
{
    public class CarStoreBot : RoleplayBotAI
    {
        int VirtualId;
        CryptoRandom Rand;
        public bool CheckForOtherWorkers;
        public int OnDutyCheckInterval;
        public int CurOnDutyCheckTime;
        //private bool CancelWorkMovement = false;

        public CarStoreBot(int VirtualId)
        {
            this.OnDuty = true;
            this.CheckForOtherWorkers = true;
            this.CurOnDutyCheckTime = 0;
            this.VirtualId = VirtualId;

            Rand = new CryptoRandom();
        }


        public override void OnDeployed(GameClient Client)
        {
            //OnDuty = false;
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
            if (!OnDuty)
                return;
        }

        public override void OnUserEnterRoom(GameClient Client)
        {
            if (!OnDuty)
                return;

            if (!GetRoomUser().IsWalking)
            {
                // Look at the user 
            }
        }

        public override void OnUserUseTeleport(GameClient Client, object[] Params)
        {
            if (!OnDuty)
                return;

            if (Client == null) return;
            if (Client.GetRoomUser() == null) return;

            if (Client == GetBotRoleplay().UserFollowing || Client == GetBotRoleplay().UserAttacking)
                GetBotRoleplay().StartTeleporting(GetRoomUser(), GetRoom(), Params);
        }

        public override void OnUserSay(RoomUser User, string Message)
        {
            if (!OnDuty)
                return;

            GameClient Client = User.GetClient();

            if (Client == null)
                return;
            HandleRequest(Client, Message);
        }

        public override void OnUserShout(RoomUser User, string Message)
        {
            if (!OnDuty)
                return;

            if (User.GetClient() == null)
                return;
            HandleRequest(User.GetClient(), Message);
        }

        public override void OnMessaged(GameClient Client, string Message)
        {
            if (!OnDuty)
                return;
        }

        public override void HandleRequest(GameClient Client, string Message)
        {
            if (!OnDuty)
                return;

            if (GetBotRoleplay().WalkingToItem)
                return;

            if (RespondToSpeech(Client, Message))
                return;

            string Name = GetBotRoleplay().Name.ToLower();

            string[] Params = Message.Split(' ');

            if (Message.ToLower() == Name)
                GetRoomUser().Chat("¡Bienvenido! " + Client.GetHabbo().Username + ", ¿Necesitas algo?", true);
            else
                switch (Params[0].ToLower())
                {
                    #region Car
                    case "car":
                    case "carro":
                        {
                            if (Client.GetRoleplay().CarType > 0)
                            {
                                string WhisperMessage = "Ya tienes un coche";
                                Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                                break;
                            }
                            else
                            {
                                int Cost = 2000;
                                bool HasOffer = false;
                                if (Client.GetHabbo().Credits >= Cost)
                                {
                                    foreach (var Offer in Client.GetRoleplay().OfferManager.ActiveOffers.Values)
                                    {
                                        if (Offer.Type.ToLower() == "carro")
                                        {
                                            HasOffer = true;
                                        }
                                    }
                                    if (!HasOffer)
                                    {
                                        GetRoomUser().Chat("*Ofrece un Toyota Corolla a " + Client.GetHabbo().Username + " por $2,000*", true);
                                        Client.GetRoleplay().OfferManager.CreateOffer("carro", 0, Cost, this);
                                        Client.SendWhisper("Acaba de ofrecerle un Toyota Corolla por $ 2.000! diga ':aceptar carro' para comprarlo", 1);
                                        break;
                                    }
                                    else
                                    {
                                        string WhisperMessage = "Ya se le ha ofrecido un coche! Escribe ':aceptar carro' para comprarlo por $2,000!";
                                        Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                                        break;
                                    }
                                }
                                else
                                {
                                    string WhisperMessage = "Usted no tiene $ 2,000 para comprar un coche!";
                                    Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                                    break;
                                }
                            }
                        }
                    #endregion

                    #region Car Upgrade
                    case "upgrade":
                    case "mejorarcarro":
                        {
                            if (Client.GetRoleplay().CarType < 1)
                            {
                                string WhisperMessage = "No tienes un coche para mejorar";
                                Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                                break;
                            }
                            else if (Client.GetRoleplay().CarType > 2)
                            {
                                string WhisperMessage = "Lo siento, pero ya tiene el coche más alto que puede conseguir";
                                Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                                break;
                            }
                            else
                            {
                                int Cost = Client.GetRoleplay().CarType == 1 ? 3000 : 5000;
                                bool HasOffer = false;
                                string CarName = RoleplayManager.GetCarName(Client, true);

                                if (Client.GetHabbo().Credits >= Cost)
                                {
                                    foreach (var Offer in Client.GetRoleplay().OfferManager.ActiveOffers.Values)
                                    {
                                        if (Offer.Type.ToLower() == "mejorarcarro")
                                        {
                                            HasOffer = true;
                                        }
                                    }
                                    if (!HasOffer)
                                    {
                                        GetRoomUser().Chat("*Ofrece una mejora al " + CarName + " de " + Client.GetHabbo().Username + " por $" + String.Format("{0:N0}", Cost) + "*", true);
                                        Client.GetRoleplay().OfferManager.CreateOffer("mejorarcarro", 0, Cost, this);
                                        Client.SendWhisper("Acaba de ofrecerle una mejora al " + CarName + " carro por $" + String.Format("{0:N0}", Cost) + " Tipo ':aceptar mejorarcarro' para comprar", 1);
                                        break;
                                    }
                                    else
                                    {
                                        string WhisperMessage = "Lo sentimos, pero ya se le ha ofrecido una actualización de coche";
                                        Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                                        break;
                                    }
                                }
                                else
                                {
                                    string WhisperMessage = "Lo sentimos, pero no puedes pagar una actualización de coche";
                                    Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                                    break;
                                }
                            }
                        }
                    #endregion

                    #region Fuel
                    case "gasolina":
                        {
                            int Amount;

                            if (Client.GetRoleplay().CarType < 1)
                            {
                                string WhisperMessage = "Lo siento, pero usted no tiene un coche para comprar combustible";
                                Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                                break;
                            }
                            if (!Client.GetRoleplay().NearItem("bump_tottero", 1))
                            {
                                string WhisperMessage = "Por favor, pare su vehiculo frente a la bomba de gasolina";
                                Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                                break;
                            }
                            if (Client.GetRoleplay().DrivingCar == false)
                            {
                                string WhisperMessage = "Usted necesita estar dentro de su carro, escribe :manejar carro y coloquese frente a la bomba de gasolina";
                                Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                                break;
                            }
                            else if (Params.Length == 1)
                            {
                                string WhisperMessage = "¡Por favor ingrese la cantidad de combustible que le gustaría comprar!";
                                Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                                return;
                            }
                            else if (!int.TryParse(Params[1], out Amount))
                            {
                                string WhisperMessage = "¡Por favor ingrese una cantidad válida de combustible que le gustaría comprar con!";
                                Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                                break;
                            }

                            if (Amount < 10)
                            {
                                string WhisperMessage = "Usted necesita comprar al menos 10 galones de gasolina a la vez";
                                Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                                break;
                            }
                            else
                            {
                                int Cost = Convert.ToInt32(Math.Floor((double)(Amount * 2) / 3));
                                bool HasOffer = false;

                                if (Client.GetHabbo().Credits >= Cost)
                                {
                                    foreach (var Offer in Client.GetRoleplay().OfferManager.ActiveOffers.Values)
                                    {
                                        if (Offer.Type.ToLower() == "gasolina")
                                        {
                                            HasOffer = true;
                                        }
                                    }
                                    if (!HasOffer)
                                    {
                                        GetRoomUser().Chat("*Ofrece " + String.Format("{0:N0}", Amount) + " Galones de combustible a " + Client.GetHabbo().Username + " por $" + String.Format("{0:N0}", Cost) + " di :aceptar gasolina*", true);
                                        Client.GetRoleplay().OfferManager.CreateOffer("gasolina", 0, Amount, this);
                                        Client.SendWhisper("Acaba de ofrecerte " + String.Format("{ 0:N0}", Amount) + " Galones de combustible por $" + String.Format("{0:N0}", Cost) + " Tipo ':aceptar gasolina' para comprar", 1);
                                        break;
                                    }
                                    else
                                    {
                                        string WhisperMessage = "Lo sentimos, pero ya le han ofrecido combustible";
                                        Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                                        break;
                                    }
                                }
                                else
                                {
                                    string WhisperMessage = "Lo sentimos, pero no puede permitirse el combustible";
                                    Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                                    break;
                                }
                            }
                        }
                        #endregion
                }
        }

        public override void StopActivities()
        {
            if (!OnDuty)
                return;

            EndTimerSafe("trabajar");

            GetRoomUser().Chat("Bien, ese es mi turno. ¡Nos vemos!", true);
            OnDuty = false;
            GetBotRoleplay().WalkingToItem = false;

            if (GetBotRoleplay().WorkUniform != "none")
                GetRoom().SendMessage(new UsersComposer(GetRoomUser()));

            Item Item;
            if (GetBotRoleplay().GetStopWorkItem(this.GetRoom(), out Item))
            {
                var Point = new System.Drawing.Point(Item.GetX, Item.GetY);
                GetRoomUser().MoveTo(Point);
                GetBotRoleplay().TimerManager.CreateTimer("notrabajar", GetBotRoleplay(), 10, true, null);
            }
        }

        public override void StartActivities()
        {
            if (OnDuty)
                return;

            EndTimerSafe("notrabajar");

            GetBotRoleplay().Invisible = false;
            GetRoom().SendMessage(new UsersComposer(GetRoomUser()));

            GetRoomUser().Chat("Bien, hora de volver al trabajo", true);
            OnDuty = true;

            if (GetBotRoleplay().WorkUniform != "none")
                GetRoom().SendMessage(new UsersComposer(GetRoomUser()));

            Item Item;
            if (GetBotRoleplay().GetStopWorkItem(this.GetRoom(), out Item))
            {
                var ItemPoint = new System.Drawing.Point(Item.GetX, Item.GetY);
                if (GetRoomUser().Coordinate == ItemPoint)
                {
                    Item.ExtraData = "2";
                    Item.UpdateState(false, true);
                    Item.RequestUpdate(2, true);
                }
            }

            var Point = new System.Drawing.Point(GetBotRoleplay().oX, GetBotRoleplay().oY);
            GetRoomUser().MoveTo(Point);
            GetBotRoleplay().TimerManager.CreateTimer("trabajar", GetBotRoleplay(), 10, true, null);
        }

    }
}