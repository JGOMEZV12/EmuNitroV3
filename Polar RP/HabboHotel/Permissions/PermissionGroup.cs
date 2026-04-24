using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Polar.HabboHotel.Permissions
{
    public class PermissionGroup
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Badge { get; set; }
        public string prefix { get; set; }


        public string prefixColor { get; set; }
        public bool hasPrefix { get; set; }
        public PermissionGroup(string Name, string Description, string Badge, string Prefix, string prefixColor)
        {
            this.Name = Name;
            this.Description = Description;
            this.Badge = Badge;
            this.prefix = Prefix;
            this.prefixColor = prefixColor;
            this.hasPrefix = !string.IsNullOrEmpty(this.prefix);
        }

    }
}
