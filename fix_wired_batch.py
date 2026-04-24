import re

with open("Polar RP/HabboHotel/Rooms/Instance/WiredComponent.cs", "r") as f:
    content = f.read()

# Add using System.Linq if not present
if "using System.Linq;" not in content:
    content = "using System.Linq;\n" + content

# Add LoadWiredBoxes method and update LoadWiredBox to use shared logic if possible
# Or just add the batch method

new_methods = """
    public void LoadWiredBoxes(IEnumerable<Item> items)
    {
        var wiredItems = items.Where(i => i.IsWired).ToList();
        if (wiredItems.Count == 0) return;

        var ids = string.Join(",", wiredItems.Select(i => i.Id));
        DataTable table;
        using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
        {
            dbClient.SetQuery($"SELECT * FROM wired_items WHERE id IN ({ids})");
            table = dbClient.getTable();
        }

        var rows = table?.Rows.Cast<DataRow>().ToDictionary(r => Convert.ToInt32(r["id"])) ?? new Dictionary<int, DataRow>();

        foreach (var item in wiredItems)
        {
            var newBox = GenerateNewBox(item);
            if (newBox == null) continue;

            if (rows.TryGetValue(item.Id, out var row))
            {
                ApplyRowToBox(newBox, row);
            }
            else
            {
                newBox.ItemsData = "";
                newBox.StringData = "";
                newBox.BoolData = false;
                SaveBox(newBox);
            }

            if (!AddBox(newBox))
            {
                TryRemove(newBox.Item.Id);
                AddBox(newBox);
            }
        }
    }

    private void ApplyRowToBox(IWiredItem newBox, DataRow row)
    {
        string rawString = Convert.ToString(row["string"]);
        newBox.StringData = string.IsNullOrEmpty(rawString)
            ? newBox.Type switch
            {
                WiredBoxType.ConditionMatchStateAndPosition or
                WiredBoxType.ConditionDontMatchStateAndPosition or
                WiredBoxType.EffectMatchPosition => "0;0;0",
                WiredBoxType.ConditionUserCountInRoom or
                WiredBoxType.ConditionUserCountDoesntInRoom or
                WiredBoxType.EffectMoveAndRotate => "0;0",
                WiredBoxType.ConditionFurniHasNoFurni => "0",
                _ => ""
            }
            : rawString;

        newBox.BoolData = Convert.ToInt32(row["bool"]) == 1;
        newBox.ItemsData = Convert.ToString(row["items"]);

        if (newBox is IWiredCycle cycle)
            cycle.Delay = Convert.ToInt32(row["delay"]);

        foreach (var str in newBox.ItemsData.Split(';'))
        {
            var sId = str.Contains(':') ? str.Split(':')[0] : str;
            if (int.TryParse(sId, out int id))
            {
                var selectedItem = _room.GetRoomItemHandler().GetItem(id);
                if (selectedItem != null)
                    newBox.SetItems.TryAdd(selectedItem.Id, selectedItem);
            }
        }
    }
"""

# Replace old LoadWiredBox and insert new methods
content = re.sub(r'public IWiredItem LoadWiredBox\(Item item\).*?return newBox;\s+}',
                 'public IWiredItem LoadWiredBox(Item item) { LoadWiredBoxes(new[] { item }); TryGet(item.Id, out var box); return box; }\n' + new_methods,
                 content, flags=re.DOTALL)

with open("Polar RP/HabboHotel/Rooms/Instance/WiredComponent.cs", "w") as f:
    f.write(content)
