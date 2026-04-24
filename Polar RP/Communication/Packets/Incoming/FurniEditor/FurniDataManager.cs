using System;
using System.IO;
using System.Text.Json;
using Polar.Core;

namespace Polar.Communication.Packets.Incoming.FurniEditor
{
    /// <summary>
    /// Manages reading of FurnitureData.json entries.
    /// Resolves the file path from emulator config keys.
    /// </summary>
    public static class FurniDataManager
    {
        /// <summary>
        /// Get the JSON string for a specific item from FurnitureData.json.
        /// Returns "{}" if not found or on error.
        /// </summary>
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

        /// <summary>
        /// Resolve the path to FurnitureData.json from emulator config.
        /// </summary>
        private static string ResolveFurniDataPath()
        {
            try
            {
                string configPath = PolarEnvironment.GetConfig().data["furni.editor.renderer.config.path"];

                if (string.IsNullOrEmpty(configPath))
                {
                    // Fallback: try base path
                    string basePath = PolarEnvironment.GetConfig().data["furni.editor.asset.base.path"];
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
                    string furniUrl = furniUrlEl.GetString() ?? "";

                    // Skip unresolved placeholders like ${gamedata.url}
                    if (furniUrl.Contains("${"))
                    {
                        string basePath = PolarEnvironment.GetConfig().data["furni.editor.asset.base.path"];
                        if (!string.IsNullOrEmpty(basePath))
                        {
                            string candidate = Path.Combine(basePath, "FurnitureData.json");
                            if (File.Exists(candidate)) return candidate;
                        }
                        return null;
                    }

                    // Strip query string
                    string cleanUrl = furniUrl.Contains("?")
                        ? furniUrl.Substring(0, furniUrl.IndexOf('?'))
                        : furniUrl;

                    // Local path — use directly
                    if (!cleanUrl.StartsWith("http"))
                        return cleanUrl;

                    // HTTP URL — derive local path from base
                    string basePathHttp = PolarEnvironment.GetConfig().data["furni.editor.asset.base.path"];
                    if (!string.IsNullOrEmpty(basePathHttp))
                    {
                        string filename = cleanUrl.Substring(cleanUrl.LastIndexOf('/') + 1);
                        return Path.Combine(basePathHttp, filename);
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
