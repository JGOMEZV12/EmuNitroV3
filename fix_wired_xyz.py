import re

with open("Polar RP/HabboHotel/Items/Wired/WiredBoxTypeUtility.cs", "r") as f:
    content = f.read()

if 'case 95:' not in content:
    content = content.replace('case 94:', 'case 94:\n                    return WiredBoxType.ConditionVariableIsLessThan;\n                case 95:\n                    return WiredBoxType.EffectMoveFurniXYZ;')

if 'wf_act_move_furni_xyz' not in content:
    content = content.replace('case "wf_act_bot_give_handitem": return WiredBoxType.EffectBotGivesHanditemBox;', 'case "wf_act_bot_give_handitem": return WiredBoxType.EffectBotGivesHanditemBox;\n                case "wf_act_move_furni_xyz": return WiredBoxType.EffectMoveFurniXYZ;')

if 'WiredBoxType.EffectMoveFurniXYZ' not in content:
    content = content.replace('case WiredBoxType.EffectSetRotation:', 'case WiredBoxType.EffectMoveFurniXYZ:\n                    return 53;\n                case WiredBoxType.EffectSetRotation:')

with open("Polar RP/HabboHotel/Items/Wired/WiredBoxTypeUtility.cs", "w") as f:
    f.write(content)
