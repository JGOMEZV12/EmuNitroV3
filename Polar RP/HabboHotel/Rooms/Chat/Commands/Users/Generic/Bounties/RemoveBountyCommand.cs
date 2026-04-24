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
    class RemoveBountyCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_bounty_undo"; }
        }

        public string Parameters
        {
            get { return "%username%"; }
        }

        public string Description
        {
            get { return "quita la recompensa por un usuario."; }
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

            if (Session.GetRoleplay().TryGetCooldown("bounty_remove"))
                return;

            var BountyList = BountyManager.BountyUsers;
            List<Bounty> Bounty = BountyManager.BountyUsers.Values.Where(x => x.AddedBy == Session.GetHabbo().Id).ToList();

            if (Bounty.Count <= 0)
            {
                Session.SendWhisper("No puedes eliminar este usuario de la lista de recompensas porque no eres el propietario de esa solicitud a " + TargetClient.GetHabbo().Username + "'s", 1);
                return;
            }

            if (!BountyList.ContainsKey(TargetClient.GetHabbo().Id))
            {
                Session.SendWhisper(TargetClient.GetHabbo().Username + " No está en la lista de recompensas", 1);
                return;
            }

            BountyManager.RemoveBounty(TargetClient.GetHabbo().Id);
            Session.Shout("*Quita a " + TargetClient.GetHabbo().Username + " De la lista de recompensas*", 4);
            Session.GetRoleplay().CooldownManager.CreateCooldown("bounty_remove", 1000, 5);
        }
    }
}
