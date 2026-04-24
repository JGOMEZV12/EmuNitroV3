using System.Collections.Generic;
using System.Linq;
using Polar.HabboHotel.Rooms.AI;
using Polar.HabboHotel.Rooms.Chat.Pets.Commands;

namespace Polar.Communication.Packets.Outgoing.Rooms.AI.Pets
{
    internal class PetTrainingPanelComposer : ServerPacket
    {
        public PetTrainingPanelComposer(Pet pet)
            : base(ServerPacketHeader.PetTrainingPanelMessageComposer)
        {
            // Obtener todos los comandos del PetCommandManager
            Dictionary<string, PetCommand> allCommands = PolarEnvironment.GetGame()
                .GetChatManager()
                .GetPetCommands()
                .GetPetCommands();

            // Separar comandos únicos por ID
            List<PetCommand> uniqueCommands = allCommands.Values
                .GroupBy(c => c.Id)
                .Select(g => g.First())
                .OrderBy(c => c.Id)
                .ToList();

            // Comandos habilitados según el nivel del pet
            List<PetCommand> enabledCommands = uniqueCommands
                .Where(c => pet.Level >= c.Level)
                .ToList();

            // Escribir ID del pet
            base.WriteInteger(pet.PetId);

            // Todos los comandos disponibles
            base.WriteInteger(uniqueCommands.Count);
            foreach (PetCommand cmd in uniqueCommands)
            {
                base.WriteInteger(cmd.Id);
            }

            // Comandos habilitados por nivel
            base.WriteInteger(enabledCommands.Count);
            foreach (PetCommand cmd in enabledCommands)
            {
                base.WriteInteger(cmd.Id);
            }
        }
    }
}