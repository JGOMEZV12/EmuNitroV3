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
using Polar.HabboHotel.Groups;

namespace Polar.HabboRoleplay.Bots.Types
{
    public class GunStoreBot : RoleplayBotAI
    {
        int VirtualId;
        CryptoRandom Rand;
        public bool CheckForOtherWorkers;
        public int OnDutyCheckInterval;
        public int CurOnDutyCheckTime;

        public GunStoreBot(int VirtualId)
        {
            this.OnDuty = true;
            this.CheckForOtherWorkers = true;
            this.CurOnDutyCheckTime = 0;
            this.VirtualId = VirtualId;
            Rand = new CryptoRandom();
        }

        public override void OnDeployed(GameClient Client)
        {
            OnDuty = true;
            this.StartActivities();
        }

        public override void OnDeath(GameClient Client) { }
        public override void OnArrest(GameClient Client) { }
        public override void OnAttacked(GameClient Client) { }
        public override void OnUserLeaveRoom(GameClient Client) { if (!OnDuty) return; }

        public override void OnUserEnterRoom(GameClient Client)
        {
            if (!OnDuty) return;
            Client.SendWhisper("Si deseas comprar un arma, por favor, parate en uno de los cuadros. Para comprar balas di: balas {cantidad}", 1);
        }

        public override void OnUserUseTeleport(GameClient Client, object[] Params)
        {
            if (!OnDuty || Client == null || Client.GetRoomUser() == null) return;
            if (Client == GetBotRoleplay().UserFollowing || Client == GetBotRoleplay().UserAttacking)
                GetBotRoleplay().StartTeleporting(GetRoomUser(), GetRoom(), Params);
        }

        public override void OnUserSay(RoomUser User, string Message)
        {
            if (!OnDuty) return;
            var Client = User.GetClient();
            if (Client == null) return;
            HandleRequest(Client, Message);
        }

        public override void OnUserShout(RoomUser User, string Message)
        {
            if (!OnDuty || User.GetClient() == null) return;
            HandleRequest(User.GetClient(), Message);
        }

        public override void OnMessaged(GameClient Client, string Message) { if (!OnDuty) return; }

