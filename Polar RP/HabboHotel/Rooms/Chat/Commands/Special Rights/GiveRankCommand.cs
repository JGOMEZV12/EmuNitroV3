using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Polar.Communication.Packets.Outgoing.Moderation;

namespace Polar.HabboHotel.Rooms.Chat.Commands.SpecialRights
{
    class GiveRankCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_give_rank"; }
        }

        public string Parameters
        {
            get { return "%username% %amount%"; }
        }

        public string Description
        {
            get { return "Dar rango a usuarios."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length != 3)
            {
                Session.SendWhisper("Debe ingresar el nombre de usuario y el rango que desea darles.", 1);
                return;
            }

            var TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);

            if (TargetClient == null)
            {
                Session.SendWhisper("Este usuario no se pudo encontrar! Tal vez ellos están fuera de línea.", 1);
                return;
            }

            if (TargetClient.GetHabbo() == null || TargetClient.GetRoomUser() == null)
            {
                Session.SendWhisper("Este usuario no se pudo encontrar! Tal vez ellos están fuera de línea.", 1);
                return;
            }

            int Rank;
            if (int.TryParse(Params[2], out Rank))
            {
                int OldRank = TargetClient.GetHabbo().Rank;

                if (Rank <= 0 || Rank > 9)
                {
                    Session.SendWhisper("Lo siento, este rango no existe!", 1);
                    return;
                }

                if (OldRank == Rank)
                {
                    Session.SendWhisper("Este usuario ya tiene [RankID: " + Rank + "]", 1);
                    return;
                }

                TargetClient.GetHabbo().Rank = Rank;
                TargetClient.GetHabbo().InitPermissions();

                using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                    dbClient.RunQuery("UPDATE `users` SET `rank` = '" + Rank + "' WHERE `id` = '" + TargetClient.GetHabbo().Id + "'");

                if (TargetClient.GetHabbo().GetPermissions().HasRight("mod_tickets"))
                {
                    TargetClient.SendMessage(new ModeratorInitComposer(
                        PolarEnvironment.GetGame().GetModerationManager().UserMessagePresets,
                        PolarEnvironment.GetGame().GetModerationManager().RoomMessagePresets,
                        PolarEnvironment.GetGame().GetModerationManager().UserActionPresets,
                        PolarEnvironment.GetGame().GetModerationTool().GetTickets));
                }

                if (OldRank > 1 && Rank == 1)
                    TargetClient.SendNotification("Ha sido degradado, ya no tiene acceso a las Herramientas de modificación.\n\nPor favor, recargar para deshacerse de la caja molesta.");
                else if (OldRank > Rank)
                    TargetClient.SendNotification("Lo siento...\n\nHas sido degradado a [RankID: " + Rank + "] Por " + Session.GetHabbo().Username + ".\n\nNo hay necesidad de volver a cargar como sus permisos se han alterado automáticamente.");
                else
                    TargetClient.SendNotification("Felicitaciones y bienvenida al Equipo de Personal de " + PolarEnvironment.GetConfig().data["hotel.name"] + "!\n\nHas sido promovido a [RankID: " + Rank + "] Por " + Session.GetHabbo().Username + "\n\nNo es necesario volver a cargar, ya que todos los permisos han sido activados.!");

                Session.Shout("*Utiliza sus poderes divinos y da a " + TargetClient.GetHabbo().Username + " [RankID: " + Rank + "]*", 23);
            }
            else
                Session.SendWhisper("¡Por favor ingrese un número valido!", 1);
        }
    }
}
