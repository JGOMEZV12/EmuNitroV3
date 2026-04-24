using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Polar.HabboRoleplay.Turfs;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Pathfinding;
using Polar.HabboRoleplay.Gambling;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Events
{
    class GamblingCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_events_gambling"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Le permite usar comandos de juego"; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            var Game = TexasHoldEmManager.GetGameForUser(Session.GetHabbo().Id);

            if (Game == null || Session.GetRoleplay().TexasHoldEmPlayer <= 0)
            {
                Session.SendWhisper("¡Debes estar en un juego de Texas Hold 'Em para usar este comando!", 1);
                return;
            }

            if (!Game.GameStarted)
            {
                Session.SendWhisper("¡El juego de Texas Hold 'Em todavía no ha comenzado!", 1);
                return;
            }

            if (Params[0].ToLower() == "apostar" || Params[0].ToLower() == "pasar")
            {
                ExecuteBet(Session, Room, Params, Game);
                return;
            }
        }

        public void ExecuteBet(GameClients.GameClient Session, Rooms.Room Room, string[] Params, TexasHoldEm Game)
        {
            if (Game.GameSequence <= 0 || Game.GameSequence > 2)
            {
                Session.SendWhisper("¡No puedes colocar un apostar (o pasar) ahora mismo!", 1);
                return;
            }

            int Number = Session.GetRoleplay().TexasHoldEmPlayer;

            if (Game.PlayersTurn != Number)
            {
                Session.SendWhisper("¡No es tu turno en el juego!", 1);
                return;
            }

            if (!Game.PlayerList.ContainsKey(Number))
                return;

            var Player = Game.PlayerList[Number];

            if (Player == null || Player.UserId != Session.GetHabbo().Id)
                return;

            if (Player.TotalAmount <= 0)
            {
                // Already has all their chips in (or has the maximum bet rn)
                Game.ChangeTurn();
                return;
            }

            bool Zero = false;
            if (Params.Length > 1 && Params[1].ToLower() == "0")
                Zero = true;

            if (Params[0].ToLower() == "pasar" && Game.MinimumBet(Number) > 0)
            {
                Session.SendWhisper("¡No puedes pasar este turno! Usted debe: apostar o: leavegame!", 1);
                return;
            }

            if (Game.MinimumBet(Number) == 0 && (Params.Length == 1 || Params[0].ToLower() == "pasar" || Zero))
            {
                // Doesnt need to make a bet, can pass
                Game.ChangeTurn();
                return;
            }

            if (Params.Length == 1)
            {
                Session.SendWhisper("¡Por favor ingrese la cantidad que le gustaría apostar!", 1);
                return;
            }

            int Amount;
            if (!int.TryParse(Params[1], out Amount))
            {
                Session.SendWhisper("¡Por favor ingrese un número válido como la cantidad que le gustaría apostar!", 1);
                return;
            }

            // Check if its a multiple of 5
            if (Convert.ToDouble((double)Amount / 5) != Math.Floor(Convert.ToDouble((double)Amount / 5)))
            {
                Session.SendWhisper("Sólo puede apostar dinero en múltiplos de 5", 1);
                return;
            }

            if (Player.TotalAmount < Amount)
                Amount = Player.TotalAmount;

            if (Amount < Game.MinimumBet(Number) && Amount != Player.TotalAmount)
            {
                Session.SendWhisper("Debes apostar al menos $" + String.Format("{0:N0}", Game.MinimumBet(Number)) + " Para que coincida con la olla actual!", 1);
                return;
            }

            if (Session.GetHabbo().Credits < Amount)
            {
                Session.SendWhisper("¡No tienes mucho dinero para apostar!", 1);
                return;
            }

            Session.Shout("*Coloca una apuesta de $" + String.Format("{0:N0}", Amount) + " a los juegos*", 4);

            Session.GetHabbo().Credits -= Amount;
            Session.GetHabbo().UpdateCreditsBalance();

            Game.PlacePotFurni(Number, Amount);
            Game.SpawnStartingBet(Number);
            Game.ChangeTurn();
            return;
        }
    }
}