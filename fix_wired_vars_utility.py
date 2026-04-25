import re

with open("Polar RP/HabboHotel/Items/Wired/WiredBoxTypeUtility.cs", "r") as f:
    content = f.read()

# Add to FromWiredId
if 'case 89:' not in content:
    content = content.replace('case 88:', 'case 88:\n                    return WiredBoxType.ConditionIsDancing;\n                case 89:\n                    return WiredBoxType.EffectSetVariable;\n                case 90:\n                    return WiredBoxType.EffectVariableAdd;\n                case 91:\n                    return WiredBoxType.ConditionVariableIsEqual;')

# Add to FromInteractionType
if 'wf_act_set_variable' not in content:
    content = content.replace('// Bot Effects', '// Variables\n                case "wf_act_set_variable": return WiredBoxType.EffectSetVariable;\n                case "wf_act_variable_add": return WiredBoxType.EffectVariableAdd;\n                case "wf_cnd_variable_is_equal": return WiredBoxType.ConditionVariableIsEqual;\n\n                // Bot Effects')

# Add to GetWiredId
if 'WiredBoxType.EffectSetVariable' not in content:
    content = content.replace('case WiredBoxType.EffectSetRotation:', 'case WiredBoxType.EffectSetVariable:\n                    return 47;\n                case WiredBoxType.EffectVariableAdd:\n                    return 48;\n                case WiredBoxType.ConditionVariableIsEqual:\n                    return 49;\n                case WiredBoxType.EffectSetRotation:')

with open("Polar RP/HabboHotel/Items/Wired/WiredBoxTypeUtility.cs", "w") as f:
    f.write(content)