        public override void HandleRequest(GameClient Client, string Message)
        {
            if (!OnDuty) return;
            if (RespondToSpeech(Client, Message)) return;

            string Name = GetBotRoleplay().Name.ToLower();
            string[] Params = Message.Split(' ');

            Weapon purchaseweapon = null;
            foreach (var Weapon in WeaponManager.Weapons.Values)
            {
                if (Message.ToLower() == Weapon.Name.ToLower())
                {
                    Params[0] = "purchaseweapon";
                    purchaseweapon = Weapon;
                }
            }

            if (Message.ToLower() == Name)
            {
                GetRoomUser().Chat("Hey " + Client.GetHabbo().Username + ", ¿Necesitas ayudas?", true);
                return;
            }

            switch (Params[0].ToLower())
            {
                case "gun":
                case "weapon":
                case "guns":
                case "weapons":
                case "armas":
                    PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_shop", "openshop");
                    break;

                case "skin":
                case "skins":
                    PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_skins", "openshop");
                    break;

                case "bullets":
                case "balas":
                {
                    if (Client.GetRoleplay().EquippedWeapon == null)
                    {
                        Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, "¡Tienes que tener equipada un arma para comprar sus balas!", 0, 2));
                        return;
                    }
                    var wName = Client.GetRoleplay().EquippedWeapon.Name;
                    if (wName == "martillo" || wName == "cuchillo" || wName == "poder" || wName == "bate" || wName == "sukhoi")
                    {
                        Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, "¡No es un arma que posea balas!", 0, 2));
                        return;
                    }
                    if (Params.Length == 1)
                    {
                        Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, "¡Por favor ingrese la cantidad de puntos que desea comprar!", 0, 2));
                        return;
                    }
                    if (!int.TryParse(Params[1], out int Amount))
                    {
                        Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, "¡Por favor ingrese una cantidad válida de balas con la que desea comprar!", 0, 2));
                        break;
                    }
                    if (Amount < 10)
                    {
                        Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, "¡Necesitas comprar al menos 10 balas a la vez!", 0, 2));
                        break;
                    }
                    if (Client.GetRoleplay().BankTarget < 1)
                    {
                        Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, "¡Hey usted necesita tener una tarjeta de débito, por seguridad no manejamos dinero en efectivo!", 0, 2));
                        break;
                    }
                    int Cost = Convert.ToInt32(Math.Floor((double)Amount / 2));
                    if (Client.GetRoleplay().BankChequings < Cost)
                    {
                        Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, "Lo siento, pero no puede comprar balas su cuenta bancaria no tiene fondos", 0, 2));
                        break;
                    }
                    bool HasOffer = Client.GetRoleplay().OfferManager.ActiveOffers.Values.Any(o => o.Type.ToLower() == "balas");
                    if (!HasOffer)
                    {
                        GetRoomUser().Chat("*Ofrece " + String.Format("{0:N0}", Amount) + " balas para " + Client.GetHabbo().Username + " por $" + String.Format("{0:N0}", Cost) + "*", true);
                        Client.GetRoleplay().OfferManager.CreateOffer("balas", 0, Amount, this);
                        Client.SendWhisper("Acaba de ofrecerte " + String.Format("{0:N0}", Amount) + " balas por $" + String.Format("{0:N0}", Cost) + "! diga ':aceptar balas' para comprar", 1);
                    }
                    else
                        Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, "Lo siento, pero ya le han ofrecido balas", 0, 2));
                    break;
                }

                case "materiales":
                {
                    if (!GroupManager.HasJobCommand(Client, "armero"))
                    {
                        Client.SendWhisper("Debes tener el trabajo de Armero para usar ese comando.", 1);
                        return;
                    }
                    if (Params.Length == 1)
                    {
                        Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, "¡Por favor ingrese la cantidad de materiales que desea comprar!", 0, 2));
                        return;
                    }
                    // FIX Bug 2: int.TryParse en lugar de Convert.ToInt32
                    if (!int.TryParse(Params[1], out int Cant))
                    {
                        Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, "¡Por favor ingrese una cantidad válida!", 0, 2));
                        return;
                    }
                    int Price = Cant * RoleplayManager.ArmMatPrice;
                    if ((Client.GetRoleplay().ArmMat + Cant) > RoleplayManager.ArmMatLimit)
                    {
                        Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, "Solo puedes llevar " + RoleplayManager.ArmMatLimit + " materiales en tu inventario.", 0, 2));
                        return;
                    }
                    if (Client.GetHabbo().Credits < Price)
                    {
                        Client.SendWhisper("Necesitas al menos $" + Price + " para comprar " + Cant + " materiales.", 1);
                        return;
                    }
                    RoleplayManager.Shout(Client, "*Compra " + Cant + " materiales y paga $" + Price + " por ellos*", 5);
                    Client.GetHabbo().Credits -= Price;
                    Client.GetHabbo().UpdateCreditsBalance();
                    Client.GetRoleplay().ArmMat += Cant;
                    break;
                }
            }
        }

        public override void StopActivities()
        {
            if (!OnDuty) return;
            // FIX Bug 1: EndTimerSafe
            EndTimerSafe("trabajar");
            EndTimerSafe("pickupdelivery");
            EndTimerSafe("deliverywait");
            GetRoomUser().Chat("Bien, he culminado mi turno. ¡Nos vemos!", true);
            OnDuty = false;
            GetBotRoleplay().WalkingToItem = false;
            if (GetBotRoleplay().WorkUniform != "none")
                GetRoom().SendMessage(new UsersComposer(GetRoomUser()));
            Item Item;
            if (GetBotRoleplay().GetStopWorkItem(this.GetRoom(), out Item))
            {
                GetRoomUser().MoveTo(new System.Drawing.Point(Item.GetX, Item.GetY));
                GetBotRoleplay().TimerManager.CreateTimer("notrabajar", GetBotRoleplay(), 10, true, null);
            }
        }

        public override void StartActivities()
        {
            if (OnDuty) return;
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
            GetRoomUser().MoveTo(new System.Drawing.Point(GetBotRoleplay().oX, GetBotRoleplay().oY));
            GetBotRoleplay().TimerManager.CreateTimer("trabajar", GetBotRoleplay(), 10, true, null);
        }
    }
}