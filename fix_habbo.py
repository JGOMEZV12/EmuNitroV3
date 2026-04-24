import re

with open("Polar RP/HabboHotel/Users/Habbo.cs", "r") as f:
    content = f.read()

# Find the redundant block at the end of constructor
# It starts after InitPermissions() and ends before the closing brace of the constructor
pattern = re.compile(r'this\.InitPermissions\(\);\s+if \(!IsBot\).*?#endregion\s+}', re.DOTALL)
new_content = pattern.sub('this.InitPermissions();\n        }', content)

with open("Polar RP/HabboHotel/Users/Habbo.cs", "w") as f:
    f.write(new_content)
