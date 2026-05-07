using System.Collections.Generic;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;

namespace Polar.HabboHotel.Items.Wired
{
    public class WiredContext
    {
        public Habbo Triggerer { get; set; }
        public List<Habbo> SelectedUsers { get; set; }
        public List<Item> SelectedItems { get; set; }

        public WiredContext(Habbo triggerer = null)
        {
            Triggerer = triggerer;
            SelectedUsers = new List<Habbo>();
            SelectedItems = new List<Item>();

            if (triggerer != null)
                SelectedUsers.Add(triggerer);
        }
    }
}