using System;
using System.IO;
using System.Text.Json;
using Polar.Core;

namespace Polar.Communication.Packets.Incoming.FurniEditor
{
    public static class FurniDataManager
    {
        public static string GetItemJson(int itemId)
        {
            try
            {
                string path = ResolveFurniDataPath();
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                    return "{}";

                string content = File.ReadAllText(path);
                using JsonDocument doc = JsonDocument.Parse(content);
                JsonElement root = doc.RootElement;

                foreach (string section in new[] { "roomitemtypes", "wallitemtypes" })
                {
                    if (!root.TryGetProperty(section, out JsonElement sectionEl)) continue;
                    if (!sectionEl.TryGetProperty("furnitype", out JsonElement types)) continue;

                    foreach (JsonElement el in types.EnumerateArray())
                    {
                        if (el.TryGetProperty("id", out JsonElement idEl) && idEl.GetInt32() == itemId)
                            return el.GetRawText();
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.WriteLine($"[FurniDataManager] Failed to read FurnitureData.json for item {itemId}: {ex.Message}");
            }

            return "{}";
        }

        private static string ResolveFurniDataPath()
        {
            try
            {
                string configPath = PolarEnvironment.GetConfig().data["furni.editor.renderer.config.path"];
                string basePath = PolarEnvironment.GetConfig().data["furni.editor.asset.base.path"];

                if (string.IsNullOrEmpty(configPath))
                {
                    if (!string.IsNullOrEmpty(basePath))
                    {
                        string candidate = Path.Combine(basePath, "FurnitureData.json");
                        if (File.Exists(candidate)) return candidate;
                    }
                    return null;
                }

                if (!File.Exists(configPath)) return null;

                string rendererContent = File.ReadAllText(configPath);
                using JsonDocument rendererDoc = JsonDocument.Parse(rendererContent);
                JsonElement rendererObj = rendererDoc.RootElement;

                if (rendererObj.TryGetProperty("furnidata.url", out JsonElement furniUrlEl))
                {
                    string furniUrl = "";
                    if (furniUrlEl.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var el in furniUrlEl.EnumerateArray())
                        {
                            furniUrl = el.GetString() ?? "";
                            if (!string.IsNullOrEmpty(furniUrl)) break;
                        }
                    }
                    else
                    {
                        furniUrl = furniUrlEl.GetString() ?? "";
                    }

                    if (string.IsNullOrEmpty(furniUrl) || furniUrl.Contains("${"))
                    {
                        if (!string.IsNullOrEmpty(basePath))
                        {
                            string candidate = Path.Combine(basePath, "FurnitureData.json");
                            if (File.Exists(candidate)) return candidate;
                        }
                        return null;
                    }

                    string cleanUrl = furniUrl.Contains("?") ? furniUrl.Substring(0, furniUrl.IndexOf('?')) : furniUrl;

                    if (!cleanUrl.StartsWith("http"))
                        return cleanUrl;

                    if (!string.IsNullOrEmpty(basePath))
                    {
                        string filename = cleanUrl.Substring(cleanUrl.LastIndexOf('/') + 1);
                        return Path.Combine(basePath, filename);
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.WriteLine($"[FurniDataManager] Failed to resolve FurnitureData.json path: {ex.Message}");
            }

            return null;
        }
    }
}