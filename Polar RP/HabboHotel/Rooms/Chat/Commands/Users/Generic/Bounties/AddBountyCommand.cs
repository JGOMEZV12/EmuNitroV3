using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms.AI;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Inventory.Pets;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Items;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Bounties
{
    class AddBountyCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_bounty_add"; }
        }

        public string Parameters
        {
            get { return "%username% %reward%"; }
        }

        public string Description
        {
            get { return "Añade el usuario a la recompensa."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("Vaya, se le olvidó ingresar un nombre de usuario", 1);
                return;
            }

            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);
            if (TargetClient == null)
            {
                Session.SendWhisper("Se ha producido un error al intentar encontrar a ese usuario, tal vez están sin conexión.", 1);
                return;
            }

            if (Session.GetRoleplay().TryGetCooldown("bounty_add"))
                return;

            if (Session.GetRoleplay().Level < 3)
            {
                Session.SendWhisper("¡Debes estar al menos nivel 3 para establecer recompensas!", 1);
                return;
            }

            if (TargetClient.GetRoleplay().Level < 3)
            {
                Session.SendWhisper(TargetClient.GetHabbo().Username + "'¡El nivel es demasiado bajo para fijar una recompensa encendido! ¡Deben ser al menos nivel 5!", 1);
                return;
            }

            if (TargetClient.GetRoleplay().IsDead)
            {
                Session.SendWhisper("No se puede establecer una recompensa en alguien que está muerto", 1);
                return;
            }

            if (TargetClient.GetRoleplay().IsJailed)
            {
                Session.SendWhisper("Usted no puede establecer una recompensa en alguien que está encarcelado", 1);
                return;
            }

            if (TargetClient == Session)
            {
                Session.SendWhisper("No se puede establecer una recompensa en ti mismo", 1);
                return;
            }

            var BountyList = BountyManager.BountyUsers;
            int Amount;

            if (Params.Length < 3)
            {
                Session.SendWhisper("Sintaxis de comandos no válido :recompensa <nombre de usuario> <cantidad>", 1);
                return;
            }

            if (int.TryParse((Params[2]), out Amount))
            {
                if (BountyList.ContainsKey(TargetClient.GetHabbo().Id))
                {
                    Session.SendWhisper(TargetClient.GetHabbo().Username + " Ya está en la lista de recompensas", 1);
                    return;
                }

                if (Amount <= 0)
                {
                    Session.SendWhisper("¡Por favor ingrese una cantidad de dinero válida!", 1);
                    return;
                }

                if (Amount < 100)
                {
                    Session.SendWhisper("¡La cantidad mínima de la recompensa es $ 100!", 1);
                    return;
                }

                if (Session.GetHabbo().Credits < Amount)
                {
                    Session.SendWhisper("No tienes $" + String.Format("{0:N0}", Amount) + " Por una nueva recompensa", 1);
                    return;
                }

                Bounty NewBounty = new Bounty(TargetClient.GetHabbo().Id, Session.GetHabbo().Id, Amount, PolarEnvironment.GetUnixTimestamp(), PolarEnvironment.GetUnixTimestamp() + 3600);
                BountyManager.AddBounty(NewBounty);

                Session.Shout("*Coloca una recompensa de $" + String.Format("{0:N0}", Amount) + " por " + TargetClient.GetHabbo().Username + "*", 4);

                Session.GetHabbo().Credits -= Amount;
                Session.GetHabbo().UpdateCreditsBalance();
                Session.GetRoleplay().CooldownManager.CreateCooldown("bounty_add", 1000, 5);
                lock (PolarEnvironment.GetGame().GetClientManager().GetClients)
                {
                    foreach (var client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                    {
                        if (client == null || client.GetHabbo() == null)
                            continue;

                        client.SendWhisper("[RECOMPENSA] Se le dará $" + String.Format("{0:N0}", Amount) + " a la persona que mate a  " + TargetClient.GetHabbo().Username + " ¡Encuentralo!*", 33);
                    }
                }
            }
            else
                Session.SendWhisper("Coloque un numero válido", 1);
        }
    }
}
