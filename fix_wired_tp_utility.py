import re

with open("Polar RP/HabboHotel/Items/Wired/WiredBoxTypeUtility.cs", "r") as f:
    content = f.read()

if 'case 97:' not in content:
    content = content.replace('case 96:', 'case 96:\n                    return WiredBoxType.EffectGiveHanditem;\n                case 97:\n                    return WiredBoxType.EffectTeleportToRoom;')

if 'wf_act_teleport_to_room' not in content:
    content = content.replace('case "wf_act_give_handitem": return WiredBoxType.EffectGiveHanditem;', 'case "wf_act_give_handitem": return WiredBoxType.EffectGiveHanditem;\n                case "wf_act_teleport_to_room": return WiredBoxType.EffectTeleportToRoom;')

if 'WiredBoxType.EffectTeleportToRoom' not in content:
    content = content.replace('case WiredBoxType.EffectSetRotation:', 'case WiredBoxType.EffectTeleportToRoom:\n                    return 55;\n                case WiredBoxType.EffectSetRotation:')

with open("Polar RP/HabboHotel/Items/Wired/WiredBoxTypeUtility.cs", "w") as f:
    f.write(content)
