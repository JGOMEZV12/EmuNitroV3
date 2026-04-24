using System;

namespace Polar.HabboHotel.Rooms.Chat.Pets.Commands
{
    public class PetCommand
    {
        public int Id;
        public string Input;
        public int Level;

        public PetCommand(int CommandId, string CommandInput, int level)
        {
            this.Id = CommandId;
            this.Input = CommandInput;
            this.Level = level;

        }
    }
}