import re

with open("Polar RP/HabboHotel/Rooms/Instance/WiredComponent.cs", "r") as f:
    content = f.read()

if 'WiredBoxType.EffectGiveHanditem' not in content:
    content = content.replace('WiredBoxType.EffectMoveFurniXYZ            => new MoveFurniXYZBox(_room, item),', 'WiredBoxType.EffectMoveFurniXYZ            => new MoveFurniXYZBox(_room, item),\n        WiredBoxType.EffectGiveHanditem            => new GiveHanditemBox(_room, item),')

with open("Polar RP/HabboHotel/Rooms/Instance/WiredComponent.cs", "w") as f:
    f.write(content)
