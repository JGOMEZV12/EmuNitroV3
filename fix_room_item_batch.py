import re

with open("Polar RP/HabboHotel/Rooms/RoomItemHandling.cs", "r") as f:
    content = f.read()

# Replace the individual wired loading with batch loading
old_wired = """                else if (floorItem.IsWired)
                {
                    if (_room?.GetWired() == null) continue;
                    _room.GetWired().LoadWiredBox(floorItem);
                }"""

# Since I modified the logic of LoadFurniture, I should just update the loop at the end
content = content.replace(old_wired, "")

# Add batch call after the loop
content = content.replace('HopperCount++;', 'HopperCount++;').replace('JukeboxCount++;', 'JukeboxCount++;')
# This is getting complicated. Let's just find the end of the post-process loop.

content = content.replace('JukeboxCount++;\n            }', 'JukeboxCount++;\n            }\n            _room?.GetWired()?.LoadWiredBoxes(_floorItems.Values);')

with open("Polar RP/HabboHotel/Rooms/RoomItemHandling.cs", "w") as f:
    f.write(content)
