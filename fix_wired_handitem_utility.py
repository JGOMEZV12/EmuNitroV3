import re

with open("Polar RP/HabboHotel/Items/Wired/WiredBoxTypeUtility.cs", "r") as f:
    content = f.read()

if 'case 96:' not in content:
    content = content.replace('case 95:', 'case 95:\n                    return WiredBoxType.EffectMoveFurniXYZ;\n                case 96:\n                    return WiredBoxType.EffectGiveHanditem;')

if 'wf_act_give_handitem' not in content:
    content = content.replace('case "wf_act_move_furni_xyz": return WiredBoxType.EffectMoveFurniXYZ;', 'case "wf_act_move_furni_xyz": return WiredBoxType.EffectMoveFurniXYZ;\n                case "wf_act_give_handitem": return WiredBoxType.EffectGiveHanditem;')

if 'WiredBoxType.EffectGiveHanditem' not in content:
    content = content.replace('case WiredBoxType.EffectSetRotation:', 'case WiredBoxType.EffectGiveHanditem:\n                    return 54;\n                case WiredBoxType.EffectSetRotation:')

with open("Polar RP/HabboHotel/Items/Wired/WiredBoxTypeUtility.cs", "w") as f:
    f.write(content)
