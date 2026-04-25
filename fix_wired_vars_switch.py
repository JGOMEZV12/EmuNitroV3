import re

with open("Polar RP/HabboHotel/Rooms/Instance/WiredComponent.cs", "r") as f:
    content = f.read()

new_cases = """        WiredBoxType.EffectSetVariable             => new SetVariableBox(_room, item),
        WiredBoxType.EffectVariableAdd             => new VariableAddBox(_room, item),
        WiredBoxType.ConditionVariableIsEqual      => new VariableIsEqualBox(_room, item),"""

if "WiredBoxType.EffectSetVariable" not in content:
    content = content.replace('WiredBoxType.AddonUnseen                   => new AddonUnseenBox(_room, item),', 'WiredBoxType.AddonUnseen                   => new AddonUnseenBox(_room, item),\n' + new_cases)

with open("Polar RP/HabboHotel/Rooms/Instance/WiredComponent.cs", "w") as f:
    f.write(content)
