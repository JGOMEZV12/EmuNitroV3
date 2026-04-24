import re

with open("Polar RP/HabboHotel/Items/Wired/WiredBoxType.cs", "r") as f:
    content = f.read()
    enum_values = re.findall(r"([A-Z][a-zA-Z0-9]+),?", content)

with open("Polar RP/HabboHotel/Rooms/Instance/WiredComponent.cs", "r") as f:
    switch_content = f.read()

missing = []
for val in enum_values:
    if val == "None": continue
    if f"WiredBoxType.{val}" not in switch_content:
        missing.append(val)

print("Missing in WiredComponent switch:")
for m in missing:
    print(m)
