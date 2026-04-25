import re

with open("Polar RP/HabboHotel/Rooms/Instance/WiredComponent.cs", "r") as f:
    content = f.read()

if 'WiredBoxType.EffectTeleportToRoom' not in content:
    content = content.replace('WiredBoxType.EffectGiveHanditem            => new GiveHanditemBox(_room, item),', 'WiredBoxType.EffectGiveHanditem            => new GiveHanditemBox(_room, item),\n        WiredBoxType.EffectTeleportToRoom         => new TeleportToRoomBox(_room, item),')

with open("Polar RP/HabboHotel/Rooms/Instance/WiredComponent.cs", "w") as f:
    f.write(content)
