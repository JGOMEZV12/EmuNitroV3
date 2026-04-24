using ConnectionManager;
using Polar.Net;
using Newtonsoft.Json;
using Polar.Core;
using Polar.HabboHotel.GameClients;
using System.IO;
using Polar.HabboRoleplay.RoleplayUsers.Offers;
using Polar.HabboRoleplay.Weapons;
using Polar.HabboRoleplay.Bots;
using Polar.HabboRoleplay.Farming;
using Polar.HabboHotel.Items;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboHotel.Roleplay.Web.Incoming.Others
{
    /// <summary>
    /// ATMWebEvent class.
    /// </summary>
    class ManejarWebEvent : IWebEvent
    {
        /// <summary>
        /// Executes socket data.
        /// </summary>
        /// <param name="Client"></param>
        /// <param name="Data"></param>
        /// <param name="Socket"></param>
        public void Execute(GameClient Client, string Data, ConnectionInformation Socket)
        {

            if (!PolarEnvironment.GetGame().GetWebEventManager().SocketReady(Socket))
            {
                if (Client == null)
                    return;

                if (Client.LoggingOut)
                    return;

                Client.SendWhisper("¡No estás conectado al websocket!Por favor, póngase en contacto con un miembro del personal si este problema persiste!", 1);

                return;
            }

            if (Client == null)
                return;

            if (Client.LoggingOut)
                return;

            if (string.IsNullOrEmpty(Data))
                return;

            Dictionary<object, object> ReturnedData = JsonConvert.DeserializeObject<Dictionary<object, object>>(Data);
            string Action = null;

            if (ReturnedData.ContainsKey("action"))
                Action = Convert.ToString(ReturnedData["action"]);


            switch (Action.ToLower())
            {

                #region Open
                case "open":
                    {
                        string jSon = "";
                        string CarType = RoleplayManager.GetCarName(Client);
                        if (Client.GetRoleplay().CarType == 0)
                                 Socket.SendWS( "compose_autos|none|[]");

                            jSon = JsonConvert.SerializeObject(new Dictionary<object, object>()
                                 {
                                    { "isTrusted", false },
                                    { "carID", Client.GetRoleplay().CarType },
                                    { "using", "no" },
                                    { "fuel", Client.GetRoleplay().CarFuel }
                                 });
                            
                            //Socket.SendWS( "{\"compose_buscado\":\"open\",\"username\":" + PolarEnvironment.GetHabboById(Convert.ToInt32(Wanted.UserId)).Username + ", \"look\":" + PolarEnvironment.GetHabboById(Convert.ToInt32(Wanted.UserId)).Look + ",\"stars\":" + WantedStar + ",\"last_seen\":" + Wanted.LastSeenRoom + "}");
                        //Logging.WriteLine("compose_autos:[" + jSon +"]");
                        Socket.SendWS( "compose_autos|[" + jSon + "]");
                    }
                    break;
                #endregion

                #region Manejar
                case "manejar":
                    {

                        string jSon = "";
                        string CarType = RoleplayManager.GetCarName(Client);

                        if (Client.GetRoleplay().DrivingCar)
                        {
                            StopCar(Client);
                            return;
                        }

                        if (Client.GetRoleplay().CarType <= 0)
                        {
                            Client.SendWhisper("¡No tienes coche, ve a la tienda de autos sala: 12!", 1);
                            break;
                        }

                        if (Client.GetRoleplay().CarFuel < 0)
                        {
                            Client.SendWhisper("Usted no tiene ningún combustible para su coche, ve a la gasolinera en la 12 y di gasolina", 1);
                            break;
                        }

                        jSon = JsonConvert.SerializeObject(new Dictionary<object, object>()
                                 {
                                    { "isTrusted", false },
                                    { "carID", Client.GetRoleplay().CarType },
                                    { "using", "si" },
                                    { "fuel", Client.GetRoleplay().CarFuel }
                                 });

                        if (Client.GetRoleplay().CarType == 1)
                            Client.GetRoleplay().CarEnableId = 800;
                        else if (Client.GetRoleplay().CarType == 2)
                            Client.GetRoleplay().CarEnableId = 812;
                        else if (Client.GetRoleplay().CarType == 3)
                            Client.GetRoleplay().CarEnableId = 813;
                        else if (Client.GetRoleplay().CarType == 4)
                            Client.GetRoleplay().CarEnableId = 21;
                        else if (Client.GetRoleplay().CarType == 5)
                            Client.GetRoleplay().CarEnableId = 22;
                        else if (Client.GetRoleplay().CarType == 6)
                            Client.GetRoleplay().CarEnableId = 48;
                        else if (Client.GetRoleplay().CarType == 7)
                            Client.GetRoleplay().CarEnableId = 54;
                        else if (Client.GetRoleplay().CarType == 8)
                            Client.GetRoleplay().CarEnableId = 510;
                        else if (Client.GetRoleplay().CarType == 9)
                            Client.GetRoleplay().CarEnableId = 900;
                        else if (Client.GetRoleplay().CarType == 10)
                            Client.GetRoleplay().CarEnableId = 820;
                        else if (Client.GetRoleplay().CarType == 11)
                            Client.GetRoleplay().CarEnableId = 831;
                        else if (Client.GetRoleplay().CarType == 12)
                            Client.GetRoleplay().CarEnableId = 832;
                        else if (Client.GetRoleplay().CarType == 13)
                            Client.GetRoleplay().CarEnableId = 827;
                        else if (Client.GetRoleplay().CarType == 14)
                            Client.GetRoleplay().CarEnableId = 826;
                        else if (Client.GetRoleplay().CarType == 15)
                            Client.GetRoleplay().CarEnableId = 69;
                        else
                            Client.GetRoleplay().CarEnableId = 813;

                        if (Client.GetRoomUser() != null)
                        {
                            if (Client.GetRoomUser().CurrentEffect != Client.GetRoleplay().CarEnableId)
                                Client.GetRoomUser().ApplyEffect(Client.GetRoleplay().CarEnableId);
                        }

                        if (Client.GetRoleplay().EquippedWeapon != null)
                            Client.GetRoleplay().EquippedWeapon = null;

                        Client.GetRoleplay().DrivingCar = true;
                        Client.GetRoleplay().CarTimer = 0;

                        Client.Shout("*Se monta en su " + CarType + " y lo enciende*", 4);
                        Client.GetRoleplay().CooldownManager.CreateCooldown("carro", 1000, 90);

                        //Socket.SendWS( "{\"compose_buscado\":\"open\",\"username\":" + PolarEnvironment.GetHabboById(Convert.ToInt32(Wanted.UserId)).Username + ", \"look\":" + PolarEnvironment.GetHabboById(Convert.ToInt32(Wanted.UserId)).Look + ",\"stars\":" + WantedStar + ",\"last_seen\":" + Wanted.LastSeenRoom + "}");
                       // Logging.WriteLine("compose_autos:[" + jSon +"]");
                       
                        Socket.SendWS( "compose_autos|[" + jSon + "]");
                    }
                    break;
                #endregion

                #region Comprar
                case "comprar":
                    {
                        var Offer = Client.GetRoleplay().OfferManager.ActiveOffers["carro"];
                        if (Offer.Params != null && Offer.Params.Length > 0)
                        {
                            RoleplayBotAI Bot = (RoleplayBotAI)Offer.Params[0];
                            if (Client.GetHabbo().Credits < Offer.Cost)
                            {
                                RoleplayOffer Junk;
                                Client.GetRoleplay().OfferManager.ActiveOffers.TryRemove("carro", out Junk);
                                Client.SendWhisper("Lo siento, no puedes permitirte un Toyota Corolla!", 1);
                                return;
                            }
                            else
                            {
                                Client.Shout("*Acepta la oferta de " + Bot.GetBotRoleplay().Name + " Y compra Toyota Corolla por $" + String.Format("{0:N0}", Offer.Cost) + "*", 4);
                                Client.GetHabbo().Credits -= Offer.Cost;
                                Client.GetHabbo().UpdateCreditsBalance();
                                Client.GetRoleplay().CarType = 1;
                                Client.GetRoleplay().CarFuel = 300;

                                RoleplayOffer Junk;
                                Client.GetRoleplay().OfferManager.ActiveOffers.TryRemove("carro", out Junk);
                                Bot.GetRoomUser().Chat("Gracias por comprar a Toyota Corolla " + Client.GetHabbo().Username + "!", true);
                                return;
                            }
                        }
                        else
                        {
                            GameClient Offerer = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Offer.OffererId);
                            if (Offerer == null || Offerer.GetRoomUser().RoomId != Client.GetRoomUser().RoomId || Offerer.GetRoomUser() == null)
                            {
                                RoleplayOffer Junk;
                                Client.GetRoleplay().OfferManager.ActiveOffers.TryRemove("carro", out Junk);
                                Client.SendWhisper("Lo sentimos, este usuario se ha desconectado o no está en la misma habitación que tú.", 1);
                                return;
                            }
                            else if (Client.GetHabbo().Credits < Offer.Cost)
                            {
                                RoleplayOffer Junk;
                                Client.GetRoleplay().OfferManager.ActiveOffers.TryRemove("carro", out Junk);
                                Client.SendWhisper("Lo siento, no puedes permitirte un Toyota Corolla!", 1);
                                return;
                            }
                            else
                            {
                                Client.Shout("*Acepta la oferta de " + Offerer.GetHabbo().Username + " Y compra Toyota Corolla por $" + String.Format("{0:N0}", Offer.Cost) + "*", 4);
                                Client.GetHabbo().Credits -= Offer.Cost;
                                Client.GetHabbo().UpdateCreditsBalance();
                                Client.GetRoleplay().CarType = 1;
                                Client.GetRoleplay().CarFuel = 300;

                                Offerer.GetHabbo().Credits += Offer.Cost / 20;
                                Offerer.GetHabbo().UpdateCreditsBalance();

                                RoleplayOffer Junk;
                                Client.GetRoleplay().OfferManager.ActiveOffers.TryRemove("carro", out Junk);
                                Offerer.SendWhisper("Recibes un recorte de $" + String.Format("{0:N0}", (Offer.Cost / 20)) + " por venderle a " + Client.GetHabbo().Username + " un " + Offer.Type + "!");
                                PolarEnvironment.GetGame().GetAchievementManager().ProgressAchievement(Offerer, "ACH_SellingCar", 1);
                                return;
                            }
                        }
                    }
                #endregion

                #region mejorar
                case "mejorar":
                    {
                        var Offer = Client.GetRoleplay().OfferManager.ActiveOffers["mejorarcarro"];
                        string CarName = RoleplayManager.GetCarName(Client, true);
                        if (Offer.Params != null && Offer.Params.Length > 0)
                        {
                            RoleplayBotAI Bot = (RoleplayBotAI)Offer.Params[0];
                            if (Client.GetHabbo().Credits < Offer.Cost)
                            {
                                RoleplayOffer Junk;
                                Client.GetRoleplay().OfferManager.ActiveOffers.TryRemove("mejorarcarro", out Junk);
                                Client.SendWhisper("Lo siento, no puedes pagar una " + CarName + "!", 1);
                                return;
                            }
                            else if (Client.GetRoleplay().CarType >= 3)
                            {
                                RoleplayOffer Junk;
                                Client.GetRoleplay().OfferManager.ActiveOffers.TryRemove("mejorarcarro", out Junk);
                                Client.SendWhisper("Ya tienes el mejor coche posible", 1);
                                return;
                            }
                            else
                            {
                                Client.Shout("*Acepta la oferta de " + Bot.GetBotRoleplay().Name + " Y compra car upgrade to the " + CarName + " por $" + String.Format("{0:N0}", Offer.Cost) + "*", 4);
                                Client.GetHabbo().Credits -= Offer.Cost;
                                Client.GetHabbo().UpdateCreditsBalance();
                                Client.GetRoleplay().CarType += 1;

                                RoleplayOffer Junk;
                                Client.GetRoleplay().OfferManager.ActiveOffers.TryRemove("mejorarcarro", out Junk);
                                Bot.GetRoomUser().Chat("Gracias por comprar a car upgrade " + Client.GetHabbo().Username + "!", true);
                                return;
                            }
                        }
                        else
                        {
                            GameClient Offerer = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Offer.OffererId);
                            if (Offerer == null || Offerer.GetRoomUser().RoomId != Client.GetRoomUser().RoomId || Offerer.GetRoomUser() == null)
                            {
                                RoleplayOffer Junk;
                                Client.GetRoleplay().OfferManager.ActiveOffers.TryRemove("mejorarcarro", out Junk);
                                Client.SendWhisper("Lo sentimos, este usuario se ha desconectado o no está en la misma habitación que tú.", 1);
                                return;
                            }
                            else if (Client.GetHabbo().Credits < Offer.Cost)
                            {
                                RoleplayOffer Junk;
                                Client.GetRoleplay().OfferManager.ActiveOffers.TryRemove("mejorarcarro", out Junk);
                                Client.SendWhisper("Siento que no puedas pagar un " + CarName + "!", 1);
                                return;
                            }
                            else if (Client.GetRoleplay().CarType >= 3)
                            {
                                RoleplayOffer Junk;
                                Client.GetRoleplay().OfferManager.ActiveOffers.TryRemove("mejorarcarro", out Junk);
                                Client.SendWhisper("Ya tienes el mejor coche posible!", 1);
                                return;
                            }
                            else
                            {
                                Client.Shout("*Acepta la oferta de " + Offerer.GetHabbo().Username + " Y compra car upgrade to the " + CarName + " por $" + String.Format("{0:N0}", Offer.Cost) + "*", 4);
                                Client.GetHabbo().Credits -= Offer.Cost;
                                Client.GetHabbo().UpdateCreditsBalance();
                                Client.GetRoleplay().CarType += 1;

                                Offerer.GetHabbo().Credits += Offer.Cost / 20;
                                Offerer.GetHabbo().UpdateCreditsBalance();

                                RoleplayOffer Junk;
                                Client.GetRoleplay().OfferManager.ActiveOffers.TryRemove("mejorarcarro", out Junk);
                                Offerer.SendWhisper("Recibes un recorte de $" + String.Format("{0:N0}", (Offer.Cost / 20)) + " por venderle a " + Client.GetHabbo().Username + " un carro mejor");
                                PolarEnvironment.GetGame().GetAchievementManager().ProgressAchievement(Offerer, "ACH_SellingCar", 1);
                                return;
                            }
                        }
                    }
                    #endregion
            }
        }
        public void StopCar(GameClient Client)
        {
            Client.GetRoleplay().DrivingCar = false;
            Client.GetRoleplay().CarEnableId = 0;
            Client.Shout("*Apaga el carro y se baja*", 4);

            if (Client.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("conditioncheck"))
                Client.GetRoleplay().TimerManager.ActiveTimers["conditioncheck"].TimeCount = 0;

            if (Client.GetRoleplay().CooldownManager.ActiveCooldowns.ContainsKey("car"))
                Client.GetRoleplay().CooldownManager.ActiveCooldowns["car"].Amount = 90;
            else
                Client.GetRoleplay().CooldownManager.CreateCooldown("car", 1000, 90);

            if (Client.GetRoomUser() != null)
            {
                if (Client.GetRoomUser().CurrentEffect != 0)
                    Client.GetRoomUser().ApplyEffect(0);
            }
            return;
        }

        // ── Helper: envía texto como frame WebSocket usando ConnectionInformation
        private static void SendWS(ConnectionInformation socket, string message)
        {
            if (socket == null || string.IsNullOrEmpty(message)) return;
            socket.SendData(System.Text.Encoding.UTF8.GetBytes(message));
        }

    }
}
