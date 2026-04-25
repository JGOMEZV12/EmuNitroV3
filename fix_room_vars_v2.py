import re

with open("Polar RP/HabboHotel/Rooms/Room.cs", "r") as f:
    content = f.read()

# Add public property/field
if 'public ConcurrentDictionary<string, string> WiredVariables' not in content:
    content = content.replace('public int IdleTime { get; set; }', 'public int IdleTime { get; set; }\n        public ConcurrentDictionary<string, string> WiredVariables;')

# Add initialization in constructor
if 'this.WiredVariables = new ConcurrentDictionary<string, string>();' not in content:
    # Find the end of constructor assignments
    content = content.replace('this.noPoolAnswers = new List<int>();', 'this.noPoolAnswers = new List<int>();\n            this.WiredVariables = new ConcurrentDictionary<string, string>();')

with open("Polar RP/HabboHotel/Rooms/Room.cs", "w") as f:
    f.write(content)
