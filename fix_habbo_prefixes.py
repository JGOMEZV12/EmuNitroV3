import re

with open("Polar RP/HabboHotel/Users/Habbo.cs", "r") as f:
    content = f.read()

# Add private field
if 'private PrefixesComponent _prefixes;' not in content:
    content = content.replace('private PermissionComponent _permissions;', 'private PermissionComponent _permissions;\n        private Polar.HabboHotel.Users.Inventory.PrefixesComponent _prefixes;')

# Add public getter
if 'public Polar.HabboHotel.Users.Inventory.PrefixesComponent GetPrefixesComponent()' not in content:
    content = content.replace('public PermissionComponent GetPermissions()', 'public Polar.HabboHotel.Users.Inventory.PrefixesComponent GetPrefixesComponent() { return this._prefixes; }\n\n        public PermissionComponent GetPermissions()')

# Add initialization in Init() or constructor. Let's do it in InitInformation
if 'this._prefixes = new Polar.HabboHotel.Users.Inventory.PrefixesComponent(this);' not in content:
    content = content.replace('Relationships = data.Relations;', 'Relationships = data.Relations;\n            this._prefixes = new Polar.HabboHotel.Users.Inventory.PrefixesComponent(this);')

# Ensure UpdateDisplayName is called on load
if 'this._prefixes.UpdateDisplayName();' not in content:
     content = content.replace('this._prefixes = new Polar.HabboHotel.Users.Inventory.PrefixesComponent(this);', 'this._prefixes = new Polar.HabboHotel.Users.Inventory.PrefixesComponent(this);\n            this._prefixes.UpdateDisplayName();')

with open("Polar RP/HabboHotel/Users/Habbo.cs", "w") as f:
    f.write(content)
