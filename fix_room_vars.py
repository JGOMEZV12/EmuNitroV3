import re

with open("Polar RP/HabboHotel/Rooms/Room.cs", "r") as f:
    content = f.read()

if 'public ConcurrentDictionary<string, string> WiredVariables' not in content:
    content = content.replace('public ConcurrentDictionary<int, int> WiredScoreBordWeek;', 'public ConcurrentDictionary<int, int> WiredScoreBordWeek;\n        public ConcurrentDictionary<string, string> WiredVariables;')
    content = content.replace('this.WiredScoreBordWeek = new ConcurrentDictionary<int, int>();', 'this.WiredScoreBordWeek = new ConcurrentDictionary<int, int>();\n            this.WiredVariables = new ConcurrentDictionary<string, string>();')

with open("Polar RP/HabboHotel/Rooms/Room.cs", "w") as f:
    f.write(content)
