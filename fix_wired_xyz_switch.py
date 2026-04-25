import re

with open("Polar RP/HabboHotel/Rooms/Instance/WiredComponent.cs", "r") as f:
    content = f.read()

if 'WiredBoxType.EffectMoveFurniXYZ' not in content:
    content = content.replace('WiredBoxType.ConditionVariableIsLessThan    => new VariableIsLessThanBox(_room, item),', 'WiredBoxType.ConditionVariableIsLessThan    => new VariableIsLessThanBox(_room, item),\n        WiredBoxType.EffectMoveFurniXYZ            => new MoveFurniXYZBox(_room, item),')

with open("Polar RP/HabboHotel/Rooms/Instance/WiredComponent.cs", "w") as f:
    f.write(content)
