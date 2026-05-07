using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Wired.Boxes.Effects
{
    class NegativeExecuteWiredStacksBox : ExecuteWiredStacksBox
    {
        public NegativeExecuteWiredStacksBox(Room instance, Item item)
            : base(instance, item) { }

        public override WiredBoxType Type => WiredBoxType.EffectNegativeExecuteWiredStacks;
    }
}